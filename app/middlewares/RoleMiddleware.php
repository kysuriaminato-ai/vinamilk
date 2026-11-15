<?php

namespace App\Middlewares;

use App\Core\Middleware;

class RoleMiddleware implements Middleware
{
    public function handle(array $params = []): bool
    {
        \Session::init();

        // Vai trò lấy từ phiên đăng nhập của Auth (RoleCode: Admin, HR_Manager, Manager, Employee)
        $userRole = \Auth::roleCode() ?? ($_SESSION['user_role'] ?? 'Guest');
        $allowedRoles = $params['allowed_roles'] ?? [];

        if (empty($allowedRoles) || in_array($userRole, $allowedRoles, true)) {
            return true;
        }

        // Vai trò khác (vd. tài khoản "Quản trị hệ thống" tự tạo) vẫn được vào nếu đã được cấp quyền chức năng
        if (!empty($params['permission']) && \Auth::can($params['permission'])) {
            return true;
        }

        $isApi = (isset($_SERVER['HTTP_X_REQUESTED_WITH']) && strtolower($_SERVER['HTTP_X_REQUESTED_WITH']) === 'xmlhttprequest')
            || strpos($_SERVER['REQUEST_URI'] ?? '', '/api/') !== false;

        http_response_code(403);
        if ($isApi) {
            header('Content-Type: application/json; charset=utf-8');
            echo json_encode(['status' => 'error', 'message' => 'Lỗi phân quyền: Bạn không có quyền truy cập vào chức năng này.'], JSON_UNESCAPED_UNICODE);
        } else {
            \Session::setFlash('error', 'Tài khoản của bạn (' . $userRole . ') không có quyền truy cập chức năng này.', 'danger');
            header('Location: ' . (defined('BASE_URL') ? BASE_URL : '/vinamilk') . '/dashboard');
        }
        return false;
    }
}
