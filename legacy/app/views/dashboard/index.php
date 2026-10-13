<?php
/**
 * View Executive HR Dashboard (Interactive Charts & Org Tree Chart)
 */
?>
<!-- Row 1: KPI Stats Grid -->
<div class="stats-grid" style="grid-template-columns: repeat(auto-fit, minmax(210px, 1fr));">
    <div class="stat-card">
        <div class="stat-icon blue">👥</div>
        <div class="stat-details">
            <span class="stat-number"><?= number_format($stats['totalStaff']) ?></span>
            <span class="stat-label">Tổng Nhân sự Chính thức</span>
        </div>
    </div>

    <div class="stat-card">
        <div class="stat-icon green">👨‍💼</div>
        <div class="stat-details">
            <span class="stat-number" style="font-size: 1.5rem;"><?= number_format($stats['maleCount']) ?> / <?= number_format($stats['femaleCount']) ?></span>
            <span class="stat-label">Tỷ lệ Nam / Nữ (<?= round(($stats['maleCount'] / ($stats['totalStaff'] ?: 1)) * 100) ?>% Nam)</span>
        </div>
    </div>

    <div class="stat-card">
        <div class="stat-icon orange">🎂</div>
        <div class="stat-details">
            <span class="stat-number" style="color: var(--vnm-gold);"><?= $stats['avgAge'] ?> Tuổi</span>
            <span class="stat-label">Độ tuổi Trung bình</span>
        </div>
    </div>

    <div class="stat-card">
        <div class="stat-icon orange" style="background:#eff6ff; color:var(--vnm-primary);">⏳</div>
        <div class="stat-details">
            <span class="stat-number"><?= number_format($stats['probationStaff']) ?></span>
            <span class="stat-label">Đang thử việc</span>
        </div>
    </div>

    <div class="stat-card">
        <div class="stat-icon red">🚪</div>
        <div class="stat-details">
            <span class="stat-number" style="color: var(--vnm-danger);"><?= number_format($stats['resignedStaff']) ?></span>
            <span class="stat-label">Đã nghỉ việc / Nghỉ hưu</span>
        </div>
    </div>
</div>

<!-- Row 2: Grid 2 Column Charts & Stats -->
<div style="display: grid; grid-template-columns: 2fr 1fr; gap: 30px; margin-bottom: 30px;">
    <!-- Cột 1: Cơ cấu Nhân sự theo Đơn vị / Khối -->
    <div class="card-box" style="margin-bottom: 0;">
        <h2 class="card-title">
            <span>🏢 Cơ cấu Nhân sự theo Khối & Nhà máy Vinamilk</span>
            <span class="badge badge-primary"><?= count($deptStats) ?> Khối/Đơn vị</span>
        </h2>

        <div class="table-responsive">
            <table class="table">
                <thead>
                    <tr>
                        <th>Mã DV</th>
                        <th>Tên Đơn vị</th>
                        <th>Phân loại</th>
                        <th>Số nhân sự</th>
                        <th>Tỷ trọng</th>
                    </tr>
                </thead>
                <tbody>
                    <?php 
                    $total = array_sum(array_column($deptStats, 'TotalCount')) ?: 1;
                    foreach ($deptStats as $dept): 
                        $percentage = round(($dept['TotalCount'] / $total) * 100, 1);
                    ?>
                        <tr>
                            <td><code><?= htmlspecialchars($dept['MaDV']) ?></code></td>
                            <td style="font-weight: 600; color: var(--vnm-navy);"><?= htmlspecialchars($dept['TenDV']) ?></td>
                            <td><span class="badge badge-primary"><?= htmlspecialchars($dept['LoaiDV']) ?></span></td>
                            <td style="font-weight: 700;"><?= number_format($dept['TotalCount']) ?> người</td>
                            <td>
                                <div style="display: flex; align-items: center; gap: 10px;">
                                    <div style="flex: 1; height: 8px; background: #e2e8f0; border-radius: 4px; overflow: hidden;">
                                        <div style="height: 100%; width: <?= $percentage ?>%; background: var(--vnm-primary); border-radius: 4px;"></div>
                                    </div>
                                    <span style="font-size: 0.8rem; font-weight: 600; min-width: 40px;"><?= $percentage ?>%</span>
                                </div>
                            </td>
                        </tr>
                    <?php endforeach; ?>
                </tbody>
            </table>
        </div>
    </div>

    <!-- Cột 2: Phân bố Trình độ Học vấn & Role Stats -->
    <div class="card-box" style="margin-bottom: 0;">
        <h2 class="card-title">
            <span>🎓 Cơ cấu Trình độ Học vấn</span>
        </h2>

        <div style="display: flex; flex-direction: column; gap: 12px; margin-bottom: 24px;">
            <?php foreach ($eduStats as $edu): ?>
                <div style="padding: 12px 16px; border: 1px solid var(--border-color); border-radius: var(--radius-md); background: #f8fafc; display: flex; align-items: center; justify-content: space-between;">
                    <span style="font-weight: 600; color: var(--vnm-navy);"><?= htmlspecialchars($edu['TrinhDo']) ?></span>
                    <span class="badge badge-success" style="font-size: 0.85rem; padding: 4px 12px;">
                        <?= number_format($edu['TotalCount']) ?> người
                    </span>
                </div>
            <?php endforeach; ?>
        </div>

        <button type="button" class="btn btn-primary btn-block" onclick="openOrgChartModal()">
            🌳 SƠ ĐỒ CÂY TỔ CHỨC TƯƠNG TÁC (ORG CHART)
        </button>
    </div>
