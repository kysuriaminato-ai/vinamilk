<?php
/**
 * View Import Timekeeper Data
 */
?>
<div class="card-box" style="max-width: 650px; margin: 0 auto;">
    <div style="display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--border-color); padding-bottom: 16px; margin-bottom: 24px;">
        <h2 style="font-size: 1.3rem; color: var(--vnm-navy); font-weight: 800;">
            📥 Import Dữ liệu Máy Chấm công Vân tay / Khuôn mặt
        </h2>
        <a href="<?= url('chamcong') ?>" class="btn btn-outline">⬅️ Hủy bỏ</a>
    </div>

    <form action="<?= url('chamcong/import') ?>" method="POST" enctype="multipart/form-data">
        <?= csrf_field() ?>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 20px;">
            <div class="form-group">
                <label class="form-label">Chọn Tháng:</label>
                <select name="thang" class="form-control">
                    <?php for ($m = 1; $m <= 12; $m++): ?>
                        <option value="<?= $m ?>" <?= ($m == date('m')) ? 'selected' : '' ?>>Tháng <?= $m ?></option>
                    <?php endfor; ?>
                </select>
            </div>
            <div class="form-group">
                <label class="form-label">Chọn Năm:</label>
                <select name="nam" class="form-control">
                    <option value="2026" selected>2026</option>
                </select>
            </div>
        </div>

        <div class="form-group">
            <label class="form-label">Chọn File dữ liệu máy chấm công (.CSV / .TXT):</label>
            <input type="file" name="file_excel" class="form-control" accept=".csv, .txt" required>
            <small style="color: var(--text-muted); margin-top: 6px; display: block;">
                Định dạng file CSV mẫu: <code>MaNV,Ngay,KyHieu</code> (Ví dụ: <code>VNM-0001,15,X</code> hoặc <code>VNM-0002,15,TC</code>)
            </small>
        </div>

        <div class="alert alert-info" style="margin-top: 20px;">
            <span>💡</span>
            <div>Tự động phân tích ca kíp, quét đi trễ về sớm và quy đổi công chuẩn để phục vụ tính toán Bảng lương 3P tự động.</div>
        </div>

        <div style="display: flex; justify-content: flex-end; gap: 10px; margin-top: 24px;">
            <a href="<?= url('chamcong') ?>" class="btn btn-outline">Quay lại</a>
            <button type="submit" class="btn btn-primary" style="padding: 12px 30px;">
                🚀 TẢI DỮ LIỆU & ĐỒNG BỘ
            </button>
        </div>
    </form>
</div>
