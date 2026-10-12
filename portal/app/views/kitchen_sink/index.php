<?php
/**
 * =====================================================================
 * VINAMILK HRM - TRANG MẪU KITCHEN SINK (TOÀN BỘ THÀNH PHẦN GIAO DIỆN)
 * =====================================================================
 * Trưng bày trực quan:
 *   1. Design tokens (Màu sắc #0033A0, Typography Be Vietnam Pro, Khoảng cách 8px)
 *   2. Thẻ KPI (KPI Metric Cards)
 *   3. Bảng dữ liệu có lọc, sắp xếp, phân trang phía server
 *   4. Badge trạng thái đạt chuẩn tương phản WCAG AA
 *   5. Modal xác nhận tương tác
 *   6. Trạng thái rỗng (Empty State)
 *   7. Các điều khiển form với nhãn rõ ràng cho mọi input (WCAG AA)
 */
?>
<div class="container-fluid px-0">
    <!-- TIÊU ĐỀ TRANG & MÔ TẢ HỆ THỐNG DESIGN SYSTEM -->
    <div class="d-flex flex-wrap align-items-center justify-content-between gap-3 mb-4">
        <div>
            <h1 class="h3 fw-bold text-navy mb-1" style="color: var(--vnm-primary);">
                <i class="bi bi-palette-fill me-2" aria-hidden="true"></i> UI Kitchen Sink - Vinamilk Design System
            </h1>
            <p class="text-muted small mb-0">
                Bộ sưu tập các thành phần giao diện tiêu chuẩn cho hệ thống Quản trị Nguồn Nhân Lực Vinamilk. Đạt chuẩn tiếp cận <strong>WCAG AA</strong>.
            </p>
        </div>
        <div class="d-flex gap-2">
            <button type="button" class="btn btn-vnm-outline btn-sm d-flex align-items-center gap-1" onclick="window.print()">
                <i class="bi bi-printer" aria-hidden="true"></i> In Trang (A4)
            </button>
            <a href="<?= url('dashboard') ?>" class="btn btn-vnm-primary btn-sm d-flex align-items-center gap-1">
                <i class="bi bi-speedometer2" aria-hidden="true"></i> Đến Bảng Điều Khiển
            </a>
        </div>
    </div>

    <!-- PHẦN 1: THẺ KPI (KPI METRIC CARDS) -->
    <section class="mb-5" aria-labelledby="kpi-section-title">
        <div class="d-flex align-items-center justify-content-between mb-3">
            <h2 class="h5 fw-bold mb-0 text-dark" id="kpi-section-title">
                <i class="bi bi-graph-up-arrow text-primary me-2" aria-hidden="true"></i> 1. Thẻ Chỉ Số Đo Lường (KPI Cards)
            </h2>
            <span class="text-muted small">Quy chuẩn lưới 4 cột, khoảng cách 16px - 24px</span>
        </div>

        <div class="row g-3 g-lg-4">
            <?php foreach ($kpis as $kpi): ?>
                <div class="col-12 col-sm-6 col-xl-3">
                    <div class="vnm-kpi-card h-100 d-flex flex-column justify-content-between">
                        <div>
                            <div class="d-flex align-items-start justify-content-between">
                                <div class="vnm-kpi-title"><?= e($kpi['title']) ?></div>
                                <div class="vnm-kpi-icon-badge <?= e($kpi['bg_tint']) ?>">
                                    <i class="bi <?= e($kpi['icon']) ?>" aria-hidden="true"></i>
                                </div>
                            </div>
                            <div class="d-flex align-items-baseline gap-2">
                                <span class="vnm-kpi-value"><?= e($kpi['value']) ?></span>
                                <span class="text-muted small fw-semibold"><?= e($kpi['unit']) ?></span>
                            </div>
                        </div>

                        <div class="pt-2 border-top mt-2">
                            <?php if ($kpi['trend_type'] === 'up'): ?>
                                <span class="vnm-kpi-trend text-success">
                                    <i class="bi bi-arrow-up-right fw-bold" aria-hidden="true"></i> <?= e($kpi['trend']) ?>
                                </span>
                            <?php elseif ($kpi['trend_type'] === 'down'): ?>
                                <span class="vnm-kpi-trend text-warning">
                                    <i class="bi bi-arrow-down-right fw-bold" aria-hidden="true"></i> <?= e($kpi['trend']) ?>
                                </span>
                            <?php else: ?>
                                <span class="vnm-kpi-trend text-danger">
                                    <i class="bi bi-exclamation-circle-fill" aria-hidden="true"></i> <?= e($kpi['trend']) ?>
                                </span>
                            <?php endif; ?>
                            <span class="text-muted small ms-1"><?= e($kpi['trend_text']) ?></span>
                        </div>
                    </div>
                </div>
            <?php endforeach; ?>
        </div>
    </section>

    <!-- PHẦN 2: BẢNG DỮ LIỆU CÓ LỌC, SẮP XẾP, PHÂN TRANG PHÍA SERVER -->
    <section class="mb-5" aria-labelledby="table-section-title">
        <div class="d-flex align-items-center justify-content-between mb-3">
            <h2 class="h5 fw-bold mb-0 text-dark" id="table-section-title">
                <i class="bi bi-table text-primary me-2" aria-hidden="true"></i> 2. Bảng Dữ Liệu Phía Server (Data Table)
            </h2>
            <span class="text-muted small">Hỗ trợ lọc từ khóa, trạng thái, sắp xếp cột & phân trang</span>
        </div>

        <div class="vnm-table-container">
            <!-- THANH CÔNG CỤ LỌC & TÌM KIẾM -->
            <form action="<?= url('kitchen-sink') ?>" method="GET" class="vnm-table-toolbar">
                <div class="d-flex flex-wrap align-items-center gap-2 flex-grow-1">
                    <!-- Ô tìm kiếm từ khóa -->
                    <div style="min-width: 260px; max-width: 380px;" class="flex-grow-1">
                        <label for="tableSearchInput" class="visually-hidden">Tìm kiếm nhân sự</label>
                        <div class="input-group input-group-sm">
                            <span class="input-group-text bg-white border-end-0">
                                <i class="bi bi-search text-muted" aria-hidden="true"></i>
                            </span>
                            <input type="search" 
                                   id="tableSearchInput" 
                                   name="keyword" 
                                   value="<?= e($tableData['keyword']) ?>" 
                                   class="form-control form-control-sm border-start-0" 
                                   placeholder="Tìm theo Mã NV, Họ tên, CCCD...">
                        </div>
                    </div>

                    <!-- Bộ lọc trạng thái -->
                    <div style="min-width: 180px;">
                        <label for="tableStatusSelect" class="visually-hidden">Lọc theo trạng thái làm việc</label>
                        <select name="status" id="tableStatusSelect" class="form-select form-select-sm" onchange="this.form.submit()">
                            <option value="">Tất cả trạng thái</option>
                            <option value="1" <?= ($tableData['status'] === '1') ? 'selected' : '' ?>>Đang làm việc</option>
                            <option value="0" <?= ($tableData['status'] === '0') ? 'selected' : '' ?>>Đã thôi việc / Tạm hoãn</option>
                        </select>
                    </div>

                    <!-- Nút tìm kiếm & xóa bộ lọc -->
                    <button type="submit" class="btn btn-vnm-primary btn-sm px-3">
                        <i class="bi bi-funnel me-1" aria-hidden="true"></i> Lọc
                    </button>
                    <?php if (!empty($tableData['keyword']) || $tableData['status'] !== ''): ?>
                        <a href="<?= url('kitchen-sink') ?>" class="btn btn-outline-secondary btn-sm">
                            <i class="bi bi-x-circle me-1" aria-hidden="true"></i> Xóa lọc
                        </a>
                    <?php endif; ?>
                </div>

                <!-- Thao tác xuất khẩu hoặc thêm mới -->
                <div class="d-flex gap-2">
                    <button type="button" class="btn btn-outline-success btn-sm d-flex align-items-center gap-1" onclick="alert('Đã xuất báo cáo nhân sự theo bộ lọc!');">
                        <i class="bi bi-file-earmark-excel me-1" aria-hidden="true"></i> Xuất Excel
                    </button>
                    <a href="<?= url('employees/create') ?>" class="btn btn-vnm-primary btn-sm d-flex align-items-center gap-1">
                        <i class="bi bi-plus-circle me-1" aria-hidden="true"></i> Tiếp Nhận Mới
                    </a>
                </div>
            </form>

            <!-- BẢNG DỮ LIỆU CHÍNH -->
            <div class="table-responsive">
                <table class="table vnm-table" aria-label="Bảng danh sách nhân viên Vinamilk">
                    <thead>
                        <tr>
                            <th scope="col" style="width: 120px;">
                                <a href="<?= url('kitchen-sink') ?>?sort=MaNV&order=<?= ($tableData['sort'] === 'MaNV' && $tableData['order'] === 'ASC') ? 'desc' : 'asc' ?>&keyword=<?= urlencode($tableData['keyword']) ?>&status=<?= urlencode($tableData['status']) ?>" class="sort-link">
                                    Mã NV
                                    <i class="bi <?= ($tableData['sort'] === 'MaNV') ? (($tableData['order'] === 'ASC') ? 'bi-sort-up text-primary' : 'bi-sort-down text-primary') : 'bi-arrow-down-up opacity-50' ?>" aria-hidden="true"></i>
                                </a>
                            </th>
                            <th scope="col">
                                <a href="<?= url('kitchen-sink') ?>?sort=HoTen&order=<?= ($tableData['sort'] === 'HoTen' && $tableData['order'] === 'ASC') ? 'desc' : 'asc' ?>&keyword=<?= urlencode($tableData['keyword']) ?>&status=<?= urlencode($tableData['status']) ?>" class="sort-link">
                                    Họ và Tên
                                    <i class="bi <?= ($tableData['sort'] === 'HoTen') ? (($tableData['order'] === 'ASC') ? 'bi-sort-up text-primary' : 'bi-sort-down text-primary') : 'bi-arrow-down-up opacity-50' ?>" aria-hidden="true"></i>
                                </a>
                            </th>
                            <th scope="col">Đơn Vị Trực Thuộc</th>
                            <th scope="col">Chức Danh / Vị Trí</th>
                            <th scope="col" style="width: 140px;" class="text-end">
                                <a href="<?= url('kitchen-sink') ?>?sort=LuongBaoHiem&order=<?= ($tableData['sort'] === 'LuongBaoHiem' && $tableData['order'] === 'ASC') ? 'desc' : 'asc' ?>&keyword=<?= urlencode($tableData['keyword']) ?>&status=<?= urlencode($tableData['status']) ?>" class="sort-link justify-content-end">
                                    Lương Đóng BH
                                    <i class="bi <?= ($tableData['sort'] === 'LuongBaoHiem') ? (($tableData['order'] === 'ASC') ? 'bi-sort-up text-primary' : 'bi-sort-down text-primary') : 'bi-arrow-down-up opacity-50' ?>" aria-hidden="true"></i>
                                </a>
                            </th>
                            <th scope="col" style="width: 150px;" class="text-center">Trạng Thái</th>
                            <th scope="col" style="width: 100px;" class="text-center">Hành Động</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php if (empty($tableData['items'])): ?>
                            <tr>
                                <td colspan="7" class="p-0 border-0">
                                    <div class="vnm-empty-state">
                                        <div class="vnm-empty-icon">
                                            <i class="bi bi-search" aria-hidden="true"></i>
                                        </div>
                                        <div class="vnm-empty-title">Không tìm thấy dữ liệu phù hợp</div>
                                        <div class="vnm-empty-desc">
                                            Không có nhân sự nào khớp với điều kiện tìm kiếm hoặc phân quyền của bạn. Thử xóa bộ lọc để hiển thị lại.
                                        </div>
                                        <a href="<?= url('kitchen-sink') ?>" class="btn btn-vnm-primary btn-sm">
                                            <i class="bi bi-arrow-counterclockwise me-1" aria-hidden="true"></i> Tải lại danh sách
                                        </a>
                                    </div>
                                </td>
                            </tr>
                        <?php else: ?>
                            <?php foreach ($tableData['items'] as $item): ?>
                                <tr>
                                    <td class="fw-bold">
                                        <a href="<?= url('employees/show/' . $item['MaNV']) ?>" class="text-navy text-decoration-none">
                                            <?= e($item['MaNV']) ?>
                                        </a>
                                    </td>
                                    <td>
                                        <div class="fw-semibold"><?= e($item['HoTen']) ?></div>
                                        <div class="text-muted" style="font-size: 0.75rem;">
                                            CCCD: <?= e(mask_id_number($item['SoCMND_CCCD'] ?? '')) ?> &bull; <?= e($item['GioiTinh'] ?? 'Nam') ?>
                                        </div>
                                    </td>
                                    <td>
                                        <div><?= e($item['TenDV'] ?? $item['MaDV']) ?></div>
                                        <div class="text-muted small" style="font-size: 0.75rem;"><?= e($item['MaDV']) ?></div>
                                    </td>
                                    <td>
                                        <span><?= e($item['TenCV'] ?? $item['ChucVu'] ?? 'Nhân viên') ?></span>
                                    </td>
                                    <td class="text-end fw-semibold">
                                        <?= format_money($item['LuongBaoHiem'] ?? 0) ?>
                                    </td>
                                    <td class="text-center">
                                        <?php if ((int)$item['TrangThai'] === 1): ?>
                                            <span class="vnm-badge vnm-badge-success">
                                                <span class="vnm-badge-dot"></span> Đang làm việc
                                            </span>
                                        <?php else: ?>
                                            <span class="vnm-badge vnm-badge-danger">
                                                <span class="vnm-badge-dot"></span> Đã thôi việc
                                            </span>
                                        <?php endif; ?>
                                    </td>
                                    <td class="text-center">
                                        <div class="btn-group btn-group-sm">
                                            <a href="<?= url('employees/show/' . $item['MaNV']) ?>" 
                                               class="btn btn-outline-primary" 
                                               title="Xem chi tiết hồ sơ" 
                                               aria-label="Xem chi tiết hồ sơ <?= e($item['HoTen']) ?>">
                                                <i class="bi bi-eye" aria-hidden="true"></i>
                                            </a>
                                            <button type="button" 
                                                    class="btn btn-outline-danger" 
                                                    title="Thử nghiệm modal xác nhận" 
                                                    aria-label="Thử xóa hồ sơ <?= e($item['HoTen']) ?>"
                                                    onclick="triggerDemoConfirm('<?= e($item['MaNV']) ?>', '<?= e($item['HoTen']) ?>')">
                                                <i class="bi bi-trash3" aria-hidden="true"></i>
                                            </button>
                                        </div>
                                    </td>
                                </tr>
                            <?php endforeach; ?>
                        <?php endif; ?>
                    </tbody>
                </table>
            </div>

            <!-- CHÂN TRANG PHÂN TRANG (SERVER-SIDE PAGINATION) -->
            <?php if (!empty($tableData['items'])): ?>
                <div class="vnm-table-footer">
                    <div class="text-muted small">
                        Hiển thị <strong><?= min($tableData['total'], ($tableData['page'] - 1) * $tableData['limit'] + 1) ?></strong> - 
                        <strong><?= min($tableData['total'], $tableData['page'] * $tableData['limit']) ?></strong> 
                        trên tổng số <strong><?= number_format($tableData['total']) ?></strong> bản ghi
                    </div>

                    <nav aria-label="Phân trang bảng dữ liệu">
                        <ul class="pagination pagination-sm mb-0">
                            <!-- Nút Trang trước -->
                            <li class="page-item <?= ($tableData['page'] <= 1) ? 'disabled' : '' ?>">
                                <a class="page-link" 
                                   href="<?= url('kitchen-sink') ?>?page=<?= max(1, $tableData['page'] - 1) ?>&keyword=<?= urlencode($tableData['keyword']) ?>&status=<?= urlencode($tableData['status']) ?>&sort=<?= $tableData['sort'] ?>&order=<?= $tableData['order'] ?>" 
                                   aria-label="Trang trước">
                                    <i class="bi bi-chevron-left" aria-hidden="true"></i>
                                </a>
                            </li>

                            <!-- Các số trang -->
                            <?php for ($p = 1; $p <= $tableData['total_pages']; $p++): ?>
                                <li class="page-item <?= ($p === $tableData['page']) ? 'active' : '' ?>">
                                    <a class="page-link" 
                                       href="<?= url('kitchen-sink') ?>?page=<?= $p ?>&keyword=<?= urlencode($tableData['keyword']) ?>&status=<?= urlencode($tableData['status']) ?>&sort=<?= $tableData['sort'] ?>&order=<?= $tableData['order'] ?>">
                                        <?= $p ?>
                                    </a>
                                </li>
                            <?php endfor; ?>

                            <!-- Nút Trang sau -->
                            <li class="page-item <?= ($tableData['page'] >= $tableData['total_pages']) ? 'disabled' : '' ?>">
                                <a class="page-link" 
                                   href="<?= url('kitchen-sink') ?>?page=<?= min($tableData['total_pages'], $tableData['page'] + 1) ?>&keyword=<?= urlencode($tableData['keyword']) ?>&status=<?= urlencode($tableData['status']) ?>&sort=<?= $tableData['sort'] ?>&order=<?= $tableData['order'] ?>" 
                                   aria-label="Trang sau">
                                    <i class="bi bi-chevron-right" aria-hidden="true"></i>
                                </a>
                            </li>
                        </ul>
                    </nav>
                </div>
            <?php endif; ?>
        </div>
    </section>

    <!-- PHẦN 3: BADGES TRẠNG THÁI & MODAL XÁC NHẬN & EMPTY STATE -->
    <div class="row g-4 mb-5">
        <!-- 3A. BADGE TRẠNG THÁI ĐẠT CHUẨN TƯƠNG PHẢN WCAG AA -->
        <div class="col-12 col-lg-6">
            <div class="card-vnm p-4 h-100">
                <h3 class="h6 fw-bold text-dark mb-3">
                    <i class="bi bi-tag-fill text-primary me-2" aria-hidden="true"></i> 3. Hệ Thống Badge Trạng Thái (WCAG AA)
                </h3>
                <p class="text-muted small mb-3">
                    Được thiết kế với tỷ lệ tương phản màu chữ và nền pastel đạt tiêu chuẩn tối thiểu 4.5:1.
                </p>

                <div class="d-flex flex-wrap gap-2 mb-3">
                    <span class="vnm-badge vnm-badge-success">
                        <span class="vnm-badge-dot"></span> Đã Phê Duyệt / Có Hiệu Lực
                    </span>
                    <span class="vnm-badge vnm-badge-warning">
                        <span class="vnm-badge-dot"></span> Chờ Duyệt / Thử Việc
                    </span>
                    <span class="vnm-badge vnm-badge-danger">
                        <span class="vnm-badge-dot"></span> Từ Chối / Đã Hết Hạn
                    </span>
                    <span class="vnm-badge vnm-badge-info">
                        <span class="vnm-badge-dot"></span> Thông Tin / Đang Xử Lý
                    </span>
                    <span class="vnm-badge vnm-badge-neutral">
                        <span class="vnm-badge-dot"></span> Bản Nháp / Không Hoạt Động
                    </span>
                </div>

                <div class="p-3 bg-light rounded-3 small">
                    <div class="fw-bold mb-1">Kiểm tra thông số tương phản:</div>
                    <ul class="mb-0 ps-3 text-muted">
                        <li>Thành công (Emerald): Chữ <code>#065F46</code> trên nền <code>#ECFDF5</code> (Tương phản 6.8:1 - Đạt AA)</li>
                        <li>Cảnh báo (Amber): Chữ <code>#92400E</code> trên nền <code>#FFFBEB</code> (Tương phản 5.3:1 - Đạt AA)</li>
                        <li>Lỗi/Nguy hiểm (Red): Chữ <code>#991B1B</code> trên nền <code>#FEF2F2</code> (Tương phản 6.9:1 - Đạt AA)</li>
                    </ul>
                </div>
            </div>
        </div>

        <!-- 3B. MODAL XÁC NHẬN TƯƠNG TÁC (CONFIRMATION MODAL) -->
        <div class="col-12 col-lg-6">
            <div class="card-vnm p-4 h-100">
                <h3 class="h6 fw-bold text-dark mb-3">
                    <i class="bi bi-shield-check text-primary me-2" aria-hidden="true"></i> 4. Modal Xác Nhận Dùng Chung (Confirmation Modal)
                </h3>
                <p class="text-muted small mb-3">
                    Hộp thoại xác nhận hành động nguy hiểm hoặc phê duyệt với điều khiển focus bàn phím rõ ràng.
                </p>

                <div class="d-flex flex-wrap gap-2 mb-4">
                    <button type="button" class="btn btn-outline-danger btn-sm px-3" onclick="testDangerModal()">
                        <i class="bi bi-trash3 me-1" aria-hidden="true"></i> Mở Modal Xóa (Danger)
                    </button>
                    <button type="button" class="btn btn-vnm-primary btn-sm px-3" onclick="testApproveModal()">
                        <i class="bi bi-check2-circle me-1" aria-hidden="true"></i> Mở Modal Duyệt (Success)
                    </button>
                </div>

                <div class="p-3 bg-light rounded-3 small">
                    <div class="fw-bold mb-1">Cách gọi từ JavaScript:</div>
                    <pre class="mb-0 text-dark"><code>window.vnmConfirm({
    title: 'Xác nhận phê duyệt',
    message: 'Bạn có chắc chắn muốn duyệt tờ trình này?',
    confirmText: 'Phê duyệt ngay',
    isDanger: false,
    onConfirm: function() { console.log('Đã duyệt!'); }
});</code></pre>
                </div>
            </div>
        </div>
    </div>

    <!-- PHẦN 4: THÀNH PHẦN EMPTY STATE & BẢN ĐỒ DESIGN TOKENS -->
    <div class="row g-4 mb-4">
        <!-- 4A. EMPTY STATE DEMO -->
        <div class="col-12 col-lg-5">
            <div class="card-vnm p-4 h-100">
                <h3 class="h6 fw-bold text-dark mb-2">
                    <i class="bi bi-inbox text-primary me-2" aria-hidden="true"></i> 5. Trạng Thái Rỗng (Empty State)
                </h3>
                <div class="vnm-empty-state py-4">
                    <div class="vnm-empty-icon" style="width: 64px; height: 64px; font-size: 2rem;">
                        <i class="bi bi-calendar2-x" aria-hidden="true"></i>
                    </div>
                    <div class="vnm-empty-title fs-6">Chưa có dữ liệu bảng lương tháng này</div>
                    <div class="vnm-empty-desc small">
                        Kỳ lương hiện tại chưa được mở hoặc chưa có nhân sự nào được phân ca tính công.
                    </div>
                    <button type="button" class="btn btn-vnm-primary btn-sm" onclick="alert('Mở wizard tạo kỳ lương mới!')">
                        <i class="bi bi-plus-lg me-1" aria-hidden="true"></i> Khởi Tạo Kỳ Lương Mới
                    </button>
                </div>
            </div>
        </div>

        <!-- 4B. DESIGN TOKENS BẢNG MÀU & KHOẢNG CÁCH 8PX -->
        <div class="col-12 col-lg-7">
            <div class="card-vnm p-4 h-100">
                <h3 class="h6 fw-bold text-dark mb-2">
                    <i class="bi bi-grid-3x3 text-primary me-2" aria-hidden="true"></i> 6. Design Tokens & Thang Khoảng Cách 8px
                </h3>
                <p class="text-muted small mb-3">
                    Màu chủ đạo Vinamilk Royal Blue (<code>#0033A0</code>), Nền trắng tinh khiết (<code>#FFFFFF</code>), Font chữ <strong>Be Vietnam Pro</strong>.
                </p>

                <!-- BẢNG MÀU CHỦ ĐẠO -->
                <div class="row g-2 mb-3">
                    <div class="col-6 col-sm-3">
                        <div class="p-2 rounded text-white text-center" style="background-color: #0033A0;">
                            <div class="fw-bold small">Primary</div>
                            <div style="font-size: 11px;">#0033A0</div>
                        </div>
                    </div>
                    <div class="col-6 col-sm-3">
                        <div class="p-2 rounded text-dark text-center border" style="background-color: #FFFFFF;">
                            <div class="fw-bold small">White</div>
                            <div style="font-size: 11px;">#FFFFFF</div>
                        </div>
                    </div>
                    <div class="col-6 col-sm-3">
                        <div class="p-2 rounded text-dark text-center border" style="background-color: #EBF1FA;">
                            <div class="fw-bold small">Subtle Blue</div>
                            <div style="font-size: 11px;">#EBF1FA</div>
                        </div>
                    </div>
                    <div class="col-6 col-sm-3">
                        <div class="p-2 rounded text-white text-center" style="background-color: #10B981;">
                            <div class="fw-bold small">Success</div>
                            <div style="font-size: 11px;">#10B981</div>
                        </div>
                    </div>
                </div>

                <!-- THANG KHOẢNG CÁCH 8PX (SPACING SCALE) -->
                <div class="fw-semibold small mb-2 text-dark">Thang khoảng cách cơ sở (Multiples of 8px):</div>
                <div class="d-flex flex-wrap align-items-end gap-2 text-center text-muted" style="font-size: 11px;">
                    <div>
                        <div style="width: 8px; height: 8px; background: var(--vnm-primary); margin: 0 auto 4px;"></div>
                        <span>8px</span>
                    </div>
                    <div>
                        <div style="width: 16px; height: 16px; background: var(--vnm-primary); margin: 0 auto 4px;"></div>
                        <span>16px</span>
                    </div>
                    <div>
                        <div style="width: 24px; height: 24px; background: var(--vnm-primary); margin: 0 auto 4px;"></div>
                        <span>24px</span>
                    </div>
                    <div>
                        <div style="width: 32px; height: 32px; background: var(--vnm-primary); margin: 0 auto 4px;"></div>
                        <span>32px</span>
                    </div>
                    <div>
                        <div style="width: 40px; height: 40px; background: var(--vnm-primary); margin: 0 auto 4px;"></div>
                        <span>40px</span>
                    </div>
                    <div>
                        <div style="width: 48px; height: 48px; background: var(--vnm-primary); margin: 0 auto 4px;"></div>
                        <span>48px</span>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

<!-- SCRIPTS DEMO MODAL TƯƠNG TÁC TRÊN KITCHEN SINK -->
<script>
    function triggerDemoConfirm(maNV, hoTen) {
        window.vnmConfirm({
            title: 'Xác nhận xóa hồ sơ ' + maNV,
            message: 'Bạn có chắc chắn muốn chuyển trạng thái hồ sơ của nhân viên ' + hoTen + ' (' + maNV + ') sang đã thôi việc?',
            confirmText: 'Xác nhận chuyển trạng thái',
            isDanger: true,
            onConfirm: function () {
                alert('Đã thực hiện chuyển trạng thái thành công cho ' + hoTen + '!');
            }
        });
    }

    function testDangerModal() {
        window.vnmConfirm({
            title: 'Cảnh báo hành động nguy hiểm',
            message: 'Hành động này sẽ xóa vĩnh viễn cấu hình tham số pháp lý và không thể khôi phục.',
            confirmText: 'Tôi hiểu rủi ro, tiếp tục',
            isDanger: true,
            onConfirm: function () {
                alert('Đã xác nhận hành động nguy hiểm!');
            }
        });
    }

    function testApproveModal() {
        window.vnmConfirm({
            title: 'Phê duyệt tờ trình tăng lương',
            message: 'Bạn đồng ý phê duyệt tờ trình tăng lương đợt 2 cho khối Nhà máy Sản xuất với tổng ngân sách 240.000.000 VNĐ?',
            confirmText: 'Phê duyệt tờ trình',
            isDanger: false,
            onConfirm: function () {
                alert('Tờ trình đã được phê duyệt thành công!');
            }
        });
    }
</script>
