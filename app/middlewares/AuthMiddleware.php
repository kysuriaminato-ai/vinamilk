<?php

namespace App\Middlewares;

use App\Core\Middleware;

class AuthMiddleware implements Middleware
{
    public function handle(array $params = []): bool
    {
        // Khởi tạo session nếu chưa có
        if (session_status() === PHP_SESSION_NONE) {
            session_start();
        }

        // Kiểm tra xem user đã đăng nhập chưa
        if (!isset($_SESSION['user_id'])) {
            // Kiểm tra xem là request API hay Web
            if (isset($_SERVER['HTTP_X_REQUESTED_WITH']) && strtolower($_SERVER['HTTP_X_REQUESTED_WITH']) == 'xmlhttprequest' || strpos($_SERVER['REQUEST_URI'], '/api/') !== false) {
                http_response_code(401); // 401 Unauthorized
                header('Content-Type: application/json');
                
                echo json_encode([
                    'status' => 'error', 
                    'message' => 'Lỗi xác thực: Vui lòng đăng nhập để tiếp tục.'
                ]);
            } else {
                header('Location: /vinamilk/auth/login');
            }
            
            return false; // Chặn request
        }

        return true; // Cho phép đi tiếp
    }
}
