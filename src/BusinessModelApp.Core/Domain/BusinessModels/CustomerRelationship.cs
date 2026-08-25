using BusinessModelApp.Core.Domain.Common;
using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.BusinessModels
{
    public class CustomerRelationship : Entity
    {
        public Guid BusinessModelId { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public RelationshipType RelationshipType { get; private set; }
        public AutomationLevel AutomationLevel { get; private set; }
        public string Touchpoints { get; private set; }
        public string CommunicationStyle { get; private set; }
        public string SuccessMetrics { get; private set; }
        public decimal? CostToServe { get; private set; }
        public decimal? LifetimeValue { get; private set; }
        public bool IsActive { get; private set; }
        
        // Navigation properties
        public BusinessModel BusinessModel { get; private set; }
        public ICollection<CustomerSegment> CustomerSegments { get; private set; } = new List<CustomerSegment>();
        public ICollection<Channel> Channels { get; private set; } = new List<Channel>();
        public ICollection<ValueProposition> ValuePropositions { get; private set; } = new List<ValueProposition>();

        private CustomerRelationship() { } // For EF Core

        public CustomerRelationship(
            Guid businessModelId,
            string name,
            string description,
            RelationshipType relationshipType,
            AutomationLevel automationLevel,
            string touchpoints = null,
            string communicationStyle = null,
            string successMetrics = null,
            decimal? costToServe = null,
            decimal? lifetimeValue = null,
            bool isActive = true)
        {
            if (businessModelId == Guid.Empty)
                throw new ArgumentException("Business Model ID cannot be empty", nameof(businessModelId));
                
            BusinessModelId = businessModelId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            RelationshipType = relationshipType;
            AutomationLevel = automationLevel;
            Touchpoints = touchpoints;
            CommunicationStyle = communicationStyle;
            SuccessMetrics = successMetrics;
            
            if (costToServe.HasValue && costToServe < 0)
                throw new ArgumentException("Cost to serve cannot be negative", nameof(costToServe));
                
            CostToServe = costToServe;
            
            if (lifetimeValue.HasValue && lifetimeValue < 0)
                throw new ArgumentException("Lifetime value cannot be negative", nameof(lifetimeValue));
                
            LifetimeValue = lifetimeValue;
            IsActive = isActive;
        }

        public void UpdateDetails(
            string name,
            string description,
            RelationshipType relationshipType,
            AutomationLevel automationLevel,
            string touchpoints,
            string communicationStyle,
            string successMetrics,
            decimal? costToServe,
            decimal? lifetimeValue,
            bool isActive)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            RelationshipType = relationshipType;
            AutomationLevel = automationLevel;
            Touchpoints = touchpoints;
            CommunicationStyle = communicationStyle;
            SuccessMetrics = successMetrics;
            
            if (costToServe.HasValue && costToServe < 0)
                throw new ArgumentException("Cost to serve cannot be negative", nameof(costToServe));
                
            CostToServe = costToServe;
            
            if (lifetimeValue.HasValue && lifetimeValue < 0)
                throw new ArgumentException("Lifetime value cannot be negative", nameof(lifetimeValue));
                
            LifetimeValue = lifetimeValue;
            IsActive = isActive;
            
            UpdateTimestamps();
        }
        
        public void ToggleActive()
        {
            IsActive = !IsActive;
            UpdateTimestamps();
        }
    }

    public enum RelationshipType
    {
        // Personal Assistance
        PersonalAssistance = 0,
        DedicatedPersonalAssistance = 1,
        
        // Self-Service
        SelfService = 10,
        AutomatedServices = 11,
        
        // Communities
        Communities = 20,
        UserCommunities = 21,
        ExpertCommunities = 22,
        
        // Co-Creation
        CoCreation = 30,
        IdeaCompetitions = 31,
        
        // Other
        Transactional = 90,
        LongTerm = 91,
        Other = 99
    }

    public enum AutomationLevel
    {
        None = 0,           // Fully manual
        Basic = 1,          // Simple automations
        Standard = 2,       // Common automations
        Advanced = 3,       // Complex automations
        Full = 4,           // Fully automated
        AIEnhanced = 5      // AI-driven automation
    }
}
