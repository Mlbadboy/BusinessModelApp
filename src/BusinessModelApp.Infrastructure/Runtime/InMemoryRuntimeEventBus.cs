using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime
{
    public class InMemoryRuntimeEventBus : IEventBus
    {
        private readonly ConcurrentDictionary<string, List<(string SubscriberName, Func<RuntimeEventEnvelope, CancellationToken, Task> Handler)>> _handlers = new();
        private readonly ConcurrentDictionary<(string SubscriberName, Guid EventId), bool> _processedEvents = new();
        private readonly ConcurrentDictionary<string, int> _processedCounts = new();
        private readonly object _lock = new();

        public async Task PublishAsync(RuntimeEventEnvelope envelope, CancellationToken cancellationToken = default)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));

            var handlersToInvoke = new List<(string SubscriberName, Func<RuntimeEventEnvelope, CancellationToken, Task> Handler)>();
            lock (_lock)
            {
                if (_handlers.TryGetValue(envelope.EventType, out var list1) && list1.Count > 0)
                {
                    handlersToInvoke.AddRange(list1);
                }

                var alternateType = envelope.EventType.EndsWith("Event", StringComparison.OrdinalIgnoreCase)
                    ? envelope.EventType[..^5]
                    : envelope.EventType + "Event";

                if (_handlers.TryGetValue(alternateType, out var list2) && list2.Count > 0)
                {
                    foreach (var item in list2)
                    {
                        if (!handlersToInvoke.Contains(item))
                            handlersToInvoke.Add(item);
                    }
                }
            }

            if (handlersToInvoke.Count == 0)
                return;

            foreach (var (subscriberName, handler) in handlersToInvoke)
            {
                var deduplicationKey = (subscriberName, envelope.EventId.Value);
                if (_processedEvents.TryAdd(deduplicationKey, true))
                {
                    _processedCounts.AddOrUpdate(subscriberName, 1, (_, current) => current + 1);
                    await handler(envelope, cancellationToken);
                }
            }
        }

        public IDisposable Subscribe(
            string eventType,
            string subscriberName,
            Func<RuntimeEventEnvelope, CancellationToken, Task> handler)
        {
            if (string.IsNullOrWhiteSpace(eventType)) throw new ArgumentException("EventType cannot be empty.", nameof(eventType));
            if (string.IsNullOrWhiteSpace(subscriberName)) throw new ArgumentException("SubscriberName cannot be empty.", nameof(subscriberName));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            lock (_lock)
            {
                var list = _handlers.GetOrAdd(eventType, _ => new List<(string, Func<RuntimeEventEnvelope, CancellationToken, Task>)>());
                list.Add((subscriberName, handler));
            }

            return new SubscriptionToken(() =>
            {
                lock (_lock)
                {
                    if (_handlers.TryGetValue(eventType, out var list))
                    {
                        list.RemoveAll(h => h.SubscriberName == subscriberName && h.Handler == handler);
                    }
                }
            });
        }

        public IDisposable Subscribe<TEvent>(
            string subscriberName,
            Func<TEvent, RuntimeEventEnvelope, CancellationToken, Task> handler) where TEvent : class, IRuntimeEvent
        {
            var eventType = typeof(TEvent).Name;
            var sub1 = Subscribe(eventType, subscriberName, async (envelope, ct) =>
            {
                var domainEvent = JsonSerializer.Deserialize<TEvent>(envelope.PayloadJson);
                if (domainEvent != null)
                {
                    await handler(domainEvent, envelope, ct);
                }
            });

            var strippedType = eventType.EndsWith("Event", StringComparison.OrdinalIgnoreCase)
                ? eventType[..^5]
                : eventType;

            IDisposable? sub2 = null;
            if (!string.Equals(strippedType, eventType, StringComparison.OrdinalIgnoreCase))
            {
                sub2 = Subscribe(strippedType, subscriberName, async (envelope, ct) =>
                {
                    var domainEvent = JsonSerializer.Deserialize<TEvent>(envelope.PayloadJson);
                    if (domainEvent != null)
                    {
                        await handler(domainEvent, envelope, ct);
                    }
                });
            }

            return new SubscriptionToken(() =>
            {
                sub1.Dispose();
                sub2?.Dispose();
            });
        }

        public int GetProcessedEventCount(string subscriberName)
        {
            return _processedCounts.TryGetValue(subscriberName, out var count) ? count : 0;
        }

        private sealed class SubscriptionToken : IDisposable
        {
            private readonly Action _unsubscribe;
            private bool _disposed;

            public SubscriptionToken(Action unsubscribe)
            {
                _unsubscribe = unsubscribe;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _unsubscribe();
                    _disposed = true;
                }
            }
        }
    }
}
