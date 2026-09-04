using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.AI;

namespace BusinessModelApp.Infrastructure.AI
{
    public class ModelRegistry : IModelRegistry
    {
        private readonly ConcurrentDictionary<string, ModelDescriptor> _models = new(StringComparer.OrdinalIgnoreCase);

        public ModelRegistry()
        {
            SeedDefaultModels();
        }

        private void SeedDefaultModels()
        {
            RegisterModel(new ModelDescriptor
            {
                ModelId = "fast-gpt4o-mini",
                Name = "GPT-4o Mini",
                Provider = "OpenAI",
                Tier = ModelTier.FastAndCheap,
                ContextWindow = 128000,
                CostPer1kTokensINR = 0.02m,
                TypicalLatencyMs = 400,
                PrivacyClass = PrivacyClassification.InternalEnterprise,
                Capabilities = new List<string> { "text", "fast", "classification" },
                ReliabilityScore = 0.96
            });

            RegisterModel(new ModelDescriptor
            {
                ModelId = "reasoning-claude-3-5-sonnet",
                Name = "Claude 3.5 Sonnet",
                Provider = "Anthropic",
                Tier = ModelTier.ReasoningAndStrategy,
                ContextWindow = 200000,
                CostPer1kTokensINR = 0.25m,
                TypicalLatencyMs = 1200,
                PrivacyClass = PrivacyClassification.ConfidentialFinancial,
                Capabilities = new List<string> { "text", "reasoning", "strategy", "vision" },
                ReliabilityScore = 0.98
            });

            RegisterModel(new ModelDescriptor
            {
                ModelId = "reasoning-o1-preview",
                Name = "OpenAI o1 Preview",
                Provider = "OpenAI",
                Tier = ModelTier.ReasoningAndStrategy,
                ContextWindow = 128000,
                CostPer1kTokensINR = 1.20m,
                TypicalLatencyMs = 3500,
                PrivacyClass = PrivacyClassification.ConfidentialFinancial,
                Capabilities = new List<string> { "text", "deep-reasoning", "math" },
                ReliabilityScore = 0.99
            });

            RegisterModel(new ModelDescriptor
            {
                ModelId = "coding-claude-3-7-sonnet",
                Name = "Claude 3.7 Sonnet Coding",
                Provider = "Anthropic",
                Tier = ModelTier.CodingAndDelivery,
                ContextWindow = 200000,
                CostPer1kTokensINR = 0.30m,
                TypicalLatencyMs = 1500,
                PrivacyClass = PrivacyClassification.InternalEnterprise,
                Capabilities = new List<string> { "coding", "architecture", "refactoring" },
                ReliabilityScore = 0.97
            });

            RegisterModel(new ModelDescriptor
            {
                ModelId = "vision-gemini-1-5-pro",
                Name = "Gemini 1.5 Pro Multimodal",
                Provider = "Google",
                Tier = ModelTier.VisionAndMultimodal,
                ContextWindow = 1000000,
                CostPer1kTokensINR = 0.20m,
                TypicalLatencyMs = 1400,
                PrivacyClass = PrivacyClassification.InternalEnterprise,
                Capabilities = new List<string> { "vision", "multimodal", "pdf" },
                ReliabilityScore = 0.95
            });
        }

        public void RegisterModel(ModelDescriptor descriptor)
        {
            if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
            _models[descriptor.ModelId] = descriptor;
        }

        public ModelDescriptor? GetModel(string modelId)
        {
            _models.TryGetValue(modelId, out var model);
            return model;
        }

        public IReadOnlyList<ModelDescriptor> GetModelsByTier(ModelTier tier)
        {
            return _models.Values.Where(m => m.Tier == tier && m.IsActive).ToList();
        }

        public IReadOnlyList<ModelDescriptor> GetAllModels()
        {
            return _models.Values.Where(m => m.IsActive).ToList();
        }
    }

    public class ModelRouter : IModelRouter
    {
        private readonly IModelRegistry _registry;

        public ModelRouter(IModelRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public ModelRoutingDecision Route(ModelRoutingRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var availableModels = _registry.GetAllModels()
                .Where(m => m.PrivacyClass >= request.MinimumPrivacy)
                .ToList();

            if (!availableModels.Any())
            {
                throw new InvalidOperationException($"No model found satisfying privacy level {request.MinimumPrivacy}");
            }

            // Filter by requested tier if available
            var tierModels = availableModels.Where(m => m.Tier == request.PreferredTier).ToList();
            var candidates = tierModels.Any() ? tierModels : availableModels;

            // Sort by budget adherence, latency, and reliability
            var selected = candidates
                .OrderBy(m => (m.CostPer1kTokensINR * (request.EstimatedTokens / 1000m)) > request.MaxBudgetINR ? 1 : 0)
                .ThenByDescending(m => m.ReliabilityScore)
                .ThenBy(m => m.TypicalLatencyMs)
                .First();

            var estimatedCost = selected.CostPer1kTokensINR * (request.EstimatedTokens / 1000m);
            var fallbacks = availableModels
                .Where(m => m.ModelId != selected.ModelId)
                .OrderByDescending(m => m.ReliabilityScore)
                .Select(m => m.ModelId)
                .Take(2)
                .ToList();

            return new ModelRoutingDecision
            {
                SelectedModelId = selected.ModelId,
                Provider = selected.Provider,
                Tier = selected.Tier,
                EstimatedCostINR = estimatedCost,
                FallbackModelIds = fallbacks,
                RoutingReason = $"Matched tier {selected.Tier} with privacy class {selected.PrivacyClass} within budget INR {request.MaxBudgetINR}",
                DecidedAtUtc = DateTime.UtcNow
            };
        }
    }

    public class DeterministicMockInferenceGateway : IAIInferenceGateway
    {
        public Task<AIResponse> ExecuteAsync(AIRequest request, CancellationToken ct = default)
        {
            var content = $"[DETERMINISTIC_MOCK_COMPLETION] Task: {request.TaskType}, Correlation: {request.RequestCorrelationId}";
            var response = new AIResponse
            {
                Content = content,
                ModelUsed = "deterministic-mock-v1",
                ProviderUsed = "LocalDeterministicMock",
                RequestId = Guid.NewGuid().ToString("N"),
                Usage = new AIUsage
                {
                    PromptTokens = 150,
                    CompletionTokens = 85
                },
                LatencyMs = 25,
                EstimatedCost = 0.005m,
                CacheHit = false,
                FallbackAttempts = 0,
                FinishReason = "stop"
            };

            return Task.FromResult(response);
        }

        public async IAsyncEnumerable<AIStreamChunk> StreamAsync(AIRequest request, [EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return new AIStreamChunk { DeltaContent = "[DETERMINISTIC_STREAM_START] " };
            await Task.Yield();
            yield return new AIStreamChunk { DeltaContent = "Task processed successfully." };
            yield return new AIStreamChunk { DeltaContent = string.Empty, FinishReason = "stop" };
        }

        public Task<float[]> GetEmbeddingAsync(string text, CancellationToken ct = default)
        {
            var embedding = new float[128];
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] = (float)Math.Sin(text.Length + i);
            }
            return Task.FromResult(embedding);
        }
    }
}
