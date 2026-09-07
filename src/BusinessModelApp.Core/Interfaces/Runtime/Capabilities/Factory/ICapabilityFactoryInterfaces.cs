using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Domain.Runtime.Capabilities;
using BusinessModelApp.Core.Domain.Runtime.Capabilities.Factory;
using BusinessModelApp.Core.Domain.Runtime.Workers;

namespace BusinessModelApp.Core.Interfaces.Runtime.Capabilities.Factory
{
    public interface ICapabilityGapDetector
    {
        Task<List<CapabilityGap>> DetectGapsAsync(string tenantId, CancellationToken ct = default);
        Task<CapabilityGap> RecordGapAsync(CapabilityGap gap, CancellationToken ct = default);
    }

    public interface ICapabilitySpecifier
    {
        Task<CapabilitySpecification> CreateSpecificationAsync(CapabilityGap gap, CancellationToken ct = default);
        bool ValidateSpecificationAdmissibility(CapabilitySpecification spec, out string rejectionReason);
    }

    public interface ICapabilityDesigner
    {
        Task<CapabilityDesignArtifact> DesignCapabilityAsync(CapabilitySpecification spec, CancellationToken ct = default);
    }

    public interface ICapabilityGenerator
    {
        Task<CapabilityCodeArtifact> GenerateArtifactAsync(CapabilitySpecification spec, CapabilityDesignArtifact design, CancellationToken ct = default);
    }

    public interface IStaticSecurityScanner
    {
        Task<StaticSecurityScanResult> ScanArtifactAsync(CapabilityCodeArtifact artifact, CapabilitySpecification spec, CancellationToken ct = default);
    }

    public interface ICapabilitySandbox
    {
        Task<SandboxExecutionResult> ExecuteInSandboxAsync(CapabilityCodeArtifact artifact, CapabilitySpecification spec, string inputJson, CancellationToken ct = default);
    }

    public interface ICapabilityTddRunner
    {
        CapabilityTestSuite GenerateTestSuite(CapabilitySpecification spec);
        Task<TddEvaluationResult> RunTddSuiteAsync(CapabilityCodeArtifact artifact, CapabilityTestSuite suite, CancellationToken ct = default);
    }

    public interface ICapabilityEvaluator
    {
        Task<CapabilityEvaluationScorecard> EvaluateCapabilityAsync(CapabilityEvidenceBundle evidence, CancellationToken ct = default);
    }

    public interface IAdversarialRedTeamEngine
    {
        Task<StrixRedTeamResult> ProbeCapabilityAsync(CapabilityCodeArtifact artifact, CapabilitySpecification spec, CancellationToken ct = default);
    }

    /// <summary>
    /// Independent Certification Authority (ICA).
    /// Invariant I17-B: Certifier cannot certify its own output or permit generator self-certification.
    /// </summary>
    public interface IIndependentCertificationAuthority
    {
        string CertifierId { get; }
        Task<CapabilityCertificate> CertifyCapabilityAsync(CapabilityEvidenceBundle evidence, string requesterIdentity, CancellationToken ct = default);
        Task<bool> VerifyCertificateAsync(CapabilityCertificate cert, CapabilityCodeArtifact artifact, CancellationToken ct = default);
    }

    /// <summary>
    /// Cryptographic Release Signing Service using asymmetric keys (Ed25519).
    /// </summary>
    public interface ICapabilitySigningService
    {
        Task<CapabilityReleaseSignature> SignReleaseAsync(CapabilityCertificate cert, CapabilityCodeArtifact artifact, string parentVersionHash, CancellationToken ct = default);
        bool VerifyReleaseSignature(CapabilityReleaseSignature signature, string manifestHash);
    }

    /// <summary>
    /// Governed Lifecycle Registry managing state transitions across the 18-state lifecycle.
    /// </summary>
    public interface ICapabilityLifecycleRegistry
    {
        Task<RegisteredCapabilityRecord> RegisterCapabilityAsync(
            CapabilitySpecification spec,
            CapabilityCodeArtifact artifact,
            CapabilityCertificate cert,
            CapabilityReleaseSignature signature,
            CancellationToken ct = default);

        Task<RegisteredCapabilityRecord?> GetCapabilityAsync(CapabilityId capabilityId, string version, CancellationToken ct = default);
        Task<List<RegisteredCapabilityRecord>> GetAllCapabilitiesAsync(string tenantId, CancellationToken ct = default);
        Task<RegisteredCapabilityRecord> TransitionStateAsync(CapabilityId capabilityId, string version, CapabilityLifecycleState targetState, string reason, CancellationToken ct = default);
        Task QuarantineCapabilityAsync(CapabilityId capabilityId, string version, string reason, CancellationToken ct = default);
        Task RevokeCapabilityAsync(CapabilityId capabilityId, string version, string reason, CancellationToken ct = default);
    }

    public interface ICapabilityPromotionGate
    {
        Task<bool> CanPromoteToShadowAsync(RegisteredCapabilityRecord record, CancellationToken ct = default);
        Task<bool> CanPromoteToProbationAsync(RegisteredCapabilityRecord record, CancellationToken ct = default);
        Task<bool> CanPromoteToActiveAsync(RegisteredCapabilityRecord record, ProbationTracker tracker, CancellationToken ct = default);
    }

    /// <summary>
    /// Master Autonomous Capability Factory pipeline orchestrator.
    /// </summary>
    public interface IAutonomousCapabilityFactory
    {
        Task<RegisteredCapabilityRecord> SynthesizeCapabilityPipelineAsync(CapabilityGap gap, CancellationToken ct = default);
        Task<List<CapabilityGap>> GetPendingGapsAsync(string tenantId, CancellationToken ct = default);
        Task<RegisteredCapabilityRecord> PromoteCapabilityAsync(CapabilityId capabilityId, string version, CapabilityLifecycleState targetState, CancellationToken ct = default);
    }
}
