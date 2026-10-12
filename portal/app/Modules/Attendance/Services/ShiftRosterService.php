<?php

namespace App\Modules\Attendance\Services;

use App\Modules\Attendance\Models\Shift;
use App\Modules\Attendance\Models\ShiftSchedule;
use App\Modules\Employee\Models\Employee;
use Carbon\Carbon;
use DomainException;
use Illuminate\Support\Facades\DB;

class ShiftRosterService
{
    /**
     * Xếp ca làm việc cho nhân viên và kiểm tra nghiêm ngặt 2 ràng buộc:
     * 1. Nghỉ tối thiểu 12 giờ giữa 2 ca liên tiếp.
     * 2. Không vượt trần giờ làm thêm (OT) theo tháng/năm từ bảng tham số pháp lý.
     */
    public function assignShift(int $employeeId, int $shiftId, string $workDateStr, ?string $note = null): ShiftSchedule
    {
        $employee = Employee::findOrFail($employeeId);
        $shift = Shift::findOrFail($shiftId);
        $workDate = Carbon::parse($workDateStr);

        // 1. KIỂM TRA RÀNG BUỘC: NGHỈ TỐI THIỂU 12 GIỜ GIỮA HAI CA LIÊN TIẾP (Điều 110 BLLĐ 2019)
        $this->validateRestInterval($employeeId, $shift, $workDate);

        // 2. Ghi nhận hoặc cập nhật lịch phân ca
        return ShiftSchedule::updateOrCreate(
            [
                'employee_id' => $employeeId,
                'work_date'   => $workDate->toDateString(),
            ],
            [
                'shift_id' => $shiftId,
                'note'     => $note,
            ]
        );
    }

    /**
     * Kiểm tra thời gian nghỉ giữa ca làm việc hôm trước, ca hiện tại và ca kế tiếp >= 12 giờ.
     */
    public function validateRestInterval(int $employeeId, Shift $newShift, Carbon $workDate): void
    {
        // Thời điểm bắt đầu và kết thúc của ca đang xếp
        $newStart = Carbon::parse($workDate->toDateString() . ' ' . $newShift->start_time);
        $newEnd = Carbon::parse($workDate->toDateString() . ' ' . $newShift->end_time);

        // Nếu ca qua đêm (VD: 22:00 -> 06:00), giờ kết thúc cộng thêm 1 ngày
        if ($newShift->is_night_shift || $newShift->end_time < $newShift->start_time) {
            $newEnd->addDay();
        }

        // 1. Kiểm tra với ca liền kề ngày hôm trước
        $prevSchedule = ShiftSchedule::with('shift')
            ->where('employee_id', $employeeId)
            ->where('work_date', $workDate->copy()->subDay()->toDateString())
            ->first();

        if ($prevSchedule && $prevSchedule->shift) {
            $prevStart = Carbon::parse($prevSchedule->work_date->toDateString() . ' ' . $prevSchedule->shift->start_time);
            $prevEnd = Carbon::parse($prevSchedule->work_date->toDateString() . ' ' . $prevSchedule->shift->end_time);
            if ($prevSchedule->shift->is_night_shift || $prevSchedule->shift->end_time < $prevSchedule->shift->start_time) {
                $prevEnd->addDay();
            }

            $restHoursFromPrev = $prevEnd->diffInMinutes($newStart, false) / 60.0;
            if ($restHoursFromPrev < 12.0) {
                throw new DomainException(
                    "Vi phạm luật lao động: Thời gian nghỉ giữa ca liền trước (tan lúc {$prevEnd->format('H:i d/m')}) " .
                    "và ca mới (bắt đầu lúc {$newStart->format('H:i d/m')}) chỉ đạt {$restHoursFromPrev} giờ (Yêu cầu tối thiểu 12 giờ)."
                );
            }
        }

        // 2. Kiểm tra với ca liền kề ngày hôm sau
        $nextSchedule = ShiftSchedule::with('shift')
            ->where('employee_id', $employeeId)
            ->where('work_date', $workDate->copy()->addDay()->toDateString())
            ->first();

        if ($nextSchedule && $nextSchedule->shift) {
            $nextStart = Carbon::parse($nextSchedule->work_date->toDateString() . ' ' . $nextSchedule->shift->start_time);
            $restHoursToNext = $newEnd->diffInMinutes($nextStart, false) / 60.0;
            if ($restHoursToNext < 12.0) {
                throw new DomainException(
                    "Vi phạm luật lao động: Ca mới tan lúc {$newEnd->format('H:i d/m')} " .
                    "nhưng ca ngày hôm sau bắt đầu lúc {$nextStart->format('H:i d/m')} (Thời gian nghỉ chỉ đạt {$restHoursToNext} giờ, yêu cầu tối thiểu 12 giờ)."
                );
            }
        }
    }

    /**
     * Kiểm tra giới hạn làm thêm giờ (OT) tích lũy trong tháng/năm.
     */
    public function checkOvertimeLimit(int $employeeId, Carbon $monthDate, float $additionalOtHours): void
    {
        // Đọc trần OT tháng từ bảng tham số pháp lý legal_parameters
        $monthlyLimitParam = DB::table('legal_parameters')
            ->where('param_code', 'MAX_OT_HOURS_PER_MONTH')
            ->where('is_current', true)
            ->value('param_value') ?? 40.0;

        // Tính tổng OT đã ghi nhận trong tháng hiện tại
        $currentMonthOt = DB::table('attendance_days')
            ->where('employee_id', $employeeId)
            ->whereYear('work_date', $monthDate->year)
            ->whereMonth('work_date', $monthDate->month)
            ->whereNull('deleted_at')
            ->sum(DB::raw('ot_normal_hours + ot_weekend_hours + ot_holiday_hours')) ?? 0.0;

        $projectedTotal = $currentMonthOt + $additionalOtHours;

        if ($projectedTotal > $monthlyLimitParam) {
            throw new DomainException(
                "Cảnh báo vi phạm trần OT: Tổng giờ làm thêm trong tháng của nhân sự sẽ đạt {$projectedTotal} giờ, " .
                "vượt quá ngưỡng tối đa cho phép {$monthlyLimitParam} giờ/tháng theo Điều 107 Bộ luật Lao động."
            );
        }
    }
}
