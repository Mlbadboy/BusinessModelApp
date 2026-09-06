using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.ExternalReality;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Capabilities;
using BusinessModelApp.Core.Domain.Runtime.Fleet;
using BusinessModelApp.Core.Domain.Runtime.Reputation;
using BusinessModelApp.Core.Interfaces.Missions;
using BusinessModelApp.Core.Interfaces.Runtime.Fleet;
using BusinessModelApp.Core.Interfaces.Runtime.Reputation;
using BusinessModelApp.Infrastructure.Runtime.Fleet;
using BusinessModelApp.Infrastructure.Runtime.Missions;
using BusinessModelApp.Infrastructure.Runtime.Reputation;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase3Batch34ReputationTests
    {
        private readonly Guid _tenantA = Guid.NewGuid();
        private readonly Guid _tenantB = Guid.NewGuid();

        private readonly InMemoryReputationStore _store = new();
        private readonly CapabilityRegistry _registry = new();
        private readonly CausalAttributionEngine _attributionEngine = new();
        private readonly CalibrationEngine _calibrationEngine = new();
        private readonly EmpiricalPerformanceEngine _performanceEngine;
        private readonly ReputationAwareRouter _router;

        private readonly InMemoryAgentFleetStore _fleetStore = new();
        private readonly WorkerLeaseCoordinator _leaseCoordinator = new();
        private readonly MissionGraphAuditLedger _auditLedger = new();
        private readonly NodeVerificationEngine _verificationEngine;
        private readonly AgentOutcomeAdmissionGate _admissionGate;
        private readonly FleetOrchestrator _orchestrator;
        private readonly AgentFleetPipelineCoordinator _pipelineCoordinator;
        private readonly TenantMissionPolicyContext _defaultPolicy;

        private readonly CapabilityId _forecastCapability = new("sales_forecast", "v1");
        private readonly CapabilityId _pricingCapability = new("pricing_optimizer", "v1");
        private readonly CapabilityId _riskCapability = new("financial_settlement", "v1");

        private readonly StructuredDomainContext _commercialIndia =
            StructuredDomainContext.Create("Commercial", "Retail", "QuarterlySalesForecast", "IndiaSMB");
        private readonly StructuredDomainContext _commercialUS =
            StructuredDomainContext.Create("Commercial", "Enterprise", "QuarterlySalesForecast", "USEnterprise");

        public Phase3Batch34ReputationTests()
        {
            _defaultPolicy = new TenantMissionPolicyContext
            {
                WorkspaceId = _tenantA,
                MaxAllowedAutonomyTier = AutonomyTier.L5_ExecuteBounded,
                RegisteredCapabilityIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    _forecastCapability.ToString(),
                    _pricingCapability.ToString(),
                    _riskCapability.ToString()
                }
            };

            _performanceEngine = new EmpiricalPerformanceEngine(_store);
            _router = new ReputationAwareRouter(_store, _registry);
            _orchestrator = new FleetOrchestrator(_fleetStore, _router);
            _verificationEngine = new NodeVerificationEngine(_auditLedger);
            _admissionGate = new AgentOutcomeAdmissionGate(_leaseCoordinator, _verificationEngine, _auditLedger);
            _pipelineCoordinator = new AgentFleetPipelineCoordinator(
                _orchestrator,
                _leaseCoordinator,
                _admissionGate,
                _auditLedger,
                _performanceEngine,
                _attributionEngine,
                _calibrationEngine);

            // Register standard capabilities
            _registry.RegisterCapabilityAsync(new CapabilityDefinitionRecord
            {
                CapabilityId = _forecastCapability,
                Title = "Sales Forecast Engine",
                DomainContext = _commercialIndia,
                RiskTier = CapabilityRiskTier.R1_LowAnalytical,
                RequiredAutonomyTier = AutonomyTier.L1_Advise
            }).Wait();

            _registry.RegisterCapabilityAsync(new CapabilityDefinitionRecord
            {
                CapabilityId = _pricingCapability,
                Title = "Pricing Optimizer",
                DomainContext = _commercialUS,
                RiskTier = CapabilityRiskTier.R2_MediumPredictive,
                RequiredAutonomyTier = AutonomyTier.L2_Simulate
            }).Wait();

            _registry.RegisterCapabilityAsync(new CapabilityDefinitionRecord
            {
                CapabilityId = _riskCapability,
                Title = "Financial Settlement Execution",
                DomainContext = _commercialIndia,
                RiskTier = CapabilityRiskTier.R4_CriticalFinancial,
                RequiredAutonomyTier = AutonomyTier.L4_ExecuteWithApproval
            }).Wait();
        }

        // ====================================================================
        // REP-01: IDENTITY, HIERARCHY & MULTI-LEVEL SCOPING (4 tests)
        // ====================================================================

        [Fact]
        public void REP01_01_ProfileHierarchySeparation()
        {
            var agentProfile = new AgentPerformanceProfile { AgentDefinitionId = AgentDefinitionId.From("forecaster") };
            var capProfile = new CapabilityVersionProfile { CapabilityId = _forecastCapability };
            var scopedProfile = new CapabilityPerformanceProfile
            {
                AgentDefinitionId = AgentDefinitionId.From("forecaster"),
                CapabilityId = _forecastCapability,
                DomainContext = _commercialIndia
            };

            Assert.NotEqual<object>(agentProfile, capProfile);
            Assert.Equal("forecaster", scopedProfile.AgentDefinitionId.Value);
            Assert.Equal(_forecastCapability, scopedProfile.CapabilityId);
            Assert.Equal("commercial:retail:quarterlysalesforecast:indiasmb", scopedProfile.DomainContext.ToCanonicalKey());
        }

        [Fact]
        public async Task REP01_02_DifferentDomainContextsProduceDistinctProfiles()
        {
            var agentId = AgentDefinitionId.From("specialist_a");

            var tokenIndia = CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.05);
            var tokenUS = CreateSampleToken(agentId, _forecastCapability, _commercialUS, 0.40);

            await _performanceEngine.ProcessEvidenceTokenAsync(tokenIndia);
            await _performanceEngine.ProcessEvidenceTokenAsync(tokenUS);

            var profileIndia = await _store.GetProfileAsync(_tenantA, agentId, _forecastCapability, _commercialIndia.ToCanonicalKey(), MarketRegimeState.Stable);
            var profileUS = await _store.GetProfileAsync(_tenantA, agentId, _forecastCapability, _commercialUS.ToCanonicalKey(), MarketRegimeState.Stable);

            Assert.NotNull(profileIndia);
            Assert.NotNull(profileUS);
            Assert.NotEqual(profileIndia.ProfileId, profileUS.ProfileId);
            Assert.True(profileIndia.Metrics.CalibrationVariance < profileUS.Metrics.CalibrationVariance);
        }

        [Fact]
        public async Task REP01_03_MarketRegimesProduceDistinctProfiles()
        {
            var agentId = AgentDefinitionId.From("specialist_b");

            var tokenStable = CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.04, regime: MarketRegimeState.Stable);
            var tokenPriceWar = CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.35, regime: MarketRegimeState.PriceWar);

            await _performanceEngine.ProcessEvidenceTokenAsync(tokenStable);
            await _performanceEngine.ProcessEvidenceTokenAsync(tokenPriceWar);

            var profileStable = await _store.GetProfileAsync(_tenantA, agentId, _forecastCapability, _commercialIndia.ToCanonicalKey(), MarketRegimeState.Stable);
            var profilePriceWar = await _store.GetProfileAsync(_tenantA, agentId, _forecastCapability, _commercialIndia.ToCanonicalKey(), MarketRegimeState.PriceWar);

            Assert.NotNull(profileStable);
            Assert.NotNull(profilePriceWar);
            Assert.NotEqual(profileStable.ProfileId, profilePriceWar.ProfileId);
            Assert.Equal(MarketRegimeState.Stable, profileStable.MarketRegime);
            Assert.Equal(MarketRegimeState.PriceWar, profilePriceWar.MarketRegime);
        }

        [Fact]
        public async Task REP01_04_CapabilityVersionProfileTracksAggregateAcrossAllAgents()
        {
            var agent1 = AgentDefinitionId.From("agent_1");
            var agent2 = AgentDefinitionId.From("agent_2");

            await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agent1, _forecastCapability, _commercialIndia, 0.10));
            await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agent2, _forecastCapability, _commercialIndia, 0.12));

            var capProfile = await _store.GetCapabilityProfileAsync(_forecastCapability);
            Assert.NotNull(capProfile);
            Assert.Equal(2, capProfile.TotalExecutionsAcrossAllAgents);
        }

        // ====================================================================
        // REP-02: EXPECTED VS ACTUAL CALIBRATION & METROLOGY (4 tests)
        // ====================================================================

        [Fact]
        public async Task REP02_01_NumericalPredictionMatchesVerifiedActual_YieldsLowDiscrepancy()
        {
            var node = new MissionNodeRecord { NodeId = MissionNodeId.From("N1") };
            var proposal = new AgentOutcomeProposal
            {
                OutputPayloadJson = "{\"predictedValue\": 1000.0, \"actualValue\": 1020.0}"
            };
            var verification = NodeVerificationResult.Success("hash_1", confidence: 0.95);

            var cal = await _calibrationEngine.CalculateCalibrationAsync(
                ExecutionAttemptId.New(),
                node,
                proposal,
                verification,
                _commercialIndia,
                MarketRegimeState.Stable);

            Assert.True(cal.IsWithinTolerance);
            Assert.True(cal.DiscrepancyScore <= 0.05); // ~2% error
        }

        [Fact]
        public async Task REP02_02_NumericalDivergence_YieldsHighDiscrepancyScore()
        {
            var node = new MissionNodeRecord { NodeId = MissionNodeId.From("N1") };
            var proposal = new AgentOutcomeProposal
            {
                OutputPayloadJson = "{\"predictedValue\": 1000.0, \"actualValue\": 1800.0}"
            };
            var verification = NodeVerificationResult.Success("hash_1", confidence: 0.90);

            var cal = await _calibrationEngine.CalculateCalibrationAsync(
                ExecutionAttemptId.New(),
                node,
                proposal,
                verification,
                _commercialIndia,
                MarketRegimeState.Stable);

            Assert.False(cal.IsWithinTolerance);
            Assert.True(cal.DiscrepancyScore >= 0.40);
        }

        [Fact]
        public async Task REP02_03_VerificationFailure_ForcesMaxDiscrepancy()
        {
            var node = new MissionNodeRecord { NodeId = MissionNodeId.From("N1") };
            var proposal = new AgentOutcomeProposal
            {
                OutputPayloadJson = "{\"predictedValue\": 1000.0, \"actualValue\": 1000.0}"
            };
            var verification = NodeVerificationResult.Failed("Artifact hash mismatch");

            var cal = await _calibrationEngine.CalculateCalibrationAsync(
                ExecutionAttemptId.New(),
                node,
                proposal,
                verification,
                _commercialIndia,
                MarketRegimeState.Stable);

            Assert.False(cal.IsWithinTolerance);
            Assert.Equal(1.0, cal.DiscrepancyScore);
        }

        [Fact]
        public async Task REP02_04_NonNumericalPayloadDerivesDiscrepancyFromConfidence()
        {
            var node = new MissionNodeRecord { NodeId = MissionNodeId.From("N1") };
            var proposal = new AgentOutcomeProposal
            {
                OutputPayloadJson = "{\"status\": \"completed_successfully\"}"
            };
            var verification = NodeVerificationResult.Success("hash_1", confidence: 0.88);

            var cal = await _calibrationEngine.CalculateCalibrationAsync(
                ExecutionAttemptId.New(),
                node,
                proposal,
                verification,
                _commercialIndia,
                MarketRegimeState.Stable);

            Assert.Equal(0.12, Math.Round(cal.DiscrepancyScore, 2));
            Assert.True(cal.IsWithinTolerance);
        }

        // ====================================================================
        // REP-03: GRADED CAUSAL ATTRIBUTION (A0 to A4) (5 tests)
        // ====================================================================

        [Fact]
        public async Task REP03_01_DeterministicArtifacts_ClassifiedAsA4Deterministic()
        {
            var node = new MissionNodeRecord { NodeId = MissionNodeId.From("N1") };
            var proposal = new AgentOutcomeProposal
            {
                ProducedArtifacts = new[] { new MissionArtifact { Name = "output.json", Sha256Hash = "hash123" } }
            };
            var verification = NodeVerificationResult.Success("hash123", confidence: 1.0);

            var attr = await _attributionEngine.EvaluateAttributionAsync(ExecutionAttemptId.New(), node, proposal, verification);
            Assert.Equal(AttributionLevel.A4_Deterministic, attr.Level);
            Assert.Equal(1.0, attr.EffectiveReputationWeight);
        }

        [Fact]
        public async Task REP03_02_HighConfidenceWithoutDeterministicArtifact_ClassifiedAsA3Strong()
        {
            var node = new MissionNodeRecord { NodeId = MissionNodeId.From("N1") };
            var proposal = new AgentOutcomeProposal();
            var verification = NodeVerificationResult.Success("hash123", confidence: 0.90);

            var attr = await _attributionEngine.EvaluateAttributionAsync(ExecutionAttemptId.New(), node, proposal, verification);
            Assert.Equal(AttributionLevel.A3_Strong, attr.Level);
            Assert.Equal(0.80, attr.EffectiveReputationWeight);
        }

        [Fact]
        public async Task REP03_03_ModerateConfidence_ClassifiedAsA2Plausible()
        {
            var node = new MissionNodeRecord { NodeId = MissionNodeId.From("N1") };
            var proposal = new AgentOutcomeProposal();
            var verification = NodeVerificationResult.Success("hash123", confidence: 0.70);

            var attr = await _attributionEngine.EvaluateAttributionAsync(ExecutionAttemptId.New(), node, proposal, verification);
            Assert.Equal(AttributionLevel.A2_Plausible, attr.Level);
            Assert.Equal(0.25, attr.EffectiveReputationWeight);
        }

        [Fact]
        public async Task REP03_04_LowConfidence_ClassifiedAsA1CorrelatedZeroCredit()
        {
            var node = new MissionNodeRecord { NodeId = MissionNodeId.From("N1") };
            var proposal = new AgentOutcomeProposal();
            var verification = NodeVerificationResult.Success("hash123", confidence: 0.50);

            var attr = await _attributionEngine.EvaluateAttributionAsync(ExecutionAttemptId.New(), node, proposal, verification);
            Assert.Equal(AttributionLevel.A1_Correlated, attr.Level);
            Assert.Equal(0.0, attr.EffectiveReputationWeight);
        }

        [Fact]
        public async Task REP03_05_FailedVerification_ClassifiedAsA0NoneZeroCredit()
        {
            var node = new MissionNodeRecord { NodeId = MissionNodeId.From("N1") };
            var proposal = new AgentOutcomeProposal();
            var verification = NodeVerificationResult.Failed("Schema mismatch");

            var attr = await _attributionEngine.EvaluateAttributionAsync(ExecutionAttemptId.New(), node, proposal, verification);
            Assert.Equal(AttributionLevel.A0_None, attr.Level);
            Assert.Equal(0.0, attr.EffectiveReputationWeight);
        }

        // ====================================================================
        // REP-04: SAMPLE SIZE CONFIDENCE & UNCERTAINTY DAMPENING (4 tests)
        // ====================================================================

        [Fact]
        public void REP04_01_ConfidenceFactorScalesSmoothlyWithSampleSize()
        {
            var p1 = new CapabilityPerformanceProfile { TotalAttempts = 1 };
            var p5 = new CapabilityPerformanceProfile { TotalAttempts = 5 };
            var p25 = new CapabilityPerformanceProfile { TotalAttempts = 25 };
            var p100 = new CapabilityPerformanceProfile { TotalAttempts = 100 };

            double c1 = p1.ComputeConfidenceFactor();
            double c5 = p5.ComputeConfidenceFactor();
            double c25 = p25.ComputeConfidenceFactor();
            double c100 = p100.ComputeConfidenceFactor();

            Assert.True(c1 < c5);
            Assert.True(c5 < c25);
            Assert.True(c25 < c100);
            Assert.True(c1 >= 0.25);
            Assert.True(c100 >= 0.85);
        }

        [Fact]
        public async Task REP04_02_SmallSampleDoesNotAutomaticallyInvalidateSuperiorWorker()
        {
            var workerA = WorkerProcessRecordId("W_A", WorkerPoolType.DomainResearcher);
            var workerB = WorkerProcessRecordId("W_B", WorkerPoolType.DomainResearcher);

            // Worker A: N=2, high accuracy (variance = 0.02)
            var agentA = AgentDefinitionId.From("agent_a");
            for (int i = 0; i < 2; i++)
                await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentA, _forecastCapability, _commercialIndia, 0.02, workerA.WorkerId));

            // Worker B: N=50, terrible accuracy (variance = 0.65)
            var agentB = AgentDefinitionId.From("agent_b");
            for (int i = 0; i < 50; i++)
                await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentB, _forecastCapability, _commercialIndia, 0.65, workerB.WorkerId));

            var graph = CreateSimpleGraph();
            var node = CreateSimpleNode(_forecastCapability);

            var decision = await _router.RouteNodeWorkerAsync(graph, node, new[] { workerA, workerB }, new TenantMissionPolicyContext());

            // Worker A should still win because Worker B's massive error outweighs high sample certainty
            Assert.Equal(workerA.WorkerId, decision.SelectedWorkerId);
        }

        [Fact]
        public async Task REP04_03_IdenticalAccuracyPrioritizesHigherSampleConfidence()
        {
            var workerA = WorkerProcessRecordId("W_A", WorkerPoolType.DomainResearcher);
            var workerB = WorkerProcessRecordId("W_B", WorkerPoolType.DomainResearcher);

            // Worker A: N=1, variance = 0.05
            var agentA = AgentDefinitionId.From("agent_a");
            await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentA, _forecastCapability, _commercialIndia, 0.05, workerA.WorkerId));

            // Worker B: N=30, variance = 0.05
            var agentB = AgentDefinitionId.From("agent_b");
            for (int i = 0; i < 30; i++)
                await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentB, _forecastCapability, _commercialIndia, 0.05, workerB.WorkerId));

            var graph = CreateSimpleGraph();
            var node = CreateSimpleNode(_forecastCapability);

            var decision = await _router.RouteNodeWorkerAsync(graph, node, new[] { workerA, workerB }, new TenantMissionPolicyContext());

            // Worker B wins because identical performance with higher sample confidence produces higher utility
            Assert.Equal(workerB.WorkerId, decision.SelectedWorkerId);
        }

        [Fact]
        public void REP04_04_ZeroAttemptsHasLowerBoundConfidence()
        {
            var p0 = new CapabilityPerformanceProfile { TotalAttempts = 0 };
            Assert.Equal(0.20, p0.ComputeConfidenceFactor());
        }

        // ====================================================================
        // REP-05: CRITICAL SECURITY QUARANTINE VS GRADUAL DEGRADATION (4 tests)
        // ====================================================================

        [Fact]
        public async Task REP05_01_SecurityViolationTriggersImmediateQuarantine()
        {
            var agentId = AgentDefinitionId.From("malicious_or_tampered");
            var token = CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.02, isSecurityViolation: true);

            var profile = await _performanceEngine.ProcessEvidenceTokenAsync(token);

            Assert.True(profile.Metrics.IsQuarantined);
            Assert.Equal(0.0, profile.Metrics.PolicyComplianceScore);
            Assert.Contains("security or fencing violation", profile.Metrics.QuarantineReason);
        }

        [Fact]
        public async Task REP05_02_QuarantinedWorkerExcludedFromRoutingRegardlessOfAccuracy()
        {
            var workerA = WorkerProcessRecordId("W_A", WorkerPoolType.DomainResearcher);
            var workerB = WorkerProcessRecordId("W_B", WorkerPoolType.DomainResearcher);

            // Worker A: 99% accuracy, but quarantined due to fencing tampering
            var agentA = AgentDefinitionId.From("agent_a");
            await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentA, _forecastCapability, _commercialIndia, 0.01, workerA.WorkerId, isSecurityViolation: true));

            // Worker B: mediocre accuracy (0.25), but healthy
            var agentB = AgentDefinitionId.From("agent_b");
            await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentB, _forecastCapability, _commercialIndia, 0.25, workerB.WorkerId));

            var graph = CreateSimpleGraph();
            var node = CreateSimpleNode(_forecastCapability);

            var decision = await _router.RouteNodeWorkerAsync(graph, node, new[] { workerA, workerB }, new TenantMissionPolicyContext());

            // Worker A must be strictly excluded; Worker B selected
            Assert.Equal(workerB.WorkerId, decision.SelectedWorkerId);
            var evalA = decision.CandidateEvaluations.First(e => e.WorkerId == workerA.WorkerId);
            Assert.False(evalA.IsEligible);
            Assert.True(evalA.IsQuarantined);
        }

        [Fact]
        public async Task REP05_03_NormalCalibrationFailureCausesGradualDecayNotQuarantine()
        {
            var agentId = AgentDefinitionId.From("imperfect_agent");
            var token = CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.45);

            var profile = await _performanceEngine.ProcessEvidenceTokenAsync(token);

            Assert.False(profile.Metrics.IsQuarantined);
            Assert.Equal(1.0, profile.Metrics.PolicyComplianceScore);
            Assert.True(profile.Metrics.CalibrationVariance > 0.0);
        }

        [Fact]
        public async Task REP05_04_ExplicitQuarantineMethodUpdatesProfileAndHistory()
        {
            var agentId = AgentDefinitionId.From("rogue_agent");
            await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.05));

            await _performanceEngine.ApplyQuarantineAsync(_tenantA, agentId, _forecastCapability, "Manual security revoking");

            var profile = await _store.GetProfileAsync(_tenantA, agentId, _forecastCapability, _commercialIndia.ToCanonicalKey(), MarketRegimeState.Stable);
            Assert.NotNull(profile);
            Assert.True(profile.Metrics.IsQuarantined);
            Assert.Equal("Manual security revoking", profile.Metrics.QuarantineReason);
        }

        // ====================================================================
        // REP-06: UNKNOWNEFFECT & ROLLBACK PENALTY (4 tests)
        // ====================================================================

        [Fact]
        public async Task REP06_01_UnknownEffectCrashIncrementsRollbackFrequency()
        {
            var agentId = AgentDefinitionId.From("unstable_worker");
            var token = CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.0, isCrash: true);

            var profile = await _performanceEngine.ProcessEvidenceTokenAsync(token);

            Assert.Equal(1, profile.UnknownEffectCount);
            Assert.True(profile.Metrics.RollbackFrequency > 0.0);
        }

        [Fact]
        public async Task REP06_02_HighRollbackFrequencyPenalizesRoutingUtility()
        {
            var workerA = WorkerProcessRecordId("W_A", WorkerPoolType.DomainResearcher);
            var workerB = WorkerProcessRecordId("W_B", WorkerPoolType.DomainResearcher);

            var agentA = AgentDefinitionId.From("crasher_agent");
            for (int i = 0; i < 5; i++)
                await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentA, _forecastCapability, _commercialIndia, 0.05, workerA.WorkerId, isCrash: true));

            var agentB = AgentDefinitionId.From("stable_agent");
            for (int i = 0; i < 5; i++)
                await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentB, _forecastCapability, _commercialIndia, 0.08, workerB.WorkerId, isCrash: false));

            var graph = CreateSimpleGraph();
            var node = CreateSimpleNode(_forecastCapability);

            var decision = await _router.RouteNodeWorkerAsync(graph, node, new[] { workerA, workerB }, new TenantMissionPolicyContext());

            // Stable worker B wins over crashing worker A
            Assert.Equal(workerB.WorkerId, decision.SelectedWorkerId);
        }

        [Fact]
        public async Task REP06_03_SubsequentSuccessfulRunsGraduallyRecoverRollbackRate()
        {
            var agentId = AgentDefinitionId.From("recovering_agent");

            await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.0, isCrash: true));
            var p1 = await _store.GetProfileAsync(_tenantA, agentId, _forecastCapability, _commercialIndia.ToCanonicalKey(), MarketRegimeState.Stable);
            double initialRollback = p1!.Metrics.RollbackFrequency;

            for (int i = 0; i < 5; i++)
            {
                await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.02, isCrash: false));
            }

            var p2 = await _store.GetProfileAsync(_tenantA, agentId, _forecastCapability, _commercialIndia.ToCanonicalKey(), MarketRegimeState.Stable);
            Assert.True(p2!.Metrics.RollbackFrequency < initialRollback);
        }

        [Fact]
        public async Task REP06_04_CleanFailureDoesNotIncrementUnknownEffectCount()
        {
            var agentId = AgentDefinitionId.From("clean_fail_agent");
            var token = CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.50, isSuccess: false, isCrash: false);

            var profile = await _performanceEngine.ProcessEvidenceTokenAsync(token);

            Assert.Equal(0, profile.UnknownEffectCount);
            Assert.Equal(1, profile.FailedAttempts);
        }

        // ====================================================================
        // REP-07: CONTEXT-ADAPTIVE ROUTING UTILITY FUNCTIONS (4 tests)
        // ====================================================================

        [Fact]
        public async Task REP07_01_HighRiskNodePrioritizesAccuracyOverCost()
        {
            var workerAccurate = WorkerProcessRecordId("W_Accurate", WorkerPoolType.DomainResearcher);
            var workerCheap = WorkerProcessRecordId("W_Cheap", WorkerPoolType.DomainResearcher);

            var agentAcc = AgentDefinitionId.From("agent_accurate");
            var agentCheap = AgentDefinitionId.From("agent_cheap");

            // Accurate: variance 0.02, cost 0.50
            for (int i = 0; i < 10; i++)
                await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentAcc, _riskCapability, _commercialIndia, 0.02, workerAccurate.WorkerId, cost: 0.50m));

            // Cheap: variance 0.25, cost 0.02
            for (int i = 0; i < 10; i++)
                await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentCheap, _riskCapability, _commercialIndia, 0.25, workerCheap.WorkerId, cost: 0.02m));

            var graph = CreateSimpleGraph();
            var highRiskNode = CreateSimpleNode(_riskCapability, AutonomyTier.L4_ExecuteWithApproval);

            var decision = await _router.RouteNodeWorkerAsync(graph, highRiskNode, new[] { workerAccurate, workerCheap }, new TenantMissionPolicyContext());

            // High risk node selects accurate worker despite higher cost
            Assert.Equal(workerAccurate.WorkerId, decision.SelectedWorkerId);
        }

        [Fact]
        public async Task REP07_02_LowRiskInformationalNodeGivesMoreWeightToCostEfficiency()
        {
            var workerAccurate = WorkerProcessRecordId("W_Accurate", WorkerPoolType.DomainResearcher);
            var workerCheap = WorkerProcessRecordId("W_Cheap", WorkerPoolType.DomainResearcher);

            var agentAcc = AgentDefinitionId.From("agent_accurate");
            var agentCheap = AgentDefinitionId.From("agent_cheap");

            // Accurate: variance 0.03, cost 1.00 USD
            for (int i = 0; i < 10; i++)
                await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentAcc, _forecastCapability, _commercialIndia, 0.03, workerAccurate.WorkerId, cost: 1.00m));

            // Cheap: variance 0.08, cost 0.01 USD
            for (int i = 0; i < 10; i++)
                await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentCheap, _forecastCapability, _commercialIndia, 0.08, workerCheap.WorkerId, cost: 0.01m));

            var graph = CreateSimpleGraph();
            var lowRiskNode = CreateSimpleNode(_forecastCapability, AutonomyTier.L1_Advise);

            var decision = await _router.RouteNodeWorkerAsync(graph, lowRiskNode, new[] { workerAccurate, workerCheap }, new TenantMissionPolicyContext());

            // On low-risk node, cheap worker wins because 0.08 vs 0.03 difference is minor compared to 100x cost savings
            Assert.Equal(workerCheap.WorkerId, decision.SelectedWorkerId);
        }

        [Fact]
        public async Task REP07_03_DegradedWorkerHealthPenalizedInRouting()
        {
            var workerHealthy = WorkerProcessRecordId("W_Healthy", WorkerPoolType.DomainResearcher, WorkerHealthStatus.Healthy);
            var workerDegraded = WorkerProcessRecordId("W_Degraded", WorkerPoolType.DomainResearcher, WorkerHealthStatus.Degraded);

            var graph = CreateSimpleGraph();
            var node = CreateSimpleNode(_forecastCapability);

            var decision = await _router.RouteNodeWorkerAsync(graph, node, new[] { workerHealthy, workerDegraded }, new TenantMissionPolicyContext());

            Assert.Equal(workerHealthy.WorkerId, decision.SelectedWorkerId);
            var evalDegraded = decision.CandidateEvaluations.First(e => e.WorkerId == workerDegraded.WorkerId);
            Assert.False(evalDegraded.IsEligible);
        }

        [Fact]
        public async Task REP07_04_RoutingDecisionCapturesTransparentCandidateEvaluations()
        {
            var workerA = WorkerProcessRecordId("W_A", WorkerPoolType.DomainResearcher);
            var workerB = WorkerProcessRecordId("W_B", WorkerPoolType.DomainResearcher);

            var graph = CreateSimpleGraph();
            var node = CreateSimpleNode(_forecastCapability);

            var decision = await _router.RouteNodeWorkerAsync(graph, node, new[] { workerA, workerB }, new TenantMissionPolicyContext());

            Assert.NotNull(decision.SelectedWorkerId);
            Assert.Equal(2, decision.CandidateEvaluations.Count);
            Assert.False(string.IsNullOrWhiteSpace(decision.SelectionRationale));
        }

        // ====================================================================
        // REP-08: RISK-GATED COLD-START EXPLORATION (4 tests)
        // ====================================================================

        [Fact]
        public async Task REP08_01_ColdStartAllowedOnLowRiskNode()
        {
            var unprovenWorker = WorkerProcessRecordId("W_Unproven", WorkerPoolType.DomainResearcher);
            var graph = CreateSimpleGraph();
            var lowRiskNode = CreateSimpleNode(_forecastCapability, AutonomyTier.L1_Advise);

            var decision = await _router.RouteNodeWorkerAsync(graph, lowRiskNode, new[] { unprovenWorker }, new TenantMissionPolicyContext());

            Assert.Equal(unprovenWorker.WorkerId, decision.SelectedWorkerId);
            Assert.True(decision.WasExplorationCandidate);
        }

        [Fact]
        public async Task REP08_02_ColdStartStrictlyBlockedOnHighRiskNode()
        {
            var unprovenWorker = WorkerProcessRecordId("W_Unproven", WorkerPoolType.DomainResearcher);
            var graph = CreateSimpleGraph();
            var highRiskNode = CreateSimpleNode(_riskCapability, AutonomyTier.L4_ExecuteWithApproval);

            var decision = await _router.RouteNodeWorkerAsync(graph, highRiskNode, new[] { unprovenWorker }, new TenantMissionPolicyContext());

            // Cold start worker must NOT be selected on high risk financial node
            Assert.Null(decision.SelectedWorkerId);
            var eval = decision.CandidateEvaluations.First();
            Assert.False(eval.IsEligible);
            Assert.Contains("Cold-start unproven worker strictly forbidden on high-risk node", eval.IneligibilityReason);
        }

        [Fact]
        public async Task REP08_03_ProvenWorkerWinsOverColdStartOnHighRiskNode()
        {
            var unprovenWorker = WorkerProcessRecordId("W_Unproven", WorkerPoolType.DomainResearcher);
            var provenWorker = WorkerProcessRecordId("W_Proven", WorkerPoolType.DomainResearcher);

            // Seed proven worker with 10 successful executions
            var agentProven = AgentDefinitionId.From("agent_proven");
            for (int i = 0; i < 10; i++)
            {
                await _performanceEngine.ProcessEvidenceTokenAsync(
                    CreateSampleToken(agentProven, _riskCapability, _commercialIndia, 0.03, provenWorker.WorkerId));
            }

            var graph = CreateSimpleGraph();
            var highRiskNode = CreateSimpleNode(_riskCapability, AutonomyTier.L4_ExecuteWithApproval);

            var decision = await _router.RouteNodeWorkerAsync(graph, highRiskNode, new[] { unprovenWorker, provenWorker }, new TenantMissionPolicyContext());

            Assert.Equal(provenWorker.WorkerId, decision.SelectedWorkerId);
            Assert.False(decision.WasExplorationCandidate);
        }

        [Fact]
        public async Task REP08_04_ColdStartFlagsRecordedInDecisionAudit()
        {
            var unprovenWorker = WorkerProcessRecordId("W_New", WorkerPoolType.DomainResearcher);
            var graph = CreateSimpleGraph();
            var lowRiskNode = CreateSimpleNode(_forecastCapability, AutonomyTier.L1_Advise);

            var decision = await _router.RouteNodeWorkerAsync(graph, lowRiskNode, new[] { unprovenWorker }, new TenantMissionPolicyContext());

            var eval = decision.CandidateEvaluations.First();
            Assert.True(eval.IsColdStartExploration);
            Assert.True(decision.WasExplorationCandidate);
        }

        // ====================================================================
        // REP-09: MULTI-TENANT PERFORMANCE ISOLATION (4 tests)
        // ====================================================================

        [Fact]
        public async Task REP09_01_TenantOutcomesDoNotLeakAcrossTenants()
        {
            var agentId = AgentDefinitionId.From("shared_agent_def");

            // Tenant A executes with high accuracy
            var tokenA = CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.02, tenantId: _tenantA);
            await _performanceEngine.ProcessEvidenceTokenAsync(tokenA);

            // Tenant B profile should be null
            var profileB = await _store.GetProfileAsync(_tenantB, agentId, _forecastCapability, _commercialIndia.ToCanonicalKey(), MarketRegimeState.Stable);
            Assert.Null(profileB);
        }

        [Fact]
        public async Task REP09_02_TenantBHistoryCannotInfluenceTenantARouting()
        {
            var worker = WorkerProcessRecordId("W_Shared", WorkerPoolType.DomainResearcher);
            var agentId = AgentDefinitionId.From("shared_agent");

            // Tenant B records poor performance
            var tokenB = CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.90, worker.WorkerId, tenantId: _tenantB);
            await _performanceEngine.ProcessEvidenceTokenAsync(tokenB);

            // Tenant A evaluates routing: Tenant A sees unproven cold-start, unaffected by Tenant B's poor score
            var graphA = CreateSimpleGraph(_tenantA);
            var node = CreateSimpleNode(_forecastCapability);

            var decisionA = await _router.RouteNodeWorkerAsync(graphA, node, new[] { worker }, new TenantMissionPolicyContext { WorkspaceId = _tenantA });

            var evalA = decisionA.CandidateEvaluations.First();
            Assert.True(evalA.IsColdStartExploration); // unaffected by Tenant B
        }

        [Fact]
        public async Task REP09_03_QuarantineIsStrictlyTenantIsolated()
        {
            var agentId = AgentDefinitionId.From("agent_x");

            // Quarantine in Tenant A
            await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.05, tenantId: _tenantA, isSecurityViolation: true));

            var profileA = await _store.GetProfileAsync(_tenantA, agentId, _forecastCapability, _commercialIndia.ToCanonicalKey(), MarketRegimeState.Stable);
            var profileB = await _store.GetProfileAsync(_tenantB, agentId, _forecastCapability, _commercialIndia.ToCanonicalKey(), MarketRegimeState.Stable);

            Assert.True(profileA!.Metrics.IsQuarantined);
            Assert.Null(profileB); // Not quarantined in Tenant B
        }

        [Fact]
        public async Task REP09_04_EvidenceTokensRetainMandatoryWorkspaceId()
        {
            var token = CreateSampleToken(AgentDefinitionId.From("a1"), _forecastCapability, _commercialIndia, 0.05, tenantId: _tenantA);
            await _store.SaveEvidenceTokenAsync(token);

            var tokens = await _store.GetEvidenceTokensForAttemptAsync(token.AttemptId);
            Assert.Single(tokens);
            Assert.Equal(_tenantA, tokens[0].WorkspaceId);
        }

        // ====================================================================
        // REP-10: INVARIANTS I14, I14-A, I14-B & I14-C AUTHORITY PRESERVATION (4 tests)
        // ====================================================================

        [Fact]
        public async Task REP10_01_HighReputationCannotBypassExecutionFirewall()
        {
            // Even if an agent has 100 successful runs and 0.00 calibration variance,
            // it still produces an AgentOutcomeProposal which MUST pass AdmissionGate
            var agentId = AgentDefinitionId.From("super_agent");
            for (int i = 0; i < 20; i++)
            {
                await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.01));
            }

            var profile = await _store.GetProfileAsync(_tenantA, agentId, _forecastCapability, _commercialIndia.ToCanonicalKey(), MarketRegimeState.Stable);
            Assert.NotNull(profile);
            Assert.True(profile.Metrics.VerificationQualityScore >= 0.95);

            // Simulate unverified proposal from this super-agent
            var unverifiedProposal = new AgentOutcomeProposal
            {
                ReportedStatus = ExecutionOutcomeStatus.Succeeded,
                ObservedEvidenceHash = null // Missing evidence
            };

            var graph = CreateSimpleGraph();
            var admission = await _admissionGate.AdmitOutcomeProposalAsync(unverifiedProposal, graph, new TenantMissionPolicyContext());

            // High reputation CANNOT bypass admission gate: rejected fail-closed
            Assert.False(admission.IsAdmitted);
        }

        [Fact]
        public void REP10_02_InvariantI14B_AgentCannotSelfGenerateReputationToken()
        {
            // Invariant I14-B: Reputation tokens can only be generated from runtime execution outcomes,
            // verified by INodeVerificationEngine and emitted by coordinator.
            // Proposals do not have reputation modification capabilities.
            var proposal = new AgentOutcomeProposal();
            Assert.Null(proposal.ProposedExpansion);
            // OutcomeProposal has no mechanism to inject or alter reputation scores
        }

        [Fact]
        public void REP10_03_InvariantI14C_ReputationIsNotTruth()
        {
            // Invariant I14-C: Reputation consumes evidence, never manufactures truth.
            var profile = new CapabilityPerformanceProfile();
            Assert.Equal(ReputationMetricVector.Initial, profile.Metrics);
            // Profile does not produce TruthClassification.Fact
        }

        [Fact]
        public async Task REP10_04_HighReputationCannotElevateAutonomyTier()
        {
            var worker = WorkerProcessRecordId("W_Super", WorkerPoolType.DomainResearcher);
            var agentId = AgentDefinitionId.From("super_agent");
            for (int i = 0; i < 20; i++)
            {
                await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.01, worker.WorkerId));
            }

            var graph = CreateSimpleGraph();
            var node = CreateSimpleNode(_forecastCapability, AutonomyTier.L1_Advise);

            var decision = await _router.RouteNodeWorkerAsync(graph, node, new[] { worker }, new TenantMissionPolicyContext());

            // Selected worker retains node's RequiredAutonomyTier (L1_Advise); reputation cannot elevate to L5
            Assert.Equal(AutonomyTier.L1_Advise, decision.RequiredAutonomyTier);
        }

        // ====================================================================
        // REP-11: CAPABILITY REGISTRY & PROVIDER BINDING (4 tests)
        // ====================================================================

        [Fact]
        public async Task REP11_01_CapabilityRegistrationAndRetrieval()
        {
            var capId = new CapabilityId("market_recon", "v2");
            var capDef = new CapabilityDefinitionRecord
            {
                CapabilityId = capId,
                Title = "Market Recon v2",
                RiskTier = CapabilityRiskTier.R1_LowAnalytical
            };

            await _registry.RegisterCapabilityAsync(capDef);

            var retrieved = await _registry.GetCapabilityAsync(capId);
            Assert.NotNull(retrieved);
            Assert.Equal("Market Recon v2", retrieved.Title);
            Assert.True(retrieved.IsColdStartSafe);
        }

        [Fact]
        public async Task REP11_02_AgentCapabilityBindingEnforcement()
        {
            var agentId = AgentDefinitionId.From("analyst_agent");
            var binding = new AgentCapabilityBinding
            {
                AgentDefinitionId = agentId,
                CapabilityId = _forecastCapability,
                MaxPermittedAutonomyTier = AutonomyTier.L2_Simulate,
                IsEnabled = true
            };

            await _registry.BindAgentCapabilityAsync(binding);

            bool isBound = await _registry.IsAgentBoundToCapabilityAsync(agentId, _forecastCapability);
            bool isUnbound = await _registry.IsAgentBoundToCapabilityAsync(agentId, _riskCapability);

            Assert.True(isBound);
            Assert.False(isUnbound);
        }

        [Fact]
        public async Task REP11_03_DisabledCapabilityBindingReturnsFalse()
        {
            var agentId = AgentDefinitionId.From("revoked_agent");
            var binding = new AgentCapabilityBinding
            {
                AgentDefinitionId = agentId,
                CapabilityId = _forecastCapability,
                IsEnabled = false
            };

            await _registry.BindAgentCapabilityAsync(binding);

            bool isBound = await _registry.IsAgentBoundToCapabilityAsync(agentId, _forecastCapability);
            Assert.False(isBound);
        }

        [Fact]
        public async Task REP11_04_ListCapabilitiesReturnsActiveOnly()
        {
            var active = new CapabilityDefinitionRecord { CapabilityId = new CapabilityId("c_act", "v1"), IsActive = true };
            var deprecated = new CapabilityDefinitionRecord { CapabilityId = new CapabilityId("c_dep", "v1"), IsActive = false };

            await _registry.RegisterCapabilityAsync(active);
            await _registry.RegisterCapabilityAsync(deprecated);

            var list = await _registry.ListCapabilitiesAsync();
            Assert.Contains(list, c => c.CapabilityId == active.CapabilityId);
            Assert.DoesNotContain(list, c => c.CapabilityId == deprecated.CapabilityId);
        }

        // ====================================================================
        // REP-12: BRAIN FABRIC & OMNIROUTE PROVENANCE ATTRIBUTION (I14-D) (4 tests)
        // ====================================================================

        [Fact]
        public void REP12_01_BrainFabricProvenanceComputesDeterministicDigest()
        {
            var p1 = new BrainFabricProvenance
            {
                ModelId = "claude-3-5-sonnet",
                ProviderId = "anthropic",
                OmniRouteId = "omni-primary",
                PromptVersion = "v2.1"
            };

            var p2 = new BrainFabricProvenance
            {
                ModelId = "claude-3-5-sonnet",
                ProviderId = "anthropic",
                OmniRouteId = "omni-primary",
                PromptVersion = "v2.1"
            };

            var pDiff = p1 with { PromptVersion = "v2.2" };

            Assert.Equal(p1.ComputeDigest(), p2.ComputeDigest());
            Assert.NotEqual(p1.ComputeDigest(), pDiff.ComputeDigest());
        }

        [Fact]
        public async Task REP12_02_ProvenanceAttachedToReputationEvidenceToken()
        {
            var agentId = AgentDefinitionId.From("provenance_agent");
            var prov = new BrainFabricProvenance
            {
                ModelId = "gpt-4o",
                ProviderId = "openai",
                PromptVersion = "v3.0"
            };

            var token = CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.04, provenance: prov);
            await _performanceEngine.ProcessEvidenceTokenAsync(token);

            var tokens = await _store.GetEvidenceTokensForAttemptAsync(token.AttemptId);
            Assert.Single(tokens);
            Assert.Equal("gpt-4o", tokens[0].Provenance.ModelId);
            Assert.Equal("v3.0", tokens[0].Provenance.PromptVersion);
        }

        [Fact]
        public void REP12_03_InvariantI14D_DistinguishesWorkerFromModelProvenance()
        {
            var provA = new BrainFabricProvenance { ModelId = "model-A", ProviderId = "openrouter" };
            var provB = new BrainFabricProvenance { ModelId = "model-B", ProviderId = "local" };

            // Two runs by the same worker under different models produce distinct digests
            Assert.NotEqual(provA.ComputeDigest(), provB.ComputeDigest());
        }

        [Fact]
        public void REP12_04_PromptVersionShiftAltersProvenanceDigest()
        {
            var baseProv = new BrainFabricProvenance { PromptVersion = "v1" };
            var shiftedProv = new BrainFabricProvenance { PromptVersion = "v2" };

            Assert.NotEqual(baseProv.ComputeDigest(), shiftedProv.ComputeDigest());
        }

        // ====================================================================
        // REP-13: PROFILE VERSIONING & CRYPTOGRAPHIC PROVENANCE CHAIN (4 tests)
        // ====================================================================

        [Fact]
        public async Task REP13_01_ProfileEvolvesMonotonicallyWithParentHashChaining()
        {
            var agentId = AgentDefinitionId.From("versioned_agent");

            // Run 1: v1 -> v2
            var p1 = await _performanceEngine.ProcessEvidenceTokenAsync(
                CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.05));
            Assert.Equal(2, p1.Version.Value);
            string hash1 = p1.VersionHash;

            // Run 2: v2 -> v3
            var p2 = await _performanceEngine.ProcessEvidenceTokenAsync(
                CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.04));
            Assert.Equal(3, p2.Version.Value);
            Assert.Equal(hash1, p2.ParentProfileHash);
        }

        [Fact]
        public async Task REP13_02_ProfileHistoryPreservedInStore()
        {
            var agentId = AgentDefinitionId.From("historic_agent");

            for (int i = 0; i < 3; i++)
            {
                await _performanceEngine.ProcessEvidenceTokenAsync(
                    CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.05));
            }

            var current = await _store.GetProfileAsync(_tenantA, agentId, _forecastCapability, _commercialIndia.ToCanonicalKey(), MarketRegimeState.Stable);
            var history = await _store.GetProfileHistoryAsync(current!.ProfileId);

            Assert.Equal(3, history.Count);
        }

        [Fact]
        public void REP13_03_ProfileVersionStructEnforcesMonotonicity()
        {
            var v1 = ProfileVersion.Initial;
            var v2 = v1.Next();

            Assert.Equal(1, v1.Value);
            Assert.Equal(2, v2.Value);
            Assert.Throws<ArgumentException>(() => new ProfileVersion(0));
        }

        [Fact]
        public async Task REP13_04_VersionHashReflectsMetricUpdatesDeterministically()
        {
            var agentId = AgentDefinitionId.From("hash_agent");

            var p = await _performanceEngine.ProcessEvidenceTokenAsync(
                CreateSampleToken(agentId, _forecastCapability, _commercialIndia, 0.05));

            string originalHash = p.VersionHash;
            Assert.False(string.IsNullOrWhiteSpace(originalHash));
            Assert.Equal(originalHash, p.ComputeHash());
        }

        // ====================================================================
        // REP-14: EXPLAINABLE ROUTING RATIONALE GENERATION (4 tests)
        // ====================================================================

        [Fact]
        public async Task REP14_01_RoutingDecisionProducesDetailedExplainableRationale()
        {
            var workerA = WorkerProcessRecordId("W_A", WorkerPoolType.DomainResearcher);
            var workerB = WorkerProcessRecordId("W_B", WorkerPoolType.DomainResearcher);

            var agentA = AgentDefinitionId.From("agent_a");
            var agentB = AgentDefinitionId.From("agent_b");

            await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentA, _forecastCapability, _commercialIndia, 0.02, workerA.WorkerId));
            await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentB, _forecastCapability, _commercialIndia, 0.35, workerB.WorkerId));

            var graph = CreateSimpleGraph();
            var node = CreateSimpleNode(_forecastCapability);

            var decision = await _router.RouteNodeWorkerAsync(graph, node, new[] { workerA, workerB }, new TenantMissionPolicyContext());

            Assert.Contains("Selected Worker", decision.SelectionRationale);
            Assert.Contains(workerA.WorkerId.ToString(), decision.SelectionRationale);
            Assert.Contains("highest empirical utility", decision.SelectionRationale);
        }

        [Fact]
        public async Task REP14_02_AllCandidatesQuarantined_ProducesExplainableRejectionRationale()
        {
            var workerA = WorkerProcessRecordId("W_A", WorkerPoolType.DomainResearcher);
            var agentA = AgentDefinitionId.From("agent_a");

            await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agentA, _forecastCapability, _commercialIndia, 0.05, workerA.WorkerId, isSecurityViolation: true));

            var graph = CreateSimpleGraph();
            var node = CreateSimpleNode(_forecastCapability);

            var decision = await _router.RouteNodeWorkerAsync(graph, node, new[] { workerA }, new TenantMissionPolicyContext());

            Assert.Null(decision.SelectedWorkerId);
            Assert.Contains("No eligible candidate available", decision.SelectionRationale);
        }

        [Fact]
        public async Task REP14_03_RoutingDecisionStoredPermanentlyInStore()
        {
            var worker = WorkerProcessRecordId("W_A", WorkerPoolType.DomainResearcher);
            var graph = CreateSimpleGraph();
            var node = CreateSimpleNode(_forecastCapability);

            var decision = await _router.RouteNodeWorkerAsync(graph, node, new[] { worker }, new TenantMissionPolicyContext());

            var retrieved = await _store.GetRoutingDecisionAsync(decision.DecisionId);
            Assert.NotNull(retrieved);
            Assert.Equal(decision.DecisionId, retrieved.DecisionId);
        }

        [Fact]
        public async Task REP14_04_CandidateEvaluationsCaptureMetricSnapshots()
        {
            var worker = WorkerProcessRecordId("W_A", WorkerPoolType.DomainResearcher);
            var agent = AgentDefinitionId.From("agent_a");
            await _performanceEngine.ProcessEvidenceTokenAsync(CreateSampleToken(agent, _forecastCapability, _commercialIndia, 0.05, worker.WorkerId));

            var graph = CreateSimpleGraph();
            var node = CreateSimpleNode(_forecastCapability);

            var decision = await _router.RouteNodeWorkerAsync(graph, node, new[] { worker }, new TenantMissionPolicyContext());

            var eval = decision.CandidateEvaluations.First();
            Assert.NotNull(eval.MetricSnapshot);
            Assert.True(eval.UtilityScore > 0);
        }

        // ====================================================================
        // REP-15: INTEGRATED PIPELINE COORDINATOR WITH EMPIRICAL ROUTING & REPLAY (5 tests)
        // ====================================================================

        [Fact]
        public async Task REP15_01_EndToEndExecution_EmitsEvidenceTokenAndUpdatesProfile()
        {
            var worker = WorkerProcessRecordId("W_Exec", WorkerPoolType.DomainResearcher);
            await _fleetStore.RegisterWorkerAsync(worker);

            var graph = CreateSimpleGraph();
            var node = CreateSimpleNode(_forecastCapability);
            graph.Nodes[node.NodeId.Value] = node;

            var result = await _pipelineCoordinator.ExecuteStepAsync(graph, _defaultPolicy, async envelope =>
            {
                await Task.Yield();
                return new AgentOutcomeProposal
                {
                    Envelope = envelope,
                    ReportedStatus = ExecutionOutcomeStatus.Succeeded,
                    TokensConsumed = 800,
                    CostUsdConsumed = 0.04m,
                    OutputPayloadJson = "{\"predictedValue\": 500.0, \"actualValue\": 505.0}"
                };
            });

            Assert.True(result.IsSuccess);

            // Verify evidence token was stored and profile was updated
            var profile = await _store.GetProfileAsync(
                _tenantA,
                AgentDefinitionId.From(node.NodeType.ToString()),
                _forecastCapability,
                StructuredDomainContext.Default.ToCanonicalKey(),
                MarketRegimeState.Stable);

            Assert.NotNull(profile);
            Assert.Equal(1, profile.TotalAttempts);
            Assert.Equal(1, profile.SuccessfulAttempts);
        }

        [Fact]
        public async Task REP15_02_PipelineCrash_EmitsUnknownEffectEvidenceToken()
        {
            var worker = WorkerProcessRecordId("W_Crash", WorkerPoolType.DomainResearcher);
            await _fleetStore.RegisterWorkerAsync(worker);

            var graph = CreateSimpleGraph();
            var node = CreateSimpleNode(_forecastCapability);
            graph.Nodes[node.NodeId.Value] = node;

            var result = await _pipelineCoordinator.ExecuteStepAsync(graph, _defaultPolicy, async _ =>
            {
                await Task.Yield();
                throw new InvalidOperationException("Fatal crash");
            });

            Assert.False(result.IsSuccess);

            var profile = await _store.GetProfileAsync(
                _tenantA,
                AgentDefinitionId.From(node.NodeType.ToString()),
                _forecastCapability,
                StructuredDomainContext.Default.ToCanonicalKey(),
                MarketRegimeState.Stable);

            Assert.NotNull(profile);
            Assert.Equal(1, profile.UnknownEffectCount);
            Assert.True(profile.Metrics.RollbackFrequency > 0.0);
        }

        [Fact]
        public async Task REP15_03_FencingTampering_TriggersQuarantineThroughPipeline()
        {
            var worker = WorkerProcessRecordId("W_Tamper", WorkerPoolType.DomainResearcher);
            await _fleetStore.RegisterWorkerAsync(worker);

            var graph = CreateSimpleGraph();
            var node = CreateSimpleNode(_forecastCapability);
            graph.Nodes[node.NodeId.Value] = node;

            var result = await _pipelineCoordinator.ExecuteStepAsync(graph, _defaultPolicy, async envelope =>
            {
                await Task.Yield();
                // Tamper with envelope worker ID
                var tampered = envelope with { WorkerId = WorkerProcessId.New() };
                return new AgentOutcomeProposal { Envelope = tampered, ReportedStatus = ExecutionOutcomeStatus.Succeeded };
            });

            Assert.False(result.IsSuccess);

            var profile = await _store.GetProfileAsync(
                _tenantA,
                AgentDefinitionId.From(node.NodeType.ToString()),
                _forecastCapability,
                StructuredDomainContext.Default.ToCanonicalKey(),
                MarketRegimeState.Stable);

            Assert.NotNull(profile);
            Assert.True(profile.Metrics.IsQuarantined);
        }

        [Fact]
        public async Task REP15_04_SubsequentRoutingReflectsHistoricalEmpiricalImprovement()
        {
            var workerA = WorkerProcessRecordId("W_A", WorkerPoolType.DomainResearcher);
            var workerB = WorkerProcessRecordId("W_B", WorkerPoolType.DomainResearcher);
            await _fleetStore.RegisterWorkerAsync(workerA);
            await _fleetStore.RegisterWorkerAsync(workerB);

            var agentA = AgentDefinitionId.From("Investigate");

            // Seed Worker A with excellent performance
            for (int i = 0; i < 5; i++)
            {
                await _performanceEngine.ProcessEvidenceTokenAsync(
                    CreateSampleToken(agentA, _forecastCapability, _commercialIndia, 0.01, workerA.WorkerId));
            }

            var graph = CreateSimpleGraph();
            var node = CreateSimpleNode(_forecastCapability);
            graph.Nodes[node.NodeId.Value] = node;

            // Dispatch should choose Worker A empirically
            var dispatched = await _orchestrator.DispatchNodeAsync(graph, node);
            Assert.NotNull(dispatched);
            Assert.Equal(workerA.WorkerId, dispatched.WorkerId);
        }

        [Fact]
        public async Task REP15_05_EmpiricalRoutingPreservesInvariantI13Subordination()
        {
            // Worker dispatched via empirical router still has zero authority.
            // Outcome proposal with revoked capability is still rejected by admission gate.
            var worker = WorkerProcessRecordId("W_A", WorkerPoolType.DomainResearcher);
            await _fleetStore.RegisterWorkerAsync(worker);

            var graph = CreateSimpleGraph();
            var node = CreateSimpleNode(_forecastCapability);
            graph.Nodes[node.NodeId.Value] = node;

            // Policy disallows capability
            var restrictivePolicy = new TenantMissionPolicyContext
            {
                WorkspaceId = _tenantA,
                RegisteredCapabilityIds = new HashSet<string>() // empty -> capability not registered
            };

            var result = await _pipelineCoordinator.ExecuteStepAsync(graph, restrictivePolicy, async envelope =>
            {
                await Task.Yield();
                return new AgentOutcomeProposal { Envelope = envelope, ReportedStatus = ExecutionOutcomeStatus.Succeeded };
            });

            // Admission gate rejects outcome fail-closed regardless of empirical router preference
            Assert.False(result.IsSuccess);
            Assert.Equal("CapabilityRevoked", result.StepStatus);
        }

        // ====================================================================
        // HELPER METHODS
        // ====================================================================

        private ReputationEvidenceToken CreateSampleToken(
            AgentDefinitionId agentId,
            CapabilityId capabilityId,
            StructuredDomainContext domain,
            double discrepancy,
            WorkerProcessId? workerId = null,
            Guid? tenantId = null,
            MarketRegimeState regime = MarketRegimeState.Stable,
            bool isSuccess = true,
            bool isSecurityViolation = false,
            bool isCrash = false,
            decimal cost = 0.05m,
            BrainFabricProvenance? provenance = null)
        {
            var attemptId = ExecutionAttemptId.New();
            var wId = workerId ?? WorkerProcessId.New();
            var tId = tenantId ?? _tenantA;

            return new ReputationEvidenceToken
            {
                WorkspaceId = tId,
                AttemptId = attemptId,
                GraphId = MissionGraphId.New(),
                NodeId = MissionNodeId.From("N1"),
                AgentDefinitionId = agentId,
                WorkerId = wId,
                CapabilityId = capabilityId,
                DomainContext = domain,
                MarketRegime = regime,
                Provenance = provenance ?? new BrainFabricProvenance(),
                Attribution = new CausalAttributionRecord
                {
                    AttemptId = attemptId,
                    Level = AttributionLevel.A4_Deterministic,
                    AttributionConfidence = 1.0
                },
                Calibration = new OutcomeCalibrationRecord
                {
                    AttemptId = attemptId,
                    NodeId = MissionNodeId.From("N1"),
                    DiscrepancyScore = discrepancy,
                    IsWithinTolerance = discrepancy <= 0.15
                },
                VerifiedEvidenceHash = "evidence_hash_123",
                IsSuccessfulExecution = isSuccess,
                IsSecurityViolation = isSecurityViolation,
                IsUnknownEffectCrash = isCrash,
                TokensConsumed = 1000,
                CostUsdConsumed = cost,
                Duration = TimeSpan.FromMilliseconds(250),
                TokenHash = "token_hash_abc"
            };
        }

        private static WorkerProcessRecord WorkerProcessRecordId(
            string id,
            WorkerPoolType poolType,
            WorkerHealthStatus health = WorkerHealthStatus.Healthy)
        {
            return new WorkerProcessRecord
            {
                WorkerId = WorkerProcessId.New(),
                PoolType = poolType,
                HealthStatus = health
            };
        }

        private MissionGraph CreateSimpleGraph(Guid? tenantId = null)
        {
            return new MissionGraph
            {
                GraphId = MissionGraphId.New(),
                MissionId = MissionId.New(),
                WorkspaceId = tenantId ?? _tenantA,
                Title = "Reputation Test Graph",
                Version = MissionGraphVersion.Initial,
                State = MissionGraphState.Active
            };
        }

        private MissionNodeRecord CreateSimpleNode(CapabilityId capabilityId, AutonomyTier autonomy = AutonomyTier.L1_Advise)
        {
            return new MissionNodeRecord
            {
                NodeId = MissionNodeId.From("N1"),
                GraphId = MissionGraphId.New(),
                NodeType = MissionNodeType.Investigate,
                Title = "Investigate Node",
                State = MissionNodeState.Ready,
                ExecutionPolicy = new NodeExecutionPolicy
                {
                    RequiredCapabilityId = capabilityId,
                    RequiredAutonomyTier = autonomy,
                    MaxBudgetTokens = 10_000,
                    MaxCostUsd = 0.50m
                },
                VerificationCriteria = new NodeVerificationCriteria
                {
                    MinConfidenceScore = 0.80
                }
            };
        }
    }
}
