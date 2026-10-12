<?php
/**
 * =====================================================================
 * VINAMILK HRM - HEADER TẬP TRUNG (HEADER.PHP)
 * =====================================================================
 * Thành phần:
 *   1. Logo Vinamilk + Tên hệ thống Enterprise HRM
 *   2. Ô tìm nhanh nhân viên (Mã NV / Họ tên / 4 số cuối CCCD) + phím tắt Ctrl+K
 *   3. Bộ chọn cây đơn vị trực thuộc (Unit Scope Selector)
 *   4. Chuông thông báo 3 nhóm (HĐ sắp hết hạn, Đơn chờ duyệt, Chứng chỉ sắp hết hạn)
 *   5. Menu tài khoản người dùng
 *   6. Thanh Breadcrumb đường dẫn trang
 */

require_once __DIR__ . '/../../helpers/NotificationHelper.php';
require_once __DIR__ . '/../../helpers/Auth.php';
require_once __DIR__ . '/../../models/OrgUnitModel.php';

$user = Auth::user();
$userRole = Auth::role();
$notifications = NotificationHelper::getNotifications();
$notifCount = $notifications['total_count'];

// Xây dựng breadcrumb tự động hoặc nhận từ view
$currentUri = parse_url($_SERVER['REQUEST_URI'] ?? '', PHP_URL_PATH);
$pageTitle = $title ?? 'Hệ Thống Quản Lý Nhân Lực';
?>
<!-- HEADER CHÍNH (VINAMILK ROYAL BLUE) -->
<header class="header-vnm no-print">
    <div class="container-fluid px-3 py-2 d-flex align-items-center justify-content-between gap-3">
        <!-- BRAND LOGO & SYSTEM NAME -->
        <div class="d-flex align-items-center gap-2 flex-shrink-0">
            <a class="text-decoration-none d-flex align-items-center gap-2" href="<?= url('dashboard') ?>" aria-label="Vinamilk HRM - Về trang chủ">
                <span class="brand-badge">VINAMILK</span>
                <span class="fw-bold fs-6 tracking-wide text-white d-none d-sm-inline">HRM ENTERPRISE</span>
            </a>
            <span class="badge bg-white bg-opacity-25 text-white fw-semibold px-2 py-1 ms-1 d-none d-xl-inline" style="font-size: 11px;">
                PHP 8.2 MVC
            </span>
        </div>

        <!-- Ô TÌM NHANH NHÂN VIÊN (QUICK SEARCH) -->
        <?php if (Auth::check()): ?>
        <div class="quick-search-wrapper mx-auto d-none d-md-block" role="search">
            <label for="vnmQuickSearch" class="visually-hidden">Tìm nhanh nhân viên theo Mã NV, Họ tên hoặc 4 số cuối CCCD</label>
            <div class="position-relative">
                <i class="bi bi-search quick-search-icon" aria-hidden="true"></i>
                <input type="search" 
                       id="vnmQuickSearch" 
                       class="quick-search-input" 
                       placeholder="Tìm nhanh nhân viên (Mã NV, Tên, 4 số CCCD)..." 
                       autocomplete="off"
                       aria-autocomplete="list"
                       aria-controls="quickSearchResults">
                <kbd class="quick-search-shortcut" title="Phím tắt">Ctrl K</kbd>
                
                <!-- Kết quả tìm kiếm AJAX động -->
                <div id="quickSearchResults" class="quick-search-results" role="listbox" aria-label="Kết quả tìm kiếm nhân sự"></div>
            </div>
        </div>
        <?php endif; ?>

        <!-- KHU VỰC PHẢI: BỘ CHỌN ĐƠN VỊ, CHUÔNG THÔNG BÁO, TÀI KHOẢN -->
        <div class="d-flex align-items-center gap-2 gap-lg-3 flex-shrink-0">
            <?php if (Auth::check()): ?>
                <!-- BỘ CHỌN PHẠM VI CÂY ĐƠN VỊ -->
                <form action="<?= url('dashboard') ?>" method="GET" class="d-none d-lg-flex align-items-center me-1">
                    <label for="headerUnitScopeSelect" class="visually-hidden">Chọn phạm vi cây đơn vị</label>
                    <div class="input-group input-group-sm">
                        <span class="input-group-text bg-white bg-opacity-20 text-white border-0 py-1" id="scope-addon">
                            <i class="bi bi-diagram-3-fill me-1" aria-hidden="true"></i> Đơn vị:
                        </span>
                        <select name="unit_scope" 
                                id="headerUnitScopeSelect" 
                                class="form-select form-select-sm border-0 bg-white py-1" 
                                onchange="this.form.submit()" 
                                aria-describedby="scope-addon"
                                style="max-width: 220px; font-weight: 500; font-size: 0.8125rem;">
                            <?php if (Auth::canViewAllUnits()): ?>
                                <option value="ALL" <?= (($_GET['unit_scope'] ?? '') === 'ALL') ? 'selected' : '' ?>>🏢 TOÀN TẬP ĐOÀN (Tất cả)</option>
                            <?php endif; ?>
                            
                            <?php
                                $orgModel = new OrgUnitModel();
                                $selectable = $orgModel->getSelectableUnitsForUser();
                                $currentScope = $_GET['unit_scope'] ?? (Auth::canViewAllUnits() ? 'ALL' : Auth::maDV());
                                foreach ($selectable as $u):
                            ?>
                                <option value="<?= e($u['MaDV']) ?>" <?= ($currentScope === $u['MaDV']) ? 'selected' : '' ?>>
                                    <?= e($u['IndentTitle'] ?? $u['TenDV']) ?> (<?= e($u['MaDV']) ?>)
                                </option>
                            <?php endforeach; ?>
                        </select>
                    </div>
                </form>

                <!-- CHUÔNG THÔNG BÁO NGHIỆP VỤ -->
                <div class="dropdown">
                    <button class="btn btn-notif position-relative" 
                            type="button" 
                            id="notifDropdownBtn" 
                            data-bs-toggle="dropdown" 
                            data-bs-auto-close="outside"
                            aria-expanded="false" 
                            aria-label="Thông báo hệ thống (<?= $notifCount ?> thông báo cần xử lý)">
                        <i class="bi bi-bell-fill fs-6" aria-hidden="true"></i>
                        <?php if ($notifCount > 0): ?>
                            <span class="notif-badge" aria-hidden="true"><?= $notifCount ?></span>
                        <?php endif; ?>
                    </button>

                    <div class="dropdown-menu dropdown-menu-end notif-dropdown-menu shadow" aria-labelledby="notifDropdownBtn">
                        <div class="notif-header d-flex align-items-center justify-content-between">
                            <div class="fw-bold text-navy" style="font-size: 0.9rem;">
                                <i class="bi bi-bell me-1" aria-hidden="true"></i> Thông báo Nghiệp vụ
                            </div>
                            <span class="vnm-badge <?= $notifCount > 0 ? 'vnm-badge-danger' : 'vnm-badge-neutral' ?>" style="font-size: 0.7rem; padding: 2px 8px;">
                                <?= $notifCount ?> mới
                            </span>
                        </div>

                        <div style="max-height: 420px; overflow-y: auto;">
                            <!-- NHÓM 1: HỢP ĐỒNG SẮP HẾT HẠN (<= 30 NGÀY) -->
                            <div class="notif-category-title d-flex justify-content-between align-items-center">
                                <span><i class="bi bi-file-earmark-text text-warning me-1" aria-hidden="true"></i> Hợp đồng sắp hết hạn (≤30 ngày)</span>
                                <span class="badge bg-secondary-subtle text-secondary" style="font-size: 10px;"><?= count($notifications['contracts']) ?></span>
                            </div>
                            <?php if (empty($notifications['contracts'])): ?>
                                <div class="p-3 text-center text-muted small">Không có hợp đồng nào sắp hết hạn trong 30 ngày tới.</div>
                            <?php else: ?>
                                <?php foreach ($notifications['contracts'] as $c): ?>
                                    <a href="<?= url('employees/show/' . $c['ma_nv']) ?>" class="notif-item">
                                        <div class="notif-item-icon bg-warning bg-opacity-10 text-warning">
                                            <i class="bi bi-file-earmark-person-fill" aria-hidden="true"></i>
                                        </div>
                                        <div class="flex-grow-1 min-w-0">
                                            <div class="fw-semibold text-truncate small"><?= e($c['HoTen']) ?> (<?= e($c['ma_nv']) ?>)</div>
                                            <div class="text-muted" style="font-size: 0.75rem;">HĐ: <?= e($c['so_hd']) ?> &bull; Hết hạn: <?= format_date($c['den_ngay']) ?></div>
                                        </div>
                                        <span class="vnm-badge <?= ($c['days_left'] <= 10) ? 'vnm-badge-danger' : 'vnm-badge-warning' ?>" style="font-size: 0.65rem; padding: 2px 6px;">
                                            Còn <?= (int)$c['days_left'] ?> ngày
                                        </span>
                                    </a>
                                <?php endforeach; ?>
                            <?php endif; ?>

                            <!-- NHÓM 2: ĐƠN TỪ CHỜ PHÊ DUYỆT -->
                            <div class="notif-category-title d-flex justify-content-between align-items-center mt-2">
                                <span><i class="bi bi-envelope-paper text-primary me-1" aria-hidden="true"></i> Đơn từ chờ phê duyệt</span>
                                <span class="badge bg-secondary-subtle text-secondary" style="font-size: 10px;"><?= count($notifications['requests']) ?></span>
                            </div>
                            <?php if (empty($notifications['requests'])): ?>
                                <div class="p-3 text-center text-muted small">Không có đơn từ nào đang chờ phê duyệt.</div>
                            <?php else: ?>
                                <?php foreach ($notifications['requests'] as $req): ?>
                                    <a href="<?= url('approvals') ?>" class="notif-item">
                                        <div class="notif-item-icon bg-primary bg-opacity-10 text-primary">
                                            <i class="bi bi-inbox-fill" aria-hidden="true"></i>
                                        </div>
                                        <div class="flex-grow-1 min-w-0">
                                            <div class="fw-semibold text-truncate small"><?= e($req['tieu_de']) ?></div>
                                            <div class="text-muted" style="font-size: 0.75rem;">Người gửi: <?= e($req['HoTen']) ?> &bull; Mã: <?= e($req['ma_don']) ?></div>
                                        </div>
                                        <span class="vnm-badge vnm-badge-info" style="font-size: 0.65rem; padding: 2px 6px;">
                                            Chờ duyệt
                                        </span>
                                    </a>
                                <?php endforeach; ?>
                            <?php endif; ?>

                            <!-- NHÓM 3: CHỨNG CHỈ & THẺ XANH SẮP HẾT HẠN (<= 45 NGÀY) -->
                            <div class="notif-category-title d-flex justify-content-between align-items-center mt-2">
                                <span><i class="bi bi-award text-danger me-1" aria-hidden="true"></i> Chứng chỉ & Thẻ xanh (≤45 ngày)</span>
                                <span class="badge bg-secondary-subtle text-secondary" style="font-size: 10px;"><?= count($notifications['certificates']) ?></span>
                            </div>
                            <?php if (empty($notifications['certificates'])): ?>
                                <div class="p-3 text-center text-muted small">Tất cả chứng chỉ lao động và an toàn đều còn hạn.</div>
                            <?php else: ?>
                                <?php foreach ($notifications['certificates'] as $cert): ?>
                                    <a href="<?= url('employees/show/' . $cert['ma_nv']) ?>" class="notif-item">
                                        <div class="notif-item-icon bg-danger bg-opacity-10 text-danger">
                                            <i class="bi bi-shield-exclamation" aria-hidden="true"></i>
                                        </div>
                                        <div class="flex-grow-1 min-w-0">
                                            <div class="fw-semibold text-truncate small"><?= e($cert['ten_chung_chi']) ?></div>
                                            <div class="text-muted" style="font-size: 0.75rem;"><?= e($cert['HoTen']) ?> &bull; Hết hạn: <?= format_date($cert['ngay_het_han']) ?></div>
                                        </div>
                                        <span class="vnm-badge <?= ($cert['days_left'] <= 15) ? 'vnm-badge-danger' : 'vnm-badge-warning' ?>" style="font-size: 0.65rem; padding: 2px 6px;">
                                            Còn <?= (int)$cert['days_left'] ?> ngày
                                        </span>
                                    </a>
                                <?php endforeach; ?>
                            <?php endif; ?>
                        </div>

                        <div class="p-2 border-top bg-light text-center">
                            <a href="<?= url('approvals') ?>" class="text-decoration-none small fw-semibold text-navy">
                                Đến Hộp Thư Phê Duyệt <i class="bi bi-arrow-right ms-1" aria-hidden="true"></i>
                            </a>
                        </div>
                    </div>
                </div>

                <!-- MENU TÀI KHOẢN (USER PROFILE DROPDOWN) -->
                <?php
                    $initials = 'VN';
                    if (!empty($user['HoTen'])) {
                        $parts = explode(' ', trim($user['HoTen']));
                        $last = end($parts);
                        $initials = mb_substr($last, 0, 1, 'UTF-8');
                        if (count($parts) > 1) {
                            $initials = mb_substr($parts[0], 0, 1, 'UTF-8') . $initials;
                        }
                    }
                ?>
                <div class="dropdown">
                    <button class="btn p-0 border-0 d-flex align-items-center gap-2 text-white" 
                            type="button" 
                            id="userProfileDropdown" 
                            data-bs-toggle="dropdown" 
                            aria-expanded="false"
                            aria-label="Menu tài khoản của <?= e($user['HoTen'] ?? $user['Username']) ?>">
                        <div class="rounded-circle bg-white text-primary fw-bold d-flex align-items-center justify-content-center shadow-sm" 
                             style="width: 36px; height: 36px; font-size: 0.85rem;" 
                             aria-hidden="true">
                            <?= strtoupper(e($initials)) ?>
                        </div>
                        <div class="text-start d-none d-md-block line-height-sm">
                            <div class="fw-bold text-white small text-truncate" style="max-width: 130px;">
                                <?= e($user['HoTen'] ?: $user['Username']) ?>
                            </div>
                            <div class="text-white-50" style="font-size: 0.7rem;">
                                <?= e($userRole) ?>
                            </div>
                        </div>
                        <i class="bi bi-chevron-down text-white-50 fs-8 d-none d-md-inline" aria-hidden="true"></i>
                    </button>

                    <ul class="dropdown-menu dropdown-menu-end shadow" aria-labelledby="userProfileDropdown" style="min-width: 220px;">
                        <li class="px-3 py-2 border-bottom bg-light">
                            <div class="fw-bold text-truncate"><?= e($user['HoTen'] ?: $user['Username']) ?></div>
                            <div class="text-muted small text-truncate">@<?= e($user['Username']) ?> (<?= e($user['MaNV'] ?: 'Tập đoàn') ?>)</div>
                            <span class="vnm-badge vnm-badge-info mt-1" style="font-size: 0.7rem; padding: 2px 6px;">
                                <span class="vnm-badge-dot"></span> <?= e($userRole) ?>
                            </span>
                        </li>
                        <li>
                            <a class="dropdown-item py-2" href="<?= url('employees/show/' . ($user['MaNV'] ?: '')) ?>">
                                <i class="bi bi-person-badge me-2 text-primary" aria-hidden="true"></i> Hồ sơ của tôi (ESS)
                            </a>
                        </li>
                        <li>
                            <a class="dropdown-item py-2" href="<?= url('approvals') ?>">
                                <i class="bi bi-envelope-paper me-2 text-primary" aria-hidden="true"></i> Hộp thư chờ duyệt
                            </a>
                        </li>
                        <li>
                            <a class="dropdown-item py-2" href="<?= url('kitchen-sink') ?>">
                                <i class="bi bi-palette-fill me-2 text-primary" aria-hidden="true"></i> Kitchen Sink Demo
                            </a>
                        </li>
                        <li><hr class="dropdown-divider"></li>
                        <li>
                            <a class="dropdown-item py-2 text-danger fw-semibold" href="<?= url('auth/logout') ?>">
                                <i class="bi bi-box-arrow-right me-2" aria-hidden="true"></i> Đăng xuất
                            </a>
                        </li>
                    </ul>
                </div>

            <?php else: ?>
                <a href="<?= url('auth/login') ?>" class="btn btn-light btn-sm fw-bold text-primary px-3">
                    <i class="bi bi-box-arrow-in-right me-1" aria-hidden="true"></i> Đăng nhập
                </a>
            <?php endif; ?>
        </div>
    </div>
