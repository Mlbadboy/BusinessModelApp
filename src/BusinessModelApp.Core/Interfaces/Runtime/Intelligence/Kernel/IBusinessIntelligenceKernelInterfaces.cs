using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Kernel;

namespace BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Kernel
{
    public interface IKpiRegistry
    {
        Task RegisterKpiAsync(KpiDefinition definition, CancellationToken ct = default);
        Task<KpiDefinition?> GetKpiAsync(string tenantId, string metricId, CancellationToken ct = default);
        Task<IReadOnlyList<KpiDefinition>> GetAllKpisAsync(string tenantId, CancellationToken ct = default);
    }

    public interface IKpiObservationStore
    {
        Task RecordObservationAsync(KpiObservation observation, CancellationToken ct = default);
        Task<IReadOnlyList<KpiObservation>> GetObservationsAsync(string tenantId, string metricId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
        Task<KpiObservation?> GetLatestObservationAsync(string tenantId, string metricId, CancellationToken ct = default);
    }

    public interface IAnalysisRecordStore
    {
        Task RecordAnalysisAsync(AnalysisRecord record, CancellationToken ct = default);
        Task<AnalysisRecord?> GetAnalysisAsync(string analysisId, CancellationToken ct = default);
        Task<IReadOnlyList<AnalysisRecord>> GetAnalysesForMetricAsync(string tenantId, string metricId, CancellationToken ct = default);
    }

    public interface IBaselineQualityEvaluator
    {
        BaselineQualityAssessment AssessBaseline(IReadOnlyList<KpiObservation> observations, AnalysisMethodPolicy policy, DateTime evaluationTimeUtc);
    }

    public interface IAnomalyDetector
    {
        Task<MetricAnomaly?> DetectAnomalyAsync(string tenantId, string metricId, CancellationToken ct = default);
        Task<IReadOnlyList<MetricAnomaly>> GetAllActiveAnomaliesAsync(string tenantId, CancellationToken ct = default);
    }

    public interface ITrendAnalyzer
    {
        Task<MetricTrend?> AnalyzeTrendAsync(string tenantId, string metricId, CancellationToken ct = default);
    }

    public interface IMetricRelationshipAnalyzer
    {
        Task<MetricRelationship?> AnalyzeRelationshipAsync(string tenantId, string metricAId, string metricBId, CancellationToken ct = default);
    }

    public interface IBusinessStateInterpreter
    {
        Task<InterpretedBusinessState> InterpretStateAsync(string tenantId, ObservedBusinessState observedState, CancellationToken ct = default);
    }

    public interface IEvidenceLinkedExplainer
    {
        Task<EvidenceLinkedInsight> GenerateInsightAsync(
            string tenantId,
            string metricId,
            EpistemicKind kind,
            string title,
            string narrative,
            string? anomalyId = null,
            string? trendId = null,
            CancellationToken ct = default);

        Task<IReadOnlyList<EvidenceLinkedInsight>> GetInsightsAsync(string tenantId, CancellationToken ct = default);
    }

    public interface IBusinessIntelligenceKernel
    {
        IKpiRegistry Registry { get; }
        IKpiObservationStore Observations { get; }
        IAnomalyDetector Anomalies { get; }
        ITrendAnalyzer Trends { get; }
        IMetricRelationshipAnalyzer Relationships { get; }
        IBusinessStateInterpreter StateInterpreter { get; }
        IEvidenceLinkedExplainer Explainer { get; }

        Task IngestObservationAsync(KpiObservation observation, CancellationToken ct = default);
        Task<InterpretedBusinessState> EvaluateEnterpriseStateAsync(string tenantId, ObservedBusinessState observedState, CancellationToken ct = default);
    }
}
