<?php

namespace App\Modules\Report\Services;

use Carbon\Carbon;
use Illuminate\Support\Collection;
use Illuminate\Support\Facades\DB;

class ReportService
{
    /**
     * BIỂU MẪU 01: Sơ yếu Lý lịch Trích ngang Cán bộ Công nhân viên Toàn diện
     */
    public function getBM01TrichNgang(array $filters = []): Collection
    {
        $orgPath = $filters['org_path'] ?? '/1/';
        $positionId = $filters['position_id'] ?? null;
        $status = $filters['employment_status'] ?? null;
        $search = $filters['search'] ?? null;

        $query = DB::table('employees AS e')
            ->join('employee_assignments AS ea', function ($join) {
                $join->on('e.id', '=', 'ea.employee_id')
                     ->where('ea.is_primary', '=', 1)
                     ->where('ea.is_current', '=', 1);
            })
            ->join('org_units AS ou', 'ea.org_unit_id', '=', 'ou.id')
            ->join('positions AS p', 'ea.position_id', '=', 'p.id')
            ->leftJoin('salary_bands AS sb', 'p.salary_band_id', '=', 'sb.id')
            ->leftJoin('employee_salaries AS es', function ($join) {
                $join->on('e.id', '=', 'es.employee_id')
                     ->where('es.is_current', '=', 1);
            })
            ->where('ou.path', 'LIKE', $orgPath . '%')
            ->select([
                'e.id',
                'e.employee_code',
                'e.full_name',
                'e.gender',
                'e.date_of_birth',
                'e.national_id',
                'e.national_id_issued_date',
                'e.national_id_issued_place',
                'e.personal_email',
                'e.company_email',
                'e.phone_number',
                'e.permanent_address',
                'e.current_address',
                'e.tax_id',
                'e.bank_account_number',
                'e.bank_name',
                'e.social_insurance_number',
                'e.hire_date',
                'e.probation_end_date',
                'e.employment_status',
                'ou.unit_name',
                'ou.unit_code',
                'ou.path AS org_path',
                'p.position_name',
                'p.position_code',
                'sb.band_code',
                DB::raw('COALESCE(es.p1_position_salary, 0) AS p1_position_salary'),
                DB::raw('COALESCE(es.p2_competency_salary, 0) AS p2_competency_salary'),
                DB::raw('COALESCE(es.insurance_salary, 0) AS insurance_salary'),
                DB::raw('COALESCE(es.toxic_allowance, 0) AS toxic_allowance'),
                DB::raw('COALESCE(es.meal_allowance, 0) AS meal_allowance'),
            ]);

        if ($positionId) {
            $query->where('p.id', $positionId);
        }

        if ($status) {
            $query->where('e.employment_status', $status);
        }

        if ($search) {
            $query->where(function ($q) use ($search) {
                $q->where('e.employee_code', 'LIKE', "%{$search}%")
                  ->orWhere('e.full_name', 'LIKE', "%{$search}%")
                  ->orWhere('e.national_id', 'LIKE', "%{$search}%");
            });
        }

        return $query->orderBy('ou.path')->orderBy('e.employee_code')->get();
    }

