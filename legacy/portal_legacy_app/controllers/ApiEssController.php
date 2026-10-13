<?php
/**
 * =====================================================================
 * VINAMILK HRM - API TỰ PHỤC VỤ NHÂN VIÊN ESS (APIESSCONTROLLER)
 * =====================================================================
 * Tuân thủ chuẩn bảo mật:
 *   - Bắt buộc kiểm tra đăng nhập trước khi truy cập
 *   - Tuyệt đối không fallback sang tài khoản ngẫu nhiên
 *   - Không sử dụng CORS wildcard
 *   - Ghi log lỗi PDO vào file log máy chủ qua error_log, không lộ cấu trúc DB
 */

require_once __DIR__ . '/../core/Controller.php';

class ApiEssController extends Controller
{
    /**
     * Dữ liệu tổng hợp cho trang cá nhân ESS
     * GET /api-ess/dashboard-data
     */
    public function dashboardData(): void
    {
        // 1. Bắt buộc người dùng phải đăng nhập hợp lệ
        $this->requireAuth();

        $currentUser = Auth::user();
        $employeeId  = $currentUser['MaNV'] ?? null;

        if (empty($employeeId)) {
            $this->json(['status' => 'error', 'success' => false, 'message' => 'Không tìm thấy thông tin hồ sơ nhân sự của tài khoản này.'], 401);
            return;
        }

        try {
            $db = Database::getInstance()->getConnection();

            // 1. Lấy dữ liệu chấm công 5 ngày gần nhất
            $stmtAtt = $db->prepare("SELECT ngay, gio_vao, gio_ra, cong_thuc_te, loai_cong, trang_thai_duyet 
                                    FROM cham_cong_ngay 
                                    WHERE ma_nv = :manv 
                                    ORDER BY ngay DESC LIMIT 5");
            $stmtAtt->execute([':manv' => $employeeId]);
            $attendance = $stmtAtt->fetchAll(PDO::FETCH_ASSOC);

            // 2. Số dư phép năm hiện tại
            $stmtLeave = $db->prepare("SELECT tong_phep_duoc_huong, phep_da_nghi, phep_cho_duyet, phep_con_lai 
                                       FROM so_du_phep 
                                       WHERE ma_nv = :manv AND nam = :nam LIMIT 1");
            $stmtLeave->execute([':manv' => $employeeId, ':nam' => (int)date('Y')]);
            $leaveBalance = $stmtLeave->fetch(PDO::FETCH_ASSOC) ?: [
                'tong_phep_duoc_huong' => 12, 'phep_da_nghi' => 0, 'phep_cho_duyet' => 0, 'phep_con_lai' => 12
            ];

            // 3. Phiếu lương tháng gần nhất
            $stmtPay = $db->prepare("SELECT blc.thuc_linh, bl.ky_luong 
                                     FROM bang_luong_chi_tiet blc 
                                     JOIN bang_luong bl ON blc.bang_luong_id = bl.id 
                                     WHERE blc.ma_nv = :manv 
                                     ORDER BY bl.ky_luong DESC LIMIT 1");
            $stmtPay->execute([':manv' => $employeeId]);
            $latestPayslip = $stmtPay->fetch(PDO::FETCH_ASSOC);

            $this->json([
                'status'  => 'success',
                'success' => true,
                'data'    => [
                    'employee_id'    => $employeeId,
                    'attendance'     => $attendance,
                    'leave_balance'  => $leaveBalance,
                    'latest_payslip' => $latestPayslip
                ]
            ]);
        } catch (PDOException $e) {
            error_log("Database Error in ApiEssController::dashboardData: " . $e->getMessage());
            $this->json([
                'status'  => 'error',
                'success' => false,
                'message' => 'Có lỗi xảy ra trong quá trình xử lý. Vui lòng liên hệ quản trị viên.'
            ], 500);
        } catch (Throwable $e) {
            error_log("System Error in ApiEssController::dashboardData: " . $e->getMessage());
            $this->json([
                'status'  => 'error',
                'success' => false,
                'message' => 'Có lỗi xảy ra trong quá trình xử lý. Vui lòng liên hệ quản trị viên.'
            ], 500);
        }
    }
}
