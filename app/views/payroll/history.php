<?php
/**
 * Lịch sử lương của một nhân viên
 * Biến: $emp, $periods (bangluong, mới nhất trước), $grades (qt_luong, mới nhất trước), $luongCoSo
 */
$years = [];
foreach ($periods as $p) { $years[(int)$p['Nam']][] = $p; }
$chart = array_reverse(array_slice($periods, 0, 12));
$maxNet = max(1, ...array_map(fn ($p) => (float)$p['ThucLinh'], $chart ?: [['ThucLinh' => 1]]));
$statusLabel = ['ChuaTinh' => 'Chưa tính', 'DaTinh' => 'Đã tính', 'KTDuyet' => 'Đã khóa', 'DaChi' => 'Đã chi'];
$gradesAsc = array_reverse($grades);
?>
<style>
    .ph-num { text-align: right; white-space: nowrap; font-variant-numeric: tabular-nums; }
    .ph-bars { display: flex; align-items: flex-end; gap: 10px; height: 180px; padding: 10px 4px 0; border-bottom: 1px solid var(--border-color); }
    .ph-bar { flex: 1; display: flex; flex-direction: column; align-items: center; justify-content: flex-end; height: 100%; min-width: 0; text-decoration: none; }
    .ph-bar span.fill { width: 100%; max-width: 44px; background: var(--vnm-primary); border-radius: 4px 4px 0 0; transition: opacity .15s; }
    .ph-bar:hover span.fill { opacity: .75; }
    .ph-bar small { font-size: .7rem; color: var(--text-secondary); margin-top: 4px; white-space: nowrap; }
    .ph-bar b { font-size: .68rem; color: var(--text-primary); margin-bottom: 4px; white-space: nowrap; }
    .ph-up { color: var(--vnm-success); font-weight: 700; }
    .ph-year td { background: var(--table-header-bg); font-weight: 800; }
</style>

<div style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 12px; margin-bottom: 18px;">
    <div>
        <a href="<?= url('luong') ?>" style="font-size: .85rem;">← Bảng lương</a>
        <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800; margin-top: 6px;">Lịch sử lương — <?= htmlspecialchars($emp['HoTen']) ?></h2>
        <p style="font-size: .85rem; color: var(--text-secondary);"><?= htmlspecialchars($emp['MaNV']) ?> · <?= htmlspecialchars($emp['TenCV'] ?? '') ?> · <?= htmlspecialchars($emp['TenDV'] ?? '') ?> · vào làm <?= format_date($emp['NgayVaoLam']) ?></p>
    </div>
    <a class="btn btn-primary btn-sm" href="<?= url('luong/detail/' . $emp['MaNV']) ?>">Chi tiết tính lương tháng này</a>
</div>

<div class="stats-grid" style="grid-template-columns: repeat(auto-fit, minmax(190px, 1fr)); margin-bottom: 20px;">
    <div class="stat-card" style="padding: 16px;"><div class="stat-details">
        <span class="stat-number" style="font-size: 1.25rem;"><?= number_format((float)$emp['HeSoLuongHienTai'], 2, ',', '') ?> · bậc <?= (int)$emp['BacLuongHienTai'] ?></span>
        <span class="stat-label">Hệ số · bậc lương hiện tại</span></div></div>
    <div class="stat-card" style="padding: 16px;"><div class="stat-details">
        <span class="stat-number" style="font-size: 1.25rem;"><?= format_money($luongCoSo * (float)$emp['HeSoLuongHienTai']) ?></span>
        <span class="stat-label">P1 đủ tháng hiện tại</span></div></div>
    <div class="stat-card" style="padding: 16px;"><div class="stat-details">
        <span class="stat-number" style="font-size: 1.25rem;"><?= count($periods) ?> kỳ</span>
        <span class="stat-label">Số kỳ lương đã có</span></div></div>
    <div class="stat-card" style="padding: 16px;"><div class="stat-details">
        <span class="stat-number" style="font-size: 1.25rem; color: var(--vnm-success);"><?= $periods ? format_money($periods[0]['ThucLinh']) : '—' ?></span>
        <span class="stat-label">Thực lĩnh kỳ gần nhất<?= $periods ? ' (T' . $periods[0]['Thang'] . '/' . $periods[0]['Nam'] . ')' : '' ?></span></div></div>
