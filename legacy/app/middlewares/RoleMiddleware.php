<?php

namespace App\Middlewares;

use App\Core\Middleware;

class RoleMiddleware implements Middleware
{
    public function handle(array $params = []): bool
    {
        if (session_status() === PHP_SESSION_NONE) {
            session_start();
        }

        // Lấy role của user hiện tại (Mặc định là Guest nếu không có)
        $userRole = $_SESSION['user_role'] ?? 'Guest';

        // Lấy mảng các roles được phép truy cập từ cấu hình Router
        $allowedRoles = $params['allowed_roles'] ?? [];

        // Nếu mảng rỗng (không yêu cầu role cụ thể) hoặc user có role phù hợp
        if (empty($allowedRoles) || in_array($userRole, $allowedRoles)) {
            return true;
        }

        // Bị từ chối truy cập do thiếu quyền
        http_response_code(403); // 403 Forbidden
        header('Content-Type: application/json');
        
        echo json_encode([
            'status' => 'error', 
            'message' => 'Lỗi phân quyền: Bạn không có quyền truy cập vào chức năng này.'
        ]);
        
        return false;
    }
}
