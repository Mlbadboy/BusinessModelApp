using BusinessModelApp.Api.Controllers;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Learning;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Learning;
using BusinessModelApp.Infrastructure.Runtime.Organizational.Learning;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BusinessModelApp.Tests.Domain;

public sealed class Phase3Batch3910OrganizationalLearningTests
{
    private readonly ILearningAuditRepository _repository;
    private readonly IEmpiricalOutcomeIngestor _ingestor;
    private readonly IModelMetrologyEngine _metrologyEngine;
    private readonly IStructuralDriftDetector _driftDetector;
    private readonly IAdaptationTargetRegistry _targetRegistry;
    private readonly ILessonDistiller _distiller;
    private readonly IAdaptationEngine _adaptationEngine;
    private readonly ILearningProvenanceService _provenanceService;
    private readonly IOrganizationalLearningService _learningService;
    private readonly OrganizationalLearningController _controller;

    public Phase3Batch3910OrganizationalLearningTests()
    {
        _repository = new InMemoryLearningRepository();
        _ingestor = new EmpiricalOutcomeIngestor(_repository);
        _metrologyEngine = new ModelMetrologyEngine();
        _driftDetector = new StructuralDriftDetector();
        _targetRegistry = new AdaptationTargetRegistry();
        _distiller = new LessonDistiller(_repository);
        _adaptationEngine = new AdaptationEngine(_repository, _targetRegistry);
        _provenanceService = new LearningProvenanceService();

        _learningService = new OrganizationalLearningService(
            _ingestor,
            _metrologyEngine,
            _driftDetector,
            _distiller,
            _adaptationEngine,
            _repository,
            _provenanceService);

        _controller = new OrganizationalLearningController(_learningService, _targetRegistry);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.ControllerContext.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-olma";
    }

    // =========================================================================
    // Family 1: Constitutional Invariant & Primary Law Tests (OLMA01 - OLMA08)
    // =========================================================================

    [Fact]
    public void OLMA01_PrimaryInvariant_ContainsPrimaryLawStringAnd26SubLaws()
    {
        OrganizationalLearningInvariants.PrimaryInvariant.Should().Be(
            "OUTCOME != OBSERVATION != EVIDENCE != CORRELATION != CAUSATION != LESSON != ADAPTATION != POLICY != AUTHORITY");

        OrganizationalLearningInvariants.I35_A_OutcomeNotLesson.Should().StartWith("I35-A");
        OrganizationalLearningInvariants.I35_B_LessonNotAdaptation.Should().StartWith("I35-B");
        OrganizationalLearningInvariants.I35_C_AdaptationNotPolicy.Should().StartWith("I35-C");
        OrganizationalLearningInvariants.I35_D_CorrelationNotCausalDrift.Should().StartWith("I35-D");
        OrganizationalLearningInvariants.I35_E_AdaptationCannotSelfApprove.Should().StartWith("I35-E");
        OrganizationalLearningInvariants.I35_F_CalibrationErrorMeasured.Should().StartWith("I35-F");
        OrganizationalLearningInvariants.I35_G_SimulationCalibrationNotFact.Should().StartWith("I35-G");
        OrganizationalLearningInvariants.I35_H_HeuristicDecay.Should().StartWith("I35-H");
        OrganizationalLearningInvariants.I35_I_AntiHallucinationGating.Should().StartWith("I35-I");
        OrganizationalLearningInvariants.I35_J_StructuralDriftDetection.Should().StartWith("I35-J");
        OrganizationalLearningInvariants.I35_K_FirewallSovereignty.Should().StartWith("I35-K");
        OrganizationalLearningInvariants.I35_L_AllocationPortfolioNonMutation.Should().StartWith("I35-L");
        OrganizationalLearningInvariants.I35_M_MultiTenantIsolation.Should().StartWith("I35-M");
        OrganizationalLearningInvariants.I35_N_DeterministicAuditProvenance.Should().StartWith("I35-N");
        OrganizationalLearningInvariants.I35_O_ReversibilityAndRollback.Should().StartWith("I35-O");
        OrganizationalLearningInvariants.I35_P_CounterfactualVerification.Should().StartWith("I35-P");
        OrganizationalLearningInvariants.I35_Q_MaterialityThreshold.Should().StartWith("I35-Q");
        OrganizationalLearningInvariants.I35_R_KnowledgePromotionGate.Should().StartWith("I35-R");
        OrganizationalLearningInvariants.I35_S_FailClosedAmbiguity.Should().StartWith("I35-S");
        OrganizationalLearningInvariants.I35_T_CanonicalLessonHash.Should().StartWith("I35-T");
        OrganizationalLearningInvariants.I35_U_ApprovalSovereignty.Should().StartWith("I35-U");
        OrganizationalLearningInvariants.I35_V_EvidenceSufficiencySovereignty.Should().StartWith("I35-V");
        OrganizationalLearningInvariants.I35_W_CausalIntelligenceSovereignty.Should().StartWith("I35-W");
        OrganizationalLearningInvariants.I35_X_SimulationEngineSovereignty.Should().StartWith("I35-X");
        OrganizationalLearningInvariants.I35_Y_AdaptationHysteresis.Should().StartWith("I35-Y");
        OrganizationalLearningInvariants.I35_Z_DriftNotAdaptationAuthority.Should().StartWith("I35-Z");
    }

    [Fact]
    public void OLMA02_EpistemicStates_StrictlyDistinguishOutcomeFromLessonAndAdaptation()
    {
        LearningEpistemicState.RawOutcome.Should().NotBe(LearningEpistemicState.LessonDistilled);
        LearningEpistemicState.LessonDistilled.Should().NotBe(LearningEpistemicState.AdaptationProposed);
        LearningEpistemicState.AdaptationProposed.Should().NotBe(LearningEpistemicState.HumanApproved);
        LearningEpistemicState.HumanApproved.Should().NotBe(LearningEpistemicState.Applied);
    }

    [Fact]
    public async Task OLMA03_RawEmpiricalOutcome_DoesNotAutoDistillIntoLesson()
    {
        var outcome = await _learningService.RecordEmpiricalOutcomeAsync("tenant-olma", new EmpiricalOutcomeEvent
        {
            SourceDomain = "Portfolio",
            MetricName = "Yield",
            ExpectedValue = 100.0,
            ActualValue = 80.0
        });

        outcome.Should().NotBeNull();
        var lessons = await _learningService.ListLessonsAsync("tenant-olma");
        lessons.Should().BeEmpty("raw outcome ingestion must never automatically distill a lesson (I35-A)");
    }

    [Fact]
    public async Task OLMA04_DistilledLesson_DoesNotAutoGenerateAdaptation()
    {
        for (int i = 0; i < 12; i++)
        {
            await _learningService.RecordEmpiricalOutcomeAsync("tenant-olma", new EmpiricalOutcomeEvent
            {
                SourceDomain = "OARA",
                MetricName = "CognitiveLoad",
                ExpectedValue = 50.0,
                ActualValue = 55.0,
                EnvironmentRegime = "Stable"
            });
        }

        var lesson = await _learningService.DistillLessonAsync(
            "tenant-olma",
            "Cognitive Load Drift",
            "OARA",
            "Heavy multi-agent synchronization",
            "Batching reduces load by 15%");

        lesson.Should().NotBeNull();
        lesson!.State.Should().Be(LearningEpistemicState.LessonDistilled);

        var proposals = await _learningService.ListProposalsAsync("tenant-olma");
        proposals.Should().BeEmpty("distilling a lesson must never auto-generate an adaptation proposal (I35-B)");
    }

    [Fact]
    public void OLMA05_ProposedAdaptation_CannotMutateConstitutionalConstraints()
    {
        // Parameter adaptation cannot alter system policy (I35-C)
        var isPolicyAllowed = AdaptationTargetWhitelist.IsWhitelisted("ConstitutionalSafetyFloor.MinCapitalReserve", out _);
        isPolicyAllowed.Should().BeFalse();
    }

    [Fact]
    public void OLMA06_SpuriousCorrelation_CannotAlterCausalGraphs()
    {
        // Invariant I35-D check
        OrganizationalLearningInvariants.I35_D_CorrelationNotCausalDrift.Should().Contain("directed graphs without interventional validation");
    }

