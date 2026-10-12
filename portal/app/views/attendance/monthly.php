<div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
    <div>
        <h3 class="fw-bold text-dark mb-1">
            <i class="bi bi-calendar3-range text-primary me-2"></i>Bảng Công Tổng Hợp Tháng <?= e($filters['thang']) ?>/<?= e($filters['nam']) ?>
        </h3>
        <p class="text-muted small mb-0">
            Dữ liệu tổng hợp từ <strong>cham_cong_ngay</strong> &bull; Quy trình duyệt 3 bước: 
            <span class="badge bg-secondary-subtle text-secondary">1. Tổ trưởng xác nhận</span> &rarr;
            <span class="badge bg-primary-subtle text-primary">2. Quản đốc duyệt</span> &rarr;
            <span class="badge bg-success-subtle text-success">3. Khóa công</span> &bull; 
            Cảnh báo giới hạn OT tháng/năm theo Bộ luật Lao động.
        </p>
    </div>

    <div class="d-flex gap-2 no-print">
        <a href="<?= url('attendance') ?>" class="btn btn-outline-secondary btn-sm fw-semibold">
            <i class="bi bi-calendar-check me-1"></i> Chấm Công Ngày
        </a>
        <a href="<?= url('attendance/roster') ?>" class="btn btn-outline-primary btn-sm fw-semibold">
            <i class="bi bi-grid-3x3-gap-fill me-1"></i> Lưới Xếp Ca Tuần
        </a>
        <?php if (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI', 'KETOAN_LUONG'])): ?>
        <a href="<?= url('attendance/monthly?thang=' . $filters['thang'] . '&nam=' . $filters['nam'] . '&ma_dv=' . urlencode($filters['ma_dv']) . '&recalculate=1') ?>" 
           class="btn btn-warning btn-sm fw-semibold text-dark" onclick="return confirm('Bạn có chắc muốn chạy lại tính toán tổng hợp công tháng này từ dữ liệu quẹt thẻ ngày?')">
            <i class="bi bi-arrow-repeat me-1"></i> Tổng Hợp Lại Công
        </a>
        <?php endif; ?>
        <a href="<?= url('attendance/export_excel?thang=' . $filters['thang'] . '&nam=' . $filters['nam'] . '&ma_dv=' . urlencode($filters['ma_dv'])) ?>" 
           class="btn btn-success btn-sm fw-semibold">
            <i class="bi bi-file-earmark-excel-fill me-1"></i> Xuất Excel (CSV)
        </a>
    </div>
</div>

<?php
    // Tính toán số liệu thống kê KPI
    $totalEmployees = count($timesheets);
    $totalCongTT = 0;
    $totalOtHours = 0;
    $totalOtAlerts = 0;
    $totalLocked = 0;

    foreach ($timesheets as $ts) {
        $totalCongTT += (float)$ts['cong_thuc_te'];
        $totalOtHours += (float)$ts['tong_gio_ot'];
        if (!empty($ts['canh_bao_ot'])) {
            $totalOtAlerts++;
        }
        if ($ts['trang_thai'] === 'KHOA_CONG') {
            $totalLocked++;
        }
    }
?>

<!-- Thẻ KPI Thống Kê Tổng Quan -->
<div class="row g-3 mb-4">
    <div class="col-md-3 col-sm-6">
        <div class="card card-custom border-0 shadow-sm p-3 h-100" style="border-radius: 8px; border-left: 4px solid #0033A0 !important;">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="text-muted small fw-semibold text-uppercase">Tổng Nhân Sự</div>
                    <div class="fs-4 fw-bold text-dark mt-1"><?= number_format($totalEmployees) ?> <small class="fs-6 text-muted">người</small></div>
                </div>
                <div class="rounded-circle p-3 bg-primary-subtle text-primary">
                    <i class="bi bi-people-fill fs-4"></i>
                </div>
            </div>
        </div>
    </div>

    <div class="col-md-3 col-sm-6">
        <div class="card card-custom border-0 shadow-sm p-3 h-100" style="border-radius: 8px; border-left: 4px solid #059669 !important;">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="text-muted small fw-semibold text-uppercase">Tổng Công Thực Tế</div>
                    <div class="fs-4 fw-bold text-success mt-1"><?= number_format($totalCongTT, 1) ?> <small class="fs-6 text-muted">công</small></div>
                </div>
                <div class="rounded-circle p-3 bg-success-subtle text-success">
                    <i class="bi bi-calendar-check-fill fs-4"></i>
                </div>
            </div>
        </div>
    </div>

    <div class="col-md-3 col-sm-6">
        <div class="card card-custom border-0 shadow-sm p-3 h-100" style="border-radius: 8px; border-left: 4px solid #D97706 !important;">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="text-muted small fw-semibold text-uppercase">Tổng Giờ OT Tháng</div>
                    <div class="fs-4 fw-bold text-warning mt-1"><?= number_format($totalOtHours, 1) ?> <small class="fs-6 text-muted">giờ</small></div>
                </div>
                <div class="rounded-circle p-3 bg-warning-subtle text-warning">
                    <i class="bi bi-clock-history fs-4"></i>
                </div>
            </div>
        </div>
    </div>

    <div class="col-md-3 col-sm-6">
        <div class="card card-custom border-0 shadow-sm p-3 h-100" style="border-radius: 8px; border-left: 4px solid #DC2626 !important;">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="text-muted small fw-semibold text-uppercase">Cảnh Báo Giới Hạn OT</div>
                    <div class="fs-4 fw-bold text-danger mt-1"><?= number_format($totalOtAlerts) ?> <small class="fs-6 text-muted">trường hợp</small></div>
                </div>
                <div class="rounded-circle p-3 bg-danger-subtle text-danger">
                    <i class="bi bi-exclamation-octagon-fill fs-4"></i>
                </div>
            </div>
        </div>
    </div>
</div>

<!-- Bộ lọc tìm kiếm tháng & đơn vị -->
<div class="card card-custom p-3 mb-4 no-print border-0 shadow-sm" style="border-radius: 8px;">
    <form action="<?= url('attendance/monthly') ?>" method="GET" class="row g-2 align-items-end">
        <div class="col-md-2">
            <label class="form-label small fw-semibold text-muted mb-1">Tháng</label>
            <select name="thang" class="form-select form-select-sm">
                <?php for ($m = 1; $m <= 12; $m++): ?>
                    <option value="<?= $m ?>" <?= ((int)$filters['thang'] === $m) ? 'selected' : '' ?>>Tháng <?= $m ?></option>
                <?php endfor; ?>
            </select>
        </div>

        <div class="col-md-2">
            <label class="form-label small fw-semibold text-muted mb-1">Năm</label>
            <select name="nam" class="form-select form-select-sm">
                <?php for ($y = 2024; $y <= 2028; $y++): ?>
                    <option value="<?= $y ?>" <?= ((int)$filters['nam'] === $y) ? 'selected' : '' ?>>Năm <?= $y ?></option>
                <?php endfor; ?>
            </select>
        </div>

        <div class="col-md-3">
            <label class="form-label small fw-semibold text-muted mb-1">Đơn vị công tác</label>
            <select name="ma_dv" class="form-select form-select-sm">
                <option value="ALL">-- Tất cả đơn vị được phép --</option>
                <?php foreach ($selectableUnits as $u): ?>
                    <option value="<?= e($u['MaDV']) ?>" <?= (($filters['ma_dv'] ?? '') === $u['MaDV']) ? 'selected' : '' ?>>
                        <?= e($u['IndentTitle'] ?? $u['TenDV']) ?>
                    </option>
                <?php endforeach; ?>
            </select>
        </div>

        <div class="col-md-2">
            <label class="form-label small fw-semibold text-muted mb-1">Trạng thái duyệt</label>
            <select name="trang_thai" class="form-select form-select-sm">
                <option value="">-- Tất cả trạng thái --</option>
                <option value="KHOI_TAO" <?= (($filters['trang_thai'] ?? '') === 'KHOI_TAO') ? 'selected' : '' ?>>Khởi tạo</option>
                <option value="TO_TRUONG_XAC_NHAN" <?= (($filters['trang_thai'] ?? '') === 'TO_TRUONG_XAC_NHAN') ? 'selected' : '' ?>>Tổ trưởng xác nhận</option>
                <option value="QUAN_DOC_DUYET" <?= (($filters['trang_thai'] ?? '') === 'QUAN_DOC_DUYET') ? 'selected' : '' ?>>Quản đốc duyệt</option>
                <option value="KHOA_CONG" <?= (($filters['trang_thai'] ?? '') === 'KHOA_CONG') ? 'selected' : '' ?>>Đã khóa công</option>
            </select>
        </div>

        <div class="col-md-3 d-flex gap-2">
            <button type="submit" class="btn btn-primary btn-sm w-100 fw-semibold" style="background: #0033A0;">
                <i class="bi bi-funnel-fill me-1"></i> Xem Bảng Công
            </button>
            <a href="<?= url('attendance/monthly') ?>" class="btn btn-light btn-sm border" title="Đặt lại">
                <i class="bi bi-arrow-counterclockwise"></i>
            </a>
        </div>
    </form>
</div>

<!-- Bảng hiển thị công tháng tổng hợp -->
<div class="card card-custom overflow-hidden border-0 shadow-sm mb-4" style="border-radius: 8px;">
    <div class="table-responsive">
        <table class="table table-hover align-middle mb-0 text-center" style="font-size: 0.84rem;">
            <thead style="background: #F8FAFC; color: #475569; font-size: 0.78rem; text-transform: uppercase; border-bottom: 2px solid #E2E8F0;">
                <tr>
                    <th rowspan="2" style="width: 45px;" class="align-middle">STT</th>
                    <th rowspan="2" style="min-width: 170px;" class="text-start align-middle">Nhân Viên</th>
                    <th rowspan="2" style="min-width: 120px;" class="text-start align-middle">Đơn Vị</th>
                    <th colspan="4" class="text-center bg-primary-subtle text-primary border-bottom-0 py-1">Ngày Công Hưởng Lương</th>
                    <th colspan="4" class="text-center bg-warning-subtle text-dark border-bottom-0 py-1">Làm Thêm Giờ (OT)</th>
                    <th rowspan="2" class="align-middle bg-purple-subtle text-purple" style="color: #7C3AED;">Làm Đêm</th>
                    <th rowspan="2" class="align-middle">Đi Trễ / Sớm</th>
                    <th rowspan="2" class="align-middle">Quên Quẹt</th>
                    <th rowspan="2" style="min-width: 130px;" class="align-middle">Cảnh Báo OT</th>
                    <th rowspan="2" style="min-width: 120px;" class="align-middle">Trạng Thái</th>
                    <th rowspan="2" style="min-width: 150px;" class="align-middle no-print">Thao Tác Duyệt</th>
                </tr>
                <tr style="font-size: 0.74rem;">
                    <th class="py-1" title="Công thực tế đi làm">Thực Tế</th>
                    <th class="py-1" title="Nghỉ phép năm có lương">Phép</th>
                    <th class="py-1" title="Nghỉ lễ tết có lương">Lễ</th>
                    <th class="py-1 fw-bold text-primary" title="Tổng công tính lương">Tổng Công</th>

                    <th class="py-1" title="OT ngày thường (150%)">Thường (h)</th>
                    <th class="py-1" title="OT ngày nghỉ tuần (200%)">Nghỉ (h)</th>
                    <th class="py-1" title="OT ngày lễ tết (300%)">Lễ (h)</th>
                    <th class="py-1 fw-bold text-dark" title="Tổng giờ OT">Tổng OT</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($timesheets)): ?>
                    <tr>
                        <td colspan="16" class="text-center py-5 text-muted">
                            <i class="bi bi-calendar-x fs-1 d-block mb-2 text-secondary"></i>
                            Chưa có dữ liệu bảng công tháng <?= e($filters['thang']) ?>/<?= e($filters['nam']) ?>. 
                            Hãy bấm <strong>"Tổng Hợp Lại Công"</strong> để hệ thống tính toán từ dữ liệu quẹt thẻ.
                        </td>
                    </tr>
                <?php else: 
                    $stt = 1;
                    foreach ($timesheets as $ts):
                ?>
                    <tr>
                        <td class="text-muted small"><?= $stt++ ?></td>
                        <td class="text-start">
                            <div class="fw-bold text-dark"><?= e($ts['HoTen']) ?></div>
                            <div class="small text-muted font-monospace"><?= e($ts['ma_nv']) ?> &bull; <?= e($ts['TenCV'] ?? '') ?></div>
                        </td>
                        <td class="text-start">
                            <span class="badge bg-light text-dark border"><?= e($ts['TenDV'] ?: $ts['ma_dv']) ?></span>
                        </td>

                        <!-- Công hưởng lương -->
                        <td class="fw-semibold text-dark"><?= number_format((float)$ts['cong_thuc_te'], 1) ?></td>
                        <td class="text-secondary"><?= number_format((float)$ts['nghi_phep_huong_luong'], 1) ?></td>
                        <td class="text-secondary"><?= number_format((float)$ts['nghi_le_tet'], 1) ?></td>
                        <td class="fw-bold text-primary bg-primary-subtle"><?= number_format((float)$ts['tong_cong_tinh_luong'], 1) ?></td>

                        <!-- Giờ OT -->
                        <td class="text-muted"><?= number_format((float)$ts['gio_ot_ngay_thuong'], 1) ?></td>
                        <td class="text-muted"><?= number_format((float)$ts['gio_ot_ngay_nghi'], 1) ?></td>
                        <td class="text-muted"><?= number_format((float)$ts['gio_ot_ngay_le'], 1) ?></td>
                        <td class="fw-bold text-warning-emphasis bg-warning-subtle"><?= number_format((float)$ts['tong_gio_ot'], 1) ?>h</td>

                        <!-- Giờ đêm -->
                        <td>
                            <?php if ((float)$ts['gio_lam_dem'] > 0): ?>
                                <span class="badge" style="background: #7C3AED; color: #fff;">
                                    <i class="bi bi-moon-stars-fill me-1"></i><?= number_format((float)$ts['gio_lam_dem'], 1) ?>h
                                </span>
                            <?php else: ?>
                                <span class="text-muted">-</span>
                            <?php endif; ?>
                        </td>

                        <!-- Đi muộn & Quên quẹt -->
                        <td>
                            <?php if ((int)$ts['di_muon_ve_som_lan'] > 0): ?>
                                <span class="badge bg-warning text-dark"><?= (int)$ts['di_muon_ve_som_lan'] ?> lần</span>
                            <?php else: ?>
                                <span class="text-muted small">0</span>
                            <?php endif; ?>
                        </td>
                        <td>
                            <?php if ((int)$ts['quen_quet_the_lan'] > 0): ?>
                                <span class="badge bg-danger-subtle text-danger border border-danger-subtle">
                                    <?= (int)$ts['quen_quet_the_lan'] ?> lần
                                </span>
                            <?php else: ?>
                                <span class="text-muted small">0</span>
                            <?php endif; ?>
                        </td>

                        <!-- Cảnh báo OT -->
                        <td>
                            <?php if (str_contains($ts['canh_bao_ot'] ?? '', 'VUOT_THANG_40H')): ?>
                                <span class="badge bg-danger text-white py-1 px-2" title="Vượt trần luật Lao động 40h/tháng">
                                    <i class="bi bi-exclamation-triangle-fill me-1"></i>Vượt 40h/tháng
                                </span>
                            <?php elseif (str_contains($ts['canh_bao_ot'] ?? '', 'CANH_BAO_35H')): ?>
                                <span class="badge bg-warning text-dark py-1 px-2" title="Gần chạm ngưỡng 40h">
                                    <i class="bi bi-exclamation-circle-fill me-1"></i>Cận trần 35h
                                </span>
                            <?php elseif (str_contains($ts['canh_bao_ot'] ?? '', 'VUOT_NAM_200H')): ?>
                                <span class="badge bg-danger text-white py-1 px-2" title="Vượt trần 200h/năm">
                                    <i class="bi bi-shield-fill-x me-1"></i>Vượt 200h/năm
                                </span>
                            <?php else: ?>
                                <span class="badge bg-success-subtle text-success border border-success-subtle py-1 px-2">
                                    <i class="bi bi-check-circle me-1"></i>Đạt chuẩn
                                </span>
                            <?php endif; ?>
                        </td>

                        <!-- Trạng thái quy trình -->
                        <td>
                            <?php if ($ts['trang_thai'] === 'KHOA_CONG'): ?>
                                <span class="badge bg-dark text-white py-1 px-2">
                                    <i class="bi bi-lock-fill me-1 text-warning"></i>ĐÃ KHÓA CÔNG
                                </span>
                            <?php elseif ($ts['trang_thai'] === 'QUAN_DOC_DUYET'): ?>
                                <span class="badge bg-success py-1 px-2 text-white">
                                    <i class="bi bi-check2-all me-1"></i>Quản đốc duyệt
                                </span>
                            <?php elseif ($ts['trang_thai'] === 'TO_TRUONG_XAC_NHAN'): ?>
                                <span class="badge bg-info py-1 px-2 text-dark">
                                    <i class="bi bi-check2 me-1"></i>Tổ trưởng xác nhận
                                </span>
                            <?php else: ?>
                                <span class="badge bg-secondary-subtle text-secondary py-1 px-2">
                                    Khởi tạo
                                </span>
                            <?php endif; ?>
                        </td>

                        <!-- Thao tác duyệt 3 bước & Điều chỉnh sau khóa -->
                        <td class="no-print">
                            <div class="d-flex justify-content-center gap-1">
                                <?php if ($ts['trang_thai'] === 'KHOI_TAO' && Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI', 'QUANLY_TRUCTIEP'])): ?>
                                    <!-- Bước 1: Tổ trưởng xác nhận -->
                                    <form action="<?= url('attendance/confirm') ?>" method="POST" class="d-inline">
                                        <input type="hidden" name="csrf_token" value="<?= csrf_token() ?>">
                                        <input type="hidden" name="id" value="<?= $ts['id'] ?>">
                                        <button type="submit" class="btn btn-outline-info btn-xs py-1 px-2" title="Bước 1: Tổ trưởng xác nhận công">
                                            <i class="bi bi-check-lg"></i> Xác nhận
                                        </button>
                                    </form>
                                <?php endif; ?>

                                <?php if (in_array($ts['trang_thai'], ['KHOI_TAO', 'TO_TRUONG_XAC_NHAN']) && Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI'])): ?>
                                    <!-- Bước 2: Quản đốc duyệt -->
                                    <form action="<?= url('attendance/approve') ?>" method="POST" class="d-inline">
                                        <input type="hidden" name="csrf_token" value="<?= csrf_token() ?>">
                                        <input type="hidden" name="id" value="<?= $ts['id'] ?>">
                                        <button type="submit" class="btn btn-outline-success btn-xs py-1 px-2" title="Bước 2: Quản đốc phê duyệt bảng công">
                                            <i class="bi bi-check2-all"></i> Quản đốc duyệt
                                        </button>
                                    </form>
                                <?php endif; ?>

                                <?php if ($ts['trang_thai'] === 'QUAN_DOC_DUYET' && Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'KETOAN_LUONG'])): ?>
                                    <!-- Bước 3: Khóa công -->
                                    <form action="<?= url('attendance/lock') ?>" method="POST" class="d-inline" onsubmit="return confirm('CẢNH BÁO: Sau khi Khóa công, dữ liệu sẽ được chuyển sang kỳ tính lương và KHÔNG THỂ chỉnh sửa trực tiếp. Bạn có chắc muốn Khóa công?')">
                                        <input type="hidden" name="csrf_token" value="<?= csrf_token() ?>">
                                        <input type="hidden" name="id" value="<?= $ts['id'] ?>">
                                        <button type="submit" class="btn btn-dark btn-xs py-1 px-2" title="Bước 3: Khóa bảng công (Cấm sửa)">
                                            <i class="bi bi-lock-fill text-warning"></i> Khóa công
                                        </button>
                                    </form>
                                <?php endif; ?>

                                <?php if ($ts['trang_thai'] === 'KHOA_CONG'): ?>
                                    <!-- Đã khóa: Mở modal đề xuất điều chỉnh -->
                                    <button type="button" class="btn btn-outline-warning btn-xs py-1 px-2 text-dark" 
                                            onclick="openAdjustmentModal('<?= e($ts['ma_nv']) ?>', '<?= e($ts['HoTen']) ?>', '<?= e($ts['cong_thuc_te']) ?>', '<?= e($ts['tong_gio_ot']) ?>')" 
                                            title="Bảng công đã khóa. Nhấp để gửi Đề xuất Điều chỉnh kèm lý do">
                                        <i class="bi bi-pencil-square"></i> Điều chỉnh
                                    </button>
                                <?php endif; ?>
                            </div>
                        </td>
                    </tr>
                <?php endforeach; endif; ?>
            </tbody>
        </table>
    </div>
