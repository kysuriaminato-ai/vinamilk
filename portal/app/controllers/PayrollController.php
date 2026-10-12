<?php
/**
 * =====================================================================
 * VINAMILK HRM - CONTROLLER TIỀN LƯƠNG & CHẾ ĐỘ (PayrollController)
 * =====================================================================
 * Tính lương 100% tự động từ bảng cau_hinh_luong và bieu_thue_tncn
 * Hỗ trợ in Phiếu lương A4 chuẩn Vinamilk (@media print)
 */

require_once __DIR__ . '/../core/Controller.php';
require_once __DIR__ . '/../models/PayrollModel.php';
require_once __DIR__ . '/../models/OrgUnitModel.php';

class PayrollController extends Controller
{
    private PayrollModel $payrollModel;
    private OrgUnitModel $orgUnitModel;

    public function __construct()
    {
        $this->payrollModel = new PayrollModel();
        $this->orgUnitModel = new OrgUnitModel();
    }

    /**
     * TRANG CHỦ PHÂN HỆ TIỀN LƯƠNG
     */
    public function index(): void
    {
        $this->requireAuth();

        $userRole = Auth::role();
        // Nếu là nhân viên thông thường, tự động chuyển đến phiếu lương của chính mình
        if (strtoupper($userRole) === 'NHANVIEN') {
            $this->redirect('payroll/payslip');
            return;
        }

        $payrolls = $this->payrollModel->getPayrolls();
        $legalConfig = $this->payrollModel->getLegalConfig(date('Y-m-d'));
        $selectableUnits = $this->orgUnitModel->getSelectableUnitsForUser();

        $this->view('payroll/index', [
            'title'           => 'Quản lý Bảng Lương & Chi Trả - Vinamilk HRM',
            'payrolls'        => $payrolls,
            'legalConfig'     => $legalConfig,
            'selectableUnits' => $selectableUnits,
            'currentPeriod'   => date('Y-m')
        ]);
    }

    /**
     * XEM CHI TIẾT 1 BẢNG LƯƠNG KỲ
     */
    public function detail(int $id = 0): void
    {
        $this->requireRole(['ADMIN', 'BOD', 'HR_TAPDOAN', 'HR_DONVI', 'KETOAN_LUONG']);

        $payroll = $this->payrollModel->getPayrollWithDetails($id);
        if (!$payroll) {
            Session::setFlash('error', "Không tìm thấy bảng lương mã #{$id}", 'danger');
            $this->redirect('payroll');
        }

        $this->view('payroll/detail', [
            'title'   => $payroll['ten_bang_luong'],
            'payroll' => $payroll
        ]);
    }

