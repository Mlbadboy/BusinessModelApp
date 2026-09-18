using BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational.Simulation;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational.Simulation;

public sealed class SimulationWorldBuilder : ISimulationWorldBuilder
{
    private readonly ISimulationTenantIsolation _tenantIsolation;

    public SimulationWorldBuilder(ISimulationTenantIsolation tenantIsolation)
    {
        _tenantIsolation = tenantIsolation ?? throw new ArgumentNullException(nameof(tenantIsolation));
    }

    public Task<SimulationWorldSnapshot> CaptureWorldSnapshotAsync(
        string tenantId,
        string source = "ProductionStateSnapshot",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentNullException(nameof(tenantId));

        var snapshot = new SimulationWorldSnapshot
        {
            TenantId = tenantId,
            Source = source,
            ObservedAtUtc = DateTime.UtcNow,
            TruthClassification = OrganizationalSimulationInvariants.TruthClassificationSimulation,
            Organization = new OrganizationSnapshot
            {
                OrgId = $"org-{tenantId}",
                Name = $"Enterprise Corp ({tenantId})",
                AnnualRevenue = 5_000_000.0,
                BurnRateMonthly = 350_000.0,
                CashReserve = 1_200_000.0,
                Headcount = 45
            },
            Customers = new CustomerPopulationSnapshot
            {
                TotalPopulationSize = 12_500,
                AverageChurnRate = 0.028,
                PriceSensitivityIndex = 0.62,
                BrandLoyaltyScore = 0.74,
                SegmentDistribution = new() { { "Enterprise", 500 }, { "MidMarket", 3000 }, { "SMB", 9000 } }
            },
            Competitors = new CompetitorPopulationSnapshot
            {
                ActiveCompetitorsCount = 4,
                AggressivenessIndex = 0.55,
                CompetitorNames = new() { "ApexCorp", "OmniDynamics", "VanguardSys", "StratumGlobal" },
                MarketShareDistribution = new() { { "Primary", 0.35 }, { "ApexCorp", 0.25 }, { "OmniDynamics", 0.20 }, { "Others", 0.20 } }
            },
            Workforce = new WorkforcePopulationSnapshot
            {
                TotalAgentsAndStaff = 45,
                AverageProductivityIndex = 0.88,
                BurnoutRiskScore = 0.18,
                RoleCounts = new() { { "Engineers", 20 }, { "Operations", 15 }, { "Product", 10 } }
            },
            Market = new MarketEnvironmentSnapshot
            {
                MarketRegime = "CompetitiveExpansion",
                InflationRate = 0.025,
                RegulatoryPressureScore = 0.20,
                MarketDemandGrowthAnnual = 0.09
            },
            Operations = new OperationalEnvironmentSnapshot
            {
                ServiceUptimePercent = 99.95,
                InfrastructureCapacityUsage = 0.58,
                ActiveIncidentsCount = 0
            },
            Resources = new ResourceCapacitySnapshot
            {
                ComputeCapacityUnits = 2500.0,
                HumanAttentionHours = 800.0,
                LiquidityBufferAvailable = 450_000.0
            },
            Portfolio = new PortfolioSnapshot
            {
                ActiveInitiativesCount = 6,
                TotalCommittedCapacities = 1200.0,
                ExpectedPortfolioValue = 2_800_000.0
            },
            Strategy = new StrategicRegimeSnapshot
            {
                ActiveRegime = "Growth",
                StrategicHurdleRate = 0.12,
                LiquidityReserveFloor = 0.20
            },
            Policies = new PolicySnapshot
            {
                HardConstraintsCount = 5,
                ActiveConstraintIds = new() { "c-liquidity-floor", "c-data-sovereignty", "c-prg1-governance" }
            }
        };

        snapshot.ComputeIntegrityHash();
        return Task.FromResult(snapshot);
    }

    public Task<bool> VerifySnapshotIntegrityAsync(SimulationWorldSnapshot snapshot)
    {
        if (snapshot == null) return Task.FromResult(false);

        string existingHash = snapshot.IntegrityHash;
        snapshot.ComputeIntegrityHash();
        bool isValid = string.Equals(existingHash, snapshot.IntegrityHash, StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(isValid);
    }
}
