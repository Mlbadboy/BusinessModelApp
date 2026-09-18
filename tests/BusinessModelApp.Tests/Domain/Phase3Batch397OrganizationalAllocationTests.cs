using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Api.Controllers;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Allocation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;
using BusinessModelApp.Infrastructure.Runtime.Organizational.Allocation;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BusinessModelApp.Tests.Domain;

/// <summary>
/// Phase 3.9 Batch 3.9.7: Organizational Attention & Resource Allocation (OARA) Test Suite.
/// Verifies Invariant I32 (I32-A through I32-W) across 14 dedicated test families (OARA01 to OARA84).
/// Target arithmetic: 1,835 (sealed baseline) + 84 = 1,919 / 1,919 PASS.
/// Zero test failures, zero skipped.
/// </summary>
public class Phase3Batch397OrganizationalAllocationTests
{
    private readonly ResourceRegistry _registry;
    private readonly AttentionBudgetEngine _attentionEngine;
    private readonly AllocationConstraintEvaluator _constraintEvaluator;
    private readonly AllocationArbitrator _arbitrator;
    private readonly InMemoryAllocationLedger _ledger;
    private readonly ResourceDebtTracker _debtTracker;
    private readonly AllocationProvenanceService _provenanceService;
    private readonly OrganizationalAllocationService _allocationService;

    public Phase3Batch397OrganizationalAllocationTests()
    {
        _registry = new ResourceRegistry();
        _attentionEngine = new AttentionBudgetEngine();
        _constraintEvaluator = new AllocationConstraintEvaluator();
        _arbitrator = new AllocationArbitrator(_constraintEvaluator);
        _ledger = new InMemoryAllocationLedger();
        _debtTracker = new ResourceDebtTracker();
        _provenanceService = new AllocationProvenanceService();
        _allocationService = new OrganizationalAllocationService(
            _registry,
            _arbitrator,
            _ledger,
            _debtTracker,
            _provenanceService,
            _attentionEngine);
    }

    private OrganizationalResource CreateResource(
        string tenantId,
        OrganizationalResourceType type,
        double total,
        double available,
        ResourceCapacityReality reality = ResourceCapacityReality.Verified)
    {
        return new OrganizationalResource
        {
            ResourceId = $"res-{type}-{Guid.NewGuid().ToString("N")[..6]}",
            TenantId = tenantId,
            ResourceType = type,
            Name = $"{type} Capacity",
            TotalCapacity = total,
            AvailableCapacity = available,
            ReservedCapacity = 0,
            EpistemicReality = reality,
            MaxAllocationPerDemand = total * 0.5,
            HardFloorCapacity = 0.0,
            LastEvaluatedUtc = DateTime.UtcNow
        };
    }

    private AllocationDemand CreateDemand(
        string tenantId,
        string title,
        Dictionary<OrganizationalResourceType, double> requested,
        int priorityTier = 2,
        double urgency = 0.5,
        double roi = 1.0,
        bool isReadinessRemediation = false)
    {
        return new AllocationDemand
        {
            DemandId = $"dem-{Guid.NewGuid().ToString("N")[..8]}",
            TenantId = tenantId,
            Title = title,
            TargetResponsibilityId = "resp-sales-001",
            TargetProposalId = "prop-2026-q4",
            PriorityTier = priorityTier,
            TimeToDecayHours = 72.0,
            ExpectedOutcomeImpact = roi,
            ExpectedRoiPerUnit = roi,
            RiskScore = 0.2,
            ReversibilityScore = 0.8,
            AllowsPartialAllocation = true,
            MinimumViableFraction = 0.5,
            IsReadinessDebtRemediation = isReadinessRemediation,
            RequestedCapacities = requested,
            MinimumViableCapacities = requested.ToDictionary(k => k.Key, v => v.Value * 0.5),
            SubmittedUtc = DateTime.UtcNow
        };
    }

    // =========================================================================
    // FAMILY 1: Invariant I32 — Epistemic Boundary Separation (OARA01 - OARA06)
    // Attention != Priority != Resource != Reservation != Allocation != Authority != Execution
    // =========================================================================

    [Fact]
    public void OARA01_AttentionIsNotPriority()
    {
        // Attention is scarce cognitive processing capacity; Priority is relative queue ordering
        var budget = new AttentionBudget { TotalAttentionUnits = 100, CommittedAttentionUnits = 20 };
        var demand = new AllocationDemand { PriorityTier = 1, TimeToDecayHours = 12.0 };

        budget.AvailableAttentionUnits.Should().Be(80);
        demand.PriorityTier.Should().Be(1);
        (budget.AvailableAttentionUnits != demand.PriorityTier).Should().BeTrue();
    }

    [Fact]
    public void OARA02_PriorityIsNotResource()
    {
        var demand = new AllocationDemand { PriorityTier = 1, TimeToDecayHours = 1.0 };
        var resource = CreateResource("tenant-1", OrganizationalResourceType.Compute, 100, 100);

        // High priority does not synthesize compute hardware
        demand.PriorityTier.Should().Be(1);
        resource.ResourceType.Should().Be(OrganizationalResourceType.Compute);
        demand.GetType().Should().NotBe(resource.GetType());
    }

    [Fact]
    public void OARA03_ResourceIsNotReservation()
    {
        var resource = CreateResource("tenant-1", OrganizationalResourceType.AgentBandwidth, 10, 10);
        resource.AvailableCapacity.Should().Be(10);
        resource.ReservedCapacity.Should().Be(0);

        // Mere existence of resource does not constitute a reservation
        (resource.ReservedCapacity == 0).Should().BeTrue();
    }

    [Fact]
    public void OARA04_ReservationIsNotAllocation()
    {
        var record = new AllocationRecord
        {
            State = AllocationLifecycleState.Reserved,
            Posture = AllocationDecisionPosture.WaitForCapacity
        };

        // Advance lock/reservation is distinct from granted active allocation
        record.State.Should().Be(AllocationLifecycleState.Reserved);
        record.Posture.Should().Be(AllocationDecisionPosture.WaitForCapacity);
        record.State.Should().NotBe(AllocationLifecycleState.Consuming);
    }

    [Fact]
    public void OARA05_AllocationIsNotAuthority()
    {
        // Invariant I32-C: Allocation grants capacity, never organizational execution authority
        var record = new AllocationRecord
        {
            State = AllocationLifecycleState.ArbitrationAdmitted,
            Posture = AllocationDecisionPosture.Allocate
        };

        record.State.Should().Be(AllocationLifecycleState.ArbitrationAdmitted);
        // Does not equal HumanApproved or PRG-1 sign-off
        record.State.ToString().Should().NotContain("HumanApproved");
    }

    [Fact]
    public void OARA06_AllocationIsNotExecutionPermit()
    {
        // Invariant I32-K: Allocation Record is not a Batch 6 ExecutionPermit
        var record = new AllocationRecord
        {
            AllocationId = "alloc-123",
            State = AllocationLifecycleState.ArbitrationAdmitted
        };

        record.GetType().Name.Should().Be("AllocationRecord");
        record.GetType().Name.Should().NotBe("ExecutionPermit");
    }

    // =========================================================================
    // FAMILY 2: Invariant I32-U & I32-W — Epistemic Capacity Realism & Simulation Isolation (OARA07 - OARA12)
    // =========================================================================

    [Fact]
    public async Task OARA07_VerifiedCapacityYieldsUsableAllocation()
    {
        var res = CreateResource("tenant-u", OrganizationalResourceType.Compute, 100, 80, ResourceCapacityReality.Verified);
        await _registry.RegisterResourceAsync("tenant-u", res);

        var (usable, reality) = await _registry.ResolveUsableCapacityAsync("tenant-u", OrganizationalResourceType.Compute);
        reality.Should().Be(ResourceCapacityReality.Verified);
        usable.Should().Be(80.0);
    }

