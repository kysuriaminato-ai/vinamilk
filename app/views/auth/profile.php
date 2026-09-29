<?php
/**
 * View User Profile & Dynamic Permissions List
 */
?>
<div class="card-box">
    <div style="display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--border-color); padding-bottom: 20px; margin-bottom: 24px;">
        <div style="display: flex; align-items: center; gap: 20px;">
            <div class="user-avatar" style="width: 72px; height: 72px; font-size: 2rem;">
                <?= strtoupper(substr($user['HoTen'] ?? $user['Username'], 0, 1)) ?>
            </div>
            <div>
                <h2 style="font-size: 1.5rem; font-weight: 800; color: var(--vnm-navy); margin-bottom: 4px;">
                    <?= htmlspecialchars($user['HoTen'] ?? $user['Username']) ?>
                </h2>
                <div style="display: flex; gap: 10px; align-items: center;">
                    <span class="badge badge-primary" style="font-size: 0.85rem;">★ Vai trò: <?= htmlspecialchars($user['RoleName']) ?> (<?= htmlspecialchars($user['RoleCode']) ?>)</span>
                    <?php if (!empty($user['MaNV'])): ?>
                        <span class="badge badge-success" style="font-size: 0.85rem;">Mã NV: <?= htmlspecialchars($user['MaNV']) ?></span>
                    <?php endif; ?>
                </div>
            </div>
        </div>
        <div>
            <a href="<?= url('auth/changePassword') ?>" class="btn btn-outline">🔑 Đổi mật khẩu</a>
        </div>
    </div>

    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 30px;">
        <!-- Cột 1: Thông tin tài khoản -->
        <div>
            <h3 style="font-size: 1.1rem; font-weight: 700; color: var(--vnm-navy); margin-bottom: 16px;">
                📋 Thông tin chi tiết Tài khoản
            </h3>

            <table class="table">
                <tr>
                    <td style="width: 160px; font-weight: 600; color: var(--text-secondary);">Tên đăng nhập:</td>
                    <td style="font-weight: 700; color: var(--vnm-navy);"><?= htmlspecialchars($user['Username']) ?></td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Đơn vị công tác:</td>
                    <td><?= htmlspecialchars($user['TenDV'] ?? 'Hệ thống Admin') ?></td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Chức vụ:</td>
                    <td><?= htmlspecialchars($user['TenCV'] ?? 'Quản trị viên') ?></td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Email công việc:</td>
                    <td><?= htmlspecialchars($user['EmailNV'] ?? 'Chưa cập nhật') ?></td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Đăng nhập gần nhất:</td>
                    <td><?= format_datetime($user['LastLogin']) ?></td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Trạng thái tài khoản:</td>
                    <td>
                        <?php if ((int)$user['IsActive'] === 1): ?>
                            <span class="badge badge-success">Đang hoạt động</span>
                        <?php else: ?>
                            <span class="badge badge-danger">Tạm khóa</span>
                        <?php endif; ?>
                    </td>
                </tr>
            </table>
        </div>

        <!-- Cột 2: Danh sách Quyền hạn được cấp -->
        <div>
            <h3 style="font-size: 1.1rem; font-weight: 700; color: var(--vnm-navy); margin-bottom: 16px;">
                🔐 Quyền hạn được cấp (Dynamic Permissions)
            </h3>

            <?php if (Auth::isAdmin()): ?>
                <div class="alert alert-success">
                    <span>👑</span>
                    <div>Tài khoản của bạn thuộc Vai trò <strong>Admin (Quản trị viên)</strong> với <strong>Toàn quyền truy cập</strong> mọi tính năng và module trong hệ thống.</div>
                </div>
            <?php elseif (!empty($permissions)): ?>
                <p style="font-size: 0.85rem; color: var(--text-secondary); margin-bottom: 12px;">
                    Các mã quyền hạn chi tiết bạn được phép thực thi trong phiên làm việc hiện tại:
                </p>
                <div style="display: flex; flex-wrap: wrap; gap: 8px; max-height: 250px; overflow-y: auto; padding: 10px; border: 1px solid var(--border-color); border-radius: var(--radius-md); background: #f8fafc;">
                    <?php foreach ($permissions as $perm): ?>
                        <span class="badge badge-primary" style="font-size: 0.8rem; padding: 6px 12px;">
                            🛡️ <?= htmlspecialchars($perm) ?>
                        </span>
                    <?php endforeach; ?>
                </div>
            <?php else: ?>
                <div class="alert alert-warning">
                    <span>⚠️</span>
                    <div>Tài khoản hiện tại chưa được cấp quyền truy cập module cụ thể.</div>
                </div>
            <?php endif; ?>
        </div>
    </div>
</div>
