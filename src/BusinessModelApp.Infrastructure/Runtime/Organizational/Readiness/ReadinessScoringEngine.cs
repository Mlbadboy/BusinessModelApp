using BusinessModelApp.Core.Domain.Runtime.Organizational.Readiness;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Readiness;

/// <summary>
/// Deterministic 8-dimensional organizational readiness scoring engine.
/// Invariant I31-G: Probability != Certainty.
/// Invariant I31-L: Readiness Score != Organizational Priority.
/// </summary>
public class ReadinessScoringEngine : IReadinessScoringEngine
{
    private readonly IActionPostureResolver _postureResolver;

    private static readonly Dictionary<ReadinessDimension, double> DimensionWeights = new()
    {
        { ReadinessDimension.Liquidity, 0.20 },
        { ReadinessDimension.Capacity, 0.18 },
        { ReadinessDimension.Demand, 0.15 },
        { ReadinessDimension.Operational, 0.15 },
        { ReadinessDimension.Workforce, 0.12 },
        { ReadinessDimension.Supplier, 0.10 },
        { ReadinessDimension.Technology, 0.05 },
        { ReadinessDimension.Governance, 0.05 }
    };

    public ReadinessScoringEngine(IActionPostureResolver postureResolver)
    {
        _postureResolver = postureResolver ?? throw new ArgumentNullException(nameof(postureResolver));
    }

