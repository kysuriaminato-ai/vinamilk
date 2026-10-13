<?php
/**
 * =====================================================================
 * VINAMILK HRM - Recruitment Controller (Quản lý Tuyển dụng & Ung viên)
 * =====================================================================
 * 
 * Controller xử lý Đề xuất tuyển dụng, Đăng tin tuyển dụng, 
 * Quản lý Hồ sơ ứng viên (Kanban Board), Chấm điểm phỏng vấn và Chuyển thành Nhân viên mới.
 */

require_once APP_DIR . '/core/Controller.php';

class RecruitmentController extends Controller {

    /**
     * View Kanban / Danh sách Tin tuyển dụng & Hồ sơ ứng viên
     */
    public function index(): void {
        $this->requirePermission('TUYEN_DUNG.View');

        /** @var RecruitmentModel $recModel */
        $recModel = $this->model('RecruitmentModel');

        $jobs = $recModel->getAllJobs();
        $candidates = $recModel->getCandidates();
        $requests = $recModel->getRecruitmentRequests();

        $this->view('recruitment/index', [
            'title' => 'Quản lý Tuyển dụng & Hồ sơ Ứng viên - Vinamilk HRM',
            'jobs' => $jobs,
            'candidates' => $candidates,
            'requests' => $requests
        ]);
    }

    /**
     * Form Đăng tin Tuyển dụng mới / Lập Đề xuất
     */
    public function create(): void {
        $this->requirePermission('TUYEN_DUNG.Add');

        /** @var RecruitmentModel $recModel */
        $recModel = $this->model('RecruitmentModel');
        /** @var EmployeeModel $empModel */
        $empModel = $this->model('EmployeeModel');

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->validateCsrf();

            $data = [
                'TieuDe' => trim($_POST['TieuDe'] ?? ''),
                'MaDV' => $_POST['MaDV'] ?? 'VINAMILK',
                'ViTriTuyen' => trim($_POST['ViTriTuyen'] ?? ''),
                'SoLuong' => (int)($_POST['SoLuong'] ?? 1),
                'MoTaCongViec' => trim($_POST['MoTaCongViec'] ?? ''),
                'YeuCau' => trim($_POST['YeuCau'] ?? ''),
                'MucLuong' => trim($_POST['MucLuong'] ?? 'Thỏa thuận'),
                'NoiLamViec' => trim($_POST['NoiLamViec'] ?? 'TP.HCM'),
                'HanNop' => $_POST['HanNop'] ?? date('Y-m-d', strtotime('+30 days')),
                'NguoiTao' => Auth::maNV(),
                'TrangThai' => 'DangTuyen'
            ];

            $recModel->insert($data);

            /** @var AuditLog $auditModel */
            $auditModel = $this->model('AuditLog');
            $auditModel->log('CreateJobPosting', 'Success', Auth::id(), Auth::user()['Username'], "Đăng tin tuyển dụng mới: {$data['TieuDe']}");

            Session::setFlash('success', "Đã đăng thành công Tin tuyển dụng <strong>{$data['TieuDe']}</strong>!", 'success');
            $this->redirect('tuyendung');
        }

        $options = $empModel->getDropdownOptions();

        $this->view('recruitment/create_job', [
            'title' => 'Tạo Tin Tuyển dụng mới - Vinamilk HRM',
            'options' => $options
        ]);
    }

    /**
     * Xem Chi tiết Ứng viên, Chấm điểm phỏng vấn & Chuyển thành Nhân viên chính thức
     */
    public function candidate(mixed $maUV = null): void {
        $this->requirePermission('TUYEN_DUNG.View');

        if (!$maUV) {
            $this->redirect('tuyendung');
        }

        /** @var RecruitmentModel $recModel */
        $recModel = $this->model('RecruitmentModel');
        /** @var EmployeeModel $empModel */
        $empModel = $this->model('EmployeeModel');

        $candidates = $recModel->getCandidates(null, null);
        $candidate = null;
        foreach ($candidates as $c) {
            if ($c['MaUV'] == $maUV) {
                $candidate = $c;
                break;
            }
        }

        if (!$candidate) {
            Session::setFlash('error', 'Không tìm thấy thông tin ứng viên.', 'danger');
            $this->redirect('tuyendung');
        }

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->requirePermission('TUYEN_DUNG.Approve');
            $this->validateCsrf();

            $maDV = $_POST['MaDV'] ?? 'VINAMILK';
            $maCV = $_POST['MaCV'] ?? 'NV';

            $newMaNV = $recModel->convertCandidateToEmployee((int)$maUV, [
                'HoTen' => $candidate['HoTen'],
                'NgaySinh' => $candidate['NgaySinh'] ?? '1995-01-01',
                'GioiTinh' => $candidate['GioiTinh'] ?? 'Nam',
                'Email' => $candidate['Email'],
                'SoDienThoai' => $candidate['SoDienThoai'],
                'MaDV' => $maDV,
                'MaCV' => $maCV
            ]);

            /** @var AuditLog $auditModel */
            $auditModel = $this->model('AuditLog');

            if ($newMaNV !== false) {
                $auditModel->log('ConvertCandidate', 'Success', Auth::id(), Auth::user()['Username'], "Duyệt trúng tuyển và tạo Mã NV {$newMaNV} cho ứng viên {$candidate['HoTen']}");
                Session::setFlash('success', "Đã duyệt Trúng tuyển thành công cho <strong>{$candidate['HoTen']}</strong>! Đã tạo Mã Nhân viên mới <strong>{$newMaNV}</strong> (Trạng thái Thử việc).", 'success');
                $this->redirect("nhansu/profile/{$newMaNV}");
            } else {
                Session::setFlash('error', 'Có lỗi xảy ra khi chuyển ứng viên thành nhân viên.', 'danger');
                $this->redirect("tuyendung/candidate/{$maUV}");
            }
        }

        $options = $empModel->getDropdownOptions();

        $this->view('recruitment/candidate_detail', [
            'title' => "Hồ sơ Ứng viên: {$candidate['HoTen']} - Vinamilk HRM",
            'cand' => $candidate,
            'options' => $options
        ]);
    }
}
