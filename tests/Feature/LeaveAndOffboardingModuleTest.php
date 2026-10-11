<?php

namespace Tests\Feature;

use App\Models\AuditLog;
use App\Models\Notification;
use App\Models\User;
use App\Modules\Attendance\Models\AttendanceDay;
use App\Modules\Employee\Models\Employee;
use App\Modules\Leave\Models\AssetHandoverItem;
use App\Modules\Leave\Models\FinalSettlement;
use App\Modules\Leave\Models\LeaveBalance;
use App\Modules\Leave\Models\LeaveRequest;
use App\Modules\Leave\Models\LeaveType;
use App\Modules\Leave\Models\ResignationRequest;
use App\Modules\Leave\Services\LeaveService;
use App\Modules\Leave\Services\OffboardingService;
use App\Modules\Organization\Models\EmployeeAssignment;
use App\Modules\Organization\Models\OrgUnit;
use App\Modules\Payroll\Models\Contract;
use Carbon\Carbon;
use DomainException;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Illuminate\Support\Facades\DB;
use Tests\TestCase;

class LeaveAndOffboardingModuleTest extends TestCase
{
    use RefreshDatabase;

    protected LeaveService $leaveService;
    protected OffboardingService $offboardingService;

    protected OrgUnit $hq;
    protected OrgUnit $factory;
    protected Employee $manager;
    protected Employee $employee;
    protected User $managerUser;
    protected User $employeeUser;

    protected LeaveType $annualLeaveType;
    protected LeaveType $unpaidLeaveType;
    protected LeaveType $sickSiLeaveType;

    protected function setUp(): void
    {
        parent::setUp();

        $this->leaveService = app(LeaveService::class);
        $this->offboardingService = app(OffboardingService::class);

        // 1. Cây tổ chức: HQ -> Factory Bình Dương
        $this->hq = OrgUnit::create([
            'unit_code'      => 'HQ',
            'unit_name'      => 'Trụ sở Tập đoàn Vinamilk',
            'unit_type'      => 'HEADQUARTERS',
            'path'           => '/1/',
            'level'          => 1,
            'effective_from' => '2000-01-01',
        ]);

        $this->factory = OrgUnit::create([
            'unit_code'      => 'FACTORY_BD',
            'unit_name'      => 'Nhà máy Sữa Thống Nhất',
            'unit_type'      => 'FACTORY',
            'parent_id'      => $this->hq->id,
            'path'           => '/1/5/',
            'level'          => 2,
            'effective_from' => '2000-01-01',
        ]);

        // 2. Quản lý trực tiếp (Giám đốc Nhà máy)
        $this->manager = Employee::create([
            'employee_code'     => 'VNM-000001',
            'full_name'         => 'Trần Quản Lý',
            'gender'            => 'MALE',
            'date_of_birth'     => '1980-01-01',
            'national_id'       => '079080000001',
            'phone_number'      => '0901000001',
            'hire_date'         => '2010-01-01',
            'employment_status' => 'ACTIVE',
        ]);

        $this->managerUser = User::create([
            'name'        => 'Manager User',
            'email'       => 'manager@vinamilk.com.vn',
            'password'    => bcrypt('Secret123!'),
            'employee_id' => $this->manager->id,
            'is_active'   => true,
        ]);

        // Gán quản lý cho Nhà máy
        $this->factory->update(['manager_id' => $this->manager->id]);

        EmployeeAssignment::create([
            'employee_id'    => $this->manager->id,
            'org_unit_id'    => $this->factory->id,
            'position_id'    => 1,
            'is_primary'     => true,
            'is_current'     => true,
            'effective_from' => '2010-01-01',
        ]);

        // 3. Nhân viên Kỹ sư (công tác tại Nhà máy, vào làm từ 2007)
        $this->employee = Employee::create([
            'employee_code'     => 'VNM-000123',
            'full_name'         => 'Nguyễn Văn A',
            'gender'            => 'MALE',
            'date_of_birth'     => '1985-05-15',
            'national_id'       => '079085000123',
            'phone_number'      => '0909123456',
            'hire_date'         => '2007-03-01', // Thâm niên 19 năm tính đến 2026
            'employment_status' => 'ACTIVE',
        ]);

        $this->employeeUser = User::create([
            'name'        => 'Nguyen Van A',
            'email'       => 'nguyenvana@vinamilk.com.vn',
            'password'    => bcrypt('Secret123!'),
            'employee_id' => $this->employee->id,
            'is_active'   => true,
        ]);

        EmployeeAssignment::create([
            'employee_id'    => $this->employee->id,
            'org_unit_id'    => $this->factory->id,
            'position_id'    => 2,
            'is_primary'     => true,
            'is_current'     => true,
            'effective_from' => '2007-03-01',
        ]);

        // 4. Hợp đồng lao động không xác định thời hạn
        Contract::create([
            'contract_code'    => 'HDLD-2007/VNM-123',
            'employee_id'      => $this->employee->id,
            'contract_type'    => 'INDEFINITE_TERM',
            'contract_status'  => 'ACTIVE',
            'signed_date'      => '2007-03-01',
            'start_date'       => '2007-03-01',
            'end_date'         => null,
            'base_salary'      => 22000000, // 22 triệu VND / tháng
            'working_location' => 'Nhà máy Sữa Thống Nhất',
            'effective_from'   => '2007-03-01',
            'is_current'       => true,
        ]);

        // 5. Lấy danh mục loại nghỉ phép
        $this->annualLeaveType = LeaveType::where('type_code', 'ANNUAL')->first();
        $this->unpaidLeaveType = LeaveType::where('type_code', 'UNPAID')->first();
        $this->sickSiLeaveType = LeaveType::where('type_code', 'SICK_SI')->first();
    }

