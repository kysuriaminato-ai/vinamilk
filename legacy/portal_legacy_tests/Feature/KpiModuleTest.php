<?php

namespace Tests\Feature;

use App\Models\AuditLog;
use App\Models\Notification;
use App\Models\User;
use App\Modules\Employee\Models\Employee;
use App\Modules\Kpi\Models\KpiAssignment;
use App\Modules\Kpi\Models\KpiEmployeeSummary;
use App\Modules\Kpi\Models\KpiPeriod;
use App\Modules\Kpi\Models\KpiScoringConfig;
use App\Modules\Kpi\Services\KpiService;
use Carbon\Carbon;
use DomainException;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Tests\TestCase;

class KpiModuleTest extends TestCase
{
    use RefreshDatabase;

    protected KpiService $kpiService;
    protected KpiPeriod $period;
    protected Employee $leader;
    protected Employee $employee;
    protected User $leaderUser;
    protected User $employeeUser;

    protected function setUp(): void
    {
        parent::setUp();

        $this->kpiService = app(KpiService::class);

        // 1. Tạo cấu hình điểm KPI chuẩn Vinamilk
        KpiScoringConfig::create(['config_key' => 'EARLY_BONUS_PER_DAY', 'config_value' => 2.00]);
        KpiScoringConfig::create(['config_key' => 'MAX_EARLY_BONUS', 'config_value' => 10.00]);
        KpiScoringConfig::create(['config_key' => 'LATE_PENALTY_PER_DAY', 'config_value' => 5.00]);
        KpiScoringConfig::create(['config_key' => 'BASELINE_SCORE', 'config_value' => 100.00]);
        KpiScoringConfig::create(['config_key' => 'MIN_TASK_SCORE', 'config_value' => 0.00]);
        KpiScoringConfig::create(['config_key' => 'MAX_TASK_SCORE', 'config_value' => 120.00]);
        KpiScoringConfig::create(['config_key' => 'REASON_REQUIRED_THRESHOLD', 'config_value' => 20.00]);

        // 2. Tạo kỳ đánh giá KPI Tháng 10/2026
        $this->period = KpiPeriod::create([
            'period_code' => 'KPI-2026-M10',
            'period_name' => 'Kỳ đánh giá KPI Tháng 10/2026',
            'period_type' => 'MONTH',
            'start_date'  => '2026-10-01',
            'end_date'    => '2026-10-31',
            'status'      => 'OPEN',
        ]);

        // 3. Tạo Lãnh đạo (Quản lý phân xưởng)
        $this->leader = Employee::create([
            'employee_code'     => 'VNM-000010',
            'full_name'         => 'Nguyễn Quản Đốc',
            'gender'            => 'MALE',
            'date_of_birth'     => '1980-01-01',
            'national_id'       => '079080000010',
            'phone_number'      => '0901000010',
            'hire_date'         => '2015-01-01',
            'employment_status' => 'ACTIVE',
        ]);

        $this->leaderUser = User::create([
            'name'        => 'Leader User',
            'email'       => 'leader@vinamilk.com.vn',
            'password'    => bcrypt('Secret123!'),
            'employee_id' => $this->leader->id,
            'is_active'   => true,
        ]);

        // 4. Tạo Nhân viên (Kỹ sư vận hành)
        $this->employee = Employee::create([
            'employee_code'     => 'VNM-000123',
            'full_name'         => 'Lê Kỹ Sư',
            'gender'            => 'MALE',
            'date_of_birth'     => '1990-05-15',
            'national_id'       => '079090000123',
            'phone_number'      => '0909123456',
            'hire_date'         => '2020-03-01',
            'employment_status' => 'ACTIVE',
        ]);

        $this->employeeUser = User::create([
            'name'        => 'Engineer User',
            'email'       => 'engineer@vinamilk.com.vn',
            'password'    => bcrypt('Secret123!'),
            'employee_id' => $this->employee->id,
            'is_active'   => true,
        ]);
    }

