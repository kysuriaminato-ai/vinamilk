<?php
/**
 * =====================================================================
 * VINAMILK HRM - Biểu mẫu 03: Thống Kê Khen Thưởng & Kỷ Luật
 * =====================================================================
 */
?>

<div class="mb-4">
    <div class="d-flex justify-content-between align-items-center">
        <div>
            <nav aria-label="breadcrumb">
                <ol class="breadcrumb mb-1">
                    <li class="breadcrumb-item"><a href="<?= BASE_URL ?>/baocao">Báo cáo</a></li>
                    <li class="breadcrumb-item active">Biểu mẫu 03</li>
                </ol>
            </nav>
            <h3 class="fw-bold text-vinamilk mb-1">
                <i class="bi bi-trophy me-2"></i>Biểu Mẫu 03: Thống Kê Khen Thưởng & Kỷ Luật
            </h3>
            <p class="text-muted small mb-0">Thống kê danh sách quyết định khen thưởng và kỷ luật toàn công ty theo từng năm</p>
        </div>
        <div class="d-flex gap-2">
            <a href="<?= BASE_URL ?>/baocao/print_report?type=bm03&year=<?= $selected_year ?>&loai=<?= urlencode($selected_type) ?>" target="_blank" class="btn btn-outline-secondary rounded-pill">
                <i class="bi bi-printer me-1"></i>In A4 Ngang
            </a>
            <a href="<?= BASE_URL ?>/baocao/export_excel?type=bm03&year=<?= $selected_year ?>&loai=<?= urlencode($selected_type) ?>" class="btn btn-success rounded-pill">
                <i class="bi bi-file-earmark-excel me-1"></i>Xuất Excel (.xlsx)
            </a>
        </div>
    </div>
</div>

<!-- Filter form -->
<div class="card border-0 shadow-sm mb-4">
    <div class="card-body p-3">
        <form method="GET" action="<?= BASE_URL ?>/baocao/bm03_ktkl" class="row g-2 align-items-center">
            <div class="col-md-4">
                <label class="form-label small fw-bold mb-1">Năm báo cáo</label>
                <select name="year" class="form-select">
                    <?php 
                    $currentY = date('Y');
                    for ($y = $currentY; $y >= $currentY - 5; $y--): 
                    ?>
                        <option value="<?= $y ?>" <?= $selected_year == $y ? 'selected' : '' ?>>Năm <?= $y ?></option>
                    <?php endfor; ?>
                </select>
            </div>
            <div class="col-md-5">
                <label class="form-label small fw-bold mb-1">Hình thức</label>
                <select name="loai" class="form-select">
                    <option value="">-- Tất cả (Khen thưởng & Kỷ luật) --</option>
                    <option value="Khen thưởng" <?= $selected_type == 'Khen thưởng' ? 'selected' : '' ?>>Chỉ Khen Thưởng</option>
                    <option value="Kỷ luật" <?= $selected_type == 'Kỷ luật' ? 'selected' : '' ?>>Chỉ Kỷ Luật</option>
                </select>
            </div>
            <div class="col-md-3 d-flex align-items-end">
                <button type="submit" class="btn btn-primary w-100 rounded-pill mt-4">
                    <i class="bi bi-funnel me-1"></i>Xem Báo Cáo
                </button>
            </div>
        </form>
    </div>
</div>

<!-- Report Table -->
<div class="card border-0 shadow-sm mb-4">
    <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center">
        <h6 class="fw-bold text-vinamilk mb-0">
            <i class="bi bi-award me-2"></i>Tổng số danh mục năm <?= $selected_year ?>: <span class="badge bg-primary rounded-pill"><?= count($records) ?></span>
        </h6>
    </div>
    <div class="table-responsive">
        <table class="table table-hover table-striped align-middle mb-0 text-nowrap" style="font-size: 0.875rem;">
            <thead class="table-dark text-center align-middle">
                <tr>
                    <th style="width: 40px;">STT</th>
                    <th>Số Quyết Định</th>
                    <th>Ngày Quyết Định</th>
                    <th>Mã NV</th>
                    <th>Họ và Tên Nhân Viên</th>
                    <th>Phòng Ban</th>
                    <th>Phân Loại</th>
                    <th>Hình Thức</th>
                    <th>Lý Do / Thành Tích</th>
                    <th>Giá Trị Khen Thưởng (VNĐ)</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($records)): ?>
                    <tr>
                        <td colspan="10" class="text-center py-4 text-muted">
                            <i class="bi bi-inbox fs-2 d-block mb-2"></i>Không có dữ liệu khen thưởng/kỷ luật năm <?= $selected_year ?>.
                        </td>
                    </tr>
                <?php else: ?>
                    <?php 
                    $stt = 1; 
                    $totalReward = 0;
                    foreach ($records as $r): 
                        if (($r['loai'] ?? '') == 'Khen thưởng') {
                            $totalReward += ($r['so_tien'] ?? 0);
                        }
                    ?>
                        <tr>
                            <td class="text-center fw-bold text-muted"><?= $stt++ ?></td>
                            <td class="fw-bold text-vinamilk"><?= htmlspecialchars($r['so_quyet_dinh'] ?? 'QĐ-KTKL') ?></td>
                            <td class="text-center"><?= !empty($r['ngay_quyet_dinh']) ? date('d/m/Y', strtotime($r['ngay_quyet_dinh'])) : 'N/A' ?></td>
                            <td class="fw-bold"><?= htmlspecialchars($r['ma_nv'] ?? 'N/A') ?></td>
                            <td class="fw-bold"><?= htmlspecialchars($r['ho_ten'] ?? 'N/A') ?></td>
                            <td><?= htmlspecialchars($r['ten_phong_ban'] ?? 'N/A') ?></td>
                            <td class="text-center">
                                <?php if (($r['loai'] ?? '') == 'Khen thưởng'): ?>
                                    <span class="badge bg-success"><i class="bi bi-hand-thumbs-up me-1"></i>Khen Thưởng</span>
                                <?php else: ?>
                                    <span class="badge bg-danger"><i class="bi bi-exclamation-octagon me-1"></i>Kỷ Luật</span>
                                <?php endif; ?>
                            </td>
                            <td><?= htmlspecialchars($r['hinh_thuc'] ?? 'Bằng khen / Nhắc nhở') ?></td>
                            <td><?= htmlspecialchars($r['ly_do'] ?? 'N/A') ?></td>
                            <td class="text-end fw-bold text-success">
                                <?= !empty($r['so_tien']) && $r['so_tien'] > 0 ? number_format($r['so_tien']) . ' đ' : '-' ?>
                            </td>
                        </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
            <?php if (!empty($records)): ?>
                <tfoot class="table-light fw-bold">
                    <tr>
                        <td colspan="9" class="text-end">Tổng quỹ tiền thưởng phát sinh trong năm:</td>
                        <td class="text-end text-success fs-6"><?= number_format($totalReward) ?> VNĐ</td>
                    </tr>
                </tfoot>
            <?php endif; ?>
        </table>
    </div>
</div>
