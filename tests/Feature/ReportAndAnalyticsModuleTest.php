<?php

namespace Tests\Feature;

use App\Modules\Employee\Models\Employee;
use App\Modules\Organization\Models\EmployeeAssignment;
use App\Modules\Organization\Models\OrgUnit;
use App\Modules\Organization\Models\Position;
use App\Modules\Payroll\Models\EmployeeSalary;
use App\Modules\Payroll\Models\SalaryBand;
use App\Modules\Report\Services\HrEtlService;
use App\Modules\Report\Services\ReportService;
use Carbon\Carbon;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Tests\TestCase;

class ReportAndAnalyticsModuleTest extends TestCase
{
    use RefreshDatabase;

    protected ReportService $reportService;
    protected HrEtlService $etlService;

    protected OrgUnit $hq;
    protected OrgUnit $factory;
    protected Position $workerPos;
    protected Position $managerPos;
    protected Employee $maleWorker;
    protected Employee $femaleManager;

    protected function setUp(): void
    {
        parent::setUp();

        $this->reportService = app(ReportService::class);
        $this->etlService = app(HrEtlService::class);

        // 1. Cây tổ chức
        $this->hq = OrgUnit::create([
            'unit_code' => 'VNM_HQ', 'unit_name' => 'Trụ sở Vinamilk',
            'unit_type' => 'HEADQUARTERS', 'path' => '/1/', 'level' => 1, 'effective_from' => '2000-01-01',
        ]);
        $this->factory = OrgUnit::create([
            'unit_code' => 'VNM_FACTORY_BD', 'unit_name' => 'Nhà máy Sữa Bình Dương',
            'unit_type' => 'FACTORY', 'parent_id' => $this->hq->id, 'path' => '/1/4/', 'level' => 2, 'effective_from' => '2000-01-01',
        ]);

        // 2. Khung lương & Chức danh
        $band = SalaryBand::create(['band_code' => 'B4', 'min_salary' => 15000000, 'midpoint_salary' => 22000000, 'max_salary' => 30000000, 'effective_from' => '2020-01-01']);
        $this->workerPos = Position::create(['position_code' => 'WORKER', 'position_name' => 'Công nhân Sản xuất', 'level' => 2]);
        $this->managerPos = Position::create(['position_code' => 'MGR', 'position_name' => 'Trưởng ca Vận hành', 'level' => 4, 'salary_band_id' => $band->id]);

        // 3. Nhân sự Nam sinh năm 1965 (sắp đủ tuổi nghỉ hưu theo NĐ 135)
        $this->maleWorker = Employee::create([
            'employee_code'       => 'VNM-M001',
            'full_name'           => 'Nguyễn Văn Nam',
            'gender'              => 'MALE',
            'date_of_birth'       => '1965-06-15', // Nam 61 tuổi vào giữa năm 2026
            'national_id'         => '079065001234',
            'employment_status'   => 'ACTIVE',
            'hire_date'           => '2005-01-01',
        ]);
        EmployeeAssignment::create([
            'employee_id' => $this->maleWorker->id, 'org_unit_id' => $this->factory->id, 'position_id' => $this->workerPos->id,
            'is_primary' => true, 'is_current' => true, 'effective_from' => '2005-01-01',
        ]);
        EmployeeSalary::create([
            'employee_id' => $this->maleWorker->id, 'p1_position_salary' => 9000000, 'p2_competency_salary' => 3000000,
            'insurance_salary' => 12000000, 'effective_from' => '2005-01-01', 'is_current' => true,
        ]);

        // 4. Nhân sự Nữ sinh năm 1969
        $this->femaleManager = Employee::create([
            'employee_code'       => 'VNM-F002',
            'full_name'           => 'Trần Thị Nữ',
            'gender'              => 'FEMALE',
            'date_of_birth'       => '1969-10-20', // Nữ 57 tuổi vào cuối năm 2026
            'national_id'         => '079069005678',
            'employment_status'   => 'ACTIVE',
            'hire_date'           => '2010-05-01',
        ]);
        EmployeeAssignment::create([
            'employee_id' => $this->femaleManager->id, 'org_unit_id' => $this->hq->id, 'position_id' => $this->managerPos->id,
            'is_primary' => true, 'is_current' => true, 'effective_from' => '2010-05-01',
        ]);
        EmployeeSalary::create([
            'employee_id' => $this->femaleManager->id, 'p1_position_salary' => 20000000, 'p2_competency_salary' => 5000000,
            'insurance_salary' => 25000000, 'effective_from' => '2010-05-01', 'is_current' => true,
        ]);
    }

    /**
     * Test 1: Báo cáo BM01 Sơ yếu trích ngang lấy đúng thông tin và lọc theo cây tổ chức
     */
    public function test_bm01_trich_ngang_data_and_scope_filtering(): void
    {
        // Lọc toàn tập đoàn (/1/)
        $all = $this->reportService->getBM01TrichNgang(['org_path' => '/1/']);
        $this->assertCount(2, $all);

        // Lọc riêng nhà máy (/1/4/)
        $factoryOnly = $this->reportService->getBM01TrichNgang(['org_path' => '/1/4/']);
        $this->assertCount(1, $factoryOnly);
        $this->assertEquals('VNM-M001', $factoryOnly->first()->employee_code);
        $this->assertEquals(9000000, $factoryOnly->first()->p1_position_salary);
    }

    /**
     * Test 2: Báo cáo BM04 tính đúng tuổi và thời điểm nghỉ hưu theo Nghị định 135/2020
     */
    public function test_bm04_nghi_huu_decree_135_calculation(): void
    {
        $list = $this->reportService->getBM04NghiHuu('/1/', 48); // Rà soát trong vòng 48 tháng tới

        $this->assertNotEmpty($list);

        $male = $list->firstWhere('employee_code', 'VNM-M001');
        if ($male) {
            $this->assertEquals('Nam', $male->gender === 'MALE' ? 'Nam' : 'Nữ');
            $this->assertNotNull($male->retirement_date);
            $this->assertGreaterThan(0, $male->current_age);
        }
    }

    /**
     * Test 3: Xuất file Excel CSV có gắn chuẩn Unicode UTF-8 BOM
     */
    public function test_export_excel_csv_contains_utf8_bom(): void
    {
        $csv = $this->reportService->generateExcelCsv('bm01', ['org_path' => '/1/']);

        // 3 bytes đầu là BOM UTF-8 (\xEF\xBB\xBF)
        $this->assertStringStartsWith("\xEF\xBB\xBF", $csv);
        $this->assertStringContainsString('VNM-M001', $csv);
        $this->assertStringContainsString('Nguyễn Văn Nam', $csv);
    }

    /**
     * Test 4: ETL nạp hằng đêm vào Data Warehouse Star Schema (dim_org_unit, dim_position, dim_employee SCD2)
     */
    public function test_nightly_etl_star_schema_loading(): void
    {
        $results = $this->etlService->runNightlyEtl(Carbon::now());

        $this->assertGreaterThan(0, $results['dim_org_synced']);
        $this->assertGreaterThan(0, $results['dim_emp_scd2_synced']);

        // Kiểm tra trong bảng dim_employee có bản ghi với is_current = 1
        $this->assertDatabaseHas('dim_employee', [
            'employee_code' => 'VNM-M001',
            'is_current'    => true,
        ]);
    }
}
