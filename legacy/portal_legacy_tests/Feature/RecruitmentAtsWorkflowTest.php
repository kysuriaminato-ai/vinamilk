<?php

namespace Tests\Feature;

use App\Models\Document;
use App\Models\User;
use App\Modules\Employee\Models\Employee;
use App\Modules\Organization\Models\EmployeeAssignment;
use App\Modules\Organization\Models\OrgUnit;
use App\Modules\Organization\Models\Position;
use App\Modules\Payroll\Models\Contract;
use App\Modules\Payroll\Models\EmployeeSalary;
use App\Modules\Recruitment\Models\Application;
use App\Modules\Recruitment\Models\Candidate;
use App\Modules\Recruitment\Models\HealthCheck;
use App\Modules\Recruitment\Models\Offer;
use App\Modules\Recruitment\Models\Requisition;
use App\Modules\Recruitment\Services\RecruitmentService;
use Carbon\Carbon;
use DomainException;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Spatie\Permission\Models\Role;
use Tests\TestCase;

class RecruitmentAtsWorkflowTest extends TestCase
{
    use RefreshDatabase;

    protected RecruitmentService $service;
    protected OrgUnit $factory;
    protected Position $position;
    protected Requisition $requisition;
    protected Candidate $candidate;
    protected Application $application;

    protected function setUp(): void
    {
        parent::setUp();

        $this->service = app(RecruitmentService::class);

        // Khởi tạo vai trò EMPLOYEE mặc định
        Role::firstOrCreate(['name' => 'EMPLOYEE', 'guard_name' => 'web']);

        // 1. Khởi tạo Cơ cấu tổ chức & Vị trí
        $this->factory = OrgUnit::create([
            'unit_code'      => 'NM_TIEN_SON',
            'unit_name'      => 'Nhà máy Sữa Tiên Sơn',
            'unit_type'      => 'FACTORY',
            'path'           => '/1/2/5/',
            'level'          => 3,
            'effective_from' => '2026-01-01',
        ]);

        $this->position = Position::create([
            'position_code'    => 'POS_QC_MILK',
            'position_name'    => 'Kỹ sư Kiểm nghiệm Chất lượng Sữa (QC)',
            'job_grade_id'     => 1,
            'org_unit_id'      => $this->factory->id,
            'headcount_budget' => 5,
        ]);

        // 2. Tạo Phiếu tuyển dụng
        $this->requisition = Requisition::create([
            'requisition_code' => 'REQ-2026-TS-QC',
            'title'            => 'Tuyển Kỹ sư QC Sữa UHT',
            'org_unit_id'      => $this->factory->id,
            'position_id'      => $this->position->id,
            'headcount'        => 1,
            'deadline'         => '2026-12-31',
            'status'           => 'APPROVED',
            'created_by'       => 1,
        ]);

        // 3. Tạo Ứng viên & Đơn ứng tuyển
        $this->candidate = Candidate::create([
            'candidate_code'  => 'CAN-2026-001',
            'full_name'       => 'Nguyễn Thị Thu Trang',
            'email'           => 'thutrang.qc@gmail.com',
            'phone_number'    => '0912345678',
            'national_id'     => '001195009999',
            'gender'          => 'FEMALE',
            'cv_file_url'     => 'recruitment/cvs/cv_thutrang.pdf',
        ]);

        $this->application = Application::create([
            'candidate_id'   => $this->candidate->id,
            'requisition_id' => $this->requisition->id,
            'source'         => 'PORTAL',
            'applied_date'   => now()->toDateString(),
            'status'         => 'INTERVIEWING',
        ]);
    }

    /**
     * BƯỚC 5 TEST: KHÁM SỨC KHỎE KHÔNG ĐẠT THÌ CHẶN TUYỆT ĐỐI VIỆC TẠO OFFER.
     */
    public function test_cannot_create_offer_if_health_check_failed_or_missing()
    {
        // Kịch bản A: Ứng viên chưa từng đi khám sức khỏe
        $this->expectException(DomainException::class);
        $this->expectExceptionMessage('Lỗi nghiệp vụ ATS: Không thể tạo Thư mời nhận việc (Offer)');

        $this->service->createOffer($this->application->id, [
            'position_id'         => $this->position->id,
            'offered_p1'          => 16000000,
            'offered_p2'          => 4000000,
            'probation_rate'      => 85.00,
            'expected_start_date' => now()->addDays(7)->toDateString(),
        ]);

        // Kịch bản B: Đã khám nhưng kết luận KHÔNG ĐẠT tiêu chuẩn Thẻ Xanh thực phẩm
        HealthCheck::create([
            'candidate_id'     => $this->candidate->id,
            'check_date'       => now()->subDays(2)->toDateString(),
            'medical_facility' => 'Bệnh viện Đa khoa Bắc Ninh',
            'health_type'      => 'GREEN_CARD_FOOD_SAFETY',
            'health_class'     => 'CLASS_4',
            'is_passed'        => false, // KHÔNG ĐẠT
            'expiry_date'      => now()->addYear()->toDateString(),
        ]);

        $this->expectException(DomainException::class);
        $this->service->createOffer($this->application->id, [
            'position_id'         => $this->position->id,
            'offered_p1'          => 16000000,
            'offered_p2'          => 4000000,
            'probation_rate'      => 85.00,
            'expected_start_date' => now()->addDays(7)->toDateString(),
        ]);
    }

