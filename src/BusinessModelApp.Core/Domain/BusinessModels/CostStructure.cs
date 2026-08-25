using BusinessModelApp.Core.Domain.Common;
using System;

namespace BusinessModelApp.Core.Domain.BusinessModels
{
    public class CostStructure : Entity, ISoftDeletable
    {
        public Guid BusinessModelId { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public CostType CostType { get; private set; }
        public decimal Amount { get; private set; }
        public BillingFrequency BillingFrequency { get; private set; }
        public bool IsFixed { get; private set; }
        public bool IsEssential { get; private set; }
        public DateTime? StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public bool IsDeleted { get; private set; }
        
        // Navigation properties
        public BusinessModel BusinessModel { get; private set; }

        private CostStructure() { } // For EF Core

        public CostStructure(
            Guid businessModelId,
            string name,
            string description,
            CostType costType,
            decimal amount,
            BillingFrequency billingFrequency,
            bool isFixed = true,
            bool isEssential = true,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            if (businessModelId == Guid.Empty)
                throw new ArgumentException("Business Model ID cannot be empty", nameof(businessModelId));
                
            BusinessModelId = businessModelId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            CostType = costType;
            Amount = amount >= 0 ? amount : throw new ArgumentException("Amount cannot be negative", nameof(amount));
            BillingFrequency = billingFrequency;
            IsFixed = isFixed;
            IsEssential = isEssential;
            
            // Validate dates
            if (endDate.HasValue && startDate.HasValue && endDate < startDate)
                throw new ArgumentException("End date cannot be before start date");
                
            StartDate = startDate;
            EndDate = endDate;
        }

        public void UpdateDetails(
            string name,
            string description,
            CostType costType,
            decimal amount,
            BillingFrequency billingFrequency,
            bool isFixed,
            bool isEssential,
            DateTime? startDate,
            DateTime? endDate)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            CostType = costType;
            Amount = amount >= 0 ? amount : throw new ArgumentException("Amount cannot be negative", nameof(amount));
            BillingFrequency = billingFrequency;
            IsFixed = isFixed;
            IsEssential = isEssential;
            
            // Validate dates
            if (endDate.HasValue && startDate.HasValue && endDate < startDate)
                throw new ArgumentException("End date cannot be before start date");
                
            StartDate = startDate;
            EndDate = endDate;
        }
        
        public bool IsActive()
        {
            var now = DateTime.UtcNow;
            bool isAfterStart = !StartDate.HasValue || StartDate <= now;
            bool isBeforeEnd = !EndDate.HasValue || EndDate >= now;
            return isAfterStart && isBeforeEnd;
        }

        public void MarkAsDeleted()
        {
            IsDeleted = true;
        }
    }

    public enum CostType
    {
        // Fixed Costs
        Salaries = 0,
        Rent = 1,
        Utilities = 2,
        Insurance = 3,
        SoftwareLicenses = 4,
        Hardware = 5,
        
        // Variable Costs
        Marketing = 20,
        SalesCommission = 21,
        Production = 22,
        Shipping = 23,
        PaymentProcessing = 24,
        
        // Semi-Variable Costs
        CustomerSupport = 40,
        Maintenance = 41,
        
        // Other
        Legal = 98,
        Other = 99
    }
}