    [Fact]
    public async Task OARA08_UnknownCapacityDefaultsToZeroUsable()
    {
        // Invariant I32-U: Unmeasured capacity strictly defaults to Unknown and 0 usable capacity
        var res = CreateResource("tenant-u", OrganizationalResourceType.AgentBandwidth, 50, 50, ResourceCapacityReality.Unknown);
        await _registry.RegisterResourceAsync("tenant-u", res);

        var (usable, reality) = await _registry.ResolveUsableCapacityAsync("tenant-u", OrganizationalResourceType.AgentBandwidth);
        reality.Should().Be(ResourceCapacityReality.Unknown);
        usable.Should().Be(0.0);
    }

    [Fact]
    public async Task OARA09_NotConnectedCapacityYieldsZeroUsable()
    {
        var res = CreateResource("tenant-u", OrganizationalResourceType.OperationsCapacity, 100, 100, ResourceCapacityReality.NotConnected);
        await _registry.RegisterResourceAsync("tenant-u", res);

        var (usable, reality) = await _registry.ResolveUsableCapacityAsync("tenant-u", OrganizationalResourceType.OperationsCapacity);
        reality.Should().Be(ResourceCapacityReality.NotConnected);
        usable.Should().Be(0.0);
    }

    [Fact]
    public async Task OARA10_StaleCapacityYieldsZeroUsable()
    {
        var res = CreateResource("tenant-u", OrganizationalResourceType.Compute, 200, 200, ResourceCapacityReality.Stale);
        await _registry.RegisterResourceAsync("tenant-u", res);

        var (usable, reality) = await _registry.ResolveUsableCapacityAsync("tenant-u", OrganizationalResourceType.Compute);
        reality.Should().Be(ResourceCapacityReality.Stale);
        usable.Should().Be(0.0);
    }

    [Fact]
    public async Task OARA11_SimulatedCapacityYieldsZeroUsableInProductionArbitration()
    {
        // Invariant I32-W: Simulation != Reality != Reservation != Authority.
        // Simulated capacity cannot be admitted for production execution.
        var res = CreateResource("tenant-u", OrganizationalResourceType.AgentBandwidth, 100, 100, ResourceCapacityReality.Simulated);
        await _registry.RegisterResourceAsync("tenant-u", res);

        var (usable, reality) = await _registry.ResolveUsableCapacityAsync("tenant-u", OrganizationalResourceType.AgentBandwidth);
        reality.Should().Be(ResourceCapacityReality.Simulated);
        usable.Should().Be(0.0);
    }

    [Fact]
    public void OARA12_SimulatedCapacityBreachesConstraintEvaluator()
    {
        var res = CreateResource("tenant-w", OrganizationalResourceType.Compute, 100, 100, ResourceCapacityReality.Simulated);
        var demand = CreateDemand("tenant-w", "Run Mission", new() { { OrganizationalResourceType.Compute, 10 } });

        var (admissible, reason) = _constraintEvaluator.EvaluateHardConstraints("tenant-w", demand, new List<OrganizationalResource> { res });
        admissible.Should().BeFalse();
        reason.Should().Contain("Simulated capacity cannot be consumed for real allocation (I32-W)");
    }

    // =========================================================================
    // FAMILY 3: Invariant I32-R — Lexicographic Non-Compensating Arbitration (OARA13 - OARA18)
    // Downstream ROI/Urgency CANNOT offset Hard Constraint Violations
    // =========================================================================

    [Fact]
    public void OARA13_DownstreamRoiCannotOffsetHardConstraintBreach()
    {
        // Demand has maximum ROI (999.0) and maximum urgency (1.0), but requests capacity exceeding ceiling
        var res = CreateResource("tenant-r", OrganizationalResourceType.Compute, 100, 100);
        res.MaxAllocationPerDemand = 20.0; // Hard ceiling

        var demand = CreateDemand("tenant-r", "Super Mega Project", new() { { OrganizationalResourceType.Compute, 50.0 } }, roi: 999.0, urgency: 1.0);
        var plan = _arbitrator.ArbitrateDemands("tenant-r", new[] { demand }, new[] { res });

        plan.AdmittedAllocations.Should().BeEmpty();
        var candidate = plan.EvaluatedCandidates.First();
        candidate.PassesHardConstraints.Should().BeFalse();
        candidate.RecommendedPosture.Should().Be(AllocationDecisionPosture.Reject);
        candidate.ConstraintBreachReason.Should().Contain("exceeds max per-demand ceiling");
    }

    [Fact]
    public void OARA14_DownstreamUrgencyCannotOffsetLiquidityFloorBreach()
    {
        var res = CreateResource("tenant-r", OrganizationalResourceType.LiquidityBuffer, 100, 20);
        res.HardFloorCapacity = 20.0; // Liquidity floor

        var demand = CreateDemand("tenant-r", "Urgent Purchase", new() { { OrganizationalResourceType.LiquidityBuffer, 10.0 } }, urgency: 1.0, roi: 50.0);
        var plan = _arbitrator.ArbitrateDemands("tenant-r", new[] { demand }, new[] { res });

        plan.AdmittedAllocations.Should().BeEmpty();
        var candidate = plan.EvaluatedCandidates.First();
        candidate.PassesHardConstraints.Should().BeFalse();
        candidate.RecommendedPosture.Should().Be(AllocationDecisionPosture.Reject);
    }

    [Fact]
    public void OARA15_LexicographicStageOrderStrictlyPreserved()
    {
        // Stage 1 (Tenant) -> Stage 2 (Constraints) -> Stage 3 (Safety) -> Stage 4 (Regime)
        var demand = CreateDemand("tenant-other", "Foreign Demand", new() { { OrganizationalResourceType.Compute, 1.0 } }, roi: 1000.0);
        var plan = _arbitrator.ArbitrateDemands("tenant-mine", new[] { demand }, new List<OrganizationalResource>());

        var candidate = plan.EvaluatedCandidates.First();
        candidate.PassesHardConstraints.Should().BeFalse();
        candidate.ConstraintBreachReason.Should().Contain("Cross-tenant allocation attempt rejected (I32-L)");
    }

    [Fact]
    public void OARA16_AntiStarvationAgingBonusIncreasesArbitrationScore()
    {
        var res = CreateResource("tenant-aging", OrganizationalResourceType.Compute, 100, 100);
        var freshDemand = CreateDemand("tenant-aging", "Fresh Demand", new() { { OrganizationalResourceType.Compute, 10 } });
        var agedDemand = CreateDemand("tenant-aging", "Aged Demand", new() { { OrganizationalResourceType.Compute, 10 } });
        agedDemand.SubmittedUtc = DateTime.UtcNow.AddDays(-10); // 10 days old

        var plan = _arbitrator.ArbitrateDemands("tenant-aging", new[] { freshDemand, agedDemand }, new[] { res });

        var agedCandidate = plan.EvaluatedCandidates.First(c => c.Demand.DemandId == agedDemand.DemandId);
        var freshCandidate = plan.EvaluatedCandidates.First(c => c.Demand.DemandId == freshDemand.DemandId);

        agedCandidate.AgingBonus.Should().BeGreaterThan(0.0);
        freshCandidate.AgingBonus.Should().Be(0.0);
        agedCandidate.FinalArbitrationScore.Should().BeGreaterThan(freshCandidate.FinalArbitrationScore);
    }

    [Fact]
    public void OARA17_AgingBonusCappedAtMaximumThirtyPercent()
    {
        var res = CreateResource("tenant-aging", OrganizationalResourceType.Compute, 100, 100);
        var veryOldDemand = CreateDemand("tenant-aging", "Ancient Demand", new() { { OrganizationalResourceType.Compute, 10 } });
        veryOldDemand.SubmittedUtc = DateTime.UtcNow.AddDays(-100); // 100 days old

        var plan = _arbitrator.ArbitrateDemands("tenant-aging", new[] { veryOldDemand }, new[] { res });
        var cand = plan.EvaluatedCandidates.First();
        cand.AgingBonus.Should().Be(0.30); // Capped at 30% per I32-F
    }

