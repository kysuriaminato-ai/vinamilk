<?php
/**
 * =====================================================================
 * VINAMILK HRM - CONTROLLER BẢNG ĐIỀU KHIỂN & HR ANALYTICS (DashboardController)
 * =====================================================================
 * Tích hợp:
 * - 6 Thẻ KPI tổng hợp so sánh cùng kỳ (MoM)
 * - 5 Biểu đồ chuyên sâu Chart.js 4 (Headcount & Nghỉ việc 12M, Khối, Xếp hạng OT Nhà máy, Tháp tuổi-giới tính, Thâm niên)
 * - 3 Danh sách hành động (HĐ hết hạn 60 ngày, Chứng chỉ ATTP sắp hết hạn, Thiếu định biên > 10%)
 * - Modal Drill-down danh sách nhân viên cấu thành số liệu
 * - Từ điển chỉ số HR Analytics (/kpi-dictionary)
 * - Bộ lọc kỳ và đơn vị tuân thủ nghiêm ngặt scopeByUnit()
 */

require_once __DIR__ . '/../core/Controller.php';
require_once __DIR__ . '/../models/OrgUnitModel.php';
require_once __DIR__ . '/../services/HeadcountSnapshotService.php';

class DashboardController extends Controller
{
    private OrgUnitModel $orgUnitModel;
    private HeadcountSnapshotService $snapshotService;

    public function __construct()
    {
        $this->orgUnitModel    = new OrgUnitModel();
        $this->snapshotService = new HeadcountSnapshotService();
    }

    /**
     * MÀN HÌNH DASHBOARD ĐIỀU HÀNH & HR ANALYTICS
     */
    public function index(): void
    {
        $this->requireAuth();

        $user = Auth::user();
        $role = strtoupper(Auth::role());

        // 1. Xác định phạm vi đơn vị được phép truy cập (scopeByUnit)
        $selectableUnits = $this->orgUnitModel->getSelectableUnitsForUser();
        $allowedUnitCodes = array_column($selectableUnits, 'MaDV');

        // Lấy tham số bộ lọc từ GET
        $selectedPeriod = trim($_GET['period'] ?? '2026-10');
        $selectedUnit   = trim($_GET['unit'] ?? 'ALL');

        // Kiểm tra thẩm quyền chọn đơn vị
        if ($selectedUnit !== 'ALL' && !in_array($selectedUnit, $allowedUnitCodes, true)) {
            // Nếu cố tình chọn đơn vị ngoài quyền, reset về đơn vị hợp lệ đầu tiên của user
            $selectedUnit = !empty($allowedUnitCodes) ? $allowedUnitCodes[0] : (Auth::maDV() ?: 'ALL');
        }

        if (!Auth::canViewAllUnits() && $selectedUnit === 'ALL') {
            $selectedUnit = Auth::maDV() ?: (!empty($allowedUnitCodes) ? $allowedUnitCodes[0] : 'ALL');
        }

        $scopeUnitsForQuery = Auth::canViewAllUnits() ? [] : $allowedUnitCodes;

        // 2. Lấy dữ liệu 6 THẺ KPI SO SÁNH CÙNG KỲ (MoM)
        $kpiCards = $this->snapshotService->getKpiCardsData($selectedPeriod, $selectedUnit, $scopeUnitsForQuery);

        // 3. Lấy dữ liệu 5 BIỂU ĐỒ CHUYÊN SÂU (Chart.js 4)
        // Biểu đồ 1: Headcount & Nghỉ việc 12 tháng trượt
        $trend12m = $this->snapshotService->get12MonthsTrend($selectedPeriod, $selectedUnit, $scopeUnitsForQuery);

        // Biểu đồ 2: Cơ cấu theo Khối
        $blockDistribution = $this->snapshotService->getHeadcountByBlock($selectedUnit, $scopeUnitsForQuery);

        // Biểu đồ 3: Xếp hạng giờ OT theo Nhà máy / Trang trại
        $factoryOtRanking = $this->snapshotService->getOvertimeRankingByFactory($selectedPeriod, $scopeUnitsForQuery);

        // Biểu đồ 4: Tháp tuổi - Giới tính (Age-Gender Pyramid)
        $ageGenderPyramid = $this->snapshotService->getAgeGenderPyramid($selectedUnit, $scopeUnitsForQuery);

        // Biểu đồ 5: Phân bổ thâm niên (Tenure Distribution)
        $tenureDistribution = $this->snapshotService->getTenureDistribution($selectedUnit, $scopeUnitsForQuery);

        // 4. Lấy dữ liệu 3 DANH SÁCH HÀNH ĐỘNG
        // - HĐ hết hạn trong vòng 60 ngày
        $expiringContracts = $this->snapshotService->getExpiringContracts(60, $selectedUnit, $scopeUnitsForQuery);

        // - Chứng chỉ ATTP & An toàn lao động sắp hết hạn (60 ngày)
        $expiringCertificates = $this->snapshotService->getExpiringCertificates(60, $selectedUnit, $scopeUnitsForQuery);

        // - Đơn vị thiếu định biên > 10%
        $understaffedUnits = $this->snapshotService->getUnderstaffedUnits(0.10, $selectedUnit, $scopeUnitsForQuery);

        // 5. Danh sách các kỳ tháng có sẵn trong snapshot
        $db = Database::getInstance()->getConnection();
        $availablePeriods = $db->query("SELECT DISTINCT ky_nam_thang FROM fact_headcount_monthly ORDER BY ky_nam_thang DESC")->fetchAll(PDO::FETCH_COLUMN);
        if (empty($availablePeriods)) {
            $availablePeriods = ['2026-10', '2026-09', '2026-08', '2026-07', '2026-06'];
        }

        $this->view('dashboard/index', [
            'title'                => 'Executive HR Analytics Dashboard - Vinamilk Enterprise HRM',
            'selectedPeriod'       => $selectedPeriod,
            'selectedUnit'         => $selectedUnit,
            'availablePeriods'     => $availablePeriods,
            'selectableUnits'      => $selectableUnits,
            'canViewAllUnits'      => Auth::canViewAllUnits(),
            'kpiCards'             => $kpiCards,
            'trend12m'             => $trend12m,
            'blockDistribution'    => $blockDistribution,
            'factoryOtRanking'     => $factoryOtRanking,
            'ageGenderPyramid'     => $ageGenderPyramid,
            'tenureDistribution'   => $tenureDistribution,
            'expiringContracts'    => $expiringContracts,
            'expiringCertificates' => $expiringCertificates,
            'understaffedUnits'    => $understaffedUnits
        ]);
    }

