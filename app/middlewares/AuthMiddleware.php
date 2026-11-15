<?php

namespace App\Middlewares;

use App\Core\Middleware;

class AuthMiddleware implements Middleware
{
    public function handle(array $params = []): bool
    {
        // Dùng chung session của hệ thống (tên cookie VNM_HRM_SESSID) với Auth::login().
        // Trước đây gọi session_start() mặc định (PHPSESSID) nên luôn coi là chưa đăng nhập.
        \Session::init();

        $loggedIn = \Auth::check() || isset($_SESSION['user_id']);
        if ($loggedIn) {
            return true;
        }

        $isApi = (isset($_SERVER['HTTP_X_REQUESTED_WITH']) && strtolower($_SERVER['HTTP_X_REQUESTED_WITH']) === 'xmlhttprequest')
            || strpos($_SERVER['REQUEST_URI'] ?? '', '/api/') !== false;

        if ($isApi) {
            http_response_code(401);
            header('Content-Type: application/json; charset=utf-8');
            echo json_encode(['status' => 'error', 'message' => 'Lỗi xác thực: Vui lòng đăng nhập để tiếp tục.'], JSON_UNESCAPED_UNICODE);
        } else {
            \Session::setFlash('login_error', 'Vui lòng đăng nhập để tiếp tục sử dụng hệ thống.', 'warning');
            header('Location: ' . (defined('BASE_URL') ? BASE_URL : '/vinamilk') . '/auth/login');
        }
        return false;
    }
}
