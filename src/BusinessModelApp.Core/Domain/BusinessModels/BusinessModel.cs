using BusinessModelApp.Core.Domain.Common;
using BusinessModelApp.Core.Domain.Users;
using System;
using System.Collections.Generic;

namespace BusinessModelApp.Core.Domain.BusinessModels
{
    public class BusinessModel : Entity, ISoftDeletable
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public Guid CreatedById { get; private set; }
        public BusinessModelStatus Status { get; private set; }
        public DateTime? PublishedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        
        // Navigation properties
        public User CreatedBy { get; private set; }
        public ICollection<RevenueStream> RevenueStreams { get; private set; } = new List<RevenueStream>();
        public ICollection<CostStructure> CostStructures { get; private set; } = new List<CostStructure>();
        public ICollection<KeyActivity> KeyActivities { get; private set; } = new List<KeyActivity>();
        public ICollection<KeyResource> KeyResources { get; private set; } = new List<KeyResource>();
        public ICollection<KeyPartnership> KeyPartnerships { get; private set; } = new List<KeyPartnership>();
        public ICollection<CustomerSegment> CustomerSegments { get; private set; } = new List<CustomerSegment>();
        public ICollection<Channel> Channels { get; private set; } = new List<Channel>();
        public ICollection<CustomerRelationship> CustomerRelationships { get; private set; } = new List<CustomerRelationship>();
        public ICollection<ValueProposition> ValuePropositions { get; private set; } = new List<ValueProposition>();

        private BusinessModel() { } // For EF Core

        public BusinessModel(string name, string description, Guid createdById)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            CreatedById = createdById != Guid.Empty ? createdById : throw new ArgumentException("Created by ID cannot be empty", nameof(createdById));
            Status = BusinessModelStatus.Draft;
        }

        public void UpdateDetails(string name, string description)
        {
            if (Status == BusinessModelStatus.Published)
                throw new InvalidOperationException("Cannot update a published business model.");
                
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
        }

        public void Publish()
        {
            if (Status == BusinessModelStatus.Published)
                return;
                
            Status = BusinessModelStatus.Published;
            PublishedAt = DateTime.UtcNow;
        }

        public void Retract()
        {
            if (Status != BusinessModelStatus.Published)
                return;
                
            Status = BusinessModelStatus.Draft;
        }

        public void MarkAsDeleted()
        {
            IsDeleted = true;
        }
    }

    public enum BusinessModelStatus
    {
        Draft = 0,
        Published = 1,
        Archived = 2
    }
}
