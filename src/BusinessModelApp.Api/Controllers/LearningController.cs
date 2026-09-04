using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Learning;
using BusinessModelApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/learning")]
    [Authorize]
    public class LearningController : ControllerBase
    {
        private readonly IInstitutionalLearningService _learningService;
        private readonly IUserContextService _userContext;
        private readonly ILogger<LearningController> _logger;

        public LearningController(
            IInstitutionalLearningService learningService,
            IUserContextService userContext,
            ILogger<LearningController> logger)
        {
            _learningService = learningService ?? throw new ArgumentNullException(nameof(learningService));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveLearning([FromQuery] LearningTier? tier, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var records = await _learningService.GetActiveLearningAsync(workspaceId, tier, ct);
            return Ok(records);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetLearningRecord(Guid id, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var record = await _learningService.GetLearningRecordAsync(workspaceId, id, ct);
            if (record == null)
                return NotFound(new { message = $"Learning record {id} not found." });

            return Ok(record);
        }

        [HttpGet("{id:guid}/explanation")]
        public async Task<IActionResult> ExplainLearning(Guid id, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            try
            {
                var explanation = await _learningService.ExplainLearningAsync(workspaceId, id, ct);
                return Ok(explanation);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Learning record {id} not found." });
            }
        }

        public class CreateCandidateRequest
        {
            public Guid MissionId { get; set; }
            public string Statement { get; set; } = string.Empty;
            public string Context { get; set; } = string.Empty;
            public string Domain { get; set; } = string.Empty;
            public double Confidence { get; set; } = 0.50;
            public double CausalConfidence { get; set; } = 0.40;
        }

        [HttpPost("candidate")]
        public async Task<IActionResult> CreateCandidate([FromBody] CreateCandidateRequest request, CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Statement))
                return BadRequest(new { message = "Statement is required." });

            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var record = await _learningService.GenerateLearningCandidateAsync(
                workspaceId,
                request.MissionId,
                request.Statement,
                request.Context,
                request.Domain,
                request.Confidence,
                request.CausalConfidence,
                ct);

            return Ok(record);
        }

        [HttpPost("{id:guid}/promote")]
        public async Task<IActionResult> PromoteLearning(Guid id, [FromQuery] bool isDirectAiCall = false, CancellationToken ct = default)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            try
            {
                var record = await _learningService.ValidateAndPromoteLearningAsync(workspaceId, id, isDirectAiCall, ct);
                return Ok(record);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = "PromotionBlocked", message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Learning record {id} not found." });
            }
        }

        [HttpGet("contradictions")]
        public async Task<IActionResult> GetContradictions(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var contradictions = await _learningService.DetectContradictionsAsync(workspaceId, ct);
            return Ok(contradictions);
        }

        [HttpGet("context")]
        public async Task<IActionResult> RetrieveContextualLearning([FromQuery] string? domain, [FromQuery] string? query, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var items = await _learningService.RetrieveContextualLearningAsync(workspaceId, domain ?? string.Empty, query ?? string.Empty, ct);
            return Ok(items);
        }

        [HttpPost("outcome")]
        public async Task<IActionResult> RecordOutcome([FromBody] OutcomeRecord outcome, CancellationToken ct)
        {
            if (outcome == null)
                return BadRequest(new { message = "Outcome payload required." });

            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var record = await _learningService.RecordMissionOutcomeAsync(workspaceId, outcome, ct);
            return Ok(record);
        }

        public class DiagnoseFailureRequest
        {
            public Guid MissionId { get; set; }
            public FailureRootCause RootCause { get; set; } = FailureRootCause.Unknown;
            public string Diagnosis { get; set; } = string.Empty;
            public string Impact { get; set; } = string.Empty;
        }

        [HttpPost("failure")]
        public async Task<IActionResult> DiagnoseFailure([FromBody] DiagnoseFailureRequest request, CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Diagnosis))
                return BadRequest(new { message = "Diagnosis statement required." });

            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var failure = await _learningService.DiagnoseFailureAsync(
                workspaceId,
                request.MissionId,
                request.RootCause,
                request.Diagnosis,
                request.Impact,
                ct);

            return Ok(failure);
        }

        public class RecordCorrectionRequest
        {
            public Guid FailureRecordId { get; set; }
            public string ActionTaken { get; set; } = string.Empty;
            public bool WasSuccessful { get; set; }
            public string OutcomeSummary { get; set; } = string.Empty;
        }

        [HttpPost("correction")]
        public async Task<IActionResult> RecordCorrection([FromBody] RecordCorrectionRequest request, CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ActionTaken))
                return BadRequest(new { message = "ActionTaken is required." });

            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            try
            {
                var correction = await _learningService.RecordCorrectionAsync(
                    workspaceId,
                    request.FailureRecordId,
                    request.ActionTaken,
                    request.WasSuccessful,
                    request.OutcomeSummary,
                    ct);

                return Ok(correction);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"FailureRecord {request.FailureRecordId} not found." });
            }
        }

        [HttpGet("failing-assumptions")]
        public async Task<IActionResult> GetFailingAssumptions(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var failures = await _learningService.GetFailingAssumptionsAsync(workspaceId, ct);
            return Ok(failures);
        }

        [HttpPost("experiments")]
        public async Task<IActionResult> ProposeExperiment([FromBody] LearningExperiment experiment, CancellationToken ct)
        {
            if (experiment == null || string.IsNullOrWhiteSpace(experiment.Hypothesis))
                return BadRequest(new { message = "Hypothesis is required." });

            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var created = await _learningService.ProposeExperimentAsync(workspaceId, experiment, ct);
            return Ok(created);
        }

        public class CompleteExperimentRequest
        {
            public decimal ActualValue { get; set; }
            public string Conclusion { get; set; } = string.Empty;
        }

        [HttpPost("experiments/{id:guid}/complete")]
        public async Task<IActionResult> CompleteExperiment(Guid id, [FromBody] CompleteExperimentRequest request, CancellationToken ct)
        {
            if (request == null)
                return BadRequest(new { message = "ActualValue and Conclusion required." });

            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            try
            {
                var completed = await _learningService.RecordExperimentResultAsync(workspaceId, id, request.ActualValue, request.Conclusion, ct);
                return Ok(completed);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Experiment {id} not found." });
            }
        }
    }
}
