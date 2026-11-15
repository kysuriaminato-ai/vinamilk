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
        builder.Property(x => x.ShiftType).HasColumnType("varchar(20)").HasDefaultValue("Fixed");
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
        builder.Property(x => x.ConflictWarning).HasMaxLength(500);

        builder.HasIndex(x => new { x.EmployeeId, x.WorkDate, x.WorkShiftId })
            .IsUnique()
            .HasDatabaseName("UQ_ShiftSchedules_Emp_Date_Shift")
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
        builder.Property(x => x.LegalReference).HasMaxLength(250);

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
        builder.Property(x => x.LocationAddress).HasMaxLength(300);
        builder.Property(x => x.IpAddress).HasColumnType("varchar(64)");
        builder.Property(x => x.PhotoUrl).HasMaxLength(300);
        builder.Property(x => x.EnteredByUsername).HasMaxLength(100);
        builder.Property(x => x.ManualReason).HasMaxLength(500);
        builder.Property(x => x.GeofenceLocationCode).HasColumnType("varchar(40)");
        builder.Property(x => x.GeofenceStatus).HasColumnType("varchar(40)");
        builder.Property(x => x.ClientDeviceFingerprint).HasColumnType("varchar(120)");
        builder.Property(x => x.ExternalLogKey).HasColumnType("varchar(140)");

        builder.HasIndex(x => new { x.EmployeeId, x.PunchTime })
            .HasDatabaseName("IX_AttendanceLogs_Emp_PunchTime");

        builder.HasIndex(x => x.ExternalLogKey)
            .IsUnique()
            .HasDatabaseName("UQ_AttendanceLogs_ExternalLogKey")
            .HasFilter("[ExternalLogKey] IS NOT NULL AND [IsDeleted] = 0");

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

        builder.Property(x => x.AttendanceSymbol).HasColumnType("nvarchar(20)").IsRequired();
        builder.Property(x => x.StandardHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.WorkingHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.WorkdayCount).HasColumnType("decimal(5,2)");
        builder.Property(x => x.PaidLeaveDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.UnpaidLeaveDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.OvertimeHours150).HasColumnType("decimal(5,2)");
        builder.Property(x => x.OvertimeHours200).HasColumnType("decimal(5,2)");
        builder.Property(x => x.OvertimeHours300).HasColumnType("decimal(5,2)");
        builder.Property(x => x.OvertimeHours150Day).HasColumnType("decimal(5,2)");
        builder.Property(x => x.OvertimeHours150Night).HasColumnType("decimal(5,2)");
        builder.Property(x => x.OvertimeHours200Day).HasColumnType("decimal(5,2)");
        builder.Property(x => x.OvertimeHours200Night).HasColumnType("decimal(5,2)");
        builder.Property(x => x.OvertimeHours300Day).HasColumnType("decimal(5,2)");
        builder.Property(x => x.OvertimeHours300Night).HasColumnType("decimal(5,2)");
        builder.Property(x => x.NightShiftHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.EditReason).HasMaxLength(500);

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
        builder.Property(x => x.TotalPaidPersonalLeaveDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalUnpaidPersonalLeaveDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalUnexcusedAbsentDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalOtHours150).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalOtHours200).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalOtHours300).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalOtHours150Day).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalOtHours150Night).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalOtHours200Day).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalOtHours200Night).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalOtHours300Day).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalOtHours300Night).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalNightHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalPayableWorkdays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.ClosingStatus).HasColumnType("varchar(30)").HasDefaultValue("Open");
        builder.Property(x => x.UnitSubmittedBy).HasMaxLength(100);
        builder.Property(x => x.ManagerConfirmedBy).HasMaxLength(100);
        builder.Property(x => x.LockedBy).HasMaxLength(100);
        builder.Property(x => x.MissedPunchPenaltyAmount).HasColumnType("decimal(18,0)");

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
        builder.Property(x => x.ActualWorkedOtHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.ReconciledOtHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.OvertimeRateType).HasColumnType("varchar(20)").IsRequired();
        builder.Property(x => x.OvertimeBatchCode).HasColumnType("varchar(50)");
        builder.Property(x => x.CompensationMode).HasColumnType("varchar(30)").HasDefaultValue("Paid");
        builder.Property(x => x.CompensatoryHoursCredited).HasColumnType("decimal(5,2)");
        builder.Property(x => x.LimitWarningMessage).HasMaxLength(500);
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Pending");
        builder.Property(x => x.CancelReason).HasMaxLength(500);
        builder.Property(x => x.CancelledByUsername).HasMaxLength(100);

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
        builder.Property(x => x.MaxDaysPerRequest).HasColumnType("decimal(5,2)");
        builder.Property(x => x.MaxDaysPerYear).HasColumnType("decimal(5,2)");
        builder.Property(x => x.AttendanceSymbol).HasColumnType("nvarchar(20)").IsRequired();
        builder.Property(x => x.LegalReference).HasMaxLength(300);
        builder.Property(x => x.Description).HasMaxLength(500);

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
        builder.Property(x => x.UsedCarriedForwardDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.ExpiredCarryForwardDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.UsedDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.PendingDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.AdvanceUsedDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.RemainingDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.CompensatoryHoursBalance).HasColumnType("decimal(6,2)");
        builder.Property(x => x.TerminationPayoutDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TerminationPayoutAmount).HasColumnType("decimal(18,0)");

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
        builder.Property(x => x.LeaveDurationMode).HasColumnType("varchar(20)").HasDefaultValue("Days");
        builder.Property(x => x.RequestedHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.AttachmentUrl).HasMaxLength(300);
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Pending");
        builder.Property(x => x.ApproverComment).HasMaxLength(500);
        builder.Property(x => x.CancelReason).HasMaxLength(500);
        builder.Property(x => x.CancelledByUsername).HasMaxLength(100);

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

