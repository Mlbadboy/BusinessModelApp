using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Memory
{
    public class MemoryFreshnessEvaluator : IMemoryFreshnessEvaluator
    {
        private readonly IOrganizationalMemoryStore _store;

        public MemoryFreshnessEvaluator(IOrganizationalMemoryStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<FreshnessEvaluationResult> EvaluateFreshnessAsync(
            string tenantId,
            string precedentId,
            TimeSpan age,
            string? currentRegime = null,
            CancellationToken ct = default)
        {
            var prec = await _store.GetPrecedentAsync(tenantId, precedentId, ct);
            if (prec == null)
            {
                return new FreshnessEvaluationResult
                {
                    MemoryId = precedentId,
                    TenantId = tenantId,
                    PreviousFreshness = FreshnessStatus.Unknown,
                    NewFreshness = FreshnessStatus.Unknown,
                    PreviousApplicability = CurrentApplicability.Inapplicable,
                    NewApplicability = CurrentApplicability.Inapplicable,
                    HistoricalValidityUnchanged = true,
                    Rationale = "Precedent not found."
                };
            }

            var prevFresh = prec.Freshness;
            var prevApp = prec.Applicability;

            // Invariant I28-E: Reality changes invalidate applicability, not historical truth
            FreshnessStatus newFresh;
            if (age < TimeSpan.FromDays(7))
            {
                newFresh = FreshnessStatus.Fresh;
            }
            else if (age < TimeSpan.FromDays(30))
            {
                newFresh = FreshnessStatus.Aging;
            }
            else if (age < TimeSpan.FromDays(90))
            {
                newFresh = FreshnessStatus.Stale;
            }
            else
            {
                newFresh = FreshnessStatus.Expired;
            }

            // Regime shift alters applicability
            CurrentApplicability newApp = prevApp;
            string rationale;
            if (!string.IsNullOrWhiteSpace(currentRegime) &&
                prec.ApplicabilityConditions.Count > 0 &&
                !prec.ApplicabilityConditions.Contains(currentRegime))
            {
                newApp = CurrentApplicability.Low;
                rationale = $"Market regime shifted to '{currentRegime}'. Precedent conditions do not match current regime; applicability degraded to Low while historical validity remains intact.";
            }
            else if (newFresh == FreshnessStatus.Expired)
            {
                newApp = CurrentApplicability.Inapplicable;
                rationale = "Precedent exceeded maximum freshness window. Current applicability marked Inapplicable; historical truth remains preserved.";
            }
            else
            {
                rationale = $"Freshness updated to {newFresh} based on elapsed age ({age.TotalDays:0} days).";
            }

            prec.Freshness = newFresh;
            prec.Applicability = newApp;
            // Historical validity is permanently preserved!
            prec.IsHistoricallyValid = true;

            await _store.SavePrecedentAsync(prec, ct);

            return new FreshnessEvaluationResult
            {
                MemoryId = precedentId,
                TenantId = tenantId,
                PreviousFreshness = prevFresh,
                NewFreshness = newFresh,
                PreviousApplicability = prevApp,
                NewApplicability = newApp,
                HistoricalValidityUnchanged = true,
                Rationale = rationale,
                EvaluatedUtc = DateTime.UtcNow
            };
        }
    }
}
