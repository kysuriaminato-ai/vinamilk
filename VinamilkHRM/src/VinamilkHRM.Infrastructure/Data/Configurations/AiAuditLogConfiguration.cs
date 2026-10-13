using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Analytics;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class AiAuditLogConfiguration : IEntityTypeConfiguration<AiAuditLog>
{
    public void Configure(EntityTypeBuilder<AiAuditLog> builder)
    {
        builder.ToTable("AiAuditLogs", "rpt");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.FeatureName).IsRequired().HasMaxLength(100);
        builder.Property(l => l.ModelName).IsRequired().HasMaxLength(50);
    }
}