    /**
     * TEST CASE 1: Khởi tạo quỹ phép năm tính đúng thâm niên (Điều 113, 114 BLLĐ 2019).
     * Nhân viên vào làm từ 01/03/2007, tính đến năm 2026 là 19 năm làm việc.
     * Cứ đủ 5 năm được thêm 1 ngày thâm niên -> floor(19 / 5) = 3 ngày.
     * Tổng quỹ phép = 12 (tiêu chuẩn) + 3 (thâm niên) = 15 ngày.
     */
    public function test_leave_balance_calculation_with_seniority_accrual(): void
    {
        $balance = $this->leaveService->initializeLeaveBalance($this->employee, 2026);

        $this->assertEquals(12.0, $balance->annual_leave_quota);
        $this->assertEquals(3.0, $balance->seniority_leave_days);
        $this->assertEquals(15.0, $balance->available_days);
    }

    /**
     * TEST CASE 2: Luồng ESS nộp đơn nghỉ phép -> Tìm quản lý trực tiếp theo cây tổ chức -> Duyệt đơn SoD.
     */
    public function test_leave_request_submission_and_direct_manager_approval(): void
    {
        // 1. Khởi tạo quỹ phép
        $this->leaveService->initializeLeaveBalance($this->employee, 2026);

        // 2. ESS Nộp đơn nghỉ phép năm 2 ngày
        $req = $this->leaveService->submitLeaveRequest($this->employee->id, [
            'leave_type_id' => $this->annualLeaveType->id,
            'start_date'    => '2026-04-13',
            'end_date'      => '2026-04-14',
            'leave_days'    => 2.0,
            'reason'        => 'Nghỉ giải quyết việc gia đình',
        ]);

        $this->assertEquals('PENDING', $req->status);
        $this->assertStringStartsWith('LR-', $req->request_code);

        // 3. Kiểm tra thông báo gửi đúng Quản lý trực tiếp trên cây tổ chức
        $this->assertDatabaseHas('notifications', [
            'user_id' => $this->managerUser->id,
            'type'    => 'APPROVAL_REQUEST',
        ]);

        // 4. KIỂM TRA SOD: Nhân viên KHÔNG được tự duyệt đơn của mình
        $this->expectException(DomainException::class);
        $this->leaveService->approveLeaveRequest($req->id, $this->employeeUser);
    }

