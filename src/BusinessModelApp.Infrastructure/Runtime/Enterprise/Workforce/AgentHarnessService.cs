using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce
{
    public class AgentHarnessService : IAgentHarnessService
    {
        private readonly IAgentHarnessStore _store;
        private readonly AgentSpawnPolicy _spawnPolicy;

        public AgentHarnessService(IAgentHarnessStore store, AgentSpawnPolicy? spawnPolicy = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _spawnPolicy = spawnPolicy ?? new AgentSpawnPolicy();
        }

        public async Task<AgentSession> StartSessionAsync(string tenantId, string agentId, string missionId)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("AgentId required", nameof(agentId));

            var session = new AgentSession
            {
                TenantId = tenantId,
                AgentId = agentId,
                MissionId = missionId,
                CreatedAt = DateTime.UtcNow
            };

            await _store.SaveSessionAsync(tenantId, session);
            return session;
        }

        public Task<AgentSession?> GetSessionAsync(string tenantId, string sessionId)
        {
            return _store.GetSessionAsync(tenantId, sessionId);
        }

        public async Task<bool> EndSessionAsync(string tenantId, string sessionId)
        {
            var session = await _store.GetSessionAsync(tenantId, sessionId);
            if (session == null) return false;

            session.EndedAt = DateTime.UtcNow;
            await _store.SaveSessionAsync(tenantId, session);
            return true;
        }

        public async Task<AgentTrajectory> RecordTrajectoryStepAsync(string tenantId, string sessionId, AgentTrajectoryStep step)
        {
            var traj = await _store.GetTrajectoryAsync(tenantId, sessionId);
            if (traj == null)
            {
                traj = new AgentTrajectory
                {
                    TenantId = tenantId,
                    SessionId = sessionId,
                    CreatedAt = DateTime.UtcNow
                };
            }

            step.StepIndex = traj.Steps.Count + 1;
            step.ExecutedAt = DateTime.UtcNow;
            traj.Steps.Add(step);

            await _store.SaveTrajectoryAsync(tenantId, traj);
            return traj;
        }

        public Task<AgentTrajectory?> GetTrajectoryAsync(string tenantId, string sessionId)
        {
            return _store.GetTrajectoryAsync(tenantId, sessionId);
        }

        public async Task<AgentInstance> SpawnSubAgentAsync(string tenantId, AgentSpawnRequest request)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required", nameof(tenantId));
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Check active agent count limit
            var activeInstances = await _store.ListInstancesAsync(tenantId);
            var activeCount = activeInstances.Count(i => !i.TerminatedAt.HasValue);
            if (activeCount >= _spawnPolicy.MaxActiveAgentsPerTenant)
            {
                throw new InvalidOperationException($"Constitutional Violation (I39-U): Max active agents limit ({_spawnPolicy.MaxActiveAgentsPerTenant}) reached for tenant {tenantId}.");
            }

            int spawnDepth = 0;
            if (!string.IsNullOrWhiteSpace(request.ParentAgentId))
            {
                var parentInstance = activeInstances.FirstOrDefault(i => i.AgentId == request.ParentAgentId && !i.TerminatedAt.HasValue);
                if (parentInstance != null)
                {
                    spawnDepth = parentInstance.SpawnDepth + 1;
                    if (spawnDepth > _spawnPolicy.MaxSpawnDepth)
                    {
                        throw new InvalidOperationException($"Constitutional Violation (I39-U): Max spawn depth ({_spawnPolicy.MaxSpawnDepth}) exceeded.");
                    }

                    if (parentInstance.ChildInstanceIds.Count >= _spawnPolicy.MaxChildAgents)
                    {
                        throw new InvalidOperationException($"Constitutional Violation (I39-U): Parent agent {request.ParentAgentId} exceeded max child limit ({_spawnPolicy.MaxChildAgents}).");
                    }
                }
            }

            var newInstance = new AgentInstance
            {
                TenantId = tenantId,
                AgentId = Guid.NewGuid().ToString("N"),
                DefinitionId = request.TargetRoleTitle,
                ParentInstanceId = request.ParentAgentId,
                SpawnDepth = spawnDepth,
                CurrentMissionId = request.MissionId,
                Status = AgentLifecycleStatus.ACTIVE,
                StartedAt = DateTime.UtcNow
            };

            await _store.SaveInstanceAsync(tenantId, newInstance);

            if (!string.IsNullOrWhiteSpace(request.ParentAgentId))
            {
                var parentInstance = activeInstances.FirstOrDefault(i => i.AgentId == request.ParentAgentId && !i.TerminatedAt.HasValue);
                if (parentInstance != null)
                {
                    parentInstance.ChildInstanceIds.Add(newInstance.InstanceId);
                    await _store.SaveInstanceAsync(tenantId, parentInstance);
                }
            }

            return newInstance;
        }

        public async Task<bool> TerminateAgentInstanceAsync(string tenantId, string instanceId)
        {
            var instance = await _store.GetInstanceAsync(tenantId, instanceId);
            if (instance == null) return false;

            instance.TerminatedAt = DateTime.UtcNow;
            instance.Status = AgentLifecycleStatus.RETIRED;
            await _store.SaveInstanceAsync(tenantId, instance);
            return true;
        }

        public async Task<IReadOnlyList<AgentInstance>> ListActiveInstancesAsync(string tenantId)
        {
            var all = await _store.ListInstancesAsync(tenantId);
            return all.Where(i => !i.TerminatedAt.HasValue).ToList();
        }

        public async Task SaveAgentMemoryAsync(string tenantId, AgentMemoryEntry memory)
        {
            memory.TenantId = tenantId;
            if (string.IsNullOrWhiteSpace(memory.MemoryId))
            {
                memory.MemoryId = Guid.NewGuid().ToString("N");
            }
            memory.CreatedAt = DateTime.UtcNow;

            await _store.SaveMemoryAsync(tenantId, memory);
        }

        public Task<IReadOnlyList<AgentMemoryEntry>> GetAgentMemoryAsync(string tenantId, string agentId)
        {
            return _store.GetAgentMemoryAsync(tenantId, agentId);
        }

        public async Task<string> AssembleExecutionPromptAsync(string tenantId, string agentId, string basePrompt, string missionContext)
        {
            var memories = await _store.GetAgentMemoryAsync(tenantId, agentId);
            var sb = new StringBuilder();

            sb.AppendLine("=== SYSTEM MISSION CONTEXT ===");
            sb.AppendLine(missionContext);
            sb.AppendLine();

            sb.AppendLine("=== AGENT RELEVANT MEMORY (CORROBORATED ONLY) ===");
            // Filter only corroborated memories to prevent memory poisoning
            foreach (var mem in memories.Where(m => m.IsCorroborated))
            {
                sb.AppendLine($"- [{mem.Key}]: {mem.Content}");
            }
            sb.AppendLine();

            sb.AppendLine("=== INSTRUCTION ===");
            sb.AppendLine(basePrompt);

            return sb.ToString();
        }
    }
}
