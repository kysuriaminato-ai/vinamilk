using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Shifts;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class ShiftDefinitionConfiguration : IEntityTypeConfiguration<ShiftDefinition>
{
    public void Configure(EntityTypeBuilder<ShiftDefinition> builder)
    {
        builder.ToTable("ShiftDefinitions");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(d => d.Code)
            .IsUnique();

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(d => d.TotalPlannedWorkHours)
            .HasPrecision(18, 2);

        builder.HasOne(d => d.OrganizationalUnit)
            .WithMany()
            .HasForeignKey(d => d.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Segments)
            .WithOne(s => s.ShiftDefinition)
            .HasForeignKey(s => s.ShiftDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ShiftSegmentConfiguration : IEntityTypeConfiguration<ShiftSegment>
{
    public void Configure(EntityTypeBuilder<ShiftSegment> builder)
    {
        builder.ToTable("ShiftSegments");
        builder.HasKey(s => s.Id);

        builder.HasIndex(s => new { s.ShiftDefinitionId, s.SegmentIndex });
    }
}