</header>

<!-- THANH BREADCRUMB (ĐƯỜNG DẪN TRANG) -->
<nav class="breadcrumb-bar no-print" aria-label="Đường dẫn trang">
    <div class="container-fluid px-3 d-flex align-items-center justify-content-between">
        <ol class="breadcrumb-vnm">
            <li class="breadcrumb-item">
                <a href="<?= url('dashboard') ?>">
                    <i class="bi bi-house-door-fill me-1" aria-hidden="true"></i> Trang chủ
                </a>
            </li>
            <?php if (isset($breadcrumbs) && is_array($breadcrumbs)): ?>
                <?php foreach ($breadcrumbs as $crumb): ?>
                    <?php if (!empty($crumb['url'])): ?>
                        <li class="breadcrumb-item">
                            <a href="<?= url($crumb['url']) ?>"><?= e($crumb['title']) ?></a>
                        </li>
                    <?php else: ?>
                        <li class="breadcrumb-item active" aria-current="page">
                            <?= e($crumb['title']) ?>
                        </li>
                    <?php endif; ?>
                <?php endforeach; ?>
            <?php else: ?>
                <li class="breadcrumb-item active" aria-current="page">
                    <?= e($pageTitle) ?>
                </li>
            <?php endif; ?>
        </ol>

        <div class="d-none d-sm-block text-muted small">
            <i class="bi bi-clock me-1" aria-hidden="true"></i> Hôm nay: <?= date('d/m/Y') ?>
        </div>
    </div>
</nav>