</div>

<?php if ($chart): ?>
<div class="card-box" style="margin-bottom: 20px;">
    <h3 style="font-size: 1rem; font-weight: 800; color: var(--vnm-navy);">Thực lĩnh <?= count($chart) ?> kỳ gần nhất</h3>
    <p style="font-size: .78rem; color: var(--text-secondary);">Bấm vào một cột để xem chi tiết tính lương tháng đó.</p>
    <div class="ph-bars" role="img" aria-label="Biểu đồ thực lĩnh theo tháng">
        <?php foreach ($chart as $p): $h = max(2, round((float)$p['ThucLinh'] / $maxNet * 140)); ?>
            <a class="ph-bar" href="<?= url('luong/detail/' . $emp['MaNV'] . '?thang=' . $p['Thang'] . '&nam=' . $p['Nam']) ?>" title="Tháng <?= $p['Thang'] ?>/<?= $p['Nam'] ?>: <?= format_money($p['ThucLinh']) ?>">
                <b><?= number_format((float)$p['ThucLinh'] / 1000000, 1, ',', '') ?>tr</b>
                <span class="fill" style="height: <?= $h ?>px;"></span>
                <small>T<?= $p['Thang'] ?>/<?= substr((string)$p['Nam'], 2) ?></small>
            </a>
        <?php endforeach; ?>
    </div>
</div>
<?php endif; ?>

<div class="card-box" style="margin-bottom: 20px;">
    <h3 style="font-size: 1rem; font-weight: 800; color: var(--vnm-navy); margin-bottom: 10px;">Các kỳ lương đã trả</h3>
    <div class="table-responsive">
        <table class="table" style="min-width: 1180px;">
            <thead><tr>
                <th>Kỳ lương</th><th class="ph-num">Công</th><th class="ph-num">Hệ số</th><th class="ph-num">P1 + P2 + P3</th><th class="ph-num">Phụ cấp</th>
                <th class="ph-num">Tăng ca + thưởng</th><th class="ph-num">Tổng thu nhập</th><th class="ph-num">Bảo hiểm</th><th class="ph-num">Thuế TNCN</th>
                <th class="ph-num">Thực lĩnh</th><th>Trạng thái</th><th style="text-align: right;">Xem</th>
            </tr></thead>
            <tbody>
            <?php if (!$periods): ?>
                <tr><td colspan="12" style="text-align: center; color: var(--text-muted); padding: 24px;">Chưa có kỳ lương nào. Mở Bảng lương của một tháng để hệ thống tính.</td></tr>
            <?php endif; ?>
            <?php foreach ($years as $y => $list):
                $sumY = fn ($k) => array_sum(array_map(fn ($r) => (float)$r[$k], $list)); ?>
                <tr class="ph-year"><td colspan="6">Năm <?= $y ?> · <?= count($list) ?> kỳ</td>
                    <td class="ph-num"><?= format_money($sumY('TongThuNhap')) ?></td><td class="ph-num"><?= format_money($sumY('KhauTruBHXH')) ?></td>
                    <td class="ph-num"><?= format_money($sumY('ThueTNCN')) ?></td><td class="ph-num"><?= format_money($sumY('ThucLinh')) ?></td><td colspan="2"></td></tr>
                <?php foreach ($list as $p): ?>
                <tr>
                    <td style="white-space: nowrap;"><strong>Tháng <?= $p['Thang'] ?>/<?= $p['Nam'] ?></strong><?php if (!empty($p['GhiChu'])): ?> <span title="<?= htmlspecialchars($p['GhiChu']) ?>" style="display: inline-block; padding: 1px 7px; border-radius: 999px; font-size: .68rem; font-weight: 700; background: rgba(245, 158, 11, .15); color: var(--vnm-warning);">Tạm tính</span><?php endif; ?></td>
                    <td class="ph-num"><?= rtrim(rtrim(number_format((float)$p['SoNgayCong'], 1, ',', ''), '0'), ',') ?></td>
                    <td class="ph-num"><?= number_format((float)$p['HeSoLuong'], 2, ',', '') ?></td>
                    <td class="ph-num"><?= format_money($p['LuongCoBan'] + $p['LuongNangLuc'] + $p['ThuongKPI']) ?></td>
                    <td class="ph-num"><?= format_money($p['PhuCapChucVu'] + $p['PhuCapKhac']) ?></td>
                    <td class="ph-num"><?= format_money($p['ThuongKhac']) ?></td>
                    <td class="ph-num" style="font-weight: 700;"><?= format_money($p['TongThuNhap']) ?></td>
                    <td class="ph-num"><?= format_money($p['KhauTruBHXH']) ?></td>
                    <td class="ph-num"><?= format_money($p['ThueTNCN']) ?></td>
                    <td class="ph-num" style="font-weight: 800; color: var(--vnm-success);"><?= format_money($p['ThucLinh']) ?></td>
                    <td style="white-space: nowrap;"><span class="badge <?= in_array($p['TrangThaiDuyet'], ['KTDuyet', 'DaChi'], true) ? 'badge-success' : 'badge-primary' ?>"><?= $statusLabel[$p['TrangThaiDuyet']] ?? $p['TrangThaiDuyet'] ?></span></td>
                    <td style="text-align: right; white-space: nowrap;">
                        <a class="btn btn-outline btn-sm" href="<?= url('luong/detail/' . $emp['MaNV'] . '?thang=' . $p['Thang'] . '&nam=' . $p['Nam']) ?>">Chi tiết</a>
                        <a class="btn btn-outline btn-sm" target="_blank" href="<?= url('luong/payslip/' . $emp['MaNV'] . '?thang=' . $p['Thang'] . '&nam=' . $p['Nam']) ?>">Phiếu</a>
                    </td>
                </tr>
                <?php endforeach; ?>
            <?php endforeach; ?>
            </tbody>
        </table>
    </div>
