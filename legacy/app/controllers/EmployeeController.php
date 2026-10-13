<?php
/**
 * =====================================================================
 * VINAMILK HRM - Employee Controller (Quản lý Hồ sơ & Vòng đời Nhân sự)
 * =====================================================================
 * 
 * Controller xử lý toàn bộ nghiệp vụ Quản lý Sơ yếu lý lịch số hóa,
 * phân trang lọc đa điều kiện, duyệt thử việc, nghỉ hưu, thôi việc và in ấn Sơ yếu lý lịch 2C.
 */

require_once APP_DIR . '/core/Controller.php';

class EmployeeController extends Controller {

    /**
     * Danh sách Nhân sự với bộ lọc nâng cao và phân trang Ajax/Request
     */
    public function index(): void {
        $this->requirePermission('NHANSU.View');

        /** @var EmployeeModel $empModel */
        $empModel = $this->model('EmployeeModel');

        // Lấy các tham số lọc từ GET
        $filters = [
            'keyword' => trim($_GET['keyword'] ?? ''),
            'ma_dv' => trim($_GET['ma_dv'] ?? ''),
            'ma_cv' => trim($_GET['ma_cv'] ?? ''),
            'ma_ngach' => trim($_GET['ma_ngach'] ?? ''),
            'trang_thai' => trim($_GET['trang_thai'] ?? ''),
            'gioi_tinh' => trim($_GET['gioi_tinh'] ?? '')
        ];

        $page = max(1, (int)($_GET['page'] ?? 1));
        $perPage = 12;

        $result = $empModel->getFilteredEmployees($filters, $page, $perPage);
        $options = $empModel->getDropdownOptions();

        $this->view('employees/index', [
            'title' => 'Quản lý Hồ sơ Nhân sự Số hóa - Vinamilk HRM',
            'employees' => $result['data'],
            'pagination' => [
                'total' => $result['total'],
                'page' => $result['page'],
                'perPage' => $result['perPage'],
                'totalPages' => $result['totalPages']
            ],
            'filters' => $filters,
            'options' => $options
        ]);
    }

    /**
     * View Hồ sơ Nhân viên 360 độ (Profile chi tiết số hóa)
     */
    public function profile(mixed $maNV = null): void {
        $this->requirePermission('NHANSU.View');

        if (!$maNV) {
            $this->redirect('nhansu');
        }

        /** @var EmployeeModel $empModel */
        $empModel = $this->model('EmployeeModel');
        $employee = $empModel->getEmployeeByMaNV($maNV);

        if (!$employee) {
            Session::setFlash('error', "Không tìm thấy hồ sơ nhân viên có mã {$maNV}.", 'danger');
            $this->redirect('nhansu');
        }

        $this->view('employees/profile', [
            'title' => "Hồ sơ Nhân sự: {$employee['HoTen']} ({$employee['MaNV']}) - Vinamilk HRM",
            'emp' => $employee
        ]);
    }

