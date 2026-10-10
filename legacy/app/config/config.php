<?php
/**
 * =====================================================================
 * VINAMILK HRM - System Configuration
 * =====================================================================
 * 
 * File cấu hình tổng thể cho ứng dụng Vinamilk HRM MVC.
 * Quản lý kết nối CSDL, URL gốc, Cấu hình Session & Security.
 */

// Ngăn chặn truy cập trực tiếp
if (!defined('VINAMILK_HRM')) {
    define('VINAMILK_HRM', true);
}

// Cấu hình Múi giờ & Font mã hóa
date_default_timezone_set('Asia/Ho_Chi_Minh');
ini_set('default_charset', 'UTF-8');

// Environment (development / production)
define('APP_ENV', 'development'); // Đổi thành 'production' khi triển khai thật

if (APP_ENV === 'development') {
    error_reporting(E_ALL);
    ini_set('display_errors', 1);
} else {
    error_reporting(0);
    ini_set('display_errors', 0);
}

// Cấu hình Thông tin Ứng dụng
define('APP_NAME', 'Vinamilk HRM');
define('APP_FULL_NAME', 'Hệ thống Quản lý Nguồn nhân lực - Công ty CP Sữa Việt Nam');
define('APP_VERSION', '2.0.0');
define('COMPANY_NAME', 'Công ty Cổ phần Sữa Việt Nam (Vinamilk)');
define('COMPANY_STOCK_CODE', 'VNM');

// Đường dẫn Thư mục Gốc
define('DS', DIRECTORY_SEPARATOR);
define('ROOT_DIR', dirname(dirname(__DIR__)));
define('APP_DIR', ROOT_DIR . DS . 'app');
define('PUBLIC_DIR', ROOT_DIR . DS . 'public');

// Cấu hình BASE_URL (Tự động phát hiện)
$protocol = (isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] === 'on') ? 'https' : 'http';
$host = $_SERVER['HTTP_HOST'] ?? 'localhost';
$scriptName = $_SERVER['SCRIPT_NAME'] ?? '';
$docRoot = $_SERVER['DOCUMENT_ROOT'] ?? '';

// Tính toán sub-folder nếu dự án nằm trong thư mục con
$scriptDir = str_replace('\\', '/', dirname($scriptName));
$baseUrl = $protocol . '://' . $host . rtrim($scriptDir, '/');

// Loại bỏ '/public' khỏi BASE_URL nếu đang chạy qua Apache Rewrite hoặc VirtualHost
if (substr($baseUrl, -7) === '/public') {
    $baseUrl = substr($baseUrl, 0, -7);
}

define('BASE_URL', $baseUrl);
define('PUBLIC_URL', BASE_URL . '/public');
define('ASSETS_URL', BASE_URL . '/public/assets');
define('UPLOADS_URL', BASE_URL . '/public/uploads');

// Cấu hình CSDL MySQL (dành cho phpMyAdmin / Localhost)
define('DB_HOST', '127.0.0.1');
define('DB_PORT', '3306');
define('DB_NAME', 'vinamilk_hrm');
define('DB_USER', 'root');
define('DB_PASS', '');
define('DB_CHARSET', 'utf8mb4');

// Cấu hình Session & Bảo mật
define('SESSION_NAME', 'VNM_HRM_SESSID');
define('SESSION_LIFETIME', 86400); // 24 giờ
define('CSRF_TOKEN_KEY', 'vnm_csrf_token');
define('AUTH_SESSION_KEY', 'vnm_logged_user');
define('PERM_SESSION_KEY', 'vnm_user_permissions');

// Mật khẩu mặc định hệ thống
define('DEFAULT_PASSWORD', 'Vinamilk@2026');
