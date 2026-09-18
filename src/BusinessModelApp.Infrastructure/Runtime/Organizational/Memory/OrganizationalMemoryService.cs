using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Memory
{
    public class OrganizationalMemoryService : IOrganizationalMemoryService
    {
        private readonly IOrganizationalMemoryStore _store;
        private readonly IOrganizationalContextAssembler _assembler;
        private readonly IWorkTrajectoryRecorder _trajectoryRecorder;
        private readonly IMemoryFreshnessEvaluator _freshnessEvaluator;

        public OrganizationalMemoryService(
            IOrganizationalMemoryStore store,
            IOrganizationalContextAssembler assembler,
            IWorkTrajectoryRecorder trajectoryRecorder,
            IMemoryFreshnessEvaluator freshnessEvaluator)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _assembler = assembler ?? throw new ArgumentNullException(nameof(assembler));
            _trajectoryRecorder = trajectoryRecorder ?? throw new ArgumentNullException(nameof(trajectoryRecorder));
            _freshnessEvaluator = freshnessEvaluator ?? throw new ArgumentNullException(nameof(freshnessEvaluator));
        }

        public Task<OrganizationalContextSnapshot> GetContextForWorkAsync(
            string tenantId,
            string workId,
            ContextAssemblyPolicy? policy = null,
            CancellationToken ct = default)
        {
            return _assembler.AssembleContextAsync(tenantId, workId, policy, ct);
        }

        public Task<OrganizationalTrajectory?> GetTrajectoryForWorkAsync(
            string tenantId,
            string workId,
            CancellationToken ct = default)
        {
            return _store.GetTrajectoryForWorkAsync(tenantId, workId, ct);
        }

        public Task<MemoryProvenanceLineage?> GetMemoryProvenanceAsync(
            string tenantId,
            string memoryId,
            CancellationToken ct = default)
        {
            return _store.GetProvenanceLineageAsync(tenantId, memoryId, ct);
        }

        public Task<IReadOnlyList<OrganizationalAntiPattern>> GetAntiPatternsForDomainAsync(
            string tenantId,
            string domain,
            CancellationToken ct = default)
        {
            return _store.ListAntiPatternsAsync(tenantId, domain, ct);
        }

        public Task<FreshnessEvaluationResult> EvaluateMemoryFreshnessAsync(
            string tenantId,
            string precedentId,
            TimeSpan age,
            string? currentRegime = null,
            CancellationToken ct = default)
        {
            return _freshnessEvaluator.EvaluateFreshnessAsync(tenantId, precedentId, age, currentRegime, ct);
        }
    }
}
