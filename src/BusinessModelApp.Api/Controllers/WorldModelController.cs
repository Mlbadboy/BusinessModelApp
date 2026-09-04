using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Core.WorldModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/world-model")]
    [Authorize]
    public class WorldModelController : ControllerBase
    {
        private readonly ICompanyWorldModel _worldModel;
        private readonly IUserContextService _userContext;
        private readonly ILogger<WorldModelController> _logger;

        public WorldModelController(
            ICompanyWorldModel worldModel,
            IUserContextService userContext,
            ILogger<WorldModelController> logger)
        {
            _worldModel = worldModel ?? throw new ArgumentNullException(nameof(worldModel));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("snapshot")]
        public async Task<IActionResult> GetSnapshot(CancellationToken ct)
        {
            Guid workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null, ct);
            var snapshot = await _worldModel.CaptureVerifiedSnapshotAsync(workspaceId, ct);

            return Ok(new
            {
                snapshot.Id,
                snapshot.WorkspaceId,
                snapshot.CapturedAt,
                snapshot.RevenueBaselineState,
                Revenue = snapshot.VerifiedRevenueINR,
                Cash = snapshot.VerifiedSettledCashINR,
                Pipeline = snapshot.ActivePipelineINR,
                QualifiedPipeline = snapshot.QualifiedPipelineINR,
                OpenOpportunities = snapshot.OpenOpportunitiesCount,
                VerifiedProspects = snapshot.VerifiedProspectsCount,
                ActiveDeliveryProjects = snapshot.ActiveDeliveryProjectsCount,
                AvailableDeliverySlots = snapshot.AvailableDeliverySlots,
                ReservedCompute = snapshot.ReservedComputeBudgetINR,
                OutstandingInvoices = snapshot.OutstandingInvoicesINR,
                PaymentRisk = snapshot.PaymentRiskScore,
                EvidenceCoverage = snapshot.OverallEvidenceCoverageScore,
                snapshot.SnapshotDigestHash
            });
        }
    }
}