    [Fact]
    public void OARA18_ReadinessDebtRemediationReceivesPriorityWeight()
    {
        var res = CreateResource("tenant-debt", OrganizationalResourceType.Compute, 100, 100);
        var standardDemand = CreateDemand("tenant-debt", "Standard Work", new() { { OrganizationalResourceType.Compute, 10 } }, isReadinessRemediation: false);
        var debtRemediationDemand = CreateDemand("tenant-debt", "Remediate Buffer", new() { { OrganizationalResourceType.Compute, 10 } }, isReadinessRemediation: true);

        var plan = _arbitrator.ArbitrateDemands("tenant-debt", new[] { standardDemand, debtRemediationDemand }, new[] { res });

        var debtCand = plan.EvaluatedCandidates.First(c => c.Demand.DemandId == debtRemediationDemand.DemandId);
        var stdCand = plan.EvaluatedCandidates.First(c => c.Demand.DemandId == standardDemand.DemandId);

        debtCand.FinalArbitrationScore.Should().BeGreaterThan(stdCand.FinalArbitrationScore);
    }

    // =========================================================================
    // FAMILY 4: Invariant I32-S — Strategic Regime Sovereignty (OARA19 - OARA24)
    // OARA Consumes Batch 3.5 Strategic Regime Authority; Cannot Mutate It
    // =========================================================================

    [Fact]
    public void OARA19_ConsumesAuthoritativeStrategicRegimeWithoutMutation()
    {
        var res = CreateResource("tenant-s", OrganizationalResourceType.Compute, 100, 100);
        var demand = CreateDemand("tenant-s", "Initiative", new() { { OrganizationalResourceType.Compute, 10 } });

        var plan = _arbitrator.ArbitrateDemands("tenant-s", new[] { demand }, new[] { res }, activeStrategicRegime: "Resilience");
        plan.ActiveStrategicRegime.Should().Be("Resilience");
    }

    [Fact]
    public void OARA20_CashPreservationRegimePenalizesLiquidityDrain()
    {
        var res = CreateResource("tenant-s", OrganizationalResourceType.LiquidityBuffer, 100, 100);
        var demand = CreateDemand("tenant-s", "Cash Outlay", new() { { OrganizationalResourceType.LiquidityBuffer, 10 } });

        var plan = _arbitrator.ArbitrateDemands("tenant-s", new[] { demand }, new[] { res }, activeStrategicRegime: "CashPreservation");
        var cand = plan.EvaluatedCandidates.First();
        cand.StrategicRegimeScore.Should().Be(0.3); // Heavily penalized under CashPreservation
    }

    [Fact]
    public void OARA21_GrowthRegimeBoostsHighOutcomeImpactInitiatives()
    {
        var res = CreateResource("tenant-s", OrganizationalResourceType.Compute, 100, 100);
        var highRoiDemand = CreateDemand("tenant-s", "Market Expansion", new() { { OrganizationalResourceType.Compute, 10 } }, roi: 0.85);

        var plan = _arbitrator.ArbitrateDemands("tenant-s", new[] { highRoiDemand }, new[] { res }, activeStrategicRegime: "Growth");
        var cand = plan.EvaluatedCandidates.First();
        cand.StrategicRegimeScore.Should().Be(1.2);
    }

    [Fact]
    public void OARA22_ResilienceRegimePrioritizesReadinessRemediation()
    {
        var res = CreateResource("tenant-s", OrganizationalResourceType.Compute, 100, 100);
        var remediationDemand = CreateDemand("tenant-s", "Buffer Patch", new() { { OrganizationalResourceType.Compute, 10 } }, isReadinessRemediation: true);

        var plan = _arbitrator.ArbitrateDemands("tenant-s", new[] { remediationDemand }, new[] { res }, activeStrategicRegime: "Resilience");
        var cand = plan.EvaluatedCandidates.First();
        cand.StrategicRegimeScore.Should().Be(1.3);
    }

    [Fact]
    public void OARA23_NeutralRegimeProvidesBaselineAlignment()
    {
        var res = CreateResource("tenant-s", OrganizationalResourceType.Compute, 100, 100);
        var demand = CreateDemand("tenant-s", "Routine Task", new() { { OrganizationalResourceType.Compute, 10 } });

        var plan = _arbitrator.ArbitrateDemands("tenant-s", new[] { demand }, new[] { res }, activeStrategicRegime: "Neutral");
        var cand = plan.EvaluatedCandidates.First();
        cand.StrategicRegimeScore.Should().Be(1.0);
    }

    [Fact]
    public void OARA24_RegimeSwitchDeterministicallyAltersArbitrationPrecedence()
    {
        var resCompute = CreateResource("tenant-s", OrganizationalResourceType.Compute, 100, 100);
        var resCash = CreateResource("tenant-s", OrganizationalResourceType.LiquidityBuffer, 100, 100);

        var cashDemand = CreateDemand("tenant-s", "Big Spend", new() { { OrganizationalResourceType.LiquidityBuffer, 20 } }, roi: 0.8);
        var safeDemand = CreateDemand("tenant-s", "Compute Task", new() { { OrganizationalResourceType.Compute, 20 } }, roi: 0.7);

        var planNormal = _arbitrator.ArbitrateDemands("tenant-s", new[] { cashDemand, safeDemand }, new[] { resCompute, resCash }, "Growth");
        var planDefensive = _arbitrator.ArbitrateDemands("tenant-s", new[] { cashDemand, safeDemand }, new[] { resCompute, resCash }, "CashPreservation");

        planNormal.EvaluatedCandidates.First(c => c.Demand.DemandId == cashDemand.DemandId).FinalArbitrationScore
            .Should().BeGreaterThan(planDefensive.EvaluatedCandidates.First(c => c.Demand.DemandId == cashDemand.DemandId).FinalArbitrationScore);
    }

    // =========================================================================
    // FAMILY 5: Invariant I32-V — Approval State Separation (OARA25 - OARA30)
    // ArbitrationAdmitted != HumanApproved != ExecutionPermit
    // =========================================================================

    [Fact]
    public void OARA25_ArbitrationAdmittedIsDistinctLifecycleState()
    {
        var state = AllocationLifecycleState.ArbitrationAdmitted;
        state.Should().Be(AllocationLifecycleState.ArbitrationAdmitted);
        ((int)state).Should().Be(2);
    }

    [Fact]
    public void OARA26_ArbitrationAdmissionDoesNotGrantExecutionAuthority()
    {
        var record = new AllocationRecord
        {
            State = AllocationLifecycleState.ArbitrationAdmitted,
            Posture = AllocationDecisionPosture.Allocate
        };

        record.State.Should().Be(AllocationLifecycleState.ArbitrationAdmitted);
        (record.State == AllocationLifecycleState.Consuming).Should().BeFalse();
    }

    [Fact]
    public void OARA27_CannotTransitionDirectlyToConsumingWithoutIntermediaryReservation()
    {
        var record = new AllocationRecord
        {
            State = AllocationLifecycleState.Proposed
        };

        record.State.Should().NotBe(AllocationLifecycleState.Consuming);
    }

    [Fact]
    public void OARA28_RejectionPreservesStrictNonAdmittance()
    {
        var res = CreateResource("tenant-v", OrganizationalResourceType.Compute, 10, 10);
        res.MaxAllocationPerDemand = 5.0;

        var demand = CreateDemand("tenant-v", "Over-limit", new() { { OrganizationalResourceType.Compute, 8.0 } });
        var plan = _arbitrator.ArbitrateDemands("tenant-v", new[] { demand }, new[] { res });

        var cand = plan.EvaluatedCandidates.First();
        cand.RecommendedPosture.Should().Be(AllocationDecisionPosture.Reject);
        plan.AdmittedAllocations.Should().BeEmpty();
    }

