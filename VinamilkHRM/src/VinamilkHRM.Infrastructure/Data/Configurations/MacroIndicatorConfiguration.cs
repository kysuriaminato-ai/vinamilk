using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Analytics;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class MacroIndicatorConfiguration : IEntityTypeConfiguration<MacroIndicator>
{
    public void Configure(EntityTypeBuilder<MacroIndicator> builder)
    {
        builder.ToTable("MacroIndicators", "rpt");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Code).IsRequired().HasMaxLength(50);
        builder.Property(m => m.Period).IsRequired().HasMaxLength(20);
        builder.Property(m => m.Value).HasPrecision(18, 4);
        builder.Property(m => m.SourceUrl).HasMaxLength(300);
        builder.Property(m => m.Notes).HasMaxLength(500);

        builder.HasIndex(m => new { m.Code, m.Period }).IsUnique();
    }
}