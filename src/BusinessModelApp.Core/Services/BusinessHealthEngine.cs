using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BusinessModelApp.Core.Domain.Commercial;

namespace BusinessModelApp.Core.Services
{
    public interface IBusinessHealthEngine
    {
        BusinessHealthResult CalculateHealth(
            IEnumerable<Lead> leads,
            IEnumerable<Opportunity> opportunities,
            decimal quarterlyTarget = 5000000m,
            DateTime? asOfDate = null);
    }

    public class BusinessHealthEngine : IBusinessHealthEngine
    {
        public const string Version = "HealthEngine:v1.0";
        public const double WeightPipeline = 0.40;
        public const double WeightConversion = 0.25;
        public const double WeightVelocity = 0.20;
        public const double WeightRisk = 0.15;

        public BusinessHealthResult CalculateHealth(
            IEnumerable<Lead> leadsInput,
            IEnumerable<Opportunity> opportunitiesInput,
            decimal quarterlyTarget = 5000000m,
            DateTime? asOfDate = null)
        {
            var now = asOfDate ?? DateTime.UtcNow;
            var leads = leadsInput?.ToList() ?? new List<Lead>();
            var opportunities = opportunitiesInput?.ToList() ?? new List<Opportunity>();

            // 1. Core Financial Deterministic Aggregations
            var activeOpps = opportunities.Where(o => o.Stage != OpportunityStage.ClosedLost).ToList();
            var closedWonOpps = opportunities.Where(o => o.Stage == OpportunityStage.ClosedWon).ToList();
            var closedLostOpps = opportunities.Where(o => o.Stage == OpportunityStage.ClosedLost).ToList();

            var totalPipeline = activeOpps.Sum(o => o.EstimatedValue);
            var weightedForecast = activeOpps.Sum(o => o.EstimatedValue * (decimal)o.Probability);
            var closedWonRevenue = closedWonOpps.Sum(o => o.EstimatedValue);

            var effectiveTarget = quarterlyTarget > 0 ? quarterlyTarget : 5000000m;
            var coverageRatio = effectiveTarget > 0 ? (double)(weightedForecast / effectiveTarget) : 0.0;

            // 2. Conversion Metrics
            var totalResolved = closedWonOpps.Count + closedLostOpps.Count;
            var winRate = totalResolved >= 3 
                ? (double)closedWonOpps.Count / totalResolved 
                : (closedWonOpps.Count > 0 ? 0.6 : 0.5); // Baseline hypothesis if sample < 3

            var qualifiedLeadsCount = leads.Count(l => l.Status == LeadStatus.Qualified || l.Opportunity != null);
            var leadQualRate = leads.Count >= 3 
                ? (double)qualifiedLeadsCount / leads.Count 
                : (leads.Count > 0 ? 0.5 : 0.0);

            // 3. Velocity Metric (Average days in pipeline or to close)
            var velocityDays = 30.0; // Default
            if (opportunities.Any())
            {
                var durations = opportunities.Select(o =>
                {
                    var endDate = (o.Stage == OpportunityStage.ClosedWon || o.Stage == OpportunityStage.ClosedLost)
                        ? o.UpdatedAt
                        : now;
                    return Math.Max(1.0, (endDate - o.CreatedAt).TotalDays);
                }).ToList();
                velocityDays = durations.Average();
            }

            // 4. Stalled Risk Index (% of active pipeline without activity for > 14 days)
            var stalledThreshold = now.AddDays(-14);
            var stalledOpps = activeOpps.Where(o =>
            {
                var lastActivityDate = o.Activities?.OrderByDescending(a => a.CreatedAt).FirstOrDefault()?.CreatedAt ?? o.CreatedAt;
                return lastActivityDate < stalledThreshold;
            }).ToList();

            var stalledValue = stalledOpps.Sum(o => o.EstimatedValue);
            var stalledRiskIndex = totalPipeline > 0 ? (double)(stalledValue / totalPipeline) * 100.0 : 0.0;

            // 5. Compute Sub-scores (0 to 100)
            // S_pipeline: 2.0x coverage = 100 pts
            var sPipeline = Math.Clamp((coverageRatio / 2.0) * 100.0, 0.0, 100.0);

            // S_conversion: (WinRate * 50) + (LeadQualRate * 50)
            var sConversion = Math.Clamp((winRate * 50.0) + (leadQualRate * 50.0), 0.0, 100.0);

            // S_velocity: Clamp(100 - (AvgDays * 1.5), 20, 100)
            var sVelocity = Math.Clamp(100.0 - (velocityDays * 1.5), 20.0, 100.0);

            // S_risk: Clamp(100 - StalledRiskIndex, 0, 100)
            var sRisk = Math.Clamp(100.0 - stalledRiskIndex, 0.0, 100.0);

            // 6. Overall Composite Health Score
            var overallHealth = (sPipeline * WeightPipeline) +
                                (sConversion * WeightConversion) +
                                (sVelocity * WeightVelocity) +
                                (sRisk * WeightRisk);

            // 7. Data Completeness & Confidence Score Formulation
            var oppFactor = Math.Min(1.0, opportunities.Count / 5.0);
            var leadFactor = Math.Min(1.0, leads.Count / 10.0);
            var activityCount = opportunities.Sum(o => o.Activities?.Count ?? 0);
            var activityFactor = Math.Min(1.0, activityCount / 15.0);

            var confidence = (oppFactor * 0.40) + (leadFactor * 0.30) + (activityFactor * 0.30);
            var confidenceLevel = confidence >= 0.75 ? "High" : (confidence >= 0.40 ? "Medium" : "Low");
            var confidenceReason = confidenceLevel switch
            {
                "High" => "Robust statistical sample across opportunities, leads, and activity logs.",
                "Medium" => "Moderate sample size; early pipeline trends established.",
                _ => "Insufficient historical data sample size; scores reflect early baselines rather than definitive operational health."
            };

            var inr = new CultureInfo("en-IN");

            var result = new BusinessHealthResult
            {
                OverallHealthScore = Math.Round(overallHealth, 1),
                ConfidenceScore = Math.Round(confidence, 2),
                ConfidenceLevel = confidenceLevel,
                ConfidenceReason = confidenceReason,
                CalculationVersion = Version,
                GeneratedAt = now,
                TotalPipelineValue = totalPipeline,
                WeightedForecastValue = weightedForecast,
                ClosedWonRevenue = closedWonRevenue,
                QuarterlyTarget = effectiveTarget,
                PipelineCoverageRatio = Math.Round(coverageRatio, 2),
                WinRate = Math.Round(winRate, 2),
                LeadQualificationRate = Math.Round(leadQualRate, 2),
                AvgVelocityDays = Math.Round(velocityDays, 1),
                StalledRiskIndex = Math.Round(stalledRiskIndex, 1),
                SubScores = new HealthSubScores
                {
                    PipelineScore = Math.Round(sPipeline, 1),
                    ConversionScore = Math.Round(sConversion, 1),
                    VelocityScore = Math.Round(sVelocity, 1),
                    RiskScore = Math.Round(sRisk, 1)
                },
                ComponentBreakdown = new List<ScoreComponentDetail>
                {
                    new ScoreComponentDetail
                    {
                        ComponentName = "Pipeline Coverage",
                        WeightPercent = WeightPipeline * 100,
                        RawScore = Math.Round(sPipeline, 1),
                        WeightedContribution = Math.Round(sPipeline * WeightPipeline, 1),
                        Explanation = $"Weighted pipeline of {weightedForecast.ToString("C0", inr)} against target {effectiveTarget.ToString("C0", inr)} yields {coverageRatio:F2}x coverage."
                    },
                    new ScoreComponentDetail
                    {
                        ComponentName = "Commercial Conversion",
                        WeightPercent = WeightConversion * 100,
                        RawScore = Math.Round(sConversion, 1),
                        WeightedContribution = Math.Round(sConversion * WeightConversion, 1),
                        Explanation = $"Win rate at {winRate * 100:F0}% and lead qualification at {leadQualRate * 100:F0}%."
                    },
                    new ScoreComponentDetail
                    {
                        ComponentName = "Deal Velocity",
                        WeightPercent = WeightVelocity * 100,
                        RawScore = Math.Round(sVelocity, 1),
                        WeightedContribution = Math.Round(sVelocity * WeightVelocity, 1),
                        Explanation = $"Average opportunity cycle duration is {velocityDays:F1} days."
                    },
                    new ScoreComponentDetail
                    {
                        ComponentName = "Risk Mitigation",
                        WeightPercent = WeightRisk * 100,
                        RawScore = Math.Round(sRisk, 1),
                        WeightedContribution = Math.Round(sRisk * WeightRisk, 1),
                        Explanation = $"{stalledRiskIndex:F1}% of pipeline has been inactive for > 14 days."
                    }
                },
                EvidenceRecords = new List<EvidenceRecord>()
            };

            // Build Evidence Records for each key metric
            result.EvidenceRecords.Add(new EvidenceRecord
            {
                EvidenceId = "EVD-PIPE-01",
                EvidenceType = "Pipeline",
                MetricKey = "METRIC_WEIGHTED_FORECAST",
                DisplayName = "Weighted Forecast",
                NumericValue = (double)weightedForecast,
                FormattedValue = weightedForecast.ToString("C0", inr),
                CalculationVersion = Version,
                Formula = "Σ(EstimatedValue * Probability)",
                Period = "Current Quarter",
                ConfidenceScore = Math.Round(confidence, 2),
                ImpactLevel = weightedForecast >= effectiveTarget ? "High" : "Critical",
                GeneratedAt = now,
                Contributors = activeOpps.Select(o => new EvidenceContributor
                {
                    EntityType = nameof(Opportunity),
                    EntityId = o.Id,
                    Name = o.Title,
                    ContributionValue = o.EstimatedValue * (decimal)o.Probability,
                    ContributionDetails = $"{o.EstimatedValue.ToString("C0", inr)} × {o.Probability * 100:F0}% ({o.Stage})"
                }).ToList()
            });

            result.EvidenceRecords.Add(new EvidenceRecord
            {
                EvidenceId = "EVD-RISK-01",
                EvidenceType = "Risk",
                MetricKey = "METRIC_STALLED_RISK_INDEX",
                DisplayName = "Stalled Deal Risk Index",
                NumericValue = stalledRiskIndex,
                FormattedValue = $"{stalledRiskIndex:F1}%",
                CalculationVersion = Version,
                Formula = "(StalledPipelineValue / TotalPipelineValue) * 100",
                Period = "Current",
                ConfidenceScore = Math.Round(confidence, 2),
                ImpactLevel = stalledRiskIndex > 25 ? "High" : "Low",
                GeneratedAt = now,
                Contributors = stalledOpps.Select(o => new EvidenceContributor
                {
                    EntityType = nameof(Opportunity),
                    EntityId = o.Id,
                    Name = o.Title,
                    ContributionValue = o.EstimatedValue,
                    ContributionDetails = $"No activity logged in > 14 days. Value: {o.EstimatedValue.ToString("C0", inr)}"
                }).ToList()
            });

            return result;
        }
    }
}