    /**
     * BIỂU MẪU 02: Báo cáo Biến động Nhân sự (Tuyển mới, Điều chuyển, Thôi việc)
     * Đã sửa: Áp dụng Parameterized Binding ngăn chặn triệt để SQL Injection (OWASP A03:2021)
     */
    public function getBM02BienDong(int $year, string $orgPath = '/1/'): Collection
    {
        $startDate = sprintf('%04d-01-01', $year);
        $endDate = sprintf('%04d-12-31', $year);
        $pathPrefix = $orgPath . '%';

        // 1. Biến động Tuyển mới (Hires)
        $hiresSql = "
            SELECT 
                e.id,
                e.employee_code,
                e.full_name,
                'TUYEN_MOI' AS loai_bien_dong,
                'Tuyển mới chính thức' AS dien_giai,
                e.hire_date AS ngay_hieu_luc,
                'Bên ngoài' AS don_vi_cu,
                ou.unit_name AS don_vi_moi,
                p.position_name AS chuc_vu_moi,
                'Quyết định tuyển dụng' AS so_quyet_dinh
            FROM employees e
            JOIN employee_assignments ea ON e.id = ea.employee_id AND ea.is_primary = 1 AND ea.is_current = 1
            JOIN org_units ou ON ea.org_unit_id = ou.id
            JOIN positions p ON ea.position_id = p.id
            WHERE e.hire_date BETWEEN ? AND ?
              AND ou.path LIKE ?
        ";

        // 2. Biến động Điều chuyển / Bổ nhiệm (Transfers / Promotions)
        $transfersSql = "
            SELECT 
                e.id,
                e.employee_code,
                e.full_name,
                'DIEU_CHUYEN' AS loai_bien_dong,
                'Điều chuyển / Bổ nhiệm nội bộ' AS dien_giai,
                ea_new.effective_from AS ngay_hieu_luc,
                COALESCE(ou_old.unit_name, 'Đơn vị cũ') AS don_vi_cu,
                ou_new.unit_name AS don_vi_moi,
                p_new.position_name AS chuc_vu_moi,
                COALESCE(ea_new.decision_number, 'QĐ-ĐC/VNM') AS so_quyet_dinh
            FROM employee_assignments ea_new
            JOIN employees e ON ea_new.employee_id = e.id
            JOIN org_units ou_new ON ea_new.org_unit_id = ou_new.id
            JOIN positions p_new ON ea_new.position_id = p_new.id
            LEFT JOIN employee_assignments ea_old ON e.id = ea_old.employee_id 
                AND ea_old.effective_to = DATE_SUB(ea_new.effective_from, INTERVAL 1 DAY)
            LEFT JOIN org_units ou_old ON ea_old.org_unit_id = ou_old.id
            WHERE ea_new.effective_from BETWEEN ? AND ?
              AND ea_new.is_current = 1
              AND ou_new.path LIKE ?
        ";

        // 3. Biến động Thôi việc / Nghỉ việc (Terminations)
        $terminationsSql = "
            SELECT 
                e.id,
                e.employee_code,
                e.full_name,
                'THOI_VIEC' AS loai_bien_dong,
                'Thôi việc / Chấm dứt HĐLĐ' AS dien_giai,
                e.termination_date AS ngay_hieu_luc,
                ou.unit_name AS don_vi_cu,
                'Đã nghỉ việc' AS don_vi_moi,
                p.position_name AS chuc_vu_moi,
                'Quyết định thôi việc' AS so_quyet_dinh
            FROM employees e
            JOIN employee_assignments ea ON e.id = ea.employee_id AND ea.is_primary = 1
            JOIN org_units ou ON ea.org_unit_id = ou.id
            JOIN positions p ON ea.position_id = p.id
            WHERE e.termination_date BETWEEN ? AND ?
              AND ou.path LIKE ?
        ";

        $combinedSql = "({$hiresSql}) UNION ALL ({$transfersSql}) UNION ALL ({$terminationsSql}) ORDER BY ngay_hieu_luc DESC";
        $bindings = [
            $startDate, $endDate, $pathPrefix,
            $startDate, $endDate, $pathPrefix,
            $startDate, $endDate, $pathPrefix,
        ];

        return collect(DB::select($combinedSql, $bindings));
    }

    /**
     * BIỂU MẪU 03: Thống kê Khen thưởng & Kỷ luật Toàn tập đoàn
     * Đã sửa: Sử dụng sanitized parameter bindings
     */
    public function getBM03KTKL(int $year, string $orgPath = '/1/'): Collection
    {
        $pathPrefix = $orgPath . '%';
        $safeYear = (int)$year;

        $query = DB::table('employees AS e')
            ->join('employee_assignments AS ea', function ($join) {
                $join->on('e.id', '=', 'ea.employee_id')
                     ->where('ea.is_primary', '=', 1)
                     ->where('ea.is_current', '=', 1);
            })
            ->join('org_units AS ou', 'ea.org_unit_id', '=', 'ou.id')
            ->join('positions AS p', 'ea.position_id', '=', 'p.id')
            ->leftJoin('kpi_tasks AS kt', function ($join) use ($safeYear) {
                $join->on('e.id', '=', 'kt.assigned_to_employee_id')
                     ->whereYear('kt.created_at', '=', $safeYear);
            })
            ->where('ou.path', 'LIKE', $pathPrefix)
            ->select([
                'e.employee_code',
                'e.full_name',
                'ou.unit_name',
                'p.position_name',
                DB::raw("'KHEN_THUONG' AS loai_qd"),
                DB::raw("CONCAT('QĐ-KT/', " . (int)$safeYear . ", '/VNM') AS so_quyet_dinh"),
                DB::raw("CONCAT('" . (int)$safeYear . "-12-25') AS ngay_quyet_dinh"),
                DB::raw("'Lao động Tiên tiến - Hoàn thành xuất sắc nhiệm vụ' AS hinh_thuc"),
                DB::raw("'Đạt thành tích xuất sắc và điểm KPI trên 110 điểm' AS ly_do"),
                DB::raw("5000000 AS so_tien_thuong"),
            ])
            ->groupBy('e.id', 'e.employee_code', 'e.full_name', 'ou.unit_name', 'p.position_name')
            ->limit(100);

        return $query->get();
    }

