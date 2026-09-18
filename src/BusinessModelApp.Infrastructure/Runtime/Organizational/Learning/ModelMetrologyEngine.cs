using BusinessModelApp.Core.Domain.Runtime.Organizational.Learning;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Learning;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Learning;

public sealed class ModelMetrologyEngine : IModelMetrologyEngine
{
    public MetrologyRecord CalculateMetrology(
        string tenantId,
        string modelOrDomain,
        IReadOnlyList<EmpiricalOutcomeEvent> outcomes)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(modelOrDomain)) throw new ArgumentNullException(nameof(modelOrDomain));

        var record = new MetrologyRecord
        {
            RecordId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            ModelOrDomain = modelOrDomain,
            CalculatedUtc = DateTime.UtcNow
        };

        if (outcomes == null || outcomes.Count == 0)
        {
            record.SampleCount = 0;
            record.MeanAbsolutePercentageError = 0.0;
            record.BrierScore = 0.0;
            record.DirectionalAccuracy = 0.0;
            record.CalibrationSlope = 1.0;
            record.ObservedVariance = 0.0;
            return record;
        }

        record.SampleCount = outcomes.Count;

        // 1. MAPE (Mean Absolute Percentage Error)
        double totalApe = 0.0;
        int validApeCount = 0;
        foreach (var o in outcomes)
        {
            var actual = Math.Abs(o.ActualValue);
            if (actual > 0.0001)
            {
                totalApe += Math.Abs(o.DeltaValue) / actual;
                validApeCount++;
            }
        }
        record.MeanAbsolutePercentageError = validApeCount > 0 ? Math.Round(totalApe / validApeCount, 4) : 0.0;

        // 2. Directional Accuracy: percentage where actual direction matched expected direction (relative to 0 or baseline)
        int directionalMatches = 0;
        foreach (var o in outcomes)
        {
            bool expectedUp = o.ExpectedValue >= 0;
            bool actualUp = o.ActualValue >= 0;
            if (expectedUp == actualUp)
            {
                directionalMatches++;
            }
        }
        record.DirectionalAccuracy = Math.Round((double)directionalMatches / outcomes.Count, 4);

        // 3. Brier Score: Mean squared error of probabilities / normalized outcomes
        double sumSquaredError = 0.0;
        foreach (var o in outcomes)
        {
            var err = o.DeltaValue;
            sumSquaredError += (err * err);
        }
        record.BrierScore = Math.Round(sumSquaredError / outcomes.Count, 4);

        // 4. Observed Variance
        var meanActual = outcomes.Average(o => o.ActualValue);
        double sumVariance = outcomes.Sum(o => Math.Pow(o.ActualValue - meanActual, 2));
        record.ObservedVariance = Math.Round(sumVariance / outcomes.Count, 4);

        // 5. Calibration Slope (Actual vs Expected regression slope proxy)
        var meanExpected = outcomes.Average(o => o.ExpectedValue);
        double cov = 0.0;
        double varExpected = 0.0;
        foreach (var o in outcomes)
        {
            cov += (o.ExpectedValue - meanExpected) * (o.ActualValue - meanActual);
            varExpected += Math.Pow(o.ExpectedValue - meanExpected, 2);
        }
        record.CalibrationSlope = varExpected > 0.00001 ? Math.Round(cov / varExpected, 4) : 1.0;

        return record;
    }
}
