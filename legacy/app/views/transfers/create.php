<?php
/**
 * View Form Lập Quyết định Thuyên chuyển công tác
 */
$selectedMaNV = $_GET['manv'] ?? '';
?>
<div class="card-box" style="max-width: 800px; margin: 0 auto;">
    <div style="display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--border-color); padding-bottom: 16px; margin-bottom: 24px;">
        <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;">
            📝 Lập Quyết định Điều động & Thuyên chuyển công tác
        </h2>
        <a href="<?= url('tuyenchuyen') ?>" class="btn btn-outline">⬅️ Hủy bỏ</a>
    </div>

    <form action="<?= url('tuyenchuyen/create') ?>" method="POST">
        <?= csrf_field() ?>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px;">
            <div class="form-group">
                <label class="form-label">Số Quyết định <span style="color:red;">*</span></label>
                <input type="text" name="SoQD" class="form-control" value="<?= $autoSoQD ?>" required>
            </div>

            <div class="form-group">
                <label class="form-label">Chọn Nhân sự điều động <span style="color:red;">*</span></label>
                <select name="MaNV" class="form-control" required>
                    <option value="">-- Chọn Nhân viên --</option>
                    <?php foreach ($employees as $e): ?>
                        <option value="<?= $e['MaNV'] ?>" <?= ($selectedMaNV === $e['MaNV']) ? 'selected' : '' ?>>
                            <?= htmlspecialchars($e['HoTen']) ?> (<?= $e['MaNV'] ?>)
                        </option>
                    <?php endforeach; ?>
                </select>
            </div>
        </div>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px;">
            <div class="form-group">
                <label class="form-label">Đơn vị tiếp nhận mới <span style="color:red;">*</span></label>
                <select name="DonViMoi" class="form-control" required>
                    <?php foreach ($options['donvi'] as $dv): ?>
                        <option value="<?= $dv['MaDV'] ?>">
                            <?= htmlspecialchars($dv['TenDV']) ?> (<?= $dv['LoaiDV'] ?>)
                        </option>
                    <?php endforeach; ?>
                </select>
            </div>

            <div class="form-group">
                <label class="form-label">Chức vụ mới tiếp nhận:</label>
                <input type="text" name="ChucVuMoi" class="form-control" placeholder="VD: Trưởng phân xưởng chế biến" required>
            </div>
        </div>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px;">
            <div class="form-group">
                <label class="form-label">Ngày Quyết định có hiệu lực <span style="color:red;">*</span></label>
                <input type="date" name="NgayHieuLuc" class="form-control" value="<?= date('Y-m-d') ?>" required>
            </div>

            <div class="form-group">
                <label class="form-label">Lý do điều động / thuyên chuyển:</label>
                <input type="text" name="LyDo" class="form-control" placeholder="VD: Yêu cầu mở rộng sản xuất Nhà máy Mega" required>
            </div>
        </div>

        <div class="form-group">
            <label class="form-label">Ghi chú bổ sung:</label>
            <textarea name="GhiChu" class="form-control" rows="3" placeholder="Chi tiết việc bàn giao công việc và tài sản..."></textarea>
        </div>

        <div style="display: flex; justify-content: flex-end; gap: 14px; margin-top: 24px; padding-top: 16px; border-top: 1px solid var(--border-color);">
            <a href="<?= url('tuyenchuyen') ?>" class="btn btn-outline">Hủy bỏ</a>
            <button type="submit" class="btn btn-primary" style="padding: 12px 30px;">
                📜 BAN HÀNH QUYẾT ĐỊNH
            </button>
        </div>
    </form>
</div>
