using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Services;

namespace BusinessModelApp.Infrastructure.Services
{
    public class DeliverySwarmService : IDeliverySwarmService
    {
        private static readonly ConcurrentDictionary<Guid, DeliveryMission> _missions = new();

        public DeliverySwarmService()
        {
            if (_missions.IsEmpty)
            {
                var sampleMissionId = Guid.Parse("99999999-8888-7777-6666-555555555555");
                var sampleMission = new DeliveryMission
                {
                    Id = sampleMissionId,
                    WorkspaceId = Guid.Empty,
                    CommercialProposalQuoteId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                    ProjectTitle = "Apex Realty - Next-Gen PropTech Portal & WhatsApp CRM",
                    ClientName = "Apex Realty & Infrastructure Ltd",
                    ProjectValueINR = 125000m,
                    CurrentPhase = DeliveryPhase.ProductionDeployment,
                    OverallProgressPercentage = 80,
                    LiveDeploymentUrl = "https://apexrealty-preview.charlie-apps.io",
                    Tasks = new List<DeliveryTaskItem>
                    {
                        new() { Role = "RequirementsAnalyst", Title = "Client Requirements Specification & Sitemap", ArtifactName = "SRS-ApexRealty-v1.pdf", IsCompleted = true, CompletedAt = DateTime.UtcNow.AddHours(-10) },
                        new() { Role = "UXDesigner", Title = "Mobile-First Wireframes & Figma Prototype", ArtifactName = "Figma-ApexRealty-Prototype", IsCompleted = true, CompletedAt = DateTime.UtcNow.AddHours(-8) },
                        new() { Role = "FrontendEngineer", Title = "Next.js 14 Headless Portal & WhatsApp Widget", ArtifactName = "GitHub-apexrealty-portal", IsCompleted = true, CompletedAt = DateTime.UtcNow.AddHours(-5) },
                        new() { Role = "QAEngineer", Title = "Automated E2E Test Suite & Security Audit", ArtifactName = "QA-TestReport-100Pass", IsCompleted = true, CompletedAt = DateTime.UtcNow.AddHours(-2) },
                        new() { Role = "DevOpsEngineer", Title = "Cloudflare Edge Deployment & Custom Domain SSL", ArtifactName = "Production-Deployment-URL", IsCompleted = false },
                        new() { Role = "CustomerSuccess", Title = "Client Onboarding & Admin Training Video", ArtifactName = "Loom-AdminWalkthrough", IsCompleted = false }
                    }
                };
                _missions[sampleMissionId] = sampleMission;
            }
        }

        public Task<DeliveryMission> InitializeDeliveryMissionAsync(Guid workspaceId, Guid proposalQuoteId)
        {
            var mission = new DeliveryMission
            {
                WorkspaceId = workspaceId,
                CommercialProposalQuoteId = proposalQuoteId,
                ProjectTitle = "Autonomous Delivery: PropTech Lead Accelerator",
                ClientName = "Apex Realty Dynamics",
                ProjectValueINR = 125000m,
                CurrentPhase = DeliveryPhase.RequirementsGathering,
                OverallProgressPercentage = 15,
                Tasks = new List<DeliveryTaskItem>
                {
                    new() { Role = "RequirementsAnalyst", Title = "Autonomous Business Requirements Specification", ArtifactName = "SRS-Client-v1.pdf", IsCompleted = true, CompletedAt = DateTime.UtcNow },
                    new() { Role = "UXDesigner", Title = "Figma UI/UX Component Architecture", ArtifactName = "Figma-Design-Tokens", IsCompleted = false },
                    new() { Role = "FrontendEngineer", Title = "Headless Application Core Implementation", ArtifactName = "GitHub-Repository", IsCompleted = false },
                    new() { Role = "QAEngineer", Title = "End-to-End Test Suite & Performance Validation", ArtifactName = "Lighthouse-95-Audit", IsCompleted = false },
                    new() { Role = "DevOpsEngineer", Title = "Zero-Downtime Edge Deployment", ArtifactName = "Production-URL", IsCompleted = false },
                    new() { Role = "CustomerSuccess", Title = "Client Handover & Automated Analytics Dashboard", ArtifactName = "Client-Welcome-Pack", IsCompleted = false }
                }
            };

            _missions[mission.Id] = mission;
            return Task.FromResult(mission);
        }

        public Task<DeliveryMission> ExecuteDeliveryStepAsync(Guid deliveryMissionId)
        {
            if (!_missions.TryGetValue(deliveryMissionId, out var mission))
            {
                mission = new DeliveryMission { Id = deliveryMissionId, ProjectValueINR = 125000m };
                _missions[deliveryMissionId] = mission;
            }

            var nextIncomplete = mission.Tasks.FirstOrDefault(t => !t.IsCompleted);
            if (nextIncomplete != null)
            {
                nextIncomplete.IsCompleted = true;
                nextIncomplete.CompletedAt = DateTime.UtcNow;
            }

            int completedCount = mission.Tasks.Count(t => t.IsCompleted);
            int total = mission.Tasks.Count > 0 ? mission.Tasks.Count : 1;
            mission.OverallProgressPercentage = (int)((double)completedCount / total * 100);

            if (mission.OverallProgressPercentage >= 100)
            {
                mission.CurrentPhase = DeliveryPhase.Completed;
                mission.CompletedAt = DateTime.UtcNow;
                mission.LiveDeploymentUrl = "https://apexrealty-live.charlie-apps.io";
            }
            else if (mission.OverallProgressPercentage >= 80)
            {
                mission.CurrentPhase = DeliveryPhase.CustomerHandoverAndSuccess;
            }
            else if (mission.OverallProgressPercentage >= 60)
            {
                mission.CurrentPhase = DeliveryPhase.ProductionDeployment;
            }
            else if (mission.OverallProgressPercentage >= 40)
            {
                mission.CurrentPhase = DeliveryPhase.QAAndSecurityAudit;
            }
            else if (mission.OverallProgressPercentage >= 20)
            {
                mission.CurrentPhase = DeliveryPhase.CoreEngineering;
            }

            return Task.FromResult(mission);
        }

        public Task<DeliveryMission> GetDeliveryMissionAsync(Guid deliveryMissionId)
        {
            if (_missions.TryGetValue(deliveryMissionId, out var mission))
            {
                return Task.FromResult(mission);
            }
            return Task.FromResult(new DeliveryMission { Id = deliveryMissionId });
        }

        public Task<List<DeliveryMission>> GetActiveMissionsAsync(Guid workspaceId)
        {
            var list = _missions.Values.Where(m => m.WorkspaceId == workspaceId || m.WorkspaceId == Guid.Empty).ToList();
            return Task.FromResult(list);
        }
    }
}
