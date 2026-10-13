<?php
require_once __DIR__ . '/../layouts/header.php';
?>

<div class="container-fluid px-4 py-4">
    <!-- Header -->
    <div class="d-flex flex-wrap justify-content-between align-items-center mb-4">
        <div>
            <nav aria-label="breadcrumb">
                <ol class="breadcrumb mb-1 small">
                    <li class="breadcrumb-item"><a href="<?= url('dashboard') ?>" class="text-decoration-none">Trang chủ</a></li>
                    <li class="breadcrumb-item"><a href="<?= url('employees') ?>" class="text-decoration-none">Hồ sơ nhân sự</a></li>
                    <li class="breadcrumb-item active">Chứng chỉ & An toàn (ATTP / ATLĐ)</li>
                </ol>
            </nav>
            <h4 class="fw-bold text-dark mb-0">
                <i class="bi bi-shield-check text-success me-2"></i>Quản Lý Chứng Chỉ, Thẻ Xanh ATTP & ATLĐ
            </h4>
            <small class="text-muted">Giám sát hạn định kỳ 60 ngày & Thực thi quy chuẩn chặn xếp ca dây chuyền sản xuất ISO 22000 / HACCP</small>
        </div>
        <div>
            <button class="btn btn-primary fw-semibold btn-sm shadow-sm" data-bs-toggle="modal" data-bs-target="#modalAddCertificate" style="background: #0033A0;">
                <i class="bi bi-plus-lg me-1"></i> Cấp Mới / Gia Hạn Chứng Chỉ
            </button>
        </div>
    </div>

    <!-- Alert nếu có thẻ xanh ATTP quá hạn gây chặn ca -->
    <?php if ($countAttpExpired > 0): ?>
    <div class="alert alert-danger border border-danger d-flex align-items-center mb-4 shadow-sm py-3 px-4">
        <i class="bi bi-slash-circle-fill text-danger fs-1 me-3"></i>
        <div class="flex-grow-1">
            <h6 class="fw-bold text-danger mb-1"><i class="bi bi-exclamation-triangle-fill me-1"></i> PHÁT HIỆN <?= $countAttpExpired ?> NHÂN SỰ CÓ THẺ XANH ATTP ĐÃ HẾT HẠN!</h6>
            <div class="small text-dark">
                Hệ thống tự động <strong>KÍCH HOẠT QUY TẮC CHẶN XẾP CA</strong> vào dây chuyền sản xuất nhà máy (Ca 1, Ca 2, Ca 3, Ca gãy) đối với các nhân sự này. Quản đốc phân xưởng không thể xếp ca cho đến khi hoàn tất khám sức khỏe định kỳ & gia hạn thẻ xanh!
            </div>
        </div>
        <a href="?loai_chung_chi=THE_XANH_ATTP" class="btn btn-danger btn-sm text-nowrap ms-3 fw-semibold">
            Xem Danh Sách Chặn Ca
        </a>
    </div>
    <?php endif; ?>

    <!-- 4 KPI Cards -->
    <div class="row g-3 mb-4">
        <div class="col-md-3 col-sm-6">
            <div class="card border-0 shadow-sm rounded-3 bg-white h-100 border-start border-4 border-danger">
                <div class="card-body p-3">
                    <div class="d-flex justify-content-between align-items-center">
                        <div>
                            <span class="text-muted small d-block">Thẻ ATTP Quá Hạn (Chặn Ca)</span>
                            <span class="fs-4 fw-bold text-danger"><?= $countAttpExpired ?></span>
                        </div>
                        <div class="rounded-circle bg-danger-subtle p-3 text-danger">
                            <i class="bi bi-slash-circle fs-4"></i>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div class="col-md-3 col-sm-6">
            <div class="card border-0 shadow-sm rounded-3 bg-white h-100 border-start border-4 border-warning">
                <div class="card-body p-3">
                    <div class="d-flex justify-content-between align-items-center">
                        <div>
                            <span class="text-muted small d-block">ATTP Cảnh Báo 60 Ngày</span>
                            <span class="fs-4 fw-bold text-warning"><?= $countAttpWarning ?></span>
                        </div>
                        <div class="rounded-circle bg-warning-subtle p-3 text-warning">
                            <i class="bi bi-clock-history fs-4"></i>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div class="col-md-3 col-sm-6">
            <div class="card border-0 shadow-sm rounded-3 bg-white h-100 border-start border-4 border-primary">
                <div class="card-body p-3">
                    <div class="d-flex justify-content-between align-items-center">
                        <div>
                            <span class="text-muted small d-block">ATLĐ Nhóm 1-6 Cần Gia Hạn</span>
                            <span class="fs-4 fw-bold text-primary"><?= $countAtldWarning ?></span>
                        </div>
                        <div class="rounded-circle bg-primary-subtle p-3 text-primary">
                            <i class="bi bi-shield-exclamation fs-4"></i>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div class="col-md-3 col-sm-6">
            <div class="card border-0 shadow-sm rounded-3 bg-white h-100 border-start border-4 border-info">
                <div class="card-body p-3">
                    <div class="d-flex justify-content-between align-items-center">
                        <div>
                            <span class="text-muted small d-block">Chứng Chỉ Thiết Bị (Tetra Pak)</span>
                            <span class="fs-4 fw-bold text-info"><?= $countEquipWarning ?></span>
                        </div>
                        <div class="rounded-circle bg-info-subtle p-3 text-info">
                            <i class="bi bi-gear fs-4"></i>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- Bộ lọc tìm kiếm -->
    <div class="card border-0 shadow-sm rounded-3 mb-4">
        <div class="card-body p-3">
            <form method="GET" action="<?= url('certificates') ?>" class="row g-2 align-items-end">
                <div class="col-md-4">
                    <label class="form-label small fw-semibold text-muted mb-1">Đơn vị / Nhà máy</label>
                    <select name="ma_dv" class="form-select form-select-sm">
                        <option value="">-- Tất cả nhà máy & đơn vị --</option>
                        <?php foreach ($selectableUnits as $u): ?>
                            <option value="<?= e($u['MaDV']) ?>" <?= ($maDV === $u['MaDV']) ? 'selected' : '' ?>>
                                <?= e($u['TenDV']) ?> (<?= e($u['MaDV']) ?>)
                            </option>
                        <?php endforeach; ?>
                    </select>
                </div>
                <div class="col-md-3">
                    <label class="form-label small fw-semibold text-muted mb-1">Phân loại chứng chỉ</label>
                    <select name="loai_chung_chi" class="form-select form-select-sm">
                        <option value="">-- Tất cả chứng chỉ --</option>
                        <option value="THE_XANH_ATTP" <?= ($loaiCC === 'THE_XANH_ATTP') ? 'selected' : '' ?>>Thẻ Xanh ATTP (Khám định kỳ)</option>
                        <option value="ATLD_VE_SINH_LD" <?= ($loaiCC === 'ATLD_VE_SINH_LD') ? 'selected' : '' ?>>ATLĐ & Vệ sinh môi trường</option>
                        <option value="CHUNG_CHI_THIET_BI" <?= ($loaiCC === 'CHUNG_CHI_THIET_BI') ? 'selected' : '' ?>>Vận hành máy móc / Tetra Pak</option>
                        <option value="PCCC_CUU_HO" <?= ($loaiCC === 'PCCC_CUU_HO') ? 'selected' : '' ?>>PCCC & Cứu nạn</option>
                    </select>
                </div>
                <div class="col-md-3">
                    <label class="form-label small fw-semibold text-muted mb-1">Mốc cảnh báo (ngày)</label>
                    <select name="warning_days" class="form-select form-select-sm">
                        <option value="30" <?= ($warningDays === 30) ? 'selected' : '' ?>>Trong vòng 30 ngày</option>
                        <option value="60" <?= ($warningDays === 60) ? 'selected' : '' ?>>Trong vòng 60 ngày (Tiêu chuẩn)</option>
                        <option value="90" <?= ($warningDays === 90) ? 'selected' : '' ?>>Trong vòng 90 ngày</option>
                    </select>
                </div>
                <div class="col-md-2">
                    <button type="submit" class="btn btn-primary btn-sm w-100 fw-semibold">
                        <i class="bi bi-funnel me-1"></i> Lọc Dữ Liệu
                    </button>
                </div>
            </form>
        </div>
    </div>

    <!-- Bảng Danh Sách Chứng Chỉ & Cảnh Báo -->
    <div class="card border-0 shadow-sm rounded-3">
        <div class="card-header bg-white py-3 border-0">
            <h6 class="fw-bold text-dark mb-0">
                <i class="bi bi-table me-2 text-primary"></i>Danh Sách Chứng Chỉ Hết Hạn & Cảnh Báo 60 Ngày (<?= count($expiringCerts) ?> hồ sơ)
            </h6>
        </div>
        <div class="table-responsive">
            <table class="table table-hover align-middle mb-0">
                <thead class="table-light small text-muted">
                    <tr>
                        <th>Nhân Sự</th>
                        <th>Đơn Vị & Chức Vụ</th>
                        <th>Loại Chứng Chỉ</th>
                        <th>Tên Chứng Chỉ / Thẻ</th>
                        <th>Số Hiệu & Nơi Cấp</th>
                        <th>Ngày Hết Hạn & Cảnh Báo</th>
                        <th class="text-center">Trạng Thái Xếp Ca</th>
                        <th class="text-end">Thao Tác</th>
                    </tr>
                </thead>
                <tbody>
                    <?php if (empty($expiringCerts)): ?>
                        <tr>
                            <td colspan="8" class="text-center py-5 text-muted">
                                <i class="bi bi-check-circle text-success fs-1 d-block mb-2"></i>
                                Tất cả chứng chỉ và thẻ ATTP đều đang trong thời hạn an toàn (> 60 ngày).
                            </td>
                        </tr>
                    <?php else: foreach ($expiringCerts as $c): 
                        $isAttp = ($c['loai_chung_chi'] === 'THE_XANH_ATTP' || !empty($c['la_the_xanh_attp']));
                        $isExpired = ($c['so_ngay_con_lai'] < 0);
                        $isWarn60 = (!$isExpired && $c['so_ngay_con_lai'] <= 60);
                    ?>
                        <tr class="<?= ($isAttp && $isExpired) ? 'table-danger' : ($isWarn60 ? 'table-warning' : '') ?>">
                            <td>
                                <a href="<?= url('employees/show/' . $c['ma_nv'] . '?tab=dao-tao') ?>" class="fw-bold text-decoration-none text-dark">
                                    <?= e($c['HoTen']) ?>
                                </a>
                                <div class="font-monospace small text-muted"><?= e($c['ma_nv']) ?></div>
                            </td>
                            <td>
                                <div><?= e($c['TenDV'] ?? $c['MaDV']) ?></div>
                                <small class="text-muted"><?= e($c['TenCV'] ?? '') ?></small>
                            </td>
                            <td>
                                <?php if ($isAttp): ?>
                                    <span class="badge bg-success-subtle text-success border border-success-subtle">
                                        <i class="bi bi-check2-circle me-1"></i> Thẻ Xanh ATTP
                                    </span>
                                <?php elseif ($c['loai_chung_chi'] === 'ATLD_VE_SINH_LD'): ?>
                                    <span class="badge bg-warning-subtle text-dark border border-warning-subtle">
                                        <i class="bi bi-shield-check me-1"></i> ATLĐ Nhóm 1-6
                                    </span>
                                <?php elseif ($c['loai_chung_chi'] === 'CHUNG_CHI_THIET_BI'): ?>
                                    <span class="badge bg-primary-subtle text-primary border border-primary-subtle">
                                        <i class="bi bi-gear-wide-connected me-1"></i> Vận Hành Máy
                                    </span>
                                <?php else: ?>
                                    <span class="badge bg-light text-dark border"><?= e($c['loai_chung_chi']) ?></span>
                                <?php endif; ?>
                            </td>
                            <td class="fw-bold text-dark">
                                <?= e($c['ten_chung_chi']) ?>
                                <?php if (!empty($c['ket_qua_kham'])): ?>
                                    <div class="small text-muted fw-normal"><i class="bi bi-heart-pulse text-danger me-1"></i><?= e($c['ket_qua_kham']) ?></div>
                                <?php endif; ?>
                            </td>
                            <td>
                                <span class="font-monospace small"><?= e($c['so_hieu_chung_chi'] ?: '-') ?></span>
                                <div class="small text-muted"><?= e($c['co_so_y_te'] ?: ($c['noi_cap'] ?: 'Vinamilk')) ?></div>
                            </td>
                            <td>
                                <span class="fw-semibold"><?= format_date($c['ngay_het_han']) ?></span>
                                <?php if ($isExpired): ?>
                                    <span class="badge bg-danger text-white d-block mt-1">Quá hạn <?= abs($c['so_ngay_con_lai']) ?> ngày</span>
                                <?php elseif ($isWarn60): ?>
                                    <span class="badge bg-warning text-dark d-block mt-1">Còn <?= $c['so_ngay_con_lai'] ?> ngày (≤ 60d)</span>
                                <?php else: ?>
                                    <span class="small text-success d-block mt-1">Còn <?= $c['so_ngay_con_lai'] ?> ngày</span>
                                <?php endif; ?>
                            </td>
                            <td class="text-center">
                                <?php if ($isAttp && $isExpired): ?>
                                    <span class="badge bg-danger text-white px-2 py-1 shadow-sm">
                                        <i class="bi bi-slash-circle me-1"></i> CHẶN XẾP CA
                                    </span>
                                <?php elseif ($isAttp): ?>
                                    <span class="badge bg-success text-white px-2 py-1">
                                        <i class="bi bi-check-lg me-1"></i> ĐỦ ĐK DÂY CHUYỀN
                                    </span>
                                <?php else: ?>
                                    <span class="badge bg-light text-secondary border"><?= e($c['trang_thai']) ?></span>
                                <?php endif; ?>
                            </td>
                            <td class="text-end">
                                <a href="<?= url('employees/show/' . $c['ma_nv'] . '?tab=dao-tao') ?>" class="btn btn-outline-primary btn-sm py-1 px-2">
                                    <i class="bi bi-arrow-repeat me-1"></i> Gia Hạn
                                </a>
                            </td>
                        </tr>
                    <?php endforeach; endif; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>

