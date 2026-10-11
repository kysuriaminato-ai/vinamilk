<?php

namespace App\Modules\Organization\Services;

use App\Models\AuditLog;
use App\Modules\Employee\Models\Employee;
use App\Modules\Organization\Models\EmployeeAssignment;
use App\Modules\Organization\Models\OrgUnit;
use Carbon\Carbon;
use Illuminate\Support\Facades\Auth;
use Illuminate\Support\Facades\DB;
use Illuminate\Validation\ValidationException;

class OrgUnitService
{
    /**
     * Tạo mới đơn vị tổ chức và tự động tính toán path, level.
     */
    public function createOrgUnit(array $data): OrgUnit
    {
        return DB::transaction(function () use ($data) {
            $parentId = $data['parent_id'] ?? null;
            $parent = $parentId ? OrgUnit::findOrFail($parentId) : null;

            $level = $parent ? $parent->level + 1 : 1;

            // Tạo bản ghi trước để lấy ID tự tăng
            $unit = OrgUnit::create(array_merge($data, [
                'path'  => '/temp/',
                'level' => $level,
            ]));

            // Cập nhật Materialized Path chuẩn: /parent_path/id/
            $path = $parent ? $parent->path . $unit->id . '/' : '/' . $unit->id . '/';
            $unit->update(['path' => $path]);

            AuditLog::create([
                'table_name'   => 'org_units',
                'record_id'    => $unit->id,
                'action'       => 'INSERT',
                'performed_by' => Auth::id(),
                'new_values'   => $unit->toArray(),
            ]);

            return $unit;
        });
    }

