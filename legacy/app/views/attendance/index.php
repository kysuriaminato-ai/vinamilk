<?php
/**
 * View Bảng Chấm công Ma trận Tháng (Grid Calendar 30/31 ngày & Ajax)
 */
$daysInMonth = $gridData['daysInMonth'];
$grid = $gridData['grid'];
?>
<div class="card-box">
    <!-- Header Title & Action Buttons -->
    <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 24px; flex-wrap: wrap; gap: 16px;">
        <div>
            <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;">
                ⏰ Bảng Chấm công Ma trận Tháng <?= $thang ?>/<?= $nam ?>
            </h2>
            <p style="font-size: 0.85rem; color: var(--text-secondary); margin-top: 4px;">
                Quản lý ca làm việc (Hành chính, Ca 1, Ca 2, Ca 3) và điểm danh 30/31 ngày trực quan thời gian thực.
            </p>
        </div>

        <div style="display: flex; gap: 10px; flex-wrap: wrap;">
            <a href="<?= url('chamcong/leave') ?>" class="btn btn-outline">
                📋 Quản lý Đơn nghỉ phép
            </a>
            <?php if (has_permission('CHAM_CONG.Add')): ?>
                <a href="<?= url('chamcong/import') ?>" class="btn btn-primary">
                    📥 Import Máy Chấm công
                </a>
            <?php endif; ?>
        </div>
    </div>

    <!-- Filter Bar -->
    <form action="<?= url('chamcong') ?>" method="GET" style="background: #f8fafc; padding: 16px 20px; border-radius: var(--radius-lg); border: 1px solid var(--border-color); margin-bottom: 24px; display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 16px;">
        <div style="display: flex; align-items: center; gap: 14px; flex-wrap: wrap;">
            <div style="display: flex; align-items: center; gap: 8px;">
                <label class="form-label" style="margin-bottom:0;">Tháng:</label>
                <select name="thang" class="form-control" style="width: auto;">
                    <?php for ($m = 1; $m <= 12; $m++): ?>
                        <option value="<?= $m ?>" <?= ($m == $thang) ? 'selected' : '' ?>>Tháng <?= $m ?></option>
                    <?php endfor; ?>
                </select>
            </div>

            <div style="display: flex; align-items: center; gap: 8px;">
                <label class="form-label" style="margin-bottom:0;">Năm:</label>
                <select name="nam" class="form-control" style="width: auto;">
                    <option value="2026" selected>2026</option>
                    <option value="2025">2025</option>
                </select>
            </div>

            <div style="display: flex; align-items: center; gap: 8px;">
                <label class="form-label" style="margin-bottom:0;">Đơn vị:</label>
                <select name="ma_dv" class="form-control" style="width: auto;">
                    <option value="">-- Tất cả Đơn vị --</option>
                    <?php foreach ($options['donvi'] as $dv): ?>
                        <option value="<?= $dv['MaDV'] ?>" <?= ($maDV === $dv['MaDV']) ? 'selected' : '' ?>>
                            <?= htmlspecialchars($dv['TenDV']) ?>
                        </option>
                    <?php endforeach; ?>
                </select>
            </div>

            <button type="submit" class="btn btn-primary btn-sm">🔍 Xem Bảng Công</button>
        </div>

        <!-- Legend Chấm công -->
        <div style="display: flex; align-items: center; gap: 12px; font-size: 0.8rem; font-weight: 600;">
            <span style="color:#059669;">X: Đủ công</span>
            <span style="color:#2563eb;">P: Phép năm</span>
            <span style="color:#dc2626;">O: Không lương</span>
            <span style="color:#d97706;">TC: Tăng ca</span>
            <span style="color:#7c3aed;">TS: Thai sản</span>
        </div>
    </form>

    <!-- Attendance Grid Table -->
    <div class="table-responsive" style="max-height: 550px; overflow-y: auto;">
        <table class="table" style="font-size: 0.8rem; border-collapse: separate; border-spacing: 0;">
            <thead>
                <tr style="position: sticky; top: 0; z-index: 10;">
                    <th style="min-width: 180px; position: sticky; left: 0; background: #f8fafc; z-index: 12;">Nhân sự</th>
                    <?php for ($d = 1; $d <= $daysInMonth; $d++): ?>
                        <?php 
                        $dw = date('w', strtotime("{$nam}-{$thang}-{$d}"));
                        $isSun = ($dw == 0);
                        ?>
                        <th style="text-align: center; min-width: 36px; padding: 6px 2px; <?= $isSun ? 'background: #fecaca; color: #991b1b;' : '' ?>">
                            <?= $d ?><br>
                            <span style="font-size: 0.65rem; font-weight: normal;"><?= ($dw == 0) ? 'CN' : 'T' . ($dw + 1) ?></span>
                        </th>
                    <?php endfor; ?>
                    <th style="text-align: center; min-width: 60px;">Công thực</th>
                    <th style="text-align: center; min-width: 50px;">Phép</th>
                    <th style="text-align: center; min-width: 50px;">Nghỉ</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($grid)): ?>
                    <tr>
                        <td colspan="<?= $daysInMonth + 4 ?>" style="text-align: center; color: var(--text-muted); padding: 30px;">
                            Không có nhân sự nào được tìm thấy.
                        </td>
                    </tr>
                <?php else: ?>
                    <?php foreach ($grid as $g): ?>
                        <tr>
                            <!-- Sticky Employee Info Column -->
                            <td style="position: sticky; left: 0; background: #ffffff; z-index: 5; font-weight: 600; border-right: 2px solid var(--border-color);">
                                <div style="color: var(--vnm-navy); font-size: 0.85rem;"><?= htmlspecialchars($g['HoTen']) ?></div>
                                <div style="font-size: 0.7rem; color: var(--text-secondary);">
                                    <code><?= $g['MaNV'] ?></code> | <?= htmlspecialchars($g['TenDV'] ?? '') ?>
                                </div>
                            </td>

                            <!-- 30/31 Day Interactive Cells -->
                            <?php for ($d = 1; $d <= $daysInMonth; $d++): ?>
                                <?php 
                                $symbol = $g['Days'][$d] ?? 'X';
                                $dw = date('w', strtotime("{$nam}-{$thang}-{$d}"));
                                $cellBg = match($symbol) {
                                    'X' => '#ecfdf5',
                                    'P' => '#eff6ff',
                                    'O' => '#fef2f2',
                                    'TC' => '#fffbeb',
                                    'TS' => '#f3e8ff',
                                    default => '#ffffff'
                                };
                                $cellColor = match($symbol) {
                                    'X' => '#065f46',
                                    'P' => '#1e40af',
                                    'O' => '#991b1b',
                                    'TC' => '#b45309',
                                    'TS' => '#6b21a8',
                                    default => '#000000'
                                };
                                ?>
                                <td style="text-align: center; padding: 2px; background-color: <?= $cellBg ?>;">
                                    <?php if (has_permission('CHAM_CONG.Edit')): ?>
                                        <select class="cell-select" 
                                                data-manv="<?= $g['MaNV'] ?>" 
                                                data-ngay="<?= $d ?>" 
                                                data-thang="<?= $thang ?>" 
                                                data-nam="<?= $nam ?>"
                                                onchange="updateCellAjax(this)"
                                                style="border: none; background: transparent; font-weight: 700; color: <?= $cellColor ?>; cursor: pointer; text-align-last: center; padding: 4px 0;">
                                            <option value="X" <?= ($symbol === 'X') ? 'selected' : '' ?>>X</option>
                                            <option value="P" <?= ($symbol === 'P') ? 'selected' : '' ?>>P</option>
                                            <option value="O" <?= ($symbol === 'O') ? 'selected' : '' ?>>O</option>
                                            <option value="TC" <?= ($symbol === 'TC') ? 'selected' : '' ?>>TC</option>
                                            <option value="TS" <?= ($symbol === 'TS') ? 'selected' : '' ?>>TS</option>
                                            <option value="H" <?= ($symbol === 'H') ? 'selected' : '' ?>>H</option>
                                        </select>
                                    <?php else: ?>
                                        <span style="font-weight: 700; color: <?= $cellColor ?>;"><?= $symbol ?></span>
                                    <?php endif; ?>
                                </td>
                            <?php endfor; ?>

                            <!-- Summary Columns -->
                            <td style="text-align: center; font-weight: 700; color: var(--vnm-success);"><?= $g['TotalCong'] ?></td>
                            <td style="text-align: center; font-weight: 600; color: var(--vnm-info);"><?= $g['TotalPhep'] ?></td>
                            <td style="text-align: center; font-weight: 600; color: var(--vnm-danger);"><?= $g['TotalKhongLuong'] ?></td>
                        </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
        </table>
    </div>
