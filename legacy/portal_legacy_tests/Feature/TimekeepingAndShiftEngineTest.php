<?php

namespace Tests\Feature;

use App\Models\AuditLog;
use App\Modules\Attendance\Models\AttendanceDay;
use App\Modules\Attendance\Models\AttendanceLog;
use App\Modules\Attendance\Models\Shift;
use App\Modules\Attendance\Models\ShiftSchedule;
use App\Modules\Attendance\Services\ShiftRosterService;
use App\Modules\Attendance\Services\TimeEngineService;
use App\Modules\Employee\Models\Employee;
use App\Modules\Organization\Models\EmployeeAssignment;
use App\Modules\Organization\Models\OrgUnit;
use App\Modules\Recruitment\Models\HealthCheck;
use Carbon\Carbon;
use DomainException;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Illuminate\Support\Facades\DB;
use Tests\TestCase;

class TimekeepingAndShiftEngineTest extends TestCase
{
    use RefreshDatabase;

    protected TimeEngineService $timeEngine;
    protected ShiftRosterService $rosterService;
    protected Employee $worker;
    protected OrgUnit $factory;
    protected Shift $shiftAdmin;
    protected Shift $shift1;
    protected Shift $shift2;
    protected Shift $shift3Night;
    protected Shift $shiftFarmEarly;

    protected function setUp(): void
    {
        parent::setUp();

        $this->timeEngine = app(TimeEngineService::class);
        $this->rosterService = app(ShiftRosterService::class);

        // 1. Tạo Đơn vị Nhà máy
        $this->factory = OrgUnit::create([
            'unit_code' => 'NM_BD', 'unit_name' => 'Nhà máy Mega Bình Dương',
            'unit_type' => 'FACTORY', 'path' => '/1/2/7/', 'level' => 3, 'effective_from' => '2026-01-01'
        ]);

        // 2. Tạo Công nhân
        $this->worker = Employee::create([
            'employee_code' => 'VNM-000088', 'full_name' => 'Lê Văn Thắng',
            'gender' => 'MALE', 'date_of_birth' => '1992-05-15', 'national_id' => '079092008888',
            'phone_number' => '0938888888', 'hire_date' => '2022-01-01', 'employment_status' => 'ACTIVE'
        ]);

        EmployeeAssignment::create([
            'employee_id' => $this->worker->id, 'org_unit_id' => $this->factory->id,
            'position_id' => 1, 'is_primary' => true, 'is_current' => true, 'effective_from' => '2022-01-01'
        ]);

        // 3. Khởi tạo Danh mục Ca đặc thù
        $this->shiftAdmin = Shift::create([
            'shift_code' => 'CA_HC', 'shift_name' => 'Ca Hành chính',
            'start_time' => '08:00:00', 'end_time' => '17:00:00',
            'break_minutes' => 60, 'grace_period_late_minutes' => 15,
            'standard_hours' => 8.0, 'is_night_shift' => false
        ]);

        $this->shift1 = Shift::create([
            'shift_code' => 'CA_1', 'shift_name' => 'Nhà máy Ca 1',
            'start_time' => '06:00:00', 'end_time' => '14:00:00',
            'break_minutes' => 30, 'grace_period_late_minutes' => 10,
            'standard_hours' => 8.0, 'is_night_shift' => false
        ]);

        $this->shift2 = Shift::create([
            'shift_code' => 'CA_2', 'shift_name' => 'Nhà máy Ca 2',
            'start_time' => '14:00:00', 'end_time' => '22:00:00',
            'break_minutes' => 30, 'grace_period_late_minutes' => 10,
            'standard_hours' => 8.0, 'is_night_shift' => false
        ]);

        $this->shift3Night = Shift::create([
            'shift_code' => 'CA_3', 'shift_name' => 'Nhà máy Ca 3 Đêm',
            'start_time' => '22:00:00', 'end_time' => '06:00:00', // Qua ngày!
            'break_minutes' => 45, 'grace_period_late_minutes' => 10,
            'standard_hours' => 8.0, 'is_night_shift' => true
        ]);

        $this->shiftFarmEarly = Shift::create([
            'shift_code' => 'CA_FARM_EARLY', 'shift_name' => 'Trang trại Ca sớm vắt sữa',
            'start_time' => '04:30:00', 'end_time' => '12:30:00',
            'break_minutes' => 60, 'grace_period_late_minutes' => 10,
            'standard_hours' => 8.0, 'is_night_shift' => false
        ]);
    }