</div>

<!-- Row 3: Interactive Tree Org Chart Section -->
<div class="card-box">
    <h2 class="card-title">
        <span>🌳 Sơ đồ Cây Cơ cấu Tổ chức Tập đoàn Vinamilk (Tree Hierarchy)</span>
        <span class="badge badge-primary">Interactive Org Chart</span>
    </h2>

    <div style="padding: 20px; background: #f8fafc; border-radius: var(--radius-lg); border: 1px solid var(--border-color); overflow-x: auto;">
        <div style="text-align: center; margin-bottom: 20px;">
            <div style="display: inline-block; padding: 14px 28px; background: var(--vnm-navy); color: #ffffff; font-weight: 800; font-size: 1.1rem; border-radius: var(--radius-md); box-shadow: var(--shadow-md);">
                🥛 CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK)
            </div>
        </div>

        <div style="display: flex; justify-content: center; gap: 20px; flex-wrap: wrap;">
            <?php 
            $khoiList = array_filter($orgTree, fn($item) => $item['CapDV'] == 2);
            foreach ($khoiList as $khoi): 
            ?>
                <div style="flex: 1; min-width: 200px; max-width: 280px; background: #ffffff; border: 2px solid var(--vnm-primary); border-radius: var(--radius-md); padding: 16px; box-shadow: var(--shadow-sm);">
                    <div style="font-weight: 800; color: var(--vnm-navy); font-size: 0.95rem; margin-bottom: 6px;">
                        <?= htmlspecialchars($khoi['TenDV']) ?>
                    </div>
                    <div style="font-size: 0.8rem; color: var(--vnm-primary); font-weight: 600; margin-bottom: 10px;">
                        👥 Thực tế: <strong><?= $khoi['ActualCount'] ?></strong> nhân sự
                    </div>

                    <!-- Sub units of this Khoi -->
                    <div style="border-top: 1px dashed var(--border-color); padding-top: 10px; font-size: 0.75rem;">
                        <?php 
                        $subUnits = array_filter($orgTree, fn($item) => $item['MaDV_Cha'] === $khoi['MaDV']);
                        foreach ($subUnits as $sub): 
                        ?>
                            <div style="padding: 4px 8px; background: #f1f5f9; border-radius: 4px; margin-bottom: 4px; display: flex; justify-content: space-between;">
                                <span><?= htmlspecialchars($sub['TenDV']) ?></span>
                                <strong><?= $sub['ActualCount'] ?></strong>
                            </div>
                        <?php endforeach; ?>
                    </div>
                </div>
            <?php endforeach; ?>
        </div>
    </div>
</div>

