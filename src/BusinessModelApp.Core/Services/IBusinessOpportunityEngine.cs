using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;

namespace BusinessModelApp.Core.Services
{
    public class OpportunityDiscoveryFilter
    {
        public string City { get; set; } = "Pune";
        public string IndustryOrCategory { get; set; } = "Real Estate Developer";
        public int TargetCount { get; set; } = 20;
    }

    public class DiscoveredOpportunityPackage
    {
        public DiscoveredBusiness Business { get; set; } = new();
        public WebsiteAuditReport Audit { get; set; } = new();
        public OpportunityHypothesis Hypothesis { get; set; } = new();
    }

    public interface IBusinessOpportunityEngine
    {
        Task<List<DiscoveredOpportunityPackage>> DiscoverOpportunitiesAsync(Guid workspaceId, OpportunityDiscoveryFilter filter);
        Task<WebsiteAuditReport> AuditBusinessPresenceAsync(string websiteUrl, string businessName);
        Task<OpportunityHypothesis> FormulateHypothesisAsync(Guid workspaceId, DiscoveredBusiness business, WebsiteAuditReport audit);
    }
}
