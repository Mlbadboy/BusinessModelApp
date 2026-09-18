using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Commercial
{
    public class InMemoryInboundResponseStore : IInboundResponseStore
    {
        private readonly ConcurrentDictionary<string, InboundMessageEvent> _messages = new();

        public Task SaveInboundMessageAsync(InboundMessageEvent message)
        {
            _messages[$"{message.TenantId}:{message.MessageId}"] = message;
            return Task.CompletedTask;
        }

        public Task<InboundMessageEvent?> GetInboundMessageAsync(string tenantId, string messageId)
        {
            _messages.TryGetValue($"{tenantId}:{messageId}", out var msg);
            return Task.FromResult(msg);
        }

        public Task<IReadOnlyList<InboundMessageEvent>> ListInboundMessagesAsync(string tenantId)
        {
            var list = _messages.Values.Where(m => m.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<InboundMessageEvent>>(list);
        }
    }

    public class InMemoryMeetingIntelligenceStore : IMeetingIntelligenceStore
    {
        private readonly ConcurrentDictionary<string, MeetingBrief> _briefs = new();
        private readonly ConcurrentDictionary<string, MeetingTranscriptAnalysis> _transcripts = new();

        public Task SaveMeetingBriefAsync(MeetingBrief brief)
        {
            _briefs[$"{brief.TenantId}:{brief.BriefId}"] = brief;
            return Task.CompletedTask;
        }

        public Task<MeetingBrief?> GetMeetingBriefAsync(string tenantId, string briefId)
        {
            _briefs.TryGetValue($"{tenantId}:{briefId}", out var brief);
            return Task.FromResult(brief);
        }

        public Task SaveTranscriptAnalysisAsync(MeetingTranscriptAnalysis analysis)
        {
            _transcripts[$"{analysis.TenantId}:{analysis.MeetingId}"] = analysis;
            return Task.CompletedTask;
        }

        public Task<MeetingTranscriptAnalysis?> GetTranscriptAnalysisAsync(string tenantId, string meetingId)
        {
            _transcripts.TryGetValue($"{tenantId}:{meetingId}", out var analysis);
            return Task.FromResult(analysis);
        }
    }

    public class InboundResponseService : IInboundResponseService
    {
        private readonly IInboundResponseStore _store;

        private static readonly string[] PromptInjectionPatterns = new[]
        {
            "ignore previous instructions",
            "ignore all previous instructions",
            "system prompt",
            "you are now an unrestricted",
            "execute arbitrary",
            "bypass firewall",
            "reveal all secrets",
            "admin override",
            "disregard all governance"
        };

        public InboundResponseService(IInboundResponseStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<InboundMessageEvent> ProcessInboundMessageAsync(string tenantId, string opportunityId, string sender, string subject, string rawBody)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.");

            var ev = new InboundMessageEvent
            {
                TenantId = tenantId,
                OpportunityId = opportunityId,
                SenderAddress = sender,
                Subject = subject,
                RawBody = rawBody,
                ReceivedAtUtc = DateTime.UtcNow
            };

            // Prompt-Injection Defense: Sub-Batch 4.4.5
            var lowerBody = rawBody.ToLowerInvariant();
            foreach (var pattern in PromptInjectionPatterns)
            {
                if (lowerBody.Contains(pattern))
                {
                    ev.IsPromptInjectionDetected = true;
                    ev.InjectionThreatIndicators.Add($"Detected adversary injection pattern: '{pattern}'");
                }
            }

            if (ev.IsPromptInjectionDetected)
            {
                ev.Classification = InboundMessageClassification.PROMPT_INJECTION;
                ev.ClassificationConfidence = 0.99m;
                ev.SanitizedBody = "[REDACTED_PROMPT_INJECTION_THREAT]";
            }
            else
            {
                // Clean HTML/script tags to sanitize external data
                ev.SanitizedBody = Regex.Replace(rawBody, "<.*?>", string.Empty).Trim();

                if (lowerBody.Contains("meeting") || lowerBody.Contains("demo") || lowerBody.Contains("call next week") || lowerBody.Contains("schedule"))
                {
                    ev.Classification = InboundMessageClassification.MEETING_REQUEST;
                    ev.ClassificationConfidence = 0.9m;
                }
                else if (lowerBody.Contains("pricing") || lowerBody.Contains("how much") || lowerBody.Contains("cost") || lowerBody.Contains("rate"))
                {
                    ev.Classification = InboundMessageClassification.PRICING_QUESTION;
                    ev.ClassificationConfidence = 0.85m;
                }
                else if (lowerBody.Contains("contract") || lowerBody.Contains("agreement") || lowerBody.Contains("msa"))
                {
                    ev.Classification = InboundMessageClassification.CONTRACT;
                    ev.ClassificationConfidence = 0.85m;
                }
                else if (lowerBody.Contains("not interested") || lowerBody.Contains("unsubscribe") || lowerBody.Contains("remove us"))
                {
                    ev.Classification = InboundMessageClassification.NOT_INTERESTED;
                    ev.ClassificationConfidence = 0.95m;
                }
                else
                {
                    ev.Classification = InboundMessageClassification.INTERESTED;
                    ev.ClassificationConfidence = 0.75m;
                }

                if (lowerBody.Contains("need") || lowerBody.Contains("require") || lowerBody.Contains("looking for"))
                {
                    ev.ExtractedRequirements.Add("Customer expressed explicit requirement in message body.");
                }
            }

            await _store.SaveInboundMessageAsync(ev);
            return ev;
        }
    }

    public class MeetingIntelligenceService : IMeetingIntelligenceService
    {
        private readonly IMeetingIntelligenceStore _store;

        public MeetingIntelligenceService(IMeetingIntelligenceStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<MeetingBrief> PrepareMeetingBriefAsync(string tenantId, string opportunityId, string companyName, List<string> attendees)
        {
            var brief = new MeetingBrief
            {
                TenantId = tenantId,
                OpportunityId = opportunityId,
                CompanyName = companyName,
                BuyingCenterAttendees = attendees ?? new List<string>(),
                AccountSummary = $"Executive briefing prepared for {companyName}.",
                KeyPainPoints = { "Operational throughput limitations", "High compliance reporting latency" },
                AnticipatedObjections = { "Integration timeline", "Budget cycles" },
                ProposedCommercialStrategy = "Demonstrate Phase 4.3 autonomous execution with human-in-the-loop governance",
                MeetingScheduledAtUtc = DateTime.UtcNow.AddDays(1)
            };

            await _store.SaveMeetingBriefAsync(brief);
            return brief;
        }

        public async Task<MeetingTranscriptAnalysis> IngestAndAnalyzeTranscriptAsync(string tenantId, string meetingId, string opportunityId, string rawTranscript)
        {
            var lower = rawTranscript.ToLowerInvariant();

            var analysis = new MeetingTranscriptAnalysis
            {
                TenantId = tenantId,
                MeetingId = meetingId,
                OpportunityId = opportunityId,
                RawTranscript = rawTranscript,
                HasCommercialOptimism = lower.Contains("sounds great") || lower.Contains("love this") || lower.Contains("very excited"),
                AnalyzedAtUtc = DateTime.UtcNow
            };

            if (lower.Contains("we need") || lower.Contains("must have"))
                analysis.DiscoveredRequirements.Add("Integration with existing workflow stack");

            if (lower.Contains("we commit to") || lower.Contains("we agree to"))
                analysis.ExplicitCustomerCommitments.Add("Customer verbally agreed to review proposal next week");

            analysis.ActionItems.Add("Send formal commercial proposal with verified pricing terms");

            await _store.SaveTranscriptAnalysisAsync(analysis);
            return analysis;
        }
    }
}
