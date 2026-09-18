using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce
{
    public interface IRevenueControlPlaneStore
    {
        Task SaveMissionAsync(string tenantId, RevenueMissionSpecification mission);
        Task<IReadOnlyList<RevenueMissionSpecification>> ListMissionsAsync(string tenantId, string? opportunityId = null);

        Task SaveControlPlaneStateAsync(string tenantId, RevenueControlPlaneState state);
        Task<RevenueControlPlaneState?> GetControlPlaneStateAsync(string tenantId);
    }

    public class InMemoryRevenueControlPlaneStore : IRevenueControlPlaneStore
    {
        private readonly ConcurrentDictionary<string, List<RevenueMissionSpecification>> _missionsByTenant = new();
        private readonly ConcurrentDictionary<string, RevenueControlPlaneState> _statesByTenant = new();

        public Task SaveMissionAsync(string tenantId, RevenueMissionSpecification mission)
        {
            var list = _missionsByTenant.GetOrAdd(tenantId, _ => new List<RevenueMissionSpecification>());
            lock (list)
            {
                list.Add(mission);
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RevenueMissionSpecification>> ListMissionsAsync(string tenantId, string? opportunityId = null)
        {
            if (_missionsByTenant.TryGetValue(tenantId, out var list))
            {
                lock (list)
                {
                    var q = list.AsEnumerable();
                    if (!string.IsNullOrWhiteSpace(opportunityId)) q = q.Where(m => m.OpportunityId == opportunityId);
                    return Task.FromResult<IReadOnlyList<RevenueMissionSpecification>>(q.ToList());
                }
            }
            return Task.FromResult<IReadOnlyList<RevenueMissionSpecification>>(Array.Empty<RevenueMissionSpecification>());
        }

        public Task SaveControlPlaneStateAsync(string tenantId, RevenueControlPlaneState state)
        {
            _statesByTenant[tenantId] = state;
            return Task.CompletedTask;
        }

        public Task<RevenueControlPlaneState?> GetControlPlaneStateAsync(string tenantId)
        {
            if (_statesByTenant.TryGetValue(tenantId, out var state))
            {
                return Task.FromResult<RevenueControlPlaneState?>(state);
            }
            return Task.FromResult<RevenueControlPlaneState?>(null);
        }
    }

    public class RevenueControlPlaneAndFactoryService : IRevenueMissionFactory, IRevenueControlPlane
    {
        private readonly IRevenueControlPlaneStore _store;

        public RevenueControlPlaneAndFactoryService(IRevenueControlPlaneStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<RevenueMissionSpecification> CreateRevenueMissionAsync(string tenantId, string opportunityId, RevenueMissionTemplateType templateType, decimal budget = 50m)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(opportunityId)) throw new ArgumentException("OpportunityId required", nameof(opportunityId));

            var spec = new RevenueMissionSpecification
            {
                TenantId = tenantId,
                OpportunityId = opportunityId,
                TemplateType = templateType,
                AllocatedBudget = budget,
                CreatedAt = DateTime.UtcNow
            };

            switch (templateType)
            {
                case RevenueMissionTemplateType.QUALIFY_ACCOUNT:
                    spec.Title = "Qualify Account & ICP Fit";
                    spec.AssignedRoleTitle = "RevenueProspector";
                    spec.RequiredCapabilities = new() { "cap-web-search", "cap-crm-read" };
                    spec.RequiredTools = new() { "Browser.Search", "CRM.Read" };
                    spec.RiskTier = 1;
                    break;

                case RevenueMissionTemplateType.RESEARCH_BUYING_CENTER:
                    spec.Title = "Research Buying Center & Personas";
                    spec.AssignedRoleTitle = "AccountResearcher";
                    spec.RequiredCapabilities = new() { "cap-web-navigate", "cap-crm-read" };
                    spec.RequiredTools = new() { "Browser.Navigate", "CRM.Read" };
                    spec.RiskTier = 1;
                    break;

                case RevenueMissionTemplateType.PREPARE_OUTREACH:
                    spec.Title = "Draft Governed Commercial Outreach";
                    spec.AssignedRoleTitle = "ProposalArchitect";
                    spec.RequiredCapabilities = new() { "cap-email-draft" };
                    spec.RequiredTools = new() { "Email.Draft" };
                    spec.RiskTier = 2;
                    break;

                case RevenueMissionTemplateType.BOOK_MEETING:
                    spec.Title = "Coordinate Meeting Schedule";
                    spec.AssignedRoleTitle = "RevenueProspector";
                    spec.RequiredCapabilities = new() { "cap-calendar-schedule" };
                    spec.RequiredTools = new() { "Calendar.ScheduleDraft" };
                    spec.RiskTier = 2;
                    break;

                case RevenueMissionTemplateType.PREPARE_PROPOSAL:
                    spec.Title = "Synthesize Evidence-Grounded Proposal";
                    spec.AssignedRoleTitle = "ProposalArchitect";
                    spec.RequiredCapabilities = new() { "cap-document-generate" };
                    spec.RequiredTools = new() { "Document.Generate" };
                    spec.RiskTier = 2;
                    break;

                case RevenueMissionTemplateType.NEGOTIATE:
                    spec.Title = "Commercial Terms & Pricing Negotiation";
                    spec.AssignedRoleTitle = "RevenueDirector";
                    spec.RequiredCapabilities = new() { "cap-commercial-negotiate" };
                    spec.RequiredTools = new() { "Document.Generate", "CRM.UpdateOpportunity" };
                    spec.RiskTier = 3; // Mandates PRG-1
                    break;

                case RevenueMissionTemplateType.INVOICE:
                    spec.Title = "Generate & Dispatch Commercial Invoice";
                    spec.AssignedRoleTitle = "FinanceOperations";
                    spec.RequiredCapabilities = new() { "cap-invoicing" };
                    spec.RequiredTools = new() { "Document.Generate" };
                    spec.RiskTier = 3; // Financial mutation
                    break;

                case RevenueMissionTemplateType.COLLECTION:
                    spec.Title = "Verify Payment & Bank Reconciliation";
                    spec.AssignedRoleTitle = "FinanceOperations";
                    spec.RequiredCapabilities = new() { "cap-payment-verify" };
                    spec.RequiredTools = new() { "CRM.Read" };
                    spec.RiskTier = 2;
                    break;

                default:
                    spec.Title = $"Execute Commercial Mission ({templateType})";
                    spec.RiskTier = 2;
                    break;
            }

            await _store.SaveMissionAsync(tenantId, spec);
            return spec;
        }

        public Task<IReadOnlyList<RevenueMissionSpecification>> ListMissionsForOpportunityAsync(string tenantId, string opportunityId)
        {
            return _store.ListMissionsAsync(tenantId, opportunityId);
        }

        public async Task<RevenueControlPlaneState> GetCurrentStateAsync(string tenantId)
        {
            var state = await _store.GetControlPlaneStateAsync(tenantId);
            if (state == null)
            {
                state = new RevenueControlPlaneState
                {
                    TenantId = tenantId,
                    TargetRevenue = 1000000m,
                    LastUpdated = DateTime.UtcNow
                };
                await _store.SaveControlPlaneStateAsync(tenantId, state);
            }
            return state;
        }

        public async Task UpdatePipelineMetricsAsync(string tenantId, decimal qualifiedPipeline, decimal weightedPipeline, decimal closedWon, decimal invoiced, decimal collected, decimal margin)
        {
            var state = await GetCurrentStateAsync(tenantId);
            state.QualifiedPipelineAmount = qualifiedPipeline;
            state.WeightedPipelineAmount = weightedPipeline;
            state.ClosedWonRevenue = closedWon;
            state.InvoicedRevenue = invoiced;
            state.CollectedCashRevenue = collected;
            state.RealizedGrossMargin = margin;
            state.LastUpdated = DateTime.UtcNow;

            await _store.SaveControlPlaneStateAsync(tenantId, state);
        }
    }
}
