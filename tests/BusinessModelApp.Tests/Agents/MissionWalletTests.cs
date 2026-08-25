using BusinessModelApp.Core.Agents;
using Xunit;

namespace BusinessModelApp.Tests.Agents
{
    public class MissionWalletTests
    {
        [Fact]
        public void MissionWallet_Calculates_Balances_And_Enforces_Reservation_Holds()
        {
            var wallet = MissionWallet.CreateDefault(5000m);

            Assert.Equal(5000m, wallet.TotalBudgetINR);
            Assert.Equal(5000m, wallet.RemainingSpendINR);
            Assert.False(wallet.IsExhausted);

            // Reserve ₹1,500
            bool reserved = wallet.TryReserve(1500m);
            Assert.True(reserved);
            Assert.Equal(1500m, wallet.ReservedSpendINR);
            Assert.Equal(3500m, wallet.RemainingSpendINR);

            // Reconcile with actual cost ₹1,200 (₹300 refunded)
            wallet.Reconcile(1500m, 1200m);
            Assert.Equal(0m, wallet.ReservedSpendINR);
            Assert.Equal(1200m, wallet.ConsumedSpendINR);
            Assert.Equal(3800m, wallet.RemainingSpendINR);
            Assert.Equal(24.0, wallet.PercentConsumed);
        }

        [Fact]
        public void MissionWallet_Rejects_Reservation_When_Budget_Is_Exhausted()
        {
            var wallet = MissionWallet.CreateDefault(100m);

            wallet.TryReserve(100m);
            wallet.Reconcile(100m, 100m);

            Assert.True(wallet.IsExhausted);
            Assert.False(wallet.TryReserve(10m));
        }
    }
}
