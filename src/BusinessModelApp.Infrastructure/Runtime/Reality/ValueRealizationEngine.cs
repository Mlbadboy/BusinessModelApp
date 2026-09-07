using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Reality;
using BusinessModelApp.Core.Interfaces.Runtime.Reality;

namespace BusinessModelApp.Infrastructure.Runtime.Reality
{
    /// <summary>
    /// Enforces the foundational business invariant:
    /// WORK COMPLETED != BUSINESS VALUE REALIZED.
    /// RealizedValue is strictly Unknown until verified by an empirical outcome ledger.
    /// </summary>
    public sealed class ValueRealizationEngine : IValueRealizationEngine
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ValueRealization>> _tenantValues = new();

        public Task<ValueRealization?> GetValueRealizationAsync(string tenantId, string missionId)
        {
            if (_tenantValues.TryGetValue(tenantId, out var dict) &&
                dict.TryGetValue(missionId, out var value))
            {
                return Task.FromResult<ValueRealization?>(value);
            }

            return Task.FromResult<ValueRealization?>(null);
        }

        public Task<IReadOnlyList<ValueRealization>> GetAllValueRealizationsAsync(string tenantId)
        {
            if (_tenantValues.TryGetValue(tenantId, out var dict))
            {
                return Task.FromResult<IReadOnlyList<ValueRealization>>(dict.Values.OrderByDescending(v => v.LastAssessedAt).ToList());
            }

            return Task.FromResult<IReadOnlyList<ValueRealization>>(Array.Empty<ValueRealization>());
        }

        public Task RecordExpectedValueAsync(
            string tenantId,
            string missionId,
            string missionName,
            decimal expected,
            decimal authorized,
            decimal actual,
            string currency = "INR")
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(missionId)) throw new ArgumentException("MissionId required.", nameof(missionId));

            var dict = _tenantValues.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, ValueRealization>());
            var unverified = ValueRealization.CreateUnverified(
                tenantId: tenantId,
                missionId: missionId,
                missionName: missionName,
                expectedValue: expected,
                authorizedExposure: authorized,
                actualSpend: actual,
                currency: currency
            );

            dict[missionId] = unverified;
            return Task.CompletedTask;
        }

        public Task<ValueRealization> RecordVerifiedOutcomeAsync(
            string tenantId,
            string missionId,
            decimal verifiedRevenue,
            decimal verifiedMargin,
            string outcomeLedgerRef,
            string evidenceSource)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required.", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(missionId)) throw new ArgumentException("MissionId required.", nameof(missionId));
            if (string.IsNullOrWhiteSpace(outcomeLedgerRef)) throw new ArgumentException("OutcomeLedgerRef required.", nameof(outcomeLedgerRef));

            var dict = _tenantValues.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, ValueRealization>());

            var existing = dict.TryGetValue(missionId, out var val)
                ? val
                : ValueRealization.CreateUnverified(tenantId, missionId, $"Mission {missionId}", 0, 0, 0);

            var netRealized = verifiedRevenue - existing.ActualSpend;

            var verified = existing with
            {
                VerifiedRevenue = verifiedRevenue,
                VerifiedMargin = verifiedMargin,
                RealizedValue = netRealized,
                RealizedValueStatus = RealityStatus.LiveVerified,
                OutcomeLedgerReference = outcomeLedgerRef,
                EvidenceSource = evidenceSource,
                LastAssessedAt = DateTimeOffset.UtcNow,
                EpistemicNote = $"Verified against Outcome Ledger #{outcomeLedgerRef} via {evidenceSource}."
            };

            dict[missionId] = verified;
            return Task.FromResult(verified);
        }
    }
}
