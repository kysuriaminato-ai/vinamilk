<?php
/**
 * Bảng lương tháng của tất cả nhân viên (3P)
 * Biến: $payrollList, $thang, $nam, $maDV, $q, $isLocked, $options
 */
$sum = fn (string $k) => array_sum(array_map(fn ($r) => (float)$r[$k], $payrollList));
$totalGross = $sum('TongThuNhap');
$totalBH = $sum('KhauTruBHXH');
$totalTNCN = $sum('ThueTNCN');
$totalNet = $sum('ThucLinh');
$tamTinh = array_filter($payrollList, fn ($r) => !empty($r['GhiChu']));
$qs = fn (array $extra = []) => http_build_query(array_merge(['thang' => $thang, 'nam' => $nam, 'ma_dv' => $maDV, 'q' => $q], $extra));
$statusLabel = ['ChuaTinh' => 'Chưa tính', 'DaTinh' => 'Đã tính', 'KTDuyet' => 'Đã khóa', 'DaChi' => 'Đã chi'];
?>
<style>
    .pl-num { text-align: right; white-space: nowrap; font-variant-numeric: tabular-nums; }
    .pl-table th { white-space: nowrap; font-size: .78rem; }
    .pl-table td { font-size: .84rem; vertical-align: middle; }
    .pl-actions { display: flex; gap: 6px; justify-content: flex-end; flex-wrap: nowrap; }
    .pl-actions .btn { padding: 4px 10px; font-size: .75rem; white-space: nowrap; }
    .pl-tag { display: inline-block; padding: 2px 8px; border-radius: 999px; font-size: .7rem; font-weight: 700; background: rgba(245, 158, 11, .15); color: var(--vnm-warning); }
    .pl-filter { display: flex; gap: 12px; flex-wrap: wrap; align-items: end; padding: 14px 18px; border: 1px solid var(--border-color); border-radius: var(--radius-lg); margin-bottom: 20px; background: var(--bg-main); }
    .pl-filter label { display: flex; flex-direction: column; gap: 4px; font-size: .78rem; font-weight: 600; color: var(--text-secondary); }
    .pl-legend { font-size: .8rem; color: var(--text-secondary); margin: -6px 0 16px; line-height: 1.6; }
    .pl-row { cursor: pointer; }
    .pl-row:hover td { background: var(--table-row-hover); }
    .pl-total td { font-weight: 800; background: var(--table-header-bg); }
</style>

