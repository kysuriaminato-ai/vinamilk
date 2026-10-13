using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;

namespace VinamilkHRM.Domain.Entities.Analytics;

/// <summary>
/// Bản chụp số liệu nhân sự định kỳ (WorkforceSnapshot) lưu trong Schema rpt.
/// Chụp vào đêm cuối tháng để số liệu lịch sử không bao giờ thay đổi khi hồ sơ nhân viên biến động.
/// </summary>
public class WorkforceSnapshot : BaseAuditableEntity
{
    public string Month { get; private set; } = string.Empty; // Định dạng YYYY-MM
    public int OrganizationalUnitId { get; private set; }
    public virtual OrganizationalUnit? OrganizationalUnit { get; private set; }

    public int StartHeadcount { get; private set; }
    public int EndHeadcount { get; private set; }
    public decimal AverageHeadcount { get; private set; }

    public int Hires { get; private set; }
    public int Exits { get; private set; }
    public string ExitsByReasonJson { get; private set; } = "{}"; // JSON phân bố lý do nghỉ

    public int TransfersIn { get; private set; }
    public int TransfersOut { get; private set; }
    public int Promotions { get; private set; }

    public decimal AbsenceRate { get; private set; }      // Tỷ lệ vắng mặt %
    public decimal OvertimeHours { get; private set; }    // Tổng giờ làm thêm
    public decimal LaborCost { get; private set; }        // Tổng chi phí nhân sự gồm BH doanh nghiệp
    public decimal AvgKpi { get; private set; }           // Điểm KPI trung bình
    public string GradeDistributionJson { get; private set; } = "{}"; // JSON phân bố bậc ngạch / xếp loại

    public DateTimeOffset SnapshotDate { get; private set; } = DateTimeOffset.UtcNow;
    public bool IsSampleData { get; private set; } = false;

    protected WorkforceSnapshot() { }

    public WorkforceSnapshot(
        string month,
        int organizationalUnitId,
        int startHeadcount,
        int endHeadcount,
        int hires,
        int exits,
        string exitsByReasonJson,
        int transfersIn,
        int transfersOut,
        int promotions,
        decimal absenceRate,
        decimal overtimeHours,
        decimal laborCost,
        decimal avgKpi,
        string gradeDistributionJson,
        bool isSampleData = false)
    {
        if (string.IsNullOrWhiteSpace(month))
            throw new DomainException("Tháng chụp dữ liệu không được để trống.", "ERR_RPT_MONTH_EMPTY");

        if (startHeadcount < 0 || endHeadcount < 0)
            throw new DomainException("Số lượng nhân sự đầu/cuối kỳ không thể âm.", "ERR_RPT_HEADCOUNT_NEGATIVE");

        Month = month.Trim();
        OrganizationalUnitId = organizationalUnitId;
        StartHeadcount = startHeadcount;
        EndHeadcount = endHeadcount;
        AverageHeadcount = (startHeadcount + endHeadcount) / 2.0m;

        Hires = hires;
        Exits = exits;
        ExitsByReasonJson = exitsByReasonJson ?? "{}";
        TransfersIn = transfersIn;
        TransfersOut = transfersOut;
        Promotions = promotions;
        AbsenceRate = absenceRate;
        OvertimeHours = overtimeHours;
        LaborCost = laborCost;
        AvgKpi = avgKpi;
        GradeDistributionJson = gradeDistributionJson ?? "{}";
        SnapshotDate = DateTimeOffset.UtcNow;
        IsSampleData = isSampleData;
    }
}