    /**
     * TEST CASE 1: Hoàn thành sớm 7 ngày -> Điểm SLA tính 7*2 = 14 nhưng BỊ CHẶN TỐI ĐA +10 -> Điểm chốt = 110.
     */
    public function test_kpi_task_completed_7_days_early_is_capped_at_plus_10(): void
    {
        // Giao việc: Hạn ngày 20/10/2026, trọng số 50%
        $task = $this->kpiService->createTask([
            'kpi_period_id'     => $this->period->id,
            'employee_id'       => $this->employee->id,
            'title'             => 'Nâng cao hiệu suất đóng hộp tiệt trùng',
            'target_metric'     => 'Tỷ lệ lỗi đóng bao bì < 0.05%',
            'weight_percentage' => 50.0,
            'due_date'          => '2026-10-20',
        ], $this->leaderUser);

        $this->assertEquals('Assigned', $task->status);

        // Nhân viên bắt đầu làm
        $this->kpiService->startTask($task->id, $this->employeeUser);

        // Nộp kết quả vào ngày 13/10/2026 (Sớm 7 ngày trước hạn 20/10/2026)
        $completedDate = Carbon::parse('2026-10-13');
        $submittedTask = $this->kpiService->submitTask(
            $task->id,
            'Đã tối ưu hóa cụm vòi phun nhiệt, tỷ lệ lỗi đạt 0.02%',
            $completedDate,
            $this->employeeUser
        );

        // Điểm thời gian: 7 ngày * 2đ = 14đ, nhưng clamp tối đa +10đ
        $this->assertEquals(10.0, $submittedTask->time_score);
        $this->assertEquals('PendingApproval', $submittedTask->status);

        // Lãnh đạo thẩm định và duyệt (không điều chỉnh thêm)
        $approvedTask = $this->kpiService->approveTask($task->id, $this->leaderUser, 0.0);

        // Điểm chốt = clamp(100 (baseline) + 10 (time) + 0, 0, 120) = 110
        $this->assertEquals(110.0, $approvedTask->final_score);
        $this->assertEquals('Approved', $approvedTask->status);
    }

    /**
     * TEST CASE 2: Hoàn thành trễ 30 ngày -> Phạt 30*5 = -150 -> Baseline 100 - 150 = -50 -> SÀN 0 ĐIỂM.
     */
    public function test_kpi_task_completed_30_days_late_hits_floor_zero(): void
    {
        // Giao việc: Hạn ngày 01/10/2026
        $task = $this->kpiService->createTask([
            'kpi_period_id'     => $this->period->id,
            'employee_id'       => $this->employee->id,
            'title'             => 'Bảo trì lớn buồng khử trùng UHT số 2',
            'target_metric'     => 'Hoàn thành chạy thử nghiệm tải 100%',
            'weight_percentage' => 40.0,
            'due_date'          => '2026-10-01',
        ], $this->leaderUser);

        // Nộp kết quả vào ngày 31/10/2026 (Trễ 30 ngày)
        $completedDate = Carbon::parse('2026-10-31');
        $submittedTask = $this->kpiService->submitTask(
            $task->id,
            'Hoàn tất thử tải trễ do chờ phụ tùng từ Đức',
            $completedDate,
            $this->employeeUser
        );

        // Phạt trễ: 30 ngày * 5đ/ngày = -150đ
        $this->assertEquals(-150.0, $submittedTask->time_score);

        // Lãnh đạo duyệt
        $approvedTask = $this->kpiService->approveTask($task->id, $this->leaderUser, 0.0);

        // Điểm: 100 - 150 = -50 -> clamp(0, 120) -> 0.0 điểm
        $this->assertEquals(0.0, $approvedTask->final_score);
        $this->assertEquals('Approved', $approvedTask->status);
    }

