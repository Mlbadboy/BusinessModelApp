using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Runtime;

namespace BusinessModelApp.Infrastructure.Runtime.BrainFabric
{
    public class ContextOptimizer : IContextOptimizer
    {
        public Task<ContextCompressionResult> OptimizeContextAsync(string prompt, long maxTargetTokens, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(prompt))
            {
                return Task.FromResult(new ContextCompressionResult
                {
                    OriginalTokenCount = 0,
                    CompressedTokenCount = 0,
                    CompressedPrompt = string.Empty,
                    SourceRecordsPreserved = true
                });
            }

            // Estimate tokens (~4 chars per token)
            var originalTokens = Math.Max(1, prompt.Length / 4);

            // Compress whitespace while strictly preserving source semantic lines
            var compressed = Regex.Replace(prompt, @"[ \t]+", " ");
            compressed = Regex.Replace(compressed, @"(\r?\n){3,}", "\n\n").Trim();

            var compressedTokens = Math.Max(1, compressed.Length / 4);

            return Task.FromResult(new ContextCompressionResult
            {
                OriginalTokenCount = originalTokens,
                CompressedTokenCount = compressedTokens,
                CompressedPrompt = compressed,
                SourceRecordsPreserved = true // Guarantees immutability of underlying data
            });
        }
    }
}
