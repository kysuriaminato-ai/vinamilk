<?php
/**
 * View Retirement & Employment Termination Lifecycle
 */
?>
<div class="card-box">
    <div style="display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--border-color); padding-bottom: 16px; margin-bottom: 24px;">
        <div>
            <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;">
                🏖️ Quy trình Nghỉ hưu & Chấm dứt Hợp đồng Lao động
            </h2>
            <p style="font-size: 0.85rem; color: var(--text-secondary); margin-top: 4px;">
                Tự động rà soát danh sách nhân sự tiệm cận tuổi nghỉ hưu theo quy định của Luật Lao động (Nam >= 60, Nữ >= 55).
            </p>
        </div>
    </div>

    <!-- Candidate Table -->
    <div class="table-responsive">
        <table class="table">
            <thead>
                <tr>
                    <th>Mã NV</th>
                    <th>Họ và tên</th>
                    <th>Giới tính</th>
                    <th>Ngày sinh & Tuổi</th>
                    <th>Đơn vị & Chức vụ</th>
                    <th>Số năm cống hiến</th>
                    <th style="text-align: right;">Thực thi Chế độ</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($candidates)): ?>
                    <tr>
                        <td colspan="7" style="text-align: center; color: var(--text-muted); padding: 30px;">
                            Hiện tại không có nhân sự nào sắp đến tuổi nghỉ hưu.
                        </td>
                    </tr>
                <?php else: ?>
                    <?php foreach ($candidates as $c): ?>
                        <tr>
                            <td><code><?= htmlspecialchars($c['MaNV']) ?></code></td>
                            <td style="font-weight: 700; color: var(--vnm-navy);"><?= htmlspecialchars($c['HoTen']) ?></td>
                            <td><?= $c['GioiTinh'] ?></td>
                            <td>
                                <strong><?= format_date($c['NgaySinh']) ?></strong> 
                                (<span style="color: var(--vnm-danger); font-weight: 700;"><?= $c['Tuoi'] ?> tuổi</span>)
                            </td>
                            <td><?= htmlspecialchars($c['TenDV'] ?? 'Vinamilk') ?> - <?= htmlspecialchars($c['TenCV'] ?? '') ?></td>
                            <td>
                                <?php
                                $years = date('Y') - date('Y', strtotime($c['NgayVaoLam']));
                                echo "{$years} năm";
                                ?>
                            </td>
                            <td style="text-align: right;">
                                <button type="button" class="btn btn-outline btn-sm" style="color: #b45309; border-color: #fde68a;" onclick="openRetireModal('<?= $c['MaNV'] ?>', '<?= htmlspecialchars($c['HoTen']) ?>')">
                                    🌴 Chế độ Nghỉ hưu
                                </button>
                            </td>
                        </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
        </table>
    </div>
</div>

<!-- Modal Form Quyết định Nghỉ hưu / Thôi việc -->
<div id="retireModal" style="display: none; position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); z-index: 999; align-items: center; justify-content: center;">
    <div class="card-box" style="width: 100%; max-width: 550px; margin: 0; background: #ffffff;">
        <h3 style="font-size: 1.2rem; color: var(--vnm-navy); margin-bottom: 16px; border-bottom: 1px solid var(--border-color); padding-bottom: 10px;">
            🌴 Ban hành Quyết định Nghỉ hưu / Chấm dứt HĐLĐ
        </h3>

        <form action="<?= url('nhansu/retirement') ?>" method="POST">
            <?= csrf_field() ?>
            <input type="hidden" id="retire_MaNV" name="MaNV" value="">

            <div class="form-group">
                <label class="form-label">Nhân sự thực hiện:</label>
                <input type="text" id="retire_HoTen" class="form-control" value="" readonly style="background: #e2e8f0; font-weight: 700;">
            </div>

            <div class="form-group">
                <label class="form-label">Hình thức giải quyết chế độ:</label>
                <select name="TrangThaiMoi" class="form-control" required>
                    <option value="4" selected>4. Quyết định Hưởng chế độ Nghỉ hưu</option>
                    <option value="3">3. Thỏa thuận Chấm dứt Hợp đồng Lao động (Thôi việc)</option>
                </select>
            </div>

            <div class="form-group">
                <label class="form-label">Ghi chú & Trợ cấp thôi việc/nghỉ hưu (nếu có):</label>
                <textarea name="GhiChu" class="form-control" rows="3" placeholder="Ghi rõ lý do, số quyết định và chi tiết các khoản trợ cấp thôi việc..."></textarea>
            </div>

            <div style="display: flex; justify-content: flex-end; gap: 10px; margin-top: 20px;">
                <button type="button" class="btn btn-outline" onclick="closeRetireModal()">Hủy</button>
                <button type="submit" class="btn btn-primary" style="background: #b45309;">💾 PHÊ DUYỆT BÀN GIAO</button>
            </div>
        </form>
    </div>
</div>

<script>
function openRetireModal(maNV, hoTen) {
    document.getElementById('retire_MaNV').value = maNV;
    document.getElementById('retire_HoTen').value = hoTen + ' (' + maNV + ')';
    document.getElementById('retireModal').style.display = 'flex';
}
function closeRetireModal() {
    document.getElementById('retireModal').style.display = 'none';
}
</script>
