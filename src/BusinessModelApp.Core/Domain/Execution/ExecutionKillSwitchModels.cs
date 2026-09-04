using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessModelApp.Core.Domain.Execution
{
    public enum ExecutionKillSwitchTier
    {
        Platform = 0,    // Halts execution across the entire platform
        Tenant = 1,      // Halts all executions for a specific workspace/tenant
        Mission = 2,     // Halts execution for a specific mission
        Agent = 3,       // Halts execution for a specific agent
        Capability = 4,  // Halts execution for a specific capability (e.g. Payment.Execute)
        Action = 5       // Halts a specific action execution request
    }

    [Table("ExecutionKillSwitches")]
    public class ExecutionKillSwitchEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public ExecutionKillSwitchTier Tier { get; set; }

        public Guid? WorkspaceId { get; set; }

        [MaxLength(100)]
        public string? TargetIdentifier { get; set; } // e.g. MissionId string, AgentId, or CapabilityId

        public bool IsActive { get; set; } = true;

        [Required]
        public string Reason { get; set; } = string.Empty;

        public Guid TriggeredByUserId { get; set; }
        public DateTime TriggeredAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? DeactivatedAtUtc { get; set; }
        public Guid? DeactivatedByUserId { get; set; }
    }
}
