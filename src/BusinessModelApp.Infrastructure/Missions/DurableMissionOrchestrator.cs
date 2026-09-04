using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Decisions;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Strategy;
using BusinessModelApp.Core.Missions;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Infrastructure.Missions
{
    public class DurableMissionOrchestrator : IDurableMissionOrchestrator
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<DurableMissionOrchestrator> _logger;

        public DurableMissionOrchestrator(AppDbContext dbContext, ILogger<DurableMissionOrchestrator> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DurableMission> CreateDurableMissionPlanAsync(
            DecisionRecord decision,
            BusinessStrategy strategy,
            CancellationToken ct = default)
        {
            _logger.LogInformation("[DurableMission] Creating durable mission plan for Strategy {StrategyName}, Decision {DecisionId}",
                strategy.StrategyName, decision.Id);

            var mission = new DurableMission
            {
                WorkspaceId = Guid.NewGuid(), // Will inherit workspace
                ObjectiveId = decision.ObjectiveId,
                StrategyId = strategy.Id,
                DecisionRecordId = decision.Id,
                Title = $"Mission: Execute {strategy.StrategyName}",
                Description = $"Governed autonomous execution of {strategy.StrategyName} targeting {strategy.TargetMarket}.",
                State = decision.HumanApprovalRequired ? DurableMissionState.WaitingForApproval : DurableMissionState.Ready,
                CurrentCheckpointIndex = 0,
                TotalPlannedSteps = 4,
                LastCompletedStepName = "None",
                AllocatedBudgetINR = strategy.EstimatedComputeSpendINR
            };

            // Pre-seed planned checkpoints
            var plannedSteps = new[]
            {
                ("Phase 2.1: Multi-Source Account Discovery & Evidence Grounding", "ProspectDiscovery"),
                ("Phase 2.2: Decision Maker Verification & Corporate Email Binding", "CompanyResearch"),
                ("Phase 2.3: Governed AI Outreach & Commercial Proposal Formulation", "ProposalGeneration"),
                ("Phase 2.4: Contract Signature & Payment Settlement Verification", "CommercialCloser")
            };

            for (int i = 0; i < plannedSteps.Length; i++)
            {
                mission.Checkpoints.Add(new DurableMissionCheckpoint
                {
                    MissionId = mission.Id,
                    StepIndex = i + 1,
                    StepName = plannedSteps[i].Item1,
                    AgentRole = plannedSteps[i].Item2,
                    IsCompleted = false,
                    IsBlockedOnApproval = (i == 2), // Step 3 requires proposal approval
                    CheckpointTimestamp = DateTime.UtcNow
                });
            }

            await _dbContext.DurableMissions.AddAsync(mission, ct);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("[DurableMission] Mission {MissionId} created in state {State} with {Count} checkpoints.",
                mission.Id, mission.State, mission.Checkpoints.Count);

            return mission;
        }

        public async Task<DurableMissionCheckpoint> RecordCheckpointAsync(
            Guid missionId,
            string stepName,
            string agentRole,
            string outputPayloadJson,
            bool isCompleted,
            CancellationToken ct = default)
        {
            var mission = await _dbContext.DurableMissions
                .Include(m => m.Checkpoints)
                .FirstOrDefaultAsync(m => m.Id == missionId, ct);

            if (mission == null)
            {
                throw new KeyNotFoundException($"DurableMission {missionId} not found.");
            }

            var checkpoint = mission.Checkpoints.FirstOrDefault(c => c.StepName == stepName)
                ?? new DurableMissionCheckpoint { MissionId = missionId, StepName = stepName, StepIndex = mission.Checkpoints.Count + 1 };

            checkpoint.AgentRole = agentRole;
            checkpoint.ExecutionOutputJson = outputPayloadJson;
            checkpoint.IsCompleted = isCompleted;
            checkpoint.CheckpointTimestamp = DateTime.UtcNow;

            if (isCompleted)
            {
                mission.CurrentCheckpointIndex = checkpoint.StepIndex;
                mission.LastCompletedStepName = stepName;
                mission.LastCheckpointSavedAt = DateTime.UtcNow;

                if (checkpoint.StepIndex == mission.TotalPlannedSteps)
                {
                    mission.State = DurableMissionState.Completed;
                }
                else
                {
                    mission.State = DurableMissionState.Running;
                }
            }

            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("[DurableMission] Recorded Checkpoint #{Index} '{StepName}' for Mission {MissionId}. Completed={Completed}",
                checkpoint.StepIndex, stepName, missionId, isCompleted);

            return checkpoint;
        }

        public async Task<DurableMission> ResumeMissionFromCheckpointAsync(Guid missionId, CancellationToken ct = default)
        {
            var mission = await _dbContext.DurableMissions
                .Include(m => m.Checkpoints)
                .FirstOrDefaultAsync(m => m.Id == missionId, ct);

            if (mission == null)
            {
                throw new KeyNotFoundException($"DurableMission {missionId} not found.");
            }

            int lastCompletedIndex = mission.Checkpoints.Where(c => c.IsCompleted).Select(c => c.StepIndex).DefaultIfEmpty(0).Max();
            int nextIndex = lastCompletedIndex + 1;

            var nextCheckpoint = mission.Checkpoints.FirstOrDefault(c => c.StepIndex == nextIndex);
            if (nextCheckpoint != null)
            {
                mission.State = DurableMissionState.Running;
                mission.CurrentCheckpointIndex = lastCompletedIndex;
                _logger.LogInformation("[DurableMission] Resumed Mission {MissionId} from Checkpoint #{NextIndex} '{StepName}'.",
                    missionId, nextIndex, nextCheckpoint.StepName);
            }
            else
            {
                mission.State = DurableMissionState.Completed;
            }

            await _dbContext.SaveChangesAsync(ct);
            return mission;
        }
    }
}
