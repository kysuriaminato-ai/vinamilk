<?php
/**
 * =====================================================================
 * VINAMILK HRM - Report Controller (Phân hệ Báo cáo Thống kê HUHA HRM)
 * =====================================================================
 * 
 * Controller xử lý Trung tâm Báo cáo Thống kê doanh nghiệp:
 * Biểu mẫu 01 (Sơ yếu trích ngang), Biểu mẫu 02 (Biến động nhân sự),
 * Biểu mẫu 03 (Khen thưởng kỷ luật), Biểu mẫu 04 (Nghỉ hưu) và Xuất file Excel CSV/PDF.
 */

require_once APP_DIR . '/core/Controller.php';

class ReportController extends Controller {

    /**
     * Trung tâm Báo cáo Thống kê Tổng quan HUHA HRM
     */
    public function index(): void {
        $this->requirePermission('BAO_CAO.View');

        /** @var ReportModel $reportModel */
        $reportModel = $this->model('ReportModel');
        $kpis = $reportModel->getExecutiveKpis();

        $this->view('reports/index', [
            'title' => 'Trung tâm Báo cáo Thống kê HUHA HRM - Vinamilk',
            'kpis' => $kpis
        ]);
    }

    /**
     * BIỂU MẪU 01: Báo cáo Trích ngang Cán bộ Công nhân viên
     */
    public function bm01(): void {
        $this->requirePermission('BAO_CAO.View');

        /** @var ReportModel $reportModel */
        $reportModel = $this->model('ReportModel');
        /** @var EmployeeModel $empModel */
        $empModel = $this->model('EmployeeModel');

        $filters = [
            'ma_dv' => trim($_GET['ma_dv'] ?? ''),
            'ma_cv' => trim($_GET['ma_cv'] ?? ''),
            'trang_thai' => trim($_GET['trang_thai'] ?? '')
        ];

        $list = $reportModel->getBM01TrichNgang($filters);
        $options = $empModel->getDropdownOptions();

        $this->view('reports/bm01_trichngang', [
            'title' => 'Biểu mẫu 01: Sơ yếu Lý lịch Trích ngang CBNV - Vinamilk',
            'list' => $list,
            'filters' => $filters,
            'options' => $options
        ]);
    }

    /**
     * BIỂU MẪU 02: Báo cáo Biến động Nhân sự (Thuyên chuyển, Bổ nhiệm, Tuyển mới)
     */
    public function bm02(): void {
        $this->requirePermission('BAO_CAO.View');

        $nam = (int)($_GET['nam'] ?? date('Y'));

        /** @var ReportModel $reportModel */
        $reportModel = $this->model('ReportModel');
        $list = $reportModel->getBM02BienDong($nam);

        $this->view('reports/bm02_biendong', [
            'title' => "Biểu mẫu 02: Báo cáo Biến động Nhân sự Năm {$nam} - Vinamilk",
            'list' => $list,
            'nam' => $nam
        ]);
    }

    /**
     * BIỂU MẪU 03: Thống kê Khen thưởng & Kỷ luật Toàn công ty
     */
    public function bm03(): void {
        $this->requirePermission('BAO_CAO.View');

        $nam = (int)($_GET['nam'] ?? date('Y'));

        /** @var ReportModel $reportModel */
        $reportModel = $this->model('ReportModel');
        $list = $reportModel->getBM03KTKL($nam);

        $this->view('reports/bm03_ktkl', [
            'title' => "Biểu mẫu 03: Thống kê Khen thưởng & Kỷ luật Năm {$nam} - Vinamilk",
            'list' => $list,
            'nam' => $nam
        ]);
    }

    /**
     * BIỂU MẪU 04: Báo cáo Nhân sự Tiệm cận Nghỉ hưu
     */
    public function bm04(): void {
        $this->requirePermission('BAO_CAO.View');

        /** @var ReportModel $reportModel */
        $reportModel = $this->model('ReportModel');
        $list = $reportModel->getBM04NghiHuu();

        $this->view('reports/bm04_nghihuu', [
            'title' => 'Biểu mẫu 04: Thống kê Danh sách Nhân sự Tiệm cận Nghỉ hưu - Vinamilk',
            'list' => $list
        ]);
    }

    /**
     * Xuất Báo cáo ra File Excel / CSV (Export Excel)
     */
    public function exportExcel(string $type = 'bm01'): void {
        $this->requirePermission('BAO_CAO.Export');

        /** @var ReportModel $reportModel */
        $reportModel = $this->model('ReportModel');

        $filename = "Vinamilk_HRM_Report_{$type}_" . date('Ymd_His') . ".csv";

        header('Content-Type: text/csv; charset=utf-8');
        header('Content-Disposition: attachment; filename="' . $filename . '"');

        $output = fopen('php://output', 'w');
        // Ghi BOM UTF-8 để Excel đọc tiếng Việt có dấu đúng
        fprintf($output, chr(0xEF).chr(0xBB).chr(0xBF));

        if ($type === 'bm01') {
            fputcsv($output, ['STT', 'MaNV', 'HoTen', 'NgaySinh', 'GioiTinh', 'SoCCCD', 'DonVi', 'ChucVu', 'NgachLuong', 'HeSo', 'ViTriLuuHoSo', 'TrangThai']);
            $data = $reportModel->getBM01TrichNgang([]);
            $stt = 1;
            foreach ($data as $r) {
                fputcsv($output, [
                    $stt++,
                    $r['MaNV'],
                    $r['HoTen'],
                    format_date($r['NgaySinh']),
                    $r['GioiTinh'],
                    $r['SoCMND_CCCD'],
                    $r['TenDV'],
                    $r['TenCV'],
                    $r['MaNgach'],
                    $r['HeSoLuongHienTai'],
                    $r['ViTriLuuHoSo'],
                    ($r['TrangThai'] == 1 ? 'Chính thức' : 'Thử việc')
                ]);
            }
        } elseif ($type === 'bm02') {
            fputcsv($output, ['STT', 'SoQD', 'LoaiBienDong', 'MaNV', 'HoTen', 'DonViCu', 'DonViMoi', 'NgayHieuLuc', 'LyDo']);
            $data = $reportModel->getBM02BienDong((int)date('Y'));
            $stt = 1;
            foreach ($data as $r) {
                fputcsv($output, [
                    $stt++,
                    $r['SoQD'],
                    $r['LoaiBienDong'],
                    $r['MaNV'],
                    $r['HoTen'],
                    $r['DonViCu'],
                    $r['DonViMoi'],
                    format_date($r['Ngay']),
                    $r['LyDo']
                ]);
            }
        }

        fclose($output);
        exit;
    }

    /**
     * Phôi In Báo cáo Chuẩn A4 Ngang / Dọc (`@media print`)
     */
    public function printReport(string $type = 'bm01'): void {
        $this->requirePermission('BAO_CAO.View');

        /** @var ReportModel $reportModel */
        $reportModel = $this->model('ReportModel');

        $data = match($type) {
            'bm01' => $reportModel->getBM01TrichNgang([]),
            'bm02' => $reportModel->getBM02BienDong((int)date('Y')),
            'bm03' => $reportModel->getBM03KTKL((int)date('Y')),
            'bm04' => $reportModel->getBM04NghiHuu(),
            default => $reportModel->getBM01TrichNgang([])
        };

        $this->view('reports/print_report', [
            'title' => "In Báo cáo Thống kê: " . strtoupper($type),
            'type' => $type,
            'list' => $data
        ], null);
    }
}
