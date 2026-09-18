using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.Runtime.Enterprise.Workforce
{
    public class WorkforceDepartment
    {
        public string DepartmentId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty; // Executive, Revenue, Marketing, Operations, Finance, Technology
        public string Description { get; set; } = string.Empty;
        public string HeadAgentId { get; set; } = string.Empty;
        public List<WorkforceTeam> Teams { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class WorkforceTeam
    {
        public string TeamId { get; set; } = Guid.NewGuid().ToString("N");
        public string DepartmentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty; // e.g. Prospecting, Account Research, Sales, Proposal, Delivery, Collections
        public string LeadAgentId { get; set; } = string.Empty;
        public List<string> AssignedAgentIds { get; set; } = new();
    }

    public class WorkforceRole
    {
        public string RoleId { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty; // e.g. "RevenueProspector", "AccountResearcher", "ProposalArchitect", "DeliveryCoordinator", "CollectionsSpecialist"
        public string DepartmentId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> StandardResponsibilities { get; set; } = new();
        public List<string> PermittedSkillIds { get; set; } = new();
        public List<string> PermittedToolIds { get; set; } = new();
        public int DefaultRiskCeiling { get; set; } = 2; // R1 or R2 default
    }

    public class WorkforceOrganization
    {
        public string OrganizationId { get; set; } = Guid.NewGuid().ToString("N");
        public string TenantId { get; set; } = string.Empty;
        public string Name { get; set; } = "Charlie Business Organization";
        public string CeoAgentId { get; set; } = "charlie-executive-ceo";
        public List<WorkforceDepartment> Departments { get; set; } = new();
        public List<WorkforceRole> DefinedRoles { get; set; } = new();
        public DateTime InitializedAt { get; set; } = DateTime.UtcNow;
    }
}
