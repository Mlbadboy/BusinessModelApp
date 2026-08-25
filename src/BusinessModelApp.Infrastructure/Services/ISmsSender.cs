using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Infrastructure.Services
{
    public class SmsResult
    {
        public bool Success { get; set; }
        public string MessageId { get; set; }
        public string ErrorMessage { get; set; }
        public string Provider { get; set; }
        public string Response { get; set; }
    }

    public interface ISmsSender
    {
        Task<SmsResult> SendSmsAsync(
            string to,
            string message,
            string from = null,
            CancellationToken cancellationToken = default);
    }
}
