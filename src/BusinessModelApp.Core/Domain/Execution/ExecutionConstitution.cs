using System;
using System.Security;

namespace BusinessModelApp.Core.Domain.Execution
{
    /// <summary>
    /// B6-H0: Immutable Execution Constitution & Sovereign Invariants.
    /// Execution is never implied. AI output, agent roles, confidence, memory,
    /// or recommendations have ZERO execution authority on their own.
    /// </summary>
    public static class ExecutionConstitution
    {
        public const string LAW = 
            "No AI model, agent, memory, learning record, connector, tool, mission, recommendation, " +
            "or external event can directly execute a consequential real-world action. " +
            "Every consequential action must pass through the deterministic Execution Firewall and " +
            "receive an explicit execution permit generated from valid identity, tenant, capability, " +
            "authority, policy, budget, risk, approval, precondition, idempotency, kill-switch, and audit state.";

        /// <summary>
        /// Validates that an ExecutionRequest meets all sovereign constitutional invariants.
        /// Throws SecurityException on any missing dimension (Fail-Closed).
        /// </summary>
        public static void AssertConstitutionalValidity(
            Guid workspaceId,
            string agentId,
            string capabilityId,
            string idempotencyKey,
            string auditContext)
        {
            if (workspaceId == Guid.Empty)
                throw new SecurityException("[Execution Constitution] VIOLATION: Execution request missing Tenant/Workspace identity. Fail-Closed.");

            if (string.IsNullOrWhiteSpace(agentId))
                throw new SecurityException("[Execution Constitution] VIOLATION: Execution request missing authenticated Agent identity. Fail-Closed.");

            if (string.IsNullOrWhiteSpace(capabilityId))
                throw new SecurityException("[Execution Constitution] VIOLATION: Execution request missing declared Capability. Fail-Closed.");

            if (string.IsNullOrWhiteSpace(idempotencyKey))
                throw new SecurityException("[Execution Constitution] VIOLATION: Execution request missing Idempotency Key. Deduplication impossible. Fail-Closed.");

            if (string.IsNullOrWhiteSpace(auditContext))
                throw new SecurityException("[Execution Constitution] VIOLATION: Execution request missing Audit Context provenance. Fail-Closed.");
        }
    }
}
