<?php
/**
 * =====================================================================
 * VINAMILK ENTERPRISE HRM - CẤU HÌNH HỆ THỐNG
 * =====================================================================
 * Stack: PHP 8.2 MVC Thuần + MySQL 8 + Bootstrap 5.3 + Chart.js 4
 */

if (!defined('VINAMILK_HRM')) {
    define('VINAMILK_HRM', true);
}

// Cấu hình Múi giờ & Bộ mã UTF-8
date_default_timezone_set('Asia/Ho_Chi_Minh');
ini_set('default_charset', 'UTF-8');

// Chế độ môi trường
define('APP_ENV', 'development');
if (APP_ENV === 'development') {
    error_reporting(E_ALL);
    ini_set('display_errors', '1');
} else {
    error_reporting(0);
    ini_set('display_errors', '0');
}

// Thông tin định danh Ứng dụng
define('APP_NAME', 'Vinamilk HRM');
define('APP_FULL_NAME', 'Hệ thống Quản lý Nguồn nhân lực - Công ty CP Sữa Việt Nam');
define('APP_VERSION', '3.0.0-PurePHP82');
define('COMPANY_NAME', 'Công ty Cổ phần Sữa Việt Nam (Vinamilk)');
define('COMPANY_COLOR_PRIMARY', '#0033A0'); // Xanh dương đậm nhận diện Vinamilk

// Đường dẫn Thư mục Gốc
define('DS', DIRECTORY_SEPARATOR);
if (!defined('ROOT_DIR')) {
    define('ROOT_DIR', dirname(dirname(__DIR__)));
}
define('APP_DIR', ROOT_DIR . DS . 'app');
define('PUBLIC_DIR', ROOT_DIR . DS . 'public');

// Tính toán BASE_URL linh hoạt
$protocol = (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off') || (isset($_SERVER['SERVER_PORT']) && $_SERVER['SERVER_PORT'] == 443) ? 'https' : 'http';
$host = $_SERVER['HTTP_HOST'] ?? 'localhost';

if (php_sapi_name() === 'cli-server') {
    $baseUrl = $protocol . '://' . $host;
} else {
    $scriptName = $_SERVER['SCRIPT_NAME'] ?? '';
    $scriptDir = str_replace('\\', '/', dirname($scriptName));
    $baseUrl = $protocol . '://' . $host . rtrim($scriptDir, '/');

    if (substr($baseUrl, -7) === '/public') {
        $baseUrl = substr($baseUrl, 0, -7);
    }
}

define('BASE_URL', $baseUrl);
define('PUBLIC_URL', BASE_URL . '/public');
define('ASSETS_URL', PUBLIC_URL . '/assets');

// Cấu hình Kết nối CSDL MySQL 8 (Hỗ trợ Environment Variables cho CI/CD & Production)
define('DB_HOST', getenv('DB_HOST') ?: '127.0.0.1');
define('DB_PORT', getenv('DB_PORT') ?: '3306');
define('DB_NAME', getenv('DB_NAME') ?: (getenv('DB_DATABASE') ?: 'vinamilk_hrm'));
define('DB_USER', getenv('DB_USER') ?: (getenv('DB_USERNAME') ?: 'root'));
define('DB_PASS', (getenv('DB_PASS') !== false && getenv('DB_PASS') !== null) ? getenv('DB_PASS') : ((getenv('DB_PASSWORD') !== false && getenv('DB_PASSWORD') !== null) ? getenv('DB_PASSWORD') : ''));
define('DB_CHARSET', 'utf8mb4');

// Cấu hình Session & Bảo mật
define('SESSION_NAME', 'VNM_HRM_SESSID');
define('SESSION_LIFETIME', 86400); // 24 giờ
define('CSRF_TOKEN_KEY', 'vnm_csrf_token');
define('AUTH_SESSION_KEY', 'vnm_logged_user');
define('PERM_SESSION_KEY', 'vnm_user_permissions');
define('DEFAULT_PASSWORD', 'Vinamilk@2026');
