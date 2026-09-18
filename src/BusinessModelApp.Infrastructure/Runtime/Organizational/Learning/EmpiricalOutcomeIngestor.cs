using BusinessModelApp.Core.Domain.Runtime.Organizational.Learning;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Learning;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Learning;

public sealed class EmpiricalOutcomeIngestor : IEmpiricalOutcomeIngestor
{
    private readonly ILearningAuditRepository _repository;

    public EmpiricalOutcomeIngestor(ILearningAuditRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<EmpiricalOutcomeEvent> IngestOutcomeAsync(EmpiricalOutcomeEvent outcomeEvent)
    {
        if (outcomeEvent == null) throw new ArgumentNullException(nameof(outcomeEvent));
        if (string.IsNullOrWhiteSpace(outcomeEvent.TenantId))
            throw new ArgumentException("TenantId is mandatory for empirical outcome ingestion. (I35-M)", nameof(outcomeEvent));

        if (string.IsNullOrWhiteSpace(outcomeEvent.OutcomeEventId))
        {
            outcomeEvent.OutcomeEventId = Guid.NewGuid().ToString("N");
        }

        if (outcomeEvent.ObservedUtc == default)
        {
            outcomeEvent.ObservedUtc = DateTime.UtcNow;
        }

        // Calculate variance score relative to expected value
        var denominator = Math.Abs(outcomeEvent.ExpectedValue) > 0.0001 ? Math.Abs(outcomeEvent.ExpectedValue) : 1.0;
        outcomeEvent.VarianceScore = Math.Round(Math.Abs(outcomeEvent.DeltaValue) / denominator, 4);

        await _repository.SaveOutcomeAsync(outcomeEvent);
        return outcomeEvent;
    }

    public Task<IReadOnlyList<EmpiricalOutcomeEvent>> GetOutcomesAsync(string tenantId, string? domain = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentNullException(nameof(tenantId));
        return _repository.ListOutcomesAsync(tenantId, domain);
    }
}
