<?php
/**
 * =====================================================================
 * VINAMILK HRM - Security & Auth Engine Helper
 * =====================================================================
 * 
 * Lớp Auth tĩnh (Static Helper) cung cấp phương thức tiện ích xác thực,
 * kiểm tra trạng thái đăng nhập, thông tin người dùng hiện tại và 
 * phân quyền đa nhiệm (Multi-role Dynamic Permission).
 */

require_once APP_DIR . '/core/Session.php';

class Auth {
    /**
     * Lấy thông tin User đang đăng nhập từ Session
     */
    public static function user(): ?array {
        return Session::get(AUTH_SESSION_KEY, null);
    }

    /**
     * Lấy ID tài khoản hiện tại
     */
    public static function id(): ?int {
        $user = self::user();
        return $user['ID'] ?? null;
    }

    /**
     * Lấy Mã Nhân viên liên kết (MaNV)
     */
    public static function maNV(): ?string {
        $user = self::user();
        return $user['MaNV'] ?? null;
    }

    /**
     * Lấy Tên hiển thị của User
     */
    public static function name(): string {
        $user = self::user();
        if (!$user) return 'Khách';
        return !empty($user['HoTen']) ? $user['HoTen'] : $user['Username'];
    }

    /**
     * Lấy Mã Vai trò hiện tại (RoleCode: Admin, HR_Manager, Manager, Employee)
     */
    public static function roleCode(): ?string {
        $user = self::user();
        return $user['RoleCode'] ?? null;
    }

    /**
     * Kiểm tra người dùng đã đăng nhập chưa
     */
    public static function check(): bool {
        return self::user() !== null;
    }

    /**
     * Kiểm tra user có vai trò cụ thể không
     */
    public static function hasRole(string|array $roles): bool {
        if (!self::check()) return false;
        $userRole = self::roleCode();

        if (is_array($roles)) {
            return in_array($userRole, $roles, true);
        }

        return $userRole === $roles;
    }

    /**
     * Kiểm tra user có phải Admin tối cao không
     */
    public static function isAdmin(): bool {
        return self::hasRole('Admin');
    }

    /**
     * Lấy danh sách Quyền hạn (Permissions) từ Session
     */
    public static function permissions(): array {
        return Session::get(PERM_SESSION_KEY, []);
    }

    /**
     * KIỂM TRA QUYỀN ĐỘNG (Dynamic Permission Check)
     * Ví dụ: Auth::can('NHANSU.View'), Auth::can('LUONG.Approve')
     */
    public static function can(string $permission): bool {
        if (!self::check()) {
            return false;
        }

        // Admin luôn có toàn quyền
        if (self::isAdmin()) {
            return true;
        }

        $userPerms = self::permissions();
        return in_array($permission, $userPerms, true);
    }

    /**
     * Đăng nhập thành công -> Lưu thông tin User & Permissions vào Session
     */
    public static function login(array $userData, array $permissions = []): void {
        Session::regenerate();
        Session::set(AUTH_SESSION_KEY, $userData);
        Session::set(PERM_SESSION_KEY, $permissions);
    }

    /**
     * Đăng xuất -> Xóa sạch Session
     */
    public static function logout(): void {
        Session::destroy();
    }
}
