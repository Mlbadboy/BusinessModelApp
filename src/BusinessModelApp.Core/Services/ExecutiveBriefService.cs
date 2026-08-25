using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Interfaces;

namespace BusinessModelApp.Core.Services
{
    public interface IExecutiveBriefService
    {
        Task<ExecutiveBriefContext> GenerateExecutiveBriefAsync(
            BusinessHealthResult health,
            CancellationToken ct = default);
    }

    public class ExecutiveBriefService : IExecutiveBriefService
    {
        private readonly IAIService _aiService;

        public ExecutiveBriefService(IAIService aiService)
        {
            _aiService = aiService;
        }

        public async Task<ExecutiveBriefContext> GenerateExecutiveBriefAsync(
            BusinessHealthResult health,
            CancellationToken ct = default)
        {
            var brief = new ExecutiveBriefContext
            {
                Health = health,
                IsActionRequired = false
            };

            // 1. Deterministic Rule Evaluation & Evidence Signals
            if (health.PipelineCoverageRatio < 1.0)
            {
                brief.CriticalRiskAlerts.Add($"Pipeline coverage is {health.PipelineCoverageRatio:F2}x, below the minimum 1.0x quarterly target threshold.");
                brief.IsActionRequired = true;
                brief.Recommendations.Add(new ExecutiveRecommendation
                {
                    Title = "Accelerate Inbound Lead Qualification",
                    Rationale = $"Weighted forecast of {health.WeightedForecastValue:C0} is insufficient to cover the target of {health.QuarterlyTarget:C0}.",
                    CitedEvidenceId = "EVD-PIPE-01",
                    ActionType = "Campaign",
                    TargetEntity = "Leads Pool"
                });
            }
            else
            {
                brief.PositiveMomentumSignals.Add($"Healthy pipeline coverage at {health.PipelineCoverageRatio:F2}x quarterly target.");
            }

            if (health.StalledRiskIndex > 25.0)
            {
                brief.CriticalRiskAlerts.Add($"{health.StalledRiskIndex:F1}% of active pipeline value is stalled with no logged activity in over 14 days.");
                brief.IsActionRequired = true;
                brief.Recommendations.Add(new ExecutiveRecommendation
                {
                    Title = "Re-engage Stalled Opportunities",
                    Rationale = "Deals stalled over 14 days have a 45% lower historical conversion probability.",
                    CitedEvidenceId = "EVD-RISK-01",
                    ActionType = "Review",
                    TargetEntity = "Stalled Deals"
                });
            }

            if (health.ConfidenceScore < 0.40)
            {
                brief.PositiveMomentumSignals.Add("Early workspace baseline initialized; confidence will increase as commercial interactions are logged.");
            }

            // 2. Determine "No Action Required" State
            if (!brief.IsActionRequired)
            {
                brief.Summary = $"Good morning. Business operating health is stable at {health.OverallHealthScore:F0}/100 with {health.ConfidenceLevel.ToLower()} statistical confidence. Pipeline momentum and activity velocity are tracking within targets. No intervention required today.";
                return brief;
            }

            // 3. Synthesize Executive Summary with AI over Structured Facts
            var prompt = new StringBuilder();
            prompt.AppendLine("You are the Executive Operating AI for Bitbloom Enterprise. Generate a concise, 2-3 sentence executive morning brief based STRICTLY on the following verified facts.");
            prompt.AppendLine("DO NOT invent or alter any financial figures. Cite the metrics directly.");
            prompt.AppendLine($"- Health Score: {health.OverallHealthScore:F0}/100 (Confidence: {health.ConfidenceLevel})");
            prompt.AppendLine($"- Weighted Forecast: {health.WeightedForecastValue:N0} INR (Coverage: {health.PipelineCoverageRatio:F2}x target)");
            prompt.AppendLine($"- Stalled Pipeline Risk: {health.StalledRiskIndex:F1}%");
            prompt.AppendLine($"- Key Risks: {string.Join("; ", brief.CriticalRiskAlerts)}");
            prompt.AppendLine($"- Key Momentum: {string.Join("; ", brief.PositiveMomentumSignals)}");

            try
            {
                var aiResponse = await _aiService.GetCompletionAsync(prompt.ToString());
                if (!string.IsNullOrWhiteSpace(aiResponse))
                {
                    brief.Summary = aiResponse.Trim();
                }
                else
                {
                    brief.Summary = $"Business health stands at {health.OverallHealthScore:F0}/100. Pipeline coverage is {health.PipelineCoverageRatio:F2}x. {brief.CriticalRiskAlerts.FirstOrDefault()}";
                }
            }
            catch
            {
                // Fallback deterministic summary
                brief.Summary = $"Good morning. Business health is {health.OverallHealthScore:F0}/100 ({health.ConfidenceLevel} confidence). {string.Join(" ", brief.CriticalRiskAlerts)}";
            }

            return brief;
        }
    }
}
