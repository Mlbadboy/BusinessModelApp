using BusinessModelApp.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessModelApp.Infrastructure.Data.Configurations
{
    public class BusinessModelConfiguration : IEntityTypeConfiguration<BusinessModel>
    {
        public void Configure(EntityTypeBuilder<BusinessModel> builder)
        {
            builder.HasKey(bm => bm.Id);
            
            builder.Property(bm => bm.RevenueStreams)
                .IsRequired()
                .HasMaxLength(1000);
            
            builder.Property(bm => bm.Expenses)
                .IsRequired()
                .HasMaxLength(1000);
            
            builder.Property(bm => bm.Strategy)
                .IsRequired()
                .HasMaxLength(1000);
            
            builder.Property(bm => bm.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            builder.HasMany(bm => bm.RevenueSources)
                .WithOne(rs => rs.BusinessModel)
                .HasForeignKey(rs => rs.BusinessModelId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany(bm => bm.ExpensesList)
                .WithOne(e => e.BusinessModel)
                .HasForeignKey(e => e.BusinessModelId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