public class AttendanceStatutoryParameterConfiguration : IEntityTypeConfiguration<AttendanceStatutoryParameter>
{
    public void Configure(EntityTypeBuilder<AttendanceStatutoryParameter> builder)
    {
        builder.ConfigureBaseAuditable<AttendanceStatutoryParameter, int>("AttendanceStatutoryParameters", "time");

        builder.Property(x => x.LegalReference).HasMaxLength(300).IsRequired();
        builder.Property(x => x.MinInterShiftRestHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.MaxNormalDailyHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.MaxNormalWeeklyHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.MaxDailyOvertimeHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.MaxTotalDailyWorkingAndOtHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.MaxMonthlyOvertimeHours).HasColumnType("decimal(6,2)");
        builder.Property(x => x.MaxYearlyOvertimeHours).HasColumnType("decimal(6,2)");
        builder.Property(x => x.MaxSpecialIndustryYearlyOvertimeHours).HasColumnType("decimal(6,2)");
        builder.Property(x => x.StandardAnnualLeaveDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.SeniorityBonusDaysPerInterval).HasColumnType("decimal(5,2)");
        builder.Property(x => x.WeekdayOtMultiplier).HasColumnType("decimal(5,2)");
        builder.Property(x => x.WeeklyDayOffOtMultiplier).HasColumnType("decimal(5,2)");
        builder.Property(x => x.HolidayOtMultiplier).HasColumnType("decimal(5,2)");
        builder.Property(x => x.NightShiftBonusRate).HasColumnType("decimal(5,4)");
        builder.Property(x => x.NightOtExtraBonusRate).HasColumnType("decimal(5,4)");
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => new { x.EffectiveFrom, x.EffectiveTo })
            .HasDatabaseName("IX_AttendanceStatutoryParameters_Effective");
    }
}

