<?php
/**
 * =====================================================================
 * VINAMILK HRM - CONTROLLER CHẤM CÔNG & QUẢN LÝ THỜI GIAN LÀM VIỆC
 * =====================================================================
 * Tuân thủ quy chuẩn:
 *   - Lọc 100% qua scopeByUnit() theo cây đơn vị
 *   - Danh mục 4 ca chuẩn: Ca 1, Ca 2, Ca 3 (đêm qua ngày), Hành chính
 *   - Xếp ca theo tuần cho tổ sản xuất (Weekly Shift Roster Grid)
 *   - Import file máy chấm công CSV/XLSX, ghép ca, ca đêm, đi trễ, về sớm, quên quẹt
 *   - Bảng công tháng tổng hợp & Cảnh báo OT tháng (>40h) / năm (>200h)
 *   - Quy trình duyệt 3 bước: Tổ trưởng xác nhận -> Quản đốc duyệt -> Khóa công
 *   - Chặn sửa sau khi khóa công, quản lý phiếu điều chỉnh công có lý do
 *   - Xuất Excel tương thích Microsoft Excel UTF-8 BOM
 */

require_once __DIR__ . '/../core/Controller.php';
require_once __DIR__ . '/../models/AttendanceModel.php';
require_once __DIR__ . '/../models/OrgUnitModel.php';

class AttendanceController extends Controller
{
    private AttendanceModel $attendanceModel;
    private OrgUnitModel $orgUnitModel;

    public function __construct()
    {
        $this->attendanceModel = new AttendanceModel();
        $this->orgUnitModel    = new OrgUnitModel();
    }

    /**
     * DASHBOARD CHẤM CÔNG NGÀY
     */
    public function index(): void
    {
        $this->requireAuth();
        $this->requirePermission('chamcong.view');

        $page = max(1, (int)($_GET['page'] ?? 1));
        $limit = max(1, min(100, (int)($_GET['limit'] ?? 25)));
        $offset = ($page - 1) * $limit;

        $filters = [
            'from_date' => $_GET['from_date'] ?? date('Y-m-01'),
            'to_date'   => $_GET['to_date'] ?? date('Y-m-d'),
            'ma_dv'     => $_GET['ma_dv'] ?? ($_GET['unit_scope'] ?? ''),
            'ma_nv'     => trim($_GET['ma_nv'] ?? ''),
            'loai_cong' => $_GET['loai_cong'] ?? ''
        ];

        $result = $this->attendanceModel->getAttendanceList($filters, $limit, $offset);
        $selectableUnits = $this->orgUnitModel->getSelectableUnitsForUser();
        $shifts = $this->attendanceModel->getShifts();

        $this->view('attendance/index', [
            'title'           => 'Bảng Chấm Công Hằng Ngày - Vinamilk HRM',
            'attendanceList'  => $result['data'],
            'pagination'      => $result,
            'filters'         => $filters,
            'selectableUnits' => $selectableUnits,
            'shifts'          => $shifts
        ]);
    }

    /**
     * XẾP CA THEO TUẦN CHO TỔ SẢN XUẤT (Weekly Shift Roster Grid)
     */
    public function roster(): void
    {
        $this->requireAuth();
        $this->requirePermission('chamcong.view');

        // Mặc định tuần hiện tại: lấy ngày Thứ 2 đầu tuần
        $dateStr = $_GET['week_date'] ?? date('Y-m-d');
        $mondayTs = strtotime('monday this week', strtotime($dateStr));
        $mondayDate = date('Y-m-d', $mondayTs);

        $targetUnit = $_GET['ma_dv'] ?? (Auth::maDV() ?: 'NM_TS');
        $rosterData = $this->attendanceModel->getWeeklyRoster($targetUnit, $mondayDate);
        $selectableUnits = $this->orgUnitModel->getSelectableUnitsForUser();

        $this->view('attendance/roster', [
            'title'           => 'Lưới Xếp Ca Sản Xuất Tuần - Vinamilk HRM',
            'roster'          => $rosterData,
            'currentUnit'     => $targetUnit,
            'mondayDate'      => $mondayDate,
            'selectableUnits' => $selectableUnits
        ]);
    }