    /**
     * Form Thêm mới Hồ sơ Nhân sự 4 Tabs
     */
    public function create(): void {
        $this->requirePermission('NHANSU.Add');

        /** @var EmployeeModel $empModel */
        $empModel = $this->model('EmployeeModel');

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->validateCsrf();

            $maNV = trim($_POST['MaNV'] ?? '');
            if (empty($maNV)) {
                $maNV = $empModel->generateNextMaNV();
            }

            // Xử lý Upload Ảnh chân dung
            $avatarPath = null;
            if (!empty($_FILES['AnhChanDung']['name'])) {
                $uploadDir = PUBLIC_DIR . '/uploads/avatars/';
                if (!is_dir($uploadDir)) {
                    mkdir($uploadDir, 0777, true);
                }
                $ext = pathinfo($_FILES['AnhChanDung']['name'], PATHINFO_EXTENSION);
                $filename = 'avatar_' . strtolower($maNV) . '_' . time() . '.' . $ext;
                if (move_uploaded_file($_FILES['AnhChanDung']['tmp_name'], $uploadDir . $filename)) {
                    $avatarPath = 'uploads/avatars/' . $filename;
                }
            }

            // Mảng dữ liệu nhân sự mới
            $data = [
                'MaNV' => $maNV,
                'HoTen' => trim($_POST['HoTen'] ?? ''),
                'TenThuongGoi' => trim($_POST['TenThuongGoi'] ?? ''),
                'NgaySinh' => $_POST['NgaySinh'] ?? date('Y-m-d'),
                'GioiTinh' => $_POST['GioiTinh'] ?? 'Nam',
                'SoCMND_CCCD' => trim($_POST['SoCMND_CCCD'] ?? ''),
                'NgayCap' => !empty($_POST['NgayCap']) ? $_POST['NgayCap'] : null,
                'NoiCap' => trim($_POST['NoiCap'] ?? ''),
                'QueQuan' => trim($_POST['QueQuan'] ?? ''),
                'NoiDKKTTru' => trim($_POST['NoiDKKTTru'] ?? ''),
                'DiaChiHienTai' => trim($_POST['DiaChiHienTai'] ?? ''),
                'DanToc' => trim($_POST['DanToc'] ?? 'Kinh'),
                'TonGiao' => trim($_POST['TonGiao'] ?? 'Không'),
                'TinhTrangHonNhan' => $_POST['TinhTrangHonNhan'] ?? 'Độc thân',
                'Email' => trim($_POST['Email'] ?? ''),
                'SoDienThoai' => trim($_POST['SoDienThoai'] ?? ''),
                'AnhChanDung' => $avatarPath,
                'NgayVaoLam' => $_POST['NgayVaoLam'] ?? date('Y-m-d'),
                'MaDV' => $_POST['MaDV'] ?? '',
                'MaCV' => $_POST['MaCV'] ?? null,
                'MaNgach' => $_POST['MaNgach'] ?? null,
                'HeSoLuongHienTai' => (float)($_POST['HeSoLuongHienTai'] ?? 1.00),
                'BacLuongHienTai' => (int)($_POST['BacLuongHienTai'] ?? 1),
                'TrinhDoHocVan' => trim($_POST['TrinhDoHocVan'] ?? ''),
                'ChuyenNganh' => trim($_POST['ChuyenNganh'] ?? ''),
                'TruongDaoTao' => trim($_POST['TruongDaoTao'] ?? ''),
                'DangVien' => isset($_POST['DangVien']) ? 1 : 0,
                'DoanVien' => isset($_POST['DoanVien']) ? 1 : 0,
                'ViTriLuuHoSo' => trim($_POST['ViTriLuuHoSo'] ?? 'Kho VP / Tủ 01'),
                'LoaiHopDong' => trim($_POST['LoaiHopDong'] ?? 'Thử việc'),
                'TrangThai' => (int)($_POST['TrangThai'] ?? 2) // Default 2=Thử việc
            ];

            $success = $empModel->insert($data);

            /** @var AuditLog $auditModel */
            $auditModel = $this->model('AuditLog');

            if ($success !== false) {
                $auditModel->log('CreateEmployee', 'Success', Auth::id(), Auth::user()['Username'], "Thêm mới thành công hồ sơ nhân viên {$data['HoTen']} ({$maNV})");
                Session::setFlash('success', "Đã tạo thành công hồ sơ nhân viên <strong>{$data['HoTen']}</strong> ({$maNV})!", 'success');
                $this->redirect("nhansu/profile/{$maNV}");
            } else {
                $auditModel->log('CreateEmployee', 'Failed', Auth::id(), Auth::user()['Username'], "Thất bại khi thêm hồ sơ nhân viên {$data['HoTen']}");
                Session::setFlash('error', 'Không thể tạo mới hồ sơ. Vui lòng kiểm tra trùng lặp Số CCCD hoặc Email.', 'danger');
            }
        }

        $options = $empModel->getDropdownOptions();
        $nextMaNV = $empModel->generateNextMaNV();

