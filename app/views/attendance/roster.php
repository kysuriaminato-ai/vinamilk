<div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
    <div>
        <h3 class="fw-bold text-dark mb-1">
            <i class="bi bi-grid-3x3-gap-fill text-primary me-2"></i>Lưới Xếp Ca Sản Xuất Tuần
        </h3>
        <p class="text-muted small mb-0">
            Kế hoạch phân công 4 ca chuẩn: <strong>Ca 1 (06:00-14:00)</strong>, <strong>Ca 2 (14:00-22:00)</strong>, <strong>Ca 3 (22:00-06:00 qua đêm)</strong>, <strong>Hành chính (08:00-17:00)</strong>.
        </p>
    </div>

    <div class="d-flex gap-2 no-print">
        <a href="<?= url('attendance') ?>" class="btn btn-outline-secondary btn-sm fw-semibold">
            <i class="bi bi-calendar-check me-1"></i> Chấm Công Ngày
        </a>
        <a href="<?= url('attendance/monthly') ?>" class="btn btn-outline-success btn-sm fw-semibold">
            <i class="bi bi-calendar3-range me-1"></i> Bảng Công Tháng
        </a>
        <button type="button" class="btn btn-outline-primary btn-sm fw-semibold" onclick="window.print()">
            <i class="bi bi-printer-fill me-1"></i> In Lịch Tuần
        </button>
    </div>
</div>

<!-- Thanh điều hướng Tuần & Bộ lọc Đơn vị -->
<div class="card card-custom p-3 mb-4 no-print border-0 shadow-sm" style="border-radius: 8px;">
    <form action="<?= url('attendance/roster') ?>" method="GET" class="row g-3 align-items-center">
        <?php
            $prevWeek = date('Y-m-d', strtotime($mondayDate . ' -7 days'));
            $nextWeek = date('Y-m-d', strtotime($mondayDate . ' +7 days'));
            $thisWeek = date('Y-m-d', strtotime('monday this week'));
        ?>
        <div class="col-md-5 d-flex align-items-center gap-2">
            <a href="<?= url('attendance/roster?ma_dv=' . urlencode($currentUnit) . '&week_date=' . $prevWeek) ?>" class="btn btn-light btn-sm border" title="Tuần trước">
                <i class="bi bi-chevron-left"></i>
            </a>
            <a href="<?= url('attendance/roster?ma_dv=' . urlencode($currentUnit) . '&week_date=' . $thisWeek) ?>" class="btn btn-outline-primary btn-sm fw-semibold">
                Tuần Này
            </a>
            <a href="<?= url('attendance/roster?ma_dv=' . urlencode($currentUnit) . '&week_date=' . $nextWeek) ?>" class="btn btn-light btn-sm border" title="Tuần sau">
                <i class="bi bi-chevron-right"></i>
            </a>
            <input type="date" name="week_date" class="form-control form-control-sm ms-2" style="max-width: 160px;" value="<?= e($mondayDate) ?>" onchange="this.form.submit()">
            <span class="badge bg-light text-dark border py-2 px-3">
                <i class="bi bi-calendar-range me-1 text-primary"></i>
                <?= date('d/m/Y', strtotime($mondayDate)) ?> &rarr; <?= date('d/m/Y', strtotime($mondayDate . ' +6 days')) ?>
            </span>
        </div>

        <div class="col-md-5">
            <div class="d-flex align-items-center gap-2">
                <label class="form-label small fw-semibold text-muted mb-0 text-nowrap">Tổ / Đơn vị:</label>
                <select name="ma_dv" class="form-select form-select-sm" onchange="this.form.submit()">
                    <option value="ALL">-- Tất cả đơn vị --</option>
                    <?php foreach ($selectableUnits as $u): ?>
                        <option value="<?= e($u['MaDV']) ?>" <?= ($currentUnit === $u['MaDV']) ? 'selected' : '' ?>>
                            <?= e($u['IndentTitle'] ?? $u['TenDV']) ?>
                        </option>
                    <?php endforeach; ?>
                </select>
            </div>
        </div>

        <div class="col-md-2 text-end">
            <button type="submit" class="btn btn-primary btn-sm w-100 fw-semibold" style="background: #0033A0;">
                <i class="bi bi-filter me-1"></i> Xem Lịch
            </button>
        </div>
    </form>
