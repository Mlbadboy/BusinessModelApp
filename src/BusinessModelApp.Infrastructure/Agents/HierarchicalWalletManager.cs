using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using BusinessModelApp.Core.Agents;

namespace BusinessModelApp.Infrastructure.Agents
{
    public class HierarchicalWalletManager : IHierarchicalWalletManager
    {
        private readonly ConcurrentDictionary<Guid, HierarchicalWallet> _wallets = new();
        private readonly ConcurrentDictionary<string, WalletTransaction> _transactions = new(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new();

        public HierarchicalWallet CreateWallet(string ownerId, WalletScope scope, decimal totalBudgetINR, Guid? parentWalletId = null)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentNullException(nameof(ownerId));
            if (totalBudgetINR < 0) throw new ArgumentException("Budget cannot be negative", nameof(totalBudgetINR));

            var wallet = new HierarchicalWallet
            {
                WalletId = Guid.NewGuid(),
                OwnerId = ownerId,
                Scope = scope,
                TotalBudgetINR = totalBudgetINR,
                ConsumedINR = 0m,
                ReservedINR = 0m,
                ParentWalletId = parentWalletId
            };

            _wallets[wallet.WalletId] = wallet;
            return wallet;
        }

        public bool TryReserveIdempotent(string transactionId, Guid walletId, string agentId, decimal amountINR)
        {
            if (string.IsNullOrWhiteSpace(transactionId)) throw new ArgumentNullException(nameof(transactionId));
            if (amountINR < 0) throw new ArgumentException("Reservation amount cannot be negative", nameof(amountINR));

            lock (_lock)
            {
                // Idempotency: If transaction already exists in Reserved or higher state, return success
                if (_transactions.TryGetValue(transactionId, out var existing))
                {
                    if (existing.WalletId == walletId && existing.Phase >= WalletPhase.Reserved)
                    {
                        return true;
                    }
                    return false;
                }

                if (!_wallets.TryGetValue(walletId, out var wallet))
                {
                    return false;
                }

                // Check wallet remaining budget
                if (wallet.RemainingINR < amountINR)
                {
                    return false; // Hard overdraft prevention
                }

                // If parent wallet exists, ensure parent also has sufficient remaining budget
                if (wallet.ParentWalletId.HasValue && _wallets.TryGetValue(wallet.ParentWalletId.Value, out var parentWallet))
                {
                    if (parentWallet.RemainingINR < amountINR)
                    {
                        return false;
                    }
                    parentWallet.ReservedINR += amountINR;
                }

                wallet.ReservedINR += amountINR;

                var txn = new WalletTransaction
                {
                    TransactionId = transactionId,
                    WalletId = walletId,
                    AgentId = agentId,
                    Scope = wallet.Scope,
                    ReservedAmountINR = amountINR,
                    ActualCostINR = 0m,
                    Phase = WalletPhase.Reserved,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _transactions[transactionId] = txn;
                return true;
            }
        }

        public bool CommitIdempotent(string transactionId, decimal actualCostINR)
        {
            if (string.IsNullOrWhiteSpace(transactionId)) throw new ArgumentNullException(nameof(transactionId));
            if (actualCostINR < 0) throw new ArgumentException("Actual cost cannot be negative", nameof(actualCostINR));

            lock (_lock)
            {
                if (!_transactions.TryGetValue(transactionId, out var txn))
                {
                    return false;
                }

                // Idempotency: already committed
                if (txn.Phase == WalletPhase.Committed || txn.Phase == WalletPhase.Settled)
                {
                    return true;
                }

                if (!_wallets.TryGetValue(txn.WalletId, out var wallet))
                {
                    return false;
                }

                wallet.ReservedINR = Math.Max(0m, wallet.ReservedINR - txn.ReservedAmountINR);
                wallet.ConsumedINR += actualCostINR;

                if (wallet.ParentWalletId.HasValue && _wallets.TryGetValue(wallet.ParentWalletId.Value, out var parentWallet))
                {
                    parentWallet.ReservedINR = Math.Max(0m, parentWallet.ReservedINR - txn.ReservedAmountINR);
                    parentWallet.ConsumedINR += actualCostINR;
                }

                txn.ActualCostINR = actualCostINR;
                txn.Phase = WalletPhase.Committed;
                txn.CompletedAtUtc = DateTime.UtcNow;

                return true;
            }
        }

        public bool ReconcileIdempotent(string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId)) throw new ArgumentNullException(nameof(transactionId));

            lock (_lock)
            {
                if (!_transactions.TryGetValue(transactionId, out var txn))
                {
                    return false;
                }

                if (txn.Phase == WalletPhase.Settled)
                {
                    return true;
                }

                txn.Phase = WalletPhase.Settled;
                txn.CompletedAtUtc ??= DateTime.UtcNow;
                return true;
            }
        }

        public WalletTransaction? GetTransaction(string transactionId)
        {
            _transactions.TryGetValue(transactionId, out var txn);
            return txn;
        }

        public HierarchicalWallet? GetWallet(Guid walletId)
        {
            _wallets.TryGetValue(walletId, out var wallet);
            return wallet;
        }
    }
}
