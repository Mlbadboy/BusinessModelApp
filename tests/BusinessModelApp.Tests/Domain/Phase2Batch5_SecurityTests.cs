using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using BusinessModelApp.Core.Domain.Security;
using BusinessModelApp.Core.Agents;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Security;
using BusinessModelApp.Infrastructure.Interceptors;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase2Batch5_SecurityTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString("N"))
                .AddInterceptors(new AppendOnlyAuditInterceptor())
                .Options;

            return new AppDbContext(options);
        }

        // ==============================================================================
        // AUDIT POINT 3: HARD TARGET ALLOWLIST ENFORCEMENT
        // ==============================================================================
        [Fact]
        public async Task B5_01_TargetAllowlist_BlocksUnregisteredTargetsAndExternalProductionProbing()
        {
            using var db = CreateInMemoryDbContext();
            var targetRegistry = new SecurityTargetRegistry(db);
            Guid tenantA = Guid.NewGuid();
            Guid sandboxA = Guid.NewGuid();

            // 1. Valid registration in disposable sandbox
            var validTarget = new SecurityTargetRegistration
            {
                WorkspaceId = tenantA,
                TargetName = "TenantA_IsolatedTestSandbox",
                Type = TargetType.DisposableSandbox,
                Environment = TargetEnvironment.Sandbox,
                SandboxId = sandboxA,
                AllowedCapabilitiesJson = "[\"ReadSandboxData\",\"FuzzPolicyEngine\"]",
                IsActive = true
            };
            var registered = await targetRegistry.RegisterTargetAsync(validTarget);
            Assert.NotEqual(Guid.Empty, registered.Id);

            // Valid capability check passes
            bool isAllowed = await targetRegistry.ValidateTargetAllowlistAsync(registered.Id, tenantA, "ReadSandboxData");
            Assert.True(isAllowed);

            // 2. Unregistered capability fails fail-closed
            var capEx = await Assert.ThrowsAsync<SecurityException>(async () =>
            {
                await targetRegistry.ValidateTargetAllowlistAsync(registered.Id, tenantA, "ExecuteProductionDrop");
            });
            Assert.Contains("Capability 'ExecuteProductionDrop' is not granted", capEx.Message);

            // 3. Unregistered arbitrary target fails fail-closed
            var unregEx = await Assert.ThrowsAsync<SecurityException>(async () =>
            {
                await targetRegistry.ValidateTargetAllowlistAsync(Guid.NewGuid(), tenantA, "ReadSandboxData");
            });
            Assert.Contains("not in the pre-approved security allowlist", unregEx.Message);

            // 4. Invariant: External production scanning cannot be registered
            var prodTarget = new SecurityTargetRegistration
            {
                WorkspaceId = tenantA,
                TargetName = "External_ThirdParty_Probe",
                Type = TargetType.InternalEndpoint,
                Environment = (TargetEnvironment)99, // Outside Sandbox/IsolatedTest
                SandboxId = Guid.NewGuid()
            };
            var prodEx = await Assert.ThrowsAsync<SecurityException>(async () =>
            {
                await targetRegistry.RegisterTargetAsync(prodTarget);
            });
            Assert.Contains("External production scanning is strictly prohibited", prodEx.Message);
        }

        // ==============================================================================
        // AUDIT POINT 2: INDEPENDENT KILL SWITCH (<= 100ms UNDER CONCURRENCY)
        // ==============================================================================
        [Fact]
        public async Task B5_02_EmergencyKillSwitch_HaltsConcurrentSecurityWorkersWithin100ms()
        {
            // Ensure kill switch is reset before test
            KillSwitchManager.Reset("TestRunner");
            Assert.False(KillSwitchManager.IsActive);

            int concurrentWorkers = 10;
            var workerStarted = new CountdownEvent(concurrentWorkers);
            var workerHalted = new CountdownEvent(concurrentWorkers);
            var exceptions = new List<Exception>();

            var tasks = new List<Task>();
            for (int i = 0; i < concurrentWorkers; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    workerStarted.Signal();
                    try
                    {
                        while (true)
                        {
                            KillSwitchManager.AssertNotHalted();
                            await Task.Delay(5);
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                        workerHalted.Signal();
                    }
                }));
            }

            // Wait until all workers are running
            workerStarted.Wait(TimeSpan.FromSeconds(5));

            // Trigger emergency kill switch
            var sw = Stopwatch.StartNew();
            var status = KillSwitchManager.Trigger("Unauthorized privilege escalation detected in sandbox", "SecurityOperator");
            sw.Stop();

            // Wait for all concurrent workers to halt
            bool allHalted = workerHalted.Wait(TimeSpan.FromMilliseconds(500));
            sw.Stop();

            // Invariant: External Kill Switch halts operations in <= 100ms
            Assert.True(status.IsActive);
            Assert.True(allHalted, "All concurrent security workers must be cancelled immediately.");
            Assert.Equal(concurrentWorkers, exceptions.Count);
            Assert.Contains("Unauthorized privilege escalation detected", exceptions.First().Message);
            Assert.True(status.ExecutionHaltDurationMs <= 100, 
                $"Kill switch halt duration exceeded 100ms threshold: {status.ExecutionHaltDurationMs}ms");

            await Task.WhenAll(tasks);

            // Cleanup & disarm
            KillSwitchManager.Reset("TestRunner");
            Assert.False(KillSwitchManager.IsActive);
        }

        // ==============================================================================
        // AUDIT POINT 4 & 5: DETERMINISTIC NON-DESTRUCTIVE PoC & EVIDENCE CHAIN
        // ==============================================================================
        [Fact]
        public async Task B5_03_RedTeamBOLAProbe_ProducesDeterministicNonDestructivePoCAndEvidenceChain()
        {
            using var db = CreateInMemoryDbContext();
            var targetRegistry = new SecurityTargetRegistry(db);
            var redEngine = new RedTeamAutonomousEngine(targetRegistry);

            Guid tenantA = Guid.NewGuid();
            Guid sandboxA = Guid.NewGuid();

            var target = await targetRegistry.RegisterTargetAsync(new SecurityTargetRegistration
            {
                WorkspaceId = tenantA,
                TargetName = "BOLA_Evaluation_Sandbox",
                Type = TargetType.DisposableSandbox,
                Environment = TargetEnvironment.Sandbox,
                SandboxId = sandboxA,
                AllowedCapabilitiesJson = "[\"ReadSandboxData\"]"
            });

            Guid campaignId = Guid.NewGuid();

            // Simulate synthetic BOLA probe where vulnerability IS detected
            var finding = await redEngine.ExecuteBOLAProbeAsync(
                target.Id,
                tenantA,
                campaignId,
                crossTenantAccessProbe: async (callerTenant, targetTenant) =>
                {
                    // Simulated probe: Mismatched tenant token unexpectedly accessed synthetic record
                    return await Task.FromResult(true);
                });

            // Invariant: Full Finding Evidence Chain
            Assert.Equal(VulnerabilityCategory.BOLA_IDOR, finding.Category);
            Assert.Equal(VulnerabilitySeverity.Critical, finding.Severity);
            Assert.Equal(VulnerabilityStatus.Reproduced, finding.Status);
            Assert.Contains("Synthetic Tenant B record was returned", finding.ObservedResult);
            Assert.Contains("Cross-tenant access blocked", finding.ExpectedResult);
            Assert.True(finding.IsContainedInSandbox);

            // Invariant: Deterministic non-destructive PoC generated
            Assert.NotEmpty(finding.ReproductionPoC);
            Assert.Contains("var tenantAToken = GenerateSyntheticToken", finding.ReproductionPoC);
            Assert.Contains("HttpStatusCode.Forbidden", finding.ReproductionPoC);

            // Invariant: Cryptographic reproduction hash
            Assert.NotEmpty(finding.ReproductionHash);
            string expectedHash = VulnerabilityFinding.ComputeReproductionHash(
                target.Id.ToString(), finding.TestName, finding.InputPayload, finding.ObservedResult);
            Assert.Equal(expectedHash, finding.ReproductionHash);
        }

        // ==============================================================================
        // AUDIT POINT 1 & 6: BOUNDED AUTOMATED CONTAINMENT & ZERO-REGRESSION GATE
        // ==============================================================================
        [Fact]
        public async Task B5_04_BlueTeamRemediation_BoundedContainmentAndZeroRegressionGate()
        {
            var blueEngine = new BlueTeamRemediationEngine();
            Guid tenantId = Guid.NewGuid();

            var criticalFinding = new VulnerabilityFinding
            {
                Id = Guid.NewGuid(),
                WorkspaceId = tenantId,
                TestName = "TenantDataIsolationProbe",
                Category = VulnerabilityCategory.TenantLeakage,
                Severity = VulnerabilitySeverity.Critical,
                Status = VulnerabilityStatus.Reproduced,
                IsContainedInSandbox = true
            };

            // 1. Formulate remediation
            var candidateRemediation = blueEngine.FormulateRemediation(criticalFinding);
            Assert.Equal(RemediationPatchType.PolicyRuleTightening, candidateRemediation.PatchType);
            Assert.True(candidateRemediation.IsPreApprovedReversibleSandboxAction);
            Assert.True(candidateRemediation.RequiresGovernanceApproval); // Production query filter changes require governance!

            // 2. Invariant: Applying production remediation without governance approval is BLOCKED
            var unauthEx = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await blueEngine.ValidateAndApplyRemediationAsync(
                    candidateRemediation,
                    rerunPocValidation: async () => await Task.FromResult(true),
                    runRegressionSuite: async () => await Task.FromResult(239),
                    isGovernanceApproved: false);
            });
            Assert.Contains("requires explicit sovereign governance approval", unauthEx.Message);

            // 3. Invariant: Fix that fails PoC re-validation is REJECTED
            var failedPocEx = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await blueEngine.ValidateAndApplyRemediationAsync(
                    candidateRemediation,
                    rerunPocValidation: async () => await Task.FromResult(false), // PoC still fails!
                    runRegressionSuite: async () => await Task.FromResult(239),
                    isGovernanceApproved: true);
            });
            Assert.Contains("Red Team Proof-of-Concept still reproduces the vulnerability", failedPocEx.Message);

            // 4. Invariant: Zero-Regression Gate passes when PoC is eliminated and regression tests pass
            var applied = await blueEngine.ValidateAndApplyRemediationAsync(
                candidateRemediation,
                rerunPocValidation: async () => await Task.FromResult(true), // PoC now passes!
                runRegressionSuite: async () => await Task.FromResult(239), // All 239 baseline tests pass!
                isGovernanceApproved: true);

            Assert.True(applied.IsApproved);
            Assert.True(applied.PoCPassed);
            Assert.True(applied.RegressionTested);
            Assert.Equal(239, applied.PassedTestCount);
            Assert.NotNull(applied.AppliedAt);
            Assert.Equal("SecurityGovernor", applied.ApprovedBy);
        }

        // ==============================================================================
        // AUDIT ADDITION: TENANT-AWARE MULTI-TENANT SECURITY ISOLATION
        // ==============================================================================
        [Fact]
        public async Task B5_05_TenantAwareSecurityIsolation_TenantACannotScanTenantBSandbox()
        {
            using var db = CreateInMemoryDbContext();
            var targetRegistry = new SecurityTargetRegistry(db);
            var redEngine = new RedTeamAutonomousEngine(targetRegistry);
            var blueEngine = new BlueTeamRemediationEngine();
            var sccService = new SecurityCommandCenterService(db, targetRegistry, redEngine, blueEngine);

            Guid tenantA = Guid.NewGuid();
            Guid tenantB = Guid.NewGuid();
            Guid sandboxB = Guid.NewGuid();

            // Tenant B registers sandbox in their workspace
            var targetB = await targetRegistry.RegisterTargetAsync(new SecurityTargetRegistration
            {
                WorkspaceId = tenantB,
                TargetName = "TenantB_PrivateSandbox",
                Type = TargetType.DisposableSandbox,
                Environment = TargetEnvironment.Sandbox,
                SandboxId = sandboxB,
                AllowedCapabilitiesJson = "[\"ReadSandboxData\"]"
            });

            // Invariant: Tenant A attempts to target Tenant B's sandbox in a campaign -> FAIL-CLOSED
            var campaignA = new RedTeamCampaign
            {
                WorkspaceId = tenantA, // Tenant A
                TargetSandboxId = targetB.Id, // Targets Tenant B's resource!
                CampaignName = "Hostile_CrossTenant_Recon",
                Strategy = RedTeamCampaignStrategy.TenantIsolation_Scan
            };

            var secEx = await Assert.ThrowsAsync<SecurityException>(async () =>
            {
                await sccService.StartCampaignAsync(campaignA);
            });
            Assert.Contains("not in the pre-approved security allowlist", secEx.Message);

            // Invariant: Findings from Tenant A are completely invisible to Tenant B
            await sccService.RecordFindingAsync(new VulnerabilityFinding
            {
                WorkspaceId = tenantA,
                TestName = "TenantA_InternalFinding",
                Severity = VulnerabilitySeverity.Medium,
                Status = VulnerabilityStatus.Discovered
            });

            var findingsB = await sccService.ListFindingsAsync(tenantB);
            Assert.Empty(findingsB);
        }

        // ==============================================================================
        // SECURITY POSTURE SCORING CALCULATION
        // ==============================================================================
        [Fact]
        public async Task B5_06_SecurityPostureScoring_CalculatesAccuratelyAcrossFindingSeverities()
        {
            using var db = CreateInMemoryDbContext();
            var targetRegistry = new SecurityTargetRegistry(db);
            var redEngine = new RedTeamAutonomousEngine(targetRegistry);
            var blueEngine = new BlueTeamRemediationEngine();
            var sccService = new SecurityCommandCenterService(db, targetRegistry, redEngine, blueEngine);

            Guid workspaceId = Guid.NewGuid();

            // 1. Initial posture: 100 (Excellent)
            var initialPosture = await sccService.CalculatePostureScoreAsync(workspaceId);
            Assert.Equal(100.0, initialPosture.OverallScore);
            Assert.Equal("Excellent", initialPosture.Rating);

            // 2. Discover a Critical finding: -25 points
            var criticalFinding = await sccService.RecordFindingAsync(new VulnerabilityFinding
            {
                WorkspaceId = workspaceId,
                TestName = "UnauthenticatedApiAccess",
                Severity = VulnerabilitySeverity.Critical,
                Status = VulnerabilityStatus.Discovered
            });

            var degradedPosture = await sccService.CalculatePostureScoreAsync(workspaceId);
            Assert.Equal(75.0, degradedPosture.OverallScore);
            Assert.Equal("Good", degradedPosture.Rating);
            Assert.Equal(1, degradedPosture.OpenCriticalFindings);

            // 3. Contain finding in sandbox: recovers 10 points
            criticalFinding.Status = VulnerabilityStatus.Contained;
            criticalFinding.IsContainedInSandbox = true;
            await db.SaveChangesAsync();

            var containedPosture = await sccService.CalculatePostureScoreAsync(workspaceId);
            Assert.Equal(85.0, containedPosture.OverallScore);
            Assert.Equal(1, containedPosture.ContainedFindings);

            // 4. Remediate finding: clears penalty back to 100
            criticalFinding.Status = VulnerabilityStatus.Remediated;
            await db.SaveChangesAsync();

            var remediatedPosture = await sccService.CalculatePostureScoreAsync(workspaceId);
            Assert.Equal(100.0, remediatedPosture.OverallScore);
            Assert.Equal("Excellent", remediatedPosture.Rating);
        }
    }
}
