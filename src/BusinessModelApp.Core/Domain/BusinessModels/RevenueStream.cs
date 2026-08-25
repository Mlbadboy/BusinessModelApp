using BusinessModelApp.Core.Domain.Common;
using System;

namespace BusinessModelApp.Core.Domain.BusinessModels
{
    public class RevenueStream : Entity, ISoftDeletable
    {
        public Guid BusinessModelId { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public RevenueType RevenueType { get; private set; }
        public decimal Amount { get; private set; }
        public BillingFrequency BillingFrequency { get; private set; }
        public bool IsRecurring { get; private set; }
        public int? RecurringInterval { get; private set; } // In months for recurring revenue
        public bool IsDeleted { get; private set; }
        
        // Navigation properties
        public BusinessModel BusinessModel { get; private set; }

        private RevenueStream() { } // For EF Core

        public RevenueStream(
            Guid businessModelId, 
            string name, 
            string description, 
            RevenueType revenueType, 
            decimal amount, 
            BillingFrequency billingFrequency,
            bool isRecurring = false,
            int? recurringInterval = null)
        {
            if (businessModelId == Guid.Empty) 
                throw new ArgumentException("Business Model ID cannot be empty", nameof(businessModelId));
                
            BusinessModelId = businessModelId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            RevenueType = revenueType;
            Amount = amount >= 0 ? amount : throw new ArgumentException("Amount cannot be negative", nameof(amount));
            BillingFrequency = billingFrequency;
            IsRecurring = isRecurring;
            
            if (isRecurring && (!recurringInterval.HasValue || recurringInterval <= 0))
                throw new ArgumentException("Recurring interval must be specified for recurring revenue", nameof(recurringInterval));
                
            RecurringInterval = recurringInterval;
        }

        public void UpdateDetails(
            string name, 
            string description, 
            RevenueType revenueType, 
            decimal amount, 
            BillingFrequency billingFrequency,
            bool isRecurring,
            int? recurringInterval)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            RevenueType = revenueType;
            Amount = amount >= 0 ? amount : throw new ArgumentException("Amount cannot be negative", nameof(amount));
            BillingFrequency = billingFrequency;
            IsRecurring = isRecurring;
            
            if (isRecurring && (!recurringInterval.HasValue || recurringInterval <= 0))
                throw new ArgumentException("Recurring interval must be specified for recurring revenue", nameof(recurringInterval));
                
            RecurringInterval = recurringInterval;
        }

        public void MarkAsDeleted()
        {
            IsDeleted = true;
        }
    }

    public enum RevenueType
    {
        ProductSales = 0,
        Subscription = 1,
        Service = 2,
        Licensing = 3,
        Advertising = 4,
        Commission = 5,
        Other = 99
    }

    public enum BillingFrequency
    {
        OneTime = 0,
        Daily = 1,
        Weekly = 2,
        BiWeekly = 3,
        Monthly = 4,
        Quarterly = 5,
        SemiAnnually = 6,
        Annually = 7
    }
}
