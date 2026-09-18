using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Memory
{
    public class MemoryWriteGate : IMemoryWriteGate
    {
        private static readonly HashSet<string> DisallowedRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "Agent",
            "Worker",
            "WorkerProcess",
            "LLM",
            "RetrievedDocument",
            "UntrustedModel"
        };

        private static readonly HashSet<string> AuthorizedSourceTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "OutcomeRecord",
            "VerificationRecord",
            "DecisionRecord",
            "HumanApproval",
            "RealityTelemetry",
            "VerifiedFact",
            "TrajectoryMilestone"
        };

        public Task<(bool Permitted, string? Reason)> ValidateWriteAsync(
            string tenantId,
            string callerRole,
            string sourceRecordType,
            string evidenceRef,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return Task.FromResult((false, (string?)"TenantId is required."));
            }

            // Invariant I28-N: No AI model, agent, or worker can directly write authoritative memory
            if (DisallowedRoles.Contains(callerRole))
            {
                return Task.FromResult((false, (string?)$"Caller role '{callerRole}' is prohibited from writing authoritative organizational memory per Invariant I28-N."));
            }

            if (!AuthorizedSourceTypes.Contains(sourceRecordType))
            {
                return Task.FromResult((false, (string?)$"Source record type '{sourceRecordType}' is not an authorized provenance source for organizational memory."));
            }

            if (string.IsNullOrWhiteSpace(evidenceRef))
            {
                return Task.FromResult((false, (string?)"Authoritative organizational memory writes require an evidence reference per Invariant I28-A."));
            }

            return Task.FromResult((true, (string?)null));
        }
    }
}
