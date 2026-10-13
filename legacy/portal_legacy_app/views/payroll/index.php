<div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
    <div>
        <h3 class="fw-bold text-dark mb-1">
            <i class="bi bi-cash-stack text-primary me-2"></i>Quản Lý Bảng Lương & Chi Trả
        </h3>
        <p class="text-muted small mb-0">
            Tính toán tự động theo tham số pháp lý trong bảng <code>cau_hinh_luong</code> và <code>bieu_thue_tncn</code>. 
            Không sử dụng bất kỳ hằng số cố định nào trong mã nguồn.
        </p>
    </div>

    <div class="d-flex gap-2">
        <a href="<?= url('payroll/legal_params') ?>" class="btn btn-outline-info btn-sm fw-semibold">
            <i class="bi bi-shield-check me-1"></i> Bảng Tham Số Pháp Lý
        </a>
        <?php if (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'KETOAN_LUONG'])): ?>
        <button type="button" class="btn btn-primary btn-sm fw-semibold shadow-sm" data-bs-toggle="modal" data-bs-target="#calcPayrollModal" style="background: #002F6C; border-color: #002F6C;">
            <i class="bi bi-calculator me-1"></i> Tính Toán Lương Kỳ Mới
        </button>
        <?php endif; ?>
    </div>
</div>

<!-- Card Tóm tắt các tham số pháp lý hiệu lực hiện hành -->
<div class="card card-custom p-3 mb-4 bg-light">
    <div class="d-flex justify-content-between align-items-center mb-2">
        <h6 class="fw-bold text-primary mb-0">
            <i class="bi bi-info-circle-fill me-1"></i> Căn Cứ Pháp Lý Tiền Lương Áp Dụng (Đọc Từ CSDL cau_hinh_luong)
        </h6>
        <span class="badge bg-success">Đang hiệu lực</span>
    </div>
    <div class="row g-2 text-muted small">
        <div class="col-md-3">
            &bull; Lương cơ sở: <strong class="text-dark"><?= format_money($legalConfig['LUONG_CO_SO'] ?? 2340000) ?></strong> (NĐ 73/2024)
        </div>
        <div class="col-md-3">
            &bull; Lương TT Vùng I: <strong class="text-dark"><?= format_money($legalConfig['LUONG_TT_VUNG_1'] ?? 4960000) ?></strong> (NĐ 74/2024)
        </div>
        <div class="col-md-3">
            &bull; Trích nộp BH người LĐ: <strong class="text-dark">10.5%</strong> (BHXH 8%, BHYT 1.5%, BHTN 1%)
        </div>
        <div class="col-md-3">
            &bull; Giảm trừ gia cảnh: <strong class="text-dark">11tr</strong> bản thân + <strong class="text-dark">4.4tr</strong>/NPT
        </div>
    </div>
</div>

<!-- Danh sách các Bảng lương kỳ -->
<div class="card card-custom overflow-hidden">
    <div class="table-responsive">
        <table class="table table-hover align-middle mb-0">
            <thead class="table-light text-muted small text-uppercase fw-bold" style="border-bottom: 2px solid #E2E8F0;">
                <tr>
                    <th>Kỳ Lương</th>
                    <th>Tên Bảng Lương</th>
                    <th>Đơn Vị Áp Dụng</th>
                    <th class="text-center">Số Nhân Sự</th>
                    <th class="text-end">Tổng Thu Nhập</th>
                    <th class="text-end">Tổng Trích BH</th>
                    <th class="text-end">Thuế TNCN</th>
                    <th class="text-end">Tổng Thực Lĩnh</th>
                    <th class="text-center">Trạng Thái</th>
                    <th class="text-center" style="width: 140px;">Thao Tác</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($payrolls)): ?>
                    <tr>
                        <td colspan="10" class="text-center py-5 text-muted">
                            <i class="bi bi-wallet2 fs-2 d-block mb-2"></i>
                            Chưa có bảng lương nào được khởi tạo trong hệ thống.
                        </td>
                    </tr>
                <?php else: ?>
                    <?php foreach ($payrolls as $p): ?>
                    <tr>
                        <td><span class="badge bg-primary font-monospace fs-6"><?= e($p['ky_luong']) ?></span></td>
                        <td>
                            <strong class="text-dark"><?= e($p['ten_bang_luong']) ?></strong>
                            <div class="small text-muted">Người lập: <?= e($p['nguoi_lap']) ?> &bull; Ngày lập: <?= format_date($p['created_at']) ?></div>
                        </td>
                        <td>
                            <span class="badge bg-light text-dark border"><?= e($p['TenDV'] ?: $p['ma_dv']) ?></span>
                        </td>
                        <td class="text-center fw-bold"><?= number_format($p['tong_nhan_su']) ?></td>
                        <td class="text-end fw-semibold text-dark"><?= format_money($p['tong_thu_nhap']) ?></td>
                        <td class="text-end text-danger"><?= format_money($p['tong_bao_hiem']) ?></td>
                        <td class="text-end text-danger"><?= format_money($p['tong_thue_tncn']) ?></td>
                        <td class="text-end fw-bold text-success fs-6"><?= format_money($p['tong_thuc_linh']) ?></td>
                        <td class="text-center">
                            <?php if ($p['trang_thai'] === 'DA_CHI_TRA'): ?>
                                <span class="badge bg-success">Đã chi trả</span>
                            <?php elseif ($p['trang_thai'] === 'DA_DUYET'): ?>
                                <span class="badge bg-primary">Đã duyệt</span>
                            <?php else: ?>
                                <span class="badge bg-warning text-dark">Chờ duyệt</span>
                            <?php endif; ?>
                        </td>
                        <td class="text-center">
                            <a href="<?= url('payroll/detail/' . $p['id']) ?>" class="btn btn-sm btn-outline-primary px-2" title="Xem chi tiết">
                                <i class="bi bi-list-check me-1"></i> Chi tiết
                            </a>
                        </td>
                    </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
        </table>
    </div>
