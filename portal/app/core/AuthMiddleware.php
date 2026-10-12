<?php
/**
 * =====================================================================
 * VINAMILK HRM - MIDDLEWARE XÁC THỰC NGƯỜI DÙNG (AUTHMIDDLEWARE)
 * =====================================================================
 * Kiểm tra:
 *   1. Trạng thái đăng nhập (Authentication)
 *   2. Timeout phiên làm việc 30 phút không hoạt động (Inactivity Timeout)
 *   3. Bắt buộc đổi mật khẩu lần đầu (MustChangePassword)
 */

require_once __DIR__ . '/Session.php';
require_once __DIR__ . '/../helpers/Auth.php';
require_once __DIR__ . '/../helpers/functions.php';

class AuthMiddleware
{
    /**
     * Thời gian chờ tối đa không hoạt động: 30 phút = 1800 giây
     */
    public const TIMEOUT_SECONDS = 1800;

    /**
     * Thực thi kiểm tra xác thực cho Request hiện tại
     * 
     * @param bool $allowMustChangePassword Cho phép truy cập ngay cả khi chưa đổi pass (chỉ dùng cho màn đổi pass)
     * @return bool True nếu hợp lệ, nếu không sẽ chuyển hướng hoặc kết thúc response
     */
    public static function handle(bool $allowMustChangePassword = false): bool
    {
        // 1. Kiểm tra trạng thái đăng nhập
        if (!Auth::check()) {
            if (self::isJsonRequest()) {
                http_response_code(401);
                header('Content-Type: application/json; charset=utf-8');
                echo json_encode([
                    'error' => '401 Unauthorized',
                    'message' => 'Bạn chưa đăng nhập hoặc phiên làm việc đã kết thúc.'
                ], JSON_UNESCAPED_UNICODE);
                exit;
            }

            Session::setFlash('login_error', 'Vui lòng đăng nhập để tiếp tục sử dụng hệ thống Vinamilk HRM.', 'warning');
            redirect('auth/login');
            exit;
        }

        // 2. Kiểm tra Timeout phiên làm việc 30 phút (1800s)
        $lastActivity = Session::get('vnm_last_activity');
        $currentTime  = time();

        if ($lastActivity !== null && ($currentTime - (int)$lastActivity) > self::TIMEOUT_SECONDS) {
            // Phiên đã hết hạn
            Auth::logout();

            if (self::isJsonRequest()) {
                http_response_code(401);
                header('Content-Type: application/json; charset=utf-8');
                echo json_encode([
                    'error' => '401 Session Timeout',
                    'message' => 'Phiên làm việc đã tự động kết thúc sau 30 phút không hoạt động để bảo mật.'
                ], JSON_UNESCAPED_UNICODE);
                exit;
            }

            Session::setFlash('login_error', 'Phiên làm việc đã tự động kết thúc sau 30 phút không hoạt động. Vui lòng đăng nhập lại.', 'warning');
            redirect('auth/login');
            exit;
        }

        // Cập nhật lại mốc thời gian hoạt động cuối cùng
        Session::set('vnm_last_activity', $currentTime);

        // 3. Kiểm tra cờ BẮT BUỘC ĐỔI MẬT KHẨU LẦN ĐẦU (MustChangePassword)
        if (!$allowMustChangePassword && Auth::mustChangePassword()) {
            $currentUrl = $_GET['url'] ?? '';
            if ($currentUrl !== 'auth/change_password' && $currentUrl !== 'auth/logout') {
                if (self::isJsonRequest()) {
                    http_response_code(403);
                    header('Content-Type: application/json; charset=utf-8');
                    echo json_encode([
                        'error' => '403 Password Change Required',
                        'message' => 'Bạn bắt buộc phải đổi mật khẩu trong lần đầu đăng nhập trước khi tiếp tục.'
                    ], JSON_UNESCAPED_UNICODE);
                    exit;
                }

                Session::setFlash('warning', 'Đây là lần đầu đăng nhập. Bạn bắt buộc phải đổi mật khẩu mới để đảm bảo an toàn thông tin.', 'warning');
                redirect('auth/change_password');
                exit;
            }
        }

        return true;
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
