<div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
    <div>
        <h3 class="fw-bold text-dark mb-1">
            <i class="bi bi-calendar2-check-fill text-primary me-2"></i>Chấm Công Hằng Ngày
        </h3>
        <p class="text-muted small mb-0">
            Dữ liệu ghép ca và quẹt thẻ hằng ngày theo phân quyền <strong>scopeByUnit()</strong>.
            Tổng số: <strong class="text-primary"><?= number_format($pagination['total']) ?></strong> lượt ghi nhận.
        </p>
    </div>

    <div class="d-flex gap-2 no-print">
        <a href="<?= url('attendance/roster') ?>" class="btn btn-outline-primary btn-sm fw-semibold">
            <i class="bi bi-grid-3x3-gap-fill me-1"></i> Lưới Xếp Ca Tuần
        </a>
        <a href="<?= url('attendance/monthly') ?>" class="btn btn-outline-success btn-sm fw-semibold">
            <i class="bi bi-calendar3-range me-1"></i> Bảng Công Tháng
        </a>
        <?php if (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI', 'KETOAN_LUONG'])): ?>
        <a href="<?= url('attendance/import') ?>" class="btn btn-primary btn-sm fw-semibold" style="background: #0033A0; border-color: #0033A0;">
            <i class="bi bi-file-earmark-arrow-up-fill me-1"></i> Import Máy Chấm Công
        </a>
        <?php endif; ?>
    </div>
</div>

<!-- Bộ lọc tìm kiếm ngày -->
<div class="card card-custom p-3 mb-4 no-print border-0 shadow-sm" style="border-radius: 8px;">
    <form action="<?= url('attendance') ?>" method="GET" class="row g-2 align-items-end">
        <div class="col-md-2">
            <label class="form-label small fw-semibold text-muted mb-1">Từ ngày</label>
            <input type="date" name="from_date" class="form-control form-control-sm" value="<?= e($filters['from_date'] ?? '') ?>">
        </div>

        <div class="col-md-2">
            <label class="form-label small fw-semibold text-muted mb-1">Đến ngày</label>
            <input type="date" name="to_date" class="form-control form-control-sm" value="<?= e($filters['to_date'] ?? '') ?>">
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
            <label class="form-label small fw-semibold text-muted mb-1">Mã NV / Họ tên</label>
            <input type="text" name="ma_nv" class="form-control form-control-sm" placeholder="Mã nhân sự..." value="<?= e($filters['ma_nv'] ?? '') ?>">
        </div>

        <div class="col-md-3 d-flex gap-2">
            <button type="submit" class="btn btn-primary btn-sm w-100 fw-semibold" style="background: #0033A0;">
                <i class="bi bi-funnel-fill me-1"></i> Lọc Chấm Công
            </button>
            <a href="<?= url('attendance') ?>" class="btn btn-light btn-sm border" title="Đặt lại">
                <i class="bi bi-arrow-counterclockwise"></i>
            </a>
        </div>
    </form>
</div>

<!-- Bảng hiển thị chấm công ngày -->
<div class="card card-custom overflow-hidden border-0 shadow-sm" style="border-radius: 8px;">
    <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
            <thead style="background: #F8FAFC; color: #475569; font-size: 0.8rem; text-transform: uppercase; border-bottom: 2px solid #E2E8F0;">
                <tr>
                    <th style="width: 45px;" class="text-center">STT</th>
                    <th>Ngày</th>
                    <th>Mã NV</th>
                    <th>Họ và Tên</th>
                    <th>Đơn Vị</th>
                    <th>Ca Làm Việc</th>
                    <th class="text-center">Giờ Vào</th>
                    <th class="text-center">Giờ Ra</th>
                    <th class="text-center">Đi Trễ / Về Sớm</th>
                    <th class="text-center">Giờ Đêm</th>
                    <th class="text-center">Giờ OT</th>
                    <th class="text-center">Công TT</th>
                    <th class="text-center">Trạng Thái</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($attendanceList)): ?>
                    <tr>
                        <td colspan="13" class="text-center py-5 text-muted">
                            <i class="bi bi-calendar-x fs-1 d-block mb-2 text-secondary"></i>
                            Không tìm thấy dữ liệu chấm công nào trong khoảng thời gian đã chọn.
                        </td>
                    </tr>
                <?php else: 
                    $stt = $pagination['offset'] + 1;
                    foreach ($attendanceList as $att):
                ?>
                    <tr>
                        <td class="text-center text-muted small"><?= $stt++ ?></td>
                        <td>
                            <strong class="text-dark"><?= format_date($att['ngay']) ?></strong>
                            <small class="text-muted d-block"><?= date('l', strtotime($att['ngay'])) ?></small>
                        </td>
                        <td>
                            <strong class="font-monospace text-primary"><?= e($att['ma_nv']) ?></strong>
                        </td>
                        <td>
                            <div class="fw-bold text-dark"><?= e($att['HoTen']) ?></div>
                            <small class="text-muted"><?= e($att['ChucVu'] ?? '') ?></small>
                        </td>
                        <td>
                            <span class="badge bg-light text-dark border"><?= e($att['TenDV'] ?: $att['MaDV']) ?></span>
                        </td>
                        <td>
                            <span class="badge bg-primary-subtle text-primary border border-primary-subtle font-monospace">
                                <?= e($att['ma_ca']) ?>
                            </span>
                        </td>
                        <td class="text-center font-monospace">
                            <?= !empty($att['gio_vao']) ? substr($att['gio_vao'], 0, 5) : '<span class="text-danger fw-bold">--:--</span>' ?>
                        </td>
                        <td class="text-center font-monospace">
                            <?= !empty($att['gio_ra']) ? substr($att['gio_ra'], 0, 5) : '<span class="text-danger fw-bold">--:--</span>' ?>
                        </td>
                        <td class="text-center">
                            <?php if ($att['di_muon_phut'] > 0): ?>
                                <span class="badge bg-warning text-dark me-1" title="Đi muộn <?= $att['di_muon_phut'] ?> phút">
                                    Trễ <?= $att['di_muon_phut'] ?>p
                                </span>
                            <?php endif; ?>
                            <?php if ($att['ve_som_phut'] > 0): ?>
                                <span class="badge bg-warning text-dark" title="Về sớm <?= $att['ve_som_phut'] ?> phút">
                                    Sớm <?= $att['ve_som_phut'] ?>p
                                </span>
                            <?php endif; ?>
                            <?php if ($att['di_muon_phut'] == 0 && $att['ve_som_phut'] == 0): ?>
                                <span class="text-success small"><i class="bi bi-check-circle"></i> Đúng giờ</span>
                            <?php endif; ?>
                        </td>
                        <td class="text-center">
                            <?php if ($att['gio_lam_dem'] > 0): ?>
                                <span class="badge bg-purple text-white" style="background: #7C3AED;" title="Làm việc ban đêm (22:00 - 06:00)">
                                    <i class="bi bi-moon-stars-fill me-1"></i><?= $att['gio_lam_dem'] ?>h
                                </span>
                            <?php else: ?>
                                <span class="text-muted small">-</span>
                            <?php endif; ?>
                        </td>
                        <td class="text-center">
                            <?php if ($att['gio_lam_them'] > 0): ?>
                                <span class="badge bg-info text-dark fw-bold">
                                    +<?= $att['gio_lam_them'] ?>h
                                </span>
                            <?php else: ?>
                                <span class="text-muted small">-</span>
                            <?php endif; ?>
                        </td>
                        <td class="text-center fw-bold text-dark">
                            <?= number_format((float)$att['cong_thuc_te'], 1) ?>
                        </td>
                        <td class="text-center">
                            <?php if ($att['quen_quet'] == 1): ?>
                                <span class="badge bg-danger-subtle text-danger border border-danger-subtle">
                                    <i class="bi bi-exclamation-triangle-fill me-1"></i>Quên quẹt
                                </span>
                            <?php else: ?>
                                <span class="badge bg-success-subtle text-success border border-success-subtle">
                                    <?= e($att['trang_thai_duyet']) ?>
                                </span>
                            <?php endif; ?>
                        </td>
                    </tr>
                <?php endforeach; endif; ?>
            </tbody>
        </table>
    </div>
</div>
