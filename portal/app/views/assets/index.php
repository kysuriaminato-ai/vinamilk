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
                    <li class="breadcrumb-item active">Tài sản & Trang bị PPE</li>
                </ol>
            </nav>
            <h4 class="fw-bold text-dark mb-0">
                <i class="bi bi-laptop text-primary me-2"></i>Quản Lý Cấp Phát Tài Sản & Trang Bị PPE
            </h4>
            <small class="text-muted">Cấp phát theo vị trí (Nhà máy: Ủng, Áo, Mũ, Găng; Văn phòng: Laptop, Thẻ từ) - Có ký nhận điện tử & Thu hồi khấu trừ lương</small>
        </div>
        <div>
            <a href="<?= url('assets/standards') ?>" class="btn btn-outline-secondary btn-sm me-2">
                <i class="bi bi-list-check me-1"></i> Danh Mục Chuẩn Theo Vị Trí
            </a>
            <button class="btn btn-primary fw-semibold btn-sm shadow-sm" data-bs-toggle="modal" data-bs-target="#modalAllocateAsset" style="background: #0033A0;">
                <i class="bi bi-plus-lg me-1"></i> Cấp Phát Mới (Có Ký Nhận)
            </button>
        </div>
    </div>

    <!-- 5 KPI Cards -->
    <div class="row g-3 mb-4">
        <div class="col-md-2 col-sm-4">
            <div class="card border-0 shadow-sm rounded-3 bg-white h-100 border-start border-4 border-primary">
                <div class="card-body p-3">
                    <span class="text-muted small d-block">Tổng Tài Sản Giao</span>
                    <span class="fs-4 fw-bold text-primary"><?= (int)($stats['tong_so'] ?? 0) ?></span>
                </div>
            </div>
        </div>
        <div class="col-md-2 col-sm-4">
            <div class="card border-0 shadow-sm rounded-3 bg-white h-100 border-start border-4 border-warning">
                <div class="card-body p-3">
                    <span class="text-muted small d-block">PPE Bảo Hộ Nhà Máy</span>
                    <span class="fs-4 fw-bold text-dark"><?= (int)($stats['so_ppe'] ?? 0) ?></span>
                </div>
            </div>
        </div>
        <div class="col-md-2 col-sm-4">
            <div class="card border-0 shadow-sm rounded-3 bg-white h-100 border-start border-4 border-info">
                <div class="card-body p-3">
                    <span class="text-muted small d-block">Laptop & Thiết Bị IT</span>
                    <span class="fs-4 fw-bold text-info"><?= (int)($stats['so_laptop'] ?? 0) ?></span>
                </div>
            </div>
        </div>
        <div class="col-md-3 col-sm-6">
            <div class="card border-0 shadow-sm rounded-3 bg-white h-100 border-start border-4 border-success">
                <div class="card-body p-3">
                    <span class="text-muted small d-block">Đã Ký Nhận Điện Tử</span>
                    <span class="fs-4 fw-bold text-success"><?= (int)($stats['da_ky_nhan'] ?? 0) ?></span>
                    <small class="text-muted d-block" style="font-size: 11px;">Có biên bản & hash bàn giao</small>
                </div>
            </div>
        </div>
        <div class="col-md-3 col-sm-6">
            <div class="card border-0 shadow-sm rounded-3 bg-white h-100 border-start border-4 border-danger">
                <div class="card-body p-3">
                    <span class="text-muted small d-block">Báo Mất / Khấu Trừ Lương</span>
                    <span class="fs-4 fw-bold text-danger"><?= number_format((float)($stats['tong_khau_tru'] ?? 0), 0, ',', '.') ?>đ</span>
                    <small class="text-danger d-block" style="font-size: 11px;"><?= (int)($stats['bao_mat_hong'] ?? 0) ?> tài sản thất thoát / hỏng</small>
                </div>
            </div>
        </div>
    </div>

    <!-- Bộ lọc tìm kiếm -->
    <div class="card border-0 shadow-sm rounded-3 mb-4">
        <div class="card-body p-3">
            <form method="GET" action="<?= url('assets') ?>" class="row g-2 align-items-end">
                <div class="col-md-3">
                    <label class="form-label small fw-semibold text-muted mb-1">Từ khóa tìm kiếm</label>
                    <input type="text" name="keyword" class="form-control form-control-sm" placeholder="Tên nhân sự, mã NV, serial..." value="<?= e($keyword) ?>">
                </div>
                <div class="col-md-3">
                    <label class="form-label small fw-semibold text-muted mb-1">Đơn vị / Nhà máy</label>
                    <select name="ma_dv" class="form-select form-select-sm">
                        <option value="">-- Tất cả đơn vị --</option>
                        <?php foreach ($selectableUnits as $u): ?>
                            <option value="<?= e($u['MaDV']) ?>" <?= ($maDV === $u['MaDV']) ? 'selected' : '' ?>>
                                <?= e($u['TenDV']) ?>
                            </option>
                        <?php endforeach; ?>
                    </select>
                </div>
                <div class="col-md-2">
                    <label class="form-label small fw-semibold text-muted mb-1">Loại tài sản</label>
                    <select name="loai_tai_san" class="form-select form-select-sm">
                        <option value="">-- Tất cả --</option>
                        <option value="BHLD_PPE" <?= ($loaiTS === 'BHLD_PPE') ? 'selected' : '' ?>>Bảo hộ PPE (Nhà máy)</option>
                        <option value="MAY_TINH_LAPTOP" <?= ($loaiTS === 'MAY_TINH_LAPTOP') ? 'selected' : '' ?>>Laptop IT</option>
                        <option value="THE_TU_RA_VAO" <?= ($loaiTS === 'THE_TU_RA_VAO') ? 'selected' : '' ?>>Thẻ từ RFID</option>
                        <option value="DIEN_THOAI_SIM" <?= ($loaiTS === 'DIEN_THOAI_SIM') ? 'selected' : '' ?>>Điện thoại / SIM</option>
                    </select>
                </div>
                <div class="col-md-2">
                    <label class="form-label small fw-semibold text-muted mb-1">Trạng thái</label>
                    <select name="trang_thai" class="form-select form-select-sm">
                        <option value="">-- Tất cả --</option>
                        <option value="DANG_SU_DUNG" <?= ($trangThai === 'DANG_SU_DUNG') ? 'selected' : '' ?>>Đang sử dụng</option>
                        <option value="DA_THU_HOI" <?= ($trangThai === 'DA_THU_HOI') ? 'selected' : '' ?>>Đã thu hồi</option>
                        <option value="BAO_MAT" <?= ($trangThai === 'BAO_MAT') ? 'selected' : '' ?>>Báo mất (Khấu trừ)</option>
                        <option value="BAO_HONG" <?= ($trangThai === 'BAO_HONG') ? 'selected' : '' ?>>Báo hỏng</option>
                    </select>
                </div>
                <div class="col-md-2">
                    <button type="submit" class="btn btn-primary btn-sm w-100 fw-semibold">
                        <i class="bi bi-funnel me-1"></i> Lọc Tài Sản
                    </button>
                </div>
            </form>
        </div>
    </div>

    <!-- Bảng danh sách tài sản -->
    <div class="card border-0 shadow-sm rounded-3">
        <div class="card-header bg-white py-3 border-0">
            <h6 class="fw-bold text-dark mb-0">
                <i class="bi bi-table me-2 text-primary"></i>Danh Sách Tài Sản Cấp Phát & Trang Bị PPE (<?= count($assets) ?> bản ghi)
            </h6>
        </div>
        <div class="table-responsive">
            <table class="table table-hover align-middle mb-0">
                <thead class="table-light small text-muted">
                    <tr>
                        <th>Nhân Sự Nhận</th>
                        <th>Đơn Vị & Chức Vụ</th>
                        <th>Loại Tài Sản / PPE</th>
                        <th>Tên Trang Bị</th>
                        <th>Serial & Mã Biên Bản</th>
                        <th>Số Lượng & Giá Trị</th>
                        <th>Ngày Cấp Phát</th>
                        <th class="text-center">Ký Nhận Bàn Giao</th>
                        <th class="text-center">Trạng Thái & Khấu Trừ</th>
                        <th class="text-end">Thao Tác</th>
                    </tr>
                </thead>
                <tbody>
                    <?php if (empty($assets)): ?>
                        <tr><td colspan="10" class="text-center py-5 text-muted">Không tìm thấy tài sản nào phù hợp điều kiện lọc.</td></tr>
                    <?php else: foreach ($assets as $ts): 
                        $isLostOrDamaged = in_array($ts['trang_thai'], ['BAO_MAT', 'BAO_HONG']);
                        $penalty = (float)($ts['gia_tri_khau_tru'] ?? 0);
                    ?>
                        <tr class="<?= $isLostOrDamaged ? 'table-danger' : '' ?>">
                            <td>
                                <a href="<?= url('employees/show/' . $ts['ma_nv'] . '?tab=tai-san') ?>" class="fw-bold text-decoration-none text-dark">
                                    <?= e($ts['HoTen']) ?>
                                </a>
                                <div class="font-monospace small text-muted"><?= e($ts['ma_nv']) ?></div>
                            </td>
                            <td>
                                <div><?= e($ts['TenDV'] ?? $ts['MaDV']) ?></div>
                                <small class="text-muted"><?= e($ts['TenCV'] ?? '') ?></small>
                            </td>
                            <td>
                                <?php if ($ts['loai_tai_san'] === 'BHLD_PPE'): ?>
                                    <span class="badge bg-warning-subtle text-dark border border-warning-subtle"><i class="bi bi-person-badge me-1"></i> PPE Nhà Máy</span>
                                <?php elseif ($ts['loai_tai_san'] === 'MAY_TINH_LAPTOP'): ?>
                                    <span class="badge bg-primary-subtle text-primary border border-primary-subtle"><i class="bi bi-laptop me-1"></i> Laptop IT</span>
                                <?php elseif ($ts['loai_tai_san'] === 'THE_TU_RA_VAO'): ?>
                                    <span class="badge bg-info-subtle text-info border border-info-subtle"><i class="bi bi-credit-card-2-front me-1"></i> Thẻ Từ RFID</span>
                                <?php else: ?>
                                    <span class="badge bg-light text-dark border"><?= e($ts['loai_tai_san']) ?></span>
                                <?php endif; ?>
                            </td>
                            <td class="fw-bold text-dark"><?= e($ts['ten_tai_san']) ?></td>
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
                                    <form action="<?= url('assets/sign') ?>" method="POST" class="d-inline">
                                        <?= csrf_field() ?>
                                        <input type="hidden" name="return_url" value="assets">
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
                                <?php if ($ts['trang_thai'] === 'DANG_SU_DUNG'): ?>
                                    <button class="btn btn-outline-danger btn-sm py-0 px-2" 
                                            data-bs-toggle="modal" 
                                            data-bs-target="#modalRecoverAssetIndex" 
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
</div>

