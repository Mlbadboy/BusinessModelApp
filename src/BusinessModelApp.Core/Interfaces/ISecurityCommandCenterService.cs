using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Security;

namespace BusinessModelApp.Core.Interfaces
{
    public interface ISecurityCommandCenterService
    {
        // Target Allowlist Governance
        Task<SecurityTargetRegistration> RegisterTargetAsync(SecurityTargetRegistration target, CancellationToken ct = default);
        Task<bool> ValidateTargetAllowlistAsync(Guid targetId, Guid workspaceId, string requestedCapability, CancellationToken ct = default);
        Task<IReadOnlyList<SecurityTargetRegistration>> ListTargetsAsync(Guid workspaceId, CancellationToken ct = default);

        // Security Posture Scoring
        Task<SecurityPostureScore> CalculatePostureScoreAsync(Guid workspaceId, CancellationToken ct = default);

        // Emergency Kill Switch
        Task<KillSwitchStatus> TriggerKillSwitchAsync(string reason, string initiatedBy, CancellationToken ct = default);
        Task<KillSwitchStatus> GetKillSwitchStatusAsync(CancellationToken ct = default);
        Task<KillSwitchStatus> ResetKillSwitchAsync(string initiatedBy, CancellationToken ct = default);

        // Red Team Scoped Campaigns
        Task<RedTeamCampaign> StartCampaignAsync(RedTeamCampaign campaign, CancellationToken ct = default);
        Task<RedTeamCampaign?> GetCampaignAsync(Guid campaignId, Guid workspaceId, CancellationToken ct = default);
        Task<IReadOnlyList<RedTeamCampaign>> ListCampaignsAsync(Guid workspaceId, CancellationToken ct = default);

        // Finding Evidence Chain & Findings Lifecycle
        Task<VulnerabilityFinding> RecordFindingAsync(VulnerabilityFinding finding, CancellationToken ct = default);
        Task<IReadOnlyList<VulnerabilityFinding>> ListFindingsAsync(Guid workspaceId, VulnerabilitySeverity? minSeverity = null, CancellationToken ct = default);
        Task<VulnerabilityFinding?> GetFindingAsync(Guid findingId, Guid workspaceId, CancellationToken ct = default);

        // Blue Team Remediations & Regression Gate
        Task<BlueTeamRemediation> ProposeRemediationAsync(BlueTeamRemediation remediation, CancellationToken ct = default);
        Task<BlueTeamRemediation> ApplyRemediationAsync(Guid remediationId, Guid workspaceId, bool isGovernanceApproved = false, CancellationToken ct = default);
        Task<IReadOnlyList<BlueTeamRemediation>> ListRemediationsAsync(Guid workspaceId, CancellationToken ct = default);
    }
}
