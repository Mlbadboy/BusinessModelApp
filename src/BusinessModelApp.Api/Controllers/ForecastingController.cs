using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Forecasting;
using BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Forecasting;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/intelligence/forecasting")]
    public class ForecastingController : ControllerBase
    {
        private readonly IForecastingMetrologyOrchestrator _orchestrator;

        public ForecastingController(IForecastingMetrologyOrchestrator orchestrator)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        [HttpGet("policies/{tenantId}")]
        public async Task<ActionResult<IReadOnlyList<ForecastMethodPolicy>>> GetPolicies(string tenantId, CancellationToken ct)
        {
            var policies = await _orchestrator.Registry.GetAllPoliciesAsync(tenantId, ct);
            return Ok(policies);
        }

        [HttpPost("policies")]
        public async Task<ActionResult> RegisterPolicy([FromBody] ForecastMethodPolicy policy, CancellationToken ct)
        {
            await _orchestrator.Registry.RegisterPolicyAsync(policy, ct);
            return Ok(policy);
        }

        [HttpPost("generate")]
        public async Task<ActionResult<ForecastOutput>> GenerateForecast([FromBody] GenerateForecastRequest request, CancellationToken ct)
        {
            var forecast = await _orchestrator.GenerateGovernedForecastAsync(request.TenantId, request.MetricId, request.HorizonSteps, ct);
            return Ok(forecast);
        }

        [HttpGet("history/{tenantId}/{metricId}")]
        public async Task<ActionResult<IReadOnlyList<ForecastOutput>>> GetHistory(string tenantId, string metricId, CancellationToken ct)
        {
            var history = await _orchestrator.RecordStore.GetForecastsForMetricAsync(tenantId, metricId, ct);
            return Ok(history);
        }

        [HttpPost("regime/detect")]
        public async Task<ActionResult<RegimeAssessment>> DetectRegime([FromBody] DetectRegimeRequest request, CancellationToken ct)
        {
            var assessment = await _orchestrator.RegimeDetector.DetectRegimeShiftAsync(request.TenantId, request.MetricId, ct);
            return Ok(assessment);
        }

        [HttpPost("backtest")]
        public async Task<ActionResult<BacktestScorecard>> BacktestModel([FromBody] BacktestRequest request, CancellationToken ct)
        {
            var scorecard = await _orchestrator.Backtester.BacktestModelAsync(
                request.TenantId, request.MetricId, request.Algorithm, request.TestSteps, ct);
            return Ok(scorecard);
        }

        [HttpPost("drift/evaluate")]
        public async Task<ActionResult<DriftReport>> EvaluateDrift([FromBody] EvaluateDriftRequest request, CancellationToken ct)
        {
            var report = await _orchestrator.DriftMonitor.EvaluateDriftAsync(request.TenantId, request.MetricId, ct);
            return Ok(report);
        }

        [HttpPost("champions")]
        public async Task<ActionResult> SetChampion([FromBody] SetChampionRequest request, CancellationToken ct)
        {
            await _orchestrator.Registry.SetChampionModelAsync(request.TenantId, request.MetricId, request.ModelId, ct);
            return Ok();
        }

        [HttpGet("champions/{tenantId}/{metricId}")]
        public async Task<ActionResult<string?>> GetChampion(string tenantId, string metricId, CancellationToken ct)
        {
            var champion = await _orchestrator.Registry.GetChampionModelAsync(tenantId, metricId, ct);
            return Ok(champion);
        }
    }

    public record GenerateForecastRequest(string TenantId, string MetricId, int HorizonSteps);
    public record DetectRegimeRequest(string TenantId, string MetricId);
    public record BacktestRequest(string TenantId, string MetricId, ForecastModelAlgorithm Algorithm, int TestSteps = 5);
    public record EvaluateDriftRequest(string TenantId, string MetricId);
    public record SetChampionRequest(string TenantId, string MetricId, string ModelId);
}