    /**
     * TEST CASE 3: Quản lý trực tiếp duyệt đơn -> Cập nhật used_days và attendance_days.
     */
    public function test_manager_approves_leave_request_updates_balance_and_attendance(): void
    {
        $this->leaveService->initializeLeaveBalance($this->employee, 2026);

        $req = $this->leaveService->submitLeaveRequest($this->employee->id, [
            'leave_type_id' => $this->annualLeaveType->id,
            'start_date'    => '2026-04-13',
            'end_date'      => '2026-04-14',
            'leave_days'    => 2.0,
            'reason'        => 'Nghỉ việc gia đình',
        ]);

        // Quản lý phê duyệt
        $approvedReq = $this->leaveService->approveLeaveRequest($req->id, $this->managerUser);

        $this->assertEquals('APPROVED', $approvedReq->status);
        $this->assertEquals($this->manager->id, $approvedReq->approved_by);

        // Quỹ phép đã bị trừ 2 ngày used_days
        $balance = LeaveBalance::where('employee_id', $this->employee->id)->where('year', 2026)->first();
        $this->assertEquals(2.0, $balance->used_days);
        $this->assertEquals(13.0, $balance->available_days);

        // Dữ liệu bảng công attendance_days được cập nhật thành LEAVE_PAID với 8 giờ công
        $this->assertDatabaseHas('attendance_days', [
            'employee_id'  => $this->employee->id,
            'work_date'    => '2026-04-13',
            'status'       => 'LEAVE_PAID',
            'actual_hours' => 8.0,
        ]);
        $this->assertDatabaseHas('attendance_days', [
            'employee_id'  => $this->employee->id,
            'work_date'    => '2026-04-14',
            'status'       => 'LEAVE_PAID',
            'actual_hours' => 8.0,
        ]);
    }

    /**
     * TEST CASE 4: Nghỉ không phép (AWOL) cộng dồn >= 5 ngày trong 30 ngày kích hoạt Cảnh báo Sa thải (Điều 125 Khoản 4 BLLĐ 2019).
     */
    public function test_unauthorized_absence_accumulated_5_days_triggers_dismissal_alert(): void
    {
        // Ghi nhận 4 ngày bỏ việc rải rác
        $this->leaveService->recordUnauthorizedAbsence($this->employee->id, Carbon::parse('2026-04-01'));
        $this->leaveService->recordUnauthorizedAbsence($this->employee->id, Carbon::parse('2026-04-03'));
        $this->leaveService->recordUnauthorizedAbsence($this->employee->id, Carbon::parse('2026-04-07'));
        $this->leaveService->recordUnauthorizedAbsence($this->employee->id, Carbon::parse('2026-04-10'));

        // Chưa đủ 5 ngày -> chưa có cảnh báo kỷ luật sa thải
        $this->assertEquals(0, Notification::where('title', 'like', '%CẢNH BÁO KỶ LUẬT SA THẢI%')->count());

        // Ngày thứ 5 tự ý bỏ việc trong vòng 30 ngày (ngày 15/04/2026)
        $this->leaveService->recordUnauthorizedAbsence($this->employee->id, Carbon::parse('2026-04-15'));

        // Hệ thống tự động kích hoạt cảnh báo sa thải gửi Quản lý và ghi AuditLog
        $this->assertDatabaseHas('notifications', [
            'user_id' => $this->managerUser->id,
            'type'    => 'SYSTEM_ALERT',
        ]);

        $this->assertDatabaseHas('audit_logs', [
            'table_name' => 'attendance_days',
            'record_id'  => $this->employee->id,
        ]);
    }

