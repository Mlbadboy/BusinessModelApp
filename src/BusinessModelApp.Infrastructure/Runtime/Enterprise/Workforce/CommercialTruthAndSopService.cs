using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce
{
    public interface ICommercialTruthAndSopStore
    {
        Task SaveFactAsync(string tenantId, CommercialFact fact);
        Task<CommercialFact?> GetFactAsync(string tenantId, string opportunityId);
        Task<IReadOnlyList<CommercialFact>> ListFactsAsync(string tenantId);

        Task SaveSopAsync(string tenantId, BusinessSopDefinition sop);
        Task<BusinessSopDefinition?> GetSopAsync(string tenantId, string sopId);
        Task<IReadOnlyList<BusinessSopDefinition>> ListSopsAsync(string tenantId);
    }

    public class InMemoryCommercialTruthAndSopStore : ICommercialTruthAndSopStore
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, CommercialFact>> _factsByTenant = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, BusinessSopDefinition>> _sopsByTenant = new();

        public InMemoryCommercialTruthAndSopStore()
        {
            // Seed standard SOPs for all tenants
            SeedDefaultSops("tenant-default");
        }

        private void SeedDefaultSops(string tenantId)
        {
            var map = _sopsByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, BusinessSopDefinition>());

            map["LEAD_QUALIFICATION_SOP"] = new BusinessSopDefinition
            {
                SopId = "LEAD_QUALIFICATION_SOP",
                Name = "Lead Qualification SOP",
                Description = "Research -> Evidence -> ICP -> Buying Center -> Pain -> Commercial Fit",
                Stages = new List<BusinessSopStage>
                {
                    new BusinessSopStage { StageOrder = 1, StageName = "AccountResearch", RequiredTools = { "Browser.Search" }, RiskLevel = 1 },
                    new BusinessSopStage { StageOrder = 2, StageName = "EvidenceCollection", RequiredTools = { "Browser.Navigate" }, RiskLevel = 1 },
                    new BusinessSopStage { StageOrder = 3, StageName = "BuyingCenterMapping", RequiredTools = { "CRM.Read" }, RiskLevel = 1 }
                }
            };

            map["SALES_SOP"] = new BusinessSopDefinition
            {
                SopId = "SALES_SOP",
                Name = "Sales SOP",
                Description = "Opportunity -> Outreach -> Meeting -> Proposal -> Negotiation -> Deal",
                Stages = new List<BusinessSopStage>
                {
                    new BusinessSopStage { StageOrder = 1, StageName = "OutreachPreparation", RequiredTools = { "Email.Draft" }, RiskLevel = 2 },
                    new BusinessSopStage { StageOrder = 2, StageName = "MeetingCoordination", RequiredTools = { "Calendar.ScheduleDraft" }, RiskLevel = 2 },
                    new BusinessSopStage { StageOrder = 3, StageName = "ProposalSynthesis", RequiredTools = { "Document.Generate" }, RiskLevel = 2 }
                }
            };

            map["DELIVERY_SOP"] = new BusinessSopDefinition
            {
                SopId = "DELIVERY_SOP",
                Name = "Delivery SOP",
                Description = "Deal -> Scope -> Work Plan -> Mission -> Verification -> Acceptance -> Invoice",
                Stages = new List<BusinessSopStage>
                {
                    new BusinessSopStage { StageOrder = 1, StageName = "ScopeDecomposition", RiskLevel = 1 },
                    new BusinessSopStage { StageOrder = 2, StageName = "ExecutionVerification", RiskLevel = 2 },
                    new BusinessSopStage { StageOrder = 3, StageName = "ClientAcceptance", RiskLevel = 2 }
                }
            };

            map["COLLECTION_SOP"] = new BusinessSopDefinition
            {
                SopId = "COLLECTION_SOP",
                Name = "Collection SOP",
                Description = "Invoice -> Due Date -> Reminder -> Escalation -> Collection -> Reconciliation",
                Stages = new List<BusinessSopStage>
                {
                    new BusinessSopStage { StageOrder = 1, StageName = "InvoiceVerification", RiskLevel = 1 },
                    new BusinessSopStage { StageOrder = 2, StageName = "PaymentReminderDraft", RiskLevel = 2 },
                    new BusinessSopStage { StageOrder = 3, StageName = "BankReconciliation", RiskLevel = 2 }
                }
            };
        }

        public Task SaveFactAsync(string tenantId, CommercialFact fact)
        {
            var map = _factsByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, CommercialFact>());
            map[fact.OpportunityId] = fact;
            return Task.CompletedTask;
        }

        public Task<CommercialFact?> GetFactAsync(string tenantId, string opportunityId)
        {
            if (_factsByTenant.TryGetValue(tenantId, out var map) && map.TryGetValue(opportunityId, out var fact))
            {
                return Task.FromResult<CommercialFact?>(fact);
            }
            return Task.FromResult<CommercialFact?>(null);
        }

        public Task<IReadOnlyList<CommercialFact>> ListFactsAsync(string tenantId)
        {
            if (_factsByTenant.TryGetValue(tenantId, out var map))
            {
                return Task.FromResult<IReadOnlyList<CommercialFact>>(map.Values.ToList());
            }
            return Task.FromResult<IReadOnlyList<CommercialFact>>(Array.Empty<CommercialFact>());
        }

        public Task SaveSopAsync(string tenantId, BusinessSopDefinition sop)
        {
            var map = _sopsByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, BusinessSopDefinition>());
            map[sop.SopId] = sop;
            return Task.CompletedTask;
        }

        public Task<BusinessSopDefinition?> GetSopAsync(string tenantId, string sopId)
        {
            if (_sopsByTenant.TryGetValue(tenantId, out var map) && map.TryGetValue(sopId, out var sop))
            {
                return Task.FromResult<BusinessSopDefinition?>(sop);
            }
            if (_sopsByTenant.TryGetValue("tenant-default", out var defMap) && defMap.TryGetValue(sopId, out var defSop))
            {
                return Task.FromResult<BusinessSopDefinition?>(defSop);
            }
            return Task.FromResult<BusinessSopDefinition?>(null);
        }

        public Task<IReadOnlyList<BusinessSopDefinition>> ListSopsAsync(string tenantId)
        {
            var list = new Dictionary<string, BusinessSopDefinition>();
            if (_sopsByTenant.TryGetValue("tenant-default", out var defMap))
            {
                foreach (var kv in defMap) list[kv.Key] = kv.Value;
            }
            if (_sopsByTenant.TryGetValue(tenantId, out var map))
            {
                foreach (var kv in map) list[kv.Key] = kv.Value;
            }
            return Task.FromResult<IReadOnlyList<BusinessSopDefinition>>(list.Values.ToList());
        }
    }

    public class CommercialTruthAndSopService : ICommercialTruthEngine, IBusinessSopCompiler
    {
        private readonly ICommercialTruthAndSopStore _store;

        public CommercialTruthAndSopService(ICommercialTruthAndSopStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        // --- Commercial Truth Engine ---

        public async Task<CommercialFact> RecordCommercialClaimAsync(string tenantId, string opportunityId, CommercialTruthState claimedState, string description)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(opportunityId)) throw new ArgumentException("OpportunityId required", nameof(opportunityId));

            // Constitutional Law I39-Q: Claimed != Verified. Agent claim is an unverified hypothesis.
            var fact = await _store.GetFactAsync(tenantId, opportunityId);
            if (fact == null)
            {
                fact = new CommercialFact
                {
                    TenantId = tenantId,
                    OpportunityId = opportunityId
                };
            }

            fact.State = claimedState;
            fact.ClaimedDescription = description;
            fact.IsVerified = false; // Requires external corroborating evidence
            fact.StateUpdatedAt = DateTime.UtcNow;

            await _store.SaveFactAsync(tenantId, fact);
            return fact;
        }

        public async Task<CommercialFact> CorroborateCommercialFactAsync(string tenantId, string opportunityId, CommercialEvidenceItem evidence)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(opportunityId)) throw new ArgumentException("OpportunityId required", nameof(opportunityId));
            if (evidence == null) throw new ArgumentNullException(nameof(evidence));

            var fact = await _store.GetFactAsync(tenantId, opportunityId);
            if (fact == null)
            {
                fact = new CommercialFact
                {
                    TenantId = tenantId,
                    OpportunityId = opportunityId,
                    State = CommercialTruthState.VERIFIED
                };
            }

            fact.CorroboratingEvidence.Add(evidence);
            // Once high-confidence evidence is provided, mark verified
            if (evidence.CorroborationConfidence >= 0.8m)
            {
                fact.IsVerified = true;
            }
            fact.StateUpdatedAt = DateTime.UtcNow;

            await _store.SaveFactAsync(tenantId, fact);
            return fact;
        }

        public Task<CommercialFact?> GetCommercialFactAsync(string tenantId, string opportunityId)
        {
            return _store.GetFactAsync(tenantId, opportunityId);
        }

        public Task<IReadOnlyList<CommercialFact>> ListCommercialFactsAsync(string tenantId)
        {
            return _store.ListFactsAsync(tenantId);
        }

        // --- Business SOP Compiler ---

        public async Task<BusinessSopDefinition> RegisterSopDefinitionAsync(string tenantId, BusinessSopDefinition sop)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required", nameof(tenantId));
            if (sop == null) throw new ArgumentNullException(nameof(sop));

            await _store.SaveSopAsync(tenantId, sop);
            return sop;
        }

        public Task<BusinessSopDefinition?> GetSopDefinitionAsync(string tenantId, string sopId)
        {
            return _store.GetSopAsync(tenantId, sopId);
        }

        public Task<IReadOnlyList<BusinessSopDefinition>> ListSopDefinitionsAsync(string tenantId)
        {
            return _store.ListSopsAsync(tenantId);
        }

        public async Task<CompiledWorkProposal> CompileSopToWorkProposalAsync(string tenantId, string sopId, string opportunityId, string title)
        {
            var sop = await _store.GetSopAsync(tenantId, sopId);
            if (sop == null)
            {
                throw new InvalidOperationException($"SOP '{sopId}' not found for tenant '{tenantId}'.");
            }

            var proposal = new CompiledWorkProposal
            {
                TenantId = tenantId,
                SopId = sopId,
                OpportunityId = opportunityId,
                Title = title,
                PlannedMissionTitles = sop.Stages.Select(s => $"{s.StageOrder}. {s.StageName}").ToList(),
                MaxRiskLevel = sop.Stages.Count > 0 ? sop.Stages.Max(s => s.RiskLevel) : 1,
                EstimatedTotalCost = sop.Stages.Count * 25.0m,
                CompiledAt = DateTime.UtcNow
            };

            return proposal;
        }
    }
}
