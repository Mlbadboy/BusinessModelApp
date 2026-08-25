using BusinessModelApp.Core.Domain.Common;
using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.BusinessModels
{
    public class Channel : Entity
    {
        public Guid BusinessModelId { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public ChannelType ChannelType { get; private set; }
        public ChannelPurpose Purpose { get; private set; }
        public ChannelOwnership Ownership { get; private set; }
        public string Url { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime? LaunchDate { get; private set; }
        public decimal? CostPerAcquisition { get; private set; }
        public decimal? MonthlyCost { get; private set; }
        public string IntegrationDetails { get; private set; }
        public string PerformanceMetrics { get; private set; }
        
        // Navigation properties
        public BusinessModel BusinessModel { get; private set; }
        public ICollection<CustomerSegment> CustomerSegments { get; private set; } = new List<CustomerSegment>();
        public ICollection<CustomerRelationship> CustomerRelationships { get; private set; } = new List<CustomerRelationship>();

        private Channel() { } // For EF Core

        public Channel(
            Guid businessModelId,
            string name,
            string description,
            ChannelType channelType,
            ChannelPurpose purpose,
            ChannelOwnership ownership,
            string? url = null,
            bool isActive = true,
            DateTime? launchDate = null,
            decimal? costPerAcquisition = null,
            decimal? monthlyCost = null,
            string? integrationDetails = null,
            string? performanceMetrics = null)
        {
            if (businessModelId == Guid.Empty)
                throw new ArgumentException("Business Model ID cannot be empty", nameof(businessModelId));
                
            BusinessModelId = businessModelId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            ChannelType = channelType;
            Purpose = purpose;
            Ownership = ownership;
            Url = url;
            IsActive = isActive;
            LaunchDate = launchDate;
            
            if (costPerAcquisition.HasValue && costPerAcquisition < 0)
                throw new ArgumentException("Cost per acquisition cannot be negative", nameof(costPerAcquisition));
                
            CostPerAcquisition = costPerAcquisition;
            
            if (monthlyCost.HasValue && monthlyCost < 0)
                throw new ArgumentException("Monthly cost cannot be negative", nameof(monthlyCost));
                
            MonthlyCost = monthlyCost;
            IntegrationDetails = integrationDetails;
            PerformanceMetrics = performanceMetrics;
        }

        public void UpdateDetails(
            string name,
            string description,
            ChannelType channelType,
            ChannelPurpose purpose,
            ChannelOwnership ownership,
            string url,
            bool isActive,
            DateTime? launchDate,
            decimal? costPerAcquisition,
            decimal? monthlyCost,
            string integrationDetails,
            string performanceMetrics)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            ChannelType = channelType;
            Purpose = purpose;
            Ownership = ownership;
            Url = url;
            IsActive = isActive;
            LaunchDate = launchDate;
            
            if (costPerAcquisition.HasValue && costPerAcquisition < 0)
                throw new ArgumentException("Cost per acquisition cannot be negative", nameof(costPerAcquisition));
                
            CostPerAcquisition = costPerAcquisition;
            
            if (monthlyCost.HasValue && monthlyCost < 0)
                throw new ArgumentException("Monthly cost cannot be negative", nameof(monthlyCost));
                
            MonthlyCost = monthlyCost;
            IntegrationDetails = integrationDetails;
            PerformanceMetrics = performanceMetrics;
            
            UpdateTimestamps();
        }
        
        public void ToggleActive()
        {
            IsActive = !IsActive;
            UpdateTimestamps();
        }
    }

    public enum ChannelType
    {
        // Digital Channels
        Website = 0,
        MobileApp = 1,
        SocialMedia = 2,
        Email = 3,
        SEO = 4,
        PPC = 5,
        Affiliate = 6,
        Marketplaces = 7,
        Webinars = 8,
        
        // Physical Channels
        RetailStore = 20,
        Kiosk = 21,
        PopupShop = 22,
        VendingMachine = 23,
        
        // Direct Sales
        SalesTeam = 30,
        DirectMail = 31,
        Telemarketing = 32,
        FieldSales = 33,
        
        // Partner Channels
        Wholesale = 40,
        Distributors = 41,
        Resellers = 42,
        Franchise = 43,
        
        // Other
        Events = 50,
        PrintMedia = 51,
        Broadcast = 52,
        Other = 99
    }

    [Flags]
    public enum ChannelPurpose
    {
        None = 0,
        Awareness = 1,
        Evaluation = 2,
        Purchase = 4,
        Delivery = 8,
        Support = 16,
        Retention = 32,
        Advocacy = 64,
        All = Awareness | Evaluation | Purchase | Delivery | Support | Retention | Advocacy
    }

    public enum ChannelOwnership
    {
        Owned = 0,
        Partner = 1,
        Paid = 2,
        Earned = 3,
        Shared = 4
    }
}
