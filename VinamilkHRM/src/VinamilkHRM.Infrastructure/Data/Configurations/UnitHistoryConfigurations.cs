using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class UnitManagerHistoryConfiguration : IEntityTypeConfiguration<UnitManagerHistory>
{
    public void Configure(EntityTypeBuilder<UnitManagerHistory> builder)
    {
        builder.ToTable("UnitManagerHistories");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.AppointmentDecisionNumber)
            .HasMaxLength(100);

        builder.Property(h => h.Note)
            .HasMaxLength(500);

        builder.HasOne(h => h.OrganizationalUnit)
            .WithMany(u => u.UnitManagerHistories)
            .HasForeignKey(h => h.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.Employee)
            .WithMany()
            .HasForeignKey(h => h.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => h.EmployeeId);
        builder.HasIndex(h => new { h.OrganizationalUnitId, h.FromDate });
    }
}

public class UnitMinimumWageRegionHistoryConfiguration : IEntityTypeConfiguration<UnitMinimumWageRegionHistory>
{
    public void Configure(EntityTypeBuilder<UnitMinimumWageRegionHistory> builder)
    {
        builder.ToTable("UnitMinimumWageRegionHistories");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Reason)
            .HasMaxLength(500);

        builder.Property(r => r.LegalReference)
            .HasMaxLength(200);

        builder.HasOne(r => r.OrganizationalUnit)
            .WithMany(u => u.UnitMinimumWageRegionHistories)
            .HasForeignKey(r => r.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.OrganizationalUnitId, r.EffectiveFrom });
    }
}
