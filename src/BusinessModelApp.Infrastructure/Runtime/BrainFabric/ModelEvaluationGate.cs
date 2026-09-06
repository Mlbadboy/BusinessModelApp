using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime.BrainFabric
{
    public class ModelEvaluationGate : IModelEvaluationGate
    {
        private readonly ConcurrentDictionary<string, ModelDefinition> _models = new(StringComparer.OrdinalIgnoreCase);

        public Task<bool> IsModelApprovedAsync(ModelId modelId, CancellationToken cancellationToken = default)
        {
            if (!_models.TryGetValue(modelId.Value, out var definition))
                return Task.FromResult(false);

            var isApproved = definition.ApprovalState == ModelApprovalState.Approved ||
                             definition.ApprovalState == ModelApprovalState.ConditionallyApproved;

            return Task.FromResult(isApproved);
        }

        public Task<ModelDefinition?> GetModelDefinitionAsync(ModelId modelId, CancellationToken cancellationToken = default)
        {
            _models.TryGetValue(modelId.Value, out var definition);
            return Task.FromResult(definition);
        }

        public Task RegisterModelDefinitionAsync(ModelDefinition definition, CancellationToken cancellationToken = default)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            _models[definition.ModelId.Value] = definition;
            return Task.CompletedTask;
        }
    }
}
