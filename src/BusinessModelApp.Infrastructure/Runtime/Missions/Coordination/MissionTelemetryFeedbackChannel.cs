using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Missions;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Missions;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Missions.Coordination
{
    public class MissionTelemetryFeedbackChannel : IMissionTelemetryFeedbackChannel
    {
        private readonly IMissionCoordinationStore _store;
        private readonly IOrganizationalWorkRepository? _workRepository;

        public MissionTelemetryFeedbackChannel(
            IMissionCoordinationStore store,
            IOrganizationalWorkRepository? workRepository = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _workRepository = workRepository;
        }

        public async Task RecordTelemetryAsync(
            MissionTelemetryFeedback feedback,
            CancellationToken ct = default)
        {
            if (feedback == null) throw new ArgumentNullException(nameof(feedback));

            if (string.IsNullOrWhiteSpace(feedback.TelemetryId))
            {
                feedback.TelemetryId = $"TEL-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
            }

            // Invariant I27-E: Telemetry is empirical metrology; Telemetry ≠ Truth ≠ Causal Attribution ≠ Reputation
            await _store.SaveTelemetryAsync(feedback, ct);

            // Propagate feedback to WorkControl repository if linked
            if (_workRepository != null && !string.IsNullOrWhiteSpace(feedback.WorkId))
            {
                var item = await _workRepository.GetWorkItemAsync(feedback.TenantId, feedback.WorkId, ct);
                if (item != null)
                {
                    item.OutcomeRecord ??= new WorkOutcome { WorkId = item.WorkId };
                    item.OutcomeRecord.ActualMetrics["DurationSeconds"] = feedback.ActualDuration.TotalSeconds;
                    item.OutcomeRecord.ActualMetrics["TokensBurned"] = feedback.ActualTokensBurned;
                    item.OutcomeRecord.ActualMetrics["CostUsd"] = (double)feedback.ActualCostUsd;
                    await _workRepository.SaveWorkItemAsync(item, ct);
                }
            }
        }

        public Task<IReadOnlyList<MissionTelemetryFeedback>> GetTelemetryForWorkAsync(
            string tenantId,
            string workId,
            CancellationToken ct = default)
        {
            return _store.ListTelemetryAsync(tenantId, workId, ct);
        }
    }
}
