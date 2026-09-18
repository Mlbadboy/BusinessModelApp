using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Domain.Runtime.Organizational.Readiness;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Readiness;

/// <summary>
/// Contingency Planning Engine.
/// Invariant I31-K: Contingency Proposal != Approved Work. Pre-staged proposals require normal 3.9.0 admission.
/// Invariant I31-O: POR Cannot Create Execution Authority. Stages proposals into the Control Plane; never executes directly.
/// </summary>
public class ContingencyPlanningEngine : IContingencyPlanningEngine
{
    private readonly IPredictiveReadinessStore _store;
    private readonly IOrganizationalWorkRepository? _workRepo;

    public ContingencyPlanningEngine(
        IPredictiveReadinessStore store,
        IOrganizationalWorkRepository? workRepo = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _workRepo = workRepo;
    }

    public ContingencyProposal CreateContingency(
        string tenantId,
        string scenarioId,
        string title,
        string objective,
        string proposedWorkPayload,
        string activationTrigger,
        double estimatedCost,
        double reversibility)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(scenarioId)) throw new ArgumentNullException(nameof(scenarioId));

        var contingency = new ContingencyProposal
        {
            ContingencyId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            ScenarioId = scenarioId,
            Title = title ?? string.Empty,
            Objective = objective ?? string.Empty,
            ProposedWorkPayload = proposedWorkPayload ?? string.Empty,
            ActivationTriggerCondition = activationTrigger ?? string.Empty,
            EstimatedPreparationCost = Math.Max(0.0, estimatedCost),
            ReversibilityScore = Math.Clamp(reversibility, 0.0, 1.0),
            IsStagedToWorkControlPlane = false,
            CreatedUtc = DateTime.UtcNow
        };

        _store.SaveContingencyAsync(tenantId, contingency).GetAwaiter().GetResult();
        return contingency;
    }

    public async Task<WorkProposal> StageContingentWorkProposalAsync(
        string tenantId,
        string contingencyId,
        string proposerActorId = "POR-ContingencyEngine")
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(contingencyId)) throw new ArgumentNullException(nameof(contingencyId));

        var contingency = await _store.GetContingencyAsync(tenantId, contingencyId)
            ?? throw new InvalidOperationException($"Contingency '{contingencyId}' not found for tenant '{tenantId}'.");

        var proposal = new WorkProposal
        {
            ProposalId = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            SourceType = "PredictiveOrganizationalReadiness",
            SourceId = contingency.ContingencyId,
            ResponsibilityId = "RESP-READINESS-PREPAREDNESS",
            Title = $"[CONTINGENCY] {contingency.Title}",
            Objective = new WorkObjective
            {
                Statement = $"{contingency.Objective} | Payload: {contingency.ProposedWorkPayload}",
                SuccessCriteria = new List<string> { contingency.ActivationTriggerCondition }
            },
            EvidenceRefs = new List<string>
            {
                $"readiness:contingency:{contingency.ContingencyId}",
                $"scenario:{contingency.ScenarioId}"
            },
            Priority = WorkPriority.High,
            Urgency = WorkUrgencyTier.Elevated,
            RiskTier = contingency.ReversibilityScore >= 0.7 
                ? WorkRiskTier.R1_InternalReversible 
                : WorkRiskTier.R2_ExternalBounded,
            SuggestedAssignee = proposerActorId,
            AdmissionStatus = ProposalAdmissionStatus.Pending,
            CreatedUtc = DateTime.UtcNow
        };

        proposal.ComputeProvenanceHash();

        if (_workRepo != null)
        {
            await _workRepo.SaveProposalAsync(proposal);
        }

        contingency.IsStagedToWorkControlPlane = true;
        contingency.StagedWorkProposalId = proposal.ProposalId;
        await _store.SaveContingencyAsync(tenantId, contingency);

        return proposal;
    }
}
