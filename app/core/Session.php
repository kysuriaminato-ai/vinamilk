<?php
/**
 * =====================================================================
 * VINAMILK HRM - QUẢN LÝ PHIÊN LÀM VIỆC & CSRF (SESSION CORE)
 * =====================================================================
 */

class Session
{
    private static bool $started = false;

    /**
     * Khởi tạo Session an toàn
     */
    public static function start(): void
    {
        if (self::$started || session_status() === PHP_SESSION_ACTIVE) {
            self::$started = true;
            return;
        }

        if (php_sapi_name() === 'cli') {
            if (!isset($_SESSION)) {
                $_SESSION = [];
            }
            self::$started = true;
            if (empty($_SESSION[defined('CSRF_TOKEN_KEY') ? CSRF_TOKEN_KEY : 'vnm_csrf_token'])) {
                self::generateCsrfToken();
            }
            return;
        }

        $sessionName = defined('SESSION_NAME') ? SESSION_NAME : 'VNM_HRM_SESSID';
        $lifetime = defined('SESSION_LIFETIME') ? SESSION_LIFETIME : 86400;

        if (!headers_sent()) {
            ini_set('session.name', $sessionName);
            ini_set('session.cookie_lifetime', (string)$lifetime);
            ini_set('session.gc_maxlifetime', (string)$lifetime);
            ini_set('session.cookie_httponly', '1');
            ini_set('session.use_only_cookies', '1');
            ini_set('session.cookie_samesite', 'Lax');

            if (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off') {
                ini_set('session.cookie_secure', '1');
            }

            session_start();
        }
        self::$started = true;

        // Tự động khởi tạo token CSRF nếu chưa có
        if (empty($_SESSION[defined('CSRF_TOKEN_KEY') ? CSRF_TOKEN_KEY : 'vnm_csrf_token'])) {
            self::generateCsrfToken();
        }
    }

    /**
     * Lấy giá trị từ Session
     */
    public static function get(string $key, mixed $default = null): mixed
    {
        self::start();
        return $_SESSION[$key] ?? $default;
    }

    /**
     * Gán giá trị vào Session
     */
    public static function set(string $key, mixed $value): void
    {
        self::start();
        $_SESSION[$key] = $value;
    }

    /**
     * Kiểm tra khóa có tồn tại trong Session
     */
    public static function has(string $key): bool
    {
        self::start();
        return isset($_SESSION[$key]);
    }

    /**
     * Xóa một khóa khỏi Session
     */
    public static function remove(string $key): void
    {
        self::start();
        unset($_SESSION[$key]);
    }

    /**
     * Tạo mới CSRF Token ngẫu nhiên (32 bytes = 64 ký tự hex)
     */
    public static function generateCsrfToken(): string
    {
        self::start();
        $token = bin2hex(random_bytes(32));
        $key = defined('CSRF_TOKEN_KEY') ? CSRF_TOKEN_KEY : 'vnm_csrf_token';
        $_SESSION[$key] = $token;
        return $token;
    }

    /**
     * Lấy mã CSRF Token hiện tại
     */
    public static function getCsrfToken(): string
    {
        self::start();
        $key = defined('CSRF_TOKEN_KEY') ? CSRF_TOKEN_KEY : 'vnm_csrf_token';
        if (empty($_SESSION[$key])) {
            return self::generateCsrfToken();
        }
        return $_SESSION[$key];
    }

    /**
     * Xác thực CSRF Token (Chống tấn công CSRF cho mọi form POST)
     */
    public static function verifyCsrfToken(?string $token = null): bool
    {
        self::start();
        $key = defined('CSRF_TOKEN_KEY') ? CSRF_TOKEN_KEY : 'vnm_csrf_token';
        $serverToken = $_SESSION[$key] ?? '';

        if ($token === null) {
            $token = $_POST[$key] ?? $_SERVER['HTTP_X_CSRF_TOKEN'] ?? '';
        }

        if (empty($token) || empty($serverToken)) {
            return false;
        }

        return hash_equals($serverToken, (string)$token);
    }

    /**
     * Thiết lập thông báo Flash (chỉ tồn tại 1 lần request tiếp theo)
     */
    public static function setFlash(string $key, string $message, string $type = 'info'): void
    {
        self::start();
        $_SESSION['_flash'][$key] = [
            'message' => $message,
            'type'    => $type
        ];
    }

    /**
     * Lấy thông báo Flash và tự động xóa sau khi lấy
     */
    public static function getFlash(string $key): ?array
    {
        self::start();
        if (isset($_SESSION['_flash'][$key])) {
            $data = $_SESSION['_flash'][$key];
            unset($_SESSION['_flash'][$key]);
            return $data;
        }
        return null;
    }

    /**
     * Tái tạo session ID chống Session Fixation
     */
    public static function regenerate(): void
    {
        self::start();
        if (session_status() === PHP_SESSION_ACTIVE && !headers_sent()) {
            session_regenerate_id(true);
        }
    }

    /**
     * Hủy bỏ toàn bộ session
     */
    public static function destroy(): void
    {
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
}
