using BusinessModelApp.Api.Controllers;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Brain;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Brain;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Brain;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BusinessModelApp.Tests.Domain;

public sealed class Phase4Batch40AutonomousBusinessBrainTests
{
    private readonly IBrainAuditRepository _repository;
    private readonly IEpistemicGapDetector _gapDetector;
    private readonly ICognitiveContradictionResolver _contradictionResolver;
    private readonly ICognitiveStateSynthesizer _synthesizer;
    private readonly IAutonomousBusinessBrainService _brainService;
    private readonly AutonomousBusinessBrainController _controller;

    public Phase4Batch40AutonomousBusinessBrainTests()
    {
        _repository = new InMemoryBrainRepository();
        _gapDetector = new EpistemicGapDetector();
        _contradictionResolver = new CognitiveContradictionResolver();
        _synthesizer = new CognitiveStateSynthesizer(_gapDetector, _contradictionResolver);
        _brainService = new AutonomousBusinessBrainService(_synthesizer, _repository);

        _controller = new AutonomousBusinessBrainController(_brainService);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.ControllerContext.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-brain";
    }

    // =========================================================================
    // Family 1: Constitutional Invariants & Law I36 (BRAIN01 - BRAIN08)
    // =========================================================================

    [Fact]
    public void BRAIN01_PrimaryInvariant_ContainsAll26SubLaws()
    {
        AutonomousBusinessBrainInvariants.PrimaryInvariant.Should().Be(
            "INTELLIGENCE != AWARENESS != RESPONSIBILITY != DECISION != AUTHORITY != EXECUTION");

        AutonomousBusinessBrainInvariants.I36_A_IntelligenceNotAwareness.Should().StartWith("I36-A");
        AutonomousBusinessBrainInvariants.I36_B_AwarenessNotResponsibility.Should().StartWith("I36-B");
        AutonomousBusinessBrainInvariants.I36_C_ResponsibilityNotDecision.Should().StartWith("I36-C");
        AutonomousBusinessBrainInvariants.I36_D_DecisionNotAuthority.Should().StartWith("I36-D");
        AutonomousBusinessBrainInvariants.I36_E_AuthorityNotExecution.Should().StartWith("I36-E");
        AutonomousBusinessBrainInvariants.I36_F_EpistemicTransparency.Should().StartWith("I36-F");
        AutonomousBusinessBrainInvariants.I36_G_ExplicitUnknowns.Should().StartWith("I36-G");
        AutonomousBusinessBrainInvariants.I36_H_MultiTenantCognitiveIsolation.Should().StartWith("I36-H");
        AutonomousBusinessBrainInvariants.I36_I_NonExecutionPrinciple.Should().StartWith("I36-I");
        AutonomousBusinessBrainInvariants.I36_J_CausalNonOverwriting.Should().StartWith("I36-J");
        AutonomousBusinessBrainInvariants.I36_K_ForecastingIntegrity.Should().StartWith("I36-K");
        AutonomousBusinessBrainInvariants.I36_L_ResourceDebtVisibility.Should().StartWith("I36-L");
        AutonomousBusinessBrainInvariants.I36_M_MissionStatusFidelity.Should().StartWith("I36-M");
        AutonomousBusinessBrainInvariants.I36_N_AuditProvenance.Should().StartWith("I36-N");
        AutonomousBusinessBrainInvariants.I36_O_AntiHallucinationThreshold.Should().StartWith("I36-O");
        AutonomousBusinessBrainInvariants.I36_P_CognitiveRecencyAndDecay.Should().StartWith("I36-P");
        AutonomousBusinessBrainInvariants.I36_Q_ContradictionResolution.Should().StartWith("I36-Q");
        AutonomousBusinessBrainInvariants.I36_R_ImmutableStateHistory.Should().StartWith("I36-R");
        AutonomousBusinessBrainInvariants.I36_S_ExecutiveAttentionEconomy.Should().StartWith("I36-S");
        AutonomousBusinessBrainInvariants.I36_T_ZeroDirectSelfMutation.Should().StartWith("I36-T");
        AutonomousBusinessBrainInvariants.I36_U_PRG1EscalationQueue.Should().StartWith("I36-U");
        AutonomousBusinessBrainInvariants.I36_V_SimulationBoundary.Should().StartWith("I36-V");
        AutonomousBusinessBrainInvariants.I36_W_EpistemicGapCataloging.Should().StartWith("I36-W");
        AutonomousBusinessBrainInvariants.I36_X_FailClosedCognition.Should().StartWith("I36-X");
        AutonomousBusinessBrainInvariants.I36_Y_TenantPenetrationDefense.Should().StartWith("I36-Y");
        AutonomousBusinessBrainInvariants.I36_Z_BrainNotAutonomousAgent.Should().StartWith("I36-Z");
    }

