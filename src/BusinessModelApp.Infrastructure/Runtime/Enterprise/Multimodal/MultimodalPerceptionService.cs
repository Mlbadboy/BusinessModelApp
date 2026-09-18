using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Multimodal;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Multimodal;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Multimodal
{
    public class MultimodalPerceptionService :
        IComputerPerceptionService,
        IVisionProcessor,
        IOcrProcessor,
        IDocumentProcessor,
        IBrowserPerceptionProvider,
        IDesktopPerceptionProvider
    {
        private readonly IComputerSafetyGuard _safetyGuard;

        public MultimodalPerceptionService(IComputerSafetyGuard safetyGuard)
        {
            _safetyGuard = safetyGuard ?? throw new ArgumentNullException(nameof(safetyGuard));
        }

        public Task<ComputerEnvironmentSnapshot> ObserveEnvironmentAsync(string tenantId, string sessionId, string application, string window, string? url = null)
        {
            var snapshot = new ComputerEnvironmentSnapshot
            {
                TenantId = tenantId,
                SessionId = sessionId,
                Application = application,
                Window = window,
                Url = url ?? string.Empty,
                PageTitle = $"{application} - {window}",
                ObservedAtUtc = DateTime.UtcNow,
                ScreenshotHash = ComputerEnvironmentSnapshot.ComputeSha256($"{application}:{window}:screenshot"),
                DomHash = ComputerEnvironmentSnapshot.ComputeSha256($"{application}:{window}:dom")
            };

            // Sample simulated UI elements
            var btnSubmit = new ReconciledUiElement(
                ElementId: "btn-submit-01",
                ElementType: "Button",
                SemanticName: "Submit Order",
                BoundingBox: new BoundingBox(800, 600, 120, 40),
                DomSelector: "#submit-btn",
                DomId: "submit-btn",
                DomTag: "button",
                VisionConfidence: 0.98,
                DomEvidence: ElementEvidenceStatus.Verified,
                VisionEvidence: ElementEvidenceStatus.Verified,
                AccessibilityEvidence: ElementEvidenceStatus.Verified,
                IsSensitive: false,
                IsInteractive: true
            );

            var inputCard = new ReconciledUiElement(
                ElementId: "input-card-02",
                ElementType: "InputField",
                SemanticName: "Payment Card Number",
                BoundingBox: new BoundingBox(800, 520, 240, 35),
                DomSelector: "#card-number",
                DomId: "card-number",
                DomTag: "input",
                VisionConfidence: 0.95,
                DomEvidence: ElementEvidenceStatus.Verified,
                VisionEvidence: ElementEvidenceStatus.Verified,
                AccessibilityEvidence: ElementEvidenceStatus.Verified,
                IsSensitive: true,
                IsInteractive: true
            );

            snapshot.InteractiveElements.Add(btnSubmit);
            snapshot.InteractiveElements.Add(inputCard);
            snapshot.VisibleElements.Add(btnSubmit);
            snapshot.VisibleElements.Add(inputCard);

            if (inputCard.IsSensitive)
            {
                snapshot.SensitiveElements.Add(new SensitiveElement(inputCard.ElementId, "PaymentCard", "[REDACTED_CARD_FIELD]"));
            }

            snapshot.IntegrityHash = snapshot.ComputeIntegrityHash();
            return Task.FromResult(snapshot);
        }

        public Task<IReadOnlyList<ReconciledUiElement>> PerceiveScreenAsync(string tenantId, byte[] screenshotData, string? domContent = null)
        {
            var elements = new List<ReconciledUiElement>();

            // If prompt injection attempt exists in DOM, sanitize it
            if (!string.IsNullOrEmpty(domContent))
            {
                var injectionResult = _safetyGuard.InspectUntrustedInput(domContent);
                if (injectionResult.IsInjectionAttempt)
                {
                    domContent = injectionResult.SanitizedText;
                }
            }

            var sampleElement = new ReconciledUiElement(
                ElementId: "elem-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                ElementType: "Button",
                SemanticName: "Confirm Action",
                BoundingBox: new BoundingBox(100, 200, 80, 30),
                DomSelector: ".btn-confirm",
                DomId: "confirm-action",
                DomTag: "button",
                VisionConfidence: 0.92,
                DomEvidence: string.IsNullOrEmpty(domContent) ? ElementEvidenceStatus.None : ElementEvidenceStatus.Verified,
                VisionEvidence: ElementEvidenceStatus.Verified,
                AccessibilityEvidence: ElementEvidenceStatus.Verified,
                IsSensitive: false,
                IsInteractive: true
            );

            elements.Add(sampleElement);
            return Task.FromResult<IReadOnlyList<ReconciledUiElement>>(elements);
        }

        public Task<string> PerceiveDocumentAsync(string tenantId, string fileName, byte[] documentData, string mimeType)
        {
            var assessment = _safetyGuard.AssessFileSafety(fileName, documentData, mimeType);
            if (assessment.IsQuarantined)
            {
                return Task.FromResult($"[DOCUMENT_QUARANTINED: {assessment.QuarantinedReason}]");
            }

            var text = Encoding.UTF8.GetString(documentData ?? Array.Empty<byte>());
            var injectionCheck = _safetyGuard.InspectUntrustedInput(text);
            var sanitized = injectionCheck.IsInjectionAttempt ? injectionCheck.SanitizedText : text;
            var redaction = _safetyGuard.RedactSensitiveInfo(sanitized);

            return Task.FromResult(redaction.SanitizedText);
        }

        public Task<string> PerceiveAudioAsync(string tenantId, byte[] audioData, string codec)
        {
            // Transcribed audio representation
            var rawTranscript = "Simulated Audio Transcript: Please prepare the financial summary report.";
            var injection = _safetyGuard.InspectUntrustedInput(rawTranscript);
            var sanitized = injection.IsInjectionAttempt ? injection.SanitizedText : rawTranscript;

            // Invariant I38-N: Voice recognition provides intent only, not authorization
            return Task.FromResult(sanitized);
        }

        public Task<IReadOnlyList<ReconciledUiElement>> ProcessScreenshotAsync(string tenantId, byte[] screenshotData)
        {
            return PerceiveScreenAsync(tenantId, screenshotData, null);
        }

        public Task<IReadOnlyList<ReconciledUiElement>> ExtractTextAndBoxesAsync(string tenantId, byte[] imageData)
        {
            return PerceiveScreenAsync(tenantId, imageData, null);
        }

        public Task<FileSecurityAssessment> AssessAndExtractDocumentAsync(string tenantId, string fileName, byte[] fileBytes, string mimeType)
        {
            var assessment = _safetyGuard.AssessFileSafety(fileName, fileBytes, mimeType);
            return Task.FromResult(assessment);
        }

        public Task<ReconciledUiElement?> ReconcileElementAsync(string tenantId, string selector, string? expectedVisualLabel = null)
        {
            var element = new ReconciledUiElement(
                ElementId: "reconciled-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                ElementType: "Button",
                SemanticName: expectedVisualLabel ?? "Action Button",
                BoundingBox: new BoundingBox(500, 400, 100, 30),
                DomSelector: selector,
                DomId: selector.Replace("#", ""),
                DomTag: "button",
                VisionConfidence: 0.96,
                DomEvidence: ElementEvidenceStatus.Verified,
                VisionEvidence: ElementEvidenceStatus.Verified,
                AccessibilityEvidence: ElementEvidenceStatus.Verified,
                IsSensitive: false,
                IsInteractive: true
            );
            return Task.FromResult<ReconciledUiElement?>(element);
        }

        public Task<string> GetActiveWindowInfoAsync(string tenantId)
        {
            return Task.FromResult("Application: EnterpriseBrowser, WindowTitle: Customer Portal - Chrome");
        }
    }
}