<!-- Modal Cấp Phát Tài Sản Mới -->
<div class="modal fade" id="modalAllocateAsset" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
        <form action="<?= url('assets/allocate') ?>" method="POST" class="modal-content">
            <?= csrf_field() ?>
            <input type="hidden" name="return_url" value="assets">

            <div class="modal-header bg-light">
                <h5 class="modal-title fw-bold text-dark"><i class="bi bi-box-seam text-primary me-2"></i>Cấp Phát Tài Sản & PPE Mới</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>

            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Mã nhân sự <span class="text-danger">*</span></label>
                    <input type="text" name="ma_nv" class="form-control form-control-sm" placeholder="VD: VNM-0016" required>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Loại tài sản / PPE <span class="text-danger">*</span></label>
                    <select name="loai_tai_san" class="form-select form-select-sm" required>
                        <option value="BHLD_PPE">Bảo hộ lao động (Ủng, Áo, Mũ, Găng tay, Kính)</option>
                        <option value="MAY_TINH_LAPTOP">Máy tính / Laptop (Dell, ThinkPad)</option>
                        <option value="THE_TU_RA_VAO">Thẻ từ thông minh RFID Vinamilk</option>
                        <option value="DIEN_THOAI_SIM">Điện thoại / SIM liên lạc</option>
                        <option value="KHAC">Tài sản khác</option>
                    </select>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Tên trang bị / tài sản <span class="text-danger">*</span></label>
                    <input type="text" name="ten_tai_san" class="form-control form-control-sm" placeholder="VD: Bộ Ủng & Trang Phục Phòng Sạch Tiệt Trùng" required>
                </div>

                <div class="row g-2 mb-3">
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Mã Serial / Định danh</label>
                        <input type="text" name="ma_dinh_danh_serial" class="form-control form-control-sm" placeholder="Serial máy...">
                    </div>
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Giá trị định mức (VNĐ)</label>
                        <input type="number" name="gia_tri_tai_san" class="form-control form-control-sm" value="500000">
                    </div>
                </div>

                <div class="row g-2 mb-3">
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Số lượng & ĐVT</label>
                        <div class="input-group input-group-sm">
                            <input type="number" name="so_luong" class="form-control" value="1" min="1">
                            <input type="text" name="don_vi_tinh" class="form-control" value="Bộ">
                        </div>
                    </div>
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Ngày cấp phát</label>
                        <input type="date" name="ngay_cap_phat" class="form-control form-control-sm" value="<?= date('Y-m-d') ?>" required>
                    </div>
                </div>

                <div class="form-check p-2 bg-light border rounded mb-2">
                    <input class="form-check-input ms-1 me-2" type="checkbox" name="da_ky_nhan_cap" value="1" id="checkSignAllocate" checked>
                    <label class="form-check-label small fw-semibold" for="checkSignAllocate">
                        Tạo biên bản bàn giao điện tử & Xác nhận ký nhận ngay
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

