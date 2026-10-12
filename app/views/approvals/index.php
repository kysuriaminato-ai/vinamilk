<div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
    <div>
        <h3 class="fw-bold text-dark mb-1">
            <i class="bi bi-envelope-paper-fill text-primary me-2"></i>Hộp Thư Phê Duyệt & Đơn Từ (ESS)
        </h3>
        <p class="text-muted small mb-0">
            Quy trình phê duyệt trực tuyến giữa Cán bộ nhân viên và Quản lý trực tiếp (QUANLY_TRUCTIEP).
        </p>
    </div>

    <button type="button" class="btn btn-primary btn-sm fw-semibold shadow-sm" data-bs-toggle="modal" data-bs-target="#createRequestModal" style="background: #002F6C; border-color: #002F6C;">
        <i class="bi bi-plus-circle me-1"></i> Gửi Đơn Mới (Nghỉ Phép / Đề Xuất)
    </button>
</div>

<?php if ($isManager): ?>
<!-- KHỐI 1: DANH SÁCH ĐƠN CHỜ DUYỆT CỦA CẤP DƯỚI -->
<div class="card card-custom p-4 mb-4 border-start border-4 border-warning">
    <div class="d-flex justify-content-between align-items-center mb-3">
        <h5 class="fw-bold text-dark mb-0">
            <i class="bi bi-hourglass-split text-warning me-2"></i>Đơn Từ Chờ Bạn Phê Duyệt (<?= count($pendingApprovals) ?>)
        </h5>
        <span class="badge bg-warning text-dark">Quyền Quản lý Trực tiếp</span>
    </div>

    <?php if (empty($pendingApprovals)): ?>
        <div class="text-center py-4 text-muted small">
            <i class="bi bi-check2-all fs-2 text-success d-block mb-1"></i>
            Tuyệt vời! Hiện tại không có đơn từ nào tồn đọng cần phê duyệt.
        </div>
    <?php else: ?>
        <div class="table-responsive">
            <table class="table table-hover align-middle mb-0" style="font-size: 13px;">
                <thead class="table-light text-muted small text-uppercase">
                    <tr>
                        <th>Mã Đơn</th>
                        <th>Nhân Viên</th>
                        <th>Đơn Vị Trực Thuộc</th>
                        <th>Loại Đơn</th>
                        <th>Thời Gian</th>
                        <th>Lý Do</th>
                        <th class="text-center" style="width: 180px;">Thao Tác</th>
                    </tr>
                </thead>
                <tbody>
                    <?php foreach ($pendingApprovals as $p): ?>
                    <tr>
                        <td class="font-monospace fw-bold text-primary"><?= e($p['ma_don']) ?></td>
                        <td>
                            <strong class="text-dark"><?= e($p['HoTen']) ?></strong>
                            <div class="small text-muted font-monospace"><?= e($p['ma_nv']) ?></div>
                        </td>
                        <td><span class="badge bg-light text-dark border"><?= e($p['TenDV'] ?: $p['MaDV']) ?></span></td>
                        <td><span class="badge bg-info-subtle text-info border"><?= e($p['loai_don']) ?></span></td>
                        <td>
                            <?= format_date($p['ngay_bat_dau']) ?> &rarr; <?= format_date($p['ngay_ket_thuc']) ?>
                            <div class="small text-muted">(<?= e($p['so_ngay']) ?> ngày)</div>
                        </td>
                        <td><?= e($p['ly_do']) ?></td>
                        <td class="text-center">
                            <form action="<?= url('approvals/approve') ?>" method="POST" class="d-inline">
                                <?= csrf_field() ?>
                                <input type="hidden" name="id" value="<?= e($p['id']) ?>">
                                <button type="submit" class="btn btn-sm btn-success px-2 py-1 fw-bold" onclick="return confirm('Bạn có chắc chắn muốn duyệt đơn này?')">
                                    <i class="bi bi-check-lg"></i> Duyệt
                                </button>
                            </form>

                            <form action="<?= url('approvals/reject') ?>" method="POST" class="d-inline ms-1">
                                <?= csrf_field() ?>
                                <input type="hidden" name="id" value="<?= e($p['id']) ?>">
                                <button type="submit" class="btn btn-sm btn-outline-danger px-2 py-1" onclick="return confirm('Bạn có chắc chắn muốn từ chối đơn này?')">
                                    <i class="bi bi-x-lg"></i> Từ chối
                                </button>
                            </form>
                        </td>
                    </tr>
                    <?php endforeach; ?>
                </tbody>
            </table>
        </div>
    <?php endif; ?>
</div>
<?php endif; ?>

