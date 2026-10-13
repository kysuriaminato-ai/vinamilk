<?php
/**
 * =====================================================================
 * VINAMILK HRM - API PHÂN TÍCH NHÂN SỰ & BÁO CÁO (APIANALYTICSCONTROLLER)
 * =====================================================================
 * Tuân thủ chuẩn bảo mật:
 *   - Bắt buộc kiểm tra quyền hạn trước mọi hành động
 *   - Tuyệt đối không sử dụng CORS wildcard
 *   - Ghi log lỗi PDO vào file log máy chủ qua error_log, không lộ cấu trúc DB
 */

require_once __DIR__ . '/../core/Controller.php';

class ApiAnalyticsController extends Controller
{
    /**
     * Dữ liệu phân tích tổng quan
     * GET /api-analytics/overview
     */
    public function overview(): void
    {
        // 1. Bắt buộc có quyền xem báo cáo nhân sự
        $this->requirePermission('baocao.view');

        try {
            $db = Database::getInstance()->getConnection();

            // Tổng nhân sự đang làm việc
            $totalActive = (int)$db->query("SELECT COUNT(*) FROM nhansu WHERE TrangThai = 1")->fetchColumn();
            
            // Nhân sự thử việc
            $probation = (int)$db->query("SELECT COUNT(*) FROM nhansu WHERE TrangThai = 2")->fetchColumn();

            // Nhân sự thôi việc
            $terminated = (int)$db->query("SELECT COUNT(*) FROM nhansu WHERE TrangThai = 3")->fetchColumn();

            // Phân bổ theo loại đơn vị
            $distribution = $db->query("SELECT d.LoaiDV as type, COUNT(n.MaNV) as count 
                                        FROM nhansu n 
                                        JOIN dm_donvi d ON n.MaDV = d.MaDV 
                                        GROUP BY d.LoaiDV")->fetchAll(PDO::FETCH_ASSOC);

            $this->json([
                'status'  => 'success',
                'success' => true,
                'data'    => [
                    'total_active'            => $totalActive,
                    'probation'               => $probation,
                    'terminated'              => $terminated,
                    'department_distribution' => $distribution,
                    'last_updated'            => date('Y-m-d H:i:s')
                ]
            ]);
        } catch (PDOException $e) {
            error_log("Database Error in ApiAnalyticsController::overview: " . $e->getMessage());
            $this->json([
                'status'  => 'error',
                'success' => false,
                'message' => 'Có lỗi xảy ra trong quá trình xử lý. Vui lòng liên hệ quản trị viên.'
            ], 500);
        } catch (Throwable $e) {
            error_log("System Error in ApiAnalyticsController::overview: " . $e->getMessage());
            $this->json([
                'status'  => 'error',
                'success' => false,
                'message' => 'Có lỗi xảy ra trong quá trình xử lý. Vui lòng liên hệ quản trị viên.'
            ], 500);
        }
    }
}
