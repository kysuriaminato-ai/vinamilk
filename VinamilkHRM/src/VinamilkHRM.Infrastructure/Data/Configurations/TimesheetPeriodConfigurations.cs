using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Attendances;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class MonthlyTimesheetPeriodConfiguration : IEntityTypeConfiguration<MonthlyTimesheetPeriod>
{
    public void Configure(EntityTypeBuilder<MonthlyTimesheetPeriod> builder)
    {
        builder.ToTable("MonthlyTimesheetPeriods");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Period)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(p => new { p.Period, p.OrganizationalUnitId })
            .IsUnique();

        builder.HasOne(p => p.OrganizationalUnit)
            .WithMany()
            .HasForeignKey(p => p.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ConfirmedByEmployee)
            .WithMany()
            .HasForeignKey(p => p.ConfirmedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ApprovedByEmployee)
            .WithMany()
            .HasForeignKey(p => p.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.LockedByEmployee)
            .WithMany()
            .HasForeignKey(p => p.LockedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MonthlyTimesheetSummaryConfiguration : IEntityTypeConfiguration<MonthlyTimesheetSummary>
{
    public void Configure(EntityTypeBuilder<MonthlyTimesheetSummary> builder)
    {
        builder.ToTable("MonthlyTimesheetSummaries");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Period)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(s => new { s.Period, s.EmployeeId })
            .IsUnique();

        builder.Property(s => s.StandardWorkDays).HasPrecision(18, 2);
        builder.Property(s => s.ActualWorkDays).HasPrecision(18, 2);
        builder.Property(s => s.PaidLeaveDays).HasPrecision(18, 2);
        builder.Property(s => s.SickLeaveDays).HasPrecision(18, 2);
        builder.Property(s => s.MaternityLeaveDays).HasPrecision(18, 2);
        builder.Property(s => s.HolidayDays).HasPrecision(18, 2);
        builder.Property(s => s.CompensatoryLeaveDays).HasPrecision(18, 2);
        builder.Property(s => s.BusinessTripDays).HasPrecision(18, 2);
        builder.Property(s => s.TrainingDays).HasPrecision(18, 2);
        builder.Property(s => s.UnpaidLeaveDays).HasPrecision(18, 2);
        builder.Property(s => s.UnexcusedAbsenceDays).HasPrecision(18, 2);

        builder.Property(s => s.TotalWorkHours).HasPrecision(18, 2);
        builder.Property(s => s.NightHours).HasPrecision(18, 2);
        builder.Property(s => s.TotalOtHours).HasPrecision(18, 2);
        builder.Property(s => s.RegularOtHours).HasPrecision(18, 2);
        builder.Property(s => s.RestDayOtHours).HasPrecision(18, 2);
        builder.Property(s => s.HolidayOtHours).HasPrecision(18, 2);
        builder.Property(s => s.NightOtHours).HasPrecision(18, 2);

        builder.HasOne(s => s.Employee)
            .WithMany()
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.OrganizationalUnit)
            .WithMany()
            .HasForeignKey(s => s.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceAdjustmentConfiguration : IEntityTypeConfiguration<AttendanceAdjustment>
{
    public void Configure(EntityTypeBuilder<AttendanceAdjustment> builder)
    {
        builder.ToTable("AttendanceAdjustments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.OriginalPeriod)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(a => a.TargetPeriod)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(a => a.AdjustedWorkDays).HasPrecision(18, 2);
        builder.Property(a => a.AdjustedOtHours).HasPrecision(18, 2);
        builder.Property(a => a.AdjustedNightHours).HasPrecision(18, 2);

        builder.Property(a => a.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
