<?php
/**
 * Layout Dynamic Sidebar Navigation Component
 */
$currentUri = $_GET['url'] ?? 'dashboard';
?>
<aside class="app-sidebar">
    <div class="sidebar-brand">
        <div class="sidebar-logo">VNM</div>
        <div class="sidebar-brand-title">VINAMILK HRM</div>
    </div>

    <nav class="sidebar-menu">
        <div class="menu-category">Tổng quan</div>
        <a href="<?= url('dashboard') ?>" class="menu-item <?= ($currentUri === 'dashboard' || $currentUri === '') ? 'active' : '' ?>">
            <span class="menu-icon">📊</span>
            <span>Dashboard Điều hành</span>
        </a>

        <?php if (has_permission('NHANSU.View')): ?>
            <div class="menu-category">Quản lý Hồ sơ & Vòng đời</div>
            <a href="<?= url('nhansu') ?>" class="menu-item <?= ($currentUri === 'nhansu' || $currentUri === 'nhansu/index') ? 'active' : '' ?>">
                <span class="menu-icon">👥</span>
                <span>Hồ sơ Nhân sự Số hóa</span>
            </a>
            <a href="<?= url('nhansu/probation') ?>" class="menu-item <?= (str_starts_with($currentUri, 'nhansu/probation')) ? 'active' : '' ?>">
                <span class="menu-icon">⏳</span>
                <span>Thử việc & Xếp lương</span>
            </a>
            <a href="<?= url('tuyenchuyen') ?>" class="menu-item <?= (str_starts_with($currentUri, 'tuyenchuyen')) ? 'active' : '' ?>">
                <span class="menu-icon">🔄</span>
                <span>Thuyên chuyển & Bổ nhiệm</span>
            </a>
            <a href="<?= url('ktkl') ?>" class="menu-item <?= (str_starts_with($currentUri, 'ktkl')) ? 'active' : '' ?>">
                <span class="menu-icon">🏅</span>
                <span>Khen thưởng & Kỷ luật</span>
            </a>
            <a href="<?= url('nhansu/retirement') ?>" class="menu-item <?= (str_starts_with($currentUri, 'nhansu/retirement')) ? 'active' : '' ?>">
                <span class="menu-icon">🏖️</span>
                <span>Nghỉ hưu & Thôi việc</span>
            </a>
        <?php endif; ?>

        <?php if (has_permission('TUYEN_DUNG.View') || has_permission('DAO_TAO.View')): ?>
            <div class="menu-category">Tuyển dụng & Đào tạo</div>
            <?php if (has_permission('TUYEN_DUNG.View')): ?>
                <a href="<?= url('tuyendung') ?>" class="menu-item <?= (str_starts_with($currentUri, 'tuyendung')) ? 'active' : '' ?>">
                    <span class="menu-icon">🎯</span>
                    <span>Tuyển dụng & UV</span>
                </a>
            <?php endif; ?>
            <?php if (has_permission('DAO_TAO.View')): ?>
                <a href="<?= url('daotao') ?>" class="menu-item <?= (str_starts_with($currentUri, 'daotao')) ? 'active' : '' ?>">
                    <span class="menu-icon">🎓</span>
                    <span>Đào tạo Chuyên môn</span>
                </a>
            <?php endif; ?>
        <?php endif; ?>

        <?php if (has_permission('CHAM_CONG.View') || has_permission('LUONG.View')): ?>
            <div class="menu-category">Chấm công & Lương 3P</div>
            <?php if (has_permission('CHAM_CONG.View')): ?>
                <a href="<?= url('chamcong') ?>" class="menu-item <?= (str_starts_with($currentUri, 'chamcong')) ? 'active' : '' ?>">
                    <span class="menu-icon">⏰</span>
                    <span>Bảng Chấm công</span>
                </a>
            <?php endif; ?>
            <?php if (has_permission('LUONG.View')): ?>
                <a href="<?= url('luong') ?>" class="menu-item <?= (str_starts_with($currentUri, 'luong')) ? 'active' : '' ?>">
                    <span class="menu-icon">💰</span>
                    <span>Bảng Lương 3P</span>
                </a>
            <?php endif; ?>
        <?php endif; ?>

        <?php if (has_permission('BAO_CAO.View')): ?>
            <div class="menu-category">Trí Tuệ Nhân Tạo AI</div>
            <a href="<?= url('ai') ?>" class="menu-item <?= (str_starts_with($currentUri, 'ai')) ? 'active' : '' ?>" style="background: rgba(245, 158, 11, 0.15); color: #f59e0b; font-weight: 700;">
                <span class="menu-icon">🤖</span>
                <span>Hệ Chuyên Gia AI Expert</span>
            </a>
        <?php endif; ?>

        <?php if (has_permission('HE_THONG.View')): ?>
            <div class="menu-category">Quản trị Hệ thống</div>
            <a href="<?= url('roles') ?>" class="menu-item <?= (str_starts_with($currentUri, 'roles')) ? 'active' : '' ?>">
                <span class="menu-icon">🔐</span>
                <span>Phân quyền Đa nhiệm</span>
            </a>
        <?php endif; ?>

        <div class="menu-category">Tài khoản</div>
        <a href="<?= url('auth/profile') ?>" class="menu-item <?= ($currentUri === 'auth/profile') ? 'active' : '' ?>">
            <span class="menu-icon">👤</span>
            <span>Thông tin cá nhân</span>
        </a>
        <a href="<?= url('auth/changePassword') ?>" class="menu-item <?= ($currentUri === 'auth/changePassword') ? 'active' : '' ?>">
            <span class="menu-icon">🔑</span>
            <span>Đổi mật khẩu</span>
        </a>
    </nav>
</aside>