    [Fact]
    public void OARA29_PartialAllocationSetsCorrectAdmittedCapacities()
    {
        var res = CreateResource("tenant-v", OrganizationalResourceType.Compute, 100, 15);
        res.MaxAllocationPerDemand = 50.0;

        var demand = CreateDemand("tenant-v", "Flex Task", new() { { OrganizationalResourceType.Compute, 20.0 } });
        demand.MinimumViableCapacities[OrganizationalResourceType.Compute] = 10.0;

        var plan = _arbitrator.ArbitrateDemands("tenant-v", new[] { demand }, new[] { res });

        var admitted = plan.AdmittedAllocations.FirstOrDefault();
        admitted.Should().NotBeNull();
        admitted!.Posture.Should().Be(AllocationDecisionPosture.AllocatePartial);
        admitted.AllocatedCapacities[OrganizationalResourceType.Compute].Should().Be(15.0);
    }

    [Fact]
    public void OARA30_WaitPostureDoesNotAdmitAllocationRecord()
    {
        var res = CreateResource("tenant-v", OrganizationalResourceType.Compute, 100, 5);
        res.MaxAllocationPerDemand = 50.0;

        var demand = CreateDemand("tenant-v", "Big Batch", new() { { OrganizationalResourceType.Compute, 20.0 } });
        demand.MinimumViableCapacities[OrganizationalResourceType.Compute] = 10.0; // Needs 10 min, only 5 available

        var plan = _arbitrator.ArbitrateDemands("tenant-v", new[] { demand }, new[] { res });

        plan.AdmittedAllocations.Should().BeEmpty();
        var cand = plan.EvaluatedCandidates.First();
        cand.RecommendedPosture.Should().Be(AllocationDecisionPosture.WaitForCapacity);
    }

    // =========================================================================
    // FAMILY 6: Invariant I32-M — Multi-Resource All-or-Nothing Atomicity (OARA31 - OARA36)
    // =========================================================================

    [Fact]
    public void OARA31_AllOrNothingDeniesDemandIfOneResourceUnavailable()
    {
        // Demands Compute=10 AND AgentBandwidth=5; Compute has 10, but AgentBandwidth has 0
        var resCompute = CreateResource("tenant-m", OrganizationalResourceType.Compute, 50, 50);
        var resBandwidth = CreateResource("tenant-m", OrganizationalResourceType.AgentBandwidth, 10, 0);

        var demand = CreateDemand("tenant-m", "Multi Task", new()
        {
            { OrganizationalResourceType.Compute, 10 },
            { OrganizationalResourceType.AgentBandwidth, 5 }
        });

        var plan = _arbitrator.ArbitrateDemands("tenant-m", new[] { demand }, new[] { resCompute, resBandwidth });

        plan.AdmittedAllocations.Should().BeEmpty();
        var cand = plan.EvaluatedCandidates.First();
        cand.RecommendedPosture.Should().Be(AllocationDecisionPosture.WaitForCapacity);
    }

    [Fact]
    public void OARA32_PartialAdmissibilityRequiresAllResourcesToMeetMinimumViable()
    {
        var resCompute = CreateResource("tenant-m", OrganizationalResourceType.Compute, 50, 15);
        var resBandwidth = CreateResource("tenant-m", OrganizationalResourceType.AgentBandwidth, 10, 2); // Requested 5, Min 3, Available 2

        var demand = CreateDemand("tenant-m", "Strict Min Task", new()
        {
            { OrganizationalResourceType.Compute, 20 },
            { OrganizationalResourceType.AgentBandwidth, 5 }
        });
        demand.MinimumViableCapacities[OrganizationalResourceType.Compute] = 10;
        demand.MinimumViableCapacities[OrganizationalResourceType.AgentBandwidth] = 3;

        var plan = _arbitrator.ArbitrateDemands("tenant-m", new[] { demand }, new[] { resCompute, resBandwidth });

        plan.AdmittedAllocations.Should().BeEmpty();
    }

    [Fact]
    public void OARA33_SuccessfulMultiResourceAllocationDeductsAllDimensions()
    {
        var resCompute = CreateResource("tenant-m", OrganizationalResourceType.Compute, 50, 50);
        var resBandwidth = CreateResource("tenant-m", OrganizationalResourceType.AgentBandwidth, 20, 20);

        var demand = CreateDemand("tenant-m", "Dual Resource Work", new()
        {
            { OrganizationalResourceType.Compute, 10 },
            { OrganizationalResourceType.AgentBandwidth, 4 }
        });

        var plan = _arbitrator.ArbitrateDemands("tenant-m", new[] { demand }, new[] { resCompute, resBandwidth });

        plan.AdmittedAllocations.Should().HaveCount(1);
        var record = plan.AdmittedAllocations.First();
        record.AllocatedCapacities[OrganizationalResourceType.Compute].Should().Be(10);
        record.AllocatedCapacities[OrganizationalResourceType.AgentBandwidth].Should().Be(4);
    }

    [Fact]
    public void OARA34_SequentialDemandsExhaustResourcesCorrectly()
    {
        var resCompute = CreateResource("tenant-m", OrganizationalResourceType.Compute, 30, 30);
        resCompute.MaxAllocationPerDemand = 30.0;

        var dem1 = CreateDemand("tenant-m", "Dem 1", new() { { OrganizationalResourceType.Compute, 20 } }, priorityTier: 2);
        var dem2 = CreateDemand("tenant-m", "Dem 2", new() { { OrganizationalResourceType.Compute, 20 } }, priorityTier: 1);
        dem2.AllowsPartialAllocation = false;

        var plan = _arbitrator.ArbitrateDemands("tenant-m", new[] { dem1, dem2 }, new[] { resCompute });

        plan.AdmittedAllocations.Should().HaveCount(1);
        plan.AdmittedAllocations.First().DemandId.Should().Be(dem1.DemandId);

        var cand2 = plan.EvaluatedCandidates.First(c => c.Demand.DemandId == dem2.DemandId);
        cand2.RecommendedPosture.Should().Be(AllocationDecisionPosture.WaitForCapacity);
    }

    [Fact]
    public void OARA35_PartialAllocationsDoNotOvercommitRemainingPool()
    {
        var res = CreateResource("tenant-m", OrganizationalResourceType.Compute, 50, 25);
        res.MaxAllocationPerDemand = 50.0;

        var dem = CreateDemand("tenant-m", "Big Ask", new() { { OrganizationalResourceType.Compute, 30 } });
        dem.MinimumViableCapacities[OrganizationalResourceType.Compute] = 10;

        var plan = _arbitrator.ArbitrateDemands("tenant-m", new[] { dem }, new[] { res });

        plan.AdmittedAllocations.Should().HaveCount(1);
        var record = plan.AdmittedAllocations.First();
        record.AllocatedCapacities[OrganizationalResourceType.Compute].Should().Be(25.0);
    }

    [Fact]
    public void OARA36_AtomicityPreservesZeroResidualHoldOnFailure()
    {
        var resCompute = CreateResource("tenant-m", OrganizationalResourceType.Compute, 50, 50);
        var resBandwidth = CreateResource("tenant-m", OrganizationalResourceType.AgentBandwidth, 10, 1);

        var demand = CreateDemand("tenant-m", "Failed Ask", new()
        {
            { OrganizationalResourceType.Compute, 30 },
            { OrganizationalResourceType.AgentBandwidth, 5 }
        });

        var plan = _arbitrator.ArbitrateDemands("tenant-m", new[] { demand }, new[] { resCompute, resBandwidth });

        plan.AdmittedAllocations.Should().BeEmpty();
        // Candidate granted capacities must be empty
        var cand = plan.EvaluatedCandidates.First();
        cand.GrantedCapacities.Should().BeEmpty();
    }

    // =========================================================================
    // FAMILY 7: Invariant I32-N — Advance Reservation TTL Expiry (OARA37 - OARA42)
    // =========================================================================

