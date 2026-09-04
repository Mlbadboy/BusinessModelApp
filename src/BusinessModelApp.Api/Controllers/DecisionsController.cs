using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Decisions;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DecisionsController : ControllerBase
    {
        private readonly IDecisionEngine _decisionEngine;
        private readonly AppDbContext _dbContext;
        private readonly BusinessModelApp.Core.Interfaces.IUserContextService _userContext;
        private readonly ILogger<DecisionsController> _logger;

        public DecisionsController(
            IDecisionEngine decisionEngine,
            AppDbContext dbContext,
            BusinessModelApp.Core.Interfaces.IUserContextService userContext,
            ILogger<DecisionsController> logger)
        {
            _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("{id}/why")]
        public async Task<IActionResult> GetWhyCharlieExplanation(Guid id, CancellationToken ct)
        {
            _logger.LogInformation("[DecisionsController] Fetching Why Charlie explanation for Decision {DecisionId}", id);

            var workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var decision = await _dbContext.DecisionRecords.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id && d.WorkspaceId == workspaceId, ct);
            if (decision == null)
            {
                return NotFound(new { message = $"Decision {id} not found." });
            }

            var explanation = await _decisionEngine.GetWhyCharlieExplanationAsync(id, ct);
            if (explanation == null)
            {
                return NotFound(new { message = $"Decision {id} not found." });
            }

            return Ok(explanation);
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentDecisions(CancellationToken ct)
        {
            var workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var decisions = await _dbContext.DecisionRecords
                .Where(d => d.WorkspaceId == workspaceId)
                .OrderByDescending(d => d.DecidedAt)
                .Take(20)
                .ToListAsync(ct);

            return Ok(decisions);
        }
    }
}
