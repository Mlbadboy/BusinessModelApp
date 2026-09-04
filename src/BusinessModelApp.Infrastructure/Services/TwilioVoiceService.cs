using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
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
    public class TwilioVoiceService : IVoiceTelephonyService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;
        private readonly ICommercialRepository _commercialRepo;
        private readonly IAIDataMinimizationService _minimizationService;
        private readonly VoiceOptions _options;
        private readonly ILogger<TwilioVoiceService> _logger;

        public VoiceProviderType ProviderType => VoiceProviderType.Twilio;

        public TwilioVoiceService(
            HttpClient httpClient,
            AppDbContext context,
            ICommercialRepository commercialRepo,
            IAIDataMinimizationService minimizationService,
            IOptions<VoiceOptions> options,
            ILogger<TwilioVoiceService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _commercialRepo = commercialRepo ?? throw new ArgumentNullException(nameof(commercialRepo));
            _minimizationService = minimizationService ?? throw new ArgumentNullException(nameof(minimizationService));
            _options = options?.Value ?? new VoiceOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VoiceCallDispatchResult> InitiateOutboundCallAsync(OutboundCallRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_options.Twilio.AccountSid) || string.IsNullOrWhiteSpace(_options.Twilio.AuthToken))
            {
                return new VoiceCallDispatchResult { Success = false, Provider = ProviderType, ErrorMessage = "Twilio AccountSid or AuthToken not configured." };
            }

            var phoneHash = ComputeSha256(request.PhoneNumber);

            try
            {
                var formValues = new Dictionary<string, string>
                {
                    { "To", request.PhoneNumber },
                    { "From", _options.Twilio.FromNumber },
                    { "Twiml", $"<Response><Say voice=\"alice\">Hello {request.ContactName}, this is Charlie calling from Bitbloom Services. We are reaching out regarding your business operations.</Say><Pause length=\"2\"/><Say>Thank you for your time.</Say></Response>" }
                };

                var authString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.Twilio.AccountSid}:{_options.Twilio.AuthToken}"));
                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"https://api.twilio.com/2010-04-01/Accounts/{_options.Twilio.AccountSid}/Calls.json");
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);
                httpRequest.Content = new FormUrlEncodedContent(formValues);

                var response = await _httpClient.SendAsync(httpRequest, ct);
                var content = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    return new VoiceCallDispatchResult { Success = false, Provider = ProviderType, ErrorMessage = $"Twilio rejected call: {response.StatusCode}" };
                }

                using var doc = System.Text.Json.JsonDocument.Parse(content);
                string providerCallId = doc.RootElement.TryGetProperty("sid", out var sidProp) ? sidProp.GetString() ?? "" : "";

                if (string.IsNullOrWhiteSpace(providerCallId))
                {
                    return new VoiceCallDispatchResult { Success = false, Provider = ProviderType, ErrorMessage = "Twilio response did not contain a valid SID." };
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
                _logger.LogError(ex, "Error dispatching Twilio call to {PhoneHash}", phoneHash);
                return new VoiceCallDispatchResult { Success = false, Provider = ProviderType, ErrorMessage = ex.Message };
            }
        }

        public async Task<VoiceCallStateResult> GetCallStatusAsync(string providerCallId, CancellationToken ct = default)
        {
            var record = await _context.VoiceCallRecords.FirstOrDefaultAsync(r => r.ProviderCallId == providerCallId, ct);
            if (record == null) return new VoiceCallStateResult { ProviderCallId = providerCallId, Status = VoiceCallStatus.Failed, FailureReason = "Not found" };

            return new VoiceCallStateResult
            {
                ProviderCallId = record.ProviderCallId,
                Status = record.Status,
                DurationSeconds = record.DurationSeconds,
                Transcript = record.Transcript,
                QualityScore = record.QualityScore,
                ExtractedIntent = record.ExtractedIntent,
                ActualCostINR = record.ActualCostINR,
                FailureReason = record.FailureReason
            };
        }

        public async Task<VoiceWebhookProcessingResult> ProcessWebhookAsync(string rawPayloadJson, IDictionary<string, string> headers, CancellationToken ct = default)
        {
            var payloadHash = ComputeSha256(rawPayloadJson);
            string eventId = headers.TryGetValue("x-twilio-signature", out var sig) ? sig : $"TWILIO-EVT-{Guid.NewGuid():N}";

            var existing = await _context.VoiceWebhookEvents.FirstOrDefaultAsync(e => e.Provider == ProviderType && e.EventId == eventId, ct);
            if (existing != null) return new VoiceWebhookProcessingResult { Success = true, Status = VoiceWebhookProcessingStatus.DuplicateIgnored };

            string providerCallId = headers.TryGetValue("CallSid", out var sid) ? sid : "";

            var webhookEvent = new VoiceWebhookEvent
            {
                Provider = ProviderType,
                EventId = eventId,
                ProviderCallId = providerCallId,
                EventType = "call-status-changed",
                PayloadHash = payloadHash,
                ReceivedAt = DateTime.UtcNow,
                ProcessingStatus = VoiceWebhookProcessingStatus.Received
            };
            _context.VoiceWebhookEvents.Add(webhookEvent);
            await _context.SaveChangesAsync(ct);

            var record = await _context.VoiceCallRecords.FirstOrDefaultAsync(r => r.ProviderCallId == providerCallId, ct);
            if (record != null)
            {
                record.Status = VoiceCallStatus.TranscriptPersisted;
                record.WebhookVerified = true;
                record.WebhookReceivedAt = DateTime.UtcNow;
                record.CompletedAt = DateTime.UtcNow;
                record.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
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
                Message = "Twilio webhook processed."
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
