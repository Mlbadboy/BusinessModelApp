using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Strategy;
using BusinessModelApp.Core.Domain.WorldModel;
using BusinessModelApp.Core.Strategy;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Infrastructure.Strategy
{
    public class DeterministicStrategySimulator : IDeterministicStrategySimulator
    {
        private readonly IReverseFunnelEngine _reverseFunnelEngine;
        private readonly ILogger<DeterministicStrategySimulator> _logger;

        public DeterministicStrategySimulator(
            IReverseFunnelEngine reverseFunnelEngine,
            ILogger<DeterministicStrategySimulator> logger)
        {
            _reverseFunnelEngine = reverseFunnelEngine ?? throw new ArgumentNullException(nameof(reverseFunnelEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public StrategyCandidate SimulateDeterministicStrategy(
            string strategyName,
            BusinessObjective objective,
            CompanySnapshot snapshot,
            decimal targetACVINR,
            double winRate,
            int capacitySlotsRequired,
            decimal requiredBudgetINR)
        {
            _logger.LogInformation("[DeterministicStrategySimulator] Simulating '{StrategyName}' for Target ₹{Target:N0}",
                strategyName, objective.TargetRevenueINR);

            var acvMetric = snapshot.Commercial.HistoricalACVINR.IsGroundedFact
                ? snapshot.Commercial.HistoricalACVINR
                : TruthMetric<decimal>.Historical(targetACVINR, snapshot.Id, "Empirical past deal size record", 0.9);

            var winRateMetric = snapshot.Commercial.HistoricalWinRate.IsGroundedFact
                ? snapshot.Commercial.HistoricalWinRate
                : TruthMetric<double>.Historical(winRate, snapshot.Id, "Empirical closed-won win rate", 0.9);

            var funnel = _reverseFunnelEngine.CalculateFunnel(objective.TargetRevenueINR, acvMetric, winRateMetric, snapshot);

            var candidate = new StrategyCandidate
            {
                StrategyId = Guid.NewGuid(),
                Name = strategyName,
                ObjectiveTitle = objective.Title,
                ObjectiveId = objective.Id,
                ExpectedRevenueINR = objective.TargetRevenueINR,
                RequiredDeliveryCapacitySlots = capacitySlotsRequired,
                RequiredBudgetINR = requiredBudgetINR,
                TimelineDays = 60,
                RequiredFunnel = funnel,
                IsDeterministic = true
            };

            // Grounded Assumptions
            candidate.Assumptions.Add(new AssumptionProvenance
            {
                Key = "TargetACV",
                Description = $"Average Contract Value of ₹{targetACVINR:N0}",
                AssumedValue = $"₹{targetACVINR:N0}",
                Origin = acvMetric.Source,
                EvidenceRecordId = acvMetric.EvidenceRecordId,
                Confidence = acvMetric.Confidence
            });

            candidate.Assumptions.Add(new AssumptionProvenance
            {
                Key = "WinRate",
                Description = $"Opportunity win rate of {winRate * 100:N0}%",
                AssumedValue = $"{winRate * 100:N0}%",
                Origin = winRateMetric.Source,
                EvidenceRecordId = winRateMetric.EvidenceRecordId,
                Confidence = winRateMetric.Confidence
            });

            candidate.Assumptions.Add(new AssumptionProvenance
            {
                Key = "DeliveryCapacity",
                Description = $"Required delivery capacity: {capacitySlotsRequired} concurrent delivery slots",
                AssumedValue = $"{capacitySlotsRequired} slots",
                Origin = MetricProvenanceSource.VerifiedFact,
                Confidence = 1.0
            });

            candidate.Assumptions.Add(new AssumptionProvenance
            {
                Key = "AutonomousGap",
                Description = "Execution addresses the Four-State Autonomous Revenue Gap",
                AssumedValue = $"₹{objective.TargetRevenueINR:N0}",
                Origin = MetricProvenanceSource.ExplicitCeoInput,
                Confidence = 1.0
            });

            // Feasibility Evaluation
            var report = new FeasibilityReport
            {
                StrategyId = candidate.StrategyId,
                StrategyName = strategyName
            };

            int availableSlots = snapshot.Delivery.AvailableDeliverySlots.Value > 0
                ? snapshot.Delivery.AvailableDeliverySlots.Value
                : snapshot.AvailableDeliverySlots.Value;

            if (capacitySlotsRequired > availableSlots)
            {
                report.Classification = StrategyFeasibilityClassification.UnfeasibleResourceConstrained;
                report.Reasons.Add($"Delivery capacity shortfall: Requires {capacitySlotsRequired} slots, but only {availableSlots} are currently available.");
                report.ResourceConstraintsViolated.Add($"Delivery Slots: Required {capacitySlotsRequired}, Available {availableSlots}");
            }
            else if (snapshot.RevenueBaselineState == RevenueBaselineState.Unavailable || !acvMetric.IsGroundedFact)
            {
                report.Classification = StrategyFeasibilityClassification.HypotheticalUnverified;
                report.Reasons.Add("External market/revenue baseline is partially or fully unverified in Company World Model.");
                report.MissingEvidenceItems.Add("Verified gateway/CRM evidence for historical deal closure baseline.");
            }
            else
            {
                report.Classification = StrategyFeasibilityClassification.FeasibleEvidenced;
                report.Reasons.Add("All unit economics backed by verified historical company performance and capacity within limits.");
            }

            candidate.Feasibility = report.Classification;
            candidate.FeasibilityReport = report;
            candidate.ConfidenceScore = candidate.Feasibility == StrategyFeasibilityClassification.FeasibleEvidenced ? 0.90 : 0.60;
            candidate.RiskScore = candidate.Feasibility == StrategyFeasibilityClassification.UnfeasibleResourceConstrained ? 0.85 : 0.25;

            return candidate;
        }
    }
}
