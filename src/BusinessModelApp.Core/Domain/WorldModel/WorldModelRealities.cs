using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Reality;

namespace BusinessModelApp.Core.Domain.WorldModel
{
    public class FinancialReality
    {
        public TruthMetric<decimal> CashInBankINR { get; set; } = TruthMetric<decimal>.Unavailable("Bank integration unconfigured.");
        public TruthMetric<decimal> MonthlyBurnINR { get; set; } = TruthMetric<decimal>.Unavailable("Cost baseline unavailable.");
        public TruthMetric<int> RunwayDays { get; set; } = TruthMetric<int>.Unavailable("Runway calculation unavailable.");
        public TruthMetric<decimal> TotalRevenueINR { get; set; } = TruthMetric<decimal>.Unavailable("Gateway reconciliation unavailable.");
        public TruthMetric<decimal> OutstandingReceivablesINR { get; set; } = TruthMetric<decimal>.Unavailable("Accounting unconfigured.");
        public TruthMetric<decimal> ContractedRevenueINR { get; set; } = TruthMetric<decimal>.Unavailable("Contracts unconfigured.");
        public TruthMetric<decimal> MonthlyRecurringRevenueINR { get; set; } = TruthMetric<decimal>.Unavailable("Subscriptions unconfigured.");
    }

    public class CommercialReality
    {
        public TruthMetric<int> TotalAccountsCount { get; set; } = new();
        public TruthMetric<int> QualifiedProspectsCount { get; set; } = new();
        public TruthMetric<int> OpenOpportunitiesCount { get; set; } = new();
        public TruthMetric<decimal> TotalPipelineINR { get; set; } = new();
        public TruthMetric<decimal> WeightedPipelineINR { get; set; } = new();
        public TruthMetric<double> HistoricalWinRate { get; set; } = TruthMetric<double>.Estimated(0.20, "Industry default baseline (AI hypothesis).", 0.4);
        public TruthMetric<decimal> HistoricalACVINR { get; set; } = TruthMetric<decimal>.Estimated(1000000m, "Industry standard deal size (AI hypothesis).", 0.4);
        public TruthMetric<int> AverageSalesCycleDays { get; set; } = TruthMetric<int>.Estimated(45, "Estimated typical B2B cycle.", 0.5);
        public TruthMetric<decimal> CustomerAcquisitionCostINR { get; set; } = TruthMetric<decimal>.Unavailable("Attribution data unavailable.");
        public string IdealCustomerProfileSummary { get; set; } = "B2B Mid-Market & Enterprise Organizations requiring Custom Software and AI Integrations.";
    }

    public class DeliveryReality
    {
        public TruthMetric<int> TotalEngineersCount { get; set; } = new() { Value = 5, Source = Objectives.MetricProvenanceSource.VerifiedFact, Confidence = 1.0 };
        public TruthMetric<int> ActiveAutonomousAgentsCount { get; set; } = new() { Value = 8, Source = Objectives.MetricProvenanceSource.VerifiedFact, Confidence = 1.0 };
        public TruthMetric<int> TotalDeliverySlots { get; set; } = new() { Value = 6, Source = Objectives.MetricProvenanceSource.VerifiedFact, Confidence = 1.0 };
        public TruthMetric<int> AvailableDeliverySlots { get; set; } = new() { Value = 4, Source = Objectives.MetricProvenanceSource.VerifiedFact, Confidence = 1.0 };
        public TruthMetric<double> TeamUtilizationPercent { get; set; } = new() { Value = 65.0, Source = Objectives.MetricProvenanceSource.VerifiedFact, Confidence = 0.9 };
        public TruthMetric<int> ActiveDeliveryProjectsCount { get; set; } = new();
        public List<string> CoreCapabilities { get; set; } = new() { "Full-Stack Development", "Cloud Architecture", "Enterprise AI Solutions", "Data Engineering" };
    }

    public enum ConnectorHealthState
    {
        NotConfigured = 0,
        Connected = 1,
        Degraded = 2,
        Disconnected = 3
    }

    public class ConnectorRealityItem
    {
        public string ConnectorType { get; set; } = string.Empty; // "CRM", "Accounting", "Bank", "Email", "Calendar", "Website", "GooglePlaces", "Contracts", "Payments"
        public ConnectorHealthState HealthState { get; set; } = ConnectorHealthState.NotConfigured;
        public string ProviderName { get; set; } = string.Empty;
        public DateTime? LastTelemetryAt { get; set; }
        public string? EvidenceHash { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
    }

    public class ConnectorReality
    {
        public List<ConnectorRealityItem> Connectors { get; set; } = new();

        public bool IsAnyPaymentConnectorActive =>
            Connectors.Exists(c => (c.ConnectorType == "Payments" || c.ConnectorType == "Bank") && c.HealthState == ConnectorHealthState.Connected);

        public bool IsCrmConnected =>
            Connectors.Exists(c => c.ConnectorType == "CRM" && c.HealthState == ConnectorHealthState.Connected);
    }
}
