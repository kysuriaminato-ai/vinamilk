<?php
/**
 * =====================================================================
 * VINAMILK HRM - API BẢO MẬT & CSRF TOKEN (APISECURITYCONTROLLER)
 * =====================================================================
 * Tuân thủ chuẩn bảo mật:
 *   - Bắt buộc kiểm tra đăng nhập trước khi cấp phát CSRF Token
 *   - Tuyệt đối không sử dụng CORS wildcard (*)
 *   - Phòng chống tấn công Cross-Origin Request Forgery
 */

require_once __DIR__ . '/../core/Controller.php';

class ApiSecurityController extends Controller
{
    /**
     * Cung cấp CSRF Token cho người dùng đã xác thực
     * GET /api-security/csrf-token
     */
    public function csrfToken(): void
    {
        // 1. Bắt buộc người dùng phải đăng nhập trước khi cấp phát token
        $this->requireAuth();

        if (session_status() === PHP_SESSION_NONE) {
            Session::start();
        }

        $token = Session::getCsrfToken();

        $this->json([
            'status'     => 'success',
            'success'    => true,
            'csrf_token' => $token
        ]);
    }
}
