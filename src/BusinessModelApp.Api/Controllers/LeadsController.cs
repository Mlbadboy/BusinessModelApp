using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.DTOs.Commercial;
using BusinessModelApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeadsController : ControllerBase
    {
        private readonly ICommercialRepository _repository;

        public LeadsController(ICommercialRepository repository)
        {
            _repository = repository;
        }

        private Guid GetWorkspaceId()
        {
            if (Request.Headers.TryGetValue("X-Workspace-Id", out var headerValue) &&
                Guid.TryParse(headerValue, out var workspaceId))
            {
                return workspaceId;
            }

            var claim = User.FindFirst("workspace_id");
            if (claim != null && Guid.TryParse(claim.Value, out var claimWorkspaceId))
            {
                return claimWorkspaceId;
            }

            // Fallback for development/testing
            return Guid.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeadDto>>> GetLeads([FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = workspaceId ?? GetWorkspaceId();
            if (targetWorkspaceId == Guid.Empty)
            {
                // Return all leads if workspace is empty in dev
                var allLeads = await _repository.GetLeadsByWorkspaceIdAsync(Guid.Empty);
                return Ok(allLeads.Select(MapToDto));
            }

            var leads = await _repository.GetLeadsByWorkspaceIdAsync(targetWorkspaceId);
            return Ok(leads.Select(MapToDto));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LeadDto>> GetLeadById(Guid id)
        {
            var lead = await _repository.GetLeadByIdAsync(id);
            if (lead == null)
            {
                return NotFound(new { message = "Lead not found." });
            }

            return Ok(MapToDto(lead));
        }

        [HttpPost]
        public async Task<ActionResult<LeadDto>> CreateLead([FromBody] CreateLeadDto request, [FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = workspaceId ?? GetWorkspaceId();

            var lead = new Lead
            {
                WorkspaceId = targetWorkspaceId,
                ContactName = request.ContactName,
                Email = request.Email,
                Phone = request.Phone,
                CompanyName = request.CompanyName,
                Source = request.Source,
                Status = LeadStatus.New,
                QualityScore = 75.0, // Initial default score
                Notes = request.Notes
            };

            var created = await _repository.CreateLeadAsync(lead);
            return CreatedAtAction(nameof(GetLeadById), new { id = created.Id }, MapToDto(created));
        }

        [HttpPost("{id}/qualify")]
        public async Task<ActionResult<OpportunityDto>> QualifyLead(Guid id, [FromBody] CreateOpportunityDto oppRequest)
        {
            var lead = await _repository.GetLeadByIdAsync(id);
            if (lead == null)
            {
                return NotFound(new { message = "Lead not found." });
            }

            if (lead.Opportunity != null)
            {
                return Conflict(new { message = "Lead has already been converted to an Opportunity." });
            }

            lead.Status = LeadStatus.Qualified;
            await _repository.UpdateLeadAsync(lead);

            var opportunity = new Opportunity
            {
                WorkspaceId = lead.WorkspaceId,
                LeadId = lead.Id,
                Title = string.IsNullOrWhiteSpace(oppRequest.Title) 
                    ? $"{lead.CompanyName} - Enterprise Solution" 
                    : oppRequest.Title,
                EstimatedValue = oppRequest.EstimatedValue > 0 ? oppRequest.EstimatedValue : 500000m,
                Currency = string.IsNullOrWhiteSpace(oppRequest.Currency) ? "INR" : oppRequest.Currency,
                Stage = OpportunityStage.Discovery,
                Probability = 0.2,
                ExpectedCloseDate = oppRequest.ExpectedCloseDate ?? DateTime.UtcNow.AddDays(30),
                PrimaryConcern = oppRequest.PrimaryConcern,
                NextStep = oppRequest.NextStep
            };

            var createdOpp = await _repository.CreateOpportunityAsync(opportunity);

            // Record initial activity
            await _repository.AddActivityAsync(new Activity
            {
                OpportunityId = createdOpp.Id,
                Type = ActivityType.StageChanged,
                Title = "Lead Qualified & Opportunity Created",
                Description = $"Converted lead {lead.ContactName} from {lead.CompanyName}.",
                PerformedByName = User.Identity?.Name ?? "User"
            });

            return Ok(new OpportunityDto
            {
                Id = createdOpp.Id,
                WorkspaceId = createdOpp.WorkspaceId,
                LeadId = createdOpp.LeadId,
                LeadContactName = lead.ContactName,
                LeadCompanyName = lead.CompanyName,
                Title = createdOpp.Title,
                EstimatedValue = createdOpp.EstimatedValue,
                Currency = createdOpp.Currency,
                Stage = createdOpp.Stage.ToString(),
                Probability = createdOpp.Probability,
                ExpectedCloseDate = createdOpp.ExpectedCloseDate,
                PrimaryConcern = createdOpp.PrimaryConcern,
                NextStep = createdOpp.NextStep,
                CreatedAt = createdOpp.CreatedAt,
                UpdatedAt = createdOpp.UpdatedAt
            });
        }

        private static LeadDto MapToDto(Lead l) => new LeadDto
        {
            Id = l.Id,
            WorkspaceId = l.WorkspaceId,
            ContactName = l.ContactName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Status = l.Status.ToString(),
            Source = l.Source.ToString(),
            QualityScore = l.QualityScore,
            Notes = l.Notes,
            CreatedAt = l.CreatedAt,
            HasOpportunity = l.Opportunity != null,
            OpportunityId = l.Opportunity?.Id
        };
    }
}
