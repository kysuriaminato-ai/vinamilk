<?php

namespace Tests\Feature;

use App\Models\AuditLog;
use App\Models\Notification;
use App\Models\User;
use App\Modules\Employee\Models\Employee;
use App\Modules\Organization\Models\EmployeeAssignment;
use App\Modules\Organization\Models\OrgUnit;
use App\Modules\Organization\Models\Position;
use App\Modules\Payroll\Models\ApprovalRule;
use App\Modules\Payroll\Models\ApprovalStep;
use App\Modules\Payroll\Models\EmployeeSalary;
use App\Modules\Payroll\Models\PayrollPeriod;
use App\Modules\Payroll\Models\RegionalMinWage;
use App\Modules\Payroll\Models\RetroactiveSalaryAdjustment;
use App\Modules\Payroll\Models\SalaryBand;
use App\Modules\Payroll\Models\SalaryProposal;
use App\Modules\Payroll\Models\UnitSalaryBudget;
use App\Modules\Payroll\Services\SalaryProposalService;
use Carbon\Carbon;
use DomainException;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Tests\TestCase;

class SalaryProposalModuleTest extends TestCase
{
    use RefreshDatabase;

    protected SalaryProposalService $proposalService;

    protected OrgUnit $hq;
    protected OrgUnit $factory;
    protected Position $engPosition;
    protected Position $dirPosition;
    protected SalaryBand $engBand;
    protected Employee $director;
    protected Employee $engineer;
    protected User $directorUser;
    protected User $engineerUser;

