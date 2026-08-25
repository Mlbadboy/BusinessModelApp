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
    public class OpportunitiesController : ControllerBase
    {
        private readonly ICommercialRepository _repository;

        public OpportunitiesController(ICommercialRepository repository)
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

            return Guid.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OpportunityDto>>> GetOpportunities([FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = workspaceId ?? GetWorkspaceId();
            var opportunities = await _repository.GetOpportunitiesByWorkspaceIdAsync(targetWorkspaceId);

            return Ok(opportunities.Select(MapToDto));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OpportunityDto>> GetOpportunityById(Guid id)
        {
            var opp = await _repository.GetOpportunityByIdAsync(id);
            if (opp == null)
            {
                return NotFound(new { message = "Opportunity not found." });
            }

            return Ok(MapToDto(opp));
        }

        [HttpPost]
        public async Task<ActionResult<OpportunityDto>> CreateOpportunity([FromBody] CreateOpportunityDto request, [FromQuery] Guid? workspaceId)
        {
            var targetWorkspaceId = workspaceId ?? GetWorkspaceId();

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

            await _repository.AddActivityAsync(new Activity
            {
                OpportunityId = created.Id,
                Type = ActivityType.NoteAdded,
                Title = "Opportunity Created",
                Description = $"Created opportunity: {created.Title} with value {created.Currency} {created.EstimatedValue:N0}",
                PerformedByName = User.Identity?.Name ?? "User"
            });

            return CreatedAtAction(nameof(GetOpportunityById), new { id = created.Id }, MapToDto(created));
        }

        [HttpPatch("{id}/stage")]
        public async Task<ActionResult<OpportunityDto>> UpdateStage(Guid id, [FromBody] UpdateOpportunityStageDto request)
        {
            var opp = await _repository.GetOpportunityByIdAsync(id);
            if (opp == null)
            {
                return NotFound(new { message = "Opportunity not found." });
            }

            var oldStage = opp.Stage;
            opp.AdvanceStage(request.Stage);
            await _repository.UpdateOpportunityAsync(opp);

            // Audit activity
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

            return Ok(MapToDto(opp));
        }

        [HttpGet("{id}/activities")]
        public async Task<ActionResult<IEnumerable<ActivityDto>>> GetActivities(Guid id)
        {
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

        [HttpPost("{id}/activities")]
        public async Task<ActionResult<ActivityDto>> AddActivity(Guid id, [FromBody] CreateActivityDto request)
        {
            var opp = await _repository.GetOpportunityByIdAsync(id);
            if (opp == null)
            {
                return NotFound(new { message = "Opportunity not found." });
            }

            var activity = new Activity
            {
                OpportunityId = id,
                Type = request.Type,
                Title = request.Title,
                Description = request.Description,
                PerformedByName = User.Identity?.Name ?? "User"
            };

            var created = await _repository.AddActivityAsync(activity);
            return Ok(new ActivityDto
            {
                Id = created.Id,
                OpportunityId = created.OpportunityId,
                Type = created.Type.ToString(),
                Title = created.Title,
                Description = created.Description,
                PerformedByName = created.PerformedByName,
                CreatedAt = created.CreatedAt
            });
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
