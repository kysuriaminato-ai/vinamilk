using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;

namespace VinamilkHRM.Domain.Entities.Attendances;

/// <summary>
/// Bảng tổng hợp công tháng của nhân viên (MonthlyTimesheetSummary).
/// Đầu vào trực tiếp cho Động cơ tính lương (Phiên 6).
/// </summary>
public class MonthlyTimesheetSummary : BaseAuditableEntity
{
    public string Period { get; private set; } = string.Empty; // YYYY-MM
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public int OrganizationalUnitId { get; private set; }
    public virtual OrganizationalUnit? OrganizationalUnit { get; private set; }

    public decimal StandardWorkDays { get; private set; } = 26m;    // Công chuẩn theo WorkPattern
    public decimal ActualWorkDays { get; private set; } = 0m;      // Công làm việc thực tế (X, X/2)
    public decimal PaidLeaveDays { get; private set; } = 0m;        // Nghỉ phép năm (P)
    public decimal SickLeaveDays { get; private set; } = 0m;        // Nghỉ ốm (Ô)
    public decimal MaternityLeaveDays { get; private set; } = 0m;   // Nghỉ thai sản (TS)
    public decimal HolidayDays { get; private set; } = 0m;          // Nghỉ lễ, Tết (L)
    public decimal CompensatoryLeaveDays { get; private set; } = 0m;// Nghỉ bù (NB)
    public decimal BusinessTripDays { get; private set; } = 0m;     // Công tác (CT)
    public decimal TrainingDays { get; private set; } = 0m;         // Đào tạo (ĐT)
    public decimal UnpaidLeaveDays { get; private set; } = 0m;      // Nghỉ không lương (KL)
    public decimal UnexcusedAbsenceDays { get; private set; } = 0m; // Nghỉ không phép (KP)

    /// <summary>
    /// Đếm ngày vắng không phép liên tục theo các ngày có lịch làm việc (không theo T2-T6).
    /// Dùng cảnh báo kỷ luật lao động khi >= 5 ngày liên tục (Điều 125 BLLĐ 2019).
    /// </summary>
    public int ConsecutiveUnexcusedDays { get; private set; } = 0;

    public decimal TotalWorkHours { get; private set; } = 0m;
    public decimal NightHours { get; private set; } = 0m;

    public decimal TotalOtHours { get; private set; } = 0m;
    public decimal RegularOtHours { get; private set; } = 0m;
    public decimal RestDayOtHours { get; private set; } = 0m;
    public decimal HolidayOtHours { get; private set; } = 0m;
    public decimal NightOtHours { get; private set; } = 0m;

    public int LateTimes { get; private set; } = 0;
    public int EarlyLeaveTimes { get; private set; } = 0;

    protected MonthlyTimesheetSummary()
    {
    }

    public MonthlyTimesheetSummary(
        string period,
        int employeeId,
        int organizationalUnitId,
        decimal standardWorkDays)
    {
        if (string.IsNullOrWhiteSpace(period))
            throw new DomainException("Kỳ công không được để trống.", "ERR_SUMMARY_PERIOD_EMPTY");

        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên không hợp lệ.", "ERR_SUMMARY_EMP_INVALID");

        Period = period.Trim();
        EmployeeId = employeeId;
        OrganizationalUnitId = organizationalUnitId;
        StandardWorkDays = standardWorkDays > 0 ? standardWorkDays : 26m;
    }

    public void UpdateMetrics(
        decimal actualWorkDays,
        decimal paidLeaveDays,
        decimal sickLeaveDays,
        decimal maternityLeaveDays,
        decimal holidayDays,
        decimal compensatoryLeaveDays,
        decimal businessTripDays,
        decimal trainingDays,
        decimal unpaidLeaveDays,
        decimal unexcusedAbsenceDays,
        int consecutiveUnexcusedDays,
        decimal totalWorkHours,
        decimal nightHours,
        decimal regularOtHours,
        decimal restDayOtHours,
        decimal holidayOtHours,
        decimal nightOtHours,
        int lateTimes,
        int earlyLeaveTimes)
    {
        ActualWorkDays = actualWorkDays;
        PaidLeaveDays = paidLeaveDays;
        SickLeaveDays = sickLeaveDays;
        MaternityLeaveDays = maternityLeaveDays;
        HolidayDays = holidayDays;
        CompensatoryLeaveDays = compensatoryLeaveDays;
        BusinessTripDays = businessTripDays;
        TrainingDays = trainingDays;
        UnpaidLeaveDays = unpaidLeaveDays;
        UnexcusedAbsenceDays = unexcusedAbsenceDays;
        ConsecutiveUnexcusedDays = consecutiveUnexcusedDays;

        TotalWorkHours = totalWorkHours;
        NightHours = nightHours;
        RegularOtHours = regularOtHours;
        RestDayOtHours = restDayOtHours;
        HolidayOtHours = holidayOtHours;
        NightOtHours = nightOtHours;
        TotalOtHours = regularOtHours + restDayOtHours + holidayOtHours + nightOtHours;

        LateTimes = lateTimes;
        EarlyLeaveTimes = earlyLeaveTimes;
    }
}
