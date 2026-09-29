<?php
/**
 * View Recruitment Management & Candidate Kanban Pipeline
 */
?>
<div class="card-box">
    <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 24px; flex-wrap: wrap; gap: 16px;">
        <div>
            <h2 style="font-size: 1.4rem; color: var(--vnm-navy); font-weight: 800;">
                🎯 Quản lý Tuyển dụng & Quy trình Phê duyệt Ứng viên (Kanban Pipeline)
            </h2>
            <p style="font-size: 0.85rem; color: var(--text-secondary); margin-top: 4px;">
                Theo dõi nhu cầu tuyển dụng từ các Đơn vị, sàng lọc CV, chấm điểm phỏng vấn và chuyển ứng viên trúng tuyển.
            </p>
        </div>

        <?php if (has_permission('TUYEN_DUNG.Add')): ?>
            <a href="<?= url('tuyendung/create') ?>" class="btn btn-primary">
                ➕ Đăng Tin Tuyển Dụng Mới
            </a>
        <?php endif; ?>
    </div>

    <!-- Active Job Postings -->
    <div style="margin-bottom: 30px;">
        <h3 style="font-size: 1.1rem; color: var(--vnm-navy); font-weight: 700; margin-bottom: 16px;">
            📢 Các Tin Tuyển Dụng Đang Mở (<?= count($jobs) ?> Tin)
        </h3>

        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 16px;">
            <?php foreach ($jobs as $j): ?>
                <div style="padding: 16px; border: 1px solid var(--border-color); border-radius: var(--radius-md); background: #ffffff; box-shadow: var(--shadow-sm);">
                    <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 8px;">
                        <h4 style="font-size: 1rem; font-weight: 700; color: var(--vnm-navy);"><?= htmlspecialchars($j['TieuDe']) ?></h4>
                        <span class="badge badge-primary"><?= number_format($j['TotalCandidates']) ?> UV</span>
                    </div>

                    <div style="font-size: 0.85rem; color: var(--text-secondary); margin-bottom: 8px;">
                        🏢 Đơn vị: <strong><?= htmlspecialchars($j['TenDV'] ?? 'Vinamilk') ?></strong> | Số lượng: <strong><?= $j['SoLuong'] ?> người</strong>
                    </div>

                    <div style="font-size: 0.8rem; color: var(--text-muted); display: flex; justify-content: space-between; align-items: center;">
                        <span>⏰ Hạn nộp: <?= format_date($j['HanNop']) ?></span>
                        <span class="badge badge-success"><?= $j['TrangThai'] ?></span>
                    </div>
                </div>
            <?php endforeach; ?>
        </div>
    </div>

    <!-- Candidate Pipeline Grid (Kanban Steps) -->
    <h3 style="font-size: 1.1rem; color: var(--vnm-navy); font-weight: 700; margin-bottom: 16px;">
        👥 Quy trình Tuyển dụng Ứng viên (Candidate Pipeline)
    </h3>

    <div class="table-responsive">
        <table class="table">
            <thead>
                <tr>
                    <th>Họ và tên ứng viên</th>
                    <th>Vị trí ứng tuyển</th>
                    <th>Trình độ & Chuyên ngành</th>
                    <th>SĐT & Email</th>
                    <th>Nguồn hồ sơ</th>
                    <th>Trạng thái pipeline</th>
                    <th style="text-align: right;">Hành động</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($candidates)): ?>
                    <tr>
                        <td colspan="7" style="text-align: center; color: var(--text-muted); padding: 30px;">
                            Chưa có hồ sơ ứng viên nào nộp.
                        </td>
                    </tr>
                <?php else: ?>
                    <?php foreach ($candidates as $cand): ?>
                        <tr>
                            <td>
                                <strong style="font-size: 0.95rem; color: var(--vnm-navy);"><?= htmlspecialchars($cand['HoTen']) ?></strong>
                            </td>
                            <td><?= htmlspecialchars($cand['TenTinTuyen'] ?? 'Ứng viên') ?></td>
                            <td style="font-size: 0.85rem;">
                                <?= htmlspecialchars($cand['TrinhDo'] ?? 'Đại học') ?> - <?= htmlspecialchars($cand['ChuyenNganh'] ?? '') ?>
                            </td>
                            <td style="font-size: 0.85rem;">
                                <?= htmlspecialchars($cand['SoDienThoai']) ?><br>
                                <span style="color: var(--text-muted);"><?= htmlspecialchars($cand['Email']) ?></span>
                            </td>
                            <td><span class="badge" style="background:#f1f5f9; color:#475569;"><?= htmlspecialchars($cand['NguonHoSo'] ?? 'Website') ?></span></td>
                            <td>
                                <?php
                                $kanbanBadge = match($cand['TrangThai']) {
                                    'MoiNop' => '<span class="badge badge-primary">Mới nộp</span>',
                                    'SangLoc' => '<span class="badge badge-warning">Sàng lọc CV</span>',
                                    'PhongVan' => '<span class="badge badge-info" style="background:#e0f2fe; color:#0369a1;">Phỏng vấn</span>',
                                    'TrungTuyen' => '<span class="badge badge-success">★ Trúng tuyển</span>',
                                    'TuChoi' => '<span class="badge badge-danger">Từ chối</span>',
                                    default => '<span class="badge">Mới</span>'
                                };
                                echo $kanbanBadge;
                                ?>
                            </td>
                            <td style="text-align: right;">
                                <a href="<?= url('tuyendung/candidate/' . $cand['MaUV']) ?>" class="btn btn-outline btn-sm">
                                    👁️ Đánh giá & Duyệt
                                </a>
                            </td>
                        </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
        </table>
    </div>
</div>
