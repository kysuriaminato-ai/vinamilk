<?php
/**
 * =====================================================================
 * VINAMILK HRM - MIDDLEWARE PHÂN QUYỀN VAI TRÒ & QUYỀN HẠN (ROLEMIDDLEWARE)
 * =====================================================================
 * Quản lý:
 *   1. Kiểm tra vai trò trong 7 nhóm vai trò chuẩn Vinamilk
 *   2. Kiểm tra quyền hạn chi tiết theo chuẩn module.hành_động
 *   3. Phản hồi mã lỗi 403 Forbidden chuẩn cho cả Web View và JSON API
 */

require_once __DIR__ . '/AuthMiddleware.php';
require_once __DIR__ . '/../helpers/Auth.php';
require_once __DIR__ . '/../helpers/functions.php';

class RoleMiddleware
{
    /**
     * BẮT BUỘC NGƯỜI DÙNG PHẢI THUỘC MỘT TRONG CÁC VAI TRÒ ĐƯỢC CHỈ ĐỊNH
     * 
     * @param array|string $allowedRoles ADMIN, BOD, HR_TAPDOAN, HR_DONVI, KETOAN_LUONG, QUANLY_TRUCTIEP, NHANVIEN
     */
    public static function checkRole(array|string $allowedRoles): bool
    {
        // 1. Phải vượt qua AuthMiddleware trước
        AuthMiddleware::handle();

        // 2. Kiểm tra vai trò
        if (!Auth::hasRole($allowedRoles)) {
            self::denyAccess('Bạn không thuộc vai trò được phép truy cập chức năng này.');
        }

        return true;
    }

    /**
     * BẮT BUỘC NGƯỜI DÙNG PHẢI CÓ QUYỀN HẠN CỤ THỂ DẠNG module.action
     * (Ví dụ: 'nhansu.view', 'nhansu.edit', 'luong.approve', 'nhansu.sensitive')
     * 
     * @param string $permissionCode Mã quyền hạn
     */
    public static function checkPermission(string $permissionCode): bool
    {
        // 1. Phải vượt qua AuthMiddleware trước
        AuthMiddleware::handle();

        // 2. Kiểm tra quyền hạn chi tiết
        if (!Auth::can($permissionCode)) {
            self::denyAccess("Bạn không có quyền hạn [{$permissionCode}] để thực hiện hành động này.");
        }

        return true;
    }

    /**
     * XỬ LÝ TỪ CHỐI TRUY CẬP (403 FORBIDDEN)
     */
    private static function denyAccess(string $message): void
    {
        if (self::isJsonRequest()) {
            http_response_code(403);
            header('Content-Type: application/json; charset=utf-8');
            echo json_encode([
                'error' => '403 Forbidden',
                'message' => $message
            ], JSON_UNESCAPED_UNICODE | JSON_PRETTY_PRINT);
            exit;
        }

        // Với Web Request: nạp view 403 hoặc thông báo lỗi
        http_response_code(403);
        $title = '403 Forbidden - Truy cập bị từ chối';
        $viewFile = APP_DIR . '/views/errors/403.php';

        if (file_exists($viewFile)) {
            require $viewFile;
        } else {
            echo "<!DOCTYPE html><html lang='vi'><head><meta charset='utf-8'><title>403 Forbidden</title></head>";
            echo "<body style='font-family: sans-serif; text-align: center; padding: 50px;'>";
            echo "<h1 style='color: #0033A0;'>403 Forbidden</h1>";
            echo "<p style='color: #475569;'>" . htmlspecialchars($message, ENT_QUOTES, 'UTF-8') . "</p>";
            echo "<a href='" . url('dashboard') . "' style='color: #0033A0; font-weight: bold;'>Quay lại Bảng Điều Khiển</a>";
            echo "</body></html>";
        }
        exit;
    }

    /**
     * Kiểm tra xem request có phải là API / AJAX hay không
     */
    private static function isJsonRequest(): bool
    {
        return (!empty($_SERVER['HTTP_X_REQUESTED_WITH']) && strtolower($_SERVER['HTTP_X_REQUESTED_WITH']) === 'xmlhttprequest')
            || (isset($_SERVER['HTTP_ACCEPT']) && str_contains($_SERVER['HTTP_ACCEPT'], 'application/json'))
            || (isset($_GET['format']) && $_GET['format'] === 'json');
    }
}
