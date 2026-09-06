using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.ExternalReality;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Capabilities;
using BusinessModelApp.Core.Domain.Runtime.Fleet;
using BusinessModelApp.Core.Domain.Runtime.Reputation;
using BusinessModelApp.Core.Interfaces.Missions;
using BusinessModelApp.Core.Interfaces.Runtime.Reputation;

namespace BusinessModelApp.Infrastructure.Runtime.Reputation
{
    public class ReputationAwareRouter : IReputationAwareRouter
    {
        private readonly IReputationStore _store;
        private readonly ICapabilityRegistry _registry;

        public ReputationAwareRouter(IReputationStore store, ICapabilityRegistry registry)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public async Task<RoutingDecisionRecord> RouteNodeWorkerAsync(
            MissionGraph graph,
            MissionNodeRecord node,
            IReadOnlyList<WorkerProcessRecord> candidates,
            TenantMissionPolicyContext tenantPolicy,
            MarketRegimeState regime = MarketRegimeState.Stable,
            CancellationToken ct = default)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));

            var capabilityId = node.ExecutionPolicy.RequiredCapabilityId ?? new CapabilityId("default", "v1");

            // 1. Fetch capability definition for risk evaluation and domain context
            var capDef = await _registry.GetCapabilityAsync(capabilityId, ct);
            var domainContext = capDef?.DomainContext ?? StructuredDomainContext.Default;
            bool isHighRisk = capDef != null && capDef.RiskTier >= CapabilityRiskTier.R3_HighOperational;
            if (node.ExecutionPolicy.RequiredAutonomyTier >= AutonomyTier.L4_ExecuteWithApproval)
            {
                isHighRisk = true;
            }

            var evaluations = new List<CandidateRoutingEvaluation>();

            // Weights adapt based on node priority and risk class
            var (wCal, wQual, wCost, wLat, wRoll) = ComputeContextualWeights(isHighRisk);

            foreach (var candidate in candidates)
            {
                // Infer AgentDefinitionId from candidate worker or store mapping
                var agentDefId = await _store.GetAgentForWorkerAsync(candidate.WorkerId, ct)
                    ?? (candidate.BoundAgentInstanceId != null
                        ? AgentDefinitionId.From(node.NodeType.ToString())
                        : AgentDefinitionId.From(candidate.PoolType.ToString()));

                // Fetch empirical profile
                var profile = await _store.GetProfileAsync(
                    graph.WorkspaceId,
                    agentDefId,
                    capabilityId,
                    domainContext.ToCanonicalKey(),
                    regime,
                    ct);

                var agentProfile = await _store.GetAgentProfileAsync(graph.WorkspaceId, agentDefId, ct);
                bool isQuarantined = (profile != null && profile.Metrics.IsQuarantined) ||
                                     (agentProfile != null && agentProfile.HasAnyQuarantinedCapability) ||
                                     candidate.HealthStatus == WorkerHealthStatus.Quarantined;

                if (isQuarantined)
                {
                    evaluations.Add(new CandidateRoutingEvaluation
                    {
                        WorkerId = candidate.WorkerId,
                        AgentDefinitionId = agentDefId,
                        ProfileVersionUsed = profile?.Version ?? ProfileVersion.Initial,
                        UtilityScore = -100.0,
                        ConfidenceDampening = 0.0,
                        IsEligible = false,
                        IneligibilityReason = "Worker or capability profile is in security quarantine.",
                        IsQuarantined = true,
                        IsColdStartExploration = false,
                        MetricSnapshot = profile?.Metrics ?? ReputationMetricVector.Initial
                    });
                    continue;
                }

                if (candidate.HealthStatus != WorkerHealthStatus.Healthy)
                {
                    evaluations.Add(new CandidateRoutingEvaluation
                    {
                        WorkerId = candidate.WorkerId,
                        AgentDefinitionId = agentDefId,
                        ProfileVersionUsed = profile?.Version ?? ProfileVersion.Initial,
                        UtilityScore = -50.0,
                        ConfidenceDampening = 0.0,
                        IsEligible = false,
                        IneligibilityReason = $"Worker health status is {candidate.HealthStatus}.",
                        IsQuarantined = false,
                        IsColdStartExploration = false,
                        MetricSnapshot = profile?.Metrics ?? ReputationMetricVector.Initial
                    });
                    continue;
                }

                // Check Cold-Start Exploration vs Risk Gate
                bool isColdStart = profile == null || profile.TotalAttempts == 0;
                if (isColdStart && isHighRisk)
                {
                    evaluations.Add(new CandidateRoutingEvaluation
                    {
                        WorkerId = candidate.WorkerId,
                        AgentDefinitionId = agentDefId,
                        ProfileVersionUsed = ProfileVersion.Initial,
                        UtilityScore = 0.0,
                        ConfidenceDampening = 0.20,
                        IsEligible = false,
                        IneligibilityReason = "Cold-start unproven worker strictly forbidden on high-risk node.",
                        IsQuarantined = false,
                        IsColdStartExploration = false,
                        MetricSnapshot = ReputationMetricVector.Initial
                    });
                    continue;
                }

                // Compute Confidence Dampening Factor
                double confidence = profile?.ComputeConfidenceFactor() ?? 0.35;

                // Multi-dimensional utility with Bayesian empirical shrinkage
                var m = profile?.Metrics ?? ReputationMetricVector.Initial;
                double effVariance = (confidence * m.CalibrationVariance) + ((1.0 - confidence) * 0.35);
                double effQuality = (confidence * m.VerificationQualityScore) + ((1.0 - confidence) * 0.50);

                double rawUtility = (wCal * (1.0 - effVariance)) +
                                    (wQual * effQuality) +
                                    (wCost * Math.Clamp(1.0 / Math.Max(m.CostEfficiencyRatio, 0.05), 0.1, 4.0)) +
                                    (wLat * Math.Clamp(1.0 / Math.Max(m.LatencyPredictabilityRatio, 0.2), 0.2, 2.0)) -
                                    (wRoll * m.RollbackFrequency) -
                                    ((1.0 - confidence) * 0.10);

                double adjustedUtility = Math.Round(rawUtility, 4);

                evaluations.Add(new CandidateRoutingEvaluation
                {
                    WorkerId = candidate.WorkerId,
                    AgentDefinitionId = agentDefId,
                    ProfileVersionUsed = profile?.Version ?? ProfileVersion.Initial,
                    UtilityScore = adjustedUtility,
                    ConfidenceDampening = confidence,
                    IsEligible = true,
                    IneligibilityReason = null,
                    IsQuarantined = false,
                    IsColdStartExploration = isColdStart,
                    MetricSnapshot = m
                });
            }

            var eligible = evaluations.Where(e => e.IsEligible).ToList();
            var winner = eligible.OrderByDescending(e => e.UtilityScore).FirstOrDefault();

            string rationale;
            if (winner != null)
            {
                rationale = $"Selected Worker '{winner.WorkerId}' (Agent: '{winner.AgentDefinitionId.Value}') with highest empirical utility score {winner.UtilityScore:F4} (confidence factor {winner.ConfidenceDampening:F2}).";
            }
            else
            {
                rationale = "No eligible candidate available. Candidates either failed risk gate, are in security quarantine, or in degraded health.";
            }

            var decision = new RoutingDecisionRecord
            {
                WorkspaceId = graph.WorkspaceId,
                GraphId = graph.GraphId,
                NodeId = node.NodeId,
                RequiredCapabilityId = capabilityId,
                DomainContext = domainContext,
                MarketRegime = regime,
                MissionPriority = MissionPriority.P2_Medium,
                RequiredAutonomyTier = node.ExecutionPolicy.RequiredAutonomyTier,
                SelectedWorkerId = winner?.WorkerId,
                SelectedAgentDefinitionId = winner?.AgentDefinitionId,
                SelectedProfileVersion = winner?.ProfileVersionUsed,
                CandidateEvaluations = evaluations,
                SelectionRationale = rationale,
                WasExplorationCandidate = winner?.IsColdStartExploration ?? false
            };

            await _store.SaveRoutingDecisionAsync(decision, ct);
            return decision;
        }

        private static (double wCal, double wQual, double wCost, double wLat, double wRoll) ComputeContextualWeights(bool isHighRisk)
        {
            if (isHighRisk)
            {
                // High risk / critical: calibration, verification quality, and rollback stability dominate
                return (wCal: 0.60, wQual: 0.35, wCost: 0.02, wLat: 0.03, wRoll: 0.40);
            }

            // Low risk: balanced trade-off with calibration remaining primary
            return (wCal: 0.45, wQual: 0.25, wCost: 0.15, wLat: 0.05, wRoll: 0.10);
        }
    }
}