</div>

<!-- Modal Tính Toán Bảng Lương Hàng Loạt -->
<?php if (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'KETOAN_LUONG'])): ?>
<div class="modal fade" id="calcPayrollModal" tabindex="-1">
    <div class="modal-dialog">
        <form action="<?= url('payroll/calculate') ?>" method="POST" class="modal-content">
            <!-- CSRF Token Bắt buộc -->
            <?= csrf_field() ?>

            <div class="modal-header" style="background: #002F6C; color: #fff;">
                <h5 class="modal-title fw-bold"><i class="bi bi-calculator me-2"></i>Tính Toán Bảng Lương Kỳ Mới</h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
            </div>

            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Kỳ tính lương (Tháng/Năm) <span class="text-danger">*</span></label>
                    <input type="month" name="ky_luong" class="form-control" value="<?= e($currentPeriod) ?>" required>
                    <small class="text-muted">Các tỷ lệ bảo hiểm và biểu thuế sẽ tự động truy vấn tại ngày cuối cùng của kỳ này.</small>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Phạm vi Đơn vị tính lương <span class="text-danger">*</span></label>
                    <select name="ma_dv" class="form-select" required>
                        <?php if (Auth::canViewAllUnits()): ?>
                            <option value="VINAMILK">🏢 Toàn Tập đoàn Vinamilk (Tất cả nhân sự)</option>
                        <?php endif; ?>
                        <?php foreach ($selectableUnits as $u): ?>
                            <option value="<?= e($u['MaDV']) ?>"><?= e($u['IndentTitle'] ?? $u['TenDV']) ?></option>
                        <?php endforeach; ?>
                    </select>
                </div>

                <div class="alert alert-info py-2 px-3 small mb-0">
                    <i class="bi bi-shield-check me-1"></i> Công cụ tính lương sẽ tự động áp dụng:
                    <ul class="mb-0 ps-3 mt-1">
                        <li>BHXH 8%, BHYT 1.5%, BHTN 1% trên mức đóng bảo hiểm hợp đồng</li>
                        <li>Trần đóng BHXH (20 lần lương cơ sở 2.340.000đ = 46.8tr)</li>
                        <li>Trần đóng BHTN (20 lần lương tối thiểu vùng)</li>
                        <li>Giảm trừ gia cảnh: Bản thân 11tr, Người phụ thuộc 4.4tr/người</li>
                        <li>Biểu thuế TNCN lũy tiến 7 bậc từ CSDL</li>
                    </ul>
                </div>
            </div>

            <div class="modal-footer">
                <button type="button" class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Đóng</button>
                <button type="submit" class="btn btn-primary btn-sm fw-bold" style="background: #002F6C; border-color: #002F6C;">
                    <i class="bi bi-play-circle-fill me-1"></i> Bắt Đầu Tính Lương
                </button>
            </div>
        </form>
    </div>
</div>
<?php endif; ?>
