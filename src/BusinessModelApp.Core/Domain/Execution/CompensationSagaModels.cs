using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessModelApp.Core.Domain.Execution
{
    public enum SagaStepStatus
    {
        Pending = 0,
        Executing = 1,
        Completed = 2,
        Failed = 3,
        Compensating = 4,
        Compensated = 5,
        CompensationFailed = 6
    }

    public enum SagaOverallStatus
    {
        Running = 0,
        Completed = 1,
        Compensating = 2,
        Compensated = 3,
        PartiallyCompensated = 4,
        Failed = 5
    }

    [Table("SagaExecutionStates")]
    public class SagaExecutionStateEntity
    {
        [Key]
        public Guid SagaId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid WorkspaceId { get; set; }

        public Guid MissionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SagaName { get; set; } = string.Empty;

        public SagaOverallStatus Status { get; set; } = SagaOverallStatus.Running;

        public int CurrentStepIndex { get; set; } = 0;
        public int TotalSteps { get; set; } = 0;

        /// <summary>
        /// JSON-serialized List&lt;SagaStepRecord&gt;
        /// </summary>
        public string StepsJson { get; set; } = "[]";

        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAtUtc { get; set; }
        public string? FailureReason { get; set; }
    }

    public class SagaStepRecord
    {
        public int StepIndex { get; set; }
        public string StepName { get; set; } = string.Empty;
        public string ForwardCapabilityId { get; set; } = string.Empty;
        public string ForwardPayloadJson { get; set; } = "{}";
        public string CompensationCapabilityId { get; set; } = string.Empty;
        public string CompensationPayloadJson { get; set; } = "{}";
        public SagaStepStatus Status { get; set; } = SagaStepStatus.Pending;
        public Guid? ExecutionReceiptId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
