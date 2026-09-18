using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Scenario;
using BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Scenario;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScenarioController : ControllerBase
    {
        private readonly IScenarioOrchestrator _orchestrator;
        private readonly IScenarioStore _store;

        public ScenarioController(IScenarioOrchestrator orchestrator, IScenarioStore store)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        [HttpPost("simulate")]
        public async Task<ActionResult<ScenarioOutcome>> SimulateScenario(
            [FromBody] ScenarioDefinition scenario,
            CancellationToken ct)
        {
            if (scenario == null || string.IsNullOrWhiteSpace(scenario.TenantId))
            {
                return BadRequest(new { message = "Valid scenario definition and TenantId are required." });
            }

            var outcome = await _orchestrator.RunScenarioPipelineAsync(scenario, ct);
            return Ok(outcome);
        }

        [HttpPost("compare")]
        public async Task<ActionResult<ScenarioComparisonResult>> CompareScenarios(
            [FromBody] CompareScenariosRequest request,
            CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TenantId) || request.ScenarioIds == null || request.ScenarioIds.Count == 0)
            {
                return BadRequest(new { message = "TenantId and at least one scenario ID are required for comparison." });
            }

            var result = await _orchestrator.RunComparisonPipelineAsync(request.TenantId, request.ScenarioIds, ct);
            return Ok(result);
        }

        [HttpGet("{tenantId}/{scenarioId}")]
        public async Task<ActionResult<ScenarioOutcome>> GetScenarioOutcome(
            string tenantId,
            string scenarioId,
            CancellationToken ct)
        {
            var outcome = await _store.GetOutcomeAsync(tenantId, scenarioId, ct);
            if (outcome == null)
            {
                return NotFound(new { message = $"Scenario {scenarioId} outcome not found for tenant {tenantId}." });
            }
            return Ok(outcome);
        }

        [HttpGet("{tenantId}/signal/{signalId}")]
        public async Task<ActionResult<IReadOnlyList<ScenarioOutcome>>> GetOutcomesForSignal(
            string tenantId,
            string signalId,
            CancellationToken ct)
        {
            var outcomes = await _store.GetOutcomesForSignalAsync(tenantId, signalId, ct);
            return Ok(outcomes);
        }

        [HttpGet("{tenantId}/{scenarioId}/provenance")]
        public async Task<ActionResult<ScenarioProvenance>> GetScenarioProvenance(
            string tenantId,
            string scenarioId,
            CancellationToken ct)
        {
            var provenance = await _store.GetProvenanceAsync(tenantId, scenarioId, ct);
            if (provenance == null)
            {
                return NotFound(new { message = $"Provenance for scenario {scenarioId} not found." });
            }
            return Ok(provenance);
        }

        [HttpPost("from-signal")]
        public async Task<ActionResult<ScenarioDefinition>> GenerateFromSignal(
            [FromBody] GenerateScenarioFromSignalRequest request,
            CancellationToken ct)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TenantId) || string.IsNullOrWhiteSpace(request.SignalId))
            {
                return BadRequest(new { message = "TenantId and SignalId are required." });
            }

            var scenario = await _orchestrator.GenerateScenarioFromRadarSignalAsync(
                request.TenantId,
                request.SignalId,
                request.Type,
                ct);
            return Ok(scenario);
        }
    }

    public class CompareScenariosRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public List<string> ScenarioIds { get; set; } = new();
    }

    public class GenerateScenarioFromSignalRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public string SignalId { get; set; } = string.Empty;
        public ScenarioType Type { get; set; } = ScenarioType.WhatIfIntervention;
    }
}
