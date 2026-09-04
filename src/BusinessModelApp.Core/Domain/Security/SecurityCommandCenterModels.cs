using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Core.Domain.Common;

namespace BusinessModelApp.Core.Domain.Security
{
    public enum VulnerabilitySeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public enum VulnerabilityCategory
    {
        BOLA_IDOR = 1,
        PromptInjection = 2,
        TenantLeakage = 3,
        StateMachineBypass = 4,
        ImmutabilityViolation = 5,
        ReplayAttack = 6,
        UnboundedCapability = 7,
        InsecureDirectObjectReference = 8
    }

    public enum VulnerabilityStatus
    {
        Discovered = 1,
        Reproduced = 2,
        Contained = 3,
        Remediating = 4,
        Remediated = 5,
        FalsePositive = 6
    }

    public enum TargetEnvironment
    {
        Sandbox = 1,
        IsolatedTest = 2
    }

    public enum TargetType
    {
        InternalEndpoint = 1,
        DisposableSandbox = 2,
        SyntheticIdentity = 3,
        TestDatabase = 4,
        AgentRuntime = 5
    }

    public enum RedTeamCampaignStrategy
    {
        BOLA_IDOR_Probe = 1,
        PromptInjection_Fuzz = 2,
        StateBypass_Audit = 3,
        TenantIsolation_Scan = 4,
        Immutability_Attack = 5
    }

    public enum RedTeamCampaignStatus
    {
        Configured = 1,
        Running = 2,
        Completed = 3,
        AbortedByKillSwitch = 4
    }

    public enum RemediationPatchType
    {
        SandboxAgentSuspension = 1,
        PolicyRuleTightening = 2,
        SanitizationPattern = 3,
        GateAssertion = 4,
        WafRule = 5
    }

    /// <summary>
    /// Governed registration for authorized targets.
    /// Invariant: Target Allowlist requires explicit registration:
    /// Target -> TargetType -> Environment -> Tenant -> SandboxId -> AllowedCapability.
    /// Any target not in the allowlist is DENIED fail-closed.
    /// </summary>
    public class SecurityTargetRegistration : Entity
    {
        public Guid WorkspaceId { get; set; }
        public string TargetName { get; set; } = string.Empty;
        public TargetType Type { get; set; } = TargetType.DisposableSandbox;
        public TargetEnvironment Environment { get; set; } = TargetEnvironment.Sandbox;
        public Guid SandboxId { get; set; }
        public string AllowedCapabilitiesJson { get; set; } = "[\"ReadSandboxData\",\"FuzzPolicyEngine\"]";
        public bool IsActive { get; set; } = true;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Scoped penetration campaign executed by the autonomous Red Team.
    /// Invariant: Strictly bounded to pre-approved disposable sandboxes and synthetic identities.
    /// </summary>
    public class RedTeamCampaign : Entity
    {
        public Guid WorkspaceId { get; set; }
        public Guid TargetSandboxId { get; set; }
        public string CampaignName { get; set; } = string.Empty;
        public RedTeamCampaignStrategy Strategy { get; set; } = RedTeamCampaignStrategy.BOLA_IDOR_Probe;
        public RedTeamCampaignStatus Status { get; set; } = RedTeamCampaignStatus.Configured;
        public int FindingsCount { get; set; } = 0;
        public string ExecutionLogJson { get; set; } = "[]";
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Vulnerability finding produced by the Red Team.
    /// Invariant: Full Finding Evidence Chain from target input to deterministic PoC reproduction hash.
    /// Invariant: PoCs are non-destructive and prove boundary violation without persistence or data destruction.
    /// </summary>
    public class VulnerabilityFinding : Entity
    {
        public Guid WorkspaceId { get; set; }
        public Guid CampaignId { get; set; }
        public Guid TargetId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public VulnerabilityCategory Category { get; set; } = VulnerabilityCategory.BOLA_IDOR;
        public VulnerabilitySeverity Severity { get; set; } = VulnerabilitySeverity.Medium;
        public VulnerabilityStatus Status { get; set; } = VulnerabilityStatus.Discovered;

        // Finding Evidence Chain
        public string InputPayload { get; set; } = string.Empty;
        public string ObservedResult { get; set; } = string.Empty;
        public string ExpectedResult { get; set; } = string.Empty;
        public string ReproductionPoC { get; set; } = string.Empty;
        public string ReproductionHash { get; set; } = string.Empty;

        public bool IsContainedInSandbox { get; set; } = true;
        public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
        public DateTime? RemediatedAt { get; set; }
        public DateTime? RetestedAt { get; set; }

        public static string ComputeReproductionHash(string target, string test, string payload, string observed)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes($"{target}:{test}:{payload}:{observed}"));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }

    /// <summary>
    /// Blue Team candidate fix or containment control.
    /// Invariant: Bounded automated containment allowed ONLY for pre-approved disposable test sandboxes.
    /// Invariant: Production capability changes require human/governance authorization.
    /// Invariant: Candidate fix must prove PoC=PASS without breaking 239 baseline regression tests.
    /// </summary>
    public class BlueTeamRemediation : Entity
    {
        public Guid WorkspaceId { get; set; }
        public Guid FindingId { get; set; }
        public string CandidateFixSummary { get; set; } = string.Empty;
        public RemediationPatchType PatchType { get; set; } = RemediationPatchType.PolicyRuleTightening;
        
        // Scope & Governance
        public bool IsPreApprovedReversibleSandboxAction { get; set; } = true;
        public bool RequiresGovernanceApproval { get; set; } = false;
        public bool IsApproved { get; set; } = false;
        public string ApprovedBy { get; set; } = string.Empty;

        // Regression Verification
        public bool PoCPassed { get; set; } = false;
        public bool RegressionTested { get; set; } = false;
        public int PassedTestCount { get; set; } = 0;
        public DateTime? AppliedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Real-time Security Posture Score (0-100) across six fundamental dimensions.
    /// </summary>
    public class SecurityPostureScore
    {
        public Guid WorkspaceId { get; set; }
        public double OverallScore { get; set; } = 95.0; // 0.0 to 100.0
        public string Rating { get; set; } = "Excellent"; // Excellent, Good, Degraded, Critical

        public double IdentityAndAccessScore { get; set; } = 100.0;
        public double EpistemicIntegrityScore { get; set; } = 95.0;
        public double TenantIsolationScore { get; set; } = 100.0;
        public double PromptInjectionImmunityScore { get; set; } = 95.0;
        public double CryptographicLineageScore { get; set; } = 100.0;
        public double AuditImmutabilityScore { get; set; } = 100.0;

        public int OpenCriticalFindings { get; set; } = 0;
        public int OpenHighFindings { get; set; } = 0;
        public int OpenMediumFindings { get; set; } = 0;
        public int OpenLowFindings { get; set; } = 0;
        public int ContainedFindings { get; set; } = 0;

        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Status of the external, decoupled hardware/software emergency Kill Switch.
    /// Invariant: Halts all security campaigns in <= 100ms under concurrency.
    /// </summary>
    public class KillSwitchStatus
    {
        public bool IsActive { get; set; } = false;
        public DateTime? TriggeredAt { get; set; }
        public string InitiatedBy { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public long ExecutionHaltDurationMs { get; set; } = 0;
    }
}
