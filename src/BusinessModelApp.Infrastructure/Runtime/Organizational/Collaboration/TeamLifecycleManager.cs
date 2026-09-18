using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Collaboration;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Collaboration
{
    public class TeamLifecycleManager : ITeamLifecycleManager
    {
        private readonly ITeamCharterStore _store;

        public TeamLifecycleManager(ITeamCharterStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<(bool Dissolved, string? Reason)> DissolveTeamAsync(
            string tenantId,
            string charterId,
            string reason,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) return (false, "TenantId is required.");
            if (string.IsNullOrWhiteSpace(charterId)) return (false, "CharterId is required.");

            var charter = await _store.GetCharterAsync(tenantId, charterId, ct);
            if (charter == null)
            {
                return (false, $"Charter '{charterId}' not found for tenant.");
            }

            if (charter.Status == TeamLifecycleStatus.Dissolved)
            {
                return (true, "Team is already dissolved.");
            }

            // Invariant I29-F: Ephemeral lifecycle & resource release
            charter.Status = TeamLifecycleStatus.Dissolved;
            await _store.SaveCharterAsync(charter, ct);

            // Calculate performance record
            var messages = await _store.ListMessagesAsync(tenantId, charterId, ct);
            var disputes = await _store.ListDisputesAsync(tenantId, charterId, ct);

            var performance = new TeamPerformanceRecord
            {
                CharterId = charterId,
                TenantId = tenantId,
                ObjectiveAchieved = !reason.Contains("Fail", StringComparison.OrdinalIgnoreCase),
                Duration = DateTime.UtcNow - charter.FormedUtc,
                RoundsUsed = messages.Count,
                DisputesEncountered = disputes.Count,
                DisputesResolved = disputes.Count(d => d.ResolutionOutcome != ArbitrationOutcome.EscalatedToGovernance),
                CollaborationEfficiencyScore = disputes.Count == 0 ? 1.0 : Math.Max(0.2, 1.0 - (disputes.Count * 0.15))
            };

            await _store.SavePerformanceRecordAsync(performance, ct);
            return (true, null);
        }

        public async Task<int> ProcessExpiredChartersAsync(
            string tenantId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) return 0;

            var active = await _store.ListActiveChartersAsync(tenantId, ct);
            int expiredCount = 0;
            var now = DateTime.UtcNow;

            foreach (var charter in active)
            {
                if (now > charter.ExpiresUtc)
                {
                    charter.Status = TeamLifecycleStatus.TimedOut;
                    await _store.SaveCharterAsync(charter, ct);
                    expiredCount++;
                }
            }

            return expiredCount;
        }
    }
}
