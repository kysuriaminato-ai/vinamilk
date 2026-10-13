<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?= htmlspecialchars($title ?? 'Đăng nhập - Vinamilk HRM') ?></title>
    <link rel="stylesheet" href="<?= asset('css/style.css') ?>">
</head>
<body class="login-body">
    <div class="login-card">
        <div class="login-header">
            <div class="login-logo-badge">
                <span>🥛</span> VINAMILK HRM
            </div>
            <h2 class="login-title">Đăng nhập Hệ thống</h2>
            <p class="login-subtitle">Hệ thống Quản lý Nguồn nhân lực Tập đoàn Vinamilk</p>
        </div>

        <?php if ($loginError = flash('login_error')): ?>
            <div class="alert alert-danger">
                <span>⚠️</span>
                <div><?= $loginError['message'] ?></div>
            </div>
        <?php endif; ?>

        <?php if ($info = flash('info')): ?>
            <div class="alert alert-info">
                <span>ℹ️</span>
                <div><?= $info['message'] ?></div>
            </div>
        <?php endif; ?>

        <form action="<?= url('auth/login') ?>" method="POST">
            <?= csrf_field() ?>

            <div class="form-group">
                <label for="username" class="form-label">Tên đăng nhập / Email</label>
                <div class="input-wrapper">
                    <span class="input-icon">👤</span>
                    <input type="text" id="username" name="username" class="form-control" placeholder="Nhập tên đăng nhập (VD: admin, huyen.ntt)" required autofocus>
                </div>
            </div>

            <div class="form-group">
                <label for="password" class="form-label">Mật khẩu</label>
                <div class="input-wrapper">
                    <span class="input-icon">🔑</span>
                    <input type="password" id="password" name="password" class="form-control" placeholder="Nhập mật khẩu hệ thống" required>
                    <span class="toggle-password" title="Hiện/Ẩn mật khẩu">🔒</span>
                </div>
            </div>

            <div class="form-group" style="display: flex; align-items: center; justify-content: space-between; font-size: 0.85rem;">
                <label style="display: flex; align-items: center; gap: 6px; cursor: pointer;">
                    <input type="checkbox" name="remember" style="accent-color: var(--vnm-primary);"> Ghi nhớ đăng nhập
                </label>
                <a href="#" onclick="alert('Vui lòng liên hệ Phòng IT / Khối Nhân sự Vinamilk để cấp lại mật khẩu.'); return false;" style="color: var(--vnm-primary); font-weight: 500;">Quên mật khẩu?</a>
            </div>

            <button type="submit" class="btn btn-primary btn-block" style="padding: 14px; font-size: 1rem; margin-top: 10px;">
                🚀 ĐĂNG NHẬP HỆ THỐNG
            </button>
        </form>

        <div style="margin-top: 30px; padding-top: 20px; border-top: 1px solid var(--border-color); font-size: 0.8rem; color: var(--text-secondary);">
            <p style="font-weight: 700; color: var(--vnm-navy); margin-bottom: 6px;">💡 Tài khoản demo thử nghiệm:</p>
            <ul style="padding-left: 18px; line-height: 1.6;">
                <li><strong>Admin:</strong> <code>admin</code> | MK: <code>Vinamilk@2026</code></li>
                <li><strong>HR Manager:</strong> <code>huyen.ntt</code> | MK: <code>Vinamilk@2026</code></li>
                <li><strong>Manager (BGD/Trưởng phòng):</strong> <code>tri.dm</code> | MK: <code>Vinamilk@2026</code></li>
                <li><strong>Employee:</strong> <code>tung.pt</code> | MK: <code>Vinamilk@2026</code></li>
            </ul>
        </div>
    </div>

    <script src="<?= asset('js/app.js') ?>"></script>
</body>
</html>
