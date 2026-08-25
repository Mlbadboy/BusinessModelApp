using System;
using System.Collections.Generic;
using System.Linq;
using BusinessModelApp.Core.Services;
using Xunit;

namespace BusinessModelApp.Tests.SRE
{
    public class SLOMonitoringTests
    {
        private readonly SLOMonitoringService _sloService = new SLOMonitoringService();

        [Fact]
        public void CalculateSLOReport_Calculates_Accurate_SLI_And_ErrorBudgets()
        {
            // Arrange: 10,000 total API requests, 9,995 successful (99.95% SLI vs 99.9% SLO target)
            long totalRequests = 10000;
            long successfulRequests = 9995;
            long totalAICalls = 1000;
            long successfulAICalls = 997; // 99.7% SLI vs 99.5% target
            var latencies = new List<double> { 120, 250, 410, 520, 890, 1100, 1450, 1780 };
            long fallbackCount = 5;
            decimal monthlySpend = 18420m;
            decimal monthlyBudgetCap = 50000m;

            // Act
            var report = _sloService.CalculateSLOReport(
                totalRequests,
                successfulRequests,
                totalAICalls,
                successfulAICalls,
                latencies,
                fallbackCount,
                monthlySpend,
                monthlyBudgetCap,
                isCircuitBreakerOpen: false,
                hourlySpendRate: 25.0,
                baselineHourlySpendRate: 22.0);

            // Assert
            Assert.NotNull(report);
            var apiSlo = report.SLOs.FirstOrDefault(s => s.Name == "API Availability");
            Assert.NotNull(apiSlo);
            Assert.Equal(99.9, apiSlo.TargetPercentage);
            Assert.Equal(99.95, apiSlo.CurrentSLIPercentage);
            Assert.False(apiSlo.IsViolated);
            Assert.True(apiSlo.ErrorBudgetRemainingPercentage > 0);

            var aiSlo = report.SLOs.FirstOrDefault(s => s.Name == "AI Gateway Availability");
            Assert.NotNull(aiSlo);
            Assert.Equal(99.5, aiSlo.TargetPercentage);
            Assert.Equal(99.7, aiSlo.CurrentSLIPercentage);
            Assert.False(aiSlo.IsViolated);

            Assert.Equal(36.8, report.BudgetConsumptionPercentage);
            Assert.Empty(report.ActiveAlerts);
        }

        [Fact]
        public void CalculateSLOReport_Triggers_Alerts_On_CircuitBreaker_And_Budget_Thresholds()
        {
            // Arrange
            long totalRequests = 1000;
            long successfulRequests = 980; // Violated
            long totalAICalls = 500;
            long successfulAICalls = 490;
            var latencies = new List<double> { 1200, 2200, 3100 }; // Degraded latency > 2000ms
            long fallbackCount = 12;
            decimal monthlySpend = 42000m;
            decimal monthlyBudgetCap = 50000m; // 84% consumed -> Warning alert

            // Act
            var report = _sloService.CalculateSLOReport(
                totalRequests,
                successfulRequests,
                totalAICalls,
                successfulAICalls,
                latencies,
                fallbackCount,
                monthlySpend,
                monthlyBudgetCap,
                isCircuitBreakerOpen: true, // Critical Alert
                hourlySpendRate: 150.0,    // Surge alert (>3x of 25.0 baseline)
                baselineHourlySpendRate: 25.0);

            // Assert
            Assert.NotEmpty(report.ActiveAlerts);
            Assert.Contains(report.ActiveAlerts, a => a.AlertName == "CircuitBreakerOpen" && a.Severity == SREAlertSeverity.Critical);
            Assert.Contains(report.ActiveAlerts, a => a.AlertName == "BudgetWarning80" && a.Severity == SREAlertSeverity.Warning);
            Assert.Contains(report.ActiveAlerts, a => a.AlertName == "SpendSurgeAnomaly" && a.Severity == SREAlertSeverity.Warning);
            Assert.Contains(report.ActiveAlerts, a => a.AlertName == "P95LatencyDegraded" && a.Severity == SREAlertSeverity.Warning);
        }
    }
}
