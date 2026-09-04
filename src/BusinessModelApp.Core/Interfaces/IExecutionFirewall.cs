using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Execution;

namespace BusinessModelApp.Core.Interfaces
{
    /// <summary>
    /// B6-H6: The sovereign deterministic Execution Firewall.
    /// Evaluates all 12 validation pillars atomically and issues single-use,
    /// cryptographically signed ExecutionPermits for governed execution.
    /// </summary>
    public interface IExecutionFirewall
    {
        Task<ExecutionDecision> EvaluateAsync(ExecutionRequest request, CancellationToken cancellationToken = default);
        Task<ExecutionReceipt> ExecuteAsync(ExecutionRequest request, ExecutionPermit permit, CancellationToken cancellationToken = default);
    }
}