    /**
     * LƯU XẾP CA TUẦN (POST)
     */
    public function save_roster(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI', 'QUANLY_TRUCTIEP']);
        $this->requirePermission('chamcong.checkin');
        $this->validateCsrf();

        $assignments = [];
        $posted = $_POST['shift'] ?? []; // Array [ma_nv][date] = ma_ca
        $maDV   = $_POST['ma_dv'] ?? 'NM_TS';

        foreach ($posted as $manv => $days) {
            foreach ($days as $date => $maca) {
                $assignments[] = [
                    'ma_nv' => $manv,
                    'ngay'  => $date,
                    'ma_ca' => $maca,
                    'ma_dv' => $maDV
                ];
            }
        }

        try {
            $this->attendanceModel->saveWeeklyRoster(['assignments' => $assignments]);
            Session::setFlash('success', 'Đã lưu kế hoạch xếp ca tuần thành công!', 'success');
        } catch (Throwable $e) {
            Session::setFlash('error', 'Lỗi lưu xếp ca: ' . $e->getMessage(), 'danger');
        }

        $weekDate = $_POST['monday_date'] ?? date('Y-m-d');
        $this->redirect('attendance/roster?ma_dv=' . urlencode($maDV) . '&week_date=' . $weekDate);
    }

    /**
     * IMPORT FILE MÁY CHẤM CÔNG (CSV/XLSX) & GHÉP CA TỰ ĐỘNG
     */
    public function import(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI', 'KETOAN_LUONG']);
        $this->requirePermission('chamcong.checkin');

        $importStats = null;

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->validateCsrf();

            if (empty($_FILES['csv_file']) || $_FILES['csv_file']['error'] !== UPLOAD_ERR_OK) {
                Session::setFlash('error', 'Vui lòng chọn file máy chấm công hợp lệ (CSV/TXT).', 'danger');
                $this->redirect('attendance/import');
            }

            $file = $_FILES['csv_file']['tmp_name'];
            $fileName = $_FILES['csv_file']['name'];

            $logs = [];
            $handle = fopen($file, 'r');
            if ($handle !== false) {
                $header = fgetcsv($handle, 1000, ','); // Đọc header nếu có
                while (($data = fgetcsv($handle, 1000, ',')) !== false) {
                    // Cấu trúc mong đợi: Cột 0: MaNV, Cột 1: ThoiGianQuet (YYYY-MM-DD HH:mm:ss)
                    if (count($data) >= 2) {
                        $manv = trim($data[0]);
                        $time = trim($data[1]);
                        if (!empty($manv) && !empty($time)) {
                            $logs[] = [
                                'ma_nv'          => $manv,
                                'thoi_gian_quet' => date('Y-m-d H:i:s', strtotime($time)),
                                'ma_may'         => trim($data[2] ?? 'MCC-01')
                            ];
                        }
                    }
                }
                fclose($handle);
            }

            if (!empty($logs)) {
                $imported = $this->attendanceModel->importSwipeLogs($logs, $fileName);

                // Tự động ghép ca cho các ngày có quẹt thẻ
                $uniqueEmployees = array_unique(array_column($logs, 'ma_nv'));
                $uniqueDates = array_unique(array_map(fn($t) => date('Y-m-d', strtotime($t['thoi_gian_quet'])), $logs));

                $totalProcessed = 0;
                $overnightCount = 0;
                $missingSwipe   = 0;

                foreach ($uniqueEmployees as $emp) {
                    foreach ($uniqueDates as $dt) {
                        $res = $this->attendanceModel->processAttendanceDay($emp, $dt);
                        $totalProcessed++;
                        if ($res['gio_lam_dem'] > 0) $overnightCount++;
                        if ($res['quen_quet'] == 1)  $missingSwipe++;
                    }
                }

                $importStats = [
                    'imported_rows'   => $imported,
                    'processed_days'  => $totalProcessed,
                    'overnight_shifts'=> $overnightCount,
                    'missing_swipes'  => $missingSwipe
                ];

                Session::setFlash('success', "Import thành công {$imported} lượt quẹt thẻ! Đã ghép ca tự động cho {$totalProcessed} ngày công.", 'success');
            } else {
                Session::setFlash('error', 'File không chứa dữ liệu quẹt thẻ hợp lệ.', 'danger');
            }
        }

