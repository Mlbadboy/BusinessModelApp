using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Memory
{
    public class OrganizationalContextAssembler : IOrganizationalContextAssembler
    {
        private readonly IOrganizationalMemoryStore _store;
        private readonly ContextAssemblyPolicy _defaultPolicy;

        public OrganizationalContextAssembler(
            IOrganizationalMemoryStore store,
            ContextAssemblyPolicy? defaultPolicy = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _defaultPolicy = defaultPolicy ?? new ContextAssemblyPolicy();
        }

        public async Task<OrganizationalContextSnapshot> AssembleContextAsync(
            string tenantId,
            string workId,
            ContextAssemblyPolicy? policyOverride = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(workId)) throw new ArgumentException("WorkId is required.", nameof(workId));

            var policy = policyOverride ?? _defaultPolicy;
            var candidates = new List<ContextSnapshotItem>();
            var excludedItems = new List<string>();
            var exclusionReasons = new Dictionary<string, string>();

            // -------------------------------------------------------------
            // STAGE 1: Candidate Retrieval & Deterministic Eligibility Gate
            // -------------------------------------------------------------

            // 1. Precedents
            var precedents = await _store.ListPrecedentsAsync(tenantId, ct);
            foreach (var prec in precedents)
            {
                // Invariant I28-I: Unknown or ungrounded memories cannot be context-eligible as facts
                if (prec.EpistemicClassification == EpistemicStatus.Unknown)
                {
                    excludedItems.Add(prec.PrecedentId);
                    exclusionReasons[prec.PrecedentId] = "Ineligible: Epistemic status is Unknown.";
                    continue;
                }

                // Freshness check: Expired or Conflicted cannot be admitted
                if (prec.Freshness == FreshnessStatus.Expired || prec.Freshness == FreshnessStatus.Conflicted)
                {
                    excludedItems.Add(prec.PrecedentId);
                    exclusionReasons[prec.PrecedentId] = $"Ineligible: Freshness status is {prec.Freshness}.";
                    continue;
                }

                // Applicability check: Below minimum policy threshold
                if (prec.Applicability < policy.MinApplicability)
                {
                    excludedItems.Add(prec.PrecedentId);
                    exclusionReasons[prec.PrecedentId] = $"Ineligible: Applicability ({prec.Applicability}) is below threshold ({policy.MinApplicability}).";
                    continue;
                }

                // Deterministic scoring formula:
                // ContextScore = EvidenceQuality * Freshness * Applicability * Relevance * HistoricalReliability
                double evidenceQuality = prec.EvidenceReferences.Count > 0 ? 0.95 : 0.50;
                double freshnessWeight = prec.Freshness switch
                {
                    FreshnessStatus.Fresh => 1.0,
                    FreshnessStatus.Aging => 0.75,
                    FreshnessStatus.Stale => 0.50,
                    _ => 0.25
                };
                double applicabilityWeight = prec.Applicability switch
                {
                    CurrentApplicability.High => 1.0,
                    CurrentApplicability.Medium => 0.75,
                    CurrentApplicability.Low => 0.40,
                    _ => 0.10
                };
                double epistemicWeight = prec.EpistemicClassification switch
                {
                    EpistemicStatus.VerifiedTruth => 1.0,
                    EpistemicStatus.ObservedFact => 0.9,
                    EpistemicStatus.Inference => 0.6,
                    EpistemicStatus.Hypothesis => 0.4,
                    _ => 0.1
                };

                double score = Math.Round(evidenceQuality * freshnessWeight * applicabilityWeight * epistemicWeight, 4);

                candidates.Add(new ContextSnapshotItem
                {
                    ItemId = prec.PrecedentId,
                    ItemType = "Precedent",
                    Content = $"[PRECEDENT {prec.PrecedentId}] {prec.Title} (Actual Variance: {prec.Variance:+0.00;-0.00}). Conditions: {prec.ObservedConditions}. Actual: {prec.ActualOutcome}",
                    Score = score,
                    EstimatedTokens = 120,
                    EpistemicStatus = prec.EpistemicClassification,
                    Freshness = prec.Freshness,
                    Applicability = prec.Applicability
                });
            }

            // 2. Anti-Patterns (Invariant I28-F: Warnings, not hard prohibitions)
            var antiPatterns = await _store.ListAntiPatternsAsync(tenantId, null, ct);
            foreach (var ap in antiPatterns)
            {
                candidates.Add(new ContextSnapshotItem
                {
                    ItemId = ap.AntiPatternId,
                    ItemType = "AntiPattern",
                    Content = $"[WARNING: ANTI-PATTERN {ap.AntiPatternId}] {ap.Title}: {ap.FailurePatternSummary}. Directive: {ap.WarningDirective}",
                    Score = 0.85, // High priority warning
                    EstimatedTokens = 90,
                    EpistemicStatus = EpistemicStatus.ObservedFact,
                    Freshness = FreshnessStatus.Fresh,
                    Applicability = CurrentApplicability.High
                });
            }

            // 3. Trajectory
            var trajectory = await _store.GetTrajectoryForWorkAsync(tenantId, workId, ct);
            if (trajectory != null)
            {
                candidates.Add(new ContextSnapshotItem
                {
                    ItemId = trajectory.TrajectoryId,
                    ItemType = "Trajectory",
                    Content = $"[TRAJECTORY {trajectory.TrajectoryId}] Work: {trajectory.WorkId}, Resp: {trajectory.ResponsibilityId}, Plan: {trajectory.WorkPlanId ?? "None"}, Nodes: {string.Join(",", trajectory.NodeIds)}",
                    Score = 1.0, // Highest contextual relevance for own work
                    EstimatedTokens = 80,
                    EpistemicStatus = EpistemicStatus.ObservedFact,
                    Freshness = FreshnessStatus.Fresh,
                    Applicability = CurrentApplicability.High
                });
            }

            // -------------------------------------------------------------
            // STAGE 2: Deterministic Ranking & Token Budgeting
            // -------------------------------------------------------------

            // Sort deterministically by Score DESC, then ItemId ASC (to guarantee tie-break reproducibility)
            var rankedCandidates = candidates
                .OrderByDescending(c => c.Score)
                .ThenBy(c => c.ItemId, StringComparer.Ordinal)
                .ToList();

            var includedItems = new List<ContextSnapshotItem>();
            int tokensUsed = 0;
            int precedentCount = 0;
            int antiPatternCount = 0;

            foreach (var item in rankedCandidates)
            {
                // Ceiling check per item type
                if (item.ItemType == "Precedent" && precedentCount >= policy.MaxPrecedents)
                {
                    excludedItems.Add(item.ItemId);
                    exclusionReasons[item.ItemId] = $"BudgetExceeded: Reached MaxPrecedents ({policy.MaxPrecedents}).";
                    continue;
                }
                if (item.ItemType == "AntiPattern" && antiPatternCount >= policy.MaxAntiPatterns)
                {
                    excludedItems.Add(item.ItemId);
                    exclusionReasons[item.ItemId] = $"BudgetExceeded: Reached MaxAntiPatterns ({policy.MaxAntiPatterns}).";
                    continue;
                }

                // Token budget check
                if (tokensUsed + item.EstimatedTokens > policy.MaxContextTokens)
                {
                    excludedItems.Add(item.ItemId);
                    exclusionReasons[item.ItemId] = $"BudgetExceeded: MaxContextTokens ({policy.MaxContextTokens}) exceeded.";
                    continue;
                }

                includedItems.Add(item);
                tokensUsed += item.EstimatedTokens;

                if (item.ItemType == "Precedent") precedentCount++;
                if (item.ItemType == "AntiPattern") antiPatternCount++;
            }

            var snapshot = new OrganizationalContextSnapshot
            {
                SnapshotId = $"SNAP-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                TenantId = tenantId,
                TargetWorkId = workId,
                TokenBudget = policy.MaxContextTokens,
                EstimatedTokenCount = tokensUsed,
                ItemsIncluded = includedItems,
                ItemsExcluded = excludedItems,
                ExclusionReasons = exclusionReasons,
                RankingPolicyVersion = policy.RankingPolicyVersion,
                InputHashes = $"{tenantId}:{workId}:{includedItems.Count}:{tokensUsed}",
                AssemblyTimestamp = DateTime.UtcNow
            };

            snapshot.ComputeSnapshotHash();
            await _store.SaveSnapshotAsync(snapshot, ct);
            return snapshot;
        }
    }
}
