using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Organizational.Watchtower
{
    /// <summary>
    /// Constitutional Invariant I30: Continuous Observation Sovereignty
    /// OBSERVATION ≠ SIGNAL ≠ ATTENTION ≠ INCIDENT ≠ EMERGENCY ≠ WORK ≠ MISSION ≠ AUTHORITY ≠ EXECUTION
    /// </summary>
    public static class ContinuousObservationSovereignty
    {
        public const string InvariantName = "I30";
        public const string InvariantStatement =
            "OBSERVATION ≠ SIGNAL ≠ ATTENTION ≠ INCIDENT ≠ EMERGENCY ≠ WORK ≠ MISSION ≠ AUTHORITY ≠ EXECUTION";

        public const string I30_A_SignalNotAction =
            "I30-A: A detected signal cannot directly trigger consequential execution or dispatch workers. All actions require governed work proposals and firewall authorization.";

        public const string I30_B_AlertNotEmergency =
            "I30-B: Severity or high attention scores cannot manufacture emergency authority, bypass PRG-1 governance, or override policy constraints.";

        public const string I30_C_CorrelationNotCausation =
            "I30-C: Multiple correlated telemetry events indicate concurrent conditions, never unverified causal explanation.";

        public const string I30_D_PersistenceNotTruth =
            "I30-D: A condition recurring over time indicates stability of observation, never converting an unverified claim into empirical fact.";

        public const string I30_E_DeduplicationSovereignty =
            "I30-E: Duplicate instances of the same event cannot inflate significance or trigger multiple downstream work proposals.";

        public const string I30_F_StormContainment =
            "I30-F: Telemetry and event storms must degrade into fewer aggregated signals with updated confidence/severity, never flooding the system or queuing thousands of WorkProposals.";

        public const string I30_G_AttentionNotPriority =
            "I30-G: Attention scoring (cognitive salience) and organizational WorkPriority (governed strategic ranking) remain strictly separated concepts.";

        public const string I30_H_WatchtowerNotScheduler =
            "I30-H: Continuous Business Watchtower consumes events and ticks from the existing EnterpriseSchedulerEngine; it cannot create an independent scheduler or background loop.";

        public const string I30_I_WatchtowerNotIncidentCommander =
            "I30-I: Continuous Business Watchtower observes and escalates; it cannot assume incident command, lock out human governance, or execute emergency mitigation.";

        public const string I30_J_EvidenceBoundedSignals =
            "I30-J: Every actionable signal must retain cryptographic evidence references and epistemic classifications.";

        public const string I30_K_UnknownPreservation =
            "I30-K: When evidence is missing, ambiguous, or below confidence thresholds, the condition is classified as UNKNOWN, never synthesized as an inferred emergency.";

        public const string I30_L_TenantIsolation =
            "I30-L: Event ingestion, correlation, persistence tracking, and signal stores are strictly partitioned by TenantId with zero cross-tenant contamination.";

        public const string I30_M_HistoricalSignalImmutability =
            "I30-M: Later telemetry updates Current Interpretation, never rewriting historical observations, event fingerprints, or audit digests.";

        public const string I30_N_EscalationNotAuthority =
            "I30-N: Escalating a signal to business leaders (CEO, COO, CRO) informs humans; it never grants autonomous execution authority.";

        public const string I30_O_NoSelfEscalation =
            "I30-O: Continuous Business Watchtower cannot escalate its own authority, spend budget, risk ceiling, or execution privileges.";
    }

    public enum ConditionTrajectory
    {
        Unknown,
        TransientSpike,
        Drift,
        PersistentDeviation,
        AcceleratingDeterioration,
        Recovering
    }

    public enum SignalAttentionLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum EventRelationType
    {
        Duplicate,
        Correlated,
        Independent
    }

    public enum SignalStatus
    {
        Active,
        Damped,
        Escalated,
        Resolved,
        Archived
    }

    public class BusinessEvent
    {
        public string EventId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string SourceRecordId { get; set; } = string.Empty;
        public string SourceSystem { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public DateTime ObservedAt { get; set; } = DateTime.UtcNow;
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        public string EvidenceHash { get; set; } = string.Empty;
        public string EventFingerprint { get; set; } = string.Empty;
        public string CorrelationKey { get; set; } = string.Empty;
        public EpistemicStatus EpistemicKind { get; set; } = EpistemicStatus.ObservedFact;
        public FreshnessStatus Freshness { get; set; } = FreshnessStatus.Fresh;
        public string ProvenanceHash { get; set; } = string.Empty;

        public void ComputeFingerprint()
        {
            var raw = $"{SourceSystem}:{EventType}:{EntityId}:{SourceRecordId}";
            using var sha = SHA256.Create();
            EventFingerprint = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));

            var prov = $"{TenantId}:{EventFingerprint}:{ObservedAt:O}:{EvidenceHash}";
            ProvenanceHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(prov)));
        }
    }

    public class PersistentCondition
    {
        public string ConditionId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string CorrelationKey { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string MetricName { get; set; } = string.Empty;
        public double BaselineValue { get; set; } = 0.0;
        public double ObservedValue { get; set; } = 0.0;
        public double Variance => BaselineValue != 0.0 ? (ObservedValue - BaselineValue) / BaselineValue : 0.0;
        public ConditionTrajectory Trajectory { get; set; } = ConditionTrajectory.Unknown;
        public int ObservationCount { get; set; } = 1;
        public DateTime FirstObservedUtc { get; set; } = DateTime.UtcNow;
        public DateTime LastObservedUtc { get; set; } = DateTime.UtcNow;
        public double Velocity { get; set; } = 0.0;     // Rate of change
        public double Acceleration { get; set; } = 0.0; // Change in velocity
        public bool IsPersistent { get; set; } = false;
        public string ConditionHash { get; set; } = string.Empty;

        public void ComputeConditionHash()
        {
            var raw = $"{TenantId}:{CorrelationKey}:{EntityId}:{MetricName}:{Trajectory}:{ObservationCount}:{BaselineValue:F4}:{ObservedValue:F4}";
            using var sha = SHA256.Create();
            ConditionHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }

    public class AttentionScoreBreakdown
    {
        public double ImpactScore { get; set; } = 0.0;
        public double UrgencyScore { get; set; } = 0.0;
        public double PersistenceScore { get; set; } = 0.0;
        public double ExposureScore { get; set; } = 0.0;
        public double ConfidenceScore { get; set; } = 1.0;
        public double CompositeAttentionScore { get; set; } = 0.0;
        public SignalAttentionLevel ResolvedAttentionLevel { get; set; } = SignalAttentionLevel.Low;

        public void CalculateComposite()
        {
            CompositeAttentionScore = Math.Round(
                (ImpactScore * 0.30) +
                (UrgencyScore * 0.25) +
                (PersistenceScore * 0.20) +
                (ExposureScore * 0.15) +
                (ConfidenceScore * 0.10), 4);

            ResolvedAttentionLevel = CompositeAttentionScore switch
            {
                >= 0.80 => SignalAttentionLevel.Critical,
                >= 0.60 => SignalAttentionLevel.High,
                >= 0.35 => SignalAttentionLevel.Medium,
                _ => SignalAttentionLevel.Low
            };
        }
    }

    public class WhyExplanationTrace
    {
        public string SourceSystem { get; set; } = string.Empty;
        public string EventFingerprint { get; set; } = string.Empty;
        public string EvidenceHash { get; set; } = string.Empty;
        public string CorrelationKey { get; set; } = string.Empty;
        public List<string> CorrelatedEventIds { get; set; } = new();
        public ConditionTrajectory Trajectory { get; set; } = ConditionTrajectory.Unknown;
        public AttentionScoreBreakdown AttentionBreakdown { get; set; } = new();
        public string PolicyRuleTriggered { get; set; } = string.Empty;
        public string ExplanationText { get; set; } = string.Empty;
    }

    public class WatchtowerSignal
    {
        public string SignalId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string ConditionId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public SignalAttentionLevel AttentionLevel { get; set; } = SignalAttentionLevel.Low;
        public SignalStatus Status { get; set; } = SignalStatus.Active;
        public AttentionScoreBreakdown Breakdown { get; set; } = new();
        public WhyExplanationTrace WhyTrace { get; set; } = new();
        public DateTime EmittedUtc { get; set; } = DateTime.UtcNow;
        public List<string> SourceEvidenceRefs { get; set; } = new();
        public string? WorkProposalId { get; set; }
        public string SignalHash { get; set; } = string.Empty;

        public void ComputeSignalHash()
        {
            var raw = $"{TenantId}:{ConditionId}:{Title}:{AttentionLevel}:{Status}:{Breakdown.CompositeAttentionScore}:{EmittedUtc:O}";
            using var sha = SHA256.Create();
            SignalHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }

    public class WatchtowerAttentionPolicy
    {
        public int MaxSignalsPerTenant { get; set; } = 50;
        public int CorrelationWindowMinutes { get; set; } = 5;
        public int DampingCooldownMinutes { get; set; } = 15;
        public int PersistenceObservationThreshold { get; set; } = 3;
        public int RateLimitEventsPerMinutePerSource { get; set; } = 120;
        public int MaxWorkProposalsPerCycle { get; set; } = 10;

        public static WatchtowerAttentionPolicy ResolveEffective(
            WatchtowerAttentionPolicy systemPolicy,
            WatchtowerAttentionPolicy? tenantOverride)
        {
            if (tenantOverride == null) return systemPolicy;
            return new WatchtowerAttentionPolicy
            {
                MaxSignalsPerTenant = Math.Min(systemPolicy.MaxSignalsPerTenant, tenantOverride.MaxSignalsPerTenant),
                CorrelationWindowMinutes = tenantOverride.CorrelationWindowMinutes > 0 ? tenantOverride.CorrelationWindowMinutes : systemPolicy.CorrelationWindowMinutes,
                DampingCooldownMinutes = Math.Max(systemPolicy.DampingCooldownMinutes, tenantOverride.DampingCooldownMinutes),
                PersistenceObservationThreshold = Math.Max(systemPolicy.PersistenceObservationThreshold, tenantOverride.PersistenceObservationThreshold),
                RateLimitEventsPerMinutePerSource = Math.Min(systemPolicy.RateLimitEventsPerMinutePerSource, tenantOverride.RateLimitEventsPerMinutePerSource),
                MaxWorkProposalsPerCycle = Math.Min(systemPolicy.MaxWorkProposalsPerCycle, tenantOverride.MaxWorkProposalsPerCycle)
            };
        }
    }
}
