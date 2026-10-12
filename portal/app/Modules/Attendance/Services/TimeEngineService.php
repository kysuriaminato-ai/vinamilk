<?php

namespace App\Modules\Attendance\Services;

use App\Models\AuditLog;
use App\Modules\Attendance\Models\AttendanceDay;
use App\Modules\Attendance\Models\AttendanceLog;
use App\Modules\Attendance\Models\Shift;
use App\Modules\Attendance\Models\ShiftSchedule;
use App\Modules\Employee\Models\Employee;
use App\Modules\Recruitment\Models\HealthCheck;
use Carbon\Carbon;
use DomainException;
use Illuminate\Support\Facades\DB;

class TimeEngineService
{
    /**
     * BƯỚC 1: Tiếp nhận log chấm công từ thiết bị vân tay / FaceID qua REST API.
     */
    public function recordBiometricLog(string $employeeCode, string $deviceCode, string $timestamp, string $type = 'UNKNOWN'): AttendanceLog
    {
        $employee = Employee::where('employee_code', $employeeCode)->firstOrFail();

        return AttendanceLog::create([
            'employee_id' => $employee->id,
            'device_code' => $deviceCode,
            'check_time'  => Carbon::parse($timestamp),
            'check_type'  => $type,
            'source'      => 'BIOMETRIC_DEVICE',
            'created_at'  => now(),
        ]);
    }

    /**
     * BƯỚC 2: Tiếp nhận check-in GPS từ Mobile và kiểm tra bán kính bằng công thức Haversine.
     */
    public function recordGpsClock(int $employeeId, float $lat, float $lng, string $timestamp): AttendanceLog
    {
        $employee = Employee::with('currentAssignment.orgUnit')->findOrFail($employeeId);
        $orgUnit = $employee->currentOrgUnit;

        // Tọa độ mẫu của Nhà máy/Trang trại Vinamilk (Nếu chưa có trong CSDL, dùng tọa độ mặc định)
        $targetLat = 11.0850; // Mega Factory Bình Dương
        $targetLng = 106.6120;
        $allowedRadiusMeters = 200.0; // Bán kính tối đa 200 mét

        $distance = $this->calculateHaversineDistance($lat, $lng, $targetLat, $targetLng);

        if ($distance > $allowedRadiusMeters) {
            throw new DomainException(
                "Chấm công thất bại: Tọa độ GPS của bạn cách cơ sở làm việc " . round($distance) . " mét, vượt quá bán kính cho phép ({$allowedRadiusMeters}m)."
            );
        }

        return AttendanceLog::create([
            'employee_id' => $employeeId,
            'device_code' => 'MOBILE_GPS',
            'check_time'  => Carbon::parse($timestamp),
            'check_type'  => 'UNKNOWN',
            'source'      => 'GPS_MOBILE',
            'latitude'    => $lat,
            'longitude'   => $lng,
            'created_at'  => now(),
        ]);
    }

    /**
     * BƯỚC 3: TIME ENGINE - BỘ MÁY XỬ LÝ CHẤM CÔNG CHẠY MỖI ĐÊM (NIGHTLY BATCH JOB).
     * Ghép log quét thẻ với ca đã xếp, áp dụng Thuật toán Ca qua đêm và tính toán OT theo ot_rates.
     */
    public function processDailyAttendance(Carbon $workDate): int
    {
        $schedules = ShiftSchedule::with(['shift', 'employee.currentAssignment.orgUnit'])
            ->where('work_date', $workDate->toDateString())
            ->get();

        $processedCount = 0;

        foreach ($schedules as $schedule) {
            $this->processSingleSchedule($schedule, $workDate);
            $processedCount++;
        }

        return $processedCount;
    }