        $this->view('attendance/import', [
            'title'       => 'Import File Máy Chấm Công - Vinamilk HRM',
            'importStats' => $importStats
        ]);
    }

    /**
     * BẢNG CÔNG THÁNG TỔNG HỢP & CẢNH BÁO GIỚI HẠN OT
     */
    public function monthly(): void
    {
        $this->requireAuth();
        $this->requirePermission('chamcong.view');

        $month = (int)($_GET['thang'] ?? date('m'));
        $year  = (int)($_GET['nam'] ?? date('Y'));
        $maDV  = $_GET['ma_dv'] ?? ($_GET['unit_scope'] ?? '');

        // Tự động tổng hợp bảng công nếu người dùng bấm "Tổng hợp lại"
        if (isset($_GET['recalculate']) && $_GET['recalculate'] == '1') {
            $count = $this->attendanceModel->generateMonthlyTimesheet($month, $year, $maDV);
            Session::setFlash('success', "Đã tổng hợp bảng công tháng {$month}/{$year} cho {$count} nhân sự!", 'success');
            $this->redirect("attendance/monthly?thang={$month}&nam={$year}&ma_dv=" . urlencode($maDV));
        }

        $filters = [
            'thang'      => $month,
            'nam'        => $year,
            'ma_dv'      => $maDV,
            'trang_thai' => $_GET['trang_thai'] ?? ''
        ];

        $result = $this->attendanceModel->getMonthlyTimesheets($filters, 50, 0);
        $selectableUnits = $this->orgUnitModel->getSelectableUnitsForUser();

        $this->view('attendance/monthly', [
            'title'           => "Bảng Công Tháng {$month}/{$year} - Vinamilk HRM",
            'timesheets'      => $result['data'],
            'filters'         => $filters,
            'selectableUnits' => $selectableUnits
        ]);
    }

    /**
     * QUY TRÌNH DUYỆT 3 BƯỚC: TỔ TRƯỞNG XÁC NHẬN (Bước 1)
     */
    public function confirm(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI', 'QUANLY_TRUCTIEP']);
        $this->requirePermission('chamcong.approve');
        $this->validateCsrf();

        $id = (int)($_POST['id'] ?? 0);
        if ($id > 0) {
            $ok = $this->attendanceModel->confirmByTeamLead($id, Auth::username() ?? 'Tổ trưởng');
            if ($ok) {
                Session::setFlash('success', 'Tổ trưởng đã xác nhận công thành công!', 'success');
            } else {
                Session::setFlash('error', 'Không thể xác nhận (Trạng thái không hợp lệ).', 'danger');
            }
        }
        $this->redirect($_SERVER['HTTP_REFERER'] ?? 'attendance/monthly');
    }

    /**
     * QUY TRÌNH DUYỆT 3 BƯỚC: QUẢN ĐỐC DUYỆT (Bước 2)
     */
    public function approve(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI']);
        $this->requirePermission('chamcong.approve');
        $this->validateCsrf();

        $id = (int)($_POST['id'] ?? 0);
        if ($id > 0) {
            $ok = $this->attendanceModel->approveByManager($id, Auth::username() ?? 'Quản đốc');
            if ($ok) {
                Session::setFlash('success', 'Quản đốc đã phê duyệt bảng công thành công!', 'success');
            } else {
                Session::setFlash('error', 'Không thể phê duyệt (Trạng thái không hợp lệ).', 'danger');
            }
        }
        $this->redirect($_SERVER['HTTP_REFERER'] ?? 'attendance/monthly');
    }

    /**
     * QUY TRÌNH DUYỆT 3 BƯỚC: KHÓA BẢNG CÔNG (Bước 3 - Cấm sửa sau khi khóa)
     */
    public function lock(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'KETOAN_LUONG']);
        $this->requirePermission('chamcong.approve');
        $this->validateCsrf();

        $id = (int)($_POST['id'] ?? 0);
        if ($id > 0) {
            $ok = $this->attendanceModel->lockTimesheet($id, Auth::username() ?? 'Kế toán trưởng');
            if ($ok) {
                Session::setFlash('success', 'ĐÃ KHÓA BẢNG CÔNG! Sau khi khóa, không thể sửa trực tiếp mà phải lập phiếu điều chỉnh công.', 'success');
            } else {
                Session::setFlash('error', 'Lỗi khóa bảng công.', 'danger');
            }
        }
        $this->redirect($_SERVER['HTTP_REFERER'] ?? 'attendance/monthly');
    }

    /**
     * LẬP PHIẾU ĐIỀU CHỈNH CÔNG SAU KHI ĐÃ KHÓA (Ghi rõ lý do)
     */
    public function adjust(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI', 'QUANLY_TRUCTIEP']);
        $this->requirePermission('chamcong.checkin');
        $this->validateCsrf();

        try {
            $id = $this->attendanceModel->createAdjustmentRequest($_POST);
            Session::setFlash('success', "Đã gửi Phiếu điều chỉnh công [#{$id}] chờ cấp trên phê duyệt!", 'success');
        } catch (Throwable $e) {
            Session::setFlash('error', 'Lỗi gửi yêu cầu điều chỉnh: ' . $e->getMessage(), 'danger');
        }

        $this->redirect($_SERVER['HTTP_REFERER'] ?? 'attendance/monthly');
    }

    /**
     * XUẤT EXCEL BẢNG CÔNG THÁNG (CSV UTF-8 BOM)
     */
    public function export_excel(): void
    {
        $this->requireAuth();
        $this->requirePermission('chamcong.export');

        $month = (int)($_GET['thang'] ?? date('m'));
        $year  = (int)($_GET['nam'] ?? date('Y'));
        $maDV  = $_GET['ma_dv'] ?? '';

        $filters = ['thang' => $month, 'nam' => $year, 'ma_dv' => $maDV];
        $result = $this->attendanceModel->getMonthlyTimesheets($filters, 10000, 0);
        $rows = $result['data'];

        $filename = "Bang_cong_Vinamilk_Thang_{$month}_{$year}.csv";
        header('Content-Type: text/csv; charset=UTF-8');
        header('Content-Disposition: attachment; filename="' . $filename . '"');
        header('Pragma: no-cache');
        header('Expires: 0');

        $output = fopen('php://output', 'w');
        // UTF-8 BOM
        fprintf($output, chr(0xEF).chr(0xBB).chr(0xBF));

        fputcsv($output, [
            'Mã NV', 'Họ và tên', 'Đơn vị', 'Chức danh', 'Công chuẩn',
            'Công thực tế', 'Nghỉ phép', 'Nghỉ lễ', 'Tổng công hưởng lương',
            'OT ngày thường (h)', 'OT ngày nghỉ (h)', 'OT ngày lễ (h)',
            'Tổng giờ OT (h)', 'Giờ làm đêm (h)', 'Đi trễ/Về sớm (lần)',
            'Quên quẹt thẻ (lần)', 'Trạng thái', 'Cảnh báo OT'
        ]);

        foreach ($rows as $r) {
            fputcsv($output, [
                $r['ma_nv'],
                $r['HoTen'],
                $r['TenDV'] ?? $r['ma_dv'],
                $r['TenCV'] ?? '',
                $r['cong_chuan_thang'],
                $r['cong_thuc_te'],
                $r['nghi_phep_huong_luong'],
                $r['nghi_le_tet'],
                $r['tong_cong_tinh_luong'],
                $r['gio_ot_ngay_thuong'],
                $r['gio_ot_ngay_nghi'],
                $r['gio_ot_ngay_le'],
                $r['tong_gio_ot'],
                $r['gio_lam_dem'],
                $r['di_muon_ve_som_lan'],
                $r['quen_quet_the_lan'],
                $r['trang_thai'],
                $r['canh_bao_ot'] ?? 'Bình thường'
            ]);
        }

        fclose($output);
        exit;
    }
}
