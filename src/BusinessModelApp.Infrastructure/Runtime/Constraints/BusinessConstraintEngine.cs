using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Constraints;
using BusinessModelApp.Core.Interfaces.Runtime.Constraints;

namespace BusinessModelApp.Infrastructure.Runtime.Constraints
{
    public class BusinessConstraintEngine : IBusinessConstraintEngine
    {
        private readonly IBusinessConstraintStore _store;
        private readonly IConstraintFreshnessEngine _freshnessEngine;
        private readonly IPreFlightSimulationEngine _simulator;
        private readonly ISafeAlternativeEngine _alternativeEngine;

        public BusinessConstraintEngine(
            IBusinessConstraintStore store,
            IConstraintFreshnessEngine freshnessEngine,
            IPreFlightSimulationEngine simulator,
            ISafeAlternativeEngine alternativeEngine)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _freshnessEngine = freshnessEngine ?? throw new ArgumentNullException(nameof(freshnessEngine));
            _simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
            _alternativeEngine = alternativeEngine ?? throw new ArgumentNullException(nameof(alternativeEngine));
        }

        public async Task<BusinessFeasibilityResult> EvaluateFeasibilityAsync(
            Guid workspaceId,
            PreFlightEffectProposal proposal,
            DigitalTwinBusinessState? currentTwinState = null,
            CancellationToken ct = default)
        {
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));

            var twin = currentTwinState ?? await _store.GetDigitalTwinStateAsync(workspaceId, ct) ?? new DigitalTwinBusinessState { WorkspaceId = workspaceId };

            // 1. Check epistemic validity (Invariant I15-A)
            var unknowns = new List<string>();
            var stales = new List<string>();
            var conflicts = new List<string>();

            if (twin.IsUnknown)
            {
                unknowns.Add("Digital Twin financial telemetry is UNKNOWN. Fail-closed.");
            }

            if (twin.IsConflicted)
            {
                conflicts.Add("Digital Twin financial telemetry is CONFLICTED. Fail-closed.");
            }

            // 2. Pre-Flight Digital Twin Simulation
            var simulation = await _simulator.SimulateEffectAsync(workspaceId, proposal, twin, ct);

            // Check freshness of telemetry across active constraints
            var activeConstraints = await _store.GetActiveConstraintsAsync(workspaceId, ct);
            foreach (var c in activeConstraints)
            {
                if (!_freshnessEngine.IsFresh(twin.TelemetryCapturedAt, c.FreshnessRequirement, out var age))
                {
                    stales.Add($"Constraint '{c.Title}' telemetry is STALE (age {age.TotalMinutes:F1}m > req {c.FreshnessRequirement.TotalMinutes:F1}m).");
                }
            }

            var hardViolations = new List<string>(simulation.HardViolations);
            if (unknowns.Count > 0) hardViolations.AddRange(unknowns);
            if (conflicts.Count > 0) hardViolations.AddRange(conflicts);
            if (stales.Count > 0) hardViolations.AddRange(stales);

            var warnings = simulation.ConstraintEvaluations
                .Where(e => e.State == ConstraintEvaluationState.Warning || e.State == ConstraintEvaluationState.Constrained)
                .Select(e => $"{e.Title}: {e.Rationale}")
                .ToList();

            bool isFeasible = hardViolations.Count == 0;
            var overallState = isFeasible
                ? (warnings.Count > 0 ? ConstraintEvaluationState.Warning : ConstraintEvaluationState.Satisfied)
                : (unknowns.Count > 0 ? ConstraintEvaluationState.Unknown :
                   conflicts.Count > 0 ? ConstraintEvaluationState.Conflicted :
                   stales.Count > 0 ? ConstraintEvaluationState.Stale : ConstraintEvaluationState.Blocked);

            // 3. If blocked, generate safe alternatives (Advisory)
            var alternatives = new List<SafeAlternativeProposal>();
            if (!isFeasible)
            {
                var generated = await _alternativeEngine.GenerateAlternativesAsync(workspaceId, proposal, twin, simulation.ConstraintEvaluations, ct);
                alternatives.AddRange(generated);
            }

            var rawAudit = $"{workspaceId}:{isFeasible}:{overallState}:{hardViolations.Count}:{DateTimeOffset.UtcNow.Ticks}";
            using var sha = SHA256.Create();
            var auditHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(rawAudit)));

            string summary = isFeasible
                ? "Business feasibility verified. All hard constraints satisfied against Digital Twin."
                : $"Business feasibility BLOCKED. {hardViolations.Count} violation(s): {string.Join("; ", hardViolations.Take(3))}";

            return new BusinessFeasibilityResult
            {
                WorkspaceId = workspaceId,
                IsFeasible = isFeasible,
                OverallState = overallState,
                EvaluatedConstraints = simulation.ConstraintEvaluations,
                HardViolations = hardViolations,
                Warnings = warnings,
                UnknownRealities = unknowns,
                StaleRealities = stales,
                ConflictedRealities = conflicts,
                Simulation = simulation,
                RecommendedSafeAlternatives = alternatives,
                SummaryRationale = summary,
                ConstraintSnapshotHash = auditHash,
                AuditHash = auditHash
            };
        }

        public Task<ConstraintEvaluationResult> EvaluateSingleConstraintAsync(
            BusinessConstraintDefinition constraint,
            DigitalTwinBusinessState twinState,
            PreFlightEffectProposal? proposedEffect = null,
            CancellationToken ct = default)
        {
            if (constraint == null) throw new ArgumentNullException(nameof(constraint));
            if (twinState == null) throw new ArgumentNullException(nameof(twinState));

            bool isFresh = _freshnessEngine.IsFresh(twinState.TelemetryCapturedAt, constraint.FreshnessRequirement, out var age);
            double observed = GetMetricValue(twinState, constraint.MetricName);

            if (proposedEffect != null)
            {
                observed = ApplyProposalDelta(observed, constraint.MetricName, proposedEffect);
            }

            bool satisfies = CheckComparison(observed, constraint.ComparisonOperator, constraint.ThresholdValue, constraint.SecondaryThresholdValue);

            var state = ConstraintEvaluationState.Satisfied;
            if (twinState.IsUnknown) state = ConstraintEvaluationState.Unknown;
            else if (twinState.IsConflicted) state = ConstraintEvaluationState.Conflicted;
            else if (!isFresh) state = ConstraintEvaluationState.Stale;
            else if (!satisfies)
            {
                state = constraint.EnforcementMode == ConstraintEnforcementMode.HardFailClosed
                    ? ConstraintEvaluationState.Blocked
                    : ConstraintEvaluationState.Warning;
            }

            return Task.FromResult(new ConstraintEvaluationResult
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
                Rationale = satisfies && isFresh && !twinState.IsUnknown && !twinState.IsConflicted
                    ? $"Observed {observed:N2} {constraint.Unit} satisfies {constraint.ComparisonOperator} {constraint.ThresholdValue:N2} {constraint.Unit}."
                    : twinState.IsUnknown
                        ? "Reality is UNKNOWN."
                        : twinState.IsConflicted
                            ? "Reality is CONFLICTED."
                            : !isFresh
                                ? $"Telemetry is STALE (age {age.TotalMinutes:F1}m > req {constraint.FreshnessRequirement.TotalMinutes:F1}m)."
                                : $"Observed {observed:N2} {constraint.Unit} violates {constraint.ComparisonOperator} {constraint.ThresholdValue:N2} {constraint.Unit}."
            });
        }

        private static double GetMetricValue(DigitalTwinBusinessState state, string metricName)
        {
            return metricName?.ToLowerInvariant() switch
            {
                "cashbalance" or "cash" or "minimumcashreserve" => state.CurrentCashBalanceINR,
                "dailyburnrate" or "burnrate" => state.DailyBurnRateINR,
                "runwaydays" or "runway" => state.RunwayDays,
                "workingcapital" => state.WorkingCapitalINR,
                "grossmarginpercent" or "grossmargin" => state.GrossMarginPercent,
                "cac" or "customeracquisitioncost" => state.CustomerAcquisitionCostINR,
                "pendingapprovals" => state.PendingHumanApprovals,
                "activemissions" => state.ActiveConcurrentMissions,
                _ => state.CurrentCashBalanceINR
            };
        }

        private static double ApplyProposalDelta(double observed, string metricName, PreFlightEffectProposal proposal)
        {
            return metricName?.ToLowerInvariant() switch
            {
                "cashbalance" or "cash" or "minimumcashreserve" => Math.Max(observed - proposal.CashOutflowINR + proposal.ExpectedRevenueINR, 0.0),
                "dailyburnrate" or "burnrate" => Math.Max(observed + proposal.ProjectedBurnRateChangeINR, 1000.0),
                "workingcapital" => Math.Max(observed - proposal.CashOutflowINR, 0.0),
                "grossmarginpercent" or "grossmargin" => proposal.ProjectedGrossMarginPercent > 0 ? proposal.ProjectedGrossMarginPercent : observed,
                "cac" or "customeracquisitioncost" => proposal.ProjectedCacINR > 0 ? proposal.ProjectedCacINR : observed,
                "pendingapprovals" => observed + proposal.AdditionalApprovalLoad,
                _ => observed
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
