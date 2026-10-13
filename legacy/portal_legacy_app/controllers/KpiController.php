<?php
/**
 * =====================================================================
 * VINAMILK HRM - CONTROLLER QUẢN LÝ KPI (KPICONTROLLER)
 * =====================================================================
 * Tuân thủ chuẩn bảo mật:
 *   - Bắt buộc kiểm tra quyền hạn trước mọi hành động
 *   - Không sử dụng CORS wildcard
 *   - Ghi log lỗi PDO vào file log máy chủ qua error_log, không lộ cấu trúc DB
 */

require_once __DIR__ . '/../core/Controller.php';

class KpiController extends Controller
{
    /**
     * Danh sách / Chi tiết KPI nhân sự
     * GET /kpi?employee_id=VNM-0001
     */
    public function index(): void
    {
        // 1. Bắt buộc có quyền xem đánh giá KPI
        $this->requirePermission('kpi.view');

        $employeeId = $_GET['employee_id'] ?? null;

        try {
            $db = Database::getInstance()->getConnection();

            if ($employeeId) {
                $stmt = $db->prepare("SELECT * FROM kpi WHERE ma_nv = :manv ORDER BY nam DESC, ky_danh_gia DESC");
                $stmt->execute([':manv' => $employeeId]);
                $kpis = $stmt->fetchAll(PDO::FETCH_ASSOC);
            } else {
                $stmt = $db->query("SELECT * FROM kpi ORDER BY id DESC LIMIT 50");
                $kpis = $stmt->fetchAll(PDO::FETCH_ASSOC);
            }

            $this->json([
                'status'  => 'success',
                'success' => true,
                'data'    => $kpis
            ]);
        } catch (PDOException $e) {
            error_log("Database Error in KpiController::index: " . $e->getMessage());
            $this->json([
                'status'  => 'error',
                'success' => false,
                'message' => 'Có lỗi xảy ra trong quá trình xử lý. Vui lòng liên hệ quản trị viên.'
            ], 500);
        } catch (Throwable $e) {
            error_log("System Error in KpiController::index: " . $e->getMessage());
            $this->json([
                'status'  => 'error',
                'success' => false,
                'message' => 'Có lỗi xảy ra trong quá trình xử lý. Vui lòng liên hệ quản trị viên.'
            ], 500);
        }
    }

    /**
     * Chấm điểm và lưu kết quả KPI
     * POST /kpi/store
     */
    public function store(): void
    {
        // 1. Bắt buộc có quyền đánh giá KPI
        $this->requirePermission('kpi.evaluate');

        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            $this->json(['status' => 'error', 'success' => false, 'message' => 'Phương thức không được hỗ trợ.'], 405);
            return;
        }

        $employeeId = $_POST['employee_id'] ?? '';
        $score      = (float)($_POST['score'] ?? 0);
        $comments   = trim($_POST['comments'] ?? '');

        if (empty($employeeId) || $score < 0 || $score > 150) {
            $this->json(['status' => 'error', 'success' => false, 'message' => 'Dữ liệu đánh giá không hợp lệ.'], 400);
            return;
        }

        try {
            $db = Database::getInstance()->getConnection();
            $stmt = $db->prepare("INSERT INTO kpi (ma_nv, diem_kpi, xep_loai, trang_thai, nguoi_danh_gia, ngay_danh_gia, ghi_chu, created_at)
                                  VALUES (:manv, :score, :loai, 'DA_DUYET', :reviewer, NOW(), :comments, NOW())");
            
            $rating = $score >= 100 ? 'A' : ($score >= 85 ? 'B' : ($score >= 70 ? 'C' : 'D'));
            $reviewer = Auth::user()['MaNV'] ?? 'ADMIN';

            $stmt->execute([
                ':manv'     => $employeeId,
                ':score'    => $score,
                ':loai'     => $rating,
                ':reviewer' => $reviewer,
                ':comments' => $comments
            ]);

            $this->json([
                'status'  => 'success',
                'success' => true,
                'message' => 'Lưu điểm đánh giá KPI thành công!'
            ]);
        } catch (PDOException $e) {
            error_log("Database Error in KpiController::store: " . $e->getMessage());
            $this->json([
                'status'  => 'error',
                'success' => false,
                'message' => 'Có lỗi xảy ra trong quá trình xử lý. Vui lòng liên hệ quản trị viên.'
            ], 500);
        } catch (Throwable $e) {
            error_log("System Error in KpiController::store: " . $e->getMessage());
            $this->json([
                'status'  => 'error',
                'success' => false,
                'message' => 'Có lỗi xảy ra trong quá trình xử lý. Vui lòng liên hệ quản trị viên.'
            ], 500);
        }
    }
}
