<?php
/**
 * =====================================================================
 * VINAMILK HRM - Payroll Controller (Phân hệ Bảng lương 3P & Payslip)
 * =====================================================================
 * 
 * Controller tính toán tự động Bảng lương tháng Vinamilk 3P,
 * kế toán kiểm tra, chốt khóa bảng lương (Lock Payroll) và xuất Phiếu lương (Payslip).
 */

require_once APP_DIR . '/core/Controller.php';

class PayrollController extends Controller {

    /**
     * Bảng Lương Tháng tổng hợp
     */
    public function index(): void {
        $this->requirePermission('LUONG.View');

        /** @var PayrollModel $payrollModel */
        $payrollModel = $this->model('PayrollModel');
        /** @var EmployeeModel $empModel */
        $empModel = $this->model('EmployeeModel');

        $thang = (int)($_GET['thang'] ?? date('m'));
        $nam = (int)($_GET['nam'] ?? date('Y'));
        $maDV = trim($_GET['ma_dv'] ?? '');

        // Lấy danh sách bảng lương từ CSDL
        $payrollList = $payrollModel->getMonthlyPayroll($thang, $nam, $maDV);

        // Nếu chưa tính lương tháng này -> Tự động tính toán mẫu
        if (empty($payrollList)) {
            $payrollModel->calculateMonthlyPayroll($thang, $nam);
            $payrollList = $payrollModel->getMonthlyPayroll($thang, $nam, $maDV);
        }

        $options = $empModel->getDropdownOptions();

        $this->view('payroll/index', [
            'title' => "Bảng Lương Tháng {$thang}/{$nam} (3P Engine) - Vinamilk HRM",
            'payrollList' => $payrollList,
            'thang' => $thang,
            'nam' => $nam,
            'maDV' => $maDV,
            'options' => $options
        ]);
    }

    /**
     * Thực hiện Tính toán lại Bảng Lương Tháng (Recalculate)
     */
    public function calculate(): void {
        $this->requirePermission('LUONG.Add');
        $this->validateCsrf();

        $thang = (int)($_POST['thang'] ?? date('m'));
        $nam = (int)($_POST['nam'] ?? date('Y'));

        /** @var PayrollModel $payrollModel */
        $payrollModel = $this->model('PayrollModel');
        $payrollModel->calculateMonthlyPayroll($thang, $nam);

        /** @var AuditLog $auditModel */
        $auditModel = $this->model('AuditLog');
        $auditModel->log('CalculatePayroll', 'Success', Auth::id(), Auth::user()['Username'], "Tính toán lại Bảng lương tháng {$thang}/{$nam}");

        Session::setFlash('success', "Đã tính toán hoàn tất Bảng lương 3P tháng <strong>{$thang}/{$nam}</strong>!", 'success');
        $this->redirect("luong?thang={$thang}&nam={$nam}");
    }

    /**
     * Khóa Bảng lương (Lock Payroll) bởi Kế toán
     */
    public function lock(): void {
        $this->requirePermission('LUONG.Approve');
        $this->validateCsrf();

        $thang = (int)($_POST['thang'] ?? date('m'));
        $nam = (int)($_POST['nam'] ?? date('Y'));

        /** @var PayrollModel $payrollModel */
        $payrollModel = $this->model('PayrollModel');
        $success = $payrollModel->lockPayroll($thang, $nam, Auth::user()['Username']);

        /** @var AuditLog $auditModel */
        $auditModel = $this->model('AuditLog');

        if ($success) {
            $auditModel->log('LockPayroll', 'Success', Auth::id(), Auth::user()['Username'], "Khóa Bảng lương tháng {$thang}/{$nam}");
            Session::setFlash('success', "Đã Khóa thành công Bảng lương tháng <strong>{$thang}/{$nam}</strong>! Không thể chỉnh sửa thêm.", 'success');
        } else {
            Session::setFlash('error', 'Có lỗi xảy ra khi khóa bảng lương.', 'danger');
        }

        $this->redirect("luong?thang={$thang}&nam={$nam}");
    }

    /**
     * Phiếu lương Điện tử Cá nhân (Payslip Detail & Print)
     */
    public function payslip(mixed $maNV = null, mixed $thang = null, mixed $nam = null): void {
        $this->requirePermission('LUONG.View');

        if (!$maNV) {
            $maNV = Auth::maNV();
        }

        $thang = (int)($thang ?? $_GET['thang'] ?? date('m'));
        $nam = (int)($nam ?? $_GET['nam'] ?? date('Y'));

        /** @var PayrollModel $payrollModel */
        $payrollModel = $this->model('PayrollModel');
        $payslip = $payrollModel->getPayslip($maNV, $thang, $nam);

        if (!$payslip) {
            Session::setFlash('error', "Không tìm thấy Phiếu lương của mã {$maNV} tháng {$thang}/{$nam}.", 'danger');
            $this->redirect('luong');
        }

        // Render trang Phiếu lương riêng biệt
        $this->view('payroll/payslip', [
            'title' => "Phiếu lương cá nhân: {$payslip['HoTen']} (T{$thang}/{$nam})",
            'ps' => $payslip
        ], null);
    }
}
