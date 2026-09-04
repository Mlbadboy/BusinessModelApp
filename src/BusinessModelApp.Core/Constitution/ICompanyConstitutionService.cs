using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Decisions;
using BusinessModelApp.Core.Domain.Strategy;
using BusinessModelApp.Core.Domain.WorldModel;

namespace BusinessModelApp.Core.Constitution
{
    /// <summary>
    /// Reusable Constitutional Policy Engine.
    /// Evaluates RULE-001 through RULE-006 across all strategy candidates and decision records.
    /// </summary>
    public interface ICompanyConstitutionService
    {
        Task<ConstitutionEvaluationResult> EvaluateStrategyAsync(
            BusinessStrategy strategy,
            CompanySnapshot snapshot,
            CancellationToken ct = default);

        Task<ConstitutionEvaluationResult> EvaluateDecisionAsync(
            DecisionRecord decision,
            CompanySnapshot snapshot,
            CancellationToken ct = default);
    }
}
