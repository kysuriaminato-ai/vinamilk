using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Attendances;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class DailyAttendanceConfiguration : IEntityTypeConfiguration<DailyAttendance>
{
    public void Configure(EntityTypeBuilder<DailyAttendance> builder)
    {
        builder.ToTable("DailyAttendances");
        builder.HasKey(d => d.Id);

        builder.HasIndex(d => new { d.EmployeeId, d.AttendanceDate })
            .IsUnique();

        builder.Property(d => d.AttendanceCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(d => d.TotalWorkHours).HasPrecision(18, 2);
        builder.Property(d => d.NightHours).HasPrecision(18, 2);
        builder.Property(d => d.RegularOtHours).HasPrecision(18, 2);
        builder.Property(d => d.RestDayOtHours).HasPrecision(18, 2);
        builder.Property(d => d.HolidayOtHours).HasPrecision(18, 2);
        builder.Property(d => d.NightOtHours).HasPrecision(18, 2);
        builder.Property(d => d.UnapprovedOtHours).HasPrecision(18, 2);
        builder.Property(d => d.WorkDayCredit).HasPrecision(18, 2);

        builder.Property(d => d.WarningReason).HasMaxLength(500);
        builder.Property(d => d.Remarks).HasMaxLength(500);

        builder.HasOne(d => d.Employee)
            .WithMany()
            .HasForeignKey(d => d.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.RosterEntry)
            .WithMany()
            .HasForeignKey(d => d.RosterEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ShiftDefinition)
            .WithMany()
            .HasForeignKey(d => d.ShiftDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Segments)
            .WithOne(s => s.DailyAttendance)
            .HasForeignKey(s => s.DailyAttendanceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AttendanceSegmentConfiguration : IEntityTypeConfiguration<AttendanceSegment>
{
    public void Configure(EntityTypeBuilder<AttendanceSegment> builder)
    {
        builder.ToTable("AttendanceSegments");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.WorkHours).HasPrecision(18, 2);
        builder.Property(s => s.NightHours).HasPrecision(18, 2);

        builder.HasIndex(s => new { s.DailyAttendanceId, s.SegmentIndex });
    }
}

