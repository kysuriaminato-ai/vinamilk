<?php
/**
 * View System Roles List
 */
?>
<div class="card-box">
    <div class="card-title">
        <div>
            <h2 style="font-size: 1.3rem; color: var(--vnm-navy);">🔐 Danh sách Vai trò Hệ thống (System Roles)</h2>
            <p style="font-size: 0.85rem; color: var(--text-secondary); font-weight: normal; margin-top: 4px;">
                Quản lý các cấp vai trò và thiết lập Ma trận phân quyền đa nhiệm (Multi-role Dynamic Permission Matrix)
            </p>
        </div>
    </div>

    <div class="table-responsive">
        <table class="table">
            <thead>
                <tr>
                    <th>Role ID</th>
                    <th>Tên Vai trò</th>
                    <th>Mã Vai trò (Code)</th>
                    <th>Mô tả chức năng</th>
                    <th>Số tài khoản</th>
                    <th>Trạng thái</th>
                    <th style="text-align: right;">Hành động</th>
                </tr>
            </thead>
            <tbody>
                <?php foreach ($roles as $r): ?>
                    <tr>
                        <td><code>#<?= $r['RoleID'] ?></code></td>
                        <td style="font-weight: 700; color: var(--vnm-navy); font-size: 0.95rem;">
                            <?= htmlspecialchars($r['RoleName']) ?>
                        </td>
                        <td><span class="badge badge-primary"><code><?= htmlspecialchars($r['RoleCode']) ?></code></span></td>
                        <td style="color: var(--text-secondary); font-size: 0.85rem;"><?= htmlspecialchars($r['Description'] ?? 'Chưa có mô tả') ?></td>
                        <td style="font-weight: 700; color: var(--vnm-primary);"><?= number_format($r['TotalUsers']) ?> tài khoản</td>
                        <td>
                            <?php if ((int)$r['TrangThai'] === 1): ?>
                                <span class="badge badge-success">Kích hoạt</span>
                            <?php else: ?>
                                <span class="badge badge-danger">Khóa</span>
                            <?php endif; ?>
                        </td>
                        <td style="text-align: right;">
                            <a href="<?= url('roles/permissions/' . $r['RoleID']) ?>" class="btn btn-primary btn-sm">
                                ⚙️ Cấu hình Phân quyền
                            </a>
                        </td>
                    </tr>
                <?php endforeach; ?>
            </tbody>
        </table>
    </div>
</div>
