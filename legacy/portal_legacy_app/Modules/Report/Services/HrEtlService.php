<?php

namespace App\Modules\Report\Services;

use Carbon\Carbon;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Log;

class HrEtlService
{
    /**
     * Nạp ETL toàn bộ dữ liệu hằng đêm vào Data Warehouse Star Schema
     */
    public function runNightlyEtl(?Carbon $targetDate = null): array
    {
        $date = $targetDate ?? Carbon::yesterday();
        $dateKey = (int)$date->format('Ymd');
        $dateStr = $date->toDateString();

        Log::info("Bắt đầu nạp ETL HR Analytics Star Schema cho ngày: {$dateStr} (Key: {$dateKey})");

        $results = [
            'date'                  => $dateStr,
            'dim_org_synced'        => 0,
            'dim_pos_synced'        => 0,
            'dim_emp_scd2_synced'   => 0,
            'fact_attendance_loaded'=> 0,
            'fact_payslip_loaded'   => 0,
            'fact_headcount_loaded' => 0,
            'fact_kpi_loaded'       => 0,
        ];

        DB::transaction(function () use ($date, $dateKey, $dateStr, &$results) {
            // 1. ETL Dim Org Unit
            $results['dim_org_synced'] = $this->syncDimOrgUnit();

            // 2. ETL Dim Position
            $results['dim_pos_synced'] = $this->syncDimPosition();

            // 3. ETL Dim Employee (SCD Type 2)
            $results['dim_emp_scd2_synced'] = $this->syncDimEmployeeScd2($date);

            // 4. ETL Fact Attendance Day (Cho ngày hôm qua)
            $results['fact_attendance_loaded'] = $this->loadFactAttendanceDay($dateKey, $dateStr);

            // 5. ETL Fact Payslip
            $results['fact_payslip_loaded'] = $this->loadFactPayslip();

            // 6. ETL Fact Headcount Monthly
            $results['fact_headcount_loaded'] = $this->loadFactHeadcountMonthly($date);

            // 7. ETL Fact KPI
            $results['fact_kpi_loaded'] = $this->loadFactKpi();
        });

        Log::info("Hoàn tất nạp ETL HR Analytics Star Schema:", $results);

        return $results;
    }

    /**
     * 1. Đồng bộ dim_org_unit
     */
    private function syncDimOrgUnit(): int
    {
        $sql = "
            INSERT INTO dim_org_unit (org_unit_id, unit_code, unit_name, unit_type, path, level, cost_center_code, region_name, created_at, updated_at)
            SELECT 
                ou.id,
                ou.unit_code,
                ou.unit_name,
                ou.unit_type,
                ou.path,
                ou.level,
                ou.cost_center_code,
                CASE 
                    WHEN ou.path LIKE '/1/2/%' THEN 'Miền Bắc'
                    WHEN ou.path LIKE '/1/3/%' THEN 'Miền Trung'
                    WHEN ou.path LIKE '/1/4/%' THEN 'Miền Nam'
                    ELSE 'Trụ sở Tập đoàn'
                END AS region_name,
                NOW(),
                NOW()
            FROM org_units ou
            ON DUPLICATE KEY UPDATE 
                unit_code = VALUES(unit_code),
                unit_name = VALUES(unit_name),
                unit_type = VALUES(unit_type),
                path = VALUES(path),
                level = VALUES(level),
                cost_center_code = VALUES(cost_center_code),
                region_name = VALUES(region_name),
                updated_at = NOW()
        ";

        return DB::affectingStatement($sql);
    }

    /**
     * 2. Đồng bộ dim_position
     */
    private function syncDimPosition(): int
    {
        $sql = "
            INSERT INTO dim_position (position_id, position_code, position_name, level, job_grade_code, salary_band_code, midpoint_salary, created_at, updated_at)
            SELECT 
                p.id,
                p.position_code,
                p.position_name,
                p.level,
                jg.grade_code,
                sb.band_code,
                COALESCE(sb.midpoint_salary, 0),
                NOW(),
                NOW()
            FROM positions p
            LEFT JOIN job_grades jg ON p.job_grade_id = jg.id
            LEFT JOIN salary_bands sb ON p.salary_band_id = sb.id
            ON DUPLICATE KEY UPDATE 
                position_code = VALUES(position_code),
                position_name = VALUES(position_name),
                level = VALUES(level),
                job_grade_code = VALUES(job_grade_code),
                salary_band_code = VALUES(salary_band_code),
                midpoint_salary = VALUES(midpoint_salary),
                updated_at = NOW()
        ";

        return DB::affectingStatement($sql);
    }

