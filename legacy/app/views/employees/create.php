<?php
/**
 * View Form Thêm mới Hồ sơ Nhân sự 4 Tabs Số hóa
 */
?>
<div class="card-box">
    <div style="display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--border-color); padding-bottom: 16px; margin-bottom: 24px;">
        <div>
            <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;">
                ➕ Thêm mới Hồ sơ Nhân sự Số hóa (Vinamilk HUHA Standard)
            </h2>
            <p style="font-size: 0.85rem; color: var(--text-secondary); margin-top: 4px;">
                Nhập đầy đủ thông tin sơ yếu lý lịch, văn bằng, lưu vết hồ sơ giấy vật lý và ảnh chân dung.
            </p>
        </div>
        <a href="<?= url('nhansu') ?>" class="btn btn-outline">⬅️ Quay lại danh sách</a>
    </div>

    <!-- Navigation Tab Buttons -->
    <div style="display: flex; gap: 10px; border-bottom: 2px solid var(--border-color); margin-bottom: 24px;">
        <button type="button" class="tab-btn active" onclick="switchTab('tab1', this)" style="padding: 10px 20px; font-weight: 600; border: none; background: none; cursor: pointer; border-bottom: 3px solid var(--vnm-primary); color: var(--vnm-primary);">
            👤 Tab 1: Thông tin Cá nhân & CCCD
        </button>
        <button type="button" class="tab-btn" onclick="switchTab('tab2', this)" style="padding: 10px 20px; font-weight: 600; border: none; background: none; cursor: pointer; color: var(--text-secondary);">
            🏢 Tab 2: Công việc tại Vinamilk
        </button>
        <button type="button" class="tab-btn" onclick="switchTab('tab3', this)" style="padding: 10px 20px; font-weight: 600; border: none; background: none; cursor: pointer; color: var(--text-secondary);">
            🎓 Tab 3: Học vấn & Bằng cấp
        </button>
        <button type="button" class="tab-btn" onclick="switchTab('tab4', this)" style="padding: 10px 20px; font-weight: 600; border: none; background: none; cursor: pointer; color: var(--text-secondary);">
            📁 Tab 4: Hồ sơ Đính kèm & Lưu kho
        </button>
    </div>

    <form action="<?= url('nhansu/create') ?>" method="POST" enctype="multipart/form-data">
        <?= csrf_field() ?>

        <!-- TAB 1: THÔNG TIN CÁ NHÂN -->
        <div id="tab1" class="tab-content">
            <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 20px;">
                <div class="form-group">
                    <label class="form-label">Họ và tên khai sinh <span style="color:red;">*</span></label>
                    <input type="text" name="HoTen" class="form-control" placeholder="VD: Nguyễn Văn An" required>
                </div>

                <div class="form-group">
                    <label class="form-label">Tên thường gọi:</label>
                    <input type="text" name="TenThuongGoi" class="form-control" placeholder="VD: An">
                </div>

                <div class="form-group">
                    <label class="form-label">Ngày sinh <span style="color:red;">*</span></label>
                    <input type="date" name="NgaySinh" class="form-control" required value="1995-01-01">
                </div>

                <div class="form-group">
                    <label class="form-label">Giới tính <span style="color:red;">*</span></label>
                    <select name="GioiTinh" class="form-control" required>
                        <option value="Nam">Nam</option>
                        <option value="Nữ">Nữ</option>
                    </select>
                </div>

                <div class="form-group">
                    <label class="form-label">Số CMND / CCCD <span style="color:red;">*</span></label>
                    <input type="text" name="SoCMND_CCCD" class="form-control" placeholder="12 chữ số CCCD" required>
                </div>

                <div class="form-group">
                    <label class="form-label">Ngày cấp CCCD:</label>
                    <input type="date" name="NgayCap" class="form-control">
                </div>

                <div class="form-group">
                    <label class="form-label">Nơi cấp CCCD:</label>
                    <input type="text" name="NoiCap" class="form-control" placeholder="VD: Cục Cảnh sát QLHC về trật tự xã hội">
                </div>

                <div class="form-group">
                    <label class="form-label">Email công việc Vinamilk:</label>
                    <input type="email" name="Email" class="form-control" placeholder="an.nv@vinamilk.com.vn">
                </div>

                <div class="form-group">
                    <label class="form-label">Số điện thoại liên hệ:</label>
                    <input type="text" name="SoDienThoai" class="form-control" placeholder="0901234567">
                </div>

                <div class="form-group">
                    <label class="form-label">Dân tộc:</label>
                    <input type="text" name="DanToc" class="form-control" value="Kinh">
                </div>

                <div class="form-group">
                    <label class="form-label">Tôn giáo:</label>
                    <input type="text" name="TonGiao" class="form-control" value="Không">
                </div>

                <div class="form-group">
                    <label class="form-label">Tình trạng hôn nhân:</label>
                    <select name="TinhTrangHonNhan" class="form-control">
                        <option value="Độc thân">Độc thân</option>
                        <option value="Đã kết hôn">Đã kết hôn</option>
                        <option value="Ly hôn">Ly hôn</option>
                    </select>
                </div>
            </div>

            <div class="form-group" style="margin-top: 15px;">
                <label class="form-label">Quê quán:</label>
                <input type="text" name="QueQuan" class="form-control" placeholder="Xã/Phường, Huyện/Quận, Tỉnh/TP">
            </div>

            <div class="form-group">
                <label class="form-label">Hộ khẩu thường trú:</label>
                <input type="text" name="NoiDKKTTru" class="form-control" placeholder="Số nhà, Đường, Phường/Xã, Quận/Huyện...">
            </div>

            <div class="form-group">
                <label class="form-label">Nơi ở hiện tại:</label>
                <input type="text" name="DiaChiHienTai" class="form-control" placeholder="Địa chỉ cư trú hiện tại">
            </div>

            <div style="display: flex; gap: 30px; margin-top: 15px;">
                <label style="display: flex; align-items: center; gap: 8px; cursor: pointer; font-weight: 600;">
                    <input type="checkbox" name="DangVien" value="1" style="accent-color: var(--vnm-primary);"> Là Đảng viên ĐCSVN
                </label>
                <label style="display: flex; align-items: center; gap: 8px; cursor: pointer; font-weight: 600;">
                    <input type="checkbox" name="DoanVien" value="1" style="accent-color: var(--vnm-primary);" checked> Là Đoàn viên Công đoàn
                </label>
            </div>
        </div>

        <!-- TAB 2: THÔNG TIN CÔNG VIỆC -->
        <div id="tab2" class="tab-content" style="display: none;">
            <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 20px;">
                <div class="form-group">
                    <label class="form-label">Mã Nhân viên <span style="color:red;">*</span></label>
                    <input type="text" name="MaNV" class="form-control" value="<?= $nextMaNV ?>" required>
                </div>

                <div class="form-group">
                    <label class="form-label">Đơn vị Vinamilk trực thuộc <span style="color:red;">*</span></label>
                    <select name="MaDV" class="form-control" required>
                        <?php foreach ($options['donvi'] as $dv): ?>
                            <option value="<?= $dv['MaDV'] ?>">
                                <?= htmlspecialchars($dv['TenDV']) ?> (<?= $dv['LoaiDV'] ?>)
                            </option>
                        <?php endforeach; ?>
                    </select>
                </div>

                <div class="form-group">
                    <label class="form-label">Chức vụ / Vị trí:</label>
                    <select name="MaCV" class="form-control">
                        <?php foreach ($options['chucvu'] as $cv): ?>
                            <option value="<?= $cv['MaCV'] ?>"><?= htmlspecialchars($cv['TenCV']) ?></option>
                        <?php endforeach; ?>
                    </select>
                </div>

                <div class="form-group">
                    <label class="form-label">Ngạch lương chức danh:</label>
                    <select name="MaNgach" class="form-control">
                        <?php foreach ($options['ngach'] as $ng): ?>
                            <option value="<?= $ng['MaNgach'] ?>"><?= htmlspecialchars($ng['TenNgach']) ?> (Mã: <?= $ng['MaNgach'] ?>)</option>
                        <?php endforeach; ?>
                    </select>
                </div>

                <div class="form-group">
                    <label class="form-label">Hệ số lương hiện tại:</label>
                    <input type="number" step="0.01" name="HeSoLuongHienTai" class="form-control" value="2.34">
                </div>

                <div class="form-group">
                    <label class="form-label">Bậc lương hiện tại:</label>
                    <input type="number" name="BacLuongHienTai" class="form-control" value="1">
                </div>

                <div class="form-group">
                    <label class="form-label">Ngày vào làm <span style="color:red;">*</span></label>
                    <input type="date" name="NgayVaoLam" class="form-control" required value="<?= date('Y-m-d') ?>">
                </div>

                <div class="form-group">
                    <label class="form-label">Loại hợp đồng lao động:</label>
                    <select name="LoaiHopDong" class="form-control">
                        <option value="Thử việc">Thử việc (1-3 tháng)</option>
                        <option value="1 năm">Xác định thời hạn 1 năm</option>
                        <option value="3 năm">Xác định thời hạn 3 năm</option>
                        <option value="Không thời hạn">Hợp đồng không xác định thời hạn</option>
                    </select>
                </div>

                <div class="form-group">
                    <label class="form-label">Trạng thái làm việc:</label>
                    <select name="TrangThai" class="form-control">
                        <option value="2" selected>2. Đang thử việc</option>
                        <option value="1">1. Chính thức làm việc</option>
                    </select>
                </div>
            </div>
        </div>

        <!-- TAB 3: HỌC VẤN & BẰNG CẤP -->
        <div id="tab3" class="tab-content" style="display: none;">
            <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 20px;">
                <div class="form-group">
                    <label class="form-label">Trình độ học vấn cao nhất:</label>
                    <select name="TrinhDoHocVan" class="form-control">
                        <option value="Đại học">Đại học</option>
                        <option value="Thạc sĩ">Thạc sĩ</option>
                        <option value="Tiến sĩ">Tiến sĩ</option>
                        <option value="Cao đẳng">Cao đẳng</option>
                        <option value="Trung cấp">Trung cấp</option>
                    </select>
                </div>

                <div class="form-group">
                    <label class="form-label">Chuyên ngành đào tạo:</label>
                    <input type="text" name="ChuyenNganh" class="form-control" placeholder="VD: Công nghệ Thực phẩm, Quản trị Nhân lực...">
                </div>

                <div class="form-group">
                    <label class="form-label">Trường đào tạo / Cơ sở cấp bằng:</label>
                    <input type="text" name="TruongDaoTao" class="form-control" placeholder="VD: Đại học Bách Khoa TP.HCM">
                </div>
            </div>
        </div>

        <!-- TAB 4: HỒ SƠ ĐÍNH KÈM & LƯU KHO -->
        <div id="tab4" class="tab-content" style="display: none;">
            <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 20px;">
                <div class="form-group">
                    <label class="form-label">Vị trí tủ/kệ/ngăn lưu hồ sơ giấy (Vật lý):</label>
                    <input type="text" name="ViTriLuuHoSo" class="form-control" value="Kho VP / Tủ 01 / Ngăn A" placeholder="VD: Kho NM-SG / Tủ 03 / Ngăn B">
                </div>

                <div class="form-group">
                    <label class="form-label">Tải lên Ảnh chân dung (.JPG, .PNG):</label>
                    <input type="file" name="AnhChanDung" class="form-control" accept="image/*">
                </div>
            </div>

            <div class="alert alert-info" style="margin-top: 20px;">
                <span>ℹ️</span>
                <div>Hệ thống tự động lưu vết vị trí hồ sơ giấy vật lý để phục vụ mượn/thu hồi và kiểm tra định kỳ của Ban Kiểm soát Vinamilk.</div>
            </div>
        </div>

        <!-- Form Submit Bar -->
        <div style="display: flex; justify-content: flex-end; gap: 14px; margin-top: 30px; padding-top: 20px; border-top: 1px solid var(--border-color);">
            <a href="<?= url('nhansu') ?>" class="btn btn-outline">Hủy bỏ</a>
            <button type="submit" class="btn btn-primary" style="padding: 12px 30px;">
                💾 LƯU HỒ SƠ NHÂN SỰ
            </button>
        </div>
    </form>
</div>

<script>
function switchTab(tabId, btn) {
    document.querySelectorAll('.tab-content').forEach(el => el.style.display = 'none');
    document.querySelectorAll('.tab-btn').forEach(el => {
        el.style.borderBottom = 'none';
        el.style.color = 'var(--text-secondary)';
    });

    document.getElementById(tabId).style.display = 'block';
    btn.style.borderBottom = '3px solid var(--vnm-primary)';
    btn.style.color = 'var(--vnm-primary)';
}
</script>
