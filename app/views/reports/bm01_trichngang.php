<?php
/**
 * =====================================================================
 * VINAMILK HRM - Biểu mẫu 01: Danh Sách Trích Ngang Toàn Bộ CBNV
 * =====================================================================
 */
?>

<div class="mb-4">
    <div class="d-flex justify-content-between align-items-center">
        <div>
            <nav aria-label="breadcrumb">
                <ol class="breadcrumb mb-1">
                    <li class="breadcrumb-item"><a href="<?= BASE_URL ?>/baocao">Báo cáo</a></li>
                    <li class="breadcrumb-item active">Biểu mẫu 01</li>
                </ol>
            </nav>
            <h3 class="fw-bold text-vinamilk mb-1">
                <i class="bi bi-person-lines-fill me-2"></i>Biểu Mẫu 01: Danh Sách Trích Ngang CBNV
            </h3>
            <p class="text-muted small mb-0">Hồ sơ lý lịch trích ngang toàn thể Cán bộ công nhân viên Tập đoàn Vinamilk</p>
        </div>
        <div class="d-flex gap-2">
            <a href="<?= BASE_URL ?>/baocao/print_report?type=bm01&dept=<?= $current_dept ?>&status=<?= $current_status ?>&keyword=<?= urlencode($keyword) ?>" target="_blank" class="btn btn-outline-secondary rounded-pill">
                <i class="bi bi-printer me-1"></i>In A4 Ngang
            </a>
            <a href="<?= BASE_URL ?>/baocao/export_excel?type=bm01&dept=<?= $current_dept ?>&status=<?= $current_status ?>&keyword=<?= urlencode($keyword) ?>" class="btn btn-success rounded-pill">
                <i class="bi bi-file-earmark-excel me-1"></i>Xuất Excel (.xlsx)
            </a>
        </div>
    </div>
</div>

<!-- Bộ lọc tìm kiếm -->
<div class="card border-0 shadow-sm mb-4">
    <div class="card-body p-3">
        <form method="GET" action="<?= BASE_URL ?>/baocao/bm01_trichngang" class="row g-2 align-items-center">
            <div class="col-md-4">
                <div class="input-group">
                    <span class="input-group-text bg-white border-end-0"><i class="bi bi-search text-muted"></i></span>
                    <input type="text" name="keyword" class="form-control border-start-0" placeholder="Tìm theo Mã NV, Họ tên, CCCD..." value="<?= htmlspecialchars($keyword) ?>">
                </div>
            </div>
            <div class="col-md-3">
                <select name="dept" class="form-select">
                    <option value="">-- Tất cả Đơn vị / Phòng ban --</option>
                    <?php foreach ($departments as $d): ?>
                        <option value="<?= $d['id'] ?>" <?= $current_dept == $d['id'] ? 'selected' : '' ?>>
                            <?= htmlspecialchars($d['ten_phong_ban']) ?> (<?= htmlspecialchars($d['ma_phong_ban']) ?>)
                        </option>
                    <?php endforeach; ?>
                </select>
            </div>
            <div class="col-md-3">
                <select name="status" class="form-select">
                    <option value="">-- Tất cả Trạng thái --</option>
                    <option value="Đang làm việc" <?= $current_status == 'Đang làm việc' ? 'selected' : '' ?>>Đang làm việc</option>
                    <option value="Thử việc" <?= $current_status == 'Thử việc' ? 'selected' : '' ?>>Thử việc</option>
                    <option value="Đã nghỉ việc" <?= $current_status == 'Đã nghỉ việc' ? 'selected' : '' ?>>Đã nghỉ việc</option>
                    <option value="Tạm hoãn" <?= $current_status == 'Tạm hoãn' ? 'selected' : '' ?>>Tạm hoãn</option>
                </select>
            </div>
            <div class="col-md-2 d-flex gap-2">
                <button type="submit" class="btn btn-primary w-100 rounded-pill"><i class="bi bi-funnel me-1"></i>Lọc</button>
                <a href="<?= BASE_URL ?>/baocao/bm01_trichngang" class="btn btn-light border rounded-pill"><i class="bi bi-arrow-counterclockwise"></i></a>
            </div>
        </form>
    </div>
</div>

