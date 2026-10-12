<?php
/**
 * =====================================================================
 * VINAMILK HRM - SIDEBAR ĐIỀU HƯỚNG TẬP TRUNG (SIDEBAR.PHP)
 * =====================================================================
 * Tự động sinh từ bảng `menu_items` + `role_permissions` qua MenuHelper.
 * Chỉ hiển thị các mục mà người dùng có thẩm quyền truy cập.
 * Tuân thủ WCAG AA với aria attributes, focus indicator và phân cấp ngữ nghĩa.
 */

require_once __DIR__ . '/../../helpers/MenuHelper.php';
require_once __DIR__ . '/../../helpers/Auth.php';

$menuItems = MenuHelper::getUserMenu();
$user = Auth::user();
$userRole = Auth::role();
$userMaDV = Auth::maDV() ?: 'Toàn tập đoàn';

$roleBadgeClass = match($userRole) {
    'ADMIN'           => 'vnm-badge-danger',
    'BOD'             => 'vnm-badge-warning',
    'HR_TAPDOAN'      => 'vnm-badge-info',
    'HR_DONVI'        => 'vnm-badge-info',
    'KETOAN_LUONG'    => 'vnm-badge-success',
    'QUANLY_TRUCTIEP' => 'vnm-badge-warning',
    default           => 'vnm-badge-neutral'
};
?>
<aside class="sidebar-vnm no-print" role="navigation" aria-label="Điều hướng chính hệ thống">
    <!-- DANH SÁCH MENU THEO PHÂN QUYỀN -->
    <div class="flex-grow-1 overflow-y-auto">
        <div class="sidebar-category">Phân hệ chức năng</div>
        <ul class="sidebar-nav">
            <?php foreach ($menuItems as $item): ?>
                <?php if (empty($item['children'])): ?>
                    <!-- Mục đơn (Single Link) -->
                    <li class="nav-item">
                        <a class="sidebar-link <?= !empty($item['is_active']) ? 'active' : '' ?>" 
                           href="<?= url($item['Route']) ?>"
                           <?= !empty($item['is_active']) ? 'aria-current="page"' : '' ?>>
                            <i class="bi <?= e($item['Icon']) ?>" aria-hidden="true"></i>
                            <span><?= e($item['Title']) ?></span>
                        </a>
                    </li>
                <?php else: ?>
                    <!-- Mục nhóm có cấp con (Collapsible Accordion Group) -->
                    <?php 
                        $collapseId = 'sub-menu-' . (int)$item['MenuID'];
                        $isExpanded = !empty($item['is_active']);
                    ?>
                    <li class="nav-item mb-1">
                        <button class="sidebar-link w-100 border-0 bg-transparent text-start d-flex align-items-center justify-content-between <?= $isExpanded ? 'fw-bold' : '' ?>" 
                                type="button" 
                                data-bs-toggle="collapse" 
                                data-bs-target="#<?= $collapseId ?>" 
                                aria-expanded="<?= $isExpanded ? 'true' : 'false' ?>" 
                                aria-controls="<?= $collapseId ?>">
                            <span class="d-flex align-items-center">
                                <i class="bi <?= e($item['Icon']) ?>" aria-hidden="true"></i>
                                <span><?= e($item['Title']) ?></span>
                            </span>
                            <i class="bi bi-chevron-down fs-8 text-muted transition-transform" aria-hidden="true"></i>
                        </button>
                        
                        <div class="collapse <?= $isExpanded ? 'show' : '' ?>" id="<?= $collapseId ?>">
                            <ul class="sidebar-submenu">
                                <?php foreach ($item['children'] as $child): ?>
                                    <li>
                                        <a class="sidebar-link <?= !empty($child['is_active']) ? 'active' : '' ?>" 
                                           href="<?= url($child['Route']) ?>"
                                           <?= !empty($child['is_active']) ? 'aria-current="page"' : '' ?>>
                                            <i class="bi <?= e($child['Icon']) ?>" aria-hidden="true"></i>
                                            <span><?= e($child['Title']) ?></span>
                                        </a>
                                    </li>
                                <?php endforeach; ?>
                            </ul>
                        </div>
                    </li>
                <?php endif; ?>
            <?php endforeach; ?>
        </ul>
    </div>

    <!-- CHÂN SIDEBAR: THÔNG TIN VAI TRÒ & PHẠM VI NGƯỜI DÙNG -->
    <div class="sidebar-footer">
        <div class="d-flex align-items-center justify-content-between mb-1">
            <span class="text-muted small fw-semibold">Vai trò truy cập:</span>
            <span class="vnm-badge <?= $roleBadgeClass ?>" style="font-size: 0.7rem; padding: 2px 8px;">
                <span class="vnm-badge-dot"></span> <?= e($userRole) ?>
            </span>
        </div>
        <div class="d-flex align-items-center gap-1 text-muted small text-truncate" title="Đơn vị: <?= e($userMaDV) ?>">
            <i class="bi bi-geo-alt-fill text-primary" aria-hidden="true"></i>
            <span class="text-truncate">Đơn vị: <strong><?= e($userMaDV) ?></strong></span>
        </div>
    </div>
</aside>
