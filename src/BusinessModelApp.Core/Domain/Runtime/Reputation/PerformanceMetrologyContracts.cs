using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessModelApp.Core.Domain.ExternalReality;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Domain.Runtime.Fleet;

namespace BusinessModelApp.Core.Domain.Runtime.Reputation
{
    /// <summary>
    /// Hierarchical business domain context for capability performance scoping.
    /// Invariant I14-A: Performance is never monolithic; it is contextualized by domain, process, and market.
    /// </summary>
    public record StructuredDomainContext
    {
        public string Domain { get; init; } = "General";
        public string Subdomain { get; init; } = "General";
        public string BusinessProcess { get; init; } = "Standard";
        public string MarketSegment { get; init; } = "Global";

        public static StructuredDomainContext Default => new();

        public static StructuredDomainContext Create(string domain, string subdomain, string businessProcess, string marketSegment = "Global") =>
            new()
            {
                Domain = domain.Trim(),
                Subdomain = subdomain.Trim(),
                BusinessProcess = businessProcess.Trim(),
                MarketSegment = marketSegment.Trim()
            };

        public string ToCanonicalKey() =>
            $"{Domain}:{Subdomain}:{BusinessProcess}:{MarketSegment}".ToLowerInvariant();

        public override string ToString() => ToCanonicalKey();
    }

    /// <summary>
    /// Graded causal attribution levels.
    /// Only A3 (Strong) and A4 (Deterministic) materially improve an agent's capability reputation.
    /// A0/A1 receive zero reputation credit; A2 receives heavily dampened credit.
    /// </summary>
    public enum AttributionLevel
    {
        A0_None = 0,
        A1_Correlated = 1,
        A2_Plausible = 2,
        A3_Strong = 3,
        A4_Deterministic = 4
    }

    /// <summary>
    /// Invariant I14-D: Provenance metadata distinguishing worker, capability, prompt, model, and provider layers.
    /// Prevents conflating model/provider degradation with agent cognitive failures.
    /// </summary>
    public record BrainFabricProvenance
    {
        public string ModelId { get; init; } = "default-model";
        public string ProviderId { get; init; } = "default-provider";
        public string OmniRouteId { get; init; } = "primary-route";
        public string PromptVersion { get; init; } = "v1.0";
        public string SchemaVersion { get; init; } = "v1.0";
        public string CapabilityVersion { get; init; } = "v1.0";
        public string PolicyVersion { get; init; } = "v1.0";

        public string ComputeDigest()
        {
            var raw = $"{ModelId}:{ProviderId}:{OmniRouteId}:{PromptVersion}:{SchemaVersion}:{CapabilityVersion}:{PolicyVersion}";
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }

    /// <summary>
    /// Multi-dimensional empirical metric vector for a worker capability profile.
    /// Invariant I14-A: Performance is a vector, never a monolithic scalar.
    /// </summary>
    public record ReputationMetricVector
    {
        /// <summary>
        /// Mean Absolute Error / Discrepancy between predicted metrics and verified actuals [0.0 = perfect calibration, 1.0 = total divergence].
        /// </summary>
        public double CalibrationVariance { get; init; } = 0.0;

        /// <summary>
        /// Verification quality score [0.0 to 1.0] derived from INodeVerificationEngine confidence and assertions.
        /// </summary>
        public double VerificationQualityScore { get; init; } = 1.0;

        /// <summary>
        /// Cost efficiency ratio: Actual tokens/cost divided by estimated/budgeted ceiling. (<= 1.0 is on-budget, > 1.0 is over-budget).
        /// </summary>
        public double CostEfficiencyRatio { get; init; } = 1.0;

        /// <summary>
        /// Latency predictability ratio: Actual duration divided by expected SLA.
        /// </summary>
        public double LatencyPredictabilityRatio { get; init; } = 1.0;

        /// <summary>
        /// Rate of rollbacks, compensations, and UnknownEffects triggered during execution [0.0 to 1.0].
        /// </summary>
        public double RollbackFrequency { get; init; } = 0.0;

        /// <summary>
        /// Security and policy compliance score [0.0 to 1.0]. Violations (fencing tampering, privilege escalation) trigger quarantine.
        /// </summary>
        public double PolicyComplianceScore { get; init; } = 1.0;

        /// <summary>
        /// Whether this capability profile is currently quarantined due to a critical security misconduct.
        /// </summary>
        public bool IsQuarantined { get; init; } = false;

        public string? QuarantineReason { get; init; }
        public DateTimeOffset? QuarantinedAt { get; init; }

        public static ReputationMetricVector Initial => new()
        {
            CalibrationVariance = 0.0,
            VerificationQualityScore = 1.0,
            CostEfficiencyRatio = 1.0,
            LatencyPredictabilityRatio = 1.0,
            RollbackFrequency = 0.0,
            PolicyComplianceScore = 1.0,
            IsQuarantined = false
        };
    }