    /**
     * BƯỚC 6 TEST: ỨNG VIÊN ĐỒNG Ý OFFER -> KÍCH HOẠT TIẾP NHẬN TRONG 1 ATOMIC TRANSACTION.
     * Sinh Employee (VNM-000123), Assignment, Salary (85%), Contract (2 tháng), User ESS, chuyển Document.
     */
    public function test_candidate_accepting_offer_triggers_atomic_onboarding()
    {
        // 1. Giả lập ứng viên đã khám sức khỏe và ĐẠT Thẻ Xanh
        HealthCheck::create([
            'candidate_id'     => $this->candidate->id,
            'check_date'       => now()->toDateString(),
            'medical_facility' => 'Bệnh viện Quốc tế Thu Cúc',
            'health_type'      => 'GREEN_CARD_FOOD_SAFETY',
            'health_class'     => 'CLASS_1',
            'is_passed'        => true, // ĐẠT
            'expiry_date'      => now()->addYear()->toDateString(),
        ]);

        $startDate = now()->addDays(5)->toDateString();

        // 2. Tạo Offer hợp lệ
        $offer = $this->service->createOffer($this->application->id, [
            'position_id'         => $this->position->id,
            'offered_p1'          => 16000000, // 16 triệu P1
            'offered_p2'          => 4000000,  // 4 triệu P2
            'probation_rate'      => 85.00,
            'expected_start_date' => $startDate,
        ]);

        $this->assertEquals('OFFERED', $this->application->fresh()->status);

        // 3. Ứng viên chấp thuận Offer -> Kích hoạt tiếp nhận
        $employee = $this->service->acceptOffer($offer->id);

        // --- KIỂM TRA TÍNH TOÀN VẸN DỮ LIỆU CỦA TRANSACTION ---

        // A. Kiểm tra Mã nhân viên định dạng VNM-xxxxxx (6 số)
        $this->assertMatchesRegularExpression('/^VNM-\d{6}$/', $employee->employee_code);
        $this->assertEquals('PROBATION', $employee->employment_status);
        $this->assertEquals('Nguyễn Thị Thu Trang', $employee->full_name);

        // B. Kiểm tra Phân công công tác (employee_assignments)
        $assignment = EmployeeAssignment::where('employee_id', $employee->id)->first();
        $this->assertNotNull($assignment);
        $this->assertEquals($this->factory->id, $assignment->org_unit_id);
        $this->assertEquals($this->position->id, $assignment->position_id);
        $this->assertTrue($assignment->is_current);
        $this->assertEquals('INITIAL', $assignment->assignment_type);

        // C. Kiểm tra Mức lương thử việc 85% (employee_salaries)
        // 16.000.000 * 85% = 13.600.000 VND
        // 4.000.000 * 85% = 3.400.000 VND
        $salary = EmployeeSalary::where('employee_id', $employee->id)->first();
        $this->assertNotNull($salary);
        $this->assertEquals(13600000, $salary->p1_position_salary);
        $this->assertEquals(3400000, $salary->p2_competency_salary);
        $this->assertTrue($salary->is_current);

        // D. Kiểm tra Hợp đồng thử việc 2 tháng (contracts)
        $contract = Contract::where('employee_id', $employee->id)->first();
        $this->assertNotNull($contract);
        $this->assertEquals('PROBATION', $contract->contract_type);
        $this->assertEquals($startDate, $contract->start_date->toDateString());
        $expectedEndDate = Carbon::parse($startDate)->addMonths(2)->subDay()->toDateString();
        $this->assertEquals($expectedEndDate, $contract->end_date->toDateString());

        // E. Kiểm tra Tài khoản người dùng ESS (users)
        $user = User::where('employee_id', $employee->id)->first();
        $this->assertNotNull($user);
        $this->assertEquals(strtolower($employee->employee_code), $user->username);
        $this->assertTrue((bool)$user->must_change_password, 'Bắt buộc đổi mật khẩu lần đầu');
        $this->assertTrue($user->hasRole('EMPLOYEE'));

        // F. Kiểm tra Chép CV đính kèm sang bảng tài liệu (documents)
        $document = Document::where('employee_id', $employee->id)->first();
        $this->assertNotNull($document);
        $this->assertEquals($this->candidate->cv_file_url, $document->file_path);

        // G. Kiểm tra Trạng thái kết thúc quy trình ATS
        $this->assertEquals('ACCEPTED', $offer->fresh()->status);
        $this->assertEquals('HIRED', $this->application->fresh()->status);
    }
}
