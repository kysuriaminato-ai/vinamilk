<?php
/**
 * =====================================================================
 * VINAMILK HRM - Role & Permission Model
 * =====================================================================
 * 
 * Model quản lý Vai trò (Roles), Quyền hạn (Permissions) và 
 * Ma trận Phân quyền Đa nhiệm Dynamic Permission.
 */

require_once APP_DIR . '/core/Model.php';

class Role extends Model {
    protected string $table = 'roles';
    protected string $primaryKey = 'RoleID';

    /**
     * Lấy danh sách tất cả Vai trò
     */
    public function getAllRoles(): array {
        $sql = "SELECT r.*, COUNT(u.ID) as TotalUsers
                FROM roles r
                LEFT JOIN users u ON r.RoleID = u.RoleID
                GROUP BY r.RoleID
                ORDER BY r.RoleID ASC";
        return $this->query($sql)->fetchAll();
    }

    /**
     * Lấy thông tin Vai trò theo ID đính kèm danh sách PermissionID đã gán
     */
    public function getRoleWithPermissions(int $roleId): ?array {
        $role = $this->find($roleId);
        if (!$role) return null;

        $sql = "SELECT PermissionID FROM role_permissions WHERE RoleID = :roleId";
        $assignedRows = $this->query($sql, ['roleId' => $roleId])->fetchAll();
        $role['AssignedPermissionIDs'] = array_column($assignedRows, 'PermissionID');

        return $role;
    }

    /**
     * Lấy tất cả Quyền hạn được gộp nhóm theo ModuleCode (Ví dụ: NHANSU, LUONG, TUYEN_DUNG...)
     */
    public function getAllPermissionsGrouped(): array {
        $sql = "SELECT * FROM permissions ORDER BY ModuleCode ASC, PermissionID ASC";
        $permissions = $this->query($sql)->fetchAll();

        $grouped = [];
        foreach ($permissions as $perm) {
            $moduleCode = $perm['ModuleCode'];
            if (!isset($grouped[$moduleCode])) {
                $grouped[$moduleCode] = [
                    'ModuleName' => $perm['ModuleName'],
                    'Items' => []
                ];
            }
            $grouped[$moduleCode]['Items'][] = $perm;
        }

        return $grouped;
    }

    /**
     * Cập nhật Ma trận Phân quyền cho 1 Vai trò trong Transaction
     */
    public function updateRolePermissions(int $roleId, array $permissionIds): bool {
        try {
            $this->beginTransaction();

            // 1. Xóa các quyền cũ của role này
            $sqlDelete = "DELETE FROM role_permissions WHERE RoleID = :roleId";
            $this->query($sqlDelete, ['roleId' => $roleId]);

            // 2. Chèn mảng quyền mới
            if (!empty($permissionIds)) {
                $sqlInsert = "INSERT INTO role_permissions (RoleID, PermissionID) VALUES (:roleId, :permId)";
                foreach ($permissionIds as $permId) {
                    $this->query($sqlInsert, [
                        'roleId' => $roleId,
                        'permId' => (int) $permId
                    ]);
                }
            }

            $this->commit();
            return true;
        } catch (Exception $e) {
            $this->rollBack();
            error_log("Error updating role permissions: " . $e->getMessage());
            return false;
        }
    }
}