    /// <summary>
    /// Calibration record comparing expected/predicted outputs against verified reality.
    /// </summary>
    public record OutcomeCalibrationRecord
    {
        public Guid CalibrationId { get; init; } = Guid.NewGuid();
        public ExecutionAttemptId AttemptId { get; init; }
        public MissionNodeId NodeId { get; init; }
        public MissionGraphId GraphId { get; init; }
        public AgentDefinitionId AgentDefinitionId { get; init; }
        public WorkerProcessId WorkerId { get; init; }
        public CapabilityId CapabilityId { get; init; }
        public StructuredDomainContext DomainContext { get; init; } = StructuredDomainContext.Default;
        public MarketRegimeState MarketRegime { get; init; } = MarketRegimeState.Stable;

        public string ExpectedOutputJson { get; init; } = "{}";
        public string ActualVerifiedOutputJson { get; init; } = "{}";

        /// <summary>
        /// Quantified error/discrepancy score between expected and actual [0.0 to 1.0].
        /// </summary>
        public double DiscrepancyScore { get; init; } = 0.0;

        public bool IsWithinTolerance { get; init; } = true;
        public DateTimeOffset RecordedAt { get; init; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Evaluated causal attribution between an agent's output and the verified business outcome.
    /// </summary>
    public record CausalAttributionRecord
    {
        public Guid AttributionId { get; init; } = Guid.NewGuid();
        public ExecutionAttemptId AttemptId { get; init; }
        public AttributionLevel Level { get; init; } = AttributionLevel.A4_Deterministic;
        public double AttributionConfidence { get; init; } = 1.0;
        public string Rationale { get; init; } = string.Empty;
        public IReadOnlyList<string> ConfoundingFactors { get; init; } = Array.Empty<string>();
        public DateTimeOffset EvaluatedAt { get; init; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Weight factor derived from attribution level:
        /// A0 = 0.0, A1 = 0.0, A2 = 0.25, A3 = 0.80, A4 = 1.0.
        /// </summary>
        public double EffectiveReputationWeight => Level switch
        {
            AttributionLevel.A0_None => 0.0,
            AttributionLevel.A1_Correlated => 0.0,
            AttributionLevel.A2_Plausible => 0.25,
            AttributionLevel.A3_Strong => 0.80,
            AttributionLevel.A4_Deterministic => 1.0,
            _ => 0.0
        };
    }

    /// <summary>
    /// Monotonically increasing profile version with SHA-256 parent hash chaining.
    /// </summary>
    public readonly record struct ProfileVersion
    {
        public int Value { get; }

        [JsonConstructor]
        public ProfileVersion(int value)
        {
            if (value < 1)
                throw new ArgumentException("ProfileVersion must be >= 1.", nameof(value));
            Value = value;
        }

        public static ProfileVersion Initial => new(1);
        public ProfileVersion Next() => new(Value + 1);
        public static implicit operator int(ProfileVersion v) => v.Value;
        public override string ToString() => $"v{Value}";
    }

    /// <summary>
    /// Immutable token of reputation evidence emitted by the verification and admission gate.
    /// Invariant I14-B: Agents cannot self-validate or manufacture reputation evidence.
    /// </summary>
    public record ReputationEvidenceToken
    {
        public Guid TokenId { get; init; } = Guid.NewGuid();
        public Guid WorkspaceId { get; init; }
        public ExecutionAttemptId AttemptId { get; init; }
        public MissionGraphId GraphId { get; init; }
        public MissionNodeId NodeId { get; init; }
        public AgentDefinitionId AgentDefinitionId { get; init; }
        public WorkerProcessId WorkerId { get; init; }
        public CapabilityId CapabilityId { get; init; }
        public StructuredDomainContext DomainContext { get; init; } = StructuredDomainContext.Default;
        public MarketRegimeState MarketRegime { get; init; } = MarketRegimeState.Stable;

        public BrainFabricProvenance Provenance { get; init; } = new();
        public CausalAttributionRecord Attribution { get; init; } = new();
        public OutcomeCalibrationRecord Calibration { get; init; } = new();
        public string VerifiedEvidenceHash { get; init; } = string.Empty;
        public Guid? AuditEntryId { get; init; }

        public bool IsSuccessfulExecution { get; init; }
        public bool IsSecurityViolation { get; init; }
        public bool IsUnknownEffectCrash { get; init; }
        public long TokensConsumed { get; init; }
        public decimal CostUsdConsumed { get; init; }
        public TimeSpan Duration { get; init; }

        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
        public string TokenHash { get; init; } = string.Empty;

        public static string ComputeTokenHash(ReputationEvidenceToken token)
        {
            var payload = $"{token.TokenId}:{token.WorkspaceId}:{token.AttemptId}:{token.AgentDefinitionId.Value}:{token.CapabilityId.Name}:{token.VerifiedEvidenceHash}:{token.Attribution.Level}:{token.Calibration.DiscrepancyScore:F4}";
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        }
    }

    /// <summary>
    /// Empirical performance profile scoped to an AgentDefinition, Capability, StructuredDomain, and MarketRegime.
    /// </summary>
    public record CapabilityPerformanceProfile
    {
        public Guid ProfileId { get; init; } = Guid.NewGuid();
        public Guid WorkspaceId { get; init; }
        public AgentDefinitionId AgentDefinitionId { get; init; }
        public CapabilityId CapabilityId { get; init; }
        public StructuredDomainContext DomainContext { get; init; } = StructuredDomainContext.Default;
        public MarketRegimeState MarketRegime { get; init; } = MarketRegimeState.Stable;

        public ProfileVersion Version { get; set; } = ProfileVersion.Initial;
        public string? ParentProfileHash { get; set; }
        public string VersionHash { get; set; } = string.Empty;

        public int TotalAttempts { get; set; }
        public int SuccessfulAttempts { get; set; }
        public int FailedAttempts { get; set; }
        public int VerificationFailureCount { get; set; }
        public int UnknownEffectCount { get; set; }
        public int RollbackCount { get; set; }
        public int PolicyViolationCount { get; set; }

        public ReputationMetricVector Metrics { get; set; } = ReputationMetricVector.Initial;

        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Computes sample-size uncertainty dampening factor using Wilson score/Beta confidence.
        /// High N approaches 1.0; low N (e.g. N=1) produces high uncertainty (~0.35).
        /// </summary>
        public double ComputeConfidenceFactor()
        {
            if (TotalAttempts <= 0) return 0.20;
            double n = TotalAttempts;
            return Math.Clamp(1.0 - (1.0 / (1.0 + Math.Sqrt(n) * 0.65)), 0.25, 1.0);
        }

        public string ComputeHash()
        {
            var raw = $"{WorkspaceId}:{AgentDefinitionId.Value}:{CapabilityId}:{DomainContext.ToCanonicalKey()}:{MarketRegime}:{Version.Value}:{ParentProfileHash}:{TotalAttempts}:{SuccessfulAttempts}:{Metrics.CalibrationVariance:F4}:{Metrics.VerificationQualityScore:F4}:{Metrics.IsQuarantined}";
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        }
    }

    /// <summary>
    /// Aggregated empirical profile across all capabilities for a given agent definition.
    /// </summary>
    public record AgentPerformanceProfile
    {
        public Guid WorkspaceId { get; init; }
        public AgentDefinitionId AgentDefinitionId { get; init; }
        public int TotalAttemptsAcrossAllCapabilities { get; set; }
        public int TotalSuccessfulAttempts { get; set; }
        public double OverallVerificationQuality { get; set; } = 1.0;
        public double OverallCalibrationVariance { get; set; } = 0.0;
        public bool HasAnyQuarantinedCapability { get; set; } = false;
        public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Aggregated empirical profile for a capability version across all agents.
    /// Allows distinguishing whether a capability definition is flawed vs an agent.
    /// </summary>
    public record CapabilityVersionProfile
    {
        public CapabilityId CapabilityId { get; init; }
        public int TotalExecutionsAcrossAllAgents { get; set; }
        public int TotalFailuresAcrossAllAgents { get; set; }
        public double MeanCalibrationVarianceAcrossAgents { get; set; }
        public double MeanVerificationQualityAcrossAgents { get; set; }
        public DateTimeOffset LastUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Candidate evaluation entry for an explainable routing decision.
    /// </summary>
    public record CandidateRoutingEvaluation
    {
        public WorkerProcessId WorkerId { get; init; }
        public AgentDefinitionId AgentDefinitionId { get; init; }
        public ProfileVersion ProfileVersionUsed { get; init; }
        public double UtilityScore { get; init; }
        public double ConfidenceDampening { get; init; }
        public bool IsEligible { get; init; }
        public string? IneligibilityReason { get; init; }
        public bool IsQuarantined { get; init; }
        public bool IsColdStartExploration { get; init; }
        public ReputationMetricVector MetricSnapshot { get; init; } = ReputationMetricVector.Initial;
    }

    /// <summary>
    /// Full explainable routing audit record detailing why candidate A was chosen over others.
    /// </summary>
    public record RoutingDecisionRecord
    {
        public Guid DecisionId { get; init; } = Guid.NewGuid();
        public Guid WorkspaceId { get; init; }
        public MissionGraphId GraphId { get; init; }
        public MissionNodeId NodeId { get; init; }
        public CapabilityId RequiredCapabilityId { get; init; }
        public StructuredDomainContext DomainContext { get; init; } = StructuredDomainContext.Default;
        public MarketRegimeState MarketRegime { get; init; } = MarketRegimeState.Stable;

        public MissionPriority MissionPriority { get; init; } = MissionPriority.P2_Medium;
        public AutonomyTier RequiredAutonomyTier { get; init; } = AutonomyTier.L1_Advise;

        public WorkerProcessId? SelectedWorkerId { get; init; }
        public AgentDefinitionId? SelectedAgentDefinitionId { get; init; }
        public ProfileVersion? SelectedProfileVersion { get; init; }

        public IReadOnlyList<CandidateRoutingEvaluation> CandidateEvaluations { get; init; } = Array.Empty<CandidateRoutingEvaluation>();

        public string SelectionRationale { get; init; } = string.Empty;
        public bool WasExplorationCandidate { get; init; }
        public DateTimeOffset DecidedAt { get; init; } = DateTimeOffset.UtcNow;
    }
}