<!-- Modal Thêm Chứng Chỉ -->
<div class="modal fade" id="modalAddCertificate" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
        <form action="<?= url('certificates/save') ?>" method="POST" class="modal-content">
            <?= csrf_field() ?>
            <input type="hidden" name="return_url" value="certificates">

            <div class="modal-header bg-light">
                <h5 class="modal-title fw-bold text-dark"><i class="bi bi-award text-primary me-2"></i>Cấp / Cập Nhật Chứng Chỉ</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>

            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Mã nhân sự <span class="text-danger">*</span></label>
                    <input type="text" name="ma_nv" class="form-control form-control-sm" placeholder="VD: VNM-0016" required>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Loại chứng chỉ <span class="text-danger">*</span></label>
                    <select name="loai_chung_chi" class="form-select form-select-sm" required>
                        <option value="THE_XANH_ATTP">Thẻ Xanh An Toàn Thực Phẩm (Khám Sức Khỏe Định Kỳ)</option>
                        <option value="ATLD_VE_SINH_LD">Chứng chỉ An Toàn Vệ Sinh Lao Động (ATLĐ Nhóm 1-6)</option>
                        <option value="CHUNG_CHI_THIET_BI">Chứng chỉ Vận Hành Thiết Bị (Tetra Pak, Xe Nâng, Lò Hơi...)</option>
                        <option value="PCCC_CUU_HO">Chứng nhận PCCC & Cứu Nạn Cứu Hộ</option>
                        <option value="KHAC">Chứng chỉ khác</option>
                    </select>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Tên chứng chỉ <span class="text-danger">*</span></label>
                    <input type="text" name="ten_chung_chi" class="form-control form-control-sm" placeholder="VD: Thẻ Xanh Vệ Sinh ATTP Nhà Máy Sữa" required>
                </div>

                <div class="row g-2 mb-3">
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Số hiệu bằng / thẻ</label>
                        <input type="text" name="so_hieu_chung_chi" class="form-control form-control-sm" placeholder="Số hiệu...">
                    </div>
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Cơ quan / Cơ sở y tế</label>
                        <input type="text" name="noi_cap" class="form-control form-control-sm" placeholder="BV hoặc Sở Y Tế...">
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

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Kết quả khám sức khỏe</label>
                    <input type="text" name="ket_qua_kham" class="form-control form-control-sm" value="Đạt yêu cầu sức khỏe ATTP">
                </div>
            </div>

            <div class="modal-footer bg-light">
                <button type="button" class="btn btn-light btn-sm" data-bs-dismiss="modal">Hủy</button>
                <button type="submit" class="btn btn-primary btn-sm fw-semibold" style="background: #0033A0;">Lưu Chứng Chỉ</button>
            </div>
        </form>
    </div>
</div>

<?php
require_once __DIR__ . '/../layouts/footer.php';
?>
