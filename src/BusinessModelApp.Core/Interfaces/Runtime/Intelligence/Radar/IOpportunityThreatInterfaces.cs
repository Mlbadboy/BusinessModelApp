using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Forecasting;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Kernel;
using BusinessModelApp.Core.Domain.Runtime.Intelligence.Radar;

namespace BusinessModelApp.Core.Interfaces.Runtime.Intelligence.Radar
{
    public class ConstraintFeasibilityResult
    {
        public ConstraintFeasibilityStatus Status { get; set; } = ConstraintFeasibilityStatus.Unknown;
        public string? ViolationReason { get; set; }
        public List<string> SafeAlternatives { get; set; } = new();
        public string ConstraintSnapshotId { get; set; } = Guid.NewGuid().ToString("N");
    }

    public class RadarScanResult
    {
        public string ScanId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public int OpportunitiesDetected { get; set; }
        public int ThreatsDetected { get; set; }
        public int SignalsConstrained { get; set; }
        public IReadOnlyList<RadarSignal> Signals { get; set; } = Array.Empty<RadarSignal>();
        public DateTime ScanCompletedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public interface ISignificanceScorer
    {
        RadarSignificancePolicy GetActivePolicy(string tenantId);
        void RegisterPolicy(RadarSignificancePolicy policy);
        RadarSignificanceBreakdown ScoreSignal(
            RadarSignalType type,
            decimal monetaryImpactINR,
            double probabilityPercent,
            SignalUrgency urgency,
            int persistenceEpochs,
            double exposurePercent,
            double confidencePercent,
            RadarSignificancePolicy policy);
    }

    public interface IOpportunityDetector
    {
        Task<IReadOnlyList<RadarSignal>> DetectOpportunitiesAsync(
            string tenantId,
            IReadOnlyList<KpiObservation> observations,
            IReadOnlyList<ForecastOutput> forecasts,
            CancellationToken ct = default);
    }

    public interface IThreatDetector
    {
        Task<IReadOnlyList<RadarSignal>> DetectThreatsAsync(
            string tenantId,
            IReadOnlyList<KpiObservation> observations,
            IReadOnlyList<ForecastOutput> forecasts,
            IReadOnlyList<RegimeAssessment>? regimeAssessments = null,
            CancellationToken ct = default);
    }

    public interface IRadarConstraintEvaluator
    {
        Task<ConstraintFeasibilityResult> EvaluateConstraintsAsync(
            string tenantId,
            RadarSignal signal,
            CancellationToken ct = default);
    }

    public interface IRadarSignalStore
    {
        Task SaveSignalAsync(RadarSignal signal, CancellationToken ct = default);
        Task<RadarSignal?> GetSignalAsync(string tenantId, string signalId, CancellationToken ct = default);
        Task<IReadOnlyList<RadarSignal>> GetSignalsAsync(
            string tenantId,
            RadarSignalType? type = null,
            RadarSignalLifecycleState? state = null,
            CancellationToken ct = default);
        Task<bool> UpdateLifecycleStateAsync(
            string tenantId,
            string signalId,
            RadarSignalLifecycleState newState,
            string reason,
            CancellationToken ct = default);
        Task ApplyTemporalDecayAsync(string tenantId, TimeSpan elapsed, CancellationToken ct = default);
        Task<bool> ReinforceSignalAsync(string tenantId, string signalId, double boost, CancellationToken ct = default);
        Task SaveAnalysisRecordAsync(RadarAnalysisRecord record, CancellationToken ct = default);
        Task<RadarAnalysisRecord?> GetAnalysisRecordAsync(string tenantId, string signalId, CancellationToken ct = default);
    }

    public interface IOpportunityThreatRadarOrchestrator
    {
        Task<RadarScanResult> ExecuteRadarScanAsync(
            string tenantId,
            IReadOnlyList<KpiObservation> observations,
            IReadOnlyList<ForecastOutput> forecasts,
            IReadOnlyList<RegimeAssessment>? regimeAssessments = null,
            CancellationToken ct = default);
    }
}
