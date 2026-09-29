<?php
/**
 * View 403 Forbidden Access Denied
 */
?>
<div class="card-box" style="text-align: center; padding: 60px 20px; max-width: 650px; margin: 40px auto;">
    <div style="font-size: 4rem; margin-bottom: 20px;">🚫</div>
    <h2 style="font-size: 2rem; font-weight: 800; color: #ef4444; margin-bottom: 10px;">403 Forbidden - Truy cập bị từ chối</h2>
    <p style="font-size: 1rem; color: var(--text-secondary); margin-bottom: 20px;">
        Tài khoản của bạn không có đủ quyền hạn để truy cập vào tài nguyên hoặc thực thi tính năng này.
    </p>

    <?php if (!empty($permission)): ?>
        <div style="background: #fef2f2; padding: 12px; border-radius: var(--radius-md); display: inline-block; margin-bottom: 30px; border: 1px solid #fecaca;">
            <span style="color: #991b1b; font-size: 0.9rem;">
                Mã quyền truy cập yêu cầu: <code><?= htmlspecialchars($permission) ?></code>
            </span>
        </div>
    <?php endif; ?>

    <div style="display: flex; justify-content: center; gap: 14px;">
        <a href="<?= url('dashboard') ?>" class="btn btn-primary">
            🏠 Trở về Trang chủ Dashboard
        </a>
        <a href="javascript:history.back()" class="btn btn-outline">
            ⬅️ Quay lại trang trước
        </a>
    </div>
</div>
