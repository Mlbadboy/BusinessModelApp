using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Agents;
using BusinessModelApp.Core.Constitution;
using BusinessModelApp.Core.Decisions;
using BusinessModelApp.Core.Domain.Decisions;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Strategy;
using BusinessModelApp.Core.Domain.WorldModel;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Infrastructure.Decisions
{
    public class DecisionEngine : IDecisionEngine
    {
        private readonly AppDbContext _dbContext;
        private readonly ICompanyConstitutionService _constitutionService;
        private readonly ILogger<DecisionEngine> _logger;

        public DecisionEngine(
            AppDbContext dbContext,
            ICompanyConstitutionService constitutionService,
            ILogger<DecisionEngine> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _constitutionService = constitutionService ?? throw new ArgumentNullException(nameof(constitutionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DecisionRecord> CommitStrategyDecisionAsync(
            BusinessObjective objective,
            BusinessStrategy selectedStrategy,
            IReadOnlyList<BusinessStrategy> competingAlternatives,
            CompanySnapshot snapshot,
            Guid? supersedesDecisionId = null,
            CancellationToken ct = default)
        {
            _logger.LogInformation("[DecisionEngine] Evaluating and committing decision for Objective {ObjectiveId}, Strategy {StrategyName}",
                objective.Id, selectedStrategy.StrategyName);

            // 1. Evaluate Strategy Against Constitution Policy
            var constitutionResult = await _constitutionService.EvaluateStrategyAsync(selectedStrategy, snapshot, ct);

            // 2. Determine Risk & Human Approval Requirements
            bool requiresApproval = !constitutionResult.IsCompliant ||
                                    selectedStrategy.FeasibilityState != StrategyFeasibilityState.Feasible ||
                                    selectedStrategy.DeliveryCapacitySlotsRequired >= 3;

            var approvalStatus = requiresApproval
                ? DecisionApprovalStatus.Pending
                : DecisionApprovalStatus.NotRequired;

            var alternativesList = competingAlternatives
                .Where(s => s.Id != selectedStrategy.Id)
                .Select(s => $"{s.StrategyName} (ACV: ₹{s.TargetACV_INR:N0}, WinRate: {s.ExpectedWinRate:P0}, Status: {s.FeasibilityState})")
                .ToList();

            var evidenceHashes = new List<string> { snapshot.SnapshotDigestHash };
            if (!string.IsNullOrWhiteSpace(selectedStrategy.EvidenceBundleHashesJson))
            {
                try
                {
                    var extra = JsonSerializer.Deserialize<List<string>>(selectedStrategy.EvidenceBundleHashesJson);
                    if (extra != null) evidenceHashes.AddRange(extra);
                }
                catch { }
            }

            // Calculate Delivery Feasibility Score (0.0 to 1.0)
            int availableSlots = snapshot.AvailableDeliverySlots.Value;
            double feasibilityScore = availableSlots > 0
                ? Math.Clamp((double)(availableSlots - selectedStrategy.DeliveryCapacitySlotsRequired + 1) / availableSlots, 0.0, 1.0)
                : 0.0;

            string rationale = selectedStrategy.FeasibilityState == StrategyFeasibilityState.EvidenceInsufficient
                ? "DECISION BLOCKED: Insufficient external market evidence to substantiate strategy claims."
                : $"Selected '{selectedStrategy.StrategyName}' because it achieves target revenue (₹{objective.TargetRevenueINR:N2}) with {selectedStrategy.ExpectedGrossMarginPercent:F0}% expected gross margin and consumes {selectedStrategy.DeliveryCapacitySlotsRequired}/{availableSlots} available delivery slots.";

            var decision = new DecisionRecord
            {
                WorkspaceId = objective.WorkspaceId,
                ObjectiveId = objective.Id,
                StrategyId = selectedStrategy.Id,
                SupersedesDecisionId = supersedesDecisionId,
                Role = AgentRole.ExecutiveOrchestrator,
                Type = DecisionType.SelectStrategy,
                SelectedAlternative = selectedStrategy.StrategyName,
                AlternativesConsideredJson = JsonSerializer.Serialize(alternativesList),
                ExpectedRevenueImpactINR = objective.TargetRevenueINR,
                ExpectedCostINR = objective.TargetRevenueINR * (1m - (selectedStrategy.ExpectedGrossMarginPercent / 100m)),
                ExpectedMarginPercent = selectedStrategy.ExpectedGrossMarginPercent,
                WinProbability = selectedStrategy.ExpectedWinRate,
                DeliveryFeasibilityScore = feasibilityScore,
                RiskScore = requiresApproval ? 0.45 : 0.20,
                ExecutedAutonomyLevel = objective.AllowedAutonomyLevel,
                HumanApprovalRequired = requiresApproval,
                ApprovalStatus = approvalStatus,
                DecisionRationale = rationale,
                GroundingEvidenceHashesJson = JsonSerializer.Serialize(evidenceHashes),
                ConstitutionRulesEvaluatedJson = JsonSerializer.Serialize(constitutionResult.PassedRules.Select(r => r.ToString())),
                DecidedAt = DateTime.UtcNow
            };

            await _dbContext.DecisionRecords.AddAsync(decision, ct);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("[DecisionEngine] Committed Decision {DecisionId}. ApprovalRequired={Approval}, Feasibility={Feasibility:F2}",
                decision.Id, requiresApproval, feasibilityScore);

            return decision;
        }

        public async Task<WhyCharlieExplanation?> GetWhyCharlieExplanationAsync(Guid decisionId, CancellationToken ct = default)
        {
            var decision = await _dbContext.DecisionRecords.FirstOrDefaultAsync(d => d.Id == decisionId, ct);
            if (decision == null) return null;

            var objective = await _dbContext.BusinessObjectives.FirstOrDefaultAsync(o => o.Id == decision.ObjectiveId, ct);
            var strategy = decision.StrategyId.HasValue
                ? await _dbContext.BusinessStrategies.FirstOrDefaultAsync(s => s.Id == decision.StrategyId.Value, ct)
                : null;

            var alternatives = new List<string>();
            if (!string.IsNullOrWhiteSpace(decision.AlternativesConsideredJson))
            {
                try { alternatives = JsonSerializer.Deserialize<List<string>>(decision.AlternativesConsideredJson) ?? new(); } catch { }
            }

            var hashes = new List<string>();
            if (!string.IsNullOrWhiteSpace(decision.GroundingEvidenceHashesJson))
            {
                try { hashes = JsonSerializer.Deserialize<List<string>>(decision.GroundingEvidenceHashesJson) ?? new(); } catch { }
            }

            var rules = new List<string>();
            if (!string.IsNullOrWhiteSpace(decision.ConstitutionRulesEvaluatedJson))
            {
                try { rules = JsonSerializer.Deserialize<List<string>>(decision.ConstitutionRulesEvaluatedJson) ?? new(); } catch { }
            }

            return new WhyCharlieExplanation
            {
                DecisionId = decision.Id,
                ObjectiveTitle = objective?.Title ?? "Strategic Mandate",
                RevenueGapINR = objective?.ObservedProgress?.RevenueGapINR ?? decision.ExpectedRevenueImpactINR,
                SelectedStrategyName = strategy?.StrategyName ?? decision.SelectedAlternative,
                SelectedAlternative = decision.SelectedAlternative,
                AlternativesConsidered = alternatives,
                ExpectedRevenueINR = decision.ExpectedRevenueImpactINR,
                ExpectedMarginPercent = decision.ExpectedMarginPercent,
                WinProbability = decision.WinProbability,
                DeliveryFeasibilityScore = decision.DeliveryFeasibilityScore,
                DecisionRationale = decision.DecisionRationale,
                GroundingEvidenceHashes = hashes,
                ConstitutionRulesEvaluated = rules,
                DecidedAt = decision.DecidedAt
            };
        }
    }
}
