<div class="container-fluid px-0">
    <!-- Tiêu đề trang -->
    <div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
        <div>
            <h3 class="fw-bold text-dark mb-1">
                <i class="bi bi-person-check-fill text-primary me-2"></i>Quy Trình Tiếp Nhận Nhân Sự Mới (Onboarding)
            </h3>
            <p class="text-muted small mb-0">
                Thực hiện đầy đủ 4 bước thiết lập hồ sơ, hợp đồng ban đầu, cấp phát trang thiết bị và tài khoản Vinamilk.
            </p>
        </div>
        <a href="<?= url('employees') ?>" class="btn btn-outline-secondary btn-sm">
            <i class="bi bi-x-circle me-1"></i> Hủy Bỏ
        </a>
    </div>

    <!-- Thanh chỉ báo tiến trình Wizard 4 bước -->
    <div class="card card-custom p-4 mb-4 border-0 shadow-sm" style="border-radius: 8px;">
        <div class="wizard-steps d-flex justify-content-between position-relative">
            <div class="wizard-step text-center flex-fill position-relative active" id="indicator-step-1">
                <div class="step-icon rounded-circle mx-auto d-flex align-items-center justify-content-center text-white fw-bold shadow-sm mb-2"
                     style="width: 44px; height: 44px; background: #0033A0; font-size: 1.1rem;">
                    1
                </div>
                <div class="step-title fw-bold text-primary small">Bước 1: Thông Tin Cá Nhân</div>
                <div class="step-desc text-muted" style="font-size: 0.75rem;">CCCD, MST, Liên hệ, Học vấn</div>
            </div>

            <div class="wizard-step text-center flex-fill position-relative" id="indicator-step-2">
                <div class="step-icon rounded-circle mx-auto d-flex align-items-center justify-content-center text-secondary bg-light border fw-bold mb-2"
                     style="width: 44px; height: 44px; font-size: 1.1rem;">
                    2
                </div>
                <div class="step-title fw-semibold text-secondary small">Bước 2: Đơn Vị & Chức Vụ</div>
                <div class="step-desc text-muted" style="font-size: 0.75rem;">Phòng ban, Chức danh, Quản lý</div>
            </div>

            <div class="wizard-step text-center flex-fill position-relative" id="indicator-step-3">
                <div class="step-icon rounded-circle mx-auto d-flex align-items-center justify-content-center text-secondary bg-light border fw-bold mb-2"
                     style="width: 44px; height: 44px; font-size: 1.1rem;">
                    3
                </div>
                <div class="step-title fw-semibold text-secondary small">Bước 3: Hợp Đồng & Lương</div>
                <div class="step-desc text-muted" style="font-size: 0.75rem;">Loại HĐ, Lương cơ bản, BHXH</div>
            </div>

            <div class="wizard-step text-center flex-fill position-relative" id="indicator-step-4">
                <div class="step-icon rounded-circle mx-auto d-flex align-items-center justify-content-center text-secondary bg-light border fw-bold mb-2"
                     style="width: 44px; height: 44px; font-size: 1.1rem;">
                    4
                </div>
                <div class="step-title fw-semibold text-secondary small">Bước 4: Cấp Tài Khoản & Tài Sản</div>
                <div class="step-desc text-muted" style="font-size: 0.75rem;">Tài khoản đăng nhập, Thẻ, PPE</div>
            </div>
        </div>
    </div>

    <!-- Khung Biểu mẫu Wizard 4 Bước -->
    <div class="card card-custom p-4 border-0 shadow-sm" style="border-radius: 8px;">
        <form action="<?= url('employees/wizard') ?>" method="POST" id="onboardingWizardForm" novalidate>
            <?= csrf_field() ?>

            <!-- ============================================================== -->
            <!-- BƯỚC 1: THÔNG TIN CÁ NHÂN -->
            <!-- ============================================================== -->
            <div class="wizard-pane active" id="pane-step-1">
                <h5 class="fw-bold text-dark border-bottom pb-2 mb-3">
                    <i class="bi bi-person-bounding-box text-primary me-2"></i>1. Thông Tin Cá Nhân & Định Danh Pháp Lý
                </h5>
                <div class="row g-3">
                    <div class="col-md-6">
                        <label class="form-label small fw-semibold">Họ và tên nhân sự <span class="text-danger">*</span></label>
                        <input type="text" name="HoTen" id="inputHoTen" class="form-control" placeholder="Nhập đầy đủ Họ và tên (chữ in hoa có dấu)" required value="<?= e($old['HoTen'] ?? '') ?>">
                        <div class="invalid-feedback">Vui lòng nhập họ và tên.</div>
                    </div>

                    <div class="col-md-3">
                        <label class="form-label small fw-semibold">Ngày sinh (Yêu cầu &ge; 18 tuổi) <span class="text-danger">*</span></label>
                        <input type="date" name="NgaySinh" id="inputNgaySinh" class="form-control" required value="<?= e($old['NgaySinh'] ?? '') ?>">
                        <div class="invalid-feedback" id="feedbackNgaySinh">Người lao động phải từ đủ 18 tuổi trở lên.</div>
                    </div>

                    <div class="col-md-3">
                        <label class="form-label small fw-semibold">Giới tính <span class="text-danger">*</span></label>
                        <select name="GioiTinh" class="form-select">
                            <option value="Nam" <?= (($old['GioiTinh'] ?? '') === 'Nam') ? 'selected' : '' ?>>Nam</option>
                            <option value="Nữ" <?= (($old['GioiTinh'] ?? '') === 'Nữ') ? 'selected' : '' ?>>Nữ</option>
                        </select>
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Số CCCD / Định danh (Đúng 12 số) <span class="text-danger">*</span></label>
                        <input type="text" name="SoCMND_CCCD" id="inputCCCD" maxlength="12" class="form-control font-monospace" placeholder="12 chữ số theo thẻ CCCD" required value="<?= e($old['SoCMND_CCCD'] ?? '') ?>">
                        <div class="invalid-feedback" id="feedbackCCCD">Số CCCD bắt buộc phải có đúng 12 chữ số.</div>
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Ngày cấp thẻ</label>
                        <input type="date" name="NgayCap" class="form-control" value="<?= e($old['NgayCap'] ?? '') ?>">
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Nơi cấp</label>
                        <input type="text" name="NoiCap" class="form-control" value="<?= e($old['NoiCap'] ?? 'Cục Cảnh sát QLHC về TTXH') ?>">
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Mã số thuế cá nhân (10 hoặc 13 số)</label>
                        <input type="text" name="MaSoThue" id="inputMST" class="form-control font-monospace" placeholder="10 hoặc 13 chữ số" value="<?= e($old['MaSoThue'] ?? '') ?>">
                        <div class="invalid-feedback" id="feedbackMST">Mã số thuế phải gồm 10 hoặc 13 chữ số.</div>
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Email làm việc / cá nhân (Duy nhất) <span class="text-danger">*</span></label>
                        <input type="email" name="Email" id="inputEmail" class="form-control" placeholder="example@vinamilk.com.vn" required value="<?= e($old['Email'] ?? '') ?>">
                        <div class="invalid-feedback">Email không đúng định dạng.</div>
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Số điện thoại liên hệ <span class="text-danger">*</span></label>
                        <input type="tel" name="SoDienThoai" class="form-control" placeholder="090x xxx xxx" required value="<?= e($old['SoDienThoai'] ?? '') ?>">
                    </div>

                    <div class="col-md-6">
                        <label class="form-label small fw-semibold">Quê quán</label>
                        <input type="text" name="QueQuan" class="form-control" placeholder="Xã/Huyện/Tỉnh nguyên quán" value="<?= e($old['QueQuan'] ?? '') ?>">
                    </div>

                    <div class="col-md-6">
                        <label class="form-label small fw-semibold">Địa chỉ nơi ở hiện tại <span class="text-danger">*</span></label>
                        <input type="text" name="DiaChiHienTai" class="form-control" placeholder="Số nhà, đường, phường/xã, quận/huyện, tỉnh/TP" required value="<?= e($old['DiaChiHienTai'] ?? '') ?>">
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Trình độ học vấn cao nhất</label>
                        <select name="TrinhDoHocVan" class="form-select">
                            <option value="Đại học">Đại học</option>
                            <option value="Thạc sĩ">Thạc sĩ</option>
                            <option value="Tiến sĩ">Tiến sĩ</option>
                            <option value="Cao đẳng">Cao đẳng</option>
                            <option value="Trung cấp">Trung cấp</option>
                            <option value="THPT">THPT</option>
                        </select>
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Chuyên ngành đào tạo</label>
                        <input type="text" name="ChuyenNganh" class="form-control" placeholder="Công nghệ thực phẩm, Cơ điện tử..." value="<?= e($old['ChuyenNganh'] ?? '') ?>">
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Cơ sở đào tạo (Trường)</label>
                        <input type="text" name="TruongDaoTao" class="form-control" placeholder="ĐH Bách Khoa, ĐH Nông Lâm..." value="<?= e($old['TruongDaoTao'] ?? '') ?>">
                    </div>
                </div>

                <div class="d-flex justify-content-end mt-4 pt-3 border-top">
                    <button type="button" class="btn btn-primary px-4 fw-semibold" style="background: #0033A0;" onclick="wizardNext(1)">
                        Tiếp Tục: Bước 2 &rarr;
                    </button>
                </div>
            </div>

            <!-- ============================================================== -->
            <!-- BƯỚC 2: ĐƠN VỊ & CHỨC VỤ -->
            <!-- ============================================================== -->
            <div class="wizard-pane d-none" id="pane-step-2">
                <h5 class="fw-bold text-dark border-bottom pb-2 mb-3">
                    <i class="bi bi-diagram-3-fill text-primary me-2"></i>2. Bố Trí Cơ Cấu Tổ Chức & Chức Vụ
                </h5>
                <div class="row g-3">
                    <div class="col-md-6">
                        <label class="form-label small fw-semibold">Đơn vị công tác (Cây tổ chức dm_donvi) <span class="text-danger">*</span></label>
                        <select name="MaDV" id="inputMaDV" class="form-select" required>
                            <option value="">-- Chọn đơn vị công tác --</option>
                            <?php foreach ($selectableUnits as $u): ?>
                                <option value="<?= e($u['MaDV']) ?>" <?= (($old['MaDV'] ?? '') === $u['MaDV']) ? 'selected' : '' ?>>
                                    <?= e($u['IndentTitle'] ?? $u['TenDV']) ?>
                                </option>
                            <?php endforeach; ?>
                        </select>
                        <div class="invalid-feedback">Vui lòng chọn đơn vị công tác.</div>
                    </div>

                    <div class="col-md-6">
                        <label class="form-label small fw-semibold">Chức danh / Chức vụ <span class="text-danger">*</span></label>
                        <select name="MaCV" class="form-select" required>
                            <?php foreach ($positions as $p): ?>
                                <option value="<?= e($p['MaCV']) ?>" <?= (($old['MaCV'] ?? 'NV') === $p['MaCV']) ? 'selected' : '' ?>>
                                    <?= e($p['TenCV']) ?>
                                </option>
                            <?php endforeach; ?>
                        </select>
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Ngày bắt đầu vào làm việc <span class="text-danger">*</span></label>
                        <input type="date" name="NgayVaoLam" class="form-control" value="<?= e($old['NgayVaoLam'] ?? date('Y-m-d')) ?>" required>
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Vùng lương tối thiểu (1, 2, 3, 4)</label>
                        <select name="VungLuong" class="form-select">
                            <option value="1">Vùng 1 (Đô thị loại 1: TP.HCM, Hà Nội...)</option>
                            <option value="2">Vùng 2 (Đô thị loại 2 / Nhà máy Tiên Sơn...)</option>
                            <option value="3">Vùng 3 (Trang trại / Vùng nguyên liệu)</option>
                            <option value="4">Vùng 4</option>
                        </select>
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Quản lý trực tiếp (Người duyệt đơn)</label>
                        <select name="MaNV_QuanLy" class="form-select">
                            <option value="">-- Không có / Tự quản lý --</option>
                            <?php foreach ($managers as $mgr): ?>
                                <option value="<?= e($mgr['MaNV']) ?>">
                                    <?= e($mgr['HoTen']) ?> (<?= e($mgr['TenCV'] ?? '') ?> - <?= e($mgr['TenDV'] ?? '') ?>)
                                </option>
                            <?php endforeach; ?>
                        </select>
                    </div>
                </div>

                <div class="d-flex justify-content-between mt-4 pt-3 border-top">
                    <button type="button" class="btn btn-outline-secondary px-4" onclick="wizardBack(2)">
                        &larr; Quay Lại Bước 1
                    </button>
                    <button type="button" class="btn btn-primary px-4 fw-semibold" style="background: #0033A0;" onclick="wizardNext(2)">
                        Tiếp Tục: Bước 3 &rarr;
                    </button>
                </div>
            </div>

            <!-- ============================================================== -->
            <!-- BƯỚC 3: HỢP ĐỒNG & CHẾ ĐỘ LƯƠNG -->
            <!-- ============================================================== -->
            <div class="wizard-pane d-none" id="pane-step-3">
                <h5 class="fw-bold text-dark border-bottom pb-2 mb-3">
                    <i class="bi bi-file-earmark-ruled-fill text-primary me-2"></i>3. Thiết Lập Hợp Đồng Lao Động & Chế Độ Lương
                </h5>
                <div class="row g-3">
                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Loại hợp đồng ban đầu <span class="text-danger">*</span></label>
                        <select name="loai_hd" class="form-select" id="wizardLoaiHD" onchange="toggleWizardContractDates()">
                            <option value="THU_VIEC">Hợp đồng thử việc (Tối đa 60 ngày)</option>
                            <option value="XAC_DINH_THOI_HAN_12T">HĐ xác định thời hạn 12 tháng (Lần 1)</option>
                            <option value="XAC_DINH_THOI_HAN_24T">HĐ xác định thời hạn 24 tháng (Lần 1)</option>
                            <option value="KHONG_XAC_DINH_THOI_HAN">HĐ KHÔNG XÁC ĐỊNH THỜI HẠN</option>
                        </select>
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Ngày bắt đầu hợp đồng <span class="text-danger">*</span></label>
                        <input type="date" name="tu_ngay" class="form-control" value="<?= date('Y-m-d') ?>" required>
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Ngày kết thúc hợp đồng</label>
                        <input type="date" name="den_ngay" id="wizardDenNgay" class="form-control" value="<?= date('Y-m-d', strtotime('+60 days')) ?>">
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Lương căn bản (VNĐ) <span class="text-danger">*</span></label>
                        <input type="number" name="luong_can_ban" class="form-control" value="12000000" required>
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Lương đóng bảo hiểm (BHXH)</label>
                        <input type="number" name="luong_dong_bh" class="form-control" value="12000000">
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Tỷ lệ hưởng lương (%)</label>
                        <input type="number" name="ty_le_huong_luong" class="form-control" value="85" min="50" max="100">
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Phụ cấp trách nhiệm (VNĐ)</label>
                        <input type="number" name="phu_cap_trach_nhiem" class="form-control" value="0">
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Phụ cấp ăn ca (VNĐ)</label>
                        <input type="number" name="phu_cap_an_ca" class="form-control" value="730000">
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Số người phụ thuộc giảm trừ gia cảnh</label>
                        <input type="number" name="SoNguoiPhuThuoc" class="form-control" value="0" min="0">
                    </div>

                    <div class="col-md-6">
                        <label class="form-label small fw-semibold">Số tài khoản ngân hàng</label>
                        <input type="text" name="SoTaiKhoanNH" class="form-control font-monospace" placeholder="Số TK nhận lương">
                    </div>

                    <div class="col-md-6">
                        <label class="form-label small fw-semibold">Tên ngân hàng</label>
                        <input type="text" name="NganHang" class="form-control" value="Vietcombank - CN TP.HCM">
                    </div>
                </div>

                <div class="d-flex justify-content-between mt-4 pt-3 border-top">
                    <button type="button" class="btn btn-outline-secondary px-4" onclick="wizardBack(3)">
                        &larr; Quay Lại Bước 2
                    </button>
                    <button type="button" class="btn btn-primary px-4 fw-semibold" style="background: #0033A0;" onclick="wizardNext(3)">
                        Tiếp Tục: Bước 4 &rarr;
                    </button>
                </div>
            </div>

            <!-- ============================================================== -->
            <!-- BƯỚC 4: CẤP TÀI KHOẢN & TÀI SẢN BAN ĐẦU -->
            <!-- ============================================================== -->
            <div class="wizard-pane d-none" id="pane-step-4">
                <h5 class="fw-bold text-dark border-bottom pb-2 mb-3">
                    <i class="bi bi-shield-lock-fill text-primary me-2"></i>4. Cấp Tài Khoản Hệ Thống & Bàn Giao Tài Sản Ban Đầu
                </h5>

                <!-- Phần tài khoản hệ thống -->
                <div class="card p-3 mb-4 bg-light border">
                    <div class="form-check form-switch mb-2">
                        <input class="form-check-input" type="checkbox" name="create_account" id="chkCreateAccount" value="1" checked onchange="toggleAccountFields()">
                        <label class="form-check-label fw-bold text-dark" for="chkCreateAccount">
                            Khởi tạo tài khoản đăng nhập Vinamilk Portal cho nhân sự
                        </label>
                    </div>
                    <small class="text-muted d-block mb-3">Mặc định kích hoạt cờ <code>MustChangePassword = 1</code> bắt buộc đổi mật khẩu khi đăng nhập lần đầu.</small>

                    <div class="row g-3" id="accountFields">
                        <div class="col-md-4">
                            <label class="form-label small fw-semibold">Tên đăng nhập (Username)</label>
                            <input type="text" name="account_username" id="inputUsername" class="form-control form-control-sm font-monospace" placeholder="Tự sinh nếu để trống">
                        </div>
                        <div class="col-md-4">
                            <label class="form-label small fw-semibold">Mật khẩu ban đầu (Bcrypt)</label>
                            <input type="text" name="account_password" class="form-control form-control-sm font-monospace" value="Vinamilk@2026">
                        </div>
                        <div class="col-md-4">
                            <label class="form-label small fw-semibold">Vai trò hệ thống</label>
                            <select name="RoleID" class="form-select form-select-sm">
                                <option value="7" selected>NHANVIEN (Cổng thông tin nhân viên ESS)</option>
                                <option value="6">QUANLY_TRUCTIEP (Quản lý trực tiếp)</option>
                                <option value="4">HR_DONVI (Nhân sự đơn vị)</option>
                            </select>
                        </div>
                    </div>
                </div>

                <!-- Phần cấp phát tài sản ban đầu -->
                <h6 class="fw-bold text-dark mb-3"><i class="bi bi-box-seam text-secondary me-2"></i>Cấp Phát Trang Thiết Bị & Tài Sản Ban Đầu</h6>
                <div class="row g-3">
                    <div class="col-md-4">
                        <div class="p-3 bg-white rounded border">
                            <div class="form-check mb-2">
                                <input class="form-check-input" type="checkbox" name="asset_card" id="chkAssetCard" value="1" checked>
                                <label class="form-check-label fw-semibold text-dark" for="chkAssetCard">
                                    Thẻ Từ Ra Vào / Thẻ NV
                                </label>
                            </div>
                            <input type="text" name="card_serial" class="form-control form-control-sm" placeholder="Mã thẻ từ (Serial)...">
                        </div>
                    </div>

                    <div class="col-md-4">
                        <div class="p-3 bg-white rounded border">
                            <div class="form-check mb-2">
                                <input class="form-check-input" type="checkbox" name="asset_laptop" id="chkAssetLaptop" value="1">
                                <label class="form-check-label fw-semibold text-dark" for="chkAssetLaptop">
                                    Máy Tính / Laptop Làm Việc
                                </label>
                            </div>
                            <input type="text" name="laptop_serial" class="form-control form-control-sm" placeholder="Mã Serial laptop...">
                        </div>
                    </div>

                    <div class="col-md-4">
                        <div class="p-3 bg-white rounded border">
                            <div class="form-check mb-2">
                                <input class="form-check-input" type="checkbox" name="asset_ppe" id="chkAssetPpe" value="1">
                                <label class="form-check-label fw-semibold text-dark" for="chkAssetPpe">
                                    Bảo Hộ Lao Động (PPE)
                                </label>
                            </div>
                            <small class="text-muted d-block">Trang bị 02 bộ đồng phục & mũ phòng sạch sản xuất.</small>
                        </div>
                    </div>
                </div>

                <div class="d-flex justify-content-between mt-4 pt-3 border-top">
                    <button type="button" class="btn btn-outline-secondary px-4" onclick="wizardBack(4)">
                        &larr; Quay Lại Bước 3
                    </button>
                    <button type="submit" class="btn btn-success px-4 fw-semibold shadow-sm">
                        <i class="bi bi-check-circle-fill me-1"></i> Hoàn Tất Tiếp Nhận Nhân Sự Mới
                    </button>
                </div>
            </div>
        </form>
    </div>
