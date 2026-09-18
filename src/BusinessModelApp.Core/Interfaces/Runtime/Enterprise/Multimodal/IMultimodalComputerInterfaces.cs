using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Multimodal;

namespace BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Multimodal
{
    public interface IComputerPerceptionService
    {
        Task<ComputerEnvironmentSnapshot> ObserveEnvironmentAsync(string tenantId, string sessionId, string application, string window, string? url = null);
        Task<IReadOnlyList<ReconciledUiElement>> PerceiveScreenAsync(string tenantId, byte[] screenshotData, string? domContent = null);
        Task<string> PerceiveDocumentAsync(string tenantId, string fileName, byte[] documentData, string mimeType);
        Task<string> PerceiveAudioAsync(string tenantId, byte[] audioData, string codec);
    }

    public interface IVisionProcessor
    {
        Task<IReadOnlyList<ReconciledUiElement>> ProcessScreenshotAsync(string tenantId, byte[] screenshotData);
    }

    public interface IOcrProcessor
    {
        Task<IReadOnlyList<ReconciledUiElement>> ExtractTextAndBoxesAsync(string tenantId, byte[] imageData);
    }

    public interface IDocumentProcessor
    {
        Task<FileSecurityAssessment> AssessAndExtractDocumentAsync(string tenantId, string fileName, byte[] fileBytes, string mimeType);
    }

    public interface IBrowserPerceptionProvider
    {
        Task<ReconciledUiElement?> ReconcileElementAsync(string tenantId, string selector, string? expectedVisualLabel = null);
    }

    public interface IDesktopPerceptionProvider
    {
        Task<string> GetActiveWindowInfoAsync(string tenantId);
    }

    public interface IEnvironmentModel
    {
        Task<ComputerEnvironmentSnapshot> CreateSnapshotAsync(string tenantId, string sessionId, string application, string window, string? url = null);
        Task<ComputerEnvironmentSnapshot?> GetSnapshotAsync(string tenantId, string snapshotId);
        bool VerifyEnvironmentIntegrity(ComputerEnvironmentSnapshot snapshot);
    }

    public interface IComputerActionPlanner
    {
        Task<ComputerActionProposal> FormulateProposalAsync(string tenantId, string sessionId, string snapshotId, string intentId, ProposedActionType actionType, string? targetElementId, (int X, int Y)? coordinates, Dictionary<string, string>? parameters = null);
        Task<ComputerActionProposal> ReplanActionAsync(string tenantId, string sessionId, string failedProposalId, string reason);
    }

    public interface IActionRiskEvaluator
    {
        ActionRiskTier EvaluateRisk(ProposedActionType actionType, string? targetElementRole, string? commandName, string? url = null);
        bool RequiresHumanApproval(ActionRiskTier tier);
    }

    public interface IComputerSafetyGuard
    {
        bool ValidateCapability(string capabilityId);
        InjectionDetectionResult InspectUntrustedInput(string text);
        RedactedContentResult RedactSensitiveInfo(string text);
        bool VerifyActionTarget(ComputerEnvironmentSnapshot snapshot, string targetElementId, (int X, int Y)? targetCoordinates);
        FileSecurityAssessment AssessFileSafety(string fileName, byte[] bytes, string mimeType);
    }

    public interface IComputerSessionManager
    {
        Task<ComputerSession> CreateSessionAsync(string tenantId, string applicationContext, string targetGoal);
        Task<ComputerSession?> GetSessionAsync(string tenantId, string sessionId);
        Task<IReadOnlyList<ComputerSession>> ListSessionsAsync(string tenantId, int limit = 50);
        Task<ComputerSession> TransitionSessionAsync(string tenantId, string sessionId, ComputerSessionState newState, string reason);
        Task<ComputerSession> PauseSessionAsync(string tenantId, string sessionId, string humanReason);
        Task<ComputerSession> ResumeSessionAsync(string tenantId, string sessionId, string supervisorId);
        Task<ComputerSession> EmergencyKillSessionAsync(string tenantId, string sessionId, string reason);
    }

    public interface IComputerVerificationService
    {
        bool VerifyPreconditions(ComputerActionProposal proposal, IReadOnlyDictionary<string, string> currentState);
        Task<ComputerActionResult> VerifyPostconditionsAsync(ComputerActionProposal proposal, ComputerEnvironmentSnapshot beforeSnapshot, ComputerEnvironmentSnapshot afterSnapshot);
        Task<ComputerActionResult> ReconcileUnknownEffectAsync(string tenantId, string sessionId, string attemptId, string expectedTarget);
    }

    public interface IComputerProvenanceService
    {
        Task<ComputerTraceRecord> RecordTraceAsync(string tenantId, string sessionId, string proposalId, string attemptId, string externalEffect, string outcomeId);
        Task<ComputerTraceRecord?> GetTraceAsync(string tenantId, string traceId);
        Task<ReplaySessionDescriptor> CreateReplaySessionAsync(string tenantId, string originalSessionId);
    }

    public interface IComputerOrchestratorService
    {
        Task<ComputerSession> StartSessionAsync(string tenantId, string application, string targetGoal);
        Task<ComputerEnvironmentSnapshot> ObserveAsync(string tenantId, string sessionId, string application, string window, string? url = null);
        Task<ComputerActionProposal> ProposeActionAsync(string tenantId, string sessionId, string snapshotId, string intentId, ProposedActionType actionType, string? targetElementId, (int X, int Y)? coordinates, Dictionary<string, string>? parameters = null);
        Task<ComputerActionResult> AdmitAndDispatchProposalAsync(string tenantId, string sessionId, string proposalId);
        Task<ComputerSession> PauseAsync(string tenantId, string sessionId, string reason);
        Task<ComputerSession> ResumeAsync(string tenantId, string sessionId, string supervisorId);
        Task<ComputerSession> EmergencyKillAsync(string tenantId, string sessionId, string reason);
    }
}
