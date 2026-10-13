<?php
/**
 * =====================================================================
 * VINAMILK HRM - API DANH SÁCH NHÂN SỰ (APIEMPLOYEECONTROLLER)
 * =====================================================================
 * Tuân thủ chuẩn bảo mật:
 *   - Bắt buộc kiểm tra quyền hạn trước mọi hành động
 *   - Tuyệt đối không sử dụng CORS wildcard
 *   - Ghi log lỗi PDO vào file log máy chủ qua error_log, không lộ cấu trúc DB
 */

require_once __DIR__ . '/../core/Controller.php';

class ApiEmployeeController extends Controller
{
    /**
     * Lấy danh sách nhân sự (JSON)
     * GET /api-employee/index
     */
    public function index(): void
    {
        // 1. Bắt buộc có quyền xem hồ sơ nhân sự
        $this->requirePermission('nhansu.view');

        try {
            $page   = max(1, (int)($_GET['page'] ?? 1));
            $limit  = max(1, min(100, (int)($_GET['limit'] ?? 15)));
            $offset = ($page - 1) * $limit;
            $search = trim($_GET['search'] ?? '');

            $db = Database::getInstance()->getConnection();

            if (!empty($search)) {
                $stmt = $db->prepare("SELECT MaNV, HoTen, MaDV, ChucVu, TrangThai, NgayVaoLam, Email, SoDienThoai 
                                      FROM nhansu 
                                      WHERE MaNV LIKE :s OR HoTen LIKE :s 
                                      ORDER BY MaNV ASC LIMIT :lim OFFSET :off");
                $stmt->bindValue(':s', "%{$search}%", PDO::PARAM_STR);
                $stmt->bindValue(':lim', $limit, PDO::PARAM_INT);
                $stmt->bindValue(':off', $offset, PDO::PARAM_INT);
                $stmt->execute();
                $employees = $stmt->fetchAll(PDO::FETCH_ASSOC);

                $countStmt = $db->prepare("SELECT COUNT(*) FROM nhansu WHERE MaNV LIKE :s OR HoTen LIKE :s");
                $countStmt->execute([':s' => "%{$search}%"]);
                $total = (int)$countStmt->fetchColumn();
            } else {
                $stmt = $db->prepare("SELECT MaNV, HoTen, MaDV, ChucVu, TrangThai, NgayVaoLam, Email, SoDienThoai 
                                      FROM nhansu 
                                      ORDER BY MaNV ASC LIMIT :lim OFFSET :off");
                $stmt->bindValue(':lim', $limit, PDO::PARAM_INT);
                $stmt->bindValue(':off', $offset, PDO::PARAM_INT);
                $stmt->execute();
                $employees = $stmt->fetchAll(PDO::FETCH_ASSOC);

                $total = (int)$db->query("SELECT COUNT(*) FROM nhansu")->fetchColumn();
            }

            $this->json([
                'status'  => 'success',
                'success' => true,
                'data'    => $employees,
                'meta'    => [
                    'current_page'  => $page,
                    'per_page'      => $limit,
                    'total_records' => $total,
                    'total_pages'   => ceil($total / $limit)
                ]
            ]);
        } catch (PDOException $e) {
            error_log("Database Error in ApiEmployeeController::index: " . $e->getMessage());
            $this->json([
                'status'  => 'error',
                'success' => false,
                'message' => 'Có lỗi xảy ra trong quá trình xử lý. Vui lòng liên hệ quản trị viên.'
            ], 500);
        } catch (Throwable $e) {
            error_log("System Error in ApiEmployeeController::index: " . $e->getMessage());
            $this->json([
                'status'  => 'error',
                'success' => false,
                'message' => 'Có lỗi xảy ra trong quá trình xử lý. Vui lòng liên hệ quản trị viên.'
            ], 500);
        }
    }
}
