<?php

namespace App\Console\Commands;

use Illuminate\Console\Command;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Hash;
use Illuminate\Support\Str;
use Carbon\Carbon;

class GenerateVinamilkDemoDataCommand extends Command
{
    /**
     * The name and signature of the console command.
     *
     * @var string
     */
    protected $signature = 'vinamilk:seed-demo-data 
                            {--fresh : Xóa dữ liệu cũ và nạp lại từ đầu} 
                            {--attendance-sample-days=365 : Số ngày chấm công mẫu trong năm 2026}';

    /**
     * The console command description.
     *
     * @var string
     */
    protected $description = 'Sinh dữ liệu mẫu quy mô doanh nghiệp Vinamilk (vi_VN) chuẩn MySQL 8 cho toàn bộ hệ sinh thái HRIS';

    /**
     * Danh sách 9 tài khoản demo được cấp mật khẩu ngẫu nhiên
     */
    protected array $generatedDemoAccounts = [];

    /**
     * Execute the console command.
     */
    public function handle(): int
    {
        $this->info("================================================================================");
        $this->info("   VINAMILK HRIS - GENERATING ENTERPRISE DEMO DATASET (vi_VN)");
        $this->info("================================================================================");

        $startTime = microtime(true);

        try {
            DB::statement('SET FOREIGN_KEY_CHECKS=0;');

            if ($this->option('fresh')) {
                $this->warn("Đang dọn dẹp các bảng dữ liệu cũ...");
                $this->truncateTables();
            }

            // 1. Pháp lý & Tham số động
            $this->info("1/9. Nạp tham số pháp lý, bảng thuế 7 bậc, ngạch lương & ca kíp...");
            $this->seedLegalParametersAndShifts();

            // 2. Cây tổ chức
            $this->info("2/9. Nạp Cây tổ chức: HQ (8 phòng), 3 Vùng, 6 Nhà máy, 4 Trang trại, 3 Chi nhánh...");
            $orgUnits = $this->seedOrgUnits();

            // 3. Vị trí & Định biên
            $this->info("3/9. Nạp danh mục chức danh, bậc ngạch và dải lương P1/P2...");
            $positions = $this->seedPositionsAndJobGrades($orgUnits);

            // 4. 800 Nhân sự
            $this->info("4/9. Sinh 800 hồ sơ nhân sự phân theo 4 khối (60% SX, 15% Nông trại, 15% KD, 10% VP; 60 thôi việc)...");
            $employees = $this->seedEmployees($orgUnits, $positions);

            // 5. Chấm công 12 tháng (~3% vắng mặt, OT hợp lý)
            $this->info("5/9. Sinh dữ liệu chấm công 12 tháng 2026 (~3% vắng mặt, OT ngày thường/cuối tuần/lễ/đêm)...");
            $this->seedAttendanceData($employees);

            // 6. 2 Kỳ KPI & Đánh giá
            $this->info("6/9. Sinh 2 kỳ KPI (H1-2026, H2-2026), nhiệm vụ, điểm thưởng/phạt và xuất lương P3...");
            $this->seedKpiData($employees);

            // 7. 40 Tờ trình tăng lương
            $this->info("7/9. Sinh 40 tờ trình tăng lương ở nhiều trạng thái kèm luồng phê duyệt động...");
            $this->seedSalaryProposals($employees);

            // 8. 150 Ứng viên ATS
            $this->info("8/9. Sinh 150 ứng viên tuyển dụng qua các giai đoạn Kanban ATS...");
            $this->seedRecruitmentData($orgUnits, $positions, $employees);

            // 9. 9 Tài khoản Demo
            $this->info("9/9. Khởi tạo 9 tài khoản demo cho 9 vai trò với mật khẩu ngẫu nhiên an toàn...");
            $this->seedDemoAccountsAndRoles($employees, $orgUnits);

            DB::statement('SET FOREIGN_KEY_CHECKS=1;');

            $duration = round(microtime(true) - $startTime, 2);
            $this->newLine();
            $this->info("================================================================================");
            $this->info("   KHỞI TẠO DỮ LIỆU MẪU THÀNH CÔNG TRONG {$duration}s !");
            $this->info("================================================================================");

            // In bảng mật khẩu demo duy nhất một lần ra màn hình
            $this->printDemoAccountsTable();

            return Command::SUCCESS;
        } catch (\Throwable $e) {
            DB::statement('SET FOREIGN_KEY_CHECKS=1;');
            $this->error("LỖI KHỞI TẠO DỮ LIỆU: " . $e->getMessage());
            $this->error($e->getTraceAsString());
            return Command::FAILURE;
        }
    }

    /**
     * Dọn dẹp dữ liệu bảng
     */
    protected function truncateTables(): void
    {
        $tables = [
            'retroactive_salary_adjustments',
            'approval_steps',
            'salary_proposals',
            'approval_rules',
            'unit_salary_budgets',
            'payslip_lines',
            'payslips',
            'payroll_periods',
            'kpi_employee_summaries',
            'kpi_assignments',
            'kpi_tasks',
            'kpi_periods',
            'attendance_days',
            'attendance_logs',
            'shift_schedules',
            'leave_requests',
            'leave_balances',
            'offers',
            'health_checks',
            'interviews',
            'applications',
            'candidates',
            'requisitions',
            'documents',
            'dependents',
            'contracts',
            'employee_salaries',
            'employee_assignments',
            'user_scopes',
            'users',
            'role_permissions',
            'permissions',
            'roles',
            'positions',
            'salary_bands',
            'job_grades',
            'org_units',
            'shifts',
            'leave_types',
            'legal_parameters',
            'tax_brackets',
            'regional_min_wages',
            'ot_rates',
        ];

        foreach ($tables as $table) {
            if (DB::getSchemaBuilder()->hasTable($table)) {
                DB::table($table)->truncate();
            }
        }
    }

    /**
     * 1. Nạp tham số pháp lý, bảng thuế TNCN 7 bậc, lương tối thiểu vùng & danh mục ca kíp
     */
    protected function seedLegalParametersAndShifts(): void
    {
        // 1.1 Legal Parameters
        $legalParams = [
            ['param_code' => 'PERSONAL_TAX_RELIEF', 'param_name' => 'Mức giảm trừ gia cảnh bản thân', 'param_value' => 11000000.0000, 'unit' => 'VND', 'description' => 'Nghị quyết 954/2020/UBTVQH14', 'effective_from' => '2020-07-01'],
            ['param_code' => 'DEPENDENT_TAX_RELIEF', 'param_name' => 'Mức giảm trừ gia cảnh người phụ thuộc', 'param_value' => 4400000.0000, 'unit' => 'VND', 'description' => 'Nghị quyết 954/2020/UBTVQH14', 'effective_from' => '2020-07-01'],
            ['param_code' => 'BASE_SALARY_SI', 'param_name' => 'Mức lương cơ sở đóng BHXH', 'param_value' => 2340000.0000, 'unit' => 'VND', 'description' => 'Nghị định 73/2024/NĐ-CP', 'effective_from' => '2024-07-01'],
            ['param_code' => 'MAX_SI_SALARY_CAP', 'param_name' => 'Trần lương đóng BHXH/BHYT (20 lần lương cơ sở)', 'param_value' => 46800000.0000, 'unit' => 'VND', 'description' => 'Luật BHXH (20 * 2.340.000đ)', 'effective_from' => '2024-07-01'],
            ['param_code' => 'SI_EMP_RATE', 'param_name' => 'Tỷ lệ đóng BHXH người lao động', 'param_value' => 0.0800, 'unit' => 'RATE', 'description' => '8% mức lương đóng BH', 'effective_from' => '2020-01-01'],
            ['param_code' => 'HI_EMP_RATE', 'param_name' => 'Tỷ lệ đóng BHYT người lao động', 'param_value' => 0.0150, 'unit' => 'RATE', 'description' => '1.5% mức lương đóng BH', 'effective_from' => '2020-01-01'],
            ['param_code' => 'UI_EMP_RATE', 'param_name' => 'Tỷ lệ đóng BHTN người lao động', 'param_value' => 0.0100, 'unit' => 'RATE', 'description' => '1% mức lương đóng BH', 'effective_from' => '2020-01-01'],
            ['param_code' => 'SI_COMP_RATE', 'param_name' => 'Tỷ lệ BHXH doanh nghiệp đóng', 'param_value' => 0.1750, 'unit' => 'RATE', 'description' => '17.5% bảo hiểm xã hội', 'effective_from' => '2020-01-01'],
            ['param_code' => 'HI_COMP_RATE', 'param_name' => 'Tỷ lệ BHYT doanh nghiệp đóng', 'param_value' => 0.0300, 'unit' => 'RATE', 'description' => '3% bảo hiểm y tế', 'effective_from' => '2020-01-01'],
            ['param_code' => 'UI_COMP_RATE', 'param_name' => 'Tỷ lệ BHTN doanh nghiệp đóng', 'param_value' => 0.0100, 'unit' => 'RATE', 'description' => '1% bảo hiểm thất nghiệp', 'effective_from' => '2020-01-01'],
            ['param_code' => 'UNION_FEE_COMP_RATE', 'param_name' => 'Kinh phí Công đoàn doanh nghiệp đóng', 'param_value' => 0.0200, 'unit' => 'RATE', 'description' => '2% quỹ tiền lương BHXH', 'effective_from' => '2020-01-01'],
            ['param_code' => 'MAX_MONTHLY_OT_HOURS', 'param_name' => 'Số giờ làm thêm tối đa trong tháng', 'param_value' => 40.0000, 'unit' => 'HOURS', 'description' => 'Điều 107 Bộ luật Lao động 2019', 'effective_from' => '2021-01-01'],
            ['param_code' => 'MAX_YEARLY_OT_HOURS', 'param_name' => 'Số giờ làm thêm tối đa trong năm', 'param_value' => 300.0000, 'unit' => 'HOURS', 'description' => 'Ngành chế biến thực phẩm / sữa', 'effective_from' => '2021-01-01'],
        ];

        foreach ($legalParams as $p) {
            DB::table('legal_parameters')->updateOrInsert(
                ['param_code' => $p['param_code']],
                array_merge($p, ['is_current' => 1, 'created_at' => now(), 'updated_at' => now()])
            );
        }

        // 1.2 Biểu thuế TNCN 7 bậc
        $taxBrackets = [
            ['bracket_level' => 1, 'min_income' => 0, 'max_income' => 5000000, 'tax_rate' => 5.00, 'quick_deduction' => 0, 'effective_from' => '2020-07-01'],
            ['bracket_level' => 2, 'min_income' => 5000000, 'max_income' => 10000000, 'tax_rate' => 10.00, 'quick_deduction' => 250000, 'effective_from' => '2020-07-01'],
            ['bracket_level' => 3, 'min_income' => 10000000, 'max_income' => 18000000, 'tax_rate' => 15.00, 'quick_deduction' => 750000, 'effective_from' => '2020-07-01'],
            ['bracket_level' => 4, 'min_income' => 18000000, 'max_income' => 32000000, 'tax_rate' => 20.00, 'quick_deduction' => 1650000, 'effective_from' => '2020-07-01'],
            ['bracket_level' => 5, 'min_income' => 32000000, 'max_income' => 52000000, 'tax_rate' => 25.00, 'quick_deduction' => 3250000, 'effective_from' => '2020-07-01'],
            ['bracket_level' => 6, 'min_income' => 52000000, 'max_income' => 80000000, 'tax_rate' => 30.00, 'quick_deduction' => 5850000, 'effective_from' => '2020-07-01'],
            ['bracket_level' => 7, 'min_income' => 80000000, 'max_income' => null, 'tax_rate' => 35.00, 'quick_deduction' => 9850000, 'effective_from' => '2020-07-01'],
        ];

        foreach ($taxBrackets as $tb) {
            DB::table('tax_brackets')->updateOrInsert(
                ['bracket_level' => $tb['bracket_level']],
                array_merge($tb, ['is_current' => 1, 'created_at' => now(), 'updated_at' => now()])
            );
        }

        // 1.3 Lương tối thiểu vùng (Nghị định 74/2024/NĐ-CP)
        $minWages = [
            ['region_zone' => 'ZONE_1', 'monthly_min_wage' => 4960000, 'hourly_min_wage' => 23800, 'effective_from' => '2024-07-01'],
            ['region_zone' => 'ZONE_2', 'monthly_min_wage' => 4410000, 'hourly_min_wage' => 21200, 'effective_from' => '2024-07-01'],
            ['region_zone' => 'ZONE_3', 'monthly_min_wage' => 3860000, 'hourly_min_wage' => 18600, 'effective_from' => '2024-07-01'],
            ['region_zone' => 'ZONE_4', 'monthly_min_wage' => 3450000, 'hourly_min_wage' => 16600, 'effective_from' => '2024-07-01'],
        ];

        foreach ($minWages as $mw) {
            DB::table('regional_min_wages')->updateOrInsert(
                ['region_zone' => $mw['region_zone']],
                array_merge($mw, ['is_current' => 1, 'created_at' => now(), 'updated_at' => now()])
            );
        }

        // 1.4 Hệ số OT
        $otRates = [
            ['rate_code' => 'OT_WEEKDAY', 'rate_name' => 'Làm thêm ngày thường (150%)', 'multiplier' => 1.50, 'night_additional_multiplier' => 0.30, 'effective_from' => '2020-01-01'],
            ['rate_code' => 'OT_WEEKEND', 'rate_name' => 'Làm thêm ngày nghỉ tuần (200%)', 'multiplier' => 2.00, 'night_additional_multiplier' => 0.30, 'effective_from' => '2020-01-01'],
            ['rate_code' => 'OT_HOLIDAY', 'rate_name' => 'Làm thêm ngày Lễ/Tết (300%)', 'multiplier' => 3.00, 'night_additional_multiplier' => 0.30, 'effective_from' => '2020-01-01'],
        ];

        foreach ($otRates as $ot) {
            DB::table('ot_rates')->updateOrInsert(
                ['rate_code' => $ot['rate_code']],
                array_merge($ot, ['is_current' => 1, 'created_at' => now(), 'updated_at' => now()])
            );
        }

        // 1.5 Danh mục ca làm việc (Shifts)
        $shifts = [
            ['shift_code' => 'CA_HC', 'shift_name' => 'Ca Hành chính Trụ sở / Văn phòng', 'start_time' => '08:00:00', 'end_time' => '17:00:00', 'break_minutes' => 60, 'standard_hours' => 8.00, 'is_night_shift' => 0],
            ['shift_code' => 'CA_1_SX', 'shift_name' => 'Ca 1 Nhà máy Chế biến (Sáng)', 'start_time' => '06:00:00', 'end_time' => '14:00:00', 'break_minutes' => 45, 'standard_hours' => 8.00, 'is_night_shift' => 0],
            ['shift_code' => 'CA_2_SX', 'shift_name' => 'Ca 2 Nhà máy Chế biến (Chiều)', 'start_time' => '14:00:00', 'end_time' => '22:00:00', 'break_minutes' => 45, 'standard_hours' => 8.00, 'is_night_shift' => 0],
            ['shift_code' => 'CA_3_DEM', 'shift_name' => 'Ca 3 Nhà máy Chế biến (Đêm)', 'start_time' => '22:00:00', 'end_time' => '06:00:00', 'break_minutes' => 45, 'standard_hours' => 8.00, 'is_night_shift' => 1],
            ['shift_code' => 'CA_SOM_FARM', 'shift_name' => 'Ca sớm Trang trại Bò sữa (Vắt sữa 1)', 'start_time' => '04:30:00', 'end_time' => '12:30:00', 'break_minutes' => 45, 'standard_hours' => 8.00, 'is_night_shift' => 0],
            ['shift_code' => 'CA_CHIEU_FARM', 'shift_name' => 'Ca chiều Trang trại Bò sữa (Vắt sữa 2)', 'start_time' => '12:30:00', 'end_time' => '20:30:00', 'break_minutes' => 45, 'standard_hours' => 8.00, 'is_night_shift' => 0],
            ['shift_code' => 'CA_RETAIL_1', 'shift_name' => 'Ca Kinh doanh / Bán lẻ Sáng', 'start_time' => '08:00:00', 'end_time' => '16:00:00', 'break_minutes' => 60, 'standard_hours' => 8.00, 'is_night_shift' => 0],
            ['shift_code' => 'CA_RETAIL_2', 'shift_name' => 'Ca Kinh doanh / Bán lẻ Tối', 'start_time' => '13:00:00', 'end_time' => '21:00:00', 'break_minutes' => 60, 'standard_hours' => 8.00, 'is_night_shift' => 0],
        ];

        foreach ($shifts as $s) {
            DB::table('shifts')->updateOrInsert(
                ['shift_code' => $s['shift_code']],
                array_merge($s, ['is_active' => 1, 'created_at' => now(), 'updated_at' => now()])
            );
        }

        // 1.6 Danh mục loại nghỉ phép
        $leaveTypes = [
            ['type_code' => 'ANNUAL', 'type_name' => 'Nghỉ phép năm', 'is_paid' => 1, 'deduct_annual_balance' => 1, 'social_insurance_coverage' => 0],
            ['type_code' => 'PERSONAL', 'type_name' => 'Nghỉ việc riêng hưởng nguyên lương', 'is_paid' => 1, 'deduct_annual_balance' => 0, 'social_insurance_coverage' => 0],
            ['type_code' => 'SICK_SI', 'type_name' => 'Nghỉ ốm đau hưởng chế độ BHXH', 'is_paid' => 0, 'deduct_annual_balance' => 0, 'social_insurance_coverage' => 1],
            ['type_code' => 'MATERNITY', 'type_name' => 'Nghỉ thai sản theo chế độ BHXH', 'is_paid' => 0, 'deduct_annual_balance' => 0, 'social_insurance_coverage' => 1],
            ['type_code' => 'UNPAID', 'type_name' => 'Nghỉ không hưởng lương', 'is_paid' => 0, 'deduct_annual_balance' => 0, 'social_insurance_coverage' => 0],
            ['type_code' => 'AWOL', 'type_name' => 'Nghỉ tự do không phép (Trừ công & kỷ luật)', 'is_paid' => 0, 'deduct_annual_balance' => 0, 'social_insurance_coverage' => 0],
        ];

        foreach ($leaveTypes as $lt) {
            DB::table('leave_types')->updateOrInsert(
                ['type_code' => $lt['type_code']],
                array_merge($lt, ['created_at' => now(), 'updated_at' => now()])
            );
        }
    }

