<?php
/**
 * View Employee Profile 360 (Hồ sơ Nhân viên số hóa chi tiết)
 */
?>
<div class="card-box">
    <!-- Header Summary Card -->
    <div style="display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--border-color); padding-bottom: 24px; margin-bottom: 30px; flex-wrap: wrap; gap: 20px;">
        <div style="display: flex; align-items: center; gap: 24px;">
            <?php if (!empty($emp['AnhChanDung'])): ?>
                <img src="<?= asset($emp['AnhChanDung']) ?>" alt="Avatar" style="width: 90px; height: 90px; border-radius: 50%; object-fit: cover; border: 3px solid var(--vnm-primary); box-shadow: var(--shadow-md);">
            <?php else: ?>
                <div class="user-avatar" style="width: 90px; height: 90px; font-size: 2.5rem; border: 3px solid var(--vnm-primary);">
                    <?= strtoupper(substr($emp['HoTen'], 0, 1)) ?>
                </div>
            <?php endif; ?>

            <div>
                <div style="display: flex; align-items: center; gap: 12px; margin-bottom: 4px;">
                    <h2 style="font-size: 1.6rem; font-weight: 800; color: var(--vnm-navy);"><?= htmlspecialchars($emp['HoTen']) ?></h2>
                    <span class="badge badge-primary"><code><?= $emp['MaNV'] ?></code></span>
                    <?php
                    $statusBadge = match((int)$emp['TrangThai']) {
                        1 => '<span class="badge badge-success">Đang làm việc chính thức</span>',
                        2 => '<span class="badge badge-warning">Đang thử việc</span>',
                        3 => '<span class="badge badge-danger">Đã nghỉ việc</span>',
                        4 => '<span class="badge badge-danger" style="background:#fef3c7; color:#b45309;">Nghỉ hưu</span>',
                        default => ''
                    };
                    echo $statusBadge;
                    ?>
                </div>

                <div style="font-size: 0.95rem; color: var(--text-primary); font-weight: 600; margin-bottom: 4px;">
                    🏢 <?= htmlspecialchars($emp['TenCV'] ?? 'Chưa gán chức vụ') ?> — <span style="color: var(--vnm-primary);"><?= htmlspecialchars($emp['TenDV'] ?? 'Vinamilk') ?></span>
                </div>

                <div style="font-size: 0.85rem; color: var(--text-secondary);">
                    📧 Email: <?= htmlspecialchars($emp['Email'] ?? '-') ?> | 📱 SĐT: <?= htmlspecialchars($emp['SoDienThoai'] ?? '-') ?> | 🗄️ Vị trí lưu kho: <strong><?= htmlspecialchars($emp['ViTriLuuHoSo'] ?? 'Kho VP / Tủ 01') ?></strong>
                </div>
            </div>
        </div>

        <!-- Toolbar Action Buttons -->
        <div style="display: flex; gap: 10px; flex-wrap: wrap;">
            <?php if (has_permission('NHANSU.Edit')): ?>
                <a href="<?= url('nhansu/edit/' . $emp['MaNV']) ?>" class="btn btn-outline">✏️ Sửa hồ sơ</a>
            <?php endif; ?>
            <a href="<?= url('tuyenchuyen/create?manv=' . $emp['MaNV']) ?>" class="btn btn-outline">🔄 Thuyên chuyển</a>
            <a href="<?= url('ktkl/create?manv=' . $emp['MaNV']) ?>" class="btn btn-outline">🏅 Khen thưởng/KL</a>
            <a href="<?= url('nhansu/print2c/' . $emp['MaNV']) ?>" target="_blank" class="btn btn-primary">🖨️ In Lý lịch 2C</a>
        </div>
    </div>

    <!-- Grid 2 Column Profile Info -->
    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 30px; margin-bottom: 30px;">
        <!-- Cột 1: Thông tin Sơ yếu lý lịch & Nhân thân -->
        <div style="border: 1px solid var(--border-color); border-radius: var(--radius-lg); padding: 20px; background: #ffffff;">
            <h3 style="font-size: 1.1rem; font-weight: 700; color: var(--vnm-navy); border-bottom: 2px solid var(--vnm-primary); padding-bottom: 8px; margin-bottom: 16px;">
                👤 1. Thông tin Sơ yếu lý lịch & Nhân thân
            </h3>

            <table class="table">
                <tr>
                    <td style="width: 170px; font-weight: 600; color: var(--text-secondary);">Tên thường gọi:</td>
                    <td><?= htmlspecialchars($emp['TenThuongGoi'] ?? '-') ?></td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Ngày sinh & Giới tính:</td>
                    <td><?= format_date($emp['NgaySinh']) ?> (<?= $emp['GioiTinh'] ?>)</td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Số CMND / CCCD:</td>
                    <td><strong><?= htmlspecialchars($emp['SoCMND_CCCD']) ?></strong> (Cấp ngày: <?= format_date($emp['NgayCap']) ?>)</td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Quê quán:</td>
                    <td><?= htmlspecialchars($emp['QueQuan'] ?? '-') ?></td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Hộ khẩu thường trú:</td>
                    <td><?= htmlspecialchars($emp['NoiDKKTTru'] ?? '-') ?></td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Địa chỉ hiện tại:</td>
                    <td><?= htmlspecialchars($emp['DiaChiHienTai'] ?? '-') ?></td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Dân tộc / Tôn giáo:</td>
                    <td><?= htmlspecialchars($emp['DanToc'] ?? 'Kinh') ?> / <?= htmlspecialchars($emp['TonGiao'] ?? 'Không') ?></td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Hôn nhân & Tổ chức:</td>
                    <td>
                        <?= htmlspecialchars($emp['TinhTrangHonNhan'] ?? 'Độc thân') ?> | 
                        <?= ((int)$emp['DangVien'] === 1) ? '<span class="badge badge-danger">Đảng viên</span>' : '' ?> 
                        <?= ((int)$emp['DoanVien'] === 1) ? '<span class="badge badge-primary">Đoàn viên</span>' : '' ?>
                    </td>
                </tr>
            </table>
        </div>

        <!-- Cột 2: Thông tin Công việc & Ngạch Lương Vinamilk -->
        <div style="border: 1px solid var(--border-color); border-radius: var(--radius-lg); padding: 20px; background: #ffffff;">
            <h3 style="font-size: 1.1rem; font-weight: 700; color: var(--vnm-navy); border-bottom: 2px solid var(--vnm-primary); padding-bottom: 8px; margin-bottom: 16px;">
                💼 2. Vị trí Công việc & Xếp lương Vinamilk
            </h3>

            <table class="table">
                <tr>
                    <td style="width: 170px; font-weight: 600; color: var(--text-secondary);">Đơn vị trực thuộc:</td>
                    <td style="font-weight: 700; color: var(--vnm-navy);"><?= htmlspecialchars($emp['TenDV'] ?? '-') ?></td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Chức vụ & Hệ số CV:</td>
                    <td><?= htmlspecialchars($emp['TenCV'] ?? '-') ?> (Hệ số CV: <?= $emp['HeSoCV'] ?? '0.0' ?>)</td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Ngạch lương chức danh:</td>
                    <td><strong><?= htmlspecialchars($emp['TenNgach'] ?? '-') ?></strong> (Mã: <code><?= $emp['MaNgach'] ?></code>)</td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Hệ số & Bậc lương:</td>
                    <td><span class="badge badge-success">Hệ số: <?= $emp['HeSoLuongHienTai'] ?></span> (Bậc: <?= $emp['BacLuongHienTai'] ?>)</td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Ngày vào làm:</td>
                    <td><?= format_date($emp['NgayVaoLam']) ?></td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Loại hợp đồng:</td>
                    <td><?= htmlspecialchars($emp['LoaiHopDong'] ?? '-') ?></td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Hồ sơ giấy vật lý:</td>
                    <td><span class="badge badge-primary">🗄️ <?= htmlspecialchars($emp['ViTriLuuHoSo'] ?? 'Kho VP / Tủ 01') ?></span></td>
                </tr>
                <tr>
                    <td style="font-weight: 600; color: var(--text-secondary);">Tài khoản Ngân hàng:</td>
                    <td><?= htmlspecialchars($emp['SoTaiKhoanNH'] ?? '-') ?> (<?= htmlspecialchars($emp['NganHang'] ?? '-') ?>)</td>
                </tr>
            </table>
        </div>
    </div>

    <!-- Row 3: Lịch sử Quá trình công tác & Thuyên chuyển -->
    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 30px;">
        <!-- Quá trình công tác -->
        <div style="border: 1px solid var(--border-color); border-radius: var(--radius-lg); padding: 20px; background: #ffffff;">
            <h3 style="font-size: 1.1rem; font-weight: 700; color: var(--vnm-navy); border-bottom: 2px solid var(--vnm-primary); padding-bottom: 8px; margin-bottom: 16px;">
                📜 3. Lịch sử Quá trình Công tác
            </h3>

            <?php if (empty($emp['QuatTrinhCongTac'])): ?>
                <p style="color: var(--text-muted); font-size: 0.85rem;">Chưa có quá trình công tác được ghi nhận.</p>
            <?php else: ?>
                <ul style="list-style: none; padding: 0;">
                    <?php foreach ($emp['QuatTrinhCongTac'] as $qt): ?>
                        <li style="padding-bottom: 12px; margin-bottom: 12px; border-bottom: 1px dashed var(--border-color);">
                            <div style="font-weight: 700; color: var(--vnm-navy); font-size: 0.9rem;">
                                <?= htmlspecialchars($qt['DonViCongTac']) ?> — <span style="color: var(--vnm-primary);"><?= htmlspecialchars($qt['ChucVu']) ?></span>
                            </div>
                            <div style="font-size: 0.8rem; color: var(--text-secondary);">
                                ⏱️ <?= format_date($qt['TuNgay']) ?> đến <?= !empty($qt['DenNgay']) ? format_date($qt['DenNgay']) : 'Hiện tại' ?>
                            </div>
                            <div style="font-size: 0.8rem; color: var(--text-muted); margin-top: 2px;">
                                <?= htmlspecialchars($qt['CongViecChinh'] ?? '') ?>
                            </div>
                        </li>
                    <?php endforeach; ?>
                </ul>
            <?php endif; ?>
        </div>

        <!-- Lịch sử Thuyên chuyển -->
        <div style="border: 1px solid var(--border-color); border-radius: var(--radius-lg); padding: 20px; background: #ffffff;">
            <h3 style="font-size: 1.1rem; font-weight: 700; color: var(--vnm-navy); border-bottom: 2px solid var(--vnm-primary); padding-bottom: 8px; margin-bottom: 16px;">
                🔄 4. Lịch sử Thuyên chuyển & Điều động
            </h3>

            <?php if (empty($emp['QuatTrinhThuyenChuyen'])): ?>
                <p style="color: var(--text-muted); font-size: 0.85rem;">Chưa có lịch sử thuyên chuyển công tác.</p>
            <?php else: ?>
                <ul style="list-style: none; padding: 0;">
                    <?php foreach ($emp['QuatTrinhThuyenChuyen'] as $tc): ?>
                        <li style="padding-bottom: 12px; margin-bottom: 12px; border-bottom: 1px dashed var(--border-color);">
                            <div style="font-weight: 700; color: var(--vnm-navy); font-size: 0.9rem;">
                                📜 Số QĐ: <code><?= htmlspecialchars($tc['SoQD']) ?></code>
                            </div>
                            <div style="font-size: 0.85rem;">
                                Từ <strong><?= htmlspecialchars($tc['TenDonViCu'] ?? $tc['DonViCu']) ?></strong> ➡️ sang <strong><?= htmlspecialchars($tc['TenDonViMoi'] ?? $tc['DonViMoi']) ?></strong>
                            </div>
                            <div style="font-size: 0.8rem; color: var(--text-secondary);">
                                📅 Ngày hiệu lực: <?= format_date($tc['NgayHieuLuc']) ?> | Lý do: <?= htmlspecialchars($tc['LyDo'] ?? '-') ?>
                            </div>
                        </li>
                    <?php endforeach; ?>
                </ul>
            <?php endif; ?>
        </div>
    </div>
</div>
