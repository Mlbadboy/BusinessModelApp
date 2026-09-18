using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Lifecycle;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Lifecycle;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    /// <summary>
    /// Phase 4.11 Batch 4111: Customer Lifecycle Red Team Tests (LIFE01 - LIFE04).
    /// </summary>
    public class Phase4Batch4111LifecycleRedTeamTests
    {
        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        [Fact]
        public async Task LIFE01_ExpansionWon_WithoutSignedContractHash_FailsClosed()
        {
            var store = new InMemoryCustomerLifecycleRuntimeStore();
            var service = new CustomerLifecycleRuntimeService(store);

            var account = await service.RegisterAccountAsync("tenant-alpha", "Target Client", 500_000m);
            var exp = await service.IdentifyExpansionAsync("tenant-alpha", account.CustomerId, "AnalyticsModule", 100_000m, 0.8m);

            // Attempt to win expansion with empty or invalid hash
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CloseWonExpansionAsync(exp.ExpansionId, "invalid-short-string"));

            Assert.Contains("valid 64-character SHA-256", ex.Message);
        }

        [Fact]
        public async Task LIFE02_NegativeARR_ThrowsArgumentOutOfRangeException()
        {
            var store = new InMemoryCustomerLifecycleRuntimeStore();
            var service = new CustomerLifecycleRuntimeService(store);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                service.RegisterAccountAsync("tenant-alpha", "Invalid Negative ARR Client", -50_000m));
        }

        [Fact]
        public async Task LIFE03_ExpansionForNonExistentCustomer_FailsClosed()
        {
            var store = new InMemoryCustomerLifecycleRuntimeStore();
            var service = new CustomerLifecycleRuntimeService(store);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.IdentifyExpansionAsync("tenant-alpha", "non-existent-cust", "SecurityModule", 50_000m, 0.5m));
        }

        [Fact]
        public async Task LIFE04_FullChurnMarksAccountChurnedAndSetsARRToZero()
        {
            var store = new InMemoryCustomerLifecycleRuntimeStore();
            var service = new CustomerLifecycleRuntimeService(store);

            var account = await service.RegisterAccountAsync("tenant-alpha", "Churning Client", 300_000m);
            account.MarkChurned();

            Assert.Equal(CustomerAccountStatus.Churned, account.Status);
            Assert.Equal(0m, account.ContractedARR_INR);
        }
    }
}
