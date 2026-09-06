using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.ExternalReality;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Reputation;
using BusinessModelApp.Core.Interfaces.Runtime.Reputation;

namespace BusinessModelApp.Infrastructure.Runtime.Reputation
{
    public class EmpiricalPerformanceEngine : IEmpiricalPerformanceEngine
    {
        private readonly IReputationStore _store;

        public EmpiricalPerformanceEngine(IReputationStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<CapabilityPerformanceProfile> ProcessEvidenceTokenAsync(ReputationEvidenceToken token, CancellationToken ct = default)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            // 1. Invariant I14-B: Save immutable evidence token
            await _store.SaveEvidenceTokenAsync(token, ct);

            // 2. Load or initialize existing CapabilityPerformanceProfile
            var existing = await _store.GetProfileAsync(
                token.WorkspaceId,
                token.AgentDefinitionId,
                token.CapabilityId,
                token.DomainContext.ToCanonicalKey(),
                token.MarketRegime,
                ct);

            var profile = existing ?? new CapabilityPerformanceProfile
            {
                WorkspaceId = token.WorkspaceId,
                AgentDefinitionId = token.AgentDefinitionId,
                CapabilityId = token.CapabilityId,
                DomainContext = token.DomainContext,
                MarketRegime = token.MarketRegime,
                Version = ProfileVersion.Initial,
                ParentProfileHash = null
            };

            // 3. Check for Critical Security Misconduct Quarantine
            if (token.IsSecurityViolation)
            {
                profile.PolicyViolationCount++;
                profile.Metrics = profile.Metrics with
                {
                    PolicyComplianceScore = 0.0,
                    IsQuarantined = true,
                    QuarantineReason = "Critical security or fencing violation detected. Worker placed in quarantine.",
                    QuarantinedAt = DateTimeOffset.UtcNow
                };

                profile.Version = profile.Version.Next();
                profile.ParentProfileHash = profile.VersionHash;
                profile.VersionHash = profile.ComputeHash();
                profile.LastUpdatedAt = DateTimeOffset.UtcNow;

                await _store.SaveProfileAsync(profile, ct);
                await _store.QuarantineCapabilityProfilesAsync(token.WorkspaceId, token.AgentDefinitionId, token.CapabilityId, "Critical security or fencing violation detected. Worker placed in quarantine.", ct);
                await UpdateAgentAndCapabilityAggregatesAsync(token, profile, ct);
                return profile;
            }

            // 4. Update Execution Counters
            profile.TotalAttempts++;
            if (token.IsSuccessfulExecution)
            {
                profile.SuccessfulAttempts++;
            }
            else
            {
                profile.FailedAttempts++;
            }

            if (token.IsUnknownEffectCrash)
            {
                profile.UnknownEffectCount++;
            }

            // 5. Causal Attribution Weighting (A0/A1 = 0, A2 = 0.25, A3 = 0.8, A4 = 1.0)
            double attributionWeight = token.Attribution.EffectiveReputationWeight;

            // Only update quality metrics if attribution is positive
            if (attributionWeight > 0.0)
            {
                double currentQuality = token.IsSuccessfulExecution ? 1.0 : 0.0;
                double costRatio = token.CostUsdConsumed > 0 ? (double)(token.CostUsdConsumed / 0.25m) : 1.0;
                double latencyRatio = token.Duration.TotalSeconds > 0 ? Math.Clamp(token.Duration.TotalSeconds / 5.0, 0.2, 5.0) : 1.0;
                double rollbackInc = token.IsUnknownEffectCrash ? 1.0 : 0.0;

                double newCalibrationVariance;
                double newQuality;
                double newCostRatio;
                double newLatencyRatio;
                double newRollbackFreq;

                if (profile.TotalAttempts <= 1)
                {
                    // First observation initializes the baseline to avoid warm-up distortion
                    newCalibrationVariance = token.Calibration.DiscrepancyScore;
                    newQuality = currentQuality;
                    newCostRatio = costRatio;
                    newLatencyRatio = latencyRatio;
                    newRollbackFreq = rollbackInc;
                }
                else
                {
                    // Exponential Moving Average alpha dampening with attribution weight
                    double alpha = Math.Clamp(0.20 * attributionWeight, 0.02, 0.35);

                    newCalibrationVariance = (profile.Metrics.CalibrationVariance * (1.0 - alpha)) +
                                             (token.Calibration.DiscrepancyScore * alpha);
                    newQuality = (profile.Metrics.VerificationQualityScore * (1.0 - alpha)) +
                                 (currentQuality * alpha);
                    newCostRatio = (profile.Metrics.CostEfficiencyRatio * (1.0 - alpha)) +
                                   (costRatio * alpha);
                    newLatencyRatio = (profile.Metrics.LatencyPredictabilityRatio * (1.0 - alpha)) +
                                      (latencyRatio * alpha);
                    newRollbackFreq = (profile.Metrics.RollbackFrequency * (1.0 - alpha)) +
                                      (rollbackInc * alpha);
                }

                profile.Metrics = profile.Metrics with
                {
                    CalibrationVariance = Math.Round(newCalibrationVariance, 4),
                    VerificationQualityScore = Math.Round(newQuality, 4),
                    CostEfficiencyRatio = Math.Round(newCostRatio, 4),
                    LatencyPredictabilityRatio = Math.Round(newLatencyRatio, 4),
                    RollbackFrequency = Math.Round(newRollbackFreq, 4)
                };
            }
            else
            {
                // Attribution A0/A1: Do NOT improve calibration/quality score, but track failure or crash penalty
                if (token.IsUnknownEffectCrash)
                {
                    double newRollbackFreq = Math.Min(profile.Metrics.RollbackFrequency + 0.15, 1.0);
                    profile.Metrics = profile.Metrics with { RollbackFrequency = Math.Round(newRollbackFreq, 4) };
                }
            }

            // 6. Version Evolution & SHA-256 Hash Chaining
            profile.Version = profile.Version.Next();
            profile.ParentProfileHash = profile.VersionHash;
            profile.VersionHash = profile.ComputeHash();
            profile.LastUpdatedAt = DateTimeOffset.UtcNow;

            await _store.SaveProfileAsync(profile, ct);
            await UpdateAgentAndCapabilityAggregatesAsync(token, profile, ct);

            return profile;
        }

