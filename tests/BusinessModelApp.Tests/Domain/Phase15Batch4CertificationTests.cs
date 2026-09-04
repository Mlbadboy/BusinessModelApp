using System;
using System.Collections.Generic;
using System.Linq;
using BusinessModelApp.Core.Agents;
using BusinessModelApp.Core.Connectors;
using BusinessModelApp.Core.Constitution;
using BusinessModelApp.Core.Observability;
using BusinessModelApp.Infrastructure.Agents;
using BusinessModelApp.Infrastructure.Connectors;
using BusinessModelApp.Infrastructure.Constitution;
using BusinessModelApp.Infrastructure.Observability;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase15Batch4CertificationTests
    {
        // -------------------------------------------------------------------------
        // GATE H13: Dynamic Connector Capability Registry & Graceful Degradation
        // -------------------------------------------------------------------------
        [Fact]
        public void GateH13_CapabilityRegistry_ResolvesHealthyConnector_WithConsentAndCapability()
        {
            var registry = new CapabilityRegistryService();

            var hubspot = new ConnectorHealthProfile
            {
                ConnectorId = "hubspot-crm-01",
                Name = "Hubspot Production CRM",
                Status = ConnectorStatus.Healthy,
                IsAuthenticated = true,
                RemainingQuota = 15000,
                LatencyMs = 180,
                ErrorRate = 0.005,
                FreshnessScore = 0.99,
                SupportedCapabilities = new List<string> { "FETCH_LEADS", "WRITE_CONTACTS" },
                AllowedTenants = new List<string> { "*" }
            };

            registry.RegisterConnector(hubspot);

            var resolution = registry.ResolveCapability("FETCH_LEADS", "tenant-test");

            Assert.True(resolution.IsAvailable);
            Assert.Equal("hubspot-crm-01", resolution.SelectedConnectorId);
            Assert.Equal("Hubspot Production CRM", resolution.ConnectorName);
            Assert.Equal(180, resolution.EstimatedLatencyMs);
        }

        [Fact]
        public void GateH13_CapabilityRegistry_GracefulDegradation_FiltersOutDegradedOrUnauthenticated()
        {
            var registry = new CapabilityRegistryService();

            // Disconnected CRM
            var salesforce = new ConnectorHealthProfile
            {
                ConnectorId = "salesforce-crm-broken",
                Name = "Salesforce Disconnected",
                Status = ConnectorStatus.Disconnected,
                IsAuthenticated = false,
                SupportedCapabilities = new List<string> { "CUSTOM_ERP_SYNC" },
                AllowedTenants = new List<string> { "*" }
            };

            // CRM with zero remaining quota
            var pipedrive = new ConnectorHealthProfile
            {
                ConnectorId = "pipedrive-depleted",
                Name = "Pipedrive Zero Quota",
                Status = ConnectorStatus.Healthy,
                IsAuthenticated = true,
                RemainingQuota = 0,
                SupportedCapabilities = new List<string> { "CUSTOM_ERP_SYNC" },
                AllowedTenants = new List<string> { "*" }
            };

            registry.RegisterConnector(salesforce);
            registry.RegisterConnector(pipedrive);

            var resolution = registry.ResolveCapability("CUSTOM_ERP_SYNC", "tenant-test");

            // Invariant: Unauthenticated, disconnected, or quota-depleted connectors must never be resolved for execution
            Assert.False(resolution.IsAvailable);
            Assert.Contains("No healthy, authenticated connector available", resolution.ResolutionReason);
        }

        // -------------------------------------------------------------------------
        // GATE H14: Causal Tracing & Distributed Correlation
        // -------------------------------------------------------------------------
        [Fact]
        public void GateH14_CausalTracer_ConstructsCausalSpans_AndAggregatesMetrics()
        {
            var tracer = new CausalTracer();
            var missionId = Guid.NewGuid();

            var rootSpan = tracer.StartSpan(missionId, "charlie-ceo-agent", "REVERSE_FUNNEL_ANALYSIS", "Identify pipeline gap for Q3");
            tracer.EndSpan(rootSpan.SpanId, "mock-gemini-pro", "Analyzed pipeline: INR 45L gap identified", 0.05m, 120);

            var childSpan = tracer.StartSpan(missionId, "commercial-strategist", "FEASIBILITY_EVALUATION", "Evaluate sales capacity", rootSpan.SpanId);
            tracer.EndSpan(childSpan.SpanId, "mock-claude-sonnet", "Feasibility 0.82: Requires 2 extra SDRs", 0.08m, 150);

            var traceGraph = tracer.GetMissionCausalGraph(missionId);

            Assert.NotNull(traceGraph);
            Assert.Equal(2, traceGraph.Spans.Count);
            Assert.Equal(0.13m, traceGraph.TotalCostINR);
            Assert.Equal(270, traceGraph.TotalDurationMs);
        }

        [Fact]
        public void GateH14_CausalTracer_ExplainWhyActionWasTaken_TraversesCausalAncestry()
        {
            var tracer = new CausalTracer();
            var missionId = Guid.NewGuid();

            var rootSpan = tracer.StartSpan(missionId, "charlie-ceo-agent", "MANDATE_RECEIVED", "Receive CEO objective");
            tracer.EndSpan(rootSpan.SpanId, "deterministic-rule", "Objective parsed: INR 50L in 60d", 0m, 10);

            var midSpan = tracer.StartSpan(missionId, "commercial-officer", "STRATEGY_SYNTHESIS", "Formulate campaigns", rootSpan.SpanId);
            tracer.EndSpan(midSpan.SpanId, "mock-gpt-4o", "Synthesized 3 candidates", 0.04m, 80);

            var leafSpan = tracer.StartSpan(missionId, "sdr-agent-01", "PREPARE_OUTREACH", "Prepare prospect messages", midSpan.SpanId);
            tracer.EndSpan(leafSpan.SpanId, "mock-gemini-flash", "Prepared 20 outreach drafts", 0.01m, 30);

            var traceGraph = tracer.GetMissionCausalGraph(missionId);
            var explanation = traceGraph.ExplainWhyActionWasTaken(leafSpan.SpanId);

            Assert.NotNull(explanation);
            // Must contain leaf action and both parent causal steps
            Assert.Equal(3, explanation.Count);
            Assert.Contains("PREPARE_OUTREACH", explanation[0]);
            Assert.Contains("STRATEGY_SYNTHESIS", explanation[1]);
            Assert.Contains("MANDATE_RECEIVED", explanation[2]);
        }

        // -------------------------------------------------------------------------
        // GATE H15: Hierarchical Kill-Switch Matrix
        // -------------------------------------------------------------------------
        [Fact]
        public void GateH15_KillSwitchManager_TripsAndHaltsAtDifferentScopes()
        {
            var killSwitch = new KillSwitchManager();
            var missionId = Guid.NewGuid();
            var tenantId = "tenant-enterprise-01";

            // Baseline: Nothing halted
            Assert.False(killSwitch.IsHalted(tenantId, missionId, "Sales", "sdr-01", "VOICE_CALL"));

            // 1. Tool-level kill switch
            killSwitch.Trip(KillSwitchScope.Tool, "VOICE_CALL", "Carrier emergency rate limit", "admin-01");
            Assert.True(killSwitch.IsHalted(tenantId, missionId, "Sales", "sdr-01", "VOICE_CALL"));
            Assert.False(killSwitch.IsHalted(tenantId, missionId, "Sales", "sdr-01", "SEND_EMAIL"));

            // 2. Agent-level kill switch
            killSwitch.Trip(KillSwitchScope.Agent, "sdr-01", "Erratic agent behavior observed", "admin-01");
            Assert.True(killSwitch.IsHalted(tenantId, missionId, "Sales", "sdr-01", "SEND_EMAIL"));
            Assert.False(killSwitch.IsHalted(tenantId, missionId, "Sales", "sdr-02", "SEND_EMAIL"));

            // 3. Mission-level kill switch
            killSwitch.Trip(KillSwitchScope.Mission, missionId.ToString(), "CEO mandate paused", "admin-01");
            Assert.True(killSwitch.IsHalted(tenantId, missionId, "Sales", "sdr-02", "SEND_EMAIL"));
            Assert.False(killSwitch.IsHalted(tenantId, Guid.NewGuid(), "Sales", "sdr-02", "SEND_EMAIL"));

            // 4. Tenant-level kill switch
            killSwitch.Trip(KillSwitchScope.Tenant, tenantId, "Payment delinquency lock", "billing-system");
            Assert.True(killSwitch.IsHalted(tenantId, Guid.NewGuid(), "Sales", "sdr-02", "SEND_EMAIL"));
            Assert.False(killSwitch.IsHalted("tenant-other", Guid.NewGuid(), "Sales", "sdr-02", "SEND_EMAIL"));

            // 5. Global kill switch
            killSwitch.Trip(KillSwitchScope.Global, "*", "Global security perimeter lockdown", "ciso-operator");
            Assert.True(killSwitch.IsHalted("any-tenant", Guid.NewGuid(), "AnyDept", "any-agent", "any-tool"));
        }

        [Fact]
        public void GateH15_KillSwitchManager_Reset_RestoresOperationalStatus()
        {
            var killSwitch = new KillSwitchManager();
            killSwitch.Trip(KillSwitchScope.Global, "*", "Drill lockdown", "admin");

            Assert.True(killSwitch.IsHalted("tenant", null, null, null, null));

            killSwitch.Reset(KillSwitchScope.Global, "*");
            Assert.False(killSwitch.IsHalted("tenant", null, null, null, null));
        }

        // -------------------------------------------------------------------------
        // GATE H16: Autonomy Tiers & Continuous Governed Learning Loop
        // -------------------------------------------------------------------------
        [Fact]
        public void GateH16_AutonomyManager_PreventsSelfPromotion_AndRequiresHumanApproval()
        {
            var manager = new AutonomyManager();
            var agentId = "autonomous-sdr-09";

            Assert.Equal(AutonomyTier.L0_Observer, manager.GetCurrentTier(agentId));

            // Invariant: Agent cannot promote itself
            var selfPromoteResult = manager.RequestPromotion(agentId, AutonomyTier.L1_Analyst, agentId, humanApproved: true);
            Assert.False(selfPromoteResult);
            Assert.Equal(AutonomyTier.L0_Observer, manager.GetCurrentTier(agentId));

            // Invariant: External operator without human approval cannot promote
            var noHumanApprovalResult = manager.RequestPromotion(agentId, AutonomyTier.L1_Analyst, "admin-operator", humanApproved: false);
            Assert.False(noHumanApprovalResult);
            Assert.Equal(AutonomyTier.L0_Observer, manager.GetCurrentTier(agentId));

            // Valid promotion to L1 (Analyst) with human operator approval
            var validPromotion = manager.RequestPromotion(agentId, AutonomyTier.L1_Analyst, "admin-operator", humanApproved: true);
            Assert.True(validPromotion);
            Assert.Equal(AutonomyTier.L1_Analyst, manager.GetCurrentTier(agentId));
        }

        [Fact]
        public void GateH16_AutonomyManager_EnforcesShadowFidelityThreshold_ForCopilotOrHigher()
        {
            var manager = new AutonomyManager();
            var agentId = "growth-copilot-02";
            manager.SetTierDirect(agentId, AutonomyTier.L1_Analyst);

            // Record sub-par shadow mode evaluation (fidelity 0.72 < 0.85 threshold)
            manager.RecordShadowComparison(agentId, "{\"action\":\"email_ceo\"}", "{\"action\":\"email_vp_sales\"}", 0.72);

            // Promotion to L2 Copilot must be rejected due to low shadow fidelity
            var failedCopilotPromotion = manager.RequestPromotion(agentId, AutonomyTier.L2_Copilot, "executive-human", humanApproved: true);
            Assert.False(failedCopilotPromotion);
            Assert.Equal(AutonomyTier.L1_Analyst, manager.GetCurrentTier(agentId));

            // Add strong evaluations raising average above 0.85
            manager.RecordShadowComparison(agentId, "{\"action\":\"qualify_lead\"}", "{\"action\":\"qualify_lead\"}", 0.95);
            manager.RecordShadowComparison(agentId, "{\"action\":\"schedule_demo\"}", "{\"action\":\"schedule_demo\"}", 0.96);

            Assert.True(manager.GetAverageShadowFidelity(agentId) >= 0.85);

            // Now promotion to L2 Copilot succeeds
            var successfulCopilotPromotion = manager.RequestPromotion(agentId, AutonomyTier.L2_Copilot, "executive-human", humanApproved: true);
            Assert.True(successfulCopilotPromotion);
            Assert.Equal(AutonomyTier.L2_Copilot, manager.GetCurrentTier(agentId));
        }

        [Fact]
        public void GateH16_GovernedLearningLoop_RequiresBenchmarkAndGovernanceApproval_BeforeActivation()
        {
            var learningLoop = new GovernedLearningLoop();

            // 1. Propose candidate prompt refinement
            var candidate = learningLoop.ProposeImprovement(
                targetArea: "Commercial Outreach Prompts",
                proposedImprovement: "Add objection handling for enterprise compliance budget cycles"
            );

            Assert.NotNull(candidate);
            Assert.Equal(LearningCandidateStatus.Proposed, candidate.Status);

            // 2. Candidate with weak benchmark (< 0.85) is rejected
            learningLoop.RecordBenchmarkEvaluation(candidate.CandidateId, 0.68);
            var prematureApproval = learningLoop.ApproveCandidate(candidate.CandidateId, "cmo-governance-officer");
            Assert.False(prematureApproval);
            Assert.Equal(LearningCandidateStatus.Rejected, candidate.Status);

            // 3. New candidate with strong benchmark (>= 0.85) succeeds after human governance approval
            var highPerfCandidate = learningLoop.ProposeImprovement(
                targetArea: "ICP Scoring Engine",
                proposedImprovement: "Incorporate tech stack churn signals from company filings"
            );
            learningLoop.RecordBenchmarkEvaluation(highPerfCandidate.CandidateId, 0.93);

            var approved = learningLoop.ApproveCandidate(highPerfCandidate.CandidateId, "cmo-governance-officer");
            Assert.True(approved);
            Assert.Equal(LearningCandidateStatus.ApprovedByGovernance, highPerfCandidate.Status);
            Assert.Equal("cmo-governance-officer", highPerfCandidate.ApprovedBy);

            // 4. Verify approved candidate is recorded in approved registry
            var approvedList = learningLoop.GetApprovedCandidates();
            Assert.Single(approvedList);
            Assert.Equal(highPerfCandidate.CandidateId, approvedList[0].CandidateId);
        }
    }
}
