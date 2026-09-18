using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Api.Controllers;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Collaboration;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;
using BusinessModelApp.Infrastructure.Runtime.Organizational.Collaboration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public class Phase3Batch394MultiAgentCollaborationTests
    {
        private const string TestTenant = "TENANT-MAC-394";
        private const string AltTenant = "TENANT-MAC-ALT";

        private (
            InMemoryTeamCharterStore store,
            TeamFormationEngine formationEngine,
            CollaborationMessageBus messageBus,
            DisputeArbitrator disputeArbitrator,
            TeamLifecycleManager lifecycleManager,
            MultiAgentCollaborationService service
        ) CreateTestRig(TeamFormationPolicy? policy = null)
        {
            var store = new InMemoryTeamCharterStore();
            var formationEngine = new TeamFormationEngine(store, policy);
            var messageBus = new CollaborationMessageBus(store, policy);
            var disputeArbitrator = new DisputeArbitrator(store);
            var lifecycleManager = new TeamLifecycleManager(store);
            var service = new MultiAgentCollaborationService(
                store,
                formationEngine,
                messageBus,
                disputeArbitrator,
                lifecycleManager);

            return (store, formationEngine, messageBus, disputeArbitrator, lifecycleManager, service);
        }

        private List<TeamMemberRole> CreateSampleRoles(int count = 3)
        {
            var definitions = new[]
            {
                ("agent-alpha", "Lead Analyst", "Finance", 1, new[] { "AnalyzeCashFlow" }),
                ("agent-beta", "Operations Scout", "Logistics", 1, new[] { "TrackDeliveries" }),
                ("agent-gamma", "Market Strategist", "CompetitorIntel", 2, new[] { "ScanPricing" }),
                ("agent-delta", "Risk Auditor", "Compliance", 1, new[] { "AuditLog" }),
                ("agent-epsilon", "Operations Coordinator", "Operations", 1, new[] { "RouteAlerts" }),
                ("agent-zeta", "Sixth Agent", "Extra", 1, new[] { "ExtraCapability" })
            };

            return definitions.Take(count).Select(d => new TeamMemberRole
            {
                AgentInstanceId = d.Item1,
                RoleName = d.Item2,
                SpecializationDomain = d.Item3,
                MaxRiskTier = d.Item4,
                AssignedCapabilities = d.Item5.ToList(),
                Status = "Active"
            }).ToList();
        }

        // =========================================================================
        // FAMILY 1: Team Charter Creation & Bounded Team Size (MAC01 - MAC06)
        // =========================================================================

        [Fact]
        public async Task MAC01_FormTeam_WithValidRoles_CreatesOperatingCharter()
        {
            var rig = CreateTestRig();
            var roles = CreateSampleRoles(3);

            var (success, charter, error) = await rig.service.FormTeamAsync(
                TestTenant, "WORK-001", "Optimize Q3 working capital", roles);

            Assert.True(success);
            Assert.NotNull(charter);
            Assert.Null(error);
            Assert.Equal(TeamLifecycleStatus.Operating, charter.Status);
            Assert.Equal(3, charter.Members.Count);
            Assert.False(string.IsNullOrWhiteSpace(charter.CharterHash));
        }

        [Fact]
        public async Task MAC02_FormTeam_ExceedingMaxTeamSize_FailsWithI29G()
        {
            var rig = CreateTestRig();
            var roles = CreateSampleRoles(6); // Ceiling is 5

            var (success, charter, error) = await rig.service.FormTeamAsync(
                TestTenant, "WORK-001", "Large swarm test", roles);

            Assert.False(success);
            Assert.Null(charter);
            Assert.NotNull(error);
            Assert.Contains("I29-G", error);
            Assert.Contains("5", error);
        }

        [Fact]
        public async Task MAC03_FormTeam_ZeroMembers_FailsValidation()
        {
            var rig = CreateTestRig();
            var roles = new List<TeamMemberRole>();

            var (success, charter, error) = await rig.service.FormTeamAsync(
                TestTenant, "WORK-001", "Empty team", roles);

            Assert.False(success);
            Assert.Null(charter);
            Assert.Contains("at least one member", error);
        }

        [Fact]
        public async Task MAC04_FormTeam_DuplicateAgentInstances_FailsValidation()
        {
            var rig = CreateTestRig();
            var roles = CreateSampleRoles(2);
            // Duplicate the first agent under another role
            roles.Add(new TeamMemberRole
            {
                AgentInstanceId = roles[0].AgentInstanceId,
                RoleName = "Duplicate Role",
                SpecializationDomain = "Finance",
                MaxRiskTier = 1
            });

            var (success, charter, error) = await rig.service.FormTeamAsync(
                TestTenant, "WORK-001", "Duplicate member test", roles);

            Assert.False(success);
            Assert.Null(charter);
            Assert.Contains("Duplicate agent instances", error);
        }

        [Fact]
        public async Task MAC05_FormTeam_ExceedingActiveTenantCeiling_Fails()
        {
            var customPolicy = new TeamFormationPolicy { MaxActiveTeamsPerTenant = 2 };
            var rig = CreateTestRig(customPolicy);

            var (s1, c1, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Team 1", CreateSampleRoles(2));
            var (s2, c2, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-02", "Team 2", CreateSampleRoles(2));
            var (s3, c3, err3) = await rig.service.FormTeamAsync(TestTenant, "WORK-03", "Team 3", CreateSampleRoles(2));

            Assert.True(s1);
            Assert.True(s2);
            Assert.False(s3);
            Assert.Null(c3);
            Assert.Contains("maximum active teams ceiling", err3);
        }

        [Fact]
        public async Task MAC06_FormTeam_TenantPolicyOverride_TakesMinimum()
        {
            var rig = CreateTestRig();
            var tenantOverride = new TeamFormationPolicy { MaxTeamSize = 2 };
            var roles = CreateSampleRoles(3); // 3 exceeds overridden ceiling of 2

            var (success, charter, error) = await rig.service.FormTeamAsync(
                TestTenant, "WORK-01", "Tenant override check", roles, policyOverride: tenantOverride);

            Assert.False(success);
            Assert.Null(charter);
            Assert.Contains("2", error);
        }

        // =========================================================================
        // FAMILY 2: Rejection of Authority Pooling & Capability Expansion (MAC07 - MAC12)
        // =========================================================================

        [Fact]
        public async Task MAC07_AuthorityPooling_RiskTierElevatedBeyondCeiling_Fails()
        {
            var rig = CreateTestRig();
            var roles = CreateSampleRoles(2);
            roles[0].MaxRiskTier = 4; // Autonomous collaboration ceiling is Tier 3

            var (success, charter, error) = await rig.service.FormTeamAsync(
                TestTenant, "WORK-01", "High risk pooling", roles);

            Assert.False(success);
            Assert.Null(charter);
            Assert.Contains("I29-B", error);
        }

        [Fact]
        public async Task MAC08_AuthorityPooling_TeamGovernanceTier_MatchesMaxRoleRiskTier()
        {
            var rig = CreateTestRig();
            var roles = CreateSampleRoles(3); // Gamma has MaxRiskTier = 2

            var (success, charter, _) = await rig.service.FormTeamAsync(
                TestTenant, "WORK-01", "Governance matching", roles, governanceTier: 1);

            Assert.True(success);
            Assert.NotNull(charter);
            Assert.Equal(2, charter.GovernanceTier); // Elevates to max member risk tier
        }

        [Fact]
        public async Task MAC09_AuthorityPooling_RolesWithoutDeclaredSpecialization_FailsI29M()
        {
            var rig = CreateTestRig();
            var roles = CreateSampleRoles(2);
            roles[1].SpecializationDomain = ""; // Empty specialization

            var (success, charter, error) = await rig.service.FormTeamAsync(
                TestTenant, "WORK-01", "Unspecialized role", roles);

            Assert.False(success);
            Assert.Null(charter);
            Assert.Contains("I29-M", error);
        }

        [Fact]
        public void MAC10_AuthorityPooling_NoCombinedExecutionPermitCreated()
        {
            var charter = new TeamCharter();
            var permitProps = typeof(TeamCharter).GetProperties()
                .Where(p => p.Name.Contains("Permit", StringComparison.OrdinalIgnoreCase) ||
                            p.PropertyType.Name.Contains("ExecutionPermit", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.Empty(permitProps);
        }

        [Fact]
        public async Task MAC11_AuthorityPooling_MemberCapabilitiesRemainScoped()
        {
            var rig = CreateTestRig();
            var roles = CreateSampleRoles(2);
            // Alpha has AnalyzeCashFlow, Beta has TrackDeliveries

            var (success, charter, _) = await rig.service.FormTeamAsync(
                TestTenant, "WORK-01", "Capability isolation", roles);

            Assert.True(success);
            Assert.NotNull(charter);
            var alpha = charter.Members.First(m => m.AgentInstanceId == "agent-alpha");
            var beta = charter.Members.First(m => m.AgentInstanceId == "agent-beta");

            Assert.Contains("AnalyzeCashFlow", alpha.AssignedCapabilities);
            Assert.DoesNotContain("TrackDeliveries", alpha.AssignedCapabilities);
            Assert.Contains("TrackDeliveries", beta.AssignedCapabilities);
            Assert.DoesNotContain("AnalyzeCashFlow", beta.AssignedCapabilities);
        }

        [Fact]
        public async Task MAC12_AuthorityPooling_MultiAgentTeamCannotBypassGovernanceTier()
        {
            var rig = CreateTestRig();
            var roles = CreateSampleRoles(2);

            var (success, charter, _) = await rig.service.FormTeamAsync(
                TestTenant, "WORK-01", "Tier 0 attempt", roles, governanceTier: 0);

            Assert.True(success);
            Assert.NotNull(charter);
            Assert.True(charter.GovernanceTier >= 1);
        }

        // =========================================================================
        // FAMILY 3: Rejection of Peer Spawning & Unvetted Delegation (MAC13 - MAC18)
        // =========================================================================

        [Fact]
        public async Task MAC13_PeerSpawning_UncharteredSender_RejectedByMessageBus()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var (dispatched, msg, err) = await rig.service.SendMessageAsync(
                TestTenant, charter!.CharterId, "rogue-agent-x", "agent-alpha",
                CollaborationMessageType.InformationSharing, "Unsolicited message");

            Assert.False(dispatched);
            Assert.Null(msg);
            Assert.Contains("I29-C", err);
        }

        [Fact]
        public void MAC14_PeerSpawning_CharterHasNoChildSpawningEndpoints()
        {
            var methods = typeof(TeamFormationEngine).GetMethods()
                .Where(m => m.Name.Contains("Spawn", StringComparison.OrdinalIgnoreCase) ||
                            m.Name.Contains("Fork", StringComparison.OrdinalIgnoreCase) ||
                            m.Name.Contains("CreateChild", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.Empty(methods);
        }

        [Fact]
        public async Task MAC15_PeerSpawning_OnlyCharteredRolesCanParticipate()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var (d1, m1, _) = await rig.service.SendMessageAsync(
                TestTenant, charter!.CharterId, "agent-alpha", "agent-beta",
                CollaborationMessageType.InformationSharing, "Valid message");

            Assert.True(d1);
            Assert.NotNull(m1);

            var (d2, m2, err2) = await rig.service.SendMessageAsync(
                TestTenant, charter.CharterId, "unvetted-worker", "agent-alpha",
                CollaborationMessageType.InformationSharing, "Invalid worker");

            Assert.False(d2);
            Assert.Contains("not an authorized member", err2);
        }

        [Fact]
        public async Task MAC16_PeerSpawning_CharterCompilationRequired()
        {
            var rig = CreateTestRig();

            var (dispatched, msg, err) = await rig.service.SendMessageAsync(
                TestTenant, "NON-EXISTENT-CHARTER", "agent-alpha", "agent-beta",
                CollaborationMessageType.InformationSharing, "Ghost charter");

            Assert.False(dispatched);
            Assert.Contains("not found", err);
        }

        [Fact]
        public async Task MAC17_PeerSpawning_SystemRoleAllowedForSupervisoryNotification()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var (dispatched, msg, err) = await rig.service.SendMessageAsync(
                TestTenant, charter!.CharterId, "System", "Broadcast",
                CollaborationMessageType.StatusUpdate, "Charter active and monitored.");

            Assert.True(dispatched);
            Assert.NotNull(msg);
            Assert.Null(err);
        }

        [Fact]
        public async Task MAC18_PeerSpawning_AgentCannotDynamicallyAddMembersToCharter()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            // Send message asking to add member - charter members count remains 2
            await rig.service.SendMessageAsync(
                TestTenant, charter!.CharterId, "agent-alpha", "Broadcast",
                CollaborationMessageType.ProposalOffer, "Can we invite agent-gamma?");

            var fetched = await rig.service.GetCharterAsync(TestTenant, charter.CharterId);
            Assert.Equal(2, fetched!.Members.Count);
        }

        // =========================================================================
        // FAMILY 4: Structured Information Exchange & Turn Ceilings (MAC19 - MAC24)
        // =========================================================================

        [Fact]
        public async Task MAC19_StructuredExchange_ValidMessage_ComputedMessageHash()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var (dispatched, msg, _) = await rig.service.SendMessageAsync(
                TestTenant, charter!.CharterId, "agent-alpha", "agent-beta",
                CollaborationMessageType.InformationSharing, "Sharing liquidity analysis");

            Assert.True(dispatched);
            Assert.NotNull(msg);
            Assert.False(string.IsNullOrWhiteSpace(msg.MessageHash));
            Assert.Equal(1, msg.TurnNumber);
        }

        [Fact]
        public async Task MAC20_StructuredExchange_TurnSequence_IncrementsMonotonically()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var (_, m1, _) = await rig.service.SendMessageAsync(TestTenant, charter!.CharterId, "agent-alpha", "agent-beta", CollaborationMessageType.InformationSharing, "Msg 1");
            var (_, m2, _) = await rig.service.SendMessageAsync(TestTenant, charter.CharterId, "agent-beta", "agent-alpha", CollaborationMessageType.ProposalOffer, "Msg 2");
            var (_, m3, _) = await rig.service.SendMessageAsync(TestTenant, charter.CharterId, "agent-alpha", "agent-beta", CollaborationMessageType.ProposalCounter, "Msg 3");

            Assert.Equal(1, m1!.TurnNumber);
            Assert.Equal(2, m2!.TurnNumber);
            Assert.Equal(3, m3!.TurnNumber);
        }

        [Fact]
        public async Task MAC21_StructuredExchange_ExceedingTurnCeiling_FailsI29G()
        {
            var rig = CreateTestRig(new TeamFormationPolicy { MaxNegotiationRounds = 3 });
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            await rig.service.SendMessageAsync(TestTenant, charter!.CharterId, "agent-alpha", "agent-beta", CollaborationMessageType.InformationSharing, "Turn 1");
            await rig.service.SendMessageAsync(TestTenant, charter.CharterId, "agent-beta", "agent-alpha", CollaborationMessageType.InformationSharing, "Turn 2");
            await rig.service.SendMessageAsync(TestTenant, charter.CharterId, "agent-alpha", "agent-beta", CollaborationMessageType.InformationSharing, "Turn 3");

            // 4th turn exceeds ceiling of 3
            var (dispatched, msg, err) = await rig.service.SendMessageAsync(
                TestTenant, charter.CharterId, "agent-beta", "agent-alpha", CollaborationMessageType.InformationSharing, "Turn 4");

            Assert.False(dispatched);
            Assert.Null(msg);
            Assert.Contains("I29-G", err);
            Assert.Contains("ceiling", err);
        }

        [Fact]
        public async Task MAC22_StructuredExchange_WorkHandoffRequiresEvidence_SucceedsWithEvidence()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var (dispatched, msg, err) = await rig.service.SendMessageAsync(
                TestTenant, charter!.CharterId, "agent-alpha", "agent-beta",
                CollaborationMessageType.WorkHandoff, "Handoff analysis report",
                evidenceRefs: new[] { "EVID-REPORT-001" });

            Assert.True(dispatched);
            Assert.NotNull(msg);
            Assert.Null(err);
        }

        [Fact]
        public async Task MAC23_StructuredExchange_WorkHandoffWithoutEvidence_FailsI29D()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var (dispatched, msg, err) = await rig.service.SendMessageAsync(
                TestTenant, charter!.CharterId, "agent-alpha", "agent-beta",
                CollaborationMessageType.WorkHandoff, "Handoff without proof");

            Assert.False(dispatched);
            Assert.Null(msg);
            Assert.Contains("I29-D", err);
            Assert.Contains("evidence", err);
        }

        [Fact]
        public async Task MAC24_StructuredExchange_ImmutableMessageAuditLog()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            await rig.service.SendMessageAsync(TestTenant, charter!.CharterId, "agent-alpha", "agent-beta", CollaborationMessageType.InformationSharing, "A to B");
            await rig.service.SendMessageAsync(TestTenant, charter.CharterId, "agent-beta", "agent-alpha", CollaborationMessageType.InformationSharing, "B to A");

            var messages = await rig.service.GetMessagesAsync(TestTenant, charter.CharterId);
            Assert.Equal(2, messages.Count);
            Assert.Equal("A to B", messages[0].Content);
            Assert.Equal("B to A", messages[1].Content);
        }

        // =========================================================================
        // FAMILY 5: Deterministic Dispute Arbitration & Evidence Precedence (MAC25 - MAC30)
        // =========================================================================

        [Fact]
        public async Task MAC25_DisputeArbitration_ClearEvidenceWinner_EvidencePrevails()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var evidenceScores = new Dictionary<string, double>
            {
                ["agent-alpha"] = 0.95,
                ["agent-beta"] = 0.60
            };

            var dispute = await rig.service.RaiseDisputeAsync(
                TestTenant, charter!.CharterId, DisputeType.ConflictingHypothesis,
                "Cash runway estimate", "agent-alpha", new[] { "agent-beta" },
                new Dictionary<string, string>
                {
                    ["agent-alpha"] = "Runway is 4 months based on audited burn",
                    ["agent-beta"] = "Runway is 6 months based on projected sales"
                },
                evidenceScores);

            Assert.Equal(ArbitrationOutcome.EvidencePrevails, dispute.ResolutionOutcome);
            Assert.Contains("agent-alpha", dispute.ResolutionEvidence);
            Assert.False(dispute.EscalatedToGovernance);
        }

        [Fact]
        public async Task MAC26_DisputeArbitration_SingleEvidenceProvided_EvidencePrevails()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var evidenceScores = new Dictionary<string, double>
            {
                ["agent-alpha"] = 0.88
            };

            var dispute = await rig.service.RaiseDisputeAsync(
                TestTenant, charter!.CharterId, DisputeType.ConflictingHypothesis,
                "Vendor risk tier", "agent-alpha", new[] { "agent-beta" },
                new Dictionary<string, string>(), evidenceScores);

            Assert.Equal(ArbitrationOutcome.EvidencePrevails, dispute.ResolutionOutcome);
        }

        [Fact]
        public async Task MAC27_DisputeArbitration_TiedEvidence_ResolvesBySpecialization()
        {
            var rig = CreateTestRig();
            // Alpha: Finance, Beta: Logistics
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var tiedEvidence = new Dictionary<string, double>
            {
                ["agent-alpha"] = 0.80,
                ["agent-beta"] = 0.80
            };

            var dispute = await rig.service.RaiseDisputeAsync(
                TestTenant, charter!.CharterId, DisputeType.RoleBoundaryOverlap,
                "Topic in Logistics delivery routing", "agent-alpha", new[] { "agent-beta" },
                new Dictionary<string, string>(), tiedEvidence);

            Assert.Equal(ArbitrationOutcome.ReputationPrevails, dispute.ResolutionOutcome);
            Assert.Contains("agent-beta", dispute.ResolutionEvidence);
            Assert.Contains("Logistics", dispute.ResolutionEvidence);
        }

        [Fact]
        public async Task MAC28_DisputeArbitration_NoEvidence_SpecializationResolves()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var dispute = await rig.service.RaiseDisputeAsync(
                TestTenant, charter!.CharterId, DisputeType.PriorityClash,
                "Matter regarding Finance budget allocation", "agent-beta", new[] { "agent-alpha" },
                new Dictionary<string, string>());

            Assert.Equal(ArbitrationOutcome.ReputationPrevails, dispute.ResolutionOutcome);
            Assert.Contains("agent-alpha", dispute.ResolutionEvidence);
        }

        [Fact]
        public async Task MAC29_DisputeArbitration_TiedEvidenceNoSpecializationTier1_SplitCompromise()
        {
            var rig = CreateTestRig();
            var roles = CreateSampleRoles(2);
            roles[0].MaxRiskTier = 1;
            roles[1].MaxRiskTier = 1;

            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", roles, governanceTier: 1);
            Assert.True(s);

            var dispute = await rig.service.RaiseDisputeAsync(
                TestTenant, charter!.CharterId, DisputeType.ResourceContention,
                "Generic compute quota contention", "agent-alpha", new[] { "agent-beta" },
                new Dictionary<string, string>());

            Assert.Equal(ArbitrationOutcome.SplitCompromise, dispute.ResolutionOutcome);
            Assert.False(dispute.EscalatedToGovernance);
        }

        [Fact]
        public async Task MAC30_DisputeArbitration_DisputeHashComputedAndAuditable()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var dispute = await rig.service.RaiseDisputeAsync(
                TestTenant, charter!.CharterId, DisputeType.ConflictingHypothesis,
                "Test Topic", "agent-alpha", new[] { "agent-beta" },
                new Dictionary<string, string>());

            Assert.False(string.IsNullOrWhiteSpace(dispute.DisputeHash));
        }

        // =========================================================================
        // FAMILY 6: Policy Constraint Dominance & Governance Escalation (MAC31 - MAC36)
        // =========================================================================

        [Fact]
        public async Task MAC31_PolicyDominance_PolicyInterpretationDispute_PolicyPrevails()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var dispute = await rig.service.RaiseDisputeAsync(
                TestTenant, charter!.CharterId, DisputeType.PolicyInterpretation,
                "Discount policy cap of 15%", "agent-alpha", new[] { "agent-beta" },
                new Dictionary<string, string>
                {
                    ["agent-alpha"] = "Policy allows 20% exception",
                    ["agent-beta"] = "Policy hard ceiling is 15%"
                });

            Assert.Equal(ArbitrationOutcome.PolicyPrevails, dispute.ResolutionOutcome);
            Assert.Contains("hard business constraints", dispute.ResolutionEvidence, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task MAC32_GovernanceEscalation_Tier2CharterUnresolved_EscalatesToGovernance()
        {
            var rig = CreateTestRig();
            // Create Tier 2 charter with roles that don't specialize in HR
            var roles = CreateSampleRoles(2);
            var (s, charter, _) = await rig.service.FormTeamAsync(
                TestTenant, "WORK-01", "Obj", roles, governanceTier: 2);
            Assert.True(s);

            var dispute = await rig.service.RaiseDisputeAsync(
                TestTenant, charter!.CharterId, DisputeType.ResourceContention,
                "Contention over HR head-count budget", "agent-alpha", new[] { "agent-beta" },
                new Dictionary<string, string>());

            Assert.Equal(ArbitrationOutcome.EscalatedToGovernance, dispute.ResolutionOutcome);
            Assert.True(dispute.EscalatedToGovernance);
            Assert.Contains("PRG-1", dispute.ResolutionEvidence);
        }

        [Fact]
        public async Task MAC33_GovernanceEscalation_CharterStatusUpdatedToDisputed()
        {
            var rig = CreateTestRig();
            var roles = CreateSampleRoles(2);
            var (s, charter, _) = await rig.service.FormTeamAsync(
                TestTenant, "WORK-01", "Obj", roles, governanceTier: 2);
            Assert.True(s);

            await rig.service.RaiseDisputeAsync(
                TestTenant, charter!.CharterId, DisputeType.ResourceContention,
                "Contention over unspecialized domain", "agent-alpha", new[] { "agent-beta" },
                new Dictionary<string, string>());

            var updated = await rig.service.GetCharterAsync(TestTenant, charter.CharterId);
            Assert.Equal(TeamLifecycleStatus.Disputed, updated!.Status);
        }

        [Fact]
        public async Task MAC34_GovernanceEscalation_Tier3Charter_AlwaysEscalatesUnresolved()
        {
            var rig = CreateTestRig();
            var roles = CreateSampleRoles(2);
            var (s, charter, _) = await rig.service.FormTeamAsync(
                TestTenant, "WORK-01", "Obj", roles, governanceTier: 3);
            Assert.True(s);

            var dispute = await rig.service.RaiseDisputeAsync(
                TestTenant, charter!.CharterId, DisputeType.ResourceContention,
                "High value commitment contention", "agent-alpha", new[] { "agent-beta" },
                new Dictionary<string, string>());

            Assert.Equal(ArbitrationOutcome.EscalatedToGovernance, dispute.ResolutionOutcome);
        }

        [Fact]
        public async Task MAC35_PolicyDominance_EnterpriseConstraintOverridesAgentPreference()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var dispute = await rig.service.RaiseDisputeAsync(
                TestTenant, charter!.CharterId, DisputeType.PolicyInterpretation,
                "Data retention limit", "agent-alpha", new[] { "agent-beta" },
                new Dictionary<string, string>());

            Assert.Contains("I29-E", dispute.ResolutionEvidence);
        }

        [Fact]
        public async Task MAC36_GovernanceEscalation_DisputeEvidenceRecordsAuditTrail()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(
                TestTenant, "WORK-01", "Obj", CreateSampleRoles(2), governanceTier: 2);
            Assert.True(s);

            var dispute = await rig.service.RaiseDisputeAsync(
                TestTenant, charter!.CharterId, DisputeType.ResourceContention,
                "Major budget dispute", "agent-alpha", new[] { "agent-beta" },
                new Dictionary<string, string>());

            var disputes = await rig.service.GetDisputesAsync(TestTenant, charter.CharterId);
            Assert.Single(disputes);
            Assert.Equal(dispute.DisputeId, disputes[0].DisputeId);
        }

        // =========================================================================
        // FAMILY 7: Consensus != Truth (MAC37 - MAC42)
        // =========================================================================

        [Fact]
        public async Task MAC37_ConsensusNotTruth_UngroundedClaim_DemotedToHypothesis()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            // Send claiming VerifiedTruth without evidence
            var (dispatched, msg, _) = await rig.service.SendMessageAsync(
                TestTenant, charter!.CharterId, "agent-alpha", "agent-beta",
                CollaborationMessageType.InformationSharing, "Unverified claim",
                evidenceRefs: null, epistemicStatus: EpistemicStatus.VerifiedTruth);

            Assert.True(dispatched);
            Assert.Equal(EpistemicStatus.Hypothesis, msg!.EpistemicClassification);
        }

        [Fact]
        public async Task MAC38_ConsensusNotTruth_ObservedFactClaimWithoutEvidence_Demoted()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var (dispatched, msg, _) = await rig.service.SendMessageAsync(
                TestTenant, charter!.CharterId, "agent-alpha", "agent-beta",
                CollaborationMessageType.InformationSharing, "Fact without proof",
                evidenceRefs: new List<string>(), epistemicStatus: EpistemicStatus.ObservedFact);

            Assert.True(dispatched);
            Assert.Equal(EpistemicStatus.Hypothesis, msg!.EpistemicClassification);
        }

        [Fact]
        public async Task MAC39_ConsensusNotTruth_ClaimWithVerifiableEvidence_PreservesStatus()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var (dispatched, msg, _) = await rig.service.SendMessageAsync(
                TestTenant, charter!.CharterId, "agent-alpha", "agent-beta",
                CollaborationMessageType.InformationSharing, "Grounded fact",
                evidenceRefs: new[] { "EVID-HASH-999" }, epistemicStatus: EpistemicStatus.ObservedFact);

            Assert.True(dispatched);
            Assert.Equal(EpistemicStatus.ObservedFact, msg!.EpistemicClassification);
        }

        [Fact]
        public async Task MAC40_ConsensusNotTruth_MultipleAgentsAgreeingWithoutEvidence_CannotElevateTruth()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            // 1st agent asserts without evidence
            var (_, m1, _) = await rig.service.SendMessageAsync(
                TestTenant, charter!.CharterId, "agent-alpha", "agent-beta",
                CollaborationMessageType.ProposalOffer, "We agree competitor lowered price",
                epistemicStatus: EpistemicStatus.Hypothesis);

            // 2nd agent concurs without evidence claiming VerifiedTruth
            var (_, m2, _) = await rig.service.SendMessageAsync(
                TestTenant, charter.CharterId, "agent-beta", "agent-alpha",
                CollaborationMessageType.ProposalCounter, "I agree 100%, let's treat this as truth",
                epistemicStatus: EpistemicStatus.VerifiedTruth);

            Assert.Equal(EpistemicStatus.Hypothesis, m1!.EpistemicClassification);
            Assert.Equal(EpistemicStatus.Hypothesis, m2!.EpistemicClassification); // Demoted because no evidence
        }

        [Fact]
        public async Task MAC41_ConsensusNotTruth_DisputeConcession_DoesNotCreateTruth()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var (_, msg, _) = await rig.service.SendMessageAsync(
                TestTenant, charter!.CharterId, "agent-alpha", "agent-beta",
                CollaborationMessageType.DisputeConcession, "I concede my assertion was wrong",
                epistemicStatus: EpistemicStatus.Hypothesis);

            Assert.Equal(EpistemicStatus.Hypothesis, msg!.EpistemicClassification);
        }

        [Fact]
        public void MAC42_ConsensusNotTruth_ConsensusRecordedAsAgreementNotFact()
        {
            Assert.Equal(
                "I29-I: Multi-agent consensus (majority vote or unanimous agreement) never converts a hypothesis or ungrounded assertion into empirical truth.",
                MultiAgentCollaborationSovereignty.I29_I_ConsensusNotTruth);
        }

        // =========================================================================
        // FAMILY 8: Ephemeral Team Lifecycle & Dissolution (MAC43 - MAC48)
        // =========================================================================

        [Fact]
        public async Task MAC43_Lifecycle_DissolveTeam_TransitionsStatusToDissolved()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var (dissolved, err) = await rig.service.DissolveTeamAsync(TestTenant, charter!.CharterId, "Work Completed");
            Assert.True(dissolved);
            Assert.Null(err);

            var updated = await rig.service.GetCharterAsync(TestTenant, charter.CharterId);
            Assert.Equal(TeamLifecycleStatus.Dissolved, updated!.Status);
        }

        [Fact]
        public async Task MAC44_Lifecycle_DissolvedTeam_RejectsNewMessages()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            await rig.service.DissolveTeamAsync(TestTenant, charter!.CharterId, "Done");

            var (dispatched, _, err) = await rig.service.SendMessageAsync(
                TestTenant, charter.CharterId, "agent-alpha", "agent-beta",
                CollaborationMessageType.InformationSharing, "Post dissolution message");

            Assert.False(dispatched);
            Assert.Contains("Dissolved", err);
        }

        [Fact]
        public async Task MAC45_Lifecycle_DissolveTeam_GeneratesPerformanceRecord()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            await rig.service.SendMessageAsync(TestTenant, charter!.CharterId, "agent-alpha", "agent-beta", CollaborationMessageType.InformationSharing, "Msg 1");
            await rig.service.DissolveTeamAsync(TestTenant, charter.CharterId, "Completed");

            var perf = await rig.store.GetPerformanceRecordAsync(TestTenant, charter.CharterId);
            Assert.NotNull(perf);
            Assert.Equal(1, perf.RoundsUsed);
            Assert.True(perf.ObjectiveAchieved);
            Assert.Equal(1.0, perf.CollaborationEfficiencyScore);
        }

        [Fact]
        public async Task MAC46_Lifecycle_AlreadyDissolvedTeam_ReturnsIdempotentSuccess()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            await rig.service.DissolveTeamAsync(TestTenant, charter!.CharterId, "First");
            var (d2, err2) = await rig.service.DissolveTeamAsync(TestTenant, charter.CharterId, "Second");

            Assert.True(d2);
            Assert.Contains("already dissolved", err2);
        }

        [Fact]
        public async Task MAC47_Lifecycle_ProcessExpiredCharters_TransitionsToTimedOut()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            // Artificially expire the charter
            charter!.ExpiresUtc = DateTime.UtcNow.AddMinutes(-5);
            await rig.store.SaveCharterAsync(charter);

            int expiredCount = await rig.lifecycleManager.ProcessExpiredChartersAsync(TestTenant);
            Assert.Equal(1, expiredCount);

            var updated = await rig.service.GetCharterAsync(TestTenant, charter.CharterId);
            Assert.Equal(TeamLifecycleStatus.TimedOut, updated!.Status);
        }

        [Fact]
        public async Task MAC48_Lifecycle_TimedOutTeam_RejectsNewMessages()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            charter!.ExpiresUtc = DateTime.UtcNow.AddMinutes(-1);
            await rig.store.SaveCharterAsync(charter);

            var (dispatched, _, err) = await rig.service.SendMessageAsync(
                TestTenant, charter.CharterId, "agent-alpha", "agent-beta",
                CollaborationMessageType.InformationSharing, "Late message");

            Assert.False(dispatched);
            Assert.Contains("expired", err);
        }

        // =========================================================================
        // FAMILY 9: Multi-Tenant Partitioning (MAC49 - MAC54)
        // =========================================================================

        [Fact]
        public async Task MAC49_MultiTenant_ChartersPartitionedByTenant()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var fetched = await rig.service.GetCharterAsync(AltTenant, charter!.CharterId);
            Assert.Null(fetched);
        }

        [Fact]
        public async Task MAC50_MultiTenant_MessagesPartitionedByTenant()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            await rig.service.SendMessageAsync(TestTenant, charter!.CharterId, "agent-alpha", "agent-beta", CollaborationMessageType.InformationSharing, "Tenant msg");

            var msgs = await rig.service.GetMessagesAsync(AltTenant, charter.CharterId);
            Assert.Empty(msgs);
        }

        [Fact]
        public async Task MAC51_MultiTenant_DisputesPartitionedByTenant()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            await rig.service.RaiseDisputeAsync(TestTenant, charter!.CharterId, DisputeType.ConflictingHypothesis, "Topic", "agent-alpha", new[] { "agent-beta" }, new Dictionary<string, string>());

            var disputes = await rig.service.GetDisputesAsync(AltTenant, charter.CharterId);
            Assert.Empty(disputes);
        }

        [Fact]
        public async Task MAC52_MultiTenant_ActiveChartersQuery_IsolatedByTenant()
        {
            var rig = CreateTestRig();
            await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Team A", CreateSampleRoles(2));
            await rig.service.FormTeamAsync(AltTenant, "WORK-02", "Team B", CreateSampleRoles(2));

            var listA = await rig.service.ListActiveChartersAsync(TestTenant);
            var listB = await rig.service.ListActiveChartersAsync(AltTenant);

            Assert.Single(listA);
            Assert.Single(listB);
            Assert.Equal("Team A", listA[0].Objective);
            Assert.Equal("Team B", listB[0].Objective);
        }

        [Fact]
        public async Task MAC53_MultiTenant_CrossTenantMessageSending_Rejected()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var (dispatched, _, err) = await rig.service.SendMessageAsync(
                AltTenant, charter!.CharterId, "agent-alpha", "agent-beta",
                CollaborationMessageType.InformationSharing, "Cross tenant breach attempt");

            Assert.False(dispatched);
            Assert.Contains("not found", err);
        }

        [Fact]
        public async Task MAC54_MultiTenant_CrossTenantDissolution_Rejected()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            var (dissolved, err) = await rig.service.DissolveTeamAsync(AltTenant, charter!.CharterId, "Breach dissolution");
            Assert.False(dissolved);
            Assert.Contains("not found", err);
        }

        // =========================================================================
        // FAMILY 10: Deterministic Replay & Cryptographic Hashes (MAC55 - MAC60)
        // =========================================================================

        [Fact]
        public void MAC55_Replay_CharterHash_DeterministicOnIdenticalInputs()
        {
            var c1 = new TeamCharter { TenantId = TestTenant, WorkId = "W1", Objective = "Goal", ExpiresUtc = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc) };
            var c2 = new TeamCharter { TenantId = TestTenant, WorkId = "W1", Objective = "Goal", ExpiresUtc = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc) };

            c1.ComputeCharterHash();
            c2.ComputeCharterHash();

            Assert.Equal(c1.CharterHash, c2.CharterHash);
        }

        [Fact]
        public void MAC56_Replay_MessageHash_DeterministicOnIdenticalInputs()
        {
            var m1 = new CollaborationMessage { TenantId = TestTenant, CharterId = "C1", SenderAgentId = "A1", RecipientAgentId = "A2", Content = "Test", TurnNumber = 1 };
            var m2 = new CollaborationMessage { TenantId = TestTenant, CharterId = "C1", SenderAgentId = "A1", RecipientAgentId = "A2", Content = "Test", TurnNumber = 1 };

            m1.ComputeMessageHash();
            m2.ComputeMessageHash();

            Assert.Equal(m1.MessageHash, m2.MessageHash);
        }

        [Fact]
        public void MAC57_Replay_DisputeHash_DeterministicOnIdenticalInputs()
        {
            var time = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);
            var d1 = new DisputeRecord { TenantId = TestTenant, CharterId = "C1", Topic = "T", InitiatorAgentId = "A1", ResolvedUtc = time };
            var d2 = new DisputeRecord { TenantId = TestTenant, CharterId = "C1", Topic = "T", InitiatorAgentId = "A1", ResolvedUtc = time };

            d1.ComputeDisputeHash();
            d2.ComputeDisputeHash();

            Assert.Equal(d1.DisputeHash, d2.DisputeHash);
        }

        [Fact]
        public async Task MAC58_Replay_MessageSequenceReconstruction()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Obj", CreateSampleRoles(2));
            Assert.True(s);

            for (int i = 1; i <= 5; i++)
            {
                await rig.service.SendMessageAsync(TestTenant, charter!.CharterId, "agent-alpha", "agent-beta", CollaborationMessageType.InformationSharing, $"Step {i}");
            }

            var msgs = await rig.service.GetMessagesAsync(TestTenant, charter!.CharterId);
            Assert.Equal(5, msgs.Count);
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(i + 1, msgs[i].TurnNumber);
                Assert.Equal($"Step {i + 1}", msgs[i].Content);
            }
        }

        [Fact]
        public void MAC59_Replay_TamperedCharter_DetectedByHashMismatch()
        {
            var charter = new TeamCharter { TenantId = TestTenant, WorkId = "W1", Objective = "Original", ExpiresUtc = DateTime.UtcNow };
            charter.ComputeCharterHash();
            var originalHash = charter.CharterHash;

            charter.Objective = "Tampered Objective";
            charter.ComputeCharterHash();

            Assert.NotEqual(originalHash, charter.CharterHash);
        }

        [Fact]
        public async Task MAC60_Replay_FullCollaborationAuditTrailPreserved()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Audit trail test", CreateSampleRoles(2));
            Assert.True(s);

            await rig.service.SendMessageAsync(TestTenant, charter!.CharterId, "agent-alpha", "agent-beta", CollaborationMessageType.InformationSharing, "Discussion");
            await rig.service.RaiseDisputeAsync(TestTenant, charter.CharterId, DisputeType.ConflictingHypothesis, "Topic", "agent-alpha", new[] { "agent-beta" }, new Dictionary<string, string>());
            await rig.service.DissolveTeamAsync(TestTenant, charter.CharterId, "Completed");

            var c = await rig.service.GetCharterAsync(TestTenant, charter.CharterId);
            var m = await rig.service.GetMessagesAsync(TestTenant, charter.CharterId);
            var d = await rig.service.GetDisputesAsync(TestTenant, charter.CharterId);
            var p = await rig.store.GetPerformanceRecordAsync(TestTenant, charter.CharterId);

            Assert.NotNull(c);
            Assert.Single(m);
            Assert.Single(d);
            Assert.NotNull(p);
        }

        // =========================================================================
        // FAMILY 11: Constitutional Invariant Laws I29-A through I29-N (MAC61 - MAC66)
        // =========================================================================

        [Fact]
        public void MAC61_InvariantLaw_NameAndStatement_Accurate()
        {
            Assert.Equal("I29", MultiAgentCollaborationSovereignty.InvariantName);
            Assert.Contains("COLLABORATION", MultiAgentCollaborationSovereignty.InvariantStatement);
            Assert.Contains("SWARM AUTONOMY", MultiAgentCollaborationSovereignty.InvariantStatement);
            Assert.Contains("AUTHORITY POOLING", MultiAgentCollaborationSovereignty.InvariantStatement);
            Assert.Contains("EXECUTION PERMIT", MultiAgentCollaborationSovereignty.InvariantStatement);
        }

        [Fact]
        public void MAC62_InvariantLaws_AllConstantsDeclaredAndNonEmpty()
        {
            var fields = typeof(MultiAgentCollaborationSovereignty).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
                .ToList();

            Assert.True(fields.Count >= 14);
            foreach (var f in fields)
            {
                var val = (string?)f.GetValue(null);
                Assert.False(string.IsNullOrWhiteSpace(val));
            }
        }

        [Fact]
        public void MAC63_InvariantLaw_NoAutonomousSwarmConstraint()
        {
            Assert.Contains("I29-A", MultiAgentCollaborationSovereignty.I29_A_NoUncontrolledSwarms);
            Assert.Contains("Team Charters", MultiAgentCollaborationSovereignty.I29_A_NoUncontrolledSwarms);
        }

        [Fact]
        public void MAC64_InvariantLaw_ConsensusSeparationDeclared()
        {
            Assert.Contains("I29-I", MultiAgentCollaborationSovereignty.I29_I_ConsensusNotTruth);
            Assert.Contains("never converts a hypothesis", MultiAgentCollaborationSovereignty.I29_I_ConsensusNotTruth);
        }

        [Fact]
        public void MAC65_InvariantLaw_FirewallIsolationDeclared()
        {
            Assert.Contains("I29-J", MultiAgentCollaborationSovereignty.I29_J_FirewallIsolation);
            Assert.Contains("cannot issue ExecutionPermits", MultiAgentCollaborationSovereignty.I29_J_FirewallIsolation);
        }

        [Fact]
        public void MAC66_InvariantLaw_RoleSpecializationDeclared()
        {
            Assert.Contains("I29-M", MultiAgentCollaborationSovereignty.I29_M_RoleSpecializationBoundaries);
            Assert.Contains("declared capability domains", MultiAgentCollaborationSovereignty.I29_M_RoleSpecializationBoundaries);
        }

        // =========================================================================
        // FAMILY 12: Batch 6 Execution Firewall Isolation (MAC67 - MAC72)
        // =========================================================================

        [Fact]
        public void MAC67_FirewallIsolation_CharterHasNoExecutionPermitProperty()
        {
            var props = typeof(TeamCharter).GetProperties();
            Assert.DoesNotContain(props, p => p.Name.Contains("Permit", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void MAC68_FirewallIsolation_MessageHasNoPermitProperty()
        {
            var props = typeof(CollaborationMessage).GetProperties();
            Assert.DoesNotContain(props, p => p.Name.Contains("Permit", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void MAC69_FirewallIsolation_ServiceHasNoPermitIssuingMethods()
        {
            var methods = typeof(IMultiAgentCollaborationService).GetMethods();
            Assert.DoesNotContain(methods, m => m.Name.Contains("Permit", StringComparison.OrdinalIgnoreCase) ||
                                                m.Name.Contains("AuthorizeExecution", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void MAC70_FirewallIsolation_TeamCannotBypassExecutionCheckpoint()
        {
            var charter = new TeamCharter { CharterId = "CHT-01", Status = TeamLifecycleStatus.Operating };
            Assert.False(typeof(BusinessModelApp.Core.Domain.Execution.ExecutionPermit).IsAssignableFrom(typeof(TeamCharter)));
            Assert.NotNull(charter);
        }

        [Fact]
        public async Task MAC71_FirewallIsolation_BudgetReservationIsNotPermit()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(
                TestTenant, "WORK-01", "Budget test", CreateSampleRoles(2), budget: 500m);

            Assert.True(s);
            Assert.Equal(500m, charter!.ResourceBudget);
            // Verify budget field is purely financial ceiling, not an execution permit
            Assert.IsType<decimal>(charter.ResourceBudget);
        }

        [Fact]
        public async Task MAC72_FirewallIsolation_TeamMemberCapabilitiesRequireFirewallValidation()
        {
            var rig = CreateTestRig();
            var roles = CreateSampleRoles(2);
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Capabilities check", roles);

            Assert.True(s);
            foreach (var member in charter!.Members)
            {
                Assert.All(member.AssignedCapabilities, cap => Assert.False(string.IsNullOrWhiteSpace(cap)));
            }
        }

        // =========================================================================
        // FAMILY 13: Team Performance & Collaboration Metrology (MAC73 - MAC78)
        // =========================================================================

        [Fact]
        public async Task MAC73_Metrology_PerfectTeam_EfficiencyScore1()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Perfect flow", CreateSampleRoles(2));
            Assert.True(s);

            await rig.service.SendMessageAsync(TestTenant, charter!.CharterId, "agent-alpha", "agent-beta", CollaborationMessageType.InformationSharing, "All good");
            await rig.service.DissolveTeamAsync(TestTenant, charter.CharterId, "Complete");

            var perf = await rig.store.GetPerformanceRecordAsync(TestTenant, charter.CharterId);
            Assert.Equal(1.0, perf!.CollaborationEfficiencyScore);
            Assert.Equal(0, perf.DisputesEncountered);
        }

        [Fact]
        public async Task MAC74_Metrology_DisputedTeam_EfficiencyScoreDegrades()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Disputed flow", CreateSampleRoles(2));
            Assert.True(s);

            await rig.service.RaiseDisputeAsync(TestTenant, charter!.CharterId, DisputeType.ConflictingHypothesis, "Topic 1", "agent-alpha", new[] { "agent-beta" }, new Dictionary<string, string>());
            await rig.service.RaiseDisputeAsync(TestTenant, charter.CharterId, DisputeType.PriorityClash, "Topic 2", "agent-alpha", new[] { "agent-beta" }, new Dictionary<string, string>());

            await rig.service.DissolveTeamAsync(TestTenant, charter.CharterId, "Complete");

            var perf = await rig.store.GetPerformanceRecordAsync(TestTenant, charter.CharterId);
            Assert.Equal(2, perf!.DisputesEncountered);
            Assert.True(perf.CollaborationEfficiencyScore < 1.0);
        }

        [Fact]
        public async Task MAC75_Metrology_PerformanceRecordPreservesRoundsUsed()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Rounds check", CreateSampleRoles(2));
            Assert.True(s);

            for (int i = 0; i < 4; i++)
            {
                await rig.service.SendMessageAsync(TestTenant, charter!.CharterId, "agent-alpha", "agent-beta", CollaborationMessageType.InformationSharing, $"Msg {i}");
            }

            await rig.service.DissolveTeamAsync(TestTenant, charter!.CharterId, "Complete");

            var perf = await rig.store.GetPerformanceRecordAsync(TestTenant, charter.CharterId);
            Assert.Equal(4, perf!.RoundsUsed);
        }

        [Fact]
        public async Task MAC76_Metrology_DisputesResolvedCountAccurate()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Resolved count check", CreateSampleRoles(2));
            Assert.True(s);

            // One resolved autonomously, one escalated
            await rig.service.RaiseDisputeAsync(TestTenant, charter!.CharterId, DisputeType.PolicyInterpretation, "Policy", "agent-alpha", new[] { "agent-beta" }, new Dictionary<string, string>());
            await rig.service.DissolveTeamAsync(TestTenant, charter.CharterId, "Complete");

            var perf = await rig.store.GetPerformanceRecordAsync(TestTenant, charter.CharterId);
            Assert.Equal(1, perf!.DisputesResolved);
        }

        [Fact]
        public async Task MAC77_Metrology_FailedObjective_RecordedInPerformance()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Failed objective", CreateSampleRoles(2));
            Assert.True(s);

            await rig.service.DissolveTeamAsync(TestTenant, charter!.CharterId, "Mission Failed due to timeout");

            var perf = await rig.store.GetPerformanceRecordAsync(TestTenant, charter.CharterId);
            Assert.False(perf!.ObjectiveAchieved);
        }

        [Fact]
        public async Task MAC78_Metrology_PerformanceRecordIsolatedByTenant()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Tenant perf", CreateSampleRoles(2));
            Assert.True(s);

            await rig.service.DissolveTeamAsync(TestTenant, charter!.CharterId, "Complete");

            var perf = await rig.store.GetPerformanceRecordAsync(AltTenant, charter.CharterId);
            Assert.Null(perf);
        }

        // =========================================================================
        // FAMILY 14: End-to-End Collaboration & Resolution Flows (MAC79 - MAC84)
        // =========================================================================

        [Fact]
        public async Task MAC79_E2E_FullLifecycle_Form_Message_Dispute_Resolve_Dissolve()
        {
            var rig = CreateTestRig();
            var (formed, charter, err) = await rig.service.FormTeamAsync(
                TestTenant, "WORK-E2E", "Autonomous budget recalibration", CreateSampleRoles(3));

            Assert.True(formed);
            Assert.NotNull(charter);

            // Step 1: Exchange proposals
            var (d1, m1, _) = await rig.service.SendMessageAsync(
                TestTenant, charter.CharterId, "agent-alpha", "agent-beta",
                CollaborationMessageType.ProposalOffer, "Proposing 10% expense reduction");
            Assert.True(d1);

            // Step 2: Raise dispute with evidence
            var dispute = await rig.service.RaiseDisputeAsync(
                TestTenant, charter.CharterId, DisputeType.ConflictingHypothesis,
                "Expense reduction feasibility", "agent-alpha", new[] { "agent-beta" },
                new Dictionary<string, string>(),
                new Dictionary<string, double> { ["agent-alpha"] = 0.92, ["agent-beta"] = 0.70 });

            Assert.Equal(ArbitrationOutcome.EvidencePrevails, dispute.ResolutionOutcome);

            // Step 3: Dissolve team upon completion
            var (dissolved, _) = await rig.service.DissolveTeamAsync(TestTenant, charter.CharterId, "Successfully recalibrated");
            Assert.True(dissolved);

            var finalCharter = await rig.service.GetCharterAsync(TestTenant, charter.CharterId);
            Assert.Equal(TeamLifecycleStatus.Dissolved, finalCharter!.Status);
        }

        [Fact]
        public async Task MAC80_E2E_MultiPartyDebate_ResolvesUnderTurnLimit()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "3-party debate", CreateSampleRoles(3));
            Assert.True(s);

            await rig.service.SendMessageAsync(TestTenant, charter!.CharterId, "agent-alpha", "Broadcast", CollaborationMessageType.ProposalOffer, "Alpha proposal");
            await rig.service.SendMessageAsync(TestTenant, charter.CharterId, "agent-beta", "Broadcast", CollaborationMessageType.ProposalCounter, "Beta counter");
            await rig.service.SendMessageAsync(TestTenant, charter.CharterId, "agent-gamma", "Broadcast", CollaborationMessageType.ProposalCounter, "Gamma compromise");

            var messages = await rig.service.GetMessagesAsync(TestTenant, charter.CharterId);
            Assert.Equal(3, messages.Count);
            Assert.True(messages.All(m => m.TurnNumber <= 3));
        }

        [Fact]
        public async Task MAC81_E2E_DisputeEscalated_CharterDisputed_ManualDissolution()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(
                TestTenant, "WORK-01", "High governance work", CreateSampleRoles(2), governanceTier: 2);
            Assert.True(s);

            await rig.service.RaiseDisputeAsync(
                TestTenant, charter!.CharterId, DisputeType.ResourceContention,
                "Headcount budget clash", "agent-alpha", new[] { "agent-beta" },
                new Dictionary<string, string>());

            var disputed = await rig.service.GetCharterAsync(TestTenant, charter.CharterId);
            Assert.Equal(TeamLifecycleStatus.Disputed, disputed!.Status);

            var (dissolved, _) = await rig.service.DissolveTeamAsync(TestTenant, charter.CharterId, "Governance ordered dissolution");
            Assert.True(dissolved);
        }

        [Fact]
        public async Task MAC82_E2E_HandoffChainWithEvidence_Succeeds()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Handoff chain", CreateSampleRoles(3));
            Assert.True(s);

            var (d1, _, _) = await rig.service.SendMessageAsync(
                TestTenant, charter!.CharterId, "agent-alpha", "agent-beta",
                CollaborationMessageType.WorkHandoff, "Stage 1 output",
                evidenceRefs: new[] { "EVID-STAGE-1" });

            var (d2, _, _) = await rig.service.SendMessageAsync(
                TestTenant, charter.CharterId, "agent-beta", "agent-gamma",
                CollaborationMessageType.WorkHandoff, "Stage 2 output",
                evidenceRefs: new[] { "EVID-STAGE-2" });

            Assert.True(d1);
            Assert.True(d2);
        }

        [Fact]
        public async Task MAC83_E2E_Controller_GetCharterAndMessages_ReturnsOk()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Controller test", CreateSampleRoles(2));
            Assert.True(s);

            await rig.service.SendMessageAsync(TestTenant, charter!.CharterId, "agent-alpha", "agent-beta", CollaborationMessageType.InformationSharing, "Hello API");

            var controller = new MultiAgentCollaborationController(rig.service);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.HttpContext.Request.Headers["X-Tenant-ID"] = TestTenant;

            var charterRes = await controller.GetCharter(charter.CharterId, CancellationToken.None);
            var okCharter = Assert.IsType<OkObjectResult>(charterRes);
            var returnedCharter = Assert.IsType<TeamCharter>(okCharter.Value);
            Assert.Equal(charter.CharterId, returnedCharter.CharterId);

            var messagesRes = await controller.GetMessages(charter.CharterId, CancellationToken.None);
            var okMessages = Assert.IsType<OkObjectResult>(messagesRes);
            var returnedMsgs = Assert.IsAssignableFrom<IReadOnlyList<CollaborationMessage>>(okMessages.Value);
            Assert.Single(returnedMsgs);
        }

        [Fact]
        public async Task MAC84_E2E_Controller_DissolveCharter_ReturnsOk()
        {
            var rig = CreateTestRig();
            var (s, charter, _) = await rig.service.FormTeamAsync(TestTenant, "WORK-01", "Dissolve via API", CreateSampleRoles(2));
            Assert.True(s);

            var controller = new MultiAgentCollaborationController(rig.service);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            controller.HttpContext.Request.Headers["X-Tenant-ID"] = TestTenant;

            var result = await controller.DissolveCharter(charter!.CharterId, new MultiAgentCollaborationController.DissolveRequest { Reason = "API Completed" }, CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(ok.Value);

            var updated = await rig.service.GetCharterAsync(TestTenant, charter.CharterId);
            Assert.Equal(TeamLifecycleStatus.Dissolved, updated!.Status);
        }
    }
}
