using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Acquisition;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Acquisition;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Acquisition
{
    public sealed class InMemoryCustomerAcquisitionRuntimeStore : ICustomerAcquisitionRuntimeStore
    {
        private readonly ConcurrentDictionary<string, OutreachCampaignPolicy> _policies = new();
        private readonly ConcurrentDictionary<string, GovernedOutreachTask> _tasks = new();

        public Task SavePolicyAsync(OutreachCampaignPolicy policy, CancellationToken cancellationToken = default)
        {
            _policies[policy.TenantId] = policy;
            return Task.CompletedTask;
        }

        public Task<OutreachCampaignPolicy?> GetPolicyAsync(string tenantId, CancellationToken cancellationToken = default)
        {
            _policies.TryGetValue(tenantId, out var policy);
            return Task.FromResult(policy);
        }

        public Task SaveOutreachTaskAsync(GovernedOutreachTask task, CancellationToken cancellationToken = default)
        {
            _tasks[task.TaskId] = task;
            return Task.CompletedTask;
        }

        public Task<GovernedOutreachTask?> GetOutreachTaskAsync(string taskId, CancellationToken cancellationToken = default)
        {
            _tasks.TryGetValue(taskId, out var task);
            return Task.FromResult(task);
        }

        public Task<IReadOnlyList<GovernedOutreachTask>> ListTasksForProspectAsync(string prospectId, CancellationToken cancellationToken = default)
        {
            var list = _tasks.Values.Where(t => t.ProspectId == prospectId).ToList();
            return Task.FromResult<IReadOnlyList<GovernedOutreachTask>>(list);
        }
    }

    public sealed class CustomerAcquisitionRuntimeService : ICustomerAcquisitionRuntimeService
    {
        private readonly ICustomerAcquisitionRuntimeStore _store;

        public CustomerAcquisitionRuntimeService(ICustomerAcquisitionRuntimeStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<GovernedOutreachTask> PrepareOutreachAsync(
            string tenantId,
            string prospectId,
            string contactEmail,
            string subjectLine,
            string messageContent,
            DateTime? lastContactedUtc,
            CancellationToken cancellationToken = default)
        {
            var policy = await _store.GetPolicyAsync(tenantId, cancellationToken) ??
                         new OutreachCampaignPolicy { TenantId = tenantId };

            var task = new GovernedOutreachTask
            {
                TenantId = tenantId,
                ProspectId = prospectId,
                ContactEmail = contactEmail,
                SubjectLine = subjectLine,
                MessageContent = messageContent
            };

            // Enforce contact frequency and anti-spam limits
            if (!policy.EvaluateOutreachAllowed(lastContactedUtc, out string suppressionReason))
            {
                task.Suppress(OutreachDeliveryStatus.SuppressedFrequencyLimit, suppressionReason);
            }

            await _store.SaveOutreachTaskAsync(task, cancellationToken);
            return task;
        }

        public async Task<GovernedOutreachTask> ExecuteGovernedOutreachAsync(
            string taskId,
            string batch6PermitId,
            string externalProviderRef,
            CancellationToken cancellationToken = default)
        {
            var task = await _store.GetOutreachTaskAsync(taskId, cancellationToken);
            if (task == null) throw new KeyNotFoundException($"Outreach task '{taskId}' not found.");

            if (task.DeliveryStatus == OutreachDeliveryStatus.SuppressedFrequencyLimit ||
                task.DeliveryStatus == OutreachDeliveryStatus.SuppressedSpamGuard)
            {
                throw new InvalidOperationException($"Cannot execute suppressed outreach task: {task.SuppressionReason}");
            }

            if (string.IsNullOrWhiteSpace(batch6PermitId))
            {
                throw new InvalidOperationException("External communication requires Batch 6 Execution Permit (Law I42-B).");
            }

            task.AuthorizePermit(batch6PermitId);
            task.MarkSent(externalProviderRef);

            await _store.SaveOutreachTaskAsync(task, cancellationToken);
            return task;
        }
    }
}
