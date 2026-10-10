<?php
/**
 * =====================================================================
 * VINAMILK HRM - Reports Hub (Trung tâm Báo cáo chuẩn HUHA HRM)
 * =====================================================================
 */
?>

<div class="mb-4">
    <div class="d-flex justify-content-between align-items-center">
        <div>
            <h3 class="fw-bold text-vinamilk mb-1">
                <i class="bi bi-file-earmark-bar-graph me-2"></i>Phân Hệ Thống Kê & Báo Cáo Chuẩn HUHA HRM
            </h3>
            <p class="text-muted small mb-0">Hệ thống báo cáo quản trị nhân sự tổng hợp, biểu mẫu trích ngang, biến động nhân sự, khen thưởng kỷ luật và tiệm cận nghỉ hưu.</p>
        </div>
        <div>
            <a href="<?= BASE_URL ?>/baocao/bm01_trichngang" class="btn btn-primary rounded-pill shadow-sm">
                <i class="bi bi-play-circle me-1"></i>Xem Biểu Mẫu 01 Ngay
            </a>
        </div>
    </div>
</div>

<div class="row g-4 mb-5">
    <?php foreach ($reports as $item): ?>
        <div class="col-md-6 col-xl-4">
            <div class="card h-100 border-0 shadow-sm hover-top transition-all">
                <div class="card-body p-4">
                    <div class="d-flex align-items-center mb-3">
                        <div class="stat-icon me-3 flex-shrink-0 bg-opacity-10 rounded-3 p-3 <?= 'bg-' . $item['color'] ?> text-<?= $item['color'] ?>">
                            <i class="bi <?= $item['icon'] ?> fs-3"></i>
                        </div>
                        <div>
                            <span class="badge bg-<?= $item['color'] ?>-subtle text-<?= $item['color'] ?> border border-<?= $item['color'] ?>-subtle mb-1">
                                <?= htmlspecialchars($item['code']) ?>
                            </span>
                            <h5 class="fw-bold text-dark mb-0"><?= htmlspecialchars($item['title']) ?></h5>
                        </div>
                    </div>
                    <p class="text-secondary small mb-4 line-clamp-2"><?= htmlspecialchars($item['description']) ?></p>
                    
                    <div class="d-flex align-items-center justify-content-between border-top pt-3">
                        <span class="badge bg-light text-muted fw-normal">
                            <i class="bi bi-file-earmark-text me-1"></i>Format: PDF, Excel, A4 Print
                        </span>
                        <a href="<?= BASE_URL ?>/baocao/<?= $item['action'] ?>" class="btn btn-sm btn-outline-primary rounded-pill px-3">
                            Truy cập <i class="bi bi-arrow-right ms-1"></i>
                        </a>
                    </div>
                </div>
            </div>
        </div>
    <?php endforeach; ?>
</div>

<div class="card border-0 shadow-sm bg-light">
    <div class="card-body p-4">
        <div class="row align-items-center">
            <div class="col-lg-8">
                <h5 class="fw-bold text-vinamilk mb-2"><i class="bi bi-printer me-2"></i>Quy chuẩn In Ấn & Xuất Dữ Liệu</h5>
                <p class="text-muted small mb-0">Tất cả các báo cáo chuẩn HUHA HRM đều được thiết kế định dạng chuẩn khổ giấy A4 (Đứng / Ngang) với stylesheet <code>@media print</code> tự động loại bỏ Sidebar, Header và các nút bấm điều hướng khi thực hiện in trực tiếp từ trình duyệt.</p>
            </div>
            <div class="col-lg-4 text-lg-end mt-3 mt-lg-0">
                <a href="<?= BASE_URL ?>/baocao/export_excel?type=bm01" class="btn btn-success me-2 rounded-pill">
                    <i class="bi bi-file-earmark-excel me-1"></i> Mẫu Excel Tải Về
                </a>
            </div>
        </div>
    </div>
</div>
