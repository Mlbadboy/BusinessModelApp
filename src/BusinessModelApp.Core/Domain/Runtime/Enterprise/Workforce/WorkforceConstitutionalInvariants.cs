using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce
{
    /// <summary>
    /// Phase 4.3: Sovereign AI Workforce & Governed Agent Fabric Constitution (Law I39).
    /// Enforces absolute non-collapse of organizational, execution, and epistemic boundaries.
    /// </summary>
    public static class WorkforceConstitutionalInvariants
    {
        public const string Axiom = "AGENT != ROLE != SKILL != TOOL != RESPONSIBILITY != AUTHORITY != POLICY != MISSION != EXECUTION != OUTCOME";

        public const string LawI39A_IdentityNotAuthority =
            "I39-A: Agent identity cannot create authority. An agent ID is a cognitive descriptor, not an authorization token.";

        public const string LawI39B_RoleNotExecutionPermission =
            "I39-B: Agent role cannot create execution permission. A role designates organizational focus, not direct execution capability.";

        public const string LawI39C_SkillNotAuthority =
            "I39-C: Skill cannot grant authority. A skill provides procedural competence, not execution authorization.";

        public const string LawI39D_ToolNotAuthorization =
            "I39-D: Tool availability cannot equal tool authorization. A tool must resolve through Capability, Policy, Risk, Governance, and Batch 6.";

        public const string LawI39E_WalletNotAuthority =
            "I39-E: Agent wallet cannot equal authority. Budget allocation defines resource limits, not permission to bypass governance.";

        public const string LawI39F_TrustNotTruth =
            "I39-F: Agent trust cannot equal truth. High confidence or reputation scores cannot substitute for empirical evidence.";

        public const string LawI39G_PerformanceNotPolicy =
            "I39-G: Agent performance cannot modify policy. Excellent metrology metrics cannot alter business rules or governance thresholds.";

        public const string LawI39H_ManagerNotGovernance =
            "I39-H: Manager agent cannot become governance authority. Manager agents route and coordinate work but cannot issue execution permits.";

        public const string LawI39I_CeoNotHumanApproval =
            "I39-I: CEO agent cannot replace human approval authority. Executive AI agents remain subject to PRG-1 human governance.";

        public const string LawI39J_DelegationNotSilentTransfer =
            "I39-J: Delegation cannot silently transfer authority. Sub-agent delegation must stay within the parent agent's strict boundaries.";

        public const string LawI39K_HiringNotAutomaticActivation =
            "I39-K: Agent hiring cannot automatically activate an agent. New agents enter EVALUATING/SANDBOXED state until formally certified.";

        public const string LawI39L_PromotionRequiresGovernance =
            "I39-L: Promotion requires governed evaluation. Agents cannot self-promote across lifecycle stages.";

        public const string LawI39M_SkillsCannotSelfCertify =
            "I39-M: Generated skills cannot self-certify. All skills must undergo threat modeling, sandboxed testing, and red-team evaluation.";

        public const string LawI39N_PoliciesCannotSelfPromote =
            "I39-N: Generated policies cannot self-promote. Dynamic policies remain advisory until approved through governance.";

        public const string LawI39O_RuntimesCannotIssuePermits =
            "I39-O: Agent runtimes cannot create ExecutionPermits. Only Batch 6 Execution Firewall can issue cryptographic execution permits.";

        public const string LawI39P_AgentsCannotModifyFirewall =
            "I39-P: Agents cannot modify Batch 6. The Execution Firewall is sovereign and immutable to runtime modification.";

        public const string LawI39Q_AgentsCannotModifyTruth =
            "I39-Q: Agents cannot modify business truth. Commercial truth requires external corroboration (CLAIMED != VERIFIED).";

        public const string LawI39R_AgentsCannotIncreaseOara =
            "I39-R: Agents cannot increase their own OARA allocation. Resource arbitration remains strictly centralized.";

        public const string LawI39S_AgentsCannotCreateSecondRuntime =
            "I39-S: Agents cannot create a second Runtime. All execution flows through the single sovereign Runtime Kernel.";

        public const string LawI39T_TrajectoryIsEvidenceNotTruth =
            "I39-T: Agent trajectory is evidence, not truth. Execution traces document attempts, not verified real-world facts.";

        public const string LawI39U_BoundedSubAgentSpawning =
            "I39-U: Sub-agent spawning depth and count are strictly bounded and tenant-isolated, consuming parent OARA quotas.";

        public const string LawI39V_UnknownEffectReconciliation =
            "I39-V: UnknownEffect reconciliation must complete before state advancement. No blind retries on unconfirmed mutations.";

        public const string LawI39W_ContinuousCyclesDurableAndBounded =
            "I39-W: Continuous cycles must be finite, checkpointed, durable, and crash-recoverable. Uncontrolled loops are strictly forbidden.";

        public const string LawI39X_EvidenceGroundedCommercialClaims =
            "I39-X: Commercial claims and offers must be strictly grounded in empirical evidence. No invented guarantees or pricing.";

        public const string LawI39Y_CryptographicRevenueLineage =
            "I39-Y: Revenue attribution requires end-to-end cryptographic lineage from BusinessObjective to collected funds.";

        public const string LawI39Z_FailClosedWorkforceSandboxing =
            "I39-Z: Fail-closed workforce sandboxing on any governance, budget, or security violation. Quarantined agents lose all tool access.";

        public static readonly IReadOnlyList<string> AllLaws = new List<string>
        {
            LawI39A_IdentityNotAuthority,
            LawI39B_RoleNotExecutionPermission,
            LawI39C_SkillNotAuthority,
            LawI39D_ToolNotAuthorization,
            LawI39E_WalletNotAuthority,
            LawI39F_TrustNotTruth,
            LawI39G_PerformanceNotPolicy,
            LawI39H_ManagerNotGovernance,
            LawI39I_CeoNotHumanApproval,
            LawI39J_DelegationNotSilentTransfer,
            LawI39K_HiringNotAutomaticActivation,
            LawI39L_PromotionRequiresGovernance,
            LawI39M_SkillsCannotSelfCertify,
            LawI39N_PoliciesCannotSelfPromote,
            LawI39O_RuntimesCannotIssuePermits,
            LawI39P_AgentsCannotModifyFirewall,
            LawI39Q_AgentsCannotModifyTruth,
            LawI39R_AgentsCannotIncreaseOara,
            LawI39S_AgentsCannotCreateSecondRuntime,
            LawI39T_TrajectoryIsEvidenceNotTruth,
            LawI39U_BoundedSubAgentSpawning,
            LawI39V_UnknownEffectReconciliation,
            LawI39W_ContinuousCyclesDurableAndBounded,
            LawI39X_EvidenceGroundedCommercialClaims,
            LawI39Y_CryptographicRevenueLineage,
            LawI39Z_FailClosedWorkforceSandboxing
        };
    }
}
