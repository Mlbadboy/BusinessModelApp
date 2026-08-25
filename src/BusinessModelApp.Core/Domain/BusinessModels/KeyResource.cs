using BusinessModelApp.Core.Domain.Common;
using System;

namespace BusinessModelApp.Core.Domain.BusinessModels
{
    public class KeyResource : Entity
    {
        public Guid BusinessModelId { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public ResourceType ResourceType { get; private set; }
        public ResourceOwnership Ownership { get; private set; }
        public decimal? Cost { get; private set; }
        public BillingFrequency? BillingFrequency { get; private set; }
        public bool IsShared { get; private set; }
        public int? Quantity { get; private set; }
        public string Unit { get; private set; }
        public DateTime? AcquisitionDate { get; private set; }
        public DateTime? ExpirationDate { get; private set; }
        public string Provider { get; private set; }
        public string ContactInfo { get; private set; }
        
        // Navigation properties
        public BusinessModel BusinessModel { get; private set; }
        public ICollection<KeyActivity> Activities { get; private set; } = new List<KeyActivity>();
        public ICollection<KeyPartnership> Partnerships { get; private set; } = new List<KeyPartnership>();

        private KeyResource() { } // For EF Core

        public KeyResource(
            Guid businessModelId,
            string name,
            string description,
            ResourceType resourceType,
            ResourceOwnership ownership,
            decimal? cost = null,
            BillingFrequency? billingFrequency = null,
            bool isShared = false,
            int? quantity = null,
            string unit = null,
            DateTime? acquisitionDate = null,
            DateTime? expirationDate = null,
            string provider = null,
            string contactInfo = null)
        {
            if (businessModelId == Guid.Empty)
                throw new ArgumentException("Business Model ID cannot be empty", nameof(businessModelId));
                
            BusinessModelId = businessModelId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            ResourceType = resourceType;
            Ownership = ownership;
            
            if (cost.HasValue && cost < 0)
                throw new ArgumentException("Cost cannot be negative", nameof(cost));
                
            Cost = cost;
            BillingFrequency = billingFrequency;
            IsShared = isShared;
            
            if (quantity.HasValue && quantity <= 0)
                throw new ArgumentException("Quantity must be positive", nameof(quantity));
                
            Quantity = quantity;
            Unit = unit;
            
            // Validate dates
            if (expirationDate.HasValue && acquisitionDate.HasValue && expirationDate < acquisitionDate)
                throw new ArgumentException("Expiration date cannot be before acquisition date");
                
            AcquisitionDate = acquisitionDate;
            ExpirationDate = expirationDate;
            Provider = provider;
            ContactInfo = contactInfo;
        }

        public void UpdateDetails(
            string name,
            string description,
            ResourceType resourceType,
            ResourceOwnership ownership,
            decimal? cost,
            BillingFrequency? billingFrequency,
            bool isShared,
            int? quantity,
            string unit,
            DateTime? acquisitionDate,
            DateTime? expirationDate,
            string provider,
            string contactInfo)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            ResourceType = resourceType;
            Ownership = ownership;
            
            if (cost.HasValue && cost < 0)
                throw new ArgumentException("Cost cannot be negative", nameof(cost));
                
            Cost = cost;
            BillingFrequency = billingFrequency;
            IsShared = isShared;
            
            if (quantity.HasValue && quantity <= 0)
                throw new ArgumentException("Quantity must be positive", nameof(quantity));
                
            Quantity = quantity;
            Unit = unit;
            
            // Validate dates
            if (expirationDate.HasValue && acquisitionDate.HasValue && expirationDate < acquisitionDate)
                throw new ArgumentException("Expiration date cannot be before acquisition date");
                
            AcquisitionDate = acquisitionDate;
            ExpirationDate = expirationDate;
            Provider = provider;
            ContactInfo = contactInfo;
            
            UpdateTimestamps();
        }
        
        public bool IsActive()
        {
            var now = DateTime.UtcNow;
            bool isAfterAcquisition = !AcquisitionDate.HasValue || AcquisitionDate <= now;
            bool isBeforeExpiration = !ExpirationDate.HasValue || ExpirationDate >= now;
            return isAfterAcquisition && isBeforeExpiration;
        }
    }

    public enum ResourceType
    {
        // Physical Resources
        Equipment = 0,
        Inventory = 1,
        Facilities = 2,
        Vehicles = 3,
        
        // Intellectual Resources
        Patents = 10,
        Copyrights = 11,
        Trademarks = 12,
        TradeSecrets = 13,
        
        // Human Resources
        Employees = 20,
        Contractors = 21,
        Consultants = 22,
        
        // Digital Resources
        Software = 30,
        Data = 31,
        CloudInfrastructure = 32,
        
        // Financial Resources
        Cash = 40,
        Credit = 41,
        Investments = 42,
        
        // Other
        Other = 99
    }

    public enum ResourceOwnership
    {
        Owned = 0,
        Leased = 1,
        Licensed = 2,
        Rented = 3,
        Subscription = 4,
        OpenSource = 5,
        Other = 99
    }
}
