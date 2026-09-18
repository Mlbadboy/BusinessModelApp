using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Delivery;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Delivery;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Delivery
{
    public sealed class InMemoryCustomerDeliveryRuntimeStore : ICustomerDeliveryRuntimeStore
    {
        private readonly ConcurrentDictionary<string, DeliveryProject> _projects = new();

        public Task SaveProjectAsync(DeliveryProject project, CancellationToken cancellationToken = default)
        {
            _projects[project.ProjectId] = project;
            return Task.CompletedTask;
        }

        public Task<DeliveryProject?> GetProjectAsync(string projectId, CancellationToken cancellationToken = default)
        {
            _projects.TryGetValue(projectId, out var project);
            return Task.FromResult(project);
        }

        public Task<IReadOnlyList<DeliveryProject>> ListProjectsForCustomerAsync(string customerId, CancellationToken cancellationToken = default)
        {
            var list = _projects.Values.Where(p => p.CustomerId == customerId).ToList();
            return Task.FromResult<IReadOnlyList<DeliveryProject>>(list);
        }
    }

    public sealed class CustomerDeliveryRuntimeService : ICustomerDeliveryRuntimeService
    {
        private readonly ICustomerDeliveryRuntimeStore _store;

        public CustomerDeliveryRuntimeService(ICustomerDeliveryRuntimeStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<DeliveryProject> CreateProjectAsync(
            string tenantId,
            string customerId,
            string contractId,
            List<DeliveryMilestone> milestones,
            List<ValueRealizationMetric> valueMetrics,
            CancellationToken cancellationToken = default)
        {
            var project = new DeliveryProject
            {
                TenantId = tenantId,
                CustomerId = customerId,
                ContractId = contractId,
                Milestones = milestones ?? new List<DeliveryMilestone>(),
                ValueMetrics = valueMetrics ?? new List<ValueRealizationMetric>()
            };

            await _store.SaveProjectAsync(project, cancellationToken);
            return project;
        }

        public async Task<DeliveryProject> AdvanceProjectStatusAsync(
            string projectId,
            DeliveryProjectStatus nextStatus,
            string? signoffSha256 = null,
            CancellationToken cancellationToken = default)
        {
            var project = await _store.GetProjectAsync(projectId, cancellationToken);
            if (project == null) throw new KeyNotFoundException($"Delivery project '{projectId}' not found.");

            switch (nextStatus)
            {
                case DeliveryProjectStatus.Onboarding:
                    project.AdvanceToOnboarding();
                    break;
                case DeliveryProjectStatus.InProgress:
                    project.AdvanceToInProgress();
                    break;
                case DeliveryProjectStatus.DeliveredPendingAcceptance:
                    project.RequestAcceptance();
                    break;
                case DeliveryProjectStatus.DeliveryAccepted:
                    project.RecordAcceptance(signoffSha256 ?? string.Empty);
                    break;
                case DeliveryProjectStatus.ValueRealizationUnderway:
                    project.BeginValueRealization();
                    break;
                case DeliveryProjectStatus.ValueRealized:
                    project.ConfirmValueRealization();
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported status transition to '{nextStatus}'.");
            }

            await _store.SaveProjectAsync(project, cancellationToken);
            return project;
        }

        public async Task<DeliveryProject> RecordMetricMeasurementAsync(
            string projectId,
            string metricId,
            decimal measuredValue,
            EpistemicEvidenceLevel level,
            string proofSha256,
            CancellationToken cancellationToken = default)
        {
            var project = await _store.GetProjectAsync(projectId, cancellationToken);
            if (project == null) throw new KeyNotFoundException($"Delivery project '{projectId}' not found.");

            var metric = project.ValueMetrics.FirstOrDefault(m => m.MetricId == metricId);
            if (metric == null) throw new KeyNotFoundException($"Value metric '{metricId}' not found on project '{projectId}'.");

            metric.RecordMeasurement(measuredValue, level, proofSha256);

            await _store.SaveProjectAsync(project, cancellationToken);
            return project;
        }
    }
}
