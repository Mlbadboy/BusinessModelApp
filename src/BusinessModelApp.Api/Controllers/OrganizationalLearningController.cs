using BusinessModelApp.Core.Domain.Runtime.Organizational.Learning;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Learning;
using Microsoft.AspNetCore.Mvc;

namespace BusinessModelApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OrganizationalLearningController : ControllerBase
{
    private readonly IOrganizationalLearningService _learningService;
    private readonly IAdaptationTargetRegistry _targetRegistry;

    public OrganizationalLearningController(
        IOrganizationalLearningService learningService,
        IAdaptationTargetRegistry targetRegistry)
    {
        _learningService = learningService ?? throw new ArgumentNullException(nameof(learningService));
        _targetRegistry = targetRegistry ?? throw new ArgumentNullException(nameof(targetRegistry));
    }

    private string ResolveTenantId()
    {
        if (Request.Headers.TryGetValue("X-Tenant-ID", out var tenantVal) && !string.IsNullOrWhiteSpace(tenantVal))
        {
            return tenantVal.ToString();
        }
        return "default";
    }

    [HttpPost("outcomes")]
    public async Task<IActionResult> RecordOutcome([FromBody] EmpiricalOutcomeEvent outcome)
    {
        var tenantId = ResolveTenantId();
        var recorded = await _learningService.RecordEmpiricalOutcomeAsync(tenantId, outcome);
        return Ok(recorded);
    }

    [HttpGet("outcomes")]
    public async Task<IActionResult> GetOutcomes([FromQuery] string? domain = null)
    {
        var tenantId = ResolveTenantId();
        var outcomes = await _learningService.GetOutcomesAsync(tenantId, domain);
        return Ok(outcomes);
    }

    [HttpPost("metrology/evaluate")]
    public async Task<IActionResult> EvaluateMetrology([FromBody] EvaluateMetrologyRequest request)
    {
        var tenantId = ResolveTenantId();
        var metrology = await _learningService.EvaluateCalibrationAsync(tenantId, request.ModelOrDomain);
        return Ok(metrology);
    }

    [HttpPost("drift/check")]
    public async Task<IActionResult> CheckDrift([FromBody] CheckDriftRequest request)
    {
        var tenantId = ResolveTenantId();
        var drift = await _learningService.CheckStructuralDriftAsync(tenantId, request.ModelOrDomain);
        return Ok(drift);
    }

    [HttpPost("lessons/distill")]
    public async Task<IActionResult> DistillLesson([FromBody] DistillLessonRequest request)
    {
        var tenantId = ResolveTenantId();
        var lesson = await _learningService.DistillLessonAsync(
            tenantId,
            request.Title,
            request.Domain,
            request.CausalHypothesis,
            request.CounterfactualInsight);
        return Ok(lesson);
    }

    [HttpGet("lessons")]
    public async Task<IActionResult> ListLessons()
    {
        var tenantId = ResolveTenantId();
        var lessons = await _learningService.ListLessonsAsync(tenantId);
        return Ok(lessons);
    }

    [HttpGet("lessons/{id}")]
    public async Task<IActionResult> GetLesson(string id)
    {
        var tenantId = ResolveTenantId();
        var lesson = await _learningService.GetLessonAsync(tenantId, id);
        if (lesson == null) return NotFound(new { error = $"Lesson '{id}' not found." });
        return Ok(lesson);
    }

    [HttpPost("adaptations/propose")]
    public async Task<IActionResult> ProposeAdaptation([FromBody] ProposeAdaptationRequest request)
    {
        var tenantId = ResolveTenantId();
        try
        {
            var proposal = await _learningService.ProposeAdaptationAsync(
                tenantId,
                request.LessonId,
                request.ParameterKey,
                request.ProposedDelta);
            return Ok(proposal);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("adaptations")]
    public async Task<IActionResult> ListAdaptations()
    {
        var tenantId = ResolveTenantId();
        var proposals = await _learningService.ListProposalsAsync(tenantId);
        return Ok(proposals);
    }

    [HttpGet("adaptations/{id}/why")]
    public async Task<IActionResult> GetWhyTrace(string id)
    {
        var tenantId = ResolveTenantId();
        try
        {
            var trace = await _learningService.GetAdaptationWhyTraceAsync(tenantId, id);
            return Ok(trace);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("capabilities")]
    public IActionResult GetCapabilities()
    {
        return Ok(new
        {
            batch = "Batch 3.9.10 OLMA",
            primaryInvariant = OrganizationalLearningInvariants.PrimaryInvariant,
            approvalSovereignty = OrganizationalLearningInvariants.I35_U_ApprovalSovereignty,
            evidenceSufficiencySovereignty = OrganizationalLearningInvariants.I35_V_EvidenceSufficiencySovereignty,
            causalIntelligenceSovereignty = OrganizationalLearningInvariants.I35_W_CausalIntelligenceSovereignty,
            simulationEngineSovereignty = OrganizationalLearningInvariants.I35_X_SimulationEngineSovereignty,
            adaptationHysteresis = OrganizationalLearningInvariants.I35_Y_AdaptationHysteresis,
            driftNotAdaptationAuthority = OrganizationalLearningInvariants.I35_Z_DriftNotAdaptationAuthority,
            whitelistedTargets = _targetRegistry.GetRegisteredTargets()
        });
    }
}

public sealed class EvaluateMetrologyRequest
{
    public string ModelOrDomain { get; set; } = string.Empty;
}

public sealed class CheckDriftRequest
{
    public string ModelOrDomain { get; set; } = string.Empty;
}

public sealed class DistillLessonRequest
{
    public string Title { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string CausalHypothesis { get; set; } = string.Empty;
    public string CounterfactualInsight { get; set; } = string.Empty;
}

public sealed class ProposeAdaptationRequest
{
    public string LessonId { get; set; } = string.Empty;
    public string ParameterKey { get; set; } = string.Empty;
    public double ProposedDelta { get; set; }
}
