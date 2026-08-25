using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Infrastructure.Services
{
    public class EmailAttachment
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
        public bool IsInline { get; set; }
        public string ContentId { get; set; }
    }

    public class EmailResult
    {
        public bool Success { get; set; }
        public string MessageId { get; set; }
        public string ErrorMessage { get; set; }
        public string Provider { get; set; }
        public string Response { get; set; }
    }

    public interface IEmailSender
    {
        Task<EmailResult> SendEmailAsync(
            string toEmail,
            string toName,
            string subject,
            string htmlContent,
            string textContent = null,
            string fromEmail = null,
            string fromName = null,
            IEnumerable<(string Email, string Name)> cc = null,
            IEnumerable<(string Email, string Name)> bcc = null,
            IEnumerable<EmailAttachment> attachments = null,
            CancellationToken cancellationToken = default);

        Task<EmailResult> SendEmailAsync(
            IEnumerable<(string Email, string Name)> to,
            string subject,
            string htmlContent,
            string textContent = null,
            string replyTo = null,
            string replyToName = null,
            IEnumerable<(string Email, string Name)> cc = null,
            IEnumerable<(string Email, string Name)> bcc = null,
            IEnumerable<EmailAttachment> attachments = null,
            CancellationToken cancellationToken = default);
    }
}