    /**
     * 2. Nạp Cây tổ chức: HQ (8 phòng), 3 Vùng, 6 Nhà máy, 4 Trang trại, 3 Chi nhánh
     */
    protected function seedOrgUnits(): array
    {
        $units = [];

        // 2.1 Trụ sở chính (Level 1)
        $hqId = DB::table('org_units')->insertGetId([
            'unit_code'        => 'VNM_HQ',
            'unit_name'        => 'Trụ sở Chính Tập đoàn Vinamilk (TP. Hồ Chí Minh)',
            'unit_type'        => 'HEADQUARTERS',
            'parent_id'        => null,
            'path'             => '/1/',
            'level'            => 1,
            'region'           => 'SOUTH',
            'address'          => 'Số 10 Tân Trào, Phường Tân Phú, Quận 7, TP. Hồ Chí Minh',
            'cost_center_code' => 'CC-HQ-CORP',
            'effective_from'   => '1976-04-29',
            'is_active'        => 1,
            'created_at'       => now(),
            'updated_at'       => now(),
        ]);
        $units['HQ'] = $hqId;

        // 2.2 Trụ sở 8 phòng ban (Level 2)
        $hqDepts = [
            ['code' => 'HQ_BOD', 'name' => 'Ban Tổng Giám đốc & Thư ký HĐQT', 'type' => 'DEPARTMENT', 'path' => "/1/101/"],
            ['code' => 'HQ_HR', 'name' => 'Khối Quản trị Nguồn nhân lực & Đào tạo', 'type' => 'DEPARTMENT', 'path' => "/1/102/"],
            ['code' => 'HQ_FIN', 'name' => 'Khối Tài chính - Kế toán & Kiểm soát Ngân sách', 'type' => 'DEPARTMENT', 'path' => "/1/103/"],
            ['code' => 'HQ_IT', 'name' => 'Khối Công nghệ Thông tin & Chuyển đổi số SAP', 'type' => 'DEPARTMENT', 'path' => "/1/104/"],
            ['code' => 'HQ_RND', 'name' => 'Khối Nghiên cứu & Phát triển Sản phẩm (R&D)', 'type' => 'DEPARTMENT', 'path' => "/1/105/"],
            ['code' => 'HQ_MKT', 'name' => 'Khối Marketing & Quản trị Nhãn hiệu', 'type' => 'DEPARTMENT', 'path' => "/1/106/"],
            ['code' => 'HQ_LOG', 'name' => 'Khối Cung ứng & Logistics Toàn quốc', 'type' => 'DEPARTMENT', 'path' => "/1/107/"],
            ['code' => 'HQ_LEGAL', 'name' => 'Khối Pháp chế Doanh nghiệp & Tuân thủ', 'type' => 'DEPARTMENT', 'path' => "/1/108/"],
        ];

        $units['HQ_DEPTS'] = [];
        foreach ($hqDepts as $dept) {
            $id = DB::table('org_units')->insertGetId([
                'unit_code'        => $dept['code'],
                'unit_name'        => $dept['name'],
                'unit_type'        => $dept['type'],
                'parent_id'        => $hqId,
                'path'             => $dept['path'],
                'level'            => 2,
                'region'           => 'SOUTH',
                'address'          => 'Tòa nhà Vinamilk Tower, Quận 7, TP.HCM',
                'cost_center_code' => 'CC-' . $dept['code'],
                'effective_from'   => '1990-01-01',
                'is_active'        => 1,
                'created_at'       => now(),
                'updated_at'       => now(),
            ]);
            $units['HQ_DEPTS'][$dept['code']] = $id;
        }

        // 2.3 3 Vùng (Level 2)
        $regions = [
            ['code' => 'VNM_NORTH', 'name' => 'Văn phòng Điều hành Vùng Miền Bắc', 'region' => 'NORTH', 'path' => '/1/2/'],
            ['code' => 'VNM_CENTRAL', 'name' => 'Văn phòng Điều hành Vùng Miền Trung', 'region' => 'CENTRAL', 'path' => '/1/3/'],
            ['code' => 'VNM_SOUTH', 'name' => 'Văn phòng Điều hành Vùng Miền Nam', 'region' => 'SOUTH', 'path' => '/1/4/'],
        ];

        $regionIds = [];
        foreach ($regions as $r) {
            $id = DB::table('org_units')->insertGetId([
                'unit_code'        => $r['code'],
                'unit_name'        => $r['name'],
                'unit_type'        => 'DIVISION',
                'parent_id'        => $hqId,
                'path'             => $r['path'],
                'level'            => 2,
                'region'           => $r['region'],
                'cost_center_code' => 'CC-' . $r['code'],
                'effective_from'   => '1995-01-01',
                'is_active'        => 1,
                'created_at'       => now(),
                'updated_at'       => now(),
            ]);
            $regionIds[$r['code']] = $id;
            $units[$r['code']] = $id;
        }

        // 2.4 6 Nhà máy (Level 3)
        $factories = [
            ['code' => 'NM_TIEN_SON', 'name' => 'Nhà máy Sữa Tiên Sơn', 'parent' => 'VNM_NORTH', 'region' => 'NORTH', 'path' => '/1/2/201/', 'addr' => 'KCN Tiên Sơn, Bắc Ninh'],
            ['code' => 'NM_LAM_SON', 'name' => 'Nhà máy Sữa Lam Sơn', 'parent' => 'VNM_NORTH', 'region' => 'NORTH', 'path' => '/1/2/202/', 'addr' => 'KCN Lễ Môn, Thanh Hóa'],
            ['code' => 'NM_DA_NANG', 'name' => 'Nhà máy Sữa Đà Nẵng', 'parent' => 'VNM_CENTRAL', 'region' => 'CENTRAL', 'path' => '/1/3/301/', 'addr' => 'KCN Hòa Khánh, TP. Đà Nẵng'],
            ['code' => 'NM_THONG_NHAT', 'name' => 'Nhà máy Sữa Thống Nhất', 'parent' => 'VNM_SOUTH', 'region' => 'SOUTH', 'path' => '/1/4/401/', 'addr' => 'KCN Trường Thọ, TP. Thủ Đức, TP.HCM'],
            ['code' => 'NM_MEGA', 'name' => 'Nhà máy Sữa Việt Nam (Mega Factory)', 'parent' => 'VNM_SOUTH', 'region' => 'SOUTH', 'path' => '/1/4/402/', 'addr' => 'KCN Mỹ Phước 2, Bến Cát, Bình Dương'],
            ['code' => 'NM_DIELAC', 'name' => 'Nhà máy Sữa Bột Dielac', 'parent' => 'VNM_SOUTH', 'region' => 'SOUTH', 'path' => '/1/4/403/', 'addr' => 'KCN Biên Hòa 1, Đồng Nai'],
        ];

        $units['FACTORIES'] = [];
        foreach ($factories as $f) {
            $id = DB::table('org_units')->insertGetId([
                'unit_code'        => $f['code'],
                'unit_name'        => $f['name'],
                'unit_type'        => 'FACTORY',
                'parent_id'        => $regionIds[$f['parent']],
                'path'             => $f['path'],
                'level'            => 3,
                'region'           => $f['region'],
                'address'          => $f['addr'],
                'cost_center_code' => 'CC-' . $f['code'],
                'effective_from'   => '2000-01-01',
                'is_active'        => 1,
                'created_at'       => now(),
                'updated_at'       => now(),
            ]);
            $units['FACTORIES'][$f['code']] = $id;
        }

        // 2.5 4 Trang trại bò sữa (Level 3)
        $farms = [
            ['code' => 'FARM_THANH_HOA', 'name' => 'Trang trại Bò sữa Thanh Hóa', 'parent' => 'VNM_NORTH', 'region' => 'NORTH', 'path' => '/1/2/203/', 'addr' => 'Yên Định, Thanh Hóa'],
            ['code' => 'FARM_NGHE_AN', 'name' => 'Trang trại Bò sữa Nghệ An', 'parent' => 'VNM_CENTRAL', 'region' => 'CENTRAL', 'path' => '/1/3/302/', 'addr' => 'Nghĩa Đàn, Nghệ An'],
            ['code' => 'FARM_TAY_NINH', 'name' => 'Trang trại Bò sữa Resort Tây Ninh', 'parent' => 'VNM_SOUTH', 'region' => 'SOUTH', 'path' => '/1/4/404/', 'addr' => 'Bến Cầu, Tây Ninh'],
            ['code' => 'FARM_LAM_DONG', 'name' => 'Trang trại Bò sữa Organic Đà Lạt', 'parent' => 'VNM_SOUTH', 'region' => 'SOUTH', 'path' => '/1/4/405/', 'addr' => 'Đơn Dương, Lâm Đồng'],
        ];

        $units['FARMS'] = [];
        foreach ($farms as $farm) {
            $id = DB::table('org_units')->insertGetId([
                'unit_code'        => $farm['code'],
                'unit_name'        => $farm['name'],
                'unit_type'        => 'DAIRY_FARM',
                'parent_id'        => $regionIds[$farm['parent']],
                'path'             => $farm['path'],
                'level'            => 3,
                'region'           => $farm['region'],
                'address'          => $farm['addr'],
                'cost_center_code' => 'CC-' . $farm['code'],
                'effective_from'   => '2005-01-01',
                'is_active'        => 1,
                'created_at'       => now(),
                'updated_at'       => now(),
            ]);
            $units['FARMS'][$farm['code']] = $id;
        }

        // 2.6 3 Chi nhánh kinh doanh (Level 3)
        $branches = [
            ['code' => 'CN_HA_NOI', 'name' => 'Chi nhánh Kinh doanh Hà Nội', 'parent' => 'VNM_NORTH', 'region' => 'NORTH', 'path' => '/1/2/204/', 'addr' => 'Ba Đình, Hà Nội'],
            ['code' => 'CN_DA_NANG', 'name' => 'Chi nhánh Kinh doanh Đà Nẵng', 'parent' => 'VNM_CENTRAL', 'region' => 'CENTRAL', 'path' => '/1/3/303/', 'addr' => 'Hải Châu, Đà Nẵng'],
            ['code' => 'CN_CAN_THO', 'name' => 'Chi nhánh Kinh doanh Cần Thơ', 'parent' => 'VNM_SOUTH', 'region' => 'SOUTH', 'path' => '/1/4/406/', 'addr' => 'Ninh Kiều, Cần Thơ'],
        ];

        $units['BRANCHES'] = [];
        foreach ($branches as $b) {
            $id = DB::table('org_units')->insertGetId([
                'unit_code'        => $b['code'],
                'unit_name'        => $b['name'],
                'unit_type'        => 'BRANCH',
                'parent_id'        => $regionIds[$b['parent']],
                'path'             => $b['path'],
                'level'            => 3,
                'region'           => $b['region'],
                'address'          => $b['addr'],
                'cost_center_code' => 'CC-' . $b['code'],
                'effective_from'   => '1998-01-01',
                'is_active'        => 1,
                'created_at'       => now(),
                'updated_at'       => now(),
            ]);
            $units['BRANCHES'][$b['code']] = $id;
        }

        return $units;
    }

