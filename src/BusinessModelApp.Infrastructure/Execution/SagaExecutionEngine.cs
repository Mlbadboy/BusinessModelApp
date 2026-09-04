using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BusinessModelApp.Core.Domain.Execution;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;

namespace BusinessModelApp.Infrastructure.Execution
{
    public class SagaExecutionEngine : ISagaExecutionEngine
    {
        private readonly AppDbContext _context;
        private readonly IExecutionFirewall _firewall;
        private readonly ILogger<SagaExecutionEngine> _logger;

        public SagaExecutionEngine(AppDbContext context, IExecutionFirewall firewall, ILogger<SagaExecutionEngine> logger)
        {
            _context = context;
            _firewall = firewall;
            _logger = logger;
        }

        public async Task<SagaExecutionStateEntity> StartSagaAsync(
            Guid workspaceId,
            Guid missionId,
            string sagaName,
            List<SagaStepRecord> steps,
            CancellationToken cancellationToken = default)
        {
            var saga = new SagaExecutionStateEntity
            {
                WorkspaceId = workspaceId,
                MissionId = missionId,
                SagaName = sagaName,
                Status = SagaOverallStatus.Running,
                CurrentStepIndex = 0,
                TotalSteps = steps.Count,
                StepsJson = JsonSerializer.Serialize(steps),
                StartedAtUtc = DateTime.UtcNow
            };

            _context.SagaExecutionStates.Add(saga);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Started Saga {SagaId} '{Name}' with {Count} steps for Mission {MissionId}",
                saga.SagaId, sagaName, steps.Count, missionId);

            return saga;
        }

        public async Task<SagaExecutionStateEntity> ExecuteSagaAsync(
            Guid sagaId,
            CancellationToken cancellationToken = default)
        {
            var saga = await _context.SagaExecutionStates.FindAsync(new object[] { sagaId }, cancellationToken);
            if (saga == null)
            {
                throw new KeyNotFoundException($"Saga {sagaId} not found.");
            }

            var steps = JsonSerializer.Deserialize<List<SagaStepRecord>>(saga.StepsJson) ?? new List<SagaStepRecord>();
            var executedSteps = new List<SagaStepRecord>();

            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                saga.CurrentStepIndex = i;
                step.Status = SagaStepStatus.Executing;

                var request = new ExecutionRequest
                {
                    WorkspaceId = saga.WorkspaceId,
                    MissionId = saga.MissionId,
                    AgentId = "SagaExecutor",
                    AgentRole = "OperationsAgent",
                    CapabilityId = step.ForwardCapabilityId,
                    PayloadJson = step.ForwardPayloadJson,
                    IdempotencyKey = $"SAGA_{saga.SagaId}_STEP_{i}",
                    AuditContext = $"Forward step {i} of Saga {saga.SagaName}"
                };

                try
                {
                    var decision = await _firewall.EvaluateAsync(request, cancellationToken);
                    if (!decision.IsPermitted || decision.Permit == null)
                    {
                        throw new InvalidOperationException($"Step {i} ({step.StepName}) denied by firewall: {decision.Denial?.Reason}");
                    }

                    var receipt = await _firewall.ExecuteAsync(request, decision.Permit, cancellationToken);
                    step.Status = SagaStepStatus.Completed;
                    step.ExecutionReceiptId = receipt.ReceiptId;
                    executedSteps.Add(step);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Saga {SagaId} failed at step {StepIndex} ({StepName}). Initiating backward compensation.",
                        saga.SagaId, i, step.StepName);

                    step.Status = SagaStepStatus.Failed;
                    step.ErrorMessage = ex.Message;
                    saga.Status = SagaOverallStatus.Compensating;
                    saga.FailureReason = $"Failed at step {i} ({step.StepName}): {ex.Message}";

                    // Execute backward compensation for previously completed steps
                    await CompensateExecutedStepsAsync(saga.WorkspaceId, saga.MissionId, saga.SagaId, executedSteps, cancellationToken);

                    saga.Status = SagaOverallStatus.Compensated;
                    saga.CompletedAtUtc = DateTime.UtcNow;
                    saga.StepsJson = JsonSerializer.Serialize(steps);
                    await _context.SaveChangesAsync(cancellationToken);

                    return saga;
                }
            }

            saga.Status = SagaOverallStatus.Completed;
            saga.CompletedAtUtc = DateTime.UtcNow;
            saga.StepsJson = JsonSerializer.Serialize(steps);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Saga {SagaId} '{Name}' completed successfully across all {Count} steps",
                saga.SagaId, saga.SagaName, steps.Count);

            return saga;
        }

        private async Task CompensateExecutedStepsAsync(
            Guid workspaceId,
            Guid missionId,
            Guid sagaId,
            List<SagaStepRecord> executedSteps,
            CancellationToken cancellationToken)
        {
            // Reverse order compensation
            for (int i = executedSteps.Count - 1; i >= 0; i--)
            {
                var step = executedSteps[i];
                if (string.IsNullOrWhiteSpace(step.CompensationCapabilityId))
                {
                    continue; // Reversible or no-op compensation
                }

                step.Status = SagaStepStatus.Compensating;
                var compRequest = new ExecutionRequest
                {
                    WorkspaceId = workspaceId,
                    MissionId = missionId,
                    AgentId = "SagaExecutor",
                    AgentRole = "OperationsAgent",
                    CapabilityId = step.CompensationCapabilityId,
                    PayloadJson = step.CompensationPayloadJson,
                    IdempotencyKey = $"SAGA_{sagaId}_COMP_{step.StepIndex}",
                    AuditContext = $"Compensating step {step.StepIndex} of Saga {sagaId}"
                };

                try
                {
                    var decision = await _firewall.EvaluateAsync(compRequest, cancellationToken);
                    if (decision.IsPermitted && decision.Permit != null)
                    {
                        await _firewall.ExecuteAsync(compRequest, decision.Permit, cancellationToken);
                        step.Status = SagaStepStatus.Compensated;
                        _logger.LogInformation("Successfully compensated step {Index} ({Cap}) for Saga {SagaId}",
                            step.StepIndex, step.CompensationCapabilityId, sagaId);
                    }
                    else
                    {
                        step.Status = SagaStepStatus.CompensationFailed;
                        _logger.LogError("Compensation permit denied for step {Index} in Saga {SagaId}", step.StepIndex, sagaId);
                    }
                }
                catch (Exception ex)
                {
                    step.Status = SagaStepStatus.CompensationFailed;
                    _logger.LogError(ex, "Compensation execution failed for step {Index} in Saga {SagaId}", step.StepIndex, sagaId);
                }
            }
        }
    }
}
