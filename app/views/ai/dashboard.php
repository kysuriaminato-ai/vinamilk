<?php
/**
 * View AI Expert System Dashboard (HR Demand Forecasting & Skill Gap Analysis)
 */
?>
<!-- Row 1: Executive AI Banner -->
<div style="background: linear-gradient(135deg, #002855 0%, #005696 60%, #0080FF 100%); color: #ffffff; padding: 26px 30px; border-radius: var(--radius-xl); margin-bottom: 30px; box-shadow: var(--shadow-md); position: relative; overflow: hidden;">
    <div style="display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 20px;">
        <div>
            <div style="display: inline-flex; align-items: center; gap: 8px; background: rgba(255,255,255,0.2); padding: 4px 14px; border-radius: 50px; font-size: 0.8rem; font-weight: 700; letter-spacing: 0.5px; margin-bottom: 10px;">
                🤖 VINAMILK AI EXPERT ENGINE v2.0
            </div>
            <h2 style="font-size: 1.6rem; font-weight: 800; margin-bottom: 6px;">
                Hệ Chuyên Gia Trí Tuệ Nhân Tạo & Dự Báo Nhu Cầu Nhân Sự
            </h2>
            <p style="font-size: 0.9rem; opacity: 0.9; max-width: 750px; line-height: 1.5;">
                Thuật toán suy diễn tiến (Forward Chaining) tự động phân tích tỷ lệ biến động lao động (turnover rate), tốc độ tăng trưởng nhà máy/trang trại và khoảng trống kỹ năng để đưa ra khuyến nghị tuyển dụng & đào tạo thời gian thực.
            </p>
        </div>

        <div>
            <span class="badge" style="background: #f59e0b; color: #ffffff; font-size: 1rem; padding: 10px 20px; font-weight: 800; box-shadow: 0 4px 12px rgba(245, 158, 11, 0.4);">
                ⚡ Đang hoạt động
            </span>
        </div>
    </div>
</div>

<!-- Row 2: KPI Stats Grid -->
<div class="stats-grid">
    <div class="stat-card">
        <div class="stat-icon blue">🤖</div>
        <div class="stat-details">
            <span class="stat-number"><?= $stats['total'] ?></span>
            <span class="stat-label">Tổng Khuyến nghị AI</span>
        </div>
    </div>

    <div class="stat-card">
        <div class="stat-icon red">⚠️</div>
        <div class="stat-details">
            <span class="stat-number" style="color: var(--vnm-danger);"><?= $stats['highPriority'] ?></span>
            <span class="stat-label">Cảnh báo Ưu tiên Cấp thiết</span>
        </div>
    </div>

    <div class="stat-card">
        <div class="stat-icon orange">🎯</div>
        <div class="stat-details">
            <span class="stat-number"><?= $stats['recruitment'] ?></span>
            <span class="stat-label">Dự báo Nhu cầu Tuyển dụng</span>
        </div>
    </div>

    <div class="stat-card">
        <div class="stat-icon green">🎓</div>
        <div class="stat-details">
            <span class="stat-number"><?= $stats['training'] ?></span>
            <span class="stat-label">Đề xuất Đào tạo Skill Gap</span>
        </div>
    </div>
</div>

<!-- Row 3: AI Recommendations List Cards -->
<div class="card-box">
    <div class="card-title">
        <span>💡 Danh sách Khuyến nghị Tự động từ AI Expert System</span>
        <span class="badge badge-success">Suy diễn Tiến Live</span>
    </div>

    <div style="display: flex; flex-direction: column; gap: 20px;">
        <?php foreach ($recommendations as $rec): ?>
            <?php
            $priorityBadge = match($rec['MucDoUuTien']) {
                'Cao' => '<span class="badge badge-danger" style="font-size:0.85rem; padding:6px 14px;">🔥 Ưu tiên Cao</span>',
                'TrungBinh' => '<span class="badge badge-warning" style="font-size:0.85rem; padding:6px 14px;">⚠️ Ưu tiên Trung bình</span>',
                default => '<span class="badge badge-primary" style="font-size:0.85rem; padding:6px 14px;">ℹ️ Thấp</span>'
            };
            ?>

            <div style="padding: 24px; border: 1.5px solid var(--border-color); border-radius: var(--radius-lg); background: #ffffff; box-shadow: var(--shadow-sm); transition: var(--transition);">
                <div style="display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px; flex-wrap: wrap; gap: 10px;">
                    <div style="display: flex; align-items: center; gap: 12px;">
                        <?= $priorityBadge ?>
                        <span class="badge badge-primary">
                            <?= ($rec['LoaiKhuyenNghi'] === 'Recruitment') ? '🎯 Tuyển dụng Nguồn lực' : '🎓 Đào tạo Skill Gap' ?>
                        </span>
                        <?php if (!empty($rec['TenDV'])): ?>
                            <span class="badge" style="background:#f1f5f9; color:#475569;">
                                🏢 <?= htmlspecialchars($rec['TenDV']) ?>
                            </span>
                        <?php endif; ?>
                    </div>

                    <!-- AI Confidence Meter -->
                    <div style="display: flex; align-items: center; gap: 10px;">
                        <span style="font-size: 0.85rem; font-weight: 700; color: var(--vnm-navy);">Độ tin cậy AI:</span>
                        <div style="width: 120px; height: 10px; background: #e2e8f0; border-radius: 5px; overflow: hidden;">
                            <div style="height: 100%; width: <?= $rec['XacSuat'] ?>%; background: linear-gradient(90deg, #10b981 0%, #059669 100%); border-radius: 5px;"></div>
                        </div>
                        <span style="font-size: 0.9rem; font-weight: 800; color: var(--vnm-success);"><?= $rec['XacSuat'] ?>%</span>
                    </div>
                </div>

                <h3 style="font-size: 1.15rem; font-weight: 800; color: var(--vnm-navy); margin-bottom: 10px;">
                    <?= htmlspecialchars($rec['TieuDe']) ?>
                </h3>

                <p style="font-size: 0.9rem; color: var(--text-primary); line-height: 1.6; margin-bottom: 16px;">
                    <?= htmlspecialchars($rec['NoiDungChiTiet']) ?>
                </p>

                <div style="display: flex; align-items: center; justify-content: space-between; border-top: 1px dashed var(--border-color); padding-top: 14px; margin-top: 10px; flex-wrap: wrap; gap: 10px;">
                    <div style="font-size: 0.8rem; color: var(--text-secondary);">
                        ⏱️ Tạo lúc: <?= format_datetime($rec['CreatedAt']) ?> | Đề xuất quy mô: <strong><?= $rec['SoLuongDeXuat'] ?> nhân sự</strong>
                    </div>

                    <div>
                        <?php if ($rec['TrangThai'] === 'DaDuyet'): ?>
                            <span class="badge badge-success" style="font-size: 0.9rem; padding: 8px 16px;">
                                ✅ ĐÃ THỰC THI KẾ HOẠCH AI
                            </span>
                        <?php else: ?>
                            <form action="<?= url('ai/approve') ?>" method="POST" style="display: inline-block;">
                                <?= csrf_field() ?>
                                <input type="hidden" name="recommendation_id" value="<?= $rec['ID'] ?>">
                                <button type="submit" class="btn btn-primary" style="background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 10px 20px;">
                                    ⚡ DUYỆT TỰ ĐỘNG KẾ HOẠCH AI
                                </button>
                            </form>
                        <?php endif; ?>
                    </div>
                </div>
            </div>
        <?php endforeach; ?>
    </div>
</div>
