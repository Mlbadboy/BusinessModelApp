using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.Prospecting;
using BusinessModelApp.Infrastructure.AI.OmniRoute;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Infrastructure.Prospecting
{
    public class ProspectDiscoveryService : IProspectDiscoveryService
    {
        private readonly ICompanyIntelligenceProvider _companyIntelligence;
        private readonly IICPScoringEngine _icpScoringEngine;
        private readonly IDecisionMakerDiscoveryProvider _decisionMakerDiscovery;
        private readonly IOmniRouteClient _aiClient;
        private readonly ILogger<ProspectDiscoveryService> _logger;

        public ProspectDiscoveryService(
            ICompanyIntelligenceProvider companyIntelligence,
            IICPScoringEngine icpScoringEngine,
            IDecisionMakerDiscoveryProvider decisionMakerDiscovery,
            IOmniRouteClient aiClient,
            ILogger<ProspectDiscoveryService> logger)
        {
            _companyIntelligence = companyIntelligence;
            _icpScoringEngine = icpScoringEngine;
            _decisionMakerDiscovery = decisionMakerDiscovery;
            _aiClient = aiClient;
            _logger = logger;
        }

        public Task<IReadOnlyList<ProspectSignal>> ScanMarketSignalsAsync(
            string industry, 
            CancellationToken ct = default)
        {
            return _companyIntelligence.ScanMarketSignalsAsync(industry, ct);
        }

        public async Task<IReadOnlyList<DiscoveredCandidateAccount>> DiscoverCandidateAccountsAsync(
            string industry, 
            string geography, 
            int minHeadcount, 
            CancellationToken ct = default)
        {
            var rawAccounts = await _companyIntelligence.DiscoverCandidateAccountsAsync(industry, geography, minHeadcount, ct);
            var scoredAccounts = new List<DiscoveredCandidateAccount>();

            foreach (var acc in rawAccounts)
            {
                var score = _icpScoringEngine.ScoreAccountICP(acc, industry, 2500000m, out _);
                acc.ICPScore = score;
                acc.EvidenceToken = $"EVD-ACC-{ComputeHash(acc.CompanyName + acc.Domain)}";
                scoredAccounts.Add(acc);
            }

            return scoredAccounts.OrderByDescending(a => a.ICPScore).ToList();
        }

        public Task<IReadOnlyList<CandidateDecisionMaker>> DiscoverDecisionMakersAsync(
            DiscoveredCandidateAccount account, 
            CancellationToken ct = default)
        {
            return _decisionMakerDiscovery.DiscoverDecisionMakersAsync(account, ct);
        }

        public Task<decimal> ScoreAccountICPAsync(
            DiscoveredCandidateAccount account, 
            string targetIndustry, 
            decimal targetDealValueINR, 
            CancellationToken ct = default)
        {
            var score = _icpScoringEngine.ScoreAccountICP(account, targetIndustry, targetDealValueINR, out _);
            account.ICPScore = score;
            return Task.FromResult(score);
        }

        public async Task<VerifiedProspectLead?> QualifyAndVerifyProspectAsync(
            DiscoveredCandidateAccount account, 
            CandidateDecisionMaker decisionMaker, 
            string targetIndustry, 
            decimal targetDealValueINR, 
            CancellationToken ct = default)
        {
            var icpScore = _icpScoringEngine.ScoreAccountICP(account, targetIndustry, targetDealValueINR, out var breakdown);
            account.ICPScore = icpScore;

            // Threshold gate: Must score at least 70 on ICP
            if (icpScore < 70m)
            {
                _logger.LogInformation("Candidate {Company} rejected: ICP score {Score} below 70 threshold.", account.CompanyName, icpScore);
                return null;
            }

            // AI Evidence Grounded Qualification
            double aiScore = (double)icpScore;
            string rationale = $"Account {account.CompanyName} verified with ICP score {icpScore:F1}/100 based on {account.Headcount} headcount, {account.Signals.Count} transformation signals, and executive buyer {decisionMaker.Name} ({decisionMaker.Title}).";

            try
            {
                var aiRequest = new AIRequest
                {
                    TaskType = AITaskType.LeadQualification,
                    Messages = new List<AIMessage>
                    {
                        AIMessage.System("You are Charlie's autonomous ICP qualification evaluator. Output JSON: {\"score\": number (0-100), \"tier\": string, \"rationale\": string}."),
                        AIMessage.User($"Company: {account.CompanyName}\nIndustry: {account.Industry}\nHeadcount: {account.Headcount}\nRevenue: INR {account.EstimatedAnnualRevenueINR}\nDecision Maker: {decisionMaker.Name}, {decisionMaker.Title}\nSignals: {string.Join("; ", account.Signals.Select(s => s.Headline))}")
                    }
                };

                var routingPolicy = new AIRoutingPolicy { TaskType = AITaskType.LeadQualification };
                var aiResponse = await _aiClient.SendChatCompletionAsync(aiRequest, routingPolicy, ct);
                if (!string.IsNullOrWhiteSpace(aiResponse?.Content))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(aiResponse.Content, @"(?i)""score""\s*:\s*([0-9]+(?:\.[0-9]+)?)");
                    if (match.Success && double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                    {
                        aiScore = Math.Clamp(parsed, 0.0, 100.0);
                        rationale = aiResponse.Content;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI gateway qualification fallback to deterministic ICP score for {Company}.", account.CompanyName);
            }

            var verifiedLead = new VerifiedProspectLead
            {
                ProvenanceToken = $"PRV-{ComputeHash(account.CompanyName + decisionMaker.Name + DateTime.UtcNow.Ticks)}",
                Account = account,
                DecisionMaker = decisionMaker,
                ICPScore = icpScore,
                AIQualificationScore = (decimal)aiScore,
                ConfidenceScore = 0.94m,
                QualificationRationale = rationale,
                VerifiedAt = DateTime.UtcNow
            };

            return verifiedLead;
        }

        private static string ComputeHash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToHexString(sha256.ComputeHash(bytes)).Substring(0, 12).ToUpper();
        }
    }
}
