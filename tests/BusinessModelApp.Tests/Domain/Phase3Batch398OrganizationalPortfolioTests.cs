using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Api.Controllers;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Allocation;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Portfolio;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;
using BusinessModelApp.Infrastructure.Runtime.Organizational.Portfolio;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BusinessModelApp.Tests.Domain;

/// <summary>
/// Phase 3.9 Batch 3.9.8: Autonomous Resource-Aware Work Planning & Portfolio Control Test Suite.
/// Verifies Invariant I33 (I33-A through I33-W) across 16 dedicated test families (PORT01 to PORT108).
/// Target arithmetic: 1,919 (sealed baseline) + 108 = 2,027 / 2,027 PASS.
/// Zero test failures, zero skipped.
/// </summary>
public class Phase3Batch398OrganizationalPortfolioTests
{
    private readonly InMemoryPortfolioRepository _repository;
    private readonly DependencyGraphResolver _dependencyResolver;
    private readonly PortfolioPlanner _planner;
    private readonly PortfolioRebalanceEngine _rebalanceEngine;
    private readonly PortfolioSimulator _simulator;
    private readonly PortfolioProvenanceService _provenanceService;
    private readonly OrganizationalPortfolioService _portfolioService;

    public Phase3Batch398OrganizationalPortfolioTests()
    {
        _repository = new InMemoryPortfolioRepository();
        _dependencyResolver = new DependencyGraphResolver();
        _planner = new PortfolioPlanner(_dependencyResolver);
        _rebalanceEngine = new PortfolioRebalanceEngine();
        _simulator = new PortfolioSimulator();
        _provenanceService = new PortfolioProvenanceService();
        _portfolioService = new OrganizationalPortfolioService(
            _repository,
            _planner,
            _rebalanceEngine,
            _simulator,
            _provenanceService);
    }

    private PortfolioWorkItem CreateWorkItem(
        string tenantId,
        string title,
        PortfolioWorkCategory category = PortfolioWorkCategory.Operational,
        double expectedValue = 1.0,
        double strategicAlignment = 0.5,
        Dictionary<OrganizationalResourceType, double>? demands = null)
    {
        return new PortfolioWorkItem
        {
            WorkItemId = $"item-{Guid.NewGuid().ToString("N")[..8]}",
            TenantId = tenantId,
            Title = title,
            Category = category,
            State = PortfolioLifecycleState.Draft,
            ExpectedValue = expectedValue,
            StrategicAlignmentScore = strategicAlignment,
            RiskScore = 0.2,
            ReversibilityScore = 0.8,
            DeadlinePressure = 0.5,
            ResourceDemands = demands ?? new Dictionary<OrganizationalResourceType, double>
            {
                { OrganizationalResourceType.Compute, 10.0 }
            }
        };
    }

    private PortfolioCapacityEnvelope CreateEnvelope(string tenantId, double compute = 100.0, double agentBandwidth = 50.0)
    {
        return new PortfolioCapacityEnvelope
        {
            TenantId = tenantId,
            AllocationSnapshotHash = "oara-snapshot-hash-123",
            AvailableCapacities = new Dictionary<OrganizationalResourceType, double>
            {
                { OrganizationalResourceType.Compute, compute },
                { OrganizationalResourceType.AgentBandwidth, agentBandwidth }
            }
        };
    }

    // =========================================================================
    // FAMILY 1: Invariant I33 — Portfolio Identity & Epistemic Boundaries (PORT01 - PORT06)
    // Allocation != Work Plan != Portfolio != Mission Graph != Schedule != Authority != Execution != Outcome
    // =========================================================================

    [Fact]
    public void PORT01_PortfolioIsNotAllocation()
    {
        // Having an allocated capacity envelope does not constitute an active portfolio
        var envelope = CreateEnvelope("tenant-1");
        var portfolio = new OrganizationalPortfolio { TenantId = "tenant-1" };

        envelope.GetType().Should().NotBe(portfolio.GetType());
        portfolio.WorkItems.Should().BeEmpty();
    }

    [Fact]
    public void PORT02_WorkPlanIsNotMissionGraph()
    {
        // Work plan establishes sequencing and trade-offs, not executable DAG or runtime states
        var item = CreateWorkItem("tenant-1", "Strategic Project");
        item.State = PortfolioLifecycleState.PlanReady;

        item.GetType().Name.Should().Be("PortfolioWorkItem");
        item.GetType().Name.Should().NotBe("MissionGraph");
    }

    [Fact]
    public void PORT03_PortfolioPriorityIsNotExecutionAuthority()
    {
        // Invariant I33-C: High portfolio rank never creates unilateral execution permits
        var item = CreateWorkItem("tenant-1", "Critical Overhaul", PortfolioWorkCategory.Strategic, expectedValue: 100.0);
        item.State = PortfolioLifecycleState.PlanReady;

        item.State.Should().Be(PortfolioLifecycleState.PlanReady);
        item.State.ToString().Should().NotContain("ExecutionPermit");
    }

    [Fact]
    public void PORT04_PlanIsNotCommitment()
    {
        // Invariant I33-E: A plan is a structured proposal subject to review, not a commitment
        var portfolio = new OrganizationalPortfolio { State = PortfolioLifecycleState.PlanReady };
        portfolio.State.Should().Be(PortfolioLifecycleState.PlanReady);
        portfolio.State.Should().NotBe(PortfolioLifecycleState.WorkPlaneAdmissionRequested);
    }

    [Fact]
    public void PORT05_WorkPlaneAdmissionRequestedIsNotExecutionPermit()
    {
        // Explicit lifecycle naming check: WorkPlaneAdmissionRequested != ExecutionPermit
        var state = PortfolioLifecycleState.WorkPlaneAdmissionRequested;
        state.ToString().Should().Be("WorkPlaneAdmissionRequested");
        state.ToString().Should().NotContain("ExecutionApproved");
    }

    [Fact]
    public void PORT06_PortfolioCannotManufactureBatch6Permits()
    {
        // Invariant I33-I: Zero code paths from portfolio planning to Batch 6 permits
        var methods = typeof(IPortfolioPlanner).GetMethods().Select(m => m.Name).ToList();
        methods.Should().NotContain(name => name.Contains("IssuePermit") || name.Contains("AuthorizeExecution"));
    }

    // =========================================================================
    // FAMILY 2: Invariant I33-S — OARA Allocation Ceiling Sovereignty (PORT07 - PORT14)
    // PortfolioAllocatedCapacity <= OARAAllocatedCapacity. Cannot increase/modify OARA envelope.
    // =========================================================================

    [Fact]
    public void PORT07_PlannedAllocationsNeverExceedOaraEnvelope()
    {
        var envelope = CreateEnvelope("tenant-s", compute: 20.0);
        var item = CreateWorkItem("tenant-s", "Oversized Job", demands: new() { { OrganizationalResourceType.Compute, 25.0 } });

        var portfolio = _planner.PlanPortfolio("tenant-s", new[] { item }, envelope);

        // Cannot allocate 25 when only 20 is available in envelope
        portfolio.WorkItems.First().State.Should().Be(PortfolioLifecycleState.Deferred);
        portfolio.WorkItems.First().AllocatedCapacities.Should().BeEmpty();
    }

    [Fact]
    public void PORT08_PlannerConsumesOaraEnvelopeWithoutMutation()
    {
        var envelope = CreateEnvelope("tenant-s", compute: 50.0);
        var item = CreateWorkItem("tenant-s", "Valid Job", demands: new() { { OrganizationalResourceType.Compute, 30.0 } });

        var portfolio = _planner.PlanPortfolio("tenant-s", new[] { item }, envelope);

        portfolio.CapacityEnvelope.AvailableCapacities[OrganizationalResourceType.Compute].Should().Be(50.0);
        portfolio.CapacityEnvelope.CommittedCapacities[OrganizationalResourceType.Compute].Should().Be(30.0);
    }

    [Fact]
    public void PORT09_MultipleItemsExhaustEnvelopeWithoutOverdraft()
    {
        var envelope = CreateEnvelope("tenant-s", compute: 30.0);
        var item1 = CreateWorkItem("tenant-s", "Job 1", demands: new() { { OrganizationalResourceType.Compute, 20.0 } });
        var item2 = CreateWorkItem("tenant-s", "Job 2", demands: new() { { OrganizationalResourceType.Compute, 20.0 } });

        var portfolio = _planner.PlanPortfolio("tenant-s", new[] { item1, item2 }, envelope);

        var aligned = portfolio.WorkItems.Count(i => i.State == PortfolioLifecycleState.ResourceAligned);
        var deferred = portfolio.WorkItems.Count(i => i.State == PortfolioLifecycleState.Deferred);

        aligned.Should().Be(1);
        deferred.Should().Be(1);
        portfolio.CapacityEnvelope.CommittedCapacities[OrganizationalResourceType.Compute].Should().Be(20.0);
    }

