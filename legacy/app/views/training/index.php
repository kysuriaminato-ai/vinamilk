<?php
/**
 * View Training Management & Course Catalogue
 */
?>
<div class="card-box">
    <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 24px; flex-wrap: wrap; gap: 16px;">
        <div>
            <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;">
                🎓 Quản lý Đào tạo & Phát triển Nguồn nhân lực Vinamilk
            </h2>
            <p style="font-size: 0.85rem; color: var(--text-secondary); margin-top: 4px;">
                Quản lý các khóa học chuyên môn (Tetra Pak, ISO 22000/HACCP, Green Farm), cấp chứng chỉ và lưu quá trình đào tạo.
            </p>
        </div>

        <div style="display: flex; gap: 10px;">
            <button type="button" class="btn btn-outline" onclick="openEnrollModal()">
                ✍️ Đăng ký Học viên & Cấp Chứng chỉ
            </button>
            <?php if (has_permission('DAO_TAO.Add')): ?>
                <a href="<?= url('daotao/create') ?>" class="btn btn-primary">
                    ➕ Tạo Khóa Đào tạo Mới
                </a>
            <?php endif; ?>
        </div>
    </div>

    <!-- Course Cards Grid -->
    <div style="margin-bottom: 30px;">
        <h3 style="font-size: 1.1rem; color: var(--vnm-navy); font-weight: 700; margin-bottom: 16px;">
            📚 Danh mục Khóa Đào tạo Chuyên môn
        </h3>

        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 16px;">
            <?php foreach ($courses as $c): ?>
                <div style="padding: 16px; border: 1px solid var(--border-color); border-radius: var(--radius-md); background: #ffffff; box-shadow: var(--shadow-sm);">
                    <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 8px;">
                        <span class="badge badge-primary"><code><?= $c['MaKhoa'] ?></code></span>
                        <span class="badge badge-success"><?= $c['LinhVuc'] ?></span>
                    </div>

                    <h4 style="font-size: 1rem; font-weight: 700; color: var(--vnm-navy); margin-bottom: 8px;"><?= htmlspecialchars($c['TenKhoaHoc']) ?></h4>

                    <div style="font-size: 0.85rem; color: var(--text-secondary); margin-bottom: 8px;">
                        🏛️ Cơ sở: <strong><?= htmlspecialchars($c['CoSoDaoTao']) ?></strong><br>
                        ⏱️ Thời lượng: <strong><?= htmlspecialchars($c['ThoiLuong']) ?></strong> | Chi phí: <strong><?= format_money($c['ChiPhi']) ?></strong>
                    </div>

                    <div style="font-size: 0.8rem; color: var(--text-muted); display: flex; justify-content: space-between; align-items: center; border-top: 1px solid var(--border-color); padding-top: 8px; margin-top: 8px;">
                        <span>👥 Học viên: <strong><?= $c['TotalStudents'] ?> người</strong></span>
                        <button type="button" class="btn btn-outline btn-sm" onclick="openEnrollModal('<?= htmlspecialchars($c['TenKhoaHoc']) ?>', '<?= htmlspecialchars($c['CoSoDaoTao']) ?>')">Cấp CC</button>
                    </div>
                </div>
            <?php endforeach; ?>
        </div>
    </div>

    <!-- Student Enrollment Records Table -->
    <h3 style="font-size: 1.1rem; color: var(--vnm-navy); font-weight: 700; margin-bottom: 16px;">
        📜 Danh sách Học viên & Chứng chỉ Đã Cấp (`qt_daotao`)
    </h3>

    <div class="table-responsive">
        <table class="table">
            <thead>
                <tr>
                    <th>Mã NV & Họ tên</th>
                    <th>Tên Khóa Đào tạo</th>
                    <th>Cơ sở Đào tạo</th>
                    <th>Thời gian học</th>
                    <th>Văn bằng / Chứng chỉ</th>
                    <th>Xếp loại</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($records)): ?>
                    <tr>
                        <td colspan="6" style="text-align: center; color: var(--text-muted); padding: 30px;">
                            Chưa có quá trình đào tạo nào được ghi nhận.
                        </td>
                    </tr>
                <?php else: ?>
                    <?php foreach ($records as $r): ?>
                        <tr>
                            <td>
                                <strong><?= htmlspecialchars($r['HoTen']) ?></strong> (<code><?= $r['MaNV'] ?></code>)
                                <div style="font-size: 0.75rem; color: var(--text-secondary);"><?= htmlspecialchars($r['TenDV'] ?? '') ?></div>
                            </td>
                            <td style="font-weight: 600; color: var(--vnm-navy);"><?= htmlspecialchars($r['TenKhoaHoc']) ?></td>
                            <td><?= htmlspecialchars($r['CoSoDaoTao']) ?></td>
                            <td><?= format_date($r['TuNgay']) ?> - <?= format_date($r['DenNgay']) ?></td>
                            <td><span class="badge badge-success">📜 <?= htmlspecialchars($r['BangCap_ChungChi']) ?></span></td>
                            <td><span class="badge badge-primary"><?= htmlspecialchars($r['XepLoai'] ?? 'Giỏi') ?></span></td>
                        </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
        </table>
    </div>
