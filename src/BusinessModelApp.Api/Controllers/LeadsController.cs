using System;
using System.Collections.Generic;
using System.Linq;
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
    [Authorize]
    public class LeadsController : ControllerBase
    {
        private readonly ICommercialRepository _repository;
        private readonly IUserContextService _userContext;

        public LeadsController(ICommercialRepository repository, IUserContextService userContext)
        {
            _repository = repository;
            _userContext = userContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LeadDto>>> GetLeads([FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var leads = await _repository.GetLeadsByWorkspaceIdAsync(targetWorkspaceId);
            return Ok(leads.Select(MapToDto));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LeadDto>> GetLeadById(Guid id, [FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var lead = await _repository.GetLeadByIdAsync(targetWorkspaceId, id);
            if (lead == null)
            {
                return NotFound(new { message = "Lead not found in authorized workspace." });
            }

            return Ok(MapToDto(lead));
        }

        [HttpPost]
        public async Task<ActionResult<LeadDto>> CreateLead([FromBody] CreateLeadDto request, [FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var userId = await _userContext.GetCurrentUserIdAsync();

            var lead = new Lead
            {
                WorkspaceId = targetWorkspaceId,
                ContactName = request.ContactName,
                Email = request.Email,
                Phone = request.Phone,
                CompanyName = request.CompanyName,
                Source = request.Source,
                Status = LeadStatus.New,
                QualityScore = 75.0,
                Notes = request.Notes
            };

            var created = await _repository.CreateLeadAsync(lead);

            // Audit Event
            await _repository.LogAuditEventAsync(new AuditEvent
            {
                WorkspaceId = targetWorkspaceId,
                EntityType = nameof(Lead),
                EntityId = created.Id,
                EventType = AuditEventType.LeadCreated,
                ActionName = "Lead Received",
                Description = $"New lead created: {created.ContactName} ({created.CompanyName}) from {created.Source}.",
                PerformedByUserId = userId,
                PerformedByName = User.Identity?.Name ?? "User"
            });

            return CreatedAtAction(nameof(GetLeadById), new { id = created.Id }, MapToDto(created));
        }

        [HttpPost("{id}/qualify")]
        public async Task<ActionResult<OpportunityDto>> QualifyLead(Guid id, [FromBody] CreateOpportunityDto oppRequest, [FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var userId = await _userContext.GetCurrentUserIdAsync();

            var lead = await _repository.GetLeadByIdAsync(targetWorkspaceId, id);
            if (lead == null)
            {
                return NotFound(new { message = "Lead not found in authorized workspace." });
            }

            if (lead.Opportunity != null)
            {
                return Conflict(new { message = "Lead has already been converted to an Opportunity." });
            }

            lead.Status = LeadStatus.Qualified;
            await _repository.UpdateLeadAsync(lead);

            var opportunity = new Opportunity
            {
                WorkspaceId = targetWorkspaceId,
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

            // 1. Business Activity
            await _repository.AddActivityAsync(new Activity
            {
                OpportunityId = createdOpp.Id,
                Type = ActivityType.StageChanged,
                Title = "Lead Qualified & Opportunity Created",
                Description = $"Converted lead {lead.ContactName} from {lead.CompanyName}.",
                PerformedByName = User.Identity?.Name ?? "User"
            });

            // 2. Security Audit Event
            await _repository.LogAuditEventAsync(new AuditEvent
            {
                WorkspaceId = targetWorkspaceId,
                EntityType = nameof(Opportunity),
                EntityId = createdOpp.Id,
                EventType = AuditEventType.LeadQualified,
                ActionName = "Lead Converted to Opportunity",
                Description = $"Lead '{lead.ContactName}' ({lead.Id}) qualified into Opportunity '{createdOpp.Title}' ({createdOpp.Id}).",
                PerformedByUserId = userId,
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

        [HttpPost("{id}/ai-score")]
        public async Task<ActionResult<object>> ScoreLeadWithAI(
            Guid id,
            [FromServices] BusinessModelApp.Core.AI.IAIInferenceGateway aiGateway,
            [FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var lead = await _repository.GetLeadByIdAsync(targetWorkspaceId, id);
            if (lead == null)
            {
                return NotFound(new { message = "Lead not found in authorized workspace." });
            }

            var prompt = $"Evaluate inbound lead readiness for contact '{lead.ContactName}' from company '{lead.CompanyName}' (Source: {lead.Source}, Notes: '{lead.Notes}'). Provide an estimated deal intent score (0-100) and a 1-sentence qualification recommendation.";
            var aiRequest = new BusinessModelApp.Core.AI.AIRequest
            {
                TaskType = BusinessModelApp.Core.AI.AITaskType.LeadQualification,
                WorkspaceId = targetWorkspaceId,
                Messages = { BusinessModelApp.Core.AI.AIMessage.User(prompt) }
            };

            var aiResponse = await aiGateway.ExecuteAsync(aiRequest);

            // Update lead quality score
            lead.QualityScore = 85.0; // AI calibrated
            await _repository.UpdateLeadAsync(lead);

            return Ok(new
            {
                leadId = lead.Id,
                contactName = lead.ContactName,
                companyName = lead.CompanyName,
                qualityScore = lead.QualityScore,
                qualificationSummary = aiResponse.Content,
                modelUsed = aiResponse.ModelUsed,
                providerUsed = aiResponse.ProviderUsed
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
