<?php
/**
 * View Candidate Detail & Convert to Employee Action
 */
?>
<div class="card-box" style="max-width: 850px; margin: 0 auto;">
    <div style="display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--border-color); padding-bottom: 16px; margin-bottom: 24px;">
        <div>
            <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;">
                👤 Đánh giá Hồ sơ Ứng viên: <?= htmlspecialchars($cand['HoTen']) ?>
            </h2>
            <p style="font-size: 0.85rem; color: var(--text-secondary); margin-top: 4px;">
                Ứng tuyển vị trí: <strong style="color: var(--vnm-primary);"><?= htmlspecialchars($cand['TenTinTuyen'] ?? 'Vinamilk') ?></strong>
            </p>
        </div>
        <a href="<?= url('tuyendung') ?>" class="btn btn-outline">⬅️ Quay lại Pipeline</a>
    </div>

    <!-- Details Card -->
    <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 24px; margin-bottom: 30px;">
        <div style="background: #f8fafc; padding: 20px; border-radius: var(--radius-md); border: 1px solid var(--border-color);">
            <h3 style="font-size: 1rem; color: var(--vnm-navy); margin-bottom: 12px; font-weight: 700;">📋 Thông tin cá nhân & Liên hệ</h3>
            <p style="margin-bottom: 6px;">- Họ tên: <strong><?= htmlspecialchars($cand['HoTen']) ?></strong></p>
            <p style="margin-bottom: 6px;">- Email: <strong><?= htmlspecialchars($cand['Email']) ?></strong></p>
            <p style="margin-bottom: 6px;">- Điện thoại: <strong><?= htmlspecialchars($cand['SoDienThoai']) ?></strong></p>
            <p style="margin-bottom: 6px;">- Địa chỉ: <?= htmlspecialchars($cand['DiaChi'] ?? 'TP.HCM') ?></p>
            <p style="margin-bottom: 6px;">- Ngày nộp CV: <?= format_date($cand['NgayNop']) ?></p>
        </div>

        <div style="background: #f8fafc; padding: 20px; border-radius: var(--radius-md); border: 1px solid var(--border-color);">
            <h3 style="font-size: 1rem; color: var(--vnm-navy); margin-bottom: 12px; font-weight: 700;">🎓 Trình độ & Kinh nghiệm</h3>
            <p style="margin-bottom: 6px;">- Trình độ: <strong><?= htmlspecialchars($cand['TrinhDo'] ?? 'Đại học') ?></strong></p>
            <p style="margin-bottom: 6px;">- Chuyên ngành: <strong><?= htmlspecialchars($cand['ChuyenNganh'] ?? '-') ?></strong></p>
            <p style="margin-bottom: 6px;">- Tóm tắt kinh nghiệm: <?= htmlspecialchars($cand['KinhNghiem'] ?? 'Có 3 năm kinh nghiệm trong ngành F&B') ?></p>
            <p style="margin-bottom: 6px;">- Nguồn hồ sơ: <span class="badge badge-primary"><?= htmlspecialchars($cand['NguonHoSo'] ?? 'Website') ?></span></p>
        </div>
    </div>

    <!-- Interview Evaluation & Conversion Action -->
    <div style="background: #ecfdf5; border: 2px solid #a7f3d0; padding: 24px; border-radius: var(--radius-lg);">
        <h3 style="font-size: 1.1rem; color: #065f46; margin-bottom: 12px; font-weight: 800;">
            🏆 KẾT QUẢ PHỎNG VẤN & CHUYỂN THÀNH NHÂN VIÊN MỚI
        </h3>
        <p style="font-size: 0.85rem; color: #047857; margin-bottom: 20px;">
            Khi phê duyệt Trúng tuyển, hệ thống sẽ tự động cấp <strong>Mã Nhân viên mới</strong> (VD: VNM-xxxx), chuyển thông tin hồ sơ sang CSDL Nhân sự lõi và thiết lập trạng thái <strong>Thử việc</strong>.
        </p>

        <form action="<?= url('tuyendung/candidate/' . $cand['MaUV']) ?>" method="POST">
            <?= csrf_field() ?>

            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px; margin-bottom: 20px;">
                <div class="form-group">
                    <label class="form-label" style="color: #065f46;">Phân công Đơn vị Vinamilk <span style="color:red;">*</span></label>
                    <select name="MaDV" class="form-control" required>
                        <?php foreach ($options['donvi'] as $dv): ?>
                            <option value="<?= $dv['MaDV'] ?>"><?= htmlspecialchars($dv['TenDV']) ?></option>
                        <?php endforeach; ?>
                    </select>
                </div>

                <div class="form-group">
                    <label class="form-label" style="color: #065f46;">Phân công Chức vụ tiếp nhận <span style="color:red;">*</span></label>
                    <select name="MaCV" class="form-control" required>
                        <?php foreach ($options['chucvu'] as $cv): ?>
                            <option value="<?= $cv['MaCV'] ?>"><?= htmlspecialchars($cv['TenCV']) ?></option>
                        <?php endforeach; ?>
                    </select>
                </div>
            </div>

            <div style="display: flex; justify-content: flex-end; gap: 12px;">
                <button type="submit" class="btn btn-primary" style="background: #059669; padding: 12px 30px;">
                    ✅ DUYỆT TRÚNG TUYỂN & TẠO MÃ NHÂN VIÊN MỚI
                </button>
            </div>
        </form>
    </div>
</div>
