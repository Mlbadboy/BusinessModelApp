using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace BusinessModelApp.Core.Domain.Runtime.Intelligence.Scenario
{
    // =========================================================================
    // THE GOLDEN ARCHITECTURAL RULE & CONSTITUTIONAL BOUNDARIES (INVARIANT I22)
    // =========================================================================
    // THE AI MAY PROPOSE THE QUESTION.
    // THE SCENARIO ENGINE DEFINES THE MATHEMATICS.
    // THE CAUSAL ENGINE DEFINES IDENTIFIABILITY.
    // THE FORECAST ENGINE DEFINES PREDICTIVE INPUTS.
    // THE CONSTRAINT ENGINE DEFINES FEASIBILITY.
    // THE SIMULATOR PRODUCES DISTRIBUTIONS.
    // THE COMPARISON ENGINE EVALUATES TRADE-OFFS.
    // THE LEDGER RECORDS THE SIMULATION.
    // THE DECISION ENGINE DECIDES.
    // THE GOVERNANCE LAYER APPROVES.
    // THE EXECUTION FIREWALL AUTHORIZES.
    //
    // AI OUTPUT != SCENARIO VALIDITY
    // SCENARIO != REALITY
    // SCENARIO != EXPERIMENT
    // SCENARIO != CAUSAL PROOF
    // SCENARIO != FORECAST
    // RECOMMENDED SCENARIO != RECOMMENDED ACTION
    // SCENARIO != DECISION
    // SCENARIO != APPROVAL
    // SCENARIO != EXECUTION AUTHORITY
    // =========================================================================

    public enum ScenarioType
    {
        WhatIfIntervention,
        CounterfactualRetrospective,
        StressTest,
        WorstCaseScenario,
        BestCaseScenario,
        PolicyAlternative
    }

    public enum SimulationMethod
    {
        DeterministicDifferential,
        MonteCarloDistribution,
        CausalDoCalculus,
        SensitivitySweep
    }

    public enum ScenarioConstraintStatus
    {
        Feasible,
        ConstrainedWithMitigation,
        InfeasibleViolation
    }

    public enum ScenarioEpistemicTier
    {
        FactBacked,
        ModelDerived,
        Hypothetical,
        Unknown
    }

    public enum ScenarioLifecycleState
    {
        Draft,
        Validating,
        Ready,
        Simulating,
        Completed,
        Compared,
        ReviewRequired,
        Superseded,
        Expired,
        Rejected
    }

    // =========================================================================
    // SNAPSHOT CONTRACTS FOR DETERMINISTIC REPRODUCIBILITY (I22-M, I22-N)
    // =========================================================================

    public sealed record SimulationInputSnapshot
    {
        public string SnapshotId { get; init; } = Guid.NewGuid().ToString("N");
        public IReadOnlyDictionary<string, decimal> InitialMetricValues { get; init; } = new Dictionary<string, decimal>();
        public IReadOnlyList<string> ActiveInputHashes { get; init; } = Array.Empty<string>();
        public DateTime CapturedAtUtc { get; init; } = DateTime.UtcNow;
    }

    public sealed record ModelSnapshot
    {
        public string ModelId { get; init; } = "DefaultSimulationEngine";
        public string ModelVersion { get; init; } = "1.0.0";
        public IReadOnlyDictionary<string, decimal> Coefficients { get; init; } = new Dictionary<string, decimal>();
        public string FormulaDescription { get; init; } = "DeterministicDifferential + MonteCarlo";
    }

    public sealed record PolicySnapshot
    {
        public string PolicyId { get; init; } = "DefaultScenarioPolicy";
        public string PolicyVersion { get; init; } = "1.0.0";
        public decimal MaxDownsideToleranceINR { get; init; } = 500000m;
        public double MinAcceptableConfidence { get; init; } = 40.0;
        public IReadOnlyDictionary<string, double> Weights { get; init; } = new Dictionary<string, double>();
    }

    public sealed record ConstraintSnapshot
    {
        public string SnapshotId { get; init; } = Guid.NewGuid().ToString("N");
        public IReadOnlyList<ScenarioConstraint> FrozenConstraints { get; init; } = Array.Empty<ScenarioConstraint>();
        public DateTime CapturedAtUtc { get; init; } = DateTime.UtcNow;
    }

    public sealed record SimulationMetadata
    {
        public string AlgorithmVersion { get; init; } = "3.8.4-RELEASE";
        public int RandomSeed { get; init; } = 42;
        public DateTime SimulatedAtUtc { get; init; } = DateTime.UtcNow;
        public string SnapshotHash { get; set; } = string.Empty;
    }

    // =========================================================================
    // CONFIDENCE METROLOGY & CLAMPING (I22-F, I22-O)
    // =========================================================================

    public sealed record ScenarioConfidenceAssessment
    {
        public double SimulationConfidence { get; init; } = 80.0;
        public double EvidenceConfidence { get; init; } = 80.0;
        public double CausalConfidence { get; init; } = 80.0;
        public double ForecastConfidence { get; init; } = 80.0;
        public double CompositeConfidence { get; init; }

        public static ScenarioConfidenceAssessment CreateClamped(
            double simulationConf,
            double evidenceConf,
            double causalConf,
            double forecastConf)
        {
            var clamped = Math.Min(
                Math.Clamp(simulationConf, 0.0, 100.0),
                Math.Min(
                    Math.Clamp(evidenceConf, 0.0, 100.0),
                    Math.Min(
                        Math.Clamp(causalConf, 0.0, 100.0),
                        Math.Clamp(forecastConf, 0.0, 100.0))));

            return new ScenarioConfidenceAssessment
            {
                SimulationConfidence = Math.Round(simulationConf, 2),
                EvidenceConfidence = Math.Round(evidenceConf, 2),
                CausalConfidence = Math.Round(causalConf, 2),
                ForecastConfidence = Math.Round(forecastConf, 2),
                CompositeConfidence = Math.Round(clamped, 2)
            };
        }
    }

    // =========================================================================
    // SCENARIO DEFINITION & INTERVENTION CONTRACTS
    // =========================================================================

    public sealed record ScenarioAssumption
    {
        public string AssumptionId { get; init; } = Guid.NewGuid().ToString("N");
        public string Key { get; init; } = string.Empty;
        public string MetricId { get; init; } = string.Empty;
        public decimal BaselineValue { get; init; }
        public decimal AssumedValue { get; init; }
        public string Justification { get; init; } = string.Empty;
        public ScenarioEpistemicTier EpistemicTier { get; init; } = ScenarioEpistemicTier.Hypothetical;

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(Key) || string.IsNullOrWhiteSpace(MetricId)) return false;
            if (double.IsNaN((double)AssumedValue) || double.IsInfinity((double)AssumedValue)) return false;
            return true;
        }
    }

    public sealed record CounterfactualIntervention
    {
        public string InterventionId { get; init; } = Guid.NewGuid().ToString("N");
        public string TargetMetricId { get; init; } = string.Empty;
        public decimal InterventionDelta { get; init; }
        public IReadOnlyList<string> ConditioningNodes { get; init; } = Array.Empty<string>();
        public string CausalMechanism { get; init; } = string.Empty;
        public bool IsIdentifiedInDag { get; init; } = true;
    }

    public sealed record ScenarioConstraint
    {
        public string ConstraintId { get; init; } = Guid.NewGuid().ToString("N");
        public string Name { get; init; } = string.Empty;
        public string MetricId { get; init; } = string.Empty;
        public decimal? MinValue { get; init; }
        public decimal? MaxValue { get; init; }
        public bool IsHardConstraint { get; init; } = true;
        public string MitigationAlternative { get; init; } = string.Empty;
    }

    public sealed record SimulationDistribution
    {
        public decimal P10 { get; init; }
        public decimal P50 { get; init; }
        public decimal P90 { get; init; }
        public decimal Mean { get; init; }
        public decimal StandardDeviation { get; init; }
        public decimal SensitivityRadius { get; init; }

        public bool ValidateMonotonicBounds() => P10 <= P50 && P50 <= P90;
    }

    public sealed record ScenarioOutcomeMetric
    {
        public string MetricId { get; init; } = string.Empty;
        public decimal BaselineValue { get; init; }
        public decimal SimulatedMedian { get; init; }
        public decimal AbsoluteDelta => SimulatedMedian - BaselineValue;
        public double PercentageDelta => BaselineValue != 0 ? (double)((SimulatedMedian - BaselineValue) / BaselineValue) : 0.0;
        public SimulationDistribution Distribution { get; init; } = new SimulationDistribution();
    }

    public sealed record ScenarioDefinition
    {
        public string ScenarioId { get; init; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; init; } = string.Empty;
        public string? RadarSignalId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public ScenarioType Type { get; init; } = ScenarioType.WhatIfIntervention;
        public SimulationMethod Method { get; init; } = SimulationMethod.MonteCarloDistribution;
        public IReadOnlyList<ScenarioAssumption> Assumptions { get; init; } = Array.Empty<ScenarioAssumption>();
        public IReadOnlyList<CounterfactualIntervention> Interventions { get; init; } = Array.Empty<CounterfactualIntervention>();
        public IReadOnlyList<ScenarioConstraint> Constraints { get; init; } = Array.Empty<ScenarioConstraint>();
        public ScenarioLifecycleState LifecycleState { get; set; } = ScenarioLifecycleState.Draft;
        public int RandomSeed { get; init; } = 42;
        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

        public string ComputeAssumptionHash()
        {
            var raw = string.Join(";", Assumptions.OrderBy(a => a.Key).Select(a => $"{a.Key}:{a.AssumedValue:F4}:{a.EpistemicTier}"));
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }

    // =========================================================================
    // SCENARIO OUTCOME & COMPARISON CONTRACTS (I22-G, I22-H, I22-J)
    // =========================================================================

    public sealed record ScenarioOutcome
    {
        public string ScenarioId { get; init; } = string.Empty;
        public string TenantId { get; init; } = string.Empty;
        public string? RadarSignalId { get; init; }
        public ScenarioLifecycleState LifecycleState { get; set; } = ScenarioLifecycleState.Completed;
        public IReadOnlyDictionary<string, ScenarioOutcomeMetric> MetricOutcomes { get; init; } = new Dictionary<string, ScenarioOutcomeMetric>();
        public decimal NetFinancialImpactINR { get; init; }
        public ScenarioConstraintStatus FeasibilityStatus { get; init; } = ScenarioConstraintStatus.Feasible;
        public IReadOnlyList<string> ViolatedConstraintNames { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> SuggestedMitigations { get; init; } = Array.Empty<string>();
        public ScenarioEpistemicTier EpistemicTier { get; init; } = ScenarioEpistemicTier.ModelDerived;
        public ScenarioConfidenceAssessment Confidence { get; init; } = new ScenarioConfidenceAssessment();
        public SimulationMetadata Metadata { get; init; } = new SimulationMetadata();
        public string IntegrityHash { get; set; } = string.Empty;

        public string ComputeIntegrityHash()
        {
            var raw = $"{ScenarioId}:{TenantId}:{NetFinancialImpactINR:F2}:{FeasibilityStatus}:{EpistemicTier}:{Confidence.CompositeConfidence:F2}:{Metadata.SnapshotHash}";
            using var sha = SHA256.Create();
            IntegrityHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
            return IntegrityHash;
        }
    }

    public sealed record ScenarioComparisonItem
    {
        public string ScenarioId { get; init; } = string.Empty;
        public string ScenarioName { get; init; } = string.Empty;
        public decimal ExpectedReturnINR { get; init; }
        public decimal DownsideP10INR { get; init; }
        public decimal UpsideP90INR { get; init; }
        public double RiskAdjustedScore { get; init; }
        public int ParetoRank { get; init; }
        public ScenarioConstraintStatus FeasibilityStatus { get; init; }
        public bool IsOnParetoFrontier { get; init; }
    }

    /// <summary>
    /// INVARIANT: RecommendedScenario != RecommendedAction != Decision != Approval != Execution.
    /// Scenario Engine identifies optimal modeled trade-offs on the Pareto frontier.
    /// It strictly does NOT emit prescriptive action commands.
    /// </summary>
    public sealed record ScenarioComparisonResult
    {
        public string ComparisonId { get; init; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; init; } = string.Empty;
        public IReadOnlyList<ScenarioComparisonItem> Scenarios { get; init; } = Array.Empty<ScenarioComparisonItem>();
        public string? RecommendedScenarioId { get; init; }
        public string TradeoffRationale { get; init; } = string.Empty;
        public DateTime ComparedAtUtc { get; init; } = DateTime.UtcNow;
        public string IntegrityHash { get; set; } = string.Empty;

        public string ComputeIntegrityHash()
        {
            var raw = $"{ComparisonId}:{TenantId}:{RecommendedScenarioId}:{string.Join(",", Scenarios.Select(s => s.ScenarioId))}";
            using var sha = SHA256.Create();
            IntegrityHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
            return IntegrityHash;
        }
    }

    // =========================================================================
    // BIDIRECTIONAL PROVENANCE RECORD (I22-K)
    // =========================================================================

    public sealed record ScenarioProvenance
    {
        public string ProvenanceId { get; init; } = Guid.NewGuid().ToString("N");
        public string ScenarioId { get; init; } = string.Empty;
        public string TenantId { get; init; } = string.Empty;
        public string? RadarSignalId { get; init; }
        public string? CausalHypothesisId { get; init; }
        public string? ForecastId { get; init; }
        public IReadOnlyList<string> SourceObservationIds { get; init; } = Array.Empty<string>();
        public string AssumptionHash { get; init; } = string.Empty;
        public string SnapshotHash { get; init; } = string.Empty;
        public string OutcomeHash { get; init; } = string.Empty;
        public DateTime ProvenanceSealedAtUtc { get; init; } = DateTime.UtcNow;

        public bool VerifyChainIntegrity()
        {
            return !string.IsNullOrWhiteSpace(ScenarioId) &&
                   !string.IsNullOrWhiteSpace(TenantId) &&
                   !string.IsNullOrWhiteSpace(AssumptionHash) &&
                   !string.IsNullOrWhiteSpace(SnapshotHash) &&
                   !string.IsNullOrWhiteSpace(OutcomeHash);
        }
    }
}
