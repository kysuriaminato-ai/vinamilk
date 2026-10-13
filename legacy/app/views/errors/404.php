<?php
/**
 * View 404 Not Found Page
 */
?>
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?= htmlspecialchars($title ?? '404 - Trang không tồn tại') ?></title>
    <link rel="stylesheet" href="<?= asset('css/style.css') ?>">
</head>
<body style="background: var(--bg-main); display: flex; align-items: center; justify-content: center; min-height: 100vh; padding: 20px;">
    <div class="card-box" style="text-align: center; padding: 60px 40px; max-width: 550px;">
        <div style="font-size: 4rem; margin-bottom: 20px;">🔍</div>
        <h1 style="font-size: 2.2rem; font-weight: 800; color: var(--vnm-navy); margin-bottom: 10px;">404 Not Found</h1>
        <p style="font-size: 1rem; color: var(--text-secondary); margin-bottom: 30px;">
            Đường dẫn URL bạn truy cập không tồn tại hoặc đã bị di chuyển.
        </p>
        <a href="<?= url('dashboard') ?>" class="btn btn-primary">
            🏠 Trở về Trang chủ Vinamilk HRM
        </a>
    </div>
</body>
</html>
