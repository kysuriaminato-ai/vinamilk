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
                    <li class="breadcrumb-item active">Quy trình thôi việc (Offboarding)</li>
                </ol>
            </nav>
            <h4 class="fw-bold text-dark mb-0">
                <i class="bi bi-box-arrow-right text-danger me-2"></i>Quy Trình Thôi Việc (Offboarding Checklist 6 Bước)
            </h4>
            <small class="text-muted">Bàn giao &rarr; Thu hồi tài sản (tự khấu trừ lương) &rarr; Khóa tài khoản &rarr; Chốt công &rarr; Quyết toán lương &rarr; Chốt BHXH & Khảo sát lý do nghỉ cho AI</small>
        </div>
        <div>
            <button class="btn btn-danger fw-semibold btn-sm shadow-sm" data-bs-toggle="modal" data-bs-target="#modalCreateOffboarding">
                <i class="bi bi-plus-lg me-1"></i> Khởi Tạo Hồ Sơ Thôi Việc Mới
            </button>
        </div>
    </div>

    <!-- AI Turnover Analytics Card từ Exit Surveys -->
    <?php if (!empty($analytics['scores']) && (int)$analytics['scores']['tong_khao_sat'] > 0): ?>
    <div class="card border-0 shadow-sm rounded-3 mb-4 bg-light border-start border-4 border-primary">
        <div class="card-body p-4">
            <div class="d-flex justify-content-between align-items-center mb-3">
                <div>
                    <h6 class="fw-bold text-dark mb-1">
                        <i class="bi bi-robot text-primary me-2"></i>Dữ Liệu Khảo Sát Thôi Việc Nuôi Mô Hình AI (AI Exit Survey Analytics)
                    </h6>
                    <small class="text-muted">Tổng hợp từ <strong><?= (int)$analytics['scores']['tong_khao_sat'] ?></strong> phiếu khảo sát phỏng vấn nghỉ việc thực tế để đào tạo mô hình Machine Learning dự báo rủi ro biến động nhân sự.</small>
                </div>
                <div>
                    <span class="badge bg-success px-3 py-2">
                        <i class="bi bi-arrow-repeat me-1"></i> <?= (int)$analytics['scores']['so_nguoi_san_sang_quay_lai'] ?> / <?= (int)$analytics['scores']['tong_khao_sat'] ?> Sẵn Sàng Quay Lại Vinamilk
                    </span>
                </div>
            </div>

            <!-- Top Lý Do Nghỉ Việc Thực Tế -->
            <div class="row g-3">
                <div class="col-md-7">
                    <span class="small fw-bold text-muted d-block mb-2">TOP LÝ DO CHÍNH KHIẾN NHÂN VIÊN RỜI BỎ DOANH NGHIỆP:</span>
                    <div class="d-flex flex-wrap gap-2">
                        <?php foreach (($analytics['reasons'] ?? []) as $r): ?>
                            <span class="badge bg-white text-dark border p-2 small shadow-sm">
                                <span class="fw-bold text-danger"><?= $r['ty_le_phan_tram'] ?>%</span> - <?= e($r['ly_do_chinh']) ?> 
                                <span class="text-muted ms-1">(<?= $r['so_luong'] ?> người)</span>
                            </span>
                        <?php endforeach; ?>
                    </div>
                </div>
                <div class="col-md-5">
                    <span class="small fw-bold text-muted d-block mb-2">ĐIỂM HÀI LÒNG BÌNH QUÂN (THANG 1 - 5):</span>
                    <div class="row g-2 text-center small">
                        <div class="col-4 bg-white p-2 rounded border">
                            <span class="text-muted d-block" style="font-size: 11px;">Lương thưởng</span>
                            <strong class="text-primary fs-6"><?= $analytics['scores']['avg_luong'] ?>/5</strong>
                        </div>
                        <div class="col-4 bg-white p-2 rounded border">
                            <span class="text-muted d-block" style="font-size: 11px;">Quản lý trực tiếp</span>
                            <strong class="text-primary fs-6"><?= $analytics['scores']['avg_quan_ly'] ?>/5</strong>
                        </div>
                        <div class="col-4 bg-white p-2 rounded border">
                            <span class="text-muted d-block" style="font-size: 11px;">Thăng tiến</span>
                            <strong class="text-primary fs-6"><?= $analytics['scores']['avg_thang_tien'] ?>/5</strong>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    <?php endif; ?>

    <!-- Bộ lọc -->
    <div class="card border-0 shadow-sm rounded-3 mb-4">
        <div class="card-body p-3">
            <form method="GET" action="<?= url('offboarding') ?>" class="row g-2 align-items-end">
                <div class="col-md-4">
                    <label class="form-label small fw-semibold text-muted mb-1">Từ khóa tìm kiếm</label>
                    <input type="text" name="keyword" class="form-control form-control-sm" placeholder="Mã hồ sơ, tên nhân sự, mã NV..." value="<?= e($filters['keyword'] ?? '') ?>">
                </div>
                <div class="col-md-3">
                    <label class="form-label small fw-semibold text-muted mb-1">Đơn vị / Nhà máy</label>
                    <select name="ma_dv" class="form-select form-select-sm">
                        <option value="">-- Tất cả đơn vị --</option>
                        <?php foreach ($selectableUnits as $u): ?>
                            <option value="<?= e($u['MaDV']) ?>" <?= (($filters['ma_dv'] ?? '') === $u['MaDV']) ? 'selected' : '' ?>>
                                <?= e($u['TenDV']) ?>
                            </option>
                        <?php endforeach; ?>
                    </select>
                </div>
                <div class="col-md-3">
                    <label class="form-label small fw-semibold text-muted mb-1">Trạng thái hồ sơ</label>
                    <select name="trang_thai" class="form-select form-select-sm">
                        <option value="">-- Tất cả trạng thái --</option>
                        <option value="DANG_XU_LY" <?= (($filters['trang_thai'] ?? '') === 'DANG_XU_LY') ? 'selected' : '' ?>>Đang xử lý (Chưa đủ 6 bước)</option>
                        <option value="HOAN_TAT" <?= (($filters['trang_thai'] ?? '') === 'HOAN_TAT') ? 'selected' : '' ?>>Hoàn tất thôi việc (6/6 bước)</option>
                    </select>
                </div>
                <div class="col-md-2">
                    <button type="submit" class="btn btn-primary btn-sm w-100 fw-semibold">
                        <i class="bi bi-funnel me-1"></i> Lọc Hồ Sơ
                    </button>
                </div>
            </form>
        </div>
    </div>

    <!-- Bảng danh sách hồ sơ thôi việc -->
    <div class="card border-0 shadow-sm rounded-3">
        <div class="card-header bg-white py-3 border-0">
            <h6 class="fw-bold text-dark mb-0">
                <i class="bi bi-card-checklist me-2 text-danger"></i>Danh Sách Tiến Độ Thôi Việc (<?= count($list) ?> hồ sơ)
            </h6>
        </div>
        <div class="table-responsive">
            <table class="table table-hover align-middle mb-0">
                <thead class="table-light small text-muted">
                    <tr>
                        <th>Mã Hồ Sơ</th>
                        <th>Nhân Sự Thôi Việc</th>
                        <th>Đơn Vị & Chức Vụ</th>
                        <th>Ngày Làm Cuối</th>
                        <th>Lý Do Thôi Việc</th>
                        <th>Tiến Độ 6 Bước</th>
                        <th>Khấu Trừ Tài Sản Nợ</th>
                        <th class="text-center">Trạng Thái</th>
                        <th class="text-end">Chi Tiết</th>
                    </tr>
                </thead>
                <tbody>
                    <?php if (empty($list)): ?>
                        <tr><td colspan="9" class="text-center py-5 text-muted">Hiện tại không có hồ sơ thôi việc nào.</td></tr>
                    <?php else: foreach ($list as $item): 
                        $percent = round(($item['so_buoc_hoan_thanh'] / 6) * 100);
                        $isCompleted = ($item['trang_thai'] === 'HOAN_TAT');
                    ?>
                        <tr>
                            <td>
                                <span class="badge bg-light text-danger border border-danger-subtle font-monospace">
                                    <?= e($item['ma_ho_so']) ?>
                                </span>
                            </td>
                            <td>
                                <a href="<?= url('offboarding/detail/' . $item['id']) ?>" class="fw-bold text-decoration-none text-dark">
                                    <?= e($item['HoTen']) ?>
                                </a>
                                <div class="font-monospace small text-muted"><?= e($item['ma_nv']) ?></div>
                            </td>
                            <td>
                                <div><?= e($item['TenDV'] ?? $item['MaDV']) ?></div>
                                <small class="text-muted"><?= e($item['TenCV'] ?? '') ?></small>
                            </td>
                            <td>
                                <span class="text-danger fw-semibold"><?= format_date($item['ngay_lam_viec_cuoi']) ?></span>
                            </td>
                            <td>
                                <span class="small text-muted text-truncate d-inline-block" style="max-width: 200px;">
                                    <?= e($item['ly_do_nghi']) ?>
                                </span>
                            </td>
                            <td style="min-width: 140px;">
                                <div class="d-flex justify-content-between small text-muted mb-1">
                                    <span><?= $item['so_buoc_hoan_thanh'] ?>/6 bước</span>
                                    <span><?= $percent ?>%</span>
                                </div>
                                <div class="progress" style="height: 6px;">
                                    <div class="progress-bar <?= $isCompleted ? 'bg-success' : 'bg-danger' ?>" style="width: <?= $percent ?>%;"></div>
                                </div>
                            </td>
                            <td>
                                <?php if (!empty($item['tong_tien_khau_tru_ts']) && (float)$item['tong_tien_khau_tru_ts'] > 0): ?>
                                    <span class="badge bg-danger-subtle text-danger border border-danger-subtle">
                                        -<?= number_format((float)$item['tong_tien_khau_tru_ts'], 0, ',', '.') ?>đ
                                    </span>
                                <?php else: ?>
                                    <span class="small text-muted">Không nợ TS</span>
                                <?php endif; ?>
                            </td>
                            <td class="text-center">
                                <?php if ($isCompleted): ?>
                                    <span class="badge bg-success text-white"><i class="bi bi-check-all me-1"></i> HOÀN TẤT</span>
                                <?php else: ?>
                                    <span class="badge bg-warning text-dark"><i class="bi bi-hourglass-split me-1"></i> ĐANG XỬ LÝ</span>
                                <?php endif; ?>
                            </td>
                            <td class="text-end">
                                <a href="<?= url('offboarding/detail/' . $item['id']) ?>" class="btn btn-outline-primary btn-sm py-1 px-2">
                                    <i class="bi bi-eye me-1"></i> Xử Lý
                                </a>
                            </td>
                        </tr>
                    <?php endforeach; endif; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>

<!-- Modal Khởi Tạo Hồ Sơ Thôi Việc Mới -->
<div class="modal fade" id="modalCreateOffboarding" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog">
        <form action="<?= url('offboarding/create') ?>" method="POST" class="modal-content">
            <?= csrf_field() ?>

            <div class="modal-header bg-light">
                <h5 class="modal-title fw-bold text-dark"><i class="bi bi-box-arrow-right text-danger me-2"></i>Khởi Tạo Hồ Sơ Thôi Việc Mới</h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>

            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Mã nhân sự <span class="text-danger">*</span></label>
                    <input type="text" name="ma_nv" class="form-control form-control-sm" placeholder="VD: VNM-0003" required>
                </div>

                <div class="row g-2 mb-3">
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Ngày nhận đề xuất</label>
                        <input type="date" name="ngay_de_xuat" class="form-control form-control-sm" value="<?= date('Y-m-d') ?>" required>
                    </div>
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Ngày làm việc cuối <span class="text-danger">*</span></label>
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
                <button type="submit" class="btn btn-danger btn-sm fw-semibold">Bắt Đầu Quy Trình 6 Bước</button>
            </div>
        </form>
    </div>
</div>

<?php
require_once __DIR__ . '/../layouts/footer.php';
?>
