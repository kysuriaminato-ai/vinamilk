<?php
/**
 * =====================================================================
 * VINAMILK HRM - Training Controller (Quản lý Đào tạo & Phát triển)
 * =====================================================================
 * 
 * Controller quản lý Danh mục các Khóa Đào tạo, đăng ký học viên,
 * ghi nhận kết quả thi cử và tự động cấp chứng chỉ vào hồ sơ nhân viên.
 */

require_once APP_DIR . '/core/Controller.php';

class TrainingController extends Controller {

    /**
     * Danh sách Khóa Đào tạo & Học viên
     */
    public function index(): void {
        $this->requirePermission('DAO_TAO.View');

        /** @var TrainingModel $trainModel */
        $trainModel = $this->model('TrainingModel');

        $courses = $trainModel->getCourses();
        $records = $trainModel->getTrainingRecords();

        $this->view('training/index', [
            'title' => 'Quản lý Đào tạo & Phát triển Nhân lực - Vinamilk HRM',
            'courses' => $courses,
            'records' => $records
        ]);
    }

    /**
     * Form Tạo mới Khóa Đào tạo
     */
    public function create(): void {
        $this->requirePermission('DAO_TAO.Add');

        /** @var TrainingModel $trainModel */
        $trainModel = $this->model('TrainingModel');

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->validateCsrf();

            $data = [
                'MaKhoa' => trim($_POST['MaKhoa'] ?? ('KDT-' . rand(100, 999))),
                'TenKhoaHoc' => trim($_POST['TenKhoaHoc'] ?? ''),
                'LinhVuc' => $_POST['LinhVuc'] ?? 'TetraPak',
                'CoSoDaoTao' => trim($_POST['CoSoDaoTao'] ?? 'Tập đoàn Vinamilk'),
                'ThoiLuong' => trim($_POST['ThoiLuong'] ?? '24 Giờ'),
                'ChiPhi' => (float)($_POST['ChiPhi'] ?? 0),
                'MoTa' => trim($_POST['MoTa'] ?? '')
            ];

            $trainModel->createCourse($data);

            /** @var AuditLog $auditModel */
            $auditModel = $this->model('AuditLog');
            $auditModel->log('CreateCourse', 'Success', Auth::id(), Auth::user()['Username'], "Tạo mới khóa đào tạo {$data['TenKhoaHoc']} ({$data['MaKhoa']})");

            Session::setFlash('success', "Đã tạo thành công Khóa Đào tạo <strong>{$data['TenKhoaHoc']}</strong>!", 'success');
            $this->redirect('daotao');
        }

        $this->view('training/create_course', [
            'title' => 'Tạo Khóa Đào tạo Mới - Vinamilk HRM'
        ]);
    }

    /**
     * Đăng ký Học viên & Cấp Chứng chỉ Đào tạo
     */
    public function enroll(): void {
        $this->requirePermission('DAO_TAO.Add');

        /** @var TrainingModel $trainModel */
        $trainModel = $this->model('TrainingModel');

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->validateCsrf();

            $data = [
                'MaNV' => trim($_POST['MaNV'] ?? ''),
                'TenKhoaHoc' => trim($_POST['TenKhoaHoc'] ?? ''),
                'CoSoDaoTao' => trim($_POST['CoSoDaoTao'] ?? 'Tập đoàn Vinamilk'),
                'HinhThuc' => $_POST['HinhThuc'] ?? 'ChinhQuy',
                'TuNgay' => $_POST['TuNgay'] ?? date('Y-m-d'),
                'DenNgay' => $_POST['DenNgay'] ?? date('Y-m-d'),
                'BangCap_ChungChi' => trim($_POST['BangCap_ChungChi'] ?? 'Chứng chỉ Hoàn thành Khóa học Vinamilk'),
                'XepLoai' => $_POST['XepLoai'] ?? 'Giỏi',
                'GhiChu' => trim($_POST['GhiChu'] ?? '')
            ];

            $trainModel->enrollAndComplete($data);

            /** @var AuditLog $auditModel */
            $auditModel = $this->model('AuditLog');
            $auditModel->log('EnrollTraining', 'Success', Auth::id(), Auth::user()['Username'], "Cấp chứng chỉ khóa {$data['TenKhoaHoc']} cho nhân viên {$data['MaNV']}");

            Session::setFlash('success', "Đã cấp thành công Chứng chỉ Đào tạo cho nhân viên <strong>{$data['MaNV']}</strong>!", 'success');
            $this->redirect('daotao');
        }
    }
}
