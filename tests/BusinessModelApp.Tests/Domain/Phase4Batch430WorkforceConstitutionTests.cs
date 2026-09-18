using System;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Api.Controllers;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;
using BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BusinessModelApp.Tests.Domain
{
    public sealed class Phase4Batch430WorkforceConstitutionTests
    {
        private readonly IWorkforceConstitutionStore _store;
        private readonly IWorkforceConstitutionService _service;
        private readonly WorkforceConstitutionController _controller;

        public Phase4Batch430WorkforceConstitutionTests()
        {
            _store = new InMemoryWorkforceConstitutionStore();
            _service = new WorkforceConstitutionService(_store);
            _controller = new WorkforceConstitutionController(_service);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-workforce-430";
        }

        // =========================================================================
        // Family 1: Constitutional Invariants & Law I39 (I39-A through I39-Z)
        // =========================================================================

        [Fact]
        public void WF430_01_ConstitutionalInvariantI39_AxiomIsExact()
        {
            WorkforceConstitutionalInvariants.Axiom.Should().Be(
                "AGENT != ROLE != SKILL != TOOL != RESPONSIBILITY != AUTHORITY != POLICY != MISSION != EXECUTION != OUTCOME");
        }

        [Fact]
        public void WF430_02_ConstitutionalInvariantI39_ContainsAll26SubLaws()
        {
            WorkforceConstitutionalInvariants.AllLaws.Should().HaveCount(26);
            WorkforceConstitutionalInvariants.LawI39A_IdentityNotAuthority.Should().StartWith("I39-A");
            WorkforceConstitutionalInvariants.LawI39B_RoleNotExecutionPermission.Should().StartWith("I39-B");
            WorkforceConstitutionalInvariants.LawI39C_SkillNotAuthority.Should().StartWith("I39-C");
            WorkforceConstitutionalInvariants.LawI39D_ToolNotAuthorization.Should().StartWith("I39-D");
            WorkforceConstitutionalInvariants.LawI39E_WalletNotAuthority.Should().StartWith("I39-E");
            WorkforceConstitutionalInvariants.LawI39F_TrustNotTruth.Should().StartWith("I39-F");
            WorkforceConstitutionalInvariants.LawI39G_PerformanceNotPolicy.Should().StartWith("I39-G");
            WorkforceConstitutionalInvariants.LawI39H_ManagerNotGovernance.Should().StartWith("I39-H");
            WorkforceConstitutionalInvariants.LawI39I_CeoNotHumanApproval.Should().StartWith("I39-I");
            WorkforceConstitutionalInvariants.LawI39J_DelegationNotSilentTransfer.Should().StartWith("I39-J");
            WorkforceConstitutionalInvariants.LawI39K_HiringNotAutomaticActivation.Should().StartWith("I39-K");
            WorkforceConstitutionalInvariants.LawI39L_PromotionRequiresGovernance.Should().StartWith("I39-L");
            WorkforceConstitutionalInvariants.LawI39M_SkillsCannotSelfCertify.Should().StartWith("I39-M");
            WorkforceConstitutionalInvariants.LawI39N_PoliciesCannotSelfPromote.Should().StartWith("I39-N");
            WorkforceConstitutionalInvariants.LawI39O_RuntimesCannotIssuePermits.Should().StartWith("I39-O");
            WorkforceConstitutionalInvariants.LawI39P_AgentsCannotModifyFirewall.Should().StartWith("I39-P");
            WorkforceConstitutionalInvariants.LawI39Q_AgentsCannotModifyTruth.Should().StartWith("I39-Q");
            WorkforceConstitutionalInvariants.LawI39R_AgentsCannotIncreaseOara.Should().StartWith("I39-R");
            WorkforceConstitutionalInvariants.LawI39S_AgentsCannotCreateSecondRuntime.Should().StartWith("I39-S");
            WorkforceConstitutionalInvariants.LawI39T_TrajectoryIsEvidenceNotTruth.Should().StartWith("I39-T");
            WorkforceConstitutionalInvariants.LawI39U_BoundedSubAgentSpawning.Should().StartWith("I39-U");
            WorkforceConstitutionalInvariants.LawI39V_UnknownEffectReconciliation.Should().StartWith("I39-V");
            WorkforceConstitutionalInvariants.LawI39W_ContinuousCyclesDurableAndBounded.Should().StartWith("I39-W");
            WorkforceConstitutionalInvariants.LawI39X_EvidenceGroundedCommercialClaims.Should().StartWith("I39-X");
            WorkforceConstitutionalInvariants.LawI39Y_CryptographicRevenueLineage.Should().StartWith("I39-Y");
            WorkforceConstitutionalInvariants.LawI39Z_FailClosedWorkforceSandboxing.Should().StartWith("I39-Z");
        }

        // =========================================================================
        // Family 2: Business & Revenue Objectives Plane
        // =========================================================================

        [Fact]
        public async Task WF430_03_CreateRevenueObjective_StoresWithDefaultDraftStatus()
        {
            var revObj = new RevenueObjective
            {
                Title = "Q4 Enterprise Growth",
                TargetRevenue = 1000000m,
                TargetMarginPercent = 45m,
                MaxCustomerAcquisitionCost = 40000m,
                MinimumDealSize = 75000m,
                TargetPeriod = "Q4-2026",
                TargetIndustries = { "SaaS", "FinTech", "HealthTech" },
                TargetGeographies = { "IN", "US", "AE" },
                MaximumRiskLevel = 2,
                RequiredEvidenceLevel = "CORROBORATED"
            };

            var created = await _service.CreateRevenueObjectiveAsync("tenant-workforce-430", revObj);

            created.Should().NotBeNull();
            created.Status.Should().Be(ObjectiveStatus.DRAFT);
            created.Metrics.Should().Contain(m => m.MetricName == "TargetRevenue" && m.TargetValue == 1000000m);
        }

        [Fact]
        public async Task WF430_04_ApproveObjective_TransitionsDraftToActive()
        {
            var obj = new BusinessObjective
            {
                Title = "B2B Outreach Scale",
                Description = "Expand prospect qualification pipeline"
            };
            var created = await _service.CreateObjectiveAsync("tenant-workforce-430", obj);
            created.Status.Should().Be(ObjectiveStatus.DRAFT);

            var approved = await _service.ApproveObjectiveAsync("tenant-workforce-430", created.ObjectiveId, "HumanSupervisor-01");
            approved.Should().BeTrue();

            var retrieved = await _service.GetObjectiveAsync("tenant-workforce-430", created.ObjectiveId);
            retrieved.Should().NotBeNull();
            retrieved!.Status.Should().Be(ObjectiveStatus.ACTIVE);
            retrieved.ApprovedBy.Should().Be("HumanSupervisor-01");
        }

        // =========================================================================
        // Family 3: Organization & Role Hierarchy
        // =========================================================================

        [Fact]
        public async Task WF430_05_DefineOrganizationDepartmentsAndTeams()
        {
            var dept = await _service.AddDepartmentAsync("tenant-workforce-430", new WorkforceDepartment
            {
                Name = "Revenue",
                Description = "Commercial Operations, Prospecting, and Sales"
            });

            var team = await _service.AddTeamAsync("tenant-workforce-430", new WorkforceTeam
            {
                DepartmentId = dept.DepartmentId,
                Name = "Prospecting"
            });

            var role = await _service.DefineRoleAsync("tenant-workforce-430", new WorkforceRole
            {
                Title = "RevenueProspector",
                DepartmentId = dept.DepartmentId,
                StandardResponsibilities = { "CompanyDiscovery", "AccountResearch", "EvidenceCollection" },
                PermittedSkillIds = { "SALES_PROSPECT_RESEARCH", "WEB_AUDIT" },
                PermittedToolIds = { "Browser.Search", "Browser.Navigate", "CRM.Read" },
                DefaultRiskCeiling = 2
            });

            var org = await _service.GetOrganizationAsync("tenant-workforce-430");
            org.Departments.Should().Contain(d => d.DepartmentId == dept.DepartmentId);
            dept.Teams.Should().Contain(t => t.TeamId == team.TeamId);
            org.DefinedRoles.Should().Contain(r => r.RoleId == role.RoleId);
        }

        // =========================================================================
        // Family 4: Agent Employment Contract & Governance (Law I39-K, I39-L, I39-Z)
        // =========================================================================

        [Fact]
        public async Task WF430_06_HireAgent_StartsInEvaluatingState_NotActive()
        {
            var contract = new AgentEmploymentContract
            {
                AgentId = "agent-prospector-01",
                AgentName = "Prospector One",
                RoleTitle = "RevenueProspector",
                AutonomyLevel = AgentAutonomyLevel.L2_SUPERVISED,
                RiskCeiling = 2,
                Quota = new AgentQuotaAllocation { DailyBudgetCap = 2500m }
            };

            var hired = await _service.HireAgentAsync("tenant-workforce-430", contract);

            hired.LifecycleStatus.Should().Be(AgentLifecycleStatus.EVALUATING);
            hired.IsActive.Should().BeFalse(); // Law I39-K
        }

        [Fact]
        public async Task WF430_07_SelfPromotion_IsRejectedByConstitution()
        {
            var contract = new AgentEmploymentContract
            {
                AgentId = "agent-prospector-02",
                AgentName = "Prospector Two",
                RoleTitle = "RevenueProspector"
            };
            await _service.HireAgentAsync("tenant-workforce-430", contract);

            // Agent attempts to promote itself
            var act = () => _service.PromoteAgentAsync("tenant-workforce-430", "agent-prospector-02", AgentLifecycleStatus.ACTIVE, "agent-prospector-02");

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*I39-L*Agent cannot promote itself*");
        }

        [Fact]
        public async Task WF430_08_DirectActivePromotionWithoutCertification_IsRejected()
        {
            var contract = new AgentEmploymentContract
            {
                AgentId = "agent-prospector-03",
                AgentName = "Prospector Three",
                RoleTitle = "RevenueProspector"
            };
            await _service.HireAgentAsync("tenant-workforce-430", contract);

            // External manager attempts to promote directly from EVALUATING to ACTIVE without CERTIFIED
            var act = () => _service.PromoteAgentAsync("tenant-workforce-430", "agent-prospector-03", AgentLifecycleStatus.ACTIVE, "HumanSupervisor");

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*I39-K/L*must pass CERTIFIED stage*");
        }

        [Fact]
        public async Task WF430_09_GovernedPromotionFlow_Succeeds()
        {
            var contract = new AgentEmploymentContract
            {
                AgentId = "agent-prospector-04",
                AgentName = "Prospector Four",
                RoleTitle = "RevenueProspector"
            };
            await _service.HireAgentAsync("tenant-workforce-430", contract);

            // 1. Promote to CERTIFIED by Governance
            var certified = await _service.PromoteAgentAsync("tenant-workforce-430", "agent-prospector-04", AgentLifecycleStatus.CERTIFIED, "GovernanceEvaluator");
            certified.Should().BeTrue();

            // 2. Promote to ACTIVE
            var activated = await _service.PromoteAgentAsync("tenant-workforce-430", "agent-prospector-04", AgentLifecycleStatus.ACTIVE, "HumanSupervisor");
            activated.Should().BeTrue();

            var updated = await _service.GetContractAsync("tenant-workforce-430", "agent-prospector-04");
            updated!.IsActive.Should().BeTrue();
            updated.LifecycleStatus.Should().Be(AgentLifecycleStatus.ACTIVE);
            updated.CertifiedBy.Should().Be("HumanSupervisor");
        }

        [Fact]
        public async Task WF430_10_ForbiddenActions_AreRejected()
        {
            var contract = new AgentEmploymentContract
            {
                AgentId = "agent-prospector-05",
                AgentName = "Prospector Five",
                RoleTitle = "RevenueProspector",
                RiskCeiling = 2,
                Quota = new AgentQuotaAllocation { DailyBudgetCap = 1000m }
            };
            await _service.HireAgentAsync("tenant-workforce-430", contract);
            await _service.PromoteAgentAsync("tenant-workforce-430", "agent-prospector-05", AgentLifecycleStatus.CERTIFIED, "GovernanceEvaluator");
            await _service.PromoteAgentAsync("tenant-workforce-430", "agent-prospector-05", AgentLifecycleStatus.ACTIVE, "HumanSupervisor");

            // Attempt forbidden action: PaymentInitiation
            var paymentAllowed = await _service.ValidateAgentActionAsync("tenant-workforce-430", "agent-prospector-05", "PaymentInitiation", 1);
            paymentAllowed.Should().BeFalse();

            // Attempt forbidden action: SelfBudgetIncrease
            var budgetAllowed = await _service.ValidateAgentActionAsync("tenant-workforce-430", "agent-prospector-05", "SelfBudgetIncrease", 1);
            budgetAllowed.Should().BeFalse();

            // Permitted action: SearchWeb
            var searchAllowed = await _service.ValidateAgentActionAsync("tenant-workforce-430", "agent-prospector-05", "SearchWeb", 1);
            searchAllowed.Should().BeTrue();
        }

        [Fact]
        public async Task WF430_11_RiskCeilingAndBudgetExhaustion_AreRejected()
        {
            var contract = new AgentEmploymentContract
            {
                AgentId = "agent-prospector-06",
                AgentName = "Prospector Six",
                RoleTitle = "RevenueProspector",
                RiskCeiling = 2,
                Quota = new AgentQuotaAllocation { DailyBudgetCap = 500m, DailySpent = 0m }
            };
            await _service.HireAgentAsync("tenant-workforce-430", contract);
            await _service.PromoteAgentAsync("tenant-workforce-430", "agent-prospector-06", AgentLifecycleStatus.CERTIFIED, "GovernanceEvaluator");
            await _service.PromoteAgentAsync("tenant-workforce-430", "agent-prospector-06", AgentLifecycleStatus.ACTIVE, "HumanSupervisor");

            // Exceeds risk ceiling (Risk 3 > RiskCeiling 2)
            var riskAllowed = await _service.ValidateAgentActionAsync("tenant-workforce-430", "agent-prospector-06", "SearchWeb", 3);
            riskAllowed.Should().BeFalse();

            // Deduct budget
            var spent300 = await _service.DeductAgentBudgetAsync("tenant-workforce-430", "agent-prospector-06", 300m);
            spent300.Should().BeTrue();

            // Spend another 300 (Total 600 > Cap 500) -> Rejected
            var spentOver = await _service.DeductAgentBudgetAsync("tenant-workforce-430", "agent-prospector-06", 300m);
            spentOver.Should().BeFalse();
        }

        [Fact]
        public async Task WF430_12_Quarantine_ImmediatelyRevokesAllActionRights()
        {
            var contract = new AgentEmploymentContract
            {
                AgentId = "agent-prospector-07",
                AgentName = "Prospector Seven",
                RoleTitle = "RevenueProspector",
                RiskCeiling = 2
            };
            await _service.HireAgentAsync("tenant-workforce-430", contract);
            await _service.PromoteAgentAsync("tenant-workforce-430", "agent-prospector-07", AgentLifecycleStatus.CERTIFIED, "GovernanceEvaluator");
            await _service.PromoteAgentAsync("tenant-workforce-430", "agent-prospector-07", AgentLifecycleStatus.ACTIVE, "HumanSupervisor");

            // Verify active
            var before = await _service.ValidateAgentActionAsync("tenant-workforce-430", "agent-prospector-07", "SearchWeb", 1);
            before.Should().BeTrue();

            // Quarantine agent
            var quarantined = await _service.QuarantineAgentAsync("tenant-workforce-430", "agent-prospector-07", "Suspicious prompt exfiltration attempt");
            quarantined.Should().BeTrue();

            // Verify action is now revoked
            var after = await _service.ValidateAgentActionAsync("tenant-workforce-430", "agent-prospector-07", "SearchWeb", 1);
            after.Should().BeFalse();
        }

        // =========================================================================
        // Family 5: Controller API Endpoints
        // =========================================================================

        [Fact]
        public async Task WF430_13_ControllerEndpoints_ReturnExpectedContractsAndInvariants()
        {
            var invariantsResult = _controller.GetConstitutionalInvariants() as OkObjectResult;
            invariantsResult.Should().NotBeNull();

            var hireResult = await _controller.HireAgent(new AgentEmploymentContract
            {
                AgentId = "agent-api-01",
                AgentName = "API Agent",
                RoleTitle = "Research"
            }) as OkObjectResult;
            hireResult.Should().NotBeNull();

            var listResult = await _controller.ListContracts() as OkObjectResult;
            listResult.Should().NotBeNull();
        }
    }
}