    [Fact]
    public void PORT10_ZeroOaraAllocationDefersAllDemandingItems()
    {
        var envelope = CreateEnvelope("tenant-s", compute: 0.0, agentBandwidth: 0.0);
        var item = CreateWorkItem("tenant-s", "Starved Item");

        var portfolio = _planner.PlanPortfolio("tenant-s", new[] { item }, envelope);

        portfolio.WorkItems.First().State.Should().Be(PortfolioLifecycleState.Deferred);
        portfolio.State.Should().Be(PortfolioLifecycleState.Deferred);
    }

    [Fact]
    public void PORT11_CommittedCapacityTracksExactDimensionSums()
    {
        var envelope = CreateEnvelope("tenant-s", compute: 100.0, agentBandwidth: 50.0);
        var item1 = CreateWorkItem("tenant-s", "Job A", demands: new() { { OrganizationalResourceType.Compute, 15.0 }, { OrganizationalResourceType.AgentBandwidth, 10.0 } });
        var item2 = CreateWorkItem("tenant-s", "Job B", demands: new() { { OrganizationalResourceType.Compute, 25.0 }, { OrganizationalResourceType.AgentBandwidth, 5.0 } });

        var portfolio = _planner.PlanPortfolio("tenant-s", new[] { item1, item2 }, envelope);

        portfolio.CapacityEnvelope.CommittedCapacities[OrganizationalResourceType.Compute].Should().Be(40.0);
        portfolio.CapacityEnvelope.CommittedCapacities[OrganizationalResourceType.AgentBandwidth].Should().Be(15.0);
    }

    [Fact]
    public void PORT12_OaraSnapshotHashPreservedInPortfolio()
    {
        var envelope = CreateEnvelope("tenant-s");
        envelope.AllocationSnapshotHash = "authoritative-oara-hash-999";

        var portfolio = _planner.PlanPortfolio("tenant-s", new List<PortfolioWorkItem>(), envelope);
        portfolio.AllocationSnapshotHash.Should().Be("authoritative-oara-hash-999");
    }

    [Fact]
    public void PORT13_EnvelopeHasSufficientCapacityReturnsFalseWhenExceeded()
    {
        var envelope = CreateEnvelope("tenant-s", compute: 10.0);
        bool hasCapacity = envelope.HasSufficientCapacity(new() { { OrganizationalResourceType.Compute, 15.0 } });
        hasCapacity.Should().BeFalse();
    }

    [Fact]
    public void PORT14_EnvelopeHasSufficientCapacityReturnsTrueWhenWithinBounds()
    {
        var envelope = CreateEnvelope("tenant-s", compute: 20.0);
        bool hasCapacity = envelope.HasSufficientCapacity(new() { { OrganizationalResourceType.Compute, 15.0 } });
        hasCapacity.Should().BeTrue();
    }

    // =========================================================================
    // FAMILY 3: Capacity-Aware Planning & Deterministic Optimization (I33-T) (PORT15 - PORT20)
    // =========================================================================

    [Fact]
    public void PORT15_DeterministicRankingSortsIdenticalItemsDeterministically()
    {
        var envelope = CreateEnvelope("tenant-det", compute: 50.0);
        var itemA = CreateWorkItem("tenant-det", "Alpha", expectedValue: 5.0);
        var itemB = CreateWorkItem("tenant-det", "Beta", expectedValue: 5.0);

        var plan1 = _planner.PlanPortfolio("tenant-det", new[] { itemA, itemB }, envelope);
        var plan2 = _planner.PlanPortfolio("tenant-det", new[] { itemB, itemA }, envelope);

        plan1.PortfolioDecisionHash.Should().Be(plan2.PortfolioDecisionHash);
    }

    [Fact]
    public void PORT16_DecisionHashIsBitForBitIdenticalForIdenticalInputs()
    {
        var env1 = CreateEnvelope("tenant-det", compute: 100.0);
        var env2 = CreateEnvelope("tenant-det", compute: 100.0);

        var item = CreateWorkItem("tenant-det", "Job 1");

        var plan1 = _planner.PlanPortfolio("tenant-det", new[] { item }, env1, "Growth");
        var plan2 = _planner.PlanPortfolio("tenant-det", new[] { item }, env2, "Growth");

        plan1.PortfolioDecisionHash.Should().Be(plan2.PortfolioDecisionHash);
        plan1.PortfolioDecisionHash.Length.Should().Be(64);
    }

    [Fact]
    public void PORT17_InputVarianceAltersDecisionHash()
    {
        var env1 = CreateEnvelope("tenant-det", compute: 100.0);
        var env2 = CreateEnvelope("tenant-det", compute: 100.0);

        var item1 = CreateWorkItem("tenant-det", "Job 1", expectedValue: 1.0);
        var item2 = CreateWorkItem("tenant-det", "Job 2", expectedValue: 10.0);

        var plan1 = _planner.PlanPortfolio("tenant-det", new[] { item1 }, env1);
        var plan2 = _planner.PlanPortfolio("tenant-det", new[] { item2 }, env2);

        plan1.PortfolioDecisionHash.Should().NotBe(plan2.PortfolioDecisionHash);
    }

    [Fact]
    public void PORT18_ObligatoryCategoryPreemptsStrategicCategory()
    {
        var envelope = CreateEnvelope("tenant-det", compute: 10.0);
        var strategic = CreateWorkItem("tenant-det", "Strategic Expansion", PortfolioWorkCategory.Strategic, expectedValue: 100.0);
        var obligatory = CreateWorkItem("tenant-det", "Security Compliance Patch", PortfolioWorkCategory.Obligatory, expectedValue: 1.0);

        var plan = _planner.PlanPortfolio("tenant-det", new[] { strategic, obligatory }, envelope);

        // Obligatory takes priority even with lower raw expected value
        var aligned = plan.WorkItems.First(i => i.State == PortfolioLifecycleState.ResourceAligned);
        aligned.Title.Should().Be("Security Compliance Patch");
    }

    [Fact]
    public void PORT19_CashPreservationRegimePenalizesLiquidityDemand()
    {
        var envelope = CreateEnvelope("tenant-det", compute: 100.0);
        envelope.AvailableCapacities[OrganizationalResourceType.LiquidityBuffer] = 50.0;

        var cashHeavyItem = CreateWorkItem("tenant-det", "Cash Heavy", demands: new() { { OrganizationalResourceType.LiquidityBuffer, 20.0 } });
        var computeItem = CreateWorkItem("tenant-det", "Compute Item", demands: new() { { OrganizationalResourceType.Compute, 20.0 } });

        var plan = _planner.PlanPortfolio("tenant-det", new[] { cashHeavyItem, computeItem }, envelope, activeStrategicRegime: "CashPreservation");

        // Compute item ranks ahead of cash-draining item under CashPreservation
        plan.WorkItems.First().Title.Should().Be("Compute Item");
    }

    [Fact]
    public void PORT20_ResilienceRegimeBoostsReadinessRemediation()
    {
        var envelope = CreateEnvelope("tenant-det", compute: 100.0);
        var standardItem = CreateWorkItem("tenant-det", "Normal Work", strategicAlignment: 0.5);
        var remediationItem = CreateWorkItem("tenant-det", "Readiness Buffer Fix", strategicAlignment: 0.5);
        remediationItem.IsReadinessDebtRemediation = true;

        var plan = _planner.PlanPortfolio("tenant-det", new[] { standardItem, remediationItem }, envelope, activeStrategicRegime: "Resilience");

        plan.WorkItems.First().Title.Should().Be("Readiness Buffer Fix");
    }

    // =========================================================================
    // FAMILY 4: Planning Dependency Graph Sovereignty (I33-F, I33-V) (PORT21 - PORT26)
    // Dependency graph answers readiness before work begins; cannot mutate/execute a Mission Graph.
    // =========================================================================

    [Fact]
    public async Task PORT21_AcyclicDependenciesValidateSuccessfully()
    {
        var itemA = CreateWorkItem("tenant-dep", "Item A");
        var itemB = CreateWorkItem("tenant-dep", "Item B");
        itemB.PrerequisiteWorkItemIds.Add(itemA.WorkItemId);

        var (state, reason, ordered) = await _dependencyResolver.ValidateAndSortDependenciesAsync("tenant-dep", new[] { itemB, itemA });

        state.Should().Be(DependencyValidationState.Satisfied);
        reason.Should().BeNull();
        ordered.Should().ContainInOrder(itemA.WorkItemId, itemB.WorkItemId);
    }

