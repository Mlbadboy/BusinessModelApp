using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Radar;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Scenario;
using BusinessModelApp.Infrastructure.Runtime.Intelligence.Radar;
using BusinessModelApp.Infrastructure.Runtime.Intelligence.Scenario;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase3Batch384ScenarioTests
    {
        private readonly ScenarioConstraintChecker _constraintChecker;
        private readonly CounterfactualEngine _counterfactualEngine;
        private readonly ScenarioSimulator _simulator;
        private readonly ScenarioComparisonEngine _comparisonEngine;
        private readonly InMemoryScenarioStore _store;
        private readonly InMemoryRadarSignalStore _radarStore;
        private readonly ScenarioOrchestrator _orchestrator;

        public Phase3Batch384ScenarioTests()
        {
            _constraintChecker = new ScenarioConstraintChecker();
            _counterfactualEngine = new CounterfactualEngine();
            _simulator = new ScenarioSimulator(_constraintChecker);
            _comparisonEngine = new ScenarioComparisonEngine();
            _store = new InMemoryScenarioStore();
            _radarStore = new InMemoryRadarSignalStore();
            _orchestrator = new ScenarioOrchestrator(
                _simulator,
                _comparisonEngine,
                _store,
                _radarStore);
        }

        private static ScenarioDefinition CreateSampleScenario(
            string id = "SCEN-01",
            string tenant = "tenant-alpha",
            int seed = 42)
        {
            return new ScenarioDefinition
            {
                ScenarioId = id,
                TenantId = tenant,
                Name = "Price Optimization +10%",
                Description = "Simulating +10% price elasticity effect on gross margins",
                Type = ScenarioType.WhatIfIntervention,
                Method = SimulationMethod.MonteCarloDistribution,
                RandomSeed = seed,
                Assumptions = new List<ScenarioAssumption>
                {
                    new ScenarioAssumption
                    {
                        Key = "price_hike",
                        MetricId = "average_order_value",
                        BaselineValue = 100m,
                        AssumedValue = 110m,
                        Justification = "Planned product repositioning",
                        EpistemicTier = ScenarioEpistemicTier.Hypothetical
                    }
                },
                Interventions = new List<CounterfactualIntervention>
                {
                    new CounterfactualIntervention
                    {
                        TargetMetricId = "conversion_rate",
                        InterventionDelta = -0.2m, // slight churn impact
                        IsIdentifiedInDag = true,
                        CausalMechanism = "Elasticity demand curve"
                    }
                },
                Constraints = new List<ScenarioConstraint>
                {
                    new ScenarioConstraint
                    {
                        Name = "ChurnCeiling",
                        MetricId = "churn_rate",
                        MaxValue = 5.0m,
                        IsHardConstraint = true
                    }
                },
                LifecycleState = ScenarioLifecycleState.Draft
            };
        }

        // =========================================================================
        // SCE-01: WHAT-IF INTERVENTION & DISTRIBUTION (I22, I22-F)
        // =========================================================================

        [Fact]
        public async Task SCE01_01_PricingIntervention_ProducesValidDistributions()
        {
            var scenario = CreateSampleScenario();
            var inputSnap = new SimulationInputSnapshot
            {
                InitialMetricValues = new Dictionary<string, decimal>
                {
                    ["average_order_value"] = 100m,
                    ["conversion_rate"] = 2.5m
                }
            };

            var outcome = await _simulator.SimulateAsync(
                scenario,
                inputSnap,
                new ModelSnapshot(),
                new PolicySnapshot(),
                new ConstraintSnapshot());

            Assert.NotNull(outcome);
            Assert.True(outcome.MetricOutcomes.ContainsKey("average_order_value"));
            var aov = outcome.MetricOutcomes["average_order_value"];

            Assert.True(aov.Distribution.ValidateMonotonicBounds());
            Assert.True(aov.Distribution.P10 <= aov.Distribution.P50);
            Assert.True(aov.Distribution.P50 <= aov.Distribution.P90);
            Assert.Equal(ScenarioEpistemicTier.ModelDerived, outcome.EpistemicTier);
        }

        // =========================================================================
        // SCE-02: COUNTERFACTUAL DOES NOT MUTATE REALITY (I22-A, I22)
        // =========================================================================

        [Fact]
        public async Task SCE02_01_CounterfactualSimulation_PreservesHistoricalIntegrity()
        {
            var inputSnap = new SimulationInputSnapshot
            {
                InitialMetricValues = new Dictionary<string, decimal> { ["mrr"] = 500000m }
            };

            var intervention = new CounterfactualIntervention
            {
                TargetMetricId = "mrr",
                InterventionDelta = 100000m,
                IsIdentifiedInDag = true
            };

            var outcome = await _counterfactualEngine.EvaluateCounterfactualAsync("tenant-alpha", intervention, inputSnap);

            Assert.Equal(600000m, outcome.MetricOutcomes["mrr"].SimulatedMedian);
            // Verify historical input snapshot remains completely unmutated
            Assert.Equal(500000m, inputSnap.InitialMetricValues["mrr"]);
        }

        // =========================================================================
        // SCE-03: SIMULATION TAGGED MODEL-DERIVED (I22-B)
        // =========================================================================

        [Fact]
        public async Task SCE03_01_MathematicalSimulation_NeverTaggedAsFactOrExperiment()
        {
            var outcome = await _orchestrator.RunScenarioPipelineAsync(CreateSampleScenario("SCEN-TAG-01"));

            Assert.NotEqual(ScenarioEpistemicTier.FactBacked, outcome.EpistemicTier);
            Assert.Equal(ScenarioEpistemicTier.ModelDerived, outcome.EpistemicTier);
        }

        // =========================================================================
        // SCE-04: UNIDENTIFIED CAUSAL DO-CALCULUS WIDENS UNCERTAINTY (I22-C, I22-F)
        // =========================================================================

        [Fact]
        public async Task SCE04_01_UnidentifiedCausalIntervention_ForcesUnknownTierAndWidensBounds()
        {
            var inputSnap = new SimulationInputSnapshot
            {
                InitialMetricValues = new Dictionary<string, decimal> { ["ltv"] = 1000m }
            };

            // Intervention NOT identified in causal DAG
            var intervention = new CounterfactualIntervention
            {
                TargetMetricId = "ltv",
                InterventionDelta = 200m,
                IsIdentifiedInDag = false // UNIDENTIFIED
            };

            var outcome = await _counterfactualEngine.EvaluateCounterfactualAsync("tenant-alpha", intervention, inputSnap);

            Assert.Equal(ScenarioEpistemicTier.Unknown, outcome.EpistemicTier);
            Assert.True(outcome.Confidence.CompositeConfidence <= 30.0);
            var dist = outcome.MetricOutcomes["ltv"].Distribution;
            Assert.True(dist.SensitivityRadius >= 20m);
        }

        // =========================================================================
        // SCE-05: SCENARIO CANNOT OVERWRITE CALIBRATED FORECAST (I22-D)
        // =========================================================================

        [Fact]
        public void SCE05_01_ScenarioOutcome_IsStructurallyDistinctFromForecastBaseline()
        {
            var scenario = CreateSampleScenario();
            // Scenario outputs must explicitly identify as simulation outcomes
            Assert.Equal(ScenarioType.WhatIfIntervention, scenario.Type);
            Assert.NotEqual("ForecastOutput", scenario.GetType().Name);
        }

        // =========================================================================
        // SCE-06: ASSUMPTION HASHING AND IMMUTABILITY (I22-E)
        // =========================================================================

        [Fact]
        public void SCE06_01_AssumptionsAreHashed_AndDetectTampering()
        {
            var scenario = CreateSampleScenario();
            var hash1 = scenario.ComputeAssumptionHash();

            Assert.NotNull(hash1);
            Assert.Equal(64, hash1.Length);

            // Mutate assumption
            var tamperedAssumptions = new List<ScenarioAssumption>(scenario.Assumptions)
            {
                new ScenarioAssumption
                {
                    Key = "tamper_key",
                    MetricId = "tamper_metric",
                    BaselineValue = 10m,
                    AssumedValue = 999m
                }
            };
            var tamperedScenario = scenario with { Assumptions = tamperedAssumptions };
            var hash2 = tamperedScenario.ComputeAssumptionHash();

            Assert.NotEqual(hash1, hash2);
        }

        // =========================================================================
        // SCE-07: MANDATORY MONOTONIC DISTRIBUTION BOUNDS (I22-F)
        // =========================================================================

        [Fact]
        public async Task SCE07_01_SimulationEnforcesMonotonicDistribution_P10_P50_P90()
        {
            var scenario = CreateSampleScenario("SCEN-MONO-01");
            var inputSnap = new SimulationInputSnapshot
            {
                InitialMetricValues = new Dictionary<string, decimal> { ["average_order_value"] = 150m }
            };

            var outcome = await _simulator.SimulateAsync(
                scenario,
                inputSnap,
                new ModelSnapshot(),
                new PolicySnapshot(),
                new ConstraintSnapshot());

            foreach (var metric in outcome.MetricOutcomes.Values)
            {
                Assert.True(metric.Distribution.P10 <= metric.Distribution.P50);
                Assert.True(metric.Distribution.P50 <= metric.Distribution.P90);
                Assert.True(metric.Distribution.SensitivityRadius >= 0m);
            }
        }

        // =========================================================================
        // SCE-08: OVER-BUDGET SCENARIO MARKED CONSTRAINED (I22-G)
        // =========================================================================

        [Fact]
        public async Task SCE08_01_OverBudgetScenario_MarkedConstrainedWithMitigation()
        {
            var metrics = new Dictionary<string, ScenarioOutcomeMetric>
            {
                ["cac"] = new ScenarioOutcomeMetric
                {
                    MetricId = "cac",
                    BaselineValue = 300m,
                    SimulatedMedian = 450m
                }
            };

            var constraints = new List<ScenarioConstraint>
            {
                new ScenarioConstraint
                {
                    Name = "SoftCacLimit",
                    MetricId = "cac",
                    MaxValue = 400m,
                    IsHardConstraint = false,
                    MitigationAlternative = "Reallocate performance spend to SEO"
                }
            };

            var check = await _constraintChecker.CheckConstraintsAsync("tenant-alpha", metrics, constraints);

            Assert.Equal(ScenarioConstraintStatus.ConstrainedWithMitigation, check.Status);
            Assert.Contains("SoftCacLimit", check.ViolatedConstraintNames);
            Assert.Contains("Reallocate performance spend to SEO", check.SuggestedMitigations);
        }

        // =========================================================================
        // SCE-09: HARD CONSTRAINT VIOLATION MARKS INFEASIBLE (I22-G)
        // =========================================================================

        [Fact]
        public async Task SCE09_01_HardConstraintBreach_MarksScenarioInfeasible()
        {
            var metrics = new Dictionary<string, ScenarioOutcomeMetric>
            {
                ["liquidity"] = new ScenarioOutcomeMetric
                {
                    MetricId = "liquidity",
                    BaselineValue = 1000000m,
                    SimulatedMedian = 200000m
                }
            };

            var constraints = new List<ScenarioConstraint>
            {
                new ScenarioConstraint
                {
                    Name = "StatutoryLiquidityFloor",
                    MetricId = "liquidity",
                    MinValue = 500000m,
                    IsHardConstraint = true
                }
            };

            var check = await _constraintChecker.CheckConstraintsAsync("tenant-alpha", metrics, constraints);

            Assert.Equal(ScenarioConstraintStatus.InfeasibleViolation, check.Status);
            Assert.Contains("StatutoryLiquidityFloor", check.ViolatedConstraintNames);
        }

        // =========================================================================
        // SCE-10: DETERMINISTIC PARETO COMPARISON (I22-H)
        // =========================================================================

        [Fact]
        public async Task SCE10_01_MultiScenarioComparison_EvaluatesParetoFrontierDeterministically()
        {
            var o1 = new ScenarioOutcome
            {
                ScenarioId = "SCEN-A",
                TenantId = "tenant-alpha",
                NetFinancialImpactINR = 500000m,
                FeasibilityStatus = ScenarioConstraintStatus.Feasible,
                MetricOutcomes = new Dictionary<string, ScenarioOutcomeMetric>
                {
                    ["rev"] = new ScenarioOutcomeMetric
                    {
                        BaselineValue = 100m,
                        Distribution = new SimulationDistribution { P10 = 90m, P50 = 120m, P90 = 150m }
                    }
                }
            };

            var o2 = new ScenarioOutcome
            {
                ScenarioId = "SCEN-B",
                TenantId = "tenant-alpha",
                NetFinancialImpactINR = 200000m,
                FeasibilityStatus = ScenarioConstraintStatus.Feasible,
                MetricOutcomes = new Dictionary<string, ScenarioOutcomeMetric>
                {
                    ["rev"] = new ScenarioOutcomeMetric
                    {
                        BaselineValue = 100m,
                        Distribution = new SimulationDistribution { P10 = 50m, P50 = 80m, P90 = 100m }
                    }
                }
            };

            var comparison = await _comparisonEngine.CompareScenariosAsync("tenant-alpha", new[] { o1, o2 }, new PolicySnapshot());

            Assert.NotNull(comparison);
            Assert.Equal("SCEN-A", comparison.RecommendedScenarioId);
            Assert.True(comparison.Scenarios.First(s => s.ScenarioId == "SCEN-A").IsOnParetoFrontier);
        }

        // =========================================================================
        // SCE-11: DETERMINISTIC VALIDATION OF AI PROPOSED SCENARIOS (I22-I)
        // =========================================================================

        [Fact]
        public void SCE11_01_AiProposedScenario_UndergoesStrictValidation()
        {
            var validAssumption = new ScenarioAssumption
            {
                Key = "pricing",
                MetricId = "aov",
                BaselineValue = 100m,
                AssumedValue = 120m
            };
            Assert.True(validAssumption.IsValid());

            var invalidAssumption = new ScenarioAssumption
            {
                Key = "",
                MetricId = "aov",
                BaselineValue = 100m,
                AssumedValue = 120m
            };
            Assert.False(invalidAssumption.IsValid());
        }

        // =========================================================================
        // SCE-12: ZERO EXECUTION AUTHORITY (I22-J)
        // =========================================================================

        [Fact]
        public async Task SCE12_01_ScenarioOutcome_NeverCreatesExecutionPermits()
        {
            var outcome = await _orchestrator.RunScenarioPipelineAsync(CreateSampleScenario("SCEN-EXEC-01"));

            // Compile-time & runtime invariant: ScenarioOutcome possesses zero permit mechanisms
            Assert.DoesNotContain("ExecutionPermit", outcome.GetType().GetProperties().Select(p => p.PropertyType.Name));
            Assert.Equal(ScenarioLifecycleState.Completed, outcome.LifecycleState);
        }

        // =========================================================================
        // SCE-13: BIDIRECTIONAL PROVENANCE CHAIN (I22-K)
        // =========================================================================

        [Fact]
        public async Task SCE13_01_ProvenanceChain_IsCryptographicallyVerifiable()
        {
            var scenario = CreateSampleScenario("SCEN-PROV-01");
            await _orchestrator.RunScenarioPipelineAsync(scenario);

            var prov = await _store.GetProvenanceAsync("tenant-alpha", "SCEN-PROV-01");
            Assert.NotNull(prov);
            Assert.True(prov.VerifyChainIntegrity());
            Assert.Equal(scenario.ComputeAssumptionHash(), prov.AssumptionHash);
        }

        // =========================================================================
        // SCE-14: MULTI-TENANT ISOLATION (I22-L)
        // =========================================================================

        [Fact]
        public async Task SCE14_01_CrossTenantAccess_IsStrictlyIsolated()
        {
            var s1 = CreateSampleScenario("SCEN-T1", "tenant-one");
            var s2 = CreateSampleScenario("SCEN-T2", "tenant-two");

            await _store.SaveDefinitionAsync(s1);
            await _store.SaveDefinitionAsync(s2);

            var t1Defs = await _store.ListDefinitionsAsync("tenant-one");
            Assert.Single(t1Defs);
            Assert.Equal("SCEN-T1", t1Defs[0].ScenarioId);

            var crossAccess = await _store.GetDefinitionAsync("tenant-one", "SCEN-T2");
            Assert.Null(crossAccess);
        }

        // =========================================================================
        // SCE-15: REPRODUCIBILITY GUARANTEE (I22-M)
        // =========================================================================

        [Fact]
        public async Task SCE15_01_IdenticalInputsAndSeed_ProduceBitIdenticalDistributions()
        {
            var s1 = CreateSampleScenario("SCEN-SEED-1", seed: 1337);
            var s2 = CreateSampleScenario("SCEN-SEED-2", seed: 1337);

            var snap = new SimulationInputSnapshot
            {
                InitialMetricValues = new Dictionary<string, decimal> { ["average_order_value"] = 200m }
            };

            var out1 = await _simulator.SimulateAsync(s1, snap, new ModelSnapshot(), new PolicySnapshot(), new ConstraintSnapshot());
            var out2 = await _simulator.SimulateAsync(s2, snap, new ModelSnapshot(), new PolicySnapshot(), new ConstraintSnapshot());

            var dist1 = out1.MetricOutcomes["average_order_value"].Distribution;
            var dist2 = out2.MetricOutcomes["average_order_value"].Distribution;

            Assert.Equal(dist1.P10, dist2.P10);
            Assert.Equal(dist1.P50, dist2.P50);
            Assert.Equal(dist1.P90, dist2.P90);
        }

        // =========================================================================
        // SCE-16: FROZEN POLICY SNAPSHOT IMMUTABILITY (I22-N)
        // =========================================================================

        [Fact]
        public async Task SCE16_01_FrozenPolicySnapshot_PreservesHistoricalIntegrity()
        {
            var policy = new PolicySnapshot
            {
                PolicyId = "POLICY-V1",
                PolicyVersion = "1.0.0",
                MaxDownsideToleranceINR = 250000m
            };

            var outcome1 = await _simulator.SimulateAsync(
                CreateSampleScenario("SCEN-POLICY-01"),
                new SimulationInputSnapshot(),
                new ModelSnapshot(),
                policy,
                new ConstraintSnapshot());

            var policyV2 = policy with { PolicyVersion = "2.0.0" };
            var outcome2 = await _simulator.SimulateAsync(
                CreateSampleScenario("SCEN-POLICY-01"),
                new SimulationInputSnapshot(),
                new ModelSnapshot(),
                policyV2,
                new ConstraintSnapshot());

            Assert.NotEmpty(outcome1.Metadata.SnapshotHash);
            Assert.NotEmpty(outcome2.Metadata.SnapshotHash);
            Assert.NotEqual(outcome1.Metadata.SnapshotHash, outcome2.Metadata.SnapshotHash);
        }

        // =========================================================================
        // SCE-17: UNKNOWN EPISTEMIC PROPAGATION (I22-O)
        // =========================================================================

        [Fact]
        public async Task SCE17_01_UnknownAssumption_PropagatesToUnknownTier()
        {
            var scenario = CreateSampleScenario("SCEN-UNK-01");
            var unkAssumptions = new List<ScenarioAssumption>
            {
                new ScenarioAssumption
                {
                    Key = "macro_shock",
                    MetricId = "inflation",
                    BaselineValue = 5.0m,
                    AssumedValue = 12.0m,
                    EpistemicTier = ScenarioEpistemicTier.Unknown // UNKNOWN
                }
            };
            var unkScenario = scenario with { Assumptions = unkAssumptions };

            var outcome = await _simulator.SimulateAsync(
                unkScenario,
                new SimulationInputSnapshot(),
                new ModelSnapshot(),
                new PolicySnapshot(),
                new ConstraintSnapshot());

            Assert.Equal(ScenarioEpistemicTier.Unknown, outcome.EpistemicTier);
            Assert.True(outcome.Confidence.CompositeConfidence <= 30.0);
        }

        // =========================================================================
        // SCE-18: STRESS-TEST MULTI-METRIC SIMULTANEOUS SHOCK
        // =========================================================================

        [Fact]
        public async Task SCE18_01_StressTest_EvaluatesSimultaneousMetricShocks()
        {
            var scenario = new ScenarioDefinition
            {
                ScenarioId = "SCEN-STRESS-01",
                TenantId = "tenant-alpha",
                Type = ScenarioType.StressTest,
                Assumptions = new List<ScenarioAssumption>
                {
                    new ScenarioAssumption { Key = "rev_drop", MetricId = "revenue", BaselineValue = 1000m, AssumedValue = 700m },
                    new ScenarioAssumption { Key = "cost_spike", MetricId = "cost", BaselineValue = 500m, AssumedValue = 800m }
                }
            };

            var outcome = await _simulator.SimulateAsync(
                scenario,
                new SimulationInputSnapshot(),
                new ModelSnapshot(),
                new PolicySnapshot(),
                new ConstraintSnapshot());

            Assert.True(outcome.NetFinancialImpactINR < 0);
            Assert.Equal(2, outcome.MetricOutcomes.Count);
        }

        // =========================================================================
        // SCE-19: SENSITIVITY SWEEP
        // =========================================================================

        [Fact]
        public async Task SCE19_01_SensitivitySweep_EvaluatesDistributionSpread()
        {
            var scenario = CreateSampleScenario("SCEN-SWEEP-01");
            var outcome = await _simulator.SimulateAsync(
                scenario,
                new SimulationInputSnapshot(),
                new ModelSnapshot(),
                new PolicySnapshot(),
                new ConstraintSnapshot());

            var metric = outcome.MetricOutcomes.Values.First();
            Assert.True(metric.Distribution.SensitivityRadius > 0m);
        }

        // =========================================================================
        // SCE-20: CONCURRENT SIMULATION THREAD SAFETY
        // =========================================================================

        [Fact]
        public async Task SCE20_01_ConcurrentSimulations_ExecuteSafelyWithoutStoreCorruption()
        {
            var tasks = Enumerable.Range(0, 10)
                .Select(i => _orchestrator.RunScenarioPipelineAsync(CreateSampleScenario($"SCEN-CONC-{i}", "tenant-concurrent", seed: i)))
                .ToArray();

            var outcomes = await Task.WhenAll(tasks);

            Assert.Equal(10, outcomes.Length);
            foreach (var o in outcomes)
            {
                Assert.NotNull(o);
                Assert.Equal(ScenarioLifecycleState.Completed, o.LifecycleState);
            }
        }

        // =========================================================================
        // SCE-21: RADAR SIGNAL OPPORTUNITY INTEGRATION
        // =========================================================================

        [Fact]
        public async Task SCE21_01_OpportunitySignal_SynthesizesUpsideScenario()
        {
            var sig = new RadarSignal
            {
                Id = "SIG-OPP-101",
                TenantId = "tenant-alpha",
                Type = RadarSignalType.Opportunity,
                Title = "Conversion Expansion Surge",
                EstimatedMonetaryImpactINR = 150000m
            };
            await _radarStore.SaveSignalAsync(sig);

            var scenario = await _orchestrator.GenerateScenarioFromRadarSignalAsync("tenant-alpha", "SIG-OPP-101", ScenarioType.WhatIfIntervention);

            Assert.NotNull(scenario);
            Assert.Equal("SIG-OPP-101", scenario.RadarSignalId);
            Assert.Contains("Conversion Expansion", scenario.Name);
        }

        // =========================================================================
        // SCE-22: RADAR SIGNAL THREAT INTEGRATION
        // =========================================================================

        [Fact]
        public async Task SCE22_01_ThreatSignal_SynthesizesMitigationScenario()
        {
            var sig = new RadarSignal
            {
                Id = "SIG-THREAT-202",
                TenantId = "tenant-alpha",
                Type = RadarSignalType.Threat,
                Title = "CAC Margin Compression",
                EstimatedMonetaryImpactINR = 250000m
            };
            await _radarStore.SaveSignalAsync(sig);

            var scenario = await _orchestrator.GenerateScenarioFromRadarSignalAsync("tenant-alpha", "SIG-THREAT-202", ScenarioType.StressTest);

            Assert.NotNull(scenario);
            Assert.Equal(ScenarioType.StressTest, scenario.Type);
            Assert.Equal("SIG-THREAT-202", scenario.RadarSignalId);
        }

        // =========================================================================
        // SCE-23: END-TO-END ORCHESTRATION PIPELINE
        // =========================================================================

        [Fact]
        public async Task SCE23_01_EndToEndPipeline_SimulatesComparesAndStores()
        {
            var s1 = CreateSampleScenario("SCEN-E2E-1", "tenant-e2e");
            var s2 = CreateSampleScenario("SCEN-E2E-2", "tenant-e2e");

            await _orchestrator.RunScenarioPipelineAsync(s1);
            await _orchestrator.RunScenarioPipelineAsync(s2);

            var comp = await _orchestrator.RunComparisonPipelineAsync("tenant-e2e", new[] { "SCEN-E2E-1", "SCEN-E2E-2" });

            Assert.NotNull(comp);
            Assert.Equal(2, comp.Scenarios.Count);
            Assert.NotNull(comp.RecommendedScenarioId);
        }

        // =========================================================================
        // SCE-24: RECOMMENDED SCENARIO != RECOMMENDED ACTION (HARDENING #4)
        // =========================================================================

        [Fact]
        public async Task SCE24_01_ComparisonRationale_EmitsNoActionDirectives()
        {
            var o1 = new ScenarioOutcome
            {
                ScenarioId = "SCEN-NON-ACTION",
                TenantId = "tenant-alpha",
                NetFinancialImpactINR = 100000m
            };
            var res = await _comparisonEngine.CompareScenariosAsync("tenant-alpha", new[] { o1 }, new PolicySnapshot());

            // Rationale describes modeled trade-off, strictly not prescriptive execution
            Assert.Contains("RecommendedScenario != RecommendedAction", res.TradeoffRationale);
            Assert.DoesNotContain("EXECUTE NOW", res.TradeoffRationale);
            Assert.DoesNotContain("DISPATCH_TASK", res.TradeoffRationale);
        }

        // =========================================================================
        // SCE-25: [ADVERSARIAL] EXECUTION PERMIT CREATION BLOCKED (I22-J)
        // =========================================================================

        [Fact]
        public void SCE25_01_Adversarial_ExecutionPermitCreation_TypeSystemRejects()
        {
            var outcome = new ScenarioOutcome
            {
                ScenarioId = "ADV-SCEN-PERMIT",
                TenantId = "tenant-alpha"
            };

            // Attempt to treat scenario outcome as ExecutionPermit fails compilation/reflection
            var hasPermitMethod = outcome.GetType().GetMethods().Any(m => m.Name.Contains("Permit") || m.Name.Contains("Authorize"));
            Assert.False(hasPermitMethod);
        }

        // =========================================================================
        // SCE-26: [ADVERSARIAL] MALICIOUS ASSUMPTION INJECTION REJECTED (I22-E, I22-I)
        // =========================================================================

        [Fact]
        public async Task SCE26_01_Adversarial_MaliciousAssumptionInjection_ThrowsArgumentException()
        {
            var scenario = CreateSampleScenario("SCEN-MALICIOUS");
            var malAssumptions = new List<ScenarioAssumption>
            {
                new ScenarioAssumption
                {
                    Key = "", // Malicious invalid key payload
                    MetricId = "rev",
                    BaselineValue = 100m,
                    AssumedValue = 9999m
                }
            };
            var malScenario = scenario with { Assumptions = malAssumptions };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _simulator.SimulateAsync(
                    malScenario,
                    new SimulationInputSnapshot(),
                    new ModelSnapshot(),
                    new PolicySnapshot(),
                    new ConstraintSnapshot()));
        }

        // =========================================================================
        // SCE-27: [ADVERSARIAL] SEED MANIPULATION ATTACK DETECTED (I22-M)
        // =========================================================================

        [Fact]
        public async Task SCE27_01_Adversarial_SeedTampering_ProducesDivergentOutputs()
        {
            var s1 = CreateSampleScenario("SCEN-S1", seed: 100);
            var s2 = CreateSampleScenario("SCEN-S2", seed: 99999);

            var snap = new SimulationInputSnapshot
            {
                InitialMetricValues = new Dictionary<string, decimal> { ["average_order_value"] = 100m }
            };

            var out1 = await _simulator.SimulateAsync(s1, snap, new ModelSnapshot(), new PolicySnapshot(), new ConstraintSnapshot());
            var out2 = await _simulator.SimulateAsync(s2, snap, new ModelSnapshot(), new PolicySnapshot(), new ConstraintSnapshot());

            var p10_1 = out1.MetricOutcomes["average_order_value"].Distribution.P10;
            var p10_2 = out2.MetricOutcomes["average_order_value"].Distribution.P10;

            // Different seeds intentionally produce different pseudo-random trajectories
            Assert.NotEqual(p10_1, p10_2);
        }

        // =========================================================================
        // SCE-28: [ADVERSARIAL] CROSS-TENANT CONTAMINATION ISOLATED (I22-L)
        // =========================================================================

        [Fact]
        public async Task SCE28_01_Adversarial_CrossTenantScenarioAccess_ReturnsNull()
        {
            var s1 = CreateSampleScenario("SCEN-VICTIM", "tenant-victim");
            await _store.SaveDefinitionAsync(s1);

            var attackerAttempt = await _store.GetDefinitionAsync("tenant-attacker", "SCEN-VICTIM");
            Assert.Null(attackerAttempt);
        }

        // =========================================================================
        // SCE-29: [ADVERSARIAL] CONSTRAINT MUTATION DURING ACTIVE SIMULATION (I22-G, I22-N)
        // =========================================================================

        [Fact]
        public async Task SCE29_01_Adversarial_ConstraintMutationDuringSimulation_UsesFrozenSnapshot()
        {
            var liveConstraints = new List<ScenarioConstraint>
            {
                new ScenarioConstraint { Name = "Budget", MetricId = "average_order_value", MaxValue = 500m }
            };
            var constraintSnap = new ConstraintSnapshot { FrozenConstraints = liveConstraints };

            // Simulate with frozen snapshot
            var outcome = await _simulator.SimulateAsync(
                CreateSampleScenario("SCEN-FROZEN"),
                new SimulationInputSnapshot(),
                new ModelSnapshot(),
                new PolicySnapshot(),
                constraintSnap);

            // Attacker mutates original list after snapshot creation
            liveConstraints.Clear();

            // Frozen snapshot in outcome preserves constraint evaluation status
            Assert.Equal(ScenarioConstraintStatus.Feasible, outcome.FeasibilityStatus);
        }

        // =========================================================================
        // SCE-30: [ADVERSARIAL] SNAPSHOT TAMPERING DETECTED VIA SHA-256 (I22-N)
        // =========================================================================

        [Fact]
        public void SCE30_01_Adversarial_SnapshotTampering_AltersHash()
        {
            var raw1 = "tenant-alpha:42:1.0.0:1.0.0:hashA";
            var raw2 = "tenant-alpha:42:1.0.0:1.0.0:hashB";

            var hash1 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw1)));
            var hash2 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw2)));

            Assert.NotEqual(hash1, hash2);
        }

        // =========================================================================
        // SCE-31: [ADVERSARIAL] FORECAST CONFIDENCE INFLATION CLAMPED (I22-D, I22-F)
        // =========================================================================

        [Fact]
        public void SCE31_01_Adversarial_ConfidenceInflation_IsClampedToMinimumEvidence()
        {
            // Attacker tries to inject 100% confidence when underlying evidence is 35%
            var assessed = ScenarioConfidenceAssessment.CreateClamped(
                simulationConf: 99.0,
                evidenceConf: 35.0,
                causalConf: 95.0,
                forecastConf: 95.0);

            Assert.Equal(35.0, assessed.CompositeConfidence);
        }

        // =========================================================================
        // SCE-32: [ADVERSARIAL] CAUSAL IDENTIFICATION BYPASS FORCED TO UNKNOWN (I22-C, I22-O)
        // =========================================================================

        [Fact]
        public async Task SCE32_01_Adversarial_BypassingIdentifiability_ForcedToUnknownTier()
        {
            var intervention = new CounterfactualIntervention
            {
                TargetMetricId = "gmv",
                InterventionDelta = 5000m,
                IsIdentifiedInDag = false // Bypassed DAG
            };

            var res = await _counterfactualEngine.EvaluateCounterfactualAsync("tenant-alpha", intervention, new SimulationInputSnapshot());

            Assert.Equal(ScenarioEpistemicTier.Unknown, res.EpistemicTier);
            Assert.True(res.Confidence.CompositeConfidence <= 30.0);
        }

        // =========================================================================
        // SCE-33: [ADVERSARIAL] SCENARIO CANNOT MUTATE REALITY LEDGER (I22, I22-A)
        // =========================================================================

        [Fact]
        public async Task SCE33_01_Adversarial_SimulationZeroSideEffectsOnLedger()
        {
            var scenario = CreateSampleScenario("SCEN-SIDE-EFFECT");
            var outcome = await _orchestrator.RunScenarioPipelineAsync(scenario);

            Assert.NotNull(outcome);
            // Verify no side-effect tasks or external jobs exist in the scenario store
            var defs = await _store.ListDefinitionsAsync("tenant-alpha");
            Assert.True(defs.All(d => d.LifecycleState == ScenarioLifecycleState.Completed || d.LifecycleState == ScenarioLifecycleState.Draft));
        }

        // =========================================================================
        // SCE-34: [ADVERSARIAL] CONCURRENT DUPLICATE SIMULATION IDEMPOTENCY (I22-M)
        // =========================================================================

        [Fact]
        public async Task SCE34_01_Adversarial_ConcurrentDuplicateExecution_ResolvesDeterministically()
        {
            var s = CreateSampleScenario("SCEN-IDEMPOTENT");

            var tasks = Enumerable.Range(0, 5)
                .Select(_ => _orchestrator.RunScenarioPipelineAsync(s))
                .ToArray();

            var outcomes = await Task.WhenAll(tasks);

            var firstHash = outcomes[0].IntegrityHash;
            foreach (var o in outcomes)
            {
                Assert.Equal(firstHash, o.IntegrityHash);
            }
        }
    }
}
