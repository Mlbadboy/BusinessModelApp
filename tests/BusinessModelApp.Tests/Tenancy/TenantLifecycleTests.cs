using System;
using BusinessModelApp.Core.Domain.Tenancy;
using BusinessModelApp.Core.Services;
using Xunit;

namespace BusinessModelApp.Tests.Tenancy
{
    public class TenantLifecycleTests
    {
        private readonly TenantLifecycleService _lifecycleService = new TenantLifecycleService();

        [Fact]
        public void Suspended_Tenant_Cannot_Login_Or_Mutate_Or_Call_AI()
        {
            // Arrange
            var policy = new TenantPolicy
            {
                OrganizationId = Guid.NewGuid(),
                State = TenantLifecycleState.Suspended,
                SuspensionReason = "Payment Delinquency"
            };

            // Act & Assert
            var loginCheck = _lifecycleService.EvaluateAccess(policy, "login");
            Assert.False(loginCheck.IsAuthorized);
            Assert.False(loginCheck.CanLogin);

            var writeCheck = _lifecycleService.EvaluateAccess(policy, "write");
            Assert.False(writeCheck.IsAuthorized);
            Assert.False(writeCheck.CanWrite);

            var aiCheck = _lifecycleService.EvaluateAccess(policy, "ai");
            Assert.False(aiCheck.IsAuthorized);
            Assert.False(aiCheck.CanUseAI);
        }

        [Fact]
        public void ReadOnly_Tenant_Can_Login_Read_And_Run_AI_Analysis_But_Cannot_Mutate()
        {
            // Arrange
            var policy = new TenantPolicy
            {
                OrganizationId = Guid.NewGuid(),
                State = TenantLifecycleState.ReadOnly
            };

            // Act & Assert
            var loginCheck = _lifecycleService.EvaluateAccess(policy, "login");
            Assert.True(loginCheck.IsAuthorized);
            Assert.True(loginCheck.CanLogin);

            var readCheck = _lifecycleService.EvaluateAccess(policy, "read");
            Assert.True(readCheck.IsAuthorized);
            Assert.True(readCheck.CanRead);

            var aiCheck = _lifecycleService.EvaluateAccess(policy, "ai");
            Assert.True(aiCheck.IsAuthorized);
            Assert.True(aiCheck.CanUseAI);

            var writeCheck = _lifecycleService.EvaluateAccess(policy, "write");
            Assert.False(writeCheck.IsAuthorized);
            Assert.False(writeCheck.CanWrite);
            Assert.Contains("Read-Only", writeCheck.RejectionReason);
        }

        [Fact]
        public void ResourceQuotas_Enforce_Tier_Limits()
        {
            // Arrange: Starter tier with 25 opp limit
            var starterPolicy = _lifecycleService.CreateDefaultPolicy(Guid.NewGuid(), "Startup Inc", TenantTier.Starter);

            // Act & Assert
            Assert.True(_lifecycleService.CheckResourceQuota(starterPolicy, "opportunity", 24));
            Assert.False(_lifecycleService.CheckResourceQuota(starterPolicy, "opportunity", 25)); // Quota reached

            // Leads limit is 100
            Assert.True(_lifecycleService.CheckResourceQuota(starterPolicy, "lead", 99));
            Assert.False(_lifecycleService.CheckResourceQuota(starterPolicy, "lead", 100)); // Quota reached
        }
    }
}