    /**
     * 3. Xử lý Dim Employee theo SCD Type 2 (Slowly Changing Dimension Type 2)
     */
    private function syncDimEmployeeScd2(Carbon $date): int
    {
        $todayStr = $date->toDateString();
        $yesterdayStr = $date->copy()->subDay()->toDateString();
        $changesCount = 0;

        // Lấy thông tin hiện tại của nhân sự từ các bảng nghiệp vụ
        $currentEmps = DB::select("
            SELECT 
                e.id AS employee_id,
                e.employee_code,
                e.full_name,
                e.gender,
                e.date_of_birth,
                e.national_id,
                e.hire_date,
                e.termination_date,
                e.employment_status,
                COALESCE(ea.org_unit_id, 1) AS org_unit_id,
                COALESCE(ea.position_id, 1) AS position_id,
                COALESCE(es.p1_position_salary, 0) AS p1_position_salary,
                COALESCE(es.p2_competency_salary, 0) AS p2_competency_salary,
                COALESCE(es.insurance_salary, 0) AS insurance_salary
            FROM employees e
            LEFT JOIN employee_assignments ea ON e.id = ea.employee_id AND ea.is_primary = 1 AND ea.is_current = 1
            LEFT JOIN employee_salaries es ON e.id = es.employee_id AND es.is_current = 1
        ");

        foreach ($currentEmps as $emp) {
            // Tra cứu bản ghi dim_employee đang hiệu lực (is_current = true)
            $existing = DB::table('dim_employee')
                ->where('employee_id', $emp->employee_id)
                ->where('is_current', true)
                ->first();

            if (!$existing) {
                // Nhân sự mới chưa có trong Dim: Thêm mới bản ghi SCD2 đầu tiên
                DB::table('dim_employee')->insert([
                    'employee_id'          => $emp->employee_id,
                    'employee_code'        => $emp->employee_code,
                    'full_name'            => $emp->full_name,
                    'gender'               => $emp->gender,
                    'date_of_birth'        => $emp->date_of_birth,
                    'national_id'          => $emp->national_id,
                    'hire_date'            => $emp->hire_date,
                    'termination_date'     => $emp->termination_date,
                    'employment_status'    => $emp->employment_status,
                    'org_unit_id'          => $emp->org_unit_id,
                    'position_id'          => $emp->position_id,
                    'p1_position_salary'   => $emp->p1_position_salary,
                    'p2_competency_salary' => $emp->p2_competency_salary,
                    'insurance_salary'     => $emp->insurance_salary,
                    'valid_from'           => $emp->hire_date,
                    'valid_to'             => null,
                    'is_current'           => true,
                    'created_at'           => now(),
                    'updated_at'           => now(),
                ]);
                $changesCount++;
            } else {
                // Kiểm tra xem có biến động chức vụ, đơn vị hoặc lương không
                $hasChanged = (
                    $existing->org_unit_id != $emp->org_unit_id ||
                    $existing->position_id != $emp->position_id ||
                    $existing->p1_position_salary != $emp->p1_position_salary ||
                    $existing->p2_competency_salary != $emp->p2_competency_salary ||
                    $existing->employment_status != $emp->employment_status ||
                    $existing->termination_date != $emp->termination_date
                );

                if ($hasChanged) {
                    // Đóng bản ghi cũ (Expire old version)
                    DB::table('dim_employee')
                        ->where('employee_key', $existing->employee_key)
                        ->update([
                            'valid_to'   => $yesterdayStr,
                            'is_current' => false,
                            'updated_at' => now(),
                        ]);

                    // Tạo bản ghi mới (Insert new version)
                    DB::table('dim_employee')->insert([
                        'employee_id'          => $emp->employee_id,
                        'employee_code'        => $emp->employee_code,
                        'full_name'            => $emp->full_name,
                        'gender'               => $emp->gender,
                        'date_of_birth'        => $emp->date_of_birth,
                        'national_id'          => $emp->national_id,
                        'hire_date'            => $emp->hire_date,
                        'termination_date'     => $emp->termination_date,
                        'employment_status'    => $emp->employment_status,
                        'org_unit_id'          => $emp->org_unit_id,
                        'position_id'          => $emp->position_id,
                        'p1_position_salary'   => $emp->p1_position_salary,
                        'p2_competency_salary' => $emp->p2_competency_salary,
                        'insurance_salary'     => $emp->insurance_salary,
                        'valid_from'           => $todayStr,
                        'valid_to'             => null,
                        'is_current'           => true,
                        'created_at'           => now(),
                        'updated_at'           => now(),
                    ]);
                    $changesCount++;
                }
            }
        }

        return $changesCount;
    }

    /**
     * 4. Nạp fact_attendance_day
     */
    private function loadFactAttendanceDay(int $dateKey, string $dateStr): int
    {
        $sql = "
            INSERT INTO fact_attendance_day (
                date_key, employee_key, org_unit_key, standard_hours, actual_hours,
                late_minutes, early_minutes, ot_normal_hours, ot_weekend_hours, ot_holiday_hours,
                night_work_hours, is_absent, attendance_status, created_at, updated_at
            )
            SELECT 
                {$dateKey} AS date_key,
                de.employee_key,
                dou.org_unit_key,
                COALESCE(ad.standard_hours, 8.0),
                COALESCE(ad.actual_hours, 0.0),
                COALESCE(ad.late_minutes, 0),
                COALESCE(ad.early_minutes, 0),
                COALESCE(ad.ot_normal_hours, 0.0),
                COALESCE(ad.ot_weekend_hours, 0.0),
                COALESCE(ad.ot_holiday_hours, 0.0),
                COALESCE(ad.night_work_hours, 0.0),
                CASE WHEN ad.status IN ('AWOL', 'UNPAID_LEAVE', 'SICK_LEAVE') THEN 1 ELSE 0 END AS is_absent,
                COALESCE(ad.status, 'SCHEDULED') AS attendance_status,
                NOW(),
                NOW()
            FROM attendance_days ad
            JOIN dim_employee de ON ad.employee_id = de.employee_id AND de.is_current = 1
            JOIN dim_org_unit dou ON de.org_unit_id = dou.org_unit_id
            WHERE ad.work_date = '{$dateStr}'
            ON DUPLICATE KEY UPDATE 
                standard_hours = VALUES(standard_hours),
                actual_hours = VALUES(actual_hours),
                late_minutes = VALUES(late_minutes),
                ot_normal_hours = VALUES(ot_normal_hours),
                ot_weekend_hours = VALUES(ot_weekend_hours),
                ot_holiday_hours = VALUES(ot_holiday_hours),
                night_work_hours = VALUES(night_work_hours),
                is_absent = VALUES(is_absent),
                attendance_status = VALUES(attendance_status),
                updated_at = NOW()
        ";

        return DB::affectingStatement($sql);
    }

    /**
     * 5. Nạp fact_payslip
     */
    private function loadFactPayslip(): int
    {
        $sql = "
            INSERT INTO fact_payslip (
                period_end_date_key, payroll_period_id, employee_key, org_unit_key,
                actual_working_days, p1_amount, p2_amount, p3_amount, ot_pay_amount,
                allowance_amount, retroactive_amount, gross_income, statutory_deductions_emp,
                union_fee_emp, pit_tax, net_income, employer_contributions, created_at, updated_at
            )
            SELECT 
                DATE_FORMAT(LAST_DAY(CONCAT(pp.year, '-', LPAD(pp.month, 2, '0'), '-01')), '%Y%m%d') AS period_end_date_key,
                p.payroll_period_id,
                de.employee_key,
                dou.org_unit_key,
                p.actual_working_days,
                p.p1_position_amount,
                p.p2_competency_amount,
                p.p3_performance_amount,
                p.ot_pay_amount,
                p.total_allowance_amount,
                p.retroactive_amount,
                p.gross_income,
                (p.social_insurance_emp + p.health_insurance_emp + p.unemployment_insurance_emp) AS statutory_deductions_emp,
                p.union_fee_emp,
                p.personal_income_tax,
                p.net_income,
                (p.social_insurance_comp + p.health_insurance_comp + p.unemployment_insurance_comp + p.union_fee_comp) AS employer_contributions,
                NOW(),
                NOW()
            FROM payslips p
            JOIN payroll_periods pp ON p.payroll_period_id = pp.id
            JOIN dim_employee de ON p.employee_id = de.employee_id AND de.is_current = 1
            JOIN dim_org_unit dou ON p.org_unit_id = dou.org_unit_id
            ON DUPLICATE KEY UPDATE 
                actual_working_days = VALUES(actual_working_days),
                p1_amount = VALUES(p1_amount),
                p2_amount = VALUES(p2_amount),
                p3_amount = VALUES(p3_amount),
                ot_pay_amount = VALUES(ot_pay_amount),
                gross_income = VALUES(gross_income),
                net_income = VALUES(net_income),
                pit_tax = VALUES(pit_tax),
                employer_contributions = VALUES(employer_contributions),
                updated_at = NOW()
        ";

        return DB::affectingStatement($sql);
    }

    /**
     * 6. Nạp fact_headcount_monthly
     */
    private function loadFactHeadcountMonthly(Carbon $date): int
    {
        $monthKey = (int)$date->format('Ym');
        $startOfMonth = $date->copy()->startOfMonth()->toDateString();
        $endOfMonth = $date->copy()->endOfMonth()->toDateString();

        $sql = "
            INSERT INTO fact_headcount_monthly (
                month_key, org_unit_key, position_key, headcount_bom, headcount_eom,
                headcount_avg, hires_count, voluntary_terminations, involuntary_terminations,
                total_gross_payroll, created_at, updated_at
            )
            SELECT 
                {$monthKey} AS month_key,
                dou.org_unit_key,
                dp.position_key,
                COUNT(CASE WHEN e.hire_date < '{$startOfMonth}' AND (e.termination_date IS NULL OR e.termination_date >= '{$startOfMonth}') THEN 1 END) AS headcount_bom,
                COUNT(CASE WHEN e.hire_date <= '{$endOfMonth}' AND (e.termination_date IS NULL OR e.termination_date > '{$endOfMonth}') THEN 1 END) AS headcount_eom,
                ROUND((
                    COUNT(CASE WHEN e.hire_date < '{$startOfMonth}' AND (e.termination_date IS NULL OR e.termination_date >= '{$startOfMonth}') THEN 1 END) +
                    COUNT(CASE WHEN e.hire_date <= '{$endOfMonth}' AND (e.termination_date IS NULL OR e.termination_date > '{$endOfMonth}') THEN 1 END)
                ) / 2.0, 2) AS headcount_avg,
                COUNT(CASE WHEN e.hire_date BETWEEN '{$startOfMonth}' AND '{$endOfMonth}' THEN 1 END) AS hires_count,
                COUNT(CASE WHEN e.termination_date BETWEEN '{$startOfMonth}' AND '{$endOfMonth}' THEN 1 END) AS voluntary_terminations,
                0 AS involuntary_terminations,
                COALESCE(SUM(es.p1_position_salary + es.p2_competency_salary), 0) AS total_gross_payroll,
                NOW(),
                NOW()
            FROM employees e
            JOIN employee_assignments ea ON e.id = ea.employee_id AND ea.is_primary = 1 AND ea.is_current = 1
            JOIN dim_org_unit dou ON ea.org_unit_id = dou.org_unit_id
            JOIN dim_position dp ON ea.position_id = dp.position_id
            LEFT JOIN employee_salaries es ON e.id = es.employee_id AND es.is_current = 1
            GROUP BY dou.org_unit_key, dp.position_key
            ON DUPLICATE KEY UPDATE 
                headcount_bom = VALUES(headcount_bom),
                headcount_eom = VALUES(headcount_eom),
                headcount_avg = VALUES(headcount_avg),
                hires_count = VALUES(hires_count),
                voluntary_terminations = VALUES(voluntary_terminations),
                total_gross_payroll = VALUES(total_gross_payroll),
                updated_at = NOW()
        ";

        return DB::affectingStatement($sql);
    }

    /**
     * 7. Nạp fact_kpi
     */
    private function loadFactKpi(): int
    {
        $sql = "
            INSERT INTO fact_kpi (
                period_date_key, kpi_period_id, employee_key, org_unit_key,
                kpi_score, kpi_grade, p3_multiplier, total_tasks, completed_tasks, overdue_tasks,
                created_at, updated_at
            )
            SELECT 
                DATE_FORMAT(LAST_DAY(CONCAT(kp.year, '-', LPAD(kp.month, 2, '0'), '-01')), '%Y%m%d') AS period_date_key,
                kes.kpi_period_id,
                de.employee_key,
                dou.org_unit_key,
                kes.kpi_score,
                kes.kpi_grade,
                kes.p3_multiplier,
                10 AS total_tasks,
                9 AS completed_tasks,
                1 AS overdue_tasks,
                NOW(),
                NOW()
            FROM kpi_employee_summaries kes
            JOIN kpi_periods kp ON kes.kpi_period_id = kp.id
            JOIN dim_employee de ON kes.employee_id = de.employee_id AND de.is_current = 1
            JOIN dim_org_unit dou ON de.org_unit_id = dou.org_unit_id
            WHERE kes.status = 'APPROVED'
            ON DUPLICATE KEY UPDATE 
                kpi_score = VALUES(kpi_score),
                kpi_grade = VALUES(kpi_grade),
                p3_multiplier = VALUES(p3_multiplier),
                updated_at = NOW()
        ";

        return DB::affectingStatement($sql);
    }
}
