using System;

namespace BusinessModelApp.Core.Agents
{
    public enum AutonomyLevel
    {
        Level0_Observe = 0,
        Level1_Recommend = 1,
        Level2_Assisted = 2,
        Level3_ControlledAutonomy = 3,
        Level4_AutonomousOperations = 4,
        Level5_StrategicAutonomy = 5
    }

    public enum PolicyActionDecision
    {
        AllowAutonomous = 0,
        RequireHumanApproval = 1,
        DenyAction = 2
    }

    public class PolicyDecision
    {
        public PolicyActionDecision Decision { get; set; }
        public bool IsAllowed => Decision == PolicyActionDecision.AllowAutonomous;
        public bool RequiresApproval => Decision == PolicyActionDecision.RequireHumanApproval;
        public string Reason { get; set; } = string.Empty;
        public int RiskScore { get; set; } = 1; // 1 (Low) to 5 (Critical)
    }

    public class AgentPolicyEngine
    {
        public PolicyDecision Evaluate(AgentIdentity identity, AgentActionType action, AutonomyLevel autonomyLevel, decimal monetaryImpactINR = 0m)
        {
            // 1. Check Identity Action Grant
            if (!identity.CanPerform(action))
            {
                return new PolicyDecision
                {
                    Decision = PolicyActionDecision.DenyAction,
                    Reason = $"Agent role '{identity.Name}' is strictly not permitted to perform '{action}'.",
                    RiskScore = 5
                };
            }

            // 2. High-Risk Consequential Commitments always require Human Approval in Levels 0-3
            switch (action)
            {
                case AgentActionType.DeleteData:
                    return new PolicyDecision
                    {
                        Decision = PolicyActionDecision.RequireHumanApproval,
                        Reason = "Data deletion is a destructive operation and requires explicit human authorization.",
                        RiskScore = 5
                    };

                case AgentActionType.SendContract:
                case AgentActionType.ProposeDiscount:
                case AgentActionType.GenerateProposal:
                    if (autonomyLevel < AutonomyLevel.Level4_AutonomousOperations || monetaryImpactINR > 500000m)
                    {
                        return new PolicyDecision
                        {
                            Decision = PolicyActionDecision.RequireHumanApproval,
                            Reason = $"Commercial commitment of ₹{monetaryImpactINR:N0} requires human approval under {autonomyLevel}.",
                            RiskScore = 4
                        };
                    }
                    break;

                case AgentActionType.SendOutreach:
                    if (autonomyLevel == AutonomyLevel.Level0_Observe || autonomyLevel == AutonomyLevel.Level1_Recommend)
                    {
                        return new PolicyDecision
                        {
                            Decision = PolicyActionDecision.RequireHumanApproval,
                            Reason = $"Outbound communications require human approval under {autonomyLevel}.",
                            RiskScore = 2
                        };
                    }
                    break;
            }

            // 3. Autonomy Level Matrix Check
            switch (autonomyLevel)
            {
                case AutonomyLevel.Level0_Observe:
                    return action == AgentActionType.SearchWeb || action == AgentActionType.ResearchCompany || action == AgentActionType.ScoreLead
                        ? new PolicyDecision { Decision = PolicyActionDecision.AllowAutonomous, Reason = "Read-only research permitted in Observe mode.", RiskScore = 1 }
                        : new PolicyDecision { Decision = PolicyActionDecision.RequireHumanApproval, Reason = "Observe mode requires human approval for all operational actions.", RiskScore = 3 };

                case AutonomyLevel.Level1_Recommend:
                case AutonomyLevel.Level2_Assisted:
                    if (action == AgentActionType.SearchWeb || action == AgentActionType.ResearchCompany || action == AgentActionType.DiscoverDecisionMakers || action == AgentActionType.ScoreLead || action == AgentActionType.DraftOutreach)
                    {
                        return new PolicyDecision { Decision = PolicyActionDecision.AllowAutonomous, Reason = "Internal analysis and drafting allowed.", RiskScore = 1 };
                    }
                    return new PolicyDecision { Decision = PolicyActionDecision.RequireHumanApproval, Reason = $"Action '{action}' requires approval in {autonomyLevel}.", RiskScore = 3 };

                case AutonomyLevel.Level3_ControlledAutonomy:
                case AutonomyLevel.Level4_AutonomousOperations:
                case AutonomyLevel.Level5_StrategicAutonomy:
                    return new PolicyDecision
                    {
                        Decision = PolicyActionDecision.AllowAutonomous,
                        Reason = "Authorized under controlled autonomous operational policy.",
                        RiskScore = 1
                    };

                default:
                    return new PolicyDecision { Decision = PolicyActionDecision.DenyAction, Reason = "Unknown autonomy level", RiskScore = 5 };
            }
        }
    }
}
