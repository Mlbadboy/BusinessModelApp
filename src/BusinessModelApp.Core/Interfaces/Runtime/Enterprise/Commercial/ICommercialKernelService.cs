using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial
{
    public interface ICommercialKernelStore
    {
        Task SaveEntityAsync(CommercialStageRecord entity);
        Task<CommercialStageRecord?> GetEntityAsync(string tenantId, string entityId);
        Task<IReadOnlyList<CommercialStageRecord>> ListEntitiesAsync(string tenantId);
    }

    public interface ICommercialKernelService
    {
        Task<CommercialStageRecord> InitializeCommercialEntityAsync(string tenantId, string opportunityId, string accountId);
        Task<CommercialStageRecord?> GetCommercialEntityAsync(string tenantId, string commercialEntityId);
        Task<IReadOnlyList<CommercialStageRecord>> ListCommercialEntitiesAsync(string tenantId);
        Task<CommercialTransitionResult> AttemptTransitionAsync(CommercialTransitionRequest request);
        Task<bool> VerifyAuditTrailIntegrityAsync(string tenantId, string commercialEntityId);
    }
}
