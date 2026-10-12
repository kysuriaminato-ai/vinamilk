<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta name="description" content="Vinamilk HRM - Hệ Thống Quản Lý Nguồn Nhân Lực Doanh Nghiệp">
    <title><?= e($title ?? 'Vinamilk Enterprise HRM') ?></title>

    <!-- Google Fonts: Be Vietnam Pro (WCAG AA Compliant Typography) -->
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Be+Vietnam+Pro:ital,wght@0,300;0,400;0,500;0,600;0,700;0,800;1,400&display=swap" rel="stylesheet">

    <!-- Bootstrap 5.3 CSS -->
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet">
    <!-- Bootstrap Icons 1.11.3 -->
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet">

    <!-- Vinamilk HRM Design System Tokens & Stylesheet -->
    <link rel="stylesheet" href="<?= asset('css/vinamilk.css') ?>?v=<?= time() ?>">

    <!-- Chart.js 4 -->
    <script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.2/dist/chart.umd.min.js"></script>
</head>
<body>
    <!-- WCAG AA SKIP LINK -->
    <a href="#main-content" class="skip-link">Chuyển đến nội dung chính</a>

    <!-- TOP HEADER (Logo, Quick Search, Unit Scope, Notifications, User Menu, Breadcrumbs) -->
    <?php require __DIR__ . '/header.php'; ?>

    <!-- MAIN WRAPPER (Sidebar + Dynamic View Content) -->
    <div class="d-flex flex-grow-1">
        <?php if (Auth::check()): ?>
            <!-- SIDEBAR THEO PHÂN QUYỀN (Sinh tự động từ menu_items + role_permissions) -->
            <?php require __DIR__ . '/sidebar.php'; ?>
        <?php endif; ?>

        <!-- MAIN CONTENT CONTAINER -->
        <main id="main-content" class="content-wrapper flex-grow-1 p-3 p-md-4" tabindex="-1">
            <!-- FLASH ALERT MESSAGES -->
            <?php 
                $successMsg = Session::getFlash('success');
                $errorMsg   = Session::getFlash('error');
                $warningMsg = Session::getFlash('warning');
                $infoMsg    = Session::getFlash('info');
            ?>

            <?php if ($successMsg): ?>
                <div class="alert alert-success alert-dismissible fade show shadow-sm no-print mb-4 border-start border-4 border-success" role="alert">
                    <div class="d-flex align-items-center">
                        <i class="bi bi-check-circle-fill me-2 fs-5 text-success" aria-hidden="true"></i>
                        <div>
                            <strong>Thành công!</strong> <?= e($successMsg['message']) ?>
                        </div>
                    </div>
                    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Đóng thông báo thành công"></button>
                </div>
            <?php endif; ?>

            <?php if ($errorMsg): ?>
                <div class="alert alert-danger alert-dismissible fade show shadow-sm no-print mb-4 border-start border-4 border-danger" role="alert">
                    <div class="d-flex align-items-center">
                        <i class="bi bi-exclamation-octagon-fill me-2 fs-5 text-danger" aria-hidden="true"></i>
                        <div>
                            <strong>Lỗi:</strong> <?= e($errorMsg['message']) ?>
                        </div>
                    </div>
                    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Đóng thông báo lỗi"></button>
                </div>
            <?php endif; ?>

            <?php if ($warningMsg): ?>
                <div class="alert alert-warning alert-dismissible fade show shadow-sm no-print mb-4 border-start border-4 border-warning" role="alert">
                    <div class="d-flex align-items-center">
                        <i class="bi bi-exclamation-triangle-fill me-2 fs-5 text-warning" aria-hidden="true"></i>
                        <div>
                            <strong>Cảnh báo:</strong> <?= e($warningMsg['message']) ?>
                        </div>
                    </div>
                    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Đóng thông báo cảnh báo"></button>
                </div>
            <?php endif; ?>

            <?php if ($infoMsg): ?>
                <div class="alert alert-info alert-dismissible fade show shadow-sm no-print mb-4 border-start border-4 border-info" role="alert">
                    <div class="d-flex align-items-center">
                        <i class="bi bi-info-circle-fill me-2 fs-5 text-info" aria-hidden="true"></i>
                        <div><?= e($infoMsg['message']) ?></div>
                    </div>
                    <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Đóng thông báo"></button>
                </div>
            <?php endif; ?>

            <!-- VIEW CONTENT NẠP TỪ CONTROLLER -->
            <?= $content ?? '' ?>
        </main>
    </div>

    <!-- MODAL XÁC NHẬN DÙNG CHUNG TOÀN HỆ THỐNG (CONFIRMATION MODAL) -->
    <div class="modal fade" id="vnmGlobalConfirmModal" tabindex="-1" aria-labelledby="vnmConfirmModalLabel" aria-hidden="true" role="dialog">
        <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content vnm-modal-content">
                <div class="modal-body p-4 text-center">
                    <div id="vnmConfirmIconContainer" class="vnm-modal-icon mx-auto bg-danger-subtle text-danger">
                        <i class="bi bi-exclamation-triangle-fill" id="vnmConfirmIcon" aria-hidden="true"></i>
                    </div>
                    <h5 class="fw-bold mb-2" id="vnmConfirmModalLabel">Xác nhận hành động</h5>
                    <p class="text-muted small mb-4" id="vnmConfirmMessage">Bạn có chắc chắn muốn thực hiện hành động này không?</p>
                    <div class="d-flex justify-content-center gap-2">
                        <button type="button" class="btn btn-outline-secondary px-4 fw-semibold" data-bs-dismiss="modal">
                            Hủy bỏ
                        </button>
                        <button type="button" class="btn btn-danger px-4 fw-semibold" id="vnmConfirmSubmitBtn">
                            Đồng ý thực hiện
                        </button>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- Bootstrap 5.3 JS Bundle -->
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>

    <!-- SCRIPT TIỆN ÍCH DÙNG CHUNG (TÌM NHANH NHÂN SỰ & CONFIRM MODAL) -->
    <script>
        document.addEventListener('DOMContentLoaded', function () {
            // 1. Phím tắt Ctrl+K để nhảy nhanh vào ô tìm kiếm nhân sự
            const searchInput = document.getElementById('vnmQuickSearch');
            const searchResults = document.getElementById('quickSearchResults');

            if (searchInput && searchResults) {
                document.addEventListener('keydown', function (e) {
                    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
                        e.preventDefault();
                        searchInput.focus();
                        searchInput.select();
                    }
                    if (e.key === 'Escape') {
                        searchResults.classList.remove('show');
                    }
                });

                let debounceTimer = null;
                searchInput.addEventListener('input', function () {
                    const query = this.value.trim();
                    clearTimeout(debounceTimer);

                    if (query.length < 1) {
                        searchResults.innerHTML = '';
                        searchResults.classList.remove('show');
                        return;
                    }

                    debounceTimer = setTimeout(function () {
                        fetch('<?= url("employees/quick_search") ?>?q=' + encodeURIComponent(query))
                            .then(res => res.json())
                            .then(data => {
                                if (!Array.isArray(data) || data.length === 0) {
                                    searchResults.innerHTML = '<div class="p-3 text-center text-muted small"><i class="bi bi-person-x me-1"></i> Không tìm thấy nhân sự phù hợp</div>';
                                    searchResults.classList.add('show');
                                    return;
                                }

                                let html = '';
                                data.forEach(emp => {
                                    const cccdNotice = emp.Last4CCCD ? ` &bull; CCCD: ***${emp.Last4CCCD}` : '';
                                    html += `
                                        <a href="<?= url("employees/show") ?>/${encodeURIComponent(emp.MaNV)}" class="quick-search-item">
                                            <div class="rounded-circle bg-primary bg-opacity-10 text-primary fw-bold d-flex align-items-center justify-content-center" style="width: 32px; height: 32px; font-size: 0.75rem; flex-shrink: 0;">
                                                <i class="bi bi-person-fill"></i>
                                            </div>
                                            <div class="flex-grow-1 min-w-0">
                                                <div class="fw-semibold text-truncate small">${emp.HoTen} <span class="badge bg-light text-primary border">${emp.MaNV}</span></div>
                                                <div class="text-muted text-truncate" style="font-size: 0.75rem;">${emp.TenCV || emp.ChucVu || 'Nhân viên'} &bull; ${emp.TenDV || emp.MaDV || 'Vinamilk'}${cccdNotice}</div>
                                            </div>
                                        </a>
                                    `;
                                });
                                searchResults.innerHTML = html;
                                searchResults.classList.add('show');
                            })
                            .catch(err => {
                                console.error('Lỗi tìm nhanh nhân sự:', err);
                            });
                    }, 250);
                });

                // Đóng kết quả khi click ra ngoài
                document.addEventListener('click', function (e) {
                    if (!searchInput.contains(e.target) && !searchResults.contains(e.target)) {
                        searchResults.classList.remove('show');
                    }
                });
            }

            // 2. Global Confirmation Modal Helper
            window.vnmConfirm = function (options) {
                const title = options.title || 'Xác nhận hành động';
                const message = options.message || 'Bạn có chắc chắn muốn thực hiện hành động này không?';
                const confirmText = options.confirmText || 'Đồng ý';
                const isDanger = options.isDanger !== false;
                const onConfirm = options.onConfirm || function () {};

                document.getElementById('vnmConfirmModalLabel').textContent = title;
                document.getElementById('vnmConfirmMessage').textContent = message;

                const submitBtn = document.getElementById('vnmConfirmSubmitBtn');
                submitBtn.textContent = confirmText;
                submitBtn.className = isDanger ? 'btn btn-danger px-4 fw-semibold' : 'btn btn-vnm-primary px-4 fw-semibold';

                const iconContainer = document.getElementById('vnmConfirmIconContainer');
                iconContainer.className = isDanger 
                    ? 'vnm-modal-icon mx-auto bg-danger-subtle text-danger' 
                    : 'vnm-modal-icon mx-auto bg-primary-subtle text-primary';

                const modalEl = document.getElementById('vnmGlobalConfirmModal');
                const modal = new bootstrap.Modal(modalEl);

                // Clone button to clear old event listeners
                const newBtn = submitBtn.cloneNode(true);
                submitBtn.parentNode.replaceChild(newBtn, submitBtn);

                newBtn.addEventListener('click', function () {
                    modal.hide();
                    onConfirm();
                });

                modal.show();
            };
        });
    </script>
</body>
</html>