    public function processSingleSchedule(ShiftSchedule $schedule, Carbon $workDate): AttendanceDay
    {
        $employee = $schedule->employee;
        $shift = $schedule->shift;

        // 1. CHẶN / CẢNH BÁO CHẤM CÔNG NẾU THẺ XANH / KSK HẾT HẠN (KHỐI SẢN XUẤT & TRANG TRẠI)
        $healthWarning = false;
        if (in_array($employee->currentOrgUnit?->unit_type, ['FACTORY', 'DAIRY_FARM'])) {
            $latestHealth = HealthCheck::where('employee_id', $employee->id)->latest('check_date')->first();
            if (!$latestHealth || !$latestHealth->is_passed || $latestHealth->expiry_date < $workDate->toDateString()) {
                $healthWarning = true;
            }
        }

        // 2. THUẬT TOÁN XÁC ĐỊNH CỬA SỔ QUÉT LOG (MATCHING WINDOW)
        // Ca ngày: Từ workDate 00:00 đến workDate 23:59
        // Ca đêm (Ca 3: 22:00 -> 06:00 hôm sau): Cửa sổ Vào [20:00 -> 01:00], Cửa sổ Ra [04:00 -> 10:00 hôm sau]
        $isOvernight = $shift->is_night_shift || ($shift->end_time < $shift->start_time);

        $shiftStart = Carbon::parse($workDate->toDateString() . ' ' . $shift->start_time);
        $shiftEnd = Carbon::parse($workDate->toDateString() . ' ' . $shift->end_time);
        if ($isOvernight) {
            $shiftEnd->addDay();
        }

        // Lấy tất cả log trong phạm vi từ (shiftStart - 2h) đến (shiftEnd + 4h)
        $windowStart = $shiftStart->copy()->subHours(2);
        $windowEnd = $shiftEnd->copy()->addHours(4);

        $logs = AttendanceLog::where('employee_id', $employee->id)
            ->whereBetween('check_time', [$windowStart, $windowEnd])
            ->orderBy('check_time')
            ->get();

        $firstIn = $logs->first()?->check_time;
        $lastOut = $logs->count() > 1 ? $logs->last()?->check_time : null;

        // 3. XỬ LÝ TRƯỜNG HỢP BIÊN: QUÊN CHẤM RA (MISSING CLOCK-OUT)
        if ($firstIn && !$lastOut) {
            return AttendanceDay::updateOrCreate(
                ['employee_id' => $employee->id, 'work_date' => $workDate->toDateString()],
                [
                    'shift_id'         => $shift->id,
                    'first_in'         => $firstIn,
                    'last_out'         => null,
                    'late_minutes'     => 0,
                    'early_minutes'    => 0,
                    'actual_hours'     => 0.0,
                    'standard_hours'   => $shift->standard_hours,
                    'status'           => 'MISSING_OUT', // Quên chấm ra -> Chờ giải trình
                    'health_warning'   => $healthWarning,
                ]
            );
        }

        // Trường hợp vắng mặt hoàn toàn
        if (!$firstIn && !$lastOut) {
            return AttendanceDay::updateOrCreate(
                ['employee_id' => $employee->id, 'work_date' => $workDate->toDateString()],
                [
                    'shift_id'         => $shift->id,
                    'actual_hours'     => 0.0,
                    'standard_hours'   => $shift->standard_hours,
                    'status'           => 'ABSENT',
                    'health_warning'   => $healthWarning,
                ]
            );
        }

        // 4. TÍNH TOÁN ĐI MUỘN & VỀ SỚM (Có tính phút cho phép trễ grace_period)
        $allowedIn = $shiftStart->copy()->addMinutes($shift->grace_period_late_minutes);
        $lateMinutes = $firstIn->greaterThan($allowedIn) ? $shiftStart->diffInMinutes($firstIn) : 0;

        $earlyMinutes = $lastOut->lessThan($shiftEnd) ? $lastOut->diffInMinutes($shiftEnd) : 0;

        // 5. TÍNH GIỜ CÔNG THỰC TẾ & GIỜ LÀM BAN ĐÊM (22:00 -> 06:00)
        $totalMinutes = $firstIn->diffInMinutes($lastOut);
        $workMinutes = max(0, $totalMinutes - $shift->break_minutes);
        $actualHours = round($workMinutes / 60.0, 2);

        $nightHours = $this->calculateNightWorkHours($firstIn, $lastOut);

        // 6. TÍNH GIỜ LÀM THÊM (OT) PHÂN LOẠI THEO NGÀY THƯỜNG / NGHỈ TUẦN / LỄ TẾT
        $otTotalHours = max(0.0, $actualHours - $shift->standard_hours);
        $otNormal = 0.0;
        $otWeekend = 0.0;
        $otHoliday = 0.0;

        $dayType = $this->determineDayType($workDate);
        if ($dayType === 'HOLIDAY') {
            $otHoliday = $otTotalHours;
        } elseif ($dayType === 'WEEKEND') {
            $otWeekend = $otTotalHours;
        } else {
            $otNormal = $otTotalHours;
        }

        $status = ($lateMinutes > 0) ? 'LATE' : (($earlyMinutes > 0) ? 'EARLY' : 'PRESENT');
        if ($healthWarning) {
            $status = 'HEALTH_CERT_EXPIRED';
        }

        return AttendanceDay::updateOrCreate(
            ['employee_id' => $employee->id, 'work_date' => $workDate->toDateString()],
            [
                'shift_id'          => $shift->id,
                'first_in'          => $firstIn,
                'last_out'          => $lastOut,
                'late_minutes'      => $lateMinutes,
                'early_minutes'     => $earlyMinutes,
                'actual_hours'      => $actualHours,
                'standard_hours'    => $shift->standard_hours,
                'ot_normal_hours'   => $otNormal,
                'ot_weekend_hours'  => $otWeekend,
                'ot_holiday_hours'  => $otHoliday,
                'ot_night_hours'    => ($nightHours > 0 && $otTotalHours > 0) ? min($nightHours, $otTotalHours) : 0.0,
                'night_work_hours'  => $nightHours,
                'status'            => $status,
                'health_warning'    => $healthWarning,
            ]
        );
    }

