using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Api.Controllers
{
    public class DispatchTestCallDto
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string GoalPrompt { get; set; } = string.Empty;
        public bool IsTestCall { get; set; } = true;
        public decimal MaxBudgetINR { get; set; } = 10.0m;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class VoiceController : ControllerBase
    {
        private readonly IVoiceTelephonyService _voiceService;
        private readonly IUserContextService _userContext;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<VoiceController> _logger;

        private static readonly Regex PhoneSanitizerRegex = new Regex(@"[^\d+]", RegexOptions.Compiled);

        public VoiceController(
            IVoiceTelephonyService voiceService,
            IUserContextService userContext,
            IWebHostEnvironment environment,
            ILogger<VoiceController> logger)
        {
            _voiceService = voiceService ?? throw new ArgumentNullException(nameof(voiceService));
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Strictly governed test-call endpoint. Permitted only in Development/Testing/Pilot for CEO/Admin roles with <= ₹10 budget.
        /// </summary>
        [HttpPost("dispatch-test-call")]
        [Authorize(Roles = "CEO,Admin")]
        public async Task<ActionResult<VoiceCallDispatchResult>> DispatchTestCall([FromBody] DispatchTestCallDto request)
        {
            // 1. Strict Environment Guard (Never in Production)
            if (_environment.IsProduction())
            {
                return StatusCode(403, new { message = "Physical test-call dispatch is strictly forbidden in Production environment." });
            }

            // 2. Explicit Test Flag Verification
            if (!request.IsTestCall)
            {
                return BadRequest(new { message = "IsTestCall must be explicitly set to true." });
            }

            // 3. Strict ₹10 Financial Ceiling Guard
            if (request.MaxBudgetINR <= 0 || request.MaxBudgetINR > 10.0m)
            {
                return BadRequest(new { message = "Test call budget reservation must be between ₹1.00 and ₹10.00." });
            }

            // 4. Phone Number Validation
            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                return BadRequest(new { message = "A valid destination phone number is required." });
            }

            string cleanPhone = PhoneSanitizerRegex.Replace(request.PhoneNumber, "");
            if (cleanPhone.Length < 10)
            {
                return BadRequest(new { message = "Phone number must contain at least 10 digits." });
            }

            var workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null);

            var outboundRequest = new OutboundCallRequest
            {
                WorkspaceId = workspaceId,
                PhoneNumber = cleanPhone,
                ContactName = string.IsNullOrWhiteSpace(request.ContactName) ? "Test Contact" : request.ContactName,
                CompanyName = string.IsNullOrWhiteSpace(request.CompanyName) ? "Test Enterprise" : request.CompanyName,
                GoalPrompt = request.GoalPrompt,
                IsTestCall = true,
                MaxBudgetINR = request.MaxBudgetINR
            };

            _logger.LogInformation("CEO/Admin triggered governed test voice call for workspace {WorkspaceId}", workspaceId);

            var result = await _voiceService.InitiateOutboundCallAsync(outboundRequest);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage ?? "Voice dispatch rejected by provider." });
            }

            return Ok(result);
        }

        /// <summary>
        /// Authenticated call dispatch against an existing workspace lead.
        /// </summary>
        [HttpPost("call-lead/{leadId}")]
        [Authorize]
        public async Task<ActionResult<VoiceCallDispatchResult>> CallLead(
            Guid leadId,
            [FromServices] ICommercialRepository commercialRepo,
            [FromQuery] decimal maxBudgetINR = 10.0m)
        {
            var workspaceId = await _userContext.GetAuthorizedWorkspaceIdAsync(null);
            var lead = await commercialRepo.GetLeadByIdAsync(workspaceId, leadId);
            if (lead == null)
            {
                return NotFound(new { message = "Lead not found in authorized workspace." });
            }

            if (string.IsNullOrWhiteSpace(lead.Phone))
            {
                return BadRequest(new { message = "Lead does not have a phone number registered." });
            }

            var outboundRequest = new OutboundCallRequest
            {
                LeadId = lead.Id,
                WorkspaceId = workspaceId,
                PhoneNumber = lead.Phone,
                ContactName = lead.ContactName,
                CompanyName = lead.CompanyName,
                GoalPrompt = $"Qualify inbound interest in {lead.Notes}",
                IsTestCall = false,
                MaxBudgetINR = Math.Min(maxBudgetINR, 10.0m)
            };

            var result = await _voiceService.InitiateOutboundCallAsync(outboundRequest);
            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(result);
        }

        /// <summary>
        /// Polling / Inspection endpoint to get current call status and transcript.
        /// </summary>
        [HttpGet("calls/{providerCallId}")]
        [Authorize]
        public async Task<ActionResult<VoiceCallStateResult>> GetCallStatus(string providerCallId)
        {
            if (string.IsNullOrWhiteSpace(providerCallId)) return BadRequest();

            var status = await _voiceService.GetCallStatusAsync(providerCallId);
            return Ok(status);
        }

        /// <summary>
        /// Public / provider webhook receiver for call lifecycle events.
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<ActionResult<VoiceWebhookProcessingResult>> ReceiveWebhook()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var rawJson = await reader.ReadToEndAsync();

            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);

            var result = await _voiceService.ProcessWebhookAsync(rawJson, headers);

            if (!result.Success && result.Status == VoiceWebhookProcessingStatus.SignatureFailed)
            {
                return Unauthorized(new { message = "Webhook signature verification failed." });
            }

            return Ok(result);
        }
    }
}
