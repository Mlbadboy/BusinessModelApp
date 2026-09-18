using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce
{
    public interface ICharlieAgentRuntime
    {
        string ProviderName { get; }
        Task<string> ExecuteCognitiveStepAsync(string tenantId, string prompt, IReadOnlyDictionary<string, object> context);
    }

    public interface IAgentHarnessService
    {
        Task<AgentSession> StartSessionAsync(string tenantId, string agentId, string missionId);
        Task<AgentSession?> GetSessionAsync(string tenantId, string sessionId);
        Task<bool> EndSessionAsync(string tenantId, string sessionId);

        Task<AgentTrajectory> RecordTrajectoryStepAsync(string tenantId, string sessionId, AgentTrajectoryStep step);
        Task<AgentTrajectory?> GetTrajectoryAsync(string tenantId, string sessionId);

        Task<AgentInstance> SpawnSubAgentAsync(string tenantId, AgentSpawnRequest request);
        Task<bool> TerminateAgentInstanceAsync(string tenantId, string instanceId);
        Task<IReadOnlyList<AgentInstance>> ListActiveInstancesAsync(string tenantId);

        // Memory vs Context Isolation
        Task SaveAgentMemoryAsync(string tenantId, AgentMemoryEntry memory);
        Task<IReadOnlyList<AgentMemoryEntry>> GetAgentMemoryAsync(string tenantId, string agentId);
        Task<string> AssembleExecutionPromptAsync(string tenantId, string agentId, string basePrompt, string missionContext);
    }
}
