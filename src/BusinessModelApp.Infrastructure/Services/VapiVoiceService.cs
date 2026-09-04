using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
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
using BusinessModelApp.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BusinessModelApp.Infrastructure.Services
{
    public class VapiVoiceService : IVoiceTelephonyService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;
        private readonly ICommercialRepository _commercialRepo;
        private readonly IAIDataMinimizationService _minimizationService;
        private readonly IAIInferenceGateway _aiGateway;
        private readonly VoiceOptions _options;
        private readonly ILogger<VapiVoiceService> _logger;

        public VoiceProviderType ProviderType => VoiceProviderType.Vapi;

        public VapiVoiceService(
            HttpClient httpClient,
            AppDbContext context,
            ICommercialRepository commercialRepo,
            IAIDataMinimizationService minimizationService,
            IAIInferenceGateway aiGateway,
            IOptions<VoiceOptions> options,
            ILogger<VapiVoiceService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _commercialRepo = commercialRepo ?? throw new ArgumentNullException(nameof(commercialRepo));
            _minimizationService = minimizationService ?? throw new ArgumentNullException(nameof(minimizationService));
            _aiGateway = aiGateway ?? throw new ArgumentNullException(nameof(aiGateway));
            _options = options?.Value ?? new VoiceOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VoiceCallDispatchResult> InitiateOutboundCallAsync(OutboundCallRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_options.Vapi.ApiKey))
            {
                return new VoiceCallDispatchResult
                {
                    Success = false,
                    Provider = ProviderType,
                    ErrorMessage = "Vapi API key is not configured in application settings."
                };
            }

            var phoneHash = ComputeSha256(request.PhoneNumber);

            try
            {
                var payload = new
                {
                    phoneNumberId = _options.Vapi.PhoneNumberId,
                    assistantId = _options.Vapi.AssistantId,
                    customer = new
                    {
                        number = request.PhoneNumber,
                        name = request.ContactName
                    },
                    assistantOverrides = new
                    {
                        variableValues = new Dictionary<string, string>
                        {
                            { "contactName", request.ContactName },
                            { "companyName", request.CompanyName },
                            { "goalPrompt", request.GoalPrompt }
                        }
                    }
                };

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.Vapi.BaseUrl.TrimEnd('/')}/call/phone");
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.Vapi.ApiKey);
                httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(httpRequest, ct);
                var content = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Vapi outbound call failed: HTTP {StatusCode} - {Content}", response.StatusCode, content);
                    return new VoiceCallDispatchResult
                    {
                        Success = false,
                        Provider = ProviderType,
                        ErrorMessage = $"Vapi rejected call request: {response.StatusCode} ({content})"
                    };
                }

                using var responseDoc = JsonDocument.Parse(content);
                var root = responseDoc.RootElement;
                string providerCallId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";

                if (string.IsNullOrWhiteSpace(providerCallId))
                {
                    return new VoiceCallDispatchResult
                    {
                        Success = false,
                        Provider = ProviderType,
                        ErrorMessage = "Vapi returned successful HTTP response without a valid Call ID."
                    };
                }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching Vapi outbound call to {PhoneHash}", phoneHash);
                return new VoiceCallDispatchResult
                {
                    Success = false,
                    Provider = ProviderType,
                    ErrorMessage = $"Voice dispatch error: {ex.Message}"
                };
            }
        }

        public async Task<VoiceCallStateResult> GetCallStatusAsync(string providerCallId, CancellationToken ct = default)
        {
            var record = await _context.VoiceCallRecords.FirstOrDefaultAsync(r => r.ProviderCallId == providerCallId, ct);
            if (record == null)
            {
                return new VoiceCallStateResult { ProviderCallId = providerCallId, Status = VoiceCallStatus.Failed, FailureReason = "Call record not found." };
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
            string eventId = headers.TryGetValue("x-vapi-event-id", out var eid) ? eid : $"VAPI-EVT-{Guid.NewGuid():N}";

            // 1. Idempotency Guard
            var existingEvent = await _context.VoiceWebhookEvents.FirstOrDefaultAsync(e => e.Provider == ProviderType && e.EventId == eventId, ct);
            if (existingEvent != null)
            {
                return new VoiceWebhookProcessingResult
                {
                    Success = true,
                    Status = VoiceWebhookProcessingStatus.DuplicateIgnored,
                    Message = "Duplicate Vapi webhook event ignored."
                };
            }

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(rawPayloadJson) ? "{}" : rawPayloadJson);
            var root = doc.RootElement;

            string messageType = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() ?? "" : "";
            var callElement = root.TryGetProperty("call", out var callProp) ? callProp : root;
            string providerCallId = callElement.TryGetProperty("id", out var cidProp) ? cidProp.GetString() ?? "" : "";

            if (string.IsNullOrWhiteSpace(providerCallId))
            {
                return new VoiceWebhookProcessingResult
                {
                    Success = false,
                    Status = VoiceWebhookProcessingStatus.Failed,
                    Message = "Missing Vapi call ID in payload."
                };
            }

            var webhookEvent = new VoiceWebhookEvent
            {
                Provider = ProviderType,
                EventId = eventId,
                ProviderCallId = providerCallId,
                EventType = messageType,
                PayloadHash = payloadHash,
                ReceivedAt = DateTime.UtcNow,
                ProcessingStatus = VoiceWebhookProcessingStatus.Received
            };
            _context.VoiceWebhookEvents.Add(webhookEvent);
            await _context.SaveChangesAsync(ct);

            var record = await _context.VoiceCallRecords.FirstOrDefaultAsync(r => r.ProviderCallId == providerCallId, ct);
            if (record == null)
            {
                webhookEvent.ProcessingStatus = VoiceWebhookProcessingStatus.Failed;
                await _context.SaveChangesAsync(ct);
                return new VoiceWebhookProcessingResult { Success = false, Status = VoiceWebhookProcessingStatus.Failed, Message = "Matching VoiceCallRecord not found." };
            }

            // Extract Transcript, Cost, and Duration
            string rawTranscript = callElement.TryGetProperty("transcript", out var trProp) ? trProp.GetString() ?? "" : "";
            int durationSeconds = callElement.TryGetProperty("duration", out var durProp) ? durProp.GetInt32() : 0;
            decimal costUSD = callElement.TryGetProperty("cost", out var costProp) ? costProp.GetDecimal() : 0.03m;
            decimal actualCostINR = costUSD * 87.0m; // USD to INR conversion rate

            string sanitizedTranscript = _minimizationService.SanitizeContent(rawTranscript, AITaskType.Transcription);

            // Intent Extraction via AI Gateway
            double qualityScore = 85.0;
            string extractedIntent = "Medium Intent";
            if (!string.IsNullOrWhiteSpace(sanitizedTranscript))
            {
                var aiRequest = new AIRequest
                {
                    TaskType = AITaskType.LeadQualification,
                    WorkspaceId = record.WorkspaceId,
                    Messages = { AIMessage.User($"Analyze call transcript and extract buyer intent (High/Medium/Low) and integer qualification score (0-100):\n{sanitizedTranscript}") }
                };
                var aiResult = await _aiGateway.ExecuteAsync(aiRequest, ct);
                extractedIntent = aiResult.Content.Contains("High", StringComparison.OrdinalIgnoreCase) ? "High Intent" : "Medium Intent";
                qualityScore = aiResult.Content.Contains("High", StringComparison.OrdinalIgnoreCase) ? 92.0 : 75.0;
            }

            record.Status = VoiceCallStatus.TranscriptPersisted;
            record.DurationSeconds = durationSeconds;
            record.Transcript = sanitizedTranscript;
            record.QualityScore = qualityScore;
            record.ExtractedIntent = extractedIntent;
            record.ActualCostINR = actualCostINR;
            record.WebhookVerified = true;
            record.WebhookReceivedAt = DateTime.UtcNow;
            record.CompletedAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;

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
                        Title = "Vapi AI Voice Call Completed",
                        Description = $"Charlie conducted an autonomous voice qualification with {record.ContactName} (Duration: {durationSeconds}s, Intent: {extractedIntent}, Score: {qualityScore:F1}/100).",
                        PerformedByName = "Vapi Voice Connector"
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
                IntentSummary = extractedIntent,
                Message = "Vapi webhook processed and persisted."
            };
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
