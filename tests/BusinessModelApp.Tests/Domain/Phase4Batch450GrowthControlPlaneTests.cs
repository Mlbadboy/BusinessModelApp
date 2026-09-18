using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Growth;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase4Batch450GrowthControlPlaneTests
    {
        private readonly InMemoryGrowthControlPlaneStore _store;
        private readonly GrowthControlPlaneService _service;

        public Phase4Batch450GrowthControlPlaneTests()
        {
            _store = new InMemoryGrowthControlPlaneStore();
            _service = new GrowthControlPlaneService(_store);
        }

        [Fact]
        public void ConstitutionalInvariants_LawI41_FullCoverage()
        {
            Assert.Contains("MARKET SIGNAL != DEMAND != LEAD != CUSTOMER != REVENUE != PROFIT", GrowthConstitutionalInvariants.Axiom);
            Assert.Contains("AGENT ACTIVITY != COMMERCIAL PROGRESS != BUSINESS GROWTH", GrowthConstitutionalInvariants.Axiom);
            Assert.Contains("REVENUE != PROFIT", GrowthConstitutionalInvariants.Axiom);
            Assert.Equal(26, GrowthConstitutionalInvariants.AllLaws.Count);
            Assert.Equal(3.0m, GrowthConstitutionalInvariants.MinLtvToCacRatio);
            Assert.Equal(12, GrowthConstitutionalInvariants.MaxPaybackPeriodMonths);
            Assert.Equal(35.0m, GrowthConstitutionalInvariants.MinGrossMarginPercent);
        }

        [Fact]
        public async Task CreateGrowthObjective_Valid_PersistsAndValidatesInvariants()
        {
            var obj = new GrowthObjective
            {
                TenantId = "tenant-growth-01",
                BusinessObjectiveId = "biz-obj-2026-q3",
                Title = "Scale Health Supply Chain Autonomous Operations in Western India",
                TargetRevenueINR = 7500000m,
                TargetGrossMarginPercent = 55.0m,
                TargetLtvCacRatio = 4.2m,
                MaxPaybackPeriodMonths = 6,
                TargetNetRetentionRatePercent = 115.0m
            };

            var created = await _service.CreateGrowthObjectiveAsync(obj);

            Assert.NotNull(created);
            Assert.True(created.IsEconomicallySustainable);
            Assert.Equal(GrowthObjectiveStatus.PLANNED, created.Status);
        }

        [Fact]
        public async Task CreateGrowthObjective_LtvCacBelowFloor_ThrowsInvalidOperationException()
        {
            var obj = new GrowthObjective
            {
                TenantId = "tenant-growth-01",
                BusinessObjectiveId = "biz-obj-2026-q3",
                Title = "Unprofitable Low-LTV Acquisition Spree",
                TargetLtvCacRatio = 2.1m // < 3.0 floor!
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateGrowthObjectiveAsync(obj));

            Assert.Contains("Law I41-G", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateGrowthObjective_PaybackExceedsMax_ThrowsInvalidOperationException()
        {
            var obj = new GrowthObjective
            {
                TenantId = "tenant-growth-01",
                BusinessObjectiveId = "biz-obj-2026-q3",
                Title = "Slow Payback Plan",
                MaxPaybackPeriodMonths = 18 // > 12m limit!
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateGrowthObjectiveAsync(obj));

            Assert.Contains("Law I41-H", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateGrowthObjective_MarginBelowFloor_ThrowsInvalidOperationException()
        {
            var obj = new GrowthObjective
            {
                TenantId = "tenant-growth-01",
                BusinessObjectiveId = "biz-obj-2026-q3",
                Title = "Margin Destructive Offering",
                TargetGrossMarginPercent = 25.0m // < 35% floor!
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateGrowthObjectiveAsync(obj));

            Assert.Contains("Law I41-N", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateGrowthObjective_MissingBusinessObjectiveLink_ThrowsArgumentException()
        {
            var obj = new GrowthObjective
            {
                TenantId = "tenant-growth-01",
                BusinessObjectiveId = "", // Missing parent link!
                Title = "Detached Floating Objective"
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.CreateGrowthObjectiveAsync(obj));

            Assert.Contains("Law I41-Y", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AuthorizeGrowthObjective_MissingSignoff_ThrowsInvalidOperationException()
        {
            var obj = new GrowthObjective
            {
                TenantId = "tenant-growth-01",
                BusinessObjectiveId = "biz-obj-parent-1",
                Title = "Governed Strategic Goal"
            };
            var created = await _service.CreateGrowthObjectiveAsync(obj);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AuthorizeGrowthObjectiveAsync("tenant-growth-01", created.ObjectiveId, ""));

            Assert.Contains("PRG-1 human signoff ID", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AuthorizeGrowthObjective_WithSignoff_ActivatesObjective()
        {
            var obj = new GrowthObjective
            {
                TenantId = "tenant-growth-01",
                BusinessObjectiveId = "biz-obj-parent-2",
                Title = "Enterprise Growth Trajectory"
            };
            var created = await _service.CreateGrowthObjectiveAsync(obj);

            bool authorized = await _service.AuthorizeGrowthObjectiveAsync("tenant-growth-01", created.ObjectiveId, "prg1-ceo-signoff");

            Assert.True(authorized);
            var updated = await _service.GetGrowthObjectiveAsync("tenant-growth-01", created.ObjectiveId);
            Assert.NotNull(updated);
            Assert.Equal(GrowthObjectiveStatus.ACTIVE, updated.Status);
            Assert.Equal("prg1-ceo-signoff", updated.HumanSignoffId);
        }

        [Fact]
        public async Task FormulateAndAuthorizeStrategyPlan_EnforcesGovernance()
        {
            var obj = new GrowthObjective
            {
                TenantId = "tenant-growth-01",
                BusinessObjectiveId = "biz-obj-parent-3",
                Title = "Account-Based Acquisition Goal"
            };
            var createdObj = await _service.CreateGrowthObjectiveAsync(obj);

            var plan = new GrowthStrategyPlan
            {
                TenantId = "tenant-growth-01",
                GrowthObjectiveId = createdObj.ObjectiveId,
                Name = "Mid-Market Logistics Strategic Push",
                TargetIcpSegment = "Healthcare Logistics ₹50Cr-₹200Cr",
                AllocatedBudgetINR = 500000m
            };
            var createdPlan = await _service.FormulateStrategyPlanAsync(plan);
            Assert.False(createdPlan.IsAuthorized);

            bool authorized = await _service.AuthorizeStrategyPlanAsync("tenant-growth-01", createdPlan.StrategyId, "prg1-cro-signoff");
            Assert.True(authorized);

            var updatedPlan = await _service.GetStrategyPlanAsync("tenant-growth-01", createdPlan.StrategyId);
            Assert.NotNull(updatedPlan);
            Assert.True(updatedPlan.IsAuthorized);
        }
    }
}
