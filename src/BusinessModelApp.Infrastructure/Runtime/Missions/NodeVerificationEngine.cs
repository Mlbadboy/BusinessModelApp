using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Missions;
using BusinessModelApp.Core.Domain.Runtime;
using BusinessModelApp.Core.Interfaces.Missions;

namespace BusinessModelApp.Infrastructure.Runtime.Missions
{
    public class NodeVerificationEngine : INodeVerificationEngine
    {
        private readonly IMissionGraphAuditLedger _auditLedger;

        public NodeVerificationEngine(IMissionGraphAuditLedger auditLedger)
        {
            _auditLedger = auditLedger ?? throw new ArgumentNullException(nameof(auditLedger));
        }

        public async Task<NodeVerificationResult> VerifyNodeOutcomeAsync(
            MissionNodeRecord node,
            object? outputPayload,
            IReadOnlyList<MissionArtifact>? artifacts = null,
            CancellationToken ct = default)
        {
            if (node == null)
            {
                return NodeVerificationResult.Failed("Node cannot be null.");
            }

            var criteria = node.VerificationCriteria ?? new NodeVerificationCriteria();

            // 1. Check if output payload is null
            if (outputPayload == null && criteria.RequiredEvidenceTypes.Count > 0)
            {
                var fail = NodeVerificationResult.Failed("Node produced no output payload when evidence was required.");
                await RecordVerificationAuditAsync(node, fail, ct);
                return fail;
            }

            // Serialize payload to inspect fields
            string jsonPayload = string.Empty;
            JsonDocument? doc = null;
            if (outputPayload != null)
            {
                jsonPayload = outputPayload is string s ? s : JsonSerializer.Serialize(outputPayload);
                try
                {
                    doc = JsonDocument.Parse(jsonPayload);
                }
                catch
                {
                    // Non-JSON payload
                }
            }

            // 2. Schema check if required
            if (!string.IsNullOrWhiteSpace(criteria.ExpectedOutputSchema))
            {
                if (doc == null)
                {
                    var fail = NodeVerificationResult.Failed($"Output does not conform to expected JSON schema: invalid JSON format.");
                    await RecordVerificationAuditAsync(node, fail, ct);
                    return fail;
                }
            }

            // 3. Required evidence presence
            if (criteria.RequiredEvidenceTypes.Count > 0)
            {
                if (doc == null)
                {
                    var fail = NodeVerificationResult.Failed("Required evidence missing: payload cannot be parsed as structured data.");
                    await RecordVerificationAuditAsync(node, fail, ct);
                    return fail;
                }

                foreach (var requiredType in criteria.RequiredEvidenceTypes)
                {
                    bool evidenceFound = false;
                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        if (doc.RootElement.TryGetProperty(requiredType, out var prop) && prop.ValueKind != JsonValueKind.Null)
                        {
                            evidenceFound = true;
                        }
                        else if (doc.RootElement.TryGetProperty("evidence", out var evProp) &&
                                 evProp.ValueKind == JsonValueKind.Object &&
                                 evProp.TryGetProperty(requiredType, out _))
                        {
                            evidenceFound = true;
                        }
                    }

                    // Also check artifacts
                    if (!evidenceFound && artifacts != null)
                    {
                        if (artifacts.Any(a => string.Equals(a.Name, requiredType, StringComparison.OrdinalIgnoreCase) ||
                                               string.Equals(a.ContentType, requiredType, StringComparison.OrdinalIgnoreCase)))
                        {
                            evidenceFound = true;
                        }
                    }

                    if (!evidenceFound)
                    {
                        var fail = NodeVerificationResult.Failed($"Required evidence '{requiredType}' was not found in output payload or artifacts.");
                        await RecordVerificationAuditAsync(node, fail, ct);
                        return fail;
                    }
                }
            }

            // 4. Deterministic assertion keys
            if (criteria.DeterministicAssertionKeys.Count > 0 && doc != null && doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var assertionKey in criteria.DeterministicAssertionKeys)
                {
                    if (!doc.RootElement.TryGetProperty(assertionKey, out var prop))
                    {
                        var fail = NodeVerificationResult.Failed($"Deterministic assertion failed: key '{assertionKey}' not found.");
                        await RecordVerificationAuditAsync(node, fail, ct);
                        return fail;
                    }

                    if (prop.ValueKind == JsonValueKind.False)
                    {
                        var fail = NodeVerificationResult.Failed($"Deterministic assertion failed: key '{assertionKey}' evaluated to false.");
                        await RecordVerificationAuditAsync(node, fail, ct);
                        return fail;
                    }
                }
            }

            // 5. Compute SHA-256 evidence hash for provenance
            var payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);
            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(payloadBytes);
            var evidenceHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

            var success = NodeVerificationResult.Success(evidenceHash, confidence: 1.0);
            await RecordVerificationAuditAsync(node, success, ct);
            return success;
        }

        private async Task RecordVerificationAuditAsync(MissionNodeRecord node, NodeVerificationResult result, CancellationToken ct)
        {
            await _auditLedger.RecordEventAsync(new MissionGraphAuditEntry
            {
                GraphId = node.GraphId,
                Version = MissionGraphVersion.Initial,
                EventType = result.IsVerified ? "NodeVerificationPassed" : "NodeVerificationFailed",
                Details = $"Node {node.NodeId} verification result: IsVerified={result.IsVerified}, Reason={result.FailureReason}, EvidenceHash={result.EvidenceHash}",
                Sha256Hash = result.EvidenceHash ?? string.Empty
            }, ct);
        }
    }
}
