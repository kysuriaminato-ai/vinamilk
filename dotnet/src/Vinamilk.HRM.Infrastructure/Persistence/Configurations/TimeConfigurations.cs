using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vinamilk.HRM.Domain.Entities.Time;

namespace Vinamilk.HRM.Infrastructure.Persistence.Configurations;

public class WorkShiftConfiguration : IEntityTypeConfiguration<WorkShift>
{
    public void Configure(EntityTypeBuilder<WorkShift> builder)
    {
        builder.ConfigureBaseAuditable<WorkShift, int>("WorkShifts", "time");

        builder.Property(x => x.ShiftCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.ShiftName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.StandardHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.WorkdayValue).HasColumnType("decimal(5,2)");
        builder.Property(x => x.NightShiftAllowanceRate).HasColumnType("decimal(5,2)");

        builder.HasIndex(x => x.ShiftCode)
            .IsUnique()
            .HasDatabaseName("UQ_WorkShifts_ShiftCode")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class ShiftScheduleConfiguration : IEntityTypeConfiguration<ShiftSchedule>
{
    public void Configure(EntityTypeBuilder<ShiftSchedule> builder)
    {
        builder.ConfigureBaseAuditable<ShiftSchedule, long>("ShiftSchedules", "time");

        builder.Property(x => x.Notes).HasMaxLength(200);

        builder.HasIndex(x => new { x.EmployeeId, x.WorkDate })
            .IsUnique()
            .HasDatabaseName("UQ_ShiftSchedules_Emp_Date")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.WorkShift)
            .WithMany(s => s.ShiftSchedules)
            .HasForeignKey(x => x.WorkShiftId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ConfigureBaseAuditable<Holiday, int>("Holidays", "time");

        builder.Property(x => x.HolidayName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.OvertimeMultiplier).HasColumnType("decimal(5,2)");

        builder.HasIndex(x => x.HolidayDate)
            .IsUnique()
            .HasDatabaseName("UQ_Holidays_HolidayDate")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class AttendanceLogConfiguration : IEntityTypeConfiguration<AttendanceLog>
{
    public void Configure(EntityTypeBuilder<AttendanceLog> builder)
    {
        builder.ConfigureBaseAuditable<AttendanceLog, long>("AttendanceLogs", "time");

        builder.Property(x => x.PunchType).HasColumnType("varchar(20)").IsRequired();
        builder.Property(x => x.SourceType).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.DeviceId).HasColumnType("varchar(50)");
        builder.Property(x => x.Latitude).HasColumnType("decimal(10,7)");
        builder.Property(x => x.Longitude).HasColumnType("decimal(10,7)");
        builder.Property(x => x.LocationAddress).HasMaxLength(200);

        builder.HasIndex(x => new { x.EmployeeId, x.PunchTime })
            .HasDatabaseName("IX_AttendanceLogs_Emp_PunchTime");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DailyAttendanceConfiguration : IEntityTypeConfiguration<DailyAttendance>
{
    public void Configure(EntityTypeBuilder<DailyAttendance> builder)
    {
        builder.ConfigureBaseAuditable<DailyAttendance, long>("DailyAttendances", "time");

        builder.Property(x => x.AttendanceSymbol).HasColumnType("varchar(10)").IsRequired();
        builder.Property(x => x.WorkingHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.WorkdayCount).HasColumnType("decimal(5,2)");
        builder.Property(x => x.OvertimeHours150).HasColumnType("decimal(5,2)");
        builder.Property(x => x.OvertimeHours200).HasColumnType("decimal(5,2)");
        builder.Property(x => x.OvertimeHours300).HasColumnType("decimal(5,2)");
        builder.Property(x => x.NightShiftHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.EditReason).HasMaxLength(300);

        builder.HasIndex(x => new { x.EmployeeId, x.AttendanceDate })
            .IsUnique()
            .HasDatabaseName("UQ_DailyAttendances_Employee_Date")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.AttendanceDate, x.AttendanceSymbol })
            .HasDatabaseName("IX_DailyAttendances_Date_Symbol");

        builder.HasOne(x => x.Employee)
            .WithMany(e => e.DailyAttendances)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.WorkShift)
            .WithMany(w => w.DailyAttendances)
            .HasForeignKey(x => x.WorkShiftId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MonthlyAttendanceSummaryConfiguration : IEntityTypeConfiguration<MonthlyAttendanceSummary>
{
    public void Configure(EntityTypeBuilder<MonthlyAttendanceSummary> builder)
    {
        builder.ConfigureBaseAuditable<MonthlyAttendanceSummary, int>("MonthlyAttendanceSummaries", "time");

        builder.Property(x => x.StandardWorkdays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.ActualWorkdays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.PaidLeaveDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.HolidayDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.SocialInsuranceLeaveDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.UnpaidLeaveDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalOtHours150).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalOtHours200).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalOtHours300).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalNightHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalPayableWorkdays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.LockedBy).HasMaxLength(100);

        builder.HasIndex(x => new { x.EmployeeId, x.Year, x.Month })
            .IsUnique()
            .HasDatabaseName("UQ_MonthlyAttendanceSummaries_Emp_Month_Year")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.Employee)
            .WithMany(e => e.MonthlyAttendanceSummaries)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class OvertimeRequestConfiguration : IEntityTypeConfiguration<OvertimeRequest>
{
    public void Configure(EntityTypeBuilder<OvertimeRequest> builder)
    {
        builder.ConfigureBaseAuditable<OvertimeRequest, int>("OvertimeRequests", "time");

        builder.Property(x => x.RequestedHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.ApprovedHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.OvertimeRateType).HasColumnType("varchar(20)").IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Pending");

        builder.HasIndex(x => new { x.EmployeeId, x.OvertimeDate })
            .HasDatabaseName("IX_OvertimeRequests_Emp_Date");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ConfigureBaseAuditable<LeaveType, int>("LeaveTypes", "time");

        builder.Property(x => x.LeaveTypeCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.LeaveTypeName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.MaxDaysPerYear).HasColumnType("decimal(5,2)");
        builder.Property(x => x.AttendanceSymbol).HasColumnType("varchar(10)").IsRequired();

        builder.HasIndex(x => x.LeaveTypeCode)
            .IsUnique()
            .HasDatabaseName("UQ_LeaveTypes_Code")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.ConfigureBaseAuditable<LeaveBalance, int>("LeaveBalances", "time");

        builder.Property(x => x.StandardEntitlement).HasColumnType("decimal(5,2)");
        builder.Property(x => x.SeniorityExtraDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.CarriedForwardDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.UsedDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.PendingDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.RemainingDays).HasColumnType("decimal(5,2)");

        builder.HasIndex(x => new { x.EmployeeId, x.LeaveTypeId, x.Year })
            .IsUnique()
            .HasDatabaseName("UQ_LeaveBalances_Emp_Type_Year")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.Employee)
            .WithMany(e => e.LeaveBalances)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LeaveType)
            .WithMany(t => t.LeaveBalances)
            .HasForeignKey(x => x.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ConfigureBaseAuditable<LeaveRequest, int>("LeaveRequests", "time");

        builder.Property(x => x.StartSession).HasColumnType("varchar(20)").HasDefaultValue("FullDay");
        builder.Property(x => x.EndSession).HasColumnType("varchar(20)").HasDefaultValue("FullDay");
        builder.Property(x => x.TotalDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.AttachmentUrl).HasMaxLength(300);
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Pending");
        builder.Property(x => x.ApproverComment).HasMaxLength(500);

        builder.HasIndex(x => new { x.EmployeeId, x.StartDate, x.Status })
            .HasDatabaseName("IX_LeaveRequests_Emp_Start_Status");

        builder.HasOne(x => x.Employee)
            .WithMany(e => e.LeaveRequests)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LeaveType)
            .WithMany(t => t.LeaveRequests)
            .HasForeignKey(x => x.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