        public Task<CapabilityPerformanceProfile?> GetProfileAsync(
            Guid workspaceId,
            AgentDefinitionId agentId,
            CapabilityId capabilityId,
            StructuredDomainContext domainContext,
            MarketRegimeState regime,
            CancellationToken ct = default)
        {
            return _store.GetProfileAsync(workspaceId, agentId, capabilityId, domainContext.ToCanonicalKey(), regime, ct);
        }

        public Task<AgentPerformanceProfile?> GetAgentProfileAsync(Guid workspaceId, AgentDefinitionId agentId, CancellationToken ct = default)
        {
            return _store.GetAgentProfileAsync(workspaceId, agentId, ct);
        }

        public Task<CapabilityVersionProfile?> GetCapabilityProfileAsync(CapabilityId capabilityId, CancellationToken ct = default)
        {
            return _store.GetCapabilityProfileAsync(capabilityId, ct);
        }

        public async Task ApplyQuarantineAsync(Guid workspaceId, AgentDefinitionId agentId, CapabilityId capabilityId, string reason, CancellationToken ct = default)
        {
            await _store.QuarantineCapabilityProfilesAsync(workspaceId, agentId, capabilityId, reason, ct);

            var profile = await _store.GetProfileAsync(
                workspaceId,
                agentId,
                capabilityId,
                StructuredDomainContext.Default.ToCanonicalKey(),
                MarketRegimeState.Stable,
                ct);

            if (profile != null)
            {
                profile.Metrics = profile.Metrics with
                {
                    IsQuarantined = true,
                    QuarantineReason = reason,
                    QuarantinedAt = DateTimeOffset.UtcNow,
                    PolicyComplianceScore = 0.0
                };
                profile.Version = profile.Version.Next();
                profile.VersionHash = profile.ComputeHash();
                await _store.SaveProfileAsync(profile, ct);
            }
        }

        private async Task UpdateAgentAndCapabilityAggregatesAsync(ReputationEvidenceToken token, CapabilityPerformanceProfile profile, CancellationToken ct)
        {
            // 1. Update Agent Performance Profile
            var agentProfile = await _store.GetAgentProfileAsync(token.WorkspaceId, token.AgentDefinitionId, ct) ?? new AgentPerformanceProfile
            {
                WorkspaceId = token.WorkspaceId,
                AgentDefinitionId = token.AgentDefinitionId
            };

            agentProfile.TotalAttemptsAcrossAllCapabilities++;
            if (token.IsSuccessfulExecution) agentProfile.TotalSuccessfulAttempts++;
            agentProfile.OverallCalibrationVariance = profile.Metrics.CalibrationVariance;
            agentProfile.OverallVerificationQuality = profile.Metrics.VerificationQualityScore;
            if (profile.Metrics.IsQuarantined) agentProfile.HasAnyQuarantinedCapability = true;
            agentProfile.LastUpdatedAt = DateTimeOffset.UtcNow;
            await _store.SaveAgentProfileAsync(agentProfile, ct);

            // 2. Update Capability Version Profile
            var capProfile = await _store.GetCapabilityProfileAsync(token.CapabilityId, ct) ?? new CapabilityVersionProfile
            {
                CapabilityId = token.CapabilityId
            };

            capProfile.TotalExecutionsAcrossAllAgents++;
            if (!token.IsSuccessfulExecution) capProfile.TotalFailuresAcrossAllAgents++;
            capProfile.MeanCalibrationVarianceAcrossAgents = profile.Metrics.CalibrationVariance;
            capProfile.MeanVerificationQualityAcrossAgents = profile.Metrics.VerificationQualityScore;
            capProfile.LastUpdatedAt = DateTimeOffset.UtcNow;
            await _store.SaveCapabilityProfileAsync(capProfile, ct);
        }
    }
}
