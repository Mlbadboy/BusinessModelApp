using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Commercial;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Commercial
{
    public interface IInboundResponseStore
    {
        Task SaveInboundMessageAsync(InboundMessageEvent message);
        Task<InboundMessageEvent?> GetInboundMessageAsync(string tenantId, string messageId);
        Task<IReadOnlyList<InboundMessageEvent>> ListInboundMessagesAsync(string tenantId);
    }

    public interface IInboundResponseService
    {
        Task<InboundMessageEvent> ProcessInboundMessageAsync(string tenantId, string opportunityId, string sender, string subject, string rawBody);
    }
}
