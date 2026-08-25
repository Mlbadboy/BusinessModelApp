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
    public class OpportunitiesController : ControllerBase
    {
        private readonly ICommercialRepository _repository;
        private readonly IUserContextService _userContext;

        public OpportunitiesController(ICommercialRepository repository, IUserContextService userContext)
        {
            _repository = repository;
            _userContext = userContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OpportunityDto>>> GetOpportunities([FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var opportunities = await _repository.GetOpportunitiesByWorkspaceIdAsync(targetWorkspaceId);

            return Ok(opportunities.Select(MapToDto));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OpportunityDto>> GetOpportunityById(Guid id, [FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var opp = await _repository.GetOpportunityByIdAsync(targetWorkspaceId, id);
            if (opp == null)
            {
                return NotFound(new { message = "Opportunity not found in authorized workspace." });
            }

            return Ok(MapToDto(opp));
        }

        [HttpPost]
        public async Task<ActionResult<OpportunityDto>> CreateOpportunity([FromBody] CreateOpportunityDto request, [FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var userId = await _userContext.GetCurrentUserIdAsync();

            var opp = new Opportunity
            {
                WorkspaceId = targetWorkspaceId,
                LeadId = request.LeadId,
                Title = request.Title,
                EstimatedValue = request.EstimatedValue,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "INR" : request.Currency,
                Stage = OpportunityStage.Discovery,
                Probability = 0.2,
                ExpectedCloseDate = request.ExpectedCloseDate ?? DateTime.UtcNow.AddDays(30),
                PrimaryConcern = request.PrimaryConcern,
                NextStep = request.NextStep
            };

            var created = await _repository.CreateOpportunityAsync(opp);

            // 1. Business Activity
            await _repository.AddActivityAsync(new Activity
            {
                OpportunityId = created.Id,
                Type = ActivityType.NoteAdded,
                Title = "Opportunity Created",
                Description = $"Created opportunity: {created.Title} with value {created.Currency} {created.EstimatedValue:N0}",
                PerformedByName = User.Identity?.Name ?? "User"
            });

            // 2. Audit Event
            await _repository.LogAuditEventAsync(new AuditEvent
            {
                WorkspaceId = targetWorkspaceId,
                EntityType = nameof(Opportunity),
                EntityId = created.Id,
                EventType = AuditEventType.OpportunityCreated,
                ActionName = "Opportunity Created",
                Description = $"Created opportunity '{created.Title}' with value {created.EstimatedValue:N0} {created.Currency}.",
                PerformedByUserId = userId,
                PerformedByName = User.Identity?.Name ?? "User"
            });

            return CreatedAtAction(nameof(GetOpportunityById), new { id = created.Id }, MapToDto(created));
        }

        [HttpPatch("{id}/stage")]
        public async Task<ActionResult<OpportunityDto>> UpdateStage(Guid id, [FromBody] UpdateOpportunityStageDto request, [FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var userId = await _userContext.GetCurrentUserIdAsync();

            var opp = await _repository.GetOpportunityByIdAsync(targetWorkspaceId, id);
            if (opp == null)
            {
                return NotFound(new { message = "Opportunity not found in authorized workspace." });
            }

            var oldStage = opp.Stage;
            opp.AdvanceStage(request.Stage);
            await _repository.UpdateOpportunityAsync(opp);

            // 1. Business Activity
            await _repository.AddActivityAsync(new Activity
            {
                OpportunityId = opp.Id,
                Type = ActivityType.StageChanged,
                Title = $"Stage Changed: {oldStage} -> {request.Stage}",
                Description = string.IsNullOrWhiteSpace(request.ReasonOrNote) 
                    ? $"Opportunity moved to {request.Stage} with probability {opp.Probability * 100}%." 
                    : request.ReasonOrNote,
                PerformedByName = User.Identity?.Name ?? "User"
            });

            // 2. Security Audit Event
            var eventType = request.Stage == OpportunityStage.ClosedWon 
                ? AuditEventType.DealClosedWon 
                : request.Stage == OpportunityStage.ClosedLost 
                    ? AuditEventType.DealClosedLost 
                    : AuditEventType.StageChanged;

            await _repository.LogAuditEventAsync(new AuditEvent
            {
                WorkspaceId = targetWorkspaceId,
                EntityType = nameof(Opportunity),
                EntityId = opp.Id,
                EventType = eventType,
                ActionName = $"Opportunity Stage: {oldStage} -> {request.Stage}",
                Description = $"Stage changed from {oldStage} to {request.Stage}. Note: {request.ReasonOrNote}",
                OldStateJson = $"{{\"stage\": \"{oldStage}\"}}",
                NewStateJson = $"{{\"stage\": \"{request.Stage}\", \"probability\": {opp.Probability}}}",
                PerformedByUserId = userId,
                PerformedByName = User.Identity?.Name ?? "User"
            });

            return Ok(MapToDto(opp));
        }

        [HttpGet("{id}/activities")]
        public async Task<ActionResult<IEnumerable<ActivityDto>>> GetActivities(Guid id, [FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var opp = await _repository.GetOpportunityByIdAsync(targetWorkspaceId, id);
            if (opp == null)
            {
                return NotFound(new { message = "Opportunity not found in authorized workspace." });
            }

            var activities = await _repository.GetActivitiesByOpportunityIdAsync(id);
            return Ok(activities.Select(a => new ActivityDto
            {
                Id = a.Id,
                OpportunityId = a.OpportunityId,
                Type = a.Type.ToString(),
                Title = a.Title,
                Description = a.Description,
                PerformedByName = a.PerformedByName,
                CreatedAt = a.CreatedAt
            }));
        }

        [HttpGet("{id}/audits")]
        public async Task<ActionResult<IEnumerable<AuditEvent>>> GetAudits(Guid id, [FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(workspaceId);
            var opp = await _repository.GetOpportunityByIdAsync(targetWorkspaceId, id);
            if (opp == null)
            {
                return NotFound(new { message = "Opportunity not found in authorized workspace." });
            }

            var audits = await _repository.GetAuditEventsForEntityAsync(nameof(Opportunity), id);
            return Ok(audits);
        }

        private static OpportunityDto MapToDto(Opportunity o) => new OpportunityDto
        {
            Id = o.Id,
            WorkspaceId = o.WorkspaceId,
            LeadId = o.LeadId,
            LeadContactName = o.Lead?.ContactName ?? string.Empty,
            LeadCompanyName = o.Lead?.CompanyName ?? string.Empty,
            Title = o.Title,
            EstimatedValue = o.EstimatedValue,
            Currency = o.Currency,
            Stage = o.Stage.ToString(),
            Probability = o.Probability,
            ExpectedCloseDate = o.ExpectedCloseDate,
            PrimaryConcern = o.PrimaryConcern,
            NextStep = o.NextStep,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,
            RecentActivities = o.Activities?.Select(a => new ActivityDto
            {
                Id = a.Id,
                OpportunityId = a.OpportunityId,
                Type = a.Type.ToString(),
                Title = a.Title,
                Description = a.Description,
                PerformedByName = a.PerformedByName,
                CreatedAt = a.CreatedAt
            }).ToList() ?? new List<ActivityDto>()
        };
    }
}