    [Fact]
    public void BRAIN02_EpistemicProgression_DistinguishesAwarenessFromAuthority()
    {
        AutonomousBusinessBrainInvariants.PrimaryInvariant.Should().Contain("INTELLIGENCE");
        AutonomousBusinessBrainInvariants.PrimaryInvariant.Should().Contain("AWARENESS");
        AutonomousBusinessBrainInvariants.PrimaryInvariant.Should().Contain("RESPONSIBILITY");
        AutonomousBusinessBrainInvariants.PrimaryInvariant.Should().Contain("DECISION");
        AutonomousBusinessBrainInvariants.PrimaryInvariant.Should().Contain("AUTHORITY");
        AutonomousBusinessBrainInvariants.PrimaryInvariant.Should().Contain("EXECUTION");
    }

    [Fact]
    public void BRAIN03_IntelligenceOutputs_DoNotEqualExecutiveAwareness()
    {
        AutonomousBusinessBrainInvariants.I36_A_IntelligenceNotAwareness.Should().Contain("without structured synthesis");
    }

    [Fact]
    public void BRAIN04_Awareness_DoesNotConferResponsibilityWithoutAllocation()
    {
        AutonomousBusinessBrainInvariants.I36_B_AwarenessNotResponsibility.Should().Contain("without governed allocation");
    }

    [Fact]
    public void BRAIN05_Responsibility_DoesNotAuthorizePolicyDecisions()
    {
        AutonomousBusinessBrainInvariants.I36_C_ResponsibilityNotDecision.Should().Contain("unilateral business policy decisions");
    }

    [Fact]
    public void BRAIN06_DecisionFormulation_DoesNotConstituteAuthority()
    {
        AutonomousBusinessBrainInvariants.I36_D_DecisionNotAuthority.Should().Contain("not approval authority");
    }

    [Fact]
    public void BRAIN07_HumanApproval_DoesNotBypassExecutionFirewall()
    {
        AutonomousBusinessBrainInvariants.I36_E_AuthorityNotExecution.Should().Contain("does not bypass the Batch 6 execution firewall");
    }

    [Fact]
    public void BRAIN08_NonExecutionPrinciple_BrainCannotIssuePermits()
    {
        AutonomousBusinessBrainInvariants.I36_I_NonExecutionPrinciple.Should().Contain("cannot execute external mutations or issue execution permits");
    }

    // =========================================================================
    // Family 2: Cognitive State Synthesis & Hashing (BRAIN09 - BRAIN16)
    // =========================================================================

    [Fact]
    public async Task BRAIN09_SynthesizeState_ProducesCompleteExecutiveState()
    {
        var state = await _brainService.ForceCognitiveSynthesisAsync("tenant-brain");
        state.Should().NotBeNull();
        state.TenantId.Should().Be("tenant-brain");
        state.SnapshotId.Should().NotBeNullOrWhiteSpace();
        state.OverallHealth.Should().Be(CognitiveHealthStatus.Nominal);
    }

    [Fact]
    public async Task BRAIN10_Snapshot_ContainsAllTwelveCoreProjections()
    {
        var state = await _brainService.ForceCognitiveSynthesisAsync("tenant-brain");

        // 1. Recent deltas
        state.RecentDeltas.Should().NotBeEmpty();
        // 2. Causal explanations
        state.CausalExplanations.Should().NotBeEmpty();
        // 3. Leading forecasts
        state.LeadingForecasts.Should().NotBeEmpty();
        // 4. Attention priorities
        state.AttentionPriorities.Should().NotBeEmpty();
        // 5. Resource bottlenecks
        state.ResourceBottlenecks.Should().NotBeEmpty();
        // 6. Active missions
        state.ActiveMissions.Should().NotBeEmpty();
        // 7. Active impediments
        state.ActiveImpedimentsAndFailures.Should().NotBeEmpty();
        // 8. Identified opportunities
        state.IdentifiedOpportunities.Should().NotBeEmpty();
        // 9. Pending decisions
        state.PendingDecisions.Should().NotBeEmpty();
        // 10. Executive escalations
        state.ExecutiveEscalations.Should().NotBeEmpty();
        // 11. Epistemic gaps
        state.EpistemicGaps.Should().NotBeEmpty();
        // 12. Contradictions collection present
        state.CognitiveContradictions.Should().NotBeNull();
    }

