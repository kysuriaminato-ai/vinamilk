<?php
/**
 * =====================================================================
 * VINAMILK HRM - Biểu mẫu 02: Báo Cáo Biến Động Nhân Sự
 * =====================================================================
 */
?>

<div class="mb-4">
    <div class="d-flex justify-content-between align-items-center">
        <div>
            <nav aria-label="breadcrumb">
                <ol class="breadcrumb mb-1">
                    <li class="breadcrumb-item"><a href="<?= BASE_URL ?>/baocao">Báo cáo</a></li>
                    <li class="breadcrumb-item active">Biểu mẫu 02</li>
                </ol>
            </nav>
            <h3 class="fw-bold text-vinamilk mb-1">
                <i class="bi bi-arrow-left-right me-2"></i>Biểu Mẫu 02: Báo Cáo Biến Động Nhân Sự
            </h3>
            <p class="text-muted small mb-0">Thống kê danh sách nhân sự điều động, thuyên chuyển, bổ nhiệm và tiếp nhận mới</p>
        </div>
        <div class="d-flex gap-2">
            <a href="<?= BASE_URL ?>/baocao/print_report?type=bm02&year=<?= $selected_year ?>&loai=<?= urlencode($selected_type) ?>" target="_blank" class="btn btn-outline-secondary rounded-pill">
                <i class="bi bi-printer me-1"></i>In A4 Ngang
            </a>
            <a href="<?= BASE_URL ?>/baocao/export_excel?type=bm02&year=<?= $selected_year ?>&loai=<?= urlencode($selected_type) ?>" class="btn btn-success rounded-pill">
                <i class="bi bi-file-earmark-excel me-1"></i>Xuất Excel (.xlsx)
            </a>
        </div>
    </div>
</div>

<!-- Filter form -->
<div class="card border-0 shadow-sm mb-4">
    <div class="card-body p-3">
        <form method="GET" action="<?= BASE_URL ?>/baocao/bm02_biendong" class="row g-2 align-items-center">
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
                <label class="form-label small fw-bold mb-1">Loại biến động</label>
                <select name="loai" class="form-select">
                    <option value="">-- Tất cả loại biến động --</option>
                    <option value="Thuyên chuyển" <?= $selected_type == 'Thuyên chuyển' ? 'selected' : '' ?>>Thuyên chuyển công tác</option>
                    <option value="Bổ nhiệm" <?= $selected_type == 'Bổ nhiệm' ? 'selected' : '' ?>>Bổ nhiệm chức vụ</option>
                    <option value="Điều động" <?= $selected_type == 'Điều động' ? 'selected' : '' ?>>Điều động đơn vị</option>
                    <option value="Tuyển mới" <?= $selected_type == 'Tuyển mới' ? 'selected' : '' ?>>Tiếp nhận mới</option>
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
            <i class="bi bi-list-check me-2"></i>Tổng số lượt biến động năm <?= $selected_year ?>: <span class="badge bg-primary rounded-pill"><?= count($records) ?></span>
        </h6>
    </div>
    <div class="table-responsive">
        <table class="table table-hover table-striped align-middle mb-0 text-nowrap" style="font-size: 0.875rem;">
            <thead class="table-dark text-center align-middle">
                <tr>
                    <th style="width: 40px;">STT</th>
                    <th>Số Quyết Định</th>
                    <th>Ngày Hiệu Lực</th>
                    <th>Mã NV</th>
                    <th>Họ và Tên Nhân Viên</th>
                    <th>Loại Biến Động</th>
                    <th>Đơn Vị Cũ</th>
                    <th>Đơn Vị Mới</th>
                    <th>Chức Vụ Cũ</th>
                    <th>Chức Vụ Mới</th>
                    <th>Ghi Chú / Lý Do</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($records)): ?>
                    <tr>
                        <td colspan="11" class="text-center py-4 text-muted">
                            <i class="bi bi-inbox fs-2 d-block mb-2"></i>Không có dữ liệu biến động nhân sự trong năm <?= $selected_year ?>.
                        </td>
                    </tr>
                <?php else: ?>
                    <?php $stt = 1; foreach ($records as $r): ?>
                        <tr>
                            <td class="text-center fw-bold text-muted"><?= $stt++ ?></td>
                            <td class="fw-bold text-vinamilk"><?= htmlspecialchars($r['so_quyet_dinh'] ?? 'QĐ-' . sprintf('%04d', $r['id'])) ?></td>
                            <td class="text-center"><?= !empty($r['ngay_hieu_luc']) ? date('d/m/Y', strtotime($r['ngay_hieu_luc'])) : 'N/A' ?></td>
                            <td class="fw-bold"><?= htmlspecialchars($r['ma_nv'] ?? 'NV-VINAMILK') ?></td>
                            <td class="fw-bold"><?= htmlspecialchars($r['ho_ten'] ?? 'N/A') ?></td>
                            <td class="text-center">
                                <?php 
                                $typeBadge = match($r['loai_chuyen'] ?? 'Thuyên chuyển') {
                                    'Bổ nhiệm' => 'bg-success',
                                    'Thuyên chuyển' => 'bg-info',
                                    'Điều động' => 'bg-warning text-dark',
                                    default => 'bg-primary'
                                };
                                ?>
                                <span class="badge <?= $typeBadge ?>"><?= htmlspecialchars($r['loai_chuyen'] ?? 'Thuyên chuyển') ?></span>
                            </td>
                            <td><?= htmlspecialchars($r['pb_cu_ten'] ?? 'N/A') ?></td>
                            <td class="fw-bold text-primary"><?= htmlspecialchars($r['pb_moi_ten'] ?? 'N/A') ?></td>
                            <td><?= htmlspecialchars($r['cv_cu_ten'] ?? 'N/A') ?></td>
                            <td class="fw-bold text-success"><?= htmlspecialchars($r['cv_moi_ten'] ?? 'N/A') ?></td>
                            <td><?= htmlspecialchars($r['ly_do'] ?? 'Theo nhu cầu công tác') ?></td>
                        </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
        </table>
    </div>
</div>
