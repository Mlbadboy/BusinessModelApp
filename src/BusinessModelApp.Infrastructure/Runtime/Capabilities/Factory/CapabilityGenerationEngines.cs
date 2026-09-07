using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Capabilities;
using BusinessModelApp.Core.Domain.Runtime.Capabilities.Factory;
using BusinessModelApp.Core.Domain.Runtime.Workers;
using BusinessModelApp.Core.Interfaces.Runtime.Capabilities.Factory;

namespace BusinessModelApp.Infrastructure.Runtime.Capabilities.Factory
{
    public class CapabilityGapDetector : ICapabilityGapDetector
    {
        private readonly ConcurrentDictionary<string, List<CapabilityGap>> _gapsByTenant = new();

        public Task<List<CapabilityGap>> DetectGapsAsync(string tenantId, CancellationToken ct = default)
        {
            if (_gapsByTenant.TryGetValue(tenantId, out var gaps))
            {
                return Task.FromResult(gaps.ToList());
            }
            return Task.FromResult(new List<CapabilityGap>());
        }

        public Task<CapabilityGap> RecordGapAsync(CapabilityGap gap, CancellationToken ct = default)
        {
            var list = _gapsByTenant.GetOrAdd(gap.TenantId, _ => new List<CapabilityGap>());
            lock (list)
            {
                list.Add(gap);
            }
            return Task.FromResult(gap);
        }
    }

    public class CapabilitySpecificationEngine : ICapabilitySpecifier
    {
        public Task<CapabilitySpecification> CreateSpecificationAsync(CapabilityGap gap, CancellationToken ct = default)
        {
            var spec = new CapabilitySpecification
            {
                CapabilityId = new CapabilityId(gap.RequiredCapabilityName.ToLowerInvariant().Replace(' ', '_'), "1.0.0"),
                Version = "1.0.0",
                WorkspaceId = gap.WorkspaceId,
                TenantId = gap.TenantId,
                GapId = gap.GapId,
                Title = gap.RequiredCapabilityName,
                Description = $"Autonomously synthesized capability to satisfy: {gap.BusinessNeed}",
                BusinessNeed = gap.BusinessNeed,
                InputSchemaJson = "{\"type\": \"object\", \"required\": [\"targetId\"], \"properties\": {\"targetId\": {\"type\": \"string\"}}}",
                OutputSchemaJson = "{\"type\": \"object\", \"required\": [\"status\", \"resultCode\"], \"properties\": {\"status\": {\"type\": \"string\"}, \"resultCode\": {\"type\": \"integer\"}}}",
                Preconditions = new List<string> { "Tenant context verified", "Valid input schema format" },
                Invariants = new List<string> { "Zero side-effects without Batch 6 permit", "Output complies with schema" },
                VerificationCriteria = "Returns deterministic status and compliant payload within SLA",
                RiskCeiling = gap.RiskTierCeiling,
                AutonomyCeiling = gap.AutonomyCeiling,
                IsConsequential = gap.RiskTierCeiling >= CapabilityRiskTier.R3_HighOperational,
                PermittedModalities = gap.RequiredModalities.Count > 0 ? gap.RequiredModalities : new List<WorkerModality> { WorkerModality.Api },
                DefaultTimeout = TimeSpan.FromSeconds(30),
                MaxRetries = 2,
                MaxBudgetINR = 5000.0,
                MaxConcurrency = 4,
                MaxMemoryMB = 512,
                MaxCPUCores = 1.0,
                NetworkRestricted = true,
                FilesystemRestricted = true,
            };

            return Task.FromResult(spec);
        }

        public bool ValidateSpecificationAdmissibility(CapabilitySpecification spec, out string rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(spec.Title))
            {
                rejectionReason = "Specification title cannot be empty.";
                return false;
            }

            if (spec.RiskCeiling > CapabilityRiskTier.R4_CriticalFinancial && !spec.IsConsequential)
            {
                rejectionReason = "High risk capabilities must be explicitly classified as consequential.";
                return false;
            }

