using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Agents
{
    public enum WalletPhase
    {
        Reserved = 1,
        Committed = 2,
        Settled = 3,
        Cancelled = 4
    }

    public enum WalletScope
    {
        Mission = 1,
        Department = 2,
        Agent = 3
    }

    public class WalletTransaction
    {
        public string TransactionId { get; set; } = string.Empty;
        public Guid WalletId { get; set; }
        public string AgentId { get; set; } = string.Empty;
        public WalletScope Scope { get; set; }
        public decimal ReservedAmountINR { get; set; }
        public decimal ActualCostINR { get; set; }
        public WalletPhase Phase { get; set; } = WalletPhase.Reserved;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAtUtc { get; set; }
    }

    public class HierarchicalWallet
    {
        public Guid WalletId { get; set; } = Guid.NewGuid();
        public string OwnerId { get; set; } = string.Empty;
        public WalletScope Scope { get; set; } = WalletScope.Agent;
        public decimal TotalBudgetINR { get; set; }
        public decimal ConsumedINR { get; set; } = 0m;
        public decimal ReservedINR { get; set; } = 0m;
        public Guid? ParentWalletId { get; set; }

        public decimal RemainingINR => Math.Max(0m, TotalBudgetINR - (ConsumedINR + ReservedINR));
        public bool IsOverdrawn => (ConsumedINR + ReservedINR) > TotalBudgetINR;
    }

    public interface IHierarchicalWalletManager
    {
        HierarchicalWallet CreateWallet(string ownerId, WalletScope scope, decimal totalBudgetINR, Guid? parentWalletId = null);
        bool TryReserveIdempotent(string transactionId, Guid walletId, string agentId, decimal amountINR);
        bool CommitIdempotent(string transactionId, decimal actualCostINR);
        bool ReconcileIdempotent(string transactionId);
        WalletTransaction? GetTransaction(string transactionId);
        HierarchicalWallet? GetWallet(Guid walletId);
    }
}
