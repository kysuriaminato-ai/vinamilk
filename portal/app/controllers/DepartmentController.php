<?php
/**
 * =====================================================================
 * VINAMILK HRM - CONTROLLER PHÒNG BAN & ĐƠN VỊ (DEPARTMENTCONTROLLER)
 * =====================================================================
 * Tuân thủ chuẩn bảo mật:
 *   - Bắt buộc kiểm tra quyền hạn trước mọi hành động
 *   - Không sử dụng CORS wildcard
 *   - Ghi log lỗi PDO vào file log máy chủ qua error_log, không lộ cấu trúc DB
 */

require_once __DIR__ . '/../core/Controller.php';

class DepartmentController extends Controller
{
    /**
     * Danh sách phòng ban và đơn vị
     * GET /department
     */
    public function index(): void
    {
        // 1. Bắt buộc có quyền xem cơ cấu tổ chức
        $this->requirePermission('donvi.view');

        try {
            $db = Database::getInstance()->getConnection();
            $stmt = $db->query("SELECT MaDV, TenDV, TenVietTat, CapDV, MaDV_Cha, LoaiDV, TrangThai FROM dm_donvi ORDER BY CapDV ASC, MaDV ASC");
            $departments = $stmt->fetchAll(PDO::FETCH_ASSOC);

            $this->json([
                'status'  => 'success',
                'success' => true,
                'data'    => $departments
            ]);
        } catch (PDOException $e) {
            error_log("Database Error in DepartmentController::index: " . $e->getMessage());
            $this->json([
                'status'  => 'error',
                'success' => false,
                'message' => 'Có lỗi xảy ra trong quá trình xử lý. Vui lòng liên hệ quản trị viên.'
            ], 500);
        } catch (Throwable $e) {
            error_log("System Error in DepartmentController::index: " . $e->getMessage());
            $this->json([
                'status'  => 'error',
                'success' => false,
                'message' => 'Có lỗi xảy ra trong quá trình xử lý. Vui lòng liên hệ quản trị viên.'
            ], 500);
        }
    }

    /**
     * Tạo mới phòng ban / đơn vị
     * POST /department/store
     */
    public function store(): void
    {
        // 1. Bắt buộc có quyền chỉnh sửa cơ cấu tổ chức
        $this->requirePermission('donvi.edit');

        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            $this->json(['status' => 'error', 'success' => false, 'message' => 'Phương thức không được hỗ trợ.'], 405);
            return;
        }

        $id       = trim($_POST['id'] ?? '');
        $name     = trim($_POST['name'] ?? '');
        $parentId = !empty($_POST['parent_id']) ? trim($_POST['parent_id']) : null;

        if (empty($id) || empty($name)) {
            $this->json(['status' => 'error', 'success' => false, 'message' => 'Mã và Tên đơn vị không được để trống.'], 400);
            return;
        }

        try {
            $db = Database::getInstance()->getConnection();
            $stmt = $db->prepare("INSERT INTO dm_donvi (MaDV, TenDV, MaDV_Cha, LoaiDV, TrangThai, CreatedAt)
                                  VALUES (:id, :name, :parent, 'Phong', 1, NOW())");
            $stmt->execute([
                ':id'     => $id,
                ':name'   => $name,
                ':parent' => $parentId
            ]);

            $this->json([
                'status'  => 'success',
                'success' => true,
                'message' => 'Thêm mới đơn vị phòng ban thành công!'
            ]);
        } catch (PDOException $e) {
            error_log("Database Error in DepartmentController::store: " . $e->getMessage());
            $this->json([
                'status'  => 'error',
                'success' => false,
                'message' => 'Có lỗi xảy ra trong quá trình xử lý. Vui lòng liên hệ quản trị viên.'
            ], 500);
        } catch (Throwable $e) {
            error_log("System Error in DepartmentController::store: " . $e->getMessage());
            $this->json([
                'status'  => 'error',
                'success' => false,
                'message' => 'Có lỗi xảy ra trong quá trình xử lý. Vui lòng liên hệ quản trị viên.'
            ], 500);
        }
    }
}