<div class="card-box">
    <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 20px; flex-wrap: wrap; gap: 16px;">
        <div>
            <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;">Bảng Lương Tháng <?= $thang ?>/<?= $nam ?> — Lương 3P</h2>
            <p style="font-size: 0.85rem; color: var(--text-secondary); margin-top: 4px;">
                <?= count($payrollList) ?> nhân viên · P1 lương vị trí, P2 năng lực, P3 thưởng KPI · bảo hiểm 10,5% · thuế TNCN 5 bậc (2026). Bấm "Chi tiết" để xem từng phép tính.
            </p>
        </div>
        <div style="display: flex; gap: 10px; flex-wrap: wrap;">
            <a class="btn btn-outline" href="<?= url('luong/export?' . $qs()) ?>">Xuất Excel (CSV)</a>
            <?php if (!$isLocked && has_permission('LUONG.Add')): ?>
                <form action="<?= url('luong/calculate') ?>" method="POST" style="display: inline-block;">
                    <?= csrf_field() ?>
                    <input type="hidden" name="thang" value="<?= $thang ?>">
                    <input type="hidden" name="nam" value="<?= $nam ?>">
                    <button type="submit" class="btn btn-outline">Tính lại bảng lương</button>
                </form>
            <?php endif; ?>
            <?php if (!$isLocked && has_permission('LUONG.Approve') && !empty($payrollList)): ?>
                <form action="<?= url('luong/lock') ?>" method="POST" style="display: inline-block;" onsubmit="return confirm('Khóa bảng lương tháng <?= $thang ?>/<?= $nam ?>? Sau khi khóa không thể tính lại.');">
                    <?= csrf_field() ?>
                    <input type="hidden" name="thang" value="<?= $thang ?>">
                    <input type="hidden" name="nam" value="<?= $nam ?>">
                    <button type="submit" class="btn btn-primary">Khóa bảng lương</button>
                </form>
            <?php elseif ($isLocked): ?>
                <span class="badge badge-success" style="font-size: 0.9rem; padding: 10px 16px;">Đã khóa và duyệt</span>
            <?php endif; ?>
        </div>
    </div>

    <div style="display: flex; align-items: center; gap: 12px; flex-wrap: wrap; margin-bottom: 18px;">
        <?php require __DIR__ . '/_search.php'; ?>
        <span style="font-size: .8rem; color: var(--text-secondary);">Chọn một nhân viên hoặc bấm vào dòng bất kỳ trong bảng để xem chi tiết tính lương, lịch sử lương theo quý và in.</span>
    </div>

    <div class="stats-grid" style="grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); margin-bottom: 20px;">
        <div class="stat-card" style="padding: 16px;"><div class="stat-details">
            <span class="stat-number" style="font-size: 1.25rem;"><?= format_money($totalGross) ?></span>
            <span class="stat-label">Tổng thu nhập (quỹ lương)</span></div></div>
        <div class="stat-card" style="padding: 16px;"><div class="stat-details">
            <span class="stat-number" style="font-size: 1.25rem;"><?= format_money($totalBH) ?></span>
            <span class="stat-label">Bảo hiểm người lao động đóng</span></div></div>
        <div class="stat-card" style="padding: 16px;"><div class="stat-details">
            <span class="stat-number" style="font-size: 1.25rem;"><?= format_money($totalTNCN) ?></span>
            <span class="stat-label">Thuế TNCN khấu trừ</span></div></div>
        <div class="stat-card" style="padding: 16px;"><div class="stat-details">
            <span class="stat-number" style="font-size: 1.25rem; color: var(--vnm-success);"><?= format_money($totalNet) ?></span>
            <span class="stat-label">Tổng thực lĩnh chuyển khoản</span></div></div>
    </div>

    <form action="<?= url('luong') ?>" method="GET" class="pl-filter">
        <label>Tháng
            <select name="thang" class="form-control" style="width: auto;">
                <?php for ($m = 1; $m <= 12; $m++): ?>
                    <option value="<?= $m ?>" <?= $m == $thang ? 'selected' : '' ?>>Tháng <?= $m ?></option>
                <?php endfor; ?>
            </select>
        </label>
        <label>Năm
            <select name="nam" class="form-control" style="width: auto;">
                <?php for ($y = (int)date('Y') + 1; $y >= 2024; $y--): ?>
                    <option value="<?= $y ?>" <?= $y == $nam ? 'selected' : '' ?>><?= $y ?></option>
                <?php endfor; ?>
            </select>
        </label>
        <label>Đơn vị
            <select name="ma_dv" class="form-control" style="width: auto; max-width: 260px;">
                <option value="">Tất cả đơn vị</option>
                <?php foreach (($options['donvi'] ?? []) as $dv): ?>
                    <option value="<?= htmlspecialchars($dv['MaDV']) ?>" <?= $dv['MaDV'] === $maDV ? 'selected' : '' ?>><?= htmlspecialchars($dv['TenDV']) ?></option>
                <?php endforeach; ?>
            </select>
        </label>
        <label>Mã NV / họ tên
            <input type="text" name="q" value="<?= htmlspecialchars($q) ?>" class="form-control" placeholder="VD: VNM-0001 hoặc Hùng" style="width: 200px;">
        </label>
        <button type="submit" class="btn btn-primary btn-sm">Xem bảng lương</button>
        <?php if ($maDV !== '' || $q !== ''): ?>
            <a href="<?= url('luong?thang=' . $thang . '&nam=' . $nam) ?>" class="btn btn-outline btn-sm">Bỏ lọc</a>
        <?php endif; ?>
    </form>

    <?php if ($tamTinh): ?>
        <div class="alert alert-warning" style="margin-bottom: 16px;">
            <div><strong><?= count($tamTinh) ?> nhân viên chưa có chấm công tháng <?= $thang ?>/<?= $nam ?></strong> nên đang được tạm tính đủ công chuẩn (đánh dấu "Tạm tính"). Nhập chấm công rồi bấm "Tính lại bảng lương" để có số chính xác.</div>
        </div>
    <?php endif; ?>

    <p class="pl-legend">Tổng thu nhập = P1 + P2 + P3 + phụ cấp + tăng ca/thưởng. Thực lĩnh = Tổng thu nhập − bảo hiểm − thuế TNCN − khấu trừ khác (đoàn phí, kỷ luật).</p>

    <div class="table-responsive">
        <table class="table pl-table" style="min-width: 1350px;">
            <thead>
                <tr>
                    <th>Nhân viên</th>
                    <th class="pl-num">Công</th>
                    <th class="pl-num">Hệ số</th>
                    <th class="pl-num">P1 Vị trí</th>
                    <th class="pl-num">P2 Năng lực</th>
                    <th class="pl-num">P3 KPI</th>
                    <th class="pl-num">Phụ cấp</th>
                    <th class="pl-num">Tăng ca + thưởng</th>
                    <th class="pl-num">Tổng thu nhập</th>
                    <th class="pl-num">Bảo hiểm</th>
                    <th class="pl-num">Thuế TNCN</th>
                    <th class="pl-num">Khấu trừ khác</th>
                    <th class="pl-num">Thực lĩnh</th>
                    <th style="text-align: right;">Xem</th>
                </tr>
            </thead>
            <tbody>
            <?php if (empty($payrollList)): ?>
                <tr><td colspan="14" style="text-align: center; color: var(--text-muted); padding: 30px;">Không có nhân viên nào phù hợp bộ lọc trong tháng này.</td></tr>
            <?php else: foreach ($payrollList as $p): ?>
                <tr class="pl-row" data-href="<?= url('luong/detail/' . $p['MaNV'] . '?thang=' . $thang . '&nam=' . $nam) ?>" title="Bấm để xem chi tiết tính lương">
                    <td style="min-width: 220px;">
                        <a href="<?= url('luong/detail/' . $p['MaNV'] . '?thang=' . $thang . '&nam=' . $nam) ?>" style="font-weight: 700; color: var(--vnm-navy);"><?= htmlspecialchars($p['HoTen']) ?></a>
                        <?php if (!empty($p['GhiChu'])): ?> <span class="pl-tag" title="<?= htmlspecialchars($p['GhiChu']) ?>">Tạm tính</span><?php endif; ?>
                        <div style="font-size: 0.74rem; color: var(--text-secondary);"><code><?= htmlspecialchars($p['MaNV']) ?></code> · <?= htmlspecialchars($p['TenCV'] ?? '') ?> · <?= htmlspecialchars($p['TenDV'] ?? '') ?></div>
                    </td>
                    <td class="pl-num"><?= rtrim(rtrim(number_format((float)$p['SoNgayCong'], 1, ',', ''), '0'), ',') ?></td>
                    <td class="pl-num"><?= number_format((float)$p['HeSoLuong'], 2, ',', '') ?></td>
                    <td class="pl-num"><?= format_money($p['LuongCoBan']) ?></td>
                    <td class="pl-num"><?= format_money($p['LuongNangLuc']) ?></td>
                    <td class="pl-num"><?= format_money($p['ThuongKPI']) ?></td>
                    <td class="pl-num"><?= format_money($p['PhuCapChucVu'] + $p['PhuCapKhac']) ?></td>
                    <td class="pl-num"><?= format_money($p['ThuongKhac']) ?></td>
                    <td class="pl-num" style="font-weight: 700; color: var(--vnm-navy);"><?= format_money($p['TongThuNhap']) ?></td>
                    <td class="pl-num" style="color: var(--vnm-danger);"><?= format_money($p['KhauTruBHXH']) ?></td>
                    <td class="pl-num" style="color: var(--vnm-warning);"><?= format_money($p['ThueTNCN']) ?></td>
                    <td class="pl-num"><?= format_money($p['KhauTruKhac']) ?></td>
                    <td class="pl-num" style="font-weight: 800; color: var(--vnm-success);"><?= format_money($p['ThucLinh']) ?></td>
                    <td>
                        <div class="pl-actions">
                            <a class="btn btn-primary" href="<?= url('luong/detail/' . $p['MaNV'] . '?thang=' . $thang . '&nam=' . $nam) ?>">Chi tiết</a>
                            <a class="btn btn-outline" href="<?= url('luong/detail/' . $p['MaNV'] . '?thang=' . $thang . '&nam=' . $nam . '&tab=quarter') ?>">Lịch sử</a>
                            <a class="btn btn-outline" href="<?= url('luong/payslip/' . $p['MaNV'] . '?thang=' . $thang . '&nam=' . $nam) ?>" target="_blank">Phiếu</a>
                        </div>
                    </td>
                </tr>
            <?php endforeach; ?>
                <tr class="pl-total">
                    <td>Tổng cộng (<?= count($payrollList) ?> người)</td>
                    <td></td><td></td>
                    <td class="pl-num"><?= format_money($sum('LuongCoBan')) ?></td>
                    <td class="pl-num"><?= format_money($sum('LuongNangLuc')) ?></td>
                    <td class="pl-num"><?= format_money($sum('ThuongKPI')) ?></td>
                    <td class="pl-num"><?= format_money($sum('PhuCapChucVu') + $sum('PhuCapKhac')) ?></td>
                    <td class="pl-num"><?= format_money($sum('ThuongKhac')) ?></td>
                    <td class="pl-num"><?= format_money($totalGross) ?></td>
                    <td class="pl-num"><?= format_money($totalBH) ?></td>
                    <td class="pl-num"><?= format_money($totalTNCN) ?></td>
                    <td class="pl-num"><?= format_money($sum('KhauTruKhac')) ?></td>
                    <td class="pl-num" style="color: var(--vnm-success);"><?= format_money($totalNet) ?></td>
                    <td></td>
                </tr>
            <?php endif; ?>
            </tbody>
        </table>
    </div>
</div>

<script>
document.querySelectorAll('.pl-row').forEach(tr => tr.addEventListener('click', e => {
    if (e.target.closest('a, button, form')) return;
    location.href = tr.dataset.href;
}));
</script>
