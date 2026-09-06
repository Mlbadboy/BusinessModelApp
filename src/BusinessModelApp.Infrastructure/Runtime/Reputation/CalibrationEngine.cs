using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.ExternalReality;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Fleet;
using BusinessModelApp.Core.Domain.Runtime.Reputation;
using BusinessModelApp.Core.Interfaces.Runtime.Reputation;

namespace BusinessModelApp.Infrastructure.Runtime.Reputation
{
    public class CalibrationEngine : ICalibrationEngine
    {
        public Task<OutcomeCalibrationRecord> CalculateCalibrationAsync(
            ExecutionAttemptId attemptId,
            MissionNodeRecord node,
            AgentOutcomeProposal proposal,
            NodeVerificationResult verification,
            StructuredDomainContext domainContext,
            MarketRegimeState regime,
            CancellationToken ct = default)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));
            if (verification == null) throw new ArgumentNullException(nameof(verification));

            double discrepancy = 0.0;
            bool withinTolerance = true;

            // 1. If verification failed, max discrepancy
            if (!verification.IsVerified)
            {
                discrepancy = 1.0;
                withinTolerance = false;
            }
            else
            {
                // 2. Try parsing numeric predicted vs actual metrics from payload
                try
                {
                    if (!string.IsNullOrWhiteSpace(proposal.OutputPayloadJson))
                    {
                        using var doc = JsonDocument.Parse(proposal.OutputPayloadJson);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("predictedValue", out var predElem) &&
                            root.TryGetProperty("actualValue", out var actElem) &&
                            predElem.TryGetDouble(out var predVal) &&
                            actElem.TryGetDouble(out var actVal))
                        {
                            double denom = Math.Max(Math.Abs(actVal), 1.0);
                            double relativeError = Math.Abs(predVal - actVal) / denom;
                            discrepancy = Math.Clamp(Math.Round(relativeError, 4), 0.0, 1.0);
                            withinTolerance = discrepancy <= 0.15; // 15% tolerance
                        }
                        else if (root.TryGetProperty("calibrationDiscrepancy", out var discElem) &&
                                 discElem.TryGetDouble(out var explicitDisc))
                        {
                            discrepancy = Math.Clamp(Math.Round(explicitDisc, 4), 0.0, 1.0);
                            withinTolerance = discrepancy <= 0.15;
                        }
                        else
                        {
                            // In the absence of explicit numerical prediction, discrepancy is inverted verification confidence
                            discrepancy = Math.Clamp(Math.Round(1.0 - verification.ConfidenceScore, 4), 0.0, 1.0);
                            withinTolerance = verification.ConfidenceScore >= node.VerificationCriteria.MinConfidenceScore;
                        }
                    }
                    else
                    {
                        discrepancy = Math.Clamp(Math.Round(1.0 - verification.ConfidenceScore, 4), 0.0, 1.0);
                        withinTolerance = verification.ConfidenceScore >= node.VerificationCriteria.MinConfidenceScore;
                    }
                }
                catch
                {
                    discrepancy = Math.Clamp(Math.Round(1.0 - verification.ConfidenceScore, 4), 0.0, 1.0);
                    withinTolerance = verification.IsVerified;
                }
            }

            var record = new OutcomeCalibrationRecord
            {
                AttemptId = attemptId,
                NodeId = node.NodeId,
                GraphId = node.GraphId,
                AgentDefinitionId = proposal.Envelope?.AgentInstanceId != null
                    ? AgentDefinitionId.From(node.NodeType.ToString())
                    : AgentDefinitionId.From("general"),
                WorkerId = proposal.Envelope?.WorkerId ?? WorkerProcessId.New(),
                CapabilityId = node.ExecutionPolicy.RequiredCapabilityId ?? new CapabilityId("default", "v1"),
                DomainContext = domainContext ?? StructuredDomainContext.Default,
                MarketRegime = regime,
                ExpectedOutputJson = node.VerificationCriteria.ExpectedOutputSchema ?? "{}",
                ActualVerifiedOutputJson = proposal.OutputPayloadJson,
                DiscrepancyScore = discrepancy,
                IsWithinTolerance = withinTolerance
            };

            return Task.FromResult(record);
        }
    }
}
