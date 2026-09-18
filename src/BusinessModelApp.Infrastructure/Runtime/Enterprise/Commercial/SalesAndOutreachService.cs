using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Commercial
{
    public class InMemorySalesIntelligenceStore : ISalesIntelligenceStore
    {
        private readonly ConcurrentDictionary<string, CommercialAccountStrategy> _strategies = new();

        public Task SaveStrategyAsync(CommercialAccountStrategy strategy)
        {
            _strategies[$"{strategy.TenantId}:{strategy.StrategyId}"] = strategy;
            return Task.CompletedTask;
        }

        public Task<CommercialAccountStrategy?> GetStrategyAsync(string tenantId, string strategyId)
        {
            _strategies.TryGetValue($"{tenantId}:{strategyId}", out var strat);
            return Task.FromResult(strat);
        }

        public Task<IReadOnlyList<CommercialAccountStrategy>> ListStrategiesAsync(string tenantId)
        {
            var list = _strategies.Values.Where(s => s.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<CommercialAccountStrategy>>(list);
        }
    }

    public class InMemoryOutreachEngineStore : IOutreachEngineStore
    {
        private readonly ConcurrentDictionary<string, CommercialCommunicationIntent> _intents = new();

        public Task SaveIntentAsync(CommercialCommunicationIntent intent)
        {
            _intents[$"{intent.TenantId}:{intent.IntentId}"] = intent;
            return Task.CompletedTask;
        }

        public Task<CommercialCommunicationIntent?> GetIntentAsync(string tenantId, string intentId)
        {
            _intents.TryGetValue($"{tenantId}:{intentId}", out var intent);
            return Task.FromResult(intent);
        }

        public Task<IReadOnlyList<CommercialCommunicationIntent>> ListIntentsAsync(string tenantId)
        {
            var list = _intents.Values.Where(i => i.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<CommercialCommunicationIntent>>(list);
        }
    }

    public class SalesIntelligenceService : ISalesIntelligenceService
    {
        private readonly ISalesIntelligenceStore _store;

        public SalesIntelligenceService(ISalesIntelligenceStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<CommercialAccountStrategy> FormulateAccountStrategyAsync(string tenantId, string accountId, string opportunityId, string agentId)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.");

            var strategy = new CommercialAccountStrategy
            {
                TenantId = tenantId,
                AccountId = accountId,
                OpportunityId = opportunityId,
                CreatedByAgentId = agentId,
                ProblemStatement = "Identified critical operational bottlenecks in core enterprise workflow.",
                BusinessImpactSummary = "Estimated 30% reduction in delivery cycles and $500k annual operational savings.",
                DecisionMakersSummary = "Targeting Chief Technology Officer and Head of Operations.",
                ValueHypothesis = "Deploying governed autonomous delivery agents eliminates manual coordination overhead.",
                ProposedSolutionDescription = "Pilot implementation of Charlie Business OS autonomous delivery engine.",
                CompetitiveContext = "Incumbent manual consultant-heavy model.",
                AnticipatedObjections = { "Data privacy & LLM boundaries", "Implementation timeline" },
                KeyRisks = { "Initial connector latency", "Stakeholder change management" },
                RecommendedNextAction = "Draft personalized outreach message for PRG-1 human review and dispatch",
                CreatedAtUtc = DateTime.UtcNow
            };

            await _store.SaveStrategyAsync(strategy);
            return strategy;
        }

        public async Task<CommercialAccountStrategy?> GetAccountStrategyAsync(string tenantId, string strategyId)
        {
            return await _store.GetStrategyAsync(tenantId, strategyId);
        }
    }

    public class OutreachEngineService : IOutreachEngineService
    {
        private readonly IOutreachEngineStore _store;

        public OutreachEngineService(IOutreachEngineStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<CommercialCommunicationIntent> SubmitOutboundIntentAsync(CommercialCommunicationIntent intent)
        {
            if (intent == null) throw new ArgumentNullException(nameof(intent));
            if (string.IsNullOrWhiteSpace(intent.TenantId)) throw new ArgumentException("TenantId is required.");

            // If low risk (R1, R2), auto-approve for autonomous outreach within safety envelope
            if (intent.RiskTier < 3)
            {
                intent.IsHumanApproved = true;
            }
            else
            {
                intent.IsHumanApproved = false; // R3+ requires explicit PRG-1 human signoff
            }

            await _store.SaveIntentAsync(intent);
            return intent;
        }

        public async Task<bool> ApproveOutboundIntentAsync(string tenantId, string intentId, string humanSignoffId)
        {
            if (string.IsNullOrWhiteSpace(humanSignoffId))
                throw new ArgumentException("HumanSignoffId is mandatory for PRG-1 approval.");

            var intent = await _store.GetIntentAsync(tenantId, intentId);
            if (intent == null) return false;

            intent.IsHumanApproved = true;
            intent.HumanSignoffId = humanSignoffId;

            await _store.SaveIntentAsync(intent);
            return true;
        }

        public async Task<bool> DispatchOutboundCommunicationAsync(string tenantId, string intentId, string? batch6PermitId = null)
        {
            var intent = await _store.GetIntentAsync(tenantId, intentId);
            if (intent == null) return false;

            // Constitutional Law I40-M & I40-W: Consequential action cannot execute without PRG-1 approval
            if (intent.RequiresHumanApproval && !intent.IsHumanApproved)
            {
                throw new InvalidOperationException($"Governance Violation: Intent {intentId} has RiskTier {intent.RiskTier} and requires PRG-1 human authorization before dispatch.");
            }

            intent.IsDispatched = true;
            intent.Batch6PermitId = batch6PermitId ?? Guid.NewGuid().ToString("N");
            intent.IsBatch6Authorized = true;
            intent.ExternalEffectId = $"eff-outreach-{intent.Channel}-{Guid.NewGuid():N}";

            await _store.SaveIntentAsync(intent);
            return true;
        }

        public async Task<IReadOnlyList<CommercialCommunicationIntent>> GetPendingApprovalsAsync(string tenantId)
        {
            var list = await _store.ListIntentsAsync(tenantId);
            return list.Where(i => i.RequiresHumanApproval && !i.IsHumanApproved && !i.IsDispatched).ToList();
        }
    }
}