    /**
     * TEST CASE 3: Bị trả lại (Rejected) rồi nộp lại và được duyệt (Approved).
     */
    public function test_kpi_task_rejected_then_resubmitted_and_approved(): void
    {
        $task = $this->kpiService->createTask([
            'kpi_period_id'     => $this->period->id,
            'employee_id'       => $this->employee->id,
            'title'             => 'Quy chuẩn hóa tài liệu SOP an toàn điện',
            'target_metric'     => 'Ban hành 5 bộ SOP',
            'weight_percentage' => 30.0,
            'due_date'          => '2026-10-25',
        ], $this->leaderUser);

        // 1. Nhân viên nộp kết quả sơ sài
        $this->kpiService->submitTask(
            $task->id,
            'Đã viết xong bản nháp Word',
            Carbon::parse('2026-10-24'),
            $this->employeeUser
        );

        // 2. Lãnh đạo từ chối và trả lại
        $rejectedTask = $this->kpiService->rejectTask(
            $task->id,
            $this->leaderUser,
            'Bản nháp chưa có chữ ký rà soát của kỹ sư an toàn. Yêu cầu hoàn thiện lại.'
        );

        $this->assertEquals('Rejected', $rejectedTask->status);
        $this->assertEquals('Bản nháp chưa có chữ ký rà soát của kỹ sư an toàn. Yêu cầu hoàn thiện lại.', $rejectedTask->rejection_reason);

        // 3. Nhân viên hoàn thiện và nộp lại
        $resubmittedTask = $this->kpiService->submitTask(
            $task->id,
            'Đã bổ sung đầy đủ chữ ký rà soát và file PDF ban hành chính thức',
            Carbon::parse('2026-10-25'), // Đúng hạn ngày 25/10
            $this->employeeUser
        );

        $this->assertEquals('PendingApproval', $resubmittedTask->status);
        $this->assertEquals(0.0, $resubmittedTask->time_score); // Đúng hạn = 0đ

        // 4. Lãnh đạo thẩm định và duyệt
        $finalApproved = $this->kpiService->approveTask($task->id, $this->leaderUser, 0.0, null, 'Đã đạt chuẩn.');

        $this->assertEquals('Approved', $finalApproved->status);
        $this->assertEquals(100.0, $finalApproved->final_score);
    }

    /**
     * TEST CASE 4: Cuối kỳ chưa nộp -> Tự động chuyển NotCompleted với điểm = 0.
     */
    public function test_kpi_task_unsubmitted_at_end_of_period_auto_closes_with_zero_score(): void
    {
        $task = $this->kpiService->createTask([
            'kpi_period_id'     => $this->period->id,
            'employee_id'       => $this->employee->id,
            'title'             => 'Nghiên cứu công nghệ đóng gói tự hủy sinh học',
            'target_metric'     => 'Báo cáo khảo sát thực nghiệm',
            'weight_percentage' => 20.0,
            'due_date'          => '2026-10-15',
        ], $this->leaderUser);

        // Nhân viên chỉ bấm bắt đầu làm nhưng không nộp báo cáo
        $this->kpiService->startTask($task->id, $this->employeeUser);
        $this->assertEquals('InProgress', $task->fresh()->status);

        // Cuối kỳ hệ thống tự động quét và đóng các nhiệm vụ chưa nộp
        $closedCount = $this->kpiService->autoCloseUncompletedTasks($this->period->id);

        $this->assertEquals(1, $closedCount);

        $task->refresh();
        $this->assertEquals('NotCompleted', $task->status);
        $this->assertEquals(0.0, $task->final_score);

        // Tổng kết kỳ của nhân viên ghi nhận 0 điểm cho nhiệm vụ này
        $summary = KpiEmployeeSummary::where('kpi_period_id', $this->period->id)
            ->where('employee_id', $this->employee->id)
            ->first();

        $this->assertNotNull($summary);
        $this->assertEquals(0.0, $summary->kpi_score);
    }

    /**
     * TEST CASE 5: Điều chỉnh lãnh đạo vượt +/- 20 điểm bắt buộc phải nhập lý do.
     */
    public function test_manager_adjustment_exceeding_20_points_requires_reason(): void
    {
        $task = $this->kpiService->createTask([
            'kpi_period_id'     => $this->period->id,
            'employee_id'       => $this->employee->id,
            'title'             => 'Xử lý sự cố khẩn cấp dây chuyền sữa đặc',
            'target_metric'     => 'Phục hồi sự cố trong 2 giờ',
            'weight_percentage' => 20.0,
            'due_date'          => '2026-10-20',
        ], $this->leaderUser);

        $this->kpiService->submitTask($task->id, 'Đã sửa chữa và phục hồi sau 45 phút', Carbon::parse('2026-10-20'), $this->employeeUser);

        // Điều chỉnh +25 điểm nhưng KHÔNG nhập lý do -> Bị chặn
        $this->expectException(DomainException::class);
        $this->expectExceptionMessage("Điều chỉnh lãnh đạo vượt mức ±20 điểm bắt buộc phải nhập lý do thẩm định cụ thể.");

        $this->kpiService->approveTask($task->id, $this->leaderUser, 25.0, null);
    }

