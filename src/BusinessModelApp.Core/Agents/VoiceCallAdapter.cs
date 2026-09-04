using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Interfaces;

namespace BusinessModelApp.Core.Agents
{
    public class VoiceCallAdapter : IToolExecutionAdapter
    {
        private readonly IVoiceTelephonyService _voiceTelephonyService;

        public AgentActionType ActionType => AgentActionType.DispatchVoiceCall;

        public VoiceCallAdapter(IVoiceTelephonyService voiceTelephonyService)
        {
            _voiceTelephonyService = voiceTelephonyService ?? throw new ArgumentNullException(nameof(voiceTelephonyService));
        }

        public async Task<ToolExecutionResult> ExecuteAsync(
            AgentIdentity agent,
            AgentMission mission,
            Dictionary<string, object> parameters,
            CancellationToken ct = default)
        {
            string phoneNumber = parameters.TryGetValue("phoneNumber", out var p) ? p?.ToString() ?? string.Empty : string.Empty;
            string contactName = parameters.TryGetValue("contactName", out var cn) ? cn?.ToString() ?? "Prospect" : "Prospect";
            string companyName = parameters.TryGetValue("companyName", out var co) ? co?.ToString() ?? "Target Company" : "Target Company";
            string goalPrompt = parameters.TryGetValue("goalPrompt", out var gp) ? gp?.ToString() ?? string.Empty : string.Empty;
            bool isTestCall = parameters.TryGetValue("isTestCall", out var tc) && tc is bool b && b;
            Guid? leadId = parameters.TryGetValue("leadId", out var lid) && lid is Guid g ? g : (Guid.TryParse(lid?.ToString(), out var parsedG) ? parsedG : null);

            var request = new OutboundCallRequest
            {
                LeadId = leadId,
                WorkspaceId = mission.WorkspaceId,
                PhoneNumber = phoneNumber,
                ContactName = contactName,
                CompanyName = companyName,
                GoalPrompt = goalPrompt,
                IsTestCall = isTestCall || mission.Mode == MissionMode.HybridPilot,
                MaxBudgetINR = 10.0m
            };

            var dispatchResult = await _voiceTelephonyService.InitiateOutboundCallAsync(request, ct);

            if (!dispatchResult.Success)
            {
                return new ToolExecutionResult
                {
                    Success = false,
                    BlockReason = dispatchResult.ErrorMessage ?? "Voice dispatch rejected by provider."
                };
            }

            string evidenceId = $"EVD-VOICE-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

            var output = new
            {
                provider = dispatchResult.Provider.ToString(),
                providerCallId = dispatchResult.ProviderCallId,
                callRecordId = dispatchResult.CallRecordId,
                status = dispatchResult.Status.ToString(),
                estimatedCostINR = dispatchResult.EstimatedCostINR,
                evidenceId = evidenceId
            };

            return new ToolExecutionResult
            {
                Success = true,
                EvidenceId = evidenceId,
                CostINR = dispatchResult.EstimatedCostINR,
                OutputJson = JsonSerializer.Serialize(output)
            };
        }
    }
}
