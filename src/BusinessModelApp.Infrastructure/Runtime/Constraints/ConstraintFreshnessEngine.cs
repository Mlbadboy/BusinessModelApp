using System;
using BusinessModelApp.Core.Interfaces.Runtime.Constraints;

namespace BusinessModelApp.Infrastructure.Runtime.Constraints
{
    public class ConstraintFreshnessEngine : IConstraintFreshnessEngine
    {
        public bool IsFresh(DateTimeOffset telemetryTimestamp, TimeSpan freshnessRequirement, out TimeSpan age)
        {
            var now = DateTimeOffset.UtcNow;
            age = now - telemetryTimestamp;
            if (age < TimeSpan.Zero)
            {
                age = TimeSpan.Zero;
            }
            return age <= freshnessRequirement;
        }
    }
}
