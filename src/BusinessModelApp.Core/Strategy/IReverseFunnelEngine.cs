using System;
using BusinessModelApp.Core.Domain.Strategy;
using BusinessModelApp.Core.Domain.WorldModel;

namespace BusinessModelApp.Core.Strategy
{
    public interface IReverseFunnelEngine
    {
        /// <summary>
        /// Calculates the required commercial funnel to achieve the target revenue.
        /// Distinguishes between evidence-backed conversion rates and AI hypotheses.
        /// </summary>
        ReverseFunnelResult CalculateFunnel(
            decimal targetRevenueINR,
            TruthMetric<decimal> acvMetric,
            TruthMetric<double> winRateMetric,
            CompanySnapshot snapshot);
    }
}
