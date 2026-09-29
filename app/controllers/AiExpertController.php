<?php
/**
 * =====================================================================
 * VINAMILK HRM - AI Expert System Controller (Trí tuệ nhân tạo Dự báo)
 * =====================================================================
 * 
 * Controller vận hành Hệ chuyên gia AI (HR Demand & Skill Gap Analysis Engine).
 * Phân tích dữ liệu biến động nhân sự, đưa ra cảnh báo khuyến nghị xác xuất
 * và cho phép 1-Click "Duyệt đề xuất AI" để tự động kích hoạt kế hoạch tuyển dụng/đào tạo.
 */

require_once APP_DIR . '/core/Controller.php';

class AiExpertController extends Controller {

    /**
     * Dashboard Phân tích Khuyến nghị từ Hệ chuyên gia AI
     */
    public function index(): void {
        $this->requirePermission('BAO_CAO.View');

        /** @var AiExpertModel $aiModel */
        $aiModel = $this->model('AiExpertModel');

        // Chạy Suy diễn hệ chuyên gia AI
        $recommendations = $aiModel->runAiInferenceEngine();

        // Tính thống kê theo ưu tiên
        $highPriorityCount = 0;
        $recruitmentCount = 0;
        $trainingCount = 0;

        foreach ($recommendations as $r) {
            if ($r['MucDoUuTien'] === 'Cao') $highPriorityCount++;
            if ($r['LoaiKhuyenNghi'] === 'Recruitment') $recruitmentCount++;
            if ($r['LoaiKhuyenNghi'] === 'Training') $trainingCount++;
        }

        $this->view('ai/dashboard', [
            'title' => 'Hệ chuyên gia AI Dự báo Nhu cầu Nhân sự - Vinamilk HRM AI Engine',
            'recommendations' => $recommendations,
            'stats' => [
                'total' => count($recommendations),
                'highPriority' => $highPriorityCount,
                'recruitment' => $recruitmentCount,
                'training' => $trainingCount
            ]
        ]);
    }

    /**
     * 1-Click Duyệt Tự động Kế hoạch do AI Đề xuất
     */
    public function approve(mixed $id = null): void {
        $this->requirePermission('BAO_CAO.Export');

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->validateCsrf();
            $id = (int)($_POST['recommendation_id'] ?? $id);

            /** @var AiExpertModel $aiModel */
            $aiModel = $this->model('AiExpertModel');
            $success = $aiModel->approveAiRecommendation($id, Auth::user()['Username']);

            /** @var AuditLog $auditModel */
            $auditModel = $this->model('AuditLog');

            if ($success) {
                $auditModel->log('ApproveAiPlan', 'Success', Auth::id(), Auth::user()['Username'], "Duyệt tự động khuyến nghị AI #{$id}");
                Session::setFlash('success', "Đã Duyệt tự động Khuyến nghị AI #<strong>{$id}</strong>! Hệ thống đã kích hoạt Kế hoạch Tuyển dụng / Đào tạo tương ứng.", 'success');
            } else {
                Session::setFlash('error', 'Có lỗi xảy ra khi duyệt đề xuất AI.', 'danger');
            }

            $this->redirect('ai');
        }
    }
}
