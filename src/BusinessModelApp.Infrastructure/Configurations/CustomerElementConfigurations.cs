using BusinessModelApp.Core.Domain.BusinessModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace BusinessModelApp.Infrastructure.Configurations
{
    public class CustomerSegmentConfiguration : IEntityTypeConfiguration<CustomerSegment>
    {
        public void Configure(EntityTypeBuilder<CustomerSegment> builder)
        {
            builder.ToTable("CustomerSegments");
            
            builder.HasKey(cs => cs.Id);
            
            builder.Property(cs => cs.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(cs => cs.Description)
                .HasMaxLength(1000);
                
            builder.Property(cs => cs.SegmentType)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(cs => cs.EstimatedSize)
                .IsRequired(false);
                
            builder.Property(cs => cs.GeographicLocation)
                .HasMaxLength(500)
                .IsRequired(false);
                
            builder.Property(cs => cs.DemographicInfo)
                .IsRequired(false);
                
            builder.Property(cs => cs.PsychographicInfo)
                .IsRequired(false);
                
            builder.Property(cs => cs.Needs)
                .IsRequired(false);
                
            builder.Property(cs => cs.PainPoints)
                .IsRequired(false);
                
            builder.Property(cs => cs.PreferredChannels)
                .IsRequired(false);
                
            builder.Property(cs => cs.CustomerLifetimeValue)
                .HasColumnType("decimal(18,2)")
                .IsRequired(false);
                
            builder.Property(cs => cs.AcquisitionCost)
                .HasColumnType("decimal(18,2)")
                .IsRequired(false);
                
            builder.Property(cs => cs.Priority)
                .IsRequired(false);
                
            builder.Property(cs => cs.IsTarget)
                .IsRequired()
                .HasDefaultValue(true);
                
            // Configure relationships
            builder.HasOne(cs => cs.BusinessModel)
                .WithMany(bm => bm.CustomerSegments)
                .HasForeignKey(cs => cs.BusinessModelId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure many-to-many with Channel
            builder.HasMany(cs => cs.Channels)
                .WithMany(c => c.CustomerSegments)
                .UsingEntity<Dictionary<string, object>>(
                    "CustomerSegmentChannels",
                    j => j.HasOne<Channel>().WithMany().HasForeignKey("ChannelId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<CustomerSegment>().WithMany().HasForeignKey("CustomerSegmentId").OnDelete(DeleteBehavior.Cascade)
                );
                
            // Configure many-to-many with CustomerRelationship
            builder.HasMany(cs => cs.CustomerRelationships)
                .WithMany(cr => cr.CustomerSegments)
                .UsingEntity<Dictionary<string, object>>(
                    "CustomerSegmentRelationships",
                    j => j.HasOne<CustomerRelationship>().WithMany().HasForeignKey("CustomerRelationshipId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<CustomerSegment>().WithMany().HasForeignKey("CustomerSegmentId").OnDelete(DeleteBehavior.Cascade)
                );
                
            // Configure many-to-many with ValueProposition
            builder.HasMany(cs => cs.ValuePropositions)
                .WithMany(vp => vp.CustomerSegments)
                .UsingEntity<Dictionary<string, object>>(
                    "CustomerSegmentValuePropositions",
                    j => j.HasOne<ValueProposition>().WithMany().HasForeignKey("ValuePropositionId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<CustomerSegment>().WithMany().HasForeignKey("CustomerSegmentId").OnDelete(DeleteBehavior.Cascade)
                );
                
            // Configure indexes
            builder.HasIndex(cs => cs.BusinessModelId);
            builder.HasIndex(cs => cs.SegmentType);
            builder.HasIndex(cs => cs.IsTarget);
            
            // Configure soft delete filter
            builder.HasQueryFilter(cs => !cs.IsDeleted);
        }
    }
    
    public class ChannelConfiguration : IEntityTypeConfiguration<Channel>
    {
        public void Configure(EntityTypeBuilder<Channel> builder)
        {
            builder.ToTable("Channels");
            
            builder.HasKey(c => c.Id);
            
            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(c => c.Description)
                .HasMaxLength(1000);
                
            builder.Property(c => c.ChannelType)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(c => c.Purpose)
                .IsRequired()
                .HasConversion<int>();
                
            builder.Property(c => c.Ownership)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(c => c.Url)
                .HasMaxLength(500)
                .IsRequired(false);
                
            builder.Property(c => c.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
                
            builder.Property(c => c.LaunchDate)
                .IsRequired(false);
                
            builder.Property(c => c.CostPerAcquisition)
                .HasColumnType("decimal(18,2)")
                .IsRequired(false);
                
            builder.Property(c => c.MonthlyCost)
                .HasColumnType("decimal(18,2)")
                .IsRequired(false);
                
            builder.Property(c => c.IntegrationDetails)
                .IsRequired(false);
                
            builder.Property(c => c.PerformanceMetrics)
                .IsRequired(false);
                
            // Configure relationships
            builder.HasOne(c => c.BusinessModel)
                .WithMany(bm => bm.Channels)
                .HasForeignKey(c => c.BusinessModelId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure many-to-many with CustomerRelationship
            builder.HasMany(c => c.CustomerRelationships)
                .WithMany(cr => cr.Channels)
                .UsingEntity<Dictionary<string, object>>(
                    "ChannelCustomerRelationships",
                    j => j.HasOne<CustomerRelationship>().WithMany().HasForeignKey("CustomerRelationshipId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Channel>().WithMany().HasForeignKey("ChannelId").OnDelete(DeleteBehavior.Cascade)
                );
                
            // Configure indexes
            builder.HasIndex(c => c.BusinessModelId);
            builder.HasIndex(c => c.ChannelType);
            builder.HasIndex(c => c.IsActive);
            
            // Configure soft delete filter
            builder.HasQueryFilter(c => !c.IsDeleted);
        }
    }
    
    public class CustomerRelationshipConfiguration : IEntityTypeConfiguration<CustomerRelationship>
    {
        public void Configure(EntityTypeBuilder<CustomerRelationship> builder)
        {
            builder.ToTable("CustomerRelationships");
            
            builder.HasKey(cr => cr.Id);
            
            builder.Property(cr => cr.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(cr => cr.Description)
                .HasMaxLength(1000);
                
            builder.Property(cr => cr.RelationshipType)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(cr => cr.AutomationLevel)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(cr => cr.Touchpoints)
                .IsRequired(false);
                
            builder.Property(cr => cr.CommunicationStyle)
                .HasMaxLength(500)
                .IsRequired(false);
                
            builder.Property(cr => cr.SuccessMetrics)
                .IsRequired(false);
                
            builder.Property(cr => cr.CostToServe)
                .HasColumnType("decimal(18,2)")
                .IsRequired(false);
                
            builder.Property(cr => cr.LifetimeValue)
                .HasColumnType("decimal(18,2)")
                .IsRequired(false);
                
            builder.Property(cr => cr.IsActive)
                .IsRequired()
                .HasDefaultValue(true);
                
            // Configure relationships
            builder.HasOne(cr => cr.BusinessModel)
                .WithMany(bm => bm.CustomerRelationships)
                .HasForeignKey(cr => cr.BusinessModelId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure many-to-many with ValueProposition
            builder.HasMany(cr => cr.ValuePropositions)
                .WithMany(vp => vp.CustomerRelationships)
                .UsingEntity<Dictionary<string, object>>(
                    "CustomerRelationshipValuePropositions",
                    j => j.HasOne<ValueProposition>().WithMany().HasForeignKey("ValuePropositionId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<CustomerRelationship>().WithMany().HasForeignKey("CustomerRelationshipId").OnDelete(DeleteBehavior.Cascade)
                );
                
            // Configure indexes
            builder.HasIndex(cr => cr.BusinessModelId);
            builder.HasIndex(cr => cr.RelationshipType);
            builder.HasIndex(cr => cr.IsActive);
            
            // Configure soft delete filter
            builder.HasQueryFilter(cr => !cr.IsDeleted);
        }
    }
    
    public class ValuePropositionConfiguration : IEntityTypeConfiguration<ValueProposition>
    {
        public void Configure(EntityTypeBuilder<ValueProposition> builder)
        {
            builder.ToTable("ValuePropositions");
            
            builder.HasKey(vp => vp.Id);
            
            builder.Property(vp => vp.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(vp => vp.Description)
                .HasMaxLength(1000);
                
            builder.Property(vp => vp.UniqueSellingPoint)
                .IsRequired()
                .HasMaxLength(500);
                
            builder.Property(vp => vp.Benefits)
                .IsRequired(false);
                
            builder.Property(vp => vp.Features)
                .IsRequired(false);
                
            builder.Property(vp => vp.Price)
                .HasColumnType("decimal(18,2)")
                .IsRequired(false);
                
            builder.Property(vp => vp.PricingModel)
                .HasMaxLength(200)
                .IsRequired(false);
                
            builder.Property(vp => vp.CompetitiveAdvantage)
                .IsRequired(false);
                
            builder.Property(vp => vp.CustomerTestimonials)
                .IsRequired(false);
                
            builder.Property(vp => vp.SuccessMetrics)
                .IsRequired(false);
                
            builder.Property(vp => vp.IsDifferentiator)
                .IsRequired()
                .HasDefaultValue(false);
                
            builder.Property(vp => vp.Status)
                .IsRequired()
                .HasConversion<string>();
                
            // Configure relationships
            builder.HasOne(vp => vp.BusinessModel)
                .WithMany(bm => bm.ValuePropositions)
                .HasForeignKey(vp => vp.BusinessModelId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure many-to-many with RevenueStream
            builder.HasMany(vp => vp.RevenueStreams)
                .WithMany(rs => rs.ValuePropositions)
                .UsingEntity<Dictionary<string, object>>(
                    "ValuePropositionRevenueStreams",
                    j => j.HasOne<RevenueStream>().WithMany().HasForeignKey("RevenueStreamId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<ValueProposition>().WithMany().HasForeignKey("ValuePropositionId").OnDelete(DeleteBehavior.Cascade)
                );
                
            // Configure indexes
            builder.HasIndex(vp => vp.BusinessModelId);
            builder.HasIndex(vp => vp.IsDifferentiator);
            builder.HasIndex(vp => vp.Status);
            
            // Configure soft delete filter
            builder.HasQueryFilter(vp => !vp.IsDeleted);
        }
    }
}
