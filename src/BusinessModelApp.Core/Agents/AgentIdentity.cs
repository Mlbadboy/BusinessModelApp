using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Agents
{
    public enum AgentRole
    {
        MarketIntelligence = 0,
        ProspectDiscovery = 1,
        CompanyResearch = 2,
        LeadQualification = 3,
        Outreach = 4,
        Conversation = 5,
        ProposalGeneration = 6,
        CommercialCloser = 7,
        RiskEvaluator = 8
    }

    public enum AgentActionType
    {
        SearchWeb = 0,
        ResearchCompany = 1,
        DiscoverDecisionMakers = 2,
        ScoreLead = 3,
        DraftOutreach = 4,
        SendOutreach = 5,
        ProcessInboundMessage = 6,
        CreateLeadInCRM = 7,
        CreateOpportunityInCRM = 8,
        GenerateProposal = 9,
        ProposeDiscount = 10,
        SendContract = 11,
        DeleteData = 12
    }

    public class AgentIdentity
    {
        public AgentRole Role { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public HashSet<AgentActionType> PermittedActions { get; set; } = new HashSet<AgentActionType>();

        public bool CanPerform(AgentActionType action) => PermittedActions.Contains(action);

        public static AgentIdentity Create(AgentRole role)
        {
            return role switch
            {
                AgentRole.MarketIntelligence => new AgentIdentity
                {
                    Role = role,
                    Name = "Market Intelligence Agent",
                    Description = "Analyzes macroeconomic demand, industry trends, and market signals.",
                    PermittedActions = new HashSet<AgentActionType> { AgentActionType.SearchWeb, AgentActionType.ResearchCompany }
                },
                AgentRole.ProspectDiscovery => new AgentIdentity
                {
                    Role = role,
                    Name = "Prospect Discovery Agent",
                    Description = "Identifies target organizations and relevant decision makers matching ICP.",
                    PermittedActions = new HashSet<AgentActionType> { AgentActionType.SearchWeb, AgentActionType.ResearchCompany, AgentActionType.DiscoverDecisionMakers }
                },
                AgentRole.CompanyResearch => new AgentIdentity
                {
                    Role = role,
                    Name = "Company Research Agent",
                    Description = "Deep-dives into organizational needs, technological initiatives, and evidence grounding.",
                    PermittedActions = new HashSet<AgentActionType> { AgentActionType.SearchWeb, AgentActionType.ResearchCompany }
                },
                AgentRole.LeadQualification => new AgentIdentity
                {
                    Role = role,
                    Name = "Qualification Agent",
                    Description = "Evaluates prospect fit, buying stage, and lead quality score.",
                    PermittedActions = new HashSet<AgentActionType> { AgentActionType.ScoreLead, AgentActionType.CreateLeadInCRM }
                },
                AgentRole.Outreach => new AgentIdentity
                {
                    Role = role,
                    Name = "Outreach Agent",
                    Description = "Drafts and delivers evidence-grounded communications via governed channels.",
                    PermittedActions = new HashSet<AgentActionType> { AgentActionType.DraftOutreach, AgentActionType.SendOutreach }
                },
                AgentRole.Conversation => new AgentIdentity
                {
                    Role = role,
                    Name = "Conversation Agent",
                    Description = "Understands prospect intent, handles commercial queries, and detects buying signals.",
                    PermittedActions = new HashSet<AgentActionType> { AgentActionType.ProcessInboundMessage, AgentActionType.DraftOutreach, AgentActionType.SendOutreach }
                },
                AgentRole.ProposalGeneration => new AgentIdentity
                {
                    Role = role,
                    Name = "Proposal Agent",
                    Description = "Compiles structured commercial proposals based on catalog pricing and customer needs.",
                    PermittedActions = new HashSet<AgentActionType> { AgentActionType.GenerateProposal, AgentActionType.ProposeDiscount, AgentActionType.CreateOpportunityInCRM }
                },
                AgentRole.CommercialCloser => new AgentIdentity
                {
                    Role = role,
                    Name = "Commercial Closing Agent",
                    Description = "Coordinates final commercial execution, approvals, and contract generation.",
                    PermittedActions = new HashSet<AgentActionType> { AgentActionType.CreateOpportunityInCRM, AgentActionType.GenerateProposal, AgentActionType.SendContract }
                },
                AgentRole.RiskEvaluator => new AgentIdentity
                {
                    Role = role,
                    Name = "Deal Risk Agent",
                    Description = "Continuously monitors deal velocity, competitor mentions, and engagement risk.",
                    PermittedActions = new HashSet<AgentActionType> { AgentActionType.ScoreLead }
                },
                _ => throw new ArgumentOutOfRangeException(nameof(role), $"Unknown agent role: {role}")
            };
        }
    }
}