</div>

<script>
function wizardNext(currentStep) {
    if (!validateStep(currentStep)) return;

    document.getElementById('pane-step-' + currentStep).classList.add('d-none');
    document.getElementById('pane-step-' + (currentStep + 1)).classList.remove('d-none');

    // Cập nhật thanh tiến trình
    var curIcon = document.querySelector('#indicator-step-' + currentStep + ' .step-icon');
    curIcon.style.background = '#10B981'; // Green completed
    curIcon.innerHTML = '<i class="bi bi-check-lg"></i>';

    var nextInd = document.getElementById('indicator-step-' + (currentStep + 1));
    nextInd.classList.add('active');
    var nextIcon = nextInd.querySelector('.step-icon');
    nextIcon.style.background = '#0033A0';
    nextIcon.classList.remove('bg-light', 'text-secondary', 'border');
    nextIcon.classList.add('text-white');
    nextInd.querySelector('.step-title').classList.remove('text-secondary');
    nextInd.querySelector('.step-title').classList.add('text-primary');

    window.scrollTo({ top: 0, behavior: 'smooth' });
}

function wizardBack(currentStep) {
    document.getElementById('pane-step-' + currentStep).classList.add('d-none');
    document.getElementById('pane-step-' + (currentStep - 1)).classList.remove('d-none');

    var curInd = document.getElementById('indicator-step-' + currentStep);
    curInd.classList.remove('active');
    var curIcon = curInd.querySelector('.step-icon');
    curIcon.style.background = '';
    curIcon.classList.add('bg-light', 'text-secondary', 'border');
    curIcon.classList.remove('text-white');
    curInd.querySelector('.step-title').classList.add('text-secondary');
    curInd.querySelector('.step-title').classList.remove('text-primary');

    window.scrollTo({ top: 0, behavior: 'smooth' });
}