            if (spec.MaxBudgetINR <= 0)
            {
                rejectionReason = "Max budget must be greater than zero.";
                return false;
            }

            if (spec.MaxMemoryMB > 2048)
            {
                rejectionReason = "Max memory ceiling exceeds factory limit of 2048 MB.";
                return false;
            }

            rejectionReason = string.Empty;
            return true;
        }
    }

    public class CapabilityDesignEngine : ICapabilityDesigner
    {
        public Task<CapabilityDesignArtifact> DesignCapabilityAsync(CapabilitySpecification spec, CancellationToken ct = default)
        {
            var design = new CapabilityDesignArtifact
            {
                CapabilityId = spec.CapabilityId,
                Version = spec.Version,
                ArchitectureSummary = $"Stateless functional worker adapter targeting {string.Join(", ", spec.PermittedModalities)}.",
                ExecutionStrategy = "Pure input-output transformation isolated in sandbox container.",
                ApprovedDependencies = new List<string> { "System.Text.Json", "System.Net.Http.Json" },
                ThreatModelRisks = new List<string>
                {
                    "Buffer overflow / malformed JSON payload injection",
                    "Unauthorized lateral privilege escalation attempt",
                    "Timeout / resource starvation in sandbox"
                },
                Mitigations = new List<string>
                {
                    "Strict JSON schema pre-validation on entrypoint",
                    "AST banned-call inspection & zero reflection permission",
                    "Enforced wall-clock timeout and cgroup memory limits"
                },
                TestPlan = "Specification-derived invariant unit tests + Strix adversarial security probes.",
            };

            return Task.FromResult(design);
        }
    }

    public class CapabilityGenerator : ICapabilityGenerator
    {
        public Task<CapabilityCodeArtifact> GenerateArtifactAsync(CapabilitySpecification spec, CapabilityDesignArtifact design, CancellationToken ct = default)
        {
            var code = $@"// Generated Autonomous Capability: {spec.Title} ({spec.Version})
using System;
using System.Text.Json;
using System.Threading.Tasks;

public class GeneratedCapabilityWorker
{{
    public static async Task<string> ExecuteAsync(string inputJson)
    {{
        // Governed execution complying with specification {spec.CapabilityId}
        using var doc = JsonDocument.Parse(inputJson);
        var targetId = doc.RootElement.TryGetProperty(""targetId"", out var prop) ? prop.GetString() : ""unknown"";
        
        var response = new
        {{
            status = ""SUCCESS"",
            resultCode = 200,
            processedTarget = targetId,
            executedAt = DateTime.UtcNow
        }};
        
        return JsonSerializer.Serialize(response);
    }}
}}";

            var manifest = JsonSerializer.Serialize(new
            {
                capabilityId = spec.CapabilityId.ToString(),
                version = spec.Version,
                title = spec.Title,
                riskTier = spec.RiskCeiling.ToString(),
                modalities = spec.PermittedModalities.Select(m => m.ToString()).ToList(),
                approvedDependencies = design.ApprovedDependencies,
            });

            using var sha = SHA256.Create();
            var sourceHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(code)));
            var manifestHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(manifest)));
            var depHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join(";", design.ApprovedDependencies))));

            var artifact = new CapabilityCodeArtifact
            {
                CapabilityId = spec.CapabilityId,
                Version = spec.Version,
                SourceCode = code,
                RuntimeLanguage = "CSharp",
                Entrypoint = "GeneratedCapabilityWorker.ExecuteAsync",
                ManifestJson = manifest,
                Dependencies = design.ApprovedDependencies,
                SourceHash = sourceHash,
                ManifestHash = manifestHash,
                DependencyHash = depHash,
                GeneratorProvenance = "AutonomousCapabilityFactory/SovereignSynthesizer",
            };

            return Task.FromResult(artifact);
        }
    }
}