</div>

<!-- Modal Đăng ký Học viên & Cấp chứng chỉ -->
<div id="enrollModal" style="display: none; position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); z-index: 999; align-items: center; justify-content: center;">
    <div class="card-box" style="width: 100%; max-width: 550px; margin: 0; background: #ffffff;">
        <h3 style="font-size: 1.2rem; color: var(--vnm-navy); margin-bottom: 16px; border-bottom: 1px solid var(--border-color); padding-bottom: 10px;">
            🎓 Đăng ký Học viên & Cấp Chứng chỉ Đào tạo
        </h3>

        <form action="<?= url('daotao/enroll') ?>" method="POST">
            <?= csrf_field() ?>

            <div class="form-group">
                <label class="form-label">Mã Nhân viên <span style="color:red;">*</span></label>
                <input type="text" name="MaNV" class="form-control" placeholder="VD: VNM-0001" required>
            </div>

            <div class="form-group">
                <label class="form-label">Tên Khóa Đào tạo <span style="color:red;">*</span></label>
                <input type="text" id="modal_TenKhoaHoc" name="TenKhoaHoc" class="form-control" required placeholder="VD: Vận hành Dây chuyền Tetra Pak A3/Flex">
            </div>

            <div class="form-group">
                <label class="form-label">Cơ sở / Đơn vị cấp chứng chỉ:</label>
                <input type="text" id="modal_CoSoDaoTao" name="CoSoDaoTao" class="form-control" value="Tập đoàn Vinamilk">
            </div>

            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
                <div class="form-group">
                    <label class="form-label">Từ ngày:</label>
                    <input type="date" name="TuNgay" class="form-control" value="<?= date('Y-m-d') ?>">
                </div>
                <div class="form-group">
                    <label class="form-label">Đến ngày:</label>
                    <input type="date" name="DenNgay" class="form-control" value="<?= date('Y-m-d') ?>">
                </div>
            </div>

            <div class="form-group">
                <label class="form-label">Tên Văn bằng / Chứng chỉ cấp:</label>
                <input type="text" name="BangCap_ChungChi" class="form-control" value="Chứng chỉ Hoàn thành Khóa học Đào tạo Vinamilk">
            </div>

            <div style="display: flex; justify-content: flex-end; gap: 10px; margin-top: 20px;">
                <button type="button" class="btn btn-outline" onclick="closeEnrollModal()">Hủy</button>
                <button type="submit" class="btn btn-primary">🎓 CẤP CHỨNG CHỈ</button>
            </div>
        </form>
    </div>
</div>

<script>
function openEnrollModal(tenKhoa = '', coSo = '') {
    if (tenKhoa) document.getElementById('modal_TenKhoaHoc').value = tenKhoa;
    if (coSo) document.getElementById('modal_CoSoDaoTao').value = coSo;
    document.getElementById('enrollModal').style.display = 'flex';
}
function closeEnrollModal() { document.getElementById('enrollModal').style.display = 'none'; }
</script>