        $this->view('employees/create', [
            'title' => 'Thêm mới Hồ sơ Nhân sự Số hóa - Vinamilk HRM',
            'options' => $options,
            'nextMaNV' => $nextMaNV
        ]);
    }

    /**
     * Form Chỉnh sửa Hồ sơ Nhân sự 4 Tabs
     */
    public function edit(mixed $maNV = null): void {
        $this->requirePermission('NHANSU.Edit');

        if (!$maNV) {
            $this->redirect('nhansu');
        }

        /** @var EmployeeModel $empModel */
        $empModel = $this->model('EmployeeModel');
        $employee = $empModel->find($maNV);

        if (!$employee) {
            Session::setFlash('error', "Không tìm thấy hồ sơ nhân viên có mã {$maNV}.", 'danger');
            $this->redirect('nhansu');
        }

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->validateCsrf();

            // Xử lý Upload Ảnh chân dung mới nếu có
            $avatarPath = $employee['AnhChanDung'];
            if (!empty($_FILES['AnhChanDung']['name'])) {
                $uploadDir = PUBLIC_DIR . '/uploads/avatars/';
                if (!is_dir($uploadDir)) {
                    mkdir($uploadDir, 0777, true);
                }
                $ext = pathinfo($_FILES['AnhChanDung']['name'], PATHINFO_EXTENSION);
                $filename = 'avatar_' . strtolower($maNV) . '_' . time() . '.' . $ext;
                if (move_uploaded_file($_FILES['AnhChanDung']['tmp_name'], $uploadDir . $filename)) {
                    $avatarPath = 'uploads/avatars/' . $filename;
                }
            }

            $updateData = [
                'HoTen' => trim($_POST['HoTen'] ?? ''),
                'TenThuongGoi' => trim($_POST['TenThuongGoi'] ?? ''),
                'NgaySinh' => $_POST['NgaySinh'] ?? $employee['NgaySinh'],
                'GioiTinh' => $_POST['GioiTinh'] ?? $employee['GioiTinh'],
                'SoCMND_CCCD' => trim($_POST['SoCMND_CCCD'] ?? ''),
                'NgayCap' => !empty($_POST['NgayCap']) ? $_POST['NgayCap'] : null,
                'NoiCap' => trim($_POST['NoiCap'] ?? ''),
                'QueQuan' => trim($_POST['QueQuan'] ?? ''),
                'NoiDKKTTru' => trim($_POST['NoiDKKTTru'] ?? ''),
                'DiaChiHienTai' => trim($_POST['DiaChiHienTai'] ?? ''),
                'DanToc' => trim($_POST['DanToc'] ?? 'Kinh'),
                'TonGiao' => trim($_POST['TonGiao'] ?? 'Không'),
                'TinhTrangHonNhan' => $_POST['TinhTrangHonNhan'] ?? 'Độc thân',
                'Email' => trim($_POST['Email'] ?? ''),
                'SoDienThoai' => trim($_POST['SoDienThoai'] ?? ''),
                'AnhChanDung' => $avatarPath,
                'NgayVaoLam' => $_POST['NgayVaoLam'] ?? $employee['NgayVaoLam'],
                'MaDV' => $_POST['MaDV'] ?? $employee['MaDV'],
                'MaCV' => $_POST['MaCV'] ?? $employee['MaCV'],
                'MaNgach' => $_POST['MaNgach'] ?? $employee['MaNgach'],
                'HeSoLuongHienTai' => (float)($_POST['HeSoLuongHienTai'] ?? 1.00),
                'BacLuongHienTai' => (int)($_POST['BacLuongHienTai'] ?? 1),
                'TrinhDoHocVan' => trim($_POST['TrinhDoHocVan'] ?? ''),
                'ChuyenNganh' => trim($_POST['ChuyenNganh'] ?? ''),
                'TruongDaoTao' => trim($_POST['TruongDaoTao'] ?? ''),
                'DangVien' => isset($_POST['DangVien']) ? 1 : 0,
                'DoanVien' => isset($_POST['DoanVien']) ? 1 : 0,
                'ViTriLuuHoSo' => trim($_POST['ViTriLuuHoSo'] ?? ''),
                'LoaiHopDong' => trim($_POST['LoaiHopDong'] ?? ''),
                'TrangThai' => (int)($_POST['TrangThai'] ?? $employee['TrangThai'])
            ];

            $empModel->update($maNV, $updateData);

            /** @var AuditLog $auditModel */
            $auditModel = $this->model('AuditLog');
            $auditModel->log('EditEmployee', 'Success', Auth::id(), Auth::user()['Username'], "Cập nhật hồ sơ nhân viên {$employee['HoTen']} ({$maNV})");

            Session::setFlash('success', "Đã cập nhật thành công hồ sơ nhân viên <strong>{$employee['HoTen']}</strong>!", 'success');
            $this->redirect("nhansu/profile/{$maNV}");
        }

        $options = $empModel->getDropdownOptions();

        $this->view('employees/edit', [
            'title' => "Cập nhật Hồ sơ: {$employee['HoTen']} ({$maNV}) - Vinamilk HRM",
            'emp' => $employee,
            'options' => $options
        ]);
    }

    /**
     * Quy trình Thử việc & Duyệt xếp lương chính thức
     */
    public function probation(): void {
        $this->requirePermission('NHANSU.View');

        /** @var EmployeeModel $empModel */
        $empModel = $this->model('EmployeeModel');

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->requirePermission('NHANSU.Edit');
            $this->validateCsrf();

            $maNV = $_POST['MaNV'] ?? '';
            $maNgach = $_POST['MaNgach'] ?? 'NG_A3';
            $heSo = (float)($_POST['HeSoLuong'] ?? 2.34);
            $bac = (int)($_POST['BacLuong'] ?? 1);

            if (!empty($maNV)) {
                $empModel->approveProbation($maNV, $maNgach, $heSo, $bac);

                /** @var AuditLog $auditModel */
                $auditModel = $this->model('AuditLog');
                $auditModel->log('ApproveProbation', 'Success', Auth::id(), Auth::user()['Username'], "Duyệt thử việc đạt yêu cầu cho nhân viên {$maNV}");

                Session::setFlash('success', "Đã duyệt Đạt thử việc và kích hoạt chuyển Nhân viên chính thức cho mã <strong>{$maNV}</strong>!", 'success');
            }
            $this->redirect('nhansu/probation');
        }

        $probationList = $empModel->getProbationEmployees();
        $options = $empModel->getDropdownOptions();

        $this->view('employees/probation', [
            'title' => 'Quy trình Thử việc & Xếp lương - Vinamilk HRM',
            'probationList' => $probationList,
            'options' => $options
        ]);
    }

    /**
     * Quy trình Nghỉ hưu & Thôi việc
     */
    public function retirement(): void {
        $this->requirePermission('NHANSU.View');

        /** @var EmployeeModel $empModel */
        $empModel = $this->model('EmployeeModel');

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->requirePermission('NHANSU.Edit');
            $this->validateCsrf();

            $maNV = $_POST['MaNV'] ?? '';
            $trangThaiMoi = (int)($_POST['TrangThaiMoi'] ?? 3); // 3=Nghỉ việc, 4=Nghỉ hưu
            $ghiChu = trim($_POST['GhiChu'] ?? '');

            if (!empty($maNV)) {
                $empModel->terminateEmployment($maNV, $trangThaiMoi, $ghiChu);

                /** @var AuditLog $auditModel */
                $auditModel = $this->model('AuditLog');
                $auditModel->log('TerminateEmployment', 'Success', Auth::id(), Auth::user()['Username'], "Cập nhật thôi việc/nghỉ hưu cho nhân viên {$maNV}");

                Session::setFlash('success', "Đã xử lý hồ sơ Chấm dứt hợp đồng/Nghỉ hưu thành công cho mã <strong>{$maNV}</strong>!", 'success');
            }
            $this->redirect('nhansu/retirement');
        }

        $candidates = $empModel->getRetirementCandidates();

        $this->view('employees/retirement', [
            'title' => 'Quy trình Nghỉ hưu & Chấm dứt hợp đồng - Vinamilk HRM',
            'candidates' => $candidates
        ]);
    }

    /**
     * In Sơ yếu lý lịch chuẩn 2C/TCTW-98 hoặc Biểu mẫu Vinamilk
     */
    public function print2c(mixed $maNV = null): void {
        $this->requirePermission('NHANSU.View');

        if (!$maNV) {
            $this->redirect('nhansu');
        }

        /** @var EmployeeModel $empModel */
        $empModel = $this->model('EmployeeModel');
        $employee = $empModel->getEmployeeByMaNV($maNV);

        if (!$employee) {
            Session::setFlash('error', "Không tìm thấy hồ sơ nhân viên.", 'danger');
            $this->redirect('nhansu');
        }

        // Render view in không nạp master layout (Dùng layout print_blank)
        $this->view('employees/print_2c', [
            'title' => "In Sơ yếu lý lịch 2C: {$employee['HoTen']}",
            'emp' => $employee
        ], null);
    }
}
