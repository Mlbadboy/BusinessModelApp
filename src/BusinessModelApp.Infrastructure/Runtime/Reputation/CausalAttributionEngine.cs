using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Fleet;
using BusinessModelApp.Core.Domain.Runtime.Reputation;
using BusinessModelApp.Core.Interfaces.Runtime.Reputation;

namespace BusinessModelApp.Infrastructure.Runtime.Reputation
{
    public class CausalAttributionEngine : ICausalAttributionEngine
    {
        public Task<CausalAttributionRecord> EvaluateAttributionAsync(
            ExecutionAttemptId attemptId,
            MissionNodeRecord node,
            AgentOutcomeProposal proposal,
            NodeVerificationResult verification,
            CancellationToken ct = default)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));
            if (verification == null) throw new ArgumentNullException(nameof(verification));

            var confoundingFactors = new List<string>();

            // 1. If verification failed outright -> No attribution possible (A0)
            if (!verification.IsVerified)
            {
                return Task.FromResult(new CausalAttributionRecord
                {
                    AttemptId = attemptId,
                    Level = AttributionLevel.A0_None,
                    AttributionConfidence = 0.0,
                    Rationale = $"Verification failed: {verification.FailureReason ?? "Verification rejected outcome."}",
                    ConfoundingFactors = new[] { "VerificationFailure" }
                });
            }

            // 2. If node has deterministic assertion keys and they all passed with cryptographic evidence
            if (verification.DeterministicAssertionsPassed &&
                !string.IsNullOrWhiteSpace(verification.EvidenceHash) &&
                proposal.ProducedArtifacts != null &&
                proposal.ProducedArtifacts.Count > 0)
            {
                return Task.FromResult(new CausalAttributionRecord
                {
                    AttemptId = attemptId,
                    Level = AttributionLevel.A4_Deterministic,
                    AttributionConfidence = 1.0,
                    Rationale = "Deterministic assertions passed with matching cryptographic artifact evidence. Full causal attribution.",
                    ConfoundingFactors = Array.Empty<string>()
                });
            }

            // 3. High confidence verification without full deterministic assertions -> Strong (A3)
            if (verification.ConfidenceScore >= 0.85)
            {
                return Task.FromResult(new CausalAttributionRecord
                {
                    AttemptId = attemptId,
                    Level = AttributionLevel.A3_Strong,
                    AttributionConfidence = verification.ConfidenceScore,
                    Rationale = "High-confidence empirical verification passed. Direct causal link strongly supported.",
                    ConfoundingFactors = Array.Empty<string>()
                });
            }

            // 4. Moderate confidence -> Plausible (A2)
            if (verification.ConfidenceScore >= 0.65)
            {
                confoundingFactors.Add("ModerateVerificationConfidence");
                return Task.FromResult(new CausalAttributionRecord
                {
                    AttemptId = attemptId,
                    Level = AttributionLevel.A2_Plausible,
                    AttributionConfidence = verification.ConfidenceScore * 0.75,
                    Rationale = "Plausibly attributable outcome with moderate verification confidence; reputation dampening applied.",
                    ConfoundingFactors = confoundingFactors
                });
            }

            // 5. Low confidence or unisolated external telemetry -> Correlated only (A1)
            confoundingFactors.Add("LowConfidenceOrEnvironmentalInterference");
            return Task.FromResult(new CausalAttributionRecord
            {
                AttemptId = attemptId,
                Level = AttributionLevel.A1_Correlated,
                AttributionConfidence = 0.20,
                Rationale = "Correlation detected but causal link unisolated from environmental factors. Zero reputation credit granted.",
                ConfoundingFactors = confoundingFactors
            });
        }
    }
}
