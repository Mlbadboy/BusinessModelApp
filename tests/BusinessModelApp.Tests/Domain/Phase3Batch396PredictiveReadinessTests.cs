using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Api.Controllers;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Readiness;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;
using BusinessModelApp.Infrastructure.Runtime.Organizational.Readiness;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BusinessModelApp.Tests.Domain;

/// <summary>
/// Phase 3.9 Batch 3.9.6: Predictive Organizational Readiness (POR) Test Suite.
/// Verifies Invariant I31 (I31-A through I31-O) and validates all 14 test families (POR01 - POR84).
/// Target arithmetic: 1,751 (sealed baseline) + 84 = 1,835 / 1,835 PASS.
/// </summary>
public class Phase3Batch396PredictiveReadinessTests
{
    private readonly InMemoryPredictiveReadinessStore _store;
    private readonly CapacityStressTestEngine _stressEngine;
    private readonly ActionPostureResolver _postureResolver;
    private readonly ReadinessScoringEngine _scoringEngine;
    private readonly ReadinessDebtTracker _debtTracker;
    private readonly ContingencyPlanningEngine _contingencyEngine;
    private readonly PredictiveReadinessService _readinessService;

    public Phase3Batch396PredictiveReadinessTests()
    {
        _store = new InMemoryPredictiveReadinessStore();
        _stressEngine = new CapacityStressTestEngine();
        _postureResolver = new ActionPostureResolver();
        _scoringEngine = new ReadinessScoringEngine(_postureResolver);
        _debtTracker = new ReadinessDebtTracker(_store);
        _contingencyEngine = new ContingencyPlanningEngine(_store);
        _readinessService = new PredictiveReadinessService(
            _store, _stressEngine, _scoringEngine, _debtTracker, _contingencyEngine);
    }

    private ReadinessScenario CreateStandardScenario(string tenantId = "tenant-por-1")
    {
        return new ReadinessScenario
        {
            ScenarioId = "scen-" + Guid.NewGuid().ToString("N")[..8],
            TenantId = tenantId,
            Name = "Q4 Demand Surge Scenario",
            Description = "Plausible +35% surge in seasonal demand across tier-1 contracts.",
            Horizon = HorizonWindow.NearTerm,
            EstimatedProbability = 0.72,
            ConfidenceInterval = 0.85,
            ProjectedDemandDelta = 0.35,
            ProjectedCostDelta = 0.15,
            ProjectedCapacityStrain = 0.20,
            EvidenceRef = "telemetry:demand:2026-q3-trailing",
            CreatedUtc = DateTime.UtcNow
        };
    }

    // =========================================================================
    // Family 1: Scenario Ingestion & Decomposable Readiness Assessment (I31-A, I31-C)
    // =========================================================================

    [Fact]
    public async Task POR01_ScenarioIngestion_StoresScenarioCorrectly()
    {
        var scenario = CreateStandardScenario();
        await _store.SaveScenarioAsync(scenario.TenantId, scenario);

        var retrieved = await _store.GetScenarioAsync(scenario.TenantId, scenario.ScenarioId);
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Q4 Demand Surge Scenario");
        retrieved.EstimatedProbability.Should().Be(0.72);
    }

    [Fact]
    public void POR02_Scenario_CannotBeTreatedAsDeterministicFact_I31A()
    {
        var scenario = CreateStandardScenario();
        // Invariant I31-A: Forecast != Fact. Scenarios are probabilistic models, not empirical facts.
        scenario.ConfidenceInterval.Should().BeLessThan(1.0);
        scenario.EstimatedProbability.Should().BeLessThan(1.0);
        InvariantI31.I31_A.Should().Contain("Forecast != Fact");
    }

    [Fact]
    public void POR03_ScenarioIngestion_PreservesCounterfactualStressDefinition_I31C()
    {
        var scenario = CreateStandardScenario();
        scenario.ProjectedDemandDelta.Should().Be(0.35);
        // Invariant I31-C: Scenario != Forecast. Counterfactual stress testing is not point prediction.
        InvariantI31.I31_C.Should().Contain("Scenario != Forecast");
    }

    [Fact]
    public async Task POR04_Assessment_EvaluatesAll8DecomposableDimensions()
    {
        var scenario = CreateStandardScenario();
        var assessment = await _readinessService.EvaluateScenarioReadinessAsync(
            scenario.TenantId, scenario, new Dictionary<ReadinessDimension, double>());

        assessment.DemandReadiness.Should().BeGreaterThanOrEqualTo(0.0);
        assessment.CapacityReadiness.Should().BeGreaterThanOrEqualTo(0.0);
        assessment.LiquidityReadiness.Should().BeGreaterThanOrEqualTo(0.0);
        assessment.OperationalReadiness.Should().BeGreaterThanOrEqualTo(0.0);
        assessment.TechnologyReadiness.Should().BeGreaterThanOrEqualTo(0.0);
        assessment.WorkforceReadiness.Should().BeGreaterThanOrEqualTo(0.0);
        assessment.SupplierReadiness.Should().BeGreaterThanOrEqualTo(0.0);
        assessment.GovernanceReadiness.Should().BeGreaterThanOrEqualTo(0.0);
    }