    /**
     * 3. Nạp danh mục chức danh, bậc ngạch và dải lương P1/P2
     */
    protected function seedPositionsAndJobGrades(array $orgUnits): array
    {
        // 3.1 Job Grades
        $grades = [
            ['code' => 'L1_WORKER', 'name' => 'Bậc L1 - Công nhân kỹ thuật / Lao động vận hành', 'level' => 1],
            ['code' => 'L2_TECHNICIAN', 'name' => 'Bậc L2 - Kỹ thuật viên / Chuyên viên sơ cấp', 'level' => 2],
            ['code' => 'L3_SPECIALIST', 'name' => 'Bậc L3 - Kỹ sư chính / Chuyên viên chuyên sâu', 'level' => 3],
            ['code' => 'M1_SUPERVISOR', 'name' => 'Bậc M1 - Quản đốc phân xưởng / Trưởng nhóm', 'level' => 4],
            ['code' => 'M2_MANAGER', 'name' => 'Bậc M2 - Trưởng phòng ban / Phó giám đốc đơn vị', 'level' => 5],
            ['code' => 'M3_DIRECTOR', 'name' => 'Bậc M3 - Giám đốc nhà máy / Giám đốc khối', 'level' => 6],
            ['code' => 'EXEC_BOD', 'name' => 'Bậc C - Ban Tổng Giám đốc Tập đoàn', 'level' => 7],
        ];

        $gradeIds = [];
        foreach ($grades as $g) {
            $id = DB::table('job_grades')->insertGetId([
                'grade_code'  => $g['code'],
                'grade_name'  => $g['name'],
                'grade_level' => $g['level'],
                'description' => 'Khung ngạch chuẩn tập đoàn Vinamilk',
                'created_at'  => now(),
                'updated_at'  => now(),
            ]);
            $gradeIds[$g['code']] = $id;
        }

        // 3.2 Salary Bands
        $bands = [
            ['code' => 'BAND_L1', 'grade' => 'L1_WORKER', 'min' => 7500000, 'mid' => 10500000, 'max' => 14000000],
            ['code' => 'BAND_L2', 'grade' => 'L2_TECHNICIAN', 'min' => 11000000, 'mid' => 16000000, 'max' => 22000000],
            ['code' => 'BAND_L3', 'grade' => 'L3_SPECIALIST', 'min' => 18000000, 'mid' => 26000000, 'max' => 36000000],
            ['code' => 'BAND_M1', 'grade' => 'M1_SUPERVISOR', 'min' => 25000000, 'mid' => 38000000, 'max' => 50000000],
            ['code' => 'BAND_M2', 'grade' => 'M2_MANAGER', 'min' => 40000000, 'mid' => 58000000, 'max' => 75000000],
            ['code' => 'BAND_M3', 'grade' => 'M3_DIRECTOR', 'min' => 60000000, 'mid' => 90000000, 'max' => 130000000],
            ['code' => 'BAND_EXEC', 'grade' => 'EXEC_BOD', 'min' => 100000000, 'mid' => 160000000, 'max' => 250000000],
        ];

        foreach ($bands as $b) {
            DB::table('salary_bands')->insert([
                'band_code'      => $b['code'],
                'job_grade_id'   => $gradeIds[$b['grade']],
                'min_salary'     => $b['min'],
                'mid_salary'     => $b['mid'],
                'max_salary'     => $b['max'],
                'effective_from' => '2024-01-01',
                'is_current'     => 1,
                'created_at'     => now(),
                'updated_at'     => now(),
            ]);
        }

        // 3.3 Danh mục vị trí công việc theo khối
        $posDefinitions = [
            // Khối sản xuất
            ['code' => 'POS_PROD_OPERATOR', 'name' => 'Công nhân Vận hành Máy Chế biến Sữa', 'grade' => 'L1_WORKER', 'cluster' => 'PROD', 'p1' => 9500000, 'p2' => 2500000, 'toxic' => 800000, 'farm' => 0],
            ['code' => 'POS_PROD_TETRA', 'name' => 'Kỹ sư Điều khiển Dây chuyền Tiệt trùng Tetra Pak', 'grade' => 'L3_SPECIALIST', 'cluster' => 'PROD', 'p1' => 24000000, 'p2' => 6000000, 'toxic' => 1000000, 'farm' => 0],
            ['code' => 'POS_PROD_QAQC', 'name' => 'Kỹ thuật viên Kiểm nghiệm Chất lượng QA/QC', 'grade' => 'L2_TECHNICIAN', 'cluster' => 'PROD', 'p1' => 15000000, 'p2' => 3500000, 'toxic' => 600000, 'farm' => 0],
            ['code' => 'POS_PROD_SUPERVISOR', 'name' => 'Quản đốc Phân xưởng Chế biến Sữa', 'grade' => 'M1_SUPERVISOR', 'cluster' => 'PROD', 'p1' => 36000000, 'p2' => 9000000, 'toxic' => 1200000, 'farm' => 0],
            ['code' => 'POS_PLANT_DIR', 'name' => 'Giám đốc Nhà máy Sữa', 'grade' => 'M3_DIRECTOR', 'cluster' => 'PROD', 'p1' => 85000000, 'p2' => 25000000, 'toxic' => 0, 'farm' => 0],

            // Khối trang trại
            ['code' => 'POS_FARM_MILKER', 'name' => 'Kỹ thuật viên Vắt sữa Tự động DeLaval', 'grade' => 'L1_WORKER', 'cluster' => 'FARM', 'p1' => 10000000, 'p2' => 2500000, 'toxic' => 0, 'farm' => 1000000],
            ['code' => 'POS_FARM_VET', 'name' => 'Bác sĩ Thú y Bò sữa Chuyên sâu', 'grade' => 'L3_SPECIALIST', 'cluster' => 'FARM', 'p1' => 25000000, 'p2' => 7000000, 'toxic' => 0, 'farm' => 1500000],
            ['code' => 'POS_FARM_FEED', 'name' => 'Kỹ sư Dinh dưỡng & Khẩu phần Thức ăn Bò', 'grade' => 'L2_TECHNICIAN', 'cluster' => 'FARM', 'p1' => 16000000, 'p2' => 4000000, 'toxic' => 0, 'farm' => 1200000],
            ['code' => 'POS_FARM_MGR', 'name' => 'Giám đốc Trang trại Bò sữa', 'grade' => 'M3_DIRECTOR', 'cluster' => 'FARM', 'p1' => 80000000, 'p2' => 22000000, 'toxic' => 0, 'farm' => 2000000],

            // Khối kinh doanh
            ['code' => 'POS_SALES_REP', 'name' => 'Nhân viên Bán hàng Kênh Truyền thống (GT)', 'grade' => 'L1_WORKER', 'cluster' => 'SALES', 'p1' => 9000000, 'p2' => 5000000, 'toxic' => 0, 'farm' => 0],
            ['code' => 'POS_SALES_MT', 'name' => 'Chuyên viên Phát triển Khách hàng Kênh Siêu thị (MT)', 'grade' => 'L2_TECHNICIAN', 'cluster' => 'SALES', 'p1' => 16000000, 'p2' => 6000000, 'toxic' => 0, 'farm' => 0],
            ['code' => 'POS_SALES_SUP', 'name' => 'Giám sát Bán hàng Khu vực (Area Sales Manager)', 'grade' => 'M1_SUPERVISOR', 'cluster' => 'SALES', 'p1' => 32000000, 'p2' => 12000000, 'toxic' => 0, 'farm' => 0],
            ['code' => 'POS_BRANCH_DIR', 'name' => 'Giám đốc Chi nhánh Kinh doanh', 'grade' => 'M3_DIRECTOR', 'cluster' => 'SALES', 'p1' => 75000000, 'p2' => 25000000, 'toxic' => 0, 'farm' => 0],

            // Khối văn phòng HQ
            ['code' => 'POS_OFFICE_CB', 'name' => 'Chuyên viên Đãi ngộ & Tiền lương (C&B Specialist)', 'grade' => 'L3_SPECIALIST', 'cluster' => 'OFFICE', 'p1' => 26000000, 'p2' => 6000000, 'toxic' => 0, 'farm' => 0],
            ['code' => 'POS_OFFICE_RECRUIT', 'name' => 'Chuyên viên Thu hút Nhân tài (Talent Acquisition)', 'grade' => 'L2_TECHNICIAN', 'cluster' => 'OFFICE', 'p1' => 17000000, 'p2' => 4000000, 'toxic' => 0, 'farm' => 0],
            ['code' => 'POS_OFFICE_IT', 'name' => 'Kỹ sư Phần mềm Chuyển đổi số & SAP ERP', 'grade' => 'L3_SPECIALIST', 'cluster' => 'OFFICE', 'p1' => 30000000, 'p2' => 8000000, 'toxic' => 0, 'farm' => 0],
            ['code' => 'POS_OFFICE_FIN', 'name' => 'Kế toán viên Tiền lương & Thuế Doanh nghiệp', 'grade' => 'L2_TECHNICIAN', 'cluster' => 'OFFICE', 'p1' => 18000000, 'p2' => 4500000, 'toxic' => 0, 'farm' => 0],
            ['code' => 'POS_OFFICE_RND', 'name' => 'Chuyên viên Nghiên cứu Công thức Dinh dưỡng Sữa', 'grade' => 'L3_SPECIALIST', 'cluster' => 'OFFICE', 'p1' => 28000000, 'p2' => 7000000, 'toxic' => 0, 'farm' => 0],
            ['code' => 'POS_OFFICE_MKT', 'name' => 'Brand Manager Nhãn hàng Sữa tươi Vinamilk', 'grade' => 'M1_SUPERVISOR', 'cluster' => 'OFFICE', 'p1' => 40000000, 'p2' => 10000000, 'toxic' => 0, 'farm' => 0],
            ['code' => 'POS_OFFICE_HR_DIR', 'name' => 'Giám đốc Nhân sự Tập đoàn', 'grade' => 'M3_DIRECTOR', 'cluster' => 'OFFICE', 'p1' => 95000000, 'p2' => 30000000, 'toxic' => 0, 'farm' => 0],
            ['code' => 'POS_OFFICE_CEO', 'name' => 'Tổng Giám đốc Điều hành Tập đoàn (CEO)', 'grade' => 'EXEC_BOD', 'cluster' => 'OFFICE', 'p1' => 180000000, 'p2' => 50000000, 'toxic' => 0, 'farm' => 0],
        ];

        $positions = [];
        foreach ($posDefinitions as $p) {
            $unitId = match ($p['cluster']) {
                'PROD'   => $orgUnits['FACTORIES']['NM_THONG_NHAT'],
                'FARM'   => $orgUnits['FARMS']['FARM_TAY_NINH'],
                'SALES'  => $orgUnits['BRANCHES']['CN_HA_NOI'],
                'OFFICE' => $orgUnits['HQ_DEPTS']['HQ_HR'] ?? $orgUnits['HQ'],
            };

            $id = DB::table('positions')->insertGetId([
                'position_code'    => $p['code'],
                'position_name'    => $p['name'],
                'job_grade_id'     => $gradeIds[$p['grade']],
                'org_unit_id'      => $unitId,
                'headcount_budget' => 50,
                'job_description'  => "Bản mô tả công việc vị trí {$p['name']} tại Vinamilk",
                'is_active'        => 1,
                'created_at'       => now(),
                'updated_at'       => now(),
            ]);

            $p['id'] = $id;
            $positions[$p['code']] = $p;
        }

        return $positions;
    }

