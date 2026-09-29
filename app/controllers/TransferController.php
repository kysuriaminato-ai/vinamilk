<?php
/**
 * =====================================================================
 * VINAMILK HRM - Transfer Controller (Thuyên chuyển & Bổ nhiệm)
 * =====================================================================
 * 
 * Controller xử lý việc lập Đề xuất, Quyết định Điều động nhân sự 
 * giữa các Đơn vị/Nhà máy/Trang trại Vinamilk và In ấn Quyết định chuẩn.
 */

require_once APP_DIR . '/core/Controller.php';

class TransferController extends Controller {

    /**
     * Danh sách Lịch sử Thuyên chuyển công tác
     */
    public function index(): void {
        $this->requirePermission('NHANSU.View');

        /** @var TransferModel $transferModel */
        $transferModel = $this->model('TransferModel');
        $transfers = $transferModel->getAllTransfers();

        $this->view('transfers/index', [
            'title' => 'Quản lý Thuyên chuyển công tác & Bổ nhiệm - Vinamilk HRM',
            'transfers' => $transfers
        ]);
    }

    /**
     * Form Lập Quyết định Thuyên chuyển công tác mới
     */
    public function create(): void {
        $this->requirePermission('NHANSU.Edit');

        /** @var TransferModel $transferModel */
        $transferModel = $this->model('TransferModel');
        /** @var EmployeeModel $empModel */
        $empModel = $this->model('EmployeeModel');

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->validateCsrf();

            $maNV = trim($_POST['MaNV'] ?? '');
            $emp = $empModel->find($maNV);

            if (!$emp) {
                Session::setFlash('error', 'Mã Nhân viên không tồn tại trong hệ thống.', 'danger');
                $this->redirect('tuyenchuyen/create');
            }

            $soQD = trim($_POST['SoQD'] ?? '');
            if (empty($soQD)) {
                $soQD = $transferModel->generateDecisionNumber();
            }

            $data = [
                'MaNV' => $maNV,
                'SoQD' => $soQD,
                'DonViCu' => $emp['MaDV'],
                'DonViMoi' => $_POST['DonViMoi'] ?? '',
                'ChucVuCu' => $emp['MaCV'] ?? 'Chuyên viên',
                'ChucVuMoi' => trim($_POST['ChucVuMoi'] ?? ''),
                'NgayHieuLuc' => $_POST['NgayHieuLuc'] ?? date('Y-m-d'),
                'LyDo' => trim($_POST['LyDo'] ?? ''),
                'GhiChu' => trim($_POST['GhiChu'] ?? '')
            ];

            $success = $transferModel->createTransferDecision($data);

            /** @var AuditLog $auditModel */
            $auditModel = $this->model('AuditLog');

            if ($success) {
                $auditModel->log('CreateTransfer', 'Success', Auth::id(), Auth::user()['Username'], "Ban hành Quyết định Thuyên chuyển {$soQD} cho nhân viên {$maNV}");
                Session::setFlash('success', "Đã ban hành thành công Quyết định Thuyên chuyển <strong>{$soQD}</strong>!", 'success');
                $this->redirect('tuyenchuyen');
            } else {
                $auditModel->log('CreateTransfer', 'Failed', Auth::id(), Auth::user()['Username'], "Thất bại khi lập Quyết định thuyên chuyển cho {$maNV}");
                Session::setFlash('error', 'Có lỗi xảy ra trong quá trình lưu Quyết định thuyên chuyển.', 'danger');
            }
        }

        $options = $empModel->getDropdownOptions();
        $employees = $empModel->all('HoTen', 'ASC');
        $autoSoQD = $transferModel->generateDecisionNumber();

        $this->view('transfers/create', [
            'title' => 'Lập Quyết định Thuyên chuyển công tác - Vinamilk HRM',
            'options' => $options,
            'employees' => $employees,
            'autoSoQD' => $autoSoQD
        ]);
    }

    /**
     * In Quyết định Điều động / Thuyên chuyển công tác (Biểu mẫu pháp lý Vinamilk)
     */
    public function print(mixed $id = null): void {
        $this->requirePermission('NHANSU.View');

        if (!$id) {
            $this->redirect('tuyenchuyen');
        }

        /** @var TransferModel $transferModel */
        $transferModel = $this->model('TransferModel');
        $decision = $transferModel->getTransferById((int)$id);

        if (!$decision) {
            Session::setFlash('error', 'Không tìm thấy dữ liệu Quyết định điều động.', 'danger');
            $this->redirect('tuyenchuyen');
        }

        // Render trang in độc lập không nhúng master layout
        $this->view('transfers/print_qd', [
            'title' => "In Quyết định Điều động: {$decision['SoQD']}",
            'qd' => $decision
        ], null);
    }
}
