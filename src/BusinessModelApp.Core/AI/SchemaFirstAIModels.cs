using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.AI
{
    public class PromptTemplate
    {
        public string PromptId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Version { get; set; } = 1;
        public string SystemPrompt { get; set; } = string.Empty;
        public string OutputSchemaName { get; set; } = string.Empty;
        public List<string> RequiredJsonProperties { get; set; } = new();
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public interface IPromptRegistry
    {
        void RegisterPrompt(PromptTemplate template);
        PromptTemplate? GetPrompt(string promptId, int? version = null);
        IReadOnlyList<PromptTemplate> GetPromptHistory(string promptId);
    }

    public interface IJsonSchemaValidator
    {
        bool ValidateOutput(string jsonPayload, List<string> requiredProperties, out List<string> errors);
    }
}
