using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BusinessModelApp.Core.Observability;

namespace BusinessModelApp.Infrastructure.Observability
{
    public class CausalTracer : ICausalTracer
    {
        private readonly ConcurrentDictionary<string, CausalTraceSpan> _spans = new();
        private readonly ConcurrentDictionary<Guid, List<string>> _missionSpanIndices = new();

        public CausalTraceSpan StartSpan(Guid missionId, string agentId, string capability, string intent, string? parentSpanId = null)
        {
            var span = new CausalTraceSpan
            {
                TraceId = Guid.NewGuid().ToString("N"),
                SpanId = Guid.NewGuid().ToString("N"),
                ParentSpanId = parentSpanId,
                MissionId = missionId,
                AgentId = agentId,
                Capability = capability,
                Intent = intent,
                TimestampUtc = DateTime.UtcNow
            };

            _spans[span.SpanId] = span;

            var list = _missionSpanIndices.GetOrAdd(missionId, _ => new List<string>());
            lock (list)
            {
                list.Add(span.SpanId);
            }

            return span;
        }

        public void EndSpan(string spanId, string modelId, string outputSummary, decimal costINR, long durationMs)
        {
            if (_spans.TryGetValue(spanId, out var span))
            {
                span.ModelId = modelId;
                span.OutputSummary = outputSummary;
                span.CostINR = costINR;
                span.DurationMs = durationMs;
            }
        }

        public CausalMissionTrace GetMissionCausalGraph(Guid missionId)
        {
            var trace = new CausalMissionTrace { MissionId = missionId };

            if (_missionSpanIndices.TryGetValue(missionId, out var spanIds))
            {
                lock (spanIds)
                {
                    trace.Spans = spanIds
                        .Select(id => _spans.TryGetValue(id, out var s) ? s : null)
                        .Where(s => s != null)
                        .OrderBy(s => s!.TimestampUtc)
                        .ToList()!;
                }
            }

            return trace;
        }
    }
}
