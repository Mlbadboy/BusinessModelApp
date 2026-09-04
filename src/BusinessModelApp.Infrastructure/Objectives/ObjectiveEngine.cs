using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Reality;
using BusinessModelApp.Core.Domain.WorldModel;
using BusinessModelApp.Core.Objectives;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Infrastructure.Objectives
{
    public class ObjectiveEngine : IObjectiveEngine
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<ObjectiveEngine> _logger;

        public ObjectiveEngine(AppDbContext dbContext, ILogger<ObjectiveEngine> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<BusinessObjective> IngestCeoPromptAsync(string prompt, Guid workspaceId, CancellationToken ct = default)
        {
            _logger.LogInformation("[ObjectiveEngine] Ingesting CEO prompt for Workspace {WorkspaceId}: '{Prompt}'",
                workspaceId, prompt);

            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Objective prompt cannot be empty.", nameof(prompt));
            }

            // Extract Revenue Target (Handles ₹50L, 50 Lakh, 50,00,000, 1 Crore, etc.)
            decimal targetRevenue = ExtractRevenueFromPrompt(prompt);
            int timeframeDays = ExtractDaysFromPrompt(prompt);

            var markets = ExtractMarketsFromPrompt(prompt);
            var offers = ExtractOffersFromPrompt(prompt);

            var objective = new BusinessObjective
            {
                WorkspaceId = workspaceId,
                Title = $"Revenue Mandate: ₹{targetRevenue / 100000m:F0}L in {timeframeDays} Days",
                Description = prompt.Trim(),
                TargetRevenueINR = targetRevenue,
                TargetMarginPercent = 55.0m,
                TargetCashCollectedINR = targetRevenue * 0.80m, // 80% Cash target by default
                StartDate = DateTime.UtcNow,
                TargetDeadline = DateTime.UtcNow.AddDays(timeframeDays),
                TargetMarketsJson = JsonSerializer.Serialize(markets),
                OfferPortfolioJson = JsonSerializer.Serialize(offers),
                MaximumBudgetINR = Math.Min(150000m, targetRevenue * 0.05m),
                MaximumRiskScore = 0.35,
                AllowedAutonomyLevel = Core.Agents.AutonomyLevel.Level3_ControlledAutonomy,
                Status = ObjectiveStatus.Active,
                DesiredState = DesiredCommercialState.ProposalDispatched
            };

            objective.ObservedProgress = new ObservedObjectiveProgress
            {
                BaselineState = RevenueBaselineState.Unavailable,
                BaselineExplanation = "Revenue baseline unavailable. Awaiting verified payment settlement records.",
                RevenueGapINR = targetRevenue,
                CashGapINR = objective.TargetCashCollectedINR,
                LastObservedAt = DateTime.UtcNow,
                OverallStatus = VerificationStatus.Unverified
            };

            await _dbContext.BusinessObjectives.AddAsync(objective, ct);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("[ObjectiveEngine] Objective {ObjectiveId} created. Target={Target:N2}, Days={Days}",
                objective.Id, targetRevenue, timeframeDays);

            return objective;
        }

        public async Task<BusinessObjective> ReconcileObjectiveProgressAsync(
            Guid objectiveId,
            CompanySnapshot snapshot,
            CancellationToken ct = default)
        {
            var objective = await _dbContext.BusinessObjectives.FirstOrDefaultAsync(o => o.Id == objectiveId, ct);
            if (objective == null)
            {
                throw new KeyNotFoundException($"BusinessObjective {objectiveId} not found.");
            }

            decimal verifiedRev = snapshot.VerifiedRevenueINR.Value;
            decimal verifiedCash = snapshot.VerifiedSettledCashINR.Value;
            decimal verifiedPipe = snapshot.ActivePipelineINR.Value;
            int verifiedProspects = snapshot.VerifiedProspectsCount.Value;

            objective.UpdateProgress(
                verifiedRevenue: verifiedRev,
                verifiedCash: verifiedCash,
                verifiedPipeline: verifiedPipe,
                verifiedProspects: verifiedProspects,
                baselineState: snapshot.RevenueBaselineState,
                evidenceHash: snapshot.SnapshotDigestHash);

            objective.ObservedProgress.BaselineExplanation = snapshot.VerifiedRevenueINR.Note;

            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("[ObjectiveEngine] Objective {ObjectiveId} reconciled. Baseline={Baseline}, RevenueGap={Gap:N2}",
                objectiveId, snapshot.RevenueBaselineState, objective.ObservedProgress.RevenueGapINR);

            return objective;
        }

        private static decimal ExtractRevenueFromPrompt(string prompt)
        {
            string clean = prompt.ToLowerInvariant();

            // Match "50l", "50 lakh", "50 lakhs"
            var lakhMatch = Regex.Match(clean, @"(?:₹|inr|rs\.?)?\s*(\d+(?:\.\d+)?)\s*(?:lakh|lakhs|l\b)");
            if (lakhMatch.Success && decimal.TryParse(lakhMatch.Groups[1].Value, out decimal lVal))
            {
                return lVal * 100000m;
            }

            // Match "1 crore", "1 cr", "1cr"
            var croreMatch = Regex.Match(clean, @"(?:₹|inr|rs\.?)?\s*(\d+(?:\.\d+)?)\s*(?:crore|crores|cr\b)");
            if (croreMatch.Success && decimal.TryParse(croreMatch.Groups[1].Value, out decimal crVal))
            {
                return crVal * 10000000m;
            }

            // Match explicit number e.g. 5000000
            var numMatch = Regex.Match(clean, @"(?:₹|inr|rs\.?)?\s*(\d{5,10})");
            if (numMatch.Success && decimal.TryParse(numMatch.Groups[1].Value, out decimal numVal))
            {
                return numVal;
            }

            return 5000000m; // Default ₹50L
        }

        private static int ExtractDaysFromPrompt(string prompt)
        {
            var match = Regex.Match(prompt.ToLowerInvariant(), @"(\d+)\s*(?:days|day)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int days))
            {
                return Math.Clamp(days, 7, 365);
            }
            return 60; // Default 60 days
        }

        private static List<string> ExtractMarketsFromPrompt(string prompt)
        {
            string lower = prompt.ToLowerInvariant();
            var markets = new List<string>();
            if (lower.Contains("bfsi") || lower.Contains("bank") || lower.Contains("financial")) markets.Add("BFSI & Fintech");
            if (lower.Contains("retail") || lower.Contains("ecommerce")) markets.Add("Retail & E-commerce");
            if (lower.Contains("health") || lower.Contains("pharma")) markets.Add("Healthcare & MedTech");
            if (lower.Contains("antarctica") || lower.Contains("quantum")) markets.Add("Antarctica Quantum Tech");

            if (!markets.Any())
            {
                markets.Add("Enterprise B2B Technology & Software");
            }
            return markets;
        }

        private static List<string> ExtractOffersFromPrompt(string prompt)
        {
            string lower = prompt.ToLowerInvariant();
            var offers = new List<string>();
            if (lower.Contains("ai") || lower.Contains("automation")) offers.Add("Enterprise AI Transformation");
            if (lower.Contains("website") || lower.Contains("web") || lower.Contains("mobile") || lower.Contains("software") || lower.Contains("full-stack"))
            {
                offers.Add("Full-Stack Software Engineering");
            }
            if (!offers.Any())
            {
                offers.Add("Autonomous Agent & AI Automation Suites");
            }
            return offers;
        }
    }
}