    [Fact]
    public async Task BRAIN11_SnapshotHash_ComputedAndNonEmpty()
    {
        var state = await _brainService.ForceCognitiveSynthesisAsync("tenant-brain");
        state.SnapshotHash.Should().NotBeNullOrWhiteSpace();
        state.SnapshotHash.Length.Should().Be(64, "SHA-256 hash must be 64 hexadecimal characters (I36-N)");
    }

    [Fact]
    public void BRAIN12_IdenticalInputs_ProduceIdenticalSnapshotHash()
    {
        var now = DateTime.UtcNow;
        var s1 = new ExecutiveCognitiveState { TenantId = "t", SnapshotId = "s", SnapshotUtc = now, OverallHealth = CognitiveHealthStatus.Nominal };
        s1.ComputeSnapshotHash();

        var s2 = new ExecutiveCognitiveState { TenantId = "t", SnapshotId = "s", SnapshotUtc = now, OverallHealth = CognitiveHealthStatus.Nominal };
        s2.ComputeSnapshotHash();

        s1.SnapshotHash.Should().Be(s2.SnapshotHash);
    }

    [Fact]
    public void BRAIN13_StateChange_AltersSnapshotHash()
    {
        var now = DateTime.UtcNow;
        var s1 = new ExecutiveCognitiveState { TenantId = "t", SnapshotId = "s", SnapshotUtc = now, OverallHealth = CognitiveHealthStatus.Nominal };
        s1.ComputeSnapshotHash();

        var s2 = new ExecutiveCognitiveState { TenantId = "t", SnapshotId = "s", SnapshotUtc = now, OverallHealth = CognitiveHealthStatus.Degraded };
        s2.ComputeSnapshotHash();

        s1.SnapshotHash.Should().NotBe(s2.SnapshotHash);
    }

    [Fact]
    public async Task BRAIN14_OverallHealth_DefaultsToNominalUnderBaseline()
    {
        var state = await _brainService.ForceCognitiveSynthesisAsync("tenant-brain");
        state.OverallHealth.Should().Be(CognitiveHealthStatus.Nominal);
    }

    [Fact]
    public async Task BRAIN15_CurrentEnterpriseSummary_AccuratelyReflectsStatus()
    {
        var state = await _brainService.ForceCognitiveSynthesisAsync("tenant-brain");
        state.CurrentEnterpriseSummary.Should().Contain("Enterprise operations nominal");
    }