<!-- KHỐI 2: DANH SÁCH ĐƠN CỦA CHÍNH BẢN THÂN (ESS) -->
<div class="card card-custom p-4">
    <h5 class="fw-bold text-dark mb-3">
        <i class="bi bi-person-lines-fill text-primary me-2"></i>Lịch Sử Đơn Từ Cá Nhân Đã Gửi
    </h5>

    <?php if (empty($myRequests)): ?>
        <div class="text-center py-4 text-muted small">
            Bạn chưa gửi đơn từ nào trong hệ thống.
        </div>
    <?php else: ?>
        <div class="table-responsive">
            <table class="table table-hover align-middle mb-0" style="font-size: 13px;">
                <thead class="table-light text-muted small text-uppercase">
                    <tr>
                        <th>Mã Đơn</th>
                        <th>Loại Đơn</th>
                        <th>Thời Gian Đề Xuất</th>
                        <th>Số Ngày</th>
                        <th>Lý Do</th>
                        <th>Trạng Thái</th>
                        <th>Ý Kiến Quản Lý</th>
                    </tr>
                </thead>
                <tbody>
                    <?php foreach ($myRequests as $r): ?>
                    <tr>
                        <td class="font-monospace fw-bold text-primary"><?= e($r['ma_don']) ?></td>
                        <td><span class="badge bg-light text-dark border"><?= e($r['loai_don']) ?></span></td>
                        <td><?= format_date($r['ngay_bat_dau']) ?> &rarr; <?= format_date($r['ngay_ket_thuc']) ?></td>
                        <td class="fw-bold"><?= e($r['so_ngay']) ?></td>
                        <td><?= e($r['ly_do']) ?></td>
                        <td>
                            <?php if ($r['trang_thai'] === 'DA_DUYET'): ?>
                                <span class="badge bg-success">Đã phê duyệt</span>
                            <?php elseif ($r['trang_thai'] === 'TU_CHOI'): ?>
                                <span class="badge bg-danger">Bị từ chối</span>
                            <?php else: ?>
                                <span class="badge bg-warning text-dark">Đang chờ duyệt</span>
                            <?php endif; ?>
                        </td>
                        <td><small class="text-muted"><?= e($r['y_kien_duyet'] ?: 'Chưa có ý kiến') ?></small></td>
                    </tr>
                    <?php endforeach; ?>
                </tbody>
            </table>
        </div>
    <?php endif; ?>
</div>

<!-- Modal Gửi đơn mới -->
<div class="modal fade" id="createRequestModal" tabindex="-1">
    <div class="modal-dialog">
        <form action="<?= url('approvals/create') ?>" method="POST" class="modal-content">
            <!-- CSRF Token Bắt buộc -->
            <?= csrf_field() ?>

            <div class="modal-header" style="background: #002F6C; color: #fff;">
                <h5 class="modal-title fw-bold"><i class="bi bi-file-earmark-plus me-2"></i>Tạo Đơn Tự Phục Vụ (ESS)</h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
            </div>

            <div class="modal-body">
                <div class="mb-3">
                    <label class="form-label small fw-semibold">Loại đơn đề xuất <span class="text-danger">*</span></label>
                    <select name="loai_don" class="form-select" required>
                        <option value="NGHI_PHEP">Đơn xin nghỉ phép năm / việc riêng</option>
                        <option value="TANG_LUONG">Tờ trình đề xuất nâng bậc lương</option>
                        <option value="CONG_TAC">Đề xuất công tác ngoại tỉnh / nhà máy</option>
                        <option value="XAC_NHAN_CONG">Đơn giải trình xác nhận giờ công</option>
                    </select>
                </div>

                <div class="row g-2 mb-3">
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Từ ngày <span class="text-danger">*</span></label>
                        <input type="date" name="ngay_bat_dau" class="form-control" value="<?= date('Y-m-d') ?>" required>
                    </div>
                    <div class="col-6">
                        <label class="form-label small fw-semibold">Đến ngày <span class="text-danger">*</span></label>
                        <input type="date" name="ngay_ket_thuc" class="form-control" value="<?= date('Y-m-d') ?>" required>
                    </div>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Số ngày đề xuất <span class="text-danger">*</span></label>
                    <input type="number" step="0.5" min="0.5" name="so_ngay" class="form-control" value="1.0" required>
                </div>

                <div class="mb-3">
                    <label class="form-label small fw-semibold">Lý do cụ thể <span class="text-danger">*</span></label>
                    <textarea name="ly_do" class="form-control" rows="3" placeholder="Nhập lý do chi tiết để Quản lý trực tiếp xem xét phê duyệt..." required></textarea>
                </div>
            </div>

            <div class="modal-footer">
                <button type="button" class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Đóng</button>
                <button type="submit" class="btn btn-primary btn-sm fw-bold" style="background: #002F6C; border-color: #002F6C;">
                    <i class="bi bi-send-fill me-1"></i> Gửi Đơn Lên Quản Lý
                </button>
            </div>
        </form>
    </div>
</div>
