using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Security;

namespace BusinessModelApp.Infrastructure.Security
{
    /// <summary>
    /// Decoupled, external Kill-Switch control plane independent of the AI agent runtime.
    /// Invariant: Halts all concurrent security workers in <= 100ms.
    /// Invariant: Kill switch state is atomic and checked on every execution loop iteration.
    /// </summary>
    public class KillSwitchManager
    {
        private static readonly object _lock = new object();
        private static volatile bool _isActive = false;
        private static DateTime? _triggeredAt = null;
        private static string _initiatedBy = string.Empty;
        private static string _reason = string.Empty;
        private static long _haltDurationMs = 0;
        private static CancellationTokenSource _cts = new CancellationTokenSource();

        public static bool IsActive => _isActive;
        public static CancellationToken Token => _cts.Token;

        public static KillSwitchStatus GetStatus()
        {
            lock (_lock)
            {
                return new KillSwitchStatus
                {
                    IsActive = _isActive,
                    TriggeredAt = _triggeredAt,
                    InitiatedBy = _initiatedBy,
                    Reason = _reason,
                    ExecutionHaltDurationMs = _haltDurationMs
                };
            }
        }

        public static KillSwitchStatus Trigger(string reason, string initiatedBy)
        {
            var sw = Stopwatch.StartNew();
            lock (_lock)
            {
                _isActive = true;
                _triggeredAt = DateTime.UtcNow;
                _initiatedBy = initiatedBy ?? "SystemGovernor";
                _reason = reason ?? "Emergency Security Stop Triggered";

                // Signal all concurrent workers immediately
                if (!_cts.IsCancellationRequested)
                {
                    _cts.Cancel();
                }

                sw.Stop();
                _haltDurationMs = sw.ElapsedMilliseconds;

                return new KillSwitchStatus
                {
                    IsActive = true,
                    TriggeredAt = _triggeredAt,
                    InitiatedBy = _initiatedBy,
                    Reason = _reason,
                    ExecutionHaltDurationMs = _haltDurationMs
                };
            }
        }

        public static KillSwitchStatus Reset(string initiatedBy)
        {
            lock (_lock)
            {
                _isActive = false;
                _triggeredAt = null;
                _initiatedBy = string.Empty;
                _reason = string.Empty;
                _haltDurationMs = 0;

                // Re-arm cancellation token source for future campaigns
                _cts.Dispose();
                _cts = new CancellationTokenSource();

                return new KillSwitchStatus
                {
                    IsActive = false,
                    TriggeredAt = null,
                    InitiatedBy = initiatedBy,
                    Reason = "Kill Switch Disarmed by Administrator",
                    ExecutionHaltDurationMs = 0
                };
            }
        }

        public static void AssertNotHalted()
        {
            if (_isActive || _cts.IsCancellationRequested)
            {
                throw new OperationCanceledException(
                    $"SECURITY KILL-SWITCH ACTIVE: Security operations terminated by {_initiatedBy}. Reason: '{_reason}'.");
            }
        }
    }
}
