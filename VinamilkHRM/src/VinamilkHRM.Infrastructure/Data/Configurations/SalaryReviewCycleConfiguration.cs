using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Salaries;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class SalaryReviewCycleConfiguration : IEntityTypeConfiguration<SalaryReviewCycle>
{
    public void Configure(EntityTypeBuilder<SalaryReviewCycle> builder)
    {
        builder.ToTable("SalaryReviewCycles");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany(c => c.Budgets)
            .WithOne(b => b.SalaryReviewCycle)
            .HasForeignKey(b => b.SalaryReviewCycleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Adjustments)
            .WithOne(a => a.SalaryReviewCycle)
            .HasForeignKey(a => a.SalaryReviewCycleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}