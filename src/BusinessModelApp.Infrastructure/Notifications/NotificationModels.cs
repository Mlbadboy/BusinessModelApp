using System;
using System.Collections.Generic;

namespace BusinessModelApp.Infrastructure.Notifications
{
    public enum NotificationType
    {
        Email = 1,
        InApp = 2,
        SMS = 3,
        Push = 4
    }

    public enum NotificationPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Urgent = 3
    }

    public class NotificationRecipient
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string UserId { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    public class NotificationAttachment
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
        public bool IsInline { get; set; }
        public string ContentId { get; set; }
    }

    public class NotificationRequest
    {
        public string TemplateId { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
        public NotificationRecipient From { get; set; }
        public List<NotificationRecipient> To { get; } = new List<NotificationRecipient>();
        public List<NotificationRecipient> Cc { get; } = new List<NotificationRecipient>();
        public List<NotificationRecipient> Bcc { get; } = new List<NotificationRecipient>();
        public List<NotificationAttachment> Attachments { get; } = new List<NotificationAttachment>();
        public NotificationType Type { get; set; } = NotificationType.Email;
        public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
        public DateTime? SendAt { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }

    public class NotificationResult
    {
        public bool Success { get; set; }
        public string MessageId { get; set; }
        public string Error { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class NotificationTemplate
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SubjectTemplate { get; set; }
        public string HtmlTemplate { get; set; }
        public string TextTemplate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Tags { get; set; }
    }
}
