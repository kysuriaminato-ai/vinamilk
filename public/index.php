<?php
/**
 * =====================================================================
 * VINAMILK HRM - Single Front Controller Entry Point
 * =====================================================================
 * 
 * Điểm khởi chạy duy nhất cho toàn bộ hệ thống Vinamilk HRM MVC.
 * Nạp cấu hình, helpers, core libraries, khởi tạo Session và Dispatcher App.
 */

// Đánh dấu hằng số truy cập hệ thống
define('VINAMILK_HRM', true);

// 1. Nạp Cấu hình hệ thống
require_once __DIR__ . '/../app/config/config.php';

// 2. Nạp Helper Functions & Security Engine
require_once APP_DIR . '/helpers/functions.php';
require_once APP_DIR . '/helpers/Auth.php';

// 3. Nạp Bộ khung Core MVC
require_once APP_DIR . '/core/Database.php';
require_once APP_DIR . '/core/Session.php';
require_once APP_DIR . '/core/Model.php';
require_once APP_DIR . '/core/Controller.php';
require_once APP_DIR . '/core/App.php';

// 4. Khởi tạo Session Bảo mật
Session::init();

// 5. Thắp sáng Bộ khung Dispatcher (App Router)
$app = new App();
