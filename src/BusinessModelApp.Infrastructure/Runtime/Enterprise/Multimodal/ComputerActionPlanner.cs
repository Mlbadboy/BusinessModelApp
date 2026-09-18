using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Multimodal;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Multimodal;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Multimodal
{
    public class ComputerActionPlanner : IComputerActionPlanner
    {
        private readonly IEnvironmentModel _environmentModel;
        private readonly IComputerSafetyGuard _safetyGuard;
        private readonly IActionRiskEvaluator _riskEvaluator;

        public ComputerActionPlanner(
            IEnvironmentModel environmentModel,
            IComputerSafetyGuard safetyGuard,
            IActionRiskEvaluator riskEvaluator)
        {
            _environmentModel = environmentModel ?? throw new ArgumentNullException(nameof(environmentModel));
            _safetyGuard = safetyGuard ?? throw new ArgumentNullException(nameof(safetyGuard));
            _riskEvaluator = riskEvaluator ?? throw new ArgumentNullException(nameof(riskEvaluator));
        }

        public async Task<ComputerActionProposal> FormulateProposalAsync(
            string tenantId,
            string sessionId,
            string snapshotId,
            string intentId,
            ProposedActionType actionType,
            string? targetElementId,
            (int X, int Y)? coordinates,
            Dictionary<string, string>? parameters = null)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId is required", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(sessionId)) throw new ArgumentException("SessionId is required", nameof(sessionId));

            var snapshot = await _environmentModel.GetSnapshotAsync(tenantId, snapshotId);
            if (snapshot == null)
            {
                throw new InvalidOperationException($"Environment snapshot {snapshotId} not found for tenant {tenantId}");
            }

            // Map ActionType to required Capability
            var capabilityId = MapActionTypeToCapability(actionType);
            if (!_safetyGuard.ValidateCapability(capabilityId))
            {
                throw new InvalidOperationException($"Action type {actionType} resolved to invalid or unregistered capability: {capabilityId}");
            }

            // Verify Target coordinate safety if target element specified
            if (!string.IsNullOrEmpty(targetElementId))
            {
                var isValidTarget = _safetyGuard.VerifyActionTarget(snapshot, targetElementId, coordinates);
                if (!isValidTarget)
                {
                    throw new InvalidOperationException($"ActionTargetMismatch: coordinates {coordinates} do not match target element {targetElementId}");
                }
            }

            // Evaluate Risk Tier
            var targetName = parameters != null && parameters.TryGetValue("TargetName", out var tn) ? tn : targetElementId;
            var command = parameters != null && parameters.TryGetValue("Command", out var cmd) ? cmd : null;
            var riskTier = _riskEvaluator.EvaluateRisk(actionType, targetName, command, snapshot.Url);

            var proposal = new ComputerActionProposal
            {
                TenantId = tenantId,
                SessionId = sessionId,
                EnvironmentSnapshotId = snapshotId,
                IntentId = intentId,
                ActionType = actionType,
                TargetElementId = targetElementId,
                TargetCoordinates = coordinates,
                Parameters = parameters ?? new Dictionary<string, string>(),
                RiskTier = riskTier,
                CapabilityId = capabilityId,
                ExpectedEffect = $"Perform {actionType} on {targetElementId ?? "screen"} to advance intent {intentId}",
                IsReversible = riskTier <= ActionRiskTier.R2_LocalModification,
                PolicySnapshotHash = snapshot.IntegrityHash
            };

            // Add standard preconditions
            proposal.Preconditions.Add(new ActionPrecondition("SnapshotIntegrity", "EnvironmentId", snapshot.EnvironmentId));
            proposal.Postconditions.Add(new ActionPostcondition("TargetEffectVerified", proposal.ExpectedEffect));

            proposal.ProposalHash = proposal.ComputeProposalHash();
            return proposal;
        }

        public async Task<ComputerActionProposal> ReplanActionAsync(string tenantId, string sessionId, string failedProposalId, string reason)
        {
            // Replanning creates a conservative observation step to resample state
            var snapshot = await _environmentModel.CreateSnapshotAsync(tenantId, sessionId, "Replanner", "RecoveryWindow");
            return await FormulateProposalAsync(
                tenantId,
                sessionId,
                snapshot.EnvironmentId,
                "Intent_Recovery_" + Guid.NewGuid().ToString("N").Substring(0, 6),
                ProposedActionType.VerifyState,
                null,
                null,
                new Dictionary<string, string> { { "RecoveryReason", reason } }
            );
        }

        private static string MapActionTypeToCapability(ProposedActionType actionType)
        {
            return actionType switch
            {
                ProposedActionType.NavigateUrl => ComputerCapabilities.BrowserNavigate,
                ProposedActionType.Click => ComputerCapabilities.BrowserClick,
                ProposedActionType.TypeText => ComputerCapabilities.BrowserType,
                ProposedActionType.SelectOption => ComputerCapabilities.BrowserSelect,
                ProposedActionType.UploadFile => ComputerCapabilities.BrowserUpload,
                ProposedActionType.DownloadFile => ComputerCapabilities.BrowserDownload,
                ProposedActionType.OpenApplication => ComputerCapabilities.DesktopOpenApplication,
                ProposedActionType.CloseWindow => ComputerCapabilities.DesktopClick,
                ProposedActionType.KeyCombination => ComputerCapabilities.KeyboardPress,
                ProposedActionType.ExtractData => ComputerCapabilities.ScreenCapture,
                ProposedActionType.VerifyState => ComputerCapabilities.ScreenCapture,
                ProposedActionType.Scroll => ComputerCapabilities.MouseMove,
                ProposedActionType.HumanInterventionRequested => ComputerCapabilities.ScreenCapture,
                _ => ComputerCapabilities.ScreenCapture
            };
        }
    }
}
