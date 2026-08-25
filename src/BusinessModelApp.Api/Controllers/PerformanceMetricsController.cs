using BusinessModelApp.Core.DTOs.Analytics;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PerformanceMetricsController : ControllerBase
    {
        [HttpGet]
        public ActionResult<PerformanceMetricsDto> GetPerformanceMetrics()
        {
            // For now, we'll return mocked data.
            // Later, this can be connected to a real data source.
            var metrics = new PerformanceMetricsDto
            {
                Revenue = "Revenue: $1.24M (+12%)",
                Efficiency = "Operational Efficiency: 92%"
            };

            return Ok(metrics);
        }
    }
}
