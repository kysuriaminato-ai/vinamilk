<?php

namespace App\Controllers;

class EmployeeDashboardController
{
    /**
     * Trả về giao diện HTML của Bảng điều khiển cá nhân (Self-Service)
     */
    public function index(): void
    {
        // Đường dẫn tuyệt đối tới file view
        $viewPath = __DIR__ . '/../../views/employee_dashboard/index.php';

        if (file_exists($viewPath)) {
            require_once $viewPath;
        } else {
            http_response_code(404);
            echo "<h1>404 Not Found</h1>";
            echo "<p>Không tìm thấy file giao diện: views/employee_dashboard/index.php</p>";
        }
    }
}
