<?php
$statusBadge = match ((int)$employee['TrangThai']) {
    1 => ['label' => 'Đang làm việc', 'class' => 'bg-success-subtle text-success border border-success-subtle'],
    2 => ['label' => 'Thử việc', 'class' => 'bg-warning-subtle text-warning border border-warning-subtle'],
    3 => ['label' => 'Nghỉ việc', 'class' => 'bg-danger-subtle text-danger border border-danger-subtle'],
    4 => ['label' => 'Nghỉ hưu', 'class' => 'bg-secondary-subtle text-secondary border border-secondary-subtle'],
    default => ['label' => 'Khác', 'class' => 'bg-light text-dark border']
};

$avatarLetter = mb_substr($employee['HoTen'], 0, 1, 'UTF-8');
?>

<!-- Thanh điều hướng và nút thao tác -->
<div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
    <div class="d-flex align-items-center gap-2">
        <a href="<?= url('employees') ?>" class="btn btn-outline-secondary btn-sm" title="Quay lại danh sách">
            <i class="bi bi-arrow-left"></i>
        </a>
        <h3 class="fw-bold text-dark mb-0">Hồ Sơ Nhân Sự Chi Tiết</h3>
    </div>

    <div class="d-flex gap-2">
        <a href="<?= url('employees/print_profile/' . $employee['MaNV']) ?>" target="_blank" class="btn btn-outline-secondary btn-sm fw-semibold">
            <i class="bi bi-printer me-1"></i> In Trích Lục A4
        </a>
        <?php if (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI'])): ?>
        <a href="#modalCreateContract" data-bs-toggle="modal" class="btn btn-primary btn-sm fw-semibold" style="background: #0033A0; border-color: #0033A0;">
            <i class="bi bi-file-earmark-plus me-1"></i> Lập Hợp Đồng Mới
        </a>
        <?php endif; ?>
    </div>
</div>

<!-- Thẻ tổng quan nhân sự (Hero Card) -->
<div class="card card-custom p-4 mb-4 border-0 shadow-sm" style="border-radius: 8px;">
    <div class="d-flex align-items-center flex-wrap gap-4">
        <!-- Avatar Initial -->
        <div class="rounded-circle d-flex align-items-center justify-content-center text-white fw-bold shadow-sm"
             style="width: 80px; height: 80px; background: linear-gradient(135deg, #0033A0, #0056D2); font-size: 2rem;">
            <?= e($avatarLetter) ?>
        </div>

        <!-- Thông tin vắn tắt -->
        <div class="flex-grow-1">
            <div class="d-flex align-items-center gap-2 flex-wrap mb-1">
                <h4 class="fw-bold text-dark mb-0"><?= e($employee['HoTen']) ?></h4>
                <span class="badge bg-light text-primary border font-monospace px-2 py-1"><?= e($employee['MaNV']) ?></span>
                <span class="badge <?= $statusBadge['class'] ?> px-2 py-1"><?= $statusBadge['label'] ?></span>
            </div>
            <div class="text-muted small d-flex flex-wrap gap-3 mt-1">
                <span><i class="bi bi-building me-1"></i><?= e($employee['TenDV'] ?: $employee['MaDV']) ?></span>
                <span><i class="bi bi-briefcase me-1"></i><?= e($employee['TenCV'] ?: ($employee['ChucVu'] ?? 'Nhân viên')) ?></span>
                <span><i class="bi bi-calendar-check me-1"></i>Vào làm: <?= format_date($employee['NgayVaoLam']) ?> (<?= ($employee['ThamNienNam'] ?? 0) ?> năm)</span>
                <span><i class="bi bi-envelope me-1"></i><?= e($employee['Email']) ?></span>
                <span><i class="bi bi-telephone me-1"></i><?= e($employee['SoDienThoai']) ?></span>
            </div>
        </div>

        <!-- Cảnh báo thời hạn hợp đồng nhanh -->
        <?php if (!empty($employee['LoaiHopDong'])): ?>
        <div class="text-end bg-light p-3 rounded border">
            <small class="text-muted d-block mb-1">Hợp đồng hiện tại</small>
            <div class="fw-bold text-dark"><?= e($employee['LoaiHopDong']) ?></div>
            <?php if (!empty($employee['NgayHetHanHD'])): ?>
                <small class="d-block mt-1 <?= ($employee['SoNgayConLaiHD'] <= 30) ? 'text-danger fw-bold' : (($employee['SoNgayConLaiHD'] <= 60) ? 'text-warning fw-bold' : 'text-muted') ?>">
                    Hết hạn: <?= format_date($employee['NgayHetHanHD']) ?>
                    <?php if ($employee['SoNgayConLaiHD'] <= 60 && $employee['SoNgayConLaiHD'] >= 0): ?>
                        <span class="badge bg-<?= ($employee['SoNgayConLaiHD'] <= 30) ? 'danger' : 'warning text-dark' ?> ms-1">
                            Còn <?= $employee['SoNgayConLaiHD'] ?> ngày
                        </span>
                    <?php endif; ?>
                </small>
            <?php else: ?>
                <small class="text-success fw-semibold"><i class="bi bi-infinity"></i> Không thời hạn</small>
            <?php endif; ?>
        </div>
        <?php endif; ?>
    </div>
</div>

<!-- Tabs Điều Hướng 8 Phân Hệ -->
<div class="card card-custom border-0 shadow-sm" style="border-radius: 8px;">
    <div class="card-header bg-white border-bottom p-0">
        <ul class="nav nav-tabs card-header-tabs m-0 px-3 border-0 flex-nowrap overflow-auto" id="profileTab" role="tablist">
            <li class="nav-item">
                <a class="nav-link <?= ($activeTab === 'ly-lich') ? 'active fw-bold text-primary border-primary border-bottom-0' : 'text-secondary' ?> py-3" 
                   data-bs-toggle="tab" href="#tab-ly-lich" role="tab">
                    <i class="bi bi-person-lines-fill me-1"></i> 1. Lý Lịch
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link <?= ($activeTab === 'hop-dong') ? 'active fw-bold text-primary border-primary border-bottom-0' : 'text-secondary' ?> py-3" 
                   data-bs-toggle="tab" href="#tab-hop-dong" role="tab">
                    <i class="bi bi-file-earmark-text me-1"></i> 2. Hợp Đồng
                    <?php if (!empty($contracts)): ?>
                        <span class="badge bg-secondary-subtle text-secondary rounded-pill"><?= count($contracts) ?></span>
                    <?php endif; ?>
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link <?= ($activeTab === 'cong-tac') ? 'active fw-bold text-primary border-primary border-bottom-0' : 'text-secondary' ?> py-3" 
                   data-bs-toggle="tab" href="#tab-cong-tac" role="tab">
                    <i class="bi bi-clock-history me-1"></i> 3. Quá Trình Công Tác
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link <?= ($activeTab === 'luong') ? 'active fw-bold text-primary border-primary border-bottom-0' : 'text-secondary' ?> py-3" 
                   data-bs-toggle="tab" href="#tab-luong" role="tab">
                    <i class="bi bi-cash-stack me-1"></i> 4. Lương & Phụ Cấp
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link <?= ($activeTab === 'dao-tao') ? 'active fw-bold text-primary border-primary border-bottom-0' : 'text-secondary' ?> py-3" 
                   data-bs-toggle="tab" href="#tab-dao-tao" role="tab">
                    <i class="bi bi-award me-1"></i> 5. Đào Tạo & Chứng Chỉ
                    <?php if (!empty($certificates)): ?>
                        <span class="badge bg-secondary-subtle text-secondary rounded-pill"><?= count($certificates) ?></span>
                    <?php endif; ?>
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link <?= ($activeTab === 'kt-kl') ? 'active fw-bold text-primary border-primary border-bottom-0' : 'text-secondary' ?> py-3" 
                   data-bs-toggle="tab" href="#tab-kt-kl" role="tab">
                    <i class="bi bi-trophy me-1"></i> 6. KT - KL
                    <?php if (!empty($rewardsDisciplines)): ?>
                        <span class="badge bg-secondary-subtle text-secondary rounded-pill"><?= count($rewardsDisciplines) ?></span>
                    <?php endif; ?>
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link <?= ($activeTab === 'tai-san') ? 'active fw-bold text-primary border-primary border-bottom-0' : 'text-secondary' ?> py-3" 
                   data-bs-toggle="tab" href="#tab-tai-san" role="tab">
                    <i class="bi bi-laptop me-1"></i> 7. Tài Sản Cấp Phát
                    <?php if (!empty($assets)): ?>
                        <span class="badge bg-secondary-subtle text-secondary rounded-pill"><?= count($assets) ?></span>
                    <?php endif; ?>
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link <?= ($activeTab === 'tai-lieu') ? 'active fw-bold text-primary border-primary border-bottom-0' : 'text-secondary' ?> py-3" 
                   data-bs-toggle="tab" href="#tab-tai-lieu" role="tab">
                    <i class="bi bi-paperclip me-1"></i> 8. Tài Liệu Scan
                    <?php if (!empty($documents)): ?>
                        <span class="badge bg-primary text-white rounded-pill"><?= count($documents) ?></span>
                    <?php endif; ?>
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link <?= ($activeTab === 'offboarding') ? 'active fw-bold text-danger border-danger border-bottom-0' : 'text-secondary' ?> py-3" 
                   data-bs-toggle="tab" href="#tab-offboarding" role="tab">
                    <i class="bi bi-box-arrow-right me-1"></i> 9. Thôi Việc (Offboarding)
                    <?php if (!empty($offboarding)): ?>
                        <span class="badge bg-danger text-white rounded-pill ms-1"><?= $offboarding['so_buoc_hoan_thanh'] ?>/6</span>
                    <?php endif; ?>
                </a>
            </li>
        </ul>
    </div>

    <div class="card-body p-4 tab-content">
        <!-- ============================================================== -->
        <!-- TAB 1: LÝ LỊCH NHÂN SỰ -->
        <!-- ============================================================== -->
        <div class="tab-pane fade <?= ($activeTab === 'ly-lich') ? 'show active' : '' ?>" id="tab-ly-lich" role="tabpanel">
            <h5 class="fw-bold text-dark border-bottom pb-2 mb-3">Thông Tin Định Danh & Cá Nhân</h5>
            <div class="row g-3 mb-4">
                <div class="col-md-3">
                    <label class="text-muted small d-block">Số CCCD / Định danh</label>
                    <span class="fw-bold text-dark font-monospace"><?= e($employee['SoCMND_CCCD']) ?></span>
                </div>
                <div class="col-md-3">
                    <label class="text-muted small d-block">Ngày cấp & Nơi cấp</label>
                    <span class="text-dark"><?= format_date($employee['NgayCap'] ?? '') ?> &bull; <?= e($employee['NoiCap'] ?: 'Cục CS QLHC về TTXH') ?></span>
                </div>
                <div class="col-md-3">
                    <label class="text-muted small d-block">Ngày sinh & Tuổi</label>
                    <span class="text-dark"><?= format_date($employee['NgaySinh']) ?> (<?= ($employee['Tuoi'] ?? '-') ?> tuổi)</span>
                </div>
                <div class="col-md-3">
                    <label class="text-muted small d-block">Giới tính</label>
                    <span class="badge bg-light text-dark border"><?= e($employee['GioiTinh']) ?></span>
                </div>
                <div class="col-md-3">
                    <label class="text-muted small d-block">Mã số thuế cá nhân</label>
                    <span class="font-monospace text-dark"><?= e($employee['MaSoThue'] ?: 'Chưa cập nhật') ?></span>
                </div>
                <div class="col-md-3">
                    <label class="text-muted small d-block">Quê quán</label>
                    <span class="text-dark"><?= e($employee['QueQuan'] ?: 'Chưa cập nhật') ?></span>
                </div>
                <div class="col-md-6">
                    <label class="text-muted small d-block">Địa chỉ thường trú / Nơi ở hiện tại</label>
                    <span class="text-dark"><?= e($employee['DiaChiHienTai'] ?: 'Chưa cập nhật') ?></span>
                </div>
            </div>

            <h5 class="fw-bold text-dark border-bottom pb-2 mb-3">Trình Độ Học Vấn & Chuyên Môn</h5>
            <div class="row g-3 mb-4">
                <div class="col-md-4">
                    <label class="text-muted small d-block">Trình độ cao nhất</label>
                    <span class="fw-bold text-primary"><?= e($employee['TrinhDoHocVan'] ?: 'Đại học') ?></span>
                </div>
                <div class="col-md-4">
                    <label class="text-muted small d-block">Chuyên ngành đào tạo</label>
                    <span class="text-dark"><?= e($employee['ChuyenNganh'] ?: 'Chưa cập nhật') ?></span>
                </div>
                <div class="col-md-4">
                    <label class="text-muted small d-block">Cơ sở đào tạo</label>
                    <span class="text-dark"><?= e($employee['TruongDaoTao'] ?: 'Chưa cập nhật') ?></span>
                </div>
            </div>

            <h5 class="fw-bold text-dark border-bottom pb-2 mb-3">Tài Khoản Chi Trả Lương & Giảm Trừ Gia Cảnh</h5>
            <div class="row g-3">
                <div class="col-md-4">
                    <label class="text-muted small d-block">Số tài khoản ngân hàng</label>
                    <span class="fw-bold text-dark font-monospace"><?= e($employee['SoTaiKhoanNH'] ?: 'Chưa có') ?></span>
                </div>
                <div class="col-md-4">
                    <label class="text-muted small d-block">Ngân hàng thụ hưởng</label>
                    <span class="text-dark"><?= e($employee['NganHang'] ?: 'Vietcombank / Vietinbank') ?></span>
                </div>
                <div class="col-md-4">
                    <label class="text-muted small d-block">Số người phụ thuộc giảm trừ gia cảnh</label>
                    <span class="badge bg-info-subtle text-info border border-info-subtle px-2 py-1"><?= (int)($employee['SoNguoiPhuThuoc'] ?? 0) ?> người</span>
                </div>
            </div>
        </div>

        <!-- ============================================================== -->
        <!-- TAB 2: HỢP ĐỒNG LAO ĐỘNG & BỘ LUẬT LAO ĐỘNG -->
        <!-- ============================================================== -->
        <div class="tab-pane fade <?= ($activeTab === 'hop-dong') ? 'show active' : '' ?>" id="tab-hop-dong" role="tabpanel">
            <div class="d-flex justify-content-between align-items-center mb-3">
                <div>
                    <h5 class="fw-bold text-dark mb-1">Lịch Sử Hợp Đồng Lao Động</h5>
                    <p class="text-muted small mb-0">
                        Tuân thủ Điều 20 Bộ luật Lao động 2019: Đã ký <strong><?= $fixedTermCount ?></strong> / 2 lần HĐ xác định thời hạn.
                        <?php if (!$canSignFixedTerm): ?>
                            <span class="text-danger fw-bold ms-1">
                                <i class="bi bi-exclamation-triangle-fill"></i> Đã đủ 2 lần: Lần tiếp theo BẮT BUỘC là Không xác định thời hạn!
                            </span>
                        <?php endif; ?>
                    </p>
                </div>

                <?php if (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI'])): ?>
                <button class="btn btn-primary btn-sm fw-semibold" data-bs-toggle="modal" data-bs-target="#modalCreateContract" style="background: #0033A0;">
                    <i class="bi bi-plus-circle me-1"></i> Ký Hợp Đồng Mới
                </button>
                <?php endif; ?>
            </div>

            <div class="table-responsive">
                <table class="table table-hover align-middle border">
                    <thead class="table-light text-muted small text-uppercase">
                        <tr>
                            <th>Số HĐ</th>
                            <th>Loại Hợp Đồng</th>
                            <th>Ngày Ký</th>
                            <th>Thời Hạn (Từ - Đến)</th>
                            <th>Lương Cơ Bản</th>
                            <th class="text-center">Cảnh Báo Hết Hạn</th>
                            <th class="text-center">Trạng Thái</th>
                            <th class="text-center" style="width: 120px;">Thao Tác</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php if (empty($contracts)): ?>
                            <tr><td colspan="8" class="text-center py-4 text-muted">Chưa ghi nhận hợp đồng lao động nào.</td></tr>
                        <?php else: foreach ($contracts as $c): 
                            $cb = $c['canh_bao'];
                        ?>
                            <tr>
                                <td class="font-monospace fw-bold text-primary"><?= e($c['so_hd']) ?></td>
                                <td>
                                    <span class="fw-semibold text-dark"><?= e($c['loai_hd']) ?></span>
                                    <small class="text-muted d-block"><?= e($c['chuc_danh'] ?? '') ?></small>
                                </td>
                                <td><?= format_date($c['ngay_ky']) ?></td>
                                <td>
                                    <?= format_date($c['tu_ngay']) ?> 
                                    <span class="text-muted">&rarr;</span> 
                                    <?= !empty($c['den_ngay']) ? format_date($c['den_ngay']) : '<span class="text-success fw-bold">Vô thời hạn</span>' ?>
                                </td>
                                <td class="fw-semibold text-dark">
                                    <?= format_money($c['luong_can_ban']) ?>
                                </td>
                                <td class="text-center">
                                    <span class="badge bg-<?= $cb['badge'] ?> px-2 py-1">
                                        <?= $cb['message'] ?>
                                    </span>
                                </td>
                                <td class="text-center">
                                    <?php if ($c['trang_thai'] === 'HIEU_LUC'): ?>
                                        <span class="badge bg-success">Hiệu lực</span>
                                    <?php else: ?>
                                        <span class="badge bg-secondary"><?= e($c['trang_thai']) ?></span>
                                    <?php endif; ?>
                                </td>
                                <td class="text-center">
                                    <a href="<?= url('employees/print_contract/' . $c['id']) ?>" target="_blank" class="btn btn-outline-primary btn-sm py-0 px-2" title="In mẫu HĐ A4 chuẩn Vinamilk">
                                        <i class="bi bi-printer"></i> In HĐ
                                    </a>
                                </td>
                            </tr>
                        <?php endforeach; endif; ?>
                    </tbody>
                </table>
            </div>
        </div>

        <!-- ============================================================== -->
        <!-- TAB 3: QUÁ TRÌNH CÔNG TÁC (TIMELINE) -->
        <!-- ============================================================== -->
        <div class="tab-pane fade <?= ($activeTab === 'cong-tac') ? 'show active' : '' ?>" id="tab-cong-tac" role="tabpanel">
            <h5 class="fw-bold text-dark border-bottom pb-2 mb-3">Dòng Thời Gian Quá Trình Công Tác & Luân Chuyển</h5>
            <?php if (empty($careerHistory)): ?>
                <div class="text-center py-4 text-muted">Chưa ghi nhận biến động công tác nào.</div>
            <?php else: ?>
                <div class="timeline position-relative ps-4" style="border-left: 2px solid #E2E8F0; margin-left: 10px;">
                    <?php foreach ($careerHistory as $sk): ?>
                        <div class="mb-4 position-relative">
                            <!-- Timeline dot -->
                            <div class="position-absolute rounded-circle bg-primary" 
                                 style="width: 12px; height: 12px; left: -23px; top: 5px; border: 2px solid #fff;"></div>
                            
                            <div class="d-flex align-items-center gap-2 mb-1">
                                <span class="badge bg-primary-subtle text-primary border"><?= e($sk['loai_su_kien']) ?></span>
                                <span class="text-muted small"><i class="bi bi-calendar3 me-1"></i><?= format_date($sk['ngay_hieu_luc']) ?></span>
                                <?php if (!empty($sk['so_quyet_dinh'])): ?>
                                    <span class="badge bg-light text-dark border">QĐ: <?= e($sk['so_quyet_dinh']) ?></span>
                                <?php endif; ?>
                            </div>
                            <div class="fw-semibold text-dark"><?= e($sk['ly_do_bien_dong'] ?: 'Biến động nhân sự') ?></div>
                            <small class="text-muted d-block">
                                <?php if (!empty($sk['TenDonViMoi'])): ?>Đơn vị: <strong><?= e($sk['TenDonViMoi']) ?></strong> &bull; <?php endif; ?>
                                <?php if (!empty($sk['TenChucVuMoi'])): ?>Chức vụ: <strong><?= e($sk['TenChucVuMoi']) ?></strong> &bull; <?php endif; ?>
                                Ghi nhận bởi: <?= e($sk['nguoi_ghi_nhan'] ?? 'HR') ?>
                            </small>
                        </div>
                    <?php endforeach; ?>
                </div>
            <?php endif; ?>
        </div>

        <!-- ============================================================== -->
        <!-- TAB 4: LƯƠNG & PHỤ CẤP -->
        <!-- ============================================================== -->
        <div class="tab-pane fade <?= ($activeTab === 'luong') ? 'show active' : '' ?>" id="tab-luong" role="tabpanel">
            <h5 class="fw-bold text-dark border-bottom pb-2 mb-3">Mức Lương Hiện Hưởng & Thông Số Lương</h5>
            <div class="row g-3 mb-4">
                <div class="col-md-3">
                    <div class="p-3 bg-light rounded border text-center">
                        <small class="text-muted d-block mb-1">Lương Đóng Bảo Hiểm</small>
                        <h4 class="fw-bold text-primary mb-0"><?= is_numeric($employee['LuongBaoHiem']) ? format_money($employee['LuongBaoHiem']) : e($employee['LuongBaoHiem']) ?></h4>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="p-3 bg-light rounded border text-center">
                        <small class="text-muted d-block mb-1">Hệ Số Lương Hiện Tại</small>
                        <h4 class="fw-bold text-dark mb-0"><?= number_format((float)($employee['HeSoLuongHienTai'] ?? 1.0), 2) ?></h4>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="p-3 bg-light rounded border text-center">
                        <small class="text-muted d-block mb-1">Bậc Lương / Vùng Lương</small>
                        <h4 class="fw-bold text-dark mb-0">Bậc <?= (int)($employee['BacLuongHienTai'] ?? 1) ?> / Vùng <?= (int)($employee['VungLuong'] ?? 1) ?></h4>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="p-3 bg-light rounded border text-center">
                        <small class="text-muted d-block mb-1">Phụ Cấp Chức Vụ</small>
                        <h4 class="fw-bold text-dark mb-0"><?= format_money($employee['PhuCapCV'] ?? 0) ?></h4>
                    </div>
                </div>
            </div>

            <h5 class="fw-bold text-dark border-bottom pb-2 mb-3">Lịch Sử Điều Chỉnh Lương</h5>
            <div class="table-responsive">
                <table class="table table-sm table-hover align-middle border">
                    <thead class="table-light text-muted small">
                        <tr>
                            <th>Ngày Hiệu Lực</th>
                            <th>Loại Biến Động</th>
                            <th>Mức Lương Cũ</th>
                            <th>Mức Lương Mới</th>
                            <th>Số Quyết Định</th>
                            <th>Lý Do Điều Chỉnh</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php if (empty($salaryHistory)): ?>
                            <tr><td colspan="6" class="text-center py-3 text-muted">Chưa ghi nhận lịch sử điều chỉnh lương.</td></tr>
                        <?php else: foreach ($salaryHistory as $sh): ?>
                            <tr>
                                <td><?= format_date($sh['ngay_hieu_luc']) ?></td>
                                <td><span class="badge bg-light text-dark border"><?= e($sh['loai_su_kien']) ?></span></td>
                                <td class="text-muted"><?= is_numeric($sh['luong_cu']) ? format_money($sh['luong_cu']) : '****** ₫' ?></td>
                                <td class="fw-bold text-success"><?= is_numeric($sh['luong_moi']) ? format_money($sh['luong_moi']) : '****** ₫' ?></td>
                                <td><?= e($sh['so_quyet_dinh'] ?: '-') ?></td>
                                <td><?= e($sh['ly_do_bien_dong'] ?: '') ?></td>
                            </tr>
                        <?php endforeach; endif; ?>
                    </tbody>
                </table>
            </div>
        </div>

        <!-- ============================================================== -->
        <!-- TAB 5: ĐÀO TẠO & CHỨNG CHỈ -->
        <!-- ============================================================== -->
        <!-- ============================================================== -->
        <!-- TAB 5: ĐÀO TẠO & CHỨNG CHỈ (ATTP, ATLĐ, THIẾT BỊ) -->
        <!-- ============================================================== -->
        <div class="tab-pane fade <?= ($activeTab === 'dao-tao') ? 'show active' : '' ?>" id="tab-dao-tao" role="tabpanel">
            <?php 
            $hasExpiredAttp = false;
            $hasWarningCert = false;
            foreach ($certificates as $c) {
                if ((($c['loai_chung_chi'] === 'THE_XANH_ATTP') || !empty($c['la_the_xanh_attp'])) && (!empty($c['da_het_han']) || (isset($c['so_ngay_con_lai']) && $c['so_ngay_con_lai'] < 0))) {
                    $hasExpiredAttp = true;
                }
                if (isset($c['so_ngay_con_lai']) && $c['so_ngay_con_lai'] >= 0 && $c['so_ngay_con_lai'] <= 60) {
                    $hasWarningCert = true;
                }
            }
            ?>

            <?php if ($hasExpiredAttp): ?>
                <div class="alert alert-danger border border-danger d-flex align-items-center mb-3 shadow-sm py-2 px-3">
                    <i class="bi bi-shield-x fs-2 text-danger me-3"></i>
                    <div>
                        <strong class="d-block text-danger fs-6"><i class="bi bi-slash-circle me-1"></i> CHẶN XẾP CA DÂY CHUYỀN SẢN XUẤT!</strong>
                        <span class="small text-dark">
                            Thẻ xanh An toàn Thực phẩm (ATTP) của nhân sự này đã <strong>hết hạn hiệu lực</strong>. 
                            Theo quy định quản lý chất lượng <strong>ISO 22000, HACCP & VSATTP Vinamilk</strong>, hệ thống tự động <strong>CHẶN xếp vào các ca sản xuất / phân xưởng chế biến sữa</strong> (Ca 1, Ca 2, Ca 3, Ca gãy). Vui lòng bố trí khám sức khỏe định kỳ để gia hạn thẻ!
                        </span>
                    </div>
                </div>
            <?php elseif ($hasWarningCert): ?>
                <div class="alert alert-warning border border-warning d-flex align-items-center mb-3 py-2 px-3 shadow-sm">
                    <i class="bi bi-exclamation-triangle-fill fs-3 text-warning me-3"></i>
                    <div>
                        <strong class="text-dark d-block"><i class="bi bi-clock-history me-1"></i> CẢNH BÁO HẾT HẠN TRƯỚC 60 NGÀY</strong>
                        <span class="small text-muted">Có chứng chỉ / Thẻ xanh ATTP sẽ hết hạn trong vòng 60 ngày tới. Bộ phận Nhân sự & HSE cần lập danh sách tái khám sức khỏe hoặc huấn luyện định kỳ.</span>
                    </div>
                </div>
            <?php endif; ?>

            <div class="d-flex justify-content-between align-items-center mb-3">
                <h5 class="fw-bold text-dark mb-0">Chứng Chỉ Nghề, Thẻ Xanh ATTP & ATLĐ</h5>
                <?php if (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI'])): ?>
                <button class="btn btn-outline-primary btn-sm fw-semibold" data-bs-toggle="modal" data-bs-target="#modalAddCertificate">
                    <i class="bi bi-plus-lg me-1"></i> Cấp / Cập Nhật Chứng Chỉ
                </button>
                <?php endif; ?>
            </div>

            <div class="table-responsive">
                <table class="table table-hover align-middle border">
                    <thead class="table-light text-muted small">
                        <tr>
                            <th>Loại Chứng Chỉ</th>
                            <th>Tên Chứng Chỉ</th>
                            <th>Số Hiệu</th>
                            <th>Cơ Sở Y Tế / Nơi Cấp</th>
                            <th>Khám Định Kỳ</th>
                            <th>Ngày Hết Hạn & Cảnh Báo</th>
                            <th class="text-center">Trạng Thái Xếp Ca</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php if (empty($certificates)): ?>
                            <tr><td colspan="7" class="text-center py-4 text-muted">Chưa có thông tin chứng chỉ nhân viên.</td></tr>
                        <?php else: foreach ($certificates as $cert): 
                            $isAttp = ($cert['loai_chung_chi'] === 'THE_XANH_ATTP' || !empty($cert['la_the_xanh_attp']));
                            $isExpired = (!empty($cert['da_het_han']) || (isset($cert['so_ngay_con_lai']) && $cert['so_ngay_con_lai'] < 0));
                            $isWarning60 = (!$isExpired && isset($cert['so_ngay_con_lai']) && $cert['so_ngay_con_lai'] <= 60);
                        ?>
                            <tr class="<?= ($isAttp && $isExpired) ? 'table-danger' : ($isWarning60 ? 'table-warning' : '') ?>">
                                <td>
                                    <?php if ($isAttp): ?>
                                        <span class="badge bg-success-subtle text-success border border-success-subtle"><i class="bi bi-check2-circle me-1"></i> Thẻ Xanh ATTP</span>
                                    <?php elseif ($cert['loai_chung_chi'] === 'ATLD_VE_SINH_LD'): ?>
                                        <span class="badge bg-warning-subtle text-dark border border-warning-subtle"><i class="bi bi-shield-check me-1"></i> ATLĐ Nhóm 1-6</span>
                                    <?php elseif ($cert['loai_chung_chi'] === 'CHUNG_CHI_THIET_BI'): ?>
                                        <span class="badge bg-primary-subtle text-primary border border-primary-subtle"><i class="bi bi-gear-wide-connected me-1"></i> Thiết Bị / Máy Móc</span>
                                    <?php else: ?>
                                        <span class="badge bg-light text-dark border"><?= e($cert['loai_chung_chi']) ?></span>
                                    <?php endif; ?>
                                </td>
                                <td class="fw-bold text-dark">
                                    <?= e($cert['ten_chung_chi']) ?>
                                    <?php if (!empty($cert['ket_qua_kham'])): ?>
                                        <div class="small text-muted fw-normal"><i class="bi bi-heart-pulse text-danger me-1"></i><?= e($cert['ket_qua_kham']) ?></div>
                                    <?php endif; ?>
                                </td>
                                <td class="font-monospace small"><?= e($cert['so_hieu_chung_chi'] ?: '-') ?></td>
                                <td>
                                    <?= e($cert['co_so_y_te'] ?: ($cert['noi_cap'] ?: 'Vinamilk')) ?>
                                </td>
                                <td><?= format_date($cert['ngay_kham_dinh_ky'] ?: $cert['ngay_cap']) ?></td>
                                <td>
                                    <span class="fw-semibold"><?= format_date($cert['ngay_het_han']) ?></span>
                                    <?php if ($isExpired): ?>
                                        <div class="badge bg-danger text-white mt-1 d-block"><i class="bi bi-x-circle me-1"></i> Quá hạn <?= abs($cert['so_ngay_con_lai']) ?> ngày</div>
                                    <?php elseif ($isWarning60): ?>
                                        <div class="badge bg-warning text-dark mt-1 d-block"><i class="bi bi-exclamation-triangle me-1"></i> Còn <?= $cert['so_ngay_con_lai'] ?> ngày (≤ 60d)</div>
                                    <?php else: ?>
                                        <div class="small text-success mt-1"><i class="bi bi-check-circle me-1"></i> Còn <?= $cert['so_ngay_con_lai'] ?> ngày</div>
                                    <?php endif; ?>
                                </td>
                                <td class="text-center">
                                    <?php if ($isAttp && $isExpired): ?>
                                        <span class="badge bg-danger text-white px-2 py-1"><i class="bi bi-slash-circle me-1"></i> CHẶN XẾP CA</span>
                                    <?php elseif ($isAttp): ?>
                                        <span class="badge bg-success text-white px-2 py-1"><i class="bi bi-check-lg me-1"></i> ĐỦ ĐK DÂY CHUYỀN</span>
                                    <?php else: ?>
                                        <span class="badge bg-light text-secondary border"><?= e($cert['trang_thai']) ?></span>
                                    <?php endif; ?>
                                </td>
                            </tr>
                        <?php endforeach; endif; ?>
                    </tbody>
                </table>
            </div>
        </div>

        <!-- ============================================================== -->
        <!-- TAB 6: KHEN THƯỞNG & KỶ LUẬT (KT-KL) -->
        <!-- ============================================================== -->
        <div class="tab-pane fade <?= ($activeTab === 'kt-kl') ? 'show active' : '' ?>" id="tab-kt-kl" role="tabpanel">
            <div class="d-flex justify-content-between align-items-center mb-3">
                <h5 class="fw-bold text-dark mb-0">Hồ Sơ Khen Thưởng & Kỷ Luật</h5>
                <?php if (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI'])): ?>
                <button class="btn btn-outline-primary btn-sm fw-semibold" data-bs-toggle="modal" data-bs-target="#modalAddKTKL">
                    <i class="bi bi-plus-lg me-1"></i> Thêm Quyết Định KT-KL
                </button>
                <?php endif; ?>
            </div>

            <div class="table-responsive">
                <table class="table table-hover align-middle border">
                    <thead class="table-light text-muted small">
                        <tr>
                            <th>Phân Loại</th>
                            <th>Hình Thức</th>
                            <th>Số Quyết Định</th>
                            <th>Ngày Quyết Định</th>
                            <th>Lý Do / Thành Tích</th>
                            <th class="text-end">Tiền Thưởng / Phạt</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php if (empty($rewardsDisciplines)): ?>
                            <tr><td colspan="6" class="text-center py-4 text-muted">Chưa ghi nhận quyết định khen thưởng hoặc kỷ luật.</td></tr>
                        <?php else: foreach ($rewardsDisciplines as $kt): ?>
                            <tr>
                                <td>
                                    <?php if ($kt['loai'] === 'KHEN_THUONG'): ?>
                                        <span class="badge bg-success-subtle text-success border border-success-subtle"><i class="bi bi-award-fill me-1"></i>Khen Thưởng</span>
                                    <?php else: ?>
                                        <span class="badge bg-danger-subtle text-danger border border-danger-subtle"><i class="bi bi-slash-circle me-1"></i>Kỷ Luật</span>
                                    <?php endif; ?>
                                </td>
                                <td class="fw-bold text-dark"><?= e($kt['hinh_thuc']) ?></td>
                                <td class="font-monospace"><?= e($kt['so_quyet_dinh'] ?: '-') ?></td>
                                <td><?= format_date($kt['ngay_quyet_dinh']) ?></td>
                                <td><?= e($kt['ly_do']) ?></td>
                                <td class="text-end fw-semibold text-<?= ($kt['loai'] === 'KHEN_THUONG') ? 'success' : 'danger' ?>">
                                    <?= format_money($kt['so_tien']) ?>
                                </td>
                            </tr>
                        <?php endforeach; endif; ?>
                    </tbody>
                </table>
            </div>
        </div>

        <!-- ============================================================== -->
        <!-- TAB 7: TÀI SẢN CẤP PHÁT & TRANG BỊ PPE THEO VỊ TRÍ -->
        <!-- ============================================================== -->
        <div class="tab-pane fade <?= ($activeTab === 'tai-san') ? 'show active' : '' ?>" id="tab-tai-san" role="tabpanel">
            <!-- Box Gợi ý Định mức PPE theo vị trí công tác -->
            <?php if (!empty($ppeSuggestions['standards'])): ?>
            <div class="card bg-light border border-primary-subtle mb-4">
                <div class="card-body p-3">
                    <div class="d-flex justify-content-between align-items-center mb-2">
                        <span class="fw-bold text-primary small">
                            <i class="bi bi-shield-check me-1"></i> TIÊU CHUẨN ĐỊNH MỨC THEO VỊ TRÍ: 
                            <span class="badge bg-primary text-white ms-1"><?= e($ppeSuggestions['group']) ?></span>
                        </span>
                        <span class="small text-muted">Dựa trên Chức vụ: <strong><?= e($employee['TenCV'] ?? $employee['ChucVu']) ?></strong></span>
                    </div>
                    <div class="d-flex flex-wrap gap-2">
                        <?php foreach ($ppeSuggestions['standards'] as $std): ?>
                            <span class="badge bg-white text-dark border p-2 small shadow-sm d-flex align-items-center">
                                <i class="bi bi-box-seam text-primary me-2"></i>
                                <span><?= e($std['ten_trang_bi']) ?> (<?= $std['dinh_muc_so_luong'] ?> <?= e($std['don_vi_tinh']) ?> - <?= number_format($std['gia_tri_dinh_muc'], 0, ',', '.') ?>đ)</span>
                                <?php if ($std['bat_buoc']): ?>
                                    <span class="badge bg-danger-subtle text-danger ms-2">Bắt buộc</span>
                                <?php endif; ?>
                            </span>
                        <?php endforeach; ?>
                    </div>
                </div>
            </div>
            <?php endif; ?>

            <div class="d-flex justify-content-between align-items-center mb-3">
                <h5 class="fw-bold text-dark mb-0">Tài Sản & Trang Thiết Bị PPE Cấp Phát</h5>
                <?php if (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI'])): ?>
                <div>
                    <a href="<?= url('assets/standards') ?>" class="btn btn-outline-secondary btn-sm me-2">
                        <i class="bi bi-list-check me-1"></i> Danh Mục Chuẩn PPE
                    </a>
                    <button class="btn btn-primary btn-sm fw-semibold" data-bs-toggle="modal" data-bs-target="#modalAddAsset" style="background: #0033A0;">
                        <i class="bi bi-plus-lg me-1"></i> Cấp Phát Mới (Có Ký Nhận)
                    </button>
                </div>
                <?php endif; ?>
            </div>

            <div class="table-responsive">
                <table class="table table-hover align-middle border">
                    <thead class="table-light text-muted small">
                        <tr>
                            <th>Loại Tài Sản / PPE</th>
                            <th>Tên Trang Bị</th>
                            <th>Mã Serial / Biên Bản</th>
                            <th>Số Lượng & Giá Trị</th>
                            <th>Ngày Cấp & Tình Trạng</th>
                            <th class="text-center">Ký Nhận Bàn Giao</th>
                            <th class="text-center">Trạng Thái & Khấu Trừ</th>
                            <th class="text-end" style="width: 140px;">Thao Tác</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php if (empty($assets)): ?>
                            <tr><td colspan="8" class="text-center py-4 text-muted">Chưa ghi nhận tài sản nào được cấp phát cho nhân sự này.</td></tr>
                        <?php else: foreach ($assets as $ts): 
                            $isLostOrDamaged = in_array($ts['trang_thai'], ['BAO_MAT', 'BAO_HONG']);
                            $penalty = (float)($ts['gia_tri_khau_tru'] ?? 0);
                        ?>
                            <tr class="<?= $isLostOrDamaged ? 'table-danger' : '' ?>">
                                <td>
                                    <?php if ($ts['loai_tai_san'] === 'BHLD_PPE'): ?>
                                        <span class="badge bg-warning-subtle text-dark border border-warning-subtle"><i class="bi bi-person-badge me-1"></i> PPE / Bảo Hộ</span>
                                    <?php elseif ($ts['loai_tai_san'] === 'MAY_TINH_LAPTOP'): ?>
                                        <span class="badge bg-primary-subtle text-primary border border-primary-subtle"><i class="bi bi-laptop me-1"></i> Laptop IT</span>
                                    <?php elseif ($ts['loai_tai_san'] === 'THE_TU_RA_VAO'): ?>
                                        <span class="badge bg-info-subtle text-info border border-info-subtle"><i class="bi bi-credit-card-2-front me-1"></i> Thẻ Từ RFID</span>
                                    <?php else: ?>
                                        <span class="badge bg-light text-dark border"><?= e($ts['loai_tai_san']) ?></span>
                                    <?php endif; ?>
                                </td>
                                <td class="fw-bold text-dark">
                                    <?= e($ts['ten_tai_san']) ?>
                                    <?php if (!empty($ts['ghi_chu'])): ?>
                                        <div class="small text-muted fw-normal"><?= e($ts['ghi_chu']) ?></div>
                                    <?php endif; ?>
                                </td>
                                <td>
                                    <div class="font-monospace small text-primary"><?= e($ts['ma_dinh_danh_serial'] ?: '-') ?></div>
                                    <?php if (!empty($ts['ma_bien_ban'])): ?>
                                        <small class="badge bg-light text-secondary border font-monospace"><?= e($ts['ma_bien_ban']) ?></small>
                                    <?php endif; ?>
                                </td>
                                <td>
                                    <span class="fw-semibold"><?= $ts['so_luong'] ?> <?= e($ts['don_vi_tinh']) ?></span>
                                    <?php if (!empty($ts['gia_tri_tai_san']) && (float)$ts['gia_tri_tai_san'] > 0): ?>
                                        <div class="small text-muted"><?= number_format((float)$ts['gia_tri_tai_san'], 0, ',', '.') ?>đ</div>
                                    <?php endif; ?>
                                </td>
                                <td>
                                    <div><?= format_date($ts['ngay_cap_phat']) ?></div>
                                    <small class="text-muted"><?= e($ts['tinh_trang_khi_cap'] ?: 'Mới 100%') ?></small>
                                </td>
                                <td class="text-center">
                                    <?php if (!empty($ts['da_ky_nhan_cap'])): ?>
                                        <span class="badge bg-success-subtle text-success border border-success-subtle">
                                            <i class="bi bi-check2-circle me-1"></i> ĐÃ KÝ NHẬN
                                        </span>
                                        <?php if (!empty($ts['ngay_ky_nhan_cap'])): ?>
                                            <div class="small text-muted" style="font-size: 10px;"><?= date('d/m/Y H:i', strtotime($ts['ngay_ky_nhan_cap'])) ?></div>
                                        <?php endif; ?>
                                    <?php else: ?>
                                        <form action="<?= url('employees/sign_asset') ?>" method="POST" class="d-inline">
                                            <?= csrf_field() ?>
                                            <input type="hidden" name="ma_nv" value="<?= e($employee['MaNV']) ?>">
                                            <input type="hidden" name="asset_id" value="<?= $ts['id'] ?>">
                                            <button type="submit" class="btn btn-outline-warning btn-xs py-0 px-2 small">
                                                <i class="bi bi-pen me-1"></i> Ký Nhận
                                            </button>
                                        </form>
                                    <?php endif; ?>
                                </td>
                                <td class="text-center">
                                    <?php if ($ts['trang_thai'] === 'DA_THU_HOI'): ?>
                                        <span class="badge bg-secondary text-white">Đã Thu Hồi</span>
                                    <?php elseif ($ts['trang_thai'] === 'BAO_MAT'): ?>
                                        <span class="badge bg-danger text-white">BÁO MẤT</span>
                                        <div class="small text-danger fw-bold mt-1">Khấu trừ: <?= number_format($penalty, 0, ',', '.') ?>đ</div>
                                    <?php elseif ($ts['trang_thai'] === 'BAO_HONG'): ?>
                                        <span class="badge bg-warning text-dark">BÁO HỎNG</span>
                                        <div class="small text-danger fw-bold mt-1">Khấu trừ: <?= number_format($penalty, 0, ',', '.') ?>đ</div>
                                    <?php else: ?>
                                        <span class="badge bg-success-subtle text-success border border-success-subtle">Đang Sử Dụng</span>
                                    <?php endif; ?>
                                </td>
                                <td class="text-end">
                                    <?php if ($ts['trang_thai'] === 'DANG_SU_DUNG' && Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI'])): ?>
                                        <button class="btn btn-outline-danger btn-sm py-0 px-2" 
                                                data-bs-toggle="modal" 
                                                data-bs-target="#modalRecoverAsset" 
                                                data-id="<?= $ts['id'] ?>"
                                                data-name="<?= e($ts['ten_tai_san']) ?>"
                                                data-value="<?= (float)($ts['gia_tri_tai_san'] ?: 500000) ?>">
                                            <i class="bi bi-arrow-return-left me-1"></i> Thu Hồi
                                        </button>
                                    <?php endif; ?>
                                </td>
                            </tr>
                        <?php endforeach; endif; ?>
                    </tbody>
                </table>
            </div>
        </div>

        <!-- ============================================================== -->
        <!-- TAB 8: TÀI LIỆU SCAN (LƯU NGOÀI PUBLIC/) -->
        <!-- ============================================================== -->
        <div class="tab-pane fade <?= ($activeTab === 'tai-lieu') ? 'show active' : '' ?>" id="tab-tai-lieu" role="tabpanel">
            <div class="d-flex justify-content-between align-items-center mb-3">
                <div>
                    <h5 class="fw-bold text-dark mb-0">Hồ Sơ & Tài Liệu Scan Đính Kèm</h5>
                    <small class="text-muted">Được lưu trữ an toàn ngoài <code>public/</code>, tải về qua xác thực phân quyền.</small>
                </div>

                <?php if (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI'])): ?>
                <button class="btn btn-primary btn-sm fw-semibold" data-bs-toggle="modal" data-bs-target="#modalUploadDocument" style="background: #0033A0;">
                    <i class="bi bi-cloud-arrow-up-fill me-1"></i> Tải Lên Tài Liệu Scan
                </button>
                <?php endif; ?>
            </div>

            <div class="table-responsive">
                <table class="table table-hover align-middle border">
                    <thead class="table-light text-muted small">
                        <tr>
                            <th>Tên Tài Liệu</th>
                            <th>Phân Loại</th>
                            <th>Tên File Gốc</th>
                            <th>Dung Lượng</th>
                            <th>Ngày Tải Lên</th>
                            <th class="text-center" style="width: 140px;">Thao Tác</th>
                        </tr>
                    </thead>
                    <tbody>
                        <?php if (empty($documents)): ?>
                            <tr>
                                <td colspan="6" class="text-center py-5 text-muted">
                                    <i class="bi bi-file-earmark-pdf fs-2 d-block mb-2 text-secondary"></i>
                                    Chưa có tài liệu scan nào được tải lên cho nhân sự này.
                                </td>
                            </tr>
                        <?php else: foreach ($documents as $doc): ?>
                            <tr>
                                <td class="fw-bold text-dark"><?= e($doc['ten_tai_lieu']) ?></td>
                                <td><span class="badge bg-light text-dark border"><?= e($doc['loai_tai_lieu']) ?></span></td>
                                <td class="font-monospace small text-muted"><?= e($doc['file_name_original']) ?></td>
                                <td><?= number_format($doc['file_size'] / 1024, 1) ?> KB</td>
                                <td><?= format_date($doc['ngay_upload']) ?></td>
                                <td class="text-center">
                                    <a href="<?= url('employees/download_document?id=' . $doc['id'] . '&preview=1') ?>" target="_blank" class="btn btn-sm btn-outline-secondary py-0 px-2" title="Xem trước tài liệu">
                                        <i class="bi bi-eye"></i>
                                    </a>
                                    <a href="<?= url('employees/download_document?id=' . $doc['id']) ?>" class="btn btn-sm btn-outline-primary py-0 px-2 ms-1" title="Tải file an toàn">
                                        <i class="bi bi-download"></i> Tải về
                                    </a>
                                </td>
                            </tr>
                        <?php endforeach; endif; ?>
                    </tbody>
                </table>
            </div>
        </div>

        <!-- ============================================================== -->
        <!-- TAB 9: QUY TRÌNH THÔI VIỆC (OFFBOARDING CHECKLIST & EXIT SURVEY) -->
        <!-- ============================================================== -->
        <div class="tab-pane fade <?= ($activeTab === 'offboarding') ? 'show active' : '' ?>" id="tab-offboarding" role="tabpanel">
            <?php if (empty($offboarding)): ?>
                <div class="card border border-dashed text-center p-5">
                    <div class="mb-3">
                        <i class="bi bi-box-arrow-right fs-1 text-secondary"></i>
                    </div>
                    <h5 class="fw-bold text-dark mb-1">Chưa Có Hồ Sơ Thôi Việc</h5>
                    <p class="text-muted small mb-4">Nhân sự hiện đang trong trạng thái làm việc bình thường. Khi nhân viên có đơn xin nghỉ việc hoặc chấm dứt hợp đồng lao động, hãy khởi tạo quy trình Offboarding.</p>
                    
                    <?php if (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI'])): ?>
                    <div>
                        <button class="btn btn-danger btn-sm px-3 fw-semibold" data-bs-toggle="modal" data-bs-target="#modalInitOffboarding">
                            <i class="bi bi-plus-lg me-1"></i> Khởi Tạo Quy Trình Thôi Việc (Offboarding)
                        </button>
                    </div>
                    <?php endif; ?>
                </div>
            <?php else: 
                $percent = round(($offboarding['so_buoc_hoan_thanh'] / 6) * 100);
                $isDone = ($offboarding['trang_thai'] === 'HOAN_TAT');
            ?>
                <!-- Header Tiến Độ Offboarding -->
                <div class="card border shadow-sm mb-4">
                    <div class="card-body p-4">
                        <div class="d-flex flex-wrap justify-content-between align-items-center mb-3">
                            <div>
                                <span class="badge bg-danger-subtle text-danger border border-danger-subtle px-2 py-1 mb-2 font-monospace">
                                    <i class="bi bi-ticket-detailed me-1"></i> <?= e($offboarding['ma_ho_so']) ?>
                                </span>
                                <h5 class="fw-bold text-dark mb-1">Tiến Độ Quy Trình Thôi Việc: <?= e($employee['HoTen']) ?></h5>
                                <div class="small text-muted">
                                    <span>Ngày đề xuất: <strong><?= format_date($offboarding['ngay_de_xuat']) ?></strong></span>
                                    <span class="mx-2">•</span>
                                    <span>Ngày làm việc cuối: <strong class="text-danger"><?= format_date($offboarding['ngay_lam_viec_cuoi']) ?></strong></span>
                                    <span class="mx-2">•</span>
                                    <span>Lý do: <em><?= e($offboarding['ly_do_nghi']) ?></em></span>
                                </div>
                            </div>
                            <div class="text-end">
                                <span class="badge <?= $isDone ? 'bg-success' : 'bg-primary' ?> fs-6 px-3 py-2">
                                    <?= $isDone ? '<i class="bi bi-check-all me-1"></i> ĐÃ HOÀN TẤT NGHỈ VIỆC' : '<i class="bi bi-arrow-repeat me-1"></i> ĐANG XỬ LÝ (' . $offboarding['so_buoc_hoan_thanh'] . '/6 BƯỚC)' ?>
                                </span>
                            </div>
                        </div>

                        <div class="progress mb-2" style="height: 10px;">
                            <div class="progress-bar <?= $isDone ? 'bg-success' : 'bg-danger progress-bar-striped progress-bar-animated' ?>" 
                                 role="progressbar" style="width: <?= $percent ?>%;" aria-valuenow="<?= $percent ?>" aria-valuemin="0" aria-valuemax="100"></div>
                        </div>
                        <div class="d-flex justify-content-between text-muted small">
                            <span>Tiến độ hoàn tất: <strong><?= $percent ?>%</strong> (<?= $offboarding['so_buoc_hoan_thanh'] ?>/6 bước)</span>
                            <span>Trạng thái hồ sơ: <strong class="text-dark"><?= e($offboarding['trang_thai']) ?></strong></span>
                        </div>
                    </div>
                </div>

                <!-- 6 BƯỚC CHECKLIST THÔI VIỆC -->
                <div class="row g-3 mb-4">
                    <!-- BƯỚC 1: BÀN GIAO CÔNG VIỆC -->
                    <div class="col-md-6">
                        <div class="card h-100 border <?= $offboarding['buoc_1_ban_giao_cong_viec'] ? 'border-success-subtle bg-success-subtle-10' : 'border-light-subtle' ?>">
                            <div class="card-body p-3">
                                <div class="d-flex justify-content-between align-items-center mb-2">
                                    <span class="fw-bold text-dark">
                                        <i class="bi bi-briefcase me-1 text-primary"></i> 1. Bàn Giao Công Việc
                                    </span>
                                    <?php if ($offboarding['buoc_1_ban_giao_cong_viec']): ?>
                                        <span class="badge bg-success"><i class="bi bi-check-lg me-1"></i> Đã Bàn Giao</span>
                                    <?php else: ?>
                                        <span class="badge bg-secondary">Chưa Xong</span>
                                    <?php endif; ?>
                                </div>
                                <p class="small text-muted mb-2"><?= e($offboarding['noi_dung_ban_giao'] ?: 'Bàn giao tài liệu, đầu việc, mã nguồn, quyền quản trị cho người tiếp quản.') ?></p>
                                <?php if ($offboarding['buoc_1_ban_giao_cong_viec']): ?>
                                    <div class="small text-success">
                                        <i class="bi bi-person-check me-1"></i> Xác nhận: <strong><?= e($offboarding['nguoi_xac_nhan_bg']) ?></strong> (<?= date('d/m/Y', strtotime($offboarding['ngay_xac_nhan_bg'])) ?>)
                                    </div>
                                <?php elseif (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI', 'QUANLY_TRUCTIEP'])): ?>
                                    <form action="<?= url('offboarding/update_step') ?>" method="POST" class="mt-2">
                                        <?= csrf_field() ?>
                                        <input type="hidden" name="offboarding_id" value="<?= $offboarding['id'] ?>">
                                        <input type="hidden" name="step" value="1">
                                        <input type="text" name="content" class="form-control form-control-sm mb-2" placeholder="Ghi chú nội dung bàn giao..." required>
                                        <button type="submit" class="btn btn-outline-success btn-sm w-100 py-1">
                                            <i class="bi bi-check-circle me-1"></i> Xác Nhận Bàn Giao Xong
                                        </button>
                                    </form>
                                <?php endif; ?>
                            </div>
                        </div>
                    </div>

                    <!-- BƯỚC 2: THU HỒI TÀI SẢN & TỰ ĐỘNG KHẤU TRỪ LƯƠNG -->
                    <div class="col-md-6">
                        <div class="card h-100 border <?= $offboarding['buoc_2_thu_hoi_tai_san'] ? 'border-success-subtle bg-success-subtle-10' : 'border-light-subtle' ?>">
                            <div class="card-body p-3">
                                <div class="d-flex justify-content-between align-items-center mb-2">
                                    <span class="fw-bold text-dark">
                                        <i class="bi bi-laptop me-1 text-primary"></i> 2. Thu Hồi Tài Sản & PPE
                                    </span>
                                    <?php if ($offboarding['buoc_2_thu_hoi_tai_san']): ?>
                                        <span class="badge bg-success"><i class="bi bi-check-lg me-1"></i> Đã Thu Hồi / Khấu Trừ</span>
                                    <?php else: ?>
                                        <span class="badge bg-warning text-dark">Chờ Kiểm Tra</span>
                                    <?php endif; ?>
                                </div>
                                <div class="small text-muted mb-2">
                                    <?php if (!empty($offboarding['tong_tien_khau_tru_ts']) && (float)$offboarding['tong_tien_khau_tru_ts'] > 0): ?>
                                        <div class="alert alert-danger py-1 px-2 small mb-1">
                                            <i class="bi bi-exclamation-octagon-fill me-1"></i> <strong>Tự động khấu trừ lương:</strong> <?= number_format($offboarding['tong_tien_khau_tru_ts'], 0, ',', '.') ?> VNĐ (Nợ <?= $offboarding['so_tai_san_chua_tra'] ?> tài sản chưa trả).
                                        </div>
                                    <?php endif; ?>
                                    <span><?= e($offboarding['ghi_chu_tai_san'] ?: 'Thu hồi Laptop, Thẻ từ RFID, PPE phòng sạch. Nếu làm mất/chưa trả tự động trừ vào lương quyết toán.') ?></span>
                                </div>
                                <?php if ($offboarding['buoc_2_thu_hoi_tai_san']): ?>
                                    <div class="small text-success">
                                        <i class="bi bi-person-check me-1"></i> Xác nhận: <strong><?= e($offboarding['nguoi_xac_nhan_ts']) ?></strong> (<?= date('d/m/Y', strtotime($offboarding['ngay_xac_nhan_ts'])) ?>)
                                    </div>
                                <?php elseif (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI'])): ?>
                                    <form action="<?= url('offboarding/update_step') ?>" method="POST" class="mt-2">
                                        <?= csrf_field() ?>
                                        <input type="hidden" name="offboarding_id" value="<?= $offboarding['id'] ?>">
                                        <input type="hidden" name="step" value="2">
                                        <input type="text" name="note" class="form-control form-control-sm mb-2" placeholder="Ghi chú thu hồi / tài sản mất...">
                                        <button type="submit" class="btn btn-outline-primary btn-sm w-100 py-1">
                                            <i class="bi bi-arrow-repeat me-1"></i> Quét & Thu Hồi Tài Sản (Tự Khấu Trừ)
                                        </button>
                                    </form>
                                <?php endif; ?>
                            </div>
                        </div>
                    </div>

                    <!-- BƯỚC 3: KHÓA TÀI KHOẢN NGƯỜI DÙNG -->
                    <div class="col-md-6">
                        <div class="card h-100 border <?= $offboarding['buoc_3_khoa_tai_khoan'] ? 'border-success-subtle bg-success-subtle-10' : 'border-light-subtle' ?>">
                            <div class="card-body p-3">
                                <div class="d-flex justify-content-between align-items-center mb-2">
                                    <span class="fw-bold text-dark">
                                        <i class="bi bi-lock-fill me-1 text-primary"></i> 3. Khóa Tài Khoản Người Dùng
                                    </span>
                                    <?php if ($offboarding['buoc_3_khoa_tai_khoan']): ?>
                                        <span class="badge bg-danger"><i class="bi bi-lock-fill me-1"></i> ĐÃ KHÓA TÀI KHOẢN</span>
                                    <?php else: ?>
                                        <span class="badge bg-secondary">Chưa Khóa</span>
                                    <?php endif; ?>
                                </div>
                                <p class="small text-muted mb-2"><?= e($offboarding['ghi_chu_khoa_tk'] ?: 'Khóa tài khoản đăng nhập HRM, email doanh nghiệp, quyền truy cập dữ liệu.') ?></p>
                                <?php if ($offboarding['buoc_3_khoa_tai_khoan']): ?>
                                    <div class="small text-success">
                                        <i class="bi bi-person-check me-1"></i> Thực hiện: <strong><?= e($offboarding['nguoi_xac_nhan_tk']) ?></strong> (User: <?= e($offboarding['username_khoa']) ?>)
                                    </div>
                                <?php elseif (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI'])): ?>
                                    <form action="<?= url('offboarding/update_step') ?>" method="POST" class="mt-2">
                                        <?= csrf_field() ?>
                                        <input type="hidden" name="offboarding_id" value="<?= $offboarding['id'] ?>">
                                        <input type="hidden" name="step" value="3">
                                        <button type="submit" class="btn btn-outline-danger btn-sm w-100 py-1" onclick="return confirm('Bạn có chắc chắn muốn khóa tài khoản người dùng của nhân sự này ngay lập tức?');">
                                            <i class="bi bi-lock me-1"></i> Khóa Ngay Tài Khoản Hệ Thống
                                        </button>
                                    </form>
                                <?php endif; ?>
                            </div>
                        </div>
                    </div>

                    <!-- BƯỚC 4: CHỐT CÔNG & PHÉP TỒN -->
                    <div class="col-md-6">
                        <div class="card h-100 border <?= $offboarding['buoc_4_chot_cong'] ? 'border-success-subtle bg-success-subtle-10' : 'border-light-subtle' ?>">
                            <div class="card-body p-3">
                                <div class="d-flex justify-content-between align-items-center mb-2">
                                    <span class="fw-bold text-dark">
                                        <i class="bi bi-calendar2-check me-1 text-primary"></i> 4. Chốt Công & Phép Tồn
                                    </span>
                                    <?php if ($offboarding['buoc_4_chot_cong']): ?>
                                        <span class="badge bg-success"><i class="bi bi-check-lg me-1"></i> Đã Chốt Công</span>
                                    <?php else: ?>
                                        <span class="badge bg-secondary">Chờ Chốt</span>
                                    <?php endif; ?>
                                </div>
                                <div class="small text-muted mb-2">
                                    <span>Công thực tế: <strong><?= $offboarding['so_cong_chot'] ?> ngày</strong></span>
                                    <span class="mx-2">•</span>
                                    <span>Phép tồn thanh toán: <strong><?= $offboarding['so_phep_con_lai'] ?> ngày</strong></span>
                                    <?php if (!empty($offboarding['ghi_chu_chot_cong'])): ?>
                                        <div class="small text-muted mt-1"><?= e($offboarding['ghi_chu_chot_cong']) ?></div>
                                    <?php endif; ?>
                                </div>
                                <?php if ($offboarding['buoc_4_chot_cong']): ?>
                                    <div class="small text-success">
                                        <i class="bi bi-person-check me-1"></i> Xác nhận: <strong><?= e($offboarding['nguoi_xac_nhan_cong']) ?></strong>
                                    </div>
                                <?php elseif (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI', 'KETOAN_LUONG'])): ?>
                                    <form action="<?= url('offboarding/update_step') ?>" method="POST" class="mt-2">
                                        <?= csrf_field() ?>
                                        <input type="hidden" name="offboarding_id" value="<?= $offboarding['id'] ?>">
                                        <input type="hidden" name="step" value="4">
                                        <div class="row g-2 mb-2">
                                            <div class="col-6">
                                                <input type="number" step="0.5" name="work_days" class="form-control form-control-sm" placeholder="Số công thực tế" value="22" required>
                                            </div>
                                            <div class="col-6">
                                                <input type="number" step="0.5" name="remaining_leave" class="form-control form-control-sm" placeholder="Phép năm tồn" value="2" required>
                                            </div>
                                        </div>
                                        <button type="submit" class="btn btn-outline-primary btn-sm w-100 py-1">
                                            <i class="bi bi-check2-square me-1"></i> Xác Nhận Chốt Công
                                        </button>
                                    </form>
                                <?php endif; ?>
                            </div>
                        </div>
                    </div>

                    <!-- BƯỚC 5: QUYẾT TOÁN LƯƠNG & KHẤU TRỪ TÀI SẢN -->
                    <div class="col-md-6">
                        <div class="card h-100 border <?= $offboarding['buoc_5_quyet_toan_luong'] ? 'border-success-subtle bg-success-subtle-10' : 'border-light-subtle' ?>">
                            <div class="card-body p-3">
                                <div class="d-flex justify-content-between align-items-center mb-2">
                                    <span class="fw-bold text-dark">
                                        <i class="bi bi-cash-coin me-1 text-primary"></i> 5. Quyết Toán Lương
                                    </span>
                                    <?php if ($offboarding['buoc_5_quyet_toan_luong']): ?>
                                        <span class="badge bg-success"><i class="bi bi-check-lg me-1"></i> Đã Quyết Toán</span>
                                    <?php else: ?>
                                        <span class="badge bg-secondary">Chờ Duyệt</span>
                                    <?php endif; ?>
                                </div>
                                <div class="small text-muted mb-2">
                                    <div>Thực lĩnh quyết toán: <strong class="text-success"><?= number_format($offboarding['thuc_linh_quyet_toan'], 0, ',', '.') ?> VNĐ</strong></div>
                                    <div>Đã trừ tiền tài sản nợ: <strong class="text-danger">-<?= number_format($offboarding['khau_tru_tai_san_da_tru'], 0, ',', '.') ?> VNĐ</strong></div>
                                </div>
                                <?php if ($offboarding['buoc_5_quyet_toan_luong']): ?>
                                    <div class="small text-success">
                                        <i class="bi bi-person-check me-1"></i> Duyệt: <strong><?= e($offboarding['nguoi_xac_nhan_luong']) ?></strong> (<?= date('d/m/Y', strtotime($offboarding['ngay_xac_nhan_luong'])) ?>)
                                    </div>
                                <?php elseif (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'KETOAN_LUONG'])): ?>
                                    <form action="<?= url('offboarding/update_step') ?>" method="POST" class="mt-2">
                                        <?= csrf_field() ?>
                                        <input type="hidden" name="offboarding_id" value="<?= $offboarding['id'] ?>">
                                        <input type="hidden" name="step" value="5">
                                        <input type="number" name="settlement_amount" class="form-control form-control-sm mb-2" placeholder="Tổng tiền lương trước khấu trừ..." required>
                                        <button type="submit" class="btn btn-outline-success btn-sm w-100 py-1">
                                            <i class="bi bi-cash me-1"></i> Quyết Toán (Trừ Tiền Tài Sản Tự Động)
                                        </button>
                                    </form>
                                <?php endif; ?>
                            </div>
                        </div>
                    </div>

                    <!-- BƯỚC 6: CHỐT SỔ BHXH -->
                    <div class="col-md-6">
                        <div class="card h-100 border <?= $offboarding['buoc_6_chot_bhxh'] ? 'border-success-subtle bg-success-subtle-10' : 'border-light-subtle' ?>">
                            <div class="card-body p-3">
                                <div class="d-flex justify-content-between align-items-center mb-2">
                                    <span class="fw-bold text-dark">
                                        <i class="bi bi-journal-check me-1 text-primary"></i> 6. Chốt Sổ BHXH & Quyết Định
                                    </span>
                                    <?php if ($offboarding['buoc_6_chot_bhxh']): ?>
                                        <span class="badge bg-success"><i class="bi bi-check-lg me-1"></i> Đã Chốt Sổ BHXH</span>
                                    <?php else: ?>
                                        <span class="badge bg-secondary">Chờ Cơ Quan BHXH</span>
                                    <?php endif; ?>
                                </div>
                                <div class="small text-muted mb-2">
                                    <span>Mã số BHXH: <strong class="font-monospace"><?= e($offboarding['so_so_bhxh'] ?: 'Chưa cập nhật') ?></strong></span>
                                    <?php if (!empty($offboarding['ngay_tra_so_bhxh'])): ?>
                                        <div>Ngày trao sổ: <?= format_date($offboarding['ngay_tra_so_bhxh']) ?></div>
                                    <?php endif; ?>
                                </div>
                                <?php if ($offboarding['buoc_6_chot_bhxh']): ?>
                                    <div class="small text-success">
                                        <i class="bi bi-person-check me-1"></i> Xác nhận: <strong><?= e($offboarding['nguoi_xac_nhan_bhxh']) ?></strong>
                                    </div>
                                <?php elseif (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI'])): ?>
                                    <form action="<?= url('offboarding/update_step') ?>" method="POST" class="mt-2">
                                        <?= csrf_field() ?>
                                        <input type="hidden" name="offboarding_id" value="<?= $offboarding['id'] ?>">
                                        <input type="hidden" name="step" value="6">
                                        <input type="text" name="so_bhxh" class="form-control form-control-sm mb-2" placeholder="Số sổ BHXH..." required>
                                        <button type="submit" class="btn btn-outline-danger btn-sm w-100 py-1" onclick="return confirm('Xác nhận hoàn tất chốt sổ BHXH và đóng hồ sơ nhân viên sang trạng thái Nghỉ việc?');">
                                            <i class="bi bi-award me-1"></i> Chốt Sổ & Hoàn Tất Offboarding
                                        </button>
                                    </form>
                                <?php endif; ?>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- KHẢO SÁT LÝ DO NGHỈ VIỆC (EXIT INTERVIEW SURVEY - DỮ LIỆU ĐÀO TẠO CHO AI) -->
                <div class="card border border-primary-subtle shadow-sm mb-4">
                    <div class="card-header bg-light d-flex justify-content-between align-items-center py-3">
                        <div>
                            <h6 class="fw-bold text-dark mb-0">
                                <i class="bi bi-robot text-primary me-2"></i> Khảo Sát Lý Do Nghỉ Việc (Exit Interview Survey)
                            </h6>
                            <small class="text-muted">Dữ liệu nguồn trực tiếp huấn luyện mô hình Machine Learning phân tích nguy cơ thôi việc (AI Turnover Engine).</small>
                        </div>
                        <?php if ($survey): ?>
                            <span class="badge bg-success"><i class="bi bi-check-circle me-1"></i> Đã Thu Thập Dữ Liệu AI</span>
                        <?php else: ?>
                            <span class="badge bg-warning text-dark"><i class="bi bi-hourglass-split me-1"></i> Chưa Khảo Sát</span>
                        <?php endif; ?>
                    </div>
                    <div class="card-body p-4">
                        <?php if ($survey): ?>
                            <div class="row g-3 mb-3">
                                <div class="col-md-4">
                                    <label class="small text-muted d-block">Lý do chính nghỉ việc</label>
                                    <span class="badge bg-danger fs-6 mt-1"><?= e($survey['ly_do_chinh']) ?></span>
                                </div>
                                <div class="col-md-4">
                                    <label class="small text-muted d-block">Loại hình doanh nghiệp mới</label>
                                    <span class="fw-bold text-dark"><?= e($survey['cong_ty_moi_loai_hinh'] ?: 'Chưa tiết lộ') ?></span>
                                </div>
                                <div class="col-md-4">
                                    <label class="small text-muted d-block">Mức lương mới tăng</label>
                                    <span class="fw-bold text-success">+<?= (float)$survey['muc_luong_moi_tang_phan_tram'] ?>%</span>
                                </div>
                            </div>
                            <div class="row g-3 p-3 bg-light rounded border mb-3">
                                <div class="col-4 col-md-2 text-center">
                                    <small class="text-muted d-block">Lương thưởng</small>
                                    <span class="fw-bold fs-5 text-primary"><?= $survey['hai_long_luong_thuong'] ?>/5</span>
                                </div>
                                <div class="col-4 col-md-2 text-center">
                                    <small class="text-muted d-block">Quản lý trực tiếp</small>
                                    <span class="fw-bold fs-5 text-primary"><?= $survey['hai_long_quan_ly'] ?>/5</span>
                                </div>
                                <div class="col-4 col-md-2 text-center">
                                    <small class="text-muted d-block">Thăng tiến</small>
                                    <span class="fw-bold fs-5 text-primary"><?= $survey['hai_long_co_hoi_thang_tien'] ?>/5</span>
                                </div>
                                <div class="col-4 col-md-2 text-center">
                                    <small class="text-muted d-block">Áp lực OT</small>
                                    <span class="fw-bold fs-5 text-warning"><?= $survey['ap_luc_cong_viec_ot'] ?>/5</span>
                                </div>
                                <div class="col-4 col-md-2 text-center">
                                    <small class="text-muted d-block">Môi trường</small>
                                    <span class="fw-bold fs-5 text-primary"><?= $survey['hai_long_moi_truong_dong_nghiep'] ?>/5</span>
                                </div>
                                <div class="col-4 col-md-2 text-center">
                                    <small class="text-muted d-block">Phúc lợi</small>
                                    <span class="fw-bold fs-5 text-primary"><?= $survey['hai_long_chinh_sach_phuc_loi'] ?>/5</span>
                                </div>
                            </div>
                            <?php if (!empty($survey['dieu_gi_vnm_nen_cai_thien'])): ?>
                                <div class="p-3 bg-white border rounded">
                                    <strong class="small text-muted d-block mb-1"><i class="bi bi-chat-quote text-primary me-1"></i> Ý kiến đóng góp cải thiện:</strong>
                                    <em>"<?= e($survey['dieu_gi_vnm_nen_cai_thien']) ?>"</em>
                                </div>
                            <?php endif; ?>
                        <?php else: ?>
                            <form action="<?= url('offboarding/save_survey') ?>" method="POST">
                                <?= csrf_field() ?>
                                <input type="hidden" name="offboarding_id" value="<?= $offboarding['id'] ?>">
                                <input type="hidden" name="ma_nv" value="<?= e($employee['MaNV']) ?>">

                                <div class="row g-3 mb-3">
                                    <div class="col-md-6">
                                        <label class="form-label small fw-semibold">Lý do nghỉ việc chính <span class="text-danger">*</span></label>
                                        <select name="ly_do_chinh" class="form-select form-select-sm" required>
                                            <option value="LUONG_THAP">Mức lương và đãi ngộ thấp</option>
                                            <option value="QUAN_LY_TRUC_TIEP">Quan hệ không tốt với quản lý trực tiếp</option>
                                            <option value="KHONG_CO_CO_HOI_PHAT_TRIEN">Thiếu lộ trình thăng tiến và đào tạo</option>
                                            <option value="AP_LUC_OT_CAO">Áp lực công việc cao, làm thêm giờ (OT) nhiều</option>
                                            <option value="MOI_TRUONG_LAM_VIEC">Môi trường làm việc không phù hợp</option>
                                            <option value="CO_HOI_VIEC_LAM_TOT_HON" selected>Có cơ hội nghề nghiệp / công ty khác tốt hơn</option>
                                            <option value="LY_DO_CA_NHAN_GIA_DINH">Lý do cá nhân / Gia đình</option>
                                            <option value="CHUYEN_CHO_O">Thay đổi nơi cư trú / Khoảng cách xa</option>
                                            <option value="KHAC">Lý do khác</option>
                                        </select>
                                    </div>
                                    <div class="col-md-3">
                                        <label class="form-label small fw-semibold">Loại hình DN mới</label>
                                        <input type="text" name="cong_ty_moi_loai_hinh" class="form-control form-control-sm" placeholder="VD: Tập đoàn FDI, Doanh nghiệp nội...">
                                    </div>
                                    <div class="col-md-3">
                                        <label class="form-label small fw-semibold">Lương mới tăng (%)</label>
                                        <input type="number" step="0.5" name="muc_luong_moi_tang_phan_tram" class="form-control form-control-sm" placeholder="VD: 25%">
                                    </div>
                                </div>

                                <label class="form-label small fw-semibold mb-2">Đánh giá mức độ hài lòng tại Vinamilk (Thang điểm 1 - 5 sao)</label>
                                <div class="row g-2 mb-3">
                                    <div class="col-md-2">
                                        <label class="small text-muted">Lương thưởng</label>
                                        <select name="hai_long_luong_thuong" class="form-select form-select-sm">
                                            <option value="1">1 - Rất kém</option><option value="2">2 - Kém</option><option value="3" selected>3 - Tạm ổn</option><option value="4">4 - Tốt</option><option value="5">5 - Rất tốt</option>
                                        </select>
                                    </div>
                                    <div class="col-md-2">
                                        <label class="small text-muted">Quản lý</label>
                                        <select name="hai_long_quan_ly" class="form-select form-select-sm">
                                            <option value="1">1 - Rất kém</option><option value="2">2 - Kém</option><option value="3">3 - Tạm ổn</option><option value="4" selected>4 - Tốt</option><option value="5">5 - Rất tốt</option>
                                        </select>
                                    </div>
                                    <div class="col-md-2">
                                        <label class="small text-muted">Thăng tiến</label>
                                        <select name="hai_long_co_hoi_thang_tien" class="form-select form-select-sm">
                                            <option value="1">1 - Rất kém</option><option value="2">2 - Kém</option><option value="3" selected>3 - Tạm ổn</option><option value="4">4 - Tốt</option><option value="5">5 - Rất tốt</option>
                                        </select>
                                    </div>
                                    <div class="col-md-2">
                                        <label class="small text-muted">Áp lực OT</label>
                                        <select name="ap_luc_cong_viec_ot" class="form-select form-select-sm">
                                            <option value="1">1 - Rất ít OT</option><option value="2">2 - Bình thường</option><option value="3">3 - Khá mệt</option><option value="4" selected>4 - Rất căng thẳng</option><option value="5">5 - Quá tải</option>
                                        </select>
                                    </div>
                                    <div class="col-md-2">
                                        <label class="small text-muted">Môi trường</label>
                                        <select name="hai_long_moi_truong_dong_nghiep" class="form-select form-select-sm">
                                            <option value="1">1 - Rất kém</option><option value="2">2 - Kém</option><option value="3">3 - Tạm ổn</option><option value="4" selected>4 - Tốt</option><option value="5">5 - Rất tốt</option>
                                        </select>
                                    </div>
                                    <div class="col-md-2">
                                        <label class="small text-muted">Phúc lợi</label>
                                        <select name="hai_long_chinh_sach_phuc_loi" class="form-select form-select-sm">
                                            <option value="1">1 - Rất kém</option><option value="2">2 - Kém</option><option value="3">3 - Tạm ổn</option><option value="4" selected>4 - Tốt</option><option value="5">5 - Rất tốt</option>
                                        </select>
                                    </div>
                                </div>

                                <div class="mb-3">
                                    <label class="form-label small fw-semibold">Vinamilk nên cải thiện điều gì nhất?</label>
                                    <textarea name="dieu_gi_vnm_nen_cai_thien" class="form-control form-control-sm" rows="2" placeholder="Góp ý chân thành từ nhân viên thôi việc..."></textarea>
                                </div>

                                <div class="d-flex justify-content-between align-items-center">
                                    <div class="form-check">
                                        <input class="form-check-input" type="checkbox" name="san_sang_quay_lai_vnm" value="1" id="checkReturn" checked>
                                        <label class="form-check-label small" for="checkReturn">Sẵn sàng quay lại Vinamilk làm việc trong tương lai nếu có cơ hội phù hợp</label>
                                    </div>
                                    <button type="submit" class="btn btn-primary btn-sm fw-semibold" style="background: #0033A0;">
                                        <i class="bi bi-send-check me-1"></i> Lưu Kết Quả Khảo Sát AI
                                    </button>
                                </div>
                            </form>
                        <?php endif; ?>
                    </div>
                </div>
            <?php endif; ?>
        </div>
    </div>
</div>

<!-- ============================================================== -->
<!-- MODAL: LẬP HỢP ĐỒNG LAO ĐỘNG MỚI (CHẶN LẦN 3 XÁC ĐỊNH THỜI HẠN) -->
<!-- ============================================================== -->
<div class="modal fade" id="modalCreateContract" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog modal-lg">
        <form action="<?= url('employees/create_contract') ?>" method="POST" class="modal-content">
            <?= csrf_field() ?>
            <input type="hidden" name="ma_nv" value="<?= e($employee['MaNV']) ?>">

            <div class="modal-header bg-light">
                <h5 class="modal-title fw-bold text-dark"><i class="bi bi-file-earmark-plus text-primary me-2"></i>Lập Hợp Đồng Lao Động Mới</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
            </div>

            <div class="modal-body">
                <?php if (!$canSignFixedTerm): ?>
                <div class="alert alert-warning d-flex align-items-center mb-3">
                    <i class="bi bi-exclamation-triangle-fill fs-4 me-3 text-warning"></i>
                    <div>
                        <strong>CẢNH BÁO PHÁP LÝ (Điều 20 BLLĐ 2019):</strong><br>
                        Nhân sự này đã ký <strong><?= $fixedTermCount ?> lần</strong> hợp đồng xác định thời hạn. Lần thứ 3 bắt buộc phải ký <strong>Hợp đồng KHÔNG XÁC ĐỊNH THỜI HẠN</strong>.
                    </div>
                </div>
                <?php endif; ?>

                <div class="row g-3">
                    <div class="col-md-6">
                        <label class="form-label small fw-semibold">Loại hợp đồng <span class="text-danger">*</span></label>
                        <select name="loai_hd" class="form-select form-select-sm" required id="selectContractType">
                            <?php if ($canSignFixedTerm): ?>
                                <option value="THU_VIEC">Hợp đồng thử việc (Tối đa 60 ngày)</option>
                                <option value="XAC_DINH_THOI_HAN_12T">HĐ xác định thời hạn 12 tháng (Lần <?= $fixedTermCount + 1 ?>/2)</option>
                                <option value="XAC_DINH_THOI_HAN_24T">HĐ xác định thời hạn 24 tháng (Lần <?= $fixedTermCount + 1 ?>/2)</option>
                                <option value="XAC_DINH_THOI_HAN_36T">HĐ xác định thời hạn 36 tháng (Lần <?= $fixedTermCount + 1 ?>/2)</option>
                            <?php endif; ?>
                            <option value="KHONG_XAC_DINH_THOI_HAN" <?= (!$canSignFixedTerm) ? 'selected' : '' ?>>Hợp đồng KHÔNG XÁC ĐỊNH THỜI HẠN</option>
                        </select>
                    </div>

                    <div class="col-md-6">
                        <label class="form-label small fw-semibold">Số hợp đồng (Để trống sẽ sinh tự động)</label>
                        <input type="text" name="so_hd" class="form-control form-control-sm" placeholder="VD: 045/HĐLĐ-VNM/2026">
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Ngày ký hợp đồng</label>
                        <input type="date" name="ngay_ky" class="form-control form-control-sm" value="<?= date('Y-m-d') ?>" required>
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Hiệu lực từ ngày <span class="text-danger">*</span></label>
                        <input type="date" name="tu_ngay" class="form-control form-control-sm" value="<?= date('Y-m-d') ?>" required>
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Đến ngày (Bỏ trống nếu không thời hạn)</label>
                        <input type="date" name="den_ngay" class="form-control form-control-sm" id="inputContractEnd">
                    </div>

                    <div class="col-md-6">
                        <label class="form-label small fw-semibold">Lương căn bản (VNĐ) <span class="text-danger">*</span></label>
                        <input type="number" name="luong_can_ban" class="form-control form-control-sm" value="12000000" required>
                    </div>

                    <div class="col-md-6">
                        <label class="form-label small fw-semibold">Lương đóng BHXH (VNĐ)</label>
                        <input type="number" name="luong_dong_bh" class="form-control form-control-sm" value="12000000">
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Phụ cấp trách nhiệm</label>
                        <input type="number" name="phu_cap_trach_nhiem" class="form-control form-control-sm" value="0">
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Phụ cấp ăn ca</label>
                        <input type="number" name="phu_cap_an_ca" class="form-control form-control-sm" value="730000">
                    </div>

                    <div class="col-md-4">
                        <label class="form-label small fw-semibold">Tỷ lệ hưởng lương (%)</label>
                        <input type="number" name="ty_le_huong_luong" class="form-control form-control-sm" value="100" min="50" max="100">
                    </div>

                    <div class="col-md-6">
                        <label class="form-label small fw-semibold">Người đại diện công ty ký</label>
                        <input type="text" name="nguoi_dai_dien_ky" class="form-control form-control-sm" value="Bùi Thị Hương">
                    </div>

                    <div class="col-md-6">
                        <label class="form-label small fw-semibold">Chức vụ người ký</label>
                        <input type="text" name="chuc_vu_nguoi_ky" class="form-control form-control-sm" value="Giám đốc Điều hành Nhân sự">
                    </div>
                </div>
            </div>

            <div class="modal-footer bg-light">
                <button type="button" class="btn btn-light btn-sm" data-bs-dismiss="modal">Hủy bỏ</button>
                <button type="submit" class="btn btn-primary btn-sm fw-semibold" style="background: #0033A0;">Xác Nhận Ký Hợp Đồng</button>
            </div>
        </form>
    </div>
</div>

<!-- ============================================================== -->
<!-- MODAL: UPLOAD TÀI LIỆU SCAN (LƯU NGOÀI PUBLIC/) -->
<!-- ============================================================== -->
<div class="modal fade" id="modalUploadDocument" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
        <form action="<?= url('employees/upload_document') ?>" method="POST" enctype="multipart/form-data" class="modal-content">
            <?= csrf_field() ?>
            <input type="hidden" name="ma_nv" value="<?= e($employee['MaNV']) ?>">

            <div class="modal-header bg-light">
                <h5 class="modal-title fw-bold text-dark"><i class="bi bi-cloud-arrow-up text-primary me-2"></i>Tải Lên Tài Liệu Scan</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>

            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Tên mô tả tài liệu <span class="text-danger">*</span></label>
                    <input type="text" name="ten_tai_lieu" class="form-control form-control-sm" placeholder="VD: Bản scan CCCD 2 mặt, Bằng Đại học..." required>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Loại tài liệu</label>
                    <select name="loai_tai_lieu" class="form-select form-select-sm">
                        <option value="CCCD_CMND">Bản scan CCCD / Hộ chiếu</option>
                        <option value="SO_YEU_LY_LICH">Sơ yếu lý lịch</option>
                        <option value="BANG_CAP_CHUNG_CHI">Bằng cấp & Chứng chỉ</option>
                        <option value="HOP_DONG_SCAN">Hợp đồng lao động đã ký</option>
                        <option value="GIAY_KHAM_SUC_KHOE">Giấy khám sức khỏe</option>
                        <option value="KHAC">Tài liệu khác</option>
                    </select>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Chọn file tài liệu (PDF, JPG, PNG - Tối đa 5MB) <span class="text-danger">*</span></label>
                    <input type="file" name="document" class="form-control form-control-sm" accept=".pdf,.jpg,.jpeg,.png" required>
                    <small class="text-muted">Tập tin sẽ được mã hóa và lưu trữ an toàn ngoài thư mục <code>public/</code>.</small>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Ghi chú</label>
                    <textarea name="ghi_chu" class="form-control form-control-sm" rows="2" placeholder="Ghi chú thêm nếu có..."></textarea>
                </div>
            </div>

            <div class="modal-footer bg-light">
                <button type="button" class="btn btn-light btn-sm" data-bs-dismiss="modal">Đóng</button>
                <button type="submit" class="btn btn-primary btn-sm fw-semibold" style="background: #0033A0;">Tải Lên</button>
            </div>
        </form>
    </div>
</div>

<!-- ============================================================== -->
<!-- MODAL: THÊM KHEN THƯỞNG / KỶ LUẬT (KT-KL) -->
<!-- ============================================================== -->
<div class="modal fade" id="modalAddKTKL" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
        <form action="<?= url('employees/add_ktkl') ?>" method="POST" class="modal-content">
            <?= csrf_field() ?>
            <input type="hidden" name="ma_nv" value="<?= e($employee['MaNV']) ?>">

            <div class="modal-header bg-light">
                <h5 class="modal-title fw-bold text-dark">Thêm Quyết Định Khen Thưởng / Kỷ Luật</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>

            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Phân loại <span class="text-danger">*</span></label>
                    <select name="loai" class="form-select form-select-sm" required>
                        <option value="KHEN_THUONG">Khen Thưởng</option>
                        <option value="KY_LUAT">Kỷ Luật</option>
                    </select>
                </div>
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Hình thức <span class="text-danger">*</span></label>
                    <input type="text" name="hinh_thuc" class="form-control form-control-sm" placeholder="VD: Bằng khen Tổng Công ty, Khiển trách..." required>
                </div>
                <div class="row g-2 mb-3">
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Số quyết định</label>
                        <input type="text" name="so_quyet_dinh" class="form-control form-control-sm" placeholder="123/QĐ-VNM">
                    </div>
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Ngày quyết định <span class="text-danger">*</span></label>
                        <input type="date" name="ngay_quyet_dinh" class="form-control form-control-sm" value="<?= date('Y-m-d') ?>" required>
                    </div>
                </div>
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Số tiền thưởng / phạt (VNĐ)</label>
                    <input type="number" name="so_tien" class="form-control form-control-sm" value="0">
                </div>
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Lý do / Thành tích <span class="text-danger">*</span></label>
                    <textarea name="ly_do" class="form-control form-control-sm" rows="3" required placeholder="Nêu rõ lý do khen thưởng hoặc vi phạm..."></textarea>
                </div>
            </div>

            <div class="modal-footer bg-light">
                <button type="button" class="btn btn-light btn-sm" data-bs-dismiss="modal">Hủy</button>
                <button type="submit" class="btn btn-primary btn-sm fw-semibold" style="background: #0033A0;">Lưu Quyết Định</button>
            </div>
        </form>
    </div>
</div>

<!-- ============================================================== -->
<!-- MODAL: CẤP PHÁT TÀI SẢN MỚI CÓ KÝ NHẬN -->
<!-- ============================================================== -->
<div class="modal fade" id="modalAddAsset" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
        <form action="<?= url('employees/add_asset') ?>" method="POST" class="modal-content">
            <?= csrf_field() ?>
            <input type="hidden" name="ma_nv" value="<?= e($employee['MaNV']) ?>">

            <div class="modal-header bg-light">
                <h5 class="modal-title fw-bold text-dark"><i class="bi bi-box-seam text-primary me-2"></i>Cấp Phát Tài Sản & PPE Mới</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>

            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Loại tài sản / PPE <span class="text-danger">*</span></label>
                    <select name="loai_tai_san" class="form-select form-select-sm" required id="selectAssetType">
                        <option value="BHLD_PPE">Bảo hộ lao động (Ủng, Áo, Mũ, Găng tay, Kính)</option>
                        <option value="MAY_TINH_LAPTOP">Máy tính / Laptop (Dell, ThinkPad)</option>
                        <option value="THE_TU_RA_VAO">Thẻ từ thông minh RFID Vinamilk</option>
                        <option value="DIEN_THOAI_SIM">Điện thoại / SIM liên lạc</option>
                        <option value="KHAC">Tài sản khác</option>
                    </select>
                </div>
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Tên trang bị / tài sản <span class="text-danger">*</span></label>
                    <input type="text" name="ten_tai_san" class="form-control form-control-sm" placeholder="VD: Bộ Trang Phục Phòng Sạch Tiệt Trùng hoặc Laptop Dell Latitude" required>
                </div>
                <div class="row g-2 mb-3">
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Mã Serial / Định danh</label>
                        <input type="text" name="ma_dinh_danh_serial" class="form-control form-control-sm" placeholder="Serial hoặc mã vạch...">
                    </div>
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Giá trị định mức (VNĐ)</label>
                        <input type="number" name="gia_tri_tai_san" class="form-control form-control-sm" value="500000" placeholder="Giá trị nếu làm mất...">
                    </div>
                </div>
                <div class="row g-2 mb-3">
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Số lượng & Đơn vị tính</label>
                        <div class="input-group input-group-sm">
                            <input type="number" name="so_luong" class="form-control" value="1" min="1">
                            <input type="text" name="don_vi_tinh" class="form-control" value="Bộ" placeholder="ĐVT">
                        </div>
                    </div>
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Ngày cấp phát</label>
                        <input type="date" name="ngay_cap_phat" class="form-control form-control-sm" value="<?= date('Y-m-d') ?>" required>
                    </div>
                </div>
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Tình trạng khi bàn giao</label>
                    <input type="text" name="tinh_trang_khi_cap" class="form-control form-control-sm" value="Mới 100% nguyên hộp">
                </div>
                <div class="form-check p-2 bg-light border rounded mb-2">
                    <input class="form-check-input ms-1 me-2" type="checkbox" name="da_ky_nhan_cap" value="1" id="checkSignNow" checked>
                    <label class="form-check-label small fw-semibold" for="checkSignNow">
                        Xác nhận ký nhận điện tử bàn giao ngay (Tạo mã biên bản & mã băm bảo mật)
                    </label>
                </div>
            </div>

            <div class="modal-footer bg-light">
                <button type="button" class="btn btn-light btn-sm" data-bs-dismiss="modal">Hủy</button>
                <button type="submit" class="btn btn-primary btn-sm fw-semibold" style="background: #0033A0;">Xác Nhận Cấp Phát</button>
            </div>
        </form>
    </div>
</div>

<!-- ============================================================== -->
<!-- MODAL: THU HỒI / BÁO MẤT / BÁO HỎNG TÀI SẢN (TỰ KHẤU TRỪ) -->
<!-- ============================================================== -->
<div class="modal fade" id="modalRecoverAsset" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
        <form action="<?= url('employees/return_asset') ?>" method="POST" class="modal-content">
            <?= csrf_field() ?>
            <input type="hidden" name="ma_nv" value="<?= e($employee['MaNV']) ?>">
            <input type="hidden" name="asset_id" id="recoverAssetId">

            <div class="modal-header bg-light">
                <h5 class="modal-title fw-bold text-dark"><i class="bi bi-arrow-return-left text-danger me-2"></i>Thu Hồi / Xử Lý Tài Sản Cấp Phát</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>

            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Tài sản xử lý</label>
                    <input type="text" id="recoverAssetName" class="form-control form-control-sm bg-light" readonly>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Hình thức xử lý <span class="text-danger">*</span></label>
                    <select name="status" class="form-select form-select-sm" required id="selectRecoverStatus">
                        <option value="DA_THU_HOI">Đã thu hồi đầy đủ (Hoàn trả công ty)</option>
                        <option value="BAO_MAT">BÁO MẤT (Chuyển khấu trừ vào lương)</option>
                        <option value="BAO_HONG">BÁO HỎNG (Chuyển khấu trừ bồi thường vào lương)</option>
                    </select>
                </div>

                <div class="mb-3" id="penaltyGroup">
                    <label class="form-label small fw-semibold text-danger">Số tiền khấu trừ lương bồi thường (VNĐ)</label>
                    <input type="number" name="penalty_amount" id="recoverPenaltyAmount" class="form-control form-control-sm" value="0">
                    <small class="text-muted">Khoản tiền này sẽ tự động chuyển vào mục Khấu trừ tài sản trong kỳ lương quyết toán.</small>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Tình trạng kiểm tra</label>
                    <input type="text" name="condition" class="form-control form-control-sm" placeholder="VD: Bình thường, Trầy xước, Không hoàn trả...">
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Ghi chú biên bản</label>
                    <textarea name="notes" class="form-control form-control-sm" rows="2" placeholder="Ghi chú chi tiết biên bản bàn giao..."></textarea>
                </div>
            </div>

            <div class="modal-footer bg-light">
                <button type="button" class="btn btn-light btn-sm" data-bs-dismiss="modal">Hủy</button>
                <button type="submit" class="btn btn-danger btn-sm fw-semibold">Xác Nhận Xử Lý</button>
            </div>
        </form>
    </div>
</div>

<!-- ============================================================== -->
<!-- MODAL: CẤP / GIA HẠN CHỨNG CHỈ (THẺ XANH ATTP, ATLĐ, THIẾT BỊ) -->
<!-- ============================================================== -->
<div class="modal fade" id="modalAddCertificate" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
        <form action="<?= url('employees/add_certificate') ?>" method="POST" class="modal-content">
            <?= csrf_field() ?>
            <input type="hidden" name="ma_nv" value="<?= e($employee['MaNV']) ?>">

            <div class="modal-header bg-light">
                <h5 class="modal-title fw-bold text-dark"><i class="bi bi-award text-primary me-2"></i>Cấp / Cập Nhật Chứng Chỉ & Thẻ ATTP</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>

            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Loại chứng chỉ <span class="text-danger">*</span></label>
                    <select name="loai_chung_chi" class="form-select form-select-sm" required>
                        <option value="THE_XANH_ATTP">Thẻ Xanh An Toàn Thực Phẩm (Khám Sức Khỏe Định Kỳ)</option>
                        <option value="ATLD_VE_SINH_LD">Chứng chỉ An Toàn Vệ Sinh Lao Động (ATLĐ Nhóm 1-6)</option>
                        <option value="CHUNG_CHI_THIET_BI">Chứng chỉ Vận Hành Thiết Bị (Tetra Pak, Xe Nâng, Lò Hơi...)</option>
                        <option value="PCCC_CUU_HO">Chứng nhận PCCC & Cứu Nạn Cứu Hộ</option>
                        <option value="KHAC">Chứng chỉ chuyên môn khác</option>
                    </select>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Tên chứng chỉ / Thẻ <span class="text-danger">*</span></label>
                    <input type="text" name="ten_chung_chi" class="form-control form-control-sm" placeholder="VD: Thẻ Xanh Vệ Sinh ATTP Nhà Máy Sữa" required>
                </div>

                <div class="row g-2 mb-3">
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Số hiệu chứng chỉ</label>
                        <input type="text" name="so_hieu_chung_chi" class="form-control form-control-sm" placeholder="Số hiệu bằng...">
                    </div>
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Cơ quan / Cơ sở y tế cấp</label>
                        <input type="text" name="noi_cap" class="form-control form-control-sm" placeholder="VD: Sở Y Tế Bình Dương">
                    </div>
                </div>

                <div class="row g-2 mb-3">
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Ngày cấp / Khám định kỳ</label>
                        <input type="date" name="ngay_cap" class="form-control form-control-sm" value="<?= date('Y-m-d') ?>" required>
                    </div>
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Ngày hết hạn <span class="text-danger">*</span></label>
                        <input type="date" name="ngay_het_han" class="form-control form-control-sm" value="<?= date('Y-m-d', strtotime('+1 year')) ?>" required>
                    </div>
                </div>

                <div class="row g-2 mb-3">
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Cảnh báo trước (ngày)</label>
                        <input type="number" name="canh_bao_truoc_ngay" class="form-control form-control-sm" value="60">
                    </div>
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Kết quả khám sức khỏe</label>
                        <input type="text" name="ket_qua_kham" class="form-control form-control-sm" value="Đạt yêu cầu sức khỏe ATTP">
                    </div>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Ghi chú</label>
                    <textarea name="ghi_chu" class="form-control form-control-sm" rows="2" placeholder="Ghi chú thêm..."></textarea>
                </div>
            </div>

            <div class="modal-footer bg-light">
                <button type="button" class="btn btn-light btn-sm" data-bs-dismiss="modal">Hủy</button>
                <button type="submit" class="btn btn-primary btn-sm fw-semibold" style="background: #0033A0;">Lưu Chứng Chỉ</button>
            </div>
        </form>
    </div>
</div>

<!-- ============================================================== -->
<!-- MODAL: KHỞI TẠO QUY TRÌNH THÔI VIỆC (OFFBOARDING) -->
<!-- ============================================================== -->
<div class="modal fade" id="modalInitOffboarding" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
        <form action="<?= url('employees/init_offboarding') ?>" method="POST" class="modal-content">
            <?= csrf_field() ?>
            <input type="hidden" name="ma_nv" value="<?= e($employee['MaNV']) ?>">

            <div class="modal-header bg-light">
                <h5 class="modal-title fw-bold text-dark"><i class="bi bi-box-arrow-right text-danger me-2"></i>Khởi Tạo Quy Trình Thôi Việc (Offboarding)</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>

            <div class="modal-body">
                <div class="alert alert-info py-2 px-3 small mb-3">
                    <i class="bi bi-info-circle me-1"></i> Quy trình 6 bước bao gồm: Bàn giao công việc &rarr; Thu hồi tài sản (tự khấu trừ nếu chưa trả) &rarr; Khóa tài khoản &rarr; Chốt công &rarr; Quyết toán lương &rarr; Chốt sổ BHXH.
                </div>

                <div class="row g-2 mb-3">
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Ngày nhận đề xuất thôi việc</label>
                        <input type="date" name="ngay_de_xuat" class="form-control form-control-sm" value="<?= date('Y-m-d') ?>" required>
                    </div>
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Ngày làm việc cuối cùng <span class="text-danger">*</span></label>
                        <input type="date" name="ngay_lam_viec_cuoi" class="form-control form-control-sm" value="<?= date('Y-m-d', strtotime('+30 days')) ?>" required>
                    </div>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Lý do nghỉ việc chính <span class="text-danger">*</span></label>
                    <input type="text" name="ly_do_nghi" class="form-control form-control-sm" placeholder="VD: Thôi việc theo nguyện vọng cá nhân, chuyển công tác..." required>
                </div>
            </div>

            <div class="modal-footer bg-light">
                <button type="button" class="btn btn-light btn-sm" data-bs-dismiss="modal">Hủy</button>
                <button type="submit" class="btn btn-danger btn-sm fw-semibold">Bắt Đầu Quy Trình Offboarding</button>
            </div>
        </form>
    </div>
</div>

<script>
document.addEventListener('DOMContentLoaded', function() {
    var modalRecover = document.getElementById('modalRecoverAsset');
    if (modalRecover) {
        modalRecover.addEventListener('show.bs.modal', function(event) {
            var btn = event.relatedTarget;
            var id = btn.getAttribute('data-id');
            var name = btn.getAttribute('data-name');
            var val = btn.getAttribute('data-value');

            document.getElementById('recoverAssetId').value = id;
            document.getElementById('recoverAssetName').value = name;
            document.getElementById('recoverPenaltyAmount').value = val;
        });
    }
});
</script>
