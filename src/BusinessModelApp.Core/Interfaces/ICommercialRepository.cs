using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;

namespace BusinessModelApp.Core.Interfaces
{
    public interface ICommercialRepository
    {
        // Workspaces & Organizations
        Task<Organization> GetOrganizationBySlugAsync(string slug, CancellationToken ct = default);
        Task<IEnumerable<Workspace>> GetWorkspacesByOrgIdAsync(Guid orgId, CancellationToken ct = default);
        Task<Workspace> GetWorkspaceByIdAsync(Guid workspaceId, CancellationToken ct = default);

        // Leads
        Task<IEnumerable<Lead>> GetLeadsByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default);
        Task<Lead> GetLeadByIdAsync(Guid leadId, CancellationToken ct = default);
        Task<Lead> CreateLeadAsync(Lead lead, CancellationToken ct = default);
        Task<Lead> UpdateLeadAsync(Lead lead, CancellationToken ct = default);

        // Opportunities
        Task<IEnumerable<Opportunity>> GetOpportunitiesByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default);
        Task<Opportunity> GetOpportunityByIdAsync(Guid opportunityId, CancellationToken ct = default);
        Task<Opportunity> CreateOpportunityAsync(Opportunity opportunity, CancellationToken ct = default);
        Task<Opportunity> UpdateOpportunityAsync(Opportunity opportunity, CancellationToken ct = default);

        // Activities & Interactions
        Task<Activity> AddActivityAsync(Activity activity, CancellationToken ct = default);
        Task<IEnumerable<Activity>> GetActivitiesByOpportunityIdAsync(Guid opportunityId, CancellationToken ct = default);
        Task<Interaction> LogInteractionAsync(Interaction interaction, CancellationToken ct = default);
    }
}
