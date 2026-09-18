using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Commercial
{
    public class InMemoryProposalAndDealStore : IProposalAndDealStore
    {
        private readonly ConcurrentDictionary<string, CommercialProposal> _proposals = new();
        private readonly ConcurrentDictionary<string, AuthoritativeDealContract> _contracts = new();

        public Task SaveProposalAsync(CommercialProposal proposal)
        {
            _proposals[$"{proposal.TenantId}:{proposal.ProposalId}"] = proposal;
            return Task.CompletedTask;
        }

        public Task<CommercialProposal?> GetProposalAsync(string tenantId, string proposalId)
        {
            _proposals.TryGetValue($"{tenantId}:{proposalId}", out var proposal);
            return Task.FromResult(proposal);
        }

        public Task<IReadOnlyList<CommercialProposal>> ListProposalsAsync(string tenantId)
        {
            var list = _proposals.Values.Where(p => p.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<CommercialProposal>>(list);
        }

        public Task SaveContractAsync(AuthoritativeDealContract contract)
        {
            _contracts[$"{contract.TenantId}:{contract.ContractId}"] = contract;
            return Task.CompletedTask;
        }

        public Task<AuthoritativeDealContract?> GetContractAsync(string tenantId, string contractId)
        {
            _contracts.TryGetValue($"{tenantId}:{contractId}", out var contract);
            return Task.FromResult(contract);
        }

        public Task<IReadOnlyList<AuthoritativeDealContract>> ListContractsAsync(string tenantId)
        {
            var list = _contracts.Values.Where(c => c.TenantId == tenantId).ToList();
            return Task.FromResult<IReadOnlyList<AuthoritativeDealContract>>(list);
        }
    }

    public class ProposalAndDealService : IProposalAndDealService
    {
        private readonly IProposalAndDealStore _store;

        public ProposalAndDealService(IProposalAndDealStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<CommercialProposal> CreateProposalAsync(CommercialProposal proposal)
        {
            if (string.IsNullOrWhiteSpace(proposal.TenantId))
                throw new ArgumentException("TenantId is required", nameof(proposal));
            if (string.IsNullOrWhiteSpace(proposal.OpportunityId))
                throw new ArgumentException("OpportunityId is required", nameof(proposal));
            if (string.IsNullOrWhiteSpace(proposal.Title))
                throw new ArgumentException("Title is required", nameof(proposal));
            if (proposal.BasePriceINR <= 0m)
                throw new ArgumentException("BasePriceINR must be greater than zero", nameof(proposal));

            await _store.SaveProposalAsync(proposal);
            return proposal;
        }

        public async Task<bool> ApproveProposalForSubmissionAsync(string tenantId, string proposalId, string humanSignoffId)
        {
            if (string.IsNullOrWhiteSpace(humanSignoffId))
                throw new InvalidOperationException("Constitutional Violation: Human signoff ID (PRG-1) is strictly required to approve proposals for external submission.");

            var proposal = await _store.GetProposalAsync(tenantId, proposalId);
            if (proposal == null)
                return false;

            if (!proposal.IsMarginCompliant)
                throw new InvalidOperationException($"Constitutional Violation: Cannot approve proposal below 35% margin threshold. Current expected margin: {proposal.ExpectedGrossMarginPercent:F1}%.");

            proposal.IsApprovedForSubmission = true;
            await _store.SaveProposalAsync(proposal);
            return true;
        }

        public async Task<NegotiationAnalysis> AnalyzeNegotiationCounterOfferAsync(string tenantId, string proposalId, decimal customerOfferedPriceINR)
        {
            var proposal = await _store.GetProposalAsync(tenantId, proposalId);
            if (proposal == null)
                throw new KeyNotFoundException($"Proposal '{proposalId}' not found for tenant '{tenantId}'");

            // Maintain minimum 35% margin floor: FloorPrice = Cost / (1 - 0.35)
            decimal floorPrice = proposal.EstimatedDeliveryCostINR > 0m
                ? Math.Round(proposal.EstimatedDeliveryCostINR / 0.65m, 2)
                : Math.Round(proposal.NetPriceINR * 0.75m, 2);

            decimal counterOffer;
            string strategy;

            if (customerOfferedPriceINR >= proposal.NetPriceINR)
            {
                counterOffer = customerOfferedPriceINR;
                strategy = "Accept favorable offer; prepare contract immediately.";
            }
            else if (customerOfferedPriceINR >= floorPrice)
            {
                // Counter at midpoint between customer offer and proposal net price
                counterOffer = Math.Round((customerOfferedPriceINR + proposal.NetPriceINR) / 2m, 2);
                strategy = "Price within acceptable envelope. Propose compromise counter-offer while seeking minor scope adjustments or multi-month commitment.";
            }
            else
            {
                // Customer offer violates floor
                counterOffer = floorPrice;
                strategy = "Customer offer violates 35% gross margin floor. Reject price reduction below floor; propose scope reduction, phased rollout, or alternative tier.";
            }

            var analysis = new NegotiationAnalysis
            {
                TenantId = tenantId,
                ProposalId = proposalId,
                CurrentProposalPriceINR = proposal.NetPriceINR,
                CustomerOfferedPriceINR = customerOfferedPriceINR,
                RecommendedCounterOfferINR = counterOffer,
                FloorPriceINR = floorPrice,
                ConcessionStrategy = strategy
            };

            return analysis;
        }

        public async Task<AuthoritativeDealContract> RecordAuthoritativeContractAsync(AuthoritativeDealContract contract)
        {
            if (string.IsNullOrWhiteSpace(contract.TenantId))
                throw new ArgumentException("TenantId is required", nameof(contract));
            if (string.IsNullOrWhiteSpace(contract.OpportunityId))
                throw new ArgumentException("OpportunityId is required", nameof(contract));
            if (string.IsNullOrWhiteSpace(contract.SignatureDigestSha256))
                throw new ArgumentException("Cryptographic SignatureDigestSha256 is required for authoritative contracts", nameof(contract));
            if (string.IsNullOrWhiteSpace(contract.VerificationSourceSystem))
                throw new ArgumentException("VerificationSourceSystem is required", nameof(contract));
            if (contract.BindingDealValueINR <= 0m)
                throw new ArgumentException("BindingDealValueINR must be positive", nameof(contract));

            await _store.SaveContractAsync(contract);
            return contract;
        }

        public async Task<bool> VerifyContractIntegrityAsync(string tenantId, string contractId)
        {
            var contract = await _store.GetContractAsync(tenantId, contractId);
            if (contract == null) return false;
            return contract.IsCryptographicallyVerified;
        }
    }
}
