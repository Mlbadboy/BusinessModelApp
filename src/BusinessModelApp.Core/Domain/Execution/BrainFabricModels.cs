using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusinessModelApp.Core.Domain.Execution
{
    public enum BrainProviderType
    {
        OpenRouter = 1,
        OpenAI = 2,
        GoogleGemini = 3,
        Anthropic = 4,
        LocalOllama = 5
    }

    public enum BrainRoleType
    {
        Strategic = 1,
        Operations = 2,
        Coding = 3,
        Vision = 4,
        Fallback = 5
    }

    public enum BrainOptimizationPreference
    {
        Balanced = 0,
        CostEfficient = 1,
        LowestLatency = 2,
        MaximumQuality = 3
    }

    [Table("BrainProviderConfigs")]
    public class BrainProviderConfigEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid WorkspaceId { get; set; }

        public Guid? OrganizationId { get; set; }

        [Required]
        public BrainProviderType Provider { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsGlobalDefault { get; set; } = false;

        // AES-256-GCM Encrypted at rest, never returned in DTOs
        public string? EncryptedApiKey { get; set; }
        public string? BaseUrl { get; set; }

        [MaxLength(100)]
        public string DefaultModelId { get; set; } = "anthropic/claude-3.5-sonnet";

        [MaxLength(100)]
        public string? FallbackModelId { get; set; } = "google/gemini-flash-1.5";

        public BrainOptimizationPreference Preference { get; set; } = BrainOptimizationPreference.Balanced;

        /// <summary>
        /// JSON-serialized Dictionary&lt;BrainRoleType, string&gt;
        /// </summary>
        public string RoleMappingsJson { get; set; } = "{}";

        /// <summary>
        /// JSON-serialized List&lt;DiscoveredModelMetadata&gt;
        /// </summary>
        public string DiscoveredModelsJson { get; set; } = "[]";

        public bool LastProbePassed { get; set; } = false;
        public int LastProbeLatencyMs { get; set; } = 0;
        public DateTime? LastProbeAtUtc { get; set; }
        public string? LastProbeError { get; set; }

        public long TotalTokensUsed { get; set; } = 0;
        public decimal TotalSpendUSD { get; set; } = 0m;
        public decimal TotalSpendINR { get; set; } = 0m;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class DiscoveredModelMetadata
    {
        public string ModelId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ContextLength { get; set; } = 128000;
        public decimal PromptCostPerMillion { get; set; }
        public decimal CompletionCostPerMillion { get; set; }
        public bool SupportsVision { get; set; }
        public bool SupportsToolCalling { get; set; }
    }

    // Public DTOs - strictly zero secret exposure
    public class BrainProviderSummaryDto
    {
        public Guid Id { get; set; }
        public BrainProviderType Provider { get; set; }
        public string ProviderName => Provider.ToString();
        public bool IsActive { get; set; }
        public bool IsConfigured { get; set; }
        public string? BaseUrl { get; set; }
        public string DefaultModelId { get; set; } = string.Empty;
        public string? FallbackModelId { get; set; }
        public BrainOptimizationPreference Preference { get; set; }
        public Dictionary<string, string> RoleMappings { get; set; } = new();
        public List<DiscoveredModelMetadata> DiscoveredModels { get; set; } = new();
        public bool LastProbePassed { get; set; }
        public int LastProbeLatencyMs { get; set; }
        public DateTime? LastProbeAtUtc { get; set; }
        public long TotalTokensUsed { get; set; }
        public decimal TotalSpendINR { get; set; }
    }

    public class ConfigureBrainProviderDto
    {
        public BrainProviderType Provider { get; set; }
        public string? ApiKey { get; set; } // Plaintext input from UI - encrypted immediately
        public string? BaseUrl { get; set; }
        public string DefaultModelId { get; set; } = string.Empty;
        public string? FallbackModelId { get; set; }
        public BrainOptimizationPreference Preference { get; set; }
        public Dictionary<string, string>? RoleMappings { get; set; }
    }

    public class BrainInferenceTestRequestDto
    {
        public BrainProviderType Provider { get; set; }
        public string ModelId { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public BrainRoleType Role { get; set; } = BrainRoleType.Strategic;
    }

    public class BrainInferenceTestResponseDto
    {
        public bool Success { get; set; }
        public string ModelUsed { get; set; } = string.Empty;
        public string OutputContent { get; set; } = string.Empty;
        public int LatencyMs { get; set; }
        public int TokensEstimated { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }
}