public class PolicyGroupConfiguration : IEntityTypeConfiguration<PolicyGroup>
{
    public void Configure(EntityTypeBuilder<PolicyGroup> builder)
    {
        builder.ConfigureBaseAuditable<PolicyGroup, int>("PolicyGroups", "time");

        builder.Property(x => x.PolicyCode).HasColumnType("varchar(40)").IsRequired();
        builder.Property(x => x.PolicyName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => x.PolicyCode)
            .IsUnique()
            .HasDatabaseName("UQ_PolicyGroups_Code")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class PolicyGroupAssignmentConfiguration : IEntityTypeConfiguration<PolicyGroupAssignment>
{
    public void Configure(EntityTypeBuilder<PolicyGroupAssignment> builder)
    {
        builder.ConfigureBaseAuditable<PolicyGroupAssignment, int>("PolicyGroupAssignments", "time");

        builder.HasIndex(x => new { x.PolicyGroupId, x.EmployeeId, x.PositionId, x.OrganizationUnitId })
            .HasDatabaseName("IX_PolicyGroupAssignments_Scope");

        builder.HasOne(x => x.PolicyGroup)
            .WithMany(g => g.Assignments)
            .HasForeignKey(x => x.PolicyGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Position)
            .WithMany()
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OrganizationUnit)
            .WithMany()
            .HasForeignKey(x => x.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendancePolicyConfiguration : IEntityTypeConfiguration<AttendancePolicy>
{
    public void Configure(EntityTypeBuilder<AttendancePolicy> builder)
    {
        builder.ConfigureBaseAuditable<AttendancePolicy, int>("AttendancePolicies", "time");

        builder.Property(x => x.RoundingMode).HasColumnType("varchar(20)").HasDefaultValue("Nearest");
        builder.Property(x => x.MissedPunchOverQuotaAction).HasColumnType("varchar(30)").HasDefaultValue("FixedFine");
        builder.Property(x => x.MissedPunchPenaltyAmount).HasColumnType("decimal(18,0)");
        builder.Property(x => x.WeeklyWorkdaysMask).HasColumnType("varchar(80)").HasDefaultValue("Mon,Tue,Wed,Thu,Fri");
        builder.Property(x => x.MaxWeeklyWorkingHoursOverride).HasColumnType("decimal(5,2)");
        builder.Property(x => x.PunchPairingMode).HasColumnType("varchar(30)").HasDefaultValue("FirstLast");
        builder.Property(x => x.MissedPunchCreditWorkdays).HasColumnType("decimal(4,2)").HasDefaultValue(0m);
        builder.Property(x => x.AnnualLeaveCarryForwardCapDays).HasColumnType("decimal(5,2)").HasDefaultValue(5m);
        builder.Property(x => x.MaxAdvanceAnnualLeaveDays).HasColumnType("decimal(5,2)").HasDefaultValue(3m);
        builder.Property(x => x.OtReconciliationMode).HasColumnType("varchar(40)").HasDefaultValue("MinOfApprovedAndActual");

        builder.HasIndex(x => x.PolicyGroupId)
            .IsUnique()
            .HasDatabaseName("UQ_AttendancePolicies_PolicyGroupId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.PolicyGroup)
            .WithMany(g => g.AttendancePolicies)
            .HasForeignKey(x => x.PolicyGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ShiftRotationPatternConfiguration : IEntityTypeConfiguration<ShiftRotationPattern>
{
    public void Configure(EntityTypeBuilder<ShiftRotationPattern> builder)
    {
        builder.ConfigureBaseAuditable<ShiftRotationPattern, int>("ShiftRotationPatterns", "time");

        builder.Property(x => x.PatternCode).HasColumnType("varchar(40)").IsRequired();
        builder.Property(x => x.PatternName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ShiftSequenceCsv).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => x.PatternCode)
            .IsUnique()
            .HasDatabaseName("UQ_ShiftRotationPatterns_Code")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class ShiftSwapRequestConfiguration : IEntityTypeConfiguration<ShiftSwapRequest>
{
    public void Configure(EntityTypeBuilder<ShiftSwapRequest> builder)
    {
        builder.ConfigureBaseAuditable<ShiftSwapRequest, int>("ShiftSwapRequests", "time");

        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Pending");
        builder.Property(x => x.ApproverComment).HasMaxLength(500);

        builder.HasIndex(x => new { x.RequesterEmployeeId, x.TargetEmployeeId, x.Status })
            .HasDatabaseName("IX_ShiftSwapRequests_Emp_Status");

        builder.HasOne(x => x.RequesterEmployee)
            .WithMany()
            .HasForeignKey(x => x.RequesterEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TargetEmployee)
            .WithMany()
            .HasForeignKey(x => x.TargetEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class GeofenceLocationConfiguration : IEntityTypeConfiguration<GeofenceLocation>
{
    public void Configure(EntityTypeBuilder<GeofenceLocation> builder)
    {
        builder.ConfigureBaseAuditable<GeofenceLocation, int>("GeofenceLocations", "time");

        builder.Property(x => x.LocationCode).HasColumnType("varchar(40)").IsRequired();
        builder.Property(x => x.LocationName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(250);
        builder.Property(x => x.Latitude).HasColumnType("decimal(10,7)");
        builder.Property(x => x.Longitude).HasColumnType("decimal(10,7)");
        builder.Property(x => x.OutsideGeofenceAction).HasColumnType("varchar(30)").HasDefaultValue("PendingApproval");

        builder.HasIndex(x => x.LocationCode)
            .IsUnique()
            .HasDatabaseName("UQ_GeofenceLocations_Code")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class AttendanceDeviceConfiguration : IEntityTypeConfiguration<AttendanceDevice>
{
    public void Configure(EntityTypeBuilder<AttendanceDevice> builder)
    {
        builder.ConfigureBaseAuditable<AttendanceDevice, int>("AttendanceDevices", "time");

        builder.Property(x => x.DeviceCode).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.DeviceName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ConnectorType).HasColumnType("varchar(30)").HasDefaultValue("FileExport");
        builder.Property(x => x.LocationName).HasMaxLength(150);
        builder.Property(x => x.EndpointOrFolderPath).HasMaxLength(300);
        builder.Property(x => x.LastSyncSummary).HasMaxLength(400);

        builder.HasIndex(x => x.DeviceCode)
            .IsUnique()
            .HasDatabaseName("UQ_AttendanceDevices_Code")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class EmployeeDeviceMappingConfiguration : IEntityTypeConfiguration<EmployeeDeviceMapping>
{
    public void Configure(EntityTypeBuilder<EmployeeDeviceMapping> builder)
    {
        builder.ConfigureBaseAuditable<EmployeeDeviceMapping, int>("EmployeeDeviceMappings", "time");

        builder.Property(x => x.DeviceCode).HasColumnType("varchar(50)");
        builder.Property(x => x.DeviceUserCode).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(200);

        builder.HasIndex(x => new { x.DeviceCode, x.DeviceUserCode })
            .HasDatabaseName("IX_EmployeeDeviceMappings_DeviceUser");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class UnmappedAttendanceLogConfiguration : IEntityTypeConfiguration<UnmappedAttendanceLog>
{
    public void Configure(EntityTypeBuilder<UnmappedAttendanceLog> builder)
    {
        builder.ConfigureBaseAuditable<UnmappedAttendanceLog, long>("UnmappedAttendanceLogs", "time");

        builder.Property(x => x.DeviceCode).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.DeviceUserCode).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.PunchType).HasColumnType("varchar(20)").IsRequired();
        builder.Property(x => x.RawPayload).HasMaxLength(400);
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("PendingMapping");
        builder.Property(x => x.ResolvedBy).HasMaxLength(100);

        builder.HasIndex(x => new { x.DeviceCode, x.DeviceUserCode, x.PunchTime, x.PunchType })
            .HasDatabaseName("IX_UnmappedAttendanceLogs_Dedup");
    }
}

public class EmployeeDeviceBindingConfiguration : IEntityTypeConfiguration<EmployeeDeviceBinding>
{
    public void Configure(EntityTypeBuilder<EmployeeDeviceBinding> builder)
    {
        builder.ConfigureBaseAuditable<EmployeeDeviceBinding, int>("EmployeeDeviceBindings", "time");

        builder.Property(x => x.DeviceFingerprint).HasColumnType("varchar(120)").IsRequired();
        builder.Property(x => x.DeviceLabel).HasMaxLength(150);
        builder.Property(x => x.LastIpAddress).HasColumnType("varchar(64)");

        builder.HasIndex(x => new { x.DeviceFingerprint, x.IsActive })
            .HasDatabaseName("IX_EmployeeDeviceBindings_Device");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceSymbolDefinitionConfiguration : IEntityTypeConfiguration<AttendanceSymbolDefinition>
{
    public void Configure(EntityTypeBuilder<AttendanceSymbolDefinition> builder)
    {
        builder.ConfigureBaseAuditable<AttendanceSymbolDefinition, int>("AttendanceSymbolDefinitions", "time");

        builder.Property(x => x.SymbolTypeCode).HasColumnType("varchar(40)").IsRequired();
        builder.Property(x => x.SymbolCode).HasColumnType("nvarchar(20)").IsRequired();
        builder.Property(x => x.SymbolName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Category).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.WorkdayMultiplier).HasColumnType("decimal(5,2)");
        builder.Property(x => x.ColorHex).HasColumnType("varchar(20)").HasDefaultValue("#198754");
        builder.Property(x => x.CssClass).HasMaxLength(150);

        builder.HasIndex(x => x.SymbolTypeCode)
            .IsUnique()
            .HasDatabaseName("UQ_AttendanceSymbolDefinitions_TypeCode")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class PostLockAttendanceAdjustmentConfiguration : IEntityTypeConfiguration<PostLockAttendanceAdjustment>
{
    public void Configure(EntityTypeBuilder<PostLockAttendanceAdjustment> builder)
    {
        builder.ConfigureBaseAuditable<PostLockAttendanceAdjustment, long>("PostLockAttendanceAdjustments", "time");

        builder.Property(x => x.AdjustmentType).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.WorkdayDelta).HasColumnType("decimal(6,2)");
        builder.Property(x => x.OtHours150Delta).HasColumnType("decimal(6,2)");
        builder.Property(x => x.OtHours200Delta).HasColumnType("decimal(6,2)");
        builder.Property(x => x.OtHours300Delta).HasColumnType("decimal(6,2)");
        builder.Property(x => x.NightHoursDelta).HasColumnType("decimal(6,2)");
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Pending");
        builder.Property(x => x.ApprovedByUsername).HasMaxLength(100);
        builder.Property(x => x.ApproverComment).HasMaxLength(500);

        builder.HasIndex(x => new { x.TargetYear, x.TargetMonth, x.EmployeeId })
            .HasDatabaseName("IX_PostLockAdjustments_TargetPeriod_Emp");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceRecalculationBatchConfiguration : IEntityTypeConfiguration<AttendanceRecalculationBatch>
{
    public void Configure(EntityTypeBuilder<AttendanceRecalculationBatch> builder)
    {
        builder.ConfigureBaseAuditable<AttendanceRecalculationBatch, int>("AttendanceRecalculationBatches", "time");

        builder.Property(x => x.BatchCode).HasColumnType("varchar(60)").IsRequired();
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Queued");
        builder.Property(x => x.RequestedByUsername).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(500);

        builder.HasIndex(x => x.BatchCode)
            .IsUnique()
            .HasDatabaseName("UQ_AttendanceRecalculationBatches_Code")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class EmployeeTimeRequestConfiguration : IEntityTypeConfiguration<EmployeeTimeRequest>
{
    public void Configure(EntityTypeBuilder<EmployeeTimeRequest> builder)
    {
        builder.ConfigureBaseAuditable<EmployeeTimeRequest, int>("EmployeeTimeRequests", "time");

        builder.Property(x => x.RequestType).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.SubType).HasColumnType("varchar(30)");
        builder.Property(x => x.LocationOrDestination).HasMaxLength(250);
        builder.Property(x => x.AttachmentUrl).HasMaxLength(300);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Pending");
        builder.Property(x => x.ApproverComment).HasMaxLength(500);
        builder.Property(x => x.CancelReason).HasMaxLength(500);
        builder.Property(x => x.CancelledByUsername).HasMaxLength(100);

        builder.HasIndex(x => new { x.EmployeeId, x.RequestType, x.StartDate })
            .HasDatabaseName("IX_EmployeeTimeRequests_Emp_Type_Start");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RequestCancellationRequestConfiguration : IEntityTypeConfiguration<RequestCancellationRequest>
{
    public void Configure(EntityTypeBuilder<RequestCancellationRequest> builder)
    {
        builder.ConfigureBaseAuditable<RequestCancellationRequest, int>("RequestCancellationRequests", "time");

        builder.Property(x => x.OriginalEntityType).HasColumnType("varchar(40)").IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Pending");
        builder.Property(x => x.ApproverComment).HasMaxLength(500);
        builder.Property(x => x.RestoredBalanceSummary).HasMaxLength(500);

        builder.HasIndex(x => new { x.OriginalEntityType, x.OriginalEntityId, x.Status })
            .HasDatabaseName("IX_RequestCancellationRequests_Entity");

        builder.HasOne(x => x.RequesterEmployee)
            .WithMany()
            .HasForeignKey(x => x.RequesterEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TerminationLeaveSettlementConfiguration : IEntityTypeConfiguration<TerminationLeaveSettlement>
{
    public void Configure(EntityTypeBuilder<TerminationLeaveSettlement> builder)
    {
        builder.ConfigureBaseAuditable<TerminationLeaveSettlement, int>("TerminationLeaveSettlements", "time");

        builder.Property(x => x.ProRatedEntitlementDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.SeniorityExtraDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.ValidCarryForwardDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.UsedDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.UnusedPayableDays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.BaseSalaryUsed).HasColumnType("decimal(18,0)");
        builder.Property(x => x.StandardWorkdaysUsed).HasColumnType("decimal(5,2)");
        builder.Property(x => x.PayoutAmount).HasColumnType("decimal(18,0)");
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("PushedToPayroll");
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => new { x.EmployeeId, x.SettlementYear })
            .IsUnique()
            .HasDatabaseName("UQ_TerminationLeaveSettlements_Emp_Year")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}



