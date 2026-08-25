using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Infrastructure.Services
{
    public enum PushNotificationPriority
    {
        Low,
        Normal,
        High
    }

    public class PushNotificationResult
    {
        public bool Success { get; set; }
        public string MessageId { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public interface IPushNotificationService
    {
        Task<PushNotificationResult> SendPushNotificationAsync(
            string userId,
            string title,
            string message,
            Dictionary<string, object> data = null,
            PushNotificationPriority priority = PushNotificationPriority.Normal,
            CancellationToken cancellationToken = default);

        Task<PushNotificationResult> SendPushNotificationAsync(
            IEnumerable<string> userIds,
            string title,
            string message,
            Dictionary<string, object> data = null,
            PushNotificationPriority priority = PushNotificationPriority.Normal,
            CancellationToken cancellationToken = default);

        Task<PushNotificationResult> SendPushToTopicAsync(
            string topic,
            string title,
            string message,
            Dictionary<string, object> data = null,
            PushNotificationPriority priority = PushNotificationPriority.Normal,
            CancellationToken cancellationToken = default);

        Task<bool> SubscribeToTopicAsync(
            string userId,
            string topic,
            CancellationToken cancellationToken = default);

        Task<bool> UnsubscribeFromTopicAsync(
            string userId,
            string topic,
            CancellationToken cancellationToken = default);
    }
}
