<?php
/**
 * Trang nhân viên trong phân hệ lương: chi tiết tính lương tháng, lịch sử theo quý / tháng, diễn biến hệ số, in.
 * Biến: $emp, $calc (null nếu không thuộc diện trả lương tháng này), $stored, $thang, $nam,
 *       $periods (các kỳ lương, mới nhất trước), $quarters (gộp theo quý), $grades (qt_luong), $tab
 */
$prevT = $thang == 1 ? 12 : $thang - 1; $prevN = $thang == 1 ? $nam - 1 : $nam;
$nextT = $thang == 12 ? 1 : $thang + 1; $nextN = $thang == 12 ? $nam + 1 : $nam;
$link = fn ($t, $n, $tab = 'calc') => url('luong/detail/' . $emp['MaNV'] . '?thang=' . $t . '&nam=' . $n . '&tab=' . $tab);
$rec = $calc['record'] ?? null;
$ex = $calc['extra'] ?? [];
$isLocked = $stored && in_array($stored['TrangThaiDuyet'], ['KTDuyet', 'DaChi'], true);
$lech = $stored && $rec && (abs((float)$stored['ThucLinh'] - (float)$rec['ThucLinh']) >= 1);
$fmtD = fn ($n) => rtrim(rtrim(number_format((float)$n, 2, ',', '.'), '0'), ',');
$statusLabel = ['ChuaTinh' => 'Chưa tính', 'DaTinh' => 'Đã tính', 'KTDuyet' => 'Đã khóa', 'DaChi' => 'Đã chi'];
$tabs = ['calc' => 'Chi tiết tính lương T' . $thang . '/' . $nam, 'quarter' => 'Lịch sử theo quý', 'month' => 'Lịch sử theo tháng', 'grade' => 'Diễn biến hệ số lương'];
$tab = isset($tabs[$tab ?? '']) ? $tab : 'calc';
$chart = array_reverse(array_slice($periods, 0, 12));
$maxNet = $chart ? max(array_map(fn ($p) => (float)$p['ThucLinh'], $chart)) : 1;
$maxNet = $maxNet > 0 ? $maxNet : 1;
$qMax = 1;
foreach ($quarters as $qs) { foreach ($qs as $r) { $qMax = max($qMax, $r['net']); } }
$luongCoSo = PayrollModel::LUONG_CO_SO;
?>
<style>
    .pd-grid { display: grid; grid-template-columns: minmax(0, 1fr) minmax(0, 1.6fr); gap: 20px; align-items: start; }
    @media (max-width: 1100px) { .pd-grid { grid-template-columns: 1fr; } }
    .pd-dl { display: grid; grid-template-columns: 150px minmax(0, 1fr); gap: 8px 12px; font-size: .86rem; }
    .pd-dl dt { color: var(--text-secondary); }
    .pd-dl dd { margin: 0; font-weight: 600; color: var(--text-primary); }
    .pd-head { font-size: .8rem; font-weight: 800; color: var(--vnm-primary); text-transform: uppercase; letter-spacing: .4px; margin: 18px 0 8px; }
    .pd-step { display: grid; grid-template-columns: minmax(0, 1fr) auto; gap: 4px 16px; padding: 10px 0; border-bottom: 1px solid var(--border-color); break-inside: avoid; }
    .pd-step .lbl { font-weight: 700; font-size: .88rem; color: var(--text-primary); }
    .pd-step .fx { grid-column: 1; font-size: .8rem; color: var(--text-secondary); font-variant-numeric: tabular-nums; }
    .pd-step .note { grid-column: 1 / -1; font-size: .75rem; color: var(--text-muted); font-style: italic; }
    .pd-step .val { grid-row: 1 / 3; grid-column: 2; align-self: center; text-align: right; font-weight: 700; white-space: nowrap; font-variant-numeric: tabular-nums; }
    .pd-step.strong { background: var(--table-header-bg); padding: 12px 10px; border-radius: var(--radius-md); border: 0; margin: 6px 0; }
    .pd-step.strong .val { color: var(--vnm-navy); font-size: 1.05rem; }
    .pd-net { display: flex; justify-content: space-between; align-items: baseline; padding: 16px 18px; border-radius: var(--radius-lg); background: var(--vnm-primary); color: #fff; margin-top: 12px; }
    .pd-net strong { font-size: 1.5rem; font-variant-numeric: tabular-nums; }
    .pd-mini { width: 100%; border-collapse: collapse; font-size: .84rem; }
    .pd-mini td { padding: 6px 0; border-bottom: 1px solid var(--border-color); }
    .pd-mini td:last-child { text-align: right; font-weight: 700; white-space: nowrap; padding-left: 12px; font-variant-numeric: tabular-nums; }
    .pd-tabs { display: flex; gap: 4px; border-bottom: 2px solid var(--border-color); margin-bottom: 18px; flex-wrap: wrap; }
    .pd-tabs button { padding: 10px 16px; border: 0; background: none; font-weight: 700; font-size: .88rem; color: var(--text-secondary); cursor: pointer; border-bottom: 3px solid transparent; margin-bottom: -2px; border-radius: 8px 8px 0 0; }
    .pd-tabs button:hover { color: var(--vnm-primary); background: var(--table-row-hover); }
    .pd-tabs button[aria-selected="true"] { color: var(--vnm-primary); border-bottom-color: var(--vnm-primary); }
    .pd-num { text-align: right; white-space: nowrap; font-variant-numeric: tabular-nums; }
    .pd-q-year td { background: var(--table-header-bg); font-weight: 800; }
    .pd-qbar { height: 8px; border-radius: 0 4px 4px 0; background: var(--vnm-primary); min-width: 2px; }
    .pd-bars { display: flex; align-items: flex-end; gap: 10px; height: 180px; padding: 10px 4px 0; border-bottom: 1px solid var(--border-color); }
    .pd-bar { flex: 1; display: flex; flex-direction: column; align-items: center; justify-content: flex-end; height: 100%; min-width: 0; text-decoration: none; }
    .pd-bar span { width: 100%; max-width: 44px; background: var(--vnm-primary); border-radius: 4px 4px 0 0; }
    .pd-bar:hover span { opacity: .75; }
    .pd-bar small { font-size: .7rem; color: var(--text-secondary); margin-top: 4px; white-space: nowrap; }
    .pd-bar b { font-size: .68rem; color: var(--text-primary); margin-bottom: 4px; white-space: nowrap; }
    .pd-tag { display: inline-block; padding: 1px 7px; border-radius: 999px; font-size: .68rem; font-weight: 700; background: rgba(245, 158, 11, .15); color: var(--vnm-warning); }
    .print-only { display: none; }
    .pd-sign { display: grid; grid-template-columns: repeat(3, 1fr); text-align: center; gap: 10px; margin-top: 28px; font-size: .9rem; }
    .pd-sign strong { display: block; margin-bottom: 70px; }
    @media print {
        @page { size: A4; margin: 12mm; }
        .app-sidebar, .app-header, .no-print, .alert { display: none !important; }
        .app-main { margin: 0 !important; padding: 0 !important; width: 100% !important; }
        .app-content { padding: 0 !important; }
        .print-only { display: block !important; }
        .pd-sign.print-only { display: grid !important; }
        .card-box { box-shadow: none !important; border: 1px solid #999 !important; break-inside: avoid; }
        .pd-grid { grid-template-columns: 1fr 1.4fr !important; gap: 10px !important; }
        .pd-net { background: #fff !important; color: #000 !important; border: 2px solid #000; }
        .pd-panel { display: none !important; }
        body[data-print="calc"] .pd-panel[data-panel="calc"],
        body[data-print="history"] .pd-panel[data-panel="quarter"],
        body[data-print="history"] .pd-panel[data-panel="month"],
        body[data-print="history"] .pd-panel[data-panel="grade"] { display: block !important; margin-bottom: 14px; }
        body:not([data-print]) .pd-panel:not([hidden]) { display: block !important; }
        body:not([data-print]) .print-calc, body:not([data-print]) .print-history { display: none !important; }
        .pd-sign { break-inside: avoid; }
        body[data-print="calc"] .print-history, body[data-print="history"] .print-calc { display: none !important; }
        .table-responsive { overflow: visible !important; }
        .table { min-width: 0 !important; font-size: 9pt !important; }
    }
</style>

<!-- Tiêu đề khi in -->
<div class="print-only" style="margin-bottom: 12px;">
    <div style="display: flex; justify-content: space-between; font-size: 10pt;"><strong>CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK)</strong><span>In ngày <?= date('d/m/Y') ?></span></div>
    <h1 class="print-calc" style="text-align: center; font-size: 16pt; margin: 10px 0 2px;">PHIẾU TÍNH LƯƠNG THÁNG <?= $thang ?>/<?= $nam ?></h1>
    <h1 class="print-history" style="text-align: center; font-size: 16pt; margin: 10px 0 2px;">LỊCH SỬ LƯƠNG NHÂN VIÊN</h1>
    <p style="text-align: center; font-size: 11pt;"><?= htmlspecialchars($emp['HoTen']) ?> — <?= htmlspecialchars($emp['MaNV']) ?> — <?= htmlspecialchars($emp['TenCV'] ?? '') ?>, <?= htmlspecialchars($emp['TenDV'] ?? '') ?></p>
</div>

<div class="no-print" style="display: flex; align-items: center; gap: 12px; flex-wrap: wrap; margin-bottom: 14px;">
    <a href="<?= url('luong?thang=' . $thang . '&nam=' . $nam) ?>" style="font-size: .85rem; white-space: nowrap;">← Bảng lương tháng <?= $thang ?>/<?= $nam ?></a>
    <?php require __DIR__ . '/_search.php'; ?>
</div>

<div class="no-print" style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 12px; margin-bottom: 16px;">
    <div>
        <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;"><?= htmlspecialchars($emp['HoTen']) ?></h2>
        <p style="font-size: .85rem; color: var(--text-secondary);"><?= htmlspecialchars($emp['MaNV']) ?> · <?= htmlspecialchars($emp['TenCV'] ?? '') ?> · <?= htmlspecialchars($emp['TenDV'] ?? '') ?> · vào làm <?= format_date($emp['NgayVaoLam']) ?></p>
    </div>
    <div style="display: flex; gap: 8px; flex-wrap: wrap; align-items: center;">
        <a class="btn btn-outline btn-sm" href="<?= $link($prevT, $prevN) ?>">‹ T<?= $prevT ?>/<?= $prevN ?></a>
        <span class="badge badge-primary" style="padding: 8px 14px;">Tháng <?= $thang ?>/<?= $nam ?></span>
        <a class="btn btn-outline btn-sm" href="<?= $link($nextT, $nextN) ?>">T<?= $nextT ?>/<?= $nextN ?> ›</a>
        <button type="button" class="btn btn-primary btn-sm" data-print-mode="calc" <?= $calc ? '' : 'disabled' ?>>In bảng lương tháng</button>
        <button type="button" class="btn btn-primary btn-sm" data-print-mode="history">In lịch sử lương</button>
        <?php if ($stored): ?><a class="btn btn-outline btn-sm" target="_blank" href="<?= url('luong/payslip/' . $emp['MaNV'] . '?thang=' . $thang . '&nam=' . $nam) ?>">Phiếu lương</a><?php endif; ?>
    </div>
</div>

<div class="stats-grid no-print" style="grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); margin-bottom: 16px;">
    <div class="stat-card" style="padding: 14px;"><div class="stat-details">
        <span class="stat-number" style="font-size: 1.15rem;"><?= number_format((float)$emp['HeSoLuongHienTai'], 2, ',', '') ?> · bậc <?= (int)$emp['BacLuongHienTai'] ?></span>
        <span class="stat-label">Hệ số · bậc lương</span></div></div>
    <div class="stat-card" style="padding: 14px;"><div class="stat-details">
        <span class="stat-number" style="font-size: 1.15rem; color: var(--vnm-success);"><?= $rec ? format_money($rec['ThucLinh']) : '—' ?></span>
        <span class="stat-label">Thực lĩnh T<?= $thang ?>/<?= $nam ?></span></div></div>
    <?php $curQ = $quarters[$nam][intdiv($thang - 1, 3) + 1] ?? null; ?>
    <div class="stat-card" style="padding: 14px;"><div class="stat-details">
        <span class="stat-number" style="font-size: 1.15rem;"><?= $curQ ? format_money($curQ['net']) : '—' ?></span>
        <span class="stat-label">Thực lĩnh quý <?= intdiv($thang - 1, 3) + 1 ?>/<?= $nam ?><?= $curQ ? ' (' . $curQ['soKy'] . ' tháng)' : '' ?></span></div></div>
    <?php $ytd = array_sum(array_map(fn ($r) => $r['net'], $quarters[$nam] ?? [])); ?>
    <div class="stat-card" style="padding: 14px;"><div class="stat-details">
        <span class="stat-number" style="font-size: 1.15rem;"><?= format_money($ytd) ?></span>
        <span class="stat-label">Lũy kế thực lĩnh năm <?= $nam ?></span></div></div>
</div>

<div class="pd-tabs no-print" role="tablist">
    <?php foreach ($tabs as $k => $label): ?>
        <button type="button" role="tab" data-tab="<?= $k ?>" aria-selected="<?= $k === $tab ? 'true' : 'false' ?>"><?= htmlspecialchars($label) ?></button>
    <?php endforeach; ?>
</div>

<!-- ===================== TAB 1: CHI TIẾT TÍNH LƯƠNG ===================== -->
<section class="pd-panel" data-panel="calc" <?= $tab === 'calc' ? '' : 'hidden' ?>>
<?php if (!$calc): ?>
    <div class="alert alert-info"><div>Nhân viên này không thuộc diện trả lương tháng <?= $thang ?>/<?= $nam ?> (chưa vào làm, đã nghỉ việc hoặc nghỉ hưu). Xem các tab lịch sử để tra cứu kỳ trước.</div></div>
<?php else: ?>
    <?php if ($isLocked && $lech): ?>
        <div class="alert alert-warning" style="margin-bottom: 16px;"><div>Bảng lương tháng này <strong>đã khóa</strong> với thực lĩnh <strong><?= format_money($stored['ThucLinh']) ?></strong>. Các bước dưới đây tính lại theo dữ liệu và quy định hiện tại nên có thể khác số đã chi.</div></div>
    <?php elseif (!$stored): ?>
        <div class="alert alert-info" style="margin-bottom: 16px;"><div>Tháng này chưa chạy bảng lương; các số dưới đây là tính thử, chưa lưu.</div></div>
    <?php endif; ?>
    <?php if (empty($ex['coChamCong'])): ?>
        <div class="alert alert-warning" style="margin-bottom: 16px;"><div>Chưa có dữ liệu chấm công tháng <?= $thang ?>/<?= $nam ?>: đang tạm tính đủ <?= $fmtD($ex['congChuan']) ?> công chuẩn.</div></div>
    <?php endif; ?>

    <div class="pd-grid">
        <div style="display: flex; flex-direction: column; gap: 20px;">
            <div class="card-box">
                <div class="pd-head" style="margin-top: 0;">Hồ sơ lương</div>
                <dl class="pd-dl">
                    <dt>Ngày vào làm</dt><dd><?= format_date($emp['NgayVaoLam']) ?></dd>
                    <dt>Ngạch · bậc</dt><dd><?= htmlspecialchars($emp['TenNgach'] ?? '—') ?> · bậc <?= (int)$emp['BacLuongHienTai'] ?></dd>
                    <dt>Hệ số lương</dt><dd><?= number_format((float)$emp['HeSoLuongHienTai'], 2, ',', '') ?></dd>
                    <dt>Trình độ</dt><dd><?= htmlspecialchars($emp['TrinhDoHocVan'] ?? '—') ?></dd>
                    <dt>Lương đóng BH</dt><dd><?= (float)$emp['LuongBaoHiem'] > 0 ? format_money($emp['LuongBaoHiem']) : 'Theo lương P1 + phụ cấp CV' ?></dd>
                    <dt>Người phụ thuộc</dt><dd><?= (int)$emp['SoNguoiPhuThuoc'] ?> người</dd>
                    <dt>Hợp đồng</dt><dd><?= htmlspecialchars($emp['LoaiHopDong'] ?? '—') ?></dd>
                    <dt>Tài khoản</dt><dd><?= htmlspecialchars(trim(($emp['NganHang'] ?? '') . ' ' . ($emp['SoTaiKhoanNH'] ?? ''))) ?: '—' ?></dd>
                </dl>
            </div>

            <div class="card-box">
                <div class="pd-head" style="margin-top: 0;">Chấm công tháng <?= $thang ?>/<?= $nam ?></div>
                <table class="pd-mini">
                    <tr><td>Công chuẩn</td><td><?= $fmtD($ex['congChuan']) ?></td></tr>
                    <tr><td>Công thực tế</td><td><?= $fmtD($ex['congThucTe']) ?></td></tr>
                    <tr><td>Nghỉ phép có lương · nghỉ lễ</td><td><?= $fmtD($ex['nghiPhep']) ?> · <?= $fmtD($ex['nghiLe']) ?></td></tr>
                    <tr><td>Nghỉ không lương</td><td><?= $fmtD($ex['nghiKhongLuong']) ?></td></tr>
                    <tr><td>Giờ tăng ca</td><td><?= $fmtD($ex['gioTangCa']) ?> giờ</td></tr>
                    <tr><td>Số lần đi trễ / về sớm</td><td><?= (int)$ex['soLanTre'] ?></td></tr>
                </table>
                <?php if (!empty($ex['ktkl'])): ?>
                    <div class="pd-head">Khen thưởng · kỷ luật trong tháng</div>
                    <table class="pd-mini">
                        <?php foreach ($ex['ktkl'] as $k): ?>
                            <tr><td><?= htmlspecialchars($k['HinhThuc']) ?> <span style="color: var(--text-muted); font-size: .75rem;">(<?= format_date($k['NgayQD']) ?>)</span></td><td style="color: <?= $k['GiaTri'] > 0 ? 'var(--vnm-success)' : 'var(--vnm-danger)' ?>;"><?= $k['GiaTri'] > 0 ? '+' : '−' ?><?= format_money(abs($k['GiaTri'])) ?></td></tr>
                        <?php endforeach; ?>
                    </table>
                <?php endif; ?>
            </div>

            <div class="card-box">
                <div class="pd-head" style="margin-top: 0;">Tóm tắt</div>
                <table class="pd-mini">
                    <tr><td>P1 + P2 + P3</td><td><?= format_money($rec['LuongCoBan'] + $rec['LuongNangLuc'] + $rec['ThuongKPI']) ?></td></tr>
                    <tr><td>Phụ cấp</td><td><?= format_money($rec['PhuCapChucVu'] + $rec['PhuCapKhac']) ?></td></tr>
                    <tr><td>Tăng ca + thưởng</td><td><?= format_money($rec['ThuongKhac']) ?></td></tr>
                    <tr><td><strong>Tổng thu nhập</strong></td><td><?= format_money($rec['TongThuNhap']) ?></td></tr>
                    <tr><td>Bảo hiểm (BHXH <?= format_money($ex['bhxh']) ?>, BHYT <?= format_money($ex['bhyt']) ?>, BHTN <?= format_money($ex['bhtn']) ?>)</td><td style="color: var(--vnm-danger);">−<?= format_money($rec['KhauTruBHXH']) ?></td></tr>
                    <tr><td>Thuế TNCN</td><td style="color: var(--vnm-danger);">−<?= format_money($rec['ThueTNCN']) ?></td></tr>
                    <tr><td>Khấu trừ khác</td><td style="color: var(--vnm-danger);">−<?= format_money($rec['KhauTruKhac']) ?></td></tr>
                </table>
                <div class="pd-net"><span>THỰC LĨNH</span><strong><?= format_money($rec['ThucLinh']) ?></strong></div>
                <p style="font-size: .78rem; color: var(--text-secondary); margin-top: 10px;">Công ty đóng thêm BHXH, BHYT, BHTN 21,5% + kinh phí công đoàn 2%: <strong><?= format_money($ex['congTyDong']) ?></strong> → chi phí lao động <strong><?= format_money($rec['TongThuNhap'] + $ex['congTyDong']) ?></strong>.</p>
            </div>
        </div>

        <div class="card-box">
            <h3 style="font-size: 1.05rem; font-weight: 800; color: var(--vnm-navy);">Các bước tính lương</h3>
            <p style="font-size: .8rem; color: var(--text-secondary); margin-top: 2px;">Mỗi dòng ghi công thức và kết quả, theo đúng thứ tự hệ thống tính.</p>
            <?php $group = null; foreach ($calc['steps'] as $s): ?>
                <?php if ($s['group'] !== $group): $group = $s['group']; ?><div class="pd-head"><?= htmlspecialchars($group) ?></div><?php endif; ?>
                <div class="pd-step<?= !empty($s['strong']) ? ' strong' : '' ?>">
                    <div class="lbl"><?= htmlspecialchars($s['label']) ?></div>
                    <div class="val"><?= is_numeric($s['value']) ? format_money($s['value']) : htmlspecialchars($s['value']) ?></div>
                    <div class="fx"><?= htmlspecialchars($s['formula']) ?></div>
                    <?php if (!empty($s['note'])): ?><div class="note"><?= htmlspecialchars($s['note']) ?></div><?php endif; ?>
                </div>
            <?php endforeach; ?>
            <p style="font-size: .74rem; color: var(--text-muted); margin-top: 14px; line-height: 1.6;">
                Tham số: lương cơ sở nội bộ <?= format_money($luongCoSo) ?> × hệ số; giảm trừ <?= format_money(PayrollModel::GIAM_TRU_BAN_THAN) ?> + <?= format_money(PayrollModel::GIAM_TRU_PHU_THUOC) ?>/người phụ thuộc;
                trần bảo hiểm <?= format_money(PayrollModel::MAX_LUONG_BHXH) ?>; thuế 5 bậc 5–10–20–30–35%. Đối chiếu lại khi quy định thay đổi.
            </p>
        </div>
    </div>
    <div class="pd-sign print-only print-calc"><div><strong>Người lập biểu</strong>(Ký, ghi rõ họ tên)</div><div><strong>Kế toán trưởng</strong>(Ký, ghi rõ họ tên)</div><div><strong>Người lao động</strong><?= htmlspecialchars($emp['HoTen']) ?></div></div>
<?php endif; ?>
</section>

<!-- ===================== TAB 2: LỊCH SỬ THEO QUÝ ===================== -->
<section class="pd-panel" data-panel="quarter" <?= $tab === 'quarter' ? '' : 'hidden' ?>>
    <div class="card-box">
        <h3 style="font-size: 1rem; font-weight: 800; color: var(--vnm-navy);">Lịch sử lương theo quý</h3>
        <p style="font-size: .8rem; color: var(--text-secondary); margin-bottom: 10px;">Cộng dồn các tháng đã có bảng lương trong từng quý. Bấm vào quý để xem bảng theo tháng.</p>
        <div class="table-responsive">
            <table class="table" style="min-width: 1000px;">
                <thead><tr>
                    <th>Quý</th><th>Các tháng</th><th class="pd-num">Công</th><th class="pd-num">P1 + P2 + P3</th><th class="pd-num">Phụ cấp</th><th class="pd-num">Tăng ca + thưởng</th>
                    <th class="pd-num">Tổng thu nhập</th><th class="pd-num">Bảo hiểm</th><th class="pd-num">Thuế TNCN</th><th class="pd-num">Thực lĩnh</th><th class="pd-num">TB / tháng</th><th style="width: 120px;"></th>
                </tr></thead>
                <tbody>
                <?php if (!$quarters): ?>
                    <tr><td colspan="12" style="text-align: center; color: var(--text-muted); padding: 24px;">Chưa có kỳ lương nào.</td></tr>
                <?php endif; ?>
                <?php foreach ($quarters as $y => $qs):
                    $ySum = fn ($k) => array_sum(array_map(fn ($r) => $r[$k], $qs)); ?>
                    <tr class="pd-q-year"><td colspan="6">Năm <?= $y ?> · <?= array_sum(array_map(fn ($r) => $r['soKy'], $qs)) ?> tháng</td>
                        <td class="pd-num"><?= format_money($ySum('gross')) ?></td><td class="pd-num"><?= format_money($ySum('bh')) ?></td>
                        <td class="pd-num"><?= format_money($ySum('thue')) ?></td><td class="pd-num"><?= format_money($ySum('net')) ?></td><td colspan="2"></td></tr>
                    <?php foreach ($qs as $q => $r): ?>
                        <tr class="pd-qrow" data-goto="month" style="cursor: pointer;">
                            <td><strong>Quý <?= $q ?>/<?= $y ?></strong></td>
                            <td style="white-space: nowrap;"><?= implode(', ', array_map(fn ($m) => 'T' . $m, $r['thang'])) ?><?= $r['soKy'] < 3 ? ' <span class="pd-tag">' . $r['soKy'] . '/3 tháng</span>' : '' ?></td>
                            <td class="pd-num"><?= $fmtD($r['cong']) ?></td>
                            <td class="pd-num"><?= format_money($r['p123']) ?></td>
                            <td class="pd-num"><?= format_money($r['phuCap']) ?></td>
                            <td class="pd-num"><?= format_money($r['thuongKhac']) ?></td>
                            <td class="pd-num" style="font-weight: 700;"><?= format_money($r['gross']) ?></td>
                            <td class="pd-num"><?= format_money($r['bh']) ?></td>
                            <td class="pd-num"><?= format_money($r['thue']) ?></td>
                            <td class="pd-num" style="font-weight: 800; color: var(--vnm-success);"><?= format_money($r['net']) ?></td>
                            <td class="pd-num"><?= format_money($r['net'] / max(1, $r['soKy'])) ?></td>
                            <td><div class="pd-qbar" style="width: <?= round($r['net'] / $qMax * 100) ?>%;"></div></td>
                        </tr>
                    <?php endforeach; ?>
                <?php endforeach; ?>
                </tbody>
            </table>
        </div>
    </div>
</section>

<!-- ===================== TAB 3: LỊCH SỬ THEO THÁNG ===================== -->
<section class="pd-panel" data-panel="month" <?= $tab === 'month' ? '' : 'hidden' ?>>
    <?php if ($chart): ?>
    <div class="card-box no-print" style="margin-bottom: 16px;">
        <h3 style="font-size: 1rem; font-weight: 800; color: var(--vnm-navy);">Thực lĩnh <?= count($chart) ?> tháng gần nhất</h3>
        <p style="font-size: .78rem; color: var(--text-secondary);">Bấm vào một cột để xem chi tiết tính lương tháng đó.</p>
        <div class="pd-bars" role="img" aria-label="Biểu đồ thực lĩnh theo tháng">
            <?php foreach ($chart as $p): ?>
                <a class="pd-bar" href="<?= $link($p['Thang'], $p['Nam']) ?>" title="Tháng <?= $p['Thang'] ?>/<?= $p['Nam'] ?>: <?= format_money($p['ThucLinh']) ?>">
                    <b><?= number_format((float)$p['ThucLinh'] / 1000000, 1, ',', '') ?>tr</b>
                    <span style="height: <?= max(2, round((float)$p['ThucLinh'] / $maxNet * 140)) ?>px;"></span>
                    <small>T<?= $p['Thang'] ?>/<?= substr((string)$p['Nam'], 2) ?></small>
                </a>
            <?php endforeach; ?>
        </div>
    </div>
    <?php endif; ?>
    <div class="card-box">
        <h3 style="font-size: 1rem; font-weight: 800; color: var(--vnm-navy); margin-bottom: 10px;">Các kỳ lương theo tháng</h3>
        <div class="table-responsive">
            <table class="table" style="min-width: 1100px;">
                <thead><tr>
                    <th>Kỳ lương</th><th class="pd-num">Công</th><th class="pd-num">Hệ số</th><th class="pd-num">P1 + P2 + P3</th><th class="pd-num">Phụ cấp</th>
                    <th class="pd-num">Tăng ca + thưởng</th><th class="pd-num">Tổng thu nhập</th><th class="pd-num">Bảo hiểm</th><th class="pd-num">Thuế TNCN</th>
                    <th class="pd-num">Thực lĩnh</th><th>Trạng thái</th><th class="no-print"></th>
                </tr></thead>
                <tbody>
                <?php if (!$periods): ?>
                    <tr><td colspan="12" style="text-align: center; color: var(--text-muted); padding: 24px;">Chưa có kỳ lương nào.</td></tr>
                <?php endif; ?>
                <?php foreach ($periods as $p): ?>
                    <tr<?= ((int)$p['Thang'] === $thang && (int)$p['Nam'] === $nam) ? ' style="background: var(--table-row-hover);"' : '' ?>>
                        <td style="white-space: nowrap;"><strong>Tháng <?= $p['Thang'] ?>/<?= $p['Nam'] ?></strong><?= !empty($p['GhiChu']) ? ' <span class="pd-tag" title="' . htmlspecialchars($p['GhiChu']) . '">Tạm tính</span>' : '' ?></td>
                        <td class="pd-num"><?= $fmtD($p['SoNgayCong']) ?></td>
                        <td class="pd-num"><?= number_format((float)$p['HeSoLuong'], 2, ',', '') ?></td>
                        <td class="pd-num"><?= format_money($p['LuongCoBan'] + $p['LuongNangLuc'] + $p['ThuongKPI']) ?></td>
                        <td class="pd-num"><?= format_money($p['PhuCapChucVu'] + $p['PhuCapKhac']) ?></td>
                        <td class="pd-num"><?= format_money($p['ThuongKhac']) ?></td>
                        <td class="pd-num" style="font-weight: 700;"><?= format_money($p['TongThuNhap']) ?></td>
                        <td class="pd-num"><?= format_money($p['KhauTruBHXH']) ?></td>
                        <td class="pd-num"><?= format_money($p['ThueTNCN']) ?></td>
                        <td class="pd-num" style="font-weight: 800; color: var(--vnm-success);"><?= format_money($p['ThucLinh']) ?></td>
                        <td style="white-space: nowrap;"><?= $statusLabel[$p['TrangThaiDuyet']] ?? htmlspecialchars($p['TrangThaiDuyet']) ?></td>
                        <td class="no-print" style="white-space: nowrap;"><a class="btn btn-outline btn-sm" href="<?= $link($p['Thang'], $p['Nam']) ?>">Chi tiết</a></td>
                    </tr>
                <?php endforeach; ?>
                </tbody>
            </table>
        </div>
    </div>
</section>

<!-- ===================== TAB 4: DIỄN BIẾN HỆ SỐ ===================== -->
<section class="pd-panel" data-panel="grade" <?= $tab === 'grade' ? '' : 'hidden' ?>>
    <div class="card-box">
        <h3 style="font-size: 1rem; font-weight: 800; color: var(--vnm-navy); margin-bottom: 10px;">Diễn biến hệ số và bậc lương (quyết định nâng lương)</h3>
        <div class="table-responsive">
            <table class="table">
                <thead><tr><th>Từ ngày</th><th>Số quyết định</th><th class="pd-num">Bậc</th><th class="pd-num">Hệ số</th><th class="pd-num">Thay đổi</th><th class="pd-num">P1 đủ tháng tương ứng</th><th>Ghi chú</th></tr></thead>
                <tbody>
                <?php if (!$grades): ?>
                    <tr><td colspan="7" style="text-align: center; color: var(--text-muted); padding: 24px;">Chưa có quyết định nâng lương nào được ghi nhận.</td></tr>
                <?php endif; ?>
                <?php foreach ($grades as $i => $g): $prev = $grades[$i + 1] ?? null; $delta = $prev ? (float)$g['HeSoLuong'] - (float)$prev['HeSoLuong'] : null; ?>
                    <tr>
                        <td><strong><?= format_date($g['TuNgay']) ?></strong></td>
                        <td><?= htmlspecialchars($g['SoQD'] ?? '—') ?></td>
                        <td class="pd-num"><?= (int)$g['BacLuong'] ?></td>
                        <td class="pd-num"><?= number_format((float)$g['HeSoLuong'], 2, ',', '') ?><?= (float)$g['VuotKhung'] > 0 ? ' (+' . number_format((float)$g['VuotKhung'], 0) . '% VK)' : '' ?></td>
                        <td class="pd-num"><?= $delta === null ? 'Khởi điểm' : '<span style="color: var(--vnm-success); font-weight: 700;">' . ($delta >= 0 ? '+' : '') . number_format($delta, 2, ',', '') . '</span>' ?></td>
                        <td class="pd-num"><?= format_money($luongCoSo * (float)$g['HeSoLuong']) ?></td>
                        <td><?= htmlspecialchars($g['GhiChu'] ?? '') ?></td>
                    </tr>
                <?php endforeach; ?>
                </tbody>
            </table>
        </div>
        <p style="font-size: .74rem; color: var(--text-muted); margin-top: 8px;">P1 đủ tháng = lương cơ sở nội bộ <?= format_money($luongCoSo) ?> × hệ số, tính theo mức hiện hành để so sánh giữa các lần nâng lương.</p>
    </div>
    <div class="pd-sign print-only print-history"><div><strong>Người lập biểu</strong>(Ký, ghi rõ họ tên)</div><div><strong>Trưởng phòng Nhân sự</strong>(Ký, ghi rõ họ tên)</div><div><strong>Người lao động</strong><?= htmlspecialchars($emp['HoTen']) ?></div></div>
</section>

<script>
(function () {
    const tabs = document.querySelectorAll('.pd-tabs [data-tab]');
    const panels = document.querySelectorAll('.pd-panel');
    function show(name) {
        tabs.forEach(t => t.setAttribute('aria-selected', String(t.dataset.tab === name)));
        panels.forEach(p => { p.hidden = p.dataset.panel !== name; });
        const u = new URL(location.href); u.searchParams.set('tab', name); history.replaceState(null, '', u);
    }
    tabs.forEach(t => t.addEventListener('click', () => show(t.dataset.tab)));
    document.querySelectorAll('.pd-qrow').forEach(r => r.addEventListener('click', () => show(r.dataset.goto)));
    document.querySelectorAll('[data-print-mode]').forEach(b => b.addEventListener('click', () => {
        document.body.dataset.print = b.dataset.printMode;
        window.print();
    }));
    window.addEventListener('afterprint', () => { delete document.body.dataset.print; });
})();
</script>
