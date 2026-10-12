<?php
/**
 * =====================================================================
 * VINAMILK HRM - MODEL CÔNG CỤ TÍNH LƯƠNG (PAYROLL MODEL ADAPTER)
 * =====================================================================
 * Kế thừa kiến trúc Model MVC thuần của Vinamilk, ủy quyền tính toán cho
 * PayrollService để đảm bảo tách hàm, dễ test, tuân thủ luật lao động
 * và biểu thuế TNCN 2026.
 */

require_once __DIR__ . '/../core/Model.php';
require_once __DIR__ . '/../services/PayrollService.php';
require_once __DIR__ . '/EmployeeModel.php';

class PayrollModel extends Model
{
    protected string $table = 'bang_luong';
    protected string $primaryKey = 'id';
    private PayrollService $payrollService;

    public function __construct()
    {
        parent::__construct();
        $this->payrollService = new PayrollService($this->db);
    }

    public function getService(): PayrollService
    {
        return $this->payrollService;
    }

    public function getLegalConfig(?string $date = null): array
    {
        return $this->payrollService->getLegalConfig($date ?? date('Y-m-d'));
    }

    public function getTaxBrackets(?string $date = null): array
    {
        return $this->payrollService->getTaxBrackets($date ?? date('Y-m-d'));
    }

    public function calculateTax(float $taxableIncome, array $brackets): float
    {
        $res = $this->payrollService->calculatePersonalIncomeTax($taxableIncome, $brackets, 0.0, 0, 0.0);
        return $res['tax_amount'];
    }

    public function calculateProgressiveTax(float $taxableIncome, ?string $date = null): float
    {
        $brackets = $this->getTaxBrackets($date);
        return $this->calculateTax($taxableIncome, $brackets);
    }

    public function calculateSingle(array $emp, array $legalConfig, array $taxBrackets): array
    {
        return $this->payrollService->calculateEmployeeSalary($emp, $legalConfig, $taxBrackets);
    }

    /**
     * TÍNH BẢNG LƯƠNG HÀNG LOẠT VÀ LƯU VÀO CSDL
     */
    public function calculateAndSaveBatch(string $kyLuong, ?string $targetUnit = null): int
    {
        $unitCode = $targetUnit && $targetUnit !== 'ALL' ? $targetUnit : (Auth::maDV() ?: 'VINAMILK');
        $creator = Auth::user()['Username'] ?? 'KETOAN_LUONG';

        // 1. Tạo bản ghi bảng lương nếu chưa có
        $bangLuongId = $this->payrollService->createDraft($kyLuong, $unitCode, $creator);

        // 2. Lấy danh sách nhân viên theo scopeByUnit
        $employeeModel = new EmployeeModel();
        $empResult = $employeeModel->getEmployees(['ma_dv' => $targetUnit], 1000, 0);
        $employees = $empResult['data'];

        if (empty($employees)) {
            throw new Exception("Không có nhân sự nào trong phạm vi quyền hạn để tính lương.");
        }

        // 3. Tính toán và lưu
        $res = $this->payrollService->calculateBatch($bangLuongId, $employees);

        // 4. Ghi vết kiểm toán
        $this->logAudit('CALCULATE_PAYROLL', 'bang_luong', (string)$bangLuongId, null, [
            'ky_luong'       => $kyLuong,
            'tong_nhan_su'   => $res['total_employees'],
            'tong_thuc_linh' => $res['total_net']
        ]);

        return $bangLuongId;
    }

    /**
     * LẤY BẢNG LƯƠNG TỔNG HỢP THEO PHẠM VI scopeByUnit()
     */
    public function getPayrolls(): array
    {
        $scope = $this->scopeByUnit('bl', 'ma_dv');
        $sql = "SELECT bl.*, dv.TenDV
                FROM bang_luong bl
                LEFT JOIN dm_donvi dv ON bl.ma_dv = dv.MaDV
                WHERE {$scope['where']}
                ORDER BY bl.ky_luong DESC, bl.id DESC";
        return $this->fetchAll($sql, $scope['params']);
    }

