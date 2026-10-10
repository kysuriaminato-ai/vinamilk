<?php
/**
 * =====================================================================
 * VINAMILK HRM - Reward & Discipline Controller (Khen thưởng & Kỷ luật)
 * =====================================================================
 * 
 * Controller quản lý Thành tích, Khen thưởng thi đua và Xử lý Kỷ luật.
 * Tự động tạo Quyết định khen thưởng/kỷ luật và in ấn văn bản pháp lý.
 */

require_once APP_DIR . '/core/Controller.php';

class RewardDisciplineController extends Controller {

    /**
     * Danh sách Khen thưởng & Kỷ luật
     */
    public function index(): void {
        $this->requirePermission('NHANSU.View');

        /** @var RewardDisciplineModel $ktklModel */
        $ktklModel = $this->model('RewardDisciplineModel');
        $loaiFilter = $_GET['loai'] ?? null;

        $list = $ktklModel->getAllKTKL($loaiFilter);

        $this->view('rewards/index', [
            'title' => 'Quản lý Khen thưởng & Kỷ luật - Vinamilk HRM',
            'list' => $list,
            'currentLoai' => $loaiFilter
        ]);
    }

    /**
     * Form Ghi nhận Khen thưởng / Kỷ luật mới
     */
    public function create(): void {
        $this->requirePermission('NHANSU.Edit');

        /** @var RewardDisciplineModel $ktklModel */
        $ktklModel = $this->model('RewardDisciplineModel');
        /** @var EmployeeModel $empModel */
        $empModel = $this->model('EmployeeModel');

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->validateCsrf();

            $maNV = trim($_POST['MaNV'] ?? '');
            $emp = $empModel->find($maNV);

            if (!$emp) {
                Session::setFlash('error', 'Mã Nhân viên không tồn tại.', 'danger');
                $this->redirect('ktkl/create');
            }

            $loai = $_POST['Loai'] ?? 'KhenThuong';
            $soQD = trim($_POST['SoQD'] ?? '');
            if (empty($soQD)) {
                $soQD = $ktklModel->generateDecisionNumber($loai);
            }

            $data = [
                'MaNV' => $maNV,
                'MaKTKL' => $_POST['MaKTKL'] ?? null,
                'SoQD' => $soQD,
                'NgayQD' => $_POST['NgayQD'] ?? date('Y-m-d'),
                'HinhThuc' => trim($_POST['HinhThuc'] ?? ''),
                'LyDo' => trim($_POST['LyDo'] ?? ''),
                'CapKhenThuong' => trim($_POST['CapKhenThuong'] ?? 'Công ty CP Sữa Việt Nam'),
                'GiaTri' => (float)($_POST['GiaTri'] ?? 0),
                'GhiChu' => trim($_POST['GhiChu'] ?? '')
            ];

            $success = $ktklModel->insert($data);

            /** @var AuditLog $auditModel */
            $auditModel = $this->model('AuditLog');

            if ($success !== false) {
                $auditModel->log('CreateKTKL', 'Success', Auth::id(), Auth::user()['Username'], "Tạo Quyết định KTKL {$soQD} cho nhân viên {$maNV}");
                Session::setFlash('success', "Đã ban hành thành công Quyết định <strong>{$soQD}</strong>!", 'success');
                $this->redirect('ktkl');
            } else {
                $auditModel->log('CreateKTKL', 'Failed', Auth::id(), Auth::user()['Username'], "Thất bại khi lập Quyết định KTKL cho {$maNV}");
                Session::setFlash('error', 'Có lỗi xảy ra khi tạo Quyết định Khen thưởng / Kỷ luật.', 'danger');
            }
        }

        $danhmuc = $ktklModel->getDanhMucKTKL();
        $employees = $empModel->all('HoTen', 'ASC');
        $autoSoQD = $ktklModel->generateDecisionNumber('KhenThuong');

        $this->view('rewards/create', [
            'title' => 'Ghi nhận Khen thưởng / Kỷ luật - Vinamilk HRM',
            'danhmuc' => $danhmuc,
            'employees' => $employees,
            'autoSoQD' => $autoSoQD
        ]);
    }

    /**
     * In Quyết định Khen thưởng / Kỷ luật
     */
    public function print(mixed $id = null): void {
        $this->requirePermission('NHANSU.View');

        if (!$id) {
            $this->redirect('ktkl');
        }

        /** @var RewardDisciplineModel $ktklModel */
        $ktklModel = $this->model('RewardDisciplineModel');
        $decision = $ktklModel->getKTKLById((int)$id);

        if (!$decision) {
            Session::setFlash('error', 'Không tìm thấy dữ liệu Quyết định.', 'danger');
            $this->redirect('ktkl');
        }

        $this->view('rewards/print_qd', [
            'title' => "In Quyết định KTKL: {$decision['SoQD']}",
            'qd' => $decision
        ], null);
    }
}
