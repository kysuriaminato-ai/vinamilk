<?php
/**
 * =====================================================================
 * VINAMILK HRM - LỚP CONTROLLER CƠ SỞ (BASE CONTROLLER)
 * =====================================================================
 * Quản lý:
 *   1. Kiểm tra CSRF token bắt buộc cho mọi form POST
 *   2. Kiểm tra phân quyền truy cập theo 7 vai trò Vinamilk
 *   3. Nạp view nhúng trong Master Layout chuẩn phong cách Vinamilk
 *   4. Phản hồi JSON API và chuyển hướng an toàn
 */

require_once __DIR__ . '/Session.php';
require_once __DIR__ . '/AuthMiddleware.php';
require_once __DIR__ . '/RoleMiddleware.php';
require_once __DIR__ . '/../helpers/Auth.php';
require_once __DIR__ . '/../helpers/functions.php';

abstract class Controller
{
    /**
     * Khởi tạo đối tượng Model
     */
    public function model(string $modelName): object
    {
        $file = APP_DIR . '/models/' . $modelName . '.php';
        if (!file_exists($file)) {
            $file = APP_DIR . '/Models/' . $modelName . '.php';
        }
        if (file_exists($file)) {
            require_once $file;
            if (class_exists($modelName)) {
                return new $modelName();
            }
        }
        throw new Exception("Model [{$modelName}] không tồn tại tại đường dẫn: {$file}");
    }

    /**
     * Nạp View với dữ liệu truyền vào và nhúng trong Layout Master
     * 
     * @param string $viewPath Đường dẫn view (VD: 'dashboard/index', 'employees/show')
     * @param array $data Mảng biến truyền vào view
     * @param string|null $layout Tên layout ('master', 'blank', null)
     */
    public function view(string $viewPath, array $data = [], ?string $layout = 'master'): void
    {
        $viewFile = APP_DIR . '/views/' . str_replace('.', '/', $viewPath) . '.php';

        if (!file_exists($viewFile)) {
            throw new Exception("View file [{$viewFile}] không tồn tại!");
        }

        // Tự động truyền thông tin người dùng và quyền hạn hiện tại vào mọi view
        $data['currentUser'] = Auth::user();
        $data['userRole']    = Auth::role();

        extract($data);

        // Trường hợp không dùng layout (view độc lập, login, in ấn đơn lẻ)
        if ($layout === null || $layout === 'blank') {
            require $viewFile;
            return;
        }

        // Nạp qua Master Layout
        $layoutFile = APP_DIR . '/views/layouts/' . $layout . '.php';
        if (file_exists($layoutFile)) {
            ob_start();
            require $viewFile;
            $content = ob_get_clean();

            require $layoutFile;
        } else {
            require $viewFile;
        }
    }

    /**
     * Trả về kết quả JSON API
     */
    public function json(array $data, int $statusCode = 200): void
    {
        http_response_code($statusCode);
        header('Content-Type: application/json; charset=utf-8');
        echo json_encode($data, JSON_UNESCAPED_UNICODE | JSON_PRETTY_PRINT);
        exit;
    }

    /**
     * Kiểm tra xem request hiện tại có yêu cầu JSON API hay không
     */
    protected function isJsonRequest(): bool
    {
        return (!empty($_SERVER['HTTP_X_REQUESTED_WITH']) && strtolower($_SERVER['HTTP_X_REQUESTED_WITH']) === 'xmlhttprequest')
            || (isset($_SERVER['HTTP_ACCEPT']) && str_contains($_SERVER['HTTP_ACCEPT'], 'application/json'))
            || (isset($_GET['format']) && $_GET['format'] === 'json');
    }

    /**
     * Chuyển hướng trình duyệt
     */
    public function redirect(string $path): void
    {
        redirect($path);
    }

    /**
     * Bắt buộc người dùng phải Đăng nhập (kèm timeout 30 phút và cờ đổi mật khẩu)
     */
    protected function requireAuth(bool $allowMustChangePassword = false): void
    {
        AuthMiddleware::handle($allowMustChangePassword);
    }

    /**
     * Bắt buộc có Vai trò cụ thể trong 7 vai trò hệ thống
     * 
     * @param array|string $allowedRoles ADMIN, BOD, HR_TAPDOAN, HR_DONVI, KETOAN_LUONG, QUANLY_TRUCTIEP, NHANVIEN
     */
    protected function requireRole(array|string $allowedRoles): void
    {
        RoleMiddleware::checkRole($allowedRoles);
    }

    /**
     * Bắt buộc có Quyền hạn cụ thể dạng module.hành_động
     * 
     * @param string $permissionCode nhansu.view, luong.approve, nhansu.sensitive...
     */
    protected function requirePermission(string $permissionCode): void
    {
        RoleMiddleware::checkPermission($permissionCode);
    }

    /**
     * Xác thực CSRF Token bắt buộc cho mọi yêu cầu POST / PUT / DELETE (BẮT BUỘC THEO ĐẶC TẢ)
     */
    protected function validateCsrf(): void
    {
        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            if (!Session::verifyCsrfToken()) {
                Session::setFlash('error', 'Lỗi bảo mật: CSRF Token không hợp lệ hoặc đã hết hạn! Vui lòng tải lại trang và thử lại.', 'danger');
                $referer = $_SERVER['HTTP_REFERER'] ?? url('dashboard');
                $this->redirect($referer);
            }
        }
    }
}
