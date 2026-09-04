using System;
using System.Collections.Generic;
using System.Text.Json;
using BusinessModelApp.Core.Domain.Common;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Reality;

namespace BusinessModelApp.Core.Domain.WorldModel
{
    /// <summary>
    /// Point-in-time snapshot of the company's verified truth state (Company World Model).
    /// Every field is backed by a TruthMetric linking to external evidence.
    /// Invariant: Agent memory is NOT truth. Hypotheses cannot become facts without evidence.
    /// </summary>
    public class CompanySnapshot : Entity
    {
        public Guid WorkspaceId { get; set; }
        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
        public RevenueBaselineState RevenueBaselineState { get; set; } = RevenueBaselineState.Unavailable;

        // Structured World Model Sub-Realities (Serialized in SerializedTruthMetricsJson)
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public FinancialReality Financial { get; set; } = new();

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public CommercialReality Commercial { get; set; } = new();

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public DeliveryReality Delivery { get; set; } = new();

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public ConnectorReality Connectors { get; set; } = new();

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public RevenueBaseline RevenueBaseline { get; set; } = new();

        // Evidence-Addressable Metrics (Field-by-Field Truth)
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public TruthMetric<decimal> VerifiedRevenueINR { get; set; } = TruthMetric<decimal>.Unavailable("Awaiting gateway reconciliation.");

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public TruthMetric<decimal> VerifiedSettledCashINR { get; set; } = TruthMetric<decimal>.Unavailable("Awaiting bank settlement records.");

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public TruthMetric<decimal> ActivePipelineINR { get; set; } = TruthMetric<decimal>.Unavailable("Awaiting active opportunities.");

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public TruthMetric<decimal> QualifiedPipelineINR { get; set; } = TruthMetric<decimal>.Unavailable("Awaiting qualified deal pipeline.");

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public TruthMetric<int> OpenOpportunitiesCount { get; set; } = new();

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public TruthMetric<int> VerifiedProspectsCount { get; set; } = new();

        // Delivery & Capacity Metrics
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public TruthMetric<int> ActiveDeliveryProjectsCount { get; set; } = new();

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public TruthMetric<int> AvailableDeliverySlots { get; set; } = new() { Value = 4, Source = MetricProvenanceSource.VerifiedFact, Confidence = 1.0 };

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public TruthMetric<decimal> ReservedComputeBudgetINR { get; set; } = new();

        // Governance & Financial Risk
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public TruthMetric<decimal> OutstandingInvoicesINR { get; set; } = new();

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public TruthMetric<double> PaymentRiskScore { get; set; } = new();

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public TruthMetric<double> OverallEvidenceCoverageScore { get; set; } = new();

        // Strategy & Active Mandate Reference
        public Guid? ActiveObjectiveId { get; set; }
        public string ActiveStrategyVersion { get; set; } = "None";
        public string SnapshotDigestHash { get; set; } = string.Empty;
        public string SerializedTruthMetricsJson { get; set; } = "{}";

        public void SynchronizeRealities()
        {
            // Sync top-level convenience metrics with sub-realities
            Financial.TotalRevenueINR = VerifiedRevenueINR;
            Financial.CashInBankINR = VerifiedSettledCashINR;
            Financial.OutstandingReceivablesINR = OutstandingInvoicesINR;

            Commercial.OpenOpportunitiesCount = OpenOpportunitiesCount;
            Commercial.QualifiedProspectsCount = VerifiedProspectsCount;
            Commercial.TotalPipelineINR = ActivePipelineINR;
            Commercial.WeightedPipelineINR = QualifiedPipelineINR;

            Delivery.ActiveDeliveryProjectsCount = ActiveDeliveryProjectsCount;
            Delivery.AvailableDeliverySlots = AvailableDeliverySlots;
        }

        public void PackTruthMetrics()
        {
            SynchronizeRealities();

            var payload = new
            {
                VerifiedRevenueINR,
                VerifiedSettledCashINR,
                ActivePipelineINR,
                QualifiedPipelineINR,
                OpenOpportunitiesCount,
                VerifiedProspectsCount,
                ActiveDeliveryProjectsCount,
                AvailableDeliverySlots,
                ReservedComputeBudgetINR,
                OutstandingInvoicesINR,
                PaymentRiskScore,
                OverallEvidenceCoverageScore,
                Financial,
                Commercial,
                Delivery,
                Connectors,
                RevenueBaseline
            };
            SerializedTruthMetricsJson = JsonSerializer.Serialize(payload);
        }
    }
}
