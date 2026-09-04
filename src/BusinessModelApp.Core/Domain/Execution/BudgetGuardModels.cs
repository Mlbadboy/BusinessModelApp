using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessModelApp.Core.Domain.Execution
{
    [Table("TenantWallets")]
    public class TenantWalletEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid WorkspaceId { get; set; }

        public decimal BalanceINR { get; set; } = 1000000m; // ₹10,00,000 baseline
        public decimal AllocatedBudgetINR { get; set; } = 500000m;
        public decimal PendingCommitmentsINR { get; set; } = 0m;
        public decimal DailyCapINR { get; set; } = 100000m; // Daily spend cap
        public decimal TodaySpendINR { get; set; } = 0m;
        public DateTime LastResetDateUtc { get; set; } = DateTime.UtcNow.Date;

        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public void CheckAndResetDailySpend()
        {
            var today = DateTime.UtcNow.Date;
            if (LastResetDateUtc < today)
            {
                TodaySpendINR = 0m;
                LastResetDateUtc = today;
            }
        }
    }

    [Table("BudgetReservations")]
    public class BudgetReservationEntity
    {
        [Key]
        public Guid ReservationId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid WorkspaceId { get; set; }

        public Guid RequestId { get; set; }
        public decimal AmountINR { get; set; }
        public bool IsSettled { get; set; } = false;
        public bool IsReleased { get; set; } = false;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddMinutes(15);
    }
}
