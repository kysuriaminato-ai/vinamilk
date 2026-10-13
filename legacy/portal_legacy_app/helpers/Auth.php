<?php
/**
 * =====================================================================
 * VINAMILK HRM - HỆ THỐNG XÁC THỰC & PHÂN QUYỀN (AUTH HELPER)
 * =====================================================================
 * Hỗ trợ:
 *   1. 7 Vai trò chuẩn Vinamilk: ADMIN, BOD, HR_TAPDOAN, HR_DONVI, KETOAN_LUONG, QUANLY_TRUCTIEP, NHANVIEN
 *   2. Phân quyền chi tiết dạng module.hành_động (nhansu.view, nhansu.sensitive, luong.approve...)
 *   3. Kiểm tra cờ bắt buộc đổi mật khẩu lần đầu (MustChangePassword)
 *   4. Quản lý timeout 30 phút và tái tạo Session an toàn
 */

require_once __DIR__ . '/../core/Session.php';
require_once __DIR__ . '/../core/Database.php';

class Auth
{
    private static ?array $cachedPermissions = null;

    /**
     * Lấy thông tin người dùng đang đăng nhập từ Session
     */
    public static function user(): ?array
    {
        return Session::get(defined('AUTH_SESSION_KEY') ? AUTH_SESSION_KEY : 'vnm_logged_user');
    }

    /**
     * Lấy ID người dùng
     */
    public static function id(): ?int
    {
        $u = self::user();
        return isset($u['ID']) ? (int)$u['ID'] : null;
    }

    /**
     * Lấy Mã nhân viên của người dùng
     */
    public static function maNV(): ?string
    {
        $u = self::user();
        return $u['MaNV'] ?? null;
    }

    /**
     * Lấy Mã đơn vị phân công của người dùng
     */
    public static function maDV(): ?string
    {
        $u = self::user();
        return $u['MaDV'] ?? null;
    }

    /**
     * Lấy Mã vai trò của người dùng (ADMIN, HR_TAPDOAN, HR_DONVI, ...)
     */
    public static function role(): string
    {
        $u = self::user();
        return strtoupper($u['RoleCode'] ?? '');
    }

    /**
     * Lấy Tên đăng nhập của người dùng
     */
    public static function username(): ?string
    {
        $u = self::user();
        return $u['Username'] ?? ($u['FullName'] ?? null);
    }

    /**
     * Kiểm tra trạng thái đã đăng nhập
     */
    public static function check(): bool
    {
        return self::user() !== null;
    }

    /**
     * Kiểm tra người dùng có bắt buộc phải đổi mật khẩu lần đầu hay không
     */
    public static function mustChangePassword(): bool
    {
        $u = self::user();
        return !empty($u['MustChangePassword']);
    }

    /**
     * Kiểm tra người dùng có thuộc một trong các vai trò được cấp phép hay không
     */
    public static function hasRole(array|string $roles): bool
    {
        if (!self::check()) {
            return false;
        }

        $userRole = self::role();
        // ADMIN luôn có toàn quyền
        if ($userRole === 'ADMIN' || $userRole === 'ADMINISTRATOR') {
            return true;
        }

        $allowed = is_array($roles) ? $roles : [$roles];
        $normalizedAllowed = array_map(function($r) {
            $r = strtoupper($r);
            if ($r === 'HR_MANAGER') return 'HR_TAPDOAN';
            if ($r === 'MANAGER') return 'QUANLY_TRUCTIEP';
            if ($r === 'EMPLOYEE') return 'NHANVIEN';
            return $r;
        }, $allowed);

        return in_array($userRole, $normalizedAllowed, true);
    }

