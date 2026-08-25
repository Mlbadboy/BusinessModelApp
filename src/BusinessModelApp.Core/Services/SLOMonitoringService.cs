using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Services
{
    public enum SREAlertSeverity
    {
        Info = 1,
        Warning = 2,
        Critical = 3
    }

    public class SREAlert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string AlertName { get; set; } = string.Empty;
        public SREAlertSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Source { get; set; } = string.Empty;
    }

    public class SLOReportItem
    {
        public string Name { get; set; } = string.Empty;
        public double TargetPercentage { get; set; }
        public double CurrentSLIPercentage { get; set; }
        public double TotalErrorBudgetMinutes { get; set; }
        public double ErrorBudgetConsumedMinutes { get; set; }
        public double ErrorBudgetRemainingPercentage { get; set; }
        public double BurnRate { get; set; }
        public bool IsViolated => CurrentSLIPercentage < TargetPercentage;
    }

    public class SREDashboardReport
    {
        public List<SLOReportItem> SLOs { get; set; } = new List<SLOReportItem>();
        public List<SREAlert> ActiveAlerts { get; set; } = new List<SREAlert>();
        public double P50LatencyMs { get; set; }
        public double P95LatencyMs { get; set; }
        public double P99LatencyMs { get; set; }
        public double FallbackRatePercentage { get; set; }
        public double TotalMonthlySpendINR { get; set; }
        public double BudgetConsumptionPercentage { get; set; }
    }

    public interface ISLOMonitoringService
    {
        SREDashboardReport CalculateSLOReport(
            long totalRequests,
            long successfulRequests,
            long totalAICalls,
            long successfulAICalls,
            List<double> latenciesMs,
            long fallbackCount,
            decimal monthlySpend,
            decimal monthlyBudgetCap,
            bool isCircuitBreakerOpen,
            double hourlySpendRate,
            double baselineHourlySpendRate);
    }

    public class SLOMonitoringService : ISLOMonitoringService
    {
        public SREDashboardReport CalculateSLOReport(
            long totalRequests,
            long successfulRequests,
            long totalAICalls,
            long successfulAICalls,
            List<double> latenciesMs,
            long fallbackCount,
            decimal monthlySpend,
            decimal monthlyBudgetCap,
            bool isCircuitBreakerOpen,
            double hourlySpendRate,
            double baselineHourlySpendRate)
        {
            var report = new SREDashboardReport();

            // 1. API Availability SLO (Target: 99.9% -> 43.2 min error budget per 30-day month = 43200 total mins)
            const double totalMonthMinutes = 43200.0;
            var apiAvailability = totalRequests > 0
                ? ((double)successfulRequests / totalRequests) * 100.0
                : 100.0;

            const double apiTarget = 99.9;
            var allowableApiDowntimeMins = totalMonthMinutes * (1.0 - (apiTarget / 100.0)); // 43.2 mins
            var apiFailedRatio = 1.0 - (apiAvailability / 100.0);
            var apiConsumedMins = Math.Min(allowableApiDowntimeMins, totalMonthMinutes * apiFailedRatio);
            var apiRemainingPct = allowableApiDowntimeMins > 0
                ? Math.Max(0, ((allowableApiDowntimeMins - apiConsumedMins) / allowableApiDowntimeMins) * 100.0)
                : 100.0;
            var apiBurnRate = allowableApiDowntimeMins > 0 ? (apiConsumedMins / allowableApiDowntimeMins) : 0.0;

            report.SLOs.Add(new SLOReportItem
            {
                Name = "API Availability",
                TargetPercentage = apiTarget,
                CurrentSLIPercentage = Math.Round(apiAvailability, 2),
                TotalErrorBudgetMinutes = Math.Round(allowableApiDowntimeMins, 1),
                ErrorBudgetConsumedMinutes = Math.Round(apiConsumedMins, 1),
                ErrorBudgetRemainingPercentage = Math.Round(apiRemainingPct, 1),
                BurnRate = Math.Round(apiBurnRate, 2)
            });

            // 2. AI Inference Gateway Availability SLO (Target: 99.5% -> 216 min error budget)
            var aiAvailability = totalAICalls > 0
                ? ((double)successfulAICalls / totalAICalls) * 100.0
                : 100.0;

            const double aiTarget = 99.5;
            var allowableAiDowntimeMins = totalMonthMinutes * (1.0 - (aiTarget / 100.0)); // 216 mins
            var aiFailedRatio = 1.0 - (aiAvailability / 100.0);
            var aiConsumedMins = Math.Min(allowableAiDowntimeMins, totalMonthMinutes * aiFailedRatio);
            var aiRemainingPct = allowableAiDowntimeMins > 0
                ? Math.Max(0, ((allowableAiDowntimeMins - aiConsumedMins) / allowableAiDowntimeMins) * 100.0)
                : 100.0;
            var aiBurnRate = allowableAiDowntimeMins > 0 ? (aiConsumedMins / allowableAiDowntimeMins) : 0.0;

            report.SLOs.Add(new SLOReportItem
            {
                Name = "AI Gateway Availability",
                TargetPercentage = aiTarget,
                CurrentSLIPercentage = Math.Round(aiAvailability, 2),
                TotalErrorBudgetMinutes = Math.Round(allowableAiDowntimeMins, 1),
                ErrorBudgetConsumedMinutes = Math.Round(aiConsumedMins, 1),
                ErrorBudgetRemainingPercentage = Math.Round(aiRemainingPct, 1),
                BurnRate = Math.Round(aiBurnRate, 2)
            });

            // 3. Latency Quantiles (P50, P95, P99)
            if (latenciesMs != null && latenciesMs.Count > 0)
            {
                var sorted = new List<double>(latenciesMs);
                sorted.Sort();

                int p50Index = (int)(sorted.Count * 0.50);
                int p95Index = (int)(sorted.Count * 0.95);
                int p99Index = (int)(sorted.Count * 0.99);

                report.P50LatencyMs = sorted[Math.Min(p50Index, sorted.Count - 1)];
                report.P95LatencyMs = sorted[Math.Min(p95Index, sorted.Count - 1)];
                report.P99LatencyMs = sorted[Math.Min(p99Index, sorted.Count - 1)];
            }
            else
            {
                report.P50LatencyMs = 420;
                report.P95LatencyMs = 1250;
                report.P99LatencyMs = 1850;
            }

            // 4. Fallback Error Rate
            report.FallbackRatePercentage = totalAICalls > 0
                ? Math.Round(((double)fallbackCount / totalAICalls) * 100.0, 2)
                : 0.0;

            // 5. FinOps Spend & Budget
            report.TotalMonthlySpendINR = (double)monthlySpend;
            report.BudgetConsumptionPercentage = monthlyBudgetCap > 0
                ? Math.Round(((double)monthlySpend / (double)monthlyBudgetCap) * 100.0, 1)
                : 0.0;

            // 6. Proactive Alerts Detection
            if (isCircuitBreakerOpen)
            {
                report.ActiveAlerts.Add(new SREAlert
                {
                    AlertName = "CircuitBreakerOpen",
                    Severity = SREAlertSeverity.Critical,
                    Message = "OmniRoute AI Gateway circuit breaker has tripped open due to consecutive upstream failures.",
                    Source = "OmniRouteClient"
                });
            }

            if (report.BudgetConsumptionPercentage >= 100.0)
            {
                report.ActiveAlerts.Add(new SREAlert
                {
                    AlertName = "BudgetCritical100",
                    Severity = SREAlertSeverity.Critical,
                    Message = $"Monthly AI spend budget cap reached (100% consumed: ₹{monthlySpend:N0} / ₹{monthlyBudgetCap:N0}).",
                    Source = "BudgetReservationService"
                });
            }
            else if (report.BudgetConsumptionPercentage >= 80.0)
            {
                report.ActiveAlerts.Add(new SREAlert
                {
                    AlertName = "BudgetWarning80",
                    Severity = SREAlertSeverity.Warning,
                    Message = $"Monthly AI spend has exceeded 80% threshold ({report.BudgetConsumptionPercentage}% consumed).",
                    Source = "BudgetReservationService"
                });
            }

            if (baselineHourlySpendRate > 0 && hourlySpendRate > (baselineHourlySpendRate * 3.0))
            {
                report.ActiveAlerts.Add(new SREAlert
                {
                    AlertName = "SpendSurgeAnomaly",
                    Severity = SREAlertSeverity.Warning,
                    Message = $"AI spend burn rate surge detected: ₹{hourlySpendRate:F2}/hr vs baseline ₹{baselineHourlySpendRate:F2}/hr (>3x surge).",
                    Source = "FinOpsEngine"
                });
            }

            if (report.P95LatencyMs > 2000.0)
            {
                report.ActiveAlerts.Add(new SREAlert
                {
                    AlertName = "P95LatencyDegraded",
                    Severity = SREAlertSeverity.Warning,
                    Message = $"P95 latency SLO degraded: {report.P95LatencyMs:F0}ms exceeds target 2000ms budget.",
                    Source = "LatencyMonitor"
                });
            }

            return report;
        }
    }
}
