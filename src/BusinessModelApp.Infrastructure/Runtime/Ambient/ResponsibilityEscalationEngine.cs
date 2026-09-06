using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Responsibilities;
using BusinessModelApp.Core.Interfaces.Ambient;

namespace BusinessModelApp.Infrastructure.Runtime.Ambient
{
    public class ResponsibilityEscalationEngine : IResponsibilityEscalator
    {
        public Task<ResponsibilityPriority> EvaluateEscalationAsync(ResponsibilityRecord record, CancellationToken cancellationToken = default)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            // P0: Critical escalation if severity > 50 or repeated persistent breaches (> 5)
            if (record.SeverityScore >= 50.0m || record.ConsecutiveBreaches >= 5)
            {
                return Task.FromResult(ResponsibilityPriority.P0_Critical);
            }

            // P1: High escalation if severity > 20 or consecutive breaches >= 3
            if (record.SeverityScore >= 20.0m || record.ConsecutiveBreaches >= 3)
            {
                return Task.FromResult(ResponsibilityPriority.P1_High);
            }

            // Otherwise retain current priority or return at least P2
            return Task.FromResult(record.Priority);
        }
    }
}
