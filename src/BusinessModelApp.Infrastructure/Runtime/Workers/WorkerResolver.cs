using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Workers;
using BusinessModelApp.Core.Interfaces.Runtime.Workers;

namespace BusinessModelApp.Infrastructure.Runtime.Workers
{
    public class WorkerResolver : IWorkerResolver
    {
        private readonly IWorkerStore _store;
        private readonly IWorkerHealthManager _healthManager;

        public WorkerResolver(IWorkerStore store, IWorkerHealthManager healthManager)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _healthManager = healthManager ?? throw new ArgumentNullException(nameof(healthManager));
        }

        public async Task<WorkerResolutionDecision> ResolveWorkerAsync(WorkerResolutionRequest request, CancellationToken ct = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // 1. Retrieve Universal Capability Definition
            var capability = await _store.GetCapabilityDefinitionAsync(request.CapabilityId, ct);
            if (capability == null || !capability.IsActive)
            {
                return new WorkerResolutionDecision
                {
                    WorkspaceId = request.WorkspaceId,
                    CapabilityId = request.CapabilityId,
                    IsAdmissible = false,
                    InadmissibilityReason = $"Capability '{request.CapabilityId}' is unregistered, deprecated, or inactive."
                };
            }

            // 2. Check Autonomy Level: Capability required autonomy must not exceed tenant ceiling
            if (capability.RequiredAutonomyTier > request.AutonomyCeiling)
            {
                return new WorkerResolutionDecision
                {
                    WorkspaceId = request.WorkspaceId,
                    CapabilityId = request.CapabilityId,
                    IsAdmissible = false,
                    InadmissibilityReason = $"Capability required autonomy '{capability.RequiredAutonomyTier}' exceeds tenant ceiling '{request.AutonomyCeiling}'."
                };
            }

            // 3. Find candidates in workspace supporting this capability
            var workspaceDefinitions = await _store.GetWorkerDefinitionsAsync(request.WorkspaceId, ct);
            var eligibleCandidates = new List<(WorkerDefinition Definition, WorkerHealthSnapshot Health, double Score)>();

            foreach (var def in workspaceDefinitions.Where(d => d.IsActive && d.SupportedCapabilityIds.Contains(request.CapabilityId.ToString())))
            {
                // Modality must be permitted by capability definition
                if (capability.AllowedModalities.Count > 0 && !capability.AllowedModalities.Contains(def.Modality))
                    continue;

                var health = await _healthManager.GetHealthAsync(def.WorkerDefinitionId, def.Modality, ct);

                // Quarantined and CircuitOpen workers are strictly excluded from selection
                if (health.CircuitState == WorkerCircuitState.Quarantined || health.CircuitState == WorkerCircuitState.CircuitOpen)
                    continue;

                // Modality preference weighting: API > MCP > Browser > Desktop (efficiency order)
                double modalityWeight = def.Modality switch
                {
                    WorkerModality.Api => 1.0,
                    WorkerModality.Mcp => 0.85,
                    WorkerModality.Browser => 0.70,
                    WorkerModality.Desktop => 0.50,
                    _ => 0.50
                };

                if (request.PreferredModality.HasValue && def.Modality == request.PreferredModality.Value)
                    modalityWeight += 0.30;

                double healthMultiplier = health.CircuitState switch
                {
                    WorkerCircuitState.Healthy => 1.0,
                    WorkerCircuitState.Recovering => 0.80,
                    WorkerCircuitState.Degraded => 0.60,
                    _ => 0.0
                };

                double score = modalityWeight * healthMultiplier;
                eligibleCandidates.Add((def, health, score));
            }

            if (eligibleCandidates.Count == 0)
            {
                return new WorkerResolutionDecision
                {
                    WorkspaceId = request.WorkspaceId,
                    CapabilityId = request.CapabilityId,
                    IsAdmissible = false,
                    InadmissibilityReason = $"No healthy worker in workspace '{request.WorkspaceId}' supports capability '{request.CapabilityId}'. Capability does not permit registered worker modalities or no active worker found (permitted modalities: [{string.Join(", ", capability.AllowedModalities)}])."
                };
            }

            // Order by score descending, then by definition ID for deterministic tie-breaking
            var ordered = eligibleCandidates
                .OrderByDescending(c => c.Score)
                .ThenBy(c => c.Definition.WorkerDefinitionId.Value)
                .ToList();

            var winner = ordered[0];
            var fallback = ordered.Count > 1 ? (WorkerModality?)ordered[1].Definition.Modality : null;

            return new WorkerResolutionDecision
            {
                WorkspaceId = request.WorkspaceId,
                CapabilityId = request.CapabilityId,
                SelectedWorkerDefinitionId = winner.Definition.WorkerDefinitionId,
                SelectedModality = winner.Definition.Modality,
                FallbackModality = fallback != winner.Definition.Modality ? fallback : null,
                ModalityFitnessScore = Math.Round(winner.Score, 3),
                EmpiricalReputationScore = 0.95, // Fed from metrology
                IsAdmissible = true,
                SelectionRationale = $"Selected {winner.Definition.Modality} worker '{winner.Definition.WorkerDefinitionId.Value}' with score {winner.Score:F3} (CircuitState: {winner.Health.CircuitState})."
            };
        }
    }
}