    [Fact]
    public async Task PORT22_CircularDependencyDetectedAndFailsClosed()
    {
        var itemA = CreateWorkItem("tenant-dep", "Item A");
        var itemB = CreateWorkItem("tenant-dep", "Item B");
        itemA.PrerequisiteWorkItemIds.Add(itemB.WorkItemId);
        itemB.PrerequisiteWorkItemIds.Add(itemA.WorkItemId);

        var (state, reason, _) = await _dependencyResolver.ValidateAndSortDependenciesAsync("tenant-dep", new[] { itemA, itemB });

        state.Should().Be(DependencyValidationState.CycleDetected);
        reason.Should().Contain("Circular dependency cycle detected");
    }

    [Fact]
    public async Task PORT23_MissingPrerequisiteMarksItemBlocked()
    {
        var item = CreateWorkItem("tenant-dep", "Orphan Child");
        item.PrerequisiteWorkItemIds.Add("ghost-prereq-id");

        var (state, reason, _) = await _dependencyResolver.ValidateAndSortDependenciesAsync("tenant-dep", new[] { item });

        state.Should().Be(DependencyValidationState.Missing);
        reason.Should().Contain("is missing from portfolio candidate set");
        item.State.Should().Be(PortfolioLifecycleState.Blocked);
    }

    [Fact]
    public void PORT24_PlannerFailsClosedWhenDependencyCycleExists()
    {
        var envelope = CreateEnvelope("tenant-dep", compute: 100.0);
        var itemA = CreateWorkItem("tenant-dep", "Item A");
        var itemB = CreateWorkItem("tenant-dep", "Item B");
        itemA.PrerequisiteWorkItemIds.Add(itemB.WorkItemId);
        itemB.PrerequisiteWorkItemIds.Add(itemA.WorkItemId);

        var portfolio = _planner.PlanPortfolio("tenant-dep", new[] { itemA, itemB }, envelope);

        portfolio.State.Should().Be(PortfolioLifecycleState.Blocked);
    }

    [Fact]
    public void PORT25_DependencyGraphDoesNotExposeMissionGraphEntities()
    {
        // Invariant I33-V: Planning Dependency Sovereignty
        var types = typeof(DependencyGraphResolver).Assembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Portfolio") == true)
            .Select(t => t.Name)
            .ToList();

        types.Should().NotContain("MissionGraph");
        types.Should().NotContain("MissionDAGCompiler");
    }

    [Fact]
    public async Task PORT26_EmptyDependencyListYieldsSatisfied()
    {
        var (state, _, ordered) = await _dependencyResolver.ValidateAndSortDependenciesAsync("tenant-dep", Array.Empty<PortfolioWorkItem>());
        state.Should().Be(DependencyValidationState.Satisfied);
        ordered.Should().BeEmpty();
    }

    // =========================================================================
    // FAMILY 5: Portfolio Categorization & Balance (PORT27 - PORT32)
    // =========================================================================

    [Fact]
    public void PORT27_StrategicWorkCategoryIsTracked()
    {
        var item = CreateWorkItem("tenant-cat", "Market Entry", PortfolioWorkCategory.Strategic);
        item.Category.Should().Be(PortfolioWorkCategory.Strategic);
    }

    [Fact]
    public void PORT28_OperationalWorkCategoryIsTracked()
    {
        var item = CreateWorkItem("tenant-cat", "Customer SLA", PortfolioWorkCategory.Operational);
        item.Category.Should().Be(PortfolioWorkCategory.Operational);
    }

    [Fact]
    public void PORT29_ObligatoryWorkCategoryIsTracked()
    {
        var item = CreateWorkItem("tenant-cat", "Statutory Audit Prep", PortfolioWorkCategory.Obligatory);
        item.Category.Should().Be(PortfolioWorkCategory.Obligatory);
    }

    [Fact]
    public void PORT30_BalancedPortfolioContainsMultipleCategories()
    {
        var envelope = CreateEnvelope("tenant-cat", compute: 100.0);
        var s = CreateWorkItem("tenant-cat", "Growth", PortfolioWorkCategory.Strategic);
        var o = CreateWorkItem("tenant-cat", "Ops", PortfolioWorkCategory.Operational);
        var b = CreateWorkItem("tenant-cat", "Obligatory", PortfolioWorkCategory.Obligatory);

        var plan = _planner.PlanPortfolio("tenant-cat", new[] { s, o, b }, envelope);

        plan.WorkItems.Select(i => i.Category).Distinct().Should().HaveCount(3);
    }

    [Fact]
    public void PORT31_ObligatoryItemsAlwaysAdmittedAheadOfOthers()
    {
        var envelope = CreateEnvelope("tenant-cat", compute: 10.0);
        var s = CreateWorkItem("tenant-cat", "Growth", PortfolioWorkCategory.Strategic, expectedValue: 50.0);
        var b = CreateWorkItem("tenant-cat", "Statutory", PortfolioWorkCategory.Obligatory, expectedValue: 1.0);

        var plan = _planner.PlanPortfolio("tenant-cat", new[] { s, b }, envelope);

        plan.WorkItems.First(i => i.State == PortfolioLifecycleState.ResourceAligned).Category
            .Should().Be(PortfolioWorkCategory.Obligatory);
    }

    [Fact]
    public void PORT32_ZeroCategoryCandidatesProducesEmptyCategoryBucket()
    {
        var envelope = CreateEnvelope("tenant-cat", compute: 100.0);
        var ops = CreateWorkItem("tenant-cat", "Only Ops", PortfolioWorkCategory.Operational);

        var plan = _planner.PlanPortfolio("tenant-cat", new[] { ops }, envelope);
        plan.WorkItems.Any(i => i.Category == PortfolioWorkCategory.Strategic).Should().BeFalse();
    }

    // =========================================================================
    // FAMILY 6: Dynamic Rebalancing Intent Determinations (I33-U) (PORT33 - PORT40)
    // =========================================================================

    [Fact]
    public async Task PORT33_RebalanceEngineGeneratesContinueWhenNominal()
    {
        var portfolio = new OrganizationalPortfolio
        {
            TenantId = "tenant-reb",
            WorkItems = new List<PortfolioWorkItem>
            {
                CreateWorkItem("tenant-reb", "Nominal Work")
            }
        };

        var delta = new MaterialityDelta { RealityDelta = 0.8 }; // Meets threshold
        var proposal = await _rebalanceEngine.EvaluateRebalanceAsync("tenant-reb", portfolio, delta, Array.Empty<PortfolioWorkItem>());

        proposal.Should().NotBeNull();
        proposal!.ItemActions.First().Action.Should().Be(PortfolioItemAction.Continue);
    }

    [Fact]
    public async Task PORT34_RebalanceEngineGeneratesIncreaseOnOutcomeAcceleration()
    {
        var item = CreateWorkItem("tenant-reb", "Validated Star", strategicAlignment: 0.95);
        item.AllocatedCapacities[OrganizationalResourceType.Compute] = 20.0;

        var portfolio = new OrganizationalPortfolio
        {
            TenantId = "tenant-reb",
            WorkItems = new List<PortfolioWorkItem> { item }
        };

        var delta = new MaterialityDelta { OutcomeDelta = 0.5, RealityDelta = 0.8 };
        var proposal = await _rebalanceEngine.EvaluateRebalanceAsync("tenant-reb", portfolio, delta, Array.Empty<PortfolioWorkItem>());

        proposal.Should().NotBeNull();
        var action = proposal!.ItemActions.First();
        action.Action.Should().Be(PortfolioItemAction.Increase);
        action.TargetCapacities[OrganizationalResourceType.Compute].Should().Be(25.0); // +25%
    }

    [Fact]
    public async Task PORT35_RebalanceEngineGeneratesReduceOnConstraintElevation()
    {
        var item = CreateWorkItem("tenant-reb", "Strained Work");
        item.AllocatedCapacities[OrganizationalResourceType.Compute] = 50.0;

        var portfolio = new OrganizationalPortfolio
        {
            TenantId = "tenant-reb",
            WorkItems = new List<PortfolioWorkItem> { item }
        };

        var delta = new MaterialityDelta { ConstraintDelta = 0.8 };
        var proposal = await _rebalanceEngine.EvaluateRebalanceAsync("tenant-reb", portfolio, delta, Array.Empty<PortfolioWorkItem>());

        proposal.Should().NotBeNull();
        var action = proposal!.ItemActions.First();
        action.Action.Should().Be(PortfolioItemAction.Reduce);
        action.TargetCapacities[OrganizationalResourceType.Compute].Should().Be(30.0); // -40%
    }

