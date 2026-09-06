using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Fleet;
using BusinessModelApp.Core.Interfaces.Runtime.Fleet;

namespace BusinessModelApp.Infrastructure.Runtime.Fleet
{
    public class InMemoryAgentFleetStore : IAgentFleetStore
    {
        private readonly ConcurrentDictionary<string, AgentDefinitionRecord> _definitions = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<Guid, AgentInstanceRecord> _instances = new();
        private readonly ConcurrentDictionary<Guid, WorkerProcessRecord> _workers = new();

        public Task RegisterDefinitionAsync(AgentDefinitionRecord definition, CancellationToken ct = default)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            _definitions[definition.DefinitionId.Value] = definition;
            return Task.CompletedTask;
        }

        public Task<AgentDefinitionRecord?> GetDefinitionAsync(AgentDefinitionId definitionId, CancellationToken ct = default)
        {
            _definitions.TryGetValue(definitionId.Value, out var def);
            return Task.FromResult(def);
        }

        public Task RegisterInstanceAsync(AgentInstanceRecord instance, CancellationToken ct = default)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            _instances[instance.InstanceId.Value] = instance;
            return Task.CompletedTask;
        }

        public Task<AgentInstanceRecord?> GetInstanceAsync(AgentInstanceId instanceId, CancellationToken ct = default)
        {
            _instances.TryGetValue(instanceId.Value, out var inst);
            return Task.FromResult(inst);
        }

        public Task RegisterWorkerAsync(WorkerProcessRecord worker, CancellationToken ct = default)
        {
            if (worker == null) throw new ArgumentNullException(nameof(worker));
            _workers[worker.WorkerId.Value] = worker;
            return Task.CompletedTask;
        }

        public Task<WorkerProcessRecord?> GetWorkerAsync(WorkerProcessId workerId, CancellationToken ct = default)
        {
            _workers.TryGetValue(workerId.Value, out var worker);
            return Task.FromResult(worker);
        }

        public Task<IReadOnlyList<WorkerProcessRecord>> GetAvailableWorkersAsync(WorkerPoolType poolType, CancellationToken ct = default)
        {
            var matched = _workers.Values
                .Where(w => w.PoolType == poolType && w.HealthStatus == WorkerHealthStatus.Healthy)
                .ToList();

            return Task.FromResult<IReadOnlyList<WorkerProcessRecord>>(matched);
        }

        public Task<IReadOnlyList<WorkerProcessRecord>> GetAllWorkersAsync(CancellationToken ct = default)
        {
            var all = _workers.Values.ToList();
            return Task.FromResult<IReadOnlyList<WorkerProcessRecord>>(all);
        }
    }
}
