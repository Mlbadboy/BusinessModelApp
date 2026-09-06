using System;

namespace BusinessModelApp.Core.Domain.Runtime
{
    public class RuntimeCheckpoint
    {
        public CheckpointId CheckpointId { get; init; }
        public RuntimeRunId RunId { get; init; }
        public MissionNodeId? CurrentNodeId { get; init; }
        public long SequenceNumber { get; init; }
        public string StateJson { get; init; } = string.Empty;
        public string StateHash { get; init; } = string.Empty;
        public RunState RunState { get; init; }
        public FenceToken FenceToken { get; init; }
        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
        public string SchemaVersion { get; init; } = "1.0.0";
        public string PolicySnapshotId { get; init; } = string.Empty;
        public string CapabilitySnapshotId { get; init; } = string.Empty;

        public RuntimeCheckpoint(
            CheckpointId checkpointId,
            RuntimeRunId runId,
            long sequenceNumber,
            string stateJson,
            RunState runState,
            FenceToken fenceToken)
        {
            CheckpointId = checkpointId;
            RunId = runId;
            SequenceNumber = sequenceNumber;
            StateJson = stateJson ?? string.Empty;
            StateHash = RuntimeEventEnvelope.ComputeSha256(StateJson);
            RunState = runState;
            FenceToken = fenceToken;
        }
    }
}