    /**
     * TEST CASE 5: Tự động tính ngày báo trước thôi việc theo loại hợp đồng (Điều 35 BLLĐ 2019).
     */
    public function test_statutory_notice_days_calculation_by_contract_type(): void
    {
        // 1. HĐ không xác định thời hạn -> 45 ngày
        $noticeDays = $this->offboardingService->calculateStatutoryNoticeDays($this->employee);
        $this->assertEquals(45, $noticeDays);

        // 2. Thử nghiệm với HĐ xác định thời hạn 2 năm -> 30 ngày
        $emp2 = Employee::create([
            'employee_code'     => 'VNM-000222',
            'full_name'         => 'Trần Thị B',
            'gender'            => 'FEMALE',
            'date_of_birth'     => '1995-01-01',
            'national_id'       => '079095000222',
            'phone_number'      => '0902222222',
            'hire_date'         => '2024-01-01',
            'employment_status' => 'ACTIVE',
        ]);

        Contract::create([
            'contract_code'   => 'HDLD-2024/VNM-222',
            'employee_id'     => $emp2->id,
            'contract_type'   => 'DEFINITE_TERM_2Y',
            'contract_status' => 'ACTIVE',
            'signed_date'     => '2024-01-01',
            'start_date'      => '2024-01-01',
            'end_date'        => '2026-01-01',
            'base_salary'     => 15000000,
            'working_location'=> 'HCM',
            'effective_from'  => '2024-01-01',
            'is_current'      => true,
        ]);

        $noticeDays2 = $this->offboardingService->calculateStatutoryNoticeDays($emp2);
        $this->assertEquals(30, $noticeDays2);
    }

    /**
     * TEST CASE 6: Nộp đơn thôi việc -> Sinh checklist bàn giao tài sản -> Luồng duyệt đa cấp -> Khóa tài khoản sau ngày cuối.
     */
    public function test_resignation_workflow_asset_handover_and_account_deactivation(): void
    {
        // 1. Nộp đơn xin nghỉ việc (báo trước 45 ngày: nộp 01/03/2026, ngày cuối 20/04/2026 = 50 ngày > 45 ngày)
        $resignation = $this->offboardingService->submitResignationRequest($this->employee->id, [
            'submitted_date'     => '2026-03-01',
            'proposed_last_date' => '2026-04-20',
            'reason'             => 'Chuyển định cư cùng gia đình',
        ]);

        $this->assertEquals('SUBMITTED', $resignation->status);
        $this->assertEquals(45, $resignation->required_notice_days);

        // Checklist 7 hạng mục bàn giao đã được tự động tạo
        $this->assertEquals(7, AssetHandoverItem::where('resignation_request_id', $resignation->id)->count());

        // 2. Quản lý duyệt (Cấp 1)
        $resignation = $this->offboardingService->approveByManager($resignation->id, $this->managerUser);
        $this->assertEquals('MANAGER_APPROVED', $resignation->status);

        // 3. HR duyệt (Cấp 2)
        $hrUser = User::create([
            'name'     => 'HR Admin',
            'email'    => 'hr@vinamilk.com.vn',
            'password' => bcrypt('Secret123!'),
        ]);
        $resignation = $this->offboardingService->approveByHr($resignation->id, $hrUser);
        $this->assertEquals('HR_APPROVED', $resignation->status);

        // 4. Ban Giám đốc phê duyệt Quyết định thôi việc (Cấp 3)
        $bodUser = User::create([
            'name'     => 'CEO',
            'email'    => 'ceo@vinamilk.com.vn',
            'password' => bcrypt('Secret123!'),
        ]);
        $resignation = $this->offboardingService->approveByBod($resignation->id, $bodUser, Carbon::parse('2026-04-30'));
        $this->assertEquals('BOD_APPROVED', $resignation->status);
        $this->assertEquals('2026-04-30', $resignation->approved_last_date->toDateString());

        // 5. Kiểm tra tự động vô hiệu hóa tài khoản sau ngày làm việc cuối
        // Giả lập hệ thống chạy vào ngày 01/05/2026 (sau ngày làm việc cuối 30/04/2026)
        Carbon::setTestNow(Carbon::parse('2026-05-01 01:00:00'));

        $deactivatedCount = $this->offboardingService->deactivateResignedAccounts();
        $this->assertEquals(1, $deactivatedCount);

        $this->employee->refresh();
        $this->employeeUser->refresh();

        $this->assertEquals('RESIGNED', $this->employee->employment_status);
        $this->assertEquals('2026-04-30', $this->employee->termination_date->toDateString());
        $this->assertFalse((bool)$this->employeeUser->is_active);

        Carbon::setTestNow(); // Reset thời gian test
    }