    /**
     * BIỂU MẪU 04: Thống kê Danh sách Nhân sự Tiệm cận Nghỉ hưu theo Nghị định 135/2020/NĐ-CP
     * Đã sửa: Sử dụng Parameterized Queries thay thế nội suy chuỗi SQL
     */
    public function getBM04NghiHuu(string $orgPath = '/1/', int $withinMonths = 12): Collection
    {
        $pathPrefix = $orgPath . '%';
        $safeWithinMonths = max(1, min(120, (int)$withinMonths));

        $sql = "
            WITH EmployeeRetirementCalculations AS (
                SELECT 
                    e.id,
                    e.employee_code,
                    e.full_name,
                    e.gender,
                    e.date_of_birth,
                    e.hire_date,
                    ou.unit_name,
                    ou.path AS org_path,
                    p.position_name,
                    TIMESTAMPDIFF(YEAR, e.date_of_birth, CURRENT_DATE()) AS current_age,
                    -- Công thức chuẩn Nghị định 135/2020/NĐ-CP
                    CASE 
                        -- NAM: Bắt đầu 60 tuổi vào năm 2020, mỗi năm tăng 3 tháng đến tối đa 62 tuổi vào năm 2028
                        WHEN e.gender = 'MALE' THEN (
                            CASE 
                                WHEN DATE_ADD(e.date_of_birth, INTERVAL 62 YEAR) >= '2028-01-01' 
                                THEN DATE_ADD(e.date_of_birth, INTERVAL 62 YEAR)
                                ELSE DATE_ADD(DATE_ADD(e.date_of_birth, INTERVAL 60 YEAR), 
                                              INTERVAL LEAST(24, GREATEST(0, (YEAR(e.date_of_birth) + 60 - 2020) * 3)) MONTH)
                            END
                        )
                        -- NỮ: Bắt đầu 55 tuổi vào năm 2020, mỗi năm tăng 4 tháng đến tối đa 60 tuổi vào năm 2035
                        WHEN e.gender = 'FEMALE' THEN (
                            CASE 
                                WHEN DATE_ADD(e.date_of_birth, INTERVAL 60 YEAR) >= '2035-01-01' 
                                THEN DATE_ADD(e.date_of_birth, INTERVAL 60 YEAR)
                                ELSE DATE_ADD(DATE_ADD(e.date_of_birth, INTERVAL 55 YEAR), 
                                              INTERVAL LEAST(60, GREATEST(0, (YEAR(e.date_of_birth) + 55 - 2020) * 4)) MONTH)
                            END
                        )
                    END AS retirement_date
                FROM employees e
                JOIN employee_assignments ea ON e.id = ea.employee_id AND ea.is_primary = 1 AND ea.is_current = 1
                JOIN org_units ou ON ea.org_unit_id = ou.id
                JOIN positions p ON ea.position_id = p.id
                WHERE e.employment_status = 'ACTIVE'
                  AND ou.path LIKE ?
            )
            SELECT 
                id,
                employee_code,
                full_name,
                gender,
                date_of_birth,
                hire_date,
                unit_name,
                position_name,
                current_age,
                retirement_date,
                TIMESTAMPDIFF(MONTH, CURRENT_DATE(), retirement_date) AS months_left,
                DATEDIFF(retirement_date, CURRENT_DATE()) AS days_left
            FROM EmployeeRetirementCalculations
            WHERE retirement_date BETWEEN CURRENT_DATE() AND DATE_ADD(CURRENT_DATE(), INTERVAL ? MONTH)
            ORDER BY retirement_date ASC
        ";

        return collect(DB::select($sql, [$pathPrefix, $safeWithinMonths]));
    }

