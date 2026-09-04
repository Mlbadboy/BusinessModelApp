using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Decisions;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Strategy;
using BusinessModelApp.Core.Missions;
using BusinessModelApp.Core.Objectives;
using BusinessModelApp.Core.Strategy;
using BusinessModelApp.Core.WorldModel;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Api.Controllers
{
    public class IngestObjectiveRequest
    {
        public string Prompt { get; set; } = string.Empty;
        public Guid? WorkspaceId { get; set; }
    }

    public class SelectStrategyRequest
    {
        public Guid StrategyId { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ObjectivesController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IObjectiveEngine _objectiveEngine;
        private readonly ICompanyWorldModel _worldModel;
        private readonly IStrategyEngine _strategyEngine;
        private readonly IDecisionEngine _decisionEngine;
        private readonly IDurableMissionOrchestrator _missionOrchestrator;
        private readonly IUserContextService _userContext;
        private readonly ILogger<ObjectivesController> _logger;

        public ObjectivesController(
            AppDbContext dbContext,
            IObjectiveEngine objectiveEngine,
            ICompanyWorldModel worldModel,
            IStrategyEngine strategyEngine,
            IDecisionEngine decisionEngine,
            IDurableMissionOrchestrator missionOrchestrator,
            IUserContextService userContext,
            ILogger<ObjectivesController> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _objectiveEngine = objectiveEngine ?? throw new ArgumentNullException(nameof(objectiveEngine));
            _worldModel = worldModel ?? throw new ArgumentNullException(nameof(worldModel));
            _strategyEngine = strategyEngine ?? throw new ArgumentNullException(nameof(strategyEngine));
            _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));
            _missionOrchestrator = missionOrchestrator ?? throw new ArgumentNullException(nameof(missionOrchestrator));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        public async Task<IActionResult> CreateObjectiveFromPrompt([FromBody] IngestObjectiveRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest(new { message = "Executive objective prompt cannot be empty." });
            }

            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(request.WorkspaceId, ct);

            // 1. Ingest prompt into structured objective
            var objective = await _objectiveEngine.IngestCeoPromptAsync(request.Prompt, workspaceId, ct);

            // 2. Capture verified truth snapshot from the Company World Model
            var snapshot = await _worldModel.CaptureVerifiedSnapshotAsync(workspaceId, ct);

            // 3. Reconcile objective progress and calculate real gap
            objective = await _objectiveEngine.ReconcileObjectiveProgressAsync(objective.Id, snapshot, ct);

            // 4. Generate candidate strategies (A, B, C) with deterministic funnel & explicit assumption labels
            var strategies = await _strategyEngine.GenerateStrategyCandidatesAsync(objective, snapshot, ct);

            return Ok(new
            {
                objective,
                snapshot = new
                {
                    snapshot.Id,
                    snapshot.RevenueBaselineState,
                    Revenue = snapshot.VerifiedRevenueINR,
                    Cash = snapshot.VerifiedSettledCashINR,
                    Pipeline = snapshot.ActivePipelineINR,
                    QualifiedPipeline = snapshot.QualifiedPipelineINR,
                    VerifiedProspects = snapshot.VerifiedProspectsCount,
                    AvailableSlots = snapshot.AvailableDeliverySlots,
                    snapshot.SnapshotDigestHash
                },
                strategies = strategies.Select(s => new
                {
                    s.Id,
                    s.StrategyName,
                    s.TargetMarket,
                    s.TargetACV_INR,
                    s.ExpectedWinRate,
                    s.RequiredClosedDeals,
                    s.RequiredPipelineINR,
                    s.RequiredMeetingsCount,
                    s.RequiredProspectsCount,
                    s.DeliveryCapacitySlotsRequired,
                    s.ExpectedGrossMarginPercent,
                    s.FeasibilityState,
                    s.FeasibilityReason,
                    s.IsRecommended,
                    Assumptions = System.Text.Json.JsonSerializer.Deserialize<List<StrategicAssumption>>(s.AssumptionsJson)
                })
            });
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveObjective(CancellationToken ct)
        {
            var workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var objective = await _dbContext.BusinessObjectives
                .Where(o => o.WorkspaceId == workspaceId && !o.IsDeleted && o.Status == ObjectiveStatus.Active)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (objective == null)
            {
                return Ok(new { activeObjective = (object?)null });
            }

            var strategies = await _dbContext.BusinessStrategies
                .Where(s => s.ObjectiveId == objective.Id && !s.IsDeleted)
                .ToListAsync(ct);

            var activeDecision = await _dbContext.DecisionRecords
                .Where(d => d.ObjectiveId == objective.Id && d.WorkspaceId == workspaceId)
                .OrderByDescending(d => d.DecidedAt)
                .FirstOrDefaultAsync(ct);

            var activeMission = await _dbContext.DurableMissions
                .Include(m => m.Checkpoints)
                .Where(m => m.ObjectiveId == objective.Id && m.WorkspaceId == workspaceId && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync(ct);

            return Ok(new
            {
                activeObjective = objective,
                strategies,
                activeDecision,
                activeMission
            });
        }

        [HttpPost("{id}/select-strategy")]
        [HttpPost("{id}/strategies/{strategyId}/select")]
        public async Task<IActionResult> SelectStrategy(Guid id, [FromBody] SelectStrategyRequest? request, Guid? strategyId, CancellationToken ct)
        {
            var workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var targetStrategyId = strategyId ?? request?.StrategyId ?? Guid.Empty;
            var objective = await _dbContext.BusinessObjectives.FirstOrDefaultAsync(o => o.Id == id && o.WorkspaceId == workspaceId, ct);
            if (objective == null) return NotFound(new { message = $"Objective {id} not found." });

            var strategies = await _dbContext.BusinessStrategies.Where(s => s.ObjectiveId == id).ToListAsync(ct);
            var selected = strategies.FirstOrDefault(s => s.Id == targetStrategyId);
            if (selected == null) return NotFound(new { message = $"Strategy {targetStrategyId} not found." });

            // Capture snapshot
            var snapshot = await _worldModel.CaptureVerifiedSnapshotAsync(objective.WorkspaceId, ct);

            // Commit Immutable DecisionRecord
            var decision = await _decisionEngine.CommitStrategyDecisionAsync(objective, selected, strategies, snapshot, ct: ct);

            // Formulate Durable Mission Plan
            var mission = await _missionOrchestrator.CreateDurableMissionPlanAsync(decision, selected, ct);

            return Ok(new
            {
                message = "Strategy selected and durable mission formulated.",
                decision,
                mission
            });
        }
    }
}
