using System.Security.Cryptography;
using System.Text;

namespace BusinessModelApp.Core.Domain.Runtime.Organizational.Simulation;

public sealed class OrganizationSnapshot
{
    public string OrgId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double AnnualRevenue { get; set; } = 0.0;
    public double BurnRateMonthly { get; set; } = 0.0;
    public double CashReserve { get; set; } = 0.0;
    public int Headcount { get; set; } = 0;
}

public sealed class CustomerPopulationSnapshot
{
    public int TotalPopulationSize { get; set; } = 10_000;
    public double AverageChurnRate { get; set; } = 0.03;
    public double PriceSensitivityIndex { get; set; } = 0.65;
    public double BrandLoyaltyScore { get; set; } = 0.70;
    public Dictionary<string, int> SegmentDistribution { get; set; } = new();
}

public sealed class CompetitorPopulationSnapshot
{
    public int ActiveCompetitorsCount { get; set; } = 3;
    public double AggressivenessIndex { get; set; } = 0.5;
    public List<string> CompetitorNames { get; set; } = new();
    public Dictionary<string, double> MarketShareDistribution { get; set; } = new();
}

public sealed class WorkforcePopulationSnapshot
{
    public int TotalAgentsAndStaff { get; set; } = 50;
    public double AverageProductivityIndex { get; set; } = 0.85;
    public double BurnoutRiskScore { get; set; } = 0.20;
    public Dictionary<string, int> RoleCounts { get; set; } = new();
}

public sealed class MarketEnvironmentSnapshot
{
    public string MarketRegime { get; set; } = "StableGrowth";
    public double InflationRate { get; set; } = 0.03;
    public double RegulatoryPressureScore { get; set; } = 0.25;
    public double MarketDemandGrowthAnnual { get; set; } = 0.08;
}

public sealed class OperationalEnvironmentSnapshot
{
    public double ServiceUptimePercent { get; set; } = 99.9;
    public double InfrastructureCapacityUsage { get; set; } = 0.60;
    public int ActiveIncidentsCount { get; set; } = 0;
}

public sealed class ResourceCapacitySnapshot
{
    public double ComputeCapacityUnits { get; set; } = 1000.0;
    public double HumanAttentionHours { get; set; } = 500.0;
    public double LiquidityBufferAvailable { get; set; } = 250_000.0;
}

public sealed class PortfolioSnapshot
{
    public int ActiveInitiativesCount { get; set; } = 5;
    public double TotalCommittedCapacities { get; set; } = 750.0;
    public double ExpectedPortfolioValue { get; set; } = 1_500_000.0;
}

public sealed class StrategicRegimeSnapshot
{
    public string ActiveRegime { get; set; } = "Resilience";
    public double StrategicHurdleRate { get; set; } = 0.15;
    public double LiquidityReserveFloor { get; set; } = 0.20;
}

public sealed class PolicySnapshot
{
    public int HardConstraintsCount { get; set; } = 4;
    public List<string> ActiveConstraintIds { get; set; } = new();
}

/// <summary>
/// Immutable synthetic world model snapshot for a simulation environment.
/// </summary>
public sealed class SimulationWorldSnapshot
{
    public string SnapshotId { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = string.Empty;
    public DateTime ObservedAtUtc { get; set; } = DateTime.UtcNow;
    public string Source { get; set; } = "SyntheticModelEngine";
    public string TruthClassification { get; set; } = OrganizationalSimulationInvariants.TruthClassificationSimulation;
    public string IntegrityHash { get; set; } = string.Empty;

    public OrganizationSnapshot Organization { get; set; } = new();
    public CustomerPopulationSnapshot Customers { get; set; } = new();
    public CompetitorPopulationSnapshot Competitors { get; set; } = new();
    public WorkforcePopulationSnapshot Workforce { get; set; } = new();
    public MarketEnvironmentSnapshot Market { get; set; } = new();
    public OperationalEnvironmentSnapshot Operations { get; set; } = new();
    public ResourceCapacitySnapshot Resources { get; set; } = new();
    public PortfolioSnapshot Portfolio { get; set; } = new();
    public StrategicRegimeSnapshot Strategy { get; set; } = new();
    public PolicySnapshot Policies { get; set; } = new();

    public void ComputeIntegrityHash()
    {
        var sb = new StringBuilder();
        sb.Append($"{SnapshotId}:{TenantId}:{ObservedAtUtc:O}:{TruthClassification}:");
        sb.Append($"{Organization.AnnualRevenue}:{Organization.CashReserve}:");
        sb.Append($"{Customers.TotalPopulationSize}:{Customers.AverageChurnRate}:");
        sb.Append($"{Competitors.ActiveCompetitorsCount}:{Competitors.AggressivenessIndex}:");
        sb.Append($"{Workforce.TotalAgentsAndStaff}:{Market.MarketRegime}:");
        sb.Append($"{Operations.ServiceUptimePercent}:{Resources.ComputeCapacityUnits}:");
        sb.Append($"{Portfolio.ActiveInitiativesCount}:{Strategy.ActiveRegime}");

        using var sha = SHA256.Create();
        IntegrityHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString())));
    }
}
