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
                    <li class="breadcrumb-item"><a href="<?= url('assets') ?>" class="text-decoration-none">Tài sản & PPE</a></li>
                    <li class="breadcrumb-item active">Danh mục tiêu chuẩn theo vị trí</li>
                </ol>
            </nav>
            <h4 class="fw-bold text-dark mb-0">
                <i class="bi bi-shield-shaded text-primary me-2"></i>Danh Mục Tiêu Chuẩn Trang Bị PPE & Tài Sản Theo Vị Trí
            </h4>
            <small class="text-muted">Quy định định mức cấp phát chuẩn Vinamilk: Nhà máy (Ủng, Áo, Mũ, Găng) vs Văn phòng (Laptop, Thẻ từ) vs Trang trại</small>
        </div>
        <div>
            <a href="<?= url('assets') ?>" class="btn btn-outline-secondary btn-sm me-2">
                <i class="bi bi-arrow-left me-1"></i> Quay Lại Quản Lý Tài Sản
            </a>
        </div>
    </div>

    <!-- Bộ lọc vị trí -->
    <div class="mb-4 d-flex gap-2">
        <a href="<?= url('assets/standards') ?>" class="btn btn-sm <?= empty($currentGroup) ? 'btn-primary' : 'btn-outline-secondary' ?>">
            Tất Cả Vị Trí
        </a>
        <a href="<?= url('assets/standards?group=NHA_MAY_SAN_XUAT') ?>" class="btn btn-sm <?= ($currentGroup === 'NHA_MAY_SAN_XUAT') ? 'btn-primary' : 'btn-outline-secondary' ?>">
            <i class="bi bi-building me-1"></i> 1. Nhà Máy Sản Xuất (Ủng, Áo, Mũ, Găng)
        </a>
        <a href="<?= url('assets/standards?group=VAN_PHONG_DIEU_HANH') ?>" class="btn btn-sm <?= ($currentGroup === 'VAN_PHONG_DIEU_HANH') ? 'btn-primary' : 'btn-outline-secondary' ?>">
            <i class="bi bi-laptop me-1"></i> 2. Văn Phòng Điều Hành (Laptop, Thẻ Từ)
        </a>
        <a href="<?= url('assets/standards?group=TRANG_TRAI_BO_SUA') ?>" class="btn btn-sm <?= ($currentGroup === 'TRANG_TRAI_BO_SUA') ? 'btn-primary' : 'btn-outline-secondary' ?>">
            <i class="bi bi-tree me-1"></i> 3. Trang Trại Bò Sữa
        </a>
    </div>

    <!-- Bảng danh mục tiêu chuẩn -->
    <div class="card border-0 shadow-sm rounded-3">
        <div class="table-responsive">
            <table class="table table-hover align-middle mb-0">
                <thead class="table-light small text-muted">
                    <tr>
                        <th>Nhóm Vị Trí</th>
                        <th>Loại Trang Bị</th>
                        <th>Tên Trang Bị / Tài Sản</th>
                        <th>Quy Cách / Tiêu Chuẩn Kỹ Thuật</th>
                        <th>Định Mức</th>
                        <th>Chu Kỳ Thay Thế</th>
                        <th>Đơn Giá Bồi Thường</th>
                        <th class="text-center">Tính Chất</th>
                    </tr>
                </thead>
                <tbody>
                    <?php foreach ($standards as $s): ?>
                        <tr>
                            <td>
                                <?php if ($s['nhom_vi_tri'] === 'NHA_MAY_SAN_XUAT'): ?>
                                    <span class="badge bg-warning-subtle text-dark border border-warning-subtle">
                                        <i class="bi bi-gear-fill me-1"></i> Nhà Máy Sản Xuất
                                    </span>
                                <?php elseif ($s['nhom_vi_tri'] === 'VAN_PHONG_DIEU_HANH'): ?>
                                    <span class="badge bg-primary-subtle text-primary border border-primary-subtle">
                                        <i class="bi bi-building me-1"></i> Văn Phòng Điều Hành
                                    </span>
                                <?php elseif ($s['nhom_vi_tri'] === 'TRANG_TRAI_BO_SUA'): ?>
                                    <span class="badge bg-success-subtle text-success border border-success-subtle">
                                        <i class="bi bi-flower1 me-1"></i> Trang Trại Bò Sữa
                                    </span>
                                <?php else: ?>
                                    <span class="badge bg-light text-dark border"><?= e($s['nhom_vi_tri']) ?></span>
                                <?php endif; ?>
                            </td>
                            <td>
                                <span class="badge bg-light text-secondary border"><?= e($s['loai_trang_bi']) ?></span>
                            </td>
                            <td class="fw-bold text-dark"><?= e($s['ten_trang_bi']) ?></td>
                            <td class="small text-muted"><?= e($s['quy_cach_tieu_chuan']) ?></td>
                            <td>
                                <span class="fw-bold text-primary"><?= $s['dinh_muc_so_luong'] ?></span> <?= e($s['don_vi_tinh']) ?>
                            </td>
                            <td><?= $s['chu_ky_thay_the_thang'] ?> tháng/lần</td>
                            <td class="fw-semibold text-danger">
                                <?= number_format($s['gia_tri_dinh_muc'], 0, ',', '.') ?> VNĐ
                            </td>
                            <td class="text-center">
                                <?php if ($s['bat_buoc']): ?>
                                    <span class="badge bg-danger text-white"><i class="bi bi-check-lg me-1"></i> Bắt Buộc</span>
                                <?php else: ?>
                                    <span class="badge bg-light text-muted border">Tùy Chọn</span>
                                <?php endif; ?>
                            </td>
                        </tr>
                    <?php endforeach; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>

<?php
require_once __DIR__ . '/../layouts/footer.php';
?>
