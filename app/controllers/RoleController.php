<?php
/**
 * =====================================================================
 * VINAMILK HRM - Multi-role Dynamic Permission Controller
 * =====================================================================
 * 
 * Controller quản lý Vai trò & Ma trận Phân quyền Đa nhiệm Động.
 * Cho phép thiết lập quyền hạn chi tiết (View, Add, Edit, Delete, Approve, Export)
 * cho từng Module theo từng Vai trò hệ thống.
 */

require_once APP_DIR . '/core/Controller.php';

class RoleController extends Controller {

    /**
     * Danh sách Vai trò Hệ thống
     */
    public function index(): void {
        $this->requirePermission('HE_THONG.View');

        /** @var Role $roleModel */
        $roleModel = $this->model('Role');
        $roles = $roleModel->getAllRoles();

        $this->view('roles/index', [
            'title' => 'Quản lý Vai trò System - Vinamilk HRM',
            'roles' => $roles
        ]);
    }

    /**
     * Ma trận Phân quyền Đa nhiệm theo Vai trò (Dynamic Permission Matrix)
     */
    public function permissions(mixed $roleId = null): void {
        $this->requirePermission('HE_THONG.View');

        if (!$roleId) {
            $this->redirect('roles');
        }

        $roleId = (int) $roleId;

        /** @var Role $roleModel */
        $roleModel = $this->model('Role');
        $role = $roleModel->getRoleWithPermissions($roleId);

        if (!$role) {
            Session::setFlash('error', 'Không tìm thấy vai trò yêu cầu.', 'danger');
            $this->redirect('roles');
        }

        // Lấy tất cả quyền hạn được nhóm theo Module (NHANSU, CHAM_CONG, LUONG...)
        $groupedPermissions = $roleModel->getAllPermissionsGrouped();
        $allRoles = $roleModel->getAllRoles();

        $this->view('roles/permissions', [
            'title' => "Phân quyền Vai trò: {$role['RoleName']} - Vinamilk HRM",
            'role' => $role,
            'allRoles' => $allRoles,
            'groupedPermissions' => $groupedPermissions
        ]);
    }

    /**
     * Xử lý Cập nhật Ma trận Phân quyền
     */
    public function updatePermissions(): void {
        $this->requirePermission('HE_THONG.Edit');
        $this->validateCsrf();

        $roleId = (int) ($_POST['role_id'] ?? 0);
        $permissionIds = $_POST['permissions'] ?? [];

        if ($roleId <= 0) {
            Session::setFlash('error', 'Vai trò không hợp lệ.', 'danger');
            $this->redirect('roles');
        }

        /** @var Role $roleModel */
        $roleModel = $this->model('Role');
        $role = $roleModel->find($roleId);

        if (!$role) {
            Session::setFlash('error', 'Vai trò không tồn tại.', 'danger');
            $this->redirect('roles');
        }

        // Thực hiện cập nhật mảng PermissionID mới
        $success = $roleModel->updateRolePermissions($roleId, $permissionIds);

        /** @var AuditLog $auditModel */
        $auditModel = $this->model('AuditLog');

        if ($success) {
            $auditModel->log('UpdatePermissions', 'Success', Auth::id(), Auth::user()['Username'], "Cập nhật phân quyền cho vai trò {$role['RoleName']} (RoleID: {$roleId})");

            // Nếu người dùng hiện tại có RoleID này -> Cập nhật ngay Session của họ
            if ((int)Auth::user()['RoleID'] === $roleId) {
                /** @var User $userModel */
                $userModel = $this->model('User');
                $newPerms = $userModel->getUserPermissions($roleId);
                Session::set(PERM_SESSION_KEY, $newPerms);
            }

            Session::setFlash('success', "Cập nhật Ma trận phân quyền thành công cho vai trò <strong>{$role['RoleName']}</strong>!", 'success');
        } else {
            $auditModel->log('UpdatePermissions', 'Failed', Auth::id(), Auth::user()['Username'], "Thất bại khi cập nhật phân quyền cho vai trò {$role['RoleName']}");
            Session::setFlash('error', 'Có lỗi xảy ra trong quá trình lưu phân quyền.', 'danger');
        }

        $this->redirect("roles/permissions/{$roleId}");
    }
}
