using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Organizational;

namespace BusinessModelApp.Core.Domain.Runtime.Missions
{
    /// <summary>
    /// Constitutional Invariant I27: Mission Coordination Sovereignty
    /// MISSION COORDINATION ≠ MISSION EXECUTION ≠ EXECUTION AUTHORITY ≠ WORKER AUTHORITY ≠ TRUTH ≠ GOVERNANCE ≠ OUTCOME
    /// </summary>
    public static class MissionCoordinationSovereignty
    {
        public const string InvariantName = "I27";
        public const string InvariantStatement =
            "MISSION COORDINATION ≠ MISSION EXECUTION ≠ EXECUTION AUTHORITY ≠ WORKER AUTHORITY ≠ TRUTH ≠ GOVERNANCE ≠ OUTCOME";

        public const string I27_A_ZeroPeerToPeerSpawning =
            "I27-A: Agents and workers cannot directly instantiate, fork, or delegate child agents. All work requests route through Mission Orchestrator and Governed DAG Compiler.";

        public const string I27_B_BoundedDepthAndFanout =
            "I27-B: Bounded mission graph depth (<= 5), max branch fanout (<= 10), and recursive expansion (<= 3).";

        public const string I27_C_CanonicalLockOrdering =
            "I27-C: Multi-resource acquisitions strictly follow deterministic canonical ordering (Namespace -> Type -> Tenant -> ResourceId) with lease TTL and fencing tokens to guarantee zero deadlock.";

        public const string I27_D_CoordinatedDrainAndCancellation =
            "I27-D: Work cancellation propagates a coordinated drain/cancel signal to Mission Runtime; safe in-flight attempts checkpoint, unstarted nodes halt, and in-flight external attempts preserve UnknownEffect.";

        public const string I27_E_TelemetryMetrologyBoundary =
            "I27-E: Telemetry feeds empirical metrology records before influencing routing interpretation. Telemetry ≠ Truth ≠ Causal Attribution ≠ Reputation.";

        public const string I27_F_ConfigurableConcurrencyCeilings =
            "I27-F: Concurrency governed by TenantMissionConcurrencyPolicy taking min(system, tenant, mission, capacity, risk).";

        public const string I27_G_NonPreemptiveExecutionFairness =
            "I27-G: Starvation/aging fairness applies to admission and queued nodes; never forcibly preempts an in-flight consequential external attempt.";

        public const string I27_H_FirewallIsolationSovereignty =
            "I27-H: Coordination Fabric organizes and balances missions; it never grants execution permits. All consequential node attempts pass through ExecutionAuthorizationCheckpoint and Batch 6 Firewall.";

        public const string I27_I_NoSecondRuntime =
            "I27-I: Mission Orchestrator cannot create an independent mission execution loop, scheduler, retry engine, worker dispatcher, or execution state machine.";

        public const string I27_J_CompilerSovereignty =
            "I27-J: MissionGraphProposal cannot become executable merely because the Orchestrator admits it. Only IDagCompiler produces an executable MissionGraph.";

        public const string I27_K_AdmissionNotAuthorization =
            "I27-K: MissionAdmissionTicket must never be interpreted as ExecutionPermit, CapabilityLease, or WorkerAuthorization.";

        public const string I27_L_CoordinationCannotManufactureAuthority =
            "I27-L: No coordination component may create, sign, modify, delegate, or issue ExecutionPermit, increase budget, or bypass PRG-1 / Batch 6.";

        public const string I27_M_SchedulerSovereignty =
            "I27-M: Mission timing, retries, backoff, heartbeat, leases, and execution attempts remain owned exclusively by the existing Runtime Scheduler and Mission Runtime.";
    }

    public sealed class TenantMissionConcurrencyPolicy
    {
        public int MaxActiveMissions { get; set; } = 5;
        public int MaxActiveNodes { get; set; } = 15;
        public int MaxQueuedMissions { get; set; } = 20;
        public int MaxResourceLocks { get; set; } = 10;
        public TimeSpan MaxMissionLifetime { get; set; } = TimeSpan.FromHours(24);
        public TimeSpan DefaultLockTtl { get; set; } = TimeSpan.FromMinutes(2);
        public long MaxTokenBudget { get; set; } = 1_000_000;
        public decimal MaxCostUsd { get; set; } = 50.00m;

        public static TenantMissionConcurrencyPolicy ResolveEffective(
            TenantMissionConcurrencyPolicy systemPolicy,
            TenantMissionConcurrencyPolicy? tenantOverride)
        {
            if (tenantOverride == null) return systemPolicy;

            return new TenantMissionConcurrencyPolicy
            {
                MaxActiveMissions = Math.Min(systemPolicy.MaxActiveMissions, tenantOverride.MaxActiveMissions),
                MaxActiveNodes = Math.Min(systemPolicy.MaxActiveNodes, tenantOverride.MaxActiveNodes),
                MaxQueuedMissions = Math.Min(systemPolicy.MaxQueuedMissions, tenantOverride.MaxQueuedMissions),
                MaxResourceLocks = Math.Min(systemPolicy.MaxResourceLocks, tenantOverride.MaxResourceLocks),
                MaxMissionLifetime = tenantOverride.MaxMissionLifetime < systemPolicy.MaxMissionLifetime
                    ? tenantOverride.MaxMissionLifetime
                    : systemPolicy.MaxMissionLifetime,
                DefaultLockTtl = tenantOverride.DefaultLockTtl < systemPolicy.DefaultLockTtl
                    ? tenantOverride.DefaultLockTtl
                    : systemPolicy.DefaultLockTtl,
                MaxTokenBudget = Math.Min(systemPolicy.MaxTokenBudget, tenantOverride.MaxTokenBudget),
                MaxCostUsd = Math.Min(systemPolicy.MaxCostUsd, tenantOverride.MaxCostUsd)
            };
        }
    }

