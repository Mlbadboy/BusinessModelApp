using BusinessModelApp.Core.Agents;
using Xunit;

namespace BusinessModelApp.Tests.Agents
{
    public class AgentPolicyEngineTests
    {
        private readonly AgentPolicyEngine _policyEngine = new AgentPolicyEngine();

        [Fact]
        public void Level3_ControlledAutonomy_Allows_Research_And_Outreach_Autonomously()
        {
            var researchAgent = AgentIdentity.Create(AgentRole.CompanyResearch);
            var outreachAgent = AgentIdentity.Create(AgentRole.Outreach);

            var researchDecision = _policyEngine.Evaluate(researchAgent, AgentActionType.ResearchCompany, AutonomyLevel.Level3_ControlledAutonomy);
            Assert.True(researchDecision.IsAllowed);
            Assert.False(researchDecision.RequiresApproval);

            var outreachDecision = _policyEngine.Evaluate(outreachAgent, AgentActionType.SendOutreach, AutonomyLevel.Level3_ControlledAutonomy);
            Assert.True(outreachDecision.IsAllowed);
            Assert.False(outreachDecision.RequiresApproval);
        }

        [Fact]
        public void Level3_Requires_Human_Approval_For_High_Value_Proposals_And_Discounts()
        {
            var proposalAgent = AgentIdentity.Create(AgentRole.ProposalGeneration);

            var proposalDecision = _policyEngine.Evaluate(proposalAgent, AgentActionType.GenerateProposal, AutonomyLevel.Level3_ControlledAutonomy, monetaryImpactINR: 1500000m);
            Assert.False(proposalDecision.IsAllowed);
            Assert.True(proposalDecision.RequiresApproval);
            Assert.Equal(4, proposalDecision.RiskScore);

            var discountDecision = _policyEngine.Evaluate(proposalAgent, AgentActionType.ProposeDiscount, AutonomyLevel.Level3_ControlledAutonomy, monetaryImpactINR: 200000m);
            Assert.False(discountDecision.IsAllowed);
            Assert.True(discountDecision.RequiresApproval);
        }

        [Fact]
        public void Destructive_Actions_Are_Always_Gated_Regardless_Of_Autonomy_Level()
        {
            // Even if an agent erroneously claims capability, destructive action requires human authorization
            var adminIdentity = new AgentIdentity
            {
                Role = AgentRole.CommercialCloser,
                Name = "Closer",
                PermittedActions = new System.Collections.Generic.HashSet<AgentActionType> { AgentActionType.DeleteData }
            };

            var deleteDecision = _policyEngine.Evaluate(adminIdentity, AgentActionType.DeleteData, AutonomyLevel.Level4_AutonomousOperations);
            Assert.False(deleteDecision.IsAllowed);
            Assert.True(deleteDecision.RequiresApproval);
            Assert.Equal(5, deleteDecision.RiskScore);
        }
    }
}
