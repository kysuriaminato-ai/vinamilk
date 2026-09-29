<?php
/**
 * View Employee List (Hồ sơ Nhân sự số hóa & Bộ lọc đa năng)
 */
?>
<div class="card-box">
    <!-- Header Title & Action Buttons -->
    <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 24px; flex-wrap: wrap; gap: 16px;">
        <div>
            <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;">
                👥 Quản lý Sơ yếu Lý lịch Số hóa (HUHA HRM Data)
            </h2>
            <p style="font-size: 0.85rem; color: var(--text-secondary); margin-top: 4px;">
                Tổng số hồ sơ tìm thấy: <strong style="color: var(--vnm-primary); font-size: 1rem;"><?= number_format($pagination['total']) ?></strong> nhân sự
            </p>
        </div>

        <?php if (has_permission('NHANSU.Add')): ?>
            <div style="display: flex; gap: 10px;">
                <a href="<?= url('nhansu/create') ?>" class="btn btn-primary">
                    ➕ Thêm mới Hồ sơ Số hóa
                </a>
            </div>
        <?php endif; ?>
    </div>

    <!-- Advanced Multi-Criteria Filter Bar -->
    <form action="<?= url('nhansu') ?>" method="GET" style="background: #f8fafc; padding: 20px; border-radius: var(--radius-lg); border: 1px solid var(--border-color); margin-bottom: 24px;">
        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px; margin-bottom: 16px;">
            <!-- Keyword search -->
            <div>
                <label class="form-label">Tìm theo từ khóa:</label>
                <input type="text" name="keyword" value="<?= htmlspecialchars($filters['keyword']) ?>" class="form-control" placeholder="Mã NV, Họ tên, CCCD, Email, SĐT...">
            </div>

            <!-- Unit Filter -->
            <div>
                <label class="form-label">Đơn vị / Khối / Nhà máy:</label>
                <select name="ma_dv" class="form-control">
                    <option value="">-- Tất cả Đơn vị --</option>
                    <?php foreach ($options['donvi'] as $dv): ?>
                        <option value="<?= $dv['MaDV'] ?>" <?= ($filters['ma_dv'] === $dv['MaDV']) ? 'selected' : '' ?>>
                            <?= htmlspecialchars($dv['TenDV']) ?> (<?= $dv['LoaiDV'] ?>)
                        </option>
                    <?php endforeach; ?>
                </select>
            </div>

            <!-- Position Filter -->
            <div>
                <label class="form-label">Chức vụ / Vị trí:</label>
                <select name="ma_cv" class="form-control">
                    <option value="">-- Tất cả Chức vụ --</option>
                    <?php foreach ($options['chucvu'] as $cv): ?>
                        <option value="<?= $cv['MaCV'] ?>" <?= ($filters['ma_cv'] === $cv['MaCV']) ? 'selected' : '' ?>>
                            <?= htmlspecialchars($cv['TenCV']) ?>
                        </option>
                    <?php endforeach; ?>
                </select>
            </div>

            <!-- Status Filter -->
            <div>
                <label class="form-label">Trạng thái công tác:</label>
                <select name="trang_thai" class="form-control">
                    <option value="">-- Tất cả Trạng thái --</option>
                    <option value="1" <?= ($filters['trang_thai'] === '1') ? 'selected' : '' ?>>1. Đang làm việc chính thức</option>
                    <option value="2" <?= ($filters['trang_thai'] === '2') ? 'selected' : '' ?>>2. Đang thử việc</option>
                    <option value="3" <?= ($filters['trang_thai'] === '3') ? 'selected' : '' ?>>3. Đã nghỉ việc</option>
                    <option value="4" <?= ($filters['trang_thai'] === '4') ? 'selected' : '' ?>>4. Đã nghỉ hưu</option>
                </select>
            </div>
        </div>

        <div style="display: flex; justify-content: flex-end; gap: 10px;">
            <a href="<?= url('nhansu') ?>" class="btn btn-outline btn-sm">🔄 Xóa bộ lọc</a>
            <button type="submit" class="btn btn-primary btn-sm">🔍 LỌC DỮ LIỆU</button>
        </div>
    </form>

    <!-- Employees Data Table -->
    <div class="table-responsive">
        <table class="table">
            <thead>
                <tr>
                    <th>Nhân sự</th>
                    <th>Mã NV</th>
                    <th>Đơn vị Vinamilk</th>
                    <th>Chức vụ & Ngạch lương</th>
                    <th>Hồ sơ Lưu kho</th>
                    <th>Trạng thái</th>
                    <th style="text-align: right;">Hành động</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($employees)): ?>
                    <tr>
                        <td colspan="7" style="text-align: center; color: var(--text-muted); padding: 30px;">
                            Không tìm thấy hồ sơ nhân sự nào phù hợp với bộ lọc.
                        </td>
                    </tr>
                <?php else: ?>
                    <?php foreach ($employees as $emp): ?>
                        <tr>
                            <td>
                                <div style="display: flex; align-items: center; gap: 12px;">
                                    <?php if (!empty($emp['AnhChanDung'])): ?>
                                        <img src="<?= asset($emp['AnhChanDung']) ?>" alt="Avatar" style="width: 44px; height: 44px; border-radius: 50%; object-fit: cover; border: 2px solid var(--border-color);">
                                    <?php else: ?>
                                        <div class="user-avatar" style="width: 44px; height: 44px; font-size: 1.1rem;">
                                            <?= strtoupper(substr($emp['HoTen'], 0, 1)) ?>
                                        </div>
                                    <?php endif; ?>

                                    <div>
                                        <a href="<?= url('nhansu/profile/' . $emp['MaNV']) ?>" style="font-weight: 700; color: var(--vnm-navy); font-size: 0.95rem;">
                                            <?= htmlspecialchars($emp['HoTen']) ?>
                                        </a>
                                        <div style="font-size: 0.8rem; color: var(--text-secondary);">
                                            SĐT: <?= htmlspecialchars($emp['SoDienThoai'] ?? 'Chưa cập nhật') ?>
                                        </div>
                                    </div>
                                </div>
                            </td>
                            <td><code><?= htmlspecialchars($emp['MaNV']) ?></code></td>
                            <td>
                                <div style="font-weight: 600; color: var(--vnm-navy); font-size: 0.85rem;">
                                    <?= htmlspecialchars($emp['TenDV'] ?? 'Vinamilk') ?>
                                </div>
                                <span class="badge badge-primary" style="font-size: 0.7rem; padding: 2px 8px;">
                                    <?= htmlspecialchars($emp['LoaiDV'] ?? 'Phòng') ?>
                                </span>
                            </td>
                            <td>
                                <div style="font-size: 0.85rem; font-weight: 600;">
                                    <?= htmlspecialchars($emp['TenCV'] ?? 'Chưa gán') ?>
                                </div>
                                <div style="font-size: 0.75rem; color: var(--text-secondary);">
                                    Ngạch: <code><?= htmlspecialchars($emp['MaNgach'] ?? '-') ?></code> (HS: <?= $emp['HeSoLuongHienTai'] ?>)
                                </div>
                            </td>
                            <td style="font-size: 0.8rem; color: var(--text-secondary);">
                                🗄️ <?= htmlspecialchars($emp['ViTriLuuHoSo'] ?? 'Kho VP / Tủ 01') ?>
                            </td>
                            <td>
                                <?php
                                $statusBadge = match((int)$emp['TrangThai']) {
                                    1 => '<span class="badge badge-success">Chính thức</span>',
                                    2 => '<span class="badge badge-warning">Thử việc</span>',
                                    3 => '<span class="badge badge-danger">Đã nghỉ việc</span>',
                                    4 => '<span class="badge badge-danger" style="background:#fef3c7; color:#b45309;">Đã nghỉ hưu</span>',
                                    default => '<span class="badge">Khác</span>'
                                };
                                echo $statusBadge;
                                ?>
                            </td>
                            <td style="text-align: right;">
                                <div style="display: flex; justify-content: flex-end; gap: 6px;">
                                    <a href="<?= url('nhansu/profile/' . $emp['MaNV']) ?>" class="btn btn-outline btn-sm" title="Xem hồ sơ Profile 360">
                                        👁️ Profile
                                    </a>
                                    <?php if (has_permission('NHANSU.Edit')): ?>
                                        <a href="<?= url('nhansu/edit/' . $emp['MaNV']) ?>" class="btn btn-outline btn-sm" title="Chỉnh sửa">
                                            ✏️ Sửa
                                        </a>
                                    <?php endif; ?>
                                    <a href="<?= url('nhansu/print2c/' . $emp['MaNV']) ?>" target="_blank" class="btn btn-primary btn-sm" title="In Sơ yếu lý lịch 2C">
                                        🖨️ In 2C
                                    </a>
                                </div>
                            </td>
                        </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
        </table>
    </div>

    <!-- Pagination -->
    <?php if ($pagination['totalPages'] > 1): ?>
        <div style="display: flex; align-items: center; justify-content: space-between; margin-top: 24px; padding-top: 16px; border-top: 1px solid var(--border-color);">
            <div style="font-size: 0.85rem; color: var(--text-secondary);">
                Trang <strong><?= $pagination['page'] ?></strong> / <strong><?= $pagination['totalPages'] ?></strong> (Hiển thị <?= count($employees) ?> / <?= number_format($pagination['total']) ?> bản ghi)
            </div>
            <div style="display: flex; gap: 6px;">
                <?php for ($i = 1; $i <= $pagination['totalPages']; $i++): ?>
                    <a href="<?= url('nhansu?page=' . $i . '&' . http_build_query($filters)) ?>" class="btn <?= ($i == $pagination['page']) ? 'btn-primary' : 'btn-outline' ?> btn-sm">
                        <?= $i ?>
                    </a>
                <?php endfor; ?>
            </div>
        </div>
    <?php endif; ?>
</div>