    /**
     * 4. Sinh 800 hồ sơ nhân sự phân theo 4 khối:
     *    - 60% Sản xuất = 480 nhân viên
     *    - 15% Trang trại = 120 nhân viên
     *    - 15% Kinh doanh = 120 nhân viên
     *    - 10% Văn phòng = 80 nhân viên
     *    Trong đó có 60 người nghỉ việc trong năm (status = 'TERMINATED')
     */
    protected function seedEmployees(array $orgUnits, array $positions): array
    {
        $ho = ['Nguyễn', 'Trần', 'Lê', 'Phạm', 'Hoàng', 'Huỳnh', 'Phan', 'Vũ', 'Võ', 'Đặng', 'Bùi', 'Đỗ', 'Hồ', 'Ngô', 'Dương', 'Lý'];
        $demNam = ['Văn', 'Đình', 'Hữu', 'Đức', 'Quốc', 'Thanh', 'Minh', 'Ngọc', 'Công', 'Gia'];
        $demNu = ['Thị', 'Ngọc', 'Thanh', 'Mai', 'Thu', 'Hồng', 'Phương', 'Bảo', 'Kim', 'Diệu'];
        $tenNam = ['An', 'Bình', 'Cường', 'Dũng', 'Đạt', 'Hải', 'Hùng', 'Huy', 'Khoa', 'Long', 'Minh', 'Nam', 'Phong', 'Phúc', 'Quân', 'Sơn', 'Tâm', 'Thắng', 'Tùng', 'Tuấn'];
        $tenNu = ['Anh', 'Chi', 'Dung', 'Giang', 'Hà', 'Hạnh', 'Hoa', 'Hương', 'Lan', 'Linh', 'Mai', 'Nga', 'Ngân', 'Nhung', 'Oanh', 'Phương', 'Quỳnh', 'Thảo', 'Trang', 'Vân'];

        $banks = ['Ngân hàng TMCP Ngoại thương Việt Nam (Vietcombank)', 'Ngân hàng TMCP Đầu tư và Phát triển Việt Nam (BIDV)', 'Ngân hàng TMCP Công thương Việt Nam (VietinBank)'];
        $provinces = ['TP. Hồ Chí Minh', 'Hà Nội', 'Bình Dương', 'Đà Nẵng', 'Bắc Ninh', 'Thanh Hóa', 'Nghệ An', 'Tây Ninh', 'Lâm Đồng', 'Cần Thơ'];

        // Phân bổ 800 nhân sự
        // 480 Sản xuất (6 nhà máy -> 80 người/nhà máy)
        // 120 Trang trại (4 trang trại -> 30 người/trang trại)
        // 120 Kinh doanh (3 chi nhánh -> 40 người/chi nhánh)
        // 80 Văn phòng (8 phòng HQ -> 10 người/phòng)
        $targets = [];

        foreach ($orgUnits['FACTORIES'] as $fCode => $fId) {
            $targets[] = ['count' => 80, 'cluster' => 'PROD', 'unit_id' => $fId, 'code_prefix' => 'SX'];
        }
        foreach ($orgUnits['FARMS'] as $farmCode => $farmId) {
            $targets[] = ['count' => 30, 'cluster' => 'FARM', 'unit_id' => $farmId, 'code_prefix' => 'TT'];
        }
        foreach ($orgUnits['BRANCHES'] as $bCode => $bId) {
            $targets[] = ['count' => 40, 'cluster' => 'SALES', 'unit_id' => $bId, 'code_prefix' => 'KD'];
        }
        foreach ($orgUnits['HQ_DEPTS'] as $dCode => $dId) {
            $targets[] = ['count' => 10, 'cluster' => 'OFFICE', 'unit_id' => $dId, 'code_prefix' => 'VP'];
        }

        // Chọn ngẫu nhiên 60 vị trí index trong tổng số 800 sẽ là TERMINATED
        $terminatedIndices = array_flip((array)array_rand(array_fill(0, 800, true), 60));

        $employees = [];
        $empCodeCounter = 1;

        $bar = $this->output->createProgressBar(800);
        $bar->start();

        foreach ($targets as $group) {
            $cluster = $group['cluster'];
            $unitId = $group['unit_id'];

            for ($i = 0; $i < $group['count']; $i++) {
                $currentIndex = $empCodeCounter - 1;
                $isTerminated = isset($terminatedIndices[$currentIndex]);

                $isMale = rand(0, 100) > 40; // 60% nam, 40% nữ
                $gender = $isMale ? 'MALE' : 'FEMALE';
                $h = $ho[array_rand($ho)];
                $d = $isMale ? $demNam[array_rand($demNam)] : $demNu[array_rand($demNu)];
                $t = $isMale ? $tenNam[array_rand($tenNam)] : $tenNu[array_rand($tenNu)];
                $fullName = "{$h} {$d} {$t}";

                $birthYear = rand(1972, 2002);
                $birthMonth = rand(1, 12);
                $birthDay = rand(1, 28);
                $dob = sprintf('%04d-%02d-%02d', $birthYear, $birthMonth, $birthDay);

                // CCCD 12 số
                $nationalId = sprintf('079%02d%07d', $birthYear % 100, rand(1000000, 9999999));
                $empCode = sprintf('VNM-%06d', $empCodeCounter);

                // Email
                $slug = Str::slug($fullName, '');
                $companyEmail = "{$slug}.{$empCodeCounter}@vinamilk.com.vn";
                $personalEmail = "{$slug}.{$birthYear}@gmail.com";
                $phone = '09' . rand(10000000, 99999999);

                $hireDate = Carbon::create(rand(2015, 2024), rand(1, 12), rand(1, 28))->format('Y-m-d');
                $probationEndDate = Carbon::parse($hireDate)->addMonths(2)->format('Y-m-d');

                $status = 'ACTIVE';
                $terminationDate = null;

                if ($isTerminated) {
                    $status = 'TERMINATED';
                    $termMonth = rand(2, 11);
                    $terminationDate = sprintf('2026-%02d-%02d', $termMonth, rand(15, 28));
                } elseif ($empCodeCounter > 770) {
                    $status = 'PROBATION';
                    $hireDate = '2026-11-01';
                    $probationEndDate = '2027-01-01';
                }

                $bankName = $banks[array_rand($banks)];
                $bankAccount = rand(1000000000, 9999999999);
                $province = $provinces[array_rand($provinces)];

                $empId = DB::table('employees')->insertGetId([
                    'employee_code'            => $empCode,
                    'full_name'                => $fullName,
                    'gender'                   => $gender,
                    'date_of_birth'            => $dob,
                    'national_id'              => $nationalId,
                    'national_id_issued_date'  => '2021-05-10',
                    'national_id_issued_place' => 'Cục Cảnh sát QLHC về TTXH',
                    'personal_email'           => $personalEmail,
                    'company_email'            => $companyEmail,
                    'phone_number'             => $phone,
                    'permanent_address'        => "Số " . rand(1, 500) . ", Đường Số " . rand(1, 50) . ", {$province}",
                    'current_address'          => "Số " . rand(1, 500) . ", Phường Tân Phú, Quận 7, TP.HCM",
                    'tax_id'                   => '8' . rand(100000000, 999999999),
                    'bank_account_number'      => (string)$bankAccount,
                    'bank_name'                => $bankName,
                    'bank_branch'              => 'Chi nhánh Sở Giao dịch',
                    'social_insurance_number'  => '79' . rand(10000000, 99999999),
                    'hire_date'                => $hireDate,
                    'probation_end_date'       => $probationEndDate,
                    'termination_date'         => $terminationDate,
                    'employment_status'        => $status,
                    'avatar_url'               => "https://i.pravatar.cc/150?u={$empCode}",
                    'created_at'               => now(),
                    'updated_at'               => now(),
                ]);

                // Chọn vị trí chức danh phù hợp khối
                $posKey = match ($cluster) {
                    'PROD'   => ($i === 0) ? 'POS_PLANT_DIR' : (($i < 5) ? 'POS_PROD_SUPERVISOR' : (($i < 15) ? 'POS_PROD_TETRA' : (($i < 25) ? 'POS_PROD_QAQC' : 'POS_PROD_OPERATOR'))),
                    'FARM'   => ($i === 0) ? 'POS_FARM_MGR' : (($i < 4) ? 'POS_FARM_VET' : (($i < 10) ? 'POS_FARM_FEED' : 'POS_FARM_MILKER')),
                    'SALES'  => ($i === 0) ? 'POS_BRANCH_DIR' : (($i < 5) ? 'POS_SALES_SUP' : (($i < 20) ? 'POS_SALES_MT' : 'POS_SALES_REP')),
                    'OFFICE' => match ($group['unit_id']) {
                        $orgUnits['HQ_DEPTS']['HQ_BOD']   => ($i === 0) ? 'POS_OFFICE_CEO' : 'POS_OFFICE_FIN',
                        $orgUnits['HQ_DEPTS']['HQ_HR']    => ($i === 0) ? 'POS_OFFICE_HR_DIR' : (($i < 5) ? 'POS_OFFICE_CB' : 'POS_OFFICE_RECRUIT'),
                        $orgUnits['HQ_DEPTS']['HQ_IT']    => 'POS_OFFICE_IT',
                        $orgUnits['HQ_DEPTS']['HQ_RND']   => 'POS_OFFICE_RND',
                        $orgUnits['HQ_DEPTS']['HQ_MKT']   => 'POS_OFFICE_MKT',
                        default                           => 'POS_OFFICE_FIN',
                    },
                };

                $pos = $positions[$posKey];

                // Gán điều chuyển công tác (Assignment)
                DB::table('employee_assignments')->insert([
                    'employee_id'     => $empId,
                    'org_unit_id'     => $unitId,
                    'position_id'     => $pos['id'],
                    'is_primary'      => 1,
                    'assignment_type' => 'INITIAL',
                    'decision_number' => "QĐ-TD-{$birthYear}/VNM",
                    'effective_from'  => $hireDate,
                    'effective_to'    => $terminationDate,
                    'is_current'      => $isTerminated ? 0 : 1,
                    'notes'           => 'Tuyển dụng và bổ nhiệm chính thức',
                    'created_at'      => now(),
                    'updated_at'      => now(),
                ]);

                // Xếp lương 3P
                $p1 = $pos['p1'] + (rand(-1, 2) * 500000);
                $p2 = $pos['p2'];
                $toxic = $pos['toxic'];
                $farm = $pos['farm'];
                $insurance = min($p1 + $p2, 46800000); // Trần 46.8tr

                DB::table('employee_salaries')->insert([
                    'employee_id'              => $empId,
                    'p1_position_salary'       => $p1,
                    'p2_competency_salary'     => $p2,
                    'insurance_salary'         => $insurance,
                    'toxic_allowance'          => $toxic,
                    'farm_field_allowance'     => $farm,
                    'responsibility_allowance' => (str_contains($pos['code'], 'DIR') || str_contains($pos['code'], 'MGR') || str_contains($pos['code'], 'SUPERVISOR')) ? 3000000 : 0,
                    'meal_allowance'           => 730000,
                    'decision_number'          => "QĐ-XL-{$birthYear}/VNM",
                    'effective_from'           => $hireDate,
                    'effective_to'             => $terminationDate,
                    'is_current'               => $isTerminated ? 0 : 1,
                    'created_at'               => now(),
                    'updated_at'               => now(),
                ]);

                // Hợp đồng lao động
                DB::table('contracts')->insert([
                    'contract_code'    => "HĐLĐ-VNM/{$empCodeCounter}",
                    'employee_id'      => $empId,
                    'contract_type'    => ($status === 'PROBATION') ? 'PROBATION' : 'INDEFINITE_TERM',
                    'contract_status'  => ($status === 'TERMINATED') ? 'TERMINATED' : 'ACTIVE',
                    'signed_date'      => $hireDate,
                    'start_date'       => $hireDate,
                    'end_date'         => ($status === 'PROBATION') ? $probationEndDate : null,
                    'base_salary'      => $p1 + $p2,
                    'working_location' => $province,
                    'effective_from'   => $hireDate,
                    'effective_to'     => $terminationDate,
                    'is_current'       => $isTerminated ? 0 : 1,
                    'created_at'       => now(),
                    'updated_at'       => now(),
                ]);

                // Quỹ phép năm 2026
                DB::table('leave_balances')->insert([
                    'employee_id'          => $empId,
                    'year'                 => 2026,
                    'annual_leave_quota'   => 12.0,
                    'seniority_leave_days' => max(0, min(5, floor((2026 - Carbon::parse($hireDate)->year) / 5))),
                    'transferred_days'     => rand(0, 3),
                    'used_days'            => rand(2, 8),
                    'created_at'           => now(),
                    'updated_at'           => now(),
                ]);

                $employees[] = [
                    'id'               => $empId,
                    'code'             => $empCode,
                    'name'             => $fullName,
                    'cluster'          => $cluster,
                    'unit_id'          => $unitId,
                    'position_id'      => $pos['id'],
                    'position_code'    => $pos['code'],
                    'gender'           => $gender,
                    'dob'              => $dob,
                    'hire_date'        => $hireDate,
                    'status'           => $status,
                    'termination_date' => $terminationDate,
                    'p1'               => $p1,
                    'p2'               => $p2,
                ];

                $empCodeCounter++;
                $bar->advance();
            }
        }

        $bar->finish();
        $this->newLine();

        return $employees;
    }

