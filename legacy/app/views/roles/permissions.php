<?php
/**
 * View Multi-role Dynamic Permission Matrix
 */
$assignedIDs = $role['AssignedPermissionIDs'] ?? [];
?>
<div class="card-box">
    <!-- Header Controls -->
    <div style="display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--border-color); padding-bottom: 20px; margin-bottom: 24px;">
        <div>
            <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;">
                ⚙️ Ma trận Phân quyền Đa nhiệm (Dynamic Permission Matrix)
            </h2>
            <p style="font-size: 0.85rem; color: var(--text-secondary); margin-top: 4px;">
                Đang cấu hình quyền hạn cho Vai trò: <strong style="color: var(--vnm-primary); font-size: 1rem;"><?= htmlspecialchars($role['RoleName']) ?></strong> (Mã: <code><?= htmlspecialchars($role['RoleCode']) ?></code>)
            </p>
        </div>

        <div style="display: flex; align-items: center; gap: 12px;">
            <label for="role-select" style="font-size: 0.85rem; font-weight: 600;">Chọn vai trò:</label>
            <select id="role-select" class="form-control" style="width: auto; padding: 8px 16px;" onchange="location = this.value;">
                <?php foreach ($allRoles as $r): ?>
                    <option value="<?= url('roles/permissions/' . $r['RoleID']) ?>" <?= ($r['RoleID'] == $role['RoleID']) ? 'selected' : '' ?>>
                        <?= htmlspecialchars($r['RoleName']) ?> (<?= htmlspecialchars($r['RoleCode']) ?>)
                    </option>
                <?php endforeach; ?>
            </select>
        </div>
    </div>

    <!-- Quick Action Controls -->
    <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 20px; background: #f8fafc; padding: 12px 20px; border-radius: var(--radius-md); border: 1px solid var(--border-color);">
        <div style="display: flex; gap: 10px;">
            <button type="button" id="btn-select-all-perms" class="btn btn-outline btn-sm">
                ☑️ Chọn tất cả
            </button>
            <button type="button" id="btn-deselect-all-perms" class="btn btn-outline btn-sm">
                ☒ Bỏ chọn tất cả
            </button>
        </div>
        <div style="font-size: 0.85rem; color: var(--text-secondary);">
            Tổng cộng: <strong><?= count($assignedIDs) ?></strong> quyền đang được tích chọn.
        </div>
    </div>

    <!-- Permission Form -->
    <form action="<?= url('roles/updatePermissions') ?>" method="POST">
        <?= csrf_field() ?>
        <input type="hidden" name="role_id" value="<?= $role['RoleID'] ?>">

        <div class="table-responsive">
            <table class="table" style="border: 1px solid var(--border-color);">
                <thead>
                    <tr>
                        <th style="width: 220px;">Tên Module</th>
                        <th style="width: 140px;">Mã Action</th>
                        <th>Mô tả Chi tiết Quyền hạn</th>
                        <th style="width: 120px; text-align: center;">Cấp quyền</th>
                    </tr>
                </thead>
                <tbody>
                    <?php foreach ($groupedPermissions as $moduleCode => $moduleGroup): ?>
                        <!-- Header Dòng Module -->
                        <tr style="background-color: #f1f5f9;">
                            <td colspan="4" style="font-weight: 700; color: var(--vnm-navy); font-size: 0.95rem; padding: 12px 16px;">
                                📦 Module: <?= htmlspecialchars($moduleGroup['ModuleName']) ?> (<code><?= htmlspecialchars($moduleCode) ?></code>)
                            </td>
                        </tr>

                        <!-- Sub-items of this Module -->
                        <?php foreach ($moduleGroup['Items'] as $item): ?>
                            <?php $isChecked = in_array((int)$item['PermissionID'], $assignedIDs, true); ?>
                            <tr>
                                <td style="padding-left: 30px; font-size: 0.85rem; color: var(--text-secondary);">
                                    <?= htmlspecialchars($moduleGroup['ModuleName']) ?>
                                </td>
                                <td>
                                    <?php
                                    $actionColor = match($item['ActionCode']) {
                                        'View' => 'badge-primary',
                                        'Add', 'Edit' => 'badge-success',
                                        'Delete' => 'badge-danger',
                                        default => 'badge-primary'
                                    };
                                    ?>
                                    <span class="badge <?= $actionColor ?>">
                                        <?= htmlspecialchars($item['ActionCode']) ?>
                                    </span>
                                </td>
                                <td>
                                    <strong><?= htmlspecialchars($item['Description'] ?? '') ?></strong>
                                    <br>
                                    <small style="color: var(--text-muted); font-size: 0.75rem;">
                                        Mã kiểm tra code: <code><?= htmlspecialchars($item['ModuleCode'] . '.' . $item['ActionCode']) ?></code>
                                    </small>
                                </td>
                                <td style="text-align: center;">
                                    <input type="checkbox" 
                                           name="permissions[]" 
                                           value="<?= $item['PermissionID'] ?>" 
                                           class="matrix-checkbox"
                                           <?= $isChecked ? 'checked' : '' ?>>
                                </td>
                            </tr>
                        <?php endforeach; ?>
                    <?php endforeach; ?>
                </tbody>
            </table>
        </div>

        <div style="display: flex; justify-content: flex-end; gap: 14px; margin-top: 24px;">
            <a href="<?= url('roles') ?>" class="btn btn-outline">Quay lại danh sách</a>
            <button type="submit" class="btn btn-primary" style="padding: 12px 30px;">
                💾 LƯU MA TRẬN PHÂN QUYỀN
            </button>
        </div>
    </form>
</div>
