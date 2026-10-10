<?php
/**
 * Layout Header Topbar Component
 */
$user = current_user();
?>
<header class="app-header">
    <div class="header-left">
        <h1 class="page-title-text"><?= htmlspecialchars($title ?? 'Vinamilk HRM') ?></h1>
    </div>

    <div class="header-right d-flex align-items-center gap-3">
        <!-- Theme Switcher Group (Sáng / Tối / Hệ thống) -->
        <div class="theme-switcher-group" role="group" aria-label="Giao diện hiển thị">
            <button type="button" class="theme-btn" data-theme-mode="light" title="Chuyển sang giao diện Sáng">
                ☀️ <span class="d-none d-md-inline">Sáng</span>
            </button>
            <button type="button" class="theme-btn" data-theme-mode="dark" title="Chuyển sang giao diện Tối">
                🌙 <span class="d-none d-md-inline">Tối</span>
            </button>
            <button type="button" class="theme-btn" data-theme-mode="system" title="Giao diện theo hệ thống OS">
                💻 <span class="d-none d-md-inline">Hệ thống</span>
            </button>
        </div>

        <?php if ($user): ?>
            <a href="<?= url('auth/profile') ?>" class="user-profile-badge" title="Xem hồ sơ & phân quyền">
                <div class="user-avatar">
                    <?= strtoupper(substr($user['HoTen'] ?? $user['Username'], 0, 1)) ?>
                </div>
                <div class="user-info">
                    <span class="user-name"><?= htmlspecialchars($user['HoTen'] ?? $user['Username']) ?></span>
                    <span class="user-role-tag">★ <?= htmlspecialchars($user['RoleName'] ?? $user['RoleCode']) ?></span>
                </div>
            </a>
            
            <a href="<?= url('auth/logout') ?>" class="btn btn-outline btn-sm" onclick="return confirm('Bạn có chắc chắn muốn đăng xuất khỏi hệ thống?');" style="color: #ef4444; border-color: #fecaca;">
                🚪 Đăng xuất
            </a>
        <?php endif; ?>
    </div>
</header>
