using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Lifecycle
{
    public enum CustomerAccountStatus
    {
        Onboarding = 0,
        Healthy = 1,
        AtRisk = 2,
        Churned = 3,
        Expanded = 4
    }

    public enum ExpansionStatus
    {
        Identified = 0,
        Qualified = 1,
        Proposed = 2,
        Won = 3,
        Lost = 4
    }

    public sealed class CustomerAccount
    {
        public string CustomerId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string CompanyName { get; init; }
        public decimal ContractedARR_INR { get; private set; }
        public decimal HealthScore { get; private set; } = 1.0m; // 0.0 to 1.0
        public CustomerAccountStatus Status { get; private set; } = CustomerAccountStatus.Onboarding;
        public DateTime CustomerSinceUtc { get; init; } = DateTime.UtcNow;
        public DateTime? LastAssessedUtc { get; private set; }

        public void SetInitialARR(decimal arrINR)
        {
            if (arrINR < 0) throw new ArgumentOutOfRangeException(nameof(arrINR), "ARR cannot be negative.");
            ContractedARR_INR = arrINR;
        }

        public void UpdateHealth(decimal score, CustomerAccountStatus newStatus)
        {
            HealthScore = Math.Clamp(score, 0.0m, 1.0m);
            Status = newStatus;
            LastAssessedUtc = DateTime.UtcNow;
        }

        public void ApplyExpansion(decimal additionalARR_INR)
        {
            if (additionalARR_INR <= 0) throw new ArgumentOutOfRangeException(nameof(additionalARR_INR), "Expansion ARR must be positive.");
            ContractedARR_INR += additionalARR_INR;
            Status = CustomerAccountStatus.Expanded;
        }

        public void MarkChurned()
        {
            Status = CustomerAccountStatus.Churned;
            ContractedARR_INR = 0m;
        }
    }

    public sealed class ChurnRiskAssessment
    {
        public string AssessmentId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string CustomerId { get; init; }
        public decimal ChurnProbability { get; init; } // 0.00 to 1.00
        public required string PrimaryRiskFactor { get; init; }
        public required string RootCauseAnalysis { get; init; }
        public List<string> ProposedInterventions { get; init; } = new();
        public DateTime AssessedAtUtc { get; init; } = DateTime.UtcNow;
        public bool InterventionTriggered { get; private set; }

        public void TriggerIntervention()
        {
            InterventionTriggered = true;
        }
    }

    public sealed class ExpansionOpportunity
    {
        public string ExpansionId { get; init; } = Guid.NewGuid().ToString("N");
        public required string TenantId { get; init; }
        public required string CustomerId { get; init; }
        public required string TargetModule { get; init; }
        public decimal AdditionalARR_INR { get; init; }
        public decimal ConfidenceScore { get; init; }
        public ExpansionStatus Status { get; private set; } = ExpansionStatus.Identified;
        public string? SignedContractRefSha256 { get; private set; }

        public void AdvanceStatus(ExpansionStatus newStatus, string? signedContractSha256 = null)
        {
            if (newStatus == ExpansionStatus.Won)
            {
                if (string.IsNullOrWhiteSpace(signedContractSha256) || signedContractSha256.Length != 64)
                    throw new InvalidOperationException("Expansion cannot be won without a valid 64-character SHA-256 signed contract hash (Law I40: Deal != Contract != Revenue).");
                SignedContractRefSha256 = signedContractSha256.Trim().ToLowerInvariant();
            }

            Status = newStatus;
        }
    }

    public sealed class NetRetentionCalculation
    {
        public required string TenantId { get; init; }
        public decimal StartingARR_INR { get; init; }
        public decimal ExpansionARR_INR { get; init; }
        public decimal ContractionARR_INR { get; init; }
        public decimal ChurnARR_INR { get; init; }

        public decimal EndingARR_INR => StartingARR_INR + ExpansionARR_INR - ContractionARR_INR - ChurnARR_INR;

        public decimal NetRevenueRetentionPct => StartingARR_INR > 0
            ? ((StartingARR_INR + ExpansionARR_INR - ContractionARR_INR - ChurnARR_INR) / StartingARR_INR) * 100m
            : 100m;

        public decimal GrossRevenueRetentionPct => StartingARR_INR > 0
            ? ((StartingARR_INR - ContractionARR_INR - ChurnARR_INR) / StartingARR_INR) * 100m
            : 100m;
    }
}