    /**
     * THỰC THI TÍNH TOÁN LƯƠNG THEO THAM SỐ PHÁP LÝ (POST)
     */
    public function calculate(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'KETOAN_LUONG']);
        $this->validateCsrf();

        $kyLuong = trim($_POST['ky_luong'] ?? date('Y-m'));
        $maDV    = trim($_POST['ma_dv'] ?? 'VINAMILK');

        try {
            $payrollId = $this->payrollModel->calculateAndSaveBatch($kyLuong, $maDV);
            Session::setFlash('success', "Đã tính toán thành công bảng lương kỳ [{$kyLuong}] từ bảng tham số pháp lý cau_hinh_luong!", 'success');
            $this->redirect("payroll/detail/{$payrollId}");
        } catch (Exception $e) {
            Session::setFlash('error', "Lỗi tính toán lương: " . $e->getMessage(), 'danger');
            $this->redirect('payroll');
        }
    }

    /**
     * XEM PHIẾU LƯƠNG CÁ NHÂN (ESS)
     */
    public function payslip(string $maNV = '', string $kyLuong = ''): void
    {
        $this->requireAuth();

        if (empty($maNV) || !Auth::canViewAllUnits()) {
            $maNV = Auth::maNV() ?: 'VNM-0001';
        }

        $kyLuong = !empty($kyLuong) ? $kyLuong : date('Y-m');
        $payslip = $this->payrollModel->getEmployeePayslip($maNV, $kyLuong);

        // Fallback lấy kỳ gần nhất nếu kỳ hiện tại chưa tính
        if (!$payslip) {
            $payslip = $this->payrollModel->getEmployeePayslip($maNV, null);
        }

        $legalConfig = $this->payrollModel->getLegalConfig(date('Y-m-d'));

        $this->view('payroll/payslip', [
            'title'       => 'Phiếu Lương Điện Tử - Vinamilk Enterprise HRM',
            'payslip'     => $payslip,
            'legalConfig' => $legalConfig,
            'maNV'        => $maNV,
            'kyLuong'     => $kyLuong
        ]);
    }

    /**
     * IN PHIẾU LƯƠNG A4 CHUẨN DOANH NGHIỆP (@media print)
     */
    public function print_payslip(string $maNV = '', string $kyLuong = ''): void
    {
        $this->requireAuth();

        if (empty($maNV) || !Auth::canViewAllUnits()) {
            $maNV = Auth::maNV() ?: 'VNM-0001';
        }

        $payslip = $this->payrollModel->getEmployeePayslip($maNV, $kyLuong ?: null);
        $legalConfig = $this->payrollModel->getLegalConfig(date('Y-m-d'));

        $this->view('payroll/print_payslip', [
            'title'       => 'Phiếu Lương A4 - ' . ($payslip['ho_ten'] ?? $maNV),
            'payslip'     => $payslip,
            'legalConfig' => $legalConfig
        ], 'blank'); // Layout blank để in A4 chuẩn
    }

    /**
     * XEM BẢNG THAM SỐ PHÁP LÝ cau_hinh_luong VÀ BIỂU THUẾ
     */
    public function legal_params(): void
    {
        $this->requireAuth();

        $legalRows = $this->payrollModel->fetchAll("SELECT * FROM cau_hinh_luong ORDER BY ngay_hieu_luc DESC, ma_tham_so ASC");
        $taxRows   = $this->payrollModel->fetchAll("SELECT * FROM bieu_thue_tncn ORDER BY bac_thue ASC");

        $this->view('payroll/legal_params', [
            'title'     => 'Bảng Tham Số Pháp Lý & Biểu Thuế - Vinamilk HRM',
            'legalRows' => $legalRows,
            'taxRows'   => $taxRows
        ]);
    }

    /**
     * KẾ TOÁN DUYỆT BẢNG LƯƠNG (POST)
     */
    public function accountant_approve(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'KETOAN_LUONG']);
        $this->validateCsrf();

        $id = (int)($_POST['id'] ?? 0);
        try {
            $ok = $this->payrollModel->accountantApprove($id, Auth::username() ?? 'Kế toán trưởng');
            if ($ok) {
                Session::setFlash('success', "Kế toán đã duyệt bảng lương [#{$id}] thành công!", 'success');
            } else {
                Session::setFlash('error', "Không thể duyệt bảng lương (trạng thái không hợp lệ).", 'danger');
            }
        } catch (Exception $e) {
            Session::setFlash('error', $e->getMessage(), 'danger');
        }

        $this->redirect($_SERVER['HTTP_REFERER'] ?? "payroll/detail/{$id}");
    }

    /**
     * GIÁM ĐỐC DUYỆT BẢNG LƯƠNG (POST)
     */
    public function director_approve(): void
    {
        $this->requireRole(['ADMIN', 'BOD']);
        $this->validateCsrf();

        $id = (int)($_POST['id'] ?? 0);
        try {
            $ok = $this->payrollModel->directorApprove($id, Auth::username() ?? 'Tổng Giám đốc');
            if ($ok) {
                Session::setFlash('success', "Giám đốc đã phê duyệt bảng lương [#{$id}] thành công!", 'success');
            } else {
                Session::setFlash('error', "Không thể phê duyệt bảng lương (trạng thái không hợp lệ).", 'danger');
            }
        } catch (Exception $e) {
            Session::setFlash('error', $e->getMessage(), 'danger');
        }

        $this->redirect($_SERVER['HTTP_REFERER'] ?? "payroll/detail/{$id}");
    }

    /**
     * CHI TRẢ TIỀN LƯƠNG & KHÓA SỔ BẤT BIẾN (POST)
     */
    public function disburse(): void
    {
        $this->requireRole(['ADMIN', 'BOD', 'KETOAN_LUONG']);
        $this->validateCsrf();

        $id = (int)($_POST['id'] ?? 0);
        try {
            $ok = $this->payrollModel->disburse($id, Auth::username() ?? 'Thủ quỹ');
            if ($ok) {
                Session::setFlash('success', "ĐÃ CHI TRẢ LƯƠNG & KHÓA SỔ BẢNG LƯƠNG [#{$id}]! Kể từ thời điểm này, bảng lương bị khóa cứng không thể sửa đổi.", 'success');
            } else {
                Session::setFlash('error', "Không thể chi trả (bảng lương chưa được Giám đốc duyệt).", 'danger');
            }
        } catch (Exception $e) {
            Session::setFlash('error', $e->getMessage(), 'danger');
        }

        $this->redirect($_SERVER['HTTP_REFERER'] ?? "payroll/detail/{$id}");
    }

    /**
     * XUẤT PHIẾU LƯƠNG PDF / IN ẤN ĐIỆN TỬ CHO ESS
     */
    public function pdf_payslip(int $id = 0, string $maNV = ''): void
    {
        $this->requireAuth();

        if (empty($maNV) || !Auth::canViewAllUnits()) {
            $maNV = Auth::maNV() ?: 'VNM-0001';
        }

        $service = $this->payrollModel->getService();
        $payslipData = $service->generatePayslip($id, $maNV);

        if (!$payslipData) {
            Session::setFlash('error', "Không tìm thấy dữ liệu phiếu lương của nhân sự {$maNV} trong bảng lương #{$id}", 'danger');
            $this->redirect('payroll');
        }

        header('Content-Type: text/html; charset=utf-8');
        echo $service->renderPayslipHtml($payslipData);
        exit;
    }
}
