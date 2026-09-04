using System;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using BusinessModelApp.Core.Domain.Execution;
using BusinessModelApp.Core.Interfaces;

namespace BusinessModelApp.Infrastructure.Execution
{
    public class AutonomousMissionExecutor : IAutonomousMissionExecutor
    {
        private readonly IExecutionFirewall _firewall;
        private readonly ILogger<AutonomousMissionExecutor> _logger;

        public AutonomousMissionExecutor(IExecutionFirewall firewall, ILogger<AutonomousMissionExecutor> logger)
        {
            _firewall = firewall;
            _logger = logger;
        }

        public async Task<ExecutionReceipt> ExecuteMissionStepAsync(
            ExecutionRequest stepRequest,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Autonomous Mission Executor running step for Mission {MissionId}, Capability {Cap}",
                stepRequest.MissionId, stepRequest.CapabilityId);

            // 1. Mandatory Firewall Evaluation (ZERO BYPASS)
            var decision = await _firewall.EvaluateAsync(stepRequest, cancellationToken);

            if (!decision.IsPermitted || decision.Permit == null)
            {
                var reason = decision.Denial?.Reason ?? "Action not permitted by Execution Firewall.";
                _logger.LogWarning("Mission step DENIED: {Reason}", reason);
                throw new SecurityException($"[Autonomous Mission Executor] Step denied by Execution Firewall: {reason}");
            }

            // 2. Governed Execution with cryptographic Permit
            var receipt = await _firewall.ExecuteAsync(stepRequest, decision.Permit, cancellationToken);
            _logger.LogInformation("Mission step executed successfully: Receipt {ReceiptId}", receipt.ReceiptId);

            return receipt;
        }
    }
}
