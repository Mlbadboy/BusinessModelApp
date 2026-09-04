using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Execution;

namespace BusinessModelApp.Core.Interfaces
{
    public interface IRiskEngine
    {
        ExecutionRiskTier EvaluateRisk(ExecutionRequest request);
    }
}
