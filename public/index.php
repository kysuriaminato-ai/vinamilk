<?php

/**
 * VINAMILK HRIS - Front Controller (Core Router)
 */

// 1. Tự động nạp Class (Native Autoloader thay thế Composer)
spl_autoload_register(function ($class) {
    $base_dir = __DIR__ . '/../app/';
    
    // Xử lý Class có Namespace chuẩn (App\...)
    $prefix = 'App\\';
    $len = strlen($prefix);
    if (strncmp($prefix, $class, $len) === 0) {
        $relative_class = substr($class, $len);
        $file = $base_dir . str_replace('\\', '/', $relative_class) . '.php';
        if (file_exists($file)) require_once $file;
        return;
    }
    
    // Xử lý Legacy Class không có Namespace (Ví dụ: DashboardController)
    $legacy_paths = [
        $base_dir . 'controllers/' . $class . '.php',
        $base_dir . 'models/' . $class . '.php',
        $base_dir . 'core/' . $class . '.php'
    ];
    foreach ($legacy_paths as $file) {
        if (file_exists($file)) {
            require_once $file;
            return;
        }
    }
});

// 2. Load cấu hình hệ thống (config.php chứa APP_DIR)
$configFile = dirname(__DIR__) . '/app/config/config.php';
if (file_exists($configFile)) {
    require_once $configFile;
}

// Load các hàm tiện ích toàn cục (functions.php)
$functionsFile = dirname(__DIR__) . '/app/helpers/functions.php';
if (file_exists($functionsFile)) {
    require_once $functionsFile;
}

// 3. Load biến môi trường từ file .env (Không cần thư viện Dotenv)
$envPath = dirname(__DIR__) . '/.env';
if (file_exists($envPath)) {
    $lines = file($envPath, FILE_IGNORE_NEW_LINES | FILE_SKIP_EMPTY_LINES);
    foreach ($lines as $line) {
        if (strpos(trim($line), '#') === 0) continue;
        list($name, $value) = explode('=', $line, 2);
        $name = trim($name);
        $value = trim($value);
        if (!array_key_exists($name, $_SERVER) && !array_key_exists($name, $_ENV)) {
            putenv(sprintf('%s=%s', $name, $value));
            $_ENV[$name] = $value;
            $_SERVER[$name] = $value;
        }
    }
}

// 3. Khởi chạy Ứng dụng thông qua Core App (Router + Middleware)
use App\Core\App;

// Mọi request sẽ được phân tích, đưa qua Middleware, và điều hướng bởi class App
$app = new App();