    /**
     * TEST CASE 6: Điều chỉnh hợp lệ vượt +25 điểm có lý do -> Được chấp thuận (clamp tối đa 120).
     */
    public function test_manager_adjustment_exceeding_20_with_reason_succeeds_and_clamps(): void
    {
        $task = $this->kpiService->createTask([
            'kpi_period_id'     => $this->period->id,
            'employee_id'       => $this->employee->id,
            'title'             => 'Xử lý sự cố xuất sắc',
            'target_metric'     => 'Phục hồi sự cố',
            'weight_percentage' => 20.0,
            'due_date'          => '2026-10-20',
        ], $this->leaderUser);

        $this->kpiService->submitTask($task->id, 'Đã xử lý xuất sắc', Carbon::parse('2026-10-20'), $this->employeeUser);

        // Điều chỉnh +25 điểm CÓ lý do cụ thể
        $approved = $this->kpiService->approveTask(
            $task->id,
            $this->leaderUser,
            25.0,
            'Kỹ sư có sáng kiến khắc phục sự cố vượt tiến độ, tiết kiệm cho nhà máy hơn 200 triệu đồng chi phí gián đoạn sản xuất.'
        );

        // 100 baseline + 0 time + 25 = 125 -> clamp(0, 120) = 120
        $this->assertEquals(120.0, $approved->final_score);
    }

    /**
     * TEST CASE 7: Tính điểm tổng kết kỳ = Σ(điểm nhiệm vụ × trọng số) và Xuất sang Payroll P3.
     */
    public function test_period_kpi_score_aggregation_and_payroll_export(): void
    {
        // Nhiệm vụ 1: 60% trọng số, hoàn thành sớm 7 ngày (110 điểm)
        $t1 = $this->kpiService->createTask([
            'kpi_period_id'     => $this->period->id,
            'employee_id'       => $this->employee->id,
            'title'             => 'Nhiệm vụ trọng điểm A',
            'target_metric'     => 'Chỉ tiêu A',
            'weight_percentage' => 60.0,
            'due_date'          => '2026-10-20',
        ], $this->leaderUser);
        $this->kpiService->submitTask($t1->id, 'Kết quả A', Carbon::parse('2026-10-13'), $this->employeeUser);
        $this->kpiService->approveTask($t1->id, $this->leaderUser, 0.0); // final_score = 110

        // Nhiệm vụ 2: 40% trọng số, hoàn thành đúng hạn (100 điểm)
        $t2 = $this->kpiService->createTask([
            'kpi_period_id'     => $this->period->id,
            'employee_id'       => $this->employee->id,
            'title'             => 'Nhiệm vụ trọng điểm B',
            'target_metric'     => 'Chỉ tiêu B',
            'weight_percentage' => 40.0,
            'due_date'          => '2026-10-25',
        ], $this->leaderUser);
        $this->kpiService->submitTask($t2->id, 'Kết quả B', Carbon::parse('2026-10-25'), $this->employeeUser);
        $this->kpiService->approveTask($t2->id, $this->leaderUser, 0.0); // final_score = 100

        // Kiểm tra điểm tổng kết kỳ: 110 * 0.6 + 100 * 0.4 = 66 + 40 = 106 điểm
        $summary = KpiEmployeeSummary::where('kpi_period_id', $this->period->id)
            ->where('employee_id', $this->employee->id)
            ->first();

        $this->assertEquals(100.0, $summary->total_weight_percentage);
        $this->assertEquals(106.0, $summary->kpi_score);
        $this->assertEquals('A', $summary->kpi_grade);
        $this->assertEquals(1.15, $summary->p3_multiplier); // Loại A: Hệ số P3 = 1.15 (115%)

        // Xuất sang Payroll P3
        $exportResult = $this->kpiService->exportPeriodKpiToPayroll($this->period->id, $this->leaderUser);

        $this->assertEquals('KPI-2026-M10', $exportResult['period_code']);
        $this->assertEquals(1, $exportResult['exported_count']);

        $summary->refresh();
        $this->assertEquals('EXPORTED_TO_PAYROLL', $summary->status);
        $this->assertNotNull($summary->exported_at);
    }
}
