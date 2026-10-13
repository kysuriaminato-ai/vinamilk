<?php
/**
 * View Form Tạo mới Khóa Đào tạo
 */
?>
<div class="card-box" style="max-width: 750px; margin: 0 auto;">
    <div style="display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--border-color); padding-bottom: 16px; margin-bottom: 24px;">
        <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;">
            📚 Tạo mới Khóa Đào tạo Chuyên môn
        </h2>
        <a href="<?= url('daotao') ?>" class="btn btn-outline">⬅️ Hủy bỏ</a>
    </div>

    <form action="<?= url('daotao/create') ?>" method="POST">
        <?= csrf_field() ?>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px;">
            <div class="form-group">
                <label class="form-label">Mã Khóa học <span style="color:red;">*</span></label>
                <input type="text" name="MaKhoa" class="form-control" value="KDT-<?= rand(100, 999) ?>" required>
            </div>

            <div class="form-group">
                <label class="form-label">Lĩnh vực đào tạo <span style="color:red;">*</span></label>
                <select name="LinhVuc" class="form-control" required>
                    <option value="TetraPak">Kỹ thuật Tetra Pak</option>
                    <option value="Safety/HACCP">An toàn thực phẩm ISO 22000 / HACCP</option>
                    <option value="Farm">Quản lý Trang trại Green Farm</option>
                    <option value="Digital">Chuyển đổi số & AI</option>
                    <option value="Leadership">Kỹ năng Quản lý Leadership</option>
                </select>
            </div>
        </div>

        <div class="form-group">
            <label class="form-label">Tên Khóa Đào tạo <span style="color:red;">*</span></label>
            <input type="text" name="TenKhoaHoc" class="form-control" placeholder="VD: Khóa học Chuyển đổi số & Ứng dụng AI trong Chuỗi cung ứng Sữa" required>
        </div>

        <div style="display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 20px;">
            <div class="form-group">
                <label class="form-label">Cơ sở Đào tạo:</label>
                <input type="text" name="CoSoDaoTao" class="form-control" value="Tập đoàn Vinamilk Academy">
            </div>

            <div class="form-group">
                <label class="form-label">Thời lượng:</label>
                <input type="text" name="ThoiLuong" class="form-control" value="30 Giờ">
            </div>

            <div class="form-group">
                <label class="form-label">Chi phí / học viên (VNĐ):</label>
                <input type="number" name="ChiPhi" class="form-control" value="8000000">
            </div>
        </div>

        <div class="form-group">
            <label class="form-label">Mô tả nội dung chương trình đào tạo:</label>
            <textarea name="MoTa" class="form-control" rows="3" placeholder="Mục tiêu khóa học và các tiêu chuẩn đầu ra..."></textarea>
        </div>

        <div style="display: flex; justify-content: flex-end; gap: 14px; margin-top: 24px; padding-top: 16px; border-top: 1px solid var(--border-color);">
            <a href="<?= url('daotao') ?>" class="btn btn-outline">Hủy bỏ</a>
            <button type="submit" class="btn btn-primary" style="padding: 12px 30px;">
                💾 LƯU KHÓA ĐÀO TẠO
            </button>
        </div>
    </form>
</div>