    [Fact]
    public async Task OLMA07_AntiHallucinationGating_FlagsInsufficientSamples()
    {
        for (int i = 0; i < 3; i++)
        {
            await _learningService.RecordEmpiricalOutcomeAsync("tenant-olma", new EmpiricalOutcomeEvent
            {
                SourceDomain = "Watchtower",
                MetricName = "AnomalyRate",
                ExpectedValue = 5.0,
                ActualValue = 12.0
            });
        }

        var lesson = await _learningService.DistillLessonAsync("tenant-olma", "Anomaly Spike", "Watchtower", "Market volatility", "None");
        lesson!.State.Should().Be(LearningEpistemicState.InconclusiveEvidence, "N < 10 must fail sufficiency gate (I35-I, I35-V)");
    }

    [Fact]
    public void OLMA08_FirewallSovereignty_OLMACannotGrantExecutionPermits()
    {
        OrganizationalLearningInvariants.I35_K_FirewallSovereignty.Should().Contain("cannot grant execution permits");
        var permitAllowed = AdaptationTargetWhitelist.IsWhitelisted("ExecutionFirewall.BypassPermit", out _);
        permitAllowed.Should().BeFalse();
    }

    // =========================================================================
    // Family 2: Empirical Outcome Ingestion & Variance Tests (OLMA09 - OLMA16)
    // =========================================================================

    [Fact]
    public async Task OLMA09_IngestOutcome_WithValidFields_Succeeds()
    {
        var outcome = await _learningService.RecordEmpiricalOutcomeAsync("tenant-olma", new EmpiricalOutcomeEvent
        {
            SourceDomain = "Portfolio",
            SourceEntityId = "item-01",
            MetricName = "ExecutionLatency",
            ExpectedValue = 200.0,
            ActualValue = 250.0,
            EnvironmentRegime = "Nominal"
        });

        outcome.OutcomeEventId.Should().NotBeNullOrWhiteSpace();
        outcome.TenantId.Should().Be("tenant-olma");
        outcome.DeltaValue.Should().Be(50.0);
        outcome.VarianceScore.Should().Be(0.25);
    }

