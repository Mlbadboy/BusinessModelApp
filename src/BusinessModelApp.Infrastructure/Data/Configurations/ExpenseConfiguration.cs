using BusinessModelApp.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusinessModelApp.Infrastructure.Data.Configurations
{
    public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
    {
        public void Configure(EntityTypeBuilder<Expense> builder)
        {
            builder.HasKey(e => e.Id);
            
            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(255);
            
            builder.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(255);
            
            builder.Property(e => e.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");
            
            builder.Property(e => e.Description)
                .HasMaxLength(1000);
            
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            builder.HasOne(e => e.BusinessModel)
                .WithMany(bm => bm.ExpensesList)
                .HasForeignKey(e => e.BusinessModelId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
