<?php
/**
 * View Form Ghi nhận Khen thưởng / Kỷ luật
 */
$selectedMaNV = $_GET['manv'] ?? '';
?>
<div class="card-box" style="max-width: 800px; margin: 0 auto;">
    <div style="display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--border-color); padding-bottom: 16px; margin-bottom: 24px;">
        <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;">
            🏅 Ghi nhận Khen thưởng / Xử lý Kỷ luật Lao động
        </h2>
        <a href="<?= url('ktkl') ?>" class="btn btn-outline">⬅️ Hủy bỏ</a>
    </div>

    <form action="<?= url('ktkl/create') ?>" method="POST">
        <?= csrf_field() ?>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px;">
            <div class="form-group">
                <label class="form-label">Loại quyết định <span style="color:red;">*</span></label>
                <select name="Loai" class="form-control" required onchange="updateSoQD(this.value)">
                    <option value="KhenThuong" selected>🏆 Khen thưởng thi đua / Sáng kiến</option>
                    <option value="KyLuat">⚠️ Xử lý Kỷ luật lao động</option>
                </select>
            </div>

            <div class="form-group">
                <label class="form-label">Số Quyết định <span style="color:red;">*</span></label>
                <input type="text" id="input_SoQD" name="SoQD" class="form-control" value="<?= $autoSoQD ?>" required>
            </div>
        </div>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px;">
            <div class="form-group">
                <label class="form-label">Nhân sự áp dụng <span style="color:red;">*</span></label>
                <select name="MaNV" class="form-control" required>
                    <option value="">-- Chọn Nhân viên --</option>
                    <?php foreach ($employees as $e): ?>
                        <option value="<?= $e['MaNV'] ?>" <?= ($selectedMaNV === $e['MaNV']) ? 'selected' : '' ?>>
                            <?= htmlspecialchars($e['HoTen']) ?> (<?= $e['MaNV'] ?>)
                        </option>
                    <?php endforeach; ?>
                </select>
            </div>

            <div class="form-group">
                <label class="form-label">Hình thức Khen thưởng / Kỷ luật <span style="color:red;">*</span></label>
                <input type="text" name="HinhThuc" class="form-control" placeholder="VD: Bằng khen Tổng Giám đốc / Giấy khen Sáng kiến" required>
            </div>
        </div>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px;">
            <div class="form-group">
                <label class="form-label">Ngày Quyết định <span style="color:red;">*</span></label>
                <input type="date" name="NgayQD" class="form-control" value="<?= date('Y-m-d') ?>" required>
            </div>

            <div class="form-group">
                <label class="form-label">Mức tiền Thưởng (+) hoặc Phạt (-) (VNĐ):</label>
                <input type="number" name="GiaTri" class="form-control" value="5000000" placeholder="VD: 5000000 cho thưởng, -1000000 cho phạt">
            </div>
        </div>

        <div class="form-group">
            <label class="form-label">Lý do cụ thể / Thành tích đạt được:</label>
            <textarea name="LyDo" class="form-control" rows="3" placeholder="Ghi rõ thành tích thi đua hoặc vi phạm kỷ luật..." required></textarea>
        </div>

        <div style="display: flex; justify-content: flex-end; gap: 14px; margin-top: 24px; padding-top: 16px; border-top: 1px solid var(--border-color);">
            <a href="<?= url('ktkl') ?>" class="btn btn-outline">Hủy bỏ</a>
            <button type="submit" class="btn btn-primary" style="padding: 12px 30px;">
                📜 BAN HÀNH QUYẾT ĐỊNH
            </button>
        </div>
    </form>
</div>

<script>
function updateSoQD(val) {
    const input = document.getElementById('input_SoQD');
    if (val === 'KhenThuong') {
        input.value = 'QĐ-KT/VNM/<?= date('Y') ?>/001';
    } else {
        input.value = 'QĐ-KL/VNM/<?= date('Y') ?>/001';
    }
}
</script>
