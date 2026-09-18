using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Growth
{
    public interface ICustomerAcquisitionAndRetentionStore
    {
        Task SaveProspectAsync(AccountProspect prospect, CancellationToken cancellationToken = default);
        Task<AccountProspect?> GetProspectAsync(string prospectId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<AccountProspect>> ListProspectsAsync(CancellationToken cancellationToken = default);

        Task SaveOutboundEngagementAsync(GovernedOutboundEngagement engagement, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GovernedOutboundEngagement>> GetEngagementsForProspectAsync(string prospectId, CancellationToken cancellationToken = default);

        Task SaveMeetingAsync(CommercialMeetingRecord meeting, CancellationToken cancellationToken = default);
        Task<CommercialMeetingRecord?> GetMeetingAsync(string meetingId, CancellationToken cancellationToken = default);

        Task SaveProposalAsync(CommercialProposalRecord proposal, CancellationToken cancellationToken = default);
        Task<CommercialProposalRecord?> GetProposalAsync(string proposalId, CancellationToken cancellationToken = default);

        Task SaveNegotiationAsync(CommercialNegotiationRecord negotiation, CancellationToken cancellationToken = default);
        Task<CommercialNegotiationRecord?> GetNegotiationAsync(string negotiationId, CancellationToken cancellationToken = default);

        Task SaveContractAsync(CommercialContractRecord contract, CancellationToken cancellationToken = default);
        Task<CommercialContractRecord?> GetContractAsync(string contractId, CancellationToken cancellationToken = default);

        Task SaveRetentionRecordAsync(CustomerRetentionAndHealthRecord retention, CancellationToken cancellationToken = default);
        Task<CustomerRetentionAndHealthRecord?> GetRetentionRecordAsync(string customerId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CustomerRetentionAndHealthRecord>> ListRetentionRecordsAsync(CancellationToken cancellationToken = default);
    }

    public interface ICustomerAcquisitionAndRetentionService
    {
        Task<AccountProspect> DiscoverAndScoreProspectAsync(
            string companyName,
            string industry,
            int estimatedEmployeeCount,
            decimal estimatedAnnualRevenue,
            decimal icpScore,
            string icpQualificationSummary,
            CancellationToken cancellationToken = default);

        Task<GovernedOutboundEngagement> SendGovernedOutreachAsync(
            string prospectId,
            string recipientEmail,
            string channel,
            string subjectLine,
            string messageBody,
            bool antiSpamComplianceVerified,
            string governedSignoffDigest,
            CancellationToken cancellationToken = default);

        Task RecordOutreachResponseAsync(
            string engagementId,
            OutreachReplyClassification classification,
            decimal sentimentScore,
            CancellationToken cancellationToken = default);

        Task<CommercialMeetingRecord> ScheduleMeetingAsync(
            string prospectId,
            string title,
            DateTime scheduledAtUtc,
            string agenda,
            CancellationToken cancellationToken = default);

        Task ConcludeMeetingAsync(
            string meetingId,
            MeetingOutcomeState outcome,
            string discoveryNotes,
            bool meddpicQualified,
            CancellationToken cancellationToken = default);

        Task<CommercialProposalRecord> GenerateProposalAsync(
            string prospectId,
            string solutionTitle,
            string scopeSummary,
            decimal proposedPrice,
            decimal estimatedCostBasis,
            decimal projectedCustomerRoiMultiple,
            int estimatedPaybackMonths,
            CancellationToken cancellationToken = default);

        Task<CommercialProposalRecord> ApproveProposalAsync(
            string proposalId,
            string approvalAuthority,
            CancellationToken cancellationToken = default);

        Task<CommercialNegotiationRecord> FinalizeNegotiationAsync(
            string proposalId,
            decimal initialProposedPrice,
            decimal finalAgreedPrice,
            decimal estimatedCostBasis,
            string concessionsGrantedSummary,
            string concessionsReceivedSummary,
            string prg1SignoffId,
            bool isClosedWon,
            CancellationToken cancellationToken = default);

        Task<CommercialContractRecord> ExecuteContractAsync(
            string prospectId,
            string proposalId,
            decimal totalContractValue,
            string paymentTerms,
            string contractDigestSha256,
            string counterpartySignatory,
            string charlieSignatory,
            CancellationToken cancellationToken = default);

        Task<CustomerRetentionAndHealthRecord> InitiateOnboardingAsync(
            string customerId,
            string contractId,
            string companyName,
            CancellationToken cancellationToken = default);

        Task RecordValueRealizationAsync(
            string customerId,
            DateTime realizedAtUtc,
            string milestoneCompleted,
            CancellationToken cancellationToken = default);

        Task UpdateCustomerHealthAsync(
            string customerId,
            decimal healthScore,
            ChurnRiskLevel riskLevel,
            decimal? npsScore = null,
            CancellationToken cancellationToken = default);
    }
}