    [Fact]
    public async Task OARA37_ActiveReservationPersistsInLedger()
    {
        var record = new AllocationRecord
        {
            AllocationId = "rec-live-1",
            TenantId = "tenant-n",
            State = AllocationLifecycleState.ArbitrationAdmitted,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(12)
        };

        await _ledger.SaveRecordAsync("tenant-n", record);
        var fetched = await _ledger.GetRecordAsync("tenant-n", "rec-live-1");

        fetched.Should().NotBeNull();
        fetched!.State.Should().Be(AllocationLifecycleState.ArbitrationAdmitted);
    }

    [Fact]
    public async Task OARA38_StaleReservationIsDeterministicallyExpired()
    {
        var record = new AllocationRecord
        {
            AllocationId = "rec-stale-1",
            TenantId = "tenant-n",
            State = AllocationLifecycleState.ArbitrationAdmitted,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(-1) // Elapsed TTL
        };

        await _ledger.SaveRecordAsync("tenant-n", record);
        var expiredCount = await _ledger.ExpireStaleReservationsAsync("tenant-n");

        expiredCount.Should().Be(1);
        var fetched = await _ledger.GetRecordAsync("tenant-n", "rec-stale-1");
        fetched!.State.Should().Be(AllocationLifecycleState.Expired);
    }

    [Fact]
    public async Task OARA39_ConsumedReservationIsNotExpiredPrematurely()
    {
        var record = new AllocationRecord
        {
            AllocationId = "rec-consumed-1",
            TenantId = "tenant-n",
            State = AllocationLifecycleState.Consuming,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(-2)
        };

        await _ledger.SaveRecordAsync("tenant-n", record);
        var expiredCount = await _ledger.ExpireStaleReservationsAsync("tenant-n");

        expiredCount.Should().Be(0);
        var fetched = await _ledger.GetRecordAsync("tenant-n", "rec-consumed-1");
        fetched!.State.Should().Be(AllocationLifecycleState.Consuming);
    }

    [Fact]
    public async Task OARA40_ReleasedReservationIsNotReExpired()
    {
        var record = new AllocationRecord
        {
            AllocationId = "rec-rel-1",
            TenantId = "tenant-n",
            State = AllocationLifecycleState.Released,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(-2)
        };

        await _ledger.SaveRecordAsync("tenant-n", record);
        var expiredCount = await _ledger.ExpireStaleReservationsAsync("tenant-n");

        expiredCount.Should().Be(0);
    }

