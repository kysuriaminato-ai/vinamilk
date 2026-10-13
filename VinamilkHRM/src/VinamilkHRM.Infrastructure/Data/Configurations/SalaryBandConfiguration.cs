using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Salaries;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class SalaryBandConfiguration : IEntityTypeConfiguration<SalaryBand>
{
    public void Configure(EntityTypeBuilder<SalaryBand> builder)
    {
        builder.ToTable("SalaryBands");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Min).HasPrecision(18, 0);
        builder.Property(b => b.Mid).HasPrecision(18, 0);
        builder.Property(b => b.Max).HasPrecision(18, 0);
        builder.Property(b => b.Description).HasMaxLength(500);

        builder.HasOne(b => b.JobGrade)
            .WithMany()
            .HasForeignKey(b => b.JobGradeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}