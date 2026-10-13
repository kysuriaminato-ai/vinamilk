using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Shifts;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class CrewConfiguration : IEntityTypeConfiguration<Crew>
{
    public void Configure(EntityTypeBuilder<Crew> builder)
    {
        builder.ToTable("Crews");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CrewCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(c => c.CrewCode);

        builder.Property(c => c.CrewName)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasOne(c => c.OrganizationalUnit)
            .WithMany()
            .HasForeignKey(c => c.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Members)
            .WithOne(m => m.Crew)
            .HasForeignKey(m => m.CrewId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CrewMemberConfiguration : IEntityTypeConfiguration<CrewMember>
{
    public void Configure(EntityTypeBuilder<CrewMember> builder)
    {
        builder.ToTable("CrewMembers");
        builder.HasKey(m => m.Id);

        builder.HasOne(m => m.Employee)
            .WithMany()
            .HasForeignKey(m => m.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => new { m.CrewId, m.EmployeeId });
    }
}

public class RotationPatternConfiguration : IEntityTypeConfiguration<RotationPattern>
{
    public void Configure(EntityTypeBuilder<RotationPattern> builder)
    {
        builder.ToTable("RotationPatterns");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.PatternCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(r => r.PatternCode)
            .IsUnique();

        builder.Property(r => r.PatternName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(r => r.PatternSequence)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(r => r.OrganizationalUnit)
            .WithMany()
            .HasForeignKey(r => r.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CrewRotationConfiguration : IEntityTypeConfiguration<CrewRotation>
{
    public void Configure(EntityTypeBuilder<CrewRotation> builder)
    {
        builder.ToTable("CrewRotations");
        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.Crew)
            .WithMany()
            .HasForeignKey(r => r.CrewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.RotationPattern)
            .WithMany()
            .HasForeignKey(r => r.RotationPatternId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.CrewId, r.RotationPatternId });
    }
}
