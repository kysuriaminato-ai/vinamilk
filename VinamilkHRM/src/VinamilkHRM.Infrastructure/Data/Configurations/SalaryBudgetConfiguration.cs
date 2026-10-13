using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Salaries;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class SalaryBudgetConfiguration : IEntityTypeConfiguration<SalaryBudget>
{
    public void Configure(EntityTypeBuilder<SalaryBudget> builder)
    {
        builder.ToTable("SalaryBudgets");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Amount).HasPrecision(18, 0);
        builder.Property(b => b.Used).HasPrecision(18, 0);
        builder.Property(b => b.Notes).HasMaxLength(500);

        builder.HasOne(b => b.OrganizationalUnit)
            .WithMany()
            .HasForeignKey(b => b.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}