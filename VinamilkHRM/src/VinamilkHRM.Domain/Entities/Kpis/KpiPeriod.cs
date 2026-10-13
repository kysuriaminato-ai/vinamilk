using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Kpis;

/// <summary>
/// Kỳ đánh giá hiệu suất công việc (KpiPeriod) theo tháng YYYY-MM.
/// Chứa các mốc thời gian quy trình cấu hình được và trạng thái kiểm soát toàn bộ chu trình KPI.
/// </summary>
public class KpiPeriod : BaseAuditableEntity
{
    public string Period { get; private set; } = string.Empty; // Format: YYYY-MM, vd: "2026-10"
    public int Month { get; private set; }
    public int Year { get; private set; }

    // Mốc cấu hình ngày trong chu trình
    public DateOnly AssignStartDate { get; private set; }        // Mở giao KPI (ngày 25 tháng trước)
    public DateOnly AssignDeadline { get; private set; }         // Hạn chót giao KPI (ngày 5)
    public DateOnly MidTermDate { get; private set; }            // Đánh giá giữa kỳ (ngày 15)
    public DateOnly SelfReviewDeadline { get; private set; }     // Hạn chót nhân viên tự đánh giá (ngày 3 tháng sau)
    public DateOnly ManagerReviewDeadline { get; private set; }  // Hạn chót quản lý chấm điểm (ngày 7 tháng sau)
    public DateOnly CalibrationDeadline { get; private set; }    // Hạn chót hiệu chuẩn và khóa kỳ (ngày 10 tháng sau)

    public KpiPeriodStatus Status { get; private set; } = KpiPeriodStatus.Planning;
    public DateTime? ClosedAt { get; private set; }
    public string? ClosedBy { get; private set; }

    private readonly List<KpiSheet> _sheets = new();
    public virtual IReadOnlyCollection<KpiSheet> Sheets => _sheets.AsReadOnly();

    protected KpiPeriod() { }

    public KpiPeriod(
        string period,
        DateOnly assignStartDate,
        DateOnly assignDeadline,
        DateOnly midTermDate,
        DateOnly selfReviewDeadline,
        DateOnly managerReviewDeadline,
        DateOnly calibrationDeadline)
    {
        if (string.IsNullOrWhiteSpace(period))
            throw new DomainException("Mã kỳ đánh giá không được để trống.", "ERR_KPI_PERIOD_EMPTY");

        var parts = period.Split('-');
        if (parts.Length != 2 || !int.TryParse(parts[0], out int year) || !int.TryParse(parts[1], out int month) || month < 1 || month > 12)
            throw new DomainException("Mã kỳ đánh giá phải có định dạng YYYY-MM hợp lệ.", "ERR_KPI_PERIOD_FORMAT_INVALID");

        Period = period.Trim();
        Year = year;
        Month = month;
        AssignStartDate = assignStartDate;
        AssignDeadline = assignDeadline;
        MidTermDate = midTermDate;
        SelfReviewDeadline = selfReviewDeadline;
        ManagerReviewDeadline = managerReviewDeadline;
        CalibrationDeadline = calibrationDeadline;
        Status = KpiPeriodStatus.Planning;
    }

    public static KpiPeriod CreateDefault(int year, int month)
    {
        string periodCode = $"{year:D4}-{month:D2}";
        var firstDayCurrentMonth = new DateOnly(year, month, 1);
        var prevMonth = firstDayCurrentMonth.AddMonths(-1);
        var nextMonth = firstDayCurrentMonth.AddMonths(1);

        var assignStart = new DateOnly(prevMonth.Year, prevMonth.Month, 25);
        var assignDeadline = new DateOnly(year, month, 5);
        var midTerm = new DateOnly(year, month, 15);
        var selfReview = new DateOnly(nextMonth.Year, nextMonth.Month, 3);
        var managerReview = new DateOnly(nextMonth.Year, nextMonth.Month, 7);
        var calibration = new DateOnly(nextMonth.Year, nextMonth.Month, 10);

        return new KpiPeriod(periodCode, assignStart, assignDeadline, midTerm, selfReview, managerReview, calibration);
    }

    public void MoveToInProgress()
    {
        if (Status != KpiPeriodStatus.Planning)
            throw new DomainException($"Không thể chuyển sang InProgress khi kỳ đang ở trạng thái {Status}.", "ERR_KPI_PERIOD_STATE_INVALID");

        Status = KpiPeriodStatus.InProgress;
    }

    public void MoveToSelfReview()
    {
        Status = KpiPeriodStatus.SelfReview;
    }

    public void MoveToManagerReview()
    {
        Status = KpiPeriodStatus.ManagerReview;
    }

    public void MoveToCalibration()
    {
        Status = KpiPeriodStatus.Calibration;
    }

    public void LockPeriod(string closedBy)
    {
        if (Status == KpiPeriodStatus.Locked)
            throw new DomainException("Kỳ đánh giá này đã được khóa trước đó.", "ERR_KPI_PERIOD_ALREADY_LOCKED");

        Status = KpiPeriodStatus.Locked;
        ClosedAt = DateTime.UtcNow;
        ClosedBy = closedBy;
    }
}
