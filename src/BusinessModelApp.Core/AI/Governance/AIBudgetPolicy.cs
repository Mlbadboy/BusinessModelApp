using System;

namespace BusinessModelApp.Core.AI.Governance
{
    public class AIBudgetPolicy
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrganizationId { get; set; }
        public Guid? WorkspaceId { get; set; } // Null for Organization-wide budget
        public decimal MonthlyBudgetCap { get; set; } = 50000m; // in INR
        public decimal DailyBudgetCap { get; set; } = 2500m;
        public decimal MaxCostPerRequest { get; set; } = 50m;
        public decimal WarningThresholdPercent { get; set; } = 80m; // Alert at 80%
        public bool EnforceHardCap { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class AIUsageDaily
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrganizationId { get; set; }
        public Guid WorkspaceId { get; set; }
        public DateOnly Date { get; set; }
        public int RequestCount { get; set; }
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens => InputTokens + OutputTokens;
        public decimal EstimatedCost { get; set; }
        public int FallbackCount { get; set; }
        public int CacheHits { get; set; }
        public long TotalLatencyMs { get; set; }
        public double AverageLatencyMs => RequestCount > 0 ? (double)TotalLatencyMs / RequestCount : 0.0;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class BudgetReservationResult
    {
        public bool IsAllowed { get; set; }
        public string ReservationId { get; set; } = string.Empty;
        public decimal EstimatedCost { get; set; }
        public decimal RemainingMonthlyBudget { get; set; }
        public decimal PercentageConsumed { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class AIBudgetExceededException : Exception
    {
        public Guid OrganizationId { get; }
        public Guid WorkspaceId { get; }
        public decimal AttemptedCost { get; }
        public decimal RemainingBudget { get; }

        public AIBudgetExceededException(Guid orgId, Guid wsId, decimal attemptedCost, decimal remainingBudget, string message)
            : base(message)
        {
            OrganizationId = orgId;
            WorkspaceId = wsId;
            AttemptedCost = attemptedCost;
            RemainingBudget = remainingBudget;
        }
    }
}
