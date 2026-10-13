using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Analytics;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class MarketSalaryBenchmarkConfiguration : IEntityTypeConfiguration<MarketSalaryBenchmark>
{
    public void Configure(EntityTypeBuilder<MarketSalaryBenchmark> builder)
    {
        builder.ToTable("MarketSalaryBenchmarks", "rpt");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.SurveySource).IsRequired().HasMaxLength(150);
        builder.Property(b => b.P25).HasPrecision(18, 0);
        builder.Property(b => b.P50).HasPrecision(18, 0);
        builder.Property(b => b.P75).HasPrecision(18, 0);

        builder.HasOne(b => b.JobFamily)
            .WithMany()
            .HasForeignKey(b => b.JobFamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.JobGrade)
            .WithMany()
            .HasForeignKey(b => b.JobGradeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}