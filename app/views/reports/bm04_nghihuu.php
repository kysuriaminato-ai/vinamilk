<?php
/**
 * =====================================================================
 * VINAMILK HRM - Biểu mẫu 04: Thống Kê Nhân Sự Tiệm Cận Nghỉ Hưu
 * =====================================================================
 */
?>

<div class="mb-4">
    <div class="d-flex justify-content-between align-items-center">
        <div>
            <nav aria-label="breadcrumb">
                <ol class="breadcrumb mb-1">
                    <li class="breadcrumb-item"><a href="<?= BASE_URL ?>/baocao">Báo cáo</a></li>
                    <li class="breadcrumb-item active">Biểu mẫu 04</li>
                </ol>
            </nav>
            <h3 class="fw-bold text-vinamilk mb-1">
                <i class="bi bi-person-exclamation me-2"></i>Biểu Mẫu 04: Thống Kê Nhân Sự Tiệm Cận Nghỉ Hưu
            </h3>
            <p class="text-muted small mb-0">Danh sách Cán bộ công nhân viên chuẩn bị đến tuổi nghỉ hưu (Nam >= 60 tuổi, Nữ >= 55 tuổi) theo Quy định Bộ Luật Lao Động</p>
        </div>
        <div class="d-flex gap-2">
            <a href="<?= BASE_URL ?>/baocao/print_report?type=bm04&months=<?= $selected_months ?>" target="_blank" class="btn btn-outline-secondary rounded-pill">
                <i class="bi bi-printer me-1"></i>In A4 Ngang
            </a>
            <a href="<?= BASE_URL ?>/baocao/export_excel?type=bm04&months=<?= $selected_months ?>" class="btn btn-success rounded-pill">
                <i class="bi bi-file-earmark-excel me-1"></i>Xuất Excel (.xlsx)
            </a>
        </div>
    </div>
</div>

<!-- Filter Form -->
<div class="card border-0 shadow-sm mb-4">
    <div class="card-body p-3">
        <form method="GET" action="<?= BASE_URL ?>/baocao/bm04_nghihuu" class="row g-2 align-items-center">
            <div class="col-md-8">
                <label class="form-label small fw-bold mb-1">Khung thời gian tiệm cận nghỉ hưu</label>
                <select name="months" class="form-select">
                    <option value="6" <?= $selected_months == 6 ? 'selected' : '' ?>>Trong vòng 6 tháng tới</option>
                    <option value="12" <?= $selected_months == 12 ? 'selected' : '' ?>>Trong vòng 12 tháng tới (1 năm)</option>
                    <option value="24" <?= $selected_months == 24 ? 'selected' : '' ?>>Trong vòng 24 tháng tới (2 năm)</option>
                    <option value="36" <?= $selected_months == 36 ? 'selected' : '' ?>>Trong vòng 36 tháng tới (3 năm)</option>
                </select>
            </div>
            <div class="col-md-4 d-flex align-items-end">
                <button type="submit" class="btn btn-primary w-100 rounded-pill mt-4">
                    <i class="bi bi-filter-circle me-1"></i>Lọc Danh Sách
                </button>
            </div>
        </form>
    </div>
</div>

<!-- Report Table -->
<div class="card border-0 shadow-sm mb-4">
    <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center">
        <h6 class="fw-bold text-vinamilk mb-0">
            <i class="bi bi-hourglass-split me-2"></i>Số lượng nhân sự tiệm cận nghỉ hưu: <span class="badge bg-danger rounded-pill"><?= count($records) ?></span>
        </h6>
    </div>
    <div class="table-responsive">
        <table class="table table-hover table-striped align-middle mb-0 text-nowrap" style="font-size: 0.875rem;">
            <thead class="table-dark text-center align-middle">
                <tr>
                    <th style="width: 40px;">STT</th>
                    <th>Mã NV</th>
                    <th>Họ và Tên</th>
                    <th>Giới tính</th>
                    <th>Ngày sinh</th>
                    <th>Tuổi hiện tại</th>
                    <th>Phòng Ban / Đơn vị</th>
                    <th>Chức vụ</th>
                    <th>Thời gian dự kiến nghỉ hưu</th>
                    <th>Trạng thái hồ sơ</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($records)): ?>
                    <tr>
                        <td colspan="10" class="text-center py-4 text-muted">
                            <i class="bi bi-shield-check fs-2 d-block mb-2 text-success"></i>Không có nhân sự nào tiệm cận tuổi nghỉ hưu trong <?= $selected_months ?> tháng tới.
                        </td>
                    </tr>
                <?php else: ?>
                    <?php $stt = 1; foreach ($records as $r): ?>
                        <tr>
                            <td class="text-center fw-bold text-muted"><?= $stt++ ?></td>
                            <td class="fw-bold text-vinamilk"><?= htmlspecialchars($r['ma_nv'] ?? 'N/A') ?></td>
                            <td class="fw-bold"><?= htmlspecialchars($r['ho_ten'] ?? 'N/A') ?></td>
                            <td class="text-center">
                                <?php if ($r['gioi_tinh'] == 'Nam'): ?>
                                    <span class="badge bg-info-subtle text-info">Nam</span>
                                <?php else: ?>
                                    <span class="badge bg-danger-subtle text-danger">Nữ</span>
                                <?php endif; ?>
                            </td>
                            <td class="text-center"><?= !empty($r['ngay_sinh']) ? date('d/m/Y', strtotime($r['ngay_sinh'])) : 'N/A' ?></td>
                            <td class="text-center fw-bold text-dark"><?= htmlspecialchars($r['tuoi'] ?? 60) ?> tuổi</td>
                            <td><?= htmlspecialchars($r['ten_phong_ban'] ?? 'N/A') ?></td>
                            <td><?= htmlspecialchars($r['ten_chuc_vu'] ?? 'N/A') ?></td>
                            <td class="text-center fw-bold text-danger">
                                <?= !empty($r['ngay_du_kien_nghi_huu']) ? date('d/m/Y', strtotime($r['ngay_du_kien_nghi_huu'])) : 'Đã đủ tuổi' ?>
                            </td>
                            <td class="text-center">
                                <span class="badge bg-warning text-dark"><i class="bi bi-clock-history me-1"></i>Đang lập thủ tục</span>
                            </td>
                        </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
        </table>
    </div>
</div>
