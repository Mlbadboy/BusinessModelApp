using BusinessModelApp.Core.Domain.Common;
using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.BusinessModels
{
    public class CustomerSegment : Entity
    {
        public Guid BusinessModelId { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public SegmentType SegmentType { get; private set; }
        public int? EstimatedSize { get; private set; }
        public string GeographicLocation { get; private set; }
        public string DemographicInfo { get; private set; }
        public string PsychographicInfo { get; private set; }
        public string Needs { get; private set; }
        public string PainPoints { get; private set; }
        public string PreferredChannels { get; private set; }
        public decimal? CustomerLifetimeValue { get; private set; }
        public decimal? AcquisitionCost { get; private set; }
        public int? Priority { get; private set; }
        public bool IsTarget { get; private set; }
        
        // Navigation properties
        public BusinessModel BusinessModel { get; private set; }
        public ICollection<Channel> Channels { get; private set; } = new List<Channel>();
        public ICollection<CustomerRelationship> CustomerRelationships { get; private set; } = new List<CustomerRelationship>();
        public ICollection<ValueProposition> ValuePropositions { get; private set; } = new List<ValueProposition>();

        private CustomerSegment() { } // For EF Core

        public CustomerSegment(
            Guid businessModelId,
            string name,
            string description,
            SegmentType segmentType,
            int? estimatedSize = null,
            string geographicLocation = null,
            string demographicInfo = null,
            string psychographicInfo = null,
            string needs = null,
            string painPoints = null,
            string preferredChannels = null,
            decimal? customerLifetimeValue = null,
            decimal? acquisitionCost = null,
            int? priority = null,
            bool isTarget = true)
        {
            if (businessModelId == Guid.Empty)
                throw new ArgumentException("Business Model ID cannot be empty", nameof(businessModelId));
                
            BusinessModelId = businessModelId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            SegmentType = segmentType;
            
            if (estimatedSize.HasValue && estimatedSize <= 0)
                throw new ArgumentException("Estimated size must be positive", nameof(estimatedSize));
                
            EstimatedSize = estimatedSize;
            GeographicLocation = geographicLocation;
            DemographicInfo = demographicInfo;
            PsychographicInfo = psychographicInfo;
            Needs = needs;
            PainPoints = painPoints;
            PreferredChannels = preferredChannels;
            
            if (customerLifetimeValue.HasValue && customerLifetimeValue < 0)
                throw new ArgumentException("Customer lifetime value cannot be negative", nameof(customerLifetimeValue));
                
            CustomerLifetimeValue = customerLifetimeValue;
            
            if (acquisitionCost.HasValue && acquisitionCost < 0)
                throw new ArgumentException("Acquisition cost cannot be negative", nameof(acquisitionCost));
                
            AcquisitionCost = acquisitionCost;
            
            if (priority.HasValue && priority <= 0)
                throw new ArgumentException("Priority must be a positive number", nameof(priority));
                
            Priority = priority;
            IsTarget = isTarget;
        }

        public void UpdateDetails(
            string name,
            string description,
            SegmentType segmentType,
            int? estimatedSize,
            string geographicLocation,
            string demographicInfo,
            string psychographicInfo,
            string needs,
            string painPoints,
            string preferredChannels,
            decimal? customerLifetimeValue,
            decimal? acquisitionCost,
            int? priority,
            bool isTarget)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            SegmentType = segmentType;
            
            if (estimatedSize.HasValue && estimatedSize <= 0)
                throw new ArgumentException("Estimated size must be positive", nameof(estimatedSize));
                
            EstimatedSize = estimatedSize;
            GeographicLocation = geographicLocation;
            DemographicInfo = demographicInfo;
            PsychographicInfo = psychographicInfo;
            Needs = needs;
            PainPoints = painPoints;
            PreferredChannels = preferredChannels;
            
            if (customerLifetimeValue.HasValue && customerLifetimeValue < 0)
                throw new ArgumentException("Customer lifetime value cannot be negative", nameof(customerLifetimeValue));
                
            CustomerLifetimeValue = customerLifetimeValue;
            
            if (acquisitionCost.HasValue && acquisitionCost < 0)
                throw new ArgumentException("Acquisition cost cannot be negative", nameof(acquisitionCost));
                
            AcquisitionCost = acquisitionCost;
            
            if (priority.HasValue && priority <= 0)
                throw new ArgumentException("Priority must be a positive number", nameof(priority));
                
            Priority = priority;
            IsTarget = isTarget;
            
            UpdateTimestamps();
        }
        
        public void ToggleTargetStatus()
        {
            IsTarget = !IsTarget;
            UpdateTimestamps();
        }
    }

    public enum SegmentType
    {
        // B2C Segments
        MassMarket = 0,
        NicheMarket = 1,
        Segmented = 2,
        Diversified = 3,
        MultiSided = 4,
        
        // B2B Segments
        Enterprise = 10,
        SmallBusiness = 11,
        Startups = 12,
        NonProfit = 13,
        Government = 14,
        Education = 15,
        
        // Other
        Other = 99
    }
}
