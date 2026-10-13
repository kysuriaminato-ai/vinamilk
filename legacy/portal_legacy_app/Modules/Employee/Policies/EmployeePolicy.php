<?php

namespace App\Modules\Employee\Policies;

use App\Models\User;
use App\Modules\Employee\Models\Employee;
use Illuminate\Auth\Access\HandlesAuthorization;

class EmployeePolicy
{
    use HandlesAuthorization;

    /**
     * Bỏ qua kiểm tra đối với Super Admin (Ban TCCB Tập đoàn).
     */
    public function before(User $user, string $ability): ?bool
    {
        if ($user->hasRole('SUPER_ADMIN')) {
            return true;
        }

        return null;
    }

    /**
     * Quyền xem danh sách nhân sự: Kiểm tra quyền module.
     */
    public function viewAny(User $user): bool
    {
        return $user->can('EMPLOYEE_VIEW');
    }

    /**
     * Quyền xem chi tiết hồ sơ: Cho phép xem chính mình (ESS) hoặc nhân sự trong cây con phụ trách.
     */
    public function view(User $user, Employee $employee): bool
    {
        // Nhân viên được phép xem hồ sơ của chính mình
        if ($user->employee_id && $user->employee_id === $employee->id) {
            return true;
        }

        if (!$user->can('EMPLOYEE_VIEW')) {
            return false;
        }

        return $this->isEmployeeInUserScope($user, $employee);
    }

    /**
     * Quyền tạo mới nhân sự: Thuộc HR Local hoặc Ban TCCB.
     */
    public function create(User $user): bool
    {
        return $user->can('EMPLOYEE_CREATE');
    }

    /**
     * Quyền chỉnh sửa hồ sơ nhân sự: Cán bộ HR có thẩm quyền trên nhánh cây.
     */
    public function update(User $user, Employee $employee): bool
    {
        if (!$user->can('EMPLOYEE_EDIT')) {
            return false;
        }

        return $this->isEmployeeInUserScope($user, $employee);
    }

    /**
     * Quyền xóa (Soft Delete) hồ sơ nhân viên.
     */
    public function delete(User $user, Employee $employee): bool
    {
        if (!$user->can('EMPLOYEE_DELETE')) {
            return false;
        }

        // Không bao giờ được phép tự xóa tài khoản của chính mình
        if ($user->employee_id === $employee->id) {
            return false;
        }

        return $this->isEmployeeInUserScope($user, $employee);
    }

    /**
     * QUY TẮC SOD: Đề xuất điều chỉnh lương 3P / Bổ nhiệm.
     * TUYỆT ĐỐI KHÔNG AI ĐƯỢC TỰ ĐỀ XUẤT TĂNG LƯƠNG CHO CHÍNH MÌNH.
     */
    public function proposeSalary(User $user, Employee $employee): bool
    {
        if (!$user->can('SALARY_PROPOSAL_CREATE')) {
            return false;
        }

        // Ràng buộc SoD: Cấm tự đề xuất cho bản thân
        if ($user->employee_id && $user->employee_id === $employee->id) {
            return false;
        }

        return $this->isEmployeeInUserScope($user, $employee);
    }

    /**
     * Kiểm tra nhân sự có nằm trong phạm vi cây tổ chức (subtree) của người dùng không.
     */
    private function isEmployeeInUserScope(User $user, Employee $employee): bool
    {
        $employeeOrgUnit = $employee->currentOrgUnit;
        if (!$employeeOrgUnit) {
            return false;
        }

        $userScopes = $user->scopes()->with('orgUnit')->get();

        foreach ($userScopes as $scope) {
            $scopeOrgUnit = $scope->orgUnit;
            if (!$scopeOrgUnit) {
                continue;
            }

            if ($scope->include_sub_units) {
                // Kiểm tra xem path của nhân viên có bắt đầu bằng path của đơn vị được phân quyền không
                if (str_starts_with($employeeOrgUnit->path, $scopeOrgUnit->path)) {
                    return true;
                }
            } else {
                if ($employeeOrgUnit->id === $scopeOrgUnit->id) {
                    return true;
                }
            }
        }

        return false;
    }
}
