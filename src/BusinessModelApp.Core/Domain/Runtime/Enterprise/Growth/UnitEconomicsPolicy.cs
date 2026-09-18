using System;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth
{
    /// <summary>
    /// Business-defined dynamic unit economics policy governing commercial proposals,
    /// negotiations, cohort sustainability, and autonomous capital allocation.
    /// Eliminates hardcoded universal constants while preserving strict economic governance.
    /// </summary>
    public sealed class UnitEconomicsPolicy
    {
        public string PolicyId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string BusinessObjectiveId { get; init; }
        public string PolicyName { get; init; } = "Standard Commercial Policy";

        // Dynamic thresholds defined by business model (e.g. SaaS, Marketplace, Services)
        public decimal MinGrossMarginPercent { get; init; } = 35.0m;
        public decimal MinContributionMarginPercent { get; init; } = 25.0m;
        public decimal MinLtvToCacRatio { get; init; } = 3.0m;
        public int MaxPaybackPeriodMonths { get; init; } = 12;
        public decimal MaxAllowableAnnualChurnPercent { get; init; } = 15.0m;
        public decimal MaxSelfDiscountPercentWithoutSignoff { get; init; } = 10.0m;
        public string? HumanSignoffId { get; init; }
        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;

        public static UnitEconomicsPolicy CreateDefaultSaaS(string tenantId, string businessObjectiveId, string? humanSignoffId = null) =>
            new()
            {
                TenantId = tenantId,
                BusinessObjectiveId = businessObjectiveId,
                PolicyName = "High-Growth B2B SaaS Policy",
                MinGrossMarginPercent = 65.0m,
                MinContributionMarginPercent = 45.0m,
                MinLtvToCacRatio = 4.0m,
                MaxPaybackPeriodMonths = 9,
                MaxAllowableAnnualChurnPercent = 7.0m,
                MaxSelfDiscountPercentWithoutSignoff = 15.0m,
                HumanSignoffId = humanSignoffId
            };

        public static UnitEconomicsPolicy CreateDefaultEnterpriseServices(string tenantId, string businessObjectiveId, string? humanSignoffId = null) =>
            new()
            {
                TenantId = tenantId,
                BusinessObjectiveId = businessObjectiveId,
                PolicyName = "Enterprise Services & Solutions Policy",
                MinGrossMarginPercent = 35.0m,
                MinContributionMarginPercent = 20.0m,
                MinLtvToCacRatio = 2.5m,
                MaxPaybackPeriodMonths = 14,
                MaxAllowableAnnualChurnPercent = 12.0m,
                MaxSelfDiscountPercentWithoutSignoff = 8.0m,
                HumanSignoffId = humanSignoffId
            };

        public bool ValidateProposal(decimal price, decimal estimatedCostBasis, out string failureReason)
        {
            if (price <= 0)
            {
                failureReason = "Proposal price must be greater than zero.";
                return false;
            }

            decimal grossProfit = price - estimatedCostBasis;
            decimal marginPercent = (grossProfit / price) * 100.0m;

            if (marginPercent < MinGrossMarginPercent)
            {
                failureReason = $"Proposal gross margin ({marginPercent:F1}%) is below policy threshold ({MinGrossMarginPercent:F1}%).";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        public bool ValidateNegotiationDiscount(decimal initialPrice, decimal agreedPrice, bool hasPrg1Signoff, out string failureReason)
        {
            if (initialPrice <= 0)
            {
                failureReason = "Initial price must be greater than zero.";
                return false;
            }

            decimal discountPercent = ((initialPrice - agreedPrice) / initialPrice) * 100.0m;
            if (discountPercent > MaxSelfDiscountPercentWithoutSignoff && !hasPrg1Signoff)
            {
                failureReason = $"Discount of {discountPercent:F1}% exceeds maximum allowable self-discount ({MaxSelfDiscountPercentWithoutSignoff:F1}%) without PRG-1 human authorization.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        public bool ValidateCohortEconomics(decimal ltvToCacRatio, int paybackMonths, decimal annualChurnRate, out string failureReason)
        {
            if (ltvToCacRatio < MinLtvToCacRatio)
            {
                failureReason = $"Cohort LTV:CAC ({ltvToCacRatio:F2}x) violates policy minimum ({MinLtvToCacRatio:F2}x).";
                return false;
            }

            if (paybackMonths > MaxPaybackPeriodMonths)
            {
                failureReason = $"Cohort payback period ({paybackMonths} months) exceeds policy maximum ({MaxPaybackPeriodMonths} months).";
                return false;
            }

            if (annualChurnRate > MaxAllowableAnnualChurnPercent)
            {
                failureReason = $"Cohort annual churn rate ({annualChurnRate:F1}%) exceeds policy limit ({MaxAllowableAnnualChurnPercent:F1}%).";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }
    }
}