</div>

<div class="card-box">
    <h3 style="font-size: 1rem; font-weight: 800; color: var(--vnm-navy); margin-bottom: 10px;">Diễn biến hệ số và bậc lương (quyết định nâng lương)</h3>
    <div class="table-responsive">
        <table class="table">
            <thead><tr><th>Từ ngày</th><th>Số quyết định</th><th class="ph-num">Bậc</th><th class="ph-num">Hệ số</th><th class="ph-num">Thay đổi</th><th class="ph-num">P1 đủ tháng tương ứng</th><th>Ghi chú</th></tr></thead>
            <tbody>
            <?php if (!$grades): ?>
                <tr><td colspan="7" style="text-align: center; color: var(--text-muted); padding: 24px;">Chưa có quyết định nâng lương nào được ghi nhận.</td></tr>
            <?php endif; ?>
            <?php foreach ($grades as $i => $g):
                $prev = $grades[$i + 1] ?? null;
                $delta = $prev ? (float)$g['HeSoLuong'] - (float)$prev['HeSoLuong'] : null; ?>
                <tr>
                    <td><strong><?= format_date($g['TuNgay']) ?></strong></td>
                    <td><?= htmlspecialchars($g['SoQD'] ?? '—') ?></td>
                    <td class="ph-num"><?= (int)$g['BacLuong'] ?></td>
                    <td class="ph-num"><?= number_format((float)$g['HeSoLuong'], 2, ',', '') ?><?= (float)$g['VuotKhung'] > 0 ? ' (+' . number_format((float)$g['VuotKhung'], 0) . '% VK)' : '' ?></td>
                    <td class="ph-num"><?= $delta === null ? 'Khởi điểm' : '<span class="ph-up">' . ($delta >= 0 ? '+' : '') . number_format($delta, 2, ',', '') . '</span>' ?></td>
                    <td class="ph-num"><?= format_money($luongCoSo * (float)$g['HeSoLuong']) ?></td>
                    <td><?= htmlspecialchars($g['GhiChu'] ?? '') ?></td>
                </tr>
            <?php endforeach; ?>
            </tbody>
        </table>
    </div>
    <p style="font-size: .74rem; color: var(--text-muted); margin-top: 8px;">P1 đủ tháng = lương cơ sở nội bộ <?= format_money($luongCoSo) ?> × hệ số, tính theo mức hiện hành để dễ so sánh giữa các lần nâng lương.</p>
</div>
