using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Constitution;
using BusinessModelApp.Core.Domain.Missions;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Core.Agents
{
    public class AgentExecutionContext
    {
        public Guid ExecutionId { get; set; } = Guid.NewGuid();
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public string ActiveTask { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public class AgentBudgetContext
    {
        public decimal AllocatedBudgetINR { get; set; } = 10000m;
        public decimal ConsumedBudgetINR { get; set; } = 0m;
        public decimal RemainingBudgetINR => Math.Max(0m, AllocatedBudgetINR - ConsumedBudgetINR);
        public bool CanAfford(decimal costINR) => RemainingBudgetINR >= costINR;
        
        public void Consume(decimal costINR)
        {
            ConsumedBudgetINR += costINR;
        }
    }

    public class AgentPolicyContext
    {
        public AutonomyLevel CurrentAutonomy { get; set; } = AutonomyLevel.Level3_ControlledAutonomy;
        public bool IsExecutionSimulatedOnly { get; set; } = true; // Hard Phase 1 Boundary: REAL TOOLS DISABLED
    }

    public class AgentContext
    {
        public AgentIdentity Identity { get; set; }
        public AgentMemory Memory { get; set; }
        public AgentMailbox Mailbox { get; set; }
        public AgentExecutionContext Execution { get; set; } = new();
        public AgentBudgetContext Budget { get; set; } = new();
        public AgentPolicyContext Policy { get; set; } = new();

        public AgentContext(AgentIdentity identity, AgentMemory memory, AgentMailbox mailbox)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            Memory = memory ?? throw new ArgumentNullException(nameof(memory));
            Mailbox = mailbox ?? throw new ArgumentNullException(nameof(mailbox));
        }
    }

    public class AgentExecutionResult
    {
        public bool Succeeded { get; set; }
        public string OutputDigest { get; set; } = string.Empty;
        public string OutputPayloadJson { get; set; } = "{}";
        public bool WasSimulated { get; set; } = true;
        public bool RequiredEscalation { get; set; } = false;
        public string? EscalationReason { get; set; }
    }

    public interface IAgentRuntime
    {
        Task<AgentExecutionResult> ExecuteStepAsync(
            AgentContext context,
            MissionBlackboard blackboard,
            AgentActionType action,
            string taskDescription,
            decimal estimatedCostINR,
            CancellationToken ct = default);
    }

    /// <summary>
    /// Autonomous Commercial Officer Runtime.
    /// Executes the standard agent lifecycle:
    /// SPAWN -> LOAD IDENTITY -> LOAD MEMORY -> LOAD MISSION -> CHECK AUTHORITY -> CHECK BUDGET -> READ INBOX -> PLAN -> REQUEST TOOL -> GOVERNANCE -> EXECUTE -> OBSERVE -> WRITE MEMORY -> SEND MESSAGE.
    /// 
    /// HARD PHASE 1 INVARIANT:
    /// NO UNAPPROVED REAL-WORLD SIDE EFFECTS.
    /// At EXECUTE stage, real-world tools remain disabled; execution returns SIMULATED / PREPARED work package.
    /// </summary>
    public class AgentRuntime : IAgentRuntime
    {
        private readonly AgentPolicyEngine _policyEngine;
        private readonly IConstitutionPolicyEngine _constitutionEngine;
        private readonly ILogger<AgentRuntime> _logger;

        public AgentRuntime(
            AgentPolicyEngine policyEngine,
            IConstitutionPolicyEngine constitutionEngine,
            ILogger<AgentRuntime> logger)
        {
            _policyEngine = policyEngine ?? throw new ArgumentNullException(nameof(policyEngine));
            _constitutionEngine = constitutionEngine ?? throw new ArgumentNullException(nameof(constitutionEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AgentExecutionResult> ExecuteStepAsync(
            AgentContext context,
            MissionBlackboard blackboard,
            AgentActionType action,
            string taskDescription,
            decimal estimatedCostINR,
            CancellationToken ct = default)
        {
            _logger.LogInformation("[AgentRuntime] Agent '{Role}' starting task '{Task}'", context.Identity.Role, taskDescription);

            // 1. Check Authority & Role Grants
            if (!context.Identity.CanPerform(action))
            {
                _logger.LogWarning("[AgentRuntime] Denied: Role {Role} cannot perform {Action}", context.Identity.Role, action);
                return new AgentExecutionResult
                {
                    Succeeded = false,
                    OutputDigest = $"Agent role '{context.Identity.Role}' is not permitted to perform action '{action}'.",
                    RequiredEscalation = true,
                    EscalationReason = "Role authority boundary exceeded."
                };
            }

            // 2. Check Budget
            if (!context.Budget.CanAfford(estimatedCostINR))
            {
                _logger.LogWarning("[AgentRuntime] Budget exceeded: Required ₹{Cost:N0}, Remaining ₹{Remaining:N0}",
                    estimatedCostINR, context.Budget.RemainingBudgetINR);
                return new AgentExecutionResult
                {
                    Succeeded = false,
                    OutputDigest = $"Budget cap exceeded: Task requires ₹{estimatedCostINR:N0} but remaining is ₹{context.Budget.RemainingBudgetINR:N0}.",
                    RequiredEscalation = true,
                    EscalationReason = "Budget cap exceeded (Rule 1)."
                };
            }

            // 3. Evaluate Constitution & Policy
            var policyDecision = _policyEngine.Evaluate(context.Identity, action, context.Policy.CurrentAutonomy, estimatedCostINR);
            if (policyDecision.Decision == PolicyActionDecision.DenyAction)
            {
                return new AgentExecutionResult
                {
                    Succeeded = false,
                    OutputDigest = policyDecision.Reason,
                    RequiredEscalation = true,
                    EscalationReason = policyDecision.Reason
                };
            }

            if (policyDecision.RequiresApproval)
            {
                return new AgentExecutionResult
                {
                    Succeeded = false,
                    OutputDigest = $"Requires human executive sign-off: {policyDecision.Reason}",
                    RequiredEscalation = true,
                    EscalationReason = policyDecision.Reason
                };
            }

            // 4. Read Inbox
            var pendingMessages = context.Mailbox.ReadPendingMessages();
            foreach (var msg in pendingMessages)
            {
                context.Mailbox.MarkAsProcessed(msg.MessageId);
            }

            // 5. Hard Phase 1 Boundary: SIMULATE / PREPARE ONLY (No external mutation)
            context.Budget.Consume(estimatedCostINR);
            string outputJson = $"{{\"task\":\"{taskDescription}\",\"action\":\"{action}\",\"status\":\"PREPARED_SIMULATED\",\"simulatedCostINR\":{estimatedCostINR}}}";
            string digest = $"Prepared and verified plan for '{taskDescription}' under governed simulation.";

            // 6. Observe & Write Memory
            context.Memory.AddObservation(context.Identity.Role, taskDescription, digest);

            // 7. Update Blackboard
            blackboard.AddTask(taskDescription, context.Identity.Role);

            await Task.Yield();

            return new AgentExecutionResult
            {
                Succeeded = true,
                OutputDigest = digest,
                OutputPayloadJson = outputJson,
                WasSimulated = true
            };
        }
    }
}
