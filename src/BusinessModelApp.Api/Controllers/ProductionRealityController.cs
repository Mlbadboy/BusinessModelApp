using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BusinessModelApp.Core.Domain.Runtime.Reality;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Core.Interfaces.Runtime.Reality;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/reality")]
    public class ProductionRealityController : ControllerBase
    {
        private readonly IProductionRealityService _realityService;
        private readonly IHumanApprovalManager _approvalManager;
        private readonly IWorkControlCenter _workControlCenter;
        private readonly IValueRealizationEngine _valueRealization;
        private readonly BusinessModelApp.Core.Interfaces.Runtime.Reality.IConnectorHealthService _connectorHealth;
        private readonly IUserContextService _userContext;
        private readonly ILogger<ProductionRealityController> _logger;

        public ProductionRealityController(
            IProductionRealityService realityService,
            IHumanApprovalManager approvalManager,
            IWorkControlCenter workControlCenter,
            IValueRealizationEngine valueRealization,
            BusinessModelApp.Core.Interfaces.Runtime.Reality.IConnectorHealthService connectorHealth,
            IUserContextService userContext,
            ILogger<ProductionRealityController> logger)
        {
            _realityService = realityService;
            _approvalManager = approvalManager;
            _workControlCenter = workControlCenter;
            _valueRealization = valueRealization;
            _connectorHealth = connectorHealth;
            _userContext = userContext;
            _logger = logger;
        }

        private async Task<string> GetTenantIdAsync(CancellationToken ct)
        {
            try
            {
                var workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
                return workspaceId.ToString();
            }
            catch
            {
                return "default-tenant";
            }
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetSystemOverview(CancellationToken ct)
        {
            var tenantId = await GetTenantIdAsync(ct);
            var overview = _realityService.GetSystemOverview(tenantId);
            var connectors = await _connectorHealth.GetConnectorHealthAsync(tenantId);
            var work = await _workControlCenter.GetWorkProgressAsync(tenantId);
            var pendingApprovals = await _approvalManager.GetPendingApprovalsAsync(tenantId);

            var enriched = overview with
            {
                ActiveMissionsCount = work.ActiveMissions,
                PendingApprovalsCount = pendingApprovals.Count,
                Connectors = connectors
            };

            return Ok(enriched);
        }

        [HttpGet("connectors")]
        public async Task<IActionResult> GetConnectors(CancellationToken ct)
        {
            var tenantId = await GetTenantIdAsync(ct);
            var connectors = await _connectorHealth.GetConnectorHealthAsync(tenantId);
            return Ok(connectors);
        }

        [HttpGet("provenance/{metricKey}")]
        public async Task<IActionResult> GetProvenance(string metricKey, CancellationToken ct)
        {
            var tenantId = await GetTenantIdAsync(ct);
            var metric = _realityService.CreateMetric<string>(
                value: null,
                status: RealityStatus.Unknown,
                classification: TruthClassification.Fact,
                metricKey: metricKey,
                label: metricKey,
                unit: "unit",
                tenantId: tenantId,
                source: "DigitalTwin / Production Ledger",
                freshnessSla: TimeSpan.FromMinutes(5),
                epistemicRationale: "Epistemic discipline enforced: Metric value is UNKNOWN until verified by real telemetry."
            );
            return Ok(metric);
        }

        [HttpGet("control-center")]
        public async Task<IActionResult> GetControlCenter(CancellationToken ct)
        {
            var tenantId = await GetTenantIdAsync(ct);
            var work = await _workControlCenter.GetWorkProgressAsync(tenantId);
            var ledgers = await _workControlCenter.GetAllMissionLedgersAsync(tenantId);
            var pendingApprovals = await _approvalManager.GetPendingApprovalsAsync(tenantId);
            var values = await _valueRealization.GetAllValueRealizationsAsync(tenantId);

            return Ok(new
            {
                Work = work,
                MissionLedgers = ledgers,
                PendingApprovals = pendingApprovals,
                ValueRealizations = values
            });
        }

        [HttpGet("approvals")]
        public async Task<IActionResult> GetApprovals([FromQuery] bool history = false, CancellationToken ct = default)
        {
            var tenantId = await GetTenantIdAsync(ct);
            var result = history 
                ? await _approvalManager.GetApprovalHistoryAsync(tenantId)
                : await _approvalManager.GetPendingApprovalsAsync(tenantId);
            return Ok(result);
        }

        [HttpGet("approvals/{id}")]
        public async Task<IActionResult> GetApproval(string id, CancellationToken ct)
        {
            var tenantId = await GetTenantIdAsync(ct);
            var req = await _approvalManager.GetApprovalRequestAsync(tenantId, id);
            if (req == null) return NotFound(new { Message = $"Approval {id} not found." });
            var audit = await _approvalManager.GetAuditTrailAsync(tenantId, id);
            return Ok(new { Request = req, AuditTrail = audit });
        }

        public class DecideDto
        {
            public string Decision { get; set; } = "Approve"; // "Approve", "Reject", "RequestChanges"
            public string? Notes { get; set; }
            public string? ExpectedPayloadDigest { get; set; }
        }

        [HttpPost("approvals/{id}/decide")]
        public async Task<IActionResult> DecideApproval(string id, [FromBody] DecideDto dto, CancellationToken ct)
        {
            var tenantId = await GetTenantIdAsync(ct);
            var reviewerId = "CEO-Authorized";

            try
            {
                if (string.Equals(dto.Decision, "Approve", StringComparison.OrdinalIgnoreCase))
                {
                    var permit = await _approvalManager.ApproveRequestAsync(tenantId, id, reviewerId, dto.ExpectedPayloadDigest, dto.Notes);
                    return Ok(new { Status = "Approved", Permit = permit });
                }
                else if (string.Equals(dto.Decision, "Reject", StringComparison.OrdinalIgnoreCase))
                {
                    var rejected = await _approvalManager.RejectRequestAsync(tenantId, id, reviewerId, dto.Notes ?? "Rejected by reviewer.");
                    return Ok(new { Status = "Rejected", Request = rejected });
                }
                else if (string.Equals(dto.Decision, "RequestChanges", StringComparison.OrdinalIgnoreCase))
                {
                    var updated = await _approvalManager.RequestChangesAsync(tenantId, id, reviewerId, dto.Notes ?? "Changes requested.");
                    return Ok(new { Status = "ChangesRequested", Request = updated });
                }
                else
                {
                    return BadRequest(new { Message = $"Unknown decision type: {dto.Decision}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Approval decision failed for {ApprovalId}", id);
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("value")]
        public async Task<IActionResult> GetValueRealizations(CancellationToken ct)
        {
            var tenantId = await GetTenantIdAsync(ct);
            var values = await _valueRealization.GetAllValueRealizationsAsync(tenantId);
            return Ok(values);
        }

        [HttpGet("ledgers/{missionId}")]
        public async Task<IActionResult> GetMissionLedger(string missionId, CancellationToken ct)
        {
            var tenantId = await GetTenantIdAsync(ct);
            var ledger = await _workControlCenter.GetMissionWorkLedgerAsync(tenantId, missionId);
            if (ledger == null) return NotFound(new { Message = $"Mission ledger {missionId} not found." });
            return Ok(ledger);
        }
    }
}
