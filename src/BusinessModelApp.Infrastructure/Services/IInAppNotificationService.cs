using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Infrastructure.Services
{
    public class InAppNotificationResult
    {
        public bool Success { get; set; }
        public string NotificationId { get; set; }
        public string ErrorMessage { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class InAppNotification
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public bool IsRead { get; set; }
        public bool IsImportant { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    public interface IInAppNotificationService
    {
        Task<InAppNotificationResult> SendInAppNotificationAsync(
            string userId,
            string title,
            string message,
            Dictionary<string, object> data = null,
            bool isImportant = false,
            CancellationToken cancellationToken = default);

        Task<InAppNotificationResult> SendInAppNotificationAsync(
            IEnumerable<string> userIds,
            string title,
            string message,
            Dictionary<string, object> data = null,
            bool isImportant = false,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<InAppNotification>> GetUserNotificationsAsync(
            string userId,
            bool includeRead = false,
            int limit = 50,
            CancellationToken cancellationToken = default);

        Task<bool> MarkAsReadAsync(
            string notificationId,
            string userId,
            CancellationToken cancellationToken = default);

        Task<bool> MarkAllAsReadAsync(
            string userId,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteNotificationAsync(
            string notificationId,
            string userId,
            CancellationToken cancellationToken = default);

        Task<int> GetUnreadCountAsync(
            string userId,
            CancellationToken cancellationToken = default);
    }
}
