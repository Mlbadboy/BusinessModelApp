using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial
{
    public interface IOutreachEngineStore
    {
        Task SaveIntentAsync(CommercialCommunicationIntent intent);
        Task<CommercialCommunicationIntent?> GetIntentAsync(string tenantId, string intentId);
        Task<IReadOnlyList<CommercialCommunicationIntent>> ListIntentsAsync(string tenantId);
    }

    public interface IOutreachEngineService
    {
        Task<CommercialCommunicationIntent> SubmitOutboundIntentAsync(CommercialCommunicationIntent intent);
        Task<bool> ApproveOutboundIntentAsync(string tenantId, string intentId, string humanSignoffId);
        Task<bool> DispatchOutboundCommunicationAsync(string tenantId, string intentId, string? batch6PermitId = null);
        Task<IReadOnlyList<CommercialCommunicationIntent>> GetPendingApprovalsAsync(string tenantId);
    }
}
