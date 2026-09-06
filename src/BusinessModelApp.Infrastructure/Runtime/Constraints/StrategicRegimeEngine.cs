using System;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime.Constraints;
using BusinessModelApp.Core.Interfaces.Runtime.Constraints;

namespace BusinessModelApp.Infrastructure.Runtime.Constraints
{
    public class StrategicRegimeEngine : IStrategicRegimeEngine
    {
        private readonly IBusinessConstraintStore _store;

        public StrategicRegimeEngine(IBusinessConstraintStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<StrategicRegimePolicy> GetActivePolicyAsync(Guid workspaceId, CancellationToken ct = default)
        {
            var policy = await _store.GetStrategicPolicyAsync(workspaceId, ct);
            if (policy != null) return policy;

            var defaultPolicy = StrategicRegimePolicy.CreateDefault(workspaceId, StrategicRegimeType.BalancedProfitability_Conservative);
            await _store.SaveStrategicPolicyAsync(defaultPolicy, ct);
            return defaultPolicy;
        }

        public async Task SetRegimeAsync(Guid workspaceId, StrategicRegimeType regime, string authority, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(authority))
                throw new ArgumentException("Regime change requires verified authority.", nameof(authority));

            var policy = StrategicRegimePolicy.CreateDefault(workspaceId, regime);
            policy = policy with { Authority = authority, PolicyVersion = policy.PolicyVersion + 1 };
            await _store.SaveStrategicPolicyAsync(policy, ct);
        }

        public double ComputeStrategicUtility(StrategicRegimePolicy policy, ArbitrationCandidate candidate)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));

            // Weight alignment according to active strategic regime priorities
            return policy.Regime switch
            {
                StrategicRegimeType.CashPreservation_Distressed =>
                    (candidate.LiquidityPreservationImpact * 0.50) +
                    (candidate.StrategicAlignmentScore * 0.20) +
                    (candidate.ExpectedReturnOnInvestment * 0.15) -
                    (candidate.RiskScore * 0.35),

                StrategicRegimeType.AggressiveGrowth_Expansion =>
                    (candidate.StrategicAlignmentScore * 0.40) +
                    (candidate.ExpectedReturnOnInvestment * 0.30) +
                    (candidate.LiquidityPreservationImpact * 0.10) -
                    (candidate.RiskScore * 0.15),

                StrategicRegimeType.MarketDefense_PriceWar =>
                    (candidate.StrategicAlignmentScore * 0.40) +
                    (candidate.LiquidityPreservationImpact * 0.25) +
                    (candidate.ExpectedReturnOnInvestment * 0.20) -
                    (candidate.RiskScore * 0.20),

                _ => // BalancedProfitability_Conservative
                    (candidate.ExpectedReturnOnInvestment * 0.35) +
                    (candidate.StrategicAlignmentScore * 0.30) +
                    (candidate.LiquidityPreservationImpact * 0.25) -
                    (candidate.RiskScore * 0.20)
            };
        }
    }
}
