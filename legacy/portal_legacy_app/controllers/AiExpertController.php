<?php
/**
 * =====================================================================
 * VINAMILK HRM - CONTROLLER HỆ CHUYÊN GIA AI & DỰ BÁO NGUỒN LỰC (AiExpertController)
 * =====================================================================
 * Tích hợp:
 * 1. Chấm điểm rủi ro nghỉ việc bằng mô hình Logistic Regression
 * 2. Hiển thị Top nhân sự rủi ro, 3 lý do chính và khuyến nghị giữ chân
 * 3. Dự báo định biên & Lộ trình tuổi nghỉ hưu Bộ luật Lao động 2019
 * 4. Ghi rõ nhãn: "Mô hình huấn luyện trên dữ liệu giả lập"
 */

require_once __DIR__ . '/../core/Controller.php';
require_once __DIR__ . '/../models/AiExpertModel.php';
require_once __DIR__ . '/../models/OrgUnitModel.php';

class AiExpertController extends Controller
{
    private AiExpertModel $aiModel;
    private OrgUnitModel  $orgUnitModel;

    public function __construct()
    {
        $this->aiModel      = new AiExpertModel();
        $this->orgUnitModel = new OrgUnitModel();
    }

    /**
     * TRANG TRUNG TÂM PHÂN TÍCH RỦI RO AI & DỰ BÁO ĐỊNH BIÊN
     */
    public function index(): void
    {
        $this->requireAuth();

        $selectedPeriod = trim($_GET['period'] ?? '2026-10');
        $selectedUnit   = trim($_GET['unit'] ?? 'ALL');

        // Phân quyền scopeByUnit
        $selectableUnits = $this->orgUnitModel->getSelectableUnitsForUser();
        $allowedUnitCodes = array_column($selectableUnits, 'MaDV');

        if ($selectedUnit !== 'ALL' && !in_array($selectedUnit, $allowedUnitCodes, true)) {
            $selectedUnit = !empty($allowedUnitCodes) ? $allowedUnitCodes[0] : (Auth::maDV() ?: 'ALL');
        }

        if (!Auth::canViewAllUnits() && $selectedUnit === 'ALL') {
            $selectedUnit = Auth::maDV() ?: (!empty($allowedUnitCodes) ? $allowedUnitCodes[0] : 'ALL');
        }

        $scopeUnitsForQuery = Auth::canViewAllUnits() ? [] : $allowedUnitCodes;

        // Tự động chấm điểm nếu kỳ này chưa có dữ liệu
        $summary = $this->aiModel->getAiSummaryStats($selectedPeriod, $selectedUnit, $scopeUnitsForQuery);
        if ($summary['TongNhanSu'] <= 0) {
            $this->aiModel->scoreAllEmployees($selectedPeriod);
            $summary = $this->aiModel->getAiSummaryStats($selectedPeriod, $selectedUnit, $scopeUnitsForQuery);
        }

        // Lấy Top 20 nhân viên rủi ro nghỉ việc cao nhất
        $topRisky = $this->aiModel->getTopRiskyEmployees($selectedPeriod, 20, $selectedUnit, $scopeUnitsForQuery);

        // Tham số Dự báo định biên
        $capacityPlan = (float)($_GET['capacity'] ?? 2500000); // 2.500.000 lít sữa/tháng
        $productivity = (float)($_GET['productivity'] ?? 15000); // 15.000 lít/người/tháng
        $yearForecast = (int)($_GET['year'] ?? 2026);

        $forecast = $this->aiModel->getCapacityForecast($selectedUnit, $capacityPlan, $productivity, $yearForecast);

        $this->view('ai_expert/index', [
            'title'           => 'AI Chuyên Gia Dự Báo Rủi Ro & Định Biên - Vinamilk HRM',
            'selectedPeriod'  => $selectedPeriod,
            'selectedUnit'    => $selectedUnit,
            'selectableUnits' => $selectableUnits,
            'canViewAllUnits' => Auth::canViewAllUnits(),
            'summary'         => $summary,
            'topRisky'        => $topRisky,
            'forecast'        => $forecast
        ]);
    }

    /**
     * CHẠY LẠI CHẤM ĐIỂM AI CHO KỲ HIỆN TẠI (POST)
     */
    public function rescore(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'BOD']);
        $this->validateCsrf();

        $period = trim($_POST['period'] ?? '2026-10');

        try {
            $count = $this->aiModel->scoreAllEmployees($period);
            Session::setFlash('success', "Đã hoàn thành chấm điểm rủi ro nghỉ việc cho {$count} nhân sự kỳ [{$period}]!", 'success');
        } catch (Exception $e) {
            Session::setFlash('error', "Lỗi khi chấm điểm AI: " . $e->getMessage(), 'danger');
        }

        $this->redirect('ai-expert?period=' . urlencode($period));
    }
}
