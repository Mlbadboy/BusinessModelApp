using BusinessModelApp.Core.Agents;
using Xunit;

namespace BusinessModelApp.Tests.Agents
{
    public class AgentIdentityTests
    {
        [Fact]
        public void MarketIntelligenceAgent_Has_Strict_Read_And_Research_Scope()
        {
            var agent = AgentIdentity.Create(AgentRole.MarketIntelligence);

            Assert.True(agent.CanPerform(AgentActionType.SearchWeb));
            Assert.True(agent.CanPerform(AgentActionType.ResearchCompany));

            // Strictly denied actions
            Assert.False(agent.CanPerform(AgentActionType.SendOutreach));
            Assert.False(agent.CanPerform(AgentActionType.CreateOpportunityInCRM));
            Assert.False(agent.CanPerform(AgentActionType.ProposeDiscount));
            Assert.False(agent.CanPerform(AgentActionType.DeleteData));
        }

        [Fact]
        public void ProposalAgent_Cannot_Send_Contracts_Or_Delete_Data()
        {
            var agent = AgentIdentity.Create(AgentRole.ProposalGeneration);

            Assert.True(agent.CanPerform(AgentActionType.GenerateProposal));
            Assert.True(agent.CanPerform(AgentActionType.ProposeDiscount));
            Assert.True(agent.CanPerform(AgentActionType.CreateOpportunityInCRM));

            Assert.False(agent.CanPerform(AgentActionType.SendContract));
            Assert.False(agent.CanPerform(AgentActionType.DeleteData));
        }
    }
}
