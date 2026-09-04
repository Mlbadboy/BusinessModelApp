using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Agents
{
    public interface IToolExecutionAdapter
    {
        AgentActionType ActionType { get; }

        Task<ToolExecutionResult> ExecuteAsync(
            AgentIdentity agent,
            AgentMission mission,
            Dictionary<string, object> parameters,
            CancellationToken ct = default);
    }
}
