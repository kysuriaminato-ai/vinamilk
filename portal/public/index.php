<?php
// Hỗ trợ PHP Built-in Server định tuyến tĩnh và động
if (php_sapi_name() === 'cli-server') {
    $path = parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);
    if ($path !== '/' && file_exists(__DIR__ . $path) && !is_dir(__DIR__ . $path)) {
        return false;
    }
}

/**
 * =====================================================================
 * VINAMILK ENTERPRISE HRM - ENTRY POINT (FRONT CONTROLLER)
 * =====================================================================
 * Stack: Pure PHP 8.2 MVC + MySQL 8 + Bootstrap 5.3 + Chart.js 4
 */

define('VINAMILK_HRM', true);
define('ROOT_DIR', dirname(__DIR__));

// Nạp cấu hình hệ thống
require_once ROOT_DIR . '/app/config/config.php';

// Nạp các hàm trợ giúp
require_once ROOT_DIR . '/app/helpers/functions.php';

// Nạp các lớp lõi (Core Framework)
require_once ROOT_DIR . '/app/core/Database.php';
require_once ROOT_DIR . '/app/core/Session.php';
require_once ROOT_DIR . '/app/core/App.php';

// Khởi động phiên làm việc bảo mật
Session::start();

// Điều hướng ứng dụng qua Front Controller
$app = new App();
