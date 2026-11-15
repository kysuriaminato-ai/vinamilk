<?php
/**
 * =====================================================================
 * VINAMILK HRM - Payroll Controller (Phân hệ Bảng lương 3P & Payslip)
 * =====================================================================
 *
 *  luong                      Bảng lương tháng của tất cả nhân viên
 *  luong/detail/{MaNV}        Chi tiết từng bước tính lương của một nhân viên
 *  luong/history/{MaNV}       Lịch sử lương (các kỳ đã trả + diễn biến hệ số lương)
 *  luong/payslip/{MaNV}       Phiếu lương in được
 *  luong/export               Xuất bảng lương tháng ra CSV (Excel)
 *  luong/calculate, luong/lock  Tính lại / khóa bảng lương (POST)
 */

require_once APP_DIR . '/core/Controller.php';

class PayrollController extends Controller {

    private function period(): array {
        $thang = (int)($_GET['thang'] ?? date('m'));
        $nam = (int)($_GET['nam'] ?? date('Y'));
        if ($thang < 1 || $thang > 12) $thang = (int)date('m');
        if ($nam < 2000 || $nam > 2100) $nam = (int)date('Y');
        return [$thang, $nam];
    }

    /**
     * Bảng Lương Tháng tổng hợp - tất cả nhân viên
     */
    public function index(): void {
        $this->requirePermission('LUONG.View');

        /** @var PayrollModel $payrollModel */
        $payrollModel = $this->model('PayrollModel');
        /** @var EmployeeModel $empModel */
        $empModel = $this->model('EmployeeModel');

        [$thang, $nam] = $this->period();
        $maDV = trim($_GET['ma_dv'] ?? '');
        $q = trim($_GET['q'] ?? '');

        // Chưa tính, hoặc còn nhân viên chưa có dòng lương và bảng chưa khóa -> tính (bổ sung) tự động
        $status = $payrollModel->getPeriodStatus($thang, $nam);
        if ($status !== 'locked') {
            $daCo = count($payrollModel->getMonthlyPayroll($thang, $nam));
            if ($daCo < $payrollModel->countPayableEmployees($thang, $nam)) {
                $payrollModel->calculateMonthlyPayroll($thang, $nam);
                $status = $payrollModel->getPeriodStatus($thang, $nam);
            }
        }

        $payrollList = $payrollModel->getMonthlyPayroll($thang, $nam, $maDV, $q);
        $options = $empModel->getDropdownOptions();

        $this->view('payroll/index', [
            'title' => "Bảng Lương Tháng {$thang}/{$nam} (3P) - Vinamilk HRM",
            'payrollList' => $payrollList,
            'thang' => $thang,
            'nam' => $nam,
            'maDV' => $maDV,
            'q' => $q,
            'isLocked' => $status === 'locked',
            'options' => $options,
        ]);
    }

    /**
     * Chi tiết tính lương của một nhân viên trong một tháng
     */
    public function detail(mixed $maNV = null): void {
        $this->requirePermission('LUONG.View');
        $maNV = $maNV ?: Auth::maNV();
        [$thang, $nam] = $this->period();

        /** @var PayrollModel $payrollModel */
        $payrollModel = $this->model('PayrollModel');
        $emp = $payrollModel->getEmployeeInfo((string)$maNV);
        if (!$emp) {
            Session::setFlash('error', "Không tìm thấy nhân viên mã {$maNV}.", 'danger');
            $this->redirect("luong?thang={$thang}&nam={$nam}");
        }

        $inputs = $payrollModel->getPayrollInputs($thang, $nam, (string)$maNV);
        $calc = null;
        if ($inputs) {
            $ktkl = $payrollModel->getKtklMap($thang, $nam, (string)$maNV);
            $calc = $payrollModel->computeEmployee($inputs[0], $ktkl[$maNV] ?? [], $thang, $nam);
        }
        $stored = $payrollModel->getPayslip((string)$maNV, $thang, $nam);
        $periods = $payrollModel->getPayrollHistory((string)$maNV);

        $this->view('payroll/detail', [
            'periods' => $periods,
            'quarters' => $payrollModel->summarizeByQuarter($periods),
            'grades' => $payrollModel->getSalaryGradeHistory((string)$maNV),
            'tab' => preg_replace('/[^a-z]/', '', $_GET['tab'] ?? 'calc') ?: 'calc',
            'title' => "Chi tiết tính lương: {$emp['HoTen']} (T{$thang}/{$nam})",
            'emp' => $emp,
            'calc' => $calc,
            'stored' => $stored,
            'thang' => $thang,
            'nam' => $nam,
        ]);
    }

