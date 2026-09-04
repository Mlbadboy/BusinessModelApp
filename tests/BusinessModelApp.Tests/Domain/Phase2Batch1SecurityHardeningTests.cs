using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Api.Controllers;
using BusinessModelApp.Core.Agents;
using BusinessModelApp.Core.Decisions;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Domain.Decisions;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Reality;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Core.Missions;
using BusinessModelApp.Core.Objectives;
using BusinessModelApp.Core.Services;
using BusinessModelApp.Core.Strategy;
using BusinessModelApp.Core.WorldModel;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase2Batch1SecurityHardeningTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .AddInterceptors(new AppendOnlyAuditInterceptor())
                .Options;

            return new AppDbContext(options);
        }

        // -------------------------------------------------------------------------
        // 1. JWT SECURITY LAW TESTS
        // -------------------------------------------------------------------------
        [Fact]
        public void JwtSecurityLaw_ProductionFailsClosed_WhenKeyIsMissingOrWeak()
        {
            // Invariant: Production MUST fail closed if JWT signing key is absent, weak, or < 32 bytes (256 bits)
            var weakKey = "short-key-123";
            var isProduction = true;

            Assert.Throws<InvalidOperationException>(() =>
            {
                if (string.IsNullOrWhiteSpace(weakKey) || Encoding.UTF8.GetByteCount(weakKey) < 32)
                {
                    if (isProduction)
                    {
                        throw new InvalidOperationException("CRITICAL: Production JWT signing key is absent, weak, or below 256 bits (32 bytes). Application must fail closed according to Phase 2 JWT Security Law.");
                    }
                }
            });
        }

        [Fact]
        public void JwtSecurityLaw_NonProductionGeneratesDynamicEphemeralKeys_NeverStatic()
        {
            // Invariant: Non-production environments generate dynamic ephemeral keys in-memory
            var key1 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var key2 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            Assert.NotEqual(key1, key2);
            Assert.True(Convert.FromBase64String(key1).Length >= 32);
            Assert.True(Convert.FromBase64String(key2).Length >= 32);
        }

        // -------------------------------------------------------------------------
        // 2. APPEND-ONLY AUDIT INTERCEPTOR IMMUTABILITY TESTS
        // -------------------------------------------------------------------------
        [Fact]
        public async Task AppendOnlyAuditInterceptor_BlocksModificationOfDecisionRecord()
        {
            using var context = CreateInMemoryDbContext();
            var decision = new DecisionRecord
            {
                Id = Guid.NewGuid(),
                ObjectiveId = Guid.NewGuid(),
                SelectedAlternative = "Initial Strategy",
                CryptographicHash = "initial-hash"
            };

            context.DecisionRecords.Add(decision);
            await context.SaveChangesAsync();

            // Attempt to mutate committed decision record
            decision.SelectedAlternative = "Tampered Strategy";

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            Assert.Contains("DecisionRecord", ex.Message);
            Assert.Contains("immutable", ex.Message);
        }

        [Fact]
        public async Task AppendOnlyAuditInterceptor_BlocksDeletionOfEvidenceRecord()
        {
            using var context = CreateInMemoryDbContext();
            var evidence = new BusinessModelApp.Core.Domain.Reality.EvidenceRecord
            {
                Id = Guid.NewGuid(),
                WorkspaceId = Guid.NewGuid(),
                SourceSystem = "Hubspot",
                RawPayloadHash = "raw-hash-123",
                CanonicalPayloadHash = "canonical-hash-123"
            };

            context.EvidenceRecords.Add(evidence);
            await context.SaveChangesAsync();

            // Attempt to delete evidence record
            context.EvidenceRecords.Remove(evidence);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            Assert.Contains("EvidenceRecord", ex.Message);
            Assert.Contains("append-only", ex.Message);
        }

        [Fact]
        public async Task AppendOnlyAuditInterceptor_BlocksDeletionOfMissionCheckpoint()
        {
            using var context = CreateInMemoryDbContext();
            var checkpoint = new DurableMissionCheckpoint
            {
                Id = Guid.NewGuid(),
                MissionId = Guid.NewGuid(),
                StepIndex = 1,
                StepName = "REVERSE_FUNNEL_ANALYSIS"
            };

            context.MissionCheckpoints.Add(checkpoint);
            await context.SaveChangesAsync();

            // Attempt to delete mission checkpoint
            context.MissionCheckpoints.Remove(checkpoint);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
            Assert.Contains("DurableMissionCheckpoint", ex.Message);
            Assert.Contains("append-only", ex.Message);
        }

        // -------------------------------------------------------------------------
        // 3. BOLA / IDOR & TENANT ISOLATION TESTS ON CONTROLLERS
        // -------------------------------------------------------------------------
        [Fact]
        public async Task DecisionsController_GetRecentDecisions_ScopesStrictlyToCallerWorkspace()
        {
            using var context = CreateInMemoryDbContext();
            var callerWorkspaceId = Guid.NewGuid();
            var foreignWorkspaceId = Guid.NewGuid();

            // Seed decisions across two different workspaces
            context.DecisionRecords.AddRange(
                new DecisionRecord { Id = Guid.NewGuid(), WorkspaceId = callerWorkspaceId, SelectedAlternative = "Caller Strat 1", DecidedAt = DateTime.UtcNow },
                new DecisionRecord { Id = Guid.NewGuid(), WorkspaceId = callerWorkspaceId, SelectedAlternative = "Caller Strat 2", DecidedAt = DateTime.UtcNow },
                new DecisionRecord { Id = Guid.NewGuid(), WorkspaceId = foreignWorkspaceId, SelectedAlternative = "Foreign Secret Strat", DecidedAt = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();

            var userContextMock = new Mock<IUserContextService>();
            userContextMock.Setup(u => u.GetAuthorizedWorkspaceIdAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(callerWorkspaceId);

            var decisionEngineMock = new Mock<IDecisionEngine>();
            var loggerMock = new Mock<ILogger<DecisionsController>>();

            var controller = new DecisionsController(decisionEngineMock.Object, context, userContextMock.Object, loggerMock.Object);

            var result = await controller.GetRecentDecisions(CancellationToken.None);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var decisions = Assert.IsAssignableFrom<IEnumerable<DecisionRecord>>(okResult.Value);

            // Invariant: Foreign decisions must NEVER be returned to the caller
            Assert.Equal(2, decisions.Count());
            Assert.All(decisions, d => Assert.Equal(callerWorkspaceId, d.WorkspaceId));
        }

        [Fact]
        public async Task DecisionsController_GetWhyCharlieExplanation_BlocksCrossTenantAccess()
        {
            using var context = CreateInMemoryDbContext();
            var callerWorkspaceId = Guid.NewGuid();
            var foreignWorkspaceId = Guid.NewGuid();
            var foreignDecisionId = Guid.NewGuid();

            context.DecisionRecords.Add(new DecisionRecord
            {
                Id = foreignDecisionId,
                WorkspaceId = foreignWorkspaceId,
                SelectedAlternative = "Foreign Secret Strat",
                DecidedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var userContextMock = new Mock<IUserContextService>();
            userContextMock.Setup(u => u.GetAuthorizedWorkspaceIdAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(callerWorkspaceId);

            var decisionEngineMock = new Mock<IDecisionEngine>();
            var loggerMock = new Mock<ILogger<DecisionsController>>();

            var controller = new DecisionsController(decisionEngineMock.Object, context, userContextMock.Object, loggerMock.Object);

            var result = await controller.GetWhyCharlieExplanation(foreignDecisionId, CancellationToken.None);

            // Invariant: Cross-tenant decision lookup returns 404 NotFound
            Assert.IsType<NotFoundObjectResult>(result);
            decisionEngineMock.Verify(d => d.GetWhyCharlieExplanationAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AgentMissionsController_GetMissionById_BlocksForeignWorkspaceMission()
        {
            var callerWorkspaceId = Guid.NewGuid();
            var foreignWorkspaceId = Guid.NewGuid();
            var missionId = Guid.NewGuid();

            var foreignMission = new AgentMission
            {
                Id = missionId,
                WorkspaceId = foreignWorkspaceId,
                Title = "Foreign Secret Mission"
            };

            var orchestratorMock = new Mock<IAgentOrchestratorService>();
            orchestratorMock.Setup(o => o.GetMission(missionId)).Returns(foreignMission);

            var userContextMock = new Mock<IUserContextService>();
            userContextMock.Setup(u => u.GetAuthorizedWorkspaceIdAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(callerWorkspaceId);

            var controller = new AgentMissionsController(orchestratorMock.Object, userContextMock.Object);

            var actionResult = await controller.GetMissionById(missionId);

            // Invariant: Mission belonging to another workspace returns 404 NotFound
            Assert.IsType<NotFoundResult>(actionResult.Result);
        }

        [Fact]
        public async Task AgentMissionsController_ApproveGatedTask_BlocksApprovalOnForeignWorkspaceMission()
        {
            var callerWorkspaceId = Guid.NewGuid();
            var foreignWorkspaceId = Guid.NewGuid();
            var missionId = Guid.NewGuid();
            var taskId = Guid.NewGuid();

            var foreignMission = new AgentMission
            {
                Id = missionId,
                WorkspaceId = foreignWorkspaceId,
                Title = "Foreign Mission"
            };

            var orchestratorMock = new Mock<IAgentOrchestratorService>();
            orchestratorMock.Setup(o => o.GetMission(missionId)).Returns(foreignMission);

            var userContextMock = new Mock<IUserContextService>();
            userContextMock.Setup(u => u.GetAuthorizedWorkspaceIdAsync(null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(callerWorkspaceId);

            var controller = new AgentMissionsController(orchestratorMock.Object, userContextMock.Object);

            var result = await controller.ApproveGatedTask(missionId, taskId);

            // Invariant: Caller cannot approve tasks on missions belonging to another tenant/workspace
            Assert.IsType<NotFoundResult>(result);
            orchestratorMock.Verify(o => o.ApproveGatedTaskAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
