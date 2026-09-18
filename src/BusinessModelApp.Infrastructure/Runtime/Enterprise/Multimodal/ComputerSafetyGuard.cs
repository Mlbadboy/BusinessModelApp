using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Multimodal;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Multimodal;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Multimodal
{
    public class ComputerSafetyGuard : IComputerSafetyGuard, IActionRiskEvaluator
    {
        private static readonly HashSet<string> DisallowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/x-msdownload", "application/x-sh", "application/x-bat", "application/x-executable",
            "application/vnd.microsoft.portable-executable", "application/x-dosexec"
        };

        public bool ValidateCapability(string capabilityId)
        {
            return ComputerCapabilities.IsValidCapability(capabilityId);
        }

        public InjectionDetectionResult InspectUntrustedInput(string text)
        {
            return ComputerSafetyPatterns.AnalyzePromptInjection(text);
        }

        public RedactedContentResult RedactSensitiveInfo(string text)
        {
            return ComputerSafetyPatterns.RedactSensitiveContent(text);
        }

        public bool VerifyActionTarget(ComputerEnvironmentSnapshot snapshot, string targetElementId, (int X, int Y)? targetCoordinates)
        {
            if (snapshot == null) return false;

            // Find element in interactive or visible elements
            var element = snapshot.InteractiveElements.FirstOrDefault(e => e.ElementId == targetElementId)
                          ?? snapshot.VisibleElements.FirstOrDefault(e => e.ElementId == targetElementId);

            if (element == null) return false;

            // If coordinates were specified, verify they fall within the bounding box
            if (targetCoordinates.HasValue)
            {
                var (x, y) = targetCoordinates.Value;
                if (!element.BoundingBox.Contains(x, y))
                {
                    return false; // Coordinate target mismatch!
                }
            }

            return true;
        }

        public FileSecurityAssessment AssessFileSafety(string fileName, byte[] bytes, string mimeType)
        {
            var assessment = new FileSecurityAssessment
            {
                FileName = fileName ?? "unnamed_file",
                ByteLength = bytes?.Length ?? 0,
                MimeType = mimeType ?? "application/octet-stream"
            };

            if (bytes != null && bytes.Length > 0)
            {
                using var sha = SHA256.Create();
                assessment.Sha256Hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            }

            // Check size limit: 50MB
            if (assessment.ByteLength > 50 * 1024 * 1024)
            {
                assessment.SafetyStatus = FileSafetyStatus.ExceededSizeLimit;
                assessment.QuarantinedReason = "File size exceeds 50MB safety limit";
                return assessment;
            }

            // Check MIME safety
            if ((!string.IsNullOrEmpty(mimeType) && DisallowedMimeTypes.Contains(mimeType)) || 
                (!string.IsNullOrEmpty(fileName) && (fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".sh", StringComparison.OrdinalIgnoreCase))))
            {
                assessment.SafetyStatus = FileSafetyStatus.RejectedUnsafeMime;
                assessment.QuarantinedReason = "Disallowed executable or script format";
                return assessment;
            }

            assessment.SafetyStatus = FileSafetyStatus.Clean;
            return assessment;
        }

        public ActionRiskTier EvaluateRisk(ProposedActionType actionType, string? targetElementRole, string? commandName, string? url = null)
        {
            // High-consequence financial / legal / destructive operations -> R5
            if (!string.IsNullOrEmpty(commandName))
            {
                var lower = commandName.ToLowerInvariant();
                if (lower.Contains("pay") || lower.Contains("transfer") || lower.Contains("delete") || lower.Contains("drop") || lower.Contains("terminate") || lower.Contains("checkout"))
                {
                    return ActionRiskTier.R5_HighConsequence;
                }
            }

            if (!string.IsNullOrEmpty(targetElementRole))
            {
                var lower = targetElementRole.ToLowerInvariant();
                if (lower.Contains("pay") || lower.Contains("transfer") || lower.Contains("purchase") || lower.Contains("confirm payment"))
                {
                    return ActionRiskTier.R5_HighConsequence;
                }
            }

            // Commercial / consequential -> R4
            if (!string.IsNullOrEmpty(commandName))
            {
                var lower = commandName.ToLowerInvariant();
                if (lower.Contains("order") || lower.Contains("sign contract") || lower.Contains("publish") || lower.Contains("invoice"))
                {
                    return ActionRiskTier.R4_CommercialConsequential;
                }
            }

            return actionType switch
            {
                ProposedActionType.ExtractData => ActionRiskTier.R0_Observation,
                ProposedActionType.VerifyState => ActionRiskTier.R0_Observation,
                ProposedActionType.Scroll => ActionRiskTier.R1_ReversibleLocal,
                ProposedActionType.NavigateUrl => ActionRiskTier.R1_ReversibleLocal,
                ProposedActionType.SelectOption => ActionRiskTier.R1_ReversibleLocal,
                ProposedActionType.OpenApplication => ActionRiskTier.R1_ReversibleLocal,
                ProposedActionType.CloseWindow => ActionRiskTier.R1_ReversibleLocal,
                ProposedActionType.TypeText => ActionRiskTier.R2_LocalModification,
                ProposedActionType.UploadFile => ActionRiskTier.R3_ExternalCommunication,
                ProposedActionType.DownloadFile => ActionRiskTier.R2_LocalModification,
                ProposedActionType.Click => ActionRiskTier.R2_LocalModification,
                ProposedActionType.KeyCombination => ActionRiskTier.R2_LocalModification,
                ProposedActionType.HumanInterventionRequested => ActionRiskTier.R0_Observation,
                _ => ActionRiskTier.R3_ExternalCommunication
            };
        }

        public bool RequiresHumanApproval(ActionRiskTier tier)
        {
            return tier >= ActionRiskTier.R4_CommercialConsequential;
        }
    }
}
