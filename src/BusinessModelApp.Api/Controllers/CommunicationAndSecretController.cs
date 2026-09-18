using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers
{
    [ApiController]
    [Route("api/governed-communication")]
    public class CommunicationAndSecretController : ControllerBase
    {
        private readonly IUnifiedCommunicationFabric _communicationFabric;
        private readonly IWorkforceSecretBroker _secretBroker;

        public CommunicationAndSecretController(IUnifiedCommunicationFabric communicationFabric, IWorkforceSecretBroker secretBroker)
        {
            _communicationFabric = communicationFabric ?? throw new ArgumentNullException(nameof(communicationFabric));
            _secretBroker = secretBroker ?? throw new ArgumentNullException(nameof(secretBroker));
        }

        private string GetTenantId() =>
            Request.Headers.TryGetValue("X-Tenant-ID", out var t) && !string.IsNullOrWhiteSpace(t) ? t.ToString() : "tenant-default";

        [HttpPost("intents")]
        public async Task<IActionResult> SubmitIntent([FromBody] CommunicationIntent intent)
        {
            var created = await _communicationFabric.SubmitCommunicationIntentAsync(GetTenantId(), intent);
            return Ok(created);
        }

        [HttpPost("intents/{intentId}/approve")]
        public async Task<IActionResult> ApproveIntent(string intentId, [FromBody] ApproveIntentRequest request)
        {
            var success = await _communicationFabric.ApproveCommunicationIntentAsync(GetTenantId(), intentId, request.ApprovedBy ?? "HumanSupervisor");
            if (!success) return NotFound();
            return Ok(new { success });
        }

        [HttpPost("intents/{intentId}/dispatch")]
        public async Task<IActionResult> DispatchIntent(string intentId)
        {
            try
            {
                var success = await _communicationFabric.DispatchCommunicationAsync(GetTenantId(), intentId);
                return Ok(new { success });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("intents/pending")]
        public async Task<IActionResult> ListPendingApprovals()
        {
            var list = await _communicationFabric.ListPendingApprovalsAsync(GetTenantId());
            return Ok(list);
        }

        [HttpPost("credentials/issue")]
        public async Task<IActionResult> IssueCredential([FromBody] IssueCredentialRequest request)
        {
            var cred = await _secretBroker.IssueScopedCredentialAsync(GetTenantId(), request.CapabilityId, request.DurationMinutes);
            return Ok(cred);
        }

        [HttpPost("credentials/{credentialId}/revoke")]
        public async Task<IActionResult> RevokeCredential(string credentialId)
        {
            var success = await _secretBroker.RevokeCredentialAsync(GetTenantId(), credentialId);
            if (!success) return NotFound();
            return Ok(new { success });
        }
    }

    public class ApproveIntentRequest
    {
        public string? ApprovedBy { get; set; }
    }

    public class IssueCredentialRequest
    {
        public string CapabilityId { get; set; } = string.Empty;
        public int DurationMinutes { get; set; } = 60;
    }
}
