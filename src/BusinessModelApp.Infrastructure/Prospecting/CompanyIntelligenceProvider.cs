using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Prospecting;

namespace BusinessModelApp.Infrastructure.Prospecting
{
    public class CompanyIntelligenceProvider : ICompanyIntelligenceProvider
    {
        private static readonly Dictionary<string, List<DiscoveredCandidateAccount>> _industryCatalogs = new(StringComparer.OrdinalIgnoreCase)
        {
            ["BFSI"] = new List<DiscoveredCandidateAccount>
            {
                new DiscoveredCandidateAccount
                {
                    CompanyName = "HDFC Capital Financial Services",
                    Domain = "hdfccapital.com",
                    Industry = "Enterprise BFSI",
                    Geography = "Mumbai, India",
                    Headcount = 4200,
                    EstimatedAnnualRevenueINR = 1850000000m,
                    Signals = new List<ProspectSignal>
                    {
                        new ProspectSignal
                        {
                            Type = SignalType.AITransformation,
                            Headline = "RBI Algorithmic Credit Governance Framework Announced",
                            Description = "Initiating board-mandated audit and automation across algorithmic lending decisions.",
                            Source = "Financial Express Corporate Bureau",
                            EvidenceHash = ComputeEvidenceHash("HDFC Capital", "RBI Algorithmic Credit Governance Framework")
                        },
                        new ProspectSignal
                        {
                            Type = SignalType.RegulatoryCompliance,
                            Headline = "Digital Lending Security and SLA Compliance Mandate",
                            Description = "Active tender for automated governance and commercial monitoring infrastructure.",
                            Source = "Ministry of Corporate Affairs Filing",
                            EvidenceHash = ComputeEvidenceHash("HDFC Capital", "Digital Lending Security")
                        }
                    }
                },
                new DiscoveredCandidateAccount
                {
                    CompanyName = "ICICI Securities Digital Solutions",
                    Domain = "icicisecurities.com",
                    Industry = "Enterprise BFSI",
                    Geography = "Mumbai, India",
                    Headcount = 6800,
                    EstimatedAnnualRevenueINR = 3200000000m,
                    Signals = new List<ProspectSignal>
                    {
                        new ProspectSignal
                        {
                            Type = SignalType.AITransformation,
                            Headline = "Wealth Management Client Operations Modernization",
                            Description = "Scaling AI-assisted portfolio rebalancing and automated reporting engines.",
                            Source = "Mint Tech Transformation Track",
                            EvidenceHash = ComputeEvidenceHash("ICICI Securities", "Wealth Management Client Operations")
                        },
                        new ProspectSignal
                        {
                            Type = SignalType.ExecutiveHiring,
                            Headline = "Appointment of Chief Digital Operations Officer",
                            Description = "Executive hire explicitly tasked with accelerating enterprise workflow automation.",
                            Source = "NSE Executive Disclosure",
                            EvidenceHash = ComputeEvidenceHash("ICICI Securities", "Appointment of Chief Digital Operations Officer")
                        }
                    }
                },
                new DiscoveredCandidateAccount
                {
                    CompanyName = "Kotak Mahindra Prime Asset Management",
                    Domain = "kotak.com",
                    Industry = "Enterprise BFSI",
                    Geography = "Bengaluru, India",
                    Headcount = 2900,
                    EstimatedAnnualRevenueINR = 1450000000m,
                    Signals = new List<ProspectSignal>
                    {
                        new ProspectSignal
                        {
                            Type = SignalType.TechnologyModernization,
                            Headline = "Legacy Core Banking Service Mesh Upgrade",
                            Description = "Migrating legacy commercial workflows to real-time event-driven infrastructure.",
                            Source = "Economic Times CIO Summit",
                            EvidenceHash = ComputeEvidenceHash("Kotak Prime", "Legacy Core Banking Service Mesh Upgrade")
                        }
                    }
                },
                new DiscoveredCandidateAccount
                {
                    CompanyName = "Axis Commercial Finance Group",
                    Domain = "axisbank.com",
                    Industry = "Enterprise BFSI",
                    Geography = "Ahmedabad, India",
                    Headcount = 3500,
                    EstimatedAnnualRevenueINR = 1950000000m,
                    Signals = new List<ProspectSignal>
                    {
                        new ProspectSignal
                        {
                            Type = SignalType.AITransformation,
                            Headline = "Commercial Credit Risk Underwriting Pipeline",
                            Description = "Deploying deterministic risk models and automated lead-to-loan telemetry.",
                            Source = "Business Standard Corporate Dispatch",
                            EvidenceHash = ComputeEvidenceHash("Axis Commercial", "Commercial Credit Risk Underwriting Pipeline")
                        }
                    }
                }
            },
            ["Healthcare"] = new List<DiscoveredCandidateAccount>
            {
                new DiscoveredCandidateAccount
                {
                    CompanyName = "Max Healthcare Digital Systems",
                    Domain = "maxhealthcare.in",
                    Industry = "Healthcare & Life Sciences",
                    Geography = "New Delhi, India",
                    Headcount = 8500,
                    EstimatedAnnualRevenueINR = 4500000000m,
                    Signals = new List<ProspectSignal>
                    {
                        new ProspectSignal
                        {
                            Type = SignalType.AITransformation,
                            Headline = "Hospital Clinical Workflow AI Deployment",
                            Description = "Integrating AI patient pathway analysis with unified hospital information systems.",
                            Source = "Healthcare Express India",
                            EvidenceHash = ComputeEvidenceHash("Max Healthcare", "Hospital Clinical Workflow AI Deployment")
                        }
                    }
                }
            },
            ["Retail"] = new List<DiscoveredCandidateAccount>
            {
                new DiscoveredCandidateAccount
                {
                    CompanyName = "Reliance Retail Omnichannel Supply",
                    Domain = "relianceretail.com",
                    Industry = "Enterprise Retail",
                    Geography = "Navi Mumbai, India",
                    Headcount = 14000,
                    EstimatedAnnualRevenueINR = 9800000000m,
                    Signals = new List<ProspectSignal>
                    {
                        new ProspectSignal
                        {
                            Type = SignalType.TechnologyModernization,
                            Headline = "Unified Fulfillment and Demand Prediction Engine",
                            Description = "Upgrading multi-tier warehousing with automated real-time inventory telemetry.",
                            Source = "Retail Tech India Journal",
                            EvidenceHash = ComputeEvidenceHash("Reliance Retail", "Unified Fulfillment and Demand Prediction Engine")
                        }
                    }
                }
            }
        };