<!-- Modal Thu Hồi Tài Sản -->
<div class="modal fade" id="modalRecoverAssetIndex" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
        <form action="<?= url('assets/recover') ?>" method="POST" class="modal-content">
            <?= csrf_field() ?>
            <input type="hidden" name="return_url" value="assets">
            <input type="hidden" name="asset_id" id="recoverIdxAssetId">

            <div class="modal-header bg-light">
                <h5 class="modal-title fw-bold text-dark"><i class="bi bi-arrow-return-left text-danger me-2"></i>Thu Hồi / Khấu Trừ Bồi Thường Tài Sản</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>

            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Tài sản xử lý</label>
                    <input type="text" id="recoverIdxAssetName" class="form-control form-control-sm bg-light" readonly>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Hình thức xử lý <span class="text-danger">*</span></label>
                    <select name="status" class="form-select form-select-sm" required id="selectRecoverIdxStatus">
                        <option value="DA_THU_HOI">Đã thu hồi đầy đủ (Hoàn trả công ty)</option>
                        <option value="BAO_MAT">BÁO MẤT (Chuyển khấu trừ vào lương)</option>
                        <option value="BAO_HONG">BÁO HỎNG (Chuyển khấu trừ bồi thường vào lương)</option>
                    </select>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold text-danger">Số tiền khấu trừ lương (VNĐ)</label>
                    <input type="number" name="penalty_amount" id="recoverIdxPenalty" class="form-control form-control-sm" value="0">
                    <small class="text-muted">Khoản tiền này sẽ tự động khấu trừ vào lương quyết toán khi nhân viên thôi việc.</small>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Tình trạng kiểm tra</label>
                    <input type="text" name="condition" class="form-control form-control-sm" placeholder="VD: Bình thường, Trầy xước, Không hoàn trả...">
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Ghi chú biên bản</label>
                    <textarea name="notes" class="form-control form-control-sm" rows="2" placeholder="Ghi chú chi tiết..."></textarea>
                </div>
            </div>

            <div class="modal-footer bg-light">
                <button type="button" class="btn btn-light btn-sm" data-bs-dismiss="modal">Hủy</button>
                <button type="submit" class="btn btn-danger btn-sm fw-semibold">Xác Nhận Xử Lý</button>
            </div>
        </form>
    </div>
</div>

<script>
document.addEventListener('DOMContentLoaded', function() {
    var modalRecoverIdx = document.getElementById('modalRecoverAssetIndex');
    if (modalRecoverIdx) {
        modalRecoverIdx.addEventListener('show.bs.modal', function(event) {
            var btn = event.relatedTarget;
            var id = btn.getAttribute('data-id');
            var name = btn.getAttribute('data-name');
            var val = btn.getAttribute('data-value');

            document.getElementById('recoverIdxAssetId').value = id;
            document.getElementById('recoverIdxAssetName').value = name;
            document.getElementById('recoverIdxPenalty').value = val;
        });
    }
});
</script>

<?php
require_once __DIR__ . '/../layouts/footer.php';
?>
