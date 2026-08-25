using System;
using System.Collections.Generic;
using BusinessModelApp.Core.Domain.Commercial;

namespace BusinessModelApp.Core.DTOs.Commercial
{
    public class LeadDto
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Status { get; set; } = "New";
        public string Source { get; set; } = "Manual";
        public double QualityScore { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool HasOpportunity { get; set; }
        public Guid? OpportunityId { get; set; }
    }

    public class CreateLeadDto
    {
        public string ContactName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public LeadSource Source { get; set; } = LeadSource.Manual;
        public string Notes { get; set; } = string.Empty;
    }

    public class OpportunityDto
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid LeadId { get; set; }
        public string LeadContactName { get; set; } = string.Empty;
        public string LeadCompanyName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal EstimatedValue { get; set; }
        public string Currency { get; set; } = "INR";
        public string Stage { get; set; } = "Discovery";
        public double Probability { get; set; }
        public DateTime? ExpectedCloseDate { get; set; }
        public string PrimaryConcern { get; set; } = string.Empty;
        public string NextStep { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ActivityDto> RecentActivities { get; set; } = new List<ActivityDto>();
    }

    public class CreateOpportunityDto
    {
        public Guid LeadId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal EstimatedValue { get; set; }
        public string Currency { get; set; } = "INR";
        public DateTime? ExpectedCloseDate { get; set; }
        public string PrimaryConcern { get; set; } = string.Empty;
        public string NextStep { get; set; } = string.Empty;
    }

    public class UpdateOpportunityStageDto
    {
        public OpportunityStage Stage { get; set; }
        public string ReasonOrNote { get; set; } = string.Empty;
    }

    public class ActivityDto
    {
        public Guid Id { get; set; }
        public Guid OpportunityId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PerformedByName { get; set; } = "System";
        public DateTime CreatedAt { get; set; }
    }

    public class CreateActivityDto
    {
        public ActivityType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