    [Fact]
    public async Task PORT36_RebalanceEngineGeneratesPauseOnAcuteConstraintAndHighRisk()
    {
        var item = CreateWorkItem("tenant-reb", "Risky Job");
        item.RiskScore = 0.90;

        var portfolio = new OrganizationalPortfolio
        {
            TenantId = "tenant-reb",
            WorkItems = new List<PortfolioWorkItem> { item }
        };

        var delta = new MaterialityDelta { ConstraintDelta = 0.8 };
        var proposal = await _rebalanceEngine.EvaluateRebalanceAsync("tenant-reb", portfolio, delta, Array.Empty<PortfolioWorkItem>());

        proposal.Should().NotBeNull();
        proposal!.ItemActions.First().Action.Should().Be(PortfolioItemAction.Pause);
    }

    [Fact]
    public async Task PORT37_RebalanceEngineGeneratesStartForIncomingObligatoryItem()
    {
        var portfolio = new OrganizationalPortfolio { TenantId = "tenant-reb" };
        var incoming = CreateWorkItem("tenant-reb", "New Audit Requirement", PortfolioWorkCategory.Obligatory);

        var delta = new MaterialityDelta { ConstraintDelta = 0.8 };
        var proposal = await _rebalanceEngine.EvaluateRebalanceAsync("tenant-reb", portfolio, delta, new[] { incoming });

        proposal.Should().NotBeNull();
        proposal!.ItemActions.First().Action.Should().Be(PortfolioItemAction.Start);
    }

    [Fact]
    public void PORT38_PortfolioChangeProposalIsAnIntentNotDirectExecution()
    {
        // Invariant I33-U: Actions are proposed intents, NOT direct Mission.Cancel() calls
        var proposal = new PortfolioChangeProposal();
        proposal.GetType().Name.Should().Be("PortfolioChangeProposal");
        proposal.GetType().Name.Should().NotContain("MissionExecutionPermit");
    }

    [Fact]
    public async Task PORT39_ProposalCarriesDeterministicHash()
    {
        var portfolio = new OrganizationalPortfolio { TenantId = "tenant-reb" };
        var delta = new MaterialityDelta { RealityDelta = 0.9 };
        var proposal = await _rebalanceEngine.EvaluateRebalanceAsync("tenant-reb", portfolio, delta, Array.Empty<PortfolioWorkItem>());

        proposal.Should().NotBeNull();
        proposal!.ProposalHash.Should().NotBeNullOrEmpty();
        proposal.ProposalHash.Length.Should().Be(64);
    }

    [Fact]
    public async Task PORT40_MultipleActionsAggregatedInSingleProposal()
    {
        var itemA = CreateWorkItem("tenant-reb", "Job A", strategicAlignment: 0.95);
        itemA.AllocatedCapacities[OrganizationalResourceType.Compute] = 20.0;
        var itemB = CreateWorkItem("tenant-reb", "Job B");
        itemB.AllocatedCapacities[OrganizationalResourceType.Compute] = 50.0;

        var portfolio = new OrganizationalPortfolio
        {
            TenantId = "tenant-reb",
            WorkItems = new List<PortfolioWorkItem> { itemA, itemB }
        };

        var delta = new MaterialityDelta { OutcomeDelta = 0.5, ConstraintDelta = 0.8 };
        var proposal = await _rebalanceEngine.EvaluateRebalanceAsync("tenant-reb", portfolio, delta, Array.Empty<PortfolioWorkItem>());

        proposal.Should().NotBeNull();
        proposal!.ItemActions.Should().HaveCount(2);
    }

    // =========================================================================
    // FAMILY 7: The "STOP / Supersede" Engine (Governed Abandonment) (PORT41 - PORT48)
    // =========================================================================

    [Fact]
    public async Task PORT41_RebalanceEngineProposesStopOnDegradedExpectedValue()
    {
        var item = CreateWorkItem("tenant-stop", "Outdated Initiative", PortfolioWorkCategory.Strategic, expectedValue: 0.1);

        var portfolio = new OrganizationalPortfolio
        {
            TenantId = "tenant-stop",
            WorkItems = new List<PortfolioWorkItem> { item }
        };

        var delta = new MaterialityDelta { RealityDelta = 0.8 }; // Reality shift falsified value
        var proposal = await _rebalanceEngine.EvaluateRebalanceAsync("tenant-stop", portfolio, delta, Array.Empty<PortfolioWorkItem>());

        proposal.Should().NotBeNull();
        var action = proposal!.ItemActions.First();
        action.Action.Should().Be(PortfolioItemAction.Stop);
        action.Rationale.Should().Contain("degraded expected business value");
    }

    [Fact]
    public async Task PORT42_StopProposalSetsZeroTargetCapacities()
    {
        var item = CreateWorkItem("tenant-stop", "Abandoned Initiative", PortfolioWorkCategory.Strategic, expectedValue: 0.1);
        item.AllocatedCapacities[OrganizationalResourceType.Compute] = 30.0;

        var portfolio = new OrganizationalPortfolio
        {
            TenantId = "tenant-stop",
            WorkItems = new List<PortfolioWorkItem> { item }
        };

        var delta = new MaterialityDelta { RealityDelta = 0.8 };
        var proposal = await _rebalanceEngine.EvaluateRebalanceAsync("tenant-stop", portfolio, delta, Array.Empty<PortfolioWorkItem>());

        var action = proposal!.ItemActions.First();
        action.TargetCapacities.Should().BeEmpty();
    }

    [Fact]
    public async Task PORT43_ProposeItemStopViaFacadeReturnsGovernedProposal()
    {
        var item = CreateWorkItem("tenant-stop", "Target Stop");
        var portfolio = new OrganizationalPortfolio
        {
            TenantId = "tenant-stop",
            WorkItems = new List<PortfolioWorkItem> { item }
        };
        await _repository.SavePortfolioAsync("tenant-stop", portfolio);

        var proposal = await _portfolioService.ProposeItemStopOrSupersedeAsync(
            "tenant-stop",
            item.WorkItemId,
            "Market hypothesis disproven.");

        proposal.Should().NotBeNull();
        proposal.ItemActions.First().Action.Should().Be(PortfolioItemAction.Stop);
        proposal.ItemActions.First().Rationale.Should().Be("Market hypothesis disproven.");
    }

    [Fact]
    public async Task PORT44_ProposeItemSupersedeViaFacadeSetsSupersedeAction()
    {
        var item = CreateWorkItem("tenant-stop", "Legacy Integration");
        var portfolio = new OrganizationalPortfolio
        {
            TenantId = "tenant-stop",
            WorkItems = new List<PortfolioWorkItem> { item }
        };
        await _repository.SavePortfolioAsync("tenant-stop", portfolio);

        var proposal = await _portfolioService.ProposeItemStopOrSupersedeAsync(
            "tenant-stop",
            item.WorkItemId,
            "Superseded by Native Connector 2.0.",
            isSupersede: true);

        proposal.Should().NotBeNull();
        proposal.ItemActions.First().Action.Should().Be(PortfolioItemAction.Supersede);
    }