function validateStep(step) {
    if (step === 1) {
        var hoTen = document.getElementById('inputHoTen').value.trim();
        var dob = document.getElementById('inputNgaySinh').value;
        var cccd = document.getElementById('inputCCCD').value.trim();
        var email = document.getElementById('inputEmail').value.trim();
        var mst = document.getElementById('inputMST').value.trim();

        if (!hoTen) {
            document.getElementById('inputHoTen').classList.add('is-invalid');
            return false;
        } else {
            document.getElementById('inputHoTen').classList.remove('is-invalid');
        }

        // Validate tuổi >= 18
        if (!dob) {
            document.getElementById('inputNgaySinh').classList.add('is-invalid');
            return false;
        }
        var birthDate = new Date(dob);
        var today = new Date();
        var age = today.getFullYear() - birthDate.getFullYear();
        var m = today.getMonth() - birthDate.getMonth();
        if (m < 0 || (m === 0 && today.getDate() < birthDate.getDate())) {
            age--;
        }
        if (age < 18) {
            document.getElementById('inputNgaySinh').classList.add('is-invalid');
            return false;
        } else {
            document.getElementById('inputNgaySinh').classList.remove('is-invalid');
        }

        // Validate CCCD đúng 12 chữ số
        if (!/^[0-9]{12}$/.test(cccd)) {
            document.getElementById('inputCCCD').classList.add('is-invalid');
            return false;
        } else {
            document.getElementById('inputCCCD').classList.remove('is-invalid');
        }

        // Validate MST 10 hoặc 13 số nếu có nhập
        if (mst && !/^[0-9]{10}$|^[0-9]{13}$/.test(mst.replace(/-/g, ''))) {
            document.getElementById('inputMST').classList.add('is-invalid');
            return false;
        } else {
            document.getElementById('inputMST').classList.remove('is-invalid');
        }

        // Validate Email
        if (!email || !/\S+@\S+\.\S+/.test(email)) {
            document.getElementById('inputEmail').classList.add('is-invalid');
            return false;
        } else {
            document.getElementById('inputEmail').classList.remove('is-invalid');
        }
    } else if (step === 2) {
        var madv = document.getElementById('inputMaDV').value;
        if (!madv) {
            document.getElementById('inputMaDV').classList.add('is-invalid');
            return false;
        } else {
            document.getElementById('inputMaDV').classList.remove('is-invalid');
        }
    }
    return true;
}

function toggleWizardContractDates() {
    var loaiHD = document.getElementById('wizardLoaiHD').value;
    var denNgay = document.getElementById('wizardDenNgay');
    if (loaiHD === 'KHONG_XAC_DINH_THOI_HAN') {
        denNgay.value = '';
        denNgay.disabled = true;
    } else {
        denNgay.disabled = false;
        if (!denNgay.value) {
            var date = new Date();
            date.setDate(date.getDate() + 365);
            denNgay.value = date.toISOString().split('T')[0];
        }
    }
}

function toggleAccountFields() {
    var chk = document.getElementById('chkCreateAccount');
    document.getElementById('accountFields').style.display = chk.checked ? 'flex' : 'none';
}
</script>