    protected function setUp(): void
    {
        parent::setUp();

        $this->proposalService = app(SalaryProposalService::class);

        // 1. Cây tổ chức
        $this->hq = OrgUnit::create([
            'unit_code' => 'HQ', 'unit_name' => 'Tập đoàn Vinamilk',
            'unit_type' => 'HEADQUARTERS', 'path' => '/1/', 'level' => 1, 'effective_from' => '2000-01-01',
        ]);
        $this->factory = OrgUnit::create([
            'unit_code' => 'FACTORY_BD', 'unit_name' => 'Nhà máy Sữa Thống Nhất',
            'unit_type' => 'FACTORY', 'parent_id' => $this->hq->id, 'path' => '/1/5/', 'level' => 2, 'effective_from' => '2000-01-01',
        ]);

        // 2. Chức danh & Khung lương
        $this->dirPosition = Position::create(['position_code' => 'DIR_FACTORY', 'position_name' => 'Giám đốc Nhà máy', 'level' => 4]);
        $this->engPosition = Position::create(['position_code' => 'ENG_SENIOR', 'position_name' => 'Kỹ sư Vận hành', 'level' => 2]);

        $this->engBand = SalaryBand::create([
            'band_code' => 'BAND-ENG-2', 'band_name' => 'Khung Kỹ sư', 'job_level' => 2,
            'min_salary' => 15000000, 'midpoint_salary' => 22000000, 'max_salary' => 30000000,
        ]);

        // 3. Lương tối thiểu vùng & Ngân sách
        RegionalMinWage::create([
            'region_zone' => 'REGION_1', 'min_wage_amount' => 4960000,
            'effective_from' => '2024-07-01', 'legal_reference' => 'NĐ 74/2024',
        ]);

        UnitSalaryBudget::create([
            'org_unit_id' => $this->factory->id, 'year' => 2026,
            'annual_budget' => 500000000, 'used_amount' => 50000000,
        ]);

        // 4. Cấu hình Luồng duyệt động (approval_rules)
        ApprovalRule::create(['rule_code' => 'DEF_1', 'rule_name' => 'Giám đốc Khối', 'workflow_type' => 'DEFAULT', 'step_order' => 1, 'approver_role' => 'DIVISION_DIRECTOR']);
        ApprovalRule::create(['rule_code' => 'DEF_2', 'rule_name' => 'Giám đốc Nhân sự', 'workflow_type' => 'DEFAULT', 'step_order' => 2, 'approver_role' => 'HR_DIRECTOR']);

        ApprovalRule::create(['rule_code' => 'EXC_1', 'rule_name' => 'Giám đốc Khối', 'workflow_type' => 'OUT_OF_BAND_OR_EXCEPTION', 'step_order' => 1, 'approver_role' => 'DIVISION_DIRECTOR']);
        ApprovalRule::create(['rule_code' => 'EXC_2', 'rule_name' => 'Ban C&B', 'workflow_type' => 'OUT_OF_BAND_OR_EXCEPTION', 'step_order' => 2, 'approver_role' => 'CB_COMMITTEE']);
        ApprovalRule::create(['rule_code' => 'EXC_3', 'rule_name' => 'Giám đốc Nhân sự', 'workflow_type' => 'OUT_OF_BAND_OR_EXCEPTION', 'step_order' => 3, 'approver_role' => 'HR_DIRECTOR']);
        ApprovalRule::create(['rule_code' => 'EXC_4', 'rule_name' => 'Tổng Giám đốc', 'workflow_type' => 'OUT_OF_BAND_OR_EXCEPTION', 'step_order' => 4, 'approver_role' => 'CEO']);

        // 5. Nhân sự Giám đốc
        $this->director = Employee::create([
            'employee_code' => 'VNM-DIR-01', 'full_name' => 'Nguyễn Giám Đốc',
            'gender' => 'MALE', 'date_of_birth' => '1975-01-01', 'national_id' => '079075000001',
            'phone_number' => '0901111111', 'hire_date' => '2010-01-01', 'employment_status' => 'ACTIVE',
        ]);
        $this->directorUser = User::create([
            'name' => 'Director', 'email' => 'director@vinamilk.com.vn', 'password' => bcrypt('Secret123!'),
            'employee_id' => $this->director->id, 'is_active' => true,
        ]);
        EmployeeAssignment::create([
            'employee_id' => $this->director->id, 'org_unit_id' => $this->factory->id, 'position_id' => $this->dirPosition->id,
            'is_primary' => true, 'is_current' => true, 'effective_from' => '2010-01-01',
        ]);

        // 6. Nhân sự Kỹ sư
        $this->engineer = Employee::create([
            'employee_code' => 'VNM-ENG-88', 'full_name' => 'Trần Kỹ Sư',
            'gender' => 'MALE', 'date_of_birth' => '1992-06-15', 'national_id' => '079092000088',
            'phone_number' => '0902222288', 'hire_date' => '2021-01-01', 'employment_status' => 'ACTIVE',
        ]);
        $this->engineerUser = User::create([
            'name' => 'Engineer', 'email' => 'engineer@vinamilk.com.vn', 'password' => bcrypt('Secret123!'),
            'employee_id' => $this->engineer->id, 'is_active' => true,
        ]);
        EmployeeAssignment::create([
            'employee_id' => $this->engineer->id, 'org_unit_id' => $this->factory->id, 'position_id' => $this->engPosition->id,
            'is_primary' => true, 'is_current' => true, 'effective_from' => '2021-01-01',
        ]);

        // Biểu lương ban đầu của Kỹ sư: 20 triệu VND
        EmployeeSalary::create([
            'employee_id'        => $this->engineer->id,
            'p1_position_salary' => 20000000,
            'insurance_salary'   => 20000000,
            'effective_from'     => '2025-01-01',
            'is_current'         => true,
        ]);
    }

    /**
     * TEST CASE 1: "Giám đốc tự đề xuất rồi tự duyệt" -> PHẢI BỊ CHẶN (Nguyên tắc SoD).
     */
    public function test_director_self_proposal_and_self_approval_is_strictly_blocked(): void
    {
        // Kịch bản A: Giám đốc cố tình tự tạo đề xuất tăng lương cho chính mình -> BỊ CHẶN
        $this->expectException(DomainException::class);
        $this->expectExceptionMessage("Nguyên tắc kiểm soát nội bộ (SoD): Người dùng không được tự lập đề xuất tăng lương cho chính mình.");

        $this->proposalService->createSalaryProposal([
            'employee_id'               => $this->director->id, // Tự đề xuất cho mình
            'proposal_type'             => 'PERIODIC',
            'proposed_p1'               => 60000000,
            'proposed_insurance_salary' => 60000000,
            'effective_from'            => '2026-11-01',
            'reason'                    => 'Tăng lương quản lý',
        ], $this->directorUser);
    }

