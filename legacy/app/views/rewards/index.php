<?php
/**
 * View Reward & Discipline List
 */
?>
<div class="card-box">
    <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 24px; flex-wrap: wrap; gap: 16px;">
        <div>
            <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;">
                🏅 Quản lý Khen thưởng & Kỷ luật (Reward & Discipline)
            </h2>
            <p style="font-size: 0.85rem; color: var(--text-secondary); margin-top: 4px;">
                Ghi nhận thành tích thi đua, sáng kiến cải tiến kỹ thuật và xử lý kỷ luật lao động Vinamilk.
            </p>
        </div>

        <?php if (has_permission('NHANSU.Edit')): ?>
            <a href="<?= url('ktkl/create') ?>" class="btn btn-primary">
                ➕ Ban hành Quyết định KT / KL
            </a>
        <?php endif; ?>
    </div>

    <!-- Filter Tabs -->
    <div style="display: flex; gap: 10px; margin-bottom: 20px;">
        <a href="<?= url('ktkl') ?>" class="btn <?= empty($currentLoai) ? 'btn-primary' : 'btn-outline' ?> btn-sm">Tất cả</a>
        <a href="<?= url('ktkl?loai=KhenThuong') ?>" class="btn <?= ($currentLoai === 'KhenThuong') ? 'btn-primary' : 'btn-outline' ?> btn-sm">🏆 Khen thưởng</a>
        <a href="<?= url('ktkl?loai=KyLuat') ?>" class="btn <?= ($currentLoai === 'KyLuat') ? 'btn-primary' : 'btn-outline' ?> btn-sm">⚠️ Kỷ luật</a>
    </div>

    <!-- Data Table -->
    <div class="table-responsive">
        <table class="table">
            <thead>
                <tr>
                    <th>Số Quyết định</th>
                    <th>Phân loại</th>
                    <th>Nhân sự</th>
                    <th>Hình thức KT/KL</th>
                    <th>Ngày QĐ</th>
                    <th>Mức tiền thưởng/phạt</th>
                    <th style="text-align: right;">Hành động</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($list)): ?>
                    <tr>
                        <td colspan="7" style="text-align: center; color: var(--text-muted); padding: 30px;">
                            Chưa có quyết định khen thưởng / kỷ luật nào.
                        </td>
                    </tr>
                <?php else: ?>
                    <?php foreach ($list as $item): ?>
                        <tr>
                            <td><code><?= htmlspecialchars($item['SoQD']) ?></code></td>
                            <td>
                                <?php if (($item['LoaiDanhMuc'] ?? $item['Loai']) === 'KyLuat'): ?>
                                    <span class="badge badge-danger">Kỷ luật</span>
                                <?php else: ?>
                                    <span class="badge badge-success">Khen thưởng</span>
                                <?php endif; ?>
                            </td>
                            <td>
                                <a href="<?= url('nhansu/profile/' . $item['MaNV']) ?>" style="font-weight: 700; color: var(--vnm-navy);">
                                    <?= htmlspecialchars($item['HoTen']) ?> (<?= $item['MaNV'] ?>)
                                </a>
                            </td>
                            <td style="font-weight: 600;"><?= htmlspecialchars($item['HinhThuc']) ?></td>
                            <td><?= format_date($item['NgayQD']) ?></td>
                            <td style="font-weight: 700;">
                                <?php if ($item['GiaTri'] > 0): ?>
                                    <span style="color: var(--vnm-success);">+<?= format_money($item['GiaTri']) ?></span>
                                <?php elseif ($item['GiaTri'] < 0): ?>
                                    <span style="color: var(--vnm-danger);"><?= format_money($item['GiaTri']) ?></span>
                                <?php else: ?>
                                    -
                                <?php endif; ?>
                            </td>
                            <td style="text-align: right;">
                                <a href="<?= url('ktkl/print/' . $item['ID']) ?>" target="_blank" class="btn btn-primary btn-sm">
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
