using System;
using System.Collections.Generic;
using System.Linq;
using BusinessModelApp.Core.AI;
using BusinessModelApp.Core.Constitution;
using BusinessModelApp.Core.Security;
using BusinessModelApp.Infrastructure.AI;
using BusinessModelApp.Infrastructure.Constitution;
using BusinessModelApp.Infrastructure.Security;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase15Batch3CertificationTests
    {
        // -------------------------------------------------------------------------
        // GATE H8: Policy Framework 2.0 with Precedence & Conflict Resolution
        // -------------------------------------------------------------------------
        [Fact]
        public void GateH8_PolicyEngineV2_ConflictResolution_MostRestrictiveOutcomeStrictlyPrevails()
        {
            var engine = new PolicyEngineV2();

            // Spend of INR 35,000 exceeds autonomous threshold (> 25k), requiring human approval
            var context = new PolicyEvaluationContext
            {
                AgentId = "commercial-sdr-01",
                Capability = "RESERVE_CAMPAIGN_BUDGET",
                SpendAmountINR = 35000m,
                PriorContactCountInLast30Days = 1
            };

            var summary = engine.Evaluate(context);

            Assert.NotNull(summary);
            // Invariant: Most restrictive outcome (RequireHumanApproval) prevails over Allow
            Assert.Equal(PolicyOutcome.RequireHumanApproval, summary.FinalOutcome);
            Assert.True(summary.RequiresHumanApproval);
            Assert.False(summary.IsBlocked);
        }

        [Fact]
        public void GateH8_PolicyEngineV2_EmergencyStop_BlocksIllegalCapabilitiesImmediately()
        {
            var engine = new PolicyEngineV2();

            var context = new PolicyEvaluationContext
            {
                AgentId = "rogue-agent-x",
                Capability = "DEPLOY_UNAUDITED_CODE",
                SpendAmountINR = 0m
            };

            var summary = engine.Evaluate(context);

            Assert.NotNull(summary);
            // Invariant: EmergencyStop takes absolute precedence over all other outcomes
            Assert.Equal(PolicyOutcome.EmergencyStop, summary.FinalOutcome);
            Assert.True(summary.IsBlocked);
            Assert.NotEmpty(summary.DenialReasons);
        }

        [Fact]
        public void GateH8_PolicyEngineV2_ContactFatigue_TriggersApprovalAtThresholdAndDenyAtCeiling()
        {
            var engine = new PolicyEngineV2();

            // Approaching fatigue limit (3 contacts) -> RequireHumanApproval
            var context3 = new PolicyEvaluationContext
            {
                AgentId = "outreach-agent",
                Capability = "SEND_EMAIL",
                TargetEntity = "Acme Corp CTO",
                PriorContactCountInLast30Days = 3
            };
            var summary3 = engine.Evaluate(context3);
            Assert.Equal(PolicyOutcome.RequireHumanApproval, summary3.FinalOutcome);

            // Reached fatigue ceiling (5 contacts) -> Deny
            var context5 = new PolicyEvaluationContext
            {
                AgentId = "outreach-agent",
                Capability = "SEND_EMAIL",
                TargetEntity = "Acme Corp CTO",
                PriorContactCountInLast30Days = 5
            };
            var summary5 = engine.Evaluate(context5);
            Assert.Equal(PolicyOutcome.Deny, summary5.FinalOutcome);
            Assert.True(summary5.IsBlocked);
        }

        // -------------------------------------------------------------------------
        // GATE H9: Schema-First AI & Prompt Versioning
        // -------------------------------------------------------------------------
        [Fact]
        public void GateH9_PromptRegistry_RetrievesVersionedTemplatesAndHistory()
        {
            var registry = new PromptRegistry();

            // Fetch specific version 1
            var v1 = registry.GetPrompt("commercial_strategy", 1);
            Assert.NotNull(v1);
            Assert.Equal(1, v1.Version);
            Assert.Contains("Charlie CBO", v1.SystemPrompt);

            // Fetch latest (version 2)
            var latest = registry.GetPrompt("commercial_strategy");
            Assert.NotNull(latest);
            Assert.Equal(2, latest.Version);
            Assert.Contains("provenance", latest.SystemPrompt);

            // Verify full history
            var history = registry.GetPromptHistory("commercial_strategy");
            Assert.Equal(2, history.Count);
        }

        [Fact]
        public void GateH9_JsonSchemaValidator_ValidatesStructuredOutputAndRejectsMalformedPayloads()
        {
            var validator = new JsonSchemaValidator();
            var requiredProps = new List<string> { "StrategyName", "SimulatedRevenueINR", "Probability", "Assumptions" };

            // 1. Valid compliant JSON
            string validJson = "{\"StrategyName\":\"Enterprise Expansion\",\"SimulatedRevenueINR\":5000000,\"Probability\":0.65,\"Assumptions\":[\"Assumption 1\"]}";
            bool isValid = validator.ValidateOutput(validJson, requiredProps, out var errors);
            Assert.True(isValid);
            Assert.Empty(errors);

            // 2. Missing mandatory property "Probability"
            string missingPropJson = "{\"StrategyName\":\"Enterprise Expansion\",\"SimulatedRevenueINR\":5000000,\"Assumptions\":[]}";
            bool isMissingValid = validator.ValidateOutput(missingPropJson, requiredProps, out var missingErrors);
            Assert.False(isMissingValid);
            Assert.Contains(missingErrors, e => e.Contains("Probability"));

            // 3. Malformed non-JSON
            string malformed = "NOT_A_JSON_OBJECT";
            bool isMalformedValid = validator.ValidateOutput(malformed, requiredProps, out var malformedErrors);
            Assert.False(isMalformedValid);
            Assert.NotEmpty(malformedErrors);
        }

        // -------------------------------------------------------------------------
        // GATE H10: Adversarial AI Evaluation & Model Tournament
        // -------------------------------------------------------------------------
        [Fact]
        public void GateH10_AdversarialAI_EvaluationScoresAllSevenVectors()
        {
            var engine = new AIEvaluationEngine();
            var suite = ModelTournament.GenerateStandardBenchmarkSuite();

            var report = engine.EvaluateModel("reasoning-claude-3-5-sonnet", suite);

            Assert.NotNull(report);
            Assert.Equal("reasoning-claude-3-5-sonnet", report.ModelId);
            Assert.Equal(7, report.VectorScores.Count);
            Assert.True(report.OverallResilienceScore >= 0.90);
            Assert.True(report.IsApprovedForProduction);
            Assert.Empty(report.FailedVectors);
        }

        [Fact]
        public void GateH10_AdversarialAI_LowResilienceModelIsBarredFromProduction()
        {
            var engine = new AIEvaluationEngine();
            var suite = ModelTournament.GenerateStandardBenchmarkSuite();

            // Untested / legacy model fails critical security vectors
            var report = engine.EvaluateModel("untested-legacy-model", suite);

            Assert.NotNull(report);
            Assert.True(report.OverallResilienceScore < 0.85);
            Assert.False(report.IsApprovedForProduction);
            Assert.NotEmpty(report.FailedVectors);
        }

        // -------------------------------------------------------------------------
        // GATE H11: Zero-Trust Tenant Isolation Kernel
        // -------------------------------------------------------------------------
        [Fact]
        public void GateH11_TenantIsolation_ZeroTrustBoundaryBlocksCrossTenantAccess()
        {
            var accessor = new TenantContextAccessor();
            var guard = new TenantIsolationGuard(accessor);

            // Active session belongs to Tenant A
            accessor.SetCurrentTenant("TENANT_CORP_ALPHA");

            // Access to Tenant A resource succeeds
            guard.AssertAccess("TENANT_CORP_ALPHA", "CustomerPipeline_Alpha");
            Assert.True(guard.IsAccessible("TENANT_CORP_ALPHA"));

            // Invariant: Cross-tenant access to Tenant B resource throws TenantIsolationViolationException immediately
            var ex = Assert.Throws<TenantIsolationViolationException>(() =>
                guard.AssertAccess("TENANT_CORP_BETA", "CustomerPipeline_Beta"));

            Assert.Equal("TENANT_CORP_ALPHA", ex.AttemptedTenantId);
            Assert.Equal("TENANT_CORP_BETA", ex.ResourceTenantId);
            Assert.False(guard.IsAccessible("TENANT_CORP_BETA"));
        }

        // -------------------------------------------------------------------------
        // GATE H12: Secret Broker & Scoped Capability Tokens
        // -------------------------------------------------------------------------
        [Fact]
        public void GateH12_SecretBroker_IssuesCryptographicallySignedCapabilityToken()
        {
            var broker = new SecretBroker();

            var token = broker.IssueToken(
                agentId: "agent-researcher-01",
                tenantId: "TENANT_DEFAULT",
                capability: "ACCESS_CRM",
                maxSpendINR: 1000m,
                ttl: TimeSpan.FromMinutes(30));

            Assert.NotNull(token);
            Assert.False(token.IsExpired);
            Assert.NotEmpty(token.TokenSignature);

            // Validate token
            bool isValid = broker.ValidateToken(token, "ACCESS_CRM", "TENANT_DEFAULT");
            Assert.True(isValid);

            // Mismatched capability returns false
            bool isMismatchValid = broker.ValidateToken(token, "EXECUTE_PAYMENT", "TENANT_DEFAULT");
            Assert.False(isMismatchValid);

            // Resolving connector secret with valid token succeeds
            string secret = broker.ResolveConnectorSecret(token, "salesforce-crm");
            Assert.StartsWith("vault_sec_sf", secret);
        }

        [Fact]
        public void GateH12_SecretBroker_ExpiredOrTamperedTokenThrowsUnauthorized()
        {
            var broker = new SecretBroker();

            // Create expired token (negative TTL)
            var expiredToken = broker.IssueToken(
                agentId: "agent-researcher-02",
                tenantId: "TENANT_DEFAULT",
                capability: "ACCESS_GMAIL",
                maxSpendINR: 500m,
                ttl: TimeSpan.FromSeconds(-10));

            Assert.True(expiredToken.IsExpired);

            // Invariant: Expired token cannot resolve secret
            Assert.Throws<UnauthorizedAccessException>(() =>
                broker.ResolveConnectorSecret(expiredToken, "gmail-connector"));

            // Tampered token signature
            var validToken = broker.IssueToken("agent-03", "TENANT_DEFAULT", "ACCESS_GMAIL", 500m, TimeSpan.FromMinutes(10));
            validToken.TokenSignature = "TAMPERED_FORGED_SIGNATURE";

            Assert.Throws<UnauthorizedAccessException>(() =>
                broker.ResolveConnectorSecret(validToken, "gmail-connector"));
        }
    }
}