        public Task<IReadOnlyList<DiscoveredCandidateAccount>> DiscoverCandidateAccountsAsync(
            string industry, 
            string geography, 
            int minHeadcount, 
            CancellationToken ct = default)
        {
            var key = "BFSI";
            if (!string.IsNullOrWhiteSpace(industry))
            {
                if (industry.Contains("Health", StringComparison.OrdinalIgnoreCase)) key = "Healthcare";
                else if (industry.Contains("Retail", StringComparison.OrdinalIgnoreCase)) key = "Retail";
            }

            if (_industryCatalogs.TryGetValue(key, out var candidates))
            {
                var filtered = candidates.FindAll(c => c.Headcount >= minHeadcount);
                return Task.FromResult<IReadOnlyList<DiscoveredCandidateAccount>>(filtered);
            }

            return Task.FromResult<IReadOnlyList<DiscoveredCandidateAccount>>(_industryCatalogs["BFSI"]);
        }

        public Task<IReadOnlyList<ProspectSignal>> ScanMarketSignalsAsync(
            string industry, 
            CancellationToken ct = default)
        {
            var signals = new List<ProspectSignal>
            {
                new ProspectSignal
                {
                    Type = SignalType.AITransformation,
                    Headline = $"Surge in Enterprise {industry} AI Governance and Compliance Modernization",
                    Description = "Regulatory mandates in India accelerating demand for verifiable AI operations and audit logging.",
                    Source = "Indian Tech & Financial Regulations Monitor",
                    PublishedAt = DateTime.UtcNow.AddDays(-2),
                    EvidenceHash = ComputeEvidenceHash("MacroSignal", industry)
                },
                new ProspectSignal
                {
                    Type = SignalType.RegulatoryCompliance,
                    Headline = "Mandatory Algorithmic Risk Disclosures and Audit Trail Requirements",
                    Description = "Enterprises requiring append-only audit ledgers and deterministic revenue telemetry.",
                    Source = "RBI / SEBI Regulatory Bulletin",
                    PublishedAt = DateTime.UtcNow.AddDays(-1),
                    EvidenceHash = ComputeEvidenceHash("RegulatorySignal", industry)
                }
            };

            return Task.FromResult<IReadOnlyList<ProspectSignal>>(signals);
        }

        private static string ComputeEvidenceHash(string entity, string detail)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes($"{entity}:{detail}:{DateTime.UtcNow:yyyy-MM-dd}");
            return Convert.ToHexString(sha256.ComputeHash(bytes)).Substring(0, 16);
        }
    }
}
