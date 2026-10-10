<?php
/**
 * =====================================================================
 * VINAMILK HRM - Secure Session Manager
 * =====================================================================
 * 
 * Lớp quản lý Session an toàn, chống tấn công Session Fixation,
 * xử lý Flash message và phát sinh/xác thực CSRF Token.
 */

class Session {
    private static bool $started = false;

    /**
     * Khởi tạo Session an toàn với cấu hình cookie bảo mật
     */
    public static function init(): void {
        if (self::$started || session_status() === PHP_SESSION_ACTIVE) {
            self::$started = true;
            return;
        }

        // Cấu hình cookie session an toàn
        if (!headers_sent()) {
            ini_set('session.use_only_cookies', '1');
            ini_set('session.use_strict_mode', '1');

            session_name(defined('SESSION_NAME') ? SESSION_NAME : 'VNM_HRM_SESSID');

            $cookieParams = [
                'lifetime' => defined('SESSION_LIFETIME') ? SESSION_LIFETIME : 86400,
                'path' => '/',
                'domain' => '',
                'secure' => isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] === 'on',
                'httponly' => true,
                'samesite' => 'Lax'
            ];

            session_set_cookie_params($cookieParams);
        }

        session_start();
        self::$started = true;

        // Tự động khởi tạo CSRF Token nếu chưa có
        if (empty($_SESSION[CSRF_TOKEN_KEY])) {
            self::generateCsrfToken();
        }
    }

    /**
     * Gán giá trị vào Session
     */
    public static function set(string $key, mixed $value): void {
        self::init();
        $_SESSION[$key] = $value;
    }

    /**
     * Lấy giá trị từ Session
     */
    public static function get(string $key, mixed $default = null): mixed {
        self::init();
        return $_SESSION[$key] ?? $default;
    }

    /**
     * Kiểm tra xem key có tồn tại trong Session không
     */
    public static function has(string $key): bool {
        self::init();
        return isset($_SESSION[$key]);
    }

    /**
     * Xóa 1 key trong Session
     */
    public static function remove(string $key): void {
        self::init();
        if (isset($_SESSION[$key])) {
            unset($_SESSION[$key]);
        }
    }

    /**
     * Đổi Session ID mới (chống Session Fixation)
     */
    public static function regenerate(): void {
        self::init();
        session_regenerate_id(true);
    }

    /**
     * Xóa toàn bộ Session & đăng xuất
     */
    public static function destroy(): void {
        if (session_status() === PHP_SESSION_ACTIVE) {
            $_SESSION = [];
            if (ini_get("session.use_cookies")) {
                $params = session_get_cookie_params();
                setcookie(
                    session_name(),
                    '',
                    time() - 42000,
                    $params["path"],
                    $params["domain"],
                    $params["secure"],
                    $params["httponly"]
                );
            }
            session_destroy();
            self::$started = false;
        }
    }

    /**
     * Tạo thông báo Flash (chỉ tồn tại trong 1 lượt chuyển trang)
     */
    public static function setFlash(string $name, string $message, string $type = 'info'): void {
        self::init();
        $_SESSION['_flash'][$name] = [
            'message' => $message,
            'type' => $type // success, danger, warning, info
        ];
    }

    /**
     * Kiểm tra có Flash message không
     */
    public static function hasFlash(string $name): bool {
        self::init();
        return isset($_SESSION['_flash'][$name]);
    }

    /**
     * Lấy Flash message và tự động xóa sau khi đọc
     */
    public static function getFlash(string $name): ?array {
        self::init();
        if (isset($_SESSION['_flash'][$name])) {
            $flash = $_SESSION['_flash'][$name];
            unset($_SESSION['_flash'][$name]);
            return $flash;
        }
        return null;
    }

    /**
     * Phát sinh ngẫu nhiên 64-character Hex CSRF Token
     */
    public static function generateCsrfToken(): string {
        self::init();
        $token = bin2hex(random_bytes(32));
        $_SESSION[CSRF_TOKEN_KEY] = $token;
        return $token;
    }

    /**
     * Lấy CSRF Token hiện tại
     */
    public static function getCsrfToken(): string {
        self::init();
        if (empty($_SESSION[CSRF_TOKEN_KEY])) {
            return self::generateCsrfToken();
        }
        return $_SESSION[CSRF_TOKEN_KEY];
    }

    /**
     * Xác thực CSRF Token từ Form Request hoặc Header
     */
    public static function verifyCsrfToken(?string $token = null): bool {
        self::init();
        if ($token === null) {
            $token = $_POST[CSRF_TOKEN_KEY] ?? $_SERVER['HTTP_X_CSRF_TOKEN'] ?? null;
        }

        $sessionToken = $_SESSION[CSRF_TOKEN_KEY] ?? '';
        if (empty($sessionToken) || empty($token)) {
            return false;
        }

        return hash_equals($sessionToken, $token);
    }
}