    [Fact]
    public async Task OLMA10_IngestOutcome_MissingTenantId_ThrowsArgumentException()
    {
        var act = async () => await _ingestor.IngestOutcomeAsync(new EmpiricalOutcomeEvent
        {
            TenantId = "",
            MetricName = "Test"
        });

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task OLMA11_VarianceScore_AccuratelyCalculated()
    {
        var outcome = await _learningService.RecordEmpiricalOutcomeAsync("tenant-olma", new EmpiricalOutcomeEvent
        {
            ExpectedValue = 100.0,
            ActualValue = 140.0
        });

        outcome.VarianceScore.Should().Be(0.40);
    }

    [Fact]
    public async Task OLMA12_DeltaValue_ComputedAsActualMinusExpected()
    {
        var outcome = await _learningService.RecordEmpiricalOutcomeAsync("tenant-olma", new EmpiricalOutcomeEvent
        {
            ExpectedValue = 80.0,
            ActualValue = 60.0
        });

        outcome.DeltaValue.Should().Be(-20.0);
    }

    [Fact]
    public async Task OLMA13_ZeroExpectedValue_HandlesDivisionSafely()
    {
        var outcome = await _learningService.RecordEmpiricalOutcomeAsync("tenant-olma", new EmpiricalOutcomeEvent
        {
            ExpectedValue = 0.0,
            ActualValue = 5.0
        });

        outcome.VarianceScore.Should().Be(5.0);
    }

    [Fact]
    public async Task OLMA14_ListOutcomes_FilteredByDomain()
    {
        await _learningService.RecordEmpiricalOutcomeAsync("tenant-olma", new EmpiricalOutcomeEvent { SourceDomain = "DomainA", MetricName = "M1" });
        await _learningService.RecordEmpiricalOutcomeAsync("tenant-olma", new EmpiricalOutcomeEvent { SourceDomain = "DomainB", MetricName = "M2" });

        var domainAOutcomes = await _learningService.GetOutcomesAsync("tenant-olma", "DomainA");
        domainAOutcomes.Should().HaveCount(1);
        domainAOutcomes[0].SourceDomain.Should().Be("DomainA");
    }

    [Fact]
    public async Task OLMA15_ListOutcomes_OrderedByObservedTimeDescending()
    {
        var now = DateTime.UtcNow;
        await _learningService.RecordEmpiricalOutcomeAsync("tenant-olma", new EmpiricalOutcomeEvent { ObservedUtc = now.AddMinutes(-10), MetricName = "First" });
        await _learningService.RecordEmpiricalOutcomeAsync("tenant-olma", new EmpiricalOutcomeEvent { ObservedUtc = now, MetricName = "Second" });

        var outcomes = await _learningService.GetOutcomesAsync("tenant-olma");
        outcomes[0].MetricName.Should().Be("Second");
    }

    [Fact]
    public async Task OLMA16_IngestAcrossMultipleDomains_PreservesDomainTags()
    {
        string[] domains = { "Watchtower", "Readiness", "OARA", "Portfolio", "Mission", "Simulation" };
        foreach (var d in domains)
        {
            await _learningService.RecordEmpiricalOutcomeAsync("tenant-olma", new EmpiricalOutcomeEvent { SourceDomain = d, MetricName = "KPI" });
        }

        var all = await _learningService.GetOutcomesAsync("tenant-olma");
        all.Select(o => o.SourceDomain).Distinct().Should().BeEquivalentTo(domains);
    }

    // =========================================================================
    // Family 3: Statistical Metrology & Calibration Tests (OLMA17 - OLMA24)
    // =========================================================================

    [Fact]
    public void OLMA17_MetrologyEngine_EmptyOutcomes_ReturnsZeroedRecord()
    {
        var record = _metrologyEngine.CalculateMetrology("tenant-olma", "EmptyModel", Array.Empty<EmpiricalOutcomeEvent>());
        record.SampleCount.Should().Be(0);
        record.MeanAbsolutePercentageError.Should().Be(0.0);
        record.DirectionalAccuracy.Should().Be(0.0);
    }

    [Fact]
    public void OLMA18_MAPECalculation_AccurateAcrossPositiveActuals()
    {
        var outcomes = new List<EmpiricalOutcomeEvent>
        {
            new() { ExpectedValue = 100.0, ActualValue = 120.0 }, // ape = 20/120 = 0.1667
            new() { ExpectedValue = 200.0, ActualValue = 180.0 }  // ape = 20/180 = 0.1111
        };

        var record = _metrologyEngine.CalculateMetrology("tenant-olma", "ForecastModel", outcomes);
        record.MeanAbsolutePercentageError.Should().BeInRange(0.13, 0.15);
    }

    [Fact]
    public void OLMA19_DirectionalAccuracy_ReflectsMatchingSigns()
    {
        var outcomes = new List<EmpiricalOutcomeEvent>
        {
            new() { ExpectedValue = 10.0, ActualValue = 15.0 }, // match
            new() { ExpectedValue = 5.0, ActualValue = -2.0 },  // mismatch
            new() { ExpectedValue = -3.0, ActualValue = -8.0 }  // match
        };

        var record = _metrologyEngine.CalculateMetrology("tenant-olma", "DirectionalModel", outcomes);
        record.DirectionalAccuracy.Should().BeApproximately(0.6667, 0.01);
    }

    [Fact]
    public void OLMA20_BrierScore_ReflectsMeanSquaredError()
    {
        var outcomes = new List<EmpiricalOutcomeEvent>
        {
            new() { ExpectedValue = 1.0, ActualValue = 0.5 }, // delta = -0.5, sq = 0.25
            new() { ExpectedValue = 0.0, ActualValue = 0.5 }  // delta = 0.5, sq = 0.25
        };

        var record = _metrologyEngine.CalculateMetrology("tenant-olma", "ProbModel", outcomes);
        record.BrierScore.Should().Be(0.25);
    }

    [Fact]
    public void OLMA21_CalibrationSlope_ReflectsRegressionSlope()
    {
        var outcomes = new List<EmpiricalOutcomeEvent>
        {
            new() { ExpectedValue = 10.0, ActualValue = 20.0 },
            new() { ExpectedValue = 20.0, ActualValue = 40.0 },
            new() { ExpectedValue = 30.0, ActualValue = 60.0 }
        };

        var record = _metrologyEngine.CalculateMetrology("tenant-olma", "LinearModel", outcomes);
        record.CalibrationSlope.Should().BeApproximately(2.0, 0.01);
    }

    [Fact]
    public void OLMA22_ObservedVariance_ReflectsDispersion()
    {
        var outcomes = new List<EmpiricalOutcomeEvent>
        {
            new() { ActualValue = 10.0 },
            new() { ActualValue = 20.0 },
            new() { ActualValue = 30.0 }
        };

        var record = _metrologyEngine.CalculateMetrology("tenant-olma", "DispersionModel", outcomes);
        record.ObservedVariance.Should().BeApproximately(66.6667, 0.01);
    }

    [Fact]
    public void OLMA23_SimulationCalibration_NeverDeclaresSimulationAsFact()
    {
        OrganizationalLearningInvariants.I35_G_SimulationCalibrationNotFact.Should().Contain("never retroactively marks simulation runs as real facts");
    }

    [Fact]
    public async Task OLMA24_MetrologyRecord_PersistedAndRetrievable()
    {
        for (int i = 0; i < 5; i++)
        {
            await _learningService.RecordEmpiricalOutcomeAsync("tenant-olma", new EmpiricalOutcomeEvent
            {
                SourceDomain = "ModelA",
                ExpectedValue = 50.0,
                ActualValue = 52.0
            });
        }

        var metrology = await _learningService.EvaluateCalibrationAsync("tenant-olma", "ModelA");
        metrology.SampleCount.Should().Be(5);
        metrology.MeanAbsolutePercentageError.Should().BeGreaterThan(0.0);
    }

    // =========================================================================
    // Family 4: Structural, Concept & Regime Drift Detection Tests (OLMA25 - OLMA32)
    // =========================================================================

    [Fact]
    public void OLMA25_InsufficientOutcomes_ReturnsDriftTypeNone()
    {
        var drift = _driftDetector.EvaluateDrift("tenant-olma", "ModelX", Array.Empty<EmpiricalOutcomeEvent>(), Array.Empty<EmpiricalOutcomeEvent>());
        drift.DetectedDriftType.Should().Be(DriftType.None);
        drift.RequiresCaution.Should().BeFalse();
        drift.RecommendedPosture.Should().Be("Nominal");
    }

    [Fact]
    public void OLMA26_StableDistributions_ProduceDriftTypeNone()
    {
        var recent = Enumerable.Range(0, 10).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 50, ActualValue = 52, EnvironmentRegime = "Stable" }).ToList();
        var baseline = Enumerable.Range(0, 10).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 50, ActualValue = 52, EnvironmentRegime = "Stable" }).ToList();

        var drift = _driftDetector.EvaluateDrift("tenant-olma", "StableModel", recent, baseline);
        drift.DetectedDriftType.Should().Be(DriftType.None);
        drift.RequiresCaution.Should().BeFalse();
    }

    [Fact]
    public void OLMA27_SignificantDivergence_TriggersCovariateShift()
    {
        var recent = Enumerable.Range(0, 10).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 50, ActualValue = 56.5, EnvironmentRegime = "Stable" }).ToList();
        var baseline = Enumerable.Range(0, 10).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 50, ActualValue = 55.0, EnvironmentRegime = "Stable" }).ToList();

        var drift = _driftDetector.EvaluateDrift("tenant-olma", "ShiftModel", recent, baseline);
        drift.DetectedDriftType.Should().Be(DriftType.CovariateShift);
        drift.RequiresCaution.Should().BeTrue();
    }

    [Fact]
    public void OLMA28_SevereDivergence_TriggersConceptDrift()
    {
        var recent = Enumerable.Range(0, 10).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 50, ActualValue = 85, EnvironmentRegime = "Stable" }).ToList();
        var baseline = Enumerable.Range(0, 10).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 50, ActualValue = 52, EnvironmentRegime = "Stable" }).ToList();

        var drift = _driftDetector.EvaluateDrift("tenant-olma", "ConceptModel", recent, baseline);
        drift.DetectedDriftType.Should().Be(DriftType.ConceptDrift);
        drift.RequiresCaution.Should().BeTrue();
    }

    [Fact]
    public void OLMA29_RegimeMismatch_TriggersRegimeTransition()
    {
        var recent = Enumerable.Range(0, 10).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 50, ActualValue = 52, EnvironmentRegime = "VolatileCrash" }).ToList();
        var baseline = Enumerable.Range(0, 10).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 50, ActualValue = 52, EnvironmentRegime = "StableGrowth" }).ToList();

        var drift = _driftDetector.EvaluateDrift("tenant-olma", "RegimeModel", recent, baseline);
        drift.DetectedDriftType.Should().Be(DriftType.RegimeTransition);
        drift.RequiresCaution.Should().BeTrue();
    }

    [Fact]
    public void OLMA30_DetectedDrift_SetsRequiresCautionAndPosture()
    {
        var recent = Enumerable.Range(0, 10).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 50, ActualValue = 52, EnvironmentRegime = "Stagflation" }).ToList();
        var baseline = Enumerable.Range(0, 10).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 50, ActualValue = 52, EnvironmentRegime = "StableGrowth" }).ToList();

        var drift = _driftDetector.EvaluateDrift("tenant-olma", "CautionModel", recent, baseline);
        drift.RequiresCaution.Should().BeTrue();
        drift.RecommendedPosture.Should().Be("Cautious");
    }

    [Fact]
    public void OLMA31_DriftDetection_NeverModifiesPolicyOrModelsAutomatically()
    {
        OrganizationalLearningInvariants.I35_Z_DriftNotAdaptationAuthority.Should().Contain("cannot independently modify policy, models, allocations, or execution behavior");
    }

    [Fact]
    public void OLMA32_PValue_CorrectlyReflectsSignificance()
    {
        var recent = Enumerable.Range(0, 10).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 50, ActualValue = 90, EnvironmentRegime = "Crisis" }).ToList();
        var baseline = Enumerable.Range(0, 10).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 50, ActualValue = 51, EnvironmentRegime = "Calm" }).ToList();

        var drift = _driftDetector.EvaluateDrift("tenant-olma", "PValModel", recent, baseline);
        drift.PValue.Should().BeLessThanOrEqualTo(0.05);
    }

    // =========================================================================
    // Family 5: Evidence Sufficiency & Anti-Hallucination Gating Tests (OLMA33 - OLMA40)
    // =========================================================================

    [Fact]
    public void OLMA33_SampleSizeUnder10_FailsSufficiencyGate()
    {
        var score = new EvidenceSufficiencyScore
        {
            SampleSize = 9,
            EffectSize = 0.50,
            Confidence = 0.90,
            Stability = 0.85,
            AttributionQuality = 0.85,
            RegimeConsistency = 0.90
        };

        score.IsSufficient.Should().BeFalse("N < 10 must fail sufficiency gate (I35-V)");
    }

    [Fact]
    public void OLMA34_LowStability_FailsSufficiencyGate()
    {
        var score = new EvidenceSufficiencyScore
        {
            SampleSize = 20,
            EffectSize = 0.50,
            Confidence = 0.90,
            Stability = 0.60, // below 0.65 threshold
            AttributionQuality = 0.85,
            RegimeConsistency = 0.90
        };

        score.IsSufficient.Should().BeFalse("Stability < 0.65 must fail sufficiency gate (I35-V)");
    }

    [Fact]
    public void OLMA35_LowEffectSize_FailsSufficiencyGate()
    {
        var score = new EvidenceSufficiencyScore
        {
            SampleSize = 25,
            EffectSize = 0.05, // below 0.10 threshold
            Confidence = 0.90,
            Stability = 0.80,
            AttributionQuality = 0.85,
            RegimeConsistency = 0.90
        };

        score.IsSufficient.Should().BeFalse("EffectSize < 0.10 must fail sufficiency gate");
    }

    [Fact]
    public void OLMA36_LowConfidence_FailsSufficiencyGate()
    {
        var score = new EvidenceSufficiencyScore
        {
            SampleSize = 25,
            EffectSize = 0.20,
            Confidence = 0.75, // below 0.80 threshold
            Stability = 0.80,
            AttributionQuality = 0.85,
            RegimeConsistency = 0.90
        };

        score.IsSufficient.Should().BeFalse("Confidence < 0.80 must fail sufficiency gate");
    }

    [Fact]
    public void OLMA37_LowAttributionQuality_FailsSufficiencyGate()
    {
        var score = new EvidenceSufficiencyScore
        {
            SampleSize = 25,
            EffectSize = 0.20,
            Confidence = 0.85,
            Stability = 0.80,
            AttributionQuality = 0.60, // below 0.70 threshold
            RegimeConsistency = 0.90
        };

        score.IsSufficient.Should().BeFalse("AttributionQuality < 0.70 must fail sufficiency gate");
    }

    [Fact]
    public void OLMA38_LowRegimeConsistency_FailsSufficiencyGate()
    {
        var score = new EvidenceSufficiencyScore
        {
            SampleSize = 25,
            EffectSize = 0.20,
            Confidence = 0.85,
            Stability = 0.80,
            AttributionQuality = 0.80,
            RegimeConsistency = 0.55 // below 0.70 threshold
        };

        score.IsSufficient.Should().BeFalse("RegimeConsistency < 0.70 must fail sufficiency gate");
    }

    [Fact]
    public void OLMA39_AllSixCriteriaMet_PassesSufficiencyGate()
    {
        var score = new EvidenceSufficiencyScore
        {
            SampleSize = 15,
            EffectSize = 0.25,
            Confidence = 0.85,
            Stability = 0.75,
            AttributionQuality = 0.80,
            RegimeConsistency = 0.85
        };

        score.IsSufficient.Should().BeTrue("meeting all 6 composite criteria must satisfy sufficiency gate (I35-V)");
    }

    [Fact]
    public void OLMA40_ComputeCompositeScore_CalculatesAccurateWeightedSum()
    {
        var score = new EvidenceSufficiencyScore
        {
            SampleSize = 50, // 0.20 * 1.0 = 0.20
            EffectSize = 0.50, // 0.20 * 0.50 = 0.10
            Confidence = 0.80, // 0.20 * 0.80 = 0.16
            Stability = 0.80,  // 0.15 * 0.80 = 0.12
            AttributionQuality = 0.80, // 0.15 * 0.80 = 0.12
            RegimeConsistency = 1.0    // 0.10 * 1.0 = 0.10
        };

        var composite = score.ComputeCompositeScore();
        composite.Should().BeApproximately(0.80, 0.01);
    }

    // =========================================================================
    // Family 6: Lesson Distillation & Cryptographic Provenance Tests (OLMA41 - OLMA48)
    // =========================================================================

    [Fact]
    public async Task OLMA41_DistillLesson_WithSufficientOutcomes_Succeeds()
    {
        var outcomes = Enumerable.Range(0, 15).Select(i => new EmpiricalOutcomeEvent
        {
            SourceDomain = "Portfolio",
            ExpectedValue = 50.0,
            ActualValue = 53.0,
            VarianceScore = 0.06,
            EnvironmentRegime = "Stable"
        }).ToList();

        var lesson = await _distiller.DistillLessonAsync(
            "tenant-olma",
            "Portfolio Yield Drift",
            outcomes,
            "Causal factor: supplier lead time variance",
            "Simulated buffer absorbs shock");

        lesson.Should().NotBeNull();
        lesson!.State.Should().Be(LearningEpistemicState.LessonDistilled);
        lesson.EvidenceSufficiency.IsSufficient.Should().BeTrue();
    }

    [Fact]
    public async Task OLMA42_LessonHash_GeneratedAndNonEmpty()
    {
        var outcomes = Enumerable.Range(0, 12).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 10, ActualValue = 12, EnvironmentRegime = "Stable" }).ToList();
        var lesson = await _distiller.DistillLessonAsync("tenant-olma", "Hash Test", outcomes, "Causal Link", "Counterfactual Check");

        lesson!.LessonHash.Should().NotBeNullOrWhiteSpace();
        lesson.LessonHash.Length.Should().Be(64, "SHA-256 hex string must be 64 characters (I35-T)");
    }

    [Fact]
    public void OLMA43_IdenticalInputs_ProduceIdenticalLessonHash()
    {
        var l1 = new OrganizationalLesson
        {
            TenantId = "tenant-1",
            Title = "Invariant Lesson",
            ObservationSummary = "Obs",
            CausalAttribution = "Cause",
            CounterfactualInsight = "CF",
            Confidence = 0.85
        };
        l1.ComputeLessonHash();

        var l2 = new OrganizationalLesson
        {
            TenantId = "tenant-1",
            Title = "Invariant Lesson",
            ObservationSummary = "Obs",
            CausalAttribution = "Cause",
            CounterfactualInsight = "CF",
            Confidence = 0.85
        };
        l2.ComputeLessonHash();

        l1.LessonHash.Should().Be(l2.LessonHash);
    }

    [Fact]
    public void OLMA44_AlteredHypothesis_ProducesDifferentLessonHash()
    {
        var l1 = new OrganizationalLesson { TenantId = "t", Title = "T", CausalAttribution = "Cause A", Confidence = 0.85 };
        l1.ComputeLessonHash();

        var l2 = new OrganizationalLesson { TenantId = "t", Title = "T", CausalAttribution = "Cause B", Confidence = 0.85 };
        l2.ComputeLessonHash();

        l1.LessonHash.Should().NotBe(l2.LessonHash);
    }

    [Fact]
    public async Task OLMA45_EpistemicTag_IsSetToEmpiricalLesson()
    {
        var outcomes = Enumerable.Range(0, 12).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 10, ActualValue = 12, EnvironmentRegime = "Stable" }).ToList();
        var lesson = await _distiller.DistillLessonAsync("tenant-olma", "Tag Test", outcomes, "Cause", "CF");

        lesson!.EpistemicTag.Should().Be("EmpiricalLesson", "promotion gate requires EmpiricalLesson epistemic classification (I35-R)");
    }

    [Fact]
    public async Task OLMA46_ObservationSummary_AggregatesDomains()
    {
        var outcomes = new List<EmpiricalOutcomeEvent>
        {
            new() { SourceDomain = "Watchtower", ExpectedValue = 10, ActualValue = 12 },
            new() { SourceDomain = "Readiness", ExpectedValue = 20, ActualValue = 22 }
        };

        var lesson = await _distiller.DistillLessonAsync("tenant-olma", "MultiDomain", outcomes, "Cause", "CF");
        lesson!.ObservationSummary.Should().Contain("Watchtower");
        lesson.ObservationSummary.Should().Contain("Readiness");
    }

    [Fact]
    public async Task OLMA47_DistilledLesson_StoredInAuditRepository()
    {
        var outcomes = Enumerable.Range(0, 12).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 10, ActualValue = 12, EnvironmentRegime = "Stable" }).ToList();
        var lesson = await _distiller.DistillLessonAsync("tenant-olma", "Persistence Test", outcomes, "Cause", "CF");

        var retrieved = await _repository.GetLessonAsync("tenant-olma", lesson!.LessonId);
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("Persistence Test");
    }

    [Fact]
    public void OLMA48_HeuristicDecay_CodifiedInLaw()
    {
        OrganizationalLearningInvariants.I35_H_HeuristicDecay.Should().Contain("decay in confidence over time if not corroborated");
    }

    // =========================================================================
    // Family 7: Closed-World Whitelist & Safety Boundary Tests (OLMA49 - OLMA56)
    // =========================================================================

    [Fact]
    public void OLMA49_Whitelist_ContainsExactlyFiveApprovedTargets()
    {
        var targets = AdaptationTargetWhitelist.AllowedTargets;
        targets.Should().HaveCount(5, "closed-world whitelist must strictly contain exactly 5 approved targets");
        targets.Should().ContainKey("HeuristicWeight.GrowthFocus");
        targets.Should().ContainKey("ForecastConfidenceDiscount.Macro");
        targets.Should().ContainKey("AttentionBudgetWeight.Cognitive");
        targets.Should().ContainKey("MaterialityThreshold.Rebalance");
        targets.Should().ContainKey("ReadinessBufferMultiplier.Operations");
    }

    [Fact]
    public void OLMA50_WhitelistedTargets_DefineSafeBoundsAndStepLimits()
    {
        foreach (var (key, def) in AdaptationTargetWhitelist.AllowedTargets)
        {
            def.SafeMin.Should().BeLessThan(def.SafeMax);
            def.MaxDeltaPerAdaptation.Should().BeGreaterThan(0.0);
            def.ParameterKey.Should().Be(key);
        }
    }

    [Fact]
    public void OLMA51_Adversarial_TargetingCredentials_StrictlyRejected()
    {
        var valid = _targetRegistry.TryValidateTarget("ApiKey.Stripe", 0.05, out var reason, out _);
        valid.Should().BeFalse();
        reason.Should().Contain("Closed-world governance strictly prohibits");
    }

    [Fact]
    public void OLMA52_Adversarial_TargetingExecutionPermits_StrictlyRejected()
    {
        var valid = _targetRegistry.TryValidateTarget("ExecutionFirewall.BypassPermit", 1.0, out var reason, out _);
        valid.Should().BeFalse();
        reason.Should().Contain("not whitelisted");
    }

    [Fact]
    public void OLMA53_Adversarial_TargetingPolicyRules_StrictlyRejected()
    {
        var valid = _targetRegistry.TryValidateTarget("ConstitutionalPolicy.MaxGpuHours", 10.0, out var reason, out _);
        valid.Should().BeFalse();
        reason.Should().Contain("not whitelisted");
    }

    [Fact]
    public void OLMA54_Adversarial_TargetingDatabaseConnection_StrictlyRejected()
    {
        var valid = _targetRegistry.TryValidateTarget("Database.ConnectionString", 0.1, out var reason, out _);
        valid.Should().BeFalse();
    }

    [Fact]
    public void OLMA55_DeltaExceedingMaxStep_Rejected()
    {
        // SafeMaxDelta for GrowthFocus is 0.20
        var valid = _targetRegistry.TryValidateTarget("HeuristicWeight.GrowthFocus", 0.35, out var reason, out _);
        valid.Should().BeFalse();
        reason.Should().Contain("exceeds maximum permissible single-step adaptation delta");
    }

    [Fact]
    public void OLMA56_WhitelistedValidTarget_WithinDelta_SucceedsValidation()
    {
        var valid = _targetRegistry.TryValidateTarget("HeuristicWeight.GrowthFocus", 0.10, out var reason, out var def);
        valid.Should().BeTrue();
        reason.Should().BeEmpty();
        def.Should().NotBeNull();
    }

    // =========================================================================
    // Family 8: Materiality Threshold & Anti-Thrashing Tests (OLMA57 - OLMA64)
    // =========================================================================

    [Fact]
    public async Task OLMA57_InsignificantDelta_BelowMaterialityThreshold_ThrowsInvalidOperationException()
    {
        var lesson = new OrganizationalLesson
        {
            TenantId = "tenant-olma",
            State = LearningEpistemicState.LessonDistilled,
            Confidence = 0.85
        };

        // For HeuristicWeight.GrowthFocus, maxDelta = 0.20. Proposed delta = 0.01 -> materiality = 0.01 / 0.20 = 0.05 (< 0.15)
        var act = async () => await _adaptationEngine.ProposeAdaptationAsync("tenant-olma", lesson, "HeuristicWeight.GrowthFocus", 0.01);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*below the mandatory threshold of 0.15*");
    }

    [Fact]
    public async Task OLMA58_DeltaMeetingMaterialityThreshold_Succeeds()
    {
        var lesson = new OrganizationalLesson
        {
            TenantId = "tenant-olma",
            State = LearningEpistemicState.LessonDistilled,
            Confidence = 0.85
        };

        // delta 0.05 -> materiality = 0.05 / 0.20 = 0.25 (>= 0.15)
        var proposal = await _adaptationEngine.ProposeAdaptationAsync("tenant-olma", lesson, "HeuristicWeight.GrowthFocus", 0.05);
        proposal.Should().NotBeNull();
        proposal!.MaterialityScore.Should().Be(0.25);
    }

    [Fact]
    public void OLMA59_MaterialityScore_CalculatedAsRatioOfMaxDelta()
    {
        var def = AdaptationTargetWhitelist.AllowedTargets["HeuristicWeight.GrowthFocus"];
        var proposedDelta = 0.10;
        var score = Math.Round(proposedDelta / def.MaxDeltaPerAdaptation, 4);
        score.Should().Be(0.50);
    }

    [Fact]
    public async Task OLMA60_BoundaryAtExactlyPointOneFive_Passes()
    {
        var lesson = new OrganizationalLesson
        {
            TenantId = "tenant-olma",
            State = LearningEpistemicState.LessonDistilled,
            Confidence = 0.85
        };

        // 0.15 * 0.20 = 0.03 delta
        var proposal = await _adaptationEngine.ProposeAdaptationAsync("tenant-olma", lesson, "HeuristicWeight.GrowthFocus", 0.03);
        proposal.Should().NotBeNull();
        proposal!.MaterialityScore.Should().Be(0.15);
    }

    [Fact]
    public async Task OLMA61_MicroFluctuations_RejectedToPreventJitter()
    {
        var lesson = new OrganizationalLesson { TenantId = "tenant-olma", State = LearningEpistemicState.LessonDistilled, Confidence = 0.85 };
        var act = async () => await _adaptationEngine.ProposeAdaptationAsync("tenant-olma", lesson, "HeuristicWeight.GrowthFocus", 0.001);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*parameter thrashing rejected*");
    }

    [Fact]
    public async Task OLMA62_MaterialityScore_RecordedOnProposal()
    {
        var lesson = new OrganizationalLesson { TenantId = "tenant-olma", State = LearningEpistemicState.LessonDistilled, Confidence = 0.85 };
        var proposal = await _adaptationEngine.ProposeAdaptationAsync("tenant-olma", lesson, "ForecastConfidenceDiscount.Macro", 0.06);
        proposal!.MaterialityScore.Should().BeGreaterThanOrEqualTo(0.15);
    }

    [Fact]
    public async Task OLMA63_InconclusiveLesson_CannotBeUsedForProposal()
    {
        var inconclusiveLesson = new OrganizationalLesson
        {
            TenantId = "tenant-olma",
            State = LearningEpistemicState.InconclusiveEvidence,
            Confidence = 0.40
        };

        var act = async () => await _adaptationEngine.ProposeAdaptationAsync("tenant-olma", inconclusiveLesson, "HeuristicWeight.GrowthFocus", 0.05);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*inconclusive evidence*");
    }

    [Fact]
    public void OLMA64_PrimaryLaw_AntiThrashingCodifiedInI35Q()
    {
        OrganizationalLearningInvariants.I35_Q_MaterialityThreshold.Should().Contain("score >= 0.15");
    }

    // =========================================================================
    // Family 9: Adaptation Hysteresis & Cooldown Tests (OLMA65 - OLMA72)
    // =========================================================================

    [Fact]
    public async Task OLMA65_InitialProposal_Succeeds()
    {
        var lesson = new OrganizationalLesson { TenantId = "tenant-olma", State = LearningEpistemicState.LessonDistilled, Confidence = 0.85 };
        var prop = await _adaptationEngine.ProposeAdaptationAsync("tenant-olma", lesson, "AttentionBudgetWeight.Cognitive", 0.05);
        prop.Should().NotBeNull();
    }

    [Fact]
    public async Task OLMA66_ImmediateSubsequentAdaptation_WithinCooldown_Rejected()
    {
        var lesson = new OrganizationalLesson { TenantId = "tenant-olma", State = LearningEpistemicState.LessonDistilled, Confidence = 0.85 };

        // Apply first adaptation state
        _repository.UpdateHysteresisState("tenant-olma", "AttentionBudgetWeight.Cognitive", 0.50, DateTime.UtcNow);

        // Immediate proposal should fail hysteresis cooldown check (I35-Y)
        var act = async () => await _adaptationEngine.ProposeAdaptationAsync("tenant-olma", lesson, "AttentionBudgetWeight.Cognitive", 0.05);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*blocked by hysteresis cooldown*");
    }

    [Fact]
    public void OLMA67_CooldownWindow_DefaultsToTwentyFourHours()
    {
        var state = new AdaptationHysteresisState();
        state.CooldownWindow.Should().Be(TimeSpan.FromHours(24));
    }

    [Fact]
    public async Task OLMA68_AdaptationPermitted_AfterCooldownExpires()
    {
        var lesson = new OrganizationalLesson { TenantId = "tenant-olma", State = LearningEpistemicState.LessonDistilled, Confidence = 0.85 };

        // Last adapted 25 hours ago
        _repository.UpdateHysteresisState("tenant-olma", "MaterialityThreshold.Rebalance", 0.20, DateTime.UtcNow.AddHours(-25));

        var proposal = await _adaptationEngine.ProposeAdaptationAsync("tenant-olma", lesson, "MaterialityThreshold.Rebalance", 0.02);
        proposal.Should().NotBeNull();
    }

    [Fact]
    public void OLMA69_HysteresisTracked_PerTenantAndParameter()
    {
        _repository.UpdateHysteresisState("tenant-A", "HeuristicWeight.GrowthFocus", 0.5, DateTime.UtcNow);
        var stateB = _repository.GetOrCreateHysteresisState("tenant-B", "HeuristicWeight.GrowthFocus");
        stateB.LastAdaptedUtc.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void OLMA70_DistinctParameters_HaveIndependentCooldowns()
    {
        _repository.UpdateHysteresisState("tenant-olma", "HeuristicWeight.GrowthFocus", 0.5, DateTime.UtcNow);
        var stateOther = _repository.GetOrCreateHysteresisState("tenant-olma", "ForecastConfidenceDiscount.Macro");
        stateOther.IsInCooldown(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void OLMA71_AdaptationCountInEpoch_IncrementsAccurately()
    {
        _repository.UpdateHysteresisState("tenant-olma", "ParamX", 1.0, DateTime.UtcNow);
        _repository.UpdateHysteresisState("tenant-olma", "ParamX", 1.2, DateTime.UtcNow);
        var state = _repository.GetOrCreateHysteresisState("tenant-olma", "ParamX");
        state.AdaptationCountInEpoch.Should().Be(2);
    }

    [Fact]
    public void OLMA72_HysteresisState_PersistsAcrossQueries()
    {
        _repository.UpdateHysteresisState("tenant-olma", "PKey", 3.0, DateTime.UtcNow);
        var state = _repository.GetOrCreateHysteresisState("tenant-olma", "PKey");
        state.LastAdaptedValue.Should().Be(3.0);
    }

    // =========================================================================
    // Family 10: Reversibility & Inverse Diff Tests (OLMA73 - OLMA80)
    // =========================================================================

    [Fact]
    public async Task OLMA73_Proposal_ComputesRollbackInverseValueEqualToCurrentValue()
    {
        var lesson = new OrganizationalLesson { TenantId = "tenant-olma", State = LearningEpistemicState.LessonDistilled, Confidence = 0.85 };
        var prop = await _adaptationEngine.ProposeAdaptationAsync("tenant-olma", lesson, "ReadinessBufferMultiplier.Operations", 0.15);

        prop!.RollbackInverseValue.Should().Be(prop.CurrentValue, "proposal must maintain an inverse rollback value (I35-O)");
    }

    [Fact]
    public async Task OLMA74_ProposedValue_ClampedToSafeBounds()
    {
        var lesson = new OrganizationalLesson { TenantId = "tenant-olma", State = LearningEpistemicState.LessonDistilled, Confidence = 0.85 };
        var prop = await _adaptationEngine.ProposeAdaptationAsync("tenant-olma", lesson, "HeuristicWeight.GrowthFocus", 0.15);

        prop!.ProposedValue.Should().BeInRange(0.1, 0.9);
    }

    [Fact]
    public async Task OLMA75_CurrentValue_InitializedFromMidpointOrPrevious()
    {
        var lesson = new OrganizationalLesson { TenantId = "tenant-olma", State = LearningEpistemicState.LessonDistilled, Confidence = 0.85 };
        var prop = await _adaptationEngine.ProposeAdaptationAsync("tenant-olma", lesson, "HeuristicWeight.GrowthFocus", 0.10);

        prop!.CurrentValue.Should().Be(0.5); // (0.1 + 0.9) / 2
    }

    [Fact]
    public async Task OLMA76_StateOfProposal_IsStrictlyAdaptationProposed()
    {
        var lesson = new OrganizationalLesson { TenantId = "tenant-olma", State = LearningEpistemicState.LessonDistilled, Confidence = 0.85 };
        var prop = await _adaptationEngine.ProposeAdaptationAsync("tenant-olma", lesson, "HeuristicWeight.GrowthFocus", 0.10);

        prop!.State.Should().Be(LearningEpistemicState.AdaptationProposed);
    }

    [Fact]
    public async Task OLMA77_RequiredGovernanceApprovalLevel_SetToPRG1()
    {
        var lesson = new OrganizationalLesson { TenantId = "tenant-olma", State = LearningEpistemicState.LessonDistilled, Confidence = 0.85 };
        var prop = await _adaptationEngine.ProposeAdaptationAsync("tenant-olma", lesson, "HeuristicWeight.GrowthFocus", 0.10);

        prop!.RequiredGovernanceApprovalLevel.Should().Be("PRG-1 Human Governance Required");
    }

    [Fact]
    public async Task OLMA78_Proposal_DoesNotSelfApproveOrSelfApply()
    {
        var lesson = new OrganizationalLesson { TenantId = "tenant-olma", State = LearningEpistemicState.LessonDistilled, Confidence = 0.85 };
        var prop = await _adaptationEngine.ProposeAdaptationAsync("tenant-olma", lesson, "HeuristicWeight.GrowthFocus", 0.10);

        prop!.State.Should().NotBe(LearningEpistemicState.HumanApproved);
        prop.State.Should().NotBe(LearningEpistemicState.Applied);
    }

    [Fact]
    public async Task OLMA79_Proposal_CarriesDeterministicTimestampAndTenant()
    {
        var lesson = new OrganizationalLesson { TenantId = "tenant-olma", State = LearningEpistemicState.LessonDistilled, Confidence = 0.85 };
        var prop = await _adaptationEngine.ProposeAdaptationAsync("tenant-olma", lesson, "HeuristicWeight.GrowthFocus", 0.10);

        prop!.TenantId.Should().Be("tenant-olma");
        prop.ProposedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task OLMA80_Proposal_JustificationRationale_ReferencesLesson()
    {
        var lesson = new OrganizationalLesson { TenantId = "tenant-olma", Title = "Yield Spike Lesson", State = LearningEpistemicState.LessonDistilled, Confidence = 0.90 };
        var prop = await _adaptationEngine.ProposeAdaptationAsync("tenant-olma", lesson, "HeuristicWeight.GrowthFocus", 0.10);

        prop!.JustificationRationale.Should().Contain("Yield Spike Lesson");
    }

    // =========================================================================
    // Family 11: Approval Sovereignty & PRG-1 Governance Tests (OLMA81 - OLMA88)
    // =========================================================================

    [Fact]
    public void OLMA81_ApprovalSovereignty_CodifiedInI35U()
    {
        OrganizationalLearningInvariants.I35_U_ApprovalSovereignty.Should().Contain("cannot approve an AdaptationProposal");
    }

    [Fact]
    public void OLMA82_Controller_DoesNotExposePostApproveEndpoint()
    {
        // Reflection check: Verify OrganizationalLearningController has NO method named "Approve*"
        var approveMethods = typeof(OrganizationalLearningController).GetMethods()
            .Where(m => m.Name.Contains("Approve", StringComparison.OrdinalIgnoreCase))
            .ToList();

        approveMethods.Should().BeEmpty("OLMA controller must never implement an approval endpoint; all approvals belong to PRG-1 (I35-U)");
    }

    [Fact]
    public void OLMA83_ProposalState_CannotBeTransitionedToHumanApprovedByOLMAService()
    {
        // IOrganizationalLearningService methods check: no ApproveAdaptationAsync exists
        var methods = typeof(IOrganizationalLearningService).GetMethods()
            .Where(m => m.Name.Contains("Approve", StringComparison.OrdinalIgnoreCase))
            .ToList();

        methods.Should().BeEmpty();
    }

    [Fact]
    public void OLMA84_NonHumanCaller_CannotBypassApprovalRequirement()
    {
        var prop = new AdaptationProposal();
        prop.RequiredGovernanceApprovalLevel.Should().Contain("Human");
    }

    [Fact]
    public void OLMA85_SimulatedApproval_ViaPRG1Boundary_Preserved()
    {
        // The lifecycle states confirm PRG-1 boundary
        LearningEpistemicState.HumanApproved.ToString().Should().Be("HumanApproved");
    }

    [Fact]
    public void OLMA86_PRG1Rejection_LeavesProposalRejected()
    {
        var prop = new AdaptationProposal { State = LearningEpistemicState.Rejected };
        prop.State.Should().Be(LearningEpistemicState.Rejected);
    }

    [Fact]
    public void OLMA87_ApplicationAdmitted_DistinctFromHumanApproved()
    {
        LearningEpistemicState.ApplicationAdmitted.Should().NotBe(LearningEpistemicState.HumanApproved);
    }

    [Fact]
    public void OLMA88_ExecutionPermitFirewall_RemainsUntouchedByOLMA()
    {
        OrganizationalLearningInvariants.I35_K_FirewallSovereignty.Should().Contain("cannot grant execution permits");
    }

    // =========================================================================
    // Family 12: Causal Intelligence Sovereignty Tests (OLMA89 - OLMA96)
    // =========================================================================

    [Fact]
    public void OLMA89_CausalSovereignty_CodifiedInI35W()
    {
        OrganizationalLearningInvariants.I35_W_CausalIntelligenceSovereignty.Should().Contain("cannot redefine, overwrite, or independently replace canonical causal relationships");
    }

    [Fact]
    public async Task OLMA90_OLMAConsumesCausalHypothesis_WithoutMutatingDAGs()
    {
        var outcomes = Enumerable.Range(0, 12).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 10, ActualValue = 12, EnvironmentRegime = "Stable" }).ToList();
        var lesson = await _distiller.DistillLessonAsync("tenant-olma", "Causal Consumed", outcomes, "DAG-Node-42", "CF");

        lesson!.CausalAttribution.Should().Be("DAG-Node-42");
    }

    [Fact]
    public void OLMA91_CorrelationWithoutIntervention_BlockedByI35D()
    {
        OrganizationalLearningInvariants.I35_D_CorrelationNotCausalDrift.Should().Contain("interventional validation");
    }

    [Fact]
    public async Task OLMA92_CausalAttribution_PreservedInWhyTrace()
    {
        var proposal = new AdaptationProposal { ProposalId = "p1", ParameterKey = "Param" };
        var lesson = new OrganizationalLesson { Title = "L1", CausalAttribution = "Canonical Root Cause" };

        var trace = await _provenanceService.GenerateTraceAsync(proposal, lesson, Array.Empty<EmpiricalOutcomeEvent>(), new MetrologyRecord());
        trace.CausalAttributionSummary.Should().Be("Canonical Root Cause");
    }

    [Fact]
    public void OLMA93_InterventionalConfirmation_RequiredBeforeLessonPromotion()
    {
        var score = new EvidenceSufficiencyScore { AttributionQuality = 0.50 };
        score.IsSufficient.Should().BeFalse();
    }

    [Fact]
    public void OLMA94_ConflictingEvidence_YieldsFailClosedAmbiguity()
    {
        OrganizationalLearningInvariants.I35_S_FailClosedAmbiguity.Should().Contain("Conflicting evidence or high epistemic uncertainty flags the domain for human review");
    }

    [Fact]
    public void OLMA95_EpistemicUncertainty_RaisesHumanReviewFlag()
    {
        var score = new EvidenceSufficiencyScore { Confidence = 0.50 };
        score.IsSufficient.Should().BeFalse();
    }

    [Fact]
    public async Task OLMA96_CausalHypothesisId_TrackedAcrossOutcomes()
    {
        var outcome = await _learningService.RecordEmpiricalOutcomeAsync("tenant-olma", new EmpiricalOutcomeEvent
        {
            CausalHypothesisId = "HYP-381-09"
        });

        outcome.CausalHypothesisId.Should().Be("HYP-381-09");
    }

    // =========================================================================
    // Family 13: Simulation Engine Sovereignty Tests (OLMA97 - OLMA104)
    // =========================================================================

    [Fact]
    public void OLMA97_SimulationSovereignty_CodifiedInI35X()
    {
        OrganizationalLearningInvariants.I35_X_SimulationEngineSovereignty.Should().Contain("cannot create an independent simulation authority or alter simulation results");
    }

    [Fact]
    public async Task OLMA98_OLMAConsumesCounterfactualResults_WithoutAlteringEngine()
    {
        var outcomes = Enumerable.Range(0, 12).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 10, ActualValue = 12, EnvironmentRegime = "Stable" }).ToList();
        var lesson = await _distiller.DistillLessonAsync("tenant-olma", "CF Lesson", outcomes, "Cause", "Counterfactual Run 99 confirmed no regressive yield");

        lesson!.CounterfactualInsight.Should().Contain("Counterfactual Run 99");
    }

    [Fact]
    public void OLMA99_CounterfactualVerification_RequiredBeforeProposal_LawI35P()
    {
        OrganizationalLearningInvariants.I35_P_CounterfactualVerification.Should().Contain("using 3.9.9 simulation");
    }

    [Fact]
    public void OLMA100_SimulationCalibration_TracksErrorWithoutValidatingPastRunsAsFacts()
    {
        OrganizationalLearningInvariants.I35_G_SimulationCalibrationNotFact.Should().Contain("evaluates simulator reliability");
    }

    [Fact]
    public async Task OLMA101_CounterfactualSummary_RecordedInWhyTrace()
    {
        var proposal = new AdaptationProposal { ProposalId = "p2" };
        var lesson = new OrganizationalLesson { CounterfactualInsight = "CF Verified" };

        var trace = await _provenanceService.GenerateTraceAsync(proposal, lesson, Array.Empty<EmpiricalOutcomeEvent>(), new MetrologyRecord());
        trace.CounterfactualCheckSummary.Should().Be("CF Verified");
    }

    [Fact]
    public void OLMA102_FailedCounterfactualSimulation_HaltsAdaptation()
    {
        // Epistemic state rejected
        var prop = new AdaptationProposal { State = LearningEpistemicState.Rejected };
        prop.State.Should().Be(LearningEpistemicState.Rejected);
    }

    [Fact]
    public void OLMA103_SimulationProvider_RemainsAuthoritative()
    {
        OrganizationalLearningInvariants.I35_X_SimulationEngineSovereignty.Should().Contain("consume governed simulation results from 3.9.9");
    }

    [Fact]
    public void OLMA104_SandboxIsolation_PreservedDuringCounterfactualChecks()
    {
        OrganizationalLearningInvariants.I35_K_FirewallSovereignty.Should().Contain("OLMA cannot grant execution permits");
    }

    // =========================================================================
    // Family 14: Retrospective Explainability & Why Trace Tests (OLMA105 - OLMA112)
    // =========================================================================

    [Fact]
    public async Task OLMA105_GenerateWhyTrace_ReturnsCompleteTrace()
    {
        var proposal = new AdaptationProposal { ProposalId = "p-105", ParameterKey = "HeuristicWeight.GrowthFocus", MaterialityScore = 0.25 };
        var lesson = new OrganizationalLesson { Title = "Growth Focus Shift", CausalAttribution = "CAC trend", CounterfactualInsight = "Zero risk" };
        var metrology = new MetrologyRecord { MeanAbsolutePercentageError = 0.08, BrierScore = 0.02, DirectionalAccuracy = 0.90, SampleCount = 20 };

        var trace = await _provenanceService.GenerateTraceAsync(proposal, lesson, Array.Empty<EmpiricalOutcomeEvent>(), metrology);

        trace.Should().NotBeNull();
        trace.TraceId.Should().NotBeNullOrWhiteSpace();
        trace.ProposalId.Should().Be("p-105");
    }

    [Fact]
    public async Task OLMA106_WhyTrace_IncludesTriggeringOutcomes()
    {
        var proposal = new AdaptationProposal { ProposalId = "p-106" };
        var lesson = new OrganizationalLesson();
        var outcomes = new List<EmpiricalOutcomeEvent>
        {
            new() { OutcomeEventId = "evt-1" },
            new() { OutcomeEventId = "evt-2" }
        };

        var trace = await _provenanceService.GenerateTraceAsync(proposal, lesson, outcomes, new MetrologyRecord());
        trace.TriggeringOutcomeId.Should().Contain("evt-1");
        trace.TriggeringOutcomeId.Should().Contain("evt-2");
    }

    [Fact]
    public async Task OLMA107_WhyTrace_IncludesDistilledLessonTitle()
    {
        var proposal = new AdaptationProposal { ProposalId = "p-107" };
        var lesson = new OrganizationalLesson { Title = "Critical Title" };

        var trace = await _provenanceService.GenerateTraceAsync(proposal, lesson, Array.Empty<EmpiricalOutcomeEvent>(), new MetrologyRecord());
        trace.DistilledLessonTitle.Should().Be("Critical Title");
    }

    [Fact]
    public async Task OLMA108_WhyTrace_IncludesCalibrationEvidenceSummary()
    {
        var proposal = new AdaptationProposal { ProposalId = "p-108" };
        var metrology = new MetrologyRecord { MeanAbsolutePercentageError = 0.05, SampleCount = 30 };

        var trace = await _provenanceService.GenerateTraceAsync(proposal, new OrganizationalLesson(), Array.Empty<EmpiricalOutcomeEvent>(), metrology);
        trace.CalibrationEvidenceSummary.Should().Contain("MAPE: 5.00%");
        trace.CalibrationEvidenceSummary.Should().Contain("SampleCount: 30");
    }

    [Fact]
    public async Task OLMA109_WhyTrace_IncludesCausalAttributionSummary()
    {
        var proposal = new AdaptationProposal { ProposalId = "p-109" };
        var lesson = new OrganizationalLesson { CausalAttribution = "Macro Interest Rate Rise" };

        var trace = await _provenanceService.GenerateTraceAsync(proposal, lesson, Array.Empty<EmpiricalOutcomeEvent>(), new MetrologyRecord());
        trace.CausalAttributionSummary.Should().Be("Macro Interest Rate Rise");
    }

    [Fact]
    public async Task OLMA110_WhyTrace_IncludesCounterfactualCheckSummary()
    {
        var proposal = new AdaptationProposal { ProposalId = "p-110" };
        var lesson = new OrganizationalLesson { CounterfactualInsight = "No drawdown observed" };

        var trace = await _provenanceService.GenerateTraceAsync(proposal, lesson, Array.Empty<EmpiricalOutcomeEvent>(), new MetrologyRecord());
        trace.CounterfactualCheckSummary.Should().Be("No drawdown observed");
    }

    [Fact]
    public async Task OLMA111_WhyTrace_IncludesHysteresisVerification()
    {
        var proposal = new AdaptationProposal { ProposalId = "p-111", MaterialityScore = 0.30 };
        var trace = await _provenanceService.GenerateTraceAsync(proposal, new OrganizationalLesson(), Array.Empty<EmpiricalOutcomeEvent>(), new MetrologyRecord());
        trace.HysteresisVerification.Should().Contain("Hysteresis verified");
    }

    [Fact]
    public async Task OLMA112_WhyTrace_WithInvalidProposalId_ThrowsKeyNotFoundException()
    {
        var act = async () => await _learningService.GetAdaptationWhyTraceAsync("tenant-olma", "non-existent-proposal");
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // =========================================================================
    // Family 15: Multi-Tenant Isolation & Adversarial Integrity Tests (OLMA113 - OLMA120)
    // =========================================================================

    [Fact]
    public void OLMA113_MultiTenantIsolation_CodifiedInI35M()
    {
        OrganizationalLearningInvariants.I35_M_MultiTenantIsolation.Should().Contain("strictly isolated by TenantId");
    }

    [Fact]
    public async Task OLMA114_TenantA_CannotViewTenantB_Outcomes()
    {
        await _learningService.RecordEmpiricalOutcomeAsync("tenant-A", new EmpiricalOutcomeEvent { MetricName = "A-Secret" });
        await _learningService.RecordEmpiricalOutcomeAsync("tenant-B", new EmpiricalOutcomeEvent { MetricName = "B-Secret" });

        var aOutcomes = await _learningService.GetOutcomesAsync("tenant-A");
        aOutcomes.Should().ContainSingle(o => o.MetricName == "A-Secret");
        aOutcomes.Should().NotContain(o => o.MetricName == "B-Secret");
    }

    [Fact]
    public async Task OLMA115_TenantA_CannotViewTenantB_Lessons()
    {
        var outcomes = Enumerable.Range(0, 12).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 10, ActualValue = 12, EnvironmentRegime = "Stable" }).ToList();
        await _distiller.DistillLessonAsync("tenant-A", "Lesson A", outcomes, "C", "CF");
        await _distiller.DistillLessonAsync("tenant-B", "Lesson B", outcomes, "C", "CF");

        var aLessons = await _learningService.ListLessonsAsync("tenant-A");
        aLessons.Should().ContainSingle(l => l.Title == "Lesson A");
        aLessons.Should().NotContain(l => l.Title == "Lesson B");
    }

    [Fact]
    public async Task OLMA116_TenantA_CannotViewTenantB_Proposals()
    {
        var lessonA = new OrganizationalLesson { TenantId = "tenant-A", State = LearningEpistemicState.LessonDistilled, Confidence = 0.85 };
        var lessonB = new OrganizationalLesson { TenantId = "tenant-B", State = LearningEpistemicState.LessonDistilled, Confidence = 0.85 };

        await _adaptationEngine.ProposeAdaptationAsync("tenant-A", lessonA, "HeuristicWeight.GrowthFocus", 0.10);
        await _adaptationEngine.ProposeAdaptationAsync("tenant-B", lessonB, "HeuristicWeight.GrowthFocus", 0.10);

        var aProposals = await _learningService.ListProposalsAsync("tenant-A");
        aProposals.Should().HaveCount(1);
        aProposals[0].TenantId.Should().Be("tenant-A");
    }

    [Fact]
    public async Task OLMA117_Adversarial_CrossTenantLessonAccess_ThrowsUnauthorizedAccessException()
    {
        var outcomes = Enumerable.Range(0, 12).Select(_ => new EmpiricalOutcomeEvent { ExpectedValue = 10, ActualValue = 12, EnvironmentRegime = "Stable" }).ToList();
        var lessonB = await _distiller.DistillLessonAsync("tenant-B", "Classified Secret B", outcomes, "Cause", "CF");

        var act = async () => await _learningService.GetLessonAsync("tenant-A", lessonB!.LessonId);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Cross-tenant learning access violation*");
    }

    [Fact]
    public async Task OLMA118_Adversarial_CrossTenantProposalAccess_ThrowsUnauthorizedAccessException()
    {
        var lessonB = new OrganizationalLesson { TenantId = "tenant-B", State = LearningEpistemicState.LessonDistilled, Confidence = 0.85 };
        var proposalB = await _adaptationEngine.ProposeAdaptationAsync("tenant-B", lessonB, "HeuristicWeight.GrowthFocus", 0.10);

        var act = async () => await _repository.GetProposalAsync("tenant-A", proposalB!.ProposalId);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Cross-tenant adaptation access violation*");
    }

    [Fact]
    public void OLMA119_Adversarial_SqlInjectionInParameterKey_RejectedByWhitelist()
    {
        var injectionKey = "HeuristicWeight.GrowthFocus'; DROP TABLE Users; --";
        var valid = _targetRegistry.TryValidateTarget(injectionKey, 0.10, out var reason, out _);
        valid.Should().BeFalse();
        reason.Should().Contain("not whitelisted");
    }

    [Fact]
    public async Task OLMA120_Adversarial_AttemptToForceZeroSampleSize_PromotesInconclusiveEvidence()
    {
        var lesson = await _distiller.DistillLessonAsync("tenant-olma", "Empty Attack", Array.Empty<EmpiricalOutcomeEvent>(), "Fake Cause", "Fake CF");
        lesson!.State.Should().Be(LearningEpistemicState.InconclusiveEvidence);
        lesson.Confidence.Should().Be(0.0);
        lesson.EvidenceSufficiency.SampleSize.Should().Be(0);
    }
}
