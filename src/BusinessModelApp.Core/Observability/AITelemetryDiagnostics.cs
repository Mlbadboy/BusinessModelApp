using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace BusinessModelApp.Core.Observability
{
    public static class AITelemetryDiagnostics
    {
        public const string ActivitySourceName = "BusinessModelApp.AI";
        public const string MeterName = "BusinessModelApp.AI.Metrics";
        public const string ServiceVersion = "1.0.0";

        // Activity Source for W3C Distributed Tracing
        public static readonly ActivitySource ActivitySource = new ActivitySource(ActivitySourceName, ServiceVersion);

        // Meter for Dimensional Metrics
        public static readonly Meter Meter = new Meter(MeterName, ServiceVersion);

        // Metric Instruments
        public static readonly Counter<long> InferenceCounter = Meter.CreateCounter<long>(
            "ai_inferences_total",
            description: "Total number of AI inferences executed via OmniRoute gateway");

        public static readonly Counter<double> SpendCounter = Meter.CreateCounter<double>(
            "ai_spend_inr_total",
            unit: "INR",
            description: "Total deterministic AI financial spend in INR");

        public static readonly Counter<long> CacheHitCounter = Meter.CreateCounter<long>(
            "ai_cache_hits_total",
            description: "Total number of AI responses served from cache");

        public static readonly Histogram<double> DurationHistogram = Meter.CreateHistogram<double>(
            "ai_inference_duration_ms",
            unit: "ms",
            description: "Inference execution latency in milliseconds");

        public static readonly UpDownCounter<int> CircuitBreakerGauge = Meter.CreateUpDownCounter<int>(
            "ai_circuit_breaker_open",
            description: "Circuit breaker status (1 = Open/Tripped, 0 = Closed/Healthy)");
    }
}
