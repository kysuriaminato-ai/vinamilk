<?php
/**
 * =====================================================================
 * VINAMILK HRM - API TIẾP NHẬN NHÂN SỰ MỚI (APIONBOARDINGCONTROLLER)
 * =====================================================================
 * Tuân thủ chuẩn bảo mật:
 *   - Bắt buộc kiểm tra quyền hạn nhansu.create trước khi tiếp nhận
 *   - Không sử dụng CORS wildcard
 *   - Ghi log lỗi PDO vào file log máy chủ qua error_log, không lộ cấu trúc DB
 */

require_once __DIR__ . '/../core/Controller.php';

class ApiOnboardingController extends Controller
{
    /**
     * Xử lý tiếp nhận nhân sự mới
     * POST /api-onboarding/process
     */
    public function process(): void
    {
        // 1. Bắt buộc có quyền tạo hồ sơ nhân sự
        $this->requirePermission('nhansu.create');

        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            $this->json(['status' => 'error', 'success' => false, 'message' => 'Phương thức không được hỗ trợ.'], 405);
            return;
        }

        $name         = trim($_POST['name'] ?? '');
        $departmentId = trim($_POST['department_id'] ?? '');
        $position     = trim($_POST['position'] ?? 'Nhân viên');
        $salary       = (float)($_POST['salary'] ?? 0);

        if (empty($name) || empty($departmentId) || $salary <= 0) {
            $this->json(['status' => 'error', 'success' => false, 'message' => 'Thông tin tiếp nhận không đầy đủ hoặc không hợp lệ.'], 400);
            return;
        }

        try {
            $db = Database::getInstance()->getConnection();
            $db->beginTransaction();

            // Sinh mã nhân sự mới dạng VNM-xxxx
            $lastId = (int)$db->query("SELECT MAX(CAST(SUBSTRING(MaNV, 5) AS UNSIGNED)) FROM nhansu WHERE MaNV LIKE 'VNM-%'")->fetchColumn();
            $newMaNV = sprintf('VNM-%04d', $lastId + 1);

            $stmtEmp = $db->prepare("INSERT INTO nhansu (MaNV, HoTen, TenThuongGoi, MaDV, ChucVu, VungLuong, TrangThai, NgayVaoLam, LuongBaoHiem, CreatedAt, UpdatedAt)
                                     VALUES (:manv, :name, :name, :madv, :chucvu, 1, 2, NOW(), :luong, NOW(), NOW())");
            $stmtEmp->execute([
                ':manv'   => $newMaNV,
                ':name'   => $name,
                ':madv'   => $departmentId,
                ':chucvu' => $position,
                ':luong'  => min(46800000.0, $salary)
            ]);

            // Tạo hợp đồng thử việc mặc định
            $stmtHd = $db->prepare("INSERT INTO hop_dong_lao_dong (ma_nv, so_hd, loai_hd, ngay_ky, tu_ngay, den_ngay, chuc_danh, luong_can_ban, luong_dong_bh, ty_le_huong_luong, trang_thai, created_at, updated_at)
                                    VALUES (:manv, :sohd, 'THU_VIEC', NOW(), NOW(), DATE_ADD(NOW(), INTERVAL 2 MONTH), :chucvu, :luong, :luongbh, 85.0, 'HIEU_LUC', NOW(), NOW())");
            $stmtHd->execute([
                ':manv'    => $newMaNV,
                ':sohd'    => "HD-TV/{$newMaNV}/" . date('Y'),
                ':chucvu'  => $position,
                ':luong'   => $salary,
                ':luongbh' => min(46800000.0, $salary)
            ]);

            $db->commit();

            $this->json([
                'status'      => 'success',
                'success'     => true,
                'message'     => 'Tiếp nhận nhân sự mới thành công!',
                'employee_id' => $newMaNV
            ]);
        } catch (PDOException $e) {
            if (isset($db) && $db->inTransaction()) {
                $db->rollBack();
            }
            error_log("Database Error in ApiOnboardingController::process: " . $e->getMessage());
            $this->json([
                'status'  => 'error',
                'success' => false,
                'message' => 'Có lỗi xảy ra trong quá trình xử lý. Vui lòng liên hệ quản trị viên.'
            ], 500);
        } catch (Throwable $e) {
            if (isset($db) && $db->inTransaction()) {
                $db->rollBack();
            }
            error_log("System Error in ApiOnboardingController::process: " . $e->getMessage());
            $this->json([
                'status'  => 'error',
                'success' => false,
                'message' => 'Có lỗi xảy ra trong quá trình xử lý. Vui lòng liên hệ quản trị viên.'
            ], 500);
        }
    }
}
