using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce
{
    public interface IUnifiedCommunicationFabric
    {
        Task<CommunicationIntent> SubmitCommunicationIntentAsync(string tenantId, CommunicationIntent intent);
        Task<bool> ApproveCommunicationIntentAsync(string tenantId, string intentId, string approvedBy);
        Task<bool> DispatchCommunicationAsync(string tenantId, string intentId);
        Task<IReadOnlyList<CommunicationIntent>> ListPendingApprovalsAsync(string tenantId);
    }

    public interface IWorkforceSecretBroker
    {
        Task<ScopedCredential> IssueScopedCredentialAsync(string tenantId, string capabilityId, int durationMinutes = 60);
        Task<bool> ValidateCredentialAsync(string tenantId, string credentialId, string capabilityId);
        Task<bool> RevokeCredentialAsync(string tenantId, string credentialId);
    }
}
