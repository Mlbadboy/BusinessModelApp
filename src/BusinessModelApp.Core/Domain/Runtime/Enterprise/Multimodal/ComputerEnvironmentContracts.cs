using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Multimodal
{
    public record BoundingBox(int X, int Y, int Width, int Height)
    {
        public bool Contains(int targetX, int targetY)
        {
            return targetX >= X && targetX <= (X + Width) && targetY >= Y && targetY <= (Y + Height);
        }
    }

    public enum ElementEvidenceStatus
    {
        None = 0,
        Unverified = 1,
        Partial = 2,
        Verified = 3,
        Mismatch = 4
    }

    public record ReconciledUiElement(
        string ElementId,
        string ElementType,
        string SemanticName,
        BoundingBox BoundingBox,
        string? DomSelector,
        string? DomId,
        string? DomTag,
        double VisionConfidence,
        ElementEvidenceStatus DomEvidence,
        ElementEvidenceStatus VisionEvidence,
        ElementEvidenceStatus AccessibilityEvidence,
        bool IsSensitive,
        bool IsInteractive
    )
    {
        public bool IsFullyVerified =>
            DomEvidence == ElementEvidenceStatus.Verified &&
            VisionEvidence == ElementEvidenceStatus.Verified;
    }

    public record FormElement(string FormId, string ActionUrl, IReadOnlyList<string> FieldNames);
    public record DialogElement(string DialogId, string Title, string Message, IReadOnlyList<string> ButtonLabels);
    public record AlertElement(string AlertId, string Severity, string Message);
    public record SensitiveElement(string ElementId, string Category, string RedactedPlaceholder);

    public class ComputerEnvironmentSnapshot
    {
        public string EnvironmentId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public DateTime ObservedAtUtc { get; set; } = DateTime.UtcNow;
        public string Application { get; set; } = string.Empty;
        public string Window { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string PageTitle { get; set; } = string.Empty;
        public (int Width, int Height) ScreenDimensions { get; set; } = (1920, 1080);
        public List<ReconciledUiElement> VisibleElements { get; set; } = new();
        public List<ReconciledUiElement> InteractiveElements { get; set; } = new();
        public List<FormElement> Forms { get; set; } = new();
        public List<DialogElement> Dialogs { get; set; } = new();
        public List<AlertElement> Alerts { get; set; } = new();
        public string DownloadState { get; set; } = "Idle";
        public string AuthenticationState { get; set; } = "Anonymous";
        public List<SensitiveElement> SensitiveElements { get; set; } = new();
        public string ScreenshotHash { get; set; } = string.Empty;
        public string DomHash { get; set; } = string.Empty;
        public string IntegrityHash { get; set; } = string.Empty;

        public static string ComputeSha256(string content)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content ?? string.Empty));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }

        public string ComputeIntegrityHash()
        {
            var raw = $"{TenantId}:{SessionId}:{Application}:{Window}:{Url}:{PageTitle}:{ScreenshotHash}:{DomHash}:{VisibleElements.Count}:{InteractiveElements.Count}";
            return ComputeSha256(raw);
        }
    }
}
