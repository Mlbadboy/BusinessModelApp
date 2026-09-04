using System;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly ILogger<WorldModelController> _logger;

        public WorldModelController(ICompanyWorldModel worldModel, ILogger<WorldModelController> logger)
        {
            _worldModel = worldModel ?? throw new ArgumentNullException(nameof(worldModel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("snapshot")]
        public async Task<IActionResult> GetSnapshot(CancellationToken ct)
        {
            Guid workspaceId = Guid.Parse("00000000-0000-0000-0000-000000000001");
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
