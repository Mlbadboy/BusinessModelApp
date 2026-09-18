using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Multimodal;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Multimodal;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/computer")]
    public class ComputerController : ControllerBase
    {
        private readonly IComputerOrchestratorService _orchestrator;
        private readonly IComputerSessionManager _sessionManager;
        private readonly IEnvironmentModel _environmentModel;
        private readonly IComputerSafetyGuard _safetyGuard;
        private readonly IActionRiskEvaluator _riskEvaluator;
        private readonly IComputerProvenanceService _provenanceService;
        private readonly IComputerVerificationService _verificationService;

        public ComputerController(
            IComputerOrchestratorService orchestrator,
            IComputerSessionManager sessionManager,
            IEnvironmentModel environmentModel,
            IComputerSafetyGuard safetyGuard,
            IActionRiskEvaluator riskEvaluator,
            IComputerProvenanceService provenanceService,
            IComputerVerificationService verificationService)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _environmentModel = environmentModel ?? throw new ArgumentNullException(nameof(environmentModel));
            _safetyGuard = safetyGuard ?? throw new ArgumentNullException(nameof(safetyGuard));
            _riskEvaluator = riskEvaluator ?? throw new ArgumentNullException(nameof(riskEvaluator));
            _provenanceService = provenanceService ?? throw new ArgumentNullException(nameof(provenanceService));
            _verificationService = verificationService ?? throw new ArgumentNullException(nameof(verificationService));
        }

        public record CreateSessionRequest(string ApplicationContext, string TargetGoal);
        public record ObserveRequest(string Application, string Window, string? Url);
        public record ProposeActionRequest(string SessionId, string SnapshotId, string IntentId, ProposedActionType ActionType, string? TargetElementId, int? CoordinateX, int? CoordinateY, Dictionary<string, string>? Parameters);
        public record EvaluateActionRequest(ProposedActionType ActionType, string? TargetElementRole, string? CommandName, string? Url);
        public record AdmitActionRequest(string SessionId, string ProposalId);
        public record PauseSessionRequest(string Reason);
        public record ResumeSessionRequest(string SupervisorId);

        private string GetTenantId()
        {
            return Request.Headers.TryGetValue("X-Tenant-Id", out var val) ? val.ToString() : "tenant-default";
        }

        [HttpPost("sessions")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            var tenantId = GetTenantId();
            var session = await _orchestrator.StartSessionAsync(tenantId, request.ApplicationContext, request.TargetGoal);
            return Ok(session);
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> ListSessions([FromQuery] int limit = 50)
        {
            var tenantId = GetTenantId();
            var sessions = await _sessionManager.ListSessionsAsync(tenantId, limit);
            return Ok(sessions);
        }

        [HttpGet("sessions/{id}")]
        public async Task<IActionResult> GetSession(string id)
        {
            var tenantId = GetTenantId();
            var session = await _sessionManager.GetSessionAsync(tenantId, id);
            if (session == null) return NotFound($"Session {id} not found");
            return Ok(session);
        }

        [HttpPost("sessions/{id}/observe")]
        public async Task<IActionResult> Observe(string id, [FromBody] ObserveRequest request)
        {
            var tenantId = GetTenantId();
            var snapshot = await _orchestrator.ObserveAsync(tenantId, id, request.Application, request.Window, request.Url);
            return Ok(snapshot);
        }

        [HttpGet("sessions/{id}/environment")]
        public async Task<IActionResult> GetEnvironment(string id)
        {
            var tenantId = GetTenantId();
            var session = await _sessionManager.GetSessionAsync(tenantId, id);
            if (session == null) return NotFound($"Session {id} not found");
            if (string.IsNullOrEmpty(session.ActiveSnapshotId)) return BadRequest("No active environment snapshot for session");

            var snapshot = await _environmentModel.GetSnapshotAsync(tenantId, session.ActiveSnapshotId);
            return Ok(snapshot);
        }

        [HttpPost("actions/propose")]
        public async Task<IActionResult> ProposeAction([FromBody] ProposeActionRequest request)
        {
            var tenantId = GetTenantId();
            (int X, int Y)? coords = (request.CoordinateX.HasValue && request.CoordinateY.HasValue)
                ? (request.CoordinateX.Value, request.CoordinateY.Value)
                : null;

            var proposal = await _orchestrator.ProposeActionAsync(
                tenantId,
                request.SessionId,
                request.SnapshotId,
                request.IntentId,
                request.ActionType,
                request.TargetElementId,
                coords,
                request.Parameters
            );

            return Ok(proposal);
        }

        [HttpPost("actions/evaluate")]
        public IActionResult EvaluateAction([FromBody] EvaluateActionRequest request)
        {
            var riskTier = _riskEvaluator.EvaluateRisk(request.ActionType, request.TargetElementRole, request.CommandName, request.Url);
            var requiresApproval = _riskEvaluator.RequiresHumanApproval(riskTier);
            return Ok(new
            {
                RiskTier = riskTier.ToString(),
                RequiresHumanApproval = requiresApproval,
                Invariant = ConstitutionalInvariantI38.InvariantId
            });
        }

        [HttpPost("actions/admit")]
        public async Task<IActionResult> AdmitAction([FromBody] AdmitActionRequest request)
        {
            var tenantId = GetTenantId();
            var result = await _orchestrator.AdmitAndDispatchProposalAsync(tenantId, request.SessionId, request.ProposalId);
            return Ok(result);
        }

        [HttpPost("sessions/{id}/pause")]
        public async Task<IActionResult> PauseSession(string id, [FromBody] PauseSessionRequest request)
        {
            var tenantId = GetTenantId();
            var session = await _orchestrator.PauseAsync(tenantId, id, request.Reason);
            return Ok(session);
        }

        [HttpPost("sessions/{id}/resume")]
        public async Task<IActionResult> ResumeSession(string id, [FromBody] ResumeSessionRequest request)
        {
            var tenantId = GetTenantId();
            var session = await _orchestrator.ResumeAsync(tenantId, id, request.SupervisorId);
            return Ok(session);
        }

        [HttpGet("capabilities")]
        public IActionResult GetCapabilities()
        {
            return Ok(new
            {
                Invariant = ConstitutionalInvariantI38.InvariantId,
                Axiom = ConstitutionalInvariantI38.Axiom,
                Capabilities = new[]
                {
                    ComputerCapabilities.BrowserNavigate, ComputerCapabilities.BrowserClick, ComputerCapabilities.BrowserType,
                    ComputerCapabilities.BrowserSelect, ComputerCapabilities.BrowserUpload, ComputerCapabilities.BrowserDownload,
                    ComputerCapabilities.DesktopOpenApplication, ComputerCapabilities.DesktopClick, ComputerCapabilities.DesktopType,
                    ComputerCapabilities.KeyboardPress, ComputerCapabilities.MouseMove, ComputerCapabilities.ClipboardRead,
                    ComputerCapabilities.ClipboardWrite, ComputerCapabilities.ScreenCapture, ComputerCapabilities.AudioRecord,
                    ComputerCapabilities.DocumentRead
                }
            });
        }

        [HttpGet("providers")]
        public IActionResult GetProviders()
        {
            return Ok(new
            {
                Providers = new[] { "GovernedBrowserWorker", "GovernedDesktopWorker", "McpComputerAdapter" },
                ExecutionAuthority = "Batch 6 Execution Firewall Sovereign",
                WorkerFabric = "Phase 3.6 Governed Worker Fabric"
            });
        }

        [HttpGet("provenance/{id}")]
        public async Task<IActionResult> GetProvenance(string id)
        {
            var tenantId = GetTenantId();
            var trace = await _provenanceService.GetTraceAsync(tenantId, id);
            if (trace == null) return NotFound($"Trace {id} not found");
            return Ok(trace);
        }
    }
}
