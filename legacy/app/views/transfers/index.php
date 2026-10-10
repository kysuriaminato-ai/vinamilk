<?php
/**
 * View Transfer & Appointment List
 */
?>
<div class="card-box">
    <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 24px;">
        <div>
            <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;">
                🔄 Quản lý Thuyên chuyển công tác & Bổ nhiệm Nhân sự
            </h2>
            <p style="font-size: 0.85rem; color: var(--text-secondary); margin-top: 4px;">
                Theo dõi quá trình điều động nhân lực giữa Văn phòng Tập đoàn, các Nhà máy Sữa và Trang trại Green Farm.
            </p>
        </div>

        <?php if (has_permission('NHANSU.Edit')): ?>
            <a href="<?= url('tuyenchuyen/create') ?>" class="btn btn-primary">
                ➕ Lập Quyết định Thuyên chuyển
            </a>
        <?php endif; ?>
    </div>

    <!-- Data Table -->
    <div class="table-responsive">
        <table class="table">
            <thead>
                <tr>
                    <th>Số Quyết định</th>
                    <th>Nhân sự điều động</th>
                    <th>Đơn vị cũ</th>
                    <th>Đơn vị mới</th>
                    <th>Ngày hiệu lực</th>
                    <th>Lý do thuyên chuyển</th>
                    <th style="text-align: right;">Hành động</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($transfers)): ?>
                    <tr>
                        <td colspan="7" style="text-align: center; color: var(--text-muted); padding: 30px;">
                            Chưa có dữ liệu thuyên chuyển công tác nào được ghi nhận.
                        </td>
                    </tr>
                <?php else: ?>
                    <?php foreach ($transfers as $t): ?>
                        <tr>
                            <td><code><?= htmlspecialchars($t['SoQD']) ?></code></td>
                            <td>
                                <a href="<?= url('nhansu/profile/' . $t['MaNV']) ?>" style="font-weight: 700; color: var(--vnm-navy);">
                                    <?= htmlspecialchars($t['HoTen']) ?> (<?= $t['MaNV'] ?>)
                                </a>
                            </td>
                            <td><span class="badge" style="background:#f1f5f9; color:#475569;"><?= htmlspecialchars($t['TenDonViCu'] ?? $t['DonViCu']) ?></span></td>
                            <td><span class="badge badge-primary"><?= htmlspecialchars($t['TenDonViMoi'] ?? $t['DonViMoi']) ?></span></td>
                            <td><strong><?= format_date($t['NgayHieuLuc']) ?></strong></td>
                            <td style="font-size: 0.85rem; color: var(--text-secondary);"><?= htmlspecialchars($t['LyDo'] ?? '-') ?></td>
                            <td style="text-align: right;">
                                <a href="<?= url('tuyenchuyen/print/' . $t['ID']) ?>" target="_blank" class="btn btn-primary btn-sm">
                                    🖨️ In Quyết định
                                </a>
                            </td>
                        </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
        </table>
    </div>
</div>
