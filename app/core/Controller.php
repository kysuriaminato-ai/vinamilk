<?php
/**
 * =====================================================================
 * VINAMILK HRM - Base Controller Class
 * =====================================================================
 * 
 * Lớp Controller cơ sở trong kiến trúc MVC Vinamilk HRM.
 * Quản lý việc nạp View, gọi Model, phản hồi JSON API, kiểm tra CSRF token,
 * và ràng buộc quyền hạn người dùng (Authentication & Dynamic Authorization).
 */

require_once APP_DIR . '/helpers/Auth.php';

abstract class Controller {
    /**
     * Nạp và khởi tạo đối tượng Model
     */
    public function model(string $modelName): Model {
        $file = APP_DIR . '/models/' . $modelName . '.php';
        if (file_exists($file)) {
            require_once $file;
            return new $modelName();
        }
        throw new Exception("Model {$modelName} không tồn tại tại path: {$file}");
    }

    /**
     * Nạp View với dữ liệu truyền vào và nhúng trong Master Layout
     *
     * @param string $viewPath Ví dụ: 'auth/login', 'dashboard/index', 'roles/permissions'
     * @param array $data Mảng biến dữ liệu truyền tới view
     * @param string|null $layout Tên layout ('master', 'blank', null)
     */
    public function view(string $viewPath, array $data = [], ?string $layout = 'master'): void {
        $viewFile = APP_DIR . '/views/' . str_replace('.', '/', $viewPath) . '.php';

        if (!file_exists($viewFile)) {
            throw new Exception("View file {$viewFile} không tồn tại!");
        }

        // Trích xuất mảng $data thành các biến đơn lẻ trong view
        extract($data);

        // Nếu chỉ nạp view độc lập không dùng layout (Ví dụ: view AJAX, login page)
        if ($layout === null || $layout === 'blank') {
            require $viewFile;
            return;
        }

        // Trường hợp nạp qua Master Layout tổng thể
        $layoutFile = APP_DIR . '/views/layouts/' . $layout . '.php';
        if (file_exists($layoutFile)) {
            // Lưu nội dung view con vào biến $content để nhúng vào layout master
            ob_start();
            require $viewFile;
            $content = ob_get_clean();

            require $layoutFile;
        } else {
            // Fallback nếu layout file không tồn tại
            require $viewFile;
        }
    }

    /**
     * Trả về kết quả JSON API
     */
    public function json(array $data, int $statusCode = 200): void {
        http_response_code($statusCode);
        header('Content-Type: application/json; charset=utf-8');
        echo json_encode($data, JSON_UNESCAPED_UNICODE | JSON_PRETTY_PRINT);
        exit;
    }

    /**
     * Chuyển hướng trang
     */
    public function redirect(string $path): void {
        redirect($path);
    }

    /**
     * Bắt buộc người dùng phải Đăng nhập (Authentication Guard)
     */
    protected function requireAuth(): void {
        if (!Auth::check()) {
            Session::setFlash('login_error', 'Vui lòng đăng nhập để tiếp tục sử dụng hệ thống.', 'warning');
            $this->redirect('auth/login');
        }
    }

    /**
     * Bắt buộc có Quyền hạn cụ thể (Dynamic Permission Guard)
     * Ví dụ: $this->requirePermission('NHANSU.View');
     */
    protected function requirePermission(string $permission): void {
        $this->requireAuth();

        if (!Auth::can($permission)) {
            // Nếu là request AJAX, trả về JSON 403
            if (!empty($_SERVER['HTTP_X_REQUESTED_WITH']) && strtolower($_SERVER['HTTP_X_REQUESTED_WITH']) === 'xmlhttprequest') {
                $this->json(['error' => 'Truy cập bị từ chối. Bạn không có quyền thực hiện hành động này.'], 403);
            }

            // Hiển thị trang lỗi 403 Forbidden
            $this->view('errors/403', [
                'title' => '403 Forbidden - Truy cập bị từ chối',
                'permission' => $permission
            ]);
            exit;
        }
    }

    /**
     * Bắt buộc phải khớp CSRF Token nếu là POST/PUT/DELETE request
     */
    protected function validateCsrf(): void {
        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            if (!Session::verifyCsrfToken()) {
                Session::setFlash('error', 'Lỗi xác thực CSRF Token! Yêu cầu không hợp lệ.', 'danger');
                $this->redirect($_SERVER['HTTP_REFERER'] ?? 'dashboard');
            }
        }
    }
}
