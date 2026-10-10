<?php
/**
 * =====================================================================
 * VINAMILK HRM - User Model
 * =====================================================================
 * 
 * Model quản lý tài khoản người dùng, xác thực đăng nhập,
 * liên kết hồ sơ nhân viên và truy vấn danh sách quyền hạn.
 */

require_once APP_DIR . '/core/Model.php';

class User extends Model {
    protected string $table = 'users';
    protected string $primaryKey = 'ID';

    /**
     * Tìm tài khoản theo Username (đính kèm thông tin Role và Nhân sự)
     */
    public function getByUsername(string $username): ?array {
        $sql = "SELECT u.*, r.RoleName, r.RoleCode, 
                       ns.HoTen, ns.Email as EmailNV, ns.MaDV, ns.MaCV, ns.AnhChanDung,
                       dv.TenDV, cv.TenCV
                FROM users u
                JOIN roles r ON u.RoleID = r.RoleID
                LEFT JOIN nhansu ns ON u.MaNV = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                WHERE u.Username = :username LIMIT 1";

        $result = $this->query($sql, ['username' => $username])->fetch();
        return $result ?: null;
    }

    /**
     * Xác thực Mật khẩu dùng password_verify (Bcrypt/Argon2)
     */
    public function verifyPassword(string $password, string $hash): bool {
        return password_verify($password, $hash);
    }

    /**
     * Lấy tất cả Mã Quyền hạn (Permissions) thuộc Vai trò của User
     * Kế quả trả về mảng các mã dạng: ['NHANSU.View', 'NHANSU.Add', 'LUONG.Approve', ...]
     */
    public function getUserPermissions(int $roleId): array {
        $sql = "SELECT CONCAT(p.ModuleCode, '.', p.ActionCode) as PermissionCode
                FROM role_permissions rp
                JOIN permissions p ON rp.PermissionID = p.PermissionID
                WHERE rp.RoleID = :roleId";

        $rows = $this->query($sql, ['roleId' => $roleId])->fetchAll();
        return array_column($rows, 'PermissionCode');
    }

    /**
     * Cập nhật thời điểm Đăng nhập gần nhất và reset số lần sai
     */
    public function updateLastLogin(int $userId): bool {
        $sql = "UPDATE users SET LastLogin = NOW(), LoginAttempts = 0 WHERE ID = :id";
        return $this->query($sql, ['id' => $userId])->rowCount() > 0;
    }

    /**
     * Tăng số lần đăng nhập sai
     */
    public function incrementFailedAttempts(int $userId): void {
        $sql = "UPDATE users SET LoginAttempts = LoginAttempts + 1 WHERE ID = :id";
        $this->query($sql, ['id' => $userId]);
    }

    /**
     * Lấy danh sách tất cả tài khoản hệ thống (dùng cho Admin quản lý)
     */
    public function getAllUsersWithDetails(): array {
        $sql = "SELECT u.ID, u.Username, u.MaNV, u.IsActive, u.LastLogin, u.CreatedAt,
                       r.RoleName, r.RoleCode,
                       ns.HoTen, ns.Email as EmailNV, dv.TenDV
                FROM users u
                JOIN roles r ON u.RoleID = r.RoleID
                LEFT JOIN nhansu ns ON u.MaNV = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                ORDER BY u.ID ASC";

        return $this->query($sql)->fetchAll();
    }

    /**
     * Đổi mật khẩu tài khoản
     */
    public function changePassword(int $userId, string $newPassword): bool {
        $hash = password_hash($newPassword, PASSWORD_BCRYPT);
        return $this->update($userId, ['Password' => $hash]);
    }
}
