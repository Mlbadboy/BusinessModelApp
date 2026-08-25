using System;

namespace BusinessModelApp.Core.Agents
{
    public class MissionWallet
    {
        public decimal TotalBudgetINR { get; set; } = 5000m;
        public decimal InferenceBudgetINR { get; set; } = 3000m;
        public decimal ResearchBudgetINR { get; set; } = 1000m;
        public decimal OutreachBudgetINR { get; set; } = 1000m;

        public decimal ConsumedSpendINR { get; set; } = 0m;
        public decimal ReservedSpendINR { get; set; } = 0m;

        public decimal RemainingSpendINR => Math.Max(0m, TotalBudgetINR - (ConsumedSpendINR + ReservedSpendINR));
        public double PercentConsumed => TotalBudgetINR > 0 ? (double)(ConsumedSpendINR / TotalBudgetINR) * 100.0 : 0.0;
        public bool IsExhausted => RemainingSpendINR <= 0m;

        public bool TryReserve(decimal amountINR)
        {
            if (amountINR <= 0) return true;
            if (RemainingSpendINR < amountINR) return false;

            ReservedSpendINR += amountINR;
            return true;
        }

        public void Reconcile(decimal reservedAmountINR, decimal actualCostINR)
        {
            ReservedSpendINR = Math.Max(0m, ReservedSpendINR - reservedAmountINR);
            ConsumedSpendINR += Math.Max(0m, actualCostINR);
        }

        public static MissionWallet CreateDefault(decimal totalBudgetINR)
        {
            return new MissionWallet
            {
                TotalBudgetINR = totalBudgetINR,
                InferenceBudgetINR = totalBudgetINR * 0.60m,
                ResearchBudgetINR = totalBudgetINR * 0.20m,
                OutreachBudgetINR = totalBudgetINR * 0.20m,
                ConsumedSpendINR = 0m,
                ReservedSpendINR = 0m
            };
        }
    }
}