    /**
     * TEST CASE 7: Tính toán kỳ lương quyết toán thôi việc (Final Settlement) bằng brick/math.
     * Số liệu cụ thể:
     * - Nhân viên Nguyễn Văn A, lương HĐLĐ: 22.000.000 VND / tháng.
     * - Ngày vào làm việc: 01/03/2007. Ngày làm việc cuối: 30/04/2026.
     * - Công chuẩn: 22 ngày. Ngày làm việc thực tế tháng 4: 22 ngày.
     * - Phép năm còn tồn: 6.5 ngày.
     * - Trợ cấp thôi việc: Thời gian từ 01/03/2007 đến 31/12/2008 = 1 năm 10 tháng -> làm tròn 2 năm.
     *   Mức hưởng = 2 năm * 0.5 * 22.000.000 = 22.000.000 VND.
     * - Tạm ứng: 5.000.000 VND.
     * - Hạn thanh toán: 14 ngày làm việc kể từ 30/04/2026 (loại trừ T7, CN).
     */
    public function test_final_settlement_detailed_numerical_calculation(): void
    {
        // Tạo đơn thôi việc đã được duyệt ngày cuối 30/04/2026
        $resignation = ResignationRequest::create([
            'resignation_code'     => 'RES-202604-00123',
            'employee_id'          => $this->employee->id,
            'submitted_date'       => '2026-03-01',
            'required_notice_days' => 45,
            'proposed_last_date'   => '2026-04-30',
            'approved_last_date'   => '2026-04-30',
            'reason'               => 'Nghỉ việc cá nhân',
            'status'               => 'BOD_APPROVED',
        ]);

        // Cấp quỹ phép tồn 6.5 ngày cho năm 2026
        LeaveBalance::create([
            'employee_id'          => $this->employee->id,
            'year'                 => 2026,
            'annual_leave_quota'   => 12.0,
            'seniority_leave_days' => 3.0,
            'transferred_days'     => 0.0,
            'used_days'            => 8.5, // Còn lại: 15 - 8.5 = 6.5 ngày
        ]);

        $settlement = $this->offboardingService->calculateFinalSettlement($resignation->id, [
            'actual_working_days'   => 22.0,
            'standard_working_days' => 22.0,
            'advance_deduction'     => 5000000,
            'other_deductions'      => 0,
        ]);

        // 1. Lương ngày công tháng cuối: (22.000.000 / 22) * 22 = 22.000.000 VND
        $this->assertEquals('22000000', $settlement->actual_work_salary);

        // 2. Tiền phép năm chưa nghỉ: 1.000.000 VND/ngày * 6.5 ngày = 6.500.000 VND
        $this->assertEquals(6.5, $settlement->remaining_leave_days);
        $this->assertEquals('6500000', $settlement->unused_leave_allowance);

        // 3. Trợ cấp thôi việc Điều 46: 1 năm 10 tháng làm tròn thành 2.0 năm * (0.5 * 22.000.000) = 22.000.000 VND
        $this->assertEquals(2.0, $settlement->severance_qualifying_years);
        $this->assertEquals('22000000', $settlement->severance_allowance);

        // 4. Trừ tạm ứng: 5.000.000 VND
        $this->assertEquals('5000000', $settlement->advance_deduction);

        // 5. Thực lĩnh thanh toán: 22.000.000 + 6.500.000 + 22.000.000 - 5.000.000 = 45.500.000 VND
        $this->assertEquals('45500000', $settlement->net_settlement_amount);

        // 6. Hạn thanh toán 14 ngày làm việc kể từ 30/04/2026:
        // Ngày 30/04/2026 là Thứ Năm. Thêm 14 ngày làm việc (không tính T7, CN) -> ngày 20/05/2026
        $this->assertEquals('2026-05-20', $settlement->payment_deadline_date->toDateString());
    }
}
