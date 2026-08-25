using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Users;
using BusinessModelApp.Infrastructure.Data;
using BusinessModelApp.Infrastructure.Notifications;
using BusinessModelApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BusinessModelApp.Infrastructure.Services
{
    public class NotificationService : INotificationService, IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;
        private readonly IPushNotificationService _pushNotificationService;
        private readonly IInAppNotificationService _inAppNotificationService;
        private readonly NotificationOptions _options;
        private bool _disposed;

        public NotificationService(
            AppDbContext context,
            ILogger<NotificationService> logger,
            IEmailSender emailSender,
            ISmsSender smsSender,
            IPushNotificationService pushNotificationService,
            IInAppNotificationService inAppNotificationService,
            IOptions<NotificationOptions> options)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailSender = emailSender;
            _smsSender = smsSender;
            _pushNotificationService = pushNotificationService;
            _inAppNotificationService = inAppNotificationService;
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<NotificationResult> SendNotificationAsync(NotificationRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Validate request
            if (!request.To.Any())
                return new NotificationResult { Success = false, Error = "At least one recipient is required" };

            try
            {
                // Process based on notification type
                switch (request.Type)
                {
                    case NotificationType.Email:
                        return await SendEmailNotificationAsync(request, cancellationToken);
                    case NotificationType.SMS:
                        return await SendSmsNotificationAsync(request, cancellationToken);
                    case NotificationType.Push:
                        return await SendPushNotificationAsync(request, cancellationToken);
                    case NotificationType.InApp:
                        return await SendInAppNotificationAsync(request, cancellationToken);
                    default:
                        return new NotificationResult { Success = false, Error = $"Unsupported notification type: {request.Type}" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending {NotificationType} notification", request.Type);
                return new NotificationResult { Success = false, Error = ex.Message };
            }
        }

        private async Task<NotificationResult> SendEmailNotificationAsync(NotificationRequest request, CancellationToken cancellationToken)
        {
            if (_emailSender == null)
                return new NotificationResult { Success = false, Error = "Email sender is not configured" };

            try
            {
                // Apply template if template ID is provided
                if (!string.IsNullOrEmpty(request.TemplateId))
                {
                    var template = await GetTemplateByIdAsync(request.TemplateId, cancellationToken);
                    if (template != null)
                    {
                        request = ApplyTemplate(request, template);
                    }
                }

                // Convert recipients
                var to = request.To.Select(r => (r.Email, r.Name)).ToList();
                var cc = request.Cc?.Select(r => (r.Email, r.Name)).ToList();
                var bcc = request.Bcc?.Select(r => (r.Email, r.Name)).ToList();

                // Send email
                var result = await _emailSender.SendEmailAsync(
                    to: to,
                    subject: request.Subject,
                    htmlContent: request.Content,
                    textContent: null, // Will be generated from HTML if needed
                    cc: cc,
                    bcc: bcc,
                    replyTo: request.From?.Email,
                    attachments: request.Attachments?.Select(a => new EmailAttachment
                    {
                        FileName = a.FileName,
                        ContentType = a.ContentType,
                        Content = a.Content,
                        IsInline = a.IsInline,
                        ContentId = a.ContentId
                    }),
                    cancellationToken: cancellationToken);

                return new NotificationResult
                {
                    Success = result.Success,
                    MessageId = result.MessageId,
                    Error = result.ErrorMessage,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Provider"] = result.Provider,
                        ["Response"] = result.Response
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email notification");
                return new NotificationResult { Success = false, Error = ex.Message };
            }
        }

        private async Task<NotificationResult> SendSmsNotificationAsync(NotificationRequest request, CancellationToken cancellationToken)
        {
            if (_smsSender == null)
                return new NotificationResult { Success = false, Error = "SMS sender is not configured" };

            try
            {
                // For SMS, we only need the first recipient's phone number
                var recipient = request.To.FirstOrDefault();
                if (recipient == null || string.IsNullOrWhiteSpace(recipient.Email)) // Assuming Email contains phone number
                    return new NotificationResult { Success = false, Error = "Recipient phone number is required" };

                // Send SMS
                var result = await _smsSender.SendSmsAsync(
                    to: recipient.Email, // Phone number
                    message: request.Content,
                    from: _options.SmsDefaultSender,
                    cancellationToken: cancellationToken);

                return new NotificationResult
                {
                    Success = result.Success,
                    MessageId = result.MessageId,
                    Error = result.ErrorMessage,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Provider"] = result.Provider,
                        ["Response"] = result.Response
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS notification");
                return new NotificationResult { Success = false, Error = ex.Message };
            }
        }

        private async Task<NotificationResult> SendPushNotificationAsync(NotificationRequest request, CancellationToken cancellationToken)
        {
            if (_pushNotificationService == null)
                return new NotificationResult { Success = false, Error = "Push notification service is not configured" };

            try
            {
                // For push notifications, we need user IDs or device tokens
                var userIds = request.To.Where(r => !string.IsNullOrEmpty(r.UserId)).Select(r => r.UserId).ToList();
                if (!userIds.Any())
                    return new NotificationResult { Success = false, Error = "At least one user ID is required for push notifications" };

                // Send push notification
                var result = await _pushNotificationService.SendPushNotificationAsync(
                    userIds: userIds,
                    title: request.Subject,
                    message: request.Content,
                    data: request.Data,
                    priority: ConvertToPushPriority(request.Priority),
                    cancellationToken: cancellationToken);

                return new NotificationResult
                {
                    Success = result.Success,
                    MessageId = result.MessageId,
                    Error = result.ErrorMessage,
                    Metadata = result.Metadata
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending push notification");
                return new NotificationResult { Success = false, Error = ex.Message };
            }
        }

        private async Task<NotificationResult> SendInAppNotificationAsync(NotificationRequest request, CancellationToken cancellationToken)
        {
            if (_inAppNotificationService == null)
                return new NotificationResult { Success = false, Error = "In-app notification service is not configured" };

            try
            {
                // For in-app notifications, we need user IDs
                var userIds = request.To.Where(r => !string.IsNullOrEmpty(r.UserId)).Select(r => r.UserId).ToList();
                if (!userIds.Any())
                    return new NotificationResult { Success = false, Error = "At least one user ID is required for in-app notifications" };

                // Send in-app notification
                var result = await _inAppNotificationService.SendInAppNotificationAsync(
                    userIds: userIds,
                    title: request.Subject,
                    message: request.Content,
                    data: request.Data,
                    isImportant: request.Priority >= NotificationPriority.High,
                    cancellationToken: cancellationToken);

                return new NotificationResult
                {
                    Success = result.Success,
                    MessageId = result.NotificationId,
                    Error = result.ErrorMessage,
                    Metadata = result.Metadata
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending in-app notification");
                return new NotificationResult { Success = false, Error = ex.Message };
            }
        }

        public async Task<IEnumerable<NotificationTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.NotificationTemplates
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.Name)
                    .ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification templates");
                throw;
            }
        }

        public async Task<NotificationTemplate> GetTemplateByIdAsync(string templateId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(templateId))
                return null;

            try
            {
                return await _context.NotificationTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId && t.IsActive, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification template with ID: {TemplateId}", templateId);
                throw;
            }
        }

        public async Task<NotificationTemplate> SaveTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            try
            {
                var now = DateTime.UtcNow;
                var existingTemplate = await _context.NotificationTemplates
                    .FirstOrDefaultAsync(t => t.Id == template.Id, cancellationToken);

                if (existingTemplate == null)
                {
                    // New template
                    template.CreatedAt = now;
                    template.UpdatedAt = now;
                    await _context.NotificationTemplates.AddAsync(template, cancellationToken);
                }
                else
                {
                    // Update existing template
                    existingTemplate.Name = template.Name;
                    existingTemplate.Description = template.Description;
                    existingTemplate.SubjectTemplate = template.SubjectTemplate;
                    existingTemplate.HtmlTemplate = template.HtmlTemplate;
                    existingTemplate.TextTemplate = template.TextTemplate;
                    existingTemplate.IsActive = template.IsActive;
                    existingTemplate.UpdatedAt = now;
                    existingTemplate.Tags = template.Tags;
                }

                await _context.SaveChangesAsync(cancellationToken);
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving notification template with ID: {TemplateId}", template.Id);
                throw;
            }
        }

        public async Task<bool> DeleteTemplateAsync(string templateId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(templateId))
                return false;

            try
            {
                var template = await _context.NotificationTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

                if (template == null)
                    return false;

                // Soft delete
                template.IsActive = false;
                template.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification template with ID: {TemplateId}", templateId);
                throw;
            }
        }

        #region Helper Methods

        private NotificationRequest ApplyTemplate(NotificationRequest request, NotificationTemplate template)
        {
            if (template == null)
                return request;

            // Apply template to request
            if (string.IsNullOrEmpty(request.Subject) && !string.IsNullOrEmpty(template.SubjectTemplate))
                request.Subject = template.SubjectTemplate;

            if (string.IsNullOrEmpty(request.Content) && !string.IsNullOrEmpty(template.HtmlTemplate))
                request.Content = template.HtmlTemplate;
            else if (string.IsNullOrEmpty(request.Content) && !string.IsNullOrEmpty(template.TextTemplate))
                request.Content = template.TextTemplate;

            // TODO: Apply template variables and logic here
            // This is a simplified version - in a real implementation, you would use a templating engine
            // like Handlebars, Razor, or similar to process the template with the provided data

            return request;
        }

        private PushNotificationPriority ConvertToPushPriority(NotificationPriority priority)
        {
            return priority switch
            {
                NotificationPriority.Low => PushNotificationPriority.Low,
                NotificationPriority.Normal => PushNotificationPriority.Normal,
                NotificationPriority.High => PushNotificationPriority.High,
                NotificationPriority.Urgent => PushNotificationPriority.High,
                _ => PushNotificationPriority.Normal
            };
        }

        #endregion

        #region IDisposable Implementation

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _emailSender?.Dispose();
                    _smsSender?.Dispose();
                    _pushNotificationService?.Dispose();
                    _inAppNotificationService?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~NotificationService()
        {
            Dispose(disposing: false);
        }

        #endregion
    }

    public class NotificationOptions
    {
        public string DefaultFromEmail { get; set; }
        public string DefaultFromName { get; set; }
        public string SmsDefaultSender { get; set; }
        public int MaxRetryAttempts { get; set; } = 3;
        public int RetryDelayMs { get; set; } = 1000;
    }
}