    public OrganizationalReadinessAssessment EvaluateReadiness(
        string tenantId,
        ReadinessScenario scenario,
        Dictionary<ReadinessDimension, double> dimensionInputs,
        CapacityStressTestResult? stressResult = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));

        var scores = new Dictionary<ReadinessDimension, double>();
        foreach (var dim in Enum.GetValues<ReadinessDimension>())
        {
            double val = dimensionInputs != null && dimensionInputs.TryGetValue(dim, out var inputVal)
                ? Math.Clamp(inputVal, 0.0, 1.0)
                : 1.0;
            scores[dim] = val;
        }

        // Incorporate stress test result into Capacity dimension if provided
        if (stressResult != null)
        {
            if (stressResult.IsBufferExhaustedWithinHorizon)
            {
                // Penalize capacity score proportionally to strain
                double strainPenalty = Math.Min(0.5, stressResult.PeakStrainFactor * 0.25);
                scores[ReadinessDimension.Capacity] = Math.Clamp(scores[ReadinessDimension.Capacity] - strainPenalty, 0.0, 1.0);
            }
        }

        // Deterministic composite calculation: sum(w_i * score_i)
        double compositeScore = 0.0;
        foreach (var (dim, weight) in DimensionWeights)
        {
            compositeScore += weight * scores[dim];
        }
        compositeScore = Math.Round(Math.Clamp(compositeScore, 0.0, 1.0), 3);

        // Classify status
        ReadinessStatus status = compositeScore >= 0.85
            ? ReadinessStatus.Green
            : (compositeScore >= 0.50 ? ReadinessStatus.Amber : ReadinessStatus.Red);

        // Detect vulnerabilities for dimensions below 0.70 threshold
        var vulnerabilities = new List<ReadinessVulnerability>();
        foreach (var (dim, score) in scores)
        {
            if (score < 0.70)
            {
                vulnerabilities.Add(new ReadinessVulnerability
                {
                    VulnerabilityId = Guid.NewGuid().ToString("N"),
                    Dimension = dim,
                    Title = $"{dim} Readiness Deficit ({score:P0})",
                    Description = $"Dimension {dim} scored {score:F2}, below the stability threshold of 0.70 under scenario '{scenario.Name}'.",
                    Severity = Math.Round(1.0 - score, 2),
                    EstimatedLeadTimeToRemediateWeeks = dim switch
                    {
                        ReadinessDimension.Capacity => 8.0,
                        ReadinessDimension.Liquidity => 4.0,
                        ReadinessDimension.Workforce => 6.0,
                        ReadinessDimension.Supplier => 12.0,
                        _ => 4.0
                    },
                    TimeToBufferExhaustionWeeks = stressResult?.BufferExhaustionHorizonWeeks ?? 8.0,
                    GapMagnitude = Math.Round((1.0 - score) * 100.0, 1),
                    RemediationRecommendation = $"Prepare contingent capacity/buffer reinforcement for {dim}."
                });
            }
        }

        // Determine action posture via resolver
        double impactMagnitude = 1.0 - compositeScore;
        var posture = _postureResolver.ResolvePosture(
            scenario.EstimatedProbability,
            impactMagnitude,
            scenario.Horizon,
            preparationCost: 0.2,
            reversibilityScore: 0.8,
            confidenceInterval: scenario.ConfidenceInterval);

        var assessment = new OrganizationalReadinessAssessment
        {
            AssessmentId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            ScenarioId = scenario.ScenarioId,
            EvaluatedUtc = DateTime.UtcNow,
            DemandReadiness = scores[ReadinessDimension.Demand],
            CapacityReadiness = scores[ReadinessDimension.Capacity],
            LiquidityReadiness = scores[ReadinessDimension.Liquidity],
            OperationalReadiness = scores[ReadinessDimension.Operational],
            TechnologyReadiness = scores[ReadinessDimension.Technology],
            WorkforceReadiness = scores[ReadinessDimension.Workforce],
            SupplierReadiness = scores[ReadinessDimension.Supplier],
            GovernanceReadiness = scores[ReadinessDimension.Governance],
            CompositeReadinessScore = compositeScore,
            Status = status,
            ActionPosture = posture,
            DetectedVulnerabilities = vulnerabilities
        };

        assessment.WhyTrace = ExplainAssessment(assessment, scenario);
        return assessment;
    }

    public WhyReadinessTrace ExplainAssessment(OrganizationalReadinessAssessment assessment, ReadinessScenario scenario)
    {
        if (assessment == null) throw new ArgumentNullException(nameof(assessment));
        if (scenario == null) throw new ArgumentNullException(nameof(scenario));

        var dimScores = new Dictionary<ReadinessDimension, double>
        {
            { ReadinessDimension.Liquidity, assessment.LiquidityReadiness },
            { ReadinessDimension.Capacity, assessment.CapacityReadiness },
            { ReadinessDimension.Demand, assessment.DemandReadiness },
            { ReadinessDimension.Operational, assessment.OperationalReadiness },
            { ReadinessDimension.Workforce, assessment.WorkforceReadiness },
            { ReadinessDimension.Supplier, assessment.SupplierReadiness },
            { ReadinessDimension.Technology, assessment.TechnologyReadiness },
            { ReadinessDimension.Governance, assessment.GovernanceReadiness }
        };

        var contributingFactors = new List<string>();
        foreach (var (dim, score) in dimScores.OrderBy(kv => kv.Value))
        {
            if (score < 0.70)
            {
                contributingFactors.Add($"Dimension '{dim}' is depressed at {score:F2} (weight: {DimensionWeights[dim]:P0})");
            }
        }

        if (contributingFactors.Count == 0)
        {
            contributingFactors.Add("All organizational dimensions operate within safe buffer parameters.");
        }

        double weeksToImpact = scenario.Horizon switch
        {
            HorizonWindow.Immediate => 2.0,
            HorizonWindow.NearTerm => 6.0,
            HorizonWindow.MediumTerm => 16.0,
            HorizonWindow.LongTerm => 36.0,
            _ => 8.0
        };

        return new WhyReadinessTrace
        {
            AssessmentId = assessment.AssessmentId,
            Status = assessment.Status,
            Posture = assessment.ActionPosture,
            CompositeScore = assessment.CompositeReadinessScore,
            EvidenceRef = scenario.EvidenceRef,
            ForecastHorizonDescription = scenario.Horizon.ToString(),
            ScenarioExposureMagnitude = Math.Round(scenario.ProjectedDemandDelta + scenario.ProjectedCapacityStrain, 2),
            CapacityGap = Math.Round((1.0 - assessment.CapacityReadiness) * 100.0, 1),
            WeeksToImpact = weeksToImpact,
            ModelConfidence = scenario.ConfidenceInterval,
            IsFreshData = true,
            PrimaryContributingFactors = contributingFactors,
            DimensionScores = dimScores
        };
    }
}
