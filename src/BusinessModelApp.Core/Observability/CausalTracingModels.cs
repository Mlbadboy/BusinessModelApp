using System;
using System.Collections.Generic;
using System.Linq;

namespace BusinessModelApp.Core.Observability
{
    public class CausalTraceSpan
    {
        public string TraceId { get; set; } = Guid.NewGuid().ToString("N");
        public string SpanId { get; set; } = Guid.NewGuid().ToString("N");
        public string? ParentSpanId { get; set; }
        public Guid MissionId { get; set; }
        public string AgentId { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public string Capability { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
        public string InputSummary { get; set; } = string.Empty;
        public string OutputSummary { get; set; } = string.Empty;
        public decimal CostINR { get; set; } = 0m;
        public long DurationMs { get; set; } = 0;
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }

    public class CausalMissionTrace
    {
        public Guid MissionId { get; set; }
        public List<CausalTraceSpan> Spans { get; set; } = new();
        public decimal TotalCostINR => Spans.Sum(s => s.CostINR);
        public long TotalDurationMs => Spans.Sum(s => s.DurationMs);

        public List<string> ExplainWhyActionWasTaken(string spanId)
        {
            var reasons = new List<string>();
            var target = Spans.FirstOrDefault(s => s.SpanId == spanId);
            if (target == null) return reasons;

            reasons.Add($"Action: '{target.Capability}' by '{target.AgentId}'. Intent: '{target.Intent}'. Result: '{target.OutputSummary}'");

            var current = target;
            while (!string.IsNullOrEmpty(current.ParentSpanId))
            {
                var parent = Spans.FirstOrDefault(s => s.SpanId == current.ParentSpanId);
                if (parent == null) break;

                reasons.Add($"Caused by Parent Step: '{parent.Capability}' by '{parent.AgentId}' (Intent: '{parent.Intent}')");
                current = parent;
            }

            return reasons;
        }
    }

    public interface ICausalTracer
    {
        CausalTraceSpan StartSpan(Guid missionId, string agentId, string capability, string intent, string? parentSpanId = null);
        void EndSpan(string spanId, string modelId, string outputSummary, decimal costINR, long durationMs);
        CausalMissionTrace GetMissionCausalGraph(Guid missionId);
    }
}
