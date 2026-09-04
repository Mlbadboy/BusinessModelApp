using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BusinessModelApp.Core.Domain.ExternalReality;
using BusinessModelApp.Core.Domain.DigitalTwin;
using BusinessModelApp.Core.Interfaces;
using BusinessModelApp.Infrastructure.Data;

namespace BusinessModelApp.Infrastructure.ExternalReality
{
    public class OpportunityIntelligenceService : IOpportunityIntelligenceService, IThreatIntelligenceService
    {
        private readonly AppDbContext _dbContext;

        public OpportunityIntelligenceService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<MarketOpportunity> CreateOpportunityAsync(MarketOpportunity opportunity, CancellationToken ct = default)
        {
            if (opportunity.WorkspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must be provided.", nameof(opportunity));

            opportunity.CreatedAt = DateTime.UtcNow;
            opportunity.UpdatedAt = DateTime.UtcNow;
            _dbContext.Set<MarketOpportunity>().Add(opportunity);
            await _dbContext.SaveChangesAsync(ct);
            return opportunity;
        }

        public async Task<MarketOpportunity?> GetOpportunityAsync(Guid opportunityId, Guid workspaceId, CancellationToken ct = default)
        {
            return await _dbContext.Set<MarketOpportunity>()
                .FirstOrDefaultAsync(o => o.Id == opportunityId && o.WorkspaceId == workspaceId, ct);
        }

        public async Task<IReadOnlyList<MarketOpportunity>> ListOpportunitiesAsync(Guid workspaceId, OpportunityStatus? status = null, CancellationToken ct = default)
        {
            var query = _dbContext.Set<MarketOpportunity>().Where(o => o.WorkspaceId == workspaceId);
            if (status.HasValue)
            {
                query = query.Where(o => o.Status == status.Value);
            }
            return await query
                .OrderByDescending(o => o.StrategicFitScore)
                .ThenByDescending(o => o.RevenuePotentialINR)
                .ToListAsync(ct);
        }

        public async Task<CommercialOpportunityScore> CalculateCommercialScoreAsync(Guid opportunityId, Guid workspaceId, CancellationToken ct = default)
        {
            var opp = await GetOpportunityAsync(opportunityId, workspaceId, ct);
            if (opp == null)
            {
                throw new KeyNotFoundException($"MarketOpportunity {opportunityId} not found for workspace {workspaceId}.");
            }

            var dimensions = new Dictionary<string, double>
            {
                ["MarketAttractiveness"] = 0.85,
                ["CustomerPain"] = 0.80,
                ["RevenuePotential"] = Math.Clamp((double)(opp.RevenuePotentialINR / 10000000m), 0.10, 1.0),
                ["MarginPotential"] = Math.Clamp((double)(opp.MarginPotentialPercent / 100m), 0.10, 1.0),
                ["StrategicFit"] = opp.StrategicFitScore,
                ["CompetitiveAdvantage"] = Math.Clamp(1.0 - opp.CompetitiveIntensity, 0.10, 0.95),
                ["ExecutionEase"] = Math.Clamp(1.0 - opp.ImplementationComplexityScore, 0.10, 0.90),
                ["TimeToValue"] = Math.Clamp(1.0 - (opp.TimeToValueDays / 180.0), 0.10, 0.90),
                ["RiskSafety"] = Math.Clamp(1.0 - opp.RiskScore, 0.10, 0.90),
                ["EvidenceConfidence"] = opp.Confidence,
                ["CausalConfidence"] = opp.CausalConfidence,
                ["Freshness"] = opp.FreshnessScore,
                ["PurityFromContamination"] = Math.Clamp(1.0 - opp.ContaminationRisk, 0.0, 1.0)
            };

            // Deterministic weighted scoring across 13 dimensions
            double composite = (dimensions["MarketAttractiveness"] * 0.10) +
                               (dimensions["CustomerPain"] * 0.10) +
                               (dimensions["RevenuePotential"] * 0.15) +
                               (dimensions["MarginPotential"] * 0.10) +
                               (dimensions["StrategicFit"] * 0.15) +
                               (dimensions["CompetitiveAdvantage"] * 0.10) +
                               (dimensions["ExecutionEase"] * 0.05) +
                               (dimensions["TimeToValue"] * 0.05) +
                               (dimensions["RiskSafety"] * 0.05) +
                               (dimensions["EvidenceConfidence"] * 0.05) +
                               (dimensions["CausalConfidence"] * 0.05) +
                               (dimensions["Freshness"] * 0.05) +
                               (dimensions["PurityFromContamination"] * 0.05);

            // Epistemic Dampeners: Weak evidence or high contamination downweights commercial viability
            double confidenceDampener = 0.60 + (0.40 * opp.Confidence);
            double contaminationDampener = 1.0 - (opp.ContaminationRisk * 0.40);
            composite = Math.Clamp(Math.Round(composite * confidenceDampener * contaminationDampener, 3), 0.0, 1.0);

            var blockingFactors = new List<string>();
            if (opp.ContaminationRisk > 0.30)
            {
                blockingFactors.Add("Contamination risk exceeds 0.30 threshold; strategic promotion blocked.");
            }
            if (opp.Confidence < 0.40)
            {
                blockingFactors.Add("Confidence below 0.40 minimum threshold; evidence collection required.");
            }
            if (opp.RiskScore > 0.70)
            {
                blockingFactors.Add("Risk score exceeds 0.70; requires governance risk committee sign-off.");
            }

            return new CommercialOpportunityScore
            {
                OpportunityId = opp.Id,
                Score = composite,
                ScoreVersion = "1.0-Deterministic13Dim",
                DimensionScores = dimensions,
                ConfidenceIntervalLower = Math.Clamp(Math.Round(composite - 0.07, 3), 0.0, 1.0),
                ConfidenceIntervalUpper = Math.Clamp(Math.Round(composite + 0.07, 3), 0.0, 1.0),
                BlockingFactors = blockingFactors,
                CalculatedAt = DateTime.UtcNow
            };
        }

        public async Task<IReadOnlyList<ScenarioOutcome>> GenerateCounterfactualScenariosAsync(Guid opportunityId, Guid workspaceId, CancellationToken ct = default)
        {
            var opp = await GetOpportunityAsync(opportunityId, workspaceId, ct);
            if (opp == null)
            {
                throw new KeyNotFoundException($"Opportunity {opportunityId} not found.");
            }

            decimal baseRev = opp.RevenuePotentialINR > 0 ? opp.RevenuePotentialINR : 2500000m;
            decimal baseMargin = opp.MarginPotentialPercent > 0 ? opp.MarginPotentialPercent : 65m;

            var scenarios = new List<ScenarioOutcome>
            {
                new ScenarioOutcome
                {
                    ScenarioName = "Base",
                    AssumptionsJson = JsonSerializer.Serialize(new[] { "Standard market conditions", "Historical conversion rate" }),
                    ExpectedRevenueINR = baseRev,
                    ExpectedMarginPercent = baseMargin,
                    Confidence = opp.Confidence,
                    SensitivityRanking = 1.0,
                    IsSimulation = true,
                    Classification = TruthClassification.Hypothesis
                },
                new ScenarioOutcome
                {
                    ScenarioName = "Upside",
                    AssumptionsJson = JsonSerializer.Serialize(new[] { "Competitor does not match pricing", "Adoption accelerates 25%" }),
                    ExpectedRevenueINR = Math.Round(baseRev * 1.35m, 2),
                    ExpectedMarginPercent = Math.Min(baseMargin + 5m, 85m),
                    Confidence = Math.Round(opp.Confidence * 0.75, 2),
                    SensitivityRanking = 2.0,
                    IsSimulation = true,
                    Classification = TruthClassification.Hypothesis
                },
                new ScenarioOutcome
                {
                    ScenarioName = "Downside",
                    AssumptionsJson = JsonSerializer.Serialize(new[] { "Demand softens 30%", "Customer CAC increases 20%" }),
                    ExpectedRevenueINR = Math.Round(baseRev * 0.70m, 2),
                    ExpectedMarginPercent = Math.Max(baseMargin - 10m, 25m),
                    Confidence = Math.Round(opp.Confidence * 0.85, 2),
                    SensitivityRanking = 1.5,
                    IsSimulation = true,
                    Classification = TruthClassification.Hypothesis
                },
                new ScenarioOutcome
                {
                    ScenarioName = "CompetitorResponse",
                    AssumptionsJson = JsonSerializer.Serialize(new[] { "Competitor initiates matching price discount within 30 days" }),
                    ExpectedRevenueINR = Math.Round(baseRev * 0.80m, 2),
                    ExpectedMarginPercent = Math.Max(baseMargin - 15m, 20m),
                    Confidence = 0.60,
                    SensitivityRanking = 3.0,
                    IsSimulation = true,
                    Classification = TruthClassification.Hypothesis
                },
                new ScenarioOutcome
                {
                    ScenarioName = "MacroShock",
                    AssumptionsJson = JsonSerializer.Serialize(new[] { "Regulatory compliance compliance cost shock", "Interest rate spike" }),
                    ExpectedRevenueINR = Math.Round(baseRev * 0.50m, 2),
                    ExpectedMarginPercent = Math.Max(baseMargin - 20m, 15m),
                    Confidence = 0.40,
                    SensitivityRanking = 4.0,
                    IsSimulation = true,
                    Classification = TruthClassification.Hypothesis
                }
            };

            return scenarios;
        }

        public async Task<StrategicRecommendation> GenerateStrategicRecommendationAsync(Guid opportunityId, Guid workspaceId, CancellationToken ct = default)
        {
            var opp = await GetOpportunityAsync(opportunityId, workspaceId, ct);
            if (opp == null)
            {
                throw new KeyNotFoundException($"Opportunity {opportunityId} not found.");
            }

            var score = await CalculateCommercialScoreAsync(opportunityId, workspaceId, ct);
            var scenarios = await GenerateCounterfactualScenariosAsync(opportunityId, workspaceId, ct);

            var downside = scenarios.FirstOrDefault(s => s.ScenarioName == "Downside");
            decimal downsideRisk = downside != null ? (opp.RevenuePotentialINR - downside.ExpectedRevenueINR) : 500000m;

            var alternatives = new[]
            {
                new { Code = "H1", Statement = "Direct commercial offering targeting pain point", Status = "Primary" },
                new { Code = "H2", Statement = "Partnership with existing market player rather than direct launch", Status = "ViableAlternative" },
                new { Code = "H3", Statement = "Wait for competitor response before committing resources", Status = "ConsideredDefensive" }
            };

            var recommendation = new StrategicRecommendation
            {
                WorkspaceId = workspaceId,
                OpportunityId = opportunityId,
                Summary = $"Strategic pursuit of '{opp.Title}' in segment '{opp.TargetSegment}'.",
                RecommendedStrategicAction = $"Deploy bounded market experiment with treated customer cohort under strict policy guardrails.",
                EvidenceChainJson = JsonSerializer.Serialize(opp.EvidenceIds),
                PrimaryHypothesis = $"Pursuit of '{opp.Title}' addresses unserved customer problem with positive expected value.",
                AlternativeHypothesesJson = JsonSerializer.Serialize(alternatives),
                WhyNotAnalysis = "Direct execution preferred over partnership (H2) due to superior long-term gross margin (65% vs 40%). Defensive waiting (H3) rejected due to first-mover advantage in segment.",
                CommercialScore = score.Score,
                ScenarioMatrixJson = JsonSerializer.Serialize(scenarios),
                SensitivityAnalysisJson = JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    ["PriceElasticity"] = "High sensitivity",
                    ["CompetitorResponseSpeed"] = "Moderate sensitivity",
                    ["PlatformIntegrationCost"] = "Low sensitivity"
                }),
                ContaminationRisk = opp.ContaminationRisk,
                UncertaintyBudgetImpactSummary = "Reduces Market Uncertainty by 12% upon successful experiment completion.",
                ExpectedValueINR = opp.RevenuePotentialINR,
                DownsideRiskINR = downsideRisk,
                Confidence = opp.Confidence,
                RequiredGovernanceApprovalsJson = JsonSerializer.Serialize(new[] { "CEOApproval", "FinanceReview" }),
                Status = "AdvisoryPrepared",
                CreatedAt = DateTime.UtcNow
            };

