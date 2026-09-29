<?php
/**
 * View Bảng Lương 3P Tháng Tổng hợp (Payroll 3P Engine)
 */
$totalGross = array_sum(array_column($payrollList, 'TongThuNhap'));
$totalBHXH = array_sum(array_column($payrollList, 'KhauTruBHXH'));
$totalTNCN = array_sum(array_column($payrollList, 'ThueTNCN'));
$totalNet = array_sum(array_column($payrollList, 'ThucLinh'));

$isLocked = !empty($payrollList) && ($payrollList[0]['TrangThaiDuyet'] === 'KTDuyet' || $payrollList[0]['TrangThaiDuyet'] === 'DaChi');
?>
<div class="card-box">
    <!-- Header Title & Lock Controls -->
    <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 24px; flex-wrap: wrap; gap: 16px;">
        <div>
            <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;">
                💰 Bảng Lương Tháng <?= $thang ?>/<?= $nam ?> (Vinamilk 3P Payroll Engine)
            </h2>
            <p style="font-size: 0.85rem; color: var(--text-secondary); margin-top: 4px;">
                Tự động tính lương P1 (Cơ bản), P2 (Năng lực), P3 (KPI/Hiệu quả), trích BHXH 10.5% & Thuế TNCN lũy tiến.
            </p>
        </div>

        <div style="display: flex; gap: 10px; flex-wrap: wrap;">
            <?php if (!$isLocked && has_permission('LUONG.Add')): ?>
                <form action="<?= url('luong/calculate') ?>" method="POST" style="display: inline-block;">
                    <?= csrf_field() ?>
                    <input type="hidden" name="thang" value="<?= $thang ?>">
                    <input type="hidden" name="nam" value="<?= $nam ?>">
                    <button type="submit" class="btn btn-outline">
                        🔄 Tính toán lại Bảng Lương
                    </button>
                </form>
            <?php endif; ?>

            <?php if (!$isLocked && has_permission('LUONG.Approve')): ?>
                <form action="<?= url('luong/lock') ?>" method="POST" style="display: inline-block;" onsubmit="return confirm('Bạn có chắc chắn muốn KHÓA BẢNG LƯƠNG tháng này? Sau khi khóa sẽ không thể thay đổi dữ liệu.');">
                    <?= csrf_field() ?>
                    <input type="hidden" name="thang" value="<?= $thang ?>">
                    <input type="hidden" name="nam" value="<?= $nam ?>">
                    <button type="submit" class="btn btn-primary" style="background: #10b981;">
                        🔒 KHÓA BẢNG LƯƠNG
                    </button>
                </form>
            <?php elseif ($isLocked): ?>
                <span class="badge badge-success" style="font-size: 0.95rem; padding: 10px 18px;">
                    🔒 ĐÃ KHÓA VÀ DUYỆT BẢNG LƯƠNG
                </span>
            <?php endif; ?>
        </div>
    </div>

    <!-- Summary KPI Cards -->
    <div class="stats-grid" style="grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); margin-bottom: 24px;">
        <div class="stat-card" style="padding: 16px;">
            <div class="stat-icon blue" style="width: 44px; height: 44px; font-size: 1.2rem;">💵</div>
            <div class="stat-details">
                <span class="stat-number" style="font-size: 1.3rem;"><?= format_money($totalGross) ?></span>
                <span class="stat-label">Tổng Quỹ Lương Thu Nhập</span>
            </div>
        </div>

        <div class="stat-card" style="padding: 16px;">
            <div class="stat-icon green" style="width: 44px; height: 44px; font-size: 1.2rem;">🏥</div>
            <div class="stat-details">
                <span class="stat-number" style="font-size: 1.3rem;"><?= format_money($totalBHXH) ?></span>
                <span class="stat-label">Tổng Trích Nộp BHXH 10.5%</span>
            </div>
        </div>

        <div class="stat-card" style="padding: 16px;">
            <div class="stat-icon orange" style="width: 44px; height: 44px; font-size: 1.2rem;">🏛️</div>
            <div class="stat-details">
                <span class="stat-number" style="font-size: 1.3rem;"><?= format_money($totalTNCN) ?></span>
                <span class="stat-label">Tổng Thuế TNCN Khấu trừ</span>
            </div>
        </div>

        <div class="stat-card" style="padding: 16px;">
            <div class="stat-icon green" style="width: 44px; height: 44px; font-size: 1.2rem; background: #ecfdf5; color: #059669;">🏦</div>
            <div class="stat-details">
                <span class="stat-number" style="font-size: 1.3rem; color: #059669;"><?= format_money($totalNet) ?></span>
                <span class="stat-label">Tổng Thực Lĩnh Chuyển Khoản</span>
            </div>
        </div>
    </div>

    <!-- Filter Form -->
    <form action="<?= url('luong') ?>" method="GET" style="background: #f8fafc; padding: 14px 20px; border-radius: var(--radius-lg); border: 1px solid var(--border-color); margin-bottom: 24px; display: flex; align-items: center; gap: 14px; flex-wrap: wrap;">
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
            </select>
        </div>

        <button type="submit" class="btn btn-primary btn-sm">🔍 Xem Bảng Lương</button>
    </form>

    <!-- Payroll Table -->
    <div class="table-responsive">
        <table class="table">
            <thead>
                <tr>
                    <th>Mã NV & Họ tên</th>
                    <th>Công thực</th>
                    <th>Lương P1 (Cơ bản)</th>
                    <th>Phụ cấp CV & Khác</th>
                    <th>Lương P2 + P3</th>
                    <th>Tổng Thu nhập</th>
                    <th>BHXH (10.5%)</th>
                    <th>Thuế TNCN</th>
                    <th>Thực Lĩnh (Net)</th>
                    <th style="text-align: right;">Phiếu Lương</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($payrollList)): ?>
                    <tr>
                        <td colspan="10" style="text-align: center; color: var(--text-muted); padding: 30px;">
                            Chưa có dữ liệu bảng lương tháng này.
                        </td>
                    </tr>
                <?php else: ?>
                    <?php foreach ($payrollList as $p): ?>
                        <tr>
                            <td>
                                <a href="<?= url('nhansu/profile/' . $p['MaNV']) ?>" style="font-weight: 700; color: var(--vnm-navy);">
                                    <?= htmlspecialchars($p['HoTen']) ?>
                                </a>
                                <div style="font-size: 0.75rem; color: var(--text-secondary);">
                                    <code><?= $p['MaNV'] ?></code> | <?= htmlspecialchars($p['TenDV'] ?? '') ?>
                                </div>
                            </td>
                            <td style="text-align: center; font-weight: 700;"><?= $p['SoNgayCong'] ?> công</td>
                            <td><?= format_money($p['LuongCoBan']) ?></td>
                            <td><?= format_money($p['PhuCapChucVu'] + $p['PhuCapKhac']) ?></td>
                            <td><?= format_money($p['LuongNangLuc'] + $p['ThuongKPI']) ?></td>
                            <td style="font-weight: 700; color: var(--vnm-navy);"><?= format_money($p['TongThuNhap']) ?></td>
                            <td style="color: var(--vnm-danger);"><?= format_money($p['KhauTruBHXH']) ?></td>
                            <td style="color: var(--vnm-warning);"><?= format_money($p['ThueTNCN']) ?></td>
                            <td style="font-weight: 800; color: var(--vnm-success); font-size: 0.95rem;">
                                <?= format_money($p['ThucLinh']) ?>
                            </td>
                            <td style="text-align: right;">
                                <a href="<?= url('luong/payslip/' . $p['MaNV'] . '?thang=' . $thang . '&nam=' . $nam) ?>" target="_blank" class="btn btn-outline btn-sm">
                                    📜 Payslip
                                </a>
                            </td>
                        </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
        </table>
    </div>
</div>
