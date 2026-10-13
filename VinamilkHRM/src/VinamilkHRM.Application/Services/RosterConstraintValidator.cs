using System;
using System.Collections.Generic;
using System.Linq;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Shifts;

namespace VinamilkHRM.Application.Services;

/// <summary>
/// Bộ kiểm tra ràng buộc phân ca và làm thêm giờ theo Bộ luật Lao động Việt Nam 2019.
/// </summary>
public class RosterConstraintValidator : IRosterConstraintValidator
{
    /// <summary>
    /// Ràng buộc Điều 110 BLLĐ 2019: Nghỉ giữa 2 ca làm việc ít nhất 12 giờ.
    /// </summary>
    public void ValidateRestBetweenShifts(
        DateOnly previousWorkDate,
        ShiftDefinition previousShift,
        DateOnly currentWorkDate,
        ShiftDefinition currentShift)
    {
        if (previousShift == null || currentShift == null)
            return;

        // Tính thời điểm kết thúc ca trước
        DateTime previousEnd = previousWorkDate.ToDateTime(previousShift.GetLastEndTime());
        if (previousShift.CrossesMidnight)
        {
            previousEnd = previousWorkDate.AddDays(1).ToDateTime(previousShift.GetLastEndTime());
        }

        // Tính thời điểm bắt đầu ca sau
        DateTime currentStart = currentWorkDate.ToDateTime(currentShift.GetFirstStartTime());

        double restHours = (currentStart - previousEnd).TotalHours;
        if (restHours < 12.0)
        {
            throw new DomainException(
                $"Vi phạm Điều 110 BLLĐ 2019: Thời gian nghỉ giữa 2 ca ({restHours:F1} giờ) nhỏ hơn quy định tối thiểu 12 giờ (Ca trước kết thúc lúc {previousEnd:yyyy-MM-dd HH:mm}, Ca sau bắt đầu lúc {currentStart:yyyy-MM-dd HH:mm}).",
                "ERR_ROSTER_REST_BETWEEN_SHIFTS_LESS_THAN_12H");
        }
    }

    /// <summary>
    /// Ràng buộc Điều 111 BLLĐ 2019: Mỗi tuần (chuỗi 7 ngày liên tục) phải có ít nhất 24 giờ nghỉ liên tục.
    /// </summary>
    public void ValidateWeeklyContinuousRest(
        IReadOnlyList<(DateOnly WorkDate, ShiftDefinition? Shift, bool IsOff)> weekSchedule)
    {
        if (weekSchedule == null || weekSchedule.Count < 7)
            return;

        // Nếu có ít nhất 1 ngày hoàn toàn OFF
        if (weekSchedule.Any(w => w.IsOff || w.Shift == null))
            return;

        // Nếu 7 ngày đều có ca, kiểm tra khoảng cách giữa ca ngày này tới ca ngày sau có chỗ nào >= 24h không
        bool has24hRest = false;
        for (int i = 0; i < weekSchedule.Count - 1; i++)
        {
            var cur = weekSchedule[i];
            var next = weekSchedule[i + 1];
            if (cur.Shift != null && next.Shift != null)
            {
                var curEnd = cur.WorkDate.ToDateTime(cur.Shift.GetLastEndTime());
                if (cur.Shift.CrossesMidnight) curEnd = cur.WorkDate.AddDays(1).ToDateTime(cur.Shift.GetLastEndTime());

                var nextStart = next.WorkDate.ToDateTime(next.Shift.GetFirstStartTime());
                if ((nextStart - curEnd).TotalHours >= 24.0)
                {
                    has24hRest = true;
                    break;
                }
            }
        }

        if (!has24hRest)
        {
            throw new DomainException(
                "Vi phạm Điều 111 BLLĐ 2019: Không đảm bảo ít nhất 24 giờ nghỉ liên tục trong chu kỳ 7 ngày.",
                "ERR_ROSTER_WEEKLY_REST_LESS_THAN_24H");
        }
    }

    /// <summary>
    /// Ràng buộc Điều 137 BLLĐ 2019: Lao động nữ mang thai từ tháng thứ 7 hoặc nuôi con < 12 tháng không làm đêm (22h-06h) hoặc tăng ca.
    /// </summary>
    public void ValidateMaternalProtection(
        Employee employee,
        DateOnly workDate,
        ShiftDefinition? shift,
        decimal plannedOtHours = 0m)
    {
        if (employee == null)
            return;

        if (employee.IsExemptFromNightAndOt(workDate))
        {
            if (shift != null)
            {
                bool isNight = shift.CrossesMidnight ||
                               shift.GetFirstStartTime() < new TimeOnly(6, 0) ||
                               shift.GetLastEndTime() > new TimeOnly(22, 0);

                if (isNight)
                {
                    throw new DomainException(
                        $"Vi phạm Điều 137 BLLĐ 2019: Lao động nữ {employee.FullName} đang trong chế độ thai sản/nuôi con nhỏ dưới 12 tháng, không được bố trí làm việc ban đêm (Ca {shift.Code}).",
                        "ERR_ROSTER_MATERNAL_NIGHT_PROHIBITED");
                }
            }

            if (plannedOtHours > 0)
            {
                throw new DomainException(
                    $"Vi phạm Điều 137 BLLĐ 2019: Lao động nữ {employee.FullName} đang trong chế độ thai sản/nuôi con nhỏ dưới 12 tháng, không được bố trí làm thêm giờ ({plannedOtHours:F1} giờ).",
                    "ERR_ROSTER_MATERNAL_OT_PROHIBITED");
            }
        }
    }

    /// <summary>
    /// Ràng buộc Điều 107 BLLĐ 2019: Giới hạn trần làm thêm giờ (ngày, tháng: 40 giờ, năm: 200/300 giờ).
    /// </summary>
    public void ValidateOvertimeCap(
        decimal dailyOtHours,
        decimal currentMonthlyOtHours,
        decimal currentYearlyOtHours,
        decimal monthlyCap = 40m,
        decimal yearlyCap = 200m)
    {
        if (dailyOtHours > 4.0m)
        {
            throw new DomainException(
                $"Vi phạm Điều 107 BLLĐ 2019: Giờ làm thêm trong ngày ({dailyOtHours:F1}h) vượt quá 4 giờ/ngày.",
                "ERR_OT_DAILY_CAP_EXCEEDED");
        }

        if (currentMonthlyOtHours + dailyOtHours > monthlyCap)
        {
            throw new DomainException(
                $"Vi phạm Điều 107 BLLĐ 2019: Tổng giờ làm thêm trong tháng ({currentMonthlyOtHours + dailyOtHours:F1}h) vượt trần quy định {monthlyCap:F0} giờ/tháng.",
                "ERR_OT_MONTHLY_CAP_EXCEEDED");
        }

        if (currentYearlyOtHours + dailyOtHours > yearlyCap)
        {
            throw new DomainException(
                $"Vi phạm Điều 107 BLLĐ 2019: Tổng giờ làm thêm trong năm ({currentYearlyOtHours + dailyOtHours:F1}h) vượt trần quy định {yearlyCap:F0} giờ/năm.",
                "ERR_OT_YEARLY_CAP_EXCEEDED");
        }
    }
}
