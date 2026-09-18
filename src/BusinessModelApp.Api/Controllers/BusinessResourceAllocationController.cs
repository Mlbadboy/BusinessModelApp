using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Allocation;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Allocation;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/enterprise/allocation")]
    public sealed class BusinessResourceAllocationController : ControllerBase
    {
        private readonly IBusinessResourceAllocationService _service;
        private readonly IBusinessResourceAllocationStore _store;

        public BusinessResourceAllocationController(
            IBusinessResourceAllocationService service,
            IBusinessResourceAllocationStore store)
        {
            _service = service;
            _store = store;
        }

        public record ConfigureBudgetRequest(string TenantId, ResourcePoolType PoolType, decimal TotalCapacity, decimal ReserveFloorPct = 20.0m);
        public record RequestAllocationRequest(string TenantId, string MissionId, ResourcePoolType PoolType, decimal RequestedAmount, AllocationPriority Priority, decimal CurrentRunwayMonths = 12m);

        [HttpPost("budgets")]
        public async Task<ActionResult<TenantResourceBudget>> ConfigureBudget([FromBody] ConfigureBudgetRequest request, CancellationToken cancellationToken)
        {
            var budget = await _service.ConfigureBudgetAsync(request.TenantId, request.PoolType, request.TotalCapacity, request.ReserveFloorPct, cancellationToken);
            return Ok(budget);
        }

        [HttpGet("budgets")]
        public async Task<ActionResult<IReadOnlyList<TenantResourceBudget>>> ListBudgets([FromQuery] string tenantId, CancellationToken cancellationToken)
        {
            var list = await _store.ListBudgetsForTenantAsync(tenantId, cancellationToken);
            return Ok(list);
        }

        [HttpPost("requests")]
        public async Task<ActionResult<ResourceAllocationRequest>> RequestAllocation([FromBody] RequestAllocationRequest request, CancellationToken cancellationToken)
        {
            var alloc = await _service.RequestAllocationAsync(
                request.TenantId,
                request.MissionId,
                request.PoolType,
                request.RequestedAmount,
                request.Priority,
                request.CurrentRunwayMonths,
                cancellationToken);

            return Ok(alloc);
        }

        [HttpPost("requests/{requestId}/release")]
        public async Task<IActionResult> ReleaseAllocation(string requestId, CancellationToken cancellationToken)
        {
            await _service.ReleaseAllocationAsync(requestId, cancellationToken);
            return Ok(new { released = true });
        }
    }
}
