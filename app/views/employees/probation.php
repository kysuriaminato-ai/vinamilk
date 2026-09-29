<?php
/**
 * View Probation Lifecycle Management & Salary Grade Activation
 */
?>
<div class="card-box">
    <div style="display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--border-color); padding-bottom: 16px; margin-bottom: 24px;">
        <div>
            <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;">
                ⏳ Quy trình Thử việc & Phê duyệt Xếp lương Chính thức
            </h2>
            <p style="font-size: 0.85rem; color: var(--text-secondary); margin-top: 4px;">
                Theo dõi thời gian thử việc, đánh giá năng lực và tự động chuyển trạng thái nhân viên chính thức tại Vinamilk.
            </p>
        </div>
        <span class="badge badge-warning" style="font-size: 0.9rem; padding: 8px 16px;">
            ⚠️ Có <?= count($probationList) ?> nhân sự đang thử việc
        </span>
    </div>

    <!-- Table Probation List -->
    <div class="table-responsive">
        <table class="table">
            <thead>
                <tr>
                    <th>Mã NV</th>
                    <th>Họ và tên</th>
                    <th>Đơn vị công tác</th>
                    <th>Chức vụ thử việc</th>
                    <th>Ngày vào thử việc</th>
                    <th>Thời hạn thử việc</th>
                    <th style="text-align: right;">Hành động Phê duyệt</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($probationList)): ?>
                    <tr>
                        <td colspan="7" style="text-align: center; color: var(--text-muted); padding: 30px;">
                            Hiện tại không có nhân sự nào trong giai đoạn thử việc.
                        </td>
                    </tr>
                <?php else: ?>
                    <?php foreach ($probationList as $p): ?>
                        <tr>
                            <td><code><?= htmlspecialchars($p['MaNV']) ?></code></td>
                            <td style="font-weight: 700; color: var(--vnm-navy);"><?= htmlspecialchars($p['HoTen']) ?></td>
                            <td><?= htmlspecialchars($p['TenDV'] ?? 'Vinamilk') ?></td>
                            <td><?= htmlspecialchars($p['TenCV'] ?? 'Chuyên viên') ?></td>
                            <td><?= format_date($p['NgayVaoLam']) ?></td>
                            <td><span class="badge badge-warning">2 Tháng (Quy định VNM)</span></td>
                            <td style="text-align: right;">
                                <button type="button" class="btn btn-primary btn-sm" onclick="openApproveModal('<?= $p['MaNV'] ?>', '<?= htmlspecialchars($p['HoTen']) ?>')">
                                    ✅ Duyệt Đạt & Xếp Lương
                                </button>
                            </td>
                        </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
        </table>
    </div>
</div>

<!-- Modal Form Duyệt thử việc & Xếp Lương -->
<div id="probationModal" style="display: none; position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); z-index: 999; align-items: center; justify-content: center;">
    <div class="card-box" style="width: 100%; max-width: 550px; margin: 0; background: #ffffff;">
        <h3 style="font-size: 1.2rem; color: var(--vnm-navy); margin-bottom: 16px; border-bottom: 1px solid var(--border-color); padding-bottom: 10px;">
            ✅ Duyệt Đạt Thử việc & Xếp Ngạch Bậc Lương
        </h3>

        <form action="<?= url('nhansu/probation') ?>" method="POST">
            <?= csrf_field() ?>
            <input type="hidden" id="modal_MaNV" name="MaNV" value="">

            <div class="form-group">
                <label class="form-label">Nhân sự phê duyệt:</label>
                <input type="text" id="modal_HoTen" class="form-control" value="" readonly style="background: #e2e8f0; font-weight: 700;">
            </div>

            <div class="form-group">
                <label class="form-label">Ngạch lương chính thức:</label>
                <select name="MaNgach" class="form-control" required>
                    <?php foreach ($options['ngach'] as $ng): ?>
                        <option value="<?= $ng['MaNgach'] ?>">
                            <?= htmlspecialchars($ng['TenNgach']) ?> (Hệ số khởi điểm: <?= $ng['HeSoKhoiDiem'] ?>)
                        </option>
                    <?php endforeach; ?>
                </select>
            </div>

            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
                <div class="form-group">
                    <label class="form-label">Hệ số lương hưởng:</label>
                    <input type="number" step="0.01" name="HeSoLuong" class="form-control" value="2.34" required>
                </div>
                <div class="form-group">
                    <label class="form-label">Bậc lương:</label>
                    <input type="number" name="BacLuong" class="form-control" value="1" required>
                </div>
            </div>

            <div style="display: flex; justify-content: flex-end; gap: 10px; margin-top: 20px;">
                <button type="button" class="btn btn-outline" onclick="closeApproveModal()">Hủy</button>
                <button type="submit" class="btn btn-primary">💾 XÁC NHẬN CHÍNH THỨC</button>
            </div>
        </form>
    </div>
</div>

<script>
function openApproveModal(maNV, hoTen) {
    document.getElementById('modal_MaNV').value = maNV;
    document.getElementById('modal_HoTen').value = hoTen + ' (' + maNV + ')';
    document.getElementById('probationModal').style.display = 'flex';
}
function closeApproveModal() {
    document.getElementById('probationModal').style.display = 'none';
}
</script>