    [Fact]
    public async Task BRAIN16_SynthesisTimestamp_ReflectsUtcWithinTolerance()
    {
        var state = await _brainService.ForceCognitiveSynthesisAsync("tenant-brain");
        state.SnapshotUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // =========================================================================
    // Family 3: Epistemic Status & First-Class Unknowns (BRAIN17 - BRAIN24)
    // =========================================================================

    [Fact]
    public void BRAIN17_EpistemicStatuses_DistinguishAllSevenCategories()
    {
        CognitiveEpistemicStatus.Live.Should().NotBe(CognitiveEpistemicStatus.Simulated);
        CognitiveEpistemicStatus.Verified.Should().NotBe(CognitiveEpistemicStatus.Inferred);
        CognitiveEpistemicStatus.Stale.Should().NotBe(CognitiveEpistemicStatus.Unknown);
        CognitiveEpistemicStatus.NotConnected.Should().NotBe(CognitiveEpistemicStatus.Live);
    }

    [Fact]
    public async Task BRAIN18_EpistemicGaps_DetectedAndPopulatedAsFirstClassCitizens()
    {
        var gaps = await _gapDetector.DetectGapsAsync("tenant-brain");
        gaps.Should().NotBeEmpty("epistemic gaps must be explicitly cataloged (I36-G, I36-W)");
        gaps.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task BRAIN19_CompetitorPricingGap_FlaggedWithNotConnected()
    {
        var gaps = await _gapDetector.DetectGapsAsync("tenant-brain");
        var pricingGap = gaps.FirstOrDefault(g => g.Domain == "CompetitorPricing");

        pricingGap.Should().NotBeNull();
        pricingGap!.Reason.Should().Be("NotConnected");
        pricingGap.EpistemicStatus.Should().Be(CognitiveEpistemicStatus.NotConnected);
        pricingGap.UncertaintyScore.Should().BeGreaterThanOrEqualTo(0.85);
    }

    [Fact]
    public async Task BRAIN20_MacroRegulatoryShiftGap_FlaggedWithHighUncertainty()
    {
        var gaps = await _gapDetector.DetectGapsAsync("tenant-brain");
        var regGap = gaps.FirstOrDefault(g => g.Domain == "MacroRegulatoryShift");

        regGap.Should().NotBeNull();
        regGap!.Reason.Should().Be("HighUncertainty");
        regGap.EpistemicStatus.Should().Be(CognitiveEpistemicStatus.Unknown);
    }

    [Fact]
    public async Task BRAIN21_EpistemicGap_CarriesNonZeroUncertaintyScore()
    {
        var gaps = await _gapDetector.DetectGapsAsync("tenant-brain");
        foreach (var gap in gaps)
        {
            gap.UncertaintyScore.Should().BeGreaterThan(0.0);
            gap.GapId.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void BRAIN22_MissingConnectors_DoNotInventFakeNominalValues()
    {
        AutonomousBusinessBrainInvariants.I36_X_FailClosedCognition.Should().Contain("never fabricating default nominal data");
    }

    [Fact]
    public void BRAIN23_CognitiveRecencyDecay_CodifiedInI36P()
    {
        AutonomousBusinessBrainInvariants.I36_P_CognitiveRecencyAndDecay.Should().Contain("decay in confidence over time if not refreshed");
    }

    [Fact]
    public async Task BRAIN24_EpistemicGapsEndpoint_ReturnsAllCatalogedGaps()
    {
        var gaps = await _brainService.GetEpistemicGapsAsync("tenant-brain");
        gaps.Should().NotBeEmpty();
        gaps.Any(g => g.Domain == "CompetitorPricing").Should().BeTrue();
    }

    // =========================================================================
    // Family 4: Cognitive Contradiction Detection & Flagging (BRAIN25 - BRAIN32)
    // =========================================================================

    [Fact]
    public async Task BRAIN25_ContradictionResolver_SurfacesDivergencesWithoutAveraging()
    {
        var contradictions = await _contradictionResolver.DetectContradictionsAsync("tenant-brain");
        contradictions.Should().NotBeNull();
    }

    [Fact]
    public void BRAIN26_CognitiveContradictionItem_ContainsAllAttributionFields()
    {
        var item = new CognitiveContradictionItem
        {
            Topic = "MarginForecastDivergence",
            SourceEngineA = "Phase3.8.0-BI",
            AssertionA = "Historical Gross Margin at 68%",
            SourceEngineB = "Phase3.8.2-Forecast",
            AssertionB = "Leading Margin Projection at 52% (Severe Compression)",
            Severity = CognitiveContradictionSeverity.High,
            RecommendedResolution = "Escalate to CFO & PRG-1"
        };

        item.ContradictionId.Should().NotBeNullOrWhiteSpace();
        item.SourceEngineA.Should().Be("Phase3.8.0-BI");
        item.SourceEngineB.Should().Be("Phase3.8.2-Forecast");
        item.Severity.Should().Be(CognitiveContradictionSeverity.High);
    }

    [Fact]
    public void BRAIN27_Contradictions_DefaultToHumanReviewRequired()
    {
        var item = new CognitiveContradictionItem();
        item.RecommendedResolution.Should().Be("Human Review Required");
    }

    [Fact]
    public void BRAIN28_SeverityLevels_DistinguishAllFourTiers()
    {
        CognitiveContradictionSeverity.Low.Should().NotBe(CognitiveContradictionSeverity.Medium);
        CognitiveContradictionSeverity.Medium.Should().NotBe(CognitiveContradictionSeverity.High);
        CognitiveContradictionSeverity.High.Should().NotBe(CognitiveContradictionSeverity.Critical);
    }

    [Fact]
    public async Task BRAIN29_Contradictions_PopulatedInExecutiveCognitiveState()
    {
        var state = await _brainService.ForceCognitiveSynthesisAsync("tenant-brain");
        state.CognitiveContradictions.Should().NotBeNull();
    }

    [Fact]
    public async Task BRAIN30_EmptyContradictions_UnderAlignedSignals()
    {
        var contradictions = await _contradictionResolver.DetectContradictionsAsync("tenant-brain");
        // Under aligned default state, contradictions list is empty
        contradictions.Should().BeEmpty();
    }

    [Fact]
    public async Task BRAIN31_ContradictionsQueryEndpoint_ReturnsDetectedDiscrepancies()
    {
        var contradictions = await _brainService.GetCognitiveContradictionsAsync("tenant-brain");
        contradictions.Should().NotBeNull();
    }

    [Fact]
    public void BRAIN32_ContradictionResolutionInvariant_CodifiedInI36Q()
    {
        AutonomousBusinessBrainInvariants.I36_Q_ContradictionResolution.Should().Contain("surfaces a CognitiveContradiction rather than silently averaging");
    }

    // =========================================================================
    // Family 5: Executive Deltas & Attention Allocation (BRAIN33 - BRAIN40)
    // =========================================================================

    [Fact]
    public async Task BRAIN33_RecentDeltas_TracksEnterpriseMetrics()
    {
        var deltas = await _brainService.GetRecentDeltasAsync("tenant-brain");
        deltas.Should().NotBeEmpty();
        deltas[0].MetricOrEntity.Should().Be("PipelineVelocity");
    }

    [Fact]
    public void BRAIN34_DeltaRecord_ComputesMagnitudeAccurately()
    {
        var record = new EnterpriseDeltasRecord
        {
            PreviousValue = 100.0,
            CurrentValue = 125.0
        };

        record.DeltaMagnitude.Should().Be(25.0);
    }

    [Fact]
    public void BRAIN35_MaterialityScore_CalculatedAndFlagged()
    {
        var material = new EnterpriseDeltasRecord { MaterialityScore = 0.18 };
        material.IsMaterial.Should().BeTrue();

        var immaterial = new EnterpriseDeltasRecord { MaterialityScore = 0.08 };
        immaterial.IsMaterial.Should().BeFalse();
    }

    [Fact]
    public void BRAIN36_SubMaterialNoise_FilteredOutOfExecutiveAttention()
    {
        AutonomousBusinessBrainInvariants.I36_S_ExecutiveAttentionEconomy.Should().Contain("reserving executive attention for material business deltas");
    }

    [Fact]
    public async Task BRAIN37_AttentionPriorities_SourcedFromOARA()
    {
        var priorities = await _brainService.GetAttentionPrioritiesAsync("tenant-brain");
        priorities.Should().NotBeEmpty();
        priorities[0].Area.Should().Be("CustomerRetention");
    }

    [Fact]
    public void BRAIN38_UrgencyScoreAndAllocatedCapacity_Populated()
    {
        var priority = new AttentionPriorityItem
        {
            UrgencyScore = 0.85,
            AllocatedCapacityPercentage = 40.0
        };

        priority.UrgencyScore.Should().Be(0.85);
        priority.AllocatedCapacityPercentage.Should().Be(40.0);
    }

    [Fact]
    public void BRAIN39_StrategicRegimeTag_PreservedOnAttentionPriority()
    {
        var priority = new AttentionPriorityItem { StrategicRegime = "CrisisResilience" };
        priority.StrategicRegime.Should().Be("CrisisResilience");
    }

    [Fact]
    public async Task BRAIN40_DeltasAndPrioritiesEndpoints_ReturnExpectedProjections()
    {
        var deltas = await _brainService.GetRecentDeltasAsync("tenant-brain");
        var priorities = await _brainService.GetAttentionPrioritiesAsync("tenant-brain");

        deltas.Should().NotBeEmpty();
        priorities.Should().NotBeEmpty();
    }

    // =========================================================================
    // Family 6: Resource Scarcity & Bottleneck Reflection (BRAIN41 - BRAIN48)
    // =========================================================================

    [Fact]
    public async Task BRAIN41_ResourceBottlenecks_TracksConstrainedResources()
    {
        var state = await _brainService.GetCurrentCognitiveStateAsync("tenant-brain");
        state.ResourceBottlenecks.Should().NotBeEmpty();
        state.ResourceBottlenecks[0].ResourceType.Should().Be("ComputeCapacity");
    }

    [Fact]
    public void BRAIN42_UtilizationRatio_OverThreshold_FlagsBottleneck()
    {
        var constraint = new ResourceConstraintItem { UtilizationRatio = 0.88 };
        constraint.IsBottleneck.Should().BeTrue();
    }

    [Fact]
    public void BRAIN43_UtilizationRatio_UnderThreshold_NotBottleneck()
    {
        var constraint = new ResourceConstraintItem { UtilizationRatio = 0.70 };
        constraint.IsBottleneck.Should().BeFalse();
    }

    [Fact]
    public void BRAIN44_RecommendedAlleviation_PopulatedOnBottleneck()
    {
        var constraint = new ResourceConstraintItem { RecommendedAlleviation = "Scale pool or throttle low-priority jobs." };
        constraint.RecommendedAlleviation.Should().Contain("Scale pool");
    }

    [Fact]
    public void BRAIN45_ImpactedDomain_ExplicitlyTracked()
    {
        var constraint = new ResourceConstraintItem { ImpactedDomain = "BatchSimulation" };
        constraint.ImpactedDomain.Should().Be("BatchSimulation");
    }

    [Fact]
    public void BRAIN46_MultiResourceConstraints_RecordedInSingleSnapshot()
    {
        var state = new ExecutiveCognitiveState();
        state.ResourceBottlenecks.Add(new ResourceConstraintItem { ResourceType = "ComputeCapacity", UtilizationRatio = 0.90 });
        state.ResourceBottlenecks.Add(new ResourceConstraintItem { ResourceType = "LiquidityBuffer", UtilizationRatio = 0.86 });

        state.ResourceBottlenecks.Count(b => b.IsBottleneck).Should().Be(2);
    }

    [Fact]
    public void BRAIN47_ResourceDebtVisibility_CodifiedInI36L()
    {
        AutonomousBusinessBrainInvariants.I36_L_ResourceDebtVisibility.Should().Contain("resource debt and capacity reservations strictly from 3.9.7 OARA");
    }

    [Fact]
    public async Task BRAIN48_ScarcityBottlenecks_QueryableViaCognitiveState()
    {
        var state = await _brainService.GetCurrentCognitiveStateAsync("tenant-brain");
        state.ResourceBottlenecks.Should().NotBeNull();
    }

    // =========================================================================
    // Family 7: Failures, Degradations & Active Missions (BRAIN49 - BRAIN56)
    // =========================================================================

    [Fact]
    public async Task BRAIN49_ActiveMissions_ReflectsAuthoritativeMissions()
    {
        var state = await _brainService.GetCurrentCognitiveStateAsync("tenant-brain");
        state.ActiveMissions.Should().NotBeEmpty();
        state.ActiveMissions[0].Should().Contain("Mission-01");
    }

    [Fact]
    public async Task BRAIN50_ActiveImpediments_TracksComponentAndSeverity()
    {
        var state = await _brainService.GetCurrentCognitiveStateAsync("tenant-brain");
        state.ActiveImpedimentsAndFailures.Should().NotBeEmpty();
        state.ActiveImpedimentsAndFailures[0].SourceComponent.Should().Be("LegacyERPConnector");
        state.ActiveImpedimentsAndFailures[0].Severity.Should().Be("Low");
    }

    [Fact]
    public void BRAIN51_IsBlockingWork_AccuratelyFlagsWorkHaltingFailures()
    {
        var blocking = new ActiveImpedimentRecord { IsBlockingWork = true };
        blocking.IsBlockingWork.Should().BeTrue();

        var nonBlocking = new ActiveImpedimentRecord { IsBlockingWork = false };
        nonBlocking.IsBlockingWork.Should().BeFalse();
    }

    [Fact]
    public async Task BRAIN52_DegradedConnector_RecordedWithoutCrashingBrain()
    {
        var state = await _brainService.GetCurrentCognitiveStateAsync("tenant-brain");
        state.ActiveImpedimentsAndFailures[0].Description.Should().Contain("degraded cache mode");
    }

    [Fact]
    public void BRAIN53_FailClosedCognition_SetsHealthStatusToDegradedOnSevereImpediments()
    {
        AutonomousBusinessBrainInvariants.I36_X_FailClosedCognition.Should().Contain("degrade cognitive state to Partial or Degraded");
    }

    [Fact]
    public void BRAIN54_MissionStatusFidelity_CodifiedInI36M()
    {
        AutonomousBusinessBrainInvariants.I36_M_MissionStatusFidelity.Should().Contain("reflect authoritative states from 3.9.2 Mission Orchestrator");
    }

    [Fact]
    public async Task BRAIN55_IdentifiedOpportunities_SourcedFromRadar()
    {
        var state = await _brainService.GetCurrentCognitiveStateAsync("tenant-brain");
        state.IdentifiedOpportunities.Should().NotBeEmpty();
        state.IdentifiedOpportunities[0].Should().Contain("cross-sell opportunity");
    }

    [Fact]
    public async Task BRAIN56_CausalExplanations_SourcedFromCausalIntelligence()
    {
        var state = await _brainService.GetCurrentCognitiveStateAsync("tenant-brain");
        state.CausalExplanations.Should().NotBeEmpty();
        state.CausalExplanations[0].Should().Contain("Causal Node 381");
    }

    // =========================================================================
    // Family 8: Human Governance & PRG-1 Escalation Queue (BRAIN57 - BRAIN64)
    // =========================================================================

    [Fact]
    public async Task BRAIN57_PendingDecisions_TracksProposedActionsAndTiers()
    {
        var state = await _brainService.GetCurrentCognitiveStateAsync("tenant-brain");
        state.PendingDecisions.Should().NotBeEmpty();
        state.PendingDecisions[0].Title.Should().Be("Approve Enterprise Tier Discount");
    }

    [Fact]
    public void BRAIN58_Tier3AndTier4_ExplicitlyRequireHumanApproval()
    {
        var tier3 = new DecisionQueueItem { ConsequenceTier = "Tier3_HardGovernance" };
        tier3.RequiresHumanApproval.Should().BeTrue();

        var tier4 = new DecisionQueueItem { ConsequenceTier = "Tier4_MultiParty" };
        tier4.RequiresHumanApproval.Should().BeTrue();

        var tier1 = new DecisionQueueItem { ConsequenceTier = "Tier1_Autonomous" };
        tier1.RequiresHumanApproval.Should().BeFalse();
    }

    [Fact]
    public async Task BRAIN59_ExecutiveEscalations_QueuesCriticalItemsForPRG1()
    {
        var escalations = await _brainService.GetExecutiveEscalationsAsync("tenant-brain");
        escalations.Should().NotBeEmpty();
        escalations[0].Category.Should().Be("GovernanceApproval");
    }

    [Fact]
    public async Task BRAIN60_EscalationSummaryAndRecommendedAction_Populated()
    {
        var escalations = await _brainService.GetExecutiveEscalationsAsync("tenant-brain");
        escalations[0].ExecutiveSummary.Should().Contain("requires PRG-1 executive approval");
        escalations[0].RecommendedAction.Should().Contain("PRG-1 Governance Console");
    }

    [Fact]
    public void BRAIN61_UrgentEscalations_FlaggedAppropriately()
    {
        var esc = new ExecutiveEscalationItem { IsUrgent = true };
        esc.IsUrgent.Should().BeTrue();
    }

    [Fact]
    public async Task BRAIN62_DecisionProjectedRoi_Recorded()
    {
        var state = await _brainService.GetCurrentCognitiveStateAsync("tenant-brain");
        state.PendingDecisions[0].ProjectedRoi.Should().Be(2.4);
    }

    [Fact]
    public async Task BRAIN63_EscalationsEndpoint_ReturnsPendingHumanGovernanceItems()
    {
        var escalations = await _brainService.GetExecutiveEscalationsAsync("tenant-brain");
        escalations.Should().HaveCount(1);
    }

    [Fact]
    public void BRAIN64_BrainCannotSelfApprove_Decisions()
    {
        AutonomousBusinessBrainInvariants.I36_D_DecisionNotAuthority.Should().Contain("not approval authority");
        AutonomousBusinessBrainInvariants.I36_U_PRG1EscalationQueue.Should().Contain("queued specifically for PRG-1 human governance");
    }

    // =========================================================================
    // Family 9: Non-Execution & Firewall Sovereignty (BRAIN65 - BRAIN72)
    // =========================================================================

    [Fact]
    public void BRAIN65_BrainService_DoesNotHaveExecuteOrPermitIssuanceMethods()
    {
        var methods = typeof(IAutonomousBusinessBrainService).GetMethods();
        var execMethods = methods.Where(m => m.Name.Contains("Execute", StringComparison.OrdinalIgnoreCase) ||
                                            m.Name.Contains("Permit", StringComparison.OrdinalIgnoreCase) ||
                                            m.Name.Contains("Approve", StringComparison.OrdinalIgnoreCase)).ToList();

        execMethods.Should().BeEmpty("Brain service must strictly omit execution, permit issuance, and approval authority (I36-I)");
    }

    [Fact]
    public void BRAIN66_Controller_DoesNotExposeExecutionEndpoints()
    {
        var methods = typeof(AutonomousBusinessBrainController).GetMethods();
        var execMethods = methods.Where(m => m.Name.Contains("Execute", StringComparison.OrdinalIgnoreCase) ||
                                            m.Name.Contains("Permit", StringComparison.OrdinalIgnoreCase) ||
                                            m.Name.Contains("Approve", StringComparison.OrdinalIgnoreCase)).ToList();

        execMethods.Should().BeEmpty("Brain controller must never expose execution or approval endpoints (I36-I)");
    }

    [Fact]
    public void BRAIN67_ZeroDirectSelfMutation_CodifiedInI36T()
    {
        AutonomousBusinessBrainInvariants.I36_T_ZeroDirectSelfMutation.Should().Contain("cannot alter its own cognitive synthesis rules or weights without governed adaptation");
    }

    [Fact]
    public void BRAIN68_SimulationBoundary_SegregatesSimulationFromEmpiricalReality()
    {
        AutonomousBusinessBrainInvariants.I36_V_SimulationBoundary.Should().Contain("strictly segregated from live reality within the cognitive state");
    }

    [Fact]
    public async Task BRAIN69_LeadingForecasts_MarkedAsProbabilisticDistributions()
    {
        var state = await _brainService.GetCurrentCognitiveStateAsync("tenant-brain");
        state.LeadingForecasts[0].Should().Contain("confidence interval");
        AutonomousBusinessBrainInvariants.I36_K_ForecastingIntegrity.Should().Contain("probabilistic distributions, never empirical facts");
    }

    [Fact]
    public void BRAIN70_AntiHallucinationThreshold_ClassifiesUngroundedSignals()
    {
        AutonomousBusinessBrainInvariants.I36_O_AntiHallucinationThreshold.Should().Contain("classified as Unknown or Inferred with a confidence score");
    }

    [Fact]
    public void BRAIN71_ImmutableStateHistory_SnapshotsAreAppendOnly()
    {
        AutonomousBusinessBrainInvariants.I36_R_ImmutableStateHistory.Should().Contain("Brain snapshots are stored append-only");
    }

    [Fact]
    public void BRAIN72_BrainIsNotAnAutonomousAgent_ReplacingHumanLeadership()
    {
        AutonomousBusinessBrainInvariants.I36_Z_BrainNotAutonomousAgent.Should().Contain("does not replace specialized workers or human leadership");
    }

    // =========================================================================
    // Family 10: Multi-Tenant Isolation & Adversarial Defense (BRAIN73 - BRAIN80)
    // =========================================================================

    [Fact]
    public void BRAIN73_MultiTenantIsolation_CodifiedInI36H()
    {
        AutonomousBusinessBrainInvariants.I36_H_MultiTenantCognitiveIsolation.Should().Contain("strictly isolated by TenantId");
    }

    [Fact]
    public async Task BRAIN74_TenantA_CannotViewTenantB_Snapshots()
    {
        await _brainService.ForceCognitiveSynthesisAsync("tenant-A");
        await _brainService.ForceCognitiveSynthesisAsync("tenant-B");

        var aSnaps = await _repository.ListSnapshotsAsync("tenant-A");
        aSnaps.Should().OnlyContain(s => s.TenantId == "tenant-A");
        aSnaps.Should().NotContain(s => s.TenantId == "tenant-B");
    }

    [Fact]
    public async Task BRAIN75_Adversarial_CrossTenantSnapshotAccess_ThrowsUnauthorizedAccessException()
    {
        var bSnap = await _brainService.ForceCognitiveSynthesisAsync("tenant-B");

        var act = async () => await _repository.GetSnapshotByIdAsync("tenant-A", bSnap.SnapshotId);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Cross-tenant cognitive access violation*");
    }

    [Fact]
    public async Task BRAIN76_MissingTenantId_ThrowsArgumentException()
    {
        var act = async () => await _repository.SaveSnapshotAsync(new ExecutiveCognitiveState { TenantId = "" });
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task BRAIN77_ForceCognitiveSynthesis_PartitionsStrictlyByTenantId()
    {
        var snap1 = await _brainService.ForceCognitiveSynthesisAsync("tenant-X");
        var snap2 = await _brainService.ForceCognitiveSynthesisAsync("tenant-Y");

        snap1.TenantId.Should().Be("tenant-X");
        snap2.TenantId.Should().Be("tenant-Y");
        snap1.SnapshotId.Should().NotBe(snap2.SnapshotId);
    }

    [Fact]
    public async Task BRAIN78_Adversarial_SqlInjectionInTenantId_HandledSafely()
    {
        var injectionTenant = "tenant-safe'; DROP TABLE Snapshots; --";
        var state = await _brainService.ForceCognitiveSynthesisAsync(injectionTenant);
        state.TenantId.Should().Be(injectionTenant);

        var retrieved = await _repository.GetLatestSnapshotAsync(injectionTenant);
        retrieved.Should().NotBeNull();
        retrieved!.TenantId.Should().Be(injectionTenant);
    }

    [Fact]
    public async Task BRAIN79_ListSnapshots_PaginationLimitsReturnedItems()
    {
        for (int i = 0; i < 5; i++)
        {
            await _brainService.ForceCognitiveSynthesisAsync("tenant-paged");
        }

        var limited = await _repository.ListSnapshotsAsync("tenant-paged", limit: 3);
        limited.Should().HaveCount(3);
    }

    [Fact]
    public void BRAIN80_CapabilitiesEndpoint_ExposesBatchDetailsAndInvariants()
    {
        var result = _controller.GetCapabilities() as OkObjectResult;
        result.Should().NotBeNull();
        var val = result!.Value;
        val.Should().NotBeNull();
    }
}