    /**
     * Cập nhật đơn vị tổ chức và tự động cập nhật path toàn bộ nhánh con khi đổi cha.
     */
    public function updateOrgUnit(OrgUnit $unit, array $data): OrgUnit
    {
        return DB::transaction(function () use ($unit, $data) {
            $oldValues = $unit->toArray();
            $newParentId = array_key_exists('parent_id', $data) ? $data['parent_id'] : $unit->parent_id;

            // Kiểm tra chống tạo chu trình vòng tròn (Cyclic Dependency)
            if ($newParentId && $newParentId != $unit->parent_id) {
                if ($newParentId == $unit->id) {
                    throw ValidationException::withMessages([
                        'parent_id' => 'Không thể thiết lập đơn vị làm cha của chính mình!'
                    ]);
                }

                $newParent = OrgUnit::findOrFail($newParentId);

                // Không thể chọn đơn vị con/cháu làm đơn vị cha mới
                if (str_starts_with($newParent->path, $unit->path)) {
                    throw ValidationException::withMessages([
                        'parent_id' => 'Không thể di chuyển đơn vị cha vào bên trong một đơn vị con/cháu trực thuộc!'
                    ]);
                }

                $oldPath = $unit->path;
                $newPath = $newParent->path . $unit->id . '/';
                $levelDiff = ($newParent->level + 1) - $unit->level;

                $data['path'] = $newPath;
                $data['level'] = $newParent->level + 1;

                // Cập nhật đơn vị hiện tại
                $unit->update($data);

                // Cập nhật hàng loạt (Bulk update) đường dẫn path và level cho tất cả con cháu phía dưới
                DB::update("
                    UPDATE org_units 
                    SET path = CONCAT(?, SUBSTRING(path, ?)),
                        level = level + ?
                    WHERE path LIKE ? AND id != ?
                ", [
                    $newPath,
                    strlen($oldPath) + 1,
                    $levelDiff,
                    $oldPath . '%',
                    $unit->id
                ]);
            } else {
                $unit->update($data);
            }

            AuditLog::create([
                'table_name'   => 'org_units',
                'record_id'    => $unit->id,
                'action'       => 'UPDATE',
                'performed_by' => Auth::id(),
                'old_values'   => $oldValues,
                'new_values'   => $unit->fresh()->toArray(),
            ]);

            return $unit->fresh();
        });
    }

    /**
     * Lấy dữ liệu cấu trúc cây phân cấp phục vụ hiển thị OrgChart.js hoặc D3.js.
     */
    public function getTreeHierarchy(?OrgUnit $root = null): array
    {
        $query = OrgUnit::query()->where('is_active', true)->with('manager');

        if ($root) {
            $query->where('path', 'LIKE', $root->path . '%');
        }

        $allUnits = $query->orderBy('level')->orderBy('unit_name')->get();

        // Xây dựng cây phân cấp cha-con trong bộ nhớ O(N)
        $unitsById = [];
        foreach ($allUnits as $u) {
            $unitsById[$u->id] = [
                'id'           => $u->id,
                'code'         => $u->unit_code,
                'name'         => $u->unit_name,
                'type'         => $u->unit_type,
                'level'        => $u->level,
                'path'         => $u->path,
                'region'       => $u->region,
                'manager_name' => $u->manager?->full_name ?? 'Chưa bổ nhiệm',
                'children'     => [],
            ];
        }

        $tree = [];
        foreach ($unitsById as $id => &$node) {
            $unitModel = $allUnits->firstWhere('id', $id);
            if ($unitModel->parent_id && isset($unitsById[$unitModel->parent_id])) {
                $unitsById[$unitModel->parent_id]['children'][] = &$node;
            } else {
                $tree[] = &$node;
            }
        }

        return $tree;
    }

    /**
     * Lấy số liệu tổng hợp (Metrics) của một nút: Quân số toàn nhánh, Quỹ lương tháng, Trưởng đơn vị.
     * Sử dụng Materialized Path (path LIKE '/prefix/%').
     */
    public function getNodeMetrics(OrgUnit $unit): array
    {
        $pathPrefix = $unit->path . '%';

        // 1. Tổng quân số và Tổng quỹ lương toàn nhánh cây con
        $summary = DB::selectOne("
            SELECT 
                COUNT(DISTINCT e.id) AS total_headcount,
                COALESCE(SUM(es.p1_position_salary + es.p2_competency_salary), 0) AS total_monthly_payroll
            FROM org_units u
            JOIN employee_assignments a ON a.org_unit_id = u.id AND a.is_current = 1 AND a.deleted_at IS NULL
            JOIN employees e ON e.id = a.employee_id AND e.deleted_at IS NULL AND e.employment_status IN ('ACTIVE', 'PROBATION')
            LEFT JOIN employee_salaries es ON es.employee_id = e.id AND es.is_current = 1 AND es.deleted_at IS NULL
            WHERE u.path LIKE ?
        ", [$pathPrefix]);

        // 2. Quân số trực tiếp tại đơn vị (không tính cấp con)
        $directCount = DB::selectOne("
            SELECT COUNT(DISTINCT e.id) AS direct_headcount
            FROM employee_assignments a
            JOIN employees e ON e.id = a.employee_id AND e.deleted_at IS NULL AND e.employment_status IN ('ACTIVE', 'PROBATION')
            WHERE a.org_unit_id = ? AND a.is_current = 1 AND a.deleted_at IS NULL
        ", [$unit->id]);

        $manager = $unit->manager;

        return [
            'unit_id'                => $unit->id,
            'unit_code'              => $unit->unit_code,
            'unit_name'              => $unit->unit_name,
            'unit_type'              => $unit->unit_type,
            'total_headcount'        => (int)($summary->total_headcount ?? 0),
            'direct_headcount'       => (int)($directCount->direct_headcount ?? 0),
            'total_monthly_payroll'  => (float)($summary->total_monthly_payroll ?? 0),
            'manager' => $manager ? [
                'id'        => $manager->id,
                'name'      => $manager->full_name,
                'code'      => $manager->employee_code,
                'email'     => $manager->company_email,
                'phone'     => $manager->phone_number,
                'position'  => $manager->currentPosition?->position_name ?? 'Trưởng đơn vị',
            ] : null,
        ];
    }

    /**
     * Điều chuyển nhân viên sang đơn vị khác: Tạo assignment mới theo chuẩn SCD Type 2.
     */
    public function transferEmployee(
        int $employeeId,
        int $newUnitId,
        int $newPositionId,
        string $effectiveFromDate,
        ?string $decisionNumber = null,
        ?string $notes = null
    ): EmployeeAssignment {
        return DB::transaction(function () use (
            $employeeId, $newUnitId, $newPositionId, $effectiveFromDate, $decisionNumber, $notes
        ) {
            $effectiveFrom = Carbon::parse($effectiveFromDate);
            $employee = Employee::findOrFail($employeeId);
            $newUnit = OrgUnit::findOrFail($newUnitId);

            // 1. Tìm vị trí phân công chính thức đang hiện hành
            $currentAssignment = EmployeeAssignment::where('employee_id', $employeeId)
                ->where('is_primary', true)
                ->where('is_current', true)
                ->first();

            $oldUnitId = $currentAssignment?->org_unit_id;

            // 2. Chốt ngày kết thúc của assignment cũ (effective_to = effective_from - 1 ngày)
            if ($currentAssignment) {
                $currentAssignment->update([
                    'effective_to' => $effectiveFrom->copy()->subDay()->toDateString(),
                    'is_current'   => false,
                ]);
            }

            // 3. Tạo assignment mới với ngày hiệu lực
            $newAssignment = EmployeeAssignment::create([
                'employee_id'     => $employeeId,
                'org_unit_id'     => $newUnitId,
                'position_id'     => $newPositionId,
                'is_primary'      => true,
                'assignment_type' => 'TRANSFER',
                'decision_number' => $decisionNumber,
                'effective_from'  => $effectiveFrom->toDateString(),
                'effective_to'    => null, // NULL = Đang hiệu lực
                'is_current'      => true,
                'notes'           => $notes ?? "Điều chuyển công tác từ đơn vị ID: {$oldUnitId} sang ID: {$newUnitId}",
            ]);

            // 4. Ghi vết kiểm toán
            AuditLog::create([
                'table_name'   => 'employee_assignments',
                'record_id'    => $newAssignment->id,
                'action'       => 'UPDATE',
                'performed_by' => Auth::id(),
                'old_values'   => $currentAssignment ? $currentAssignment->toArray() : null,
                'new_values'   => $newAssignment->toArray(),
            ]);

            return $newAssignment;
        });
    }
}
