using BusinessModelApp.Core.Domain.Common;
using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.BusinessModels
{
    public class KeyPartnership : Entity
    {
        public Guid BusinessModelId { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public PartnershipType PartnershipType { get; private set; }
        public string PartnerCompany { get; private set; }
        public string ContactPerson { get; private set; }
        public string ContactEmail { get; private set; }
        public string ContactPhone { get; private set; }
        public DateTime? StartDate { get; private set; }
        public DateTime? EndDate { get; private set; }
        public bool IsExclusive { get; private set; }
        public PartnershipStatus Status { get; private set; }
        public string AgreementDetails { get; private set; }
        public decimal? RevenueSharePercentage { get; private set; }
        public decimal? FixedFee { get; private set; }
        public BillingFrequency? BillingFrequency { get; private set; }
        
        // Navigation properties
        public BusinessModel BusinessModel { get; private set; }
        public ICollection<KeyResource> SharedResources { get; private set; } = new List<KeyResource>();
        public ICollection<ValueProposition> ValuePropositions { get; private set; } = new List<ValueProposition>();

        private KeyPartnership() { } // For EF Core

        public KeyPartnership(
            Guid businessModelId,
            string name,
            string description,
            PartnershipType partnershipType,
            string partnerCompany,
            string contactPerson = null,
            string contactEmail = null,
            string contactPhone = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool isExclusive = false,
            PartnershipStatus status = PartnershipStatus.Proposed,
            string agreementDetails = null,
            decimal? revenueSharePercentage = null,
            decimal? fixedFee = null,
            BillingFrequency? billingFrequency = null)
        {
            if (businessModelId == Guid.Empty)
                throw new ArgumentException("Business Model ID cannot be empty", nameof(businessModelId));
                
            BusinessModelId = businessModelId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            PartnershipType = partnershipType;
            PartnerCompany = partnerCompany ?? throw new ArgumentNullException(nameof(partnerCompany));
            ContactPerson = contactPerson;
            ContactEmail = contactEmail;
            ContactPhone = contactPhone;
            
            // Validate dates
            if (endDate.HasValue && startDate.HasValue && endDate < startDate)
                throw new ArgumentException("End date cannot be before start date");
                
            StartDate = startDate;
            EndDate = endDate;
            
            IsExclusive = isExclusive;
            Status = status;
            AgreementDetails = agreementDetails;
            
            if (revenueSharePercentage.HasValue && (revenueSharePercentage < 0 || revenueSharePercentage > 100))
                throw new ArgumentException("Revenue share percentage must be between 0 and 100", nameof(revenueSharePercentage));
                
            RevenueSharePercentage = revenueSharePercentage;
            
            if (fixedFee.HasValue && fixedFee < 0)
                throw new ArgumentException("Fixed fee cannot be negative", nameof(fixedFee));
                
            FixedFee = fixedFee;
            BillingFrequency = billingFrequency;
        }

        public void UpdateDetails(
            string name,
            string description,
            PartnershipType partnershipType,
            string partnerCompany,
            string contactPerson,
            string contactEmail,
            string contactPhone,
            DateTime? startDate,
            DateTime? endDate,
            bool isExclusive,
            string agreementDetails,
            decimal? revenueSharePercentage,
            decimal? fixedFee,
            BillingFrequency? billingFrequency)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            PartnershipType = partnershipType;
            PartnerCompany = partnerCompany ?? throw new ArgumentNullException(nameof(partnerCompany));
            ContactPerson = contactPerson;
            ContactEmail = contactEmail;
            ContactPhone = contactPhone;
            
            // Validate dates
            if (endDate.HasValue && startDate.HasValue && endDate < startDate)
                throw new ArgumentException("End date cannot be before start date");
                
            StartDate = startDate;
            EndDate = endDate;
            
            IsExclusive = isExclusive;
            AgreementDetails = agreementDetails;
            
            if (revenueSharePercentage.HasValue && (revenueSharePercentage < 0 || revenueSharePercentage > 100))
                throw new ArgumentException("Revenue share percentage must be between 0 and 100", nameof(revenueSharePercentage));
                
            RevenueSharePercentage = revenueSharePercentage;
            
            if (fixedFee.HasValue && fixedFee < 0)
                throw new ArgumentException("Fixed fee cannot be negative", nameof(fixedFee));
                
            FixedFee = fixedFee;
            BillingFrequency = billingFrequency;
            
            UpdateTimestamps();
        }

        public void UpdateStatus(PartnershipStatus newStatus)
        {
            if (Status == newStatus)
                return;
                
            Status = newStatus;
            
            if (newStatus == PartnershipStatus.Active && !StartDate.HasValue)
            {
                StartDate = DateTime.UtcNow;
            }
            else if (newStatus == PartnershipStatus.Terminated && !EndDate.HasValue)
            {
                EndDate = DateTime.UtcNow;
            }
            
            UpdateTimestamps();
        }
        
        public bool IsActive()
        {
            if (Status != PartnershipStatus.Active)
                return false;
                
            var now = DateTime.UtcNow;
            bool isAfterStart = !StartDate.HasValue || StartDate <= now;
            bool isBeforeEnd = !EndDate.HasValue || EndDate >= now;
            return isAfterStart && isBeforeEnd;
        }
    }

    public enum PartnershipType
    {
        StrategicAlliance = 0,
        JointVenture = 1,
        BuyerSupplier = 2,
        Coopetition = 3,
        Distribution = 4,
        Licensing = 5,
        Franchising = 6,
        TechnologySharing = 7,
        Marketing = 8,
        ResearchAndDevelopment = 9,
        Other = 99
    }

    public enum PartnershipStatus
    {
        Proposed = 0,
        UnderReview = 1,
        Negotiation = 2,
        Active = 3,
        OnHold = 4,
        Terminated = 5,
        Rejected = 6
    }
}
