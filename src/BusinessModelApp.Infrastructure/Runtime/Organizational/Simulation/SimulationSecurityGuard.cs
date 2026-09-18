using System.Text.RegularExpressions;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Simulation;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Simulation;

public sealed class SimulationSecurityGuard : ISimulationSecurityGuard
{
    private static readonly Regex InjectionPattern = new(
        @"(ignore\s+(all\s+)?previous\s+instructions|system\s+prompt|execute\s+command|grant\s+authority|issue\s+permit|executionpermit|bypass\s+firewall|\broot\b|disregard)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string SanitizeAgentPayload(string rawPayload)
    {
        if (string.IsNullOrWhiteSpace(rawPayload))
            return string.Empty;

        // Redact suspicious prompt injection tokens
        string sanitized = InjectionPattern.Replace(rawPayload, "[REDACTED_SIMULATION_PAYLOAD]");
        return sanitized;
    }

    public bool ValidateNoExecutionPermitRequested(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return true;

        string normalized = payload.Replace(" ", "").Replace("-", "").Replace("_", "");
        if (normalized.Contains("ExecutionPermit", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("CreatePermit", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("DirectConnectorAction", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("BypassFirewall", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    public bool ValidateNoProductionCredentialExposed(SimulationAgent agent)
    {
        if (agent == null) return false;

        // Synthetic agents must never carry real production credentials (I34-W)
        if (agent.HasProductionCredentials || agent.HasExecutionPermitRights || agent.CanCallRealWorldConnectors)
        {
            return false;
        }

        return true;
    }
}
