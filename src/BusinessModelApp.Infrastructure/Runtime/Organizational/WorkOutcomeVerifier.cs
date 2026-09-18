using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using BusinessModelApp.Core.Domain.Runtime.Organizational;
using BusinessModelApp.Core.Interfaces.Runtime.Organizational;

namespace BusinessModelApp.Infrastructure.Runtime.Organizational
{
    public class WorkOutcomeVerifier : IWorkOutcomeVerifier
    {
        public (bool Verified, double CalculatedSuccessScore, string VerificationHash, string? ErrorMessage) VerifyOutcome(
            WorkItem item,
            string claimedSummary,
            Dictionary<string, double> expectedMetrics,
            Dictionary<string, double> actualMetrics,
            string evidenceSha256,
            string verifierActor)
        {
            if (item == null)
                return (false, 0.0, string.Empty, "WorkItem cannot be null.");

            if (string.IsNullOrWhiteSpace(claimedSummary))
                return (false, 0.0, string.Empty, "Claimed outcome summary cannot be empty.");

            if (string.IsNullOrWhiteSpace(evidenceSha256) || evidenceSha256.Length < 32)
                return (false, 0.0, string.Empty, "Valid cryptographic evidence hash is mandatory (CLAIMED ≠ VERIFIED).");

            if (string.IsNullOrWhiteSpace(verifierActor))
                return (false, 0.0, string.Empty, "Verifier actor is required for verification audit.");

            // Verify metrics variance
            double totalScore = 0.0;
            int count = 0;

            if (expectedMetrics != null && expectedMetrics.Count > 0 && actualMetrics != null)
            {
                foreach (var kvp in expectedMetrics)
                {
                    count++;
                    if (actualMetrics.TryGetValue(kvp.Key, out var actualVal))
                    {
                        double expected = kvp.Value;
                        if (Math.Abs(expected) < 0.0001)
                        {
                            totalScore += (Math.Abs(actualVal) < 0.0001) ? 1.0 : 0.0;
                        }
                        else
                        {
                            double ratio = actualVal / expected;
                            double clamped = Math.Clamp(ratio, 0.0, 1.0);
                            totalScore += clamped;
                        }
                    }
                    else
                    {
                        // Missing metric penalizes score
                        totalScore += 0.0;
                    }
                }
            }
            else
            {
                // Fallback score if no quantitative metrics provided but evidence is verified
                totalScore = 0.5;
                count = 1;
            }

            double finalScore = count > 0 ? Math.Round(totalScore / count, 4) : 0.0;

            // Compute verification SHA-256 hash
            var raw = $"{item.WorkId}:{claimedSummary}:{finalScore}:{evidenceSha256}:{verifierActor}:{DateTime.UtcNow:yyyyMMddHH}";
            using var sha = SHA256.Create();
            string hash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(raw)));

            return (true, finalScore, hash, null);
        }
    }
}
