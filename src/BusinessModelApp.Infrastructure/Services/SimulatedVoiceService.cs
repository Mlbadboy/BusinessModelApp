using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.AI.Governance;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Infrastructure.Services
{
    public class SimulatedVoiceService : IVoiceTelephonyService
    {
        private readonly AppDbContext _context;
        private readonly ICommercialRepository _commercialRepo;
        private readonly IAIDataMinimizationService _minimizationService;
        private readonly ILogger<SimulatedVoiceService> _logger;

        public VoiceProviderType ProviderType => VoiceProviderType.Simulation;

        public SimulatedVoiceService(
            AppDbContext context,
            ICommercialRepository commercialRepo,
            IAIDataMinimizationService minimizationService,
            ILogger<SimulatedVoiceService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _commercialRepo = commercialRepo ?? throw new ArgumentNullException(nameof(commercialRepo));
            _minimizationService = minimizationService ?? throw new ArgumentNullException(nameof(minimizationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VoiceCallDispatchResult> InitiateOutboundCallAsync(OutboundCallRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return new VoiceCallDispatchResult
                {
                    Success = false,
                    ErrorMessage = "Destination phone number is required.",
                    Provider = ProviderType
                };
            }

            var phoneHash = ComputeSha256(request.PhoneNumber);
            var providerCallId = $"SIM-CALL-{Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper()}";

            var record = new VoiceCallRecord
            {
                WorkspaceId = request.WorkspaceId,
                OrganizationId = request.OrganizationId,
                LeadId = request.LeadId,
                Provider = ProviderType,
                ProviderCallId = providerCallId,
                PhoneNumberHash = phoneHash,
                ContactName = request.ContactName,
                CompanyName = request.CompanyName,
                Status = VoiceCallStatus.DispatchAccepted,
                StartedAt = DateTime.UtcNow,
                EstimatedCostINR = Math.Min(request.MaxBudgetINR, 10.0m),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.VoiceCallRecords.Add(record);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Simulated outbound voice call accepted with ProviderCallId {ProviderCallId} for {ContactName}", providerCallId, request.ContactName);

            return new VoiceCallDispatchResult
            {
                Success = true,
                ProviderCallId = providerCallId,
                CallRecordId = record.Id,
                Status = VoiceCallStatus.DispatchAccepted,
                Provider = ProviderType,
                EstimatedCostINR = record.EstimatedCostINR
            };
        }

        public async Task<VoiceCallStateResult> GetCallStatusAsync(string providerCallId, CancellationToken ct = default)
        {
            var record = await _context.VoiceCallRecords.FirstOrDefaultAsync(r => r.ProviderCallId == providerCallId, ct);
            if (record == null)
            {
                return new VoiceCallStateResult
                {
                    ProviderCallId = providerCallId,
                    Status = VoiceCallStatus.Failed,
                    FailureReason = "Call record not found."
                };
            }

            return new VoiceCallStateResult
            {
                ProviderCallId = record.ProviderCallId,
                Status = record.Status,
                DurationSeconds = record.DurationSeconds,
                Transcript = record.Transcript,
                QualityScore = record.QualityScore,
                ExtractedIntent = record.ExtractedIntent,
                ActualCostINR = record.ActualCostINR,
                RecordingUrl = record.RecordingUrl,
                FailureReason = record.FailureReason
            };
        }

        public async Task<VoiceWebhookProcessingResult> ProcessWebhookAsync(string rawPayloadJson, IDictionary<string, string> headers, CancellationToken ct = default)
        {
            var payloadHash = ComputeSha256(rawPayloadJson);
            string eventId = headers.TryGetValue("x-event-id", out var eid) ? eid : $"SIM-EVT-{Guid.NewGuid().ToString("N").Substring(0, 10)}";

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(rawPayloadJson) ? "{}" : rawPayloadJson);
            var root = doc.RootElement;

            string providerCallId = root.TryGetProperty("providerCallId", out var pcProp) ? pcProp.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(providerCallId))
            {
                return new VoiceWebhookProcessingResult
                {
                    Success = false,
                    Status = VoiceWebhookProcessingStatus.Failed,
                    Message = "Missing providerCallId in webhook payload."
                };
            }

            // 1. Idempotency Check
            var existingEvent = await _context.VoiceWebhookEvents.FirstOrDefaultAsync(e => e.Provider == ProviderType && e.EventId == eventId, ct);
            if (existingEvent != null)
            {
                _logger.LogInformation("Voice webhook event {EventId} already processed. Returning duplicate acknowledgment.", eventId);
                return new VoiceWebhookProcessingResult
                {
                    Success = true,
                    Status = VoiceWebhookProcessingStatus.DuplicateIgnored,
                    ProviderCallId = providerCallId,
                    Message = "Event previously processed."
                };
            }

            var webhookEvent = new VoiceWebhookEvent
            {
                Provider = ProviderType,
                EventId = eventId,
                ProviderCallId = providerCallId,
                EventType = root.TryGetProperty("eventType", out var etProp) ? etProp.GetString() ?? "call.completed" : "call.completed",
                PayloadHash = payloadHash,
                ReceivedAt = DateTime.UtcNow,
                ProcessingStatus = VoiceWebhookProcessingStatus.Received
            };
            _context.VoiceWebhookEvents.Add(webhookEvent);
            await _context.SaveChangesAsync(ct);

            // 2. Load Call Record
            var record = await _context.VoiceCallRecords.FirstOrDefaultAsync(r => r.ProviderCallId == providerCallId, ct);
            if (record == null)
            {
                webhookEvent.ProcessingStatus = VoiceWebhookProcessingStatus.Failed;
                await _context.SaveChangesAsync(ct);
                return new VoiceWebhookProcessingResult
                {
                    Success = false,
                    Status = VoiceWebhookProcessingStatus.Failed,
                    ProviderCallId = providerCallId,
                    Message = "No matching VoiceCallRecord found."
                };
            }

            // 3. Process Transcript and PII Sanitization
            string rawTranscript = root.TryGetProperty("transcript", out var trProp) 
                ? trProp.GetString() ?? GetDefaultSimulatedTranscript(record.ContactName, record.CompanyName) 
                : GetDefaultSimulatedTranscript(record.ContactName, record.CompanyName);

            string sanitizedTranscript = _minimizationService.SanitizeContent(rawTranscript, AITaskType.Transcription);
            int durationSeconds = root.TryGetProperty("durationSeconds", out var durProp) ? durProp.GetInt32() : 167; // ~2m 47s
            decimal actualCostINR = root.TryGetProperty("actualCostINR", out var costProp) ? costProp.GetDecimal() : 2.40m;
            double qualityScore = root.TryGetProperty("qualityScore", out var qsProp) ? qsProp.GetDouble() : 91.5;
            string intent = root.TryGetProperty("intent", out var inProp) ? inProp.GetString() ?? "High Intent" : "High Intent";

            record.Status = VoiceCallStatus.TranscriptPersisted;
            record.DurationSeconds = durationSeconds;
            record.Transcript = sanitizedTranscript;
            record.QualityScore = qualityScore;
            record.ExtractedIntent = intent;
            record.ActualCostINR = actualCostINR;
            record.Sentiment = "Positive";
            record.WebhookVerified = true;
            record.WebhookReceivedAt = DateTime.UtcNow;
            record.CompletedAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;

            // 4. Update Lead & Append Business Activity if lead exists
            if (record.LeadId.HasValue)
            {
                var lead = await _commercialRepo.GetLeadByIdAsync(record.WorkspaceId, record.LeadId.Value, ct);
                if (lead != null)
                {
                    lead.QualityScore = qualityScore;
                    lead.Status = LeadStatus.Qualified;
                    await _commercialRepo.UpdateLeadAsync(lead, ct);

                    await _commercialRepo.AddActivityAsync(new Activity
                    {
                        OpportunityId = lead.Opportunity?.Id ?? Guid.Empty,
                        Type = ActivityType.InteractionLogged,
                        Title = "Governed Voice AI Call Completed",
                        Description = $"Charlie conducted a 2m 47s qualification conversation with {record.ContactName}. Intent: {intent} (Score: {qualityScore:F1}/100).",
                        PerformedByName = "Charlie Voice Agent"
                    }, ct);
                }
            }

            webhookEvent.ProcessingStatus = VoiceWebhookProcessingStatus.Processed;
            webhookEvent.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            return new VoiceWebhookProcessingResult
            {
                Success = true,
                Status = VoiceWebhookProcessingStatus.Processed,
                ProviderCallId = providerCallId,
                ResultingCallStatus = VoiceCallStatus.TranscriptPersisted,
                ActualCostINR = actualCostINR,
                SanitizedTranscript = sanitizedTranscript,
                QualityScore = qualityScore,
                IntentSummary = intent,
                Message = "Simulated voice webhook processed and persisted."
            };
        }

        private static string GetDefaultSimulatedTranscript(string contactName, string companyName)
        {
            return $"Charlie: Hello {contactName}, this is Charlie, an autonomous AI Operations Specialist from Bitbloom. I'm calling regarding {companyName}'s multi-location operations.\n" +
                   $"{contactName}: Hi Charlie. Yes, we manage over 500 retail branches and our POS reconciliation takes days.\n" +
                   $"Charlie: That is a significant operational drag. Our automated orchestration reconciles POS feeds in real-time with zero human data-entry error. Would you be open to an executive briefing next Tuesday?\n" +
                   $"{contactName}: Absolutely. Please send the architecture overview to our digital transformation team.\n" +
                   $"Charlie: Done. Thank you {contactName}, have a productive day!";
        }

        private static string ComputeSha256(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
