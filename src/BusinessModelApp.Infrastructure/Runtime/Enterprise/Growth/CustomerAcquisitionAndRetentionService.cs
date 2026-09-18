using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Growth;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Growth
{
    public sealed class InMemoryCustomerAcquisitionAndRetentionStore : ICustomerAcquisitionAndRetentionStore
    {
        private readonly ConcurrentDictionary<string, AccountProspect> _prospects = new();
        private readonly ConcurrentDictionary<string, List<GovernedOutboundEngagement>> _engagementsByProspect = new();
        private readonly ConcurrentDictionary<string, GovernedOutboundEngagement> _engagementsById = new();
        private readonly ConcurrentDictionary<string, CommercialMeetingRecord> _meetings = new();
        private readonly ConcurrentDictionary<string, CommercialProposalRecord> _proposals = new();
        private readonly ConcurrentDictionary<string, CommercialNegotiationRecord> _negotiations = new();
        private readonly ConcurrentDictionary<string, CommercialContractRecord> _contracts = new();
        private readonly ConcurrentDictionary<string, CustomerRetentionAndHealthRecord> _retentionRecords = new();

        public Task SaveProspectAsync(AccountProspect prospect, CancellationToken cancellationToken = default)
        {
            _prospects[prospect.ProspectId] = prospect;
            return Task.CompletedTask;
        }

        public Task<AccountProspect?> GetProspectAsync(string prospectId, CancellationToken cancellationToken = default)
        {
            _prospects.TryGetValue(prospectId, out var prospect);
            return Task.FromResult(prospect);
        }

        public Task<IReadOnlyList<AccountProspect>> ListProspectsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AccountProspect>>(_prospects.Values.ToList());
        }

        public Task SaveOutboundEngagementAsync(GovernedOutboundEngagement engagement, CancellationToken cancellationToken = default)
        {
            _engagementsById[engagement.EngagementId] = engagement;
            _engagementsByProspect.AddOrUpdate(
                engagement.ProspectId,
                new List<GovernedOutboundEngagement> { engagement },
                (_, list) => { lock (list) { list.Add(engagement); } return list; });
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<GovernedOutboundEngagement>> GetEngagementsForProspectAsync(string prospectId, CancellationToken cancellationToken = default)
        {
            if (_engagementsByProspect.TryGetValue(prospectId, out var list))
            {
                lock (list)
                {
                    return Task.FromResult<IReadOnlyList<GovernedOutboundEngagement>>(list.ToList());
                }
            }
            return Task.FromResult<IReadOnlyList<GovernedOutboundEngagement>>(Array.Empty<GovernedOutboundEngagement>());
        }

        public Task SaveMeetingAsync(CommercialMeetingRecord meeting, CancellationToken cancellationToken = default)
        {
            _meetings[meeting.MeetingId] = meeting;
            return Task.CompletedTask;
        }

        public Task<CommercialMeetingRecord?> GetMeetingAsync(string meetingId, CancellationToken cancellationToken = default)
        {
            _meetings.TryGetValue(meetingId, out var meeting);
            return Task.FromResult(meeting);
        }

        public Task SaveProposalAsync(CommercialProposalRecord proposal, CancellationToken cancellationToken = default)
        {
            _proposals[proposal.ProposalId] = proposal;
            return Task.CompletedTask;
        }

        public Task<CommercialProposalRecord?> GetProposalAsync(string proposalId, CancellationToken cancellationToken = default)
        {
            _proposals.TryGetValue(proposalId, out var proposal);
            return Task.FromResult(proposal);
        }

        public Task SaveNegotiationAsync(CommercialNegotiationRecord negotiation, CancellationToken cancellationToken = default)
        {
            _negotiations[negotiation.NegotiationId] = negotiation;
            return Task.CompletedTask;
        }

        public Task<CommercialNegotiationRecord?> GetNegotiationAsync(string negotiationId, CancellationToken cancellationToken = default)
        {
            _negotiations.TryGetValue(negotiationId, out var negotiation);
            return Task.FromResult(negotiation);
        }

        public Task SaveContractAsync(CommercialContractRecord contract, CancellationToken cancellationToken = default)
        {
            _contracts[contract.ContractId] = contract;
            return Task.CompletedTask;
        }

        public Task<CommercialContractRecord?> GetContractAsync(string contractId, CancellationToken cancellationToken = default)
        {
            _contracts.TryGetValue(contractId, out var contract);
            return Task.FromResult(contract);
        }

        public Task SaveRetentionRecordAsync(CustomerRetentionAndHealthRecord retention, CancellationToken cancellationToken = default)
        {
            _retentionRecords[retention.CustomerId] = retention;
            return Task.CompletedTask;
        }

        public Task<CustomerRetentionAndHealthRecord?> GetRetentionRecordAsync(string customerId, CancellationToken cancellationToken = default)
        {
            _retentionRecords.TryGetValue(customerId, out var record);
            return Task.FromResult(record);
        }

        public Task<IReadOnlyList<CustomerRetentionAndHealthRecord>> ListRetentionRecordsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<CustomerRetentionAndHealthRecord>>(_retentionRecords.Values.ToList());
        }

        public GovernedOutboundEngagement? GetEngagementById(string engagementId)
        {
            _engagementsById.TryGetValue(engagementId, out var engagement);
            return engagement;
        }
    }

    public sealed class CustomerAcquisitionAndRetentionService : ICustomerAcquisitionAndRetentionService
    {
        private readonly ICustomerAcquisitionAndRetentionStore _store;

        public CustomerAcquisitionAndRetentionService(ICustomerAcquisitionAndRetentionStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<AccountProspect> DiscoverAndScoreProspectAsync(
            string companyName,
            string industry,
            int estimatedEmployeeCount,
            decimal estimatedAnnualRevenue,
            decimal icpScore,
            string icpQualificationSummary,
            CancellationToken cancellationToken = default)
        {
            var prospect = new AccountProspect(
                companyName,
                industry,
                estimatedEmployeeCount,
                estimatedAnnualRevenue,
                icpScore,
                icpQualificationSummary);

            await _store.SaveProspectAsync(prospect, cancellationToken);
            return prospect;
        }

        public async Task<GovernedOutboundEngagement> SendGovernedOutreachAsync(
            string prospectId,
            string recipientEmail,
            string channel,
            string subjectLine,
            string messageBody,
            bool antiSpamComplianceVerified,
            string governedSignoffDigest,
            CancellationToken cancellationToken = default)
        {
            var prospect = await _store.GetProspectAsync(prospectId, cancellationToken);
            if (prospect == null)
                throw new KeyNotFoundException($"Prospect '{prospectId}' not found.");

            // Constitutional Law I41-B: Market Signal != Demand != Lead != Customer != Revenue != Business Growth
            // Outbound message must be strictly governed and anti-spam checked
            var engagement = new GovernedOutboundEngagement(
                prospectId,
                recipientEmail,
                channel,
                subjectLine,
                messageBody,
                antiSpamComplianceVerified,
                governedSignoffDigest);

            await _store.SaveOutboundEngagementAsync(engagement, cancellationToken);
            return engagement;
        }

        public async Task RecordOutreachResponseAsync(
            string engagementId,
            OutreachReplyClassification classification,
            decimal sentimentScore,
            CancellationToken cancellationToken = default)
        {
            if (_store is InMemoryCustomerAcquisitionAndRetentionStore memStore)
            {
                var engagement = memStore.GetEngagementById(engagementId);
                if (engagement != null)
                {
                    engagement.RecordReply(classification, sentimentScore);
                }
            }
            await Task.CompletedTask;
        }

        public async Task<CommercialMeetingRecord> ScheduleMeetingAsync(
            string prospectId,
            string title,
            DateTime scheduledAtUtc,
            string agenda,
            CancellationToken cancellationToken = default)
        {
            var prospect = await _store.GetProspectAsync(prospectId, cancellationToken);
            if (prospect == null)
                throw new KeyNotFoundException($"Prospect '{prospectId}' not found.");

            var meeting = new CommercialMeetingRecord(prospectId, title, scheduledAtUtc, agenda);
            await _store.SaveMeetingAsync(meeting, cancellationToken);
            return meeting;
        }

        public async Task ConcludeMeetingAsync(
            string meetingId,
            MeetingOutcomeState outcome,
            string discoveryNotes,
            bool meddpicQualified,
            CancellationToken cancellationToken = default)
        {
            var meeting = await _store.GetMeetingAsync(meetingId, cancellationToken);
            if (meeting == null)
                throw new KeyNotFoundException($"Meeting '{meetingId}' not found.");

            meeting.ConcludeMeeting(outcome, discoveryNotes, meddpicQualified);
            await _store.SaveMeetingAsync(meeting, cancellationToken);
        }

        public async Task<CommercialProposalRecord> GenerateProposalAsync(
            string prospectId,
            string solutionTitle,
            string scopeSummary,
            decimal proposedPrice,
            decimal estimatedCostBasis,
            decimal projectedCustomerRoiMultiple,
            int estimatedPaybackMonths,
            CancellationToken cancellationToken = default)
        {
            var prospect = await _store.GetProspectAsync(prospectId, cancellationToken);
            if (prospect == null)
                throw new KeyNotFoundException($"Prospect '{prospectId}' not found.");

            // Constitutional Law I41-G: Margin Floor enforcement happens inside record constructor
            var proposal = new CommercialProposalRecord(
                prospectId,
                solutionTitle,
                scopeSummary,
                proposedPrice,
                estimatedCostBasis,
                projectedCustomerRoiMultiple,
                estimatedPaybackMonths);

            await _store.SaveProposalAsync(proposal, cancellationToken);
            return proposal;
        }

        public async Task<CommercialProposalRecord> ApproveProposalAsync(
            string proposalId,
            string approvalAuthority,
            CancellationToken cancellationToken = default)
        {
            var proposal = await _store.GetProposalAsync(proposalId, cancellationToken);
            if (proposal == null)
                throw new KeyNotFoundException($"Proposal '{proposalId}' not found.");

            proposal.ApproveProposal(approvalAuthority);
            await _store.SaveProposalAsync(proposal, cancellationToken);
            return proposal;
        }

        public async Task<CommercialNegotiationRecord> FinalizeNegotiationAsync(
            string proposalId,
            decimal initialProposedPrice,
            decimal finalAgreedPrice,
            decimal estimatedCostBasis,
            string concessionsGrantedSummary,
            string concessionsReceivedSummary,
            string prg1SignoffId,
            bool isClosedWon,
            CancellationToken cancellationToken = default)
        {
            var proposal = await _store.GetProposalAsync(proposalId, cancellationToken);
            if (proposal == null)
                throw new KeyNotFoundException($"Proposal '{proposalId}' not found.");

            // Enforces Law I41-H (PRG-1 signoff) and Law I41-G (margin floor on closed-won)
            var negotiation = new CommercialNegotiationRecord(
                proposalId,
                initialProposedPrice,
                finalAgreedPrice,
                estimatedCostBasis,
                concessionsGrantedSummary,
                concessionsReceivedSummary,
                prg1SignoffId,
                isClosedWon);

            await _store.SaveNegotiationAsync(negotiation, cancellationToken);
            return negotiation;
        }

        public async Task<CommercialContractRecord> ExecuteContractAsync(
            string prospectId,
            string proposalId,
            decimal totalContractValue,
            string paymentTerms,
            string contractDigestSha256,
            string counterpartySignatory,
            string charlieSignatory,
            CancellationToken cancellationToken = default)
        {
            var contract = new CommercialContractRecord(
                prospectId,
                proposalId,
                totalContractValue,
                paymentTerms,
                contractDigestSha256,
                counterpartySignatory,
                charlieSignatory,
                isFullyExecuted: true);

            await _store.SaveContractAsync(contract, cancellationToken);
            return contract;
        }

        public async Task<CustomerRetentionAndHealthRecord> InitiateOnboardingAsync(
            string customerId,
            string contractId,
            string companyName,
            CancellationToken cancellationToken = default)
        {
            var record = new CustomerRetentionAndHealthRecord(customerId, contractId, companyName);
            await _store.SaveRetentionRecordAsync(record, cancellationToken);
            return record;
        }

        public async Task RecordValueRealizationAsync(
            string customerId,
            DateTime realizedAtUtc,
            string milestoneCompleted,
            CancellationToken cancellationToken = default)
        {
            var record = await _store.GetRetentionRecordAsync(customerId, cancellationToken);
            if (record == null)
                throw new KeyNotFoundException($"Customer retention record '{customerId}' not found.");

            record.RecordFirstValueRealized(realizedAtUtc);
            record.AddMilestone(milestoneCompleted);
            await _store.SaveRetentionRecordAsync(record, cancellationToken);
        }

        public async Task UpdateCustomerHealthAsync(
            string customerId,
            decimal healthScore,
            ChurnRiskLevel riskLevel,
            decimal? npsScore = null,
            CancellationToken cancellationToken = default)
        {
            var record = await _store.GetRetentionRecordAsync(customerId, cancellationToken);
            if (record == null)
                throw new KeyNotFoundException($"Customer retention record '{customerId}' not found.");

            record.UpdateHealth(healthScore, riskLevel, npsScore);
            await _store.SaveRetentionRecordAsync(record, cancellationToken);
        }
    }
}
