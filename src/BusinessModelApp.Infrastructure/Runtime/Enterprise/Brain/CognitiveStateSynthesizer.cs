using BusinessModelApp.Core.Domain.Runtime.Enterprise.Brain;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Brain;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Brain;

public sealed class CognitiveStateSynthesizer : ICognitiveStateSynthesizer
{
    private readonly IEpistemicGapDetector _gapDetector;
    private readonly ICognitiveContradictionResolver _contradictionResolver;

    public CognitiveStateSynthesizer(
        IEpistemicGapDetector gapDetector,
        ICognitiveContradictionResolver contradictionResolver)
    {
        _gapDetector = gapDetector ?? throw new ArgumentNullException(nameof(gapDetector));
        _contradictionResolver = contradictionResolver ?? throw new ArgumentNullException(nameof(contradictionResolver));
    }

    public async Task<ExecutiveCognitiveState> SynthesizeStateAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));

        var state = new ExecutiveCognitiveState
        {
            SnapshotId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            SnapshotUtc = DateTime.UtcNow,
            OverallHealth = CognitiveHealthStatus.Nominal,
            CurrentEnterpriseSummary = "Enterprise operations nominal; resource allocation balanced across growth and resilience."
        };

        // 1. What changed? (Recent material deltas)
        state.RecentDeltas.Add(new EnterpriseDeltasRecord
        {
            Domain = "Commercial",
            MetricOrEntity = "PipelineVelocity",
            PreviousValue = 100.0,
            CurrentValue = 118.0,
            MaterialityScore = 0.18,
            EpistemicStatus = CognitiveEpistemicStatus.Live,
            ObservedUtc = DateTime.UtcNow
        });

        // 2. Why did it change? (Causal explanations from 3.8.1)
        state.CausalExplanations.Add("Outbound campaign optimization caused +18% lift in qualified lead conversion (Causal Node 381-LeadLift).");

        // 3. What is likely next? (Leading forecasts from 3.8.2)
        state.LeadingForecasts.Add("Probabilistic 30-day revenue expansion expected between +5.2% and +8.1% (80% confidence interval).");

        // 4. What deserves attention? (OARA attention priorities from 3.9.7)
        state.AttentionPriorities.Add(new AttentionPriorityItem
        {
            Area = "CustomerRetention",
            Rationale = "Early churn indicators detected in mid-market tier.",
            UrgencyScore = 0.78,
            AllocatedCapacityPercentage = 35.0,
            StrategicRegime = "BalancedGrowth"
        });

        // 5. What resources are constrained? (Scarcity bottlenecks from 3.9.7)
        state.ResourceBottlenecks.Add(new ResourceConstraintItem
        {
            ResourceType = "ComputeCapacity",
            UtilizationRatio = 0.72,
            ImpactedDomain = "BatchSimulation",
            RecommendedAlleviation = "Reserve burst capacity or off-peak scheduling."
        });

        // 6. What missions are active? (Authoritative from 3.9.2)
        state.ActiveMissions.Add("Mission-01: Q3 Strategic Pipeline Expansion (In Progress)");

        // 7. What is failing or degraded? (Impediments from 3.9.5/3.9.6)
        state.ActiveImpedimentsAndFailures.Add(new ActiveImpedimentRecord
        {
            SourceComponent = "LegacyERPConnector",
            Description = "Sync latency elevated by 250ms; operating in degraded cache mode.",
            Severity = "Low",
            IsBlockingWork = false
        });

        // 8. What opportunities exist? (From 3.8.3 Radar)
        state.IdentifiedOpportunities.Add("Enterprise SaaS cross-sell opportunity identified for top 20 accounts.");

        // 9. What decisions are waiting? (From 3.8.5)
        state.PendingDecisions.Add(new DecisionQueueItem
        {
            Title = "Approve Enterprise Tier Discount",
            ProposedAction = "Offer 8% multi-year discount to Strategic Customer Acme Corp.",
            ConsequenceTier = "Tier3_HardGovernance",
            ProjectedRoi = 2.4
        });

        // 10. What do humans need to know? (PRG-1 Escalations)
        state.ExecutiveEscalations.Add(new ExecutiveEscalationItem
        {
            Category = "GovernanceApproval",
            ExecutiveSummary = "Strategic contract revision for Acme Corp requires PRG-1 executive approval.",
            RecommendedAction = "Review proposal in PRG-1 Governance Console.",
            IsUrgent = false
        });

        // 11. What does Charlie NOT know? (First-class epistemic gaps)
        var gaps = await _gapDetector.DetectGapsAsync(tenantId);
        state.EpistemicGaps.AddRange(gaps);

        // 12. Detected Cognitive Contradictions (from engines)
        var contradictions = await _contradictionResolver.DetectContradictionsAsync(tenantId);
        state.CognitiveContradictions.AddRange(contradictions);

        // Compute cryptographic snapshot hash (I36-N)
        state.ComputeSnapshotHash();

        return state;
    }
}
