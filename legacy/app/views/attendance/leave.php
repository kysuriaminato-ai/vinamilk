<?php
/**
 * View Online Leave Requests & Approval Management
 */
?>
<div class="card-box">
    <div style="display: flex; align-items: center; justify-content: space-between; border-bottom: 1px solid var(--border-color); padding-bottom: 16px; margin-bottom: 24px; flex-wrap: wrap; gap: 16px;">
        <div>
            <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;">
                📋 Quản lý Đơn xin nghỉ phép Trực tuyến
            </h2>
            <p style="font-size: 0.85rem; color: var(--text-secondary); margin-top: 4px;">
                Nộp đơn xin nghỉ phép năm, nghỉ thai sản, học tập và phê duyệt tự động trừ quỹ phép / cập nhật bảng công.
            </p>
        </div>

        <button type="button" class="btn btn-primary" onclick="openLeaveModal()">
            ➕ Nộp Đơn Xin Nghỉ Phép
        </button>
    </div>

    <!-- Data Table -->
    <div class="table-responsive">
        <table class="table">
            <thead>
                <tr>
                    <th>Mã NV & Họ tên</th>
                    <th>Loại nghỉ</th>
                    <th>Từ ngày</th>
                    <th>Đến ngày</th>
                    <th>Số ngày nghỉ</th>
                    <th>Lý do nghỉ</th>
                    <th>Trạng thái duyệt</th>
                    <th style="text-align: right;">Hành động Phê duyệt</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($leaveList)): ?>
                    <tr>
                        <td colspan="8" style="text-align: center; color: var(--text-muted); padding: 30px;">
                            Chưa có đơn xin nghỉ phép nào được tạo.
                        </td>
                    </tr>
                <?php else: ?>
                    <?php foreach ($leaveList as $l): ?>
                        <tr>
                            <td>
                                <strong><?= htmlspecialchars($l['HoTen']) ?></strong> (<code><?= $l['MaNV'] ?></code>)
                                <div style="font-size: 0.75rem; color: var(--text-secondary);"><?= htmlspecialchars($l['TenDV'] ?? '') ?></div>
                            </td>
                            <td><span class="badge badge-primary"><?= htmlspecialchars($l['LoaiNghi']) ?></span></td>
                            <td><?= format_date($l['TuNgay']) ?></td>
                            <td><?= format_date($l['DenNgay']) ?></td>
                            <td style="font-weight: 700; text-align: center;"><?= $l['SoNgay'] ?> ngày</td>
                            <td style="font-size: 0.85rem; color: var(--text-secondary);"><?= htmlspecialchars($l['LyDo'] ?? '-') ?></td>
                            <td>
                                <?php if ($l['TrangThai'] === 'DaDuyet'): ?>
                                    <span class="badge badge-success">Đã phê duyệt</span>
                                <?php elseif ($l['TrangThai'] === 'TuChoi'): ?>
                                    <span class="badge badge-danger">Từ chối</span>
                                <?php else: ?>
                                    <span class="badge badge-warning">Chờ duyệt</span>
                                <?php endif; ?>
                            </td>
                            <td style="text-align: right;">
                                <?php if ($l['TrangThai'] === 'ChoDuyet' && has_permission('CHAM_CONG.Approve')): ?>
                                    <form action="<?= url('chamcong/leave') ?>" method="POST" style="display: inline-block;">
                                        <?= csrf_field() ?>
                                        <input type="hidden" name="action" value="approve">
                                        <input type="hidden" name="leave_id" value="<?= $l['ID'] ?>">
                                        <input type="hidden" name="status" value="DaDuyet">
                                        <button type="submit" class="btn btn-primary btn-sm">✅ Duyệt</button>
                                    </form>

                                    <form action="<?= url('chamcong/leave') ?>" method="POST" style="display: inline-block;">
                                        <?= csrf_field() ?>
                                        <input type="hidden" name="action" value="approve">
                                        <input type="hidden" name="leave_id" value="<?= $l['ID'] ?>">
                                        <input type="hidden" name="status" value="TuChoi">
                                        <button type="submit" class="btn btn-outline btn-sm" style="color:red; border-color:#fecaca;">❌ Từ chối</button>
                                    </form>
                                <?php else: ?>
                                    <span style="font-size: 0.8rem; color: var(--text-muted);">-</span>
                                <?php endif; ?>
                            </td>
                        </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
        </table>
    </div>
</div>

<!-- Modal Nộp đơn xin nghỉ phép -->
<div id="leaveModal" style="display: none; position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); z-index: 999; align-items: center; justify-content: center;">
    <div class="card-box" style="width: 100%; max-width: 550px; margin: 0; background: #ffffff;">
        <h3 style="font-size: 1.2rem; color: var(--vnm-navy); margin-bottom: 16px; border-bottom: 1px solid var(--border-color); padding-bottom: 10px;">
            📝 Nộp Đơn Xin Nghỉ Phép Trực tuyến
        </h3>

        <form action="<?= url('chamcong/leave') ?>" method="POST">
            <?= csrf_field() ?>
            <input type="hidden" name="action" value="create">

            <div class="form-group">
                <label class="form-label">Loại nghỉ phép <span style="color:red;">*</span></label>
                <select name="LoaiNghi" class="form-control" required>
                    <option value="Phép năm">Phép năm (Hưởng 100% lương)</option>
                    <option value="Không lương">Nghỉ không hưởng lương</option>
                    <option value="Thai sản">Nghỉ thai sản</option>
                    <option value="Học tập">Nghỉ học tập / Công tác</option>
                </select>
            </div>

            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
                <div class="form-group">
                    <label class="form-label">Từ ngày <span style="color:red;">*</span></label>
                    <input type="date" name="TuNgay" class="form-control" value="<?= date('Y-m-d') ?>" required>
                </div>
                <div class="form-group">
                    <label class="form-label">Đến ngày <span style="color:red;">*</span></label>
                    <input type="date" name="DenNgay" class="form-control" value="<?= date('Y-m-d') ?>" required>
                </div>
            </div>

            <div class="form-group">
                <label class="form-label">Số ngày xin nghỉ:</label>
                <input type="number" step="0.5" name="SoNgay" class="form-control" value="1.0" required>
            </div>

            <div class="form-group">
                <label class="form-label">Lý do xin nghỉ:</label>
                <textarea name="LyDo" class="form-control" rows="3" placeholder="Ghi rõ lý do nghỉ phép..." required></textarea>
            </div>

            <div style="display: flex; justify-content: flex-end; gap: 10px; margin-top: 20px;">
                <button type="button" class="btn btn-outline" onclick="closeLeaveModal()">Hủy</button>
                <button type="submit" class="btn btn-primary">📤 NỘP ĐƠN NGHỈ PHÉP</button>
            </div>
        </form>
    </div>
</div>

<script>
function openLeaveModal() { document.getElementById('leaveModal').style.display = 'flex'; }
function closeLeaveModal() { document.getElementById('leaveModal').style.display = 'none'; }
</script>
