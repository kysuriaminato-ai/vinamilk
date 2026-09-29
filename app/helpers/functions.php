<?php
/**
 * =====================================================================
 * VINAMILK HRM - Helper Functions
 * =====================================================================
 * 
 * Các hàm tiện ích dùng chung trong toàn hệ thống:
 * url, asset, redirect, sanitize, csrf, formatting data, flash messaging...
 */

require_once APP_DIR . '/helpers/Auth.php';

/**
 * Tạo URL đường dẫn tuyệt đối trong ứng dụng
 */
if (!function_exists('url')) {
    function url(string $path = ''): string {
        $path = ltrim($path, '/');
        return BASE_URL . ($path ? '/' . $path : '');
    }
}

/**
 * Tạo URL tới thư mục tài nguyên static assets (CSS, JS, images)
 */
if (!function_exists('asset')) {
    function asset(string $path = ''): string {
        $path = ltrim($path, '/');
        return PUBLIC_URL . '/assets/' . $path;
    }
}

/**
 * Chuyển hướng trình duyệt
 */
if (!function_exists('redirect')) {
    function redirect(string $path): void {
        $url = (str_starts_with($path, 'http://') || str_starts_with($path, 'https://')) ? $path : url($path);
        header('Location: ' . $url);
        exit;
    }
}

/**
 * Làm sạch dữ liệu đầu vào chống XSS
 */
if (!function_exists('sanitize')) {
    function sanitize(mixed $data): mixed {
        if (is_array($data)) {
            foreach ($data as $key => $value) {
                $data[$key] = sanitize($value);
            }
            return $data;
        }
        if (is_string($data)) {
            return htmlspecialchars(trim($data), ENT_QUOTES, 'UTF-8');
        }
        return $data;
    }
}

/**
 * Trả về chuỗi HTML chứa input ẩn CSRF token cho Form
 */
if (!function_exists('csrf_field')) {
    function csrf_field(): string {
        $token = Session::getCsrfToken();
        $key = CSRF_TOKEN_KEY;
        return '<input type="hidden" name="' . $key . '" value="' . $token . '">';
    }
}

/**
 * Lấy CSRF token hiện tại
 */
if (!function_exists('csrf_token')) {
    function csrf_token(): string {
        return Session::getCsrfToken();
    }
}

/**
 * Thiết lập hoặc lấy thông báo Flash
 */
if (!function_exists('flash')) {
    function flash(string $name, ?string $message = null, string $type = 'info'): ?array {
        if ($message !== null) {
            Session::setFlash($name, $message, $type);
            return null;
        }
        return Session::getFlash($name);
    }
}

/**
 * Định dạng tiền tệ VNĐ (Đồng Việt Nam)
 */
if (!function_exists('format_money')) {
    function format_money(float|int|string|null $amount): string {
        if ($amount === null || $amount === '') return '0 ₫';
        return number_format((float)$amount, 0, ',', '.') . ' ₫';
    }
}

/**
 * Định dạng ngày tháng theo tiêu chuẩn Việt Nam (dd/mm/YYYY)
 */
if (!function_exists('format_date')) {
    function format_date(?string $dateStr, string $format = 'd/m/Y'): string {
        if (empty($dateStr) || $dateStr === '0000-00-00') return '-';
        $timestamp = strtotime($dateStr);
        return $timestamp ? date($format, $timestamp) : '-';
    }
}

/**
 * Định dạng ngày giờ Việt Nam (dd/mm/YYYY HH:ii)
 */
if (!function_exists('format_datetime')) {
    function format_datetime(?string $dateStr): string {
        return format_date($dateStr, 'd/m/Y H:i');
    }
}

/**
 * Lấy thông tin user hiện tại
 */
if (!function_exists('current_user')) {
    function current_user(): ?array {
        return Auth::user();
    }
}

/**
 * Kiểm tra quyền hạn của user
 */
if (!function_exists('has_permission')) {
    function has_permission(string $permission): bool {
        return Auth::can($permission);
    }
}

/**
 * In dữ liệu debug (Dump and Die)
 */
if (!function_exists('dd')) {
    function dd(...$vars): void {
        echo '<pre style="background: #1e1e1e; color: #00ff66; padding: 15px; border-radius: 8px; font-family: monospace; font-size: 14px; z-index: 99999; position: relative;">';
        foreach ($vars as $var) {
            var_dump($var);
        }
        echo '</pre>';
        exit;
    }
}
