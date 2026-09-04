using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.AI
{
    public enum AdversarialVector
    {
        Hallucination = 1,
        PromptInjection = 2,
        ToolMisuse = 3,
        EvidenceFabrication = 4,
        PolicyBypass = 5,
        BudgetManipulation = 6,
        AuthorityEscalation = 7
    }

    public class AdversarialTestCase
    {
        public string TestCaseId { get; set; } = Guid.NewGuid().ToString("N");
        public AdversarialVector Vector { get; set; }
        public string AdversarialPrompt { get; set; } = string.Empty;
        public string ForbiddenSubstring { get; set; } = string.Empty;
    }

    public class ModelEvaluationReport
    {
        public string ModelId { get; set; } = string.Empty;
        public Dictionary<AdversarialVector, double> VectorScores { get; set; } = new();
        public double OverallResilienceScore { get; set; }
        public bool IsApprovedForProduction => OverallResilienceScore >= 0.85;
        public List<string> FailedVectors { get; set; } = new();
        public DateTime EvaluatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public interface IAIEvaluationEngine
    {
        ModelEvaluationReport EvaluateModel(string modelId, List<AdversarialTestCase> testSuite);
    }

    public interface IModelTournament
    {
        IReadOnlyList<ModelEvaluationReport> RunTournament(List<string> candidateModelIds);
    }
}
