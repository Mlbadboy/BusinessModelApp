using System;
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
    public sealed class Phase4Batch432ToolSkillFabricTests
    {
        private readonly IWorkforceConstitutionStore _constitutionStore;
        private readonly IWorkforceConstitutionService _constitutionService;
        private readonly IToolAndSkillStore _toolSkillStore;
        private readonly ToolAndSkillFabricService _fabricService;
        private readonly GovernedToolAndSkillController _controller;

        public Phase4Batch432ToolSkillFabricTests()
        {
            _constitutionStore = new InMemoryWorkforceConstitutionStore();
            _constitutionService = new WorkforceConstitutionService(_constitutionStore);

            _toolSkillStore = new InMemoryToolAndSkillStore();
            _fabricService = new ToolAndSkillFabricService(_toolSkillStore, _constitutionService);

            _controller = new GovernedToolAndSkillController(_fabricService, _fabricService);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            _controller.ControllerContext.HttpContext.Request.Headers["X-Tenant-ID"] = "tenant-toolskill-432";
        }

        // =========================================================================
        // Family 1: Governed Tool Registry & Resolution Pipeline (Law I39-D)
        // =========================================================================

        [Fact]
        public async Task TOOL432_01_ToolResolution_RequiresActiveAgentAndContract()
        {
            await _fabricService.RegisterToolAsync("tenant-toolskill-432", new GovernedTool
            {
                ToolId = "Browser.Search",
                CapabilityId = "cap-browser-search",
                RiskTier = 1,
                BudgetCostPerCall = 0.10m
            });

            // Unregistered / inactive agent tries to call tool
            var res = await _fabricService.ResolveToolExecutionAsync(new ToolExecutionResolutionRequest
            {
                TenantId = "tenant-toolskill-432",
                AgentId = "unregistered-agent",
                ToolId = "Browser.Search",
                RiskLevel = 1
            });

            res.IsAuthorized.Should().BeFalse();
            res.RejectionReason.Should().Contain("not active or lacks an employment contract");
        }

        [Fact]
        public async Task TOOL432_02_ToolResolution_EnforcesAllowedToolList()
        {
            await _fabricService.RegisterToolAsync("tenant-toolskill-432", new GovernedTool
            {
                ToolId = "CRM.DeleteAccount",
                CapabilityId = "cap-crm-delete",
                RiskTier = 3,
                BudgetCostPerCall = 1.00m
            });

            // Hire and activate an agent with limited tools
            var contract = new AgentEmploymentContract
            {
                AgentId = "agent-prospector-432",
                AgentName = "Prospector 432",
                AllowedToolIds = { "Browser.Search", "Browser.Navigate" },
                RiskCeiling = 2,
                Quota = new AgentQuotaAllocation { DailyBudgetCap = 500m }
            };
            await _constitutionService.HireAgentAsync("tenant-toolskill-432", contract);
            await _constitutionService.PromoteAgentAsync("tenant-toolskill-432", "agent-prospector-432", AgentLifecycleStatus.CERTIFIED, "GovernanceEvaluator");
            await _constitutionService.PromoteAgentAsync("tenant-toolskill-432", "agent-prospector-432", AgentLifecycleStatus.ACTIVE, "HumanSupervisor");

            // Attempt calling disallowed tool
            var res = await _fabricService.ResolveToolExecutionAsync(new ToolExecutionResolutionRequest
            {
                TenantId = "tenant-toolskill-432",
                AgentId = "agent-prospector-432",
                ToolId = "CRM.DeleteAccount",
                RiskLevel = 3
            });

            res.IsAuthorized.Should().BeFalse();
            res.RejectionReason.Should().Contain("not permitted by agent contract");
        }

        [Fact]
        public async Task TOOL432_03_ToolResolution_DeductsBudgetAndChecksRisk()
        {
            await _fabricService.RegisterToolAsync("tenant-toolskill-432", new GovernedTool
            {
                ToolId = "Browser.Search",
                CapabilityId = "cap-browser-search",
                RiskTier = 1,
                BudgetCostPerCall = 25m
            });

            var contract = new AgentEmploymentContract
            {
                AgentId = "agent-searcher-432",
                AgentName = "Searcher 432",
                AllowedToolIds = { "Browser.Search" },
                RiskCeiling = 2,
                Quota = new AgentQuotaAllocation { DailyBudgetCap = 40m, DailySpent = 0m }
            };
            await _constitutionService.HireAgentAsync("tenant-toolskill-432", contract);
            await _constitutionService.PromoteAgentAsync("tenant-toolskill-432", "agent-searcher-432", AgentLifecycleStatus.CERTIFIED, "GovernanceEvaluator");
            await _constitutionService.PromoteAgentAsync("tenant-toolskill-432", "agent-searcher-432", AgentLifecycleStatus.ACTIVE, "HumanSupervisor");

            // Call 1: 25 spent <= 40 cap -> Authorized
            var res1 = await _fabricService.ResolveToolExecutionAsync(new ToolExecutionResolutionRequest
            {
                TenantId = "tenant-toolskill-432",
                AgentId = "agent-searcher-432",
                ToolId = "Browser.Search",
                RiskLevel = 1
            });
            res1.IsAuthorized.Should().BeTrue();

            // Call 2: 25 + 25 = 50 > 40 cap -> Rejected
            var res2 = await _fabricService.ResolveToolExecutionAsync(new ToolExecutionResolutionRequest
            {
                TenantId = "tenant-toolskill-432",
                AgentId = "agent-searcher-432",
                ToolId = "Browser.Search",
                RiskLevel = 1
            });
            res2.IsAuthorized.Should().BeFalse();
            res2.RejectionReason.Should().Contain("budget exhausted");
        }

        // =========================================================================
        // Family 2: Governed Skill Registry & Self-Certification Defense (Law I39-M)
        // =========================================================================

        [Fact]
        public async Task SKILL432_04_SkillSelfCertification_IsBlockedByConstitution()
        {
            var skill = await _fabricService.RegisterSkillAsync("tenant-toolskill-432", new GovernedSkill
            {
                SkillId = "SKILL_AUTONOMOUS_PRICING",
                Owner = "Agent-Pricing-Bot",
                Description = "Dynamically adjusts customer pricing"
            });

            skill.LifecycleState.Should().Be(SkillLifecycleState.DISCOVER);

            // Generator/Owner attempts to self-certify
            var act = () => _fabricService.PromoteSkillStateAsync("tenant-toolskill-432", "SKILL_AUTONOMOUS_PRICING", SkillLifecycleState.CERTIFIED, "Agent-Pricing-Bot");

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*I39-M*Skill owner/generator cannot self-certify*");
        }

        [Fact]
        public async Task SKILL432_05_DirectActivePromotionWithoutRedTeam_IsBlocked()
        {
            var skill = await _fabricService.RegisterSkillAsync("tenant-toolskill-432", new GovernedSkill
            {
                SkillId = "SKILL_EMAIL_OUTREACH",
                Owner = "System",
                Description = "Prepares commercial email templates"
            });

            // External manager attempts direct skip from DISCOVER to ACTIVE
            var act = () => _fabricService.PromoteSkillStateAsync("tenant-toolskill-432", "SKILL_EMAIL_OUTREACH", SkillLifecycleState.ACTIVE, "GovernanceAdmin");

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*I39-M*must pass TEST, RED_TEAM, and CERTIFIED*");
        }

        [Fact]
        public async Task SKILL432_06_GovernedSkillPromotionFlow_Succeeds()
        {
            var skill = await _fabricService.RegisterSkillAsync("tenant-toolskill-432", new GovernedSkill
            {
                SkillId = "SKILL_ACCOUNT_RESEARCH",
                Owner = "Engineering",
                Description = "Gathers organizational graphs and evidence"
            });

            // 1. Advance through TEST
            await _fabricService.PromoteSkillStateAsync("tenant-toolskill-432", "SKILL_ACCOUNT_RESEARCH", SkillLifecycleState.TEST, "QaTeam");

            // 2. Advance through RED_TEAM
            await _fabricService.PromoteSkillStateAsync("tenant-toolskill-432", "SKILL_ACCOUNT_RESEARCH", SkillLifecycleState.RED_TEAM, "SecurityRedTeam");

            // 3. CERTIFIED
            await _fabricService.PromoteSkillStateAsync("tenant-toolskill-432", "SKILL_ACCOUNT_RESEARCH", SkillLifecycleState.CERTIFIED, "ChiefRiskOfficer");

            // 4. ACTIVE
            var activated = await _fabricService.PromoteSkillStateAsync("tenant-toolskill-432", "SKILL_ACCOUNT_RESEARCH", SkillLifecycleState.ACTIVE, "ChiefCommercialOfficer");
            activated.Should().BeTrue();

            var retrieved = await _fabricService.GetSkillAsync("tenant-toolskill-432", "SKILL_ACCOUNT_RESEARCH");
            retrieved!.IsActive.Should().BeTrue();
            retrieved.LifecycleState.Should().Be(SkillLifecycleState.ACTIVE);
            retrieved.CertifiedBy.Should().Be("ChiefRiskOfficer");
        }

        // =========================================================================
        // Family 3: Controller API Endpoints
        // =========================================================================

        [Fact]
        public async Task TOOL432_07_ControllerEndpoints_RegisterAndListToolsAndSkills()
        {
            var toolRes = await _controller.RegisterTool(new GovernedTool
            {
                ToolId = "Api.TestTool",
                CapabilityId = "cap-test"
            }) as OkObjectResult;
            toolRes.Should().NotBeNull();

            var listTools = await _controller.ListTools() as OkObjectResult;
            listTools.Should().NotBeNull();

            var listSkills = await _controller.ListSkills() as OkObjectResult;
            listSkills.Should().NotBeNull();
        }
    }
}
