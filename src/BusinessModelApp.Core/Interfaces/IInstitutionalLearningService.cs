using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Learning;

namespace BusinessModelApp.Core.Interfaces
{
    public class LearningExplanation
    {
        public Guid LearningId { get; set; }
        public string Statement { get; set; } = string.Empty;
        public string Classification { get; set; } = "LEARNING";
        public double Confidence { get; set; }
        public double CausalConfidence { get; set; }
        public int ValidationCount { get; set; }
        public int ContradictionCount { get; set; }
        public string Freshness { get; set; } = "VERIFIED";
        public string Tier { get; set; } = "L1_Mission";
        public string ApplicabilityScope { get; set; } = string.Empty;
        public List<Guid> SupportingEvidenceIds { get; set; } = new();
        public List<Guid> SupportingMissionIds { get; set; } = new();
        public List<string> ContradictingNotes { get; set; } = new();
        public string Rationale { get; set; } = string.Empty;
    }

    public class ContextualLearningPromptItem
    {
        public Guid LearningId { get; set; }
        public string AdvisoryHeader { get; set; } = string.Empty;
        public string Statement { get; set; } = string.Empty;
        public string Classification { get; set; } = "LEARNING";
        public double CausalConfidence { get; set; }
        public int ValidationCount { get; set; }
        public int ContradictionCount { get; set; }
        public string Applicability { get; set; } = string.Empty;
        public string Freshness { get; set; } = "ACTIVE";
        public string AdvisoryWarning { get; set; } = "NOTICE: This information is advisory institutional learning and MUST NOT redefine business truth or revenue facts.";
    }

    public interface IInstitutionalLearningService
    {
        Task<OutcomeRecord> RecordMissionOutcomeAsync(Guid workspaceId, OutcomeRecord outcome, CancellationToken ct = default);
        Task<FailureRecord> DiagnoseFailureAsync(Guid workspaceId, Guid missionId, FailureRootCause rootCause, string diagnosis, string impact, CancellationToken ct = default);
        Task<CorrectionRecord> RecordCorrectionAsync(Guid workspaceId, Guid failureRecordId, string actionTaken, bool wasSuccessful, string outcomeSummary, CancellationToken ct = default);
        Task<LearningRecord> GenerateLearningCandidateAsync(Guid workspaceId, Guid missionId, string statement, string context, string domain, double confidence, double causalConfidence, CancellationToken ct = default);
        Task<LearningRecord> ValidateAndPromoteLearningAsync(Guid workspaceId, Guid learningRecordId, bool isDirectAiCall = false, CancellationToken ct = default);
        Task<IReadOnlyList<LearningContradictionRecord>> DetectContradictionsAsync(Guid workspaceId, CancellationToken ct = default);
        Task<IReadOnlyList<ContextualLearningPromptItem>> RetrieveContextualLearningAsync(Guid workspaceId, string domain, string query, CancellationToken ct = default);
        Task<LearningRecord?> GetLearningRecordAsync(Guid workspaceId, Guid id, CancellationToken ct = default);
        Task<IReadOnlyList<LearningRecord>> GetActiveLearningAsync(Guid workspaceId, LearningTier? tier = null, CancellationToken ct = default);
        Task<IReadOnlyList<FailureRecord>> GetFailingAssumptionsAsync(Guid workspaceId, CancellationToken ct = default);
        Task<LearningExperiment> ProposeExperimentAsync(Guid workspaceId, LearningExperiment experiment, CancellationToken ct = default);
        Task<LearningExperiment> RecordExperimentResultAsync(Guid workspaceId, Guid experimentId, decimal actualValue, string conclusion, CancellationToken ct = default);
        Task<LearningExplanation> ExplainLearningAsync(Guid workspaceId, Guid learningRecordId, CancellationToken ct = default);
    }
}