</div>

<!-- Chú thích ca làm việc (Legend) -->
<div class="card card-custom p-3 mb-3 border-0 shadow-sm" style="border-radius: 8px; background: #FAFBFD;">
    <div class="d-flex flex-wrap align-items-center justify-content-between gap-2">
        <div class="d-flex flex-wrap align-items-center gap-3">
            <span class="small fw-bold text-muted text-uppercase">Quy ước ca:</span>
            <span class="badge px-2 py-1" style="background: #0033A0; color: #fff;">
                <i class="bi bi-sun-fill me-1"></i> Ca 1 (06:00 - 14:00)
            </span>
            <span class="badge px-2 py-1" style="background: #D97706; color: #fff;">
                <i class="bi bi-brightness-high-fill me-1"></i> Ca 2 (14:00 - 22:00)
            </span>
            <span class="badge px-2 py-1" style="background: #7C3AED; color: #fff;">
                <i class="bi bi-moon-stars-fill me-1"></i> Ca 3 (22:00 - 06:00 Đêm)
            </span>
            <span class="badge px-2 py-1" style="background: #059669; color: #fff;">
                <i class="bi bi-building me-1"></i> Hành chính (08:00 - 17:00)
            </span>
            <span class="badge px-2 py-1" style="background: #64748B; color: #fff;">
                <i class="bi bi-cup-hot-fill me-1"></i> OFF (Nghỉ tuần)
            </span>
        </div>

        <?php if (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI', 'QUANLY_TRUCTIEP'])): ?>
        <div class="d-flex align-items-center gap-2 no-print">
            <span class="small text-muted">Gán nhanh hàng loạt:</span>
            <button type="button" class="btn btn-xs btn-outline-primary py-1 px-2" style="font-size: 0.75rem;" onclick="batchApplyShift('CA_1_SANG')">Set Ca 1</button>
            <button type="button" class="btn btn-xs btn-outline-warning py-1 px-2" style="font-size: 0.75rem;" onclick="batchApplyShift('CA_2_CHIEU')">Set Ca 2</button>
            <button type="button" class="btn btn-xs btn-outline-purple py-1 px-2" style="font-size: 0.75rem; color: #7C3AED; border-color: #7C3AED;" onclick="batchApplyShift('CA_3_DEM')">Set Ca 3</button>
            <button type="button" class="btn btn-xs btn-outline-success py-1 px-2" style="font-size: 0.75rem;" onclick="batchApplyShift('CA_HANH_CHINH')">Set HC</button>
        </div>
        <?php endif; ?>
    </div>
</div>

<!-- Lưới xếp ca tuần (Weekly Roster Form) -->
<form action="<?= url('attendance/save_roster') ?>" method="POST" id="rosterForm">
    <input type="hidden" name="csrf_token" value="<?= csrf_token() ?>">
    <input type="hidden" name="ma_dv" value="<?= e($currentUnit) ?>">
    <input type="hidden" name="monday_date" value="<?= e($mondayDate) ?>">

    <div class="card card-custom overflow-hidden border-0 shadow-sm mb-4" style="border-radius: 8px;">
        <div class="table-responsive">
            <table class="table table-bordered align-middle mb-0 text-center" id="rosterTable">
                <thead style="background: #F1F5F9; color: #334155; font-size: 0.82rem; text-transform: uppercase;">
                    <tr>
                        <th style="width: 50px;" class="align-middle">STT</th>
                        <th style="min-width: 200px;" class="text-start align-middle">Nhân Viên / Vị Trí</th>
                        <th style="min-width: 120px;" class="text-start align-middle">Tổ / Đơn Vị</th>
                        <?php foreach ($roster['dates'] as $d): 
                            $isSunday = (date('N', strtotime($d['date'])) == 7);
                            $isToday  = ($d['date'] === date('Y-m-d'));
                        ?>
                            <th style="min-width: 130px; background: <?= $isToday ? '#EFF6FF' : ($isSunday ? '#FEF2F2' : '#F1F5F9') ?>;">
                                <div class="fw-bold <?= $isToday ? 'text-primary' : ($isSunday ? 'text-danger' : '') ?>"><?= e($d['day_name']) ?></div>
                                <div class="small fw-normal text-muted"><?= e($d['display']) ?></div>
                            </th>
                        <?php endforeach; ?>
                    </tr>
                </thead>
                <tbody>
                    <?php if (empty($roster['employees'])): ?>
                        <tr>
                            <td colspan="10" class="py-5 text-muted">
                                <i class="bi bi-people fs-1 d-block mb-2 text-secondary"></i>
                                Không có nhân sự nào trong đơn vị đã chọn hoặc bạn chưa có quyền quản lý đơn vị này.
                            </td>
                        </tr>
                    <?php else: 
                        $stt = 1;
                        foreach ($roster['employees'] as $emp):
                    ?>
                        <tr>
                            <td class="text-muted small"><?= $stt++ ?></td>
                            <td class="text-start">
                                <div class="fw-bold text-dark"><?= e($emp['HoTen']) ?></div>
                                <div class="small text-muted font-monospace"><?= e($emp['MaNV']) ?> &bull; <?= e($emp['TenCV'] ?? '') ?></div>
                            </td>
                            <td class="text-start">
                                <span class="badge bg-light text-dark border"><?= e($emp['TenDV'] ?: $emp['MaDV']) ?></span>
                            </td>

                            <?php foreach ($roster['dates'] as $d): 
                                $dt = $d['date'];
                                $curShift = $emp['shifts'][$dt] ?? 'CA_1_SANG';
                                $isSun = (date('N', strtotime($dt)) == 7);
                            ?>
                                <td class="p-1 roster-cell" style="background: <?= $isSun ? '#FFFBFB' : '#FFF' ?>;">
                                    <?php if (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI', 'QUANLY_TRUCTIEP'])): ?>
                                        <select name="shift[<?= e($emp['MaNV']) ?>][<?= e($dt) ?>]" 
                                                class="form-select form-select-sm shift-select text-center fw-semibold" 
                                                style="font-size: 0.78rem; border-radius: 6px;"
                                                onchange="updateShiftStyle(this)">
                                            <option value="CA_1_SANG" <?= ($curShift === 'CA_1_SANG') ? 'selected' : '' ?>>Ca 1 (06-14)</option>
                                            <option value="CA_2_CHIEU" <?= ($curShift === 'CA_2_CHIEU') ? 'selected' : '' ?>>Ca 2 (14-22)</option>
                                            <option value="CA_3_DEM" <?= ($curShift === 'CA_3_DEM') ? 'selected' : '' ?>>Ca 3 (22-06Đ)</option>
                                            <option value="CA_HANH_CHINH" <?= ($curShift === 'CA_HANH_CHINH') ? 'selected' : '' ?>>H.Chính (08-17)</option>
                                            <option value="OFF" <?= ($curShift === 'OFF') ? 'selected' : '' ?>>OFF (Nghỉ)</option>
                                        </select>
                                    <?php else: ?>
                                        <span class="badge shift-badge py-2 px-2 w-100" data-shift="<?= e($curShift) ?>">
                                            <?= match($curShift) {
                                                'CA_1_SANG' => 'Ca 1 (06-14)',
                                                'CA_2_CHIEU' => 'Ca 2 (14-22)',
                                                'CA_3_DEM' => 'Ca 3 (22-06Đ)',
                                                'CA_HANH_CHINH' => 'H.Chính',
                                                'OFF' => 'OFF',
                                                default => $curShift
                                            } ?>
                                        </span>
                                    <?php endif; ?>
                                </td>
                            <?php endforeach; ?>
                        </tr>
                    <?php endforeach; endif; ?>
                </tbody>
            </table>
        </div>

        <?php if (!empty($roster['employees']) && Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI', 'QUANLY_TRUCTIEP'])): ?>
        <div class="card-footer bg-white p-3 d-flex justify-content-between align-items-center no-print border-top">
            <span class="small text-muted">
                <i class="bi bi-info-circle me-1"></i> Sau khi thay đổi ca, bấm <strong>Lưu Kế Hoạch Xếp Ca</strong> để áp dụng lịch làm việc tự động cho máy chấm công.
            </span>
            <div class="d-flex gap-2">
                <button type="reset" class="btn btn-outline-secondary btn-sm px-3">
                    <i class="bi bi-arrow-counterclockwise me-1"></i> Hoàn Tác
                </button>
                <button type="submit" class="btn btn-primary btn-sm px-4 fw-semibold" style="background: #0033A0;">
                    <i class="bi bi-check2-circle me-1"></i> Lưu Kế Hoạch Xếp Ca Tuần
                </button>
            </div>
        </div>
        <?php endif; ?>
    </div>
</form>

<script>
document.addEventListener('DOMContentLoaded', function() {
    // Cập nhật màu sắc cho tất cả dropdown ca
    document.querySelectorAll('.shift-select').forEach(function(sel) {
        updateShiftStyle(sel);
    });

    document.querySelectorAll('.shift-badge').forEach(function(badge) {
        const s = badge.getAttribute('data-shift');
        applyBadgeColor(badge, s);
    });
});

function updateShiftStyle(selectElem) {
    const val = selectElem.value;
    selectElem.style.color = '#FFFFFF';
    selectElem.style.fontWeight = '600';

    if (val === 'CA_1_SANG') {
        selectElem.style.backgroundColor = '#0033A0';
        selectElem.style.borderColor = '#0033A0';
    } else if (val === 'CA_2_CHIEU') {
        selectElem.style.backgroundColor = '#D97706';
        selectElem.style.borderColor = '#D97706';
    } else if (val === 'CA_3_DEM') {
        selectElem.style.backgroundColor = '#7C3AED';
        selectElem.style.borderColor = '#7C3AED';
    } else if (val === 'CA_HANH_CHINH') {
        selectElem.style.backgroundColor = '#059669';
        selectElem.style.borderColor = '#059669';
    } else { // OFF
        selectElem.style.backgroundColor = '#64748B';
        selectElem.style.borderColor = '#64748B';
    }
}

function applyBadgeColor(elem, shiftVal) {
    elem.style.color = '#FFFFFF';
    elem.style.fontWeight = '600';
    if (shiftVal === 'CA_1_SANG') elem.style.backgroundColor = '#0033A0';
    else if (shiftVal === 'CA_2_CHIEU') elem.style.backgroundColor = '#D97706';
    else if (shiftVal === 'CA_3_DEM') elem.style.backgroundColor = '#7C3AED';
    else if (shiftVal === 'CA_HANH_CHINH') elem.style.backgroundColor = '#059669';
    else elem.style.backgroundColor = '#64748B';
}

function batchApplyShift(targetShift) {
    if (!confirm('Bạn có chắc chắn muốn gán hàng loạt tất cả các ô trong tuần (trừ Chủ nhật) thành ca đã chọn?')) {
        return;
    }
    const selects = document.querySelectorAll('.shift-select');
    selects.forEach(function(sel) {
        const nameAttr = sel.getAttribute('name'); // shift[VNM-xxxx][YYYY-MM-DD]
        // Kiểm tra xem ô có phải là Chủ nhật không
        const matches = nameAttr.match(/\[([0-9]{4}-[0-9]{2}-[0-9]{2})\]/);
        if (matches && matches[1]) {
            const dt = new Date(matches[1]);
            if (dt.getDay() === 0) { // Chủ nhật giữ nguyên OFF
                sel.value = 'OFF';
            } else {
                sel.value = targetShift;
            }
        } else {
            sel.value = targetShift;
        }
        updateShiftStyle(sel);
    });
}
</script>