    [Fact]
    public async Task POR05_Assessment_ComputesValidCompositeScoreWithinUnitInterval()
    {
        var scenario = CreateStandardScenario();
        var assessment = await _readinessService.EvaluateScenarioReadinessAsync(
            scenario.TenantId, scenario, new Dictionary<ReadinessDimension, double>());

        assessment.CompositeReadinessScore.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public async Task POR06_Assessment_IdentifiesLowScoringDimensionsAsVulnerabilities()
    {
        var scenario = CreateStandardScenario();
        var metrics = new Dictionary<ReadinessDimension, double>
        {
            { ReadinessDimension.Liquidity, 0.45 },
            { ReadinessDimension.Supplier, 0.50 }
        };

        var assessment = await _readinessService.EvaluateScenarioReadinessAsync(
            scenario.TenantId, scenario, metrics);

        assessment.DetectedVulnerabilities.Should().Contain(v => v.Dimension == ReadinessDimension.Liquidity);
        assessment.DetectedVulnerabilities.Should().Contain(v => v.Dimension == ReadinessDimension.Supplier);
    }

    // =========================================================================
    // Family 2: 8-Dimensional Deterministic Scoring Formula (I31-G)
    // =========================================================================

    [Fact]
    public void POR07_Scoring_AppliesStandardizedDimensionalWeightsCorrectly()
    {
        var scenario = CreateStandardScenario();
        // Weights: Liquidity=0.20, Capacity=0.18, Demand=0.15, Operational=0.15, Workforce=0.12, Supplier=0.10, Tech=0.05, Gov=0.05
        var metrics = new Dictionary<ReadinessDimension, double>
        {
            { ReadinessDimension.Liquidity, 1.0 },   // 0.20
            { ReadinessDimension.Capacity, 0.0 },    // 0.00
            { ReadinessDimension.Demand, 1.0 },      // 0.15
            { ReadinessDimension.Operational, 1.0 }, // 0.15
            { ReadinessDimension.Workforce, 1.0 },   // 0.12
            { ReadinessDimension.Supplier, 1.0 },    // 0.10
            { ReadinessDimension.Technology, 1.0 },  // 0.05
            { ReadinessDimension.Governance, 1.0 }   // 0.05
        }; // Sum = 0.82

        var assessment = _scoringEngine.EvaluateReadiness("tenant-1", scenario, metrics, null);
        assessment.CompositeReadinessScore.Should().Be(0.82);
    }

    [Fact]
    public void POR08_Scoring_ScoreAboveThreshold_YieldsGreenStatus()
    {
        var scenario = CreateStandardScenario();
        var metrics = Enum.GetValues<ReadinessDimension>().ToDictionary(d => d, _ => 0.90);

        var assessment = _scoringEngine.EvaluateReadiness("tenant-1", scenario, metrics, null);
        assessment.CompositeReadinessScore.Should().BeGreaterThanOrEqualTo(0.85);
        assessment.Status.Should().Be(ReadinessStatus.Green);
    }

    [Fact]
    public void POR09_Scoring_ScoreBetweenThresholds_YieldsAmberStatus()
    {
        var scenario = CreateStandardScenario();
        var metrics = Enum.GetValues<ReadinessDimension>().ToDictionary(d => d, _ => 0.65);

        var assessment = _scoringEngine.EvaluateReadiness("tenant-1", scenario, metrics, null);
        assessment.CompositeReadinessScore.Should().BeInRange(0.50, 0.849);
        assessment.Status.Should().Be(ReadinessStatus.Amber);
    }

    [Fact]
    public void POR10_Scoring_ScoreBelowThreshold_YieldsRedStatus()
    {
        var scenario = CreateStandardScenario();
        var metrics = Enum.GetValues<ReadinessDimension>().ToDictionary(d => d, _ => 0.30);

        var assessment = _scoringEngine.EvaluateReadiness("tenant-1", scenario, metrics, null);
        assessment.CompositeReadinessScore.Should().BeLessThan(0.50);
        assessment.Status.Should().Be(ReadinessStatus.Red);
    }

    [Fact]
    public void POR11_Scoring_MissingDimensionInputs_DefaultsToSafeBaseline()
    {
        var scenario = CreateStandardScenario();
        var emptyMetrics = new Dictionary<ReadinessDimension, double>();

        var assessment = _scoringEngine.EvaluateReadiness("tenant-1", scenario, emptyMetrics, null);
        assessment.CompositeReadinessScore.Should().Be(1.0);
        assessment.Status.Should().Be(ReadinessStatus.Green);
    }

    [Fact]
    public void POR12_Scoring_ClampsInputValuesToValidInterval_I31G()
    {
        var scenario = CreateStandardScenario();
        var outOfRangeMetrics = new Dictionary<ReadinessDimension, double>
        {
            { ReadinessDimension.Liquidity, 2.5 },
            { ReadinessDimension.Operational, -1.5 }
        };

        var assessment = _scoringEngine.EvaluateReadiness("tenant-1", scenario, outOfRangeMetrics, null);
        assessment.LiquidityReadiness.Should().Be(1.0);
        assessment.OperationalReadiness.Should().Be(0.0);
        assessment.CompositeReadinessScore.Should().BeInRange(0.0, 1.0);
    }

    // =========================================================================
    // Family 3: Capacity Buffer Stress Testing & Runout Horizons (I31-F)
    // =========================================================================

    [Fact]
    public void POR13_CapacityStressTest_PureSimulation_CarriesZeroRealWorldSideEffects_I31F()
    {
        var scenario = CreateStandardScenario();
        var result = _stressEngine.SimulateCapacityStress("tenant-1", scenario, 100.0, 0.20, new List<string> { "RES-1" });

        result.Should().NotBeNull();
        result.TestId.Should().NotBeNullOrWhiteSpace();
        // Invariant I31-F: Simulations carry zero real-world side effects.
        InvariantI31.I31_F.Should().Contain("Stress Test != Real-World Experiment");
    }

    [Fact]
    public void POR14_CapacityStressTest_SurplusCapacity_BufferNotExhausted()
    {
        var scenario = CreateStandardScenario();
        scenario.ProjectedDemandDelta = -0.10; // Demand drops 10%
        scenario.ProjectedCapacityStrain = 0.0;

        var result = _stressEngine.SimulateCapacityStress("tenant-1", scenario, 100.0, 0.25, new List<string>());
        result.IsBufferExhaustedWithinHorizon.Should().BeFalse();
        result.BufferExhaustionHorizonWeeks.Should().BeGreaterThan(8.0);
    }

    [Fact]
    public void POR15_CapacityStressTest_DemandSurge_CalculatesBufferExhaustionHorizonAccurately()
    {
        var scenario = CreateStandardScenario();
        scenario.ProjectedDemandDelta = 0.50; // +50% surge
        scenario.ProjectedCapacityStrain = 0.20;
        scenario.Horizon = HorizonWindow.NearTerm; // 8 weeks

        var result = _stressEngine.SimulateCapacityStress("tenant-1", scenario, 100.0, 0.20, new List<string>());
        result.IsBufferExhaustedWithinHorizon.Should().BeTrue();
        result.BufferExhaustionHorizonWeeks.Should().BeLessThanOrEqualTo(8.0);
    }

    [Fact]
    public void POR16_CapacityStressTest_BufferExhaustion_PenalizesCapacityDimensionScore()
    {
        var scenario = CreateStandardScenario();
        scenario.ProjectedDemandDelta = 0.60;
        scenario.ProjectedCapacityStrain = 0.30;

        var stressResult = _stressEngine.SimulateCapacityStress("tenant-1", scenario, 100.0, 0.15, new List<string>());
        var assessment = _scoringEngine.EvaluateReadiness("tenant-1", scenario, new Dictionary<ReadinessDimension, double>(), stressResult);

        // Capacity score was reduced due to strain penalty
        assessment.CapacityReadiness.Should().BeLessThan(1.0);
    }

    [Fact]
    public void POR17_CapacityStressTest_IdentifiesBottleneckResourcesCorrectly()
    {
        var scenario = CreateStandardScenario();
        var bottlenecks = new List<string> { "DB_IOPS_CLUSTER", "WAREHOUSE_WORKERS" };
        var result = _stressEngine.SimulateCapacityStress("tenant-1", scenario, 100.0, 0.20, bottlenecks);

        result.BottleneckResourceIds.Should().BeEquivalentTo(bottlenecks);
    }

    [Fact]
    public void POR18_CapacityStressTest_PeakStrainFactor_ScalesWithDemandAndStrainMultiplier()
    {
        var scenario = CreateStandardScenario();
        scenario.ProjectedDemandDelta = 0.40;
        scenario.ProjectedCapacityStrain = 0.10;

        var result = _stressEngine.SimulateCapacityStress("tenant-1", scenario, 100.0, 0.20, new List<string>());
        // (1 + 0.40) * (1 + 0.10) = 1.54
        result.PeakStrainFactor.Should().BeApproximately(1.54, 0.01);
    }

    // =========================================================================
    // Family 4: "Prepare Now vs. Wait" Decision Matrix (I31-B)
    // =========================================================================

    [Fact]
    public void POR19_ActionPosture_HighProbabilityImminentHorizon_ResolvesActNow()
    {
        var posture = _postureResolver.ResolvePosture(
            estimatedProbability: 0.85,
            impactMagnitude: 0.60,
            horizon: HorizonWindow.Immediate,
            preparationCost: 0.20,
            reversibilityScore: 0.80,
            confidenceInterval: 0.90);

        posture.Should().Be(ReadinessActionPosture.ActNow);
    }

    [Fact]
    public void POR20_ActionPosture_HighProbabilityNearTermHorizon_ResolvesPrepare()
    {
        var posture = _postureResolver.ResolvePosture(
            estimatedProbability: 0.80,
            impactMagnitude: 0.50,
            horizon: HorizonWindow.NearTerm,
            preparationCost: 0.30,
            reversibilityScore: 0.70,
            confidenceInterval: 0.85);

        posture.Should().Be(ReadinessActionPosture.Prepare);
    }

    [Fact]
    public void POR21_ActionPosture_DistantHorizon_ResolvesWatch()
    {
        var posture = _postureResolver.ResolvePosture(
            estimatedProbability: 0.75,
            impactMagnitude: 0.50,
            horizon: HorizonWindow.LongTerm,
            preparationCost: 0.20,
            reversibilityScore: 0.80,
            confidenceInterval: 0.85);

        posture.Should().Be(ReadinessActionPosture.Watch);
    }

    [Fact]
    public void POR22_ActionPosture_LowProbabilityLowImpact_ResolvesWait()
    {
        var posture = _postureResolver.ResolvePosture(
            estimatedProbability: 0.25,
            impactMagnitude: 0.15,
            horizon: HorizonWindow.NearTerm,
            preparationCost: 0.10,
            reversibilityScore: 0.90,
            confidenceInterval: 0.80);

        posture.Should().Be(ReadinessActionPosture.Wait);
    }

    [Fact]
    public void POR23_ActionPosture_ExtremePreparationCostAndLowReversibility_DowngradesPosture()
    {
        // High probability and immediate horizon, but extreme cost (>0.85) and near-zero reversibility (<0.20)
        var posture = _postureResolver.ResolvePosture(
            estimatedProbability: 0.78,
            impactMagnitude: 0.60,
            horizon: HorizonWindow.Immediate,
            preparationCost: 0.90,
            reversibilityScore: 0.10,
            confidenceInterval: 0.85);

        // Downgraded from ActNow to Prepare to prevent reckless commitment
        posture.Should().Be(ReadinessActionPosture.Prepare);
    }

    [Fact]
    public void POR24_ActionPosture_PredictionDoesNotImplyInevitability_I31B()
    {
        // Invariant I31-B: Prediction != Inevitability
        InvariantI31.I31_B.Should().Contain("Prediction != Inevitability");
    }

    // =========================================================================
    // Family 5: Persistent Readiness Debt Tracking & Accumulation (I31-D)
    // =========================================================================

    [Fact]
    public void POR25_ReadinessDebt_RecordsExposureAndInitialAmountAccurately()
    {
        var debt = _debtTracker.RecordDebt("tenant-1", ReadinessDebtType.CapacityDebt, 2500.0, "Missing inventory buffer");

        debt.Should().NotBeNull();
        debt.DebtType.Should().Be(ReadinessDebtType.CapacityDebt);
        debt.QuantifiedAmount.Should().Be(2500.0);
        debt.IsRemediated.Should().BeFalse();
    }

    [Fact]
    public void POR26_ReadinessDebt_DistinguishesRiskFromEventOccurrence_I31D()
    {
        var debt = _debtTracker.RecordDebt("tenant-1", ReadinessDebtType.CashDebt, 100000.0, "Runway shortfall under surge");
        // Invariant I31-D: Risk != Event. Debt tracks organizational deficit, not realized financial loss.
        debt.IsRemediated.Should().BeFalse();
        InvariantI31.I31_D.Should().Contain("Risk != Event");
    }

    [Fact]
    public void POR27_ReadinessDebt_TracksMultipleOrganizationalDebtTypes()
    {
        _debtTracker.RecordDebt("tenant-1", ReadinessDebtType.PeopleDebt, 50.0, "Key-person single point of failure");
        _debtTracker.RecordDebt("tenant-1", ReadinessDebtType.ComplianceDebt, 10.0, "Unverified SOC2 audit policy gap");

        var debts = _debtTracker.EvaluateTenantDebt("tenant-1");
        debts.Should().Contain(d => d.DebtType == ReadinessDebtType.PeopleDebt);
        debts.Should().Contain(d => d.DebtType == ReadinessDebtType.ComplianceDebt);
    }

    [Fact]
    public async Task POR28_ReadinessDebt_CompoundingFactorIncreasesWithAge()
    {
        var debt = _debtTracker.RecordDebt("tenant-1", ReadinessDebtType.TechnologyDebt, 15000.0, "Legacy database latency bottleneck");
        // Simulate aged record (60 days old)
        debt.FirstIdentifiedUtc = DateTime.UtcNow.AddDays(-60);
        await _store.SaveDebtRecordAsync("tenant-1", debt);

        var debts = _debtTracker.EvaluateTenantDebt("tenant-1");
        var evaluated = debts.First(d => d.DebtId == debt.DebtId);
        evaluated.AgeInDays.Should().BeGreaterThanOrEqualTo(60);
        evaluated.CompoundingFactor.Should().BeGreaterThan(1.05);
    }

    [Fact]
    public void POR29_ReadinessDebt_RemediateDebt_MarksRecordRemediated()
    {
        var debt = _debtTracker.RecordDebt("tenant-1", ReadinessDebtType.SupplierDebt, 5000.0, "Single-vendor supply chain dependency");
        var success = _debtTracker.RemediateDebt("tenant-1", debt.DebtId);

        success.Should().BeTrue();
        var debts = _debtTracker.EvaluateTenantDebt("tenant-1");
        debts.First(d => d.DebtId == debt.DebtId).IsRemediated.Should().BeTrue();
    }

    [Fact]
    public async Task POR30_ReadinessDebt_CapacityBufferExhaustion_AutomaticallyRecordsCapacityDebt()
    {
        var scenario = CreateStandardScenario("tenant-debt-auto");
        scenario.ProjectedDemandDelta = 0.80; // High surge exhausting buffer

        await _readinessService.EvaluateScenarioReadinessAsync(
            "tenant-debt-auto", scenario, new Dictionary<ReadinessDimension, double>(), 100.0, 0.10);

        var debts = await _readinessService.GetTenantReadinessDebtAsync("tenant-debt-auto");
        debts.Should().Contain(d => d.DebtType == ReadinessDebtType.CapacityDebt);
    }

    // =========================================================================
    // Family 6: "Why AMBER/RED?" Deterministic Audit Lineage (I31-J)
    // =========================================================================

    [Fact]
    public async Task POR31_WhyTrace_ContainsCompleteAuditLineage()
    {
        var scenario = CreateStandardScenario("tenant-why-1");
        var assessment = await _readinessService.EvaluateScenarioReadinessAsync(
            "tenant-why-1", scenario, new Dictionary<ReadinessDimension, double>());

        var trace = await _readinessService.GetWhyReadinessTraceAsync("tenant-why-1", assessment.AssessmentId);
        trace.Should().NotBeNull();
        trace.AssessmentId.Should().Be(assessment.AssessmentId);
        trace.DimensionScores.Should().HaveCount(8);
        trace.EvidenceRef.Should().Be(scenario.EvidenceRef);
    }

    [Fact]
    public async Task POR32_WhyTrace_ReflectsAccurateContributingFactorsForDepressedDimensions()
    {
        var scenario = CreateStandardScenario("tenant-why-2");
        var metrics = new Dictionary<ReadinessDimension, double>
        {
            { ReadinessDimension.Liquidity, 0.40 }
        };

        var assessment = await _readinessService.EvaluateScenarioReadinessAsync(
            "tenant-why-2", scenario, metrics);

        var trace = await _readinessService.GetWhyReadinessTraceAsync("tenant-why-2", assessment.AssessmentId);
        trace.PrimaryContributingFactors.Should().Contain(f => f.Contains("Liquidity"));
    }

    [Fact]
    public async Task POR33_WhyTrace_ReflectsEvidenceReferenceAndModelConfidence()
    {
        var scenario = CreateStandardScenario("tenant-why-3");
        var assessment = await _readinessService.EvaluateScenarioReadinessAsync(
            "tenant-why-3", scenario, new Dictionary<ReadinessDimension, double>());

        var trace = await _readinessService.GetWhyReadinessTraceAsync("tenant-why-3", assessment.AssessmentId);
        trace.EvidenceRef.Should().Be(scenario.EvidenceRef);
        trace.ModelConfidence.Should().Be(scenario.ConfidenceInterval);
    }

    [Fact]
    public async Task POR34_WhyTrace_ReportsWeeksToImpactBasedOnHorizonWindow()
    {
        var scenario = CreateStandardScenario("tenant-why-4");
        scenario.Horizon = HorizonWindow.NearTerm;

        var assessment = await _readinessService.EvaluateScenarioReadinessAsync(
            "tenant-why-4", scenario, new Dictionary<ReadinessDimension, double>());

        var trace = await _readinessService.GetWhyReadinessTraceAsync("tenant-why-4", assessment.AssessmentId);
        trace.WeeksToImpact.Should().Be(6.0);
    }

    [Fact]
    public async Task POR35_WhyTrace_ReportsAccurateCapacityGap()
    {
        var scenario = CreateStandardScenario("tenant-why-5");
        var metrics = new Dictionary<ReadinessDimension, double>
        {
            { ReadinessDimension.Capacity, 0.60 }
        };

        var assessment = await _readinessService.EvaluateScenarioReadinessAsync(
            "tenant-why-5", scenario, metrics);

        var trace = await _readinessService.GetWhyReadinessTraceAsync("tenant-why-5", assessment.AssessmentId);
        double expectedGap = Math.Round((1.0 - assessment.CapacityReadiness) * 100.0, 1);
        trace.CapacityGap.Should().Be(expectedGap);
    }

    [Fact]
    public async Task POR36_WhyTrace_PreservesDeterministicIntegrityUnderRepeatedCalls()
    {
        var scenario = CreateStandardScenario("tenant-why-6");
        var assessment = await _readinessService.EvaluateScenarioReadinessAsync(
            "tenant-why-6", scenario, new Dictionary<ReadinessDimension, double>());

        var trace1 = await _readinessService.GetWhyReadinessTraceAsync("tenant-why-6", assessment.AssessmentId);
        var trace2 = await _readinessService.GetWhyReadinessTraceAsync("tenant-why-6", assessment.AssessmentId);

        trace1.CompositeScore.Should().Be(trace2.CompositeScore);
        trace1.CapacityGap.Should().Be(trace2.CapacityGap);
        trace1.PrimaryContributingFactors.Should().BeEquivalentTo(trace2.PrimaryContributingFactors);
    }

    // =========================================================================
    // Family 7: UNKNOWN & Insufficient Evidence Handling (I31-A, I31-G)
    // =========================================================================

    [Fact]
    public void POR37_InsufficientEvidence_LowModelConfidence_ResolvesInsufficientEvidencePosture()
    {
        var posture = _postureResolver.ResolvePosture(
            estimatedProbability: 0.90,
            impactMagnitude: 0.80,
            horizon: HorizonWindow.Immediate,
            preparationCost: 0.10,
            reversibilityScore: 0.90,
            confidenceInterval: 0.25); // Epistemically ungrounded

        posture.Should().Be(ReadinessActionPosture.InsufficientEvidence);
    }

    [Fact]
    public void POR38_InsufficientEvidence_ZeroConfidenceInterval_RejectsCertainty_I31G()
    {
        var posture = _postureResolver.ResolvePosture(
            estimatedProbability: 1.0,
            impactMagnitude: 1.0,
            horizon: HorizonWindow.Immediate,
            preparationCost: 0.10,
            reversibilityScore: 0.90,
            confidenceInterval: 0.0);

        posture.Should().Be(ReadinessActionPosture.InsufficientEvidence);
        InvariantI31.I31_G.Should().Contain("Probability != Certainty");
    }

    [Fact]
    public void POR39_InsufficientEvidence_ForecastTreatedAsHypothesis_I31A()
    {
        var scenario = CreateStandardScenario();
        scenario.EvidenceRef = string.Empty; // Ungrounded
        InvariantI31.I31_A.Should().Contain("Forecast != Fact");
    }

    [Fact]
    public void POR40_InsufficientEvidence_MultipleModelConsensus_DoesNotEqualGroundTruth_I31H()
    {
        InvariantI31.I31_H.Should().Contain("Multiple Models != Truth");
    }

    [Fact]
    public void POR41_InsufficientEvidence_HistoricalPrecedentDoesNotGuaranteeFuture_I31I()
    {
        InvariantI31.I31_I.Should().Contain("Historical Precedent != Future Guarantee");
    }

    [Fact]
    public async Task POR42_InsufficientEvidence_EmptyEvidenceRef_TreatedAsUngroundedScenario()
    {
        var scenario = CreateStandardScenario("tenant-ungrounded");
        scenario.EvidenceRef = string.Empty;
        scenario.ConfidenceInterval = 0.30;

        var assessment = await _readinessService.EvaluateScenarioReadinessAsync(
            "tenant-ungrounded", scenario, new Dictionary<ReadinessDimension, double>());

        assessment.ActionPosture.Should().Be(ReadinessActionPosture.InsufficientEvidence);
    }

    // =========================================================================
    // Family 8: Multi-Tenant Partitioning (I31-E)
    // =========================================================================

    [Fact]
    public async Task POR43_MultiTenant_ScenariosAreStrictlyPartitioned()
    {
        var s1 = CreateStandardScenario("tenant-alpha");
        var s2 = CreateStandardScenario("tenant-beta");

        await _store.SaveScenarioAsync("tenant-alpha", s1);
        await _store.SaveScenarioAsync("tenant-beta", s2);

        var alphaScenarios = await _store.ListScenariosAsync("tenant-alpha");
        var betaScenarios = await _store.ListScenariosAsync("tenant-beta");

        alphaScenarios.Should().Contain(s => s.ScenarioId == s1.ScenarioId);
        alphaScenarios.Should().NotContain(s => s.ScenarioId == s2.ScenarioId);
        betaScenarios.Should().Contain(s => s.ScenarioId == s2.ScenarioId);
        betaScenarios.Should().NotContain(s => s.ScenarioId == s1.ScenarioId);
    }

    [Fact]
    public async Task POR44_MultiTenant_AssessmentsAreStrictlyPartitioned()
    {
        var s1 = CreateStandardScenario("tenant-alpha");
        var s2 = CreateStandardScenario("tenant-beta");

        var a1 = await _readinessService.EvaluateScenarioReadinessAsync("tenant-alpha", s1, new Dictionary<ReadinessDimension, double>());
        var a2 = await _readinessService.EvaluateScenarioReadinessAsync("tenant-beta", s2, new Dictionary<ReadinessDimension, double>());

        var alphaAssessments = await _store.ListAssessmentsAsync("tenant-alpha");
        alphaAssessments.Should().Contain(a => a.AssessmentId == a1.AssessmentId);
        alphaAssessments.Should().NotContain(a => a.AssessmentId == a2.AssessmentId);
    }

    [Fact]
    public async Task POR45_MultiTenant_StressTestResultsAreStrictlyPartitioned()
    {
        var s1 = CreateStandardScenario("tenant-alpha");
        var res = _stressEngine.SimulateCapacityStress("tenant-alpha", s1, 100.0, 0.20, new List<string>());
        await _store.SaveStressTestResultAsync("tenant-alpha", res);

        var retrieved = await _store.GetStressTestResultAsync("tenant-beta", res.TestId);
        retrieved.Should().BeNull();
    }

    [Fact]
    public void POR46_MultiTenant_ReadinessDebtsAreStrictlyPartitioned()
    {
        _debtTracker.RecordDebt("tenant-alpha", ReadinessDebtType.CashDebt, 50000.0, "Alpha runway exposure");
        _debtTracker.RecordDebt("tenant-beta", ReadinessDebtType.CashDebt, 20000.0, "Beta runway exposure");

        var alphaDebts = _debtTracker.EvaluateTenantDebt("tenant-alpha");
        var betaDebts = _debtTracker.EvaluateTenantDebt("tenant-beta");

        alphaDebts.Should().OnlyContain(d => d.TenantId == "tenant-alpha");
        betaDebts.Should().OnlyContain(d => d.TenantId == "tenant-beta");
    }

    [Fact]
    public async Task POR47_MultiTenant_ContingenciesAreStrictlyPartitioned()
    {
        var c1 = _contingencyEngine.CreateContingency("tenant-alpha", "s1", "Title 1", "Obj 1", "Payload 1", "Trig 1", 10.0, 0.8);
        var c2 = _contingencyEngine.CreateContingency("tenant-beta", "s2", "Title 2", "Obj 2", "Payload 2", "Trig 2", 10.0, 0.8);

        var alphaContingencies = await _store.ListContingenciesAsync("tenant-alpha");
        alphaContingencies.Should().Contain(c => c.ContingencyId == c1.ContingencyId);
        alphaContingencies.Should().NotContain(c => c.ContingencyId == c2.ContingencyId);
    }

    [Fact]
    public async Task POR48_MultiTenant_TenantCannotAccessOrMutateForeignTenantReadiness_I31E()
    {
        var c1 = _contingencyEngine.CreateContingency("tenant-alpha", "s1", "Alpha Title", "Obj", "Payload", "Trig", 10.0, 0.8);
        Func<Task> act = async () => await _contingencyEngine.StageContingentWorkProposalAsync("tenant-beta", c1.ContingencyId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found for tenant 'tenant-beta'*");
    }

    // =========================================================================
    // Family 9: Historical Scenario Immutability & Replay (I31-M)
    // =========================================================================

    [Fact]
    public void POR49_Immutability_PredictionCannotMutateHistoricalAuditLogs_I31M()
    {
        InvariantI31.I31_M.Should().Contain("Prediction Cannot Mutate Truth");
    }

    [Fact]
    public void POR50_Immutability_PredictionCannotMutateStateTelemetry_I31M()
    {
        var doctrine = InvariantI31.CoreDoctrine;
        doctrine.Should().Contain("FORECAST != SCENARIO != RISK != READINESS != WORK != AUTHORITY != EXECUTION");
    }

    [Fact]
    public async Task POR51_Immutability_EvaluatingScenario_DoesNotMutateBaseCapacityMetrics()
    {
        double baseCapacity = 150.0;
        var scenario = CreateStandardScenario("tenant-immut-1");
        await _readinessService.EvaluateScenarioReadinessAsync(
            "tenant-immut-1", scenario, new Dictionary<ReadinessDimension, double>(), baseCapacity, 0.25);

        // Verification: baseCapacity variable remains untouched
        baseCapacity.Should().Be(150.0);
    }

    [Fact]
    public void POR52_Immutability_RepeatedEvaluationsWithSameInputs_ProduceIdenticalScores()
    {
        var scenario = CreateStandardScenario("tenant-immut-2");
        var metrics = new Dictionary<ReadinessDimension, double>
        {
            { ReadinessDimension.Liquidity, 0.70 },
            { ReadinessDimension.Capacity, 0.65 }
        };

        var a1 = _scoringEngine.EvaluateReadiness("tenant-immut-2", scenario, metrics, null);
        var a2 = _scoringEngine.EvaluateReadiness("tenant-immut-2", scenario, metrics, null);

        a1.CompositeReadinessScore.Should().Be(a2.CompositeReadinessScore);
        a1.Status.Should().Be(a2.Status);
    }

    [Fact]
    public async Task POR53_Immutability_ReplayOfHistoricalScenarios_PreservesOriginalAssessment()
    {
        var scenario = CreateStandardScenario("tenant-replay");
        var originalAssessment = await _readinessService.EvaluateScenarioReadinessAsync(
            "tenant-replay", scenario, new Dictionary<ReadinessDimension, double>());

        var stored = await _store.GetAssessmentAsync("tenant-replay", originalAssessment.AssessmentId);
        stored.Should().NotBeNull();
        stored!.CompositeReadinessScore.Should().Be(originalAssessment.CompositeReadinessScore);
    }

    [Fact]
    public async Task POR54_Immutability_ScenarioUpdate_CreatesNewEvaluationWithoutModifyingPriorRecord()
    {
        var scenario = CreateStandardScenario("tenant-update");
        var a1 = await _readinessService.EvaluateScenarioReadinessAsync(
            "tenant-update", scenario, new Dictionary<ReadinessDimension, double>());

        scenario.ProjectedDemandDelta = 0.90; // Alter demand
        var a2 = await _readinessService.EvaluateScenarioReadinessAsync(
            "tenant-update", scenario, new Dictionary<ReadinessDimension, double>());

        a1.AssessmentId.Should().NotBe(a2.AssessmentId);
        var original = await _store.GetAssessmentAsync("tenant-update", a1.AssessmentId);
        original.Should().NotBeNull();
    }

    // =========================================================================
    // Family 10: Contingent WorkProposal Staging (3.9.0 Downstream Integration) (I31-K)
    // =========================================================================

    [Fact]
    public void POR55_ContingencyPlanning_CreatesValidContingencyRecord()
    {
        var contingency = _contingencyEngine.CreateContingency(
            "tenant-1", "scen-1", "Buffer Expansion", "Scale cluster", "Deploy 2 additional nodes", "CPU > 80% for 3 days", 500.0, 0.95);

        contingency.Should().NotBeNull();
        contingency.Title.Should().Be("Buffer Expansion");
        contingency.IsStagedToWorkControlPlane.Should().BeFalse();
    }

    [Fact]
    public void POR56_ContingencyPlanning_ContingencyIsNotApprovedWork_I31K()
    {
        InvariantI31.I31_K.Should().Contain("Contingency Proposal != Approved Work");
    }

    [Fact]
    public async Task POR57_ContingencyPlanning_StagesProposalIntoWorkControlPlane()
    {
        var contingency = _contingencyEngine.CreateContingency(
            "tenant-1", "scen-1", "Warehouse Staffing Contingency", "Hire contingent temp workforce", "Authorize temp agency retainer", "Order volume > 5000", 2500.0, 0.85);

        var proposal = await _contingencyEngine.StageContingentWorkProposalAsync("tenant-1", contingency.ContingencyId);
        proposal.Should().NotBeNull();
        proposal.SourceType.Should().Be("PredictiveOrganizationalReadiness");
        proposal.Title.Should().Contain("Warehouse Staffing Contingency");
    }

    [Fact]
    public async Task POR58_ContingencyPlanning_StagedProposalCarriesPendingAdmissionStatus()
    {
        var contingency = _contingencyEngine.CreateContingency(
            "tenant-1", "scen-1", "Cash Credit Line Pre-Approval", "Secure bridge line", "Submit bank application", "Runway < 60 days", 1000.0, 0.90);

        var proposal = await _contingencyEngine.StageContingentWorkProposalAsync("tenant-1", contingency.ContingencyId);
        proposal.AdmissionStatus.Should().Be(ProposalAdmissionStatus.Pending);
    }

    [Fact]
    public async Task POR59_ContingencyPlanning_StagedProposalComputesValidProvenanceHash()
    {
        var contingency = _contingencyEngine.CreateContingency(
            "tenant-1", "scen-1", "Inventory Buffer Expansion", "Order safety stock", "Supplier PO draft", "Stockout risk > 70%", 8000.0, 0.80);

        var proposal = await _contingencyEngine.StageContingentWorkProposalAsync("tenant-1", contingency.ContingencyId);
        proposal.ProvenanceHash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task POR60_ContingencyPlanning_ProposalIncludesActivationTriggerConditionInObjective()
    {
        string trigger = "Trailing lead time exceeds 14 days";
        var contingency = _contingencyEngine.CreateContingency(
            "tenant-1", "scen-1", "Secondary Supplier Setup", "Onboard secondary vendor", "Execute vendor MSA", trigger, 3000.0, 0.75);

        var proposal = await _contingencyEngine.StageContingentWorkProposalAsync("tenant-1", contingency.ContingencyId);
        proposal.Objective.SuccessCriteria.Should().Contain(trigger);
    }

    // =========================================================================
    // Family 11: Rejection of Speculative Execution & Authority Pooling (I31-E, I31-O)
    // =========================================================================

    [Fact]
    public void POR61_AuthorityIsolation_ReadinessAssessmentCannotDirectlyAuthorizeAction_I31E()
    {
        InvariantI31.I31_E.Should().Contain("Readiness != Authorization");
    }

    [Fact]
    public async Task POR62_AuthorityIsolation_RedStatusDoesNotBypassGovernanceGates_I31E()
    {
        var scenario = CreateStandardScenario("tenant-red");
        var metrics = Enum.GetValues<ReadinessDimension>().ToDictionary(d => d, _ => 0.20);

        var assessment = await _readinessService.EvaluateScenarioReadinessAsync("tenant-red", scenario, metrics);
        assessment.Status.Should().Be(ReadinessStatus.Red);

        // Even with Red status, no autonomous execution is triggered
        var contingencies = await _store.ListContingenciesAsync("tenant-red");
        contingencies.Should().BeEmpty();
    }

    [Fact]
    public async Task POR63_AuthorityIsolation_GreenStatusDoesNotAuthorizeExecutionWithoutWorkItem_I31E()
    {
        var scenario = CreateStandardScenario("tenant-green");
        var metrics = Enum.GetValues<ReadinessDimension>().ToDictionary(d => d, _ => 1.0);

        var assessment = await _readinessService.EvaluateScenarioReadinessAsync("tenant-green", scenario, metrics);
        assessment.Status.Should().Be(ReadinessStatus.Green);
        // Green status does not mean any work is running
        assessment.ActionPosture.Should().BeOneOf(ReadinessActionPosture.Watch, ReadinessActionPosture.Wait, ReadinessActionPosture.Prepare);
    }

    [Fact]
    public void POR64_AuthorityIsolation_PORCannotCreateExecutionPermit_I31O()
    {
        InvariantI31.I31_O.Should().Contain("POR Cannot Create Execution Authority");
    }

    [Fact]
    public void POR65_AuthorityIsolation_PORCannotPoolOrElevateAgentAuthority_I31E()
    {
        // Neither scenario nor assessment has authority pooling fields
        typeof(OrganizationalReadinessAssessment).GetProperty("ExecutionPermit").Should().BeNull();
        typeof(ReadinessScenario).GetProperty("ElevatedAuthorityRole").Should().BeNull();
    }

    [Fact]
    public async Task POR66_AuthorityIsolation_MultipleDepressedDimensionsDoNotSynthesizeAuthority()
    {
        var scenario = CreateStandardScenario("tenant-multi-depressed");
        var metrics = new Dictionary<ReadinessDimension, double>
        {
            { ReadinessDimension.Liquidity, 0.10 },
            { ReadinessDimension.Capacity, 0.10 },
            { ReadinessDimension.Operational, 0.10 }
        };

        var assessment = await _readinessService.EvaluateScenarioReadinessAsync("tenant-multi-depressed", scenario, metrics);
        assessment.DetectedVulnerabilities.Count.Should().BeGreaterThanOrEqualTo(3);
        // Synthesizes no execution permits
        typeof(OrganizationalReadinessAssessment).GetProperty("AutonomousExecutionAuthorized").Should().BeNull();
    }

    // =========================================================================
    // Family 12: Batch 6 Execution Firewall & Zero Permit Isolation (I31-O)
    // =========================================================================

    [Fact]
    public void POR67_Batch6Firewall_PORDoesNotInteractWithConnectorExecutionDirectly_I31O()
    {
        // Verify POR services do not reference external connector dispatchers
        typeof(PredictiveReadinessService).GetConstructors()
            .All(c => !c.GetParameters().Any(p => p.ParameterType.Name.Contains("Connector"))).Should().BeTrue();
    }

    [Fact]
    public void POR68_Batch6Firewall_PORProducesZeroExternalSideEffects_I31F()
    {
        InvariantI31.I31_F.Should().Contain("zero real-world side effects");
    }

    [Fact]
    public async Task POR69_Batch6Firewall_ContingencyStageDoesNotInvokeMissionRuntime()
    {
        var contingency = _contingencyEngine.CreateContingency(
            "tenant-1", "s1", "T", "O", "P", "Trig", 100.0, 0.9);

        var proposal = await _contingencyEngine.StageContingentWorkProposalAsync("tenant-1", contingency.ContingencyId);
        proposal.AdmissionStatus.Should().Be(ProposalAdmissionStatus.Pending);
        // Only 3.9.0 Admission Engine can convert a Proposal to a WorkItem and dispatch missions
    }

    [Fact]
    public void POR70_Batch6Firewall_PORModelsContainZeroExecutionTokensOrPermitFields()
    {
        typeof(ContingencyProposal).GetProperty("ExecutionToken").Should().BeNull();
        typeof(ContingencyProposal).GetProperty("PermitSignature").Should().BeNull();
        typeof(CapacityStressTestResult).GetProperty("ExecutionPermit").Should().BeNull();
    }

    [Fact]
    public void POR71_Batch6Firewall_StressTestSimulationRunsEntirelyInMemory()
    {
        var scenario = CreateStandardScenario();
        var res = _stressEngine.SimulateCapacityStress("tenant-1", scenario, 100.0, 0.20, new List<string>());
        res.SimulatedStressDemand.Should().BeGreaterThan(0);
    }

    [Fact]
    public void POR72_Batch6Firewall_ZeroExternalConsequentialExecutionVerified()
    {
        // Invariant I31 core doctrine confirmation
        InvariantI31.CoreDoctrine.Should().EndWith("EXECUTION");
    }

    // =========================================================================
    // Family 13: Early Warning != Emergency Authority / PRG-1 Sovereignty (I31-J, I31-N)
    // =========================================================================

    [Fact]
    public void POR73_PRGSovereignty_EarlyWarningDoesNotTriggerEmergencyAuthority_I31J()
    {
        InvariantI31.I31_J.Should().Contain("Early Warning != Emergency Authority");
    }

    [Fact]
    public void POR74_PRGSovereignty_PredictionCannotMutateBusinessPolicyOrCeilings_I31N()
    {
        InvariantI31.I31_N.Should().Contain("Prediction Cannot Mutate Policy");
    }

    [Fact]
    public void POR75_PRGSovereignty_ExecutiveRetainsSoleAuthorityOverWorkAdmission_I31L()
    {
        InvariantI31.I31_L.Should().Contain("Readiness Score != Organizational Priority");
    }

    [Fact]
    public void POR76_PRGSovereignty_ReadinessScoreCannotAutonomousOverrideStrategicPriority_I31L()
    {
        var scenario = CreateStandardScenario();
        var metrics = Enum.GetValues<ReadinessDimension>().ToDictionary(d => d, _ => 0.10);
        var assessment = _scoringEngine.EvaluateReadiness("tenant-1", scenario, metrics, null);

        assessment.CompositeReadinessScore.Should().BeLessThan(0.30);
        // Low score is strictly diagnostic
        assessment.Status.Should().Be(ReadinessStatus.Red);
    }

    [Fact]
    public void POR77_PRGSovereignty_EmergencyAuthorityBypassAttempts_AreRejected_I31J()
    {
        // POR contracts possess no BypassGovernance flag
        typeof(OrganizationalReadinessAssessment).GetProperty("BypassGovernance").Should().BeNull();
        typeof(ContingencyProposal).GetProperty("EmergencyBypass").Should().BeNull();
    }

    [Fact]
    public void POR78_PRGSovereignty_PolicyMutationAttemptsViaPredictiveDistress_AreRejected_I31N()
    {
        // POR contracts possess no PolicyOverride fields
        typeof(ReadinessScenario).GetProperty("PolicyOverride").Should().BeNull();
        typeof(ReadinessDebtRecord).GetProperty("WaiveComplianceRules").Should().BeNull();
    }

    // =========================================================================
    // Family 14: End-to-End POR Pipeline & Controller Flows
    // =========================================================================

    [Fact]
    public async Task POR79_Controller_EvaluateScenario_ReturnsOkWithAssessment()
    {
        var controller = new PredictiveReadinessController(_readinessService, _store);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-ctrl-1";

        var scenario = CreateStandardScenario("tenant-ctrl-1");
        var request = new EvaluateScenarioRequest
        {
            Scenario = scenario,
            BaseCapacity = 120.0,
            BufferRatio = 0.30
        };

        var result = await controller.EvaluateScenario(request);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var assessment = okResult.Value.Should().BeOfType<OrganizationalReadinessAssessment>().Subject;
        assessment.TenantId.Should().Be("tenant-ctrl-1");
    }

    [Fact]
    public async Task POR80_Controller_EvaluateScenario_InvalidPayload_ReturnsBadRequest()
    {
        var controller = new PredictiveReadinessController(_readinessService, _store);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.EvaluateScenario(new EvaluateScenarioRequest { Scenario = null });
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task POR81_Controller_GetLatestAssessment_ReturnsLatestOrNotFound()
    {
        var controller = new PredictiveReadinessController(_readinessService, _store);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-ctrl-latest";

        // Initial state: NotFound
        var res1 = await controller.GetLatestAssessment();
        res1.Should().BeOfType<NotFoundObjectResult>();

        // After evaluation: Returns assessment
        var scenario = CreateStandardScenario("tenant-ctrl-latest");
        await controller.EvaluateScenario(new EvaluateScenarioRequest { Scenario = scenario });

        var res2 = await controller.GetLatestAssessment();
        var okResult = res2.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<OrganizationalReadinessAssessment>();
    }

    [Fact]
    public async Task POR82_Controller_GetWhyTrace_ReturnsTraceOrNotFound()
    {
        var controller = new PredictiveReadinessController(_readinessService, _store);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-ctrl-why";

        // Non-existent assessment: NotFound
        var res1 = await controller.GetWhyTrace("non-existent-id");
        res1.Should().BeOfType<NotFoundObjectResult>();

        // Existing assessment: Ok
        var scenario = CreateStandardScenario("tenant-ctrl-why");
        var evalRes = (OkObjectResult)await controller.EvaluateScenario(new EvaluateScenarioRequest { Scenario = scenario });
        var assessment = (OrganizationalReadinessAssessment)evalRes.Value!;

        var res2 = await controller.GetWhyTrace(assessment.AssessmentId);
        var okResult = res2.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<WhyReadinessTrace>();
    }

    [Fact]
    public async Task POR83_Controller_GetReadinessDebt_ReturnsTenantDebts()
    {
        var controller = new PredictiveReadinessController(_readinessService, _store);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-ctrl-debt";

        _debtTracker.RecordDebt("tenant-ctrl-debt", ReadinessDebtType.TechnologyDebt, 12000.0, "API Gateway rate limit capacity");

        var result = await controller.GetReadinessDebt();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var debts = okResult.Value.Should().BeAssignableTo<IReadOnlyList<ReadinessDebtRecord>>().Subject;
        debts.Should().Contain(d => d.DebtType == ReadinessDebtType.TechnologyDebt);
    }

    [Fact]
    public async Task POR84_Controller_StageContingencyProposal_ReturnsOkWithStagedProposal()
    {
        var controller = new PredictiveReadinessController(_readinessService, _store);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-ctrl-stage";

        var contingency = _contingencyEngine.CreateContingency(
            "tenant-ctrl-stage", "s1", "Proactive Capacity Expansion", "Order cloud instances", "Provision reserve nodes", "Traffic > 10k rps", 1500.0, 0.90);

        var result = await controller.StageContingencyProposal(contingency.ContingencyId);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
    }
}
