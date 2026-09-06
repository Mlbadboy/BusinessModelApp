using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Interfaces.Runtime
{
    public interface IEventBus
    {
        Task PublishAsync(
            RuntimeEventEnvelope envelope,
            CancellationToken cancellationToken = default);

        IDisposable Subscribe(
            string eventType,
            string subscriberName,
            Func<RuntimeEventEnvelope, CancellationToken, Task> handler);

        IDisposable Subscribe<TEvent>(
            string subscriberName,
            Func<TEvent, RuntimeEventEnvelope, CancellationToken, Task> handler) where TEvent : class, IRuntimeEvent;

        int GetProcessedEventCount(string subscriberName);
    }
}
