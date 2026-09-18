using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Delivery;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Delivery
{
    public interface ICustomerDeliveryRuntimeStore
    {
        Task SaveProjectAsync(DeliveryProject project, CancellationToken cancellationToken = default);
        Task<DeliveryProject?> GetProjectAsync(string projectId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DeliveryProject>> ListProjectsForCustomerAsync(string customerId, CancellationToken cancellationToken = default);
    }

    public interface ICustomerDeliveryRuntimeService
    {
        Task<DeliveryProject> CreateProjectAsync(
            string tenantId,
            string customerId,
            string contractId,
            List<DeliveryMilestone> milestones,
            List<ValueRealizationMetric> valueMetrics,
            CancellationToken cancellationToken = default);

        Task<DeliveryProject> AdvanceProjectStatusAsync(
            string projectId,
            DeliveryProjectStatus nextStatus,
            string? signoffSha256 = null,
            CancellationToken cancellationToken = default);

        Task<DeliveryProject> RecordMetricMeasurementAsync(
            string projectId,
            string metricId,
            decimal measuredValue,
            EpistemicEvidenceLevel level,
            string proofSha256,
            CancellationToken cancellationToken = default);
    }
}
