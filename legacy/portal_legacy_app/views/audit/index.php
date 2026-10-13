<div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
    <div>
        <h3 class="fw-bold text-dark mb-1">
            <i class="bi bi-journal-text text-primary me-2"></i>Nhật Ký Kiểm Toán & Tuân Thủ (Audit Trail)
        </h3>
        <p class="text-muted small mb-0">
            Hệ thống tự động ghi vết 100% mọi hành động Thêm, Sửa, Xóa, Duyệt đơn và Tính toán tiền lương để đảm bảo an ninh thông tin và tuân thủ kiểm toán.
        </p>
    </div>
</div>

<!-- Bộ lọc nhật ký -->
<div class="card card-custom p-3 mb-4 no-print">
    <form action="<?= url('audit') ?>" method="GET" class="row g-2 align-items-end">
        <div class="col-md-4">
            <label class="form-label small fw-semibold text-muted mb-1">Hành động kiểm toán</label>
            <select name="action" class="form-select form-select-sm">
                <option value="ALL">-- Tất cả hành động --</option>
                <option value="CREATE" <?= ($action === 'CREATE') ? 'selected' : '' ?>>CREATE (Thêm mới bản ghi)</option>
                <option value="UPDATE" <?= ($action === 'UPDATE') ? 'selected' : '' ?>>UPDATE (Cập nhật dữ liệu)</option>
                <option value="DELETE" <?= ($action === 'DELETE') ? 'selected' : '' ?>>DELETE (Xóa dữ liệu)</option>
                <option value="APPROVE" <?= ($action === 'APPROVE') ? 'selected' : '' ?>>APPROVE (Phê duyệt)</option>
                <option value="REJECT" <?= ($action === 'REJECT') ? 'selected' : '' ?>>REJECT (Từ chối duyệt)</option>
                <option value="CALCULATE_PAYROLL" <?= ($action === 'CALCULATE_PAYROLL') ? 'selected' : '' ?>>CALCULATE_PAYROLL (Tính toán lương)</option>
                <option value="LOGIN" <?= ($action === 'LOGIN') ? 'selected' : '' ?>>LOGIN (Đăng nhập)</option>
            </select>
        </div>

        <div class="col-md-4">
            <label class="form-label small fw-semibold text-muted mb-1">Bảng dữ liệu bị tác động</label>
            <select name="table" class="form-select form-select-sm">
                <option value="ALL">-- Tất cả bảng dữ liệu --</option>
                <option value="nhansu" <?= ($table === 'nhansu') ? 'selected' : '' ?>>nhansu (Hồ sơ nhân sự)</option>
                <option value="bang_luong" <?= ($table === 'bang_luong') ? 'selected' : '' ?>>bang_luong (Bảng lương)</option>
                <option value="don_tu" <?= ($table === 'don_tu') ? 'selected' : '' ?>>don_tu (Hộp thư đơn từ)</option>
                <option value="users" <?= ($table === 'users') ? 'selected' : '' ?>>users (Tài khoản người dùng)</option>
                <option value="cau_hinh_luong" <?= ($table === 'cau_hinh_luong') ? 'selected' : '' ?>>cau_hinh_luong (Tham số lương)</option>
            </select>
        </div>

        <div class="col-md-4 d-flex gap-2">
            <button type="submit" class="btn btn-primary btn-sm w-100 fw-semibold" style="background: #002F6C;">
                <i class="bi bi-funnel-fill me-1"></i> Lọc Nhật Ký
            </button>
            <a href="<?= url('audit') ?>" class="btn btn-light btn-sm border" title="Đặt lại">
                <i class="bi bi-arrow-counterclockwise"></i>
            </a>
        </div>
    </form>
</div>

<!-- Bảng nhật ký kiểm toán -->
<div class="card card-custom overflow-hidden">
    <div class="table-responsive">
        <table class="table table-hover align-middle mb-0" style="font-size: 13px;">
            <thead class="table-light text-muted small text-uppercase fw-bold" style="border-bottom: 2px solid #E2E8F0;">
                <tr>
                    <th>Thời Gian</th>
                    <th>Người Thực Hiện</th>
                    <th>Hành Động</th>
                    <th>Bảng Tác Động</th>
                    <th>ID Bản Ghi</th>
                    <th>Dữ Liệu Thay Đổi (JSON)</th>
                    <th>Địa Chỉ IP</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($logs)): ?>
                    <tr>
                        <td colspan="7" class="text-center py-5 text-muted">
                            <i class="bi bi-journal-check fs-2 d-block mb-2"></i>
                            Không có bản ghi nhật ký kiểm toán nào thỏa mãn điều kiện lọc.
                        </td>
                    </tr>
                <?php else: ?>
                    <?php foreach ($logs as $l): 
                        $badgeAction = match($l['hanh_dong']) {
                            'CREATE' => 'bg-success',
                            'UPDATE' => 'bg-primary',
                            'DELETE' => 'bg-danger',
                            'APPROVE' => 'bg-info text-dark',
                            'REJECT' => 'bg-warning text-dark',
                            'CALCULATE_PAYROLL' => 'bg-purple text-white',
                            default => 'bg-secondary'
                        };
                    ?>
                    <tr>
                        <td class="font-monospace text-muted small"><?= format_datetime($l['thoi_gian']) ?></td>
                        <td>
                            <strong class="text-dark"><?= e($l['username']) ?></strong>
                            <?php if (!empty($l['HoTen'])): ?>
                                <div class="small text-muted"><?= e($l['HoTen']) ?> (<?= e($l['ma_nv']) ?>)</div>
                            <?php endif; ?>
                        </td>
                        <td>
                            <span class="badge <?= $badgeAction ?>"><?= e($l['hanh_dong']) ?></span>
                        </td>
                        <td><code class="text-dark fw-bold"><?= e($l['bang_tac_dong']) ?></code></td>
                        <td><span class="badge bg-light text-dark border font-monospace"><?= e($l['ban_ghi_id']) ?></span></td>
                        <td style="max-width: 320px;">
                            <?php if ($l['du_lieu_moi']): ?>
                                <details class="small">
                                    <summary class="text-primary" style="cursor: pointer;">Xem chi tiết thay đổi</summary>
                                    <pre class="bg-light p-2 rounded mt-1 border text-wrap mb-0" style="font-size: 11px; max-height: 150px; overflow-y: auto;"><?= e($l['du_lieu_moi']) ?></pre>
                                </details>
                            <?php else: ?>
                                <span class="text-muted small">Không có dữ liệu chi tiết</span>
                            <?php endif; ?>
                        </td>
                        <td><small class="text-muted font-monospace"><?= e($l['ip_address']) ?></small></td>
                    </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
        </table>
    </div>
</div>
