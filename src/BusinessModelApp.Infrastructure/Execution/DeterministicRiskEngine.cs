using System;
using BusinessModelApp.Core.Domain.Execution;
using BusinessModelApp.Core.Interfaces;

namespace BusinessModelApp.Infrastructure.Execution
{
    public class DeterministicRiskEngine : IRiskEngine
    {
        public ExecutionRiskTier EvaluateRisk(ExecutionRequest request)
        {
            if (request == null)
                return ExecutionRiskTier.R5_DestructiveOrHighImpact;

            var cap = request.CapabilityId?.Trim().ToUpperInvariant() ?? string.Empty;

            // 1. Destructive / High-Impact (R5)
            if (cap.Contains("DELETE") || cap.Contains("PURGE") || cap.Contains("DROP") || cap.Contains("TERMINATE"))
            {
                return ExecutionRiskTier.R5_DestructiveOrHighImpact;
            }

            // 2. Legal / Contract / Policy Binding (R4)
            if (cap.Contains("CONTRACT") || cap.Contains("LEGAL") || cap.Contains("POLICY.CHANGE") || cap.Contains("SIGN"))
            {
                return ExecutionRiskTier.R4_LegalContract;
            }

            // High financial threshold (> ₹5,00,000) automatically escalates to R4
            if (request.MonetaryImpactINR > 500000m)
            {
                return ExecutionRiskTier.R4_LegalContract;
            }

            // 3. Financial Commitments (R3)
            if (request.MonetaryImpactINR > 0m || cap.Contains("PAYMENT") || cap.Contains("REFUND") || cap.Contains("TRANSFER") || cap.Contains("DISCOUNT"))
            {
                return ExecutionRiskTier.R3_FinancialCommitment;
            }

            // 4. External Customer Communication (R2)
            if (cap.Contains("EMAIL.SEND") || cap.Contains("MESSAGE.SEND") || cap.Contains("CALL.DISPATCH") || cap.Contains("WHATSAPP"))
            {
                return ExecutionRiskTier.R2_CustomerCommunication;
            }

            // 5. Internal Reversible Actions (R1)
            if (request.ActionTier == ExecutionActionTier.L4_ReversibleAction || cap.Contains("DRAFT") || cap.Contains("STAGE") || cap.Contains("CACHE") || cap.Contains("SANDBOX"))
            {
                return ExecutionRiskTier.R1_InternalReversible;
            }

            // 6. Read-Only Telemetry / Search (R0)
            if (request.ActionTier <= ExecutionActionTier.L2_Recommend || cap.Contains("READ") || cap.Contains("GET") || cap.Contains("SEARCH") || cap.Contains("ANALYZE"))
            {
                return ExecutionRiskTier.R0_ReadData;
            }

            // Default to R2 for unclassified consequential actions
            return ExecutionRiskTier.R2_CustomerCommunication;
        }
    }
}
