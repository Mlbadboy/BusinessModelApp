using BusinessModelApp.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessModelApp.Infrastructure.Data.Configurations
{
    public class RevenueSourceConfiguration : IEntityTypeConfiguration<RevenueSource>
    {
        public void Configure(EntityTypeBuilder<RevenueSource> builder)
        {
            builder.HasKey(rs => rs.Id);
            
            builder.Property(rs => rs.Name)
                .IsRequired()
                .HasMaxLength(255);
            
            builder.Property(rs => rs.Description)
                .HasMaxLength(1000);
            
            builder.Property(rs => rs.ExpectedRevenue)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
            
            builder.Property(rs => rs.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            builder.HasOne(rs => rs.BusinessModel)
                .WithMany(bm => bm.RevenueSources)
                .HasForeignKey(rs => rs.BusinessModelId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
