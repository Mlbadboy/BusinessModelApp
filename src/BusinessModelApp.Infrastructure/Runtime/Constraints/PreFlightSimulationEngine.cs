using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Constraints;
using BusinessModelApp.Core.Interfaces.Runtime.Constraints;

namespace BusinessModelApp.Infrastructure.Runtime.Constraints
{
    public class PreFlightSimulationEngine : IPreFlightSimulationEngine
    {
        private readonly IBusinessConstraintStore _store;
        private readonly IConstraintFreshnessEngine _freshnessEngine;

        public PreFlightSimulationEngine(IBusinessConstraintStore store, IConstraintFreshnessEngine freshnessEngine)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _freshnessEngine = freshnessEngine ?? throw new ArgumentNullException(nameof(freshnessEngine));
        }

        public async Task<PreFlightSimulationResult> SimulateEffectAsync(
            Guid workspaceId,
            PreFlightEffectProposal proposal,
            DigitalTwinBusinessState currentTwinState,
            CancellationToken ct = default)
        {
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));
            if (currentTwinState == null) throw new ArgumentNullException(nameof(currentTwinState));

            // 1. Check if required current reality is UNKNOWN or CONFLICTED
            if (currentTwinState.IsUnknown)
            {
                return new PreFlightSimulationResult
                {
                    IsPermissible = false,
                    CurrentState = currentTwinState,
                    ProjectedState = currentTwinState,
                    HardViolations = new[] { "Current business reality is UNKNOWN. Consequential simulation fails closed." },
                    SummaryRationale = "Digital Twin state is UNKNOWN. Cannot safely simulate consequential effect."
                };
            }

            if (currentTwinState.IsConflicted)
            {
                return new PreFlightSimulationResult
                {
                    IsPermissible = false,
                    CurrentState = currentTwinState,
                    ProjectedState = currentTwinState,
                    HardViolations = new[] { "Current business reality has CONFLICTED authoritative telemetry. Consequential simulation fails closed." },
                    SummaryRationale = "Digital Twin telemetry sources are in conflict. Cannot proceed with consequential execution."
                };
            }

            // 2. Project Future State: State_projected = State_current - Outflow (speculative revenue cannot offset immediate cash reserve)
            double projectedCash = Math.Max(currentTwinState.CurrentCashBalanceINR - proposal.CashOutflowINR, 0.0);
            double projectedDailyBurn = Math.Max(currentTwinState.DailyBurnRateINR + proposal.ProjectedBurnRateChangeINR, 1000.0);
            double projectedWorkingCapital = Math.Max(currentTwinState.WorkingCapitalINR - proposal.CashOutflowINR, 0.0);
            double projectedMargin = proposal.ProjectedGrossMarginPercent > 0 ? proposal.ProjectedGrossMarginPercent : currentTwinState.GrossMarginPercent;
            double projectedCac = proposal.ProjectedCacINR > 0 ? proposal.ProjectedCacINR : currentTwinState.CustomerAcquisitionCostINR;
            int projectedApprovals = currentTwinState.PendingHumanApprovals + proposal.AdditionalApprovalLoad;

            var projectedState = new DigitalTwinBusinessState
            {
                WorkspaceId = workspaceId,
                CurrentCashBalanceINR = projectedCash,
                DailyBurnRateINR = projectedDailyBurn,
                WorkingCapitalINR = projectedWorkingCapital,
                InventoryValueINR = currentTwinState.InventoryValueINR,
                GrossMarginPercent = projectedMargin,
                CustomerAcquisitionCostINR = projectedCac,
                PendingHumanApprovals = projectedApprovals,
                ActiveConcurrentMissions = currentTwinState.ActiveConcurrentMissions,
                AvailableCreditLimitINR = currentTwinState.AvailableCreditLimitINR,
                TelemetryCapturedAt = DateTimeOffset.UtcNow,
                SourceProvenance = "ProjectedDigitalTwinSimulation"
            };

            double runwayChange = projectedState.RunwayDays - currentTwinState.RunwayDays;

            // 3. Evaluate Active Constraints Against Projected State
            var constraints = await _store.GetActiveConstraintsAsync(workspaceId, ct);
            var evaluations = new List<ConstraintEvaluationResult>();
            var hardViolations = new List<string>();

            foreach (var constraint in constraints)
            {
                // Check freshness requirement
                bool isFresh = _freshnessEngine.IsFresh(currentTwinState.TelemetryCapturedAt, constraint.FreshnessRequirement, out var age);

                double observed = GetMetricValue(projectedState, constraint.MetricName);
                bool satisfies = CheckComparison(observed, constraint.ComparisonOperator, constraint.ThresholdValue, constraint.SecondaryThresholdValue);

                var state = ConstraintEvaluationState.Satisfied;
                if (!isFresh)
                {
                    state = ConstraintEvaluationState.Stale;
                }
                else if (!satisfies)
                {
                    state = constraint.EnforcementMode == ConstraintEnforcementMode.HardFailClosed
                        ? ConstraintEvaluationState.Blocked
                        : ConstraintEvaluationState.Warning;
                }

                var eval = new ConstraintEvaluationResult
                {
                    ConstraintId = constraint.ConstraintId,
                    Title = constraint.Title,
                    Type = constraint.Type,
                    EnforcementMode = constraint.EnforcementMode,
                    State = state,
                    ObservedValue = observed,
                    ThresholdValue = constraint.ThresholdValue,
                    Unit = constraint.Unit,
                    EvidenceAge = age,
                    IsStale = !isFresh,
                    Severity = constraint.Severity,
                    ProjectedValue = observed,
                    Rationale = satisfies && isFresh
                        ? $"Projected value {observed:N2} {constraint.Unit} satisfies {constraint.ComparisonOperator} {constraint.ThresholdValue:N2} {constraint.Unit}."
                        : !isFresh
                            ? $"Evidence is STALE (age {age.TotalMinutes:F1} min > required {constraint.FreshnessRequirement.TotalMinutes:F1} min)."
                            : $"Projected value {observed:N2} {constraint.Unit} VIOLATES constraint {constraint.ComparisonOperator} {constraint.ThresholdValue:N2} {constraint.Unit}."
                };

                evaluations.Add(eval);
                if (eval.IsHardViolation)
                {
                    hardViolations.Add($"{constraint.Title}: {eval.Rationale}");
                }
            }

            bool isPermissible = hardViolations.Count == 0;
            string summary = isPermissible
                ? $"Pre-flight simulation PERMISSIBLE. Projected Cash: ₹{projectedCash:N0}, Projected Runway: {projectedState.RunwayDays:F1} days."
                : $"Pre-flight simulation BLOCKED. {hardViolations.Count} hard constraint violations detected.";

            return new PreFlightSimulationResult
            {
                IsPermissible = isPermissible,
                CurrentState = currentTwinState,
                ProjectedState = projectedState,
                ConstraintEvaluations = evaluations,
                HardViolations = hardViolations,
                ProjectedRunwayChangeDays = runwayChange,
                SummaryRationale = summary
            };
        }

        private static double GetMetricValue(DigitalTwinBusinessState state, string metricName)
        {
            return metricName?.ToLowerInvariant() switch
            {
                "cashbalance" or "cash" or "minimumcashreserve" => state.CurrentCashBalanceINR,
                "dailyburnrate" or "burnrate" => state.DailyBurnRateINR,
                "runwaydays" or "runway" => state.RunwayDays,
                "workingcapital" => state.WorkingCapitalINR,
                "inventoryvalue" or "inventory" => state.InventoryValueINR,
                "grossmarginpercent" or "grossmargin" => state.GrossMarginPercent,
                "cac" or "customeracquisitioncost" => state.CustomerAcquisitionCostINR,
                "pendingapprovals" => state.PendingHumanApprovals,
                "activemissions" => state.ActiveConcurrentMissions,
                _ => state.CurrentCashBalanceINR
            };
        }

        private static bool CheckComparison(double value, ConstraintComparisonOperator op, double thresh, double? secondary)
        {
            return op switch
            {
                ConstraintComparisonOperator.GreaterThanOrEqual => value >= thresh,
                ConstraintComparisonOperator.LessThanOrEqual => value <= thresh,
                ConstraintComparisonOperator.Equal => Math.Abs(value - thresh) < 0.001,
                ConstraintComparisonOperator.NotEqual => Math.Abs(value - thresh) >= 0.001,
                ConstraintComparisonOperator.Between => secondary.HasValue ? value >= thresh && value <= secondary.Value : value >= thresh,
                _ => value >= thresh
            };
        }
    }
}