    /**
     * AJAX DRILL-DOWN: XEM DANH SÁCH NHÂN VIÊN CẤU THÀNH CHỈ SỐ
     */
    public function drilldown(): void
    {
        $this->requireAuth();

        $metric = trim($_GET['metric'] ?? 'headcount');
        $period = trim($_GET['period'] ?? '2026-10');
        $unit   = trim($_GET['unit'] ?? 'ALL');

        $selectableUnits = $this->orgUnitModel->getSelectableUnitsForUser();
        $allowedUnitCodes = array_column($selectableUnits, 'MaDV');

        if ($unit !== 'ALL' && !in_array($unit, $allowedUnitCodes, true)) {
            $unit = !empty($allowedUnitCodes) ? $allowedUnitCodes[0] : (Auth::maDV() ?: 'ALL');
        }

        $scopeUnitsForQuery = Auth::canViewAllUnits() ? [] : $allowedUnitCodes;

        $employees = $this->snapshotService->getDrilldownEmployees($metric, $period, $unit, $scopeUnitsForQuery);

        $titles = [
            'headcount'      => 'Danh Sách Nhân Sự Đang Làm Việc (Headcount)',
            'turnover_12m'   => 'Danh Sách Nhân Sự Thôi Việc (12 Tháng Trượt)',
            'turnover'       => 'Danh Sách Nhân Sự Thôi Việc Trong Kỳ',
            'absenteeism'    => 'Danh Sách Nhân Sự Có Ngày Nghỉ Phép / Vắng Mặt',
            'absence'        => 'Danh Sách Nhân Sự Có Ngày Nghỉ Phép / Vắng Mặt',
            'overtime_avg'   => 'Danh Sách Nhân Sự Có Giờ Làm Thêm (OT)',
            'overtime'       => 'Danh Sách Nhân Sự Có Giờ Làm Thêm (OT)',
            'payroll_fund'   => 'Danh Sách Bảng Lương Nhân Sự Trong Kỳ',
            'payroll'        => 'Danh Sách Bảng Lương Nhân Sự Trong Kỳ',
            'staffing_fill'  => 'Danh Sách Nhân Sự Theo Định Biên Đơn Vị'
        ];

        header('Content-Type: application/json; charset=utf-8');
        echo json_encode([
            'status'  => 'success',
            'metric'  => $metric,
            'title'   => $titles[strtolower($metric)] ?? 'Danh Sách Chi Tiết Nhân Sự',
            'period'  => $period,
            'unit'    => $unit,
            'count'   => count($employees),
            'data'    => $employees
        ], JSON_UNESCAPED_UNICODE);
        exit;
    }

    /**
     * TRANG TỪ ĐIỂN CHỈ SỐ HR ANALYTICS (/kpi-dictionary)
     */
    public function kpi_dictionary(): void
    {
        $this->requireAuth();

        $this->view('dashboard/kpi_dictionary', [
            'title' => 'Từ Điển Chỉ Số Quản Trị Nhân Sự (HR Metric Dictionary) - Vinamilk HRM'
        ]);
    }

    /**
     * THỰC HIỆN KÍCH HOẠT CHẠY JOB SNAPSHOT TỪ GIAO DIỆN ADMIN
     */
    public function run_snapshot(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'BOD']);
        $this->validateCsrf();

        $period = trim($_POST['period'] ?? date('Y-m', strtotime('first day of last month')));

        try {
            $res = $this->snapshotService->takeSnapshot($period, true);
            Session::setFlash('success', "Đã hoàn thành chốt dữ liệu Snapshot kỳ [{$period}] cho {$res['total_units']} đơn vị thành công!", 'success');
        } catch (Exception $e) {
            Session::setFlash('error', "Lỗi khi chốt Snapshot: " . $e->getMessage(), 'danger');
        }

        $this->redirect('dashboard?period=' . urlencode($period));
    }
}
