using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Commercial;
using BusinessModelApp.Core.Services;
using BusinessModelApp.Infrastructure.Services;
using Xunit;

namespace BusinessModelApp.Tests.Services
{
    public class BusinessOpportunityEngineTests
    {
        private readonly BusinessOpportunityEngine _engine = new();

        [Fact]
        public async Task DiscoverOpportunities_ReturnsGroundedOpportunityPackages()
        {
            var wsId = Guid.NewGuid();
            var filter = new OpportunityDiscoveryFilter
            {
                City = "Pune",
                IndustryOrCategory = "Real Estate Developer",
                TargetCount = 5
            };

            var packages = await _engine.DiscoverOpportunitiesAsync(wsId, filter);

            Assert.NotEmpty(packages);
            foreach (var pkg in packages)
            {
                Assert.NotNull(pkg.Business);
                Assert.False(string.IsNullOrWhiteSpace(pkg.Business.Name));
                Assert.NotNull(pkg.Audit);
                Assert.True(pkg.Audit.OverallScore > 0 && pkg.Audit.OverallScore <= 100);
                Assert.NotEmpty(pkg.Audit.CriticalPainPoints);
                
                Assert.NotNull(pkg.Hypothesis);
                Assert.Equal(125000m, pkg.Hypothesis.EstimatedValueINR);
                Assert.StartsWith("EVD-OPP-", pkg.Hypothesis.EvidenceKey);
                Assert.True(pkg.Hypothesis.OpportunityScore >= 80);
            }
        }

        [Fact]
        public async Task AuditBusinessPresence_CalculatesScoresAndPainPoints()
        {
            var audit = await _engine.AuditBusinessPresenceAsync("https://samplebusiness.in", "Sample Real Estate");

            Assert.NotNull(audit);
            Assert.True(audit.MobileUXScore <= 50);
            Assert.NotEmpty(audit.CriticalPainPoints);
            Assert.Contains("Audited Sample Real Estate", audit.AuditSummary);
        }
    }
}
