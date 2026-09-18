using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational
{
    public class WorkCommitmentMonitor : IWorkCommitmentMonitor
    {
        private readonly IOrganizationalWorkRepository _repository;

        public WorkCommitmentMonitor(IOrganizationalWorkRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<IReadOnlyList<WorkCommitment>> EvaluateCommitmentsAsync(string tenantId, CancellationToken ct = default)
        {
            var workItems = await _repository.ListWorkItemsAsync(tenantId, null, ct);
            var evaluated = new List<WorkCommitment>();
            var now = DateTime.UtcNow;

            foreach (var item in workItems)
            {
                // Skip terminal states
                if (item.State == WorkState.Completed ||
                    item.State == WorkState.Measured ||
                    item.State == WorkState.Closed ||
                    item.State == WorkState.Cancelled)
                {
                    continue;
                }

                bool itemChanged = false;

                // 1. Evaluate Commitments
                foreach (var commitment in item.Commitments)
                {
                    if (commitment.Status == CommitmentStatus.Fulfilled)
                        continue;

                    commitment.EvaluatedAtUtc = now;
                    var remaining = commitment.DueUtc - now;

                    if (remaining <= TimeSpan.Zero)
                    {
                        if (commitment.Status != CommitmentStatus.Breached)
                        {
                            commitment.Status = CommitmentStatus.Breached;
                            commitment.Note = $"SLA breached at {now:O}. Overdue by {(-remaining).TotalMinutes:F1} minutes.";
                            itemChanged = true;

                            // Trigger escalation if not already escalated
                            if (item.EscalationRecord == null || item.EscalationRecord.IsResolved)
                            {
                                item.EscalationRecord = new WorkEscalation
                                {
                                    EscalationId = $"ESC-{Guid.NewGuid().ToString("N")[..8]}",
                                    WorkId = item.WorkId,
                                    TriggerReason = $"Commitment '{commitment.Deliverable}' SLA Breached.",
                                    UrgencyTier = WorkUrgencyTier.Urgent,
                                    RequiredApproverRole = "COO",
                                    InitiatedUtc = now
                                };
                            }
                        }
                    }
                    else if (remaining.TotalSeconds < (commitment.MaxSlaSeconds * 0.3)) // Under 30% remaining
                    {
                        if (commitment.Status == CommitmentStatus.Nominal)
                        {
                            commitment.Status = CommitmentStatus.AtRisk;
                            commitment.Note = $"Commitment SLA at risk. {remaining.TotalMinutes:F1} minutes remaining.";
                            itemChanged = true;
                        }
                    }

                    evaluated.Add(commitment);
                }

                // 2. Evaluate Overall WorkDeadline
                if (item.Deadline != null)
                {
                    if (now > item.Deadline.HardCutoffUtc)
                    {
                        if (item.Deadline.AutoEscalateOnBreach && (item.EscalationRecord == null || item.EscalationRecord.IsResolved))
                        {
                            item.EscalationRecord = new WorkEscalation
                            {
                                EscalationId = $"ESC-{Guid.NewGuid().ToString("N")[..8]}",
                                WorkId = item.WorkId,
                                TriggerReason = $"Hard cutoff deadline breached at {item.Deadline.HardCutoffUtc:O}.",
                                UrgencyTier = WorkUrgencyTier.Immediate,
                                RequiredApproverRole = "CEO",
                                InitiatedUtc = now
                            };
                            itemChanged = true;
                        }
                    }
                }

                if (itemChanged)
                {
                    await _repository.SaveWorkItemAsync(item, ct);
                }
            }

            return evaluated;
        }
    }
}