    /**
     * Lịch sử lương của một nhân viên
     */
    public function history(mixed $maNV = null): void {
        // Lịch sử lương nay nằm trong trang chi tiết nhân viên (tab "Lịch sử theo quý")
        $this->requirePermission('LUONG.View');
        $maNV = $maNV ?: Auth::maNV();
        $this->redirect('luong/detail/' . rawurlencode((string)$maNV) . '?tab=quarter');
    }

    /**
     * Tìm nhân viên (JSON) cho ô tìm kiếm: luong/search?q=...
     */
    public function search(): void {
        $this->requirePermission('LUONG.View');
        $q = trim($_GET['q'] ?? '');
        if (mb_strlen($q) < 1) {
            $this->json(['data' => []]);
        }
        /** @var PayrollModel $payrollModel */
        $payrollModel = $this->model('PayrollModel');
        $this->json(['data' => $payrollModel->searchEmployees($q)]);
    }

    /**
     * Xuất bảng lương tháng ra CSV (mở được bằng Excel)
     */
    public function export(): void {
        $this->requirePermission('LUONG.View');
        [$thang, $nam] = $this->period();

        /** @var PayrollModel $payrollModel */
        $payrollModel = $this->model('PayrollModel');
        $rows = $payrollModel->getMonthlyPayroll($thang, $nam, trim($_GET['ma_dv'] ?? ''), trim($_GET['q'] ?? ''));

        header('Content-Type: text/csv; charset=utf-8');
        header("Content-Disposition: attachment; filename=\"bang-luong-{$thang}-{$nam}.csv\"");
        $out = fopen('php://output', 'w');
        fwrite($out, "\xEF\xBB\xBF");
        fputcsv($out, ['Mã NV', 'Họ tên', 'Đơn vị', 'Chức vụ', 'Công', 'Hệ số', 'P1 Lương vị trí', 'P2 Năng lực', 'P3 KPI',
            'PC chức vụ', 'PC khác', 'Tăng ca + thưởng', 'Tổng thu nhập', 'Bảo hiểm NLĐ', 'Thuế TNCN', 'Khấu trừ khác', 'Thực lĩnh', 'Trạng thái', 'Ghi chú']);
        foreach ($rows as $r) {
            fputcsv($out, [$r['MaNV'], $r['HoTen'], $r['TenDV'], $r['TenCV'], $r['SoNgayCong'], $r['HeSoLuong'], $r['LuongCoBan'],
                $r['LuongNangLuc'], $r['ThuongKPI'], $r['PhuCapChucVu'], $r['PhuCapKhac'], $r['ThuongKhac'], $r['TongThuNhap'],
                $r['KhauTruBHXH'], $r['ThueTNCN'], $r['KhauTruKhac'], $r['ThucLinh'], $r['TrangThaiDuyet'], $r['GhiChu'] ?? '']);
        }
        fclose($out);
        exit;
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
        if ($payrollModel->getPeriodStatus($thang, $nam) === 'locked') {
            Session::setFlash('error', "Bảng lương tháng {$thang}/{$nam} đã khóa, không thể tính lại.", 'danger');
            $this->redirect("luong?thang={$thang}&nam={$nam}");
        }
        $rows = $payrollModel->calculateMonthlyPayroll($thang, $nam);

        /** @var AuditLog $auditModel */
        $auditModel = $this->model('AuditLog');
        $auditModel->log('CalculatePayroll', 'Success', Auth::id(), Auth::user()['Username'], "Tính toán lại Bảng lương tháng {$thang}/{$nam}");

        Session::setFlash('success', "Đã tính lại Bảng lương 3P tháng <strong>{$thang}/{$nam}</strong> cho " . count($rows) . " nhân viên.", 'success');
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
        $payslip = $payrollModel->getPayslip((string)$maNV, $thang, $nam);

        if (!$payslip) {
            Session::setFlash('error', "Không tìm thấy Phiếu lương của mã {$maNV} tháng {$thang}/{$nam}.", 'danger');
            $this->redirect('luong');
        }

        $this->view('payroll/payslip', [
            'title' => "Phiếu lương cá nhân: {$payslip['HoTen']} (T{$thang}/{$nam})",
            'ps' => $payslip
        ], null);
    }
}
