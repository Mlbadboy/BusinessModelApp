using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BusinessModelApp.Core.Domain.Security;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;

namespace BusinessModelApp.Infrastructure.Security
{
    public class SecurityCommandCenterService : ISecurityCommandCenterService
    {
        private readonly AppDbContext _dbContext;
        private readonly SecurityTargetRegistry _targetRegistry;
        private readonly RedTeamAutonomousEngine _redTeamEngine;
        private readonly BlueTeamRemediationEngine _blueTeamEngine;

        public SecurityCommandCenterService(
            AppDbContext dbContext,
            SecurityTargetRegistry targetRegistry,
            RedTeamAutonomousEngine redTeamEngine,
            BlueTeamRemediationEngine blueTeamEngine)
        {
            _dbContext = dbContext;
            _targetRegistry = targetRegistry;
            _redTeamEngine = redTeamEngine;
            _blueTeamEngine = blueTeamEngine;
        }

        // =========================================================================
        // TARGET ALLOWLIST GOVERNANCE
        // =========================================================================
        public async Task<SecurityTargetRegistration> RegisterTargetAsync(SecurityTargetRegistration target, CancellationToken ct = default)
        {
            return await _targetRegistry.RegisterTargetAsync(target, ct);
        }

        public async Task<bool> ValidateTargetAllowlistAsync(Guid targetId, Guid workspaceId, string requestedCapability, CancellationToken ct = default)
        {
            return await _targetRegistry.ValidateTargetAllowlistAsync(targetId, workspaceId, requestedCapability, ct);
        }

        public async Task<IReadOnlyList<SecurityTargetRegistration>> ListTargetsAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return await _targetRegistry.ListTargetsAsync(workspaceId, ct);
        }

        // =========================================================================
        // SECURITY POSTURE SCORING
        // =========================================================================
        public async Task<SecurityPostureScore> CalculatePostureScoreAsync(Guid workspaceId, CancellationToken ct = default)
        {
            var findings = await _dbContext.VulnerabilityFindings
                .Where(f => f.WorkspaceId == workspaceId && f.Status != VulnerabilityStatus.Remediated && f.Status != VulnerabilityStatus.FalsePositive)
                .ToListAsync(ct);

            int critical = findings.Count(f => f.Severity == VulnerabilitySeverity.Critical);
            int high = findings.Count(f => f.Severity == VulnerabilitySeverity.High);
            int medium = findings.Count(f => f.Severity == VulnerabilitySeverity.Medium);
            int low = findings.Count(f => f.Severity == VulnerabilitySeverity.Low);
            int contained = findings.Count(f => f.IsContainedInSandbox && f.Status == VulnerabilityStatus.Contained);

            // Deductions
            double penalty = (critical * 25.0) + (high * 15.0) + (medium * 5.0) + (low * 2.0);
            double containmentCredit = (contained * 10.0);
            double score = Math.Clamp(100.0 - penalty + containmentCredit, 10.0, 100.0);

            string rating = score switch
            {
                >= 90.0 => "Excellent",
                >= 75.0 => "Good",
                >= 50.0 => "Degraded",
                _ => "Critical"
            };

            return new SecurityPostureScore
            {
                WorkspaceId = workspaceId,
                OverallScore = Math.Round(score, 1),
                Rating = rating,
                IdentityAndAccessScore = Math.Clamp(100.0 - (critical * 20.0), 20.0, 100.0),
                EpistemicIntegrityScore = Math.Clamp(100.0 - (high * 15.0), 30.0, 100.0),
                TenantIsolationScore = Math.Clamp(100.0 - (critical * 30.0), 10.0, 100.0),
                PromptInjectionImmunityScore = Math.Clamp(100.0 - (high * 20.0), 20.0, 100.0),
                CryptographicLineageScore = 100.0,
                AuditImmutabilityScore = 100.0,
                OpenCriticalFindings = critical,
                OpenHighFindings = high,
                OpenMediumFindings = medium,
                OpenLowFindings = low,
                ContainedFindings = contained,
                CalculatedAt = DateTime.UtcNow
            };
        }

        // =========================================================================
        // EMERGENCY KILL SWITCH
        // =========================================================================
        public Task<KillSwitchStatus> TriggerKillSwitchAsync(string reason, string initiatedBy, CancellationToken ct = default)
        {
            var status = KillSwitchManager.Trigger(reason, initiatedBy);
            return Task.FromResult(status);
        }

        public Task<KillSwitchStatus> GetKillSwitchStatusAsync(CancellationToken ct = default)
        {
            return Task.FromResult(KillSwitchManager.GetStatus());
        }

        public Task<KillSwitchStatus> ResetKillSwitchAsync(string initiatedBy, CancellationToken ct = default)
        {
            var status = KillSwitchManager.Reset(initiatedBy);
            return Task.FromResult(status);
        }