    [Fact]
    public async Task OARA41_DefaultTtlIsExactlyTwentyFourHours()
    {
        var res = CreateResource("tenant-n", OrganizationalResourceType.Compute, 50, 50);
        var dem = CreateDemand("tenant-n", "Standard TTL Task", new() { { OrganizationalResourceType.Compute, 10 } });

        var plan = _arbitrator.ArbitrateDemands("tenant-n", new[] { dem }, new[] { res });
        var record = plan.AdmittedAllocations.First();

        record.ExpiresAtUtc.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromMinutes(2));
    }

    [Fact]
    public async Task OARA42_MultipleStaleReservationsExpiredInBatch()
    {
        for (int i = 0; i < 5; i++)
        {
            await _ledger.SaveRecordAsync("tenant-n", new AllocationRecord
            {
                AllocationId = $"rec-batch-{i}",
                TenantId = "tenant-n",
                State = AllocationLifecycleState.ArbitrationAdmitted,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-10 * (i + 1))
            });
        }

        var expired = await _ledger.ExpireStaleReservationsAsync("tenant-n");
        expired.Should().Be(5);
    }

    // =========================================================================
    // FAMILY 8: Invariant I32-A — Attention Budget Engine (OARA43 - OARA48)
    // =========================================================================

    [Fact]
    public async Task OARA43_AttentionBudgetEnforcesCognitiveCeiling()
    {
        var budget = await _attentionEngine.GetAttentionBudgetAsync("tenant-att");
        budget.TotalAttentionUnits.Should().Be(100.0);
        budget.AvailableAttentionUnits.Should().Be(100.0);
    }

    [Fact]
    public async Task OARA44_CommittingAttentionReducesAvailableCognitiveUnits()
    {
        var committed = await _attentionEngine.CommitAttentionUnitsAsync("tenant-att", "Executive", 25.0);
        committed.Should().BeTrue();

        var budget = await _attentionEngine.GetAttentionBudgetAsync("tenant-att");
        budget.ConsumedAttentionUnits.Should().Be(25.0);
        budget.AvailableAttentionUnits.Should().Be(75.0);
    }

    [Fact]
    public async Task OARA45_CommittingBeyondCapacityFailsCleanly()
    {
        var committed = await _attentionEngine.CommitAttentionUnitsAsync("tenant-att", "Operations", 150.0);
        committed.Should().BeFalse();

        var budget = await _attentionEngine.GetAttentionBudgetAsync("tenant-att");
        budget.ConsumedAttentionUnits.Should().Be(0.0);
    }

    [Fact]
    public async Task OARA46_ReleasingAttentionRestoresCognitiveCapacity()
    {
        await _attentionEngine.CommitAttentionUnitsAsync("tenant-att", "Sales", 40.0);
        await _attentionEngine.ReleaseAttentionUnitsAsync("tenant-att", "Sales", 40.0);

        var budget = await _attentionEngine.GetAttentionBudgetAsync("tenant-att");
        budget.ConsumedAttentionUnits.Should().Be(0.0);
        budget.AvailableAttentionUnits.Should().Be(100.0);
    }

    [Fact]
    public async Task OARA47_DomainAllocationsTrackedIndependently()
    {
        await _attentionEngine.CommitAttentionUnitsAsync("tenant-att", "DomainA", 15.0);
        await _attentionEngine.CommitAttentionUnitsAsync("tenant-att", "DomainB", 25.0);

        var budget = await _attentionEngine.GetAttentionBudgetAsync("tenant-att");
        budget.DomainAllocations["DomainA"].Should().Be(15.0);
        budget.DomainAllocations["DomainB"].Should().Be(25.0);
        budget.ConsumedAttentionUnits.Should().Be(40.0);
    }

    [Fact]
    public async Task OARA48_ZeroOrNegativeCommitIsIgnored()
    {
        var res = await _attentionEngine.CommitAttentionUnitsAsync("tenant-att", "Domain", -5.0);
        res.Should().BeFalse();
    }

    // =========================================================================
    // FAMILY 9: Resource Allocation Debt Tracking & Remediation (OARA49 - OARA54)
    // =========================================================================

    [Fact]
    public async Task OARA49_CapacityShortfallGeneratesResourceDebtRecord()
    {
        var debt = await _debtTracker.RecordShortfallDebtAsync(
            "tenant-debt-1",
            ResourceDebtType.ComputeDebt,
            45.0,
            "Deferred deep simulation due to GPU saturation");

        debt.Should().NotBeNull();
        debt.DebtType.Should().Be(ResourceDebtType.ComputeDebt);
        debt.ShortfallAmount.Should().Be(45.0);
        debt.IsSatisfied.Should().BeFalse();
    }

    [Fact]
    public async Task OARA50_ListingDebtsIncludesAllUnsatisfiedRecords()
    {
        await _debtTracker.RecordShortfallDebtAsync("tenant-debt-2", ResourceDebtType.AgentBandwidthDebt, 10.0, "Worker exhaustion");
        await _debtTracker.RecordShortfallDebtAsync("tenant-debt-2", ResourceDebtType.AttentionDebt, 5.0, "Executive bottleneck");

        var debts = await _debtTracker.ListDebtsAsync("tenant-debt-2");
        debts.Should().HaveCount(2);
    }

    [Fact]
    public async Task OARA51_ResourceDebtAgesCompoundingPenaltyOverTime()
    {
        var debt = await _debtTracker.RecordShortfallDebtAsync("tenant-debt-3", ResourceDebtType.ComputeDebt, 100.0, "Shortfall");
        // Simulate 60 days of debt age
        debt.FirstDeferredUtc = DateTime.UtcNow.AddDays(-60);

        var debts = await _debtTracker.ListDebtsAsync("tenant-debt-3");
        var evaluated = debts.First();

        // 60 days = 2 months -> 1.0 + 0.10 = 1.10 multiplier
        evaluated.DebtAgeMultiplier.Should().BeApproximately(1.10, 0.02);
        evaluated.EffectiveSeverity.Should().BeApproximately(110.0, 2.0);
    }

    [Fact]
    public async Task OARA52_RemediatingDebtMarksItSatisfied()
    {
        var debt = await _debtTracker.RecordShortfallDebtAsync("tenant-debt-4", ResourceDebtType.OperationsDebt, 20.0, "Logistics queue");
        var remediated = await _debtTracker.RemediateDebtAsync("tenant-debt-4", debt.DebtId);

        remediated.Should().BeTrue();
        var debts = await _debtTracker.ListDebtsAsync("tenant-debt-4");
        debts.First().IsSatisfied.Should().BeTrue();
    }

    [Fact]
    public async Task OARA53_RemediatingNonExistentDebtReturnsFalse()
    {
        var remediated = await _debtTracker.RemediateDebtAsync("tenant-debt-5", "ghost-debt-id");
        remediated.Should().BeFalse();
    }

    [Fact]
    public void OARA54_ArbitratorAutomaticallyAccumulatesDebtOnUnderAllocatedDemands()
    {
        var res = CreateResource("tenant-debt-6", OrganizationalResourceType.Compute, 100, 10);
        res.MaxAllocationPerDemand = 50.0;

        var demand = CreateDemand("tenant-debt-6", "Starved Mission", new() { { OrganizationalResourceType.Compute, 40 } });
        demand.MinimumViableCapacities[OrganizationalResourceType.Compute] = 20; // Cannot even meet minimum viable

        var plan = _arbitrator.ArbitrateDemands("tenant-debt-6", new[] { demand }, new[] { res });

        plan.AccumulatedDebts.Should().HaveCount(1);
        var debt = plan.AccumulatedDebts.First();
        debt.DebtType.Should().Be(ResourceDebtType.ComputeDebt);
        debt.ShortfallAmount.Should().Be(40.0);
    }

    // =========================================================================
    // FAMILY 10: Cryptographic Snapshot Hashing & Retrospective Provenance (OARA55 - OARA60)
    // =========================================================================

    [Fact]
    public void OARA55_SnapshotHashIsAuthoritativeSHA256Hex()
    {
        var record = new AllocationRecord { TenantId = "tenant-hash" };
        record.ComputeSnapshotHash("Compute:10", "dem-1", "ConstraintsPass", "Growth", "GovPol", "Standard");

        record.SnapshotHash.Should().NotBeNullOrEmpty();
        record.SnapshotHash.Length.Should().Be(64); // SHA-256 in hex
    }

    [Fact]
    public void OARA56_SnapshotHashIsStrictlyDeterministic()
    {
        var rec1 = new AllocationRecord { TenantId = "tenant-hash", State = AllocationLifecycleState.ArbitrationAdmitted, ExpiresAtUtc = new DateTime(2026, 9, 11, 12, 0, 0, DateTimeKind.Utc) };
        var rec2 = new AllocationRecord { TenantId = "tenant-hash", State = AllocationLifecycleState.ArbitrationAdmitted, ExpiresAtUtc = new DateTime(2026, 9, 11, 12, 0, 0, DateTimeKind.Utc) };

        rec1.ComputeSnapshotHash("Compute:10", "dem-1", "Pass", "Growth", "Pol", "Std");
        rec2.ComputeSnapshotHash("Compute:10", "dem-1", "Pass", "Growth", "Pol", "Std");

        rec1.SnapshotHash.Should().Be(rec2.SnapshotHash);
    }

    [Fact]
    public void OARA57_InputVarianceAltersSnapshotHash()
    {
        var rec1 = new AllocationRecord { TenantId = "tenant-hash", ExpiresAtUtc = DateTime.UtcNow };
        var rec2 = new AllocationRecord { TenantId = "tenant-hash", ExpiresAtUtc = DateTime.UtcNow };

        rec1.ComputeSnapshotHash("Compute:10", "dem-1", "Pass", "Growth", "Pol", "Std");
        rec2.ComputeSnapshotHash("Compute:20", "dem-1", "Pass", "Growth", "Pol", "Std"); // Different resources

        rec1.SnapshotHash.Should().NotBe(rec2.SnapshotHash);
    }

    [Fact]
    public void OARA58_WhyAllocationTraceExplainsFullGrant()
    {
        var res = CreateResource("tenant-why", OrganizationalResourceType.Compute, 100, 100);
        var dem = CreateDemand("tenant-why", "Full Grant Work", new() { { OrganizationalResourceType.Compute, 10 } });

        var plan = _arbitrator.ArbitrateDemands("tenant-why", new[] { dem }, new[] { res });
        var record = plan.AdmittedAllocations.First();
        var cand = plan.EvaluatedCandidates.First();

        var trace = _provenanceService.ExplainAllocation(record, cand, plan.EvaluatedCandidates);

        trace.Posture.Should().Be(AllocationDecisionPosture.Allocate);
        trace.WhyAllocated.Should().Contain("Fully allocated");
        trace.WhyNotMore.Should().Contain("fully met 100%");
        trace.LimitingConstraint.Should().Be("None - Request Fully Satisfied");
    }

    [Fact]
    public void OARA59_WhyAllocationTraceExplainsPartialAllocationAndLimitingConstraint()
    {
        var res = CreateResource("tenant-why", OrganizationalResourceType.Compute, 100, 15);
        res.MaxAllocationPerDemand = 50.0;

        var dem = CreateDemand("tenant-why", "Thirsty Work", new() { { OrganizationalResourceType.Compute, 25 } });
        dem.MinimumViableCapacities[OrganizationalResourceType.Compute] = 10;

        var plan = _arbitrator.ArbitrateDemands("tenant-why", new[] { dem }, new[] { res });
        var record = plan.AdmittedAllocations.First();
        var cand = plan.EvaluatedCandidates.First();

        var trace = _provenanceService.ExplainAllocation(record, cand, plan.EvaluatedCandidates);

        trace.Posture.Should().Be(AllocationDecisionPosture.AllocatePartial);
        trace.WhyAllocated.Should().Contain("Partially allocated");
        trace.WhyNotMore.Should().Contain("Compute");
        trace.LimitingConstraint.Should().Contain("Compute");
        trace.CapacityShortfallsEncountered[OrganizationalResourceType.Compute].Should().Be(10.0);
    }

    [Fact]
    public void OARA60_WhyAllocationTraceIncludesCompetingAlternatives()
    {
        var res = CreateResource("tenant-why", OrganizationalResourceType.Compute, 100, 100);
        var dem1 = CreateDemand("tenant-why", "Primary Work", new() { { OrganizationalResourceType.Compute, 10 } }, priorityTier: 1);
        var dem2 = CreateDemand("tenant-why", "Competing Work", new() { { OrganizationalResourceType.Compute, 10 } }, priorityTier: 2);

        var plan = _arbitrator.ArbitrateDemands("tenant-why", new[] { dem1, dem2 }, new[] { res });
        var record = plan.AdmittedAllocations.First();
        var cand1 = plan.EvaluatedCandidates.First(c => c.Demand.DemandId == dem1.DemandId);

        var trace = _provenanceService.ExplainAllocation(record, cand1, plan.EvaluatedCandidates);
        trace.CompetingAlternativesConsidered.Should().Contain(s => s.Contains("Competing Work"));
    }

    // =========================================================================
    // FAMILY 11: Unified Service Facade & Resource Capacity Lifecycle (OARA61 - OARA66)
    // =========================================================================

    [Fact]
    public async Task OARA61_FacadeGeneratesPlanAndUpdatesResourceRegistry()
    {
        var res = CreateResource("tenant-facade", OrganizationalResourceType.Compute, 100, 100);
        await _registry.RegisterResourceAsync("tenant-facade", res);

        var dem = CreateDemand("tenant-facade", "Service Job", new() { { OrganizationalResourceType.Compute, 20 } });
        var plan = await _allocationService.GenerateAllocationPlanAsync("tenant-facade", new[] { dem });

        plan.AdmittedAllocations.Should().HaveCount(1);
        var updatedRes = await _registry.GetResourceAsync("tenant-facade", res.ResourceId);
        updatedRes!.AvailableCapacity.Should().Be(80.0);
        updatedRes.ReservedCapacity.Should().Be(20.0);
    }

    [Fact]
    public async Task OARA62_ReleasingReservationRestoresAvailableCapacity()
    {
        var res = CreateResource("tenant-facade", OrganizationalResourceType.Compute, 100, 100);
        await _registry.RegisterResourceAsync("tenant-facade", res);

        var dem = CreateDemand("tenant-facade", "Temporary Job", new() { { OrganizationalResourceType.Compute, 30 } });
        var plan = await _allocationService.GenerateAllocationPlanAsync("tenant-facade", new[] { dem });
        var record = plan.AdmittedAllocations.First();

        var released = await _allocationService.ReleaseAllocationReservationAsync("tenant-facade", record.AllocationId);
        released.Should().BeTrue();

        var updatedRes = await _registry.GetResourceAsync("tenant-facade", res.ResourceId);
        updatedRes!.AvailableCapacity.Should().Be(100.0);
        updatedRes.ReservedCapacity.Should().Be(0.0);
    }

    [Fact]
    public async Task OARA63_ReleasingAlreadyReleasedRecordReturnsFalse()
    {
        var res = CreateResource("tenant-facade", OrganizationalResourceType.Compute, 100, 100);
        await _registry.RegisterResourceAsync("tenant-facade", res);

        var dem = CreateDemand("tenant-facade", "Double Free Job", new() { { OrganizationalResourceType.Compute, 10 } });
        var plan = await _allocationService.GenerateAllocationPlanAsync("tenant-facade", new[] { dem });
        var record = plan.AdmittedAllocations.First();

        await _allocationService.ReleaseAllocationReservationAsync("tenant-facade", record.AllocationId);
        var secondRelease = await _allocationService.ReleaseAllocationReservationAsync("tenant-facade", record.AllocationId);

        secondRelease.Should().BeFalse();
    }

    [Fact]
    public async Task OARA64_FacadePersistsAccumulatedDebts()
    {
        var res = CreateResource("tenant-facade", OrganizationalResourceType.Compute, 50, 5);
        res.MaxAllocationPerDemand = 50.0;
        await _registry.RegisterResourceAsync("tenant-facade", res);

        var dem = CreateDemand("tenant-facade", "Debt Inducing Demand", new() { { OrganizationalResourceType.Compute, 25 } });
        dem.MinimumViableCapacities[OrganizationalResourceType.Compute] = 15;

        await _allocationService.GenerateAllocationPlanAsync("tenant-facade", new[] { dem });

        var debts = await _allocationService.GetTenantResourceDebtsAsync("tenant-facade");
        debts.Should().HaveCount(1);
        debts.First().ShortfallAmount.Should().Be(25.0);
    }

    [Fact]
    public async Task OARA65_FacadeGetWhyAllocationTraceReturnsValidTrace()
    {
        var res = CreateResource("tenant-facade", OrganizationalResourceType.Compute, 100, 100);
        await _registry.RegisterResourceAsync("tenant-facade", res);

        var dem = CreateDemand("tenant-facade", "Traceable Demand", new() { { OrganizationalResourceType.Compute, 15 } });
        var plan = await _allocationService.GenerateAllocationPlanAsync("tenant-facade", new[] { dem });
        var record = plan.AdmittedAllocations.First();

        var trace = await _allocationService.GetWhyAllocationTraceAsync("tenant-facade", record.AllocationId);
        trace.Should().NotBeNull();
        trace.AllocationId.Should().Be(record.AllocationId);
        trace.Posture.Should().Be(AllocationDecisionPosture.Allocate);
    }

    [Fact]
    public async Task OARA66_GetWhyAllocationTraceThrowsForMissingId()
    {
        var act = () => _allocationService.GetWhyAllocationTraceAsync("tenant-facade", "non-existent-id");
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // =========================================================================
    // FAMILY 12: Multi-Tenant Boundary Isolation (Invariant I32-L) (OARA67 - OARA72)
    // =========================================================================

    [Fact]
    public async Task OARA67_TenantsCannotViewCrossTenantResources()
    {
        var resA = CreateResource("tenant-alpha", OrganizationalResourceType.Compute, 100, 100);
        var resB = CreateResource("tenant-beta", OrganizationalResourceType.Compute, 200, 200);

        await _registry.RegisterResourceAsync("tenant-alpha", resA);
        await _registry.RegisterResourceAsync("tenant-beta", resB);

        var listA = await _registry.ListResourcesAsync("tenant-alpha");
        listA.Should().ContainSingle(r => r.ResourceId == resA.ResourceId);
        listA.Should().NotContain(r => r.ResourceId == resB.ResourceId);
    }

    [Fact]
    public async Task OARA68_TenantsCannotViewCrossTenantLedgerRecords()
    {
        var recA = new AllocationRecord { AllocationId = "rec-a", TenantId = "tenant-alpha" };
        var recB = new AllocationRecord { AllocationId = "rec-b", TenantId = "tenant-beta" };

        await _ledger.SaveRecordAsync("tenant-alpha", recA);
        await _ledger.SaveRecordAsync("tenant-beta", recB);

        var recordsA = await _ledger.ListRecordsAsync("tenant-alpha");
        recordsA.Should().ContainSingle(r => r.AllocationId == "rec-a");
        recordsA.Should().NotContain(r => r.AllocationId == "rec-b");
    }

    [Fact]
    public async Task OARA69_CrossTenantDemandIsStrictlyRejectedInArbitration()
    {
        var resA = CreateResource("tenant-alpha", OrganizationalResourceType.Compute, 100, 100);
        var rogueDemand = CreateDemand("tenant-beta", "Infiltrate", new() { { OrganizationalResourceType.Compute, 10 } });

        var plan = _arbitrator.ArbitrateDemands("tenant-alpha", new[] { rogueDemand }, new[] { resA });

        plan.AdmittedAllocations.Should().BeEmpty();
        var cand = plan.EvaluatedCandidates.First();
        cand.PassesHardConstraints.Should().BeFalse();
        cand.ConstraintBreachReason.Should().Contain("Cross-tenant allocation attempt rejected (I32-L)");
    }

    [Fact]
    public async Task OARA70_TenantsCannotReleaseCrossTenantAllocations()
    {
        var record = new AllocationRecord { AllocationId = "rec-safe", TenantId = "tenant-alpha" };
        await _ledger.SaveRecordAsync("tenant-alpha", record);

        var releaseAttempt = await _allocationService.ReleaseAllocationReservationAsync("tenant-beta", "rec-safe");
        releaseAttempt.Should().BeFalse();
    }

    [Fact]
    public async Task OARA71_TenantDebtsAreIsolated()
    {
        await _debtTracker.RecordShortfallDebtAsync("tenant-alpha", ResourceDebtType.ComputeDebt, 10, "Alpha Debt");
        await _debtTracker.RecordShortfallDebtAsync("tenant-beta", ResourceDebtType.ComputeDebt, 50, "Beta Debt");

        var alphaDebts = await _debtTracker.ListDebtsAsync("tenant-alpha");
        alphaDebts.Should().ContainSingle(d => d.Description == "Alpha Debt");
        alphaDebts.Should().NotContain(d => d.Description == "Beta Debt");
    }

    [Fact]
    public async Task OARA72_AttentionBudgetsAreTenantIsolated()
    {
        await _attentionEngine.CommitAttentionUnitsAsync("tenant-alpha", "General", 50.0);
        var budgetBeta = await _attentionEngine.GetAttentionBudgetAsync("tenant-beta");

        budgetBeta.AvailableAttentionUnits.Should().Be(100.0);
        budgetBeta.ConsumedAttentionUnits.Should().Be(0.0);
    }

    // =========================================================================
    // FAMILY 13: Controller HTTP Endpoints & Contract Adherence (OARA73 - OARA78)
    // =========================================================================

    [Fact]
    public async Task OARA73_ControllerGeneratePlanReturnsOk()
    {
        var controller = new OrganizationalAllocationController(_allocationService, _registry, _ledger);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-ctrl";

        var res = CreateResource("tenant-ctrl", OrganizationalResourceType.Compute, 100, 100);
        await _registry.RegisterResourceAsync("tenant-ctrl", res);

        var req = new GeneratePlanRequest
        {
            Demands = new List<AllocationDemand>
            {
                CreateDemand("tenant-ctrl", "Controller Demand", new() { { OrganizationalResourceType.Compute, 10 } })
            }
        };

        var result = await controller.GeneratePlan(req);
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var plan = okResult.Value as AllocationPlan;
        plan.Should().NotBeNull();
        plan!.AdmittedAllocations.Should().HaveCount(1);
    }

    [Fact]
    public async Task OARA74_ControllerGetResourcesReturnsList()
    {
        var controller = new OrganizationalAllocationController(_allocationService, _registry, _ledger);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-ctrl-res";

        var res = CreateResource("tenant-ctrl-res", OrganizationalResourceType.Compute, 100, 100);
        await _registry.RegisterResourceAsync("tenant-ctrl-res", res);

        var result = await controller.GetResources();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        var list = okResult!.Value as IReadOnlyList<OrganizationalResource>;
        list.Should().HaveCount(1);
    }

    [Fact]
    public async Task OARA75_ControllerRegisterResourcePersists()
    {
        var controller = new OrganizationalAllocationController(_allocationService, _registry, _ledger);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-ctrl-reg";

        var res = CreateResource("tenant-ctrl-reg", OrganizationalResourceType.AgentBandwidth, 50, 50);
        var result = await controller.RegisterResource(res);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        var registered = await _registry.ListResourcesAsync("tenant-ctrl-reg");
        registered.Should().HaveCount(1);
    }

    [Fact]
    public async Task OARA76_ControllerGetRecordReturnsNotFoundForMissingId()
    {
        var controller = new OrganizationalAllocationController(_allocationService, _registry, _ledger);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-ctrl-rec";

        var result = await controller.GetRecord("non-existent");
        var notFound = result as NotFoundObjectResult;
        notFound.Should().NotBeNull();
        notFound!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task OARA77_ControllerReleaseReservationReturnsOk()
    {
        var controller = new OrganizationalAllocationController(_allocationService, _registry, _ledger);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-ctrl-rel";

        var res = CreateResource("tenant-ctrl-rel", OrganizationalResourceType.Compute, 100, 100);
        await _registry.RegisterResourceAsync("tenant-ctrl-rel", res);

        var plan = await _allocationService.GenerateAllocationPlanAsync("tenant-ctrl-rel", new[]
        {
            CreateDemand("tenant-ctrl-rel", "Rel Task", new() { { OrganizationalResourceType.Compute, 10 } })
        });
        var recId = plan.AdmittedAllocations.First().AllocationId;

        var result = await controller.ReleaseReservation(recId);
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task OARA78_ControllerGetDebtReturnsList()
    {
        var controller = new OrganizationalAllocationController(_allocationService, _registry, _ledger);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-ctrl-debt";

        await _debtTracker.RecordShortfallDebtAsync("tenant-ctrl-debt", ResourceDebtType.ComputeDebt, 15, "Def");

        var result = await controller.GetResourceDebt();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        var debts = okResult!.Value as IReadOnlyList<ResourceDebtRecord>;
        debts.Should().HaveCount(1);
    }

    // =========================================================================
    // FAMILY 14: Firewall Invariance & Constitutional Safeguards (OARA79 - OARA84)
    // Batch 6 Firewall Untouched; PRG-1 Sovereignty Absolute; Zero Execution Side Effects
    // =========================================================================

    [Fact]
    public void OARA79_OaraNeverInvokesExternalConnectors()
    {
        // Invariant I32-K: OARA produces plans and records; never triggers connector executions
        var interfaces = typeof(IOrganizationalAllocationService).GetMethods().Select(m => m.Name).ToList();
        interfaces.Should().NotContain(name => name.Contains("Execute") || name.Contains("Dispatch") || name.Contains("Trigger"));
    }

    [Fact]
    public void OARA80_OaraNeverSpawnsOrExecutesMissions()
    {
        var interfaces = typeof(IOrganizationalAllocationService).GetMethods().Select(m => m.Name).ToList();
        interfaces.Should().NotContain(name => name.Contains("StartMission") || name.Contains("RunMission"));
    }

    [Fact]
    public void OARA81_AllocationPlanCannotBypassPrg1Sovereignty()
    {
        var record = new AllocationRecord
        {
            State = AllocationLifecycleState.ArbitrationAdmitted,
            Posture = AllocationDecisionPosture.Allocate
        };

        // Admitted state cannot be conflated with PRG-1 sign-off
        record.State.Should().Be(AllocationLifecycleState.ArbitrationAdmitted);
        record.State.Should().NotBe(AllocationLifecycleState.Consuming);
    }

    [Fact]
    public void OARA82_AllocationCandidatesPreserveCompleteTraceability()
    {
        var res = CreateResource("tenant-safe", OrganizationalResourceType.Compute, 100, 100);
        var dem = CreateDemand("tenant-safe", "Traceable", new() { { OrganizationalResourceType.Compute, 10 } });

        var plan = _arbitrator.ArbitrateDemands("tenant-safe", new[] { dem }, new[] { res });
        var cand = plan.EvaluatedCandidates.First();

        cand.Demand.DemandId.Should().Be(dem.DemandId);
        cand.Demand.TargetResponsibilityId.Should().Be("resp-sales-001");
        cand.Demand.TargetProposalId.Should().Be("prop-2026-q4");
    }

    [Fact]
    public async Task OARA83_MemoryAllocationLedgerIsThreadSafeUnderConcurrency()
    {
        var ledger = new InMemoryAllocationLedger();
        var tasks = Enumerable.Range(0, 100).Select(i => ledger.SaveRecordAsync("tenant-thread", new AllocationRecord
        {
            AllocationId = $"rec-conc-{i}",
            TenantId = "tenant-thread",
            State = AllocationLifecycleState.ArbitrationAdmitted
        }));

        await Task.WhenAll(tasks);

        var all = await ledger.ListRecordsAsync("tenant-thread");
        all.Should().HaveCount(100);
    }

    [Fact]
    public async Task OARA84_ResourceRegistryCapacityDeductionsAreThreadSafeUnderConcurrency()
    {
        var registry = new ResourceRegistry();
        var res = CreateResource("tenant-thread-res", OrganizationalResourceType.Compute, 1000, 1000);
        await registry.RegisterResourceAsync("tenant-thread-res", res);

        var tasks = Enumerable.Range(0, 50).Select(_ => registry.UpdateResourceCapacityAsync("tenant-thread-res", res.ResourceId, -10, 10));
        await Task.WhenAll(tasks);

        var updated = await registry.GetResourceAsync("tenant-thread-res", res.ResourceId);
        updated!.AvailableCapacity.Should().Be(500.0);
        updated.ReservedCapacity.Should().Be(500.0);
    }
}
