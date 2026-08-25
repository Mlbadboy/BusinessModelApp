using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BusinessModelApp.Infrastructure.Repositories
{
    public class CommercialRepository : ICommercialRepository
    {
        private readonly AppDbContext _context;

        public CommercialRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Organization> GetOrganizationBySlugAsync(string slug, CancellationToken ct = default)
        {
            return await _context.Organizations
                .Include(o => o.Workspaces)
                .FirstOrDefaultAsync(o => o.Slug == slug && !o.IsDeleted, ct);
        }

        public async Task<IEnumerable<Workspace>> GetWorkspacesByOrgIdAsync(Guid orgId, CancellationToken ct = default)
        {
            return await _context.Workspaces
                .Where(w => w.OrganizationId == orgId && !w.IsDeleted)
                .ToListAsync(ct);
        }

        public async Task<Workspace> GetWorkspaceByIdAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return await _context.Workspaces
                .FirstOrDefaultAsync(w => w.Id == workspaceId && !w.IsDeleted, ct);
        }

        public async Task<IEnumerable<Lead>> GetLeadsByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return await _context.Leads
                .Include(l => l.Interactions)
                .Include(l => l.Opportunity)
                .Where(l => l.WorkspaceId == workspaceId && !l.IsDeleted)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<Lead> GetLeadByIdAsync(Guid leadId, CancellationToken ct = default)
        {
            return await _context.Leads
                .Include(l => l.Interactions)
                .Include(l => l.Opportunity)
                .FirstOrDefaultAsync(l => l.Id == leadId && !l.IsDeleted, ct);
        }

        public async Task<Lead> CreateLeadAsync(Lead lead, CancellationToken ct = default)
        {
            _context.Leads.Add(lead);
            await _context.SaveChangesAsync(ct);
            return lead;
        }

        public async Task<Lead> UpdateLeadAsync(Lead lead, CancellationToken ct = default)
        {
            _context.Leads.Update(lead);
            await _context.SaveChangesAsync(ct);
            return lead;
        }

        public async Task<IEnumerable<Opportunity>> GetOpportunitiesByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return await _context.Opportunities
                .Include(o => o.Lead)
                .Include(o => o.Activities)
                .Where(o => o.WorkspaceId == workspaceId && !o.IsDeleted)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<Opportunity> GetOpportunityByIdAsync(Guid opportunityId, CancellationToken ct = default)
        {
            return await _context.Opportunities
                .Include(o => o.Lead)
                .Include(o => o.Activities)
                .FirstOrDefaultAsync(o => o.Id == opportunityId && !o.IsDeleted, ct);
        }

        public async Task<Opportunity> CreateOpportunityAsync(Opportunity opportunity, CancellationToken ct = default)
        {
            _context.Opportunities.Add(opportunity);
            await _context.SaveChangesAsync(ct);
            return opportunity;
        }

        public async Task<Opportunity> UpdateOpportunityAsync(Opportunity opportunity, CancellationToken ct = default)
        {
            _context.Opportunities.Update(opportunity);
            await _context.SaveChangesAsync(ct);
            return opportunity;
        }

        public async Task<Activity> AddActivityAsync(Activity activity, CancellationToken ct = default)
        {
            _context.Activities.Add(activity);
            await _context.SaveChangesAsync(ct);
            return activity;
        }

        public async Task<IEnumerable<Activity>> GetActivitiesByOpportunityIdAsync(Guid opportunityId, CancellationToken ct = default)
        {
            return await _context.Activities
                .Where(a => a.OpportunityId == opportunityId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<Interaction> LogInteractionAsync(Interaction interaction, CancellationToken ct = default)
        {
            _context.Interactions.Add(interaction);
            await _context.SaveChangesAsync(ct);
            return interaction;
        }
    }
}