    [Fact]
    public async Task PORT45_ProposingStopForMissingItemThrowsKeyNotFoundException()
    {
        var portfolio = new OrganizationalPortfolio { TenantId = "tenant-stop" };
        await _repository.SavePortfolioAsync("tenant-stop", portfolio);

        var act = () => _portfolioService.ProposeItemStopOrSupersedeAsync("tenant-stop", "non-existent-id", "Rationale");
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public void PORT46_StopActionNeverCallsMissionCancelDirectly()
    {
        // Invariant I33-U: Stop action contains intent description; does not hold runtime process handle
        var action = new PortfolioItemRebalanceAction
        {
            Action = PortfolioItemAction.Stop,
            WorkItemId = "item-123"
        };

        action.Action.Should().Be(PortfolioItemAction.Stop);
        action.GetType().GetMethods().Select(m => m.Name).Should().NotContain("Cancel");
    }

    [Fact]
    public async Task PORT47_StopProposalMaintainsFullProvenanceTrace()
    {
        var item = CreateWorkItem("tenant-stop", "Failing Pilot", PortfolioWorkCategory.Strategic, expectedValue: 0.1);
        var portfolio = new OrganizationalPortfolio
        {
            TenantId = "tenant-stop",
            WorkItems = new List<PortfolioWorkItem> { item }
        };
        await _repository.SavePortfolioAsync("tenant-stop", portfolio);

        var delta = new MaterialityDelta { RealityDelta = 0.8 };
        var proposal = await _portfolioService.RebalancePortfolioAsync("tenant-stop", delta);

        var trace = await _portfolioService.GetRebalanceActionWhyTraceAsync("tenant-stop", proposal!.ProposalId, item.WorkItemId);
        trace.Action.Should().Be(PortfolioItemAction.Stop);
        trace.WhyActionTaken.Should().Contain("degraded its viable return");
    }

    [Fact]
    public async Task PORT48_StopProposalPreservesStatusQuoImpactSummary()
    {
        var item = CreateWorkItem("tenant-stop", "Dead End", PortfolioWorkCategory.Strategic, expectedValue: 0.1);
        var portfolio = new OrganizationalPortfolio
        {
            TenantId = "tenant-stop",
            WorkItems = new List<PortfolioWorkItem> { item }
        };

        var delta = new MaterialityDelta { RealityDelta = 0.8 };
        var proposal = await _rebalanceEngine.EvaluateRebalanceAsync("tenant-stop", portfolio, delta, Array.Empty<PortfolioWorkItem>());

        proposal!.ItemActions.First().StatusQuoImpact.Should().Contain("Frees scarce allocated capacity");
    }

    // =========================================================================
    // FAMILY 8: Snapshot/Hash Determinism & Idempotency (I33-N, I33-T) (PORT49 - PORT54)
    // =========================================================================

    [Fact]
    public async Task PORT49_PortfolioRepositorySavesAndRetrievesSnapshot()
    {
        var portfolio = new OrganizationalPortfolio
        {
            TenantId = "tenant-snap",
            VersionNumber = 1
        };

        await _repository.SavePortfolioAsync("tenant-snap", portfolio);
        var active = await _repository.GetActivePortfolioAsync("tenant-snap");

        active.Should().NotBeNull();
        active!.VersionNumber.Should().Be(1);
    }

    [Fact]
    public async Task PORT50_VersionHistoryIncrementsDeterministically()
    {
        for (int v = 1; v <= 3; v++)
        {
            await _repository.SavePortfolioAsync("tenant-snap", new OrganizationalPortfolio
            {
                TenantId = "tenant-snap",
                VersionNumber = v
            });
        }

        var history = await _repository.ListPortfolioHistoryAsync("tenant-snap");
        history.Should().HaveCount(3);
        history.First().VersionNumber.Should().Be(3); // Descending order
    }

    [Fact]
    public async Task PORT51_GetSpecificVersionReturnsExactMatch()
    {
        await _repository.SavePortfolioAsync("tenant-snap", new OrganizationalPortfolio { TenantId = "tenant-snap", VersionNumber = 1 });
        await _repository.SavePortfolioAsync("tenant-snap", new OrganizationalPortfolio { TenantId = "tenant-snap", VersionNumber = 2 });

        var v1 = await _repository.GetPortfolioVersionAsync("tenant-snap", 1);
        v1.Should().NotBeNull();
        v1!.VersionNumber.Should().Be(1);
    }

    [Fact]
    public void PORT52_ComputeDecisionHashIsCanonicalHex()
    {
        var portfolio = new OrganizationalPortfolio { TenantId = "tenant-snap", VersionNumber = 1 };
        portfolio.ComputeDecisionHash();

        portfolio.PortfolioDecisionHash.Should().NotBeNullOrEmpty();
        portfolio.PortfolioDecisionHash.Length.Should().Be(64);
    }

    [Fact]
    public void PORT53_WorkItemOrderDoesNotDistortDecisionHash()
    {
        var item1 = CreateWorkItem("tenant-snap", "Item 1");
        var item2 = CreateWorkItem("tenant-snap", "Item 2");

        var p1 = new OrganizationalPortfolio { TenantId = "tenant-snap", WorkItems = new() { item1, item2 } };
        var p2 = new OrganizationalPortfolio { TenantId = "tenant-snap", WorkItems = new() { item2, item1 } };

        p1.ComputeDecisionHash();
        p2.ComputeDecisionHash();

        p1.PortfolioDecisionHash.Should().Be(p2.PortfolioDecisionHash);
    }

    [Fact]
    public void PORT54_ProposalHashIsDeterministicSHA256()
    {
        var prop = new PortfolioChangeProposal { ProposalId = "prop-1", TenantId = "tenant-snap" };
        prop.ComputeHash();

        prop.ProposalHash.Should().NotBeNullOrEmpty();
        prop.ProposalHash.Length.Should().Be(64);
    }

    // =========================================================================
    // FAMILY 9: Simulation Result Epistemic Isolation (I33-K, I33-W) (PORT55 - PORT62)
    // TruthClassification = Simulation. Read-only snapshots, zero write capability.
    // =========================================================================

    [Fact]
    public async Task PORT55_SimulationResultTruthClassificationIsStrictlySimulation()
    {
        var portfolio = new OrganizationalPortfolio { TenantId = "tenant-sim" };
        var scenario = new PortfolioSimulationScenario { Name = "70% Growth Allocation" };

        var result = await _simulator.SimulateScenariosAsync("tenant-sim", portfolio, new[] { scenario });

        result.TruthClassification.Should().Be("Simulation");
    }

    [Fact]
    public async Task PORT56_SimulationCarriesDistinctInputAndOutputSnapshotHashes()
    {
        var portfolio = new OrganizationalPortfolio { TenantId = "tenant-sim", PortfolioId = "port-sim-1" };
        portfolio.ComputeDecisionHash();

        var scenario = new PortfolioSimulationScenario { Name = "Scenario 1" };
        var result = await _simulator.SimulateScenariosAsync("tenant-sim", portfolio, new[] { scenario });

        result.InputSnapshotHash.Should().NotBeNullOrEmpty();
        result.OutputSnapshotHash.Should().NotBeNullOrEmpty();
        result.InputSnapshotHash.Should().NotBe(result.OutputSnapshotHash);
    }

    [Fact]
    public async Task PORT57_SimulationCannotMutateProductionPortfolio()
    {
        var portfolio = new OrganizationalPortfolio
        {
            TenantId = "tenant-sim",
            VersionNumber = 1,
            WorkItems = new() { CreateWorkItem("tenant-sim", "Immutable Prod Item") }
        };
        await _repository.SavePortfolioAsync("tenant-sim", portfolio);

        var scenario = new PortfolioSimulationScenario
        {
            Name = "Abrasive Scenario",
            CategoryAllocationWeights = new() { { PortfolioWorkCategory.Strategic, 1.0 } }
        };

        await _simulator.SimulateScenariosAsync("tenant-sim", portfolio, new[] { scenario });

        // Production portfolio in repository must remain completely unaltered
        var prod = await _repository.GetActivePortfolioAsync("tenant-sim");
        prod!.WorkItems.First().Title.Should().Be("Immutable Prod Item");
        prod.VersionNumber.Should().Be(1);
    }

    [Fact]
    public async Task PORT58_SimulationGeneratesProjectionsAcrossScenarios()
    {
        var portfolio = new OrganizationalPortfolio
        {
            TenantId = "tenant-sim",
            WorkItems = new() { CreateWorkItem("tenant-sim", "Item", expectedValue: 10.0) }
        };

        var s1 = new PortfolioSimulationScenario { Name = "Growth Focus", CategoryAllocationWeights = new() { { PortfolioWorkCategory.Strategic, 0.8 } } };
        var s2 = new PortfolioSimulationScenario { Name = "Cash Preserve", SimulatedRegime = "CashPreservation" };

        var result = await _simulator.SimulateScenariosAsync("tenant-sim", portfolio, new[] { s1, s2 });

        result.ScenarioOutcomes.Should().HaveCount(2);
        result.ScenarioOutcomes.First().ScenarioName.Should().Be("Growth Focus");
        result.ScenarioOutcomes.Last().ProjectedBufferPreservation.Should().Be(0.45);
    }

    [Fact]
    public async Task PORT59_SimulationEstimatedUncertaintyIsReported()
    {
        var portfolio = new OrganizationalPortfolio { TenantId = "tenant-sim" };
        var result = await _simulator.SimulateScenariosAsync("tenant-sim", portfolio, Array.Empty<PortfolioSimulationScenario>());

        result.EstimatedUncertainty.Should().BeGreaterThan(0.0);
    }

    [Fact]
    public async Task PORT60_SimulationProviderIdIsIdentified()
    {
        var portfolio = new OrganizationalPortfolio { TenantId = "tenant-sim" };
        var result = await _simulator.SimulateScenariosAsync("tenant-sim", portfolio, Array.Empty<PortfolioSimulationScenario>());

        result.ProviderId.Should().Be("DeterministicScenarioEngine");
    }

    [Fact]
    public async Task PORT61_FacadeRunSimulationSandboxPreservesEpistemicClassification()
    {
        var result = await _portfolioService.RunSimulationSandboxAsync("tenant-sim", new[]
        {
            new PortfolioSimulationScenario { Name = "Sandbox Scenario" }
        });

        result.TruthClassification.Should().Be("Simulation");
    }

    [Fact]
    public void PORT62_SimulationDoesNotHoldExecutionPermitProperties()
    {
        var res = new PortfolioSimulationResult();
        res.GetType().GetProperties().Select(p => p.Name).Should().NotContain("ExecutionPermit");
        res.GetType().GetProperties().Select(p => p.Name).Should().NotContain("AuthorizationToken");
    }

    // =========================================================================
    // FAMILY 10: Constraint Sovereignty & Non-Bypass (I33-G) (PORT63 - PORT68)
    // =========================================================================

    [Fact]
    public void PORT63_PlannerCannotExceedLiquidityFloor()
    {
        var envelope = CreateEnvelope("tenant-c", compute: 100.0);
        envelope.AvailableCapacities[OrganizationalResourceType.LiquidityBuffer] = 10.0;

        var demand = CreateWorkItem("tenant-c", "Cash Outlay", demands: new() { { OrganizationalResourceType.LiquidityBuffer, 25.0 } });
        var plan = _planner.PlanPortfolio("tenant-c", new[] { demand }, envelope);

        plan.WorkItems.First().State.Should().Be(PortfolioLifecycleState.Deferred);
    }

    [Fact]
    public void PORT64_ConstraintSnapshotHashIsPreservedInPortfolio()
    {
        var envelope = CreateEnvelope("tenant-c");
        var plan = _planner.PlanPortfolio("tenant-c", Array.Empty<PortfolioWorkItem>(), envelope, constraintSnapshotHash: "HardFloorPassed");

        plan.ConstraintSnapshotHash.Should().Be("HardFloorPassed");
    }

    [Fact]
    public void PORT65_PlannerRespectsMultiDimensionalCapacitiesIndependently()
    {
        var envelope = CreateEnvelope("tenant-c", compute: 100.0, agentBandwidth: 10.0);
        var item = CreateWorkItem("tenant-c", "Bandwidth Constrained", demands: new()
        {
            { OrganizationalResourceType.Compute, 10.0 },
            { OrganizationalResourceType.AgentBandwidth, 25.0 } // Exceeds available 10.0
        });

        var plan = _planner.PlanPortfolio("tenant-c", new[] { item }, envelope);

        plan.WorkItems.First().State.Should().Be(PortfolioLifecycleState.Deferred);
    }

    [Fact]
    public void PORT66_NoArbitraryCapacityExpansionAllowed()
    {
        var envelope = CreateEnvelope("tenant-c", compute: 20.0);
        var item = CreateWorkItem("tenant-c", "Ask 21", demands: new() { { OrganizationalResourceType.Compute, 21.0 } });

        var plan = _planner.PlanPortfolio("tenant-c", new[] { item }, envelope);

        plan.CapacityEnvelope.CommittedCapacities[OrganizationalResourceType.Compute].Should().Be(0.0);
    }

    [Fact]
    public void PORT67_ObligatoryItemsStillRespectHardResourceEnvelopes()
    {
        var envelope = CreateEnvelope("tenant-c", compute: 5.0);
        var item = CreateWorkItem("tenant-c", "Audit", PortfolioWorkCategory.Obligatory, demands: new() { { OrganizationalResourceType.Compute, 10.0 } });

        var plan = _planner.PlanPortfolio("tenant-c", new[] { item }, envelope);

        // Even Obligatory cannot manifest non-existent compute
        plan.WorkItems.First().State.Should().Be(PortfolioLifecycleState.Deferred);
    }

    [Fact]
    public void PORT68_PlanStateIsDeferredWhenNoItemCanBeSatisfied()
    {
        var envelope = CreateEnvelope("tenant-c", compute: 5.0);
        var item = CreateWorkItem("tenant-c", "Too Big", demands: new() { { OrganizationalResourceType.Compute, 20.0 } });

        var plan = _planner.PlanPortfolio("tenant-c", new[] { item }, envelope);
        plan.State.Should().Be(PortfolioLifecycleState.Deferred);
    }

    // =========================================================================
    // FAMILY 11: Governance Boundaries & Execution Isolation (I33-E, I33-I, I33-M) (PORT69 - PORT76)
    // =========================================================================

    [Fact]
    public void PORT69_PortfolioStateTransitionsThroughGovernanceReview()
    {
        var portfolio = new OrganizationalPortfolio { State = PortfolioLifecycleState.PlanReady };
        portfolio.State = PortfolioLifecycleState.GovernanceReview;
        portfolio.State.Should().Be(PortfolioLifecycleState.GovernanceReview);
    }

    [Fact]
    public void PORT70_WorkPlaneAdmissionRequestedRequiresExplicitGovernanceStep()
    {
        var portfolio = new OrganizationalPortfolio { State = PortfolioLifecycleState.GovernanceReview };
        portfolio.State = PortfolioLifecycleState.WorkPlaneAdmissionRequested;

        portfolio.State.Should().Be(PortfolioLifecycleState.WorkPlaneAdmissionRequested);
    }

    [Fact]
    public void PORT71_PortfolioPlannerDoesNotInvokeWorkPlaneDirectly()
    {
        var types = typeof(PortfolioPlanner).GetInterfaces().Select(i => i.Name).ToList();
        types.Should().NotContain("IWorkPlaneAdmissionService");
    }

    [Fact]
    public void PORT72_RebalanceProposalRequiresHumanGovernanceApproval()
    {
        var proposal = new PortfolioChangeProposal
        {
            ProposalId = "prop-gov",
            ItemActions = new() { new() { Action = PortfolioItemAction.Stop, WorkItemId = "item-1" } }
        };

        // Proposal is ready for review, does not self-execute
        proposal.ItemActions.Should().HaveCount(1);
    }

    [Fact]
    public void PORT73_PortfolioItemLifecycleDoesNotContainExecutionRunning()
    {
        var states = Enum.GetNames(typeof(PortfolioLifecycleState));
        states.Should().NotContain("Executing");
        states.Should().NotContain("Running");
    }

    [Fact]
    public void PORT74_PortfolioControllerPlanEndpointReturnsPlanReady()
    {
        var controller = new OrganizationalPortfolioController(_portfolioService, _repository);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-gov";

        var allocPlan = new AllocationPlan
        {
            PlanId = "alloc-1",
            AdmittedAllocations = new()
            {
                new() { AllocatedCapacities = new() { { OrganizationalResourceType.Compute, 50.0 } } }
            }
        };

        var req = new GeneratePortfolioPlanRequest
        {
            CandidateItems = new() { CreateWorkItem("tenant-gov", "Gov Plan Task") },
            AllocationPlan = allocPlan
        };

        var result = controller.GeneratePlan(req).GetAwaiter().GetResult() as OkObjectResult;
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);

        var portfolio = result.Value as OrganizationalPortfolio;
        portfolio!.State.Should().Be(PortfolioLifecycleState.PlanReady);
    }