    /**
     * LẤY CHI TIẾT BẢNG LƯƠNG KÈM DANH SÁCH NHÂN SỰ ĐÃ ÁP DỤNG scopeByUnit() & CHE DỮ LIỆU NHẠY CẢM
     */
    public function getPayrollWithDetails(int $id): ?array
    {
        $scopeBl = $this->scopeByUnit('bl', 'ma_dv');
        $summary = $this->fetchOne("SELECT bl.*, dv.TenDV 
                                    FROM bang_luong bl 
                                    LEFT JOIN dm_donvi dv ON bl.ma_dv = dv.MaDV 
                                    WHERE bl.id = :id AND ({$scopeBl['where']})", 
                                    array_merge([':id' => $id], $scopeBl['params']));
        if (!$summary) return null;

        $scopeNs = $this->scopeByUnit('ns', 'MaDV');
        $details = $this->fetchAll("SELECT d.*, ns.SoCMND_CCCD, ns.SoTaiKhoanNH 
                                    FROM bang_luong_chi_tiet d 
                                    LEFT JOIN nhansu ns ON d.ma_nv = ns.MaNV 
                                    WHERE d.bang_luong_id = :id AND ({$scopeNs['where']})
                                    ORDER BY d.ma_nv ASC", 
                                    array_merge([':id' => $id], $scopeNs['params']));

        // Che dữ liệu nhạy cảm nếu người xem không có quyền luong.sensitive
        if (!Auth::canViewSensitive('luong')) {
            $viewerMaNV = Auth::maNV();
            foreach ($details as &$row) {
                if ($row['ma_nv'] !== $viewerMaNV) {
                    $row['luong_p1_hop_dong'] = 0;
                    $row['luong_p2_hieu_suat'] = 0;
                    $row['luong_p3_kpi']       = 0;
                    $row['thuc_linh']          = 0;
                    $row['thuc_linh_masked']   = '****** ₫';
                }
            }
            unset($row);
        }

        $summary['details'] = $details;
        return $summary;
    }

    /**
     * LẤY PHIẾU LƯƠNG CÁ NHÂN (PAYSLIP) CÓ KIỂM TRA PHẠM VI scopeByUnit()
     */
    public function getEmployeePayslip(string $maNV, ?string $kyLuong = null): ?array
    {
        $scopeNs = $this->scopeByUnit('ns', 'MaDV');
        $where = "d.ma_nv = :manv AND ({$scopeNs['where']})";
        $params = array_merge([':manv' => $maNV], $scopeNs['params']);

        if ($kyLuong) {
            $where .= " AND bl.ky_luong = :ky";
            $params[':ky'] = $kyLuong;
        }

        $sql = "SELECT d.*, bl.ky_luong, bl.trang_thai, bl.ngay_duyet, bl.nguoi_duyet,
                       ns.Email, ns.SoDienThoai, ns.NgayVaoLam, ns.SoCMND_CCCD, ns.SoTaiKhoanNH, ns.NganHang,
                       dv.TenDV as DonViTen, dv.DiaChi as DonViDiaChi
                FROM bang_luong_chi_tiet d
                INNER JOIN bang_luong bl ON d.bang_luong_id = bl.id
                LEFT JOIN nhansu ns ON d.ma_nv = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                WHERE {$where}
                ORDER BY bl.ky_luong DESC
                LIMIT 1";

        $res = $this->fetchOne($sql, $params);
        if (!$res) return null;

        // Nếu người xem không phải chính chủ và không có quyền luong.sensitive thì che lương
        if (Auth::maNV() !== $res['ma_nv'] && !Auth::canViewSensitive('luong')) {
            $res['luong_p1_hop_dong'] = 0;
            $res['luong_p2_hieu_suat'] = 0;
            $res['luong_p3_kpi']       = 0;
            $res['thuc_linh']          = 0;
            $res['thuc_linh_masked']   = '****** ₫';
        }

        return $res;
    }

    // Các hàm quy trình phê duyệt
    public function accountantApprove(int $id, string $approver): bool
    {
        $ok = $this->payrollService->accountantApprove($id, $approver);
        if ($ok) $this->logAudit('APPROVE_PAYROLL_ACCOUNTANT', 'bang_luong', (string)$id, null, ['status' => 'KETOAN_DUYET']);
        return $ok;
    }

    public function directorApprove(int $id, string $approver): bool
    {
        $ok = $this->payrollService->directorApprove($id, $approver);
        if ($ok) $this->logAudit('APPROVE_PAYROLL_DIRECTOR', 'bang_luong', (string)$id, null, ['status' => 'GIAMDOC_DUYET']);
        return $ok;
    }

    public function disburse(int $id, string $disburser): bool
    {
        $ok = $this->payrollService->disburse($id, $disburser);
        if ($ok) $this->logAudit('DISBURSE_PAYROLL', 'bang_luong', (string)$id, null, ['status' => 'DA_CHI']);
        return $ok;
    }

    public function isLocked(int $id): bool
    {
        return $this->payrollService->isLocked($id);
    }
}