<!-- Row 4: Recent Live Logs Table -->
<div class="card-box">
    <h2 class="card-title">
        <span>🕒 Lịch sử Đăng nhập & Truy cập Gần đây</span>
        <span class="badge badge-success">Live Audit Logs</span>
    </h2>

    <div class="table-responsive">
        <table class="table">
            <thead>
                <tr>
                    <th>Thời gian</th>
                    <th>Tên đăng nhập</th>
                    <th>Hành động</th>
                    <th>Trạng thái</th>
                    <th>Địa chỉ IP</th>
                    <th>Chi tiết nhật ký</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($recentLogs)): ?>
                    <tr>
                        <td colspan="6" style="text-align: center; color: var(--text-muted);">Chưa có nhật ký nào được ghi nhận.</td>
                    </tr>
                <?php else: ?>
                    <?php foreach ($recentLogs as $log): ?>
                        <tr>
                            <td style="font-size: 0.85rem;"><?= format_datetime($log['CreatedAt']) ?></td>
                            <td style="font-weight: 600; color: var(--vnm-navy);"><?= htmlspecialchars($log['Username']) ?></td>
                            <td><code><?= htmlspecialchars($log['HanhDong']) ?></code></td>
                            <td>
                                <?php if ($log['TrangThai'] === 'Success'): ?>
                                    <span class="badge badge-success">Thành công</span>
                                <?php else: ?>
                                    <span class="badge badge-danger"><?= htmlspecialchars($log['TrangThai']) ?></span>
                                <?php endif; ?>
                            </td>
                            <td><span style="font-size: 0.85rem; font-family: monospace;"><?= htmlspecialchars($log['DiaChiIP']) ?></span></td>
                            <td style="font-size: 0.85rem; color: var(--text-secondary);"><?= htmlspecialchars($log['MoTa']) ?></td>
                        </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
        </table>
    </div>
</div>

<!-- Modal Xem Sơ đồ Cây Tổ chức Chi tiết -->
<div id="orgChartModal" style="display: none; position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); z-index: 999; align-items: center; justify-content: center;">
    <div class="card-box" style="width: 100%; max-width: 900px; margin: 0; background: #ffffff; max-height: 90vh; overflow-y: auto;">
        <div style="display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid var(--border-color); padding-bottom: 12px; margin-bottom: 20px;">
            <h3 style="font-size: 1.3rem; color: var(--vnm-navy); font-weight: 800;">
                🌳 Sơ đồ Cây Cơ cấu Tổ chức Phân cấp Chi tiết Vinamilk
            </h3>
            <button type="button" class="btn btn-outline btn-sm" onclick="closeOrgChartModal()">❌ Đóng</button>
        </div>

        <div style="padding: 10px;">
            <ul style="list-style: none; padding-left: 0;">
                <li style="font-weight: 800; font-size: 1.1rem; color: var(--vnm-navy); margin-bottom: 10px;">
                    🥛 Đại hội đồng Cổ đông -> Hội đồng Quản trị -> Tổng Giám đốc (Vinamilk)
                </li>
                <?php foreach ($khoiList as $k): ?>
                    <li style="margin-left: 20px; margin-bottom: 15px; border-left: 2px solid var(--vnm-primary); padding-left: 15px;">
                        <strong style="color: var(--vnm-primary); font-size: 1rem;"><?= htmlspecialchars($k['TenDV']) ?></strong>
                        <span class="badge badge-primary"><?= $k['ActualCount'] ?> nhân sự</span>

                        <ul style="list-style: none; padding-left: 20px; margin-top: 8px;">
                            <?php 
                            $subs = array_filter($orgTree, fn($i) => $i['MaDV_Cha'] === $k['MaDV']);
                            foreach ($subs as $s): 
                            ?>
                                <li style="margin-bottom: 4px; font-size: 0.9rem;">
                                    🏢 <?= htmlspecialchars($s['TenDV']) ?> — <strong><?= $s['ActualCount'] ?> người</strong>
                                </li>
                            <?php endforeach; ?>
                        </ul>
                    </li>
                <?php endforeach; ?>
            </ul>
        </div>
    </div>
</div>

<script>
function openOrgChartModal() { document.getElementById('orgChartModal').style.display = 'flex'; }
function closeOrgChartModal() { document.getElementById('orgChartModal').style.display = 'none'; }
</script>
