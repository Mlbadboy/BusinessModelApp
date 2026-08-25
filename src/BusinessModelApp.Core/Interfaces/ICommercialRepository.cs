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
        Task<Organization?> GetOrganizationBySlugAsync(string slug, CancellationToken ct = default);
        Task<IEnumerable<Workspace>> GetWorkspacesByOrgIdAsync(Guid orgId, CancellationToken ct = default);
        Task<Workspace?> GetWorkspaceByIdAsync(Guid workspaceId, CancellationToken ct = default);

        // Leads (Scoped by workspace)
        Task<IEnumerable<Lead>> GetLeadsByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default);
        Task<Lead?> GetLeadByIdAsync(Guid workspaceId, Guid leadId, CancellationToken ct = default);
        Task<Lead> CreateLeadAsync(Lead lead, CancellationToken ct = default);
        Task<Lead> UpdateLeadAsync(Lead lead, CancellationToken ct = default);

        // Opportunities (Scoped by workspace)
        Task<IEnumerable<Opportunity>> GetOpportunitiesByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default);
        Task<Opportunity?> GetOpportunityByIdAsync(Guid workspaceId, Guid opportunityId, CancellationToken ct = default);
        Task<Opportunity> CreateOpportunityAsync(Opportunity opportunity, CancellationToken ct = default);
        Task<Opportunity> UpdateOpportunityAsync(Opportunity opportunity, CancellationToken ct = default);

        // Activities & Legacy
        Task<Activity> AddActivityAsync(Activity activity, CancellationToken ct = default);
        Task<IEnumerable<Activity>> GetActivitiesByOpportunityIdAsync(Guid opportunityId, CancellationToken ct = default);
        Task<Interaction> LogInteractionAsync(Interaction interaction, CancellationToken ct = default);

        // Audit Events (Append-Only)
        Task<AuditEvent> LogAuditEventAsync(AuditEvent auditEvent, CancellationToken ct = default);
        Task<IEnumerable<AuditEvent>> GetAuditEventsByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
        Task<IEnumerable<AuditEvent>> GetAuditEventsForEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);

        // Business Activities (Append-Only)
        Task<BusinessActivity> LogBusinessActivityAsync(BusinessActivity activity, CancellationToken ct = default);
        Task<IEnumerable<BusinessActivity>> GetBusinessActivitiesByOpportunityAsync(Guid opportunityId, CancellationToken ct = default);
    }
}
