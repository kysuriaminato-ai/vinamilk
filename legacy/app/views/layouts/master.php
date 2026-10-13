<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?= htmlspecialchars($title ?? 'Vinamilk HRM') ?></title>
    
    <!-- Instant Anti-FOUC Theme Script -->
    <script>
    (function() {
        const savedTheme = localStorage.getItem('vnm_theme') || 'system';
        document.documentElement.setAttribute('data-theme', savedTheme);
    })();
    </script>

    <!-- Vinamilk Corporate Stylesheet -->
    <link rel="stylesheet" href="<?= asset('css/style.css') ?>">
    <link rel="icon" href="https://www.vinamilk.com.vn/favicon.ico" type="image/x-icon">
</head>
<body>
    <div class="app-wrapper">
        <!-- Sidebar Navigation -->
        <?php require_once APP_DIR . '/views/layouts/sidebar.php'; ?>

        <div class="app-main">
            <!-- Header Topbar -->
            <?php require_once APP_DIR . '/views/layouts/header.php'; ?>

            <!-- Main Page Content -->
            <main class="app-content">
                <!-- Flash Notification Messages -->
                <?php if ($flashSuccess = flash('success')): ?>
                    <div class="alert alert-success">
                        <span>✅</span>
                        <div><?= $flashSuccess['message'] ?></div>
                    </div>
                <?php endif; ?>

                <?php if ($flashError = flash('error')): ?>
                    <div class="alert alert-danger">
                        <span>⚠️</span>
                        <div><?= $flashError['message'] ?></div>
                    </div>
                <?php endif; ?>

                <?php if ($flashInfo = flash('info')): ?>
                    <div class="alert alert-info">
                        <span>ℹ️</span>
                        <div><?= $flashInfo['message'] ?></div>
                    </div>
                <?php endif; ?>

                <!-- Render Sub-view Content -->
                <?= $content ?? '' ?>
            </main>

            <!-- Footer -->
            <?php require_once APP_DIR . '/views/layouts/footer.php'; ?>
        </div>
    </div>

    <!-- Main JavaScript -->
    <script src="<?= asset('js/app.js') ?>"></script>
</body>
</html>