</div>

<script>
function updateCellAjax(selectEl) {
    const maNV = selectEl.dataset.manv;
    const ngay = selectEl.dataset.ngay;
    const thang = selectEl.dataset.thang;
    const nam = selectEl.dataset.nam;
    const kyHieu = selectEl.value;

    selectEl.style.opacity = '0.5';

    const formData = new FormData();
    formData.append('vnm_csrf_token', '<?= csrf_token() ?>');
    formData.append('ma_nv', maNV);
    formData.append('ngay', ngay);
    formData.append('thang', thang);
    formData.append('nam', nam);
    formData.append('ky_hieu', kyHieu);

    fetch('<?= url('chamcong/updateCell') ?>', {
        method: 'POST',
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        body: formData
    })
    .then(response => response.json())
    .then(data => {
        selectEl.style.opacity = '1';
        if (data.success) {
            // Đổi màu nền theo ký hiệu mới
            let cellBg = '#ffffff';
            let cellColor = '#000000';
            if (kyHieu === 'X') { cellBg = '#ecfdf5'; cellColor = '#065f46'; }
            else if (kyHieu === 'P') { cellBg = '#eff6ff'; cellColor = '#1e40af'; }
            else if (kyHieu === 'O') { cellBg = '#fef2f2'; cellColor = '#991b1b'; }
            else if (kyHieu === 'TC') { cellBg = '#fffbeb'; cellColor = '#b45309'; }

            selectEl.parentElement.style.backgroundColor = cellBg;
            selectEl.style.color = cellColor;
        } else {
            alert(data.error || 'Thất bại khi cập nhật công');
        }
    })
    .catch(err => {
        selectEl.style.opacity = '1';
        console.error(err);
    });
}
</script>
