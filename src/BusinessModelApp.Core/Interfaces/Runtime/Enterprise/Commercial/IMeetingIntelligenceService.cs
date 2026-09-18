using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial
{
    public interface IMeetingIntelligenceStore
    {
        Task SaveMeetingBriefAsync(MeetingBrief brief);
        Task<MeetingBrief?> GetMeetingBriefAsync(string tenantId, string briefId);
        Task SaveTranscriptAnalysisAsync(MeetingTranscriptAnalysis analysis);
        Task<MeetingTranscriptAnalysis?> GetTranscriptAnalysisAsync(string tenantId, string meetingId);
    }

    public interface IMeetingIntelligenceService
    {
        Task<MeetingBrief> PrepareMeetingBriefAsync(string tenantId, string opportunityId, string companyName, List<string> attendees);
        Task<MeetingTranscriptAnalysis> IngestAndAnalyzeTranscriptAsync(string tenantId, string meetingId, string opportunityId, string rawTranscript);
    }
}
