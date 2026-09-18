using BusinessModelApp.Core.Domain.Runtime.Organizational.Readiness;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Readiness;

/// <summary>
/// Persistent Readiness Debt Tracker.
/// Known Future Exposure + Insufficient Preparation = Readiness Debt.
/// Invariant I31-D: Risk != Event.
/// </summary>
public class ReadinessDebtTracker : IReadinessDebtTracker
{
    private readonly IPredictiveReadinessStore _store;

    public ReadinessDebtTracker(IPredictiveReadinessStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public ReadinessDebtRecord RecordDebt(
        string tenantId,
        ReadinessDebtType debtType,
        double exposureAmount,
        string exposureDescription,
        double compoundingFactor = 1.0)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (exposureAmount < 0) exposureAmount = 0.0;

        var record = new ReadinessDebtRecord
        {
            DebtId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            DebtType = debtType,
            QuantifiedAmount = Math.Round(exposureAmount, 2),
            ExposureDescription = exposureDescription ?? string.Empty,
            CompoundingFactor = Math.Max(1.0, compoundingFactor),
            AgeInDays = 0,
            FirstIdentifiedUtc = DateTime.UtcNow,
            LastEvaluatedUtc = DateTime.UtcNow,
            IsRemediated = false
        };

        _store.SaveDebtRecordAsync(tenantId, record).GetAwaiter().GetResult();
        return record;
    }

    public IReadOnlyList<ReadinessDebtRecord> EvaluateTenantDebt(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) return Array.Empty<ReadinessDebtRecord>();

        var records = _store.ListDebtRecordsAsync(tenantId).GetAwaiter().GetResult();
        var now = DateTime.UtcNow;

        foreach (var record in records)
        {
            if (!record.IsRemediated)
            {
                int ageDays = (int)(now - record.FirstIdentifiedUtc).TotalDays;
                record.AgeInDays = Math.Max(0, ageDays);
                // Compounding factor grows 5% per 30 days unaddressed
                record.CompoundingFactor = Math.Round(1.0 + (record.AgeInDays / 30.0) * 0.05, 3);
                record.LastEvaluatedUtc = now;
            }
        }

        return records;
    }

    public bool RemediateDebt(string tenantId, string debtId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(debtId)) return false;

        var records = _store.ListDebtRecordsAsync(tenantId).GetAwaiter().GetResult();
        var target = records.FirstOrDefault(r => r.DebtId == debtId);
        if (target != null && !target.IsRemediated)
        {
            target.IsRemediated = true;
            target.LastEvaluatedUtc = DateTime.UtcNow;
            _store.SaveDebtRecordAsync(tenantId, target).GetAwaiter().GetResult();
            return true;
        }

        return false;
    }
}
