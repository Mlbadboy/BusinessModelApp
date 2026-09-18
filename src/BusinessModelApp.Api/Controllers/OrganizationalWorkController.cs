using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;
using BusinessModelApp.Infrastructure.Runtime.Organizational;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrganizationalWorkController : ControllerBase
    {
        private readonly IOrganizationalWorkRepository _repository;
        private readonly OrganizationalWorkOrchestrator _orchestrator;
        private readonly IAutonomousWorkManager _workManager;
        private readonly IWorkManagerRunStore _runStore;
        private readonly IWorkPortfolioPrioritizer _prioritizer;
        private readonly IGovernanceQueueManager _governanceQueueManager;

        public OrganizationalWorkController(
            IOrganizationalWorkRepository repository,
            OrganizationalWorkOrchestrator orchestrator,
            IAutonomousWorkManager workManager,
            IWorkManagerRunStore runStore,
            IWorkPortfolioPrioritizer prioritizer,
            IGovernanceQueueManager governanceQueueManager)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _workManager = workManager ?? throw new ArgumentNullException(nameof(workManager));
            _runStore = runStore ?? throw new ArgumentNullException(nameof(runStore));
            _prioritizer = prioritizer ?? throw new ArgumentNullException(nameof(prioritizer));
            _governanceQueueManager = governanceQueueManager ?? throw new ArgumentNullException(nameof(governanceQueueManager));
        }

        private string GetTenantId() =>
            Request.Headers.TryGetValue("X-Tenant-ID", out var tenant) && !string.IsNullOrWhiteSpace(tenant)
                ? tenant.ToString()
                : "DEFAULT-TENANT";

        [HttpPost("responsibilities")]
        public async Task<IActionResult> RegisterResponsibility([FromBody] OrganizationalResponsibility responsibility, CancellationToken ct)
        {
            if (responsibility == null) return BadRequest("Responsibility cannot be null.");
            responsibility.TenantId = GetTenantId();
            if (string.IsNullOrWhiteSpace(responsibility.ResponsibilityId))
            {
                responsibility.ResponsibilityId = $"RESP-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            }
            await _repository.SaveResponsibilityAsync(responsibility, ct);
            return Ok(responsibility);
        }

        [HttpGet("responsibilities")]
        public async Task<IActionResult> ListResponsibilities(CancellationToken ct)
        {
            var list = await _repository.ListResponsibilitiesAsync(GetTenantId(), ct);
            return Ok(list);
        }

        [HttpPost("proposals")]
        public async Task<IActionResult> SubmitProposal([FromBody] WorkProposal proposal, CancellationToken ct)
        {
            if (proposal == null) return BadRequest("Proposal cannot be null.");
            proposal.TenantId = GetTenantId();
            var result = await _orchestrator.SubmitProposalAsync(proposal, ct);
            return Ok(result);
        }

        [HttpGet("proposals")]
        public async Task<IActionResult> ListProposals(CancellationToken ct)
        {
            var list = await _repository.ListProposalsAsync(GetTenantId(), ct);
            return Ok(list);
        }

        [HttpPost("admit/{proposalId}")]
        public async Task<IActionResult> AdmitProposal(string proposalId, CancellationToken ct)
        {
            var (admitted, reason, item) = await _orchestrator.AdmitProposalAsync(GetTenantId(), proposalId, ct);
            if (!admitted)
            {
                return BadRequest(new { Admitted = false, Reason = reason });
            }
            return Ok(new { Admitted = true, WorkItem = item });
        }

        [HttpGet("work")]
        public async Task<IActionResult> ListWork([FromQuery] WorkState? state, CancellationToken ct)
        {
            var list = await _repository.ListWorkItemsAsync(GetTenantId(), state, ct);
            return Ok(list);
        }

        [HttpGet("work/{workId}")]
        public async Task<IActionResult> GetWork(string workId, CancellationToken ct)
        {
            var item = await _repository.GetWorkItemAsync(GetTenantId(), workId, ct);
            if (item == null) return NotFound($"Work item '{workId}' not found.");
            return Ok(item);
        }

        [HttpPost("work/{workId}/transition")]
        public async Task<IActionResult> TransitionWork(
            string workId,
            [FromBody] TransitionRequest request,
            CancellationToken ct)
        {
            if (request == null) return BadRequest("Transition request cannot be null.");
            var (success, error, item) = await _orchestrator.TransitionWorkStateAsync(
                GetTenantId(),
                workId,
                request.TargetState,
                request.GovernanceApprovalActor,
                request.VerificationEvidenceHash,
                ct);

            if (!success)
            {
                return BadRequest(new { Success = false, Error = error, Item = item });
            }

            return Ok(new { Success = true, Item = item });
        }

        [HttpGet("work/{workId}/lineage")]
        public async Task<IActionResult> GetLineage(string workId, CancellationToken ct)
        {
            var lineage = await _orchestrator.GetLineageAsync(GetTenantId(), workId, ct);
            if (lineage == null) return NotFound($"Lineage for work item '{workId}' not found.");
            return Ok(lineage);
        }

        [HttpPost("work/{workId}/outcome")]
        public async Task<IActionResult> RecordOutcome(
            string workId,
            [FromBody] OutcomeRequest request,
            CancellationToken ct)
        {
            if (request == null) return BadRequest("Outcome request cannot be null.");
            try
            {
                var outcome = await _orchestrator.RecordOutcomeAsync(
                    GetTenantId(),
                    workId,
                    request.ClaimedSummary,
                    request.ExpectedMetrics,
                    request.ActualMetrics,
                    request.EvidenceSha256,
                    request.VerifierActor,
                    ct);

                return Ok(outcome);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Error = ex.Message });
            }
        }

        [HttpPost("commitments/evaluate")]
        public async Task<IActionResult> EvaluateCommitments(CancellationToken ct)
        {
            var evaluated = await _orchestrator.EvaluateCommitmentsAsync(GetTenantId(), ct);
            return Ok(evaluated);
        }

        // --- Batch 3.9.1 Autonomous Work Manager Endpoints ---

        [HttpPost("manager/cycle")]
        public async Task<IActionResult> ExecuteManagerCycle([FromBody] CycleRequest? request, CancellationToken ct)
        {
            var budget = request?.Budget;
            var triggerType = request?.TriggerType ?? ManagerTriggerType.ManualTrigger;
            var result = await _workManager.ExecuteCycleAsync(GetTenantId(), triggerType, request?.TriggerId, budget, ct);
            return Ok(result);
        }

        [HttpGet("manager/runs")]
        public async Task<IActionResult> ListManagerRuns(CancellationToken ct)
        {
            var runs = await _runStore.ListRunsAsync(GetTenantId(), ct);
            return Ok(runs);
        }

        [HttpGet("governance/queue")]
        public async Task<IActionResult> ListGovernanceQueue([FromQuery] GovernanceQueueStatus? status, CancellationToken ct)
        {
            var queue = await _runStore.ListGovernanceQueueAsync(GetTenantId(), status, ct);
            return Ok(queue);
        }

        [HttpPost("governance/decide")]
        public async Task<IActionResult> RecordGovernanceDecision([FromBody] GovernanceDecisionRequest request, CancellationToken ct)
        {
            if (request == null) return BadRequest("Governance decision request cannot be null.");
            var (success, error) = await _governanceQueueManager.RecordGovernanceDecisionAsync(
                GetTenantId(),
                request.QueueItemId,
                request.Approved,
                request.Actor,
                request.Note,
                ct);

            if (!success)
            {
                return BadRequest(new { Success = false, Error = error });
            }

            return Ok(new { Success = true });
        }

        [HttpGet("portfolio/ranked")]
        public async Task<IActionResult> GetRankedPortfolio([FromQuery] int maxItems = 50, CancellationToken ct = default)
        {
            var activeItems = await _repository.ListWorkItemsAsync(GetTenantId(), null, ct);
            var ranked = await _prioritizer.RankPortfolioAsync(GetTenantId(), activeItems, maxItems, ct);
            return Ok(ranked);
        }
    }

    public class CycleRequest
    {
        public ManagerTriggerType TriggerType { get; set; } = ManagerTriggerType.ManualTrigger;
        public string? TriggerId { get; set; }
        public WorkManagerBudget? Budget { get; set; }
    }

    public class GovernanceDecisionRequest
    {
        public string QueueItemId { get; set; } = string.Empty;
        public bool Approved { get; set; }
        public string Actor { get; set; } = string.Empty;
        public string? Note { get; set; }
    }

    public class TransitionRequest
    {
        public WorkState TargetState { get; set; }
        public string? GovernanceApprovalActor { get; set; }
        public string? VerificationEvidenceHash { get; set; }
    }

    public class OutcomeRequest
    {
        public string ClaimedSummary { get; set; } = string.Empty;
        public Dictionary<string, double> ExpectedMetrics { get; set; } = new();
        public Dictionary<string, double> ActualMetrics { get; set; } = new();
        public string EvidenceSha256 { get; set; } = string.Empty;
        public string VerifierActor { get; set; } = string.Empty;
    }
}