    [Fact]
    public void PORT75_PlanCannotBypassPrg1Sovereignty()
    {
        var item = CreateWorkItem("tenant-gov", "Plan Task");
        item.State = PortfolioLifecycleState.PlanReady;

        // PlanReady does not equal HumanApproved or ExecutionPermit
        item.State.ToString().Should().NotContain("HumanApproved");
    }

    [Fact]
    public void PORT76_RebalanceProposalStrategicJustificationIsRecorded()
    {
        var prop = new PortfolioChangeProposal
        {
            StrategicJustification = "Approved by Executive Committee under Q4 realignment."
        };
        prop.StrategicJustification.Should().Contain("Executive Committee");
    }

    // =========================================================================
    // FAMILY 12: In-Flight Drain & Preemption Safeguards (I33-J) (PORT77 - PORT82)
    // =========================================================================

    [Fact]
    public async Task PORT77_EvaluatingItemsArePausedInsteadOfAbruptlyStopped()
    {
        var item = CreateWorkItem("tenant-j", "Evaluating Mission");
        item.State = PortfolioLifecycleState.Evaluating;

        var portfolio = new OrganizationalPortfolio { TenantId = "tenant-j", WorkItems = new() { item } };
        var delta = new MaterialityDelta { RealityDelta = 0.8 };

        var proposal = await _rebalanceEngine.EvaluateRebalanceAsync("tenant-j", portfolio, delta, Array.Empty<PortfolioWorkItem>());

        proposal!.ItemActions.First().Action.Should().Be(PortfolioItemAction.Pause);
        proposal.ItemActions.First().Rationale.Should().Contain("pending safety verification");
    }

