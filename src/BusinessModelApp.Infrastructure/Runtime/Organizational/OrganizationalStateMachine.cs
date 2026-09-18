using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational
{
    public class OrganizationalStateMachine : IOrganizationalStateMachine
    {
        // Legal forward transitions
        private static readonly Dictionary<WorkState, HashSet<WorkState>> AllowedTransitions = new()
        {
            [WorkState.Detected] = new HashSet<WorkState> { WorkState.Qualified, WorkState.Cancelled },
            [WorkState.Qualified] = new HashSet<WorkState> { WorkState.Planned, WorkState.Cancelled },
            [WorkState.Planned] = new HashSet<WorkState> { WorkState.Assigned, WorkState.Cancelled },
            [WorkState.Assigned] = new HashSet<WorkState> { WorkState.Preparing, WorkState.Blocked, WorkState.Cancelled },
            [WorkState.Preparing] = new HashSet<WorkState> { WorkState.WaitingForGovernance, WorkState.Blocked, WorkState.Paused, WorkState.Cancelled },
            [WorkState.WaitingForGovernance] = new HashSet<WorkState> { WorkState.GovernanceApproved, WorkState.Escalated, WorkState.Cancelled },
            [WorkState.GovernanceApproved] = new HashSet<WorkState> { WorkState.ExecutionAdmitted, WorkState.Cancelled },
            [WorkState.ExecutionAdmitted] = new HashSet<WorkState> { WorkState.Executing, WorkState.Paused, WorkState.Cancelled },
            [WorkState.Executing] = new HashSet<WorkState> { WorkState.Verifying, WorkState.UnknownEffect, WorkState.Paused, WorkState.Cancelled },
            [WorkState.Verifying] = new HashSet<WorkState> { WorkState.Completed, WorkState.Quarantined, WorkState.Executing, WorkState.Cancelled },
            [WorkState.Completed] = new HashSet<WorkState> { WorkState.Measured, WorkState.Closed },
            [WorkState.Measured] = new HashSet<WorkState> { WorkState.Closed },
            [WorkState.Closed] = new HashSet<WorkState>(), // Terminal

            // Exception recovery transitions
            [WorkState.Blocked] = new HashSet<WorkState> { WorkState.Preparing, WorkState.Assigned, WorkState.Escalated, WorkState.Cancelled },
            [WorkState.Paused] = new HashSet<WorkState> { WorkState.Preparing, WorkState.ExecutionAdmitted, WorkState.Executing, WorkState.Cancelled },
            [WorkState.Escalated] = new HashSet<WorkState> { WorkState.WaitingForGovernance, WorkState.GovernanceApproved, WorkState.Cancelled },
            [WorkState.UnknownEffect] = new HashSet<WorkState> { WorkState.Quarantined, WorkState.Escalated, WorkState.Cancelled },
            [WorkState.Quarantined] = new HashSet<WorkState> { WorkState.Closed, WorkState.Cancelled },
            [WorkState.Expired] = new HashSet<WorkState> { WorkState.Closed, WorkState.Cancelled },
            [WorkState.Cancelled] = new HashSet<WorkState>() // Terminal
        };

        public (bool Success, string? ErrorMessage, WorkState NewState) ValidateAndTransition(
            WorkItem item,
            WorkState targetState,
            string? governanceApprovalActor = null,
            string? verificationEvidenceHash = null)
        {
            if (item == null)
                return (false, "WorkItem cannot be null.", WorkState.Detected);

            // Idempotent transition
            if (item.State == targetState)
                return (true, null, item.State);

            // Invariant check: Terminal states
            if (item.State == WorkState.Closed || item.State == WorkState.Cancelled)
                return (false, $"Cannot transition out of terminal state {item.State}.", item.State);

            // Check if transition is allowed in the state machine
            if (!AllowedTransitions.TryGetValue(item.State, out var nextAllowed) || !nextAllowed.Contains(targetState))
            {
                return (false, $"Illegal state transition from {item.State} to {targetState}. Jump violates nominal and exception contracts.", item.State);
            }

            // Governance Approval Gate
            if (targetState == WorkState.GovernanceApproved)
            {
                if (string.IsNullOrWhiteSpace(governanceApprovalActor))
                {
                    return (false, "GovernanceApproved state requires explicit human/governance approval actor.", item.State);
                }

                // High risk tier requirements (R3+ must be CEO/Board)
                if (item.RiskTier >= WorkRiskTier.R3_Consequential &&
                    !governanceApprovalActor.Equals("CEO", StringComparison.OrdinalIgnoreCase) &&
                    !governanceApprovalActor.Equals("Board", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, $"WorkItem risk tier {item.RiskTier} requires CEO or Board governance approval; '{governanceApprovalActor}' is unauthorized.", item.State);
                }
            }

            // Verification Evidence Gate for Completed
            if (targetState == WorkState.Completed)
            {
                if (string.IsNullOrWhiteSpace(verificationEvidenceHash))
                {
                    return (false, "Transition to Completed requires verified cryptographic evidence hash (CLAIMED ≠ VERIFIED).", item.State);
                }
            }

            // Invariant I25-Q: WorkState transition to ExecutionAdmitted or Executing NEVER issues an ExecutionPermit
            // This is strictly an organizational control state transition.

            item.State = targetState;
            item.ComputeProvenanceHash();

            return (true, null, targetState);
        }
    }
}