    /**
     * TEST CASE 1: Quên chấm ra (Missing Clock-Out) -> Ghi nhận status MISSING_OUT, chờ giải trình
     */
    public function test_1_missing_clock_out_is_flagged_as_missing_out()
    {
        $date = Carbon::parse('2026-10-15');
        $schedule = ShiftSchedule::create([
            'employee_id' => $this->worker->id, 'shift_id' => $this->shiftAdmin->id, 'work_date' => $date
        ]);

        // Chỉ có log quẹt vào lúc 07:55, không có log quẹt ra
        AttendanceLog::create([
            'employee_id' => $this->worker->id, 'device_code' => 'DEVICE_GATE_1',
            'check_time' => '2026-10-15 07:55:00', 'created_at' => now()
        ]);

        $result = $this->timeEngine->processSingleSchedule($schedule, $date);

        $this->assertEquals('MISSING_OUT', $result->status);
        $this->assertEquals(0.0, $result->actual_hours);
        $this->assertNull($result->last_out);
    }

    /**
     * TEST CASE 2: Ca 3 xuyên đêm (22:00 -> 06:00 hôm sau) vắt sang ngày mới và tính giờ đêm
     */
    public function test_2_overnight_shift_pairs_next_day_clock_out_and_calculates_night_hours()
    {
        $date = Carbon::parse('2026-10-15');
        $schedule = ShiftSchedule::create([
            'employee_id' => $this->worker->id, 'shift_id' => $this->shift3Night->id, 'work_date' => $date
        ]);

        // Quẹt vào: 21:52 ngày 15/10. Quẹt ra: 06:05 ngày 16/10 (hôm sau)
        AttendanceLog::create(['employee_id' => $this->worker->id, 'device_code' => 'FAC_1', 'check_time' => '2026-10-15 21:52:00', 'created_at' => now()]);
        AttendanceLog::create(['employee_id' => $this->worker->id, 'device_code' => 'FAC_1', 'check_time' => '2026-10-16 06:05:00', 'created_at' => now()]);

        $result = $this->timeEngine->processSingleSchedule($schedule, $date);

        $this->assertEquals('PRESENT', $result->status);
        $this->assertEquals('2026-10-16 06:05:00', $result->last_out->toDateTimeString());
        // Giờ làm đêm từ 22:00 đến 06:00 = 8.0 giờ (hưởng phụ cấp đêm +30%)
        $this->assertGreaterThanOrEqual(7.5, $result->night_work_hours);
    }

    /**
     * TEST CASE 3: Đổi ca giữa tuần vi phạm quy định nghỉ tối thiểu 12 giờ (Điều 110 BLLĐ)
     */
    public function test_3_shift_change_violating_12_hours_rest_is_blocked()
    {
        $tuesday = Carbon::parse('2026-10-13');
        $wednesday = Carbon::parse('2026-10-14');

        // Thứ Ba xếp Ca 2 (Tan ca lúc 22:00)
        ShiftSchedule::create([
            'employee_id' => $this->worker->id, 'shift_id' => $this->shift2->id, 'work_date' => $tuesday
        ]);

        // Thứ Tư cố tình xếp Ca 1 (Bắt đầu lúc 06:00 sáng hôm sau -> Chỉ nghỉ 8 giờ!)
        $this->expectException(DomainException::class);
        $this->expectExceptionMessage('Vi phạm luật lao động: Thời gian nghỉ giữa ca');

        $this->rosterService->assignShift($this->worker->id, $this->shift1->id, $wednesday->toDateString());
    }

    /**
     * TEST CASE 4: Chấm công Mobile GPS ngoài bán kính cho phép (> 200m) bị từ chối
     */
    public function test_4_gps_clocking_outside_allowed_radius_throws_exception()
    {
        $this->expectException(DomainException::class);
        $this->expectExceptionMessage('Tọa độ GPS của bạn cách cơ sở làm việc');

        // Tọa độ cách xa nhà máy Bình Dương 5km
        $this->timeEngine->recordGpsClock($this->worker->id, 10.8231, 106.6297, now()->toDateTimeString());
    }

    /**
     * TEST CASE 5: Chặn / Cảnh báo công khi Giấy khám sức khỏe / Thẻ Xanh thực phẩm hết hạn
     */
    public function test_5_expired_health_certificate_flags_warning_and_blocks_normal_status()
    {
        $date = Carbon::parse('2026-10-15');
        $schedule = ShiftSchedule::create([
            'employee_id' => $this->worker->id, 'shift_id' => $this->shift1->id, 'work_date' => $date
        ]);

        // Thẻ xanh đã hết hạn từ ngày 10/10/2026
        HealthCheck::create([
            'employee_id' => $this->worker->id, 'check_date' => '2025-10-10',
            'medical_facility' => 'BV Đa khoa', 'health_type' => 'GREEN_CARD_FOOD_SAFETY',
            'health_class' => 'CLASS_1', 'is_passed' => true, 'expiry_date' => '2026-10-10'
        ]);

        AttendanceLog::create(['employee_id' => $this->worker->id, 'device_code' => 'GATE', 'check_time' => '2026-10-15 05:50:00', 'created_at' => now()]);
        AttendanceLog::create(['employee_id' => $this->worker->id, 'device_code' => 'GATE', 'check_time' => '2026-10-15 14:05:00', 'created_at' => now()]);

        $result = $this->timeEngine->processSingleSchedule($schedule, $date);

        $this->assertTrue($result->health_warning);
        $this->assertEquals('HEALTH_CERT_EXPIRED', $result->status);
    }

