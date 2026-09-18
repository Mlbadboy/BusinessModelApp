using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Readiness;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PredictiveReadinessController : ControllerBase
    {
        private readonly IPredictiveReadinessService _readinessService;
        private readonly IPredictiveReadinessStore _store;

        public PredictiveReadinessController(
            IPredictiveReadinessService readinessService,
            IPredictiveReadinessStore store)
        {
            _readinessService = readinessService ?? throw new ArgumentNullException(nameof(readinessService));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        private string GetTenantId() =>
            Request.Headers.TryGetValue("X-Tenant-ID", out var tenant) && !string.IsNullOrWhiteSpace(tenant)
                ? tenant.ToString()
                : "default-tenant";

        [HttpPost("scenarios/evaluate")]
        public async Task<IActionResult> EvaluateScenario([FromBody] EvaluateScenarioRequest request)
        {
            var tenantId = GetTenantId();
            if (request?.Scenario == null) return BadRequest(new { error = "Scenario payload is required." });

            request.Scenario.TenantId = tenantId;
            var assessment = await _readinessService.EvaluateScenarioReadinessAsync(
                tenantId,
                request.Scenario,
                request.DimensionMetrics ?? new Dictionary<ReadinessDimension, double>(),
                request.BaseCapacity > 0 ? request.BaseCapacity : 100.0,
                request.BufferRatio >= 0 ? request.BufferRatio : 0.25);

            return Ok(assessment);
        }

        [HttpGet("assessments/latest")]
        public async Task<IActionResult> GetLatestAssessment()
        {
            var tenantId = GetTenantId();
            var assessments = await _store.ListAssessmentsAsync(tenantId);
            var latest = assessments.OrderByDescending(a => a.EvaluatedUtc).FirstOrDefault();
            if (latest == null) return NotFound(new { error = "No readiness assessments found for tenant." });

            return Ok(latest);
        }

        [HttpGet("assessments/{assessmentId}/why")]
        public async Task<IActionResult> GetWhyTrace(string assessmentId)
        {
            var tenantId = GetTenantId();
            if (string.IsNullOrWhiteSpace(assessmentId)) return BadRequest(new { error = "assessmentId is required." });

            try
            {
                var trace = await _readinessService.GetWhyReadinessTraceAsync(tenantId, assessmentId);
                return Ok(trace);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpGet("debt")]
        public async Task<IActionResult> GetReadinessDebt()
        {
            var tenantId = GetTenantId();
            var debts = await _readinessService.GetTenantReadinessDebtAsync(tenantId);
            return Ok(debts);
        }

        [HttpPost("contingencies/{contingencyId}/stage-proposal")]
        public async Task<IActionResult> StageContingencyProposal(string contingencyId)
        {
            var tenantId = GetTenantId();
            if (string.IsNullOrWhiteSpace(contingencyId)) return BadRequest(new { error = "contingencyId is required." });

            try
            {
                var proposal = await _readinessService.StageContingencyToControlPlaneAsync(tenantId, contingencyId);
                return Ok(new
                {
                    message = "Contingency proposal successfully staged to 3.9.0 Organizational Work Control Plane.",
                    proposal
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class EvaluateScenarioRequest
    {
        public ReadinessScenario? Scenario { get; set; }
        public Dictionary<ReadinessDimension, double>? DimensionMetrics { get; set; }
        public double BaseCapacity { get; set; } = 100.0;
        public double BufferRatio { get; set; } = 0.25;
    }
}