    /**
     * BƯỚC 6: CHỐT CÔNG THÁNG & KHÓA SỬA (YÊU CẦU LÝ DO VÀ GHI AUDIT KHI ĐIỀU CHỈNH).
     */
    public function lockMonthlyTimesheet(int $orgUnitId, int $year, int $month, int $hrUserId): int
    {
        return DB::transaction(function () use ($orgUnitId, $year, $month, $hrUserId) {
            $count = AttendanceDay::whereHas('employee.currentAssignment', function ($q) use ($orgUnitId) {
                $q->where('org_unit_id', $orgUnitId);
            })
            ->whereYear('work_date', $year)
            ->whereMonth('work_date', $month)
            ->update(['is_locked' => true]);

            AuditLog::create([
                'table_name'   => 'attendance_days',
                'record_id'    => $orgUnitId,
                'action'       => 'UPDATE',
                'performed_by' => $hrUserId,
                'new_values'   => ['event' => 'MONTHLY_TIMEKEEPING_LOCKED', 'year' => $year, 'month' => $month, 'records' => $count],
            ]);

            return $count;
        });
    }

    public function modifyLockedRecord(int $attendanceDayId, array $newData, string $reason, int $hrUserId): AttendanceDay
    {
        return DB::transaction(function () use ($attendanceDayId, $newData, $reason, $hrUserId) {
            $record = AttendanceDay::findOrFail($attendanceDayId);
            $oldValues = $record->toArray();

            if ($record->is_locked && empty($reason)) {
                throw new DomainException('Bảng công tháng đã bị khóa. Bắt buộc phải nhập lý do giải trình để thực hiện điều chỉnh.');
            }

            $record->update($newData);

            AuditLog::create([
                'table_name'   => 'attendance_days',
                'record_id'    => $record->id,
                'action'       => 'UPDATE',
                'performed_by' => $hrUserId,
                'old_values'   => $oldValues,
                'new_values'   => array_merge($record->fresh()->toArray(), ['modification_reason' => $reason]),
            ]);

            return $record->fresh();
        });
    }

    /**
     * Thuật toán tính số giờ làm việc nằm trong khung giờ ban đêm (22:00 -> 06:00).
     */
    public function calculateNightWorkHours(Carbon $in, Carbon $out): float
    {
        $nightMinutes = 0;
        $current = $in->copy();

        // Bước nhảy 15 phút để tính giao thoa chính xác
        while ($current->lessThan($out)) {
            $hour = $current->hour;
            // Khung giờ đêm từ 22:00 hôm nay đến trước 06:00 sáng hôm sau
            if ($hour >= 22 || $hour < 6) {
                $nightMinutes += 15;
            }
            $current->addMinutes(15);
        }

        return round($nightMinutes / 60.0, 2);
    }

    private function determineDayType(Carbon $date): string
    {
        // Có thể mở rộng đối soát với bảng danh mục ngày lễ holidays
        if ($date->isSunday()) {
            return 'WEEKEND';
        }
        return 'WEEKDAY';
    }

    private function calculateHaversineDistance(float $lat1, float $lon1, float $lat2, float $lon2): float
    {
        $earthRadius = 6371000; // mét
        $latDelta = deg2rad($lat2 - $lat1);
        $lonDelta = deg2rad($lon2 - $lon1);

        $a = sin($latDelta / 2) * sin($latDelta / 2) +
             cos(deg2rad($lat1)) * cos(deg2rad($lat2)) *
             sin($lonDelta / 2) * sin($lonDelta / 2);

        $c = 2 * atan2(sqrt($a), sqrt(1 - $a));

        return $earthRadius * $c;
    }
}
