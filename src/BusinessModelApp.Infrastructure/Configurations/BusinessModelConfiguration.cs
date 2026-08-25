using BusinessModelApp.Core.Domain.BusinessModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace BusinessModelApp.Infrastructure.Configurations
{
    public class BusinessModelConfiguration : IEntityTypeConfiguration<BusinessModel>
    {
        public void Configure(EntityTypeBuilder<BusinessModel> builder)
        {
            builder.ToTable("BusinessModels");
            
            builder.HasKey(bm => bm.Id);
            
            builder.Property(bm => bm.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(bm => bm.Description)
                .HasMaxLength(2000);
                
            builder.Property(bm => bm.Status)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(bm => bm.PublishedAt)
                .IsRequired(false);
                
            // Configure relationships
            builder.HasOne(bm => bm.CreatedBy)
                .WithMany()
                .HasForeignKey(bm => bm.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Configure indexes
            builder.HasIndex(bm => bm.Name);
            builder.HasIndex(bm => bm.Status);
            builder.HasIndex(bm => bm.CreatedById);
            
            // Configure soft delete filter
            builder.HasQueryFilter(bm => !bm.IsDeleted);
        }
    }
    
    public class RevenueStreamConfiguration : IEntityTypeConfiguration<RevenueStream>
    {
        public void Configure(EntityTypeBuilder<RevenueStream> builder)
        {
            builder.ToTable("RevenueStreams");
            
            builder.HasKey(rs => rs.Id);
            
            builder.Property(rs => rs.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(rs => rs.Description)
                .HasMaxLength(1000);
                
            builder.Property(rs => rs.RevenueType)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(rs => rs.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
                
            builder.Property(rs => rs.BillingFrequency)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(rs => rs.IsRecurring)
                .IsRequired()
                .HasDefaultValue(false);
                
            builder.Property(rs => rs.RecurringInterval)
                .IsRequired(false);
                
            // Configure relationships
            builder.HasOne(rs => rs.BusinessModel)
                .WithMany(bm => bm.RevenueStreams)
                .HasForeignKey(rs => rs.BusinessModelId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure indexes
            builder.HasIndex(rs => rs.BusinessModelId);
            builder.HasIndex(rs => new { rs.BusinessModelId, rs.RevenueType });
            
            // Configure soft delete filter
            builder.HasQueryFilter(rs => !rs.IsDeleted);
        }
    }
    
    public class CostStructureConfiguration : IEntityTypeConfiguration<CostStructure>
    {
        public void Configure(EntityTypeBuilder<CostStructure> builder)
        {
            builder.ToTable("CostStructures");
            
            builder.HasKey(cs => cs.Id);
            
            builder.Property(cs => cs.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(cs => cs.Description)
                .HasMaxLength(1000);
                
            builder.Property(cs => cs.CostType)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(cs => cs.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
                
            builder.Property(cs => cs.BillingFrequency)
                .IsRequired()
                .HasConversion<string>();
                
            builder.Property(cs => cs.IsFixed)
                .IsRequired()
                .HasDefaultValue(true);
                
            builder.Property(cs => cs.IsEssential)
                .IsRequired()
                .HasDefaultValue(true);
                
            builder.Property(cs => cs.StartDate)
                .IsRequired(false);
                
            builder.Property(cs => cs.EndDate)
                .IsRequired(false);
                
            // Configure relationships
            builder.HasOne(cs => cs.BusinessModel)
                .WithMany(bm => bm.CostStructures)
                .HasForeignKey(cs => cs.BusinessModelId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure indexes
            builder.HasIndex(cs => cs.BusinessModelId);
            builder.HasIndex(cs => cs.CostType);
            builder.HasIndex(cs => cs.IsFixed);
            builder.HasIndex(cs => cs.IsEssential);
            
            // Configure soft delete filter
            builder.HasQueryFilter(cs => !cs.IsDeleted);
        }
    }
}
