using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BusinessModelApp.Core.AI;

namespace BusinessModelApp.Infrastructure.AI
{
    public class PromptRegistry : IPromptRegistry
    {
        private readonly ConcurrentDictionary<string, List<PromptTemplate>> _prompts = new(StringComparer.OrdinalIgnoreCase);

        public PromptRegistry()
        {
            SeedStandardPrompts();
        }

        private void SeedStandardPrompts()
        {
            RegisterPrompt(new PromptTemplate
            {
                PromptId = "commercial_strategy",
                Name = "Commercial Strategy Formulator",
                Version = 1,
                SystemPrompt = "You are Charlie CBO. Formulate strategy candidates based strictly on ground truth.",
                OutputSchemaName = "StrategyCandidateSchema",
                RequiredJsonProperties = new List<string> { "StrategyName", "SimulatedRevenueINR", "Probability", "Assumptions" }
            });

            RegisterPrompt(new PromptTemplate
            {
                PromptId = "commercial_strategy",
                Name = "Commercial Strategy Formulator",
                Version = 2,
                SystemPrompt = "You are Charlie CBO v2. Formulate strategy candidates with explicit assumption provenance and budget limits.",
                OutputSchemaName = "StrategyCandidateSchemaV2",
                RequiredJsonProperties = new List<string> { "StrategyName", "SimulatedRevenueINR", "Probability", "Assumptions", "AllocatedBudgetINR" }
            });

            RegisterPrompt(new PromptTemplate
            {
                PromptId = "prospect_qualification",
                Name = "Prospect ICP Qualifier",
                Version = 1,
                SystemPrompt = "Evaluate prospect fit against Ideal Customer Profile.",
                OutputSchemaName = "ProspectQualificationSchema",
                RequiredJsonProperties = new List<string> { "CompanyName", "ICPScore", "EvidenceSourceIds" }
            });
        }

        public void RegisterPrompt(PromptTemplate template)
        {
            if (template == null) throw new ArgumentNullException(nameof(template));

            var list = _prompts.GetOrAdd(template.PromptId, _ => new List<PromptTemplate>());
            lock (list)
            {
                // Remove older entry of same version if exists
                list.RemoveAll(p => p.Version == template.Version);
                list.Add(template);
            }
        }

        public PromptTemplate? GetPrompt(string promptId, int? version = null)
        {
            if (!_prompts.TryGetValue(promptId, out var list))
            {
                return null;
            }

            lock (list)
            {
                if (version.HasValue)
                {
                    return list.FirstOrDefault(p => p.Version == version.Value);
                }

                // Default to latest version
                return list.OrderByDescending(p => p.Version).FirstOrDefault();
            }
        }

        public IReadOnlyList<PromptTemplate> GetPromptHistory(string promptId)
        {
            if (!_prompts.TryGetValue(promptId, out var list))
            {
                return Array.Empty<PromptTemplate>();
            }

            lock (list)
            {
                return list.OrderBy(p => p.Version).ToList();
            }
        }
    }

    public class JsonSchemaValidator : IJsonSchemaValidator
    {
        public bool ValidateOutput(string jsonPayload, List<string> requiredProperties, out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(jsonPayload))
            {
                errors.Add("JSON payload is empty or null.");
                return false;
            }

            try
            {
                using var document = JsonDocument.Parse(jsonPayload);
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    errors.Add($"Root JSON must be an object, but was {root.ValueKind}.");
                    return false;
                }

                if (requiredProperties != null)
                {
                    foreach (var prop in requiredProperties)
                    {
                        if (!root.TryGetProperty(prop, out var element))
                        {
                            errors.Add($"Missing mandatory property: '{prop}'.");
                        }
                    }
                }

                return errors.Count == 0;
            }
            catch (JsonException ex)
            {
                errors.Add($"Malformed JSON payload: {ex.Message}");
                return false;
            }
        }
    }
}