    /**
     * 5. Sinh dữ liệu chấm công 12 tháng 2026
     *    - ~3% vắng mặt (nghỉ phép, nghỉ ốm, không phép)
     *    - OT hợp lý tuân thủ Bộ luật Lao động
     *    - Sinh lịch tuần, nhật ký log FaceID/vân tay và bảng công ngày
     */
    protected function seedAttendanceData(array $employees): void
    {
        $shiftIds = DB::table('shifts')->pluck('id', 'shift_code')->toArray();

        // Lấy danh sách nhân viên ACTIVE (hoặc làm việc trong giai đoạn đó)
        // Để tối ưu tốc độ sinh 12 tháng x 800 nhân sự (~250.000 bản ghi),
        // thực hiện bulk insert theo từng tháng
        $this->info("   -> Đang tạo bảng công 12 tháng cho toàn bộ nhân sự...");

        // Chọn các ngày lễ 2026 tại VN
        $holidays = [
            '2026-01-01', // Tết Dương Lịch
            '2026-02-16', '2026-02-17', '2026-02-18', '2026-02-19', '2026-02-20', // Tết Nguyên Đán
            '2026-04-26', // Giỗ Tổ Hùng Vương
            '2026-04-30', // Giải phóng Miền Nam
            '2026-05-01', // Quốc tế Lao động
            '2026-09-02', '2026-09-03', // Quốc khánh
        ];
        $holidaySet = array_flip($holidays);

        $bar = $this->output->createProgressBar(12);
        $bar->start();

        for ($month = 1; $month <= 12; $month++) {
            $startDate = Carbon::create(2026, $month, 1);
            $daysInMonth = $startDate->daysInMonth;

            $attDaysBatch = [];

            foreach ($employees as $emp) {
                // Kiểm tra nếu nhân viên đã nghỉ việc trước tháng này
                if ($emp['status'] === 'TERMINATED' && $emp['termination_date']) {
                    if (Carbon::parse($emp['termination_date'])->lt($startDate)) {
                        continue;
                    }
                }

                // Chọn ca mặc định theo khối
                $defaultShiftCode = match ($emp['cluster']) {
                    'PROD'   => 'CA_1_SX',
                    'FARM'   => 'CA_SOM_FARM',
                    'SALES'  => 'CA_RETAIL_1',
                    'OFFICE' => 'CA_HC',
                };
                $shiftId = $shiftIds[$defaultShiftCode] ?? 1;

                // Chọn một nhóm công nhân luân phiên ca đêm (Ca 3)
                $isNightShiftWorker = ($emp['cluster'] === 'PROD' && ($emp['id'] % 3 === 0));
                if ($isNightShiftWorker) {
                    $shiftId = $shiftIds['CA_3_DEM'] ?? $shiftId;
                }

                for ($day = 1; $day <= $daysInMonth; $day++) {
                    $currentDate = sprintf('2026-%02d-%02d', $month, $day);
                    $carbonDate = Carbon::parse($currentDate);
                    $isWeekend = $carbonDate->isWeekend();
                    $isHoliday = isset($holidaySet[$currentDate]);

                    // Kiểm tra ngày nghỉ việc trong tháng
                    if ($emp['status'] === 'TERMINATED' && $emp['termination_date']) {
                        if ($carbonDate->gt(Carbon::parse($emp['termination_date']))) {
                            continue;
                        }
                    }

                    // Cuối tuần
                    if ($isWeekend) {
                        // 8% công nhân sản xuất/trang trại có làm OT ngày nghỉ tuần
                        if (($emp['cluster'] === 'PROD' || $emp['cluster'] === 'FARM') && rand(1, 100) <= 8) {
                            $attDaysBatch[] = [
                                'employee_id'      => $emp['id'],
                                'work_date'        => $currentDate,
                                'shift_id'         => $shiftId,
                                'first_in'         => "{$currentDate} 08:00:00",
                                'last_out'         => "{$currentDate} 16:30:00",
                                'late_minutes'     => 0,
                                'early_minutes'    => 0,
                                'actual_hours'     => 8.00,
                                'standard_hours'   => 0.00,
                                'ot_normal_hours'  => 0.00,
                                'ot_weekend_hours' => 8.00,
                                'ot_holiday_hours' => 0.00,
                                'ot_night_hours'   => 0.00,
                                'status'           => 'PRESENT',
                                'created_at'       => now(),
                                'updated_at'       => now(),
                            ];
                        } else {
                            $attDaysBatch[] = [
                                'employee_id'      => $emp['id'],
                                'work_date'        => $currentDate,
                                'shift_id'         => null,
                                'first_in'         => null,
                                'last_out'         => null,
                                'late_minutes'     => 0,
                                'early_minutes'    => 0,
                                'actual_hours'     => 0.00,
                                'standard_hours'   => 0.00,
                                'ot_normal_hours'  => 0.00,
                                'ot_weekend_hours' => 0.00,
                                'ot_holiday_hours' => 0.00,
                                'ot_night_hours'   => 0.00,
                                'status'           => 'WEEKEND_OFF',
                                'created_at'       => now(),
                                'updated_at'       => now(),
                            ];
                        }
                        continue;
                    }

                    // Ngày Lễ
                    if ($isHoliday) {
                        $attDaysBatch[] = [
                            'employee_id'      => $emp['id'],
                            'work_date'        => $currentDate,
                            'shift_id'         => null,
                            'first_in'         => null,
                            'last_out'         => null,
                            'late_minutes'     => 0,
                            'early_minutes'    => 0,
                            'actual_hours'     => 0.00,
                            'standard_hours'   => 8.00,
                            'ot_normal_hours'  => 0.00,
                            'ot_weekend_hours' => 0.00,
                            'ot_holiday_hours' => 0.00,
                            'ot_night_hours'   => 0.00,
                            'status'           => 'HOLIDAY',
                            'created_at'       => now(),
                            'updated_at'       => now(),
                        ];
                        continue;
                    }

                    // Ngày làm việc bình thường: Tỷ lệ vắng mặt ~3%
                    $randAttendance = rand(1, 1000);

                    if ($randAttendance <= 15) {
                        // 1.5% Nghỉ phép năm
                        $attDaysBatch[] = [
                            'employee_id'      => $emp['id'],
                            'work_date'        => $currentDate,
                            'shift_id'         => $shiftId,
                            'first_in'         => null,
                            'last_out'         => null,
                            'late_minutes'     => 0,
                            'early_minutes'    => 0,
                            'actual_hours'     => 0.00,
                            'standard_hours'   => 8.00,
                            'ot_normal_hours'  => 0.00,
                            'ot_weekend_hours' => 0.00,
                            'ot_holiday_hours' => 0.00,
                            'ot_night_hours'   => 0.00,
                            'status'           => 'LEAVE_PAID',
                            'created_at'       => now(),
                            'updated_at'       => now(),
                        ];
                    } elseif ($randAttendance <= 23) {
                        // 0.8% Nghỉ ốm BHXH / việc riêng
                        $attDaysBatch[] = [
                            'employee_id'      => $emp['id'],
                            'work_date'        => $currentDate,
                            'shift_id'         => $shiftId,
                            'first_in'         => null,
                            'last_out'         => null,
                            'late_minutes'     => 0,
                            'early_minutes'    => 0,
                            'actual_hours'     => 0.00,
                            'standard_hours'   => 8.00,
                            'ot_normal_hours'  => 0.00,
                            'ot_weekend_hours' => 0.00,
                            'ot_holiday_hours' => 0.00,
                            'ot_night_hours'   => 0.00,
                            'status'           => 'LEAVE_UNPAID',
                            'created_at'       => now(),
                            'updated_at'       => now(),
                        ];
                    } elseif ($randAttendance <= 30) {
                        // 0.7% Vắng mặt không phép (AWOL)
                        $attDaysBatch[] = [
                            'employee_id'      => $emp['id'],
                            'work_date'        => $currentDate,
                            'shift_id'         => $shiftId,
                            'first_in'         => null,
                            'last_out'         => null,
                            'late_minutes'     => 0,
                            'early_minutes'    => 0,
                            'actual_hours'     => 0.00,
                            'standard_hours'   => 8.00,
                            'ot_normal_hours'  => 0.00,
                            'ot_weekend_hours' => 0.00,
                            'ot_holiday_hours' => 0.00,
                            'ot_night_hours'   => 0.00,
                            'status'           => 'ABSENT',
                            'created_at'       => now(),
                            'updated_at'       => now(),
                        ];
                    } else {
                        // 97% Có mặt làm việc
                        $isLate = ($randAttendance <= 60); // 3% đi muộn một chút
                        $lateMinutes = $isLate ? rand(5, 25) : 0;

                        // OT ngày thường: 15% công nhân nhà máy tăng ca 1.5 - 2h
                        $hasOt = ($emp['cluster'] === 'PROD' && rand(1, 100) <= 15);
                        $otHours = $hasOt ? (rand(1, 2) === 1 ? 1.50 : 2.00) : 0.00;

                        $attDaysBatch[] = [
                            'employee_id'      => $emp['id'],
                            'work_date'        => $currentDate,
                            'shift_id'         => $shiftId,
                            'first_in'         => "{$currentDate} " . ($isLate ? '08:15:00' : '07:55:00'),
                            'last_out'         => "{$currentDate} " . ($hasOt ? '19:00:00' : '17:05:00'),
                            'late_minutes'     => $lateMinutes,
                            'early_minutes'    => 0,
                            'actual_hours'     => 8.00 + $otHours,
                            'standard_hours'   => 8.00,
                            'ot_normal_hours'  => $otHours,
                            'ot_weekend_hours' => 0.00,
                            'ot_holiday_hours' => 0.00,
                            'ot_night_hours'   => $isNightShiftWorker ? 8.00 : 0.00,
                            'status'           => $isLate ? 'LATE' : 'PRESENT',
                            'created_at'       => now(),
                            'updated_at'       => now(),
                        ];
                    }
                }

                // Bulk insert theo lô 2.000 dòng để tiết kiệm RAM
                if (count($attDaysBatch) >= 2000) {
                    DB::table('attendance_days')->insert($attDaysBatch);
                    $attDaysBatch = [];
                }
            }

            if (!empty($attDaysBatch)) {
                DB::table('attendance_days')->insert($attDaysBatch);
                $attDaysBatch = [];
            }

            $bar->advance();
        }

        $bar->finish();
        $this->newLine();
    }

