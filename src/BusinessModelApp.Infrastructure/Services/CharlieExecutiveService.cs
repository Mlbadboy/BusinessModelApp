using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Agents;
using BusinessModelApp.Core.Constitution;
using BusinessModelApp.Core.Decisions;
using BusinessModelApp.Core.Domain.Decisions;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Objectives;
using BusinessModelApp.Core.Domain.Strategy;
using BusinessModelApp.Core.Domain.WorldModel;
using BusinessModelApp.Core.Missions;
using BusinessModelApp.Core.Objectives;
using BusinessModelApp.Core.Services;
using BusinessModelApp.Core.Strategy;
using BusinessModelApp.Core.WorldModel;
using BusinessModelApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessModelApp.Infrastructure.Services
{
    public class CharlieExecutiveService : ICharlieExecutiveService
    {
        private readonly AppDbContext _dbContext;
        private readonly ICompanyWorldModel _worldModel;
        private readonly IObjectiveEngine _objectiveEngine;
        private readonly ICommercialStrategyEngine _strategyEngine;
        private readonly IConstitutionPolicyEngine _constitutionEngine;
        private readonly IDecisionEngine _decisionEngine;
        private readonly IDurableMissionOrchestrator _missionOrchestrator;
        private readonly IAgentRuntime _agentRuntime;
        private readonly ILogger<CharlieExecutiveService> _logger;

        public CharlieExecutiveService(
            AppDbContext dbContext,
            ICompanyWorldModel worldModel,
            IObjectiveEngine objectiveEngine,
            ICommercialStrategyEngine strategyEngine,
            IConstitutionPolicyEngine constitutionEngine,
            IDecisionEngine decisionEngine,
            IDurableMissionOrchestrator missionOrchestrator,
            IAgentRuntime agentRuntime,
            ILogger<CharlieExecutiveService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _worldModel = worldModel ?? throw new ArgumentNullException(nameof(worldModel));
            _objectiveEngine = objectiveEngine ?? throw new ArgumentNullException(nameof(objectiveEngine));
            _strategyEngine = strategyEngine ?? throw new ArgumentNullException(nameof(strategyEngine));
            _constitutionEngine = constitutionEngine ?? throw new ArgumentNullException(nameof(constitutionEngine));
            _decisionEngine = decisionEngine ?? throw new ArgumentNullException(nameof(decisionEngine));
            _missionOrchestrator = missionOrchestrator ?? throw new ArgumentNullException(nameof(missionOrchestrator));
            _agentRuntime = agentRuntime ?? throw new ArgumentNullException(nameof(agentRuntime));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CharlieExecutivePipelineResult> ProcessExecutiveMandateAsync(
            string ceoPrompt,
            Guid workspaceId,
            CancellationToken ct = default)
        {
            _logger.LogInformation("[CharlieExecutiveService] Processing CEO mandate: \"{Prompt}\" for Workspace {WorkspaceId}",
                ceoPrompt, workspaceId);

            // Step 1: Parse & Ingest Objective
            var objective = await _objectiveEngine.IngestCeoPromptAsync(ceoPrompt, workspaceId, ct);

            // Step 2: Build Company World Model
            var snapshot = await _worldModel.CaptureVerifiedSnapshotAsync(workspaceId, ct);
            objective = await _objectiveEngine.ReconcileObjectiveProgressAsync(objective.Id, snapshot, ct);

            // Step 3: Four-State Revenue Baseline
            var baseline = new RevenueBaseline
            {
                WorkspaceId = workspaceId,
                ObjectiveId = objective.Id,
                State = snapshot.RevenueBaselineState,
                ContractedGuaranteedRevenueINR = snapshot.VerifiedRevenueINR,
                WeightedPipelineRevenueINR = snapshot.QualifiedPipelineINR,
                HistoricalRunRateRevenueINR = TruthMetric<decimal>.Unavailable("No recurring run-rate recorded.")
            };
            baseline.ComputeAutonomousGap(objective.TargetRevenueINR);
            snapshot.RevenueBaseline = baseline;

            // Step 4: Formulate Strategies & Simulations
            var strategyCandidates = await _strategyEngine.FormulateStrategiesAsync(objective, snapshot, ct);
            var selectedCandidate = strategyCandidates.FirstOrDefault(c => c.IsRecommended) ?? strategyCandidates.First();

            // Step 5: Hive Delegation & Stigmergy (Simulated preparation only)
            var hive = new BusinessHive(workspaceId);
            var marketAgent = hive.RegisterAgent(AgentRole.MarketIntelligence);
            var prospectAgent = hive.RegisterAgent(AgentRole.ProspectDiscovery);
            var blackboard = hive.GetOrCreateBlackboard(Guid.NewGuid(), objective.Id, objective.Title, objective.TargetRevenueINR);

            // Share known facts
            blackboard.PostFact("RevenueBaseline", $"Baseline State is {snapshot.RevenueBaselineState} with gap of ₹{baseline.AutonomousRevenueGapINR.Value:N0}", snapshot.Id, snapshot.SnapshotDigestHash);

            // Run simulated prep task via AgentRuntime
            await _agentRuntime.ExecuteStepAsync(
                marketAgent,
                blackboard,
                AgentActionType.ResearchCompany,
                $"Map target ICP accounts for '{selectedCandidate.Name}'",
                estimatedCostINR: 500m,
                ct: ct);

            // Step 6: Commit Immutable DecisionRecord
            var businessStrategy = new BusinessStrategy
            {
                ObjectiveId = objective.Id,
                StrategyName = selectedCandidate.Name,
                TargetACV_INR = selectedCandidate.RequiredFunnel.TargetACVINR.Value,
                ExpectedWinRate = selectedCandidate.RequiredFunnel.WinRate.Value,
                RequiredClosedDeals = selectedCandidate.RequiredFunnel.RequiredClosedDeals,
                RequiredPipelineINR = selectedCandidate.RequiredFunnel.RequiredPipelineINR,
                RequiredQualifiedOppsCount = selectedCandidate.RequiredFunnel.RequiredOpportunities,
                RequiredMeetingsCount = selectedCandidate.RequiredFunnel.RequiredConversations,
                RequiredProspectsCount = selectedCandidate.RequiredFunnel.RequiredTargetAccounts,
                DeliveryCapacitySlotsRequired = selectedCandidate.RequiredDeliveryCapacitySlots,
                FeasibilityState = selectedCandidate.Feasibility switch
                {
                    StrategyFeasibilityClassification.FeasibleEvidenced => StrategyFeasibilityState.Feasible,
                    StrategyFeasibilityClassification.HypotheticalUnverified => StrategyFeasibilityState.EvidenceInsufficient,
                    StrategyFeasibilityClassification.UnfeasibleResourceConstrained => StrategyFeasibilityState.CapacityBlocked,
                    StrategyFeasibilityClassification.UnfeasibleConstitutionalViolation => StrategyFeasibilityState.PolicyBlocked,
                    _ => StrategyFeasibilityState.EvidenceInsufficient
                },
                FeasibilityReason = selectedCandidate.FeasibilityReport.SummaryText,
                AssumptionsJson = JsonSerializer.Serialize(selectedCandidate.Assumptions),
                StrategicRationale = selectedCandidate.FeasibilityReport.SummaryText
            };
            await _dbContext.BusinessStrategies.AddAsync(businessStrategy, ct);
            await _dbContext.SaveChangesAsync(ct);

            var competingStrategies = strategyCandidates
                .Where(c => c.StrategyId != selectedCandidate.StrategyId)
                .Select(c => new BusinessStrategy { StrategyName = c.Name, ExpectedGrossMarginPercent = 60m })
                .ToList();

            var decisionRecord = await _decisionEngine.CommitStrategyDecisionAsync(
                objective,
                businessStrategy,
                competingStrategies,
                snapshot,
                null,
                ct);

            // Populate additional Phase 1 v1.2 audit fields
            decisionRecord.CEOObjective = ceoPrompt;
            decisionRecord.WorldModelSnapshotId = snapshot.Id;
            decisionRecord.SelectedStrategyName = selectedCandidate.Name;
            decisionRecord.StrategyCandidatesJson = JsonSerializer.Serialize(strategyCandidates.Select(s => new { s.Name, s.ExpectedRevenueINR, s.Feasibility, s.ConfidenceScore }));
            decisionRecord.AssumptionsJson = JsonSerializer.Serialize(selectedCandidate.Assumptions);
            decisionRecord.FeasibilityResultJson = JsonSerializer.Serialize(selectedCandidate.FeasibilityReport);
            decisionRecord.ComputeCryptographicHash();

            // Step 7: Create Durable Mission State Machine
            var mission = await _missionOrchestrator.CreateDurableMissionPlanAsync(decisionRecord, businessStrategy, ct);

            // Advance through 12-state sequence
            mission.TransitionTo(DurableMissionState.WorldModelBuilt);
            mission.TransitionTo(DurableMissionState.RevenueBaselineCalculated);
            mission.TransitionTo(DurableMissionState.StrategiesGenerated);
            mission.TransitionTo(DurableMissionState.SimulationComplete);
            mission.TransitionTo(DurableMissionState.FeasibilityChecked);
            mission.TransitionTo(DurableMissionState.ConstitutionValidated);
            mission.TransitionTo(DurableMissionState.DecisionRecorded);

            if (decisionRecord.HumanApprovalRequired)
            {
                mission.TransitionTo(DurableMissionState.ExecutiveApprovalRequired, "Requires human approval due to unverified market assumptions or budget threshold.");
            }
            else
            {
                mission.TransitionTo(DurableMissionState.ExecutiveApproved);
                mission.TransitionTo(DurableMissionState.MissionPrepared);
                mission.TransitionTo(DurableMissionState.ReadyForExecution);
            }

            await _dbContext.SaveChangesAsync(ct);

            // Summary formatting
            string summary = $"Objective '{objective.Title}' processed. Selected Strategy: '{selectedCandidate.Name}' (Feasibility: {selectedCandidate.Feasibility}). Autonomous Gap: ₹{baseline.AutonomousRevenueGapINR.Value:N0}. Mission State: {mission.State}. Cryptographic Hash: {decisionRecord.CryptographicHash[..12]}...";

            _logger.LogInformation("[CharlieExecutiveService] Successfully completed executive pipeline. {Summary}", summary);

            return new CharlieExecutivePipelineResult
            {
                Objective = objective,
                Snapshot = snapshot,
                RevenueBaseline = baseline,
                SelectedStrategy = selectedCandidate,
                DecisionRecord = decisionRecord,
                Mission = mission,
                ExecutionWallEnforced = true,
                ExecutiveSummary = summary
            };
        }
    }
}
