using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Api.Controllers;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public sealed class Phase4Batch431AgentHarnessTests
    {
        private readonly IAgentHarnessStore _store;
        private readonly IAgentHarnessService _service;
        private readonly AgentHarnessController _controller;

        public Phase4Batch431AgentHarnessTests()
        {
            _store = new InMemoryAgentHarnessStore();
            var policy = new AgentSpawnPolicy
            {
                MaxSpawnDepth = 2,
                MaxChildAgents = 2,
                MaxActiveAgentsPerTenant = 5
            };
            _service = new AgentHarnessService(_store, policy);
            _controller = new AgentHarnessController(_service);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-harness-431";
        }

        // =========================================================================
        // Family 1: Provider Adapters (Cognitive Capacity Only)
        // =========================================================================

        [Fact]
        public async Task HARN431_01_ProviderAdapters_SupplyCognitiveOutputWithoutAuthority()
        {
            var native = new CharlieNativeRuntime();
            var hermes = new HermesAdapter();
            var deepseek = new DeepSeekHarnessAdapter();
            var openhands = new OpenHandsAdapter();

            native.ProviderName.Should().Be("CharlieNativeRuntime");
            hermes.ProviderName.Should().Be("HermesAdapter");
            deepseek.ProviderName.Should().Be("DeepSeekHarnessAdapter");
            openhands.ProviderName.Should().Be("OpenHandsAdapter");

            var ctx = new Dictionary<string, object> { ["Goal"] = "ProspectResearch" };

            var nativeRes = await native.ExecuteCognitiveStepAsync("tenant-harness-431", "Plan step", ctx);
            var hermesRes = await hermes.ExecuteCognitiveStepAsync("tenant-harness-431", "Evaluate lead", ctx);
            var deepseekRes = await deepseek.ExecuteCognitiveStepAsync("tenant-harness-431", "Analyze financials", ctx);
            var openhandsRes = await openhands.ExecuteCognitiveStepAsync("tenant-harness-431", "Extract table", ctx);

            nativeRes.Should().Contain("[CharlieNative]");
            hermesRes.Should().Contain("[HermesAdapter]");
            deepseekRes.Should().Contain("[DeepSeekHarness]");
            openhandsRes.Should().Contain("[OpenHands]");
        }

        // =========================================================================
        // Family 2: Session & Trajectory Management (Law I39-T)
        // =========================================================================

        [Fact]
        public async Task HARN431_02_SessionAndTrajectoryLifecycle_RecordsStepsAccurately()
        {
            var session = await _service.StartSessionAsync("tenant-harness-431", "agent-sales-01", "mission-q4-01");
            session.IsActive.Should().BeTrue();

            var step1 = new AgentTrajectoryStep
            {
                Thought = "Searching for accounts matching ICP criteria",
                ProposedAction = "Execute search",
                ToolName = "Browser.Search",
                ToolInputJson = "{\"query\":\"enterprise cloud companies\"}",
                ToolOutputJson = "{\"count\":15}",
                IsSuccess = true
            };
            await _service.RecordTrajectoryStepAsync("tenant-harness-431", session.SessionId, step1);

            var step2 = new AgentTrajectoryStep
            {
                Thought = "Extracting decision maker titles",
                ProposedAction = "Navigate to team page",
                ToolName = "Browser.Navigate",
                ToolInputJson = "{\"url\":\"https://example.com/team\"}",
                ToolOutputJson = "{\"status\":200}",
                IsSuccess = true
            };
            var traj = await _service.RecordTrajectoryStepAsync("tenant-harness-431", session.SessionId, step2);

            traj.Steps.Should().HaveCount(2);
            traj.Steps[0].StepIndex.Should().Be(1);
            traj.Steps[1].StepIndex.Should().Be(2);

            var ended = await _service.EndSessionAsync("tenant-harness-431", session.SessionId);
            ended.Should().BeTrue();

            var retrieved = await _service.GetSessionAsync("tenant-harness-431", session.SessionId);
            retrieved!.IsActive.Should().BeFalse();
        }

        // =========================================================================
        // Family 3: Governed Bounded Spawning (Law I39-U)
        // =========================================================================

        [Fact]
        public async Task HARN431_03_SpawningEnforcesMaxDepthAndChildLimits()
        {
            // Root agent (Depth 0)
            var root = await _service.SpawnSubAgentAsync("tenant-harness-431", new AgentSpawnRequest
            {
                TargetRoleTitle = "RevenueDirector",
                MissionId = "mission-root"
            });
            root.SpawnDepth.Should().Be(0);

            // Child 1 (Depth 1)
            var child1 = await _service.SpawnSubAgentAsync("tenant-harness-431", new AgentSpawnRequest
            {
                ParentAgentId = root.AgentId,
                TargetRoleTitle = "ProspectorOne",
                MissionId = "mission-child-1"
            });
            child1.SpawnDepth.Should().Be(1);

            // Child 2 (Depth 1)
            var child2 = await _service.SpawnSubAgentAsync("tenant-harness-431", new AgentSpawnRequest
            {
                ParentAgentId = root.AgentId,
                TargetRoleTitle = "ProspectorTwo",
                MissionId = "mission-child-2"
            });
            child2.SpawnDepth.Should().Be(1);

            // Child 3 from root (Exceeds MaxChildAgents = 2)
            var actExceedChildren = () => _service.SpawnSubAgentAsync("tenant-harness-431", new AgentSpawnRequest
            {
                ParentAgentId = root.AgentId,
                TargetRoleTitle = "ProspectorThree",
                MissionId = "mission-child-3"
            });
            await actExceedChildren.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*I39-U*exceeded max child limit*");

            // Grandchild from Child 1 (Depth 2 - Allowed)
            var grandchild = await _service.SpawnSubAgentAsync("tenant-harness-431", new AgentSpawnRequest
            {
                ParentAgentId = child1.AgentId,
                TargetRoleTitle = "ResearcherOne",
                MissionId = "mission-grandchild"
            });
            grandchild.SpawnDepth.Should().Be(2);

            // Great-grandchild from Grandchild (Depth 3 - Exceeds MaxSpawnDepth = 2)
            var actExceedDepth = () => _service.SpawnSubAgentAsync("tenant-harness-431", new AgentSpawnRequest
            {
                ParentAgentId = grandchild.AgentId,
                TargetRoleTitle = "SubWorker",
                MissionId = "mission-deep"
            });
            await actExceedDepth.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*I39-U*Max spawn depth*exceeded*");
        }

        [Fact]
        public async Task HARN431_04_ActiveInstanceLimitEnforced()
        {
            for (int i = 0; i < 5; i++)
            {
                await _service.SpawnSubAgentAsync("tenant-harness-431", new AgentSpawnRequest
                {
                    TargetRoleTitle = $"Worker-{i}"
                });
            }

            // 6th agent exceeds MaxActiveAgentsPerTenant = 5
            var act = () => _service.SpawnSubAgentAsync("tenant-harness-431", new AgentSpawnRequest
            {
                TargetRoleTitle = "Worker-Overflow"
            });
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*I39-U*Max active agents limit*reached*");
        }

        // =========================================================================
        // Family 4: Memory vs Context Isolation (Anti-Poisoning & Corroboration)
        // =========================================================================

        [Fact]
        public async Task HARN431_05_AssemblePrompt_FiltersUncorroboratedMemories()
        {
            var agentId = "agent-researcher-05";

            // Save uncorroborated memory (e.g. unverified hallucination or rumor)
            await _service.SaveAgentMemoryAsync("tenant-harness-431", new AgentMemoryEntry
            {
                AgentId = agentId,
                Key = "UnverifiedClaim",
                Content = "Customer verbally promised $1,000,000 contract without sign-off",
                IsCorroborated = false
            });

            // Save corroborated memory (grounded in verified evidence)
            await _service.SaveAgentMemoryAsync("tenant-harness-431", new AgentMemoryEntry
            {
                AgentId = agentId,
                Key = "VerifiedBudget",
                Content = "Procurement department published formal RFP budget of ₹500,000",
                IsCorroborated = true
            });

            var prompt = await _service.AssembleExecutionPromptAsync(
                "tenant-harness-431",
                agentId,
                "Formulate commercial proposal draft",
                "Mission: Q4 Enterprise Outreach");

            prompt.Should().Contain("Procurement department published formal RFP budget of ₹500,000");
            prompt.Should().NotContain("Customer verbally promised $1,000,000 contract without sign-off");
        }

        // =========================================================================
        // Family 5: Controller API Endpoints
        // =========================================================================

        [Fact]
        public async Task HARN431_06_ControllerEndpoints_StartAndListSessionsAndInstances()
        {
            var sessionResult = await _controller.StartSession(new StartSessionRequest
            {
                AgentId = "agent-api-harness",
                MissionId = "mission-api-01"
            }) as OkObjectResult;
            sessionResult.Should().NotBeNull();

            var instanceList = await _controller.ListActiveInstances() as OkObjectResult;
            instanceList.Should().NotBeNull();
        }
    }
}
