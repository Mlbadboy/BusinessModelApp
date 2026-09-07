using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Reality
{
    /// <summary>
    /// Epistemic status of a business reality data point.
    /// </summary>
    public enum RealityStatus
    {
        LiveVerified,
        LiveUnverified,
        Stale,
        Unknown,
        Simulation,
        Replay,
        NotConnected,
        Error
    }

    /// <summary>
    /// Epistemic classification of truth.
    /// </summary>
    public enum TruthClassification
    {
        Fact,
        Inference,
        Simulation,
        Projection,
        Policy
    }

    /// <summary>
    /// Active operating mode of the Charlie Business OS.
    /// In production, only Live is permitted.
    /// </summary>
    public enum RealityMode
    {
        Live,
        Simulation,
        Replay,
        Development
    }

    /// <summary>
    /// Immutable, cryptographic envelope wrapping all production business metrics.
    /// Implements Invariant I17-PR: No metric without provenance.
    /// </summary>
    public sealed record RealityEnvelope<T>(
        T? Value,
        RealityStatus Status,
        TruthClassification Classification,
        string TenantId,
        string? Source,
        string? SourceRecordId,
        DateTimeOffset? ObservedAt,
        DateTimeOffset? VerifiedAt,
        string? ProvenanceId,
        string? IntegrityHash
    );

    /// <summary>
    /// Generic metric value representation for dashboard and API surfaces.
    /// </summary>
    public sealed record MetricValue<T>(
        T? Value,
        RealityStatus Status,
        TruthClassification Classification,
        string MetricKey,
        string Label,
        string Unit,
        string TenantId,
        string Source,
        string? SourceRecordId,
        DateTimeOffset ObservedAt,
        TimeSpan FreshnessSla,
        bool IsFresh,
        string ProvenanceId,
        string IntegrityHash,
        string? EpistemicRationale
    );
}
