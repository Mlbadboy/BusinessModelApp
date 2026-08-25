using BusinessModelApp.Core.Domain.BusinessModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace BusinessModelApp.Infrastructure.Configurations
{
    public class KeyActivityConfiguration : IEntityTypeConfiguration<KeyActivity>
    {
        public void Configure(EntityTypeBuilder<KeyActivity> builder)
        {
            builder.ToTable("KeyActivities");
            
            builder.HasKey(ka => ka.Id);
            
            builder.Property(ka => ka.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(ka => ka.Description)
                .HasMaxLength(1000);
                
            builder.Property(ka => ka.ActivityType)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(ka => ka.Priority)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(ka => ka.Status)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(ka => ka.StartDate)
                .IsRequired(false);
                
            builder.Property(ka => ka.TargetDate)
                .IsRequired(false);
                
            builder.Property(ka => ka.CompletionDate)
                .IsRequired(false);
                
            builder.Property(ka => ka.Budget)
                .HasColumnType("decimal(18,2)")
                .IsRequired(false);
                
            builder.Property(ka => ka.ActualCost)
                .HasColumnType("decimal(18,2)")
                .IsRequired(false);
                
            // Configure relationships
            builder.HasOne(ka => ka.BusinessModel)
                .WithMany(bm => bm.KeyActivities)
                .HasForeignKey(ka => ka.BusinessModelId)
                .OnDelete(DeleteBehavior.Cascade);
                
            builder.HasOne(ka => ka.AssignedTo)
                .WithMany()
                .HasForeignKey(ka => ka.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Configure many-to-many with KeyResource
            builder.HasMany(ka => ka.Resources)
                .WithMany(kr => kr.Activities)
                .UsingEntity<Dictionary<string, object>>(
                    "KeyActivityResources",
                    j => j.HasOne<KeyResource>().WithMany().HasForeignKey("ResourceId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<KeyActivity>().WithMany().HasForeignKey("ActivityId").OnDelete(DeleteBehavior.Cascade)
                );
                
            // Configure indexes
            builder.HasIndex(ka => ka.BusinessModelId);
            builder.HasIndex(ka => ka.ActivityType);
            builder.HasIndex(ka => ka.Priority);
            builder.HasIndex(ka => ka.Status);
            builder.HasIndex(ka => ka.AssignedToId);
            
            // Configure soft delete filter
            builder.HasQueryFilter(ka => !ka.IsDeleted);
        }
    }
    
    public class KeyResourceConfiguration : IEntityTypeConfiguration<KeyResource>
    {
        public void Configure(EntityTypeBuilder<KeyResource> builder)
        {
            builder.ToTable("KeyResources");
            
            builder.HasKey(kr => kr.Id);
            
            builder.Property(kr => kr.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(kr => kr.Description)
                .HasMaxLength(1000);
                
            builder.Property(kr => kr.ResourceType)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(kr => kr.Ownership)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(kr => kr.Cost)
                .HasColumnType("decimal(18,2)")
                .IsRequired(false);
                
            builder.Property(kr => kr.BillingFrequency)
                .HasConversion<string>()
                .IsRequired(false);
                
            builder.Property(kr => kr.IsShared)
                .IsRequired()
                .HasDefaultValue(false);
                
            builder.Property(kr => kr.Quantity)
                .IsRequired(false);
                
            builder.Property(kr => kr.Unit)
                .HasMaxLength(50)
                .IsRequired(false);
                
            builder.Property(kr => kr.AcquisitionDate)
                .IsRequired(false);
                
            builder.Property(kr => kr.ExpirationDate)
                .IsRequired(false);
                
            builder.Property(kr => kr.Provider)
                .HasMaxLength(200)
                .IsRequired(false);
                
            builder.Property(kr => kr.ContactInfo)
                .HasMaxLength(500)
                .IsRequired(false);
                
            // Configure relationships
            builder.HasOne(kr => kr.BusinessModel)
                .WithMany(bm => bm.KeyResources)
                .HasForeignKey(kr => kr.BusinessModelId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure many-to-many with KeyPartnership
            builder.HasMany(kr => kr.Partnerships)
                .WithMany(kp => kp.SharedResources)
                .UsingEntity<Dictionary<string, object>>(
                    "KeyResourcePartnerships",
                    j => j.HasOne<KeyPartnership>().WithMany().HasForeignKey("PartnershipId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<KeyResource>().WithMany().HasForeignKey("ResourceId").OnDelete(DeleteBehavior.Cascade)
                );
                
            // Configure indexes
            builder.HasIndex(kr => kr.BusinessModelId);
            builder.HasIndex(kr => kr.ResourceType);
            builder.HasIndex(kr => kr.Ownership);
            builder.HasIndex(kr => kr.IsShared);
            
            // Configure soft delete filter
            builder.HasQueryFilter(kr => !kr.IsDeleted);
        }
    }
    
    public class KeyPartnershipConfiguration : IEntityTypeConfiguration<KeyPartnership>
    {
        public void Configure(EntityTypeBuilder<KeyPartnership> builder)
        {
            builder.ToTable("KeyPartnerships");
            
            builder.HasKey(kp => kp.Id);
            
            builder.Property(kp => kp.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(kp => kp.Description)
                .HasMaxLength(1000);
                
            builder.Property(kp => kp.PartnershipType)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(kp => kp.PartnerCompany)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(kp => kp.ContactPerson)
                .HasMaxLength(200)
                .IsRequired(false);
                
            builder.Property(kp => kp.ContactEmail)
                .HasMaxLength(200)
                .IsRequired(false);
                
            builder.Property(kp => kp.ContactPhone)
                .HasMaxLength(50)
                .IsRequired(false);
                
            builder.Property(kp => kp.StartDate)
                .IsRequired(false);
                
            builder.Property(kp => kp.EndDate)
                .IsRequired(false);
                
            builder.Property(kp => kp.IsExclusive)
                .IsRequired()
                .HasDefaultValue(false);
                
            builder.Property(kp => kp.Status)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(kp => kp.AgreementDetails)
                .IsRequired(false);
                
            builder.Property(kp => kp.RevenueSharePercentage)
                .HasColumnType("decimal(5,2)")
                .IsRequired(false);
                
            builder.Property(kp => kp.FixedFee)
                .HasColumnType("decimal(18,2)")
                .IsRequired(false);
                
            builder.Property(kp => kp.BillingFrequency)
                .HasConversion<string>()
                .IsRequired(false);
                
            // Configure relationships
            builder.HasOne(kp => kp.BusinessModel)
                .WithMany(bm => bm.KeyPartnerships)
                .HasForeignKey(kp => kp.BusinessModelId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure many-to-many with ValueProposition
            builder.HasMany(kp => kp.ValuePropositions)
                .WithMany(vp => vp.KeyPartnerships)
                .UsingEntity<Dictionary<string, object>>(
                    "KeyPartnershipValuePropositions",
                    j => j.HasOne<ValueProposition>().WithMany().HasForeignKey("ValuePropositionId").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<KeyPartnership>().WithMany().HasForeignKey("PartnershipId").OnDelete(DeleteBehavior.Cascade)
                );
                
            // Configure indexes
            builder.HasIndex(kp => kp.BusinessModelId);
            builder.HasIndex(kp => kp.PartnershipType);
            builder.HasIndex(kp => kp.PartnerCompany);
            builder.HasIndex(kp => kp.Status);
            
            // Configure soft delete filter
            builder.HasQueryFilter(kp => !kp.IsDeleted);
        }
    }
}
