using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;

namespace BusinessModelApp.Core.Interfaces.Runtime
{
    public interface IRuntimeAdmissionController
    {
        Task<AdmissionDecision> EvaluateAdmissionAsync(
            AdmissionRequest request,
            CancellationToken cancellationToken = default);

        void RecordRunCompleted(RuntimeRunId runId);
    }
}