<!-- Bảng Trích Ngang -->
<div class="card border-0 shadow-sm mb-4">
    <div class="card-header bg-white py-3 d-flex justify-content-between align-items-center">
        <h6 class="fw-bold text-vinamilk mb-0">
            <i class="bi bi-table me-2"></i>Tổng số bản ghi: <span class="badge bg-primary rounded-pill"><?= number_format($pagination['total_records']) ?></span>
        </h6>
    </div>
    <div class="table-responsive">
        <table class="table table-hover table-striped align-middle mb-0 text-nowrap" style="font-size: 0.875rem;">
            <thead class="table-dark text-center align-middle">
                <tr>
                    <th style="width: 40px;">STT</th>
                    <th>Mã NV</th>
                    <th>Họ và Tên</th>
                    <th>Ngày sinh</th>
                    <th>Giới tính</th>
                    <th>Phòng Ban / Đơn vị</th>
                    <th>Chức vụ</th>
                    <th>Ngạch / Bậc</th>
                    <th>Trình độ chuyên môn</th>
                    <th>Quê quán</th>
                    <th>Ngày vào làm</th>
                    <th>Trạng thái</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($employees)): ?>
                    <tr>
                        <td colspan="12" class="text-center py-4 text-muted">
                            <i class="bi bi-inbox fs-2 d-block mb-2"></i>Không tìm thấy dữ liệu nhân sự phù hợp điều kiện lọc.
                        </td>
                    </tr>
                <?php else: ?>
                    <?php 
                    $stt = ($pagination['page'] - 1) * $pagination['limit'] + 1;
                    foreach ($employees as $emp): 
                    ?>
                        <tr>
                            <td class="text-center fw-bold text-muted"><?= $stt++ ?></td>
                            <td class="fw-bold text-vinamilk">
                                <a href="<?= BASE_URL ?>/nhansu/view/<?= $emp['id'] ?>" class="text-decoration-none">
                                    <?= htmlspecialchars($emp['ma_nv']) ?>
                                </a>
                            </td>
                            <td class="fw-bold"><?= htmlspecialchars($emp['ho_ten']) ?></td>
                            <td class="text-center"><?= !empty($emp['ngay_sinh']) ? date('d/m/Y', strtotime($emp['ngay_sinh'])) : 'N/A' ?></td>
                            <td class="text-center">
                                <?php if ($emp['gioi_tinh'] == 'Nam'): ?>
                                    <span class="badge bg-info-subtle text-info">Nam</span>
                                <?php else: ?>
                                    <span class="badge bg-danger-subtle text-danger">Nữ</span>
                                <?php endif; ?>
                            </td>
                            <td><?= htmlspecialchars($emp['ten_phong_ban'] ?? 'N/A') ?></td>
                            <td><?= htmlspecialchars($emp['ten_chuc_vu'] ?? 'N/A') ?></td>
                            <td class="text-center"><?= htmlspecialchars($emp['ma_ngach'] ?? 'CV01') ?></td>
                            <td><?= htmlspecialchars($emp['trinh_do_hoc_van'] ?? 'Đại học') ?></td>
                            <td><?= htmlspecialchars($emp['que_quan'] ?? 'Chưa cập nhật') ?></td>
                            <td class="text-center"><?= !empty($emp['ngay_vao_lam']) ? date('d/m/Y', strtotime($emp['ngay_vao_lam'])) : 'N/A' ?></td>
                            <td class="text-center">
                                <?php 
                                $statusClass = match($emp['trang_thai'] ?? 'Đang làm việc') {
                                    'Đang làm việc' => 'bg-success',
                                    'Thử việc' => 'bg-warning text-dark',
                                    'Đã nghỉ việc' => 'bg-danger',
                                    default => 'bg-secondary'
                                };
                                ?>
                                <span class="badge <?= $statusClass ?>"><?= htmlspecialchars($emp['trang_thai'] ?? 'Đang làm việc') ?></span>
                            </td>
                        </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
        </table>
    </div>

    <!-- Phân trang -->
    <?php if ($pagination['total_pages'] > 1): ?>
        <div class="card-footer bg-white py-3 d-flex justify-content-between align-items-center">
            <span class="small text-muted">Hiển thị Trang <?= $pagination['page'] ?> / <?= $pagination['total_pages'] ?></span>
            <ul class="pagination pagination-sm mb-0">
                <?php for ($p = 1; $p <= $pagination['total_pages']; $p++): ?>
                    <li class="page-item <?= $p == $pagination['page'] ? 'active' : '' ?>">
                        <a class="page-link" href="<?= BASE_URL ?>/baocao/bm01_trichngang?page=<?= $p ?>&dept=<?= $current_dept ?>&status=<?= $current_status ?>&keyword=<?= urlencode($keyword) ?>">
                            <?= $p ?>
                        </a>
                    </li>
                <?php endfor; ?>
            </ul>
        </div>
    <?php endif; ?>
</div>
