<?php

/**
 * VINAMILK HRIS - Front Controller (Core Router)
 * Tất cả request đều đi qua file này nhờ cấu hình .htaccess
 */

// 1. Nạp Autoloader của Composer (Chuẩn PSR-4)
$autoloadPath = __DIR__ . '/../vendor/autoload.php';
if (file_exists($autoloadPath)) {
    require_once $autoloadPath;
} else {
    die('Lỗi: Chưa chạy lệnh "composer install". Vui lòng chạy composer install ở thư mục gốc.');
}

// 2. Load biến môi trường từ file .env
use Dotenv\Dotenv;
$dotenv = Dotenv::createImmutable(dirname(__DIR__));
if (file_exists(dirname(__DIR__) . '/.env')) {
    $dotenv->load();
}

// 3. Simple Router Logic
// Lấy URL truyền vào từ .htaccess (Ví dụ: public/department/index)
$url = isset($_GET['url']) ? rtrim($_GET['url'], '/') : '';
$url = filter_var($url, FILTER_SANITIZE_URL);
$urlParams = explode('/', $url);

// Mặc định gọi DashboardController nếu URL trống
$controllerName = 'Dashboard';
$actionName = 'index';

// Phân tích Controller
if (!empty($urlParams[0])) {
    // Chuyển đổi tên Controller theo chuẩn StudlyCaps (VD: department -> Department)
    $controllerName = str_replace(' ', '', ucwords(str_replace('-', ' ', $urlParams[0])));
}

// Phân tích Action (Method)
if (isset($urlParams[1])) {
    // Chuyển đổi tên Action theo chuẩn camelCase
    $actionName = lcfirst(str_replace(' ', '', ucwords(str_replace('-', ' ', $urlParams[1]))));
}

// Lấy các tham số còn lại truyền vào hàm
$params = array_slice($urlParams, 2);

// Đường dẫn class đầy đủ
$controllerClass = '\\App\\Controllers\\' . $controllerName . 'Controller';

// 4. Dispatcher (Khởi tạo Controller và gọi Method)
if (class_exists($controllerClass)) {
    $controllerInstance = new $controllerClass();
    
    if (method_exists($controllerInstance, $actionName)) {
        // Gọi hàm với các tham số tương ứng
        call_user_func_array([$controllerInstance, $actionName], $params);
    } else {
        http_response_code(404);
        echo json_encode(['status' => 'error', 'message' => "Method {$actionName} không tồn tại trong {$controllerName}Controller."]);
    }
} else {
    http_response_code(404);
    echo json_encode(['status' => 'error', 'message' => "Controller {$controllerClass} không tồn tại."]);
}