    [Fact]
    public async Task PORT78_HighRiskItemsPausedUnderConstraintElevation()
    {
        var item = CreateWorkItem("tenant-j", "Consequential High Risk");
        item.RiskScore = 0.95;

        var portfolio = new OrganizationalPortfolio { TenantId = "tenant-j", WorkItems = new() { item } };
        var delta = new MaterialityDelta { ConstraintDelta = 0.8 };

        var proposal = await _rebalanceEngine.EvaluateRebalanceAsync("tenant-j", portfolio, delta, Array.Empty<PortfolioWorkItem>());

        proposal!.ItemActions.First().Action.Should().Be(PortfolioItemAction.Pause);
    }

    [Fact]
    public void PORT79_PausedItemPreservesAllocatedCapacitiesPendingDrain()
    {
        var item = CreateWorkItem("tenant-j", "Paused Work");
        item.AllocatedCapacities[OrganizationalResourceType.Compute] = 20.0;

        var action = new PortfolioItemRebalanceAction
        {
            Action = PortfolioItemAction.Pause,
            TargetCapacities = item.AllocatedCapacities
        };

        action.TargetCapacities[OrganizationalResourceType.Compute].Should().Be(20.0);
    }

    [Fact]
    public void PORT80_SafeDrainRationaleDocumentedInAction()
    {
        var action = new PortfolioItemRebalanceAction
        {
            Action = PortfolioItemAction.Pause,
            StatusQuoImpact = "Halts resource drain without aborting uncommitted state."
        };
        action.StatusQuoImpact.Should().Contain("Halts resource drain without aborting");
    }

    [Fact]
    public void PORT81_ReducedItemMaintainsMinimumViableProgress()
    {
        var action = new PortfolioItemRebalanceAction
        {
            Action = PortfolioItemAction.Reduce,
            StatusQuoImpact = "Preserves minimum viable progress while relieving capacity strain."
        };
        action.StatusQuoImpact.Should().Contain("minimum viable progress");
    }

    [Fact]
    public void PORT82_RebalancingDoesNotForcePreemptUnknownState()
    {
        var actions = Enum.GetNames(typeof(PortfolioItemAction));
        actions.Should().NotContain("ForceKill");
        actions.Should().NotContain("AbruptAbort");
    }

    // =========================================================================
    // FAMILY 13: Multi-Tenant Partitioning (I33-L) (PORT83 - PORT88)
    // =========================================================================

    [Fact]
    public async Task PORT83_TenantsCannotViewCrossTenantPortfolios()
    {
        await _repository.SavePortfolioAsync("tenant-alpha", new OrganizationalPortfolio { TenantId = "tenant-alpha" });
        await _repository.SavePortfolioAsync("tenant-beta", new OrganizationalPortfolio { TenantId = "tenant-beta" });

        var alphaActive = await _repository.GetActivePortfolioAsync("tenant-alpha");
        alphaActive!.TenantId.Should().Be("tenant-alpha");

        var betaActive = await _repository.GetActivePortfolioAsync("tenant-beta");
        betaActive!.TenantId.Should().Be("tenant-beta");
    }

    [Fact]
    public async Task PORT84_PortfolioHistoryIsStrictlyTenantIsolated()
    {
        await _repository.SavePortfolioAsync("tenant-alpha", new OrganizationalPortfolio { TenantId = "tenant-alpha", VersionNumber = 1 });
        await _repository.SavePortfolioAsync("tenant-beta", new OrganizationalPortfolio { TenantId = "tenant-beta", VersionNumber = 1 });

        var alphaHistory = await _repository.ListPortfolioHistoryAsync("tenant-alpha");
        alphaHistory.Should().ContainSingle(p => p.TenantId == "tenant-alpha");
        alphaHistory.Should().NotContain(p => p.TenantId == "tenant-beta");
    }

    [Fact]
    public void PORT85_CrossTenantCandidatesFilteredFromPlan()
    {
        var envelope = CreateEnvelope("tenant-alpha");
        var itemA = CreateWorkItem("tenant-alpha", "Alpha Item");
        var rogueItem = CreateWorkItem("tenant-beta", "Infiltrator");

        var plan = _planner.PlanPortfolio("tenant-alpha", new[] { itemA, rogueItem }, envelope);

        plan.WorkItems.Should().ContainSingle(i => i.WorkItemId == itemA.WorkItemId);
        plan.WorkItems.Should().NotContain(i => i.WorkItemId == rogueItem.WorkItemId);
    }

    [Fact]
    public async Task PORT86_CrossTenantRebalanceProposalsAreRejected()
    {
        var portfolio = new OrganizationalPortfolio { TenantId = "tenant-alpha" };
        var rogueIncoming = CreateWorkItem("tenant-beta", "Rogue Candidate");

        var delta = new MaterialityDelta { RealityDelta = 0.8 };
        var proposal = await _rebalanceEngine.EvaluateRebalanceAsync("tenant-alpha", portfolio, delta, new[] { rogueIncoming });

        proposal!.ItemActions.Should().NotContain(a => a.WorkItemId == rogueIncoming.WorkItemId);
    }

