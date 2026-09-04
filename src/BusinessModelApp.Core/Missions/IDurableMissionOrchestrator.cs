using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Decisions;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Strategy;

namespace BusinessModelApp.Core.Missions
{
    public interface IDurableMissionOrchestrator
    {
        /// <summary>
        /// Translates an approved strategy and decision into a durable mission plan with discrete checkpoints.
        /// Invariant: State machine starts at Created/Planned. Does not execute external actions without governance.
        /// </summary>
        Task<DurableMission> CreateDurableMissionPlanAsync(
            DecisionRecord decision,
            BusinessStrategy strategy,
            CancellationToken ct = default);

        /// <summary>
        /// Persists a discrete checkpoint for an executing mission step.
        /// Survives API restarts, browser closures, and human approval pauses.
        /// </summary>
        Task<DurableMissionCheckpoint> RecordCheckpointAsync(
            Guid missionId,
            string stepName,
            string agentRole,
            string outputPayloadJson,
            bool isCompleted,
            CancellationToken ct = default);

        /// <summary>
        /// Resumes a paused, blocked, or interrupted mission from its last completed checkpoint + 1.
        /// </summary>
        Task<DurableMission> ResumeMissionFromCheckpointAsync(
            Guid missionId,
            CancellationToken ct = default);
    }
}