    /**
     * TEST CASE 2: Một người khác lập đề xuất cho Giám đốc, nhưng Giám đốc cố tình tự duyệt -> BỊ CHẶN (SoD).
     */
    public function test_director_cannot_self_approve_proposal(): void
    {
        // Ban Nhân sự lập đề xuất tăng lương cho Giám đốc
        $hrUser = User::create([
            'name' => 'HR Staff', 'email' => 'hrstaff@vinamilk.com.vn', 'password' => bcrypt('Secret123!'),
            'employee_id' => 9999,
        ]);

        $proposal = $this->proposalService->createSalaryProposal([
            'employee_id'               => $this->director->id,
            'proposal_type'             => 'PERIODIC',
            'proposed_p1'               => 65000000,
            'proposed_insurance_salary' => 65000000,
            'effective_from'            => '2026-11-01',
            'reason'                    => 'Đánh giá định kỳ năm 2026',
        ], $hrUser);

        $this->assertEquals('SUBMITTED', $proposal->status);

        // Giám đốc cố tình đăng nhập vào và bấm phê duyệt đề xuất của chính mình -> BỊ CHẶN
        $this->expectException(DomainException::class);
        $this->expectExceptionMessage("Nguyên tắc kiểm soát nội bộ (SoD): Bạn không thể tự phê duyệt đề xuất tăng lương của chính mình.");

        $this->proposalService->approveStep($proposal->id, $this->directorUser, "Tôi đồng ý duyệt");
    }

    /**
     * TEST CASE 3: TRUY LĨNH 2 THÁNG (Retroactive Arrears):
     * - Quyết định duyệt ngày hiệu lực lùi về 01/08/2026 (Tháng 8 & Tháng 9 đã chốt lương).
     * - Mức lương tăng từ 20 triệu -> 25 triệu (Chênh lệch +5 triệu/tháng).
     * - Hệ thống tự động tính chính xác khoản truy lĩnh 2 tháng = 10 triệu VND cộng vào kỳ lương tiếp theo.
     */
    public function test_retroactive_salary_adjustment_for_2_months(): void
    {
        // 1. Tạo 2 kỳ lương Tháng 8/2026 và Tháng 9/2026 đã chốt (PAID)
        $pAug = PayrollPeriod::create([
            'period_code' => 'PAY-2026-08', 'month' => 8, 'year' => 2026, 'status' => 'PAID',
        ]);
        $pSep = PayrollPeriod::create([
            'period_code' => 'PAY-2026-09', 'month' => 9, 'year' => 2026, 'status' => 'PAID',
        ]);

        // Tạo kỳ lương Tháng 10/2026 đang mở (DRAFT) để nhận tiền truy lĩnh
        $pOct = PayrollPeriod::create([
            'period_code' => 'PAY-2026-10', 'month' => 10, 'year' => 2026, 'status' => 'DRAFT',
        ]);

        // 2. Giám đốc lập đề xuất tăng lương cho Kỹ sư: từ 20 triệu lên 25 triệu, hiệu lực từ ngày 01/08/2026
        $proposal = $this->proposalService->createSalaryProposal([
            'employee_id'               => $this->engineer->id,
            'proposal_type'             => 'PERIODIC',
            'proposed_p1'               => 25000000, // Tăng 5 triệu/tháng
            'proposed_insurance_salary' => 25000000,
            'effective_from'            => '2026-08-01', // Hiệu lực hồi tố về 2 tháng trước
            'reason'                    => 'Bổ nhiệm phụ trách kíp vận hành mới',
        ], $this->directorUser);

        // Luồng mặc định 2 cấp: Bước 1 (Giám đốc Khối) -> Bước 2 (Giám đốc Nhân sự)
        $this->assertEquals(2, $proposal->steps->count());

        // Bước 1: Giám đốc duyệt
        $proposal = $this->proposalService->approveStep($proposal->id, $this->directorUser, "Đồng ý đề xuất");
        $this->assertEquals('PENDING_STEP', $proposal->status);
        $this->assertEquals(2, $proposal->current_step_number);

        // Bước 2: Giám đốc Nhân sự duyệt (Cấp cuối cùng -> Kích hoạt Transaction nguyên tử)
        $hrDirectorUser = User::create([
            'name' => 'HR Director', 'email' => 'hrdir@vinamilk.com.vn', 'password' => bcrypt('Secret123!'),
            'employee_id' => 7777,
        ]);
        $finalApproved = $this->proposalService->approveStep($proposal->id, $hrDirectorUser, "Đồng ý duyệt cấp cuối");

        // 3. KIỂM TRA KẾT QUẢ QUYẾT ĐỊNH & TRUY LĨNH
        $this->assertEquals('APPROVED', $finalApproved->status);
        $this->assertNotNull($finalApproved->decision_number);
        $this->assertStringStartsWith('QD-TL/2026/VNM-', $finalApproved->decision_number);

        // Hệ thống phát hiện đúng 2 tháng hồi tố và tổng số tiền truy lĩnh 10 triệu VND
        $this->assertEquals(2, $finalApproved->retroactive_months);
        $this->assertEquals('10000000', $finalApproved->retroactive_amount);

        // Kiểm tra chi tiết 2 bản ghi truy lĩnh được tạo vào kỳ Tháng 10
        $adjustments = RetroactiveSalaryAdjustment::where('salary_proposal_id', $finalApproved->id)->get();
        $this->assertEquals(2, $adjustments->count());
        $this->assertEquals('5000000', $adjustments->first()->retroactive_difference);
        $this->assertEquals($pOct->id, $adjustments->first()->payroll_period_id);

        // Kiểm tra bản ghi lương mới được mở và bản ghi cũ được đóng đúng ngày 31/07/2026
        $oldSalary = EmployeeSalary::where('employee_id', $this->engineer->id)->where('is_current', false)->first();
        $this->assertEquals('2026-07-31', $oldSalary->effective_to->toDateString());

        $newSalary = EmployeeSalary::where('employee_id', $this->engineer->id)->where('is_current', true)->first();
        $this->assertEquals('2026-08-01', $newSalary->effective_from->toDateString());
        $this->assertEquals('25000000', $newSalary->p1_position_salary);
        $this->assertEquals($finalApproved->decision_number, $newSalary->decision_number);
    }

