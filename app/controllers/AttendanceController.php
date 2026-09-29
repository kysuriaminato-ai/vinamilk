<?php
/**
 * =====================================================================
 * VINAMILK HRM - Attendance Controller (Quản lý Chấm công & Phép năm)
 * =====================================================================
 * 
 * Controller xử lý Bảng chấm công Lưới (Grid Calendar 30/31 ngày),
 * cập nhật công Ajax thời gian thực, Import dữ liệu máy chấm công vân tay
 * và Phê duyệt đơn xin nghỉ phép trực tuyến.
 */

require_once APP_DIR . '/core/Controller.php';

class AttendanceController extends Controller {

    /**
     * View Bảng chấm công ma trận tháng 30/31 ngày
     */
    public function index(): void {
        $this->requirePermission('CHAM_CONG.View');

        /** @var AttendanceModel $attModel */
        $attModel = $this->model('AttendanceModel');
        /** @var EmployeeModel $empModel */
        $empModel = $this->model('EmployeeModel');

        $thang = (int)($_GET['thang'] ?? date('m'));
        $nam = (int)($_GET['nam'] ?? date('Y'));
        $maDV = trim($_GET['ma_dv'] ?? '');

        $gridData = $attModel->getMonthlyAttendanceGrid($thang, $nam, $maDV);
        $options = $empModel->getDropdownOptions();

        $this->view('attendance/index', [
            'title' => "Bảng Chấm công Ma trận Tháng {$thang}/{$nam} - Vinamilk HRM",
            'gridData' => $gridData,
            'thang' => $thang,
            'nam' => $nam,
            'maDV' => $maDV,
            'options' => $options
        ]);
    }

    /**
     * Ajax Endpoint Cập nhật Ký hiệu Chấm công từng ô thời gian thực
     */
    public function updateCell(): void {
        $this->requirePermission('CHAM_CONG.Edit');

        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            $this->json(['error' => 'Phương thức không hợp lệ'], 400);
        }

        $maNV = trim($_POST['ma_nv'] ?? '');
        $thang = (int)($_POST['thang'] ?? 0);
        $nam = (int)($_POST['nam'] ?? 0);
        $ngay = (int)($_POST['ngay'] ?? 0);
        $kyHieu = trim($_POST['ky_hieu'] ?? 'X');

        if (empty($maNV) || $thang <= 0 || $nam <= 0 || $ngay <= 0) {
            $this->json(['error' => 'Tham số chấm công không hợp lệ'], 400);
        }

        /** @var AttendanceModel $attModel */
        $attModel = $this->model('AttendanceModel');
        $success = $attModel->updateDailySymbol($maNV, $thang, $nam, $ngay, $kyHieu);

        /** @var AuditLog $auditModel */
        $auditModel = $this->model('AuditLog');
        $auditModel->log('UpdateAttendanceCell', 'Success', Auth::id(), Auth::user()['Username'], "Cập nhật công ngày {$ngay}/{$thang} cho {$maNV} thành {$kyHieu}");

        $this->json([
            'success' => $success,
            'message' => 'Cập nhật ký hiệu công thành công',
            'maNV' => $maNV,
            'ngay' => $ngay,
            'kyHieu' => $kyHieu
        ]);
    }

    /**
     * Import dữ liệu chấm công từ file máy chấm công
     */
    public function import(): void {
        $this->requirePermission('CHAM_CONG.Add');

        /** @var AttendanceModel $attModel */
        $attModel = $this->model('AttendanceModel');

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->validateCsrf();

            $thang = (int)($_POST['thang'] ?? date('m'));
            $nam = (int)($_POST['nam'] ?? date('Y'));

            if (!empty($_FILES['file_excel']['tmp_name'])) {
                // Đọc file CSV / Dữ liệu chấm công
                $handle = fopen($_FILES['file_excel']['tmp_name'], "r");
                $rows = [];
                $header = fgetcsv($handle, 1000, ","); // Bỏ qua dòng tiêu đề

                while (($data = fgetcsv($handle, 1000, ",")) !== FALSE) {
                    if (isset($data[0]) && isset($data[1])) {
                        $rows[] = [
                            'MaNV' => trim($data[0]),
                            'Ngay' => (int)trim($data[1]),
                            'KyHieu' => trim($data[2] ?? 'X')
                        ];
                    }
                }
                fclose($handle);

                $count = $attModel->importAttendanceData($rows, $thang, $nam);

                /** @var AuditLog $auditModel */
                $auditModel = $this->model('AuditLog');
                $auditModel->log('ImportAttendance', 'Success', Auth::id(), Auth::user()['Username'], "Import thành công {$count} lượt chấm công tháng {$thang}/{$nam}");

                Session::setFlash('success', "Đã import thành công <strong>{$count}</strong> bản ghi chấm công tháng {$thang}/{$nam}!", 'success');
            } else {
                Session::setFlash('error', 'Vui lòng chọn file dữ liệu máy chấm công hợp lệ.', 'danger');
            }

            $this->redirect("chamcong?thang={$thang}&nam={$nam}");
        }

        $this->view('attendance/import', [
            'title' => 'Import Dữ liệu Máy Chấm công - Vinamilk HRM'
        ]);
    }

    /**
     * Quản lý Đơn xin nghỉ phép trực tuyến & Phê duyệt
     */
    public function leave(): void {
        $this->requirePermission('CHAM_CONG.View');

        /** @var AttendanceModel $attModel */
        $attModel = $this->model('AttendanceModel');

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->validateCsrf();

            $action = $_POST['action'] ?? 'create';

            if ($action === 'create') {
                $data = [
                    'MaNV' => Auth::maNV() ?? trim($_POST['MaNV'] ?? ''),
                    'TuNgay' => $_POST['TuNgay'] ?? date('Y-m-d'),
                    'DenNgay' => $_POST['DenNgay'] ?? date('Y-m-d'),
                    'SoNgay' => (float)($_POST['SoNgay'] ?? 1.0),
                    'LoaiNghi' => $_POST['LoaiNghi'] ?? 'Phép năm',
                    'LyDo' => trim($_POST['LyDo'] ?? '')
                ];

                $attModel->createLeaveRequest($data);
                Session::setFlash('success', 'Đã nộp Đơn xin nghỉ phép thành công! Vui lòng chờ Trưởng bộ phận duyệt.', 'success');
            } else if ($action === 'approve') {
                $this->requirePermission('CHAM_CONG.Approve');

                $leaveId = (int)($_POST['leave_id'] ?? 0);
                $status = $_POST['status'] ?? 'DaDuyet'; // DaDuyet, TuChoi

                $attModel->approveLeaveRequest($leaveId, Auth::user()['Username'], $status);
                Session::setFlash('success', 'Đã cập nhật phê duyệt Đơn xin nghỉ phép!', 'success');
            }

            $this->redirect('chamcong/leave');
        }

        $leaveList = $attModel->getLeaveRequests(Auth::isAdmin() ? null : Auth::maNV());

        $this->view('attendance/leave', [
            'title' => 'Quản lý Đơn xin nghỉ phép Trực tuyến - Vinamilk HRM',
            'leaveList' => $leaveList
        ]);
    }
}
