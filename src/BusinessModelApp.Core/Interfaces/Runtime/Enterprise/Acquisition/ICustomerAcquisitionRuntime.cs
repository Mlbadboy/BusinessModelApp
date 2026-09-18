using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Acquisition;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Growth;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Acquisition
{
    public interface ICustomerAcquisitionRuntimeStore
    {
        Task SavePolicyAsync(OutreachCampaignPolicy policy, CancellationToken cancellationToken = default);
        Task<OutreachCampaignPolicy?> GetPolicyAsync(string tenantId, CancellationToken cancellationToken = default);

        Task SaveOutreachTaskAsync(GovernedOutreachTask task, CancellationToken cancellationToken = default);
        Task<GovernedOutreachTask?> GetOutreachTaskAsync(string taskId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<GovernedOutreachTask>> ListTasksForProspectAsync(string prospectId, CancellationToken cancellationToken = default);
    }

    public interface ICustomerAcquisitionRuntimeService
    {
        Task<GovernedOutreachTask> PrepareOutreachAsync(
            string tenantId,
            string prospectId,
            string contactEmail,
            string subjectLine,
            string messageContent,
            DateTime? lastContactedUtc,
            CancellationToken cancellationToken = default);

        Task<GovernedOutreachTask> ExecuteGovernedOutreachAsync(
            string taskId,
            string batch6PermitId,
            string externalProviderRef,
            CancellationToken cancellationToken = default);
    }
}
