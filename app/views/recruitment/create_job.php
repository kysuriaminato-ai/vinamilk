<?php
/**
 * View Create Job Posting Form
 */
?>
<div class="card-box" style="max-width: 800px; margin: 0 auto;">
    <div style="display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--border-color); padding-bottom: 16px; margin-bottom: 24px;">
        <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;">
            📢 Đăng Tin Tuyển Dụng Mới (Vinamilk Recruitment Portal)
        </h2>
        <a href="<?= url('tuyendung') ?>" class="btn btn-outline">⬅️ Hủy bỏ</a>
    </div>

    <form action="<?= url('tuyendung/create') ?>" method="POST">
        <?= csrf_field() ?>

        <div class="form-group">
            <label class="form-label">Tiêu đề Tin tuyển dụng <span style="color:red;">*</span></label>
            <input type="text" name="TieuDe" class="form-control" placeholder="VD: Tuyển dụng 10 Kỹ sư Vận hành Dây chuyền Tetra Pak" required>
        </div>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px;">
            <div class="form-group">
                <label class="form-label">Đơn vị nhu cầu <span style="color:red;">*</span></label>
                <select name="MaDV" class="form-control" required>
                    <?php foreach ($options['donvi'] as $dv): ?>
                        <option value="<?= $dv['MaDV'] ?>">
                            <?= htmlspecialchars($dv['TenDV']) ?> (<?= $dv['LoaiDV'] ?>)
                        </option>
                    <?php endforeach; ?>
                </select>
            </div>

            <div class="form-group">
                <label class="form-label">Vị trí cần tuyển:</label>
                <input type="text" name="ViTriTuyen" class="form-control" placeholder="VD: Kỹ sư Cơ điện / Chuyên viên QA" required>
            </div>
        </div>

        <div style="display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 20px;">
            <div class="form-group">
                <label class="form-label">Số lượng cần tuyển:</label>
                <input type="number" name="SoLuong" class="form-control" value="5" required>
            </div>

            <div class="form-group">
                <label class="form-label">Khoảng Mức lương:</label>
                <input type="text" name="MucLuong" class="form-control" value="15.000.000 - 22.000.000 VNĐ">
            </div>

            <div class="form-group">
                <label class="form-label">Hạn nộp hồ sơ:</label>
                <input type="date" name="HanNop" class="form-control" value="<?= date('Y-m-d', strtotime('+30 days')) ?>" required>
            </div>
        </div>

        <div class="form-group">
            <label class="form-label">Mô tả công việc & Yêu cầu kỹ năng:</label>
            <textarea name="MoTaCongViec" class="form-control" rows="4" placeholder="Mô tả chi tiết nhiệm vụ và yêu cầu bằng cấp, kỹ năng chuyên môn..."></textarea>
        </div>

        <div style="display: flex; justify-content: flex-end; gap: 14px; margin-top: 24px; padding-top: 16px; border-top: 1px solid var(--border-color);">
            <a href="<?= url('tuyendung') ?>" class="btn btn-outline">Hủy bỏ</a>
            <button type="submit" class="btn btn-primary" style="padding: 12px 30px;">
                🚀 XUẤT BẢN TIN TUYỂN DỤNG
            </button>
        </div>
    </form>
</div>
