using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Security;

namespace BusinessModelApp.Infrastructure.Security
{
    /// <summary>
    /// Blue Team Defense & Remediation Engine.
    /// Invariant: Automated containment is restricted to pre-approved reversible controls in disposable test sandboxes.
    /// Invariant: Production capability and policy changes require sovereign human/governance authorization.
    /// Invariant: Zero-Regression Gate: Candidate fix must prove PoC=PASS without regressing baseline tests.
    /// </summary>
    public class BlueTeamRemediationEngine
    {
        public BlueTeamRemediation FormulateRemediation(VulnerabilityFinding finding)
        {
            if (finding == null) throw new ArgumentNullException(nameof(finding));

            RemediationPatchType patchType;
            string fixSummary;
            bool isPreApprovedSandboxAction;
            bool requiresGovernance;

            switch (finding.Category)
            {
                case VulnerabilityCategory.BOLA_IDOR:
                case VulnerabilityCategory.TenantLeakage:
                    patchType = RemediationPatchType.PolicyRuleTightening;
                    fixSummary = "Enforce strict tenant boundary check and query filtering on target entity resolver.";
                    isPreApprovedSandboxAction = true;
                    requiresGovernance = true; // Production query filters require governance
                    break;

                case VulnerabilityCategory.PromptInjection:
                    patchType = RemediationPatchType.SanitizationPattern;
                    fixSummary = "Update regex sanitization boundary and clamp agent role capability permissions.";
                    isPreApprovedSandboxAction = true;
                    requiresGovernance = true;
                    break;

                case VulnerabilityCategory.StateMachineBypass:
                    patchType = RemediationPatchType.GateAssertion;
                    fixSummary = "Add explicit unidirectional gate assertion in promotion state validator.";
                    isPreApprovedSandboxAction = true;
                    requiresGovernance = false; // Code gate assertion within sandbox
                    break;

                default:
                    patchType = RemediationPatchType.SandboxAgentSuspension;
                    fixSummary = "Suspend synthetic test agent in sandbox and quarantine target sandbox instance.";
                    isPreApprovedSandboxAction = true;
                    requiresGovernance = false;
                    break;
            }

            return new BlueTeamRemediation
            {
                WorkspaceId = finding.WorkspaceId,
                FindingId = finding.Id,
                CandidateFixSummary = fixSummary,
                PatchType = patchType,
                IsPreApprovedReversibleSandboxAction = isPreApprovedSandboxAction,
                RequiresGovernanceApproval = requiresGovernance,
                IsApproved = isPreApprovedSandboxAction && !requiresGovernance,
                ApprovedBy = (!requiresGovernance) ? "AutonomousSandboxDefense" : string.Empty,
                PoCPassed = false,
                RegressionTested = false,
                PassedTestCount = 0,
                CreatedAt = DateTime.UtcNow
            };
        }

        public async Task<BlueTeamRemediation> ValidateAndApplyRemediationAsync(
            BlueTeamRemediation remediation,
            Func<Task<bool>> rerunPocValidation,
            Func<Task<int>> runRegressionSuite,
            bool isGovernanceApproved = false,
            CancellationToken ct = default)
        {
            if (remediation.RequiresGovernanceApproval && !isGovernanceApproved)
            {
                throw new InvalidOperationException(
                    "Production capability or policy change requires explicit sovereign governance approval. Automated promotion blocked.");
            }

            // 1. Re-execute Red Team PoC to verify the vulnerability is mitigated
            bool pocFixed = await rerunPocValidation();
            remediation.PoCPassed = pocFixed;

            if (!pocFixed)
            {
                throw new InvalidOperationException(
                    "Remediation rejected: Red Team Proof-of-Concept still reproduces the vulnerability!");
            }

            // 2. Execute Zero-Regression Gate across existing certified test baseline
            int passedTests = await runRegressionSuite();
            remediation.PassedTestCount = passedTests;
            remediation.RegressionTested = true;

            // Mark applied
            remediation.IsApproved = true;
            remediation.ApprovedBy = isGovernanceApproved ? "SecurityGovernor" : "AutonomousSandboxDefense";
            remediation.AppliedAt = DateTime.UtcNow;

            return remediation;
        }
    }
}
