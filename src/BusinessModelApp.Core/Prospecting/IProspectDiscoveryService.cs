using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Prospecting
{
    public interface ICompanyIntelligenceProvider
    {
        Task<IReadOnlyList<DiscoveredCandidateAccount>> DiscoverCandidateAccountsAsync(
            string industry, 
            string geography, 
            int minHeadcount, 
            CancellationToken ct = default);

        Task<IReadOnlyList<ProspectSignal>> ScanMarketSignalsAsync(
            string industry, 
            CancellationToken ct = default);
    }

    public interface IICPScoringEngine
    {
        decimal ScoreAccountICP(
            DiscoveredCandidateAccount account, 
            string targetIndustry, 
            decimal targetDealValueINR,
            out Dictionary<string, decimal> scoreBreakdown);
    }

    public interface IDecisionMakerDiscoveryProvider
    {
        Task<IReadOnlyList<CandidateDecisionMaker>> DiscoverDecisionMakersAsync(
            DiscoveredCandidateAccount account, 
            CancellationToken ct = default);
    }

    public interface IProspectDiscoveryService
    {
        Task<IReadOnlyList<ProspectSignal>> ScanMarketSignalsAsync(
            string industry, 
            CancellationToken ct = default);

        Task<IReadOnlyList<DiscoveredCandidateAccount>> DiscoverCandidateAccountsAsync(
            string industry, 
            string geography, 
            int minHeadcount, 
            CancellationToken ct = default);

        Task<IReadOnlyList<CandidateDecisionMaker>> DiscoverDecisionMakersAsync(
            DiscoveredCandidateAccount account, 
            CancellationToken ct = default);

        Task<decimal> ScoreAccountICPAsync(
            DiscoveredCandidateAccount account, 
            string targetIndustry, 
            decimal targetDealValueINR, 
            CancellationToken ct = default);

        Task<VerifiedProspectLead?> QualifyAndVerifyProspectAsync(
            DiscoveredCandidateAccount account, 
            CandidateDecisionMaker decisionMaker, 
            string targetIndustry, 
            decimal targetDealValueINR, 
            CancellationToken ct = default);
    }
}
