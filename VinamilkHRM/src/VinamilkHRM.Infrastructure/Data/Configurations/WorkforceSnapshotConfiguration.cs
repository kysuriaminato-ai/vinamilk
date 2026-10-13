using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Analytics;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class WorkforceSnapshotConfiguration : IEntityTypeConfiguration<WorkforceSnapshot>
{
    public void Configure(EntityTypeBuilder<WorkforceSnapshot> builder)
    {
        builder.ToTable("WorkforceSnapshots", "rpt");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Month)
            .IsRequired()
            .HasMaxLength(7);

        builder.Property(s => s.AverageHeadcount).HasPrecision(10, 2);
        builder.Property(s => s.AbsenceRate).HasPrecision(5, 2);
        builder.Property(s => s.OvertimeHours).HasPrecision(10, 1);
        builder.Property(s => s.LaborCost).HasPrecision(18, 0);
        builder.Property(s => s.AvgKpi).HasPrecision(5, 2);

        builder.HasOne(s => s.OrganizationalUnit)
            .WithMany()
            .HasForeignKey(s => s.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.Month, s.OrganizationalUnitId }).IsUnique();
    }
}