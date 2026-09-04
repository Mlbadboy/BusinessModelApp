using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BusinessModelApp.Core.Domain.ExternalReality;
using BusinessModelApp.Core.Interfaces;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/market")]
    [Authorize]
    public class MarketRadarController : ControllerBase
    {
        private readonly IMarketRadarService _marketRadar;
        private readonly IExternalSourceRegistry _sourceRegistry;
        private readonly IOpportunityIntelligenceService _opportunityService;
        private readonly IThreatIntelligenceService _threatService;
        private readonly IUserContextService _userContext;
        private readonly ILogger<MarketRadarController> _logger;

        public MarketRadarController(
            IMarketRadarService marketRadar,
            IExternalSourceRegistry sourceRegistry,
            IOpportunityIntelligenceService opportunityService,
            IThreatIntelligenceService threatService,
            IUserContextService userContext,
            ILogger<MarketRadarController> logger)
        {
            _marketRadar = marketRadar ?? throw new ArgumentNullException(nameof(marketRadar));
            _sourceRegistry = sourceRegistry ?? throw new ArgumentNullException(nameof(sourceRegistry));
            _opportunityService = opportunityService ?? throw new ArgumentNullException(nameof(opportunityService));
            _threatService = threatService ?? throw new ArgumentNullException(nameof(threatService));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("radar")]
        public async Task<IActionResult> GetRadarSummary(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var activeSignals = await _marketRadar.GetActiveSignalsAsync(workspaceId, null, ct);
            var competitors = await _marketRadar.ListCompetitorsAsync(workspaceId, ct);
            var opportunities = await _opportunityService.ListOpportunitiesAsync(workspaceId, null, ct);
            var threats = await _threatService.ListThreatsAsync(workspaceId, ct);
            var recommendations = await _opportunityService.ListRecommendationsAsync(workspaceId, ct);

            return Ok(new
            {
                WorkspaceId = workspaceId,
                TotalActiveSignals = activeSignals.Count,
                CriticalSignals = activeSignals.Count(s => s.Priority == ExternalSignalPriority.Critical),
                CompetitorCount = competitors.Count,
                OpportunityCount = opportunities.Count,
                ThreatCount = threats.Count,
                RecommendationCount = recommendations.Count,
                Signals = activeSignals.Take(10),
                Competitors = competitors.Take(5),
                TopOpportunities = opportunities.Take(5),
                ActiveThreats = threats.Take(5)
            });
        }

        [HttpGet("signals")]
        public async Task<IActionResult> GetSignals([FromQuery] ExternalSignalPriority? minPriority, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var signals = await _marketRadar.GetActiveSignalsAsync(workspaceId, minPriority, ct);
            return Ok(signals);
        }

        [HttpGet("signals/{id:guid}")]
        public async Task<IActionResult> GetSignalById(Guid id, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var signal = await _marketRadar.GetSignalAsync(id, workspaceId, ct);
            if (signal == null) return NotFound(new { message = $"Signal {id} not found." });
            return Ok(signal);
        }

        [HttpPost("signals")]
        public async Task<IActionResult> DetectSignal([FromBody] ExternalSignal signal, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            signal.WorkspaceId = workspaceId;
            var created = await _marketRadar.DetectSignalAsync(signal, ct);
            return CreatedAtAction(nameof(GetSignalById), new { id = created.Id }, created);
        }

        [HttpGet("sources")]
        public async Task<IActionResult> GetSources(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var sources = await _sourceRegistry.ListSourcesAsync(workspaceId, ct);
            return Ok(sources);
        }

        [HttpPost("sources")]
        public async Task<IActionResult> RegisterSource([FromBody] ExternalSourceRegistryEntry entry, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            entry.WorkspaceId = workspaceId;
            var created = await _sourceRegistry.RegisterSourceAsync(entry, ct);
            return Ok(created);
        }

        [HttpGet("evidence")]
        public async Task<IActionResult> GetEvidence([FromQuery] int limit = 50, CancellationToken ct = default)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var evidence = await _sourceRegistry.ListEvidenceAsync(workspaceId, limit, ct);
            return Ok(evidence);
        }

        [HttpPost("evidence")]
        public async Task<IActionResult> IngestEvidence([FromBody] ExternalEvidenceRecord evidence, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            evidence.WorkspaceId = workspaceId;
            var created = await _sourceRegistry.IngestExternalEvidenceAsync(evidence, ct);
            return Ok(created);
        }

        [HttpGet("competitors")]
        public async Task<IActionResult> GetCompetitors(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var competitors = await _marketRadar.ListCompetitorsAsync(workspaceId, ct);
            return Ok(competitors);
        }

        [HttpGet("competitors/{id:guid}")]
        public async Task<IActionResult> GetCompetitorById(Guid id, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var comp = await _marketRadar.GetCompetitorAsync(id, workspaceId, ct);
            if (comp == null) return NotFound(new { message = $"Competitor {id} not found." });
            return Ok(comp);
        }

        [HttpGet("opportunities")]
        public async Task<IActionResult> GetOpportunities([FromQuery] OpportunityStatus? status, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var opps = await _opportunityService.ListOpportunitiesAsync(workspaceId, status, ct);
            return Ok(opps);
        }

        [HttpPost("opportunities")]
        public async Task<IActionResult> CreateOpportunity([FromBody] MarketOpportunity opportunity, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            opportunity.WorkspaceId = workspaceId;
            var created = await _opportunityService.CreateOpportunityAsync(opportunity, ct);
            return Ok(created);
        }

        [HttpGet("opportunities/{id:guid}")]
        public async Task<IActionResult> GetOpportunityById(Guid id, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var opp = await _opportunityService.GetOpportunityAsync(id, workspaceId, ct);
            if (opp == null) return NotFound(new { message = $"Opportunity {id} not found." });
            return Ok(opp);
        }

        [HttpGet("opportunities/{id:guid}/score")]
        public async Task<IActionResult> GetOpportunityScore(Guid id, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var score = await _opportunityService.CalculateCommercialScoreAsync(id, workspaceId, ct);
            return Ok(score);
        }

        [HttpGet("opportunities/{id:guid}/scenarios")]
        public async Task<IActionResult> GetOpportunityScenarios(Guid id, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var scenarios = await _opportunityService.GenerateCounterfactualScenariosAsync(id, workspaceId, ct);
            return Ok(scenarios);
        }

        [HttpPost("opportunities/{id:guid}/recommendation")]
        public async Task<IActionResult> GenerateRecommendation(Guid id, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var rec = await _opportunityService.GenerateStrategicRecommendationAsync(id, workspaceId, ct);
            return Ok(rec);
        }

        [HttpGet("recommendations")]
        public async Task<IActionResult> GetRecommendations(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var recs = await _opportunityService.ListRecommendationsAsync(workspaceId, ct);
            return Ok(recs);
        }

        [HttpGet("recommendations/{id:guid}")]
        public async Task<IActionResult> GetRecommendationById(Guid id, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var rec = await _opportunityService.GetRecommendationAsync(id, workspaceId, ct);
            if (rec == null) return NotFound(new { message = $"Recommendation {id} not found." });
            return Ok(rec);
        }

        [HttpGet("threats")]
        public async Task<IActionResult> GetThreats(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var threats = await _threatService.ListThreatsAsync(workspaceId, ct);
            return Ok(threats);
        }

        [HttpPost("threats")]
        public async Task<IActionResult> RecordThreat([FromBody] MarketThreat threat, CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            threat.WorkspaceId = workspaceId;
            var created = await _threatService.RecordThreatAsync(threat, ct);
            return Ok(created);
        }
    }
}
