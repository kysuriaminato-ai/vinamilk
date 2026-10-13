<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?= e($title ?? 'Đăng nhập - Vinamilk Enterprise HRM') ?></title>
    <!-- Bootstrap 5.3 CSS -->
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet">
    <!-- Bootstrap Icons -->
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet">
    <!-- Google Fonts: Plus Jakarta Sans -->
    <link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;600;700;800&display=swap" rel="stylesheet">
    <style>
        body {
            font-family: 'Plus Jakarta Sans', sans-serif;
            background: linear-gradient(135deg, #002F6C 0%, #0047BA 50%, #00A3E0 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }
        .login-card {
            background: #ffffff;
            border-radius: 16px;
            box-shadow: 0 15px 35px rgba(0, 47, 108, 0.25);
            max-width: 900px;
            width: 100%;
            overflow: hidden;
        }
        .brand-badge {
            background: #002F6C;
            color: #ffffff;
            font-weight: 800;
            padding: 6px 12px;
            border-radius: 6px;
            letter-spacing: 1px;
            display: inline-block;
        }
        .demo-account-item {
            cursor: pointer;
            transition: all 0.2s;
            border: 1px solid #E2E8F0;
            border-radius: 8px;
            padding: 8px 12px;
            margin-bottom: 6px;
            font-size: 13px;
        }
        .demo-account-item:hover {
            background: #EFF6FF;
            border-color: #0047BA;
        }
    </style>
</head>
<body>

<div class="login-card row g-0">
    <!-- Cột trái: Form đăng nhập -->
    <div class="col-md-7 p-4 p-md-5">
        <div class="mb-4">
            <span class="brand-badge mb-2">VINAMILK</span>
            <h3 class="fw-bold text-dark mt-2 mb-1">Đăng Nhập Hệ Thống</h3>
            <p class="text-muted small">Cổng Quản Trị Nhân Lực Doanh Nghiệp (PHP 8.2 MVC & MySQL 8)</p>
        </div>

        <?php $error = Session::getFlash('login_error'); ?>
        <?php if ($error): ?>
            <div class="alert alert-danger py-2 px-3 small mb-3">
                <i class="bi bi-exclamation-triangle-fill me-1"></i> <?= e($error['message']) ?>
            </div>
        <?php endif; ?>

        <?php $info = Session::getFlash('info'); ?>
        <?php if ($info): ?>
            <div class="alert alert-info py-2 px-3 small mb-3">
                <i class="bi bi-info-circle-fill me-1"></i> <?= e($info['message']) ?>
            </div>
        <?php endif; ?>

        <form action="<?= url('auth/login') ?>" method="POST" id="loginForm">
            <!-- CSRF Token Bắt buộc -->
            <?= csrf_field() ?>

            <div class="mb-3">
                <label for="username" class="form-label fw-semibold small text-muted">Tên đăng nhập</label>
                <div class="input-group">
                    <span class="input-group-text bg-light border-end-0"><i class="bi bi-person text-primary"></i></span>
                    <input type="text" class="form-control border-start-0 ps-0" id="username" name="username" placeholder="Nhập username (vd: admin, bod.ceo)" required autofocus>
                </div>
            </div>

            <div class="mb-4">
                <label for="password" class="form-label fw-semibold small text-muted">Mật khẩu</label>
                <div class="input-group">
                    <span class="input-group-text bg-light border-end-0"><i class="bi bi-lock text-primary"></i></span>
                    <input type="password" class="form-control border-start-0 ps-0" id="password" name="password" placeholder="Mật khẩu mặc định: Vinamilk@2026" required>
                </div>
            </div>

            <button type="submit" class="btn btn-primary w-100 py-2 fw-bold shadow-sm" style="background: #002F6C; border-color: #002F6C;">
                <i class="bi bi-box-arrow-in-right me-1"></i> Đăng Nhập
            </button>
        </form>

        <div class="mt-4 pt-3 border-top text-center text-muted" style="font-size: 12px;">
            Hệ thống Quản lý Nguồn nhân lực Tập đoàn Vinamilk &bull; Phiên bản 3.0.0
        </div>
    </div>

    <!-- Cột phải: Danh sách 7 Tài khoản Demo chuẩn -->
    <div class="col-md-5 p-4 bg-light border-start">
        <h6 class="fw-bold text-dark mb-2"><i class="bi bi-people-fill text-primary me-1"></i> 7 Tài khoản Demo (Click điền nhanh):</h6>
        <p class="text-muted" style="font-size: 11px;">Mật khẩu chung cho tất cả tài khoản: <strong>Vinamilk@2026</strong></p>

        <div class="demo-account-item" onclick="fillLogin('admin')">
            <div class="fw-bold text-danger"><i class="bi bi-shield-shaded me-1"></i> ADMIN</div>
            <div class="text-muted">Username: <code>admin</code> &bull; Toàn quyền hệ thống</div>
        </div>

        <div class="demo-account-item" onclick="fillLogin('bod.ceo')">
            <div class="fw-bold text-warning text-dark"><i class="bi bi-briefcase-fill me-1"></i> BOD (Ban TGĐ)</div>
            <div class="text-muted">Username: <code>bod.ceo</code> &bull; Chỉ xem toàn tập đoàn</div>
        </div>

        <div class="demo-account-item" onclick="fillLogin('hr.tapdoan')">
            <div class="fw-bold text-info"><i class="bi bi-building me-1"></i> HR_TAPDOAN</div>
            <div class="text-muted">Username: <code>hr.tapdoan</code> &bull; Nhân sự Tập đoàn</div>
        </div>

        <div class="demo-account-item" onclick="fillLogin('hr.nhamay')">
            <div class="fw-bold text-primary"><i class="bi bi-gear-fill me-1"></i> HR_DONVI</div>
            <div class="text-muted">Username: <code>hr.nhamay</code> &bull; Cây Nhà máy Bình Dương</div>
        </div>

        <div class="demo-account-item" onclick="fillLogin('ketoan.luong')">
            <div class="fw-bold text-success"><i class="bi bi-calculator me-1"></i> KETOAN_LUONG</div>
            <div class="text-muted">Username: <code>ketoan.luong</code> &bull; Kế toán tiền lương</div>
        </div>

        <div class="demo-account-item" onclick="fillLogin('quanly.sx')">
            <div class="fw-bold text-secondary"><i class="bi bi-check-circle-fill me-1"></i> QUANLY_TRUCTIEP</div>
            <div class="text-muted">Username: <code>quanly.sx</code> &bull; Quản lý Xưởng SX Tiên Sơn</div>
        </div>

        <div class="demo-account-item" onclick="fillLogin('nhanvien.ess')">
            <div class="fw-bold text-dark"><i class="bi bi-person-badge me-1"></i> NHANVIEN (ESS)</div>
            <div class="text-muted">Username: <code>nhanvien.ess</code> &bull; Tự phục vụ, tra cứu lương</div>
        </div>
    </div>
</div>

<script>
    function fillLogin(u) {
        document.getElementById('username').value = u;
        document.getElementById('password').value = 'Vinamilk@2026';
    }
</script>

</body>
</html>
