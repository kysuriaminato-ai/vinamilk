<?php
$hasAdvancedFilters = !empty($filters['gioi_tinh']) || !empty($filters['age_min']) || !empty($filters['age_max']) || !empty($filters['trinh_do']) || !empty($filters['seniority']);
$exportUrl = url('employees/export_excel?' . http_build_query($_GET));
?>

<div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
    <div>
        <h3 class="fw-bold text-dark mb-1">
            <i class="bi bi-people-fill text-primary me-2"></i>Quản Lý Hồ Sơ Nhân Sự
        </h3>
        <p class="text-muted small mb-0">
            Dữ liệu phân quyền đệ quy theo cây <strong>scopeByUnit()</strong>. 
            Tổng số: <strong class="text-primary"><?= number_format($pagination['total']) ?></strong> nhân sự.
        </p>
    </div>

    <div class="d-flex gap-2 no-print">
        <!-- Nút Xuất Excel -->
        <a href="<?= $exportUrl ?>" class="btn btn-outline-success btn-sm fw-semibold" title="Xuất danh sách ra file Excel">
            <i class="bi bi-file-earmark-excel-fill me-1"></i> Xuất Excel
        </a>

        <button onclick="window.print()" class="btn btn-outline-secondary btn-sm fw-semibold">
            <i class="bi bi-printer-fill me-1"></i> In Danh Sách
        </button>

        <?php if (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI'])): ?>
        <a href="<?= url('employees/wizard') ?>" class="btn btn-primary btn-sm fw-semibold" style="background: #0033A0; border-color: #0033A0;">
            <i class="bi bi-person-plus-fill me-1"></i> Tiếp Nhận Nhân Sự Mới (Wizard)
        </a>
        <?php endif; ?>
    </div>
</div>

<!-- Header tiêu đề dành riêng cho bản in A4 -->
<div class="d-none d-print-block mb-4 text-center">
    <h4 class="fw-bold text-uppercase mb-1">TẬP ĐOÀN VINAMILK - DANH SÁCH CÁN BỘ CÔNG NHÂN VIÊN</h4>
    <p class="small text-muted mb-0">Ngày trích xuất: <?= date('d/m/Y H:i') ?> &bull; Người trích xuất: <?= e(Auth::user()['FullName'] ?? 'Admin') ?></p>
</div>

<!-- Khung Tìm kiếm & Lọc Đa Chiều -->
<div class="card card-custom p-3 mb-4 no-print border-0 shadow-sm" style="border-radius: 8px;">
    <form action="<?= url('employees') ?>" method="GET" id="searchFilterForm">
        <!-- Hàng 1: Bộ lọc chính -->
        <div class="row g-2 align-items-end mb-2">
            <div class="col-lg-3 col-md-6">
                <label class="form-label small fw-semibold text-muted mb-1">Tìm kiếm từ khóa</label>
                <div class="input-group input-group-sm">
                    <span class="input-group-text bg-light border-end-0"><i class="bi bi-search text-muted"></i></span>
                    <input type="text" name="keyword" class="form-control border-start-0" placeholder="Mã NV, Họ tên, CCCD, Email..." value="<?= e($filters['keyword'] ?? '') ?>">
                </div>
            </div>

            <div class="col-lg-3 col-md-6">
                <label class="form-label small fw-semibold text-muted mb-1">Đơn vị trực thuộc (Cây tổ chức)</label>
                <select name="ma_dv" class="form-select form-select-sm">
                    <option value="ALL">-- Tất cả đơn vị trong phạm vi --</option>
                    <?php foreach ($selectableUnits as $u): ?>
                        <option value="<?= e($u['MaDV']) ?>" <?= (($filters['ma_dv'] ?? '') === $u['MaDV']) ? 'selected' : '' ?>>
                            <?= e($u['IndentTitle'] ?? $u['TenDV']) ?>
                        </option>
                    <?php endforeach; ?>
                </select>
            </div>

            <div class="col-lg-2 col-md-4">
                <label class="form-label small fw-semibold text-muted mb-1">Chức vụ</label>
                <select name="ma_cv" class="form-select form-select-sm">
                    <option value="">-- Tất cả chức vụ --</option>
                    <?php if (!empty($positions)): foreach ($positions as $pos): ?>
                        <option value="<?= e($pos['MaCV']) ?>" <?= (($filters['ma_cv'] ?? '') === $pos['MaCV']) ? 'selected' : '' ?>>
                            <?= e($pos['TenCV']) ?>
                        </option>
                    <?php endforeach; endif; ?>
                </select>
            </div>

            <div class="col-lg-2 col-md-4">
                <label class="form-label small fw-semibold text-muted mb-1">Trạng thái làm việc</label>
                <select name="trang_thai" class="form-select form-select-sm">
                    <option value="">-- Tất cả trạng thái --</option>
                    <option value="1" <?= (($filters['trang_thai'] ?? '') === '1') ? 'selected' : '' ?>>Đang làm việc</option>
                    <option value="2" <?= (($filters['trang_thai'] ?? '') === '2') ? 'selected' : '' ?>>Thử việc</option>
                    <option value="3" <?= (($filters['trang_thai'] ?? '') === '3') ? 'selected' : '' ?>>Nghỉ việc / Thôi việc</option>
                    <option value="4" <?= (($filters['trang_thai'] ?? '') === '4') ? 'selected' : '' ?>>Nghỉ hưu</option>
                </select>
            </div>

            <div class="col-lg-2 col-md-4">
                <label class="form-label small fw-semibold text-muted mb-1">Loại hợp đồng</label>
                <select name="loai_hd" class="form-select form-select-sm">
                    <option value="">-- Tất cả loại HĐ --</option>
                    <option value="THU_VIEC" <?= (($filters['loai_hd'] ?? '') === 'THU_VIEC') ? 'selected' : '' ?>>Thử việc</option>
                    <option value="XAC_DINH_THOI_HAN" <?= (str_contains($filters['loai_hd'] ?? '', 'XAC_DINH_THOI_HAN')) ? 'selected' : '' ?>>Xác định thời hạn</option>
                    <option value="KHONG_XAC_DINH_THOI_HAN" <?= (($filters['loai_hd'] ?? '') === 'KHONG_XAC_DINH_THOI_HAN') ? 'selected' : '' ?>>Không xác định thời hạn</option>
                </select>
            </div>
        </div>

        <!-- Hàng 2: Tìm kiếm nâng cao (Collapse) -->
        <div class="collapse <?= $hasAdvancedFilters ? 'show' : '' ?> pt-2 border-top mt-2" id="advancedFilterSection">
            <div class="row g-2 align-items-center">
                <div class="col-md-2">
                    <label class="form-label small fw-semibold text-muted mb-1">Giới tính</label>
                    <select name="gioi_tinh" class="form-select form-select-sm">
                        <option value="">-- Cả hai --</option>
                        <option value="Nam" <?= (($filters['gioi_tinh'] ?? '') === 'Nam') ? 'selected' : '' ?>>Nam</option>
                        <option value="Nữ" <?= (($filters['gioi_tinh'] ?? '') === 'Nữ') ? 'selected' : '' ?>>Nữ</option>
                    </select>
                </div>

                <div class="col-md-3">
                    <label class="form-label small fw-semibold text-muted mb-1">Độ tuổi (Từ - Đến)</label>
                    <div class="d-flex gap-1 align-items-center">
                        <input type="number" name="age_min" min="18" max="70" class="form-control form-control-sm" placeholder="Từ tuổi" value="<?= e($filters['age_min'] ?? '') ?>">
                        <span class="text-muted">-</span>
                        <input type="number" name="age_max" min="18" max="70" class="form-control form-control-sm" placeholder="Đến tuổi" value="<?= e($filters['age_max'] ?? '') ?>">
                    </div>
                </div>

                <div class="col-md-2">
                    <label class="form-label small fw-semibold text-muted mb-1">Trình độ học vấn</label>
                    <select name="trinh_do" class="form-select form-select-sm">
                        <option value="">-- Tất cả --</option>
                        <option value="Đại học" <?= (($filters['trinh_do'] ?? '') === 'Đại học') ? 'selected' : '' ?>>Đại học</option>
                        <option value="Thạc sĩ" <?= (($filters['trinh_do'] ?? '') === 'Thạc sĩ') ? 'selected' : '' ?>>Thạc sĩ</option>
                        <option value="Tiến sĩ" <?= (($filters['trinh_do'] ?? '') === 'Tiến sĩ') ? 'selected' : '' ?>>Tiến sĩ</option>
                        <option value="Cao đẳng" <?= (($filters['trinh_do'] ?? '') === 'Cao đẳng') ? 'selected' : '' ?>>Cao đẳng</option>
                        <option value="Trung cấp" <?= (($filters['trinh_do'] ?? '') === 'Trung cấp') ? 'selected' : '' ?>>Trung cấp</option>
                        <option value="THPT" <?= (($filters['trinh_do'] ?? '') === 'THPT') ? 'selected' : '' ?>>THPT</option>
                    </select>
                </div>

                <div class="col-md-2">
                    <label class="form-label small fw-semibold text-muted mb-1">Thâm niên công tác</label>
                    <select name="seniority" class="form-select form-select-sm">
                        <option value="">-- Tất cả --</option>
                        <option value="under_1" <?= (($filters['seniority'] ?? '') === 'under_1') ? 'selected' : '' ?>>Dưới 1 năm</option>
                        <option value="1_to_3" <?= (($filters['seniority'] ?? '') === '1_to_3') ? 'selected' : '' ?>>1 - 3 năm</option>
                        <option value="3_to_5" <?= (($filters['seniority'] ?? '') === '3_to_5') ? 'selected' : '' ?>>3 - 5 năm</option>
                        <option value="over_5" <?= (($filters['seniority'] ?? '') === 'over_5') ? 'selected' : '' ?>>Trên 5 năm</option>
                    </select>
                </div>

                <div class="col-md-3">
                    <label class="form-label small fw-semibold text-muted mb-1">Cơ chế Logic nâng cao</label>
                    <div class="btn-group btn-group-sm w-100" role="group">
                        <input type="radio" class="btn-check" name="advanced_logic" id="logic_and" value="AND" <?= (($filters['advanced_logic'] ?? 'AND') === 'AND') ? 'checked' : '' ?>>
                        <label class="btn btn-outline-primary" for="logic_and" title="Thỏa mãn TẤT CẢ các tiêu chí nâng cao">AND (Đồng thời)</label>

                        <input type="radio" class="btn-check" name="advanced_logic" id="logic_or" value="OR" <?= (($filters['advanced_logic'] ?? '') === 'OR') ? 'checked' : '' ?>>
                        <label class="btn btn-outline-primary" for="logic_or" title="Thỏa mãn BẤT KỲ tiêu chí nào trong nhóm nâng cao">OR (Hoặc)</label>
                    </div>
                </div>
            </div>
        </div>

        <!-- Nút tác vụ lọc -->
        <div class="d-flex justify-content-between align-items-center mt-3 pt-2 border-top">
            <button class="btn btn-link btn-sm text-decoration-none px-0" type="button" data-bs-toggle="collapse" data-bs-target="#advancedFilterSection" aria-expanded="<?= $hasAdvancedFilters ? 'true' : 'false' ?>">
                <i class="bi bi-sliders me-1"></i> <?= $hasAdvancedFilters ? 'Ẩn bộ lọc nâng cao' : 'Mở rộng tìm kiếm nâng cao (Tuổi, Trình độ, Thâm niên, AND/OR)' ?>
            </button>

            <div class="d-flex gap-2">
                <a href="<?= url('employees') ?>" class="btn btn-light btn-sm border" title="Đặt lại bộ lọc mặc định">
                    <i class="bi bi-arrow-counterclockwise me-1"></i> Đặt lại
                </a>
                <button type="submit" class="btn btn-primary btn-sm px-4 fw-semibold" style="background: #0033A0; border-color: #0033A0;">
                    <i class="bi bi-funnel-fill me-1"></i> Áp Dụng Lọc
                </button>
            </div>
        </div>
    </form>
</div>

<!-- Bảng danh sách nhân sự -->
<div class="card card-custom overflow-hidden border-0 shadow-sm" style="border-radius: 8px;">
    <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
            <thead style="background: #F8FAFC; color: #475569; font-size: 0.8rem; text-transform: uppercase; border-bottom: 2px solid #E2E8F0;">
                <tr>
                    <th style="width: 45px;" class="text-center">STT</th>
                    <th style="width: 100px;">Mã NV</th>
                    <th>Họ và Tên</th>
                    <th style="width: 80px;" class="text-center">Tuổi</th>
                    <th>Đơn Vị Công Tác</th>
                    <th>Chức Danh</th>
                    <th>Hợp Đồng</th>
                    <th>Thâm Niên</th>
                    <th class="text-center">Trạng Thái</th>
                    <th class="text-end">Lương Đóng BH</th>
                    <th class="text-center no-print" style="width: 110px;">Thao Tác</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($employees)): ?>
                    <tr>
                        <td colspan="11" class="text-center py-5 text-muted">
                            <i class="bi bi-inbox fs-1 d-block mb-2 text-secondary"></i>
                            Không tìm thấy hồ sơ nhân sự nào phù hợp với điều kiện tìm kiếm và phạm vi đơn vị.
                        </td>
                    </tr>
                <?php else: ?>
                    <?php 
                        $stt = $pagination['offset'] + 1;
                        foreach ($employees as $emp): 
                            $statusInfo = match ((int)$emp['TrangThai']) {
                                1 => ['label' => 'Đang làm việc', 'class' => 'bg-success-subtle text-success border border-success-subtle'],
                                2 => ['label' => 'Thử việc', 'class' => 'bg-warning-subtle text-warning border border-warning-subtle'],
                                3 => ['label' => 'Nghỉ việc', 'class' => 'bg-danger-subtle text-danger border border-danger-subtle'],
                                4 => ['label' => 'Nghỉ hưu', 'class' => 'bg-secondary-subtle text-secondary border border-secondary-subtle'],
                                default => ['label' => 'Khác', 'class' => 'bg-light text-dark border']
                            };
                    ?>
                    <tr>
                        <td class="text-center text-muted small"><?= $stt++ ?></td>
                        <td>
                            <a href="<?= url('employees/show/' . $emp['MaNV']) ?>" class="fw-bold font-monospace text-decoration-none" style="color: #0033A0;">
                                <?= e($emp['MaNV']) ?>
                            </a>
                        </td>
                        <td>
                            <div class="fw-bold text-dark"><?= e($emp['HoTen']) ?></div>
                            <small class="text-muted d-block">
                                <i class="bi bi-person-vcard me-1"></i><?= e($emp['SoCMND_CCCD']) ?> &bull; <?= e($emp['Email'] ?: 'Chưa có email') ?>
                            </small>
                        </td>
                        <td class="text-center">
                            <span class="badge bg-light text-secondary border"><?= $emp['Tuoi'] ?? '-' ?></span>
                        </td>
                        <td>
                            <span class="badge bg-light text-dark border fw-normal"><?= e($emp['TenDV'] ?: $emp['MaDV']) ?></span>
                        </td>
                        <td>
                            <div class="fw-semibold text-dark"><?= e($emp['TenCV'] ?: ($emp['ChucVu'] ?? 'Nhân viên')) ?></div>
                            <small class="text-muted"><?= e($emp['TrinhDoHocVan'] ?: '') ?></small>
                        </td>
                        <td>
                            <?php if (!empty($emp['LoaiHopDong'])): ?>
                                <span class="badge bg-primary-subtle text-primary border border-primary-subtle" style="font-size: 0.72rem;">
                                    <?= e($emp['LoaiHopDong']) ?>
                                </span>
                                <?php if (!empty($emp['SoNgayConLaiHD']) && $emp['SoNgayConLaiHD'] <= 60 && $emp['SoNgayConLaiHD'] >= 0): ?>
                                    <span class="badge bg-warning text-dark d-block mt-1" style="font-size: 0.68rem;" title="Hết hạn trong <?= $emp['SoNgayConLaiHD'] ?> ngày">
                                        <i class="bi bi-clock-history"></i> Còn <?= $emp['SoNgayConLaiHD'] ?> ngày
                                    </span>
                                <?php endif; ?>
                            <?php else: ?>
                                <span class="text-muted small">Chưa có HĐ</span>
                            <?php endif; ?>
                        </td>
                        <td>
                            <span class="small text-muted"><?= ($emp['ThamNienNam'] ?? 0) ?> năm</span>
                        </td>
                        <td class="text-center">
                            <span class="badge <?= $statusInfo['class'] ?> px-2 py-1"><?= $statusInfo['label'] ?></span>
                        </td>
                        <td class="text-end fw-semibold text-dark">
                            <?= is_numeric($emp['LuongBaoHiem']) ? format_money($emp['LuongBaoHiem']) : e($emp['LuongBaoHiem']) ?>
                        </td>
                        <td class="text-center no-print">
                            <a href="<?= url('employees/show/' . $emp['MaNV']) ?>" class="btn btn-sm btn-outline-primary py-1 px-2" title="Xem đầy đủ 8 tab hồ sơ">
                                <i class="bi bi-folder2-open me-1"></i> Hồ sơ
                            </a>
                        </td>
                    </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
        </table>
    </div>

    <!-- Phân trang Pagination -->
    <?php if ($pagination['total_pages'] > 1): ?>
    <div class="card-footer bg-white d-flex justify-content-between align-items-center py-3 border-top no-print flex-wrap gap-2">
        <small class="text-muted">
            Hiển thị từ <strong><?= $pagination['offset'] + 1 ?></strong> đến <strong><?= min($pagination['offset'] + $pagination['limit'], $pagination['total']) ?></strong> trên tổng số <strong><?= number_format($pagination['total']) ?></strong> nhân sự
        </small>
        
        <ul class="pagination pagination-sm mb-0">
            <?php 
                $startP = max(1, $pagination['current_page'] - 2);
                $endP   = min($pagination['total_pages'], $pagination['current_page'] + 2);

                if ($pagination['current_page'] > 1): 
                    $prevParams = $_GET; $prevParams['page'] = $pagination['current_page'] - 1;
            ?>
                <li class="page-item"><a class="page-link" href="<?= url('employees?' . http_build_query($prevParams)) ?>">&laquo;</a></li>
            <?php endif; ?>

            <?php for ($p = $startP; $p <= $endP; $p++): 
                $queryParams = $_GET;
                $queryParams['page'] = $p;
                $pageUrl = url('employees?' . http_build_query($queryParams));
            ?>
                <li class="page-item <?= ($p == $pagination['current_page']) ? 'active' : '' ?>">
                    <a class="page-link" href="<?= $pageUrl ?>"><?= $p ?></a>
                </li>
            <?php endfor; ?>

            <?php if ($pagination['current_page'] < $pagination['total_pages']): 
                $nextParams = $_GET; $nextParams['page'] = $pagination['current_page'] + 1;
            ?>
                <li class="page-item"><a class="page-link" href="<?= url('employees?' . http_build_query($nextParams)) ?>">&raquo;</a></li>
            <?php endif; ?>
        </ul>
    </div>
    <?php endif; ?>
</div>
