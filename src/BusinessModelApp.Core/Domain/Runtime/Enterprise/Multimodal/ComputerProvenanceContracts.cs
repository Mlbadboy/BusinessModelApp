using System;
using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Multimodal
{
    public class ComputerTraceRecord
    {
        public string TraceId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string ResponsibilityId { get; set; } = string.Empty;
        public string MissionGraphId { get; set; } = string.Empty;
        public string MissionRunId { get; set; } = string.Empty;
        public string MissionNodeId { get; set; } = string.Empty;
        public string AgentInstanceId { get; set; } = string.Empty;
        public string CapabilityId { get; set; } = string.Empty;
        public string ComputerSessionId { get; set; } = string.Empty;
        public string EnvironmentSnapshotId { get; set; } = string.Empty;
        public string ActionProposalId { get; set; } = string.Empty;
        public string ExecutionIntentId { get; set; } = string.Empty;
        public string ExecutionAttemptId { get; set; } = string.Empty;
        public string ExternalEffect { get; set; } = "None";
        public string OutcomeId { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string TraceHash { get; set; } = string.Empty;

        public static string ComputeSha256(string content)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content ?? string.Empty));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        public string ComputeTraceHash()
        {
            var raw = $"{TenantId}:{ResponsibilityId}:{MissionGraphId}:{ComputerSessionId}:{ActionProposalId}:{ExecutionAttemptId}:{ExternalEffect}:{OutcomeId}";
            return ComputeSha256(raw);
        }
    }

    public class ReplaySessionDescriptor
    {
        public string OriginalSessionId { get; set; } = string.Empty;
        public string ReplaySessionId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public int SnapshotCount { get; set; }
        public string DeterministicHash { get; set; } = string.Empty;
        public DateTime ReplayedAtUtc { get; set; } = DateTime.UtcNow;
        public bool IsOriginalPreserved { get; set; } = true;
    }
}
