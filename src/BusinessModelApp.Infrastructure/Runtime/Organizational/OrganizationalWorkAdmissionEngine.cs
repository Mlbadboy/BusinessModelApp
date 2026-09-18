using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational
{
    public class OrganizationalWorkAdmissionEngine : IOrganizationalWorkAdmissionEngine
    {
        private readonly IOrganizationalWorkRepository _repository;
        private readonly WorkPriorityPolicy _priorityPolicy;

        private static readonly string[] AdversarialInjectionMarkers = new[]
        {
            "ignore previous instructions",
            "bypass firewall",
            "grant admin permit",
            "self-authorize",
            "elevation of privilege",
            "disable governance",
            "rm -rf",
            "drop table"
        };

        public OrganizationalWorkAdmissionEngine(
            IOrganizationalWorkRepository repository,
            WorkPriorityPolicy? priorityPolicy = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _priorityPolicy = priorityPolicy ?? new WorkPriorityPolicy();
        }

        public async Task<(bool Admitted, string? RejectionReason, WorkItem? Item)> EvaluateAndAdmitAsync(
            WorkProposal proposal,
            CancellationToken ct = default)
        {
            if (proposal == null)
                return (false, "WorkProposal cannot be null.", null);

            // 1. Tenant Validation
            if (string.IsNullOrWhiteSpace(proposal.TenantId))
            {
                proposal.AdmissionStatus = ProposalAdmissionStatus.Rejected;
                proposal.RejectionReason = "TenantId is required.";
                return (false, proposal.RejectionReason, null);
            }

            // 2. Responsibility Validation
            if (string.IsNullOrWhiteSpace(proposal.ResponsibilityId))
            {
                proposal.AdmissionStatus = ProposalAdmissionStatus.Rejected;
                proposal.RejectionReason = "ResponsibilityId is required.";
                return (false, proposal.RejectionReason, null);
            }

            var responsibility = await _repository.GetResponsibilityAsync(proposal.TenantId, proposal.ResponsibilityId, ct);
            if (responsibility == null)
            {
                proposal.AdmissionStatus = ProposalAdmissionStatus.Rejected;
                proposal.RejectionReason = $"Responsibility '{proposal.ResponsibilityId}' does not exist for tenant '{proposal.TenantId}'.";
                return (false, proposal.RejectionReason, null);
            }

            if (!responsibility.IsActive)
            {
                proposal.AdmissionStatus = ProposalAdmissionStatus.Rejected;
                proposal.RejectionReason = $"Responsibility '{proposal.ResponsibilityId}' is inactive.";
                return (false, proposal.RejectionReason, null);
            }

            // 3. Evidence Validation (R1+ requires evidence refs)
            if (proposal.RiskTier > WorkRiskTier.R0_Informational && (proposal.EvidenceRefs == null || proposal.EvidenceRefs.Count == 0))
            {
                proposal.AdmissionStatus = ProposalAdmissionStatus.Rejected;
                proposal.RejectionReason = "Work proposals with risk tier above R0 require valid grounding evidence references.";
                return (false, proposal.RejectionReason, null);
            }

            // 4. Scope & Adversarial Poisoning Validation
            if (string.IsNullOrWhiteSpace(proposal.Title) || string.IsNullOrWhiteSpace(proposal.Objective?.Statement))
            {
                proposal.AdmissionStatus = ProposalAdmissionStatus.Rejected;
                proposal.RejectionReason = "WorkProposal title and objective statement cannot be empty.";
                return (false, proposal.RejectionReason, null);
            }

            var textToCheck = $"{proposal.Title} {proposal.Objective?.Statement}".ToLowerInvariant();
            if (AdversarialInjectionMarkers.Any(marker => textToCheck.Contains(marker)))
            {
                proposal.AdmissionStatus = ProposalAdmissionStatus.Rejected;
                proposal.RejectionReason = "Adversarial prompt-injection or self-elevation marker detected in work proposal.";
                return (false, proposal.RejectionReason, null);
            }

            // 5. Duplicate Detection
            var existingItems = await _repository.ListWorkItemsAsync(proposal.TenantId, null, ct);
            var duplicate = existingItems.FirstOrDefault(w =>
                w.ResponsibilityId == proposal.ResponsibilityId &&
                string.Equals(w.Title, proposal.Title, StringComparison.OrdinalIgnoreCase) &&
                w.State != WorkState.Closed &&
                w.State != WorkState.Cancelled);

            if (duplicate != null)
            {
                proposal.AdmissionStatus = ProposalAdmissionStatus.Deduplicated;
                proposal.RejectionReason = $"Duplicate active work item exists: '{duplicate.WorkId}'.";
                return (false, proposal.RejectionReason, null);
            }

            // 6. Constraint & Sovereignty Validation
            if (proposal.RiskTier >= WorkRiskTier.R4_CriticalSovereignty && !proposal.SourceType.Equals("CEO", StringComparison.OrdinalIgnoreCase))
            {
                proposal.AdmissionStatus = ProposalAdmissionStatus.Rejected;
                proposal.RejectionReason = "R4 Critical Sovereignty work can only be proposed directly by CEO role.";
                return (false, proposal.RejectionReason, null);
            }

            // 7. Deterministic Priority Calculation
            double strategicImportance = 0.5;
            if (proposal.Priority == WorkPriority.Critical) strategicImportance = 0.9;
            else if (proposal.Priority == WorkPriority.High) strategicImportance = 0.7;

            double priorityScore = _priorityPolicy.CalculateScore(
                proposal.Priority,
                proposal.Urgency,
                proposal.RiskTier,
                strategicImportance);

            // 8. Work Admission
            var workId = $"WRK-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            var workItem = new WorkItem
            {
                WorkId = workId,
                TenantId = proposal.TenantId,
                ResponsibilityId = proposal.ResponsibilityId,
                ProposalId = proposal.ProposalId,
                Title = proposal.Title,
                Objective = proposal.Objective ?? new WorkObjective(),
                State = WorkState.Detected,
                Priority = proposal.Priority,
                Urgency = proposal.Urgency,
                RiskTier = proposal.RiskTier,
                PriorityScore = priorityScore,
                CreatedUtc = DateTime.UtcNow,
                Deadline = proposal.SuggestedDeadline
            };

            if (!string.IsNullOrWhiteSpace(proposal.SuggestedAssignee))
            {
                workItem.Assignment = new WorkAssignment
                {
                    AssignmentId = $"ASN-{Guid.NewGuid().ToString("N")[..8]}",
                    WorkId = workId,
                    AssigneeType = AssigneeType.AgentFleet,
                    AssigneeId = proposal.SuggestedAssignee,
                    Role = "LeadAnalyst"
                };
            }

            workItem.ComputeProvenanceHash();

            // Save admitted WorkItem and update proposal status
            proposal.AdmissionStatus = ProposalAdmissionStatus.Admitted;
            await _repository.SaveProposalAsync(proposal, ct);
            await _repository.SaveWorkItemAsync(workItem, ct);

            // Record initial lineage
            var lineage = new WorkMissionLineageRecord
            {
                LineageId = $"LIN-{Guid.NewGuid().ToString("N")[..8]}",
                ResponsibilityId = proposal.ResponsibilityId,
                WorkId = workId,
                TimestampUtc = DateTime.UtcNow
            };
            lineage.ComputeChainHash();
            await _repository.SaveLineageRecordAsync(proposal.TenantId, lineage, ct);

            return (true, null, workItem);
        }
    }
}