    /**
     * TEST CASE 4: ĐỀ XUẤT ĐẶC CÁCH VƯỢT KHUNG -> TỰ ĐỘNG SINH LUỒNG DUYỆT 4 CẤP (Khối -> C&B -> HR -> CEO).
     */
    public function test_special_exception_out_of_band_generates_4_tier_approval_workflow(): void
    {
        // Khung lương max = 30 triệu. Đề xuất 35 triệu (Vượt khung 5 triệu)
        $proposal = $this->proposalService->createSalaryProposal([
            'employee_id'               => $this->engineer->id,
            'proposal_type'             => 'SPECIAL_EXCEPTION', // Diện đặc cách
            'proposed_p1'               => 35000000,
            'proposed_insurance_salary' => 35000000,
            'effective_from'            => '2026-11-01',
            'reason'                    => 'Đặc cách thu hút nhân tài sáng chế công nghệ',
            'exception_reason'          => 'Kỹ sư đạt giải thưởng sáng tạo khoa học kỹ thuật cấp Bộ, chế tạo máy tiệt trùng tiết kiệm 1.2 tỷ/năm',
            'attachment_url'            => 'https://hrm.vinamilk.com.vn/docs/chung-chi-sang-che.pdf',
        ], $this->directorUser);

        $this->assertTrue((bool)$proposal->is_out_of_band);
        $this->assertTrue((bool)$proposal->is_special_exception);
        $this->assertEquals('OUT_OF_BAND_OR_EXCEPTION', $proposal->current_workflow_type);

        // Luồng duyệt gồm chính xác 4 bước
        $steps = ApprovalStep::where('proposal_id', $proposal->id)->orderBy('step_number')->get();
        $this->assertEquals(4, $steps->count());

        $this->assertEquals('DIVISION_DIRECTOR', $steps[0]->approver_role);
        $this->assertEquals('CB_COMMITTEE', $steps[1]->approver_role);
        $this->assertEquals('HR_DIRECTOR', $steps[2]->approver_role);
        $this->assertEquals('CEO', $steps[3]->approver_role);

        // Có cảnh báo vượt trần khung lương
        $this->assertNotEmpty($proposal->warnings_json);
    }
}