        // =========================================================================
        // RED TEAM CAMPAIGNS
        // =========================================================================
        public async Task<RedTeamCampaign> StartCampaignAsync(RedTeamCampaign campaign, CancellationToken ct = default)
        {
            KillSwitchManager.AssertNotHalted();

            if (campaign.WorkspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must be set.", nameof(campaign));

            // Validate target allowlist before starting campaign
            await _targetRegistry.ValidateTargetAllowlistAsync(campaign.TargetSandboxId, campaign.WorkspaceId, "ReadSandboxData", ct);

            campaign.Status = RedTeamCampaignStatus.Running;
            campaign.StartedAt = DateTime.UtcNow;
            campaign.CreatedAt = DateTime.UtcNow;

            _dbContext.RedTeamCampaigns.Add(campaign);
            await _dbContext.SaveChangesAsync(ct);

            return campaign;
        }

        public async Task<RedTeamCampaign?> GetCampaignAsync(Guid campaignId, Guid workspaceId, CancellationToken ct = default)
        {
            return await _dbContext.RedTeamCampaigns
                .FirstOrDefaultAsync(c => c.Id == campaignId && c.WorkspaceId == workspaceId, ct);
        }

        public async Task<IReadOnlyList<RedTeamCampaign>> ListCampaignsAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return await _dbContext.RedTeamCampaigns
                .Where(c => c.WorkspaceId == workspaceId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(ct);
        }

        // =========================================================================
        // FINDINGS LIFECYCLE & EVIDENCE CHAIN
        // =========================================================================
        public async Task<VulnerabilityFinding> RecordFindingAsync(VulnerabilityFinding finding, CancellationToken ct = default)
        {
            if (finding.WorkspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must be set.", nameof(finding));

            finding.DiscoveredAt = DateTime.UtcNow;
            _dbContext.VulnerabilityFindings.Add(finding);
            await _dbContext.SaveChangesAsync(ct);
            return finding;
        }

        public async Task<IReadOnlyList<VulnerabilityFinding>> ListFindingsAsync(Guid workspaceId, VulnerabilitySeverity? minSeverity = null, CancellationToken ct = default)
        {
            var query = _dbContext.VulnerabilityFindings.Where(f => f.WorkspaceId == workspaceId);
            if (minSeverity.HasValue)
            {
                query = query.Where(f => f.Severity >= minSeverity.Value);
            }
            return await query
                .OrderByDescending(f => f.Severity)
                .ThenByDescending(f => f.DiscoveredAt)
                .ToListAsync(ct);
        }

        public async Task<VulnerabilityFinding?> GetFindingAsync(Guid findingId, Guid workspaceId, CancellationToken ct = default)
        {
            return await _dbContext.VulnerabilityFindings
                .FirstOrDefaultAsync(f => f.Id == findingId && f.WorkspaceId == workspaceId, ct);
        }

        // =========================================================================
        // BLUE TEAM REMEDIATIONS
        // =========================================================================
        public async Task<BlueTeamRemediation> ProposeRemediationAsync(BlueTeamRemediation remediation, CancellationToken ct = default)
        {
            if (remediation.WorkspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must be set.", nameof(remediation));

            remediation.CreatedAt = DateTime.UtcNow;
            _dbContext.BlueTeamRemediations.Add(remediation);
            await _dbContext.SaveChangesAsync(ct);
            return remediation;
        }

        public async Task<BlueTeamRemediation> ApplyRemediationAsync(Guid remediationId, Guid workspaceId, bool isGovernanceApproved = false, CancellationToken ct = default)
        {
            var rem = await _dbContext.BlueTeamRemediations
                .FirstOrDefaultAsync(r => r.Id == remediationId && r.WorkspaceId == workspaceId, ct);

            if (rem == null)
            {
                throw new KeyNotFoundException($"BlueTeamRemediation {remediationId} not found in workspace {workspaceId}.");
            }

            if (rem.RequiresGovernanceApproval && !isGovernanceApproved)
            {
                throw new InvalidOperationException(
                    "Sovereign governance sign-off required: Automated application of production capability modification is forbidden.");
            }

            rem.IsApproved = true;
            rem.ApprovedBy = isGovernanceApproved ? "SecurityGovernor" : "AutonomousSandboxDefense";
            rem.AppliedAt = DateTime.UtcNow;

            // Update associated finding
            var finding = await _dbContext.VulnerabilityFindings.FindAsync(new object[] { rem.FindingId }, ct);
            if (finding != null)
            {
                finding.Status = VulnerabilityStatus.Remediated;
                finding.RemediatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(ct);
            return rem;
        }

        public async Task<IReadOnlyList<BlueTeamRemediation>> ListRemediationsAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return await _dbContext.BlueTeamRemediations
                .Where(r => r.WorkspaceId == workspaceId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(ct);
        }
    }
}
