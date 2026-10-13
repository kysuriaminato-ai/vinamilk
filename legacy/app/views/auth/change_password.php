<?php
/**
 * View Change Password Page
 */
?>
<div class="card-box" style="max-width: 600px; margin: 0 auto;">
    <h2 class="card-title">🔑 Đổi mật khẩu tài khoản</h2>

    <?php if ($err = flash('password_error')): ?>
        <div class="alert alert-danger">
            <span>⚠️</span>
            <div><?= $err['message'] ?></div>
        </div>
    <?php endif; ?>

    <form action="<?= url('auth/changePassword') ?>" method="POST">
        <?= csrf_field() ?>

        <div class="form-group">
            <label for="current_password" class="form-label">Mật khẩu hiện tại</label>
            <div class="input-wrapper">
                <span class="input-icon">🔒</span>
                <input type="password" id="current_password" name="current_password" class="form-control" required autofocus>
                <span class="toggle-password">🔒</span>
            </div>
        </div>

        <div class="form-group">
            <label for="new_password" class="form-label">Mật khẩu mới (Tối thiểu 6 ký tự)</label>
            <div class="input-wrapper">
                <span class="input-icon">🔑</span>
                <input type="password" id="new_password" name="new_password" class="form-control" required minlength="6">
                <span class="toggle-password">🔒</span>
            </div>
        </div>

        <div class="form-group">
            <label for="confirm_password" class="form-label">Xác nhận mật khẩu mới</label>
            <div class="input-wrapper">
                <span class="input-icon">✅</span>
                <input type="password" id="confirm_password" name="confirm_password" class="form-control" required minlength="6">
                <span class="toggle-password">🔒</span>
            </div>
        </div>

        <div style="display: flex; justify-content: flex-end; gap: 12px; margin-top: 30px;">
            <a href="<?= url('auth/profile') ?>" class="btn btn-outline">Hủy bỏ</a>
            <button type="submit" class="btn btn-primary">💾 Lưu mật khẩu mới</button>
        </div>
    </form>
</div>
