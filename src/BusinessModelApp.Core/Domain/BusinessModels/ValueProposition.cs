using BusinessModelApp.Core.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BusinessModelApp.Core.Domain.BusinessModels
{
    public class ValueProposition : Entity
    {
        public Guid BusinessModelId { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string UniqueSellingPoint { get; private set; }
        public string Benefits { get; private set; }
        public string Features { get; private set; }
        public decimal? Price { get; private set; }
        public string PricingModel { get; private set; }
        public string CompetitiveAdvantage { get; private set; }
        public string CustomerTestimonials { get; private set; }
        public string SuccessMetrics { get; private set; }
        public bool IsDifferentiator { get; private set; }
        public ValuePropositionStatus Status { get; private set; }
        
        // Navigation properties
        public BusinessModel BusinessModel { get; private set; }
        public ICollection<CustomerSegment> CustomerSegments { get; private set; } = new List<CustomerSegment>();
        public ICollection<KeyPartnership> KeyPartnerships { get; private set; } = new List<KeyPartnership>();
        public ICollection<CustomerRelationship> CustomerRelationships { get; private set; } = new List<CustomerRelationship>();
        public ICollection<RevenueStream> RevenueStreams { get; private set; } = new List<RevenueStream>();

        private ValueProposition() { } // For EF Core

        public ValueProposition(
            Guid businessModelId,
            string name,
            string description,
            string uniqueSellingPoint,
            string benefits = null,
            string features = null,
            decimal? price = null,
            string pricingModel = null,
            string competitiveAdvantage = null,
            string customerTestimonials = null,
            string successMetrics = null,
            bool isDifferentiator = false,
            ValuePropositionStatus status = ValuePropositionStatus.Draft)
        {
            if (businessModelId == Guid.Empty)
                throw new ArgumentException("Business Model ID cannot be empty", nameof(businessModelId));
                
            BusinessModelId = businessModelId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            UniqueSellingPoint = uniqueSellingPoint ?? throw new ArgumentNullException(nameof(uniqueSellingPoint));
            Benefits = benefits;
            Features = features;
            
            if (price.HasValue && price < 0)
                throw new ArgumentException("Price cannot be negative", nameof(price));
                
            Price = price;
            PricingModel = pricingModel;
            CompetitiveAdvantage = competitiveAdvantage;
            CustomerTestimonials = customerTestimonials;
            SuccessMetrics = successMetrics;
            IsDifferentiator = isDifferentiator;
            Status = status;
        }

        public void UpdateDetails(
            string name,
            string description,
            string uniqueSellingPoint,
            string benefits,
            string features,
            decimal? price,
            string pricingModel,
            string competitiveAdvantage,
            string customerTestimonials,
            string successMetrics,
            bool isDifferentiator)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            UniqueSellingPoint = uniqueSellingPoint ?? throw new ArgumentNullException(nameof(uniqueSellingPoint));
            Benefits = benefits;
            Features = features;
            
            if (price.HasValue && price < 0)
                throw new ArgumentException("Price cannot be negative", nameof(price));
                
            Price = price;
            PricingModel = pricingModel;
            CompetitiveAdvantage = competitiveAdvantage;
            CustomerTestimonials = customerTestimonials;
            SuccessMetrics = successMetrics;
            IsDifferentiator = isDifferentiator;
            
            UpdateTimestamps();
        }

        public void UpdateStatus(ValuePropositionStatus newStatus)
        {
            if (Status == newStatus)
                return;
                
            Status = newStatus;
            UpdateTimestamps();
        }
        
        public void ToggleDifferentiator()
        {
            IsDifferentiator = !IsDifferentiator;
            UpdateTimestamps();
        }
        
        public decimal? CalculateCustomerLifetimeValue()
        {
            if (!RevenueStreams.Any() || !CustomerSegments.Any())
                return null;
                
            // Simple calculation: (Average Revenue per Customer * Average Customer Lifespan) - Customer Acquisition Cost
            decimal? avgRevenuePerCustomer = RevenueStreams.Average(rs => rs.Amount);
            
            // This is a simplified calculation - in a real scenario, you'd have more detailed metrics
            int avgCustomerLifespanMonths = 12; // Default to 1 year
            
            decimal? cltv = avgRevenuePerCustomer * avgCustomerLifespanMonths;
            
            // Adjust for customer acquisition cost if available
            decimal? avgCac = CustomerSegments.Average(cs => cs.AcquisitionCost);
            if (avgCac.HasValue)
            {
                cltv -= avgCac;
            }
            
            return cltv >= 0 ? cltv : 0;
        }
    }

    public enum ValuePropositionStatus
    {
        Draft = 0,
        Validated = 1,
        Active = 2,
        UnderReview = 3,
        Retired = 4
    }
}
