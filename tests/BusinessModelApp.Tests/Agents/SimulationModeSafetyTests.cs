using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Agents;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Interfaces;
using Moq;
using Xunit;

namespace BusinessModelApp.Tests.Agents
{
    public class SimulationModeSafetyTests
    {
        [Fact]
        public async Task Simulation_Mode_Never_Mutates_Real_CRM_And_Returns_Synthetic_Identifiers()
        {
            // Arrange
            var mockCommercialRepo = new Mock<ICommercialRepository>();
            var toolRegistry = new GovernedToolRegistry(mockCommercialRepo.Object);

            var mission = new AgentMission
            {
                WorkspaceId = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                Mode = MissionMode.Simulation,
                AutonomyLevel = AutonomyLevel.Level3_ControlledAutonomy,
                Wallet = MissionWallet.CreateDefault(1000m)
            };

            var qualificationAgent = AgentIdentity.Create(AgentRole.LeadQualification);
            var parameters = new Dictionary<string, object>
            {
                { "company", "Synthetic Demo Bank" }
            };

            // Act
            var result = await toolRegistry.ExecuteToolAsync(qualificationAgent, AgentActionType.CreateLeadInCRM, mission, parameters);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("SIM-LEAD-", result.OutputJson);
            Assert.Contains("\"synthetic\":true", result.OutputJson);

            // Real DB repo was NEVER called
            mockCommercialRepo.Verify(r => r.CreateLeadAsync(It.IsAny<Lead>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