    /**
     * 6. Sinh 2 kỳ KPI (H1-2026 & H2-2026), nhiệm vụ và điểm xuất sang lương P3
     */
    protected function seedKpiData(array $employees): void
    {
        // 6.1 Tạo 2 kỳ KPI
        $kpiPeriods = [
            [
                'period_code' => 'KPI-2026-H1',
                'period_name' => 'Kỳ Đánh giá Hiệu suất 6 Tháng Đầu Năm 2026',
                'period_type' => 'YEAR',
                'start_date'  => '2026-01-01',
                'end_date'    => '2026-06-30',
                'status'      => 'CLOSED',
            ],
            [
                'period_code' => 'KPI-2026-H2',
                'period_name' => 'Kỳ Đánh giá Hiệu suất 6 Tháng Cuối Năm 2026',
                'period_type' => 'YEAR',
                'start_date'  => '2026-07-01',
                'end_date'    => '2026-12-31',
                'status'      => 'EVALUATING',
            ],
        ];

        $periodIds = [];
        foreach ($kpiPeriods as $kp) {
            $id = DB::table('kpi_periods')->insertGetId(array_merge($kp, ['created_at' => now(), 'updated_at' => now()]));
            $periodIds[$kp['period_code']] = $id;
        }

        // Cấu hình tính điểm KPI
        DB::table('kpi_scoring_configs')->updateOrInsert(['config_key' => 'EARLY_BONUS_PER_DAY'], ['config_value' => 2.00, 'description' => '+2đ/ngày sớm', 'created_at' => now(), 'updated_at' => now()]);
        DB::table('kpi_scoring_configs')->updateOrInsert(['config_key' => 'LATE_PENALTY_PER_DAY'], ['config_value' => 5.00, 'description' => '-5đ/ngày trễ', 'created_at' => now(), 'updated_at' => now()]);
        DB::table('kpi_scoring_configs')->updateOrInsert(['config_key' => 'BASELINE_SCORE'], ['config_value' => 100.00, 'description' => 'Chuẩn 100đ', 'created_at' => now(), 'updated_at' => now()]);

        // Danh mục mục tiêu theo khối
        $tasksByCluster = [
            'PROD' => [
                ['title' => 'Tỷ lệ thu hồi sữa tươi tiệt trùng trong dây chuyền', 'metric' => 'Tỷ lệ đạt >= 99.2%', 'weight' => 40.00],
                ['title' => 'Kiểm soát tỷ lệ vi phạm an toàn thực phẩm & vi sinh', 'metric' => 'Số vụ vi phạm = 0', 'weight' => 30.00],
                ['title' => 'Hiệu suất vận hành dây chuyền Tetra Pak (OEE)', 'metric' => 'OEE >= 88%', 'weight' => 30.00],
            ],
            'FARM' => [
                ['title' => 'Năng suất bình quân đàn bò sữa vắt (kg sữa/con/ngày)', 'metric' => 'Đạt >= 32.5 kg/con/ngày', 'weight' => 40.00],
                ['title' => 'Kiểm soát tỷ lệ tế bào soma (SCC) đạt chuẩn sữa tươi sạch', 'metric' => 'SCC <= 250.000 tế bào/ml', 'weight' => 35.00],
                ['title' => 'Tỷ lệ nuôi sống bê cái thay thế đạt chuẩn', 'metric' => 'Tỷ lệ sống >= 98%', 'weight' => 25.00],
            ],
            'SALES' => [
                ['title' => 'Hoàn thành chỉ tiêu doanh thu sữa chua & sữa bột', 'metric' => 'Đạt >= 102% kế hoạch vùng', 'weight' => 50.00],
                ['title' => 'Độ phủ điểm bán lẻ và phát triển đại lý mới', 'metric' => 'Mở mới >= 15 điểm bán/tháng', 'weight' => 30.00],
                ['title' => 'Kiểm soát công nợ đại lý không quá hạn 30 ngày', 'metric' => 'Tỷ lệ nợ xấu = 0%', 'weight' => 20.00],
            ],
            'OFFICE' => [
                ['title' => 'Hoàn thành báo cáo phân tích & số hóa quy trình HRIS/ERP', 'metric' => 'Đúng hạn 100% không trễ SLA', 'weight' => 40.00],
                ['title' => 'Đảm bảo tính chính xác và bảo mật tính lương & thuế TNCN', 'metric' => 'Sai sót = 0%', 'weight' => 35.00],
                ['title' => 'Chỉ số hài lòng nội bộ của các đơn vị thành viên (CSAT)', 'metric' => 'CSAT >= 95%', 'weight' => 25.00],
            ],
        ];

        // Lấy ID một người quản lý làm evaluator (SoD: Evaluator != Employee)
        $hrDirectorId = $employees[array_key_last($employees)]['id'];

        foreach ($periodIds as $pCode => $pId) {
            $assignmentsBatch = [];
            $summariesBatch = [];

            foreach ($employees as $emp) {
                if ($emp['status'] === 'TERMINATED' && $pCode === 'KPI-2026-H2') {
                    // Người đã thôi việc không đánh giá H2
                    continue;
                }

                $evaluatorId = ($emp['id'] === $hrDirectorId) ? $employees[0]['id'] : $hrDirectorId;
                $clusterTasks = $tasksByCluster[$emp['cluster']];

                $totalPeriodScore = 0.0;

                foreach ($clusterTasks as $t) {
                    $timeScore = rand(-1, 2) * 2.0; // Thưởng sớm hoặc phạt trễ
                    $managerAdj = (rand(1, 10) === 10) ? 5.0 : 0.0;
                    $finalScore = max(0.0, min(120.0, 100.0 + $timeScore + $managerAdj));
                    $totalPeriodScore += ($finalScore * ($t['weight'] / 100.0));

                    $assignmentsBatch[] = [
                        'kpi_period_id'      => $pId,
                        'employee_id'        => $emp['id'],
                        'evaluator_id'       => $evaluatorId,
                        'title'              => $t['title'],
                        'description'        => "Chỉ tiêu KPI đánh giá cho kỳ {$pCode}",
                        'target_metric'      => $t['metric'],
                        'weight_percentage'  => $t['weight'],
                        'due_date'           => ($pCode === 'KPI-2026-H1') ? '2026-06-25' : '2026-12-25',
                        'baseline_score'     => 100.00,
                        'time_score'         => $timeScore,
                        'manager_adjustment' => $managerAdj,
                        'adjustment_reason'  => ($managerAdj != 0) ? 'Xuất sắc vượt tiến độ yêu cầu' : null,
                        'final_score'        => $finalScore,
                        'actual_result'      => 'Đã hoàn thành xuất sắc các tiêu chí định lượng',
                        'completed_date'     => ($pCode === 'KPI-2026-H1') ? '2026-06-20' : '2026-12-20',
                        'status'             => 'Approved',
                        'created_at'         => now(),
                        'updated_at'         => now(),
                    ];
                }

                // Xếp loại KPI & Hệ số P3
                $grade = 'B';
                $p3Multiplier = 1.00;
                if ($totalPeriodScore >= 110) {
                    $grade = 'A+';
                    $p3Multiplier = 1.25;
                } elseif ($totalPeriodScore >= 100) {
                    $grade = 'A';
                    $p3Multiplier = 1.15;
                } elseif ($totalPeriodScore >= 85) {
                    $grade = 'B';
                    $p3Multiplier = 1.00;
                } elseif ($totalPeriodScore >= 70) {
                    $grade = 'C';
                    $p3Multiplier = 0.80;
                } else {
                    $grade = 'D';
                    $p3Multiplier = 0.00;
                }

                $summariesBatch[] = [
                    'kpi_period_id'          => $pId,
                    'employee_id'            => $emp['id'],
                    'total_weight_percentage' => 100.00,
                    'kpi_score'              => round($totalPeriodScore, 2),
                    'kpi_grade'              => $grade,
                    'p3_multiplier'          => $p3Multiplier,
                    'status'                 => 'APPROVED',
                    'exported_at'            => ($pCode === 'KPI-2026-H1') ? '2026-07-02 08:00:00' : null,
                    'created_at'             => now(),
                    'updated_at'             => now(),
                ];

                if (count($assignmentsBatch) >= 1500) {
                    DB::table('kpi_assignments')->insert($assignmentsBatch);
                    $assignmentsBatch = [];
                }
            }

            if (!empty($assignmentsBatch)) {
                DB::table('kpi_assignments')->insert($assignmentsBatch);
            }
            if (!empty($summariesBatch)) {
                DB::table('kpi_employee_summaries')->insert($summariesBatch);
            }
        }
    }