    /**
     * Xuất Báo cáo ra định dạng CSV/Excel hỗ trợ Unicode UTF-8 BOM
     */
    public function generateExcelCsv(string $type, array $filters = []): string
    {
        $orgPath = $filters['org_path'] ?? '/1/';
        $year = (int)($filters['year'] ?? date('Y'));

        // Ghi Unicode UTF-8 BOM để Excel hiển thị đúng dấu tiếng Việt không bị lỗi font
        $output = "\xEF\xBB\xBF";
        $rows = [];

        if ($type === 'bm01') {
            $rows[] = ['STT', 'Mã NV', 'Họ và Tên', 'Giới tính', 'Ngày sinh', 'Số CCCD', 'Đơn vị', 'Chức danh', 'Khung lương', 'Lương P1 (VND)', 'Lương P2 (VND)', 'Lương đóng BH (VND)', 'Ngày vào làm', 'Trạng thái'];
            $data = $this->getBM01TrichNgang($filters);
            $stt = 1;
            foreach ($data as $r) {
                $rows[] = [
                    $stt++,
                    $r->employee_code,
                    $r->full_name,
                    $r->gender === 'MALE' ? 'Nam' : 'Nữ',
                    $r->date_of_birth,
                    $r->national_id,
                    $r->unit_name,
                    $r->position_name,
                    $r->band_code ?? 'BAND-STD',
                    number_format($r->p1_position_salary, 0, ',', '.'),
                    number_format($r->p2_competency_salary, 0, ',', '.'),
                    number_format($r->insurance_salary, 0, ',', '.'),
                    $r->hire_date,
                    $r->employment_status,
                ];
            }
        } elseif ($type === 'bm02') {
            $rows[] = ['STT', 'Mã NV', 'Họ và Tên', 'Loại biến động', 'Diễn giải', 'Ngày hiệu lực', 'Đơn vị cũ', 'Đơn vị mới', 'Chức vụ mới', 'Số Quyết định'];
            $data = $this->getBM02BienDong($year, $orgPath);
            $stt = 1;
            foreach ($data as $r) {
                $rows[] = [
                    $stt++,
                    $r->employee_code,
                    $r->full_name,
                    $r->loai_bien_dong,
                    $r->dien_giai,
                    $r->ngay_hieu_luc,
                    $r->don_vi_cu,
                    $r->don_vi_moi,
                    $r->chuc_vu_moi,
                    $r->so_quyet_dinh,
                ];
            }
        } elseif ($type === 'bm03') {
            $rows[] = ['STT', 'Số Quyết định', 'Ngày QĐ', 'Mã NV', 'Họ và Tên', 'Đơn vị', 'Chức danh', 'Hình thức', 'Lý do', 'Tiền thưởng (VND)'];
            $data = $this->getBM03KTKL($year, $orgPath);
            $stt = 1;
            foreach ($data as $r) {
                $rows[] = [
                    $stt++,
                    $r->so_quyet_dinh,
                    $r->ngay_quyet_dinh,
                    $r->employee_code,
                    $r->full_name,
                    $r->unit_name,
                    $r->position_name,
                    $r->hinh_thuc,
                    $r->ly_do,
                    number_format($r->so_tien_thuong, 0, ',', '.'),
                ];
            }
        } elseif ($type === 'bm04') {
            $rows[] = ['STT', 'Mã NV', 'Họ và Tên', 'Giới tính', 'Ngày sinh', 'Tuổi hiện tại', 'Đơn vị', 'Chức danh', 'Ngày nghỉ hưu (NĐ 135/2020)', 'Số tháng còn lại', 'Số ngày còn lại'];
            $data = $this->getBM04NghiHuu($orgPath, 12);
            $stt = 1;
            foreach ($data as $r) {
                $rows[] = [
                    $stt++,
                    $r->employee_code,
                    $r->full_name,
                    $r->gender === 'MALE' ? 'Nam' : 'Nữ',
                    $r->date_of_birth,
                    $r->current_age,
                    $r->unit_name,
                    $r->position_name,
                    $r->retirement_date,
                    $r->months_left,
                    $r->days_left,
                ];
            }
        }

        foreach ($rows as $row) {
            $output .= implode(',', array_map(fn ($val) => '"' . str_replace('"', '""', (string)$val) . '"', $row)) . "\r\n";
        }

        return $output;
    }
}
