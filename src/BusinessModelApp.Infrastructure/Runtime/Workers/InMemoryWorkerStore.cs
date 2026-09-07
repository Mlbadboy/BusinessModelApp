using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Capabilities;
using BusinessModelApp.Core.Domain.Runtime.Workers;
using BusinessModelApp.Core.Interfaces.Runtime.Workers;

namespace BusinessModelApp.Infrastructure.Runtime.Workers
{
    public class InMemoryWorkerStore : IWorkerStore
    {
        private readonly ConcurrentDictionary<WorkerDefinitionId, WorkerDefinition> _definitions = new();
        private readonly ConcurrentDictionary<WorkerInstanceId, WorkerInstance> _instances = new();
        private readonly ConcurrentDictionary<WorkerProcessId, WorkerProcess> _processes = new();
        private readonly ConcurrentDictionary<CapabilityId, UniversalCapabilityDefinition> _capabilities = new();
        private readonly ConcurrentDictionary<Guid, WorkerActionProposal> _proposals = new();
        private readonly ConcurrentDictionary<WorkerDefinitionId, WorkerHealthSnapshot> _health = new();
        private readonly ConcurrentBag<WorkerIsolationViolation> _violations = new();

        public Task SaveWorkerDefinitionAsync(WorkerDefinition definition, CancellationToken ct = default)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            _definitions[definition.WorkerDefinitionId] = definition;
            return Task.CompletedTask;
        }

        public Task<WorkerDefinition?> GetWorkerDefinitionAsync(WorkerDefinitionId definitionId, CancellationToken ct = default)
        {
            _definitions.TryGetValue(definitionId, out var def);
            return Task.FromResult(def);
        }

        public Task<IReadOnlyList<WorkerDefinition>> GetWorkerDefinitionsAsync(Guid workspaceId, CancellationToken ct = default)
        {
            var list = _definitions.Values.Where(d => d.WorkspaceId == workspaceId).ToList();
            return Task.FromResult<IReadOnlyList<WorkerDefinition>>(list);
        }

        public Task SaveWorkerInstanceAsync(WorkerInstance instance, CancellationToken ct = default)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            _instances[instance.WorkerInstanceId] = instance;
            return Task.CompletedTask;
        }

        public Task<WorkerInstance?> GetWorkerInstanceAsync(WorkerInstanceId instanceId, CancellationToken ct = default)
        {
            _instances.TryGetValue(instanceId, out var inst);
            return Task.FromResult(inst);
        }

        public Task<IReadOnlyList<WorkerInstance>> GetWorkerInstancesAsync(Guid workspaceId, CancellationToken ct = default)
        {
            var list = _instances.Values.Where(i => i.WorkspaceId == workspaceId).ToList();
            return Task.FromResult<IReadOnlyList<WorkerInstance>>(list);
        }

        public Task SaveWorkerProcessAsync(WorkerProcess process, CancellationToken ct = default)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            _processes[process.ProcessId] = process;
            return Task.CompletedTask;
        }

        public Task<WorkerProcess?> GetWorkerProcessAsync(WorkerProcessId processId, CancellationToken ct = default)
        {
            _processes.TryGetValue(processId, out var proc);
            return Task.FromResult(proc);
        }

        public Task SaveCapabilityDefinitionAsync(UniversalCapabilityDefinition capability, CancellationToken ct = default)
        {
            if (capability == null) throw new ArgumentNullException(nameof(capability));
            _capabilities[capability.CapabilityId] = capability;
            return Task.CompletedTask;
        }

        public Task<UniversalCapabilityDefinition?> GetCapabilityDefinitionAsync(CapabilityId capabilityId, CancellationToken ct = default)
        {
            _capabilities.TryGetValue(capabilityId, out var cap);
            return Task.FromResult(cap);
        }

        public Task<IReadOnlyList<UniversalCapabilityDefinition>> GetCapabilityDefinitionsAsync(CancellationToken ct = default)
        {
            var list = _capabilities.Values.ToList();
            return Task.FromResult<IReadOnlyList<UniversalCapabilityDefinition>>(list);
        }

        public Task SaveActionProposalAsync(WorkerActionProposal proposal, CancellationToken ct = default)
        {
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));
            _proposals[proposal.ProposalId] = proposal;
            return Task.CompletedTask;
        }

        public Task<WorkerActionProposal?> GetActionProposalAsync(Guid proposalId, CancellationToken ct = default)
        {
            _proposals.TryGetValue(proposalId, out var p);
            return Task.FromResult(p);
        }

        public Task<IReadOnlyList<WorkerActionProposal>> GetActionProposalsAsync(Guid workspaceId, CancellationToken ct = default)
        {
            var list = _proposals.Values.Where(p => p.WorkspaceId == workspaceId).ToList();
            return Task.FromResult<IReadOnlyList<WorkerActionProposal>>(list);
        }

        public Task RecordIsolationViolationAsync(WorkerIsolationViolation violation, CancellationToken ct = default)
        {
            if (violation == null) throw new ArgumentNullException(nameof(violation));
            _violations.Add(violation);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<WorkerIsolationViolation>> GetIsolationViolationsAsync(Guid workspaceId, CancellationToken ct = default)
        {
            var list = _violations.Where(v => v.WorkspaceId == workspaceId).ToList();
            return Task.FromResult<IReadOnlyList<WorkerIsolationViolation>>(list);
        }

        public Task SaveHealthSnapshotAsync(WorkerHealthSnapshot snapshot, CancellationToken ct = default)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            _health[snapshot.WorkerDefinitionId] = snapshot;
            return Task.CompletedTask;
        }

        public Task<WorkerHealthSnapshot?> GetHealthSnapshotAsync(WorkerDefinitionId definitionId, CancellationToken ct = default)
        {
            _health.TryGetValue(definitionId, out var h);
            return Task.FromResult(h);
        }

        public Task<IReadOnlyList<WorkerHealthSnapshot>> GetHealthSnapshotsAsync(CancellationToken ct = default)
        {
            var list = _health.Values.ToList();
            return Task.FromResult<IReadOnlyList<WorkerHealthSnapshot>>(list);
        }
    }
}
