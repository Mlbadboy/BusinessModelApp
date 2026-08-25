using BusinessModelApp.Infrastructure.Notifications;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Infrastructure.Services
{
    public interface INotificationService
    {
        Task<NotificationResult> SendNotificationAsync(NotificationRequest request, CancellationToken cancellationToken = default);
        Task<IEnumerable<NotificationTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default);
        Task<NotificationTemplate> GetTemplateByIdAsync(string templateId, CancellationToken cancellationToken = default);
        Task<NotificationTemplate> SaveTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default);
        Task<bool> DeleteTemplateAsync(string templateId, CancellationToken cancellationToken = default);
    }
}