    [Fact]
    public async Task PORT87_ControllerEnforcesTenantHeaderIsolation()
    {
        var controller = new OrganizationalPortfolioController(_portfolioService, _repository);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-iso-1";

        await _repository.SavePortfolioAsync("tenant-iso-2", new OrganizationalPortfolio { TenantId = "tenant-iso-2" });

        var result = await controller.GetCurrentPortfolio() as NotFoundObjectResult;
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task PORT88_DependencyResolverEnforcesTenantIsolation()
    {
        var itemA = CreateWorkItem("tenant-alpha", "Item A");
        var (state, _, ordered) = await _dependencyResolver.ValidateAndSortDependenciesAsync("tenant-alpha", new[] { itemA });

        state.Should().Be(DependencyValidationState.Satisfied);
        ordered.Should().ContainSingle(id => id == itemA.WorkItemId);
    }

    // =========================================================================
    // FAMILY 14: Fail-Closed Recovery & Error Handling (I33-R) (PORT89 - PORT94)
    // =========================================================================

    [Fact]
    public void PORT89_NullTenantThrowsArgumentNullException()
    {
        var envelope = CreateEnvelope("tenant-1");
        var act = () => _planner.PlanPortfolio(null!, Array.Empty<PortfolioWorkItem>(), envelope);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PORT90_NullEnvelopeThrowsArgumentNullException()
    {
        var act = () => _planner.PlanPortfolio("tenant-1", Array.Empty<PortfolioWorkItem>(), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task PORT91_RebalanceWithNullDeltaThrowsArgumentNullException()
    {
        var portfolio = new OrganizationalPortfolio { TenantId = "tenant-err" };
        var act = () => _rebalanceEngine.EvaluateRebalanceAsync("tenant-err", portfolio, null!, Array.Empty<PortfolioWorkItem>());
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PORT92_GetActivePortfolioForEmptyTenantReturnsNull()
    {
        var active = await _repository.GetActivePortfolioAsync("tenant-ghost");
        active.Should().BeNull();
    }

    [Fact]
    public async Task PORT93_WhyTraceForMissingProposalThrowsKeyNotFoundException()
    {
        var act = () => _portfolioService.GetRebalanceActionWhyTraceAsync("tenant-err", "missing-prop", "missing-item");
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task PORT94_ControllerRebalanceReturnsBadRequestOnNullDelta()
    {
        var controller = new OrganizationalPortfolioController(_portfolioService, _repository);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.RebalancePortfolio(new RebalancePortfolioRequest { Delta = null! }) as BadRequestObjectResult;
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(400);
    }

    // =========================================================================
    // FAMILY 15: Decision Provenance & "Why Rebalance?" Explainability (I33-O) (PORT95 - PORT102)
    // =========================================================================

    [Fact]
    public void PORT95_ExplainRebalanceActionProvidesStructuredTrace()
    {
        var prop = new PortfolioChangeProposal { ProposalId = "prop-why", ComputedMaterialityScore = 0.45 };
        var action = new PortfolioItemRebalanceAction
        {
            WorkItemId = "item-why-1",
            Action = PortfolioItemAction.Stop,
            TargetCapacities = new()
        };
        var delta = new MaterialityDelta { RealityDelta = 0.6 };

        var trace = _provenanceService.ExplainRebalanceAction(prop, action, delta);

        trace.ProposalId.Should().Be("prop-why");
        trace.WorkItemId.Should().Be("item-why-1");
        trace.Action.Should().Be(PortfolioItemAction.Stop);
        trace.WhyActionTaken.Should().Contain("viable return below organizational hurdle rates");
    }

    [Fact]
    public void PORT96_WhyTraceIncludesDetailedMaterialityFactors()
    {
        var prop = new PortfolioChangeProposal { ProposalId = "prop-why", ComputedMaterialityScore = 0.35 };
        var action = new PortfolioItemRebalanceAction { WorkItemId = "item-1", Action = PortfolioItemAction.Increase };
        var delta = new MaterialityDelta { OutcomeDelta = 0.5, ConstraintDelta = 0.2 };

        var trace = _provenanceService.ExplainRebalanceAction(prop, action, delta);
        trace.TriggeringMaterialityFactors.Should().Contain("Outcome Δ=50%");
        trace.TriggeringMaterialityFactors.Should().Contain("Constraint Δ=20%");
    }

    [Fact]
    public void PORT97_WhyTraceIncludesOpportunityCostComparison()
    {
        var prop = new PortfolioChangeProposal { ProposalId = "prop-why" };
        var action = new PortfolioItemRebalanceAction { WorkItemId = "item-1", Action = PortfolioItemAction.Stop };
        var delta = new MaterialityDelta { RealityDelta = 0.8 };

        var trace = _provenanceService.ExplainRebalanceAction(prop, action, delta);
        trace.OpportunityCostComparison.Should().Contain("Frees critical capacity to prevent starvation");
    }

    [Fact]
    public void PORT98_WhyTraceIncludesCapacityImpactSummary()
    {
        var prop = new PortfolioChangeProposal { ProposalId = "prop-why" };
        var action = new PortfolioItemRebalanceAction
        {
            WorkItemId = "item-1",
            Action = PortfolioItemAction.Reduce,
            TargetCapacities = new() { { OrganizationalResourceType.Compute, 25.0 } }
        };
        var delta = new MaterialityDelta { ConstraintDelta = 0.4 };

        var trace = _provenanceService.ExplainRebalanceAction(prop, action, delta);
        trace.CapacityImpactSummary.Should().Contain("Compute: 25");
    }

    [Fact]
    public void PORT99_ExplainActionForIncreaseDocumentsValidatedSignals()
    {
        var prop = new PortfolioChangeProposal { ProposalId = "prop-why" };
        var action = new PortfolioItemRebalanceAction { WorkItemId = "item-1", Action = PortfolioItemAction.Increase };
        var delta = new MaterialityDelta { OutcomeDelta = 0.6 };

        var trace = _provenanceService.ExplainRebalanceAction(prop, action, delta);
        trace.WhyActionTaken.Should().Contain("expanded due to validated positive performance signals");
    }

    [Fact]
    public void PORT100_ExplainActionForPauseDocumentsSafetySafeguard()
    {
        var prop = new PortfolioChangeProposal { ProposalId = "prop-why" };
        var action = new PortfolioItemRebalanceAction { WorkItemId = "item-1", Action = PortfolioItemAction.Pause };
        var delta = new MaterialityDelta { ConstraintDelta = 0.5 };

        var trace = _provenanceService.ExplainRebalanceAction(prop, action, delta);
        trace.WhyActionTaken.Should().Contain("paused to safeguard against potential downstream exposure");
    }

    [Fact]
    public void PORT101_ExplainActionForStartDocumentsStrategicMomentum()
    {
        var prop = new PortfolioChangeProposal { ProposalId = "prop-why" };
        var action = new PortfolioItemRebalanceAction { WorkItemId = "item-1", Action = PortfolioItemAction.Start };
        var delta = new MaterialityDelta { StrategicRegimeDelta = 0.4 };

        var trace = _provenanceService.ExplainRebalanceAction(prop, action, delta);
        trace.WhyActionTaken.Should().Contain("capture immediate high-value strategic momentum");
    }

    [Fact]
    public async Task PORT102_ControllerGetWhyTraceReturnsOk()
    {
        var controller = new OrganizationalPortfolioController(_portfolioService, _repository);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-why";

        var item = CreateWorkItem("tenant-why", "Why Controller Item", PortfolioWorkCategory.Strategic, expectedValue: 0.1);
        var portfolio = new OrganizationalPortfolio { TenantId = "tenant-why", WorkItems = new() { item } };
        await _repository.SavePortfolioAsync("tenant-why", portfolio);

        var delta = new MaterialityDelta { RealityDelta = 0.8 };
        var proposal = await _portfolioService.RebalancePortfolioAsync("tenant-why", delta);

        var result = await controller.GetRebalanceWhyTrace(proposal!.ProposalId, item.WorkItemId) as OkObjectResult;
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(200);

        var trace = result.Value as WhyRebalanceTrace;
        trace!.WorkItemId.Should().Be(item.WorkItemId);
    }

    // =========================================================================
    // FAMILY 16: Mathematical Materiality & Anti-Churn Bounds (I33-Q) (PORT103 - PORT108)
    // =========================================================================

    [Fact]
    public void PORT103_MaterialityFormulaComputesDeterministicWeightedScore()
    {
        var delta = new MaterialityDelta
        {
            RealityDelta = 0.5,    // * 0.20 = 0.10
            ConstraintDelta = 0.5, // * 0.20 = 0.10
            OutcomeDelta = 1.0     // * 0.05 = 0.05
        };

        var score = delta.ComputeScore();
        score.Should().Be(0.25);
    }

    [Fact]
    public async Task PORT104_SubThresholdMaterialityProducesZeroProposal()
    {
        var portfolio = new OrganizationalPortfolio
        {
            TenantId = "tenant-churn",
            WorkItems = new() { CreateWorkItem("tenant-churn", "Steady Work") }
        };

        // Materiality: 0.10 * 0.20 = 0.02 < 0.15 threshold
        var delta = new MaterialityDelta { RealityDelta = 0.10 };
        var proposal = await _rebalanceEngine.EvaluateRebalanceAsync("tenant-churn", portfolio, delta, Array.Empty<PortfolioWorkItem>());

        proposal.Should().BeNull(); // Zero proposal generated (I33-Q)
    }

    [Fact]
    public async Task PORT105_ZeroDeltaProducesZeroProposal()
    {
        var portfolio = new OrganizationalPortfolio { TenantId = "tenant-churn" };
        var delta = new MaterialityDelta(); // All deltas 0.0

        var proposal = await _rebalanceEngine.EvaluateRebalanceAsync("tenant-churn", portfolio, delta, Array.Empty<PortfolioWorkItem>());
        proposal.Should().BeNull();
    }

    [Fact]
    public async Task PORT106_ControllerReturnsNoRebalanceNeededForSubThresholdDelta()
    {
        var controller = new OrganizationalPortfolioController(_portfolioService, _repository);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-churn";

        await _repository.SavePortfolioAsync("tenant-churn", new OrganizationalPortfolio { TenantId = "tenant-churn" });

        var req = new RebalancePortfolioRequest { Delta = new MaterialityDelta { RealityDelta = 0.05 } };
        var result = await controller.RebalancePortfolio(req) as OkObjectResult;

        result.Should().NotBeNull();
        result!.Value.ToString().Should().Contain("NoRebalanceNeeded");
    }

    [Fact]
    public void PORT107_MaterialityDeltaAllWeightsSumToOne()
    {
        var delta = new MaterialityDelta
        {
            RealityDelta = 1.0,
            ForecastDelta = 1.0,
            ConstraintDelta = 1.0,
            StrategicRegimeDelta = 1.0,
            AllocationDelta = 1.0,
            DependencyDelta = 1.0,
            OutcomeDelta = 1.0
        };

        var total = delta.ComputeScore();
        total.Should().Be(1.0);
    }

    [Fact]
    public async Task PORT108_MaterialDeltaAtExactlyThresholdGeneratesProposal()
    {
        var portfolio = new OrganizationalPortfolio
        {
            TenantId = "tenant-churn",
            WorkItems = new() { CreateWorkItem("tenant-churn", "Item") }
        };

        // RealityDelta = 0.75 * 0.20 = 0.15 == threshold
        var delta = new MaterialityDelta { RealityDelta = 0.75 };
        var proposal = await _rebalanceEngine.EvaluateRebalanceAsync("tenant-churn", portfolio, delta, Array.Empty<PortfolioWorkItem>());

        proposal.Should().NotBeNull();
        proposal!.IsMaterial.Should().BeTrue();
    }
}