    /**
     * 7. Sinh 40 tờ trình tăng lương ở nhiều trạng thái (DRAFT, SUBMITTED, PENDING_STEP, APPROVED, REJECTED, CANCELLED)
     */
    protected function seedSalaryProposals(array $employees): void
    {
        // Danh sách trạng thái phân bổ
        $statuses = [
            'APPROVED', 'APPROVED', 'APPROVED', 'APPROVED', 'APPROVED', 'APPROVED', 'APPROVED', 'APPROVED', 'APPROVED', 'APPROVED', 'APPROVED', 'APPROVED', // 12 APPROVED
            'PENDING_STEP', 'PENDING_STEP', 'PENDING_STEP', 'PENDING_STEP', 'PENDING_STEP', 'PENDING_STEP', 'PENDING_STEP', 'PENDING_STEP', 'PENDING_STEP', 'PENDING_STEP', 'PENDING_STEP', 'PENDING_STEP', // 12 PENDING_STEP
            'SUBMITTED', 'SUBMITTED', 'SUBMITTED', 'SUBMITTED', 'SUBMITTED', 'SUBMITTED', // 6 SUBMITTED
            'DRAFT', 'DRAFT', 'DRAFT', 'DRAFT', // 4 DRAFT
            'REJECTED', 'REJECTED', 'REJECTED', 'REJECTED', // 4 REJECTED
            'CANCELLED', 'CANCELLED', // 2 CANCELLED
        ];

        // 4 loại đề xuất
        $proposalTypes = ['PERIODIC', 'DEPARTMENT_RECOMMENDED', 'SPECIAL_EXCEPTION', 'MASS_STATUTORY'];

        // Chọn 40 nhân viên ngẫu nhiên (không chọn terminated)
        $activeEmployees = array_values(array_filter($employees, fn($e) => $e['status'] === 'ACTIVE'));
        $selectedKeys = (array)array_rand($activeEmployees, 40);

        $creatorId = $employees[1]['id']; // Trưởng phòng

        for ($i = 0; $i < 40; $i++) {
            $emp = $activeEmployees[$selectedKeys[$i]];
            $status = $statuses[$i];
            $type = $proposalTypes[$i % 4];

            $code = sprintf('SAL-PROP-2026-%03d', $i + 1);

            $currentP1 = $emp['p1'];
            $proposedP1 = $currentP1 + (rand(1, 4) * 1000000);
            $currentP2 = $emp['p2'];
            $proposedP2 = $currentP2 + (rand(0, 2) * 500000);

            $isSpecial = ($type === 'SPECIAL_EXCEPTION');
            $isOutOfBand = ($proposedP1 > 80000000);

            $workflowType = $isSpecial || $isOutOfBand ? 'OUT_OF_BAND_OR_EXCEPTION' : 'DEFAULT';

            $propId = DB::table('salary_proposals')->insertGetId([
                'proposal_code'              => $code,
                'employee_id'                => $emp['id'],
                'proposal_type'              => $type,
                'current_p1'                 => $currentP1,
                'proposed_p1'                => $proposedP1,
                'current_p2'                 => $currentP2,
                'proposed_p2'                => $proposedP2,
                'current_insurance_salary'   => min($currentP1 + $currentP2, 46800000),
                'proposed_insurance_salary'  => min($proposedP1 + $proposedP2, 46800000),
                'salary_band_id'             => 1,
                'band_min'                   => 10000000,
                'band_midpoint'              => 20000000,
                'band_max'                   => 35000000,
                'current_compa_ratio'        => round(($currentP1 / 20000000) * 100, 2),
                'proposed_compa_ratio'       => round(($proposedP1 / 20000000) * 100, 2),
                'kpi_history_json'           => json_encode(['H1_2025' => 102.5, 'H2_2025' => 108.0]),
                'last_salary_increase_date'  => '2025-04-01',
                'months_since_last_increase' => 12,
                'is_out_of_band'             => $isOutOfBand,
                'is_special_exception'       => $isSpecial,
                'is_senior_management'       => false,
                'exception_reason'           => $isSpecial ? 'Thành tích sáng kiến kỹ thuật tiết kiệm 2.5 tỷ VNĐ chi phí bao bì Tetra Pak' : null,
                'attachment_url'             => $isSpecial ? 'https://storage.vinamilk.com.vn/docs/sang-kien-ky-thuat-2026.pdf' : null,
                'warnings_json'              => json_encode($isSpecial ? ['Cảnh báo đặc cách: Chưa đủ 12 tháng từ lần tăng trước'] : []),
                'effective_from'             => '2026-07-01',
                'reason'                     => "Đề xuất tăng lương định kỳ & ghi nhận năng lực đóng góp năm 2026",
                'status'                     => $status,
                'current_step_number'        => ($status === 'APPROVED') ? 2 : (($status === 'PENDING_STEP') ? 1 : 1),
                'current_workflow_type'      => $workflowType,
                'decision_number'            => ($status === 'APPROVED') ? "QĐ-TL-2026/{$code}" : null,
                'retroactive_months'         => ($status === 'APPROVED') ? 2 : 0,
                'retroactive_amount'         => ($status === 'APPROVED') ? (($proposedP1 - $currentP1) * 2) : 0,
                'created_by'                 => $creatorId,
                'approved_at'                => ($status === 'APPROVED') ? '2026-07-05 14:30:00' : null,
                'approved_by'                => ($status === 'APPROVED') ? $employees[0]['id'] : null,
                'created_at'                 => now(),
                'updated_at'                 => now(),
            ]);

            // Sinh các bước duyệt (approval_steps)
            DB::table('approval_steps')->insert([
                'proposal_id'   => $propId,
                'step_number'   => 1,
                'approver_role' => 'DIVISION_DIRECTOR',
                'approver_id'   => $employees[2]['id'],
                'action'        => ($status === 'APPROVED' || $status === 'PENDING_STEP') ? 'APPROVED' : (($status === 'REJECTED') ? 'REJECTED' : 'PENDING'),
                'comments'      => ($status === 'REJECTED') ? 'Ngân sách tăng lương đơn vị đã vượt trần năm 2026' : 'Đồng ý tờ trình cấp khối',
                'action_at'     => now(),
                'created_at'    => now(),
                'updated_at'    => now(),
            ]);

            if ($status === 'APPROVED') {
                DB::table('approval_steps')->insert([
                    'proposal_id'   => $propId,
                    'step_number'   => 2,
                    'approver_role' => 'HR_DIRECTOR',
                    'approver_id'   => $employees[0]['id'],
                    'action'        => 'APPROVED',
                    'comments'      => 'Giám đốc Nhân sự Tập đoàn ký duyệt quyết định',
                    'action_at'     => now(),
                    'created_at'    => now(),
                    'updated_at'    => now(),
                ]);
            }
        }
    }

    /**
     * 8. Sinh 150 ứng viên tuyển dụng qua các giai đoạn Kanban ATS
     */
    protected function seedRecruitmentData(array $orgUnits, array $positions, array $employees): array
    {
        // 8.1 Tạo 4 phiếu tuyển dụng (Requisitions)
        $reqs = [
            ['code' => 'REQ-2026-01', 'title' => 'Tuyển dụng Kỹ sư Tiệt trùng Tetra Pak Ca 3', 'pos' => 'POS_PROD_TETRA', 'unit' => $orgUnits['FACTORIES']['NM_THONG_NHAT'], 'count' => 15],
            ['code' => 'REQ-2026-02', 'title' => 'Tuyển dụng Bác sĩ Thú y Bò sữa Trang trại', 'pos' => 'POS_FARM_VET', 'unit' => $orgUnits['FARMS']['FARM_TAY_NINH'], 'count' => 10],
            ['code' => 'REQ-2026-03', 'title' => 'Tuyển dụng Nhân viên Kinh doanh Kênh MT Siêu thị', 'pos' => 'POS_SALES_MT', 'unit' => $orgUnits['BRANCHES']['CN_HA_NOI'], 'count' => 20],
            ['code' => 'REQ-2026-04', 'title' => 'Tuyển dụng Chuyên viên R&D Sữa chua Dinh dưỡng', 'pos' => 'POS_OFFICE_RND', 'unit' => $orgUnits['HQ_DEPTS']['HQ_RND'], 'count' => 5],
        ];

        $reqIds = [];
        foreach ($reqs as $r) {
            $id = DB::table('requisitions')->insertGetId([
                'requisition_code'   => $r['code'],
                'title'              => $r['title'],
                'org_unit_id'        => $r['unit'],
                'position_id'        => $positions[$r['pos']]['id'],
                'headcount'          => $r['count'],
                'job_description'    => "Mô tả công việc vị trí {$r['title']} tại Vinamilk",
                'requirements'       => 'Tốt nghiệp Đại học/Cao đẳng chuyên ngành liên quan, trung thực, nhiệt huyết.',
                'budget_salary_from' => 15000000,
                'budget_salary_to'   => 30000000,
                'deadline'           => '2026-12-31',
                'status'             => 'PUBLISHED',
                'created_by'         => $employees[0]['id'],
                'created_at'         => now(),
                'updated_at'         => now(),
            ]);
            $reqIds[] = ['id' => $id, 'pos_id' => $positions[$r['pos']]['id']];
        }

        // 8.2 150 Ứng viên phân bổ theo các giai đoạn Kanban ATS
        // NEW (30), SCREENING (30), INTERVIEWING (35), HEALTH_CHECK (15), OFFERED (15), HIRED (15), REJECTED (10)
        $stages = array_merge(
            array_fill(0, 30, 'NEW'),
            array_fill(0, 30, 'SCREENING'),
            array_fill(0, 35, 'INTERVIEWING'),
            array_fill(0, 15, 'HEALTH_CHECK'),
            array_fill(0, 15, 'OFFERED'),
            array_fill(0, 15, 'HIRED'),
            array_fill(0, 10, 'REJECTED')
        );

        $ho = ['Trần', 'Nguyễn', 'Lê', 'Vũ', 'Đặng', 'Bùi', 'Hoàng', 'Phạm'];
        $dem = ['Thị', 'Văn', 'Đức', 'Quốc', 'Thanh', 'Mai', 'Ngọc', 'Hữu'];
        $ten = ['Hương', 'Dũng', 'Khánh', 'Lâm', 'Oanh', 'Thịnh', 'Thảo', 'Trí', 'Phúc', 'Nga'];

        for ($i = 0; $i < 150; $i++) {
            $fullName = $ho[array_rand($ho)] . ' ' . $dem[array_rand($dem)] . ' ' . $ten[array_rand($ten)];
            $code = sprintf('CAN-2026-%04d', $i + 1);
            $email = Str::slug($fullName, '') . ($i + 1) . '@gmail.com';
            $phone = '09' . rand(20000000, 89999999);
            $stage = $stages[$i];

            $req = $reqIds[$i % count($reqIds)];

            $candId = DB::table('candidates')->insertGetId([
                'candidate_code'  => $code,
                'full_name'       => $fullName,
                'email'           => $email,
                'phone_number'    => $phone,
                'national_id'     => sprintf('079%09d', rand(100000000, 999999999)),
                'gender'          => (rand(0, 1) === 1) ? 'MALE' : 'FEMALE',
                'education_level' => 'Đại học Bách Khoa / Nông Lâm',
                'cv_file_url'     => "https://storage.vinamilk.com.vn/cv/{$code}.pdf",
                'created_at'      => now(),
                'updated_at'      => now(),
            ]);

            $appStatus = match ($stage) {
                'NEW'          => 'NEW',
                'SCREENING'    => 'SCREENING',
                'INTERVIEWING' => 'INTERVIEWING',
                'HEALTH_CHECK' => 'INTERVIEWING',
                'OFFERED'      => 'OFFERED',
                'HIRED'        => 'HIRED',
                'REJECTED'     => 'REJECTED',
            };

            $appId = DB::table('applications')->insertGetId([
                'candidate_id'   => $candId,
                'requisition_id' => $req['id'],
                'source'         => 'Cổng Nghề nghiệp Vinamilk Careers',
                'applied_date'   => '2026-09-01',
                'status'         => $appStatus,
                'notes'          => "Hồ sơ ứng tuyển giai đoạn {$stage}",
                'created_at'     => now(),
                'updated_at'     => now(),
            ]);

            // Nếu đến bước phỏng vấn trở lên -> Tạo lịch và rubric phỏng vấn
            if (in_array($stage, ['INTERVIEWING', 'HEALTH_CHECK', 'OFFERED', 'HIRED'])) {
                DB::table('interviews')->insert([
                    'application_id' => $appId,
                    'round_number'   => 1,
                    'interview_date' => '2026-09-15 09:30:00',
                    'interviewer_id' => $employees[1]['id'],
                    'score'          => 8.5,
                    'feedback'       => 'Ứng viên nắm chắc quy trình vệ sinh tiệt trùng CIP và vận hành chiết rót tiệt trùng.',
                    'result'         => 'PASS',
                    'created_at'     => now(),
                    'updated_at'     => now(),
                ]);
            }

            // Nếu đến bước khám sức khỏe trở lên -> Tạo kết quả Khám sức khỏe & Thẻ Xanh
            if (in_array($stage, ['HEALTH_CHECK', 'OFFERED', 'HIRED'])) {
                $isPassed = ($stage !== 'HEALTH_CHECK' || rand(1, 10) > 2); // 80% pass
                DB::table('health_checks')->insert([
                    'candidate_id'     => $candId,
                    'employee_id'      => null,
                    'check_date'       => '2026-09-20',
                    'medical_facility' => 'Bệnh viện Đa khoa Hoàn Mỹ Sài Gòn',
                    'health_type'      => 'GREEN_CARD_FOOD_SAFETY',
                    'health_class'     => $isPassed ? 'CLASS_1' : 'CLASS_4',
                    'is_passed'        => $isPassed ? 1 : 0,
                    'expiry_date'      => '2027-09-20',
                    'certificate_url'  => "https://storage.vinamilk.com.vn/health/{$code}.pdf",
                    'created_at'       => now(),
                    'updated_at'       => now(),
                ]);
            }

            // Nếu đến bước Offer / Hired -> Tạo Offer
            if (in_array($stage, ['OFFERED', 'HIRED'])) {
                DB::table('offers')->insert([
                    'application_id'      => $appId,
                    'position_id'         => $req['pos_id'],
                    'offered_p1'          => 22000000,
                    'offered_p2'          => 5000000,
                    'probation_rate'      => 85.00,
                    'expected_start_date' => '2026-10-15',
                    'status'              => ($stage === 'HIRED') ? 'ACCEPTED' : 'SENT_TO_CANDIDATE',
                    'approved_by'         => $employees[0]['id'],
                    'created_at'          => now(),
                    'updated_at'          => now(),
                ]);
            }
        }

        return $reqs;
    }

