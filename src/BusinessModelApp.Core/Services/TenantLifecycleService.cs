using System;
using System.Threading.Tasks;
using BusinessModelApp.Core.Domain.Tenancy;

namespace BusinessModelApp.Core.Services
{
    public interface ITenantLifecycleService
    {
        TenantAuthorizationResult EvaluateAccess(TenantPolicy policy, string operationType);
        bool CheckResourceQuota(TenantPolicy policy, string resourceType, int currentCount);
        TenantPolicy CreateDefaultPolicy(Guid organizationId, string organizationName, TenantTier tier);
    }

    public class TenantLifecycleService : ITenantLifecycleService
    {
        public TenantAuthorizationResult EvaluateAccess(TenantPolicy policy, string operationType)
        {
            if (policy == null)
            {
                return new TenantAuthorizationResult
                {
                    IsAuthorized = false,
                    RejectionReason = "Tenant policy not found."
                };
            }

            var op = (operationType ?? string.Empty).ToLowerInvariant();

            switch (policy.State)
            {
                case TenantLifecycleState.Archived:
                    return new TenantAuthorizationResult
                    {
                        IsAuthorized = false,
                        CanLogin = false,
                        CanRead = false,
                        CanWrite = false,
                        CanUseAI = false,
                        RejectionReason = "Organization account is archived and decommissioned."
                    };

                case TenantLifecycleState.Suspended:
                    return new TenantAuthorizationResult
                    {
                        IsAuthorized = false,
                        CanLogin = false,
                        CanRead = false,
                        CanWrite = false,
                        CanUseAI = false,
                        RejectionReason = $"Organization account is suspended. Reason: {policy.SuspensionReason ?? "Administrative action"}"
                    };

                case TenantLifecycleState.ReadOnly:
                    var canDoReadOnlyOp = (op == "login" || op == "read" || op == "ai");
                    return new TenantAuthorizationResult
                    {
                        IsAuthorized = canDoReadOnlyOp,
                        CanLogin = true,
                        CanRead = true,
                        CanWrite = false,
                        CanUseAI = true,
                        RejectionReason = canDoReadOnlyOp ? null : "Organization account is in Read-Only state. Commercial mutation prohibited."
                    };

                case TenantLifecycleState.Active:
                default:
                    return new TenantAuthorizationResult
                    {
                        IsAuthorized = true,
                        CanLogin = true,
                        CanRead = true,
                        CanWrite = true,
                        CanUseAI = true
                    };
            }
        }

        public bool CheckResourceQuota(TenantPolicy policy, string resourceType, int currentCount)
        {
            if (policy == null || policy.State != TenantLifecycleState.Active)
                return false;

            return (resourceType?.ToLowerInvariant()) switch
            {
                "opportunity" or "opportunities" => currentCount < policy.MaxOpportunities,
                "lead" or "leads" => currentCount < policy.MaxLeads,
                "ai_call" or "ai_calls" => currentCount < policy.MaxDailyAICalls,
                _ => true,
            };
        }

        public TenantPolicy CreateDefaultPolicy(Guid organizationId, string organizationName, TenantTier tier)
        {
            var policy = new TenantPolicy
            {
                OrganizationId = organizationId,
                OrganizationName = organizationName,
                State = TenantLifecycleState.Active,
                Tier = tier,
                CreatedAt = DateTime.UtcNow
            };

            switch (tier)
            {
                case TenantTier.Starter:
                    policy.MaxOpportunities = 25;
                    policy.MaxLeads = 100;
                    policy.MaxDailyAICalls = 50;
                    policy.MonthlyBudgetCapINR = 5000m;
                    break;

                case TenantTier.Enterprise:
                    policy.MaxOpportunities = 10000;
                    policy.MaxLeads = 50000;
                    policy.MaxDailyAICalls = 10000;
                    policy.MonthlyBudgetCapINR = 200000m;
                    break;

                case TenantTier.Professional:
                default:
                    policy.MaxOpportunities = 250;
                    policy.MaxLeads = 1000;
                    policy.MaxDailyAICalls = 500;
                    policy.MonthlyBudgetCapINR = 25000m;
                    break;
            }

            return policy;
        }
    }
}
