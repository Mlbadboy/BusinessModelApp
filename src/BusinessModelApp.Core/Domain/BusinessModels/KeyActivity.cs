using BusinessModelApp.Core.Domain.Common;
using System;
using BusinessModelApp.Core.Domain.Users;

namespace BusinessModelApp.Core.Domain.BusinessModels
{
    public class KeyActivity : Entity
    {
        public Guid BusinessModelId { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public ActivityType ActivityType { get; private set; }
        public ActivityPriority Priority { get; private set; }
        public ActivityStatus Status { get; private set; }
        public DateTime? StartDate { get; private set; }
        public DateTime? TargetDate { get; private set; }
        public DateTime? CompletionDate { get; private set; }
        public Guid? AssignedToId { get; private set; }
        public decimal? Budget { get; private set; }
        public decimal? ActualCost { get; private set; }
        
        // Navigation properties
        public BusinessModel BusinessModel { get; private set; }
        public User AssignedTo { get; private set; }
        public ICollection<KeyResource> Resources { get; private set; } = new List<KeyResource>();

        private KeyActivity() { } // For EF Core

        public KeyActivity(
            Guid businessModelId,
            string name,
            string description,
            ActivityType activityType,
            ActivityPriority priority = ActivityPriority.Medium,
            DateTime? startDate = null,
            DateTime? targetDate = null,
            Guid? assignedToId = null,
            decimal? budget = null)
        {
            if (businessModelId == Guid.Empty)
                throw new ArgumentException("Business Model ID cannot be empty", nameof(businessModelId));
                
            BusinessModelId = businessModelId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            ActivityType = activityType;
            Priority = priority;
            Status = ActivityStatus.NotStarted;
            StartDate = startDate;
            TargetDate = targetDate;
            AssignedToId = assignedToId;
            Budget = budget >= 0 ? budget : throw new ArgumentException("Budget cannot be negative", nameof(budget));
        }

        public void UpdateDetails(
            string name,
            string description,
            ActivityType activityType,
            ActivityPriority priority,
            DateTime? startDate,
            DateTime? targetDate,
            Guid? assignedToId,
            decimal? budget)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            ActivityType = activityType;
            Priority = priority;
            StartDate = startDate;
            TargetDate = targetDate;
            AssignedToId = assignedToId;
            Budget = budget >= 0 ? budget : throw new ArgumentException("Budget cannot be negative", nameof(budget));
        }

        public void UpdateStatus(ActivityStatus newStatus, DateTime? completionDate = null)
        {
            if (Status == newStatus)
                return;
                
            Status = newStatus;
            
            if (newStatus == ActivityStatus.Completed)
            {
                CompletionDate = completionDate ?? DateTime.UtcNow;
            }
            else if (newStatus == ActivityStatus.InProgress && !StartDate.HasValue)
            {
                StartDate = DateTime.UtcNow;
            }
        }

        public void UpdateCost(decimal actualCost)
        {
            ActualCost = actualCost >= 0 ? actualCost : throw new ArgumentException("Actual cost cannot be negative", nameof(actualCost));
        }
    }

    public enum ActivityType
    {
        // Production
        ProductDevelopment = 0,
        Manufacturing = 1,
        QualityAssurance = 2,
        
        // Service
        CustomerSupport = 10,
        Consulting = 11,
        Training = 12,
        
        // Sales & Marketing
        MarketingCampaign = 20,
        SalesOutreach = 21,
        PartnershipDevelopment = 22,
        
        // Operations
        ProcessOptimization = 30,
        SupplyChainManagement = 31,
        Logistics = 32,
        
        // Technology
        ResearchAndDevelopment = 40,
        ITInfrastructure = 41,
        DataAnalysis = 42,
        
        // Other
        Administrative = 98,
        Other = 99
    }

    public enum ActivityPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }

    public enum ActivityStatus
    {
        NotStarted = 0,
        InProgress = 1,
        OnHold = 2,
        Completed = 3,
        Cancelled = 4
    }
}