    /**
     * TEST CASE 6: Ca sáng sớm trang trại (04:30 - 12:30): Tách 1.5 giờ đêm (04:30 - 06:00) và giờ ngày
     */
    public function test_6_farm_early_shift_splits_night_work_from_day_work()
    {
        $date = Carbon::parse('2026-10-15');
        $schedule = ShiftSchedule::create([
            'employee_id' => $this->worker->id, 'shift_id' => $this->shiftFarmEarly->id, 'work_date' => $date
        ]);

        AttendanceLog::create(['employee_id' => $this->worker->id, 'device_code' => 'FARM_GATE', 'check_time' => '2026-10-15 04:25:00', 'created_at' => now()]);
        AttendanceLog::create(['employee_id' => $this->worker->id, 'device_code' => 'FARM_GATE', 'check_time' => '2026-10-15 12:35:00', 'created_at' => now()]);

        $result = $this->timeEngine->processSingleSchedule($schedule, $date);

        // Khung giờ từ 04:30 đến 06:00 sáng là giờ đêm = 1.5 giờ đêm
        $this->assertEquals(1.5, $result->night_work_hours);
        $this->assertEquals(8.0, $result->actual_hours);
    }

    /**
     * TEST CASE 7: Vượt quá giới hạn làm thêm giờ 40 giờ/tháng bị cảnh báo chặn
     */
    public function test_7_overtime_exceeding_monthly_legal_cap_throws_exception()
    {
        DB::table('legal_parameters')->insert([
            'param_code' => 'MAX_OT_HOURS_PER_MONTH', 'param_name' => 'Trần OT tháng',
            'param_value' => 40.0, 'unit' => 'HOURS', 'effective_from' => '2026-01-01', 'is_current' => 1
        ]);

        // Đã tích lũy 38 giờ OT trong tháng 10
        AttendanceDay::create([
            'employee_id' => $this->worker->id, 'work_date' => '2026-10-05',
            'ot_normal_hours' => 38.0, 'status' => 'PRESENT'
        ]);

        $this->expectException(DomainException::class);
        $this->expectExceptionMessage('Cảnh báo vi phạm trần OT');

        // Cố tình xếp thêm 4 giờ OT nữa -> Tổng = 42 > 40h!
        $this->rosterService->checkOvertimeLimit($this->worker->id, Carbon::parse('2026-10-15'), 4.0);
    }

    /**
     * TEST CASE 8: Bảng công sau khi HR Local chốt tháng bị khóa; sửa đổi bắt buộc có lý do và ghi audit_logs
     */
    public function test_8_modifying_locked_timesheet_requires_reason_and_records_audit_log()
    {
        $date = Carbon::parse('2026-10-15');
        $attDay = AttendanceDay::create([
            'employee_id' => $this->worker->id, 'work_date' => $date, 'actual_hours' => 7.0, 'is_locked' => false
        ]);

        // 1. HR Local chốt khóa bảng công tháng 10
        $this->timeEngine->lockMonthlyTimesheet($this->factory->id, 2026, 10, 99);
        $this->assertTrue($attDay->fresh()->is_locked);

        // 2. Cố tình sửa mà không có lý do giải trình -> Bị chặn
        try {
            $this->timeEngine->modifyLockedRecord($attDay->id, ['actual_hours' => 8.0], '', 99);
            $this->fail('Phải báo lỗi khi sửa bảng công bị khóa mà không có lý do.');
        } catch (DomainException $e) {
            $this->assertStringContainsString('Bắt buộc phải nhập lý do giải trình', $e->getMessage());
        }

        // 3. Sửa hợp lệ có lý do -> Ghi nhận và ghi vết vào audit_logs
        $updated = $this->timeEngine->modifyLockedRecord(
            $attDay->id,
            ['actual_hours' => 8.0],
            'Duyệt đơn giải trình bổ sung quẹt thẻ cổng phụ do sự cố cúp điện',
            99
        );

        $this->assertEquals(8.0, $updated->actual_hours);
        $this->assertDatabaseHas('audit_logs', [
            'table_name'   => 'attendance_days',
            'record_id'    => $attDay->id,
            'performed_by' => 99,
        ]);
    }
}
