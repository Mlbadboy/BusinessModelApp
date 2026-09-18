using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce;
using BusinessModelApp.Core.Interfaces.Runtime.Enterprise.Workforce;

namespace BusinessModelApp.Infrastructure.Runtime.Enterprise.Workforce
{
    public class BrainSpaceTelemetryService : IBrainSpaceTelemetryService
    {
        private readonly IWorkforceConstitutionService _constitutionService;
        private readonly IRevenueControlPlane _revenueControlPlane;
        private readonly IUnifiedCommunicationFabric _communicationFabric;
        private readonly IAgentHarnessService _harnessService;

        public BrainSpaceTelemetryService(
            IWorkforceConstitutionService constitutionService,
            IRevenueControlPlane revenueControlPlane,
            IUnifiedCommunicationFabric communicationFabric,
            IAgentHarnessService harnessService)
        {
            _constitutionService = constitutionService ?? throw new ArgumentNullException(nameof(constitutionService));
            _revenueControlPlane = revenueControlPlane ?? throw new ArgumentNullException(nameof(revenueControlPlane));
            _communicationFabric = communicationFabric ?? throw new ArgumentNullException(nameof(communicationFabric));
            _harnessService = harnessService ?? throw new ArgumentNullException(nameof(harnessService));
        }

        public async Task<BrainSpaceTelemetrySnapshot> CaptureSnapshotAsync(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("TenantId required", nameof(tenantId));

            var contracts = await _constitutionService.ListContractsAsync(tenantId);
            var rcpState = await _revenueControlPlane.GetCurrentStateAsync(tenantId);
            var pendingApprovals = await _communicationFabric.ListPendingApprovalsAsync(tenantId);
            var activeInstances = await _harnessService.ListActiveInstancesAsync(tenantId);

            var agentNodes = new List<BrainSpaceAgentNode>();
            foreach (var c in contracts)
            {
                var matchingInstance = activeInstances.FirstOrDefault(i => i.AgentId == c.AgentId);
                var agentPendingCount = pendingApprovals.Count(p => p.AgentId == c.AgentId);

                agentNodes.Add(new BrainSpaceAgentNode
                {
                    AgentId = c.AgentId,
                    AgentName = c.AgentName,
                    RoleTitle = c.RoleTitle,
                    Department = c.DepartmentId,
                    LifecycleStatus = c.LifecycleStatus,
                    CurrentMission = matchingInstance?.CurrentMissionId ?? "Idle",
                    DailyBudget = c.Quota.DailyBudgetCap,
                    DailySpent = c.Quota.DailySpent,
                    RiskCeiling = c.RiskCeiling,
                    HealthStatus = c.LifecycleStatus == AgentLifecycleStatus.QUARANTINED ? "QUARANTINED" : "HEALTHY",
                    PerformanceScore = 0.95m,
                    PendingApprovalCount = agentPendingCount
                });
            }

            var snapshot = new BrainSpaceTelemetrySnapshot
            {
                TenantId = tenantId,
                Timestamp = DateTime.UtcNow,
                SystemTelemetry = new SystemTelemetryData
                {
                    ActiveAgentCount = activeInstances.Count,
                    CpuUsagePercent = 12.0 + (activeInstances.Count * 1.5),
                    MemoryUsageMb = 250.0 + (activeInstances.Count * 18.0),
                    ActiveQueueDepth = pendingApprovals.Count,
                    AverageLatencyMs = 210.0
                },
                BusinessTelemetry = new BusinessTelemetryData
                {
                    QualifiedPipeline = rcpState.QualifiedPipelineAmount,
                    WeightedPipeline = rcpState.WeightedPipelineAmount,
                    ClosedWonRevenue = rcpState.ClosedWonRevenue,
                    InvoicedRevenue = rcpState.InvoicedRevenue,
                    CollectedCashRevenue = rcpState.CollectedCashRevenue,
                    RealizedGrossMargin = rcpState.RealizedGrossMargin,
                    NetContribution = rcpState.RealizedGrossMargin,
                    ActiveCommercialMissions = rcpState.ActiveCommercialMissionsCount
                },
                AgentNodes = agentNodes
            };

            return snapshot;
        }
    }
}