</div>

<!-- Modal Đề Xuất Điều Chỉnh Công Sau Khóa -->
<div class="modal fade" id="adjustmentModal" tabindex="-1" aria-labelledby="adjustmentModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-0 shadow">
            <div class="modal-header bg-warning-subtle text-dark border-bottom">
                <h5 class="modal-title fw-bold" id="adjustmentModalLabel">
                    <i class="bi bi-shield-exclamation text-warning me-2"></i>Phiếu Điều Chỉnh Công (Sau Khi Đã Khóa)
                </h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>
            <form action="<?= url('attendance/adjust') ?>" method="POST" id="adjustmentForm">
                <input type="hidden" name="csrf_token" value="<?= csrf_token() ?>">
                <div class="modal-body p-4">
                    <div class="alert alert-warning py-2 px-3 small mb-3">
                        <i class="bi bi-exclamation-triangle-fill me-1"></i> Bảng công tháng đã được khóa. Mọi thay đổi đều được ghi lại trong <strong>Audit Log</strong> và phải được cấp trên phê duyệt.
                    </div>

                    <div class="mb-3">
                        <label class="form-label small fw-semibold text-muted">Nhân viên cần điều chỉnh:</label>
                        <input type="text" id="adjEmpDisplay" class="form-control form-control-sm bg-light" readonly>
                        <input type="hidden" name="ma_nv" id="adjMaNV">
                    </div>

                    <div class="row g-2 mb-3">
                        <div class="col-md-6">
                            <label class="form-label small fw-semibold text-muted">Ngày công cần điều chỉnh:</label>
                            <input type="date" name="ngay_cong" class="form-control form-control-sm" required value="<?= date('Y-m-d') ?>">
                        </div>
                        <div class="col-md-6">
                            <label class="form-label small fw-semibold text-muted">Loại điều chỉnh:</label>
                            <select name="loai_dieu_chinh" class="form-select form-select-sm" required>
                                <option value="DIEU_CHINH_CONG_NGAY">Điều chỉnh ngày công (công thực tế)</option>
                                <option value="DIEU_CHINH_GIO_OT">Điều chỉnh số giờ làm thêm (OT)</option>
                                <option value="BO_SUNG_GIO_QUET">Bổ sung giờ quẹt thẻ do quên quẹt</option>
                            </select>
                        </div>
                    </div>

                    <div class="row g-2 mb-3">
                        <div class="col-md-6">
                            <label class="form-label small fw-semibold text-muted">Giá trị cũ:</label>
                            <input type="text" name="gia_tri_cu" id="adjOldVal" class="form-control form-control-sm" placeholder="VD: 0.5 công">
                        </div>
                        <div class="col-md-6">
                            <label class="form-label small fw-semibold text-muted">Giá trị mới đề xuất (*):</label>
                            <input type="text" name="gia_tri_moi" id="adjNewVal" class="form-control form-control-sm" placeholder="VD: 1.0 công" required>
                        </div>
                    </div>

                    <div class="mb-3">
                        <label class="form-label small fw-semibold text-muted">Lý do điều chỉnh (*) (Bắt buộc theo quy chế):</label>
                        <textarea name="ly_do" rows="3" class="form-control form-control-sm" placeholder="Nhập chi tiết lý do (VD: Nhân viên quên quẹt thẻ ra do họp đột xuất tại xưởng, có xác nhận của tổ trưởng...)" required></textarea>
                    </div>
                </div>
                <div class="modal-footer bg-light border-top">
                    <button type="button" class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Đóng</button>
                    <button type="submit" class="btn btn-warning btn-sm fw-semibold text-dark">
                        <i class="bi bi-send-fill me-1"></i> Gửi Yêu Cầu Điều Chỉnh
                    </button>
                </div>
            </form>
        </div>
    </div>
</div>

<script>
function openAdjustmentModal(maNV, hoTen, congTT, otHours) {
    document.getElementById('adjMaNV').value = maNV;
    document.getElementById('adjEmpDisplay').value = maNV + ' - ' + hoTen;
    document.getElementById('adjOldVal').value = congTT + ' công (' + otHours + 'h OT)';
    
    const modal = new bootstrap.Modal(document.getElementById('adjustmentModal'));
    modal.show();
}
</script>
