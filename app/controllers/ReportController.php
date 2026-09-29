<?php

namespace App\Controllers;

use Exception;

class ReportController
{
    /**
     * Render view báo cáo dựa trên tên file
     * Ví dụ: url.com/reports/show?view=employee_summary
     */
    public function show(): void
    {
        // Lấy tên view từ tham số GET (Mặc định là dashboard)
        $viewName = $_GET['view'] ?? 'dashboard';
        
        // Bảo mật: Xóa bỏ các ký tự đặc biệt để chống Path Traversal (LFI)
        $viewName = preg_replace('/[^a-zA-Z0-9_]/', '', $viewName);

        // Đường dẫn tuyệt đối tới thư mục views (sử dụng hằng số __DIR__)
        $viewPath = __DIR__ . '/../../views/reports/' . $viewName . '.php';

        if (file_exists($viewPath)) {
            // Include view nếu file tồn tại
            require_once $viewPath;
        } else {
            // Trả về trang 404 nếu không tìm thấy file
            http_response_code(404);
            echo "<h1>404 Not Found</h1>";
            echo "<p>Không tìm thấy báo cáo: {$viewName}</p>";
        }
    }
}