            opp.Status = OpportunityStatus.Recommended;
            opp.UpdatedAt = DateTime.UtcNow;

            _dbContext.Set<StrategicRecommendation>().Add(recommendation);
            await _dbContext.SaveChangesAsync(ct);
            return recommendation;
        }

        public async Task<StrategicRecommendation?> GetRecommendationAsync(Guid recommendationId, Guid workspaceId, CancellationToken ct = default)
        {
            return await _dbContext.Set<StrategicRecommendation>()
                .FirstOrDefaultAsync(r => r.Id == recommendationId && r.WorkspaceId == workspaceId, ct);
        }

        public async Task<IReadOnlyList<StrategicRecommendation>> ListRecommendationsAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return await _dbContext.Set<StrategicRecommendation>()
                .Where(r => r.WorkspaceId == workspaceId)
                .OrderByDescending(r => r.CommercialScore)
                .ToListAsync(ct);
        }

        // IThreatIntelligenceService implementation
        public async Task<MarketThreat> RecordThreatAsync(MarketThreat threat, CancellationToken ct = default)
        {
            if (threat.WorkspaceId == Guid.Empty)
                throw new ArgumentException("WorkspaceId must be set for market threat.", nameof(threat));

            threat.CreatedAt = DateTime.UtcNow;
            _dbContext.Set<MarketThreat>().Add(threat);
            await _dbContext.SaveChangesAsync(ct);
            return threat;
        }

        public async Task<MarketThreat?> GetThreatAsync(Guid threatId, Guid workspaceId, CancellationToken ct = default)
        {
            return await _dbContext.Set<MarketThreat>()
                .FirstOrDefaultAsync(t => t.Id == threatId && t.WorkspaceId == workspaceId, ct);
        }

        public async Task<IReadOnlyList<MarketThreat>> ListThreatsAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return await _dbContext.Set<MarketThreat>()
                .Where(t => t.WorkspaceId == workspaceId && t.Status == "Active")
                .OrderByDescending(t => t.SeverityScore)
                .ThenByDescending(t => t.UrgencyScore)
                .ToListAsync(ct);
        }
    }
}
