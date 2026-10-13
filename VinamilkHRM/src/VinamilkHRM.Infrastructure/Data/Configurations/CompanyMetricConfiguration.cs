using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Analytics;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class CompanyMetricConfiguration : IEntityTypeConfiguration<CompanyMetric>
{
    public void Configure(EntityTypeBuilder<CompanyMetric> builder)
    {
        builder.ToTable("CompanyMetrics", "rpt");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Quarter).IsRequired().HasMaxLength(10);
        builder.Property(m => m.Revenue).HasPrecision(18, 0);
        builder.Property(m => m.NetProfitAfterTax).HasPrecision(18, 0);
        builder.Property(m => m.ProductionVolumeLiters).HasPrecision(18, 2);

        builder.HasIndex(m => m.Quarter).IsUnique();
    }
}