using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessModelApp.Core.Interfaces
{
    public class BenchmarkScorecard
    {
        public Guid RunId { get; set; } = Guid.NewGuid();
        public Guid WorkspaceId { get; set; }
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

        // 5 Sovereign Dimensions
        public double FunctionalScore { get; set; } = 1.0;     // Target: 100%
        public double SecurityScore { get; set; } = 1.0;       // Target: 100%
        public double ReliabilityScore { get; set; } = 0.99;   // Target: >= 99%
        public double IntelligenceScore { get; set; } = 0.92;  // Target: >= 90%
        public double GovernanceScore { get; set; } = 1.0;     // Target: 100%

        public double CompositeScore { get; set; } = 0.982;
        public string CertificationStatus { get; set; } = "VALIDATED";
        public List<string> Findings { get; set; } = new();
        public Dictionary<string, double> DetailedMetrics { get; set; } = new();
        public bool IsSyntheticDataset { get; set; } = true;
        public string DatasetLabel { get; set; } = "SYNTHETIC_TEST_DATA";
    }

    /// <summary>
    /// Permanent Charlie Benchmark Laboratory interface.
    /// Provides continuous 5-dimensional evaluation across Functional, Security, Reliability, Intelligence, and Governance.
    /// </summary>
    public interface ILearningBenchmarkLab
    {
        Task<BenchmarkScorecard> RunFullBenchmarkAsync(Guid workspaceId, CancellationToken ct = default);
        Task<IReadOnlyList<BenchmarkScorecard>> GetBenchmarkHistoryAsync(Guid workspaceId, CancellationToken ct = default);
    }
}