    /**
     * 9. Khởi tạo 9 tài khoản demo cho 9 vai trò với mật khẩu ngẫu nhiên an toàn
     */
    protected function seedDemoAccountsAndRoles(array $employees, array $orgUnits): void
    {
        // 9.1 Nạp 9 Vai trò chuẩn (Roles)
        $rolesData = [
            ['code' => 'SUPER_ADMIN', 'name' => 'Quản trị Hệ thống Cấp cao (Toàn quyền)', 'desc' => 'Quản trị cấu hình, phân quyền, tham số hệ thống toàn tập đoàn', 'is_sys' => 1],
            ['code' => 'BOD_EXECUTIVE', 'name' => 'Ban Tổng Giám đốc (BOD Executive)', 'desc' => 'Phê duyệt tờ trình chiến lược, xem toàn bộ dashboard tập đoàn', 'is_sys' => 1],
            ['code' => 'HR_DIRECTOR', 'name' => 'Giám đốc Nhân sự Tập đoàn (HR Director)', 'desc' => 'Phê duyệt tờ trình tăng lương, tuyển dụng, kế hoạch nhân sự', 'is_sys' => 1],
            ['code' => 'CB_SPECIALIST', 'name' => 'Chuyên viên Đãi ngộ & Tiền lương (C&B)', 'desc' => 'Thẩm định dải lương, compa-ratio, chạy máy tính lương', 'is_sys' => 1],
            ['code' => 'PLANT_DIRECTOR', 'name' => 'Giám đốc Nhà máy (Plant Director)', 'desc' => 'Quản lý toàn diện nhân sự & chi phí tại cơ sở nhà máy', 'is_sys' => 1],
            ['code' => 'HR_LOCAL', 'name' => 'Chuyên viên Nhân sự Cơ sở Nhà máy (HR Local)', 'desc' => 'Quản lý hồ sơ công nhân, duyệt phép cơ sở, chấm công nhà máy', 'is_sys' => 1],
            ['code' => 'SHIFT_SUPERVISOR', 'name' => 'Quản đốc / Trưởng ca Sản xuất (Shift Supervisor)', 'desc' => 'Xếp ca tuần, duyệt giờ OT, đánh giá KPI công nhân trong ca', 'is_sys' => 1],
            ['code' => 'PAYROLL_ACCOUNTANT', 'name' => 'Kế toán Tiền lương Tập đoàn (Payroll Accountant)', 'desc' => 'Vận hành Payroll Engine, kiểm tra thuế TNCN, xuất hạch toán SAP', 'is_sys' => 1],
            ['code' => 'EMPLOYEE_ESS', 'name' => 'Cán bộ Công nhân viên (Employee Self-Service)', 'desc' => 'Tra cứu phiếu lương 3P, nộp đơn xin nghỉ, báo cáo tiến độ KPI', 'is_sys' => 1],
        ];

        $roleIds = [];
        foreach ($rolesData as $rd) {
            $id = DB::table('roles')->insertGetId([
                'role_code'   => $rd['code'],
                'role_name'   => $rd['name'],
                'description' => $rd['desc'],
                'is_system'   => $rd['is_sys'],
                'created_at'  => now(),
                'updated_at'  => now(),
            ]);
            $roleIds[$rd['code']] = $id;
        }

        // 9.2 Danh sách 9 tài khoản demo chi tiết
        $demoConfigs = [
            [
                'role'      => 'SUPER_ADMIN',
                'username'  => 'admin.vnm',
                'emp_index' => 0,
                'title'     => 'Quản trị Hệ thống Cấp cao',
                'scope_unit'=> $orgUnits['HQ'],
                'scope_sub' => 1,
            ],
            [
                'role'      => 'BOD_EXECUTIVE',
                'username'  => 'ceo.vnm',
                'emp_index' => 1,
                'title'     => 'Tổng Giám đốc Tập đoàn Vinamilk',
                'scope_unit'=> $orgUnits['HQ'],
                'scope_sub' => 1,
            ],
            [
                'role'      => 'HR_DIRECTOR',
                'username'  => 'hr.director',
                'emp_index' => 2,
                'title'     => 'Giám đốc Nhân sự Tập đoàn',
                'scope_unit'=> $orgUnits['HQ'],
                'scope_sub' => 1,
            ],
            [
                'role'      => 'CB_SPECIALIST',
                'username'  => 'cb.specialist',
                'emp_index' => 3,
                'title'     => 'Chuyên viên Đãi ngộ & Tiền lương (C&B)',
                'scope_unit'=> $orgUnits['HQ_DEPTS']['HQ_HR'] ?? $orgUnits['HQ'],
                'scope_sub' => 1,
            ],
            [
                'role'      => 'PLANT_DIRECTOR',
                'username'  => 'director.thongnhat',
                'emp_index' => 4,
                'title'     => 'Giám đốc Nhà máy Sữa Thống Nhất',
                'scope_unit'=> $orgUnits['FACTORIES']['NM_THONG_NHAT'],
                'scope_sub' => 1,
            ],
            [
                'role'      => 'HR_LOCAL',
                'username'  => 'hr.thongnhat',
                'emp_index' => 5,
                'title'     => 'Chuyên viên Nhân sự Nhà máy Thống Nhất',
                'scope_unit'=> $orgUnits['FACTORIES']['NM_THONG_NHAT'],
                'scope_sub' => 1,
            ],
            [
                'role'      => 'SHIFT_SUPERVISOR',
                'username'  => 'supervisor.shift1',
                'emp_index' => 6,
                'title'     => 'Quản đốc Phân xưởng Tiệt trùng Ca 1',
                'scope_unit'=> $orgUnits['FACTORIES']['NM_THONG_NHAT'],
                'scope_sub' => 1,
            ],
            [
                'role'      => 'PAYROLL_ACCOUNTANT',
                'username'  => 'payroll.accountant',
                'emp_index' => 7,
                'title'     => 'Kế toán Tiền lương Toàn quốc',
                'scope_unit'=> $orgUnits['HQ_DEPTS']['HQ_FIN'] ?? $orgUnits['HQ'],
                'scope_sub' => 1,
            ],
            [
                'role'      => 'EMPLOYEE_ESS',
                'username'  => 'worker.ess',
                'emp_index' => 8,
                'title'     => 'Công nhân Vận hành Máy Chế biến Sữa (ESS)',
                'scope_unit'=> $orgUnits['FACTORIES']['NM_THONG_NHAT'],
                'scope_sub' => 0,
            ],
        ];

        $this->generatedDemoAccounts = [];

        foreach ($demoConfigs as $cfg) {
            // Sinh mật khẩu ngẫu nhiên phức tạp 12 ký tự: kết hợp chữ hoa, thường, số, ký tự đặc biệt
            $randomPassword = $this->generateSecurePassword(12);

            $emp = $employees[$cfg['emp_index']];

            $userId = DB::table('users')->insertGetId([
                'username'              => $cfg['username'],
                'password_hash'         => Hash::make($randomPassword),
                'employee_id'           => $emp['id'],
                'role_id'               => $roleIds[$cfg['role']],
                'is_active'             => 1,
                'two_factor_enabled'    => 0,
                'failed_login_attempts' => 0,
                'created_at'            => now(),
                'updated_at'            => now(),
            ]);

            // Gán Data Scope cây tổ chức
            DB::table('user_scopes')->insert([
                'user_id'           => $userId,
                'org_unit_id'       => $cfg['scope_unit'],
                'include_sub_units' => $cfg['scope_sub'],
                'created_at'        => now(),
                'updated_at'        => now(),
            ]);

            $unitName = DB::table('org_units')->where('id', $cfg['scope_unit'])->value('unit_name') ?? 'Toàn tập đoàn';

            $this->generatedDemoAccounts[] = [
                'role'      => $cfg['role'],
                'username'  => $cfg['username'],
                'name'      => $emp['name'],
                'scope'     => $unitName . ($cfg['scope_sub'] ? ' (+ Cây con)' : ''),
                'password'  => $randomPassword,
            ];
        }
    }

    /**
     * Sinh chuỗi mật khẩu ngẫu nhiên an toàn tuyệt đối (Hoa, Thường, Số, Ký tự đặc biệt)
     */
    protected function generateSecurePassword(int $length = 12): string
    {
        $uppercase = 'ABCDEFGHJKLMNPQRSTUVWXYZ';
        $lowercase = 'abcdefghijkmnopqrstuvwxyz';
        $numbers   = '23456789';
        $symbols   = '@#$%&*!';

        // Đảm bảo có ít nhất 1 chữ hoa, 1 chữ thường, 1 số, 1 ký tự đặc biệt
        $password = [
            $uppercase[random_int(0, strlen($uppercase) - 1)],
            $lowercase[random_int(0, strlen($lowercase) - 1)],
            $numbers[random_int(0, strlen($numbers) - 1)],
            $symbols[random_int(0, strlen($symbols) - 1)],
        ];

        $allChars = $uppercase . $lowercase . $numbers . $symbols;
        for ($i = 4; $i < $length; $i++) {
            $password[] = $allChars[random_int(0, strlen($allChars) - 1)];
        }

        shuffle($password);
        return implode('', $password);
    }

    /**
     * In bảng mật khẩu demo duy nhất một lần ra terminal
     */
    protected function printDemoAccountsTable(): void
    {
        $this->newLine();
        $this->alert("DANH SÁCH 9 TÀI KHOẢN DEMO ĐĂNG NHẬP (MẬT KHẨU NGẪU NHIÊN IN 1 LẦN)");
        $this->warn("LƯU Ý: Vui lòng lưu lại các thông tin đăng nhập bên dưới để phục vụ kiểm thử và demo UAT!");

        $tableHeaders = ['STT', 'Vai trò (Role)', 'Tên đăng nhập', 'Họ và tên', 'Phạm vi dữ liệu (Scope)', 'Mật khẩu ngẫu nhiên'];
        $tableRows = [];

        foreach ($this->generatedDemoAccounts as $idx => $acc) {
            $tableRows[] = [
                $idx + 1,
                $acc['role'],
                $acc['username'],
                $acc['name'],
                Str::limit($acc['scope'], 35),
                "<fg=yellow;options=bold>{$acc['password']}</>",
            ];
        }

        $this->table($tableHeaders, $tableRows);
        $this->newLine();
    }
}