    public enum AdmissionDecisionStatus
    {
        Admitted = 1,
        Queued = 2,
        Rejected = 3
    }

    public sealed class MissionAdmissionTicket
    {
        public string TicketId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string MissionId { get; set; } = string.Empty;
        public string WorkId { get; set; } = string.Empty;
        public string MissionGraphHash { get; set; } = string.Empty;
        public WorkRiskTier RiskTier { get; set; } = WorkRiskTier.R1_InternalReversible;
        public AdmissionDecisionStatus Decision { get; set; } = AdmissionDecisionStatus.Admitted;
        public string PolicySnapshotHash { get; set; } = string.Empty;
        public long FencingToken { get; set; } = 1;
        public DateTime IssuedUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresUtc { get; set; } = DateTime.UtcNow.AddMinutes(30);
        public string ProvenanceHash { get; set; } = string.Empty;

        public void ComputeProvenance()
        {
            var raw = $"{TicketId}:{TenantId}:{MissionId}:{WorkId}:{MissionGraphHash}:{RiskTier}:{Decision}:{FencingToken}:{IssuedUtc:O}:{ExpiresUtc:O}";
            using var sha = SHA256.Create();
            ProvenanceHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }

    public static class CanonicalResourceOrdering
    {
        public static string GetCanonicalKey(string resourceNamespace, string resourceType, string tenantId, string resourceId)
        {
            return $"{resourceNamespace.Trim().ToLowerInvariant()}:{resourceType.Trim().ToLowerInvariant()}:{tenantId.Trim().ToLowerInvariant()}:{resourceId.Trim().ToLowerInvariant()}";
        }

        public static List<string> SortCanonicalKeys(IEnumerable<string> keys)
        {
            var list = new List<string>(keys);
            list.Sort(StringComparer.Ordinal);
            return list;
        }
    }

    public sealed class MissionResourceLock
    {
        public string LockId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string ResourceNamespace { get; set; } = "Default";
        public string ResourceType { get; set; } = "Entity";
        public string ResourceId { get; set; } = string.Empty;
        public long LockVersion { get; set; } = 1;
        public string HolderMissionId { get; set; } = string.Empty;
        public string HolderNodeId { get; set; } = string.Empty;
        public DateTime AcquiredUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresUtc { get; set; } = DateTime.UtcNow.AddMinutes(2);
        public long FencingToken { get; set; } = 1;
        public string CanonicalKey => CanonicalResourceOrdering.GetCanonicalKey(ResourceNamespace, ResourceType, TenantId, ResourceId);

        public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresUtc;
    }

    public sealed class CrossMissionDependency
    {
        public string DependencyId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string ConsumerMissionId { get; set; } = string.Empty;
        public string ConsumerNodeId { get; set; } = string.Empty;
        public string PrerequisiteMissionId { get; set; } = string.Empty;
        public string PrerequisiteArtifactType { get; set; } = string.Empty;
        public string PrerequisiteArtifactId { get; set; } = string.Empty;
        public bool IsSatisfied { get; set; }
        public DateTime? SatisfiedAtUtc { get; set; }
    }

    public sealed class MissionDrainSignal
    {
        public string DrainId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string MissionId { get; set; } = string.Empty;
        public string WorkId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string InitiatedByActor { get; set; } = string.Empty;
        public DateTime RequestedUtc { get; set; } = DateTime.UtcNow;
        public TimeSpan GracePeriod { get; set; } = TimeSpan.FromSeconds(10);
    }

    public sealed class MissionCancellationReceipt
    {
        public string ReceiptId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string MissionId { get; set; } = string.Empty;
        public string WorkId { get; set; } = string.Empty;
        public int HaltedNodeCount { get; set; }
        public int CheckpointNodeCount { get; set; }
        public int UnknownEffectCount { get; set; }
        public DateTime CompletedUtc { get; set; } = DateTime.UtcNow;
        public string AuditDigest { get; set; } = string.Empty;

        public void ComputeAuditDigest()
        {
            var raw = $"{ReceiptId}:{TenantId}:{MissionId}:{WorkId}:{HaltedNodeCount}:{CheckpointNodeCount}:{UnknownEffectCount}:{CompletedUtc:O}";
            using var sha = SHA256.Create();
            AuditDigest = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));
        }
    }

    public sealed class MissionTelemetryFeedback
    {
        public string TelemetryId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string MissionId { get; set; } = string.Empty;
        public string WorkId { get; set; } = string.Empty;
        public long ActualTokensBurned { get; set; }
        public decimal ActualCostUsd { get; set; }
        public TimeSpan ActualDuration { get; set; }
        public int VerifiedOutcomeCount { get; set; }
        public double VarianceScore { get; set; }
        public string Summary { get; set; } = string.Empty;
        public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;
    }

    public sealed class OrchestratorActiveState
    {
        public string TenantId { get; set; } = string.Empty;
        public int ActiveMissionCount { get; set; }
        public int ActiveNodeCount { get; set; }
        public int QueuedMissionCount { get; set; }
        public int HeldLockCount { get; set; }
        public List<string> ActiveMissionIds { get; set; } = new();
        public List<string> QueuedMissionIds { get; set; } = new();
    }
}
