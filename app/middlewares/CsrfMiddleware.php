<?php

namespace App\Middlewares;

use App\Core\Middleware;

class CsrfMiddleware implements Middleware
{
    public function handle(array $params = []): bool
    {
        // Chỉ áp dụng CSRF check cho các method làm thay đổi dữ liệu
        if ($_SERVER['REQUEST_METHOD'] === 'POST' || $_SERVER['REQUEST_METHOD'] === 'PUT' || $_SERVER['REQUEST_METHOD'] === 'DELETE') {
            if (session_status() === PHP_SESSION_NONE) {
                session_start();
            }

            // Lấy token từ Header (do JS gửi lên) hoặc từ biến $_POST (nếu là form submit thông thường)
            $headerToken = $_SERVER['HTTP_X_CSRF_TOKEN'] ?? '';
            $postToken = $_POST['csrf_token'] ?? '';
            
            $clientToken = !empty($headerToken) ? $headerToken : $postToken;
            $serverToken = $_SESSION['csrf_token'] ?? null;

            // Kiểm tra tính hợp lệ
            if (empty($clientToken) || empty($serverToken) || !hash_equals($serverToken, $clientToken)) {
                http_response_code(419); // 419 Page Expired (Chuẩn Laravel cho CSRF Error)
                header('Content-Type: application/json');
                
                echo json_encode([
                    'status' => 'error', 
                    'message' => 'Lỗi bảo mật CSRF: Yêu cầu đã bị từ chối do thiếu mã thông báo bảo mật hợp lệ.'
                ]);
                
                return false; // Chặn request
            }
        }

        return true; // Hợp lệ, cho phép đi tiếp
    }
}
