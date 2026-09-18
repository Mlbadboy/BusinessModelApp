using BusinessModelApp.Core.Domain.Runtime.Organizational.Readiness;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Readiness;

/// <summary>
/// Deterministic capacity and buffer stress test engine.
/// Invariant I31-F: Stress Test != Real-World Experiment.
/// Simulates mathematical scenarios without allocating capital, modifying production queues, or triggering external execution.
/// </summary>
public class CapacityStressTestEngine : ICapacityStressTestEngine
{
    public CapacityStressTestResult SimulateCapacityStress(
        string tenantId,
        ReadinessScenario scenario,
        double baseCapacity,
        double bufferRatio,
        List<string> criticalResourceIds)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));
        if (baseCapacity <= 0) baseCapacity = 100.0;
        if (bufferRatio < 0) bufferRatio = 0.0;

        double currentBufferCapacity = baseCapacity * bufferRatio;
        
        // Demand scale: projected demand delta (+0.3 = +30%) and capacity strain
        double demandMultiplier = Math.Max(0.1, 1.0 + scenario.ProjectedDemandDelta);
        double strainMultiplier = Math.Max(1.0, 1.0 + scenario.ProjectedCapacityStrain);
        double simulatedStressDemand = baseCapacity * demandMultiplier * strainMultiplier;

        double maxHorizonWeeks = scenario.Horizon switch
        {
            HorizonWindow.Immediate => 2.0,
            HorizonWindow.NearTerm => 8.0,
            HorizonWindow.MediumTerm => 26.0,
            HorizonWindow.LongTerm => 52.0,
            _ => 8.0
        };

        double excessDemandRate = simulatedStressDemand - baseCapacity;
        double bufferExhaustionHorizonWeeks;
        bool isBufferExhausted;

        if (excessDemandRate <= 0)
        {
            // Within base capacity; buffer is not depleted
            bufferExhaustionHorizonWeeks = maxHorizonWeeks * 2.0; // plenty of buffer
            isBufferExhausted = false;
        }
        else
        {
            // Depleting buffer: weeks until buffer hits 0
            // Weekly depletion rate relative to horizon
            double weeklyDepletion = excessDemandRate / maxHorizonWeeks;
            bufferExhaustionHorizonWeeks = weeklyDepletion > 0 ? (currentBufferCapacity / weeklyDepletion) : maxHorizonWeeks;
            isBufferExhausted = bufferExhaustionHorizonWeeks <= maxHorizonWeeks;
        }

        double peakStrainFactor = simulatedStressDemand / baseCapacity;

        return new CapacityStressTestResult
        {
            TestId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            ScenarioId = scenario.ScenarioId,
            CurrentBufferCapacity = currentBufferCapacity,
            SimulatedStressDemand = simulatedStressDemand,
            BufferExhaustionHorizonWeeks = Math.Round(bufferExhaustionHorizonWeeks, 2),
            IsBufferExhaustedWithinHorizon = isBufferExhausted,
            PeakStrainFactor = Math.Round(peakStrainFactor, 3),
            BottleneckResourceIds = criticalResourceIds?.ToList() ?? new List<string>(),
            SimulatedAtUtc = DateTime.UtcNow
        };
    }
}
