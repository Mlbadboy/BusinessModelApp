using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce
{
    public interface IAgentHarnessStore
    {
        Task SaveSessionAsync(string tenantId, AgentSession session);
        Task<AgentSession?> GetSessionAsync(string tenantId, string sessionId);

        Task SaveTrajectoryAsync(string tenantId, AgentTrajectory trajectory);
        Task<AgentTrajectory?> GetTrajectoryAsync(string tenantId, string sessionId);

        Task SaveInstanceAsync(string tenantId, AgentInstance instance);
        Task<AgentInstance?> GetInstanceAsync(string tenantId, string instanceId);
        Task<IReadOnlyList<AgentInstance>> ListInstancesAsync(string tenantId);

        Task SaveMemoryAsync(string tenantId, AgentMemoryEntry memory);
        Task<IReadOnlyList<AgentMemoryEntry>> GetAgentMemoryAsync(string tenantId, string agentId);
    }

    public class InMemoryAgentHarnessStore : IAgentHarnessStore
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AgentSession>> _sessionsByTenant = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AgentTrajectory>> _trajectoriesByTenant = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AgentInstance>> _instancesByTenant = new();
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, List<AgentMemoryEntry>>> _memoriesByTenant = new();

        public Task SaveSessionAsync(string tenantId, AgentSession session)
        {
            var map = _sessionsByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, AgentSession>());
            map[session.SessionId] = session;
            return Task.CompletedTask;
        }

        public Task<AgentSession?> GetSessionAsync(string tenantId, string sessionId)
        {
            if (_sessionsByTenant.TryGetValue(tenantId, out var map) && map.TryGetValue(sessionId, out var session))
            {
                return Task.FromResult<AgentSession?>(session);
            }
            return Task.FromResult<AgentSession?>(null);
        }

        public Task SaveTrajectoryAsync(string tenantId, AgentTrajectory trajectory)
        {
            var map = _trajectoriesByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, AgentTrajectory>());
            map[trajectory.SessionId] = trajectory;
            return Task.CompletedTask;
        }

        public Task<AgentTrajectory?> GetTrajectoryAsync(string tenantId, string sessionId)
        {
            if (_trajectoriesByTenant.TryGetValue(tenantId, out var map) && map.TryGetValue(sessionId, out var traj))
            {
                return Task.FromResult<AgentTrajectory?>(traj);
            }
            return Task.FromResult<AgentTrajectory?>(null);
        }

        public Task SaveInstanceAsync(string tenantId, AgentInstance instance)
        {
            var map = _instancesByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, AgentInstance>());
            map[instance.InstanceId] = instance;
            return Task.CompletedTask;
        }

        public Task<AgentInstance?> GetInstanceAsync(string tenantId, string instanceId)
        {
            if (_instancesByTenant.TryGetValue(tenantId, out var map) && map.TryGetValue(instanceId, out var instance))
            {
                return Task.FromResult<AgentInstance?>(instance);
            }
            return Task.FromResult<AgentInstance?>(null);
        }

        public Task<IReadOnlyList<AgentInstance>> ListInstancesAsync(string tenantId)
        {
            if (_instancesByTenant.TryGetValue(tenantId, out var map))
            {
                return Task.FromResult<IReadOnlyList<AgentInstance>>(map.Values.ToList());
            }
            return Task.FromResult<IReadOnlyList<AgentInstance>>(Array.Empty<AgentInstance>());
        }

        public Task SaveMemoryAsync(string tenantId, AgentMemoryEntry memory)
        {
            var map = _memoriesByTenant.GetOrAdd(tenantId, _ => new ConcurrentDictionary<string, List<AgentMemoryEntry>>());
            var list = map.GetOrAdd(memory.AgentId, _ => new List<AgentMemoryEntry>());
            lock (list)
            {
                list.Add(memory);
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AgentMemoryEntry>> GetAgentMemoryAsync(string tenantId, string agentId)
        {
            if (_memoriesByTenant.TryGetValue(tenantId, out var map) && map.TryGetValue(agentId, out var list))
            {
                lock (list)
                {
                    return Task.FromResult<IReadOnlyList<AgentMemoryEntry>>(list.ToList());
                }
            }
            return Task.FromResult<IReadOnlyList<AgentMemoryEntry>>(Array.Empty<AgentMemoryEntry>());
        }
    }
}