    /**
     * LẤY TOÀN BỘ DANH SÁCH MÃ QUYỀN HẠN (module.action) CỦA NGƯỜI DÙNG HIỆN TẠI
     * @return array Mảng các permission code dạng ['nhansu.view', 'nhansu.sensitive', ...]
     */
    public static function permissions(): array
    {
        if (!self::check()) {
            return [];
        }

        $userId = self::id();
        $cachedUserId = Session::get('vnm_perms_user_id');

        // Kiểm tra cache trong Session và bộ nhớ nếu cùng user
        if ($cachedUserId === $userId && self::$cachedPermissions !== null) {
            return self::$cachedPermissions;
        }

        $sessionPerms = Session::get('vnm_user_permissions');
        if (is_array($sessionPerms) && !empty($sessionPerms)) {
            self::$cachedPermissions = $sessionPerms;
            return $sessionPerms;
        }

        $roleId = (int)(self::user()['RoleID'] ?? 0);
        $userRole = self::role();

        // ADMIN sở hữu toàn bộ quyền
        if ($userRole === 'ADMIN') {
            $db = Database::getInstance()->getConnection();
            $perms = $db->query("SELECT PermissionCode FROM permissions WHERE PermissionCode IS NOT NULL")->fetchAll(PDO::FETCH_COLUMN);
            $perms[] = '*';
            Session::set('vnm_user_permissions', $perms);
            Session::set('vnm_perms_user_id', $userId);
            self::$cachedPermissions = $perms;
            return $perms;
        }

        try {
            $db = Database::getInstance()->getConnection();
            $sql = "SELECT DISTINCT p.PermissionCode
                    FROM role_permissions rp
                    JOIN permissions p ON rp.PermissionID = p.PermissionID
                    WHERE rp.RoleID = :role_id AND p.PermissionCode IS NOT NULL";
            $stmt = $db->prepare($sql);
            $stmt->execute([':role_id' => $roleId]);
            $perms = $stmt->fetchAll(PDO::FETCH_COLUMN);
        } catch (Throwable $e) {
            error_log('[Auth Permissions Error] ' . $e->getMessage());
            $perms = [];
        }

        Session::set('vnm_user_permissions', $perms);
        Session::set('vnm_perms_user_id', $userId);
        self::$cachedPermissions = $perms;
        return $perms;
    }

    /**
     * KIỂM TRA QUYỀN HẠN DẠNG module.action (VD: 'nhansu.view', 'luong.approve', 'nhansu.sensitive')
     */
    public static function can(string $permissionCode): bool
    {
        if (!self::check()) {
            return false;
        }

        // ADMIN luôn có mọi quyền
        if (self::role() === 'ADMIN') {
            return true;
        }

        $userPerms = self::permissions();
        if (in_array('*', $userPerms, true)) {
            return true;
        }

        // Hỗ trợ kiểm tra quyền chính xác
        if (in_array(strtolower($permissionCode), array_map('strtolower', $userPerms), true)) {
            return true;
        }

        // Hỗ trợ kiểm tra quyền wildcard theo module (ví dụ 'nhansu.*')
        $parts = explode('.', $permissionCode);
        if (count($parts) === 2) {
            $wildcard = strtolower($parts[0]) . '.*';
            if (in_array($wildcard, array_map('strtolower', $userPerms), true)) {
                return true;
            }
        }

        return false;
    }

    /**
     * KIỂM TRA QUYỀN XEM DỮ LIỆU NHẠY CẢM (*.sensitive: CCCD, Số TK ngân hàng, Mức lương)
     */
    public static function canViewSensitive(string $module = 'nhansu'): bool
    {
        if (!self::check()) {
            return false;
        }

        if (self::role() === 'ADMIN') {
            return true;
        }

        return self::can($module . '.sensitive') || self::can('*.sensitive');
    }

    /**
     * Kiểm tra xem vai trò hiện tại có được xem toàn bộ cây đơn vị hay không
     */
    public static function canViewAllUnits(): bool
    {
        $r = self::role();
        return in_array($r, ['ADMIN', 'BOD', 'HR_TAPDOAN', 'KETOAN_LUONG', 'HR_MANAGER']);
    }

    /**
     * Kiểm tra xem vai trò có phải chỉ xem (Read-only như BOD)
     */
    public static function isReadOnly(): bool
    {
        return self::role() === 'BOD';
    }

    /**
     * ĐĂNG NHẬP NGƯỜI DÙNG VÀO PHIÊN LÀM VIỆC (Có session_regenerate_id & timeout 30 phút)
     */
    public static function login(array $user): void
    {
        // 1. Tái tạo Session ID chống Session Fixation
        Session::regenerate();

        // 2. Lưu trữ thông tin người dùng vào Session
        Session::set(defined('AUTH_SESSION_KEY') ? AUTH_SESSION_KEY : 'vnm_logged_user', $user);

        // 3. Khởi tạo mốc thời gian hoạt động cuối cùng (Last Activity cho timeout 30 phút)
        Session::set('vnm_last_activity', time());

        // 4. Xóa cache quyền cũ
        Session::remove('vnm_user_permissions');
        self::$cachedPermissions = null;
    }

    /**
     * ĐĂNG XUẤT NGƯỜI DÙNG
     */
    public static function logout(): void
    {
        Session::remove(defined('AUTH_SESSION_KEY') ? AUTH_SESSION_KEY : 'vnm_logged_user');
        Session::remove('vnm_last_activity');
        Session::remove('vnm_user_permissions');
        self::$cachedPermissions = null;
        Session::destroy();
    }
}
