<?php
/**
 * =====================================================================
 * VINAMILK HRM - EXECUTIVE HR ANALYTICS DASHBOARD (index.php)
 * =====================================================================
 * Tích hợp:
 * - Bộ lọc kỳ và đơn vị tuân thủ scopeByUnit()
 * - 6 Thẻ KPI có so sánh cùng kỳ (MoM) & Click-to-Drilldown
 * - 5 Biểu đồ chuyên sâu Chart.js 4 (Headcount & Turnover 12M, Khối, Xếp hạng OT Nhà máy, Tháp tuổi-giới tính, Thâm niên)
 * - 3 Danh sách hành động (HĐ hết hạn 60 ngày, Chứng chỉ ATTP hết hạn, Đơn vị thiếu định biên > 10%)
 * - Modal Drill-Down chi tiết danh sách nhân viên cấu thành con số
 * - Liên kết Từ điển chỉ số (/kpi-dictionary)
 */

$userRole = Auth::role();
$userUnit = Auth::maDV() ?: 'Toàn tập đoàn';

// Helper format tiền tệ
$fmtMoney = fn($val) => number_format((float)$val, 0, ',', '.') . ' ₫';
$fmtPct   = fn($val) => number_format((float)$val, 1, ',', '.') . '%';
?>

<!-- THANH TIÊU ĐỀ & ĐIỀU HƯỚNG TỔNG QUAN -->
<div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
    <div>
        <h3 class="fw-bold text-dark mb-1">
            <i class="bi bi-speedometer2 text-primary me-2"></i>Executive HR Analytics Dashboard
        </h3>
        <p class="text-muted small mb-0">
            Hệ thống phân tích dữ liệu nhân lực cấp điều hành Vinamilk &bull; Phạm vi: 
            <span class="badge bg-primary"><?= e($userRole) ?></span> 
            <span class="badge bg-secondary"><?= e($userUnit) ?></span>
            &bull; Dữ liệu snapshot chốt ngày 1 hằng tháng
        </p>
    </div>

    <div class="d-flex gap-2 align-items-center flex-wrap">
        <a href="<?= url('kpi-dictionary') ?>" class="btn btn-outline-primary btn-sm fw-semibold">
            <i class="bi bi-book-half me-1"></i> Từ Điển Chỉ Số (/kpi-dictionary)
        </a>
        <a href="<?= url('employees') ?>" class="btn btn-outline-secondary btn-sm fw-semibold">
            <i class="bi bi-people-fill me-1"></i> Hồ Sơ Nhân Sự
        </a>
        <?php if (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'BOD'])): ?>
            <button type="button" class="btn btn-sm btn-primary fw-semibold" style="background:#002F6C; border-color:#002F6C;" data-bs-toggle="modal" data-bs-target="#snapshotModal">
                <i class="bi bi-camera-fill me-1"></i> Chốt Snapshot Kỳ
            </button>
        <?php endif; ?>
    </div>
</div>

<!-- THANH BỘ LỌC KỲ VÀ ĐƠN VỊ (TUÂN THỦ SCOPE BY UNIT) -->
<div class="card card-custom p-3 mb-4 shadow-sm border-0">
    <form method="GET" action="<?= url('dashboard') ?>" id="dashboardFilterForm" class="row g-2 align-items-center">
        <!-- Bộ lọc Kỳ tháng -->
        <div class="col-md-4 col-lg-3">
            <label class="form-label small fw-bold text-muted mb-1"><i class="bi bi-calendar-month me-1"></i>Kỳ Báo Cáo / Snapshot:</label>
            <select name="period" class="form-select form-select-sm fw-semibold" onchange="this.form.submit()">
                <?php foreach ($availablePeriods as $p): ?>
                    <option value="<?= e($p) ?>" <?= ($selectedPeriod === $p) ? 'selected' : '' ?>>
                        Tháng <?= date('m/Y', strtotime($p . '-01')) ?> (<?= e($p) ?>)
                    </option>
                <?php endforeach; ?>
            </select>
        </div>

        <!-- Bộ lọc Đơn vị theo cây phân cấp -->
        <div class="col-md-6 col-lg-5">
            <label class="form-label small fw-bold text-muted mb-1"><i class="bi bi-diagram-3-fill me-1"></i>Đơn Vị Quản Lý (Scope By Unit):</label>
            <select name="unit" class="form-select form-select-sm fw-semibold" onchange="this.form.submit()">
                <?php if ($canViewAllUnits): ?>
                    <option value="ALL" <?= ($selectedUnit === 'ALL') ? 'selected' : '' ?>>-- Toàn Bộ Tập Đoàn Vinamilk (All Units) --</option>
                <?php endif; ?>
                <?php foreach ($selectableUnits as $u): ?>
                    <option value="<?= e($u['MaDV']) ?>" <?= ($selectedUnit === $u['MaDV']) ? 'selected' : '' ?>>
                        <?= e($u['IndentTitle'] ?? $u['TenDV']) ?> (<?= e($u['MaDV']) ?>)
                    </option>
                <?php endforeach; ?>
            </select>
        </div>

        <!-- Nút áp dụng & Làm mới -->
        <div class="col-md-2 col-lg-4 text-md-end pt-md-3">
            <a href="<?= url('dashboard') ?>" class="btn btn-outline-secondary btn-sm" title="Đặt lại bộ lọc mặc định">
                <i class="bi bi-arrow-counterclockwise"></i> Mặc định
            </a>
            <button type="submit" class="btn btn-primary btn-sm px-3 fw-semibold ms-1" style="background:#002F6C; border-color:#002F6C;">
                <i class="bi bi-funnel-fill me-1"></i> Lọc dữ liệu
            </button>
        </div>
    </form>
</div>

<!-- ========================================================================= -->
<!-- 6 THẺ KPI TỔNG HỢP SO SÁNH CÙNG KỲ (MOM) & CLICK-TO-DRILLDOWN             -->
<!-- ========================================================================= -->
<div class="row g-3 mb-4">
    <!-- 1. Headcount -->
    <div class="col-sm-6 col-xl-4 col-xxl-2">
        <div class="card card-custom p-3 h-100 border-start border-4 border-primary position-relative kpi-card-hover" 
             onclick="openDrilldownModal('headcount', 'Danh Sách Nhân Sự Hiện Diện (Headcount)')" 
             style="cursor: pointer;" title="Nhấn để xem danh sách chi tiết nhân viên cấu thành con số này">
            <div class="d-flex justify-content-between align-items-start">
                <div>
                    <div class="text-muted small fw-semibold text-uppercase">Headcount</div>
                    <div class="fs-3 fw-bold text-dark mt-1 font-outfit"><?= number_format($kpiCards['headcount']['value']) ?></div>
                </div>
                <div class="bg-primary bg-opacity-10 p-2 rounded-3 text-primary fs-4">
                    <i class="bi bi-people-fill"></i>
                </div>
            </div>
            <div class="mt-2 pt-2 border-top d-flex align-items-center justify-content-between small">
                <?php 
                    $hcDiff = $kpiCards['headcount']['diff'];
                    $hcDiffPct = $kpiCards['headcount']['diff_pct'];
                    $isHcUp = $hcDiff >= 0;
                ?>
                <span class="badge <?= $isHcUp ? 'bg-success bg-opacity-10 text-success' : 'bg-secondary bg-opacity-10 text-secondary' ?> fw-semibold">
                    <i class="bi <?= $isHcUp ? 'bi-arrow-up-right' : 'bi-arrow-down-right' ?> me-1"></i>
                    <?= ($hcDiff > 0 ? '+' : '') . $hcDiff ?> (<?= ($hcDiffPct > 0 ? '+' : '') . $hcDiffPct ?>%)
                </span>
                <span class="text-muted fs-8">So cùng kỳ MoM</span>
            </div>
            <div class="click-hint text-primary fs-8 mt-1"><i class="bi bi-cursor-fill me-1"></i>Chi tiết &rarr;</div>
        </div>
    </div>

    <!-- 2. Tỷ lệ nghỉ việc 12 tháng trượt -->
    <div class="col-sm-6 col-xl-4 col-xxl-2">
        <div class="card card-custom p-3 h-100 border-start border-4 border-danger position-relative kpi-card-hover" 
             onclick="openDrilldownModal('turnover_12m', 'Danh Sách Nhân Sự Thôi Việc (12 Tháng Trượt)')" 
             style="cursor: pointer;" title="Nhấn để xem danh sách nhân sự thôi việc">
            <div class="d-flex justify-content-between align-items-start">
                <div>
                    <div class="text-muted small fw-semibold text-uppercase">Nghỉ Việc 12M Trượt</div>
                    <div class="fs-3 fw-bold text-danger mt-1 font-outfit"><?= $fmtPct($kpiCards['turnover_12m']['value']) ?></div>
                </div>
                <div class="bg-danger bg-opacity-10 p-2 rounded-3 text-danger fs-4">
                    <i class="bi bi-person-x-fill"></i>
                </div>
            </div>
            <div class="mt-2 pt-2 border-top d-flex align-items-center justify-content-between small">
                <?php 
                    $toDiff = $kpiCards['turnover_12m']['diff'];
                    $isToGood = $toDiff <= 0; // Giảm nghỉ việc là tốt
                ?>
                <span class="badge <?= $isToGood ? 'bg-success bg-opacity-10 text-success' : 'bg-danger bg-opacity-10 text-danger' ?> fw-semibold">
                    <i class="bi <?= $isToGood ? 'bi-arrow-down-right' : 'bi-arrow-up-right' ?> me-1"></i>
                    <?= ($toDiff > 0 ? '+' : '') . $toDiff ?>% điểm
                </span>
                <span class="text-muted fs-8">Mục tiêu &le; 8%</span>
            </div>
            <div class="click-hint text-danger fs-8 mt-1"><i class="bi bi-cursor-fill me-1"></i>Chi tiết &rarr;</div>
        </div>
    </div>

    <!-- 3. Tỷ lệ vắng mặt -->
    <div class="col-sm-6 col-xl-4 col-xxl-2">
        <div class="card card-custom p-3 h-100 border-start border-4 border-warning position-relative kpi-card-hover" 
             onclick="openDrilldownModal('absenteeism', 'Danh Sách Nhân Sự Nghỉ Phép & Vắng Mặt Trong Kỳ')" 
             style="cursor: pointer;" title="Nhấn để xem danh sách nhân sự nghỉ phép và vắng mặt">
            <div class="d-flex justify-content-between align-items-start">
                <div>
                    <div class="text-muted small fw-semibold text-uppercase">Tỷ Lệ Vắng Mặt</div>
                    <div class="fs-3 fw-bold text-dark mt-1 font-outfit"><?= $fmtPct($kpiCards['absenteeism']['value']) ?></div>
                </div>
                <div class="bg-warning bg-opacity-10 p-2 rounded-3 text-warning fs-4">
                    <i class="bi bi-calendar2-x-fill"></i>
                </div>
            </div>
            <div class="mt-2 pt-2 border-top d-flex align-items-center justify-content-between small">
                <?php 
                    $absDiff = $kpiCards['absenteeism']['diff'];
                    $isAbsGood = $absDiff <= 0;
                ?>
                <span class="badge <?= $isAbsGood ? 'bg-success bg-opacity-10 text-success' : 'bg-warning bg-opacity-10 text-dark' ?> fw-semibold">
                    <i class="bi <?= $isAbsGood ? 'bi-arrow-down-right' : 'bi-arrow-up-right' ?> me-1"></i>
                    <?= ($absDiff > 0 ? '+' : '') . $absDiff ?>% điểm
                </span>
                <span class="text-muted fs-8">Mục tiêu &le; 3%</span>
            </div>
            <div class="click-hint text-warning fs-8 mt-1"><i class="bi bi-cursor-fill me-1"></i>Chi tiết &rarr;</div>
        </div>
    </div>

    <!-- 4. Giờ OT bình quân / người -->
    <div class="col-sm-6 col-xl-4 col-xxl-2">
        <div class="card card-custom p-3 h-100 border-start border-4 border-info position-relative kpi-card-hover" 
             onclick="openDrilldownModal('overtime_avg', 'Danh Sách Nhân Sự Làm Thêm Giờ (OT) Trong Kỳ')" 
             style="cursor: pointer;" title="Nhấn để xem danh sách nhân sự có giờ làm thêm">
            <div class="d-flex justify-content-between align-items-start">
                <div>
                    <div class="text-muted small fw-semibold text-uppercase">OT BQ / Người</div>
                    <div class="fs-3 fw-bold text-info mt-1 font-outfit"><?= number_format($kpiCards['overtime_avg']['value'], 1) ?>h</div>
                </div>
                <div class="bg-info bg-opacity-10 p-2 rounded-3 text-info fs-4">
                    <i class="bi bi-clock-history"></i>
                </div>
            </div>
            <div class="mt-2 pt-2 border-top d-flex align-items-center justify-content-between small">
                <?php 
                    $otDiff = $kpiCards['overtime_avg']['diff'];
                    $isOtOk = $kpiCards['overtime_avg']['value'] <= 40;
                ?>
                <span class="badge <?= $isOtOk ? 'bg-success bg-opacity-10 text-success' : 'bg-danger bg-opacity-10 text-danger' ?> fw-semibold">
                    <i class="bi <?= $otDiff <= 0 ? 'bi-arrow-down-right' : 'bi-arrow-up-right' ?> me-1"></i>
                    <?= ($otDiff > 0 ? '+' : '') . $otDiff ?>h MoM
                </span>
                <span class="text-muted fs-8">Giới hạn 40h</span>
            </div>
            <div class="click-hint text-info fs-8 mt-1"><i class="bi bi-cursor-fill me-1"></i>Chi tiết &rarr;</div>
        </div>
    </div>

    <!-- 5. Quỹ lương tháng -->
    <div class="col-sm-6 col-xl-4 col-xxl-2">
        <div class="card card-custom p-3 h-100 border-start border-4 border-success position-relative kpi-card-hover" 
             onclick="openDrilldownModal('payroll_fund', 'Danh Sách Chi Tiết Quỹ Lương & Thu Nhập Nhân Sự')" 
             style="cursor: pointer;" title="Nhấn để xem bảng lương chi tiết">
            <div class="d-flex justify-content-between align-items-start">
                <div>
                    <div class="text-muted small fw-semibold text-uppercase">Quỹ Lương Tháng</div>
                    <div class="fs-4 fw-bold text-success mt-1 font-outfit"><?= $fmtMoney($kpiCards['payroll_fund']['value']) ?></div>
                </div>
                <div class="bg-success bg-opacity-10 p-2 rounded-3 text-success fs-4">
                    <i class="bi bi-cash-coin"></i>
                </div>
            </div>
            <div class="mt-2 pt-2 border-top d-flex align-items-center justify-content-between small">
                <?php 
                    $payDiffPct = $kpiCards['payroll_fund']['diff_pct'];
                ?>
                <span class="badge bg-secondary bg-opacity-10 text-dark fw-semibold">
                    <i class="bi <?= $payDiffPct >= 0 ? 'bi-arrow-up-right' : 'bi-arrow-down-right' ?> me-1"></i>
                    <?= ($payDiffPct > 0 ? '+' : '') . $payDiffPct ?>% MoM
                </span>
                <span class="text-muted fs-8">Tổng chi phí</span>
            </div>
            <div class="click-hint text-success fs-8 mt-1"><i class="bi bi-cursor-fill me-1"></i>Chi tiết &rarr;</div>
        </div>
    </div>

    <!-- 6. Tỷ lệ lấp đầy định biên -->
    <div class="col-sm-6 col-xl-4 col-xxl-2">
        <div class="card card-custom p-3 h-100 border-start border-4 border-secondary position-relative kpi-card-hover" 
             onclick="openDrilldownModal('staffing_fill', 'Danh Sách Nhân Sự Theo Định Biên Kế Hoạch')" 
             style="cursor: pointer;" title="Nhấn để xem danh sách nhân sự định biên">
            <div class="d-flex justify-content-between align-items-start">
                <div>
                    <div class="text-muted small fw-semibold text-uppercase">Lấp Đầy Định Biên</div>
                    <div class="fs-3 fw-bold text-dark mt-1 font-outfit"><?= $fmtPct($kpiCards['staffing_fill']['value']) ?></div>
                </div>
                <div class="bg-secondary bg-opacity-10 p-2 rounded-3 text-secondary fs-4">
                    <i class="bi bi-pie-chart-fill"></i>
                </div>
            </div>
            <div class="mt-2 pt-2 border-top d-flex align-items-center justify-content-between small">
                <?php 
                    $fillDiff = $kpiCards['staffing_fill']['diff'];
                    $isFillOk = $kpiCards['staffing_fill']['value'] >= 90;
                ?>
                <span class="badge <?= $isFillOk ? 'bg-success bg-opacity-10 text-success' : 'bg-danger bg-opacity-10 text-danger' ?> fw-semibold">
                    <i class="bi <?= $fillDiff >= 0 ? 'bi-arrow-up-right' : 'bi-arrow-down-right' ?> me-1"></i>
                    <?= ($fillDiff > 0 ? '+' : '') . $fillDiff ?>% MoM
                </span>
                <span class="text-muted fs-8">Chuẩn &ge; 90%</span>
            </div>
            <div class="click-hint text-dark fs-8 mt-1"><i class="bi bi-cursor-fill me-1"></i>Chi tiết &rarr;</div>
        </div>
    </div>
</div>

<!-- ========================================================================= -->
<!-- 5 BIỂU ĐỒ CHUYÊN SÂU (CHART.JS 4)                                         -->
<!-- ========================================================================= -->
<div class="row g-4 mb-4">
    <!-- Biểu đồ 1: Headcount & Nghỉ việc theo tháng (12 Tháng Trượt) -->
    <div class="col-lg-8">
        <div class="card card-custom p-4 h-100 shadow-sm">
            <div class="d-flex justify-content-between align-items-center mb-3">
                <div>
                    <h5 class="fw-bold text-dark mb-0">
                        <i class="bi bi-graph-up text-primary me-2"></i>Xu Hướng Headcount & Nghỉ Việc 12 Tháng Trượt
                    </h5>
                    <div class="text-muted fs-8">Quy mô lao động hiện diện kết hợp số nhân sự thôi việc từng tháng</div>
                </div>
                <span class="badge bg-light text-muted border">Chart.js 4 &bull; 12 Tháng</span>
            </div>
            <div style="position: relative; height: 320px;">
                <canvas id="chartTrend12m"></canvas>
            </div>
        </div>
    </div>

    <!-- Biểu đồ 2: Cơ cấu nhân sự theo Khối chức năng -->
    <div class="col-lg-4">
        <div class="card card-custom p-4 h-100 shadow-sm">
            <div class="d-flex justify-content-between align-items-center mb-3">
                <div>
                    <h5 class="fw-bold text-dark mb-0">
                        <i class="bi bi-bar-chart-fill text-primary me-2"></i>Cơ Cấu Theo Khối
                    </h5>
                    <div class="text-muted fs-8">Phân bổ nhân lực theo 4 khối chức năng</div>
                </div>
                <span class="badge bg-light text-muted border">Cột dọc</span>
            </div>
            <div style="position: relative; height: 320px;">
                <canvas id="chartBlockDistribution"></canvas>
            </div>
        </div>
    </div>
</div>

<div class="row g-4 mb-4">
    <!-- Biểu đồ 3: Xếp hạng giờ OT theo Nhà máy (Cột ngang) -->
    <div class="col-lg-5">
        <div class="card card-custom p-4 h-100 shadow-sm">
            <div class="d-flex justify-content-between align-items-center mb-3">
                <div>
                    <h5 class="fw-bold text-dark mb-0">
                        <i class="bi bi-bar-chart-steps text-info me-2"></i>Giờ OT Bình Quân Theo Nhà Máy
                    </h5>
                    <div class="text-muted fs-8">Xếp hạng giờ làm thêm ngoài giờ (Giới hạn: 40h/tháng)</div>
                </div>
                <span class="badge bg-light text-muted border">Cột ngang</span>
            </div>
            <div style="position: relative; height: 300px;">
                <canvas id="chartFactoryOtRanking"></canvas>
            </div>
        </div>
    </div>

    <!-- Biểu đồ 4: Tháp tuổi - Giới tính (Age-Gender Pyramid) -->
    <div class="col-lg-4">
        <div class="card card-custom p-4 h-100 shadow-sm">
            <div class="d-flex justify-content-between align-items-center mb-3">
                <div>
                    <h5 class="fw-bold text-dark mb-0">
                        <i class="bi bi-people text-danger me-2"></i>Tháp Tuổi - Giới Tính
                    </h5>
                    <div class="text-muted fs-8">Phân bố lứa tuổi đối xứng Nam & Nữ</div>
                </div>
                <span class="badge bg-light text-muted border">Kim tự tháp</span>
            </div>
            <div style="position: relative; height: 300px;">
                <canvas id="chartAgeGenderPyramid"></canvas>
            </div>
        </div>
    </div>

    <!-- Biểu đồ 5: Phân bổ thâm niên công tác -->
    <div class="col-lg-3">
        <div class="card card-custom p-4 h-100 shadow-sm">
            <div class="d-flex justify-content-between align-items-center mb-3">
                <div>
                    <h5 class="fw-bold text-dark mb-0">
                        <i class="bi bi-award-fill text-warning me-2"></i>Phân Bổ Thâm Niên
                    </h5>
                    <div class="text-muted fs-8">Thời gian cống hiến Vinamilk</div>
                </div>
                <span class="badge bg-light text-muted border">Phân nhóm</span>
            </div>
            <div style="position: relative; height: 300px;">
                <canvas id="chartTenureDistribution"></canvas>
            </div>
        </div>
    </div>
</div>

<!-- ========================================================================= -->
<!-- 3 DANH SÁCH HÀNH ĐỘNG CẦN XỬ LÝ (ACTIONABLE LISTS & CARDS)               -->
<!-- ========================================================================= -->
<div class="row g-4 mb-4">
    <!-- 1. Hợp đồng lao động hết hạn trong vòng 60 ngày -->
    <div class="col-lg-4">
        <div class="card card-custom shadow-sm h-100">
            <div class="card-header bg-white py-3 border-bottom d-flex justify-content-between align-items-center">
                <div class="fw-bold text-danger">
                    <i class="bi bi-exclamation-triangle-fill me-1"></i>HĐ Hết Hạn Trong 60 Ngày
                </div>
                <span class="badge bg-danger rounded-pill"><?= count($expiringContracts) ?> HĐ</span>
            </div>
            <div class="card-body p-0" style="max-height: 380px; overflow-y: auto;">
                <?php if (empty($expiringContracts)): ?>
                    <div class="p-4 text-center text-muted small">
                        <i class="bi bi-check-circle-fill text-success fs-3 d-block mb-1"></i>
                        Không có hợp đồng lao động nào sắp hết hạn trong 60 ngày tới.
                    </div>
                <?php else: ?>
                    <div class="list-group list-group-flush">
                        <?php foreach ($expiringContracts as $hd): ?>
                            <div class="list-group-item list-group-item-action p-3">
                                <div class="d-flex justify-content-between align-items-start mb-1">
                                    <div class="fw-bold text-dark"><?= e($hd['HoTen']) ?> (<?= e($hd['ma_nv']) ?>)</div>
                                    <span class="badge <?= $hd['SoNgayConLai'] <= 30 ? 'bg-danger' : 'bg-warning text-dark' ?>">
                                        Còn <?= (int)$hd['SoNgayConLai'] ?> ngày
                                    </span>
                                </div>
                                <div class="small text-muted mb-2">
                                    <span><?= e($hd['TenDV']) ?></span> &bull; 
                                    <span class="badge bg-light text-dark border"><?= e($hd['loai_hd'] ?: 'HĐ Lao động') ?></span>
                                </div>
                                <div class="d-flex justify-content-between align-items-center small">
                                    <span class="text-muted fs-8">Hết hạn: <strong><?= date('d/m/Y', strtotime($hd['den_ngay'])) ?></strong></span>
                                    <a href="<?= url('employees/show/' . $hd['ma_nv']) ?>#contracts" class="btn btn-outline-danger btn-xs py-0 px-2 fw-semibold">
                                        Tái ký / Gia hạn &rarr;
                                    </a>
                                </div>
                            </div>
                        <?php endforeach; ?>
                    </div>
                <?php endif; ?>
            </div>
        </div>
    </div>

    <!-- 2. Chứng chỉ ATTP & An toàn lao động sắp hết hạn (60 ngày) -->
    <div class="col-lg-4">
        <div class="card card-custom shadow-sm h-100">
            <div class="card-header bg-white py-3 border-bottom d-flex justify-content-between align-items-center">
                <div class="fw-bold text-warning text-dark">
                    <i class="bi bi-shield-exclamation text-warning me-1"></i>Chứng Chỉ ATTP Sắp Hết Hạn
                </div>
                <span class="badge bg-warning text-dark rounded-pill"><?= count($expiringCertificates) ?> Chứng chỉ</span>
            </div>
            <div class="card-body p-0" style="max-height: 380px; overflow-y: auto;">
                <?php if (empty($expiringCertificates)): ?>
                    <div class="p-4 text-center text-muted small">
                        <i class="bi bi-patch-check-fill text-success fs-3 d-block mb-1"></i>
                        Tất cả chứng chỉ An toàn thực phẩm & ATLĐ đều còn hạn đầy đủ.
                    </div>
                <?php else: ?>
                    <div class="list-group list-group-flush">
                        <?php foreach ($expiringCertificates as $cc): ?>
                            <div class="list-group-item list-group-item-action p-3">
                                <div class="d-flex justify-content-between align-items-start mb-1">
                                    <div class="fw-bold text-dark"><?= e($cc['HoTen']) ?> (<?= e($cc['ma_nv']) ?>)</div>
                                    <span class="badge bg-warning text-dark">Còn <?= (int)$cc['SoNgayConLai'] ?> ngày</span>
                                </div>
                                <div class="small fw-semibold text-primary mb-1"><?= e($cc['ten_chung_chi']) ?></div>
                                <div class="d-flex justify-content-between align-items-center small text-muted">
                                    <span class="fs-8">Hạn dùng: <strong><?= date('d/m/Y', strtotime($cc['ngay_het_han'])) ?></strong></span>
                                    <a href="<?= url('employees/show/' . $cc['ma_nv']) ?>#certificates" class="btn btn-outline-primary btn-xs py-0 px-2 fw-semibold">
                                        Tái huấn luyện &rarr;
                                    </a>
                                </div>
                            </div>
                        <?php endforeach; ?>
                    </div>
                <?php endif; ?>
            </div>
        </div>
    </div>

    <!-- 3. Đơn vị thiếu định biên > 10% -->
    <div class="col-lg-4">
        <div class="card card-custom shadow-sm h-100">
            <div class="card-header bg-white py-3 border-bottom d-flex justify-content-between align-items-center">
                <div class="fw-bold text-danger">
                    <i class="bi bi-diagram-3-fill text-danger me-1"></i>Đơn Vị Thiếu Định Biên &gt; 10%
                </div>
                <span class="badge bg-danger rounded-pill"><?= count($understaffedUnits) ?> Đơn vị</span>
            </div>
            <div class="card-body p-0" style="max-height: 380px; overflow-y: auto;">
                <?php if (empty($understaffedUnits)): ?>
                    <div class="p-4 text-center text-muted small">
                        <i class="bi bi-check-all text-success fs-3 d-block mb-1"></i>
                        Không có đơn vị nào bị thiếu hụt quá 10% định biên lao động.
                    </div>
                <?php else: ?>
                    <div class="list-group list-group-flush">
                        <?php foreach ($understaffedUnits as $us): ?>
                            <div class="list-group-item list-group-item-action p-3">
                                <div class="d-flex justify-content-between align-items-start mb-1">
                                    <div class="fw-bold text-dark"><?= e($us['TenDV']) ?></div>
                                    <span class="badge bg-danger">Thiếu <?= e($us['TyLeThieu']) ?>%</span>
                                </div>
                                <div class="d-flex justify-content-between small text-muted mb-2">
                                    <span>Định biên: <strong><?= (int)$us['TongDinhBien'] ?></strong></span>
                                    <span>Hiện có: <strong><?= (int)$us['HeadcountThucTe'] ?></strong></span>
                                    <span class="text-danger fw-semibold">Khuyết: <strong><?= (int)$us['SoLuongThieu'] ?> NS</strong></span>
                                </div>
                                <div class="progress mb-2" style="height: 6px;">
                                    <?php $pct = round(((int)$us['HeadcountThucTe'] / max(1, (int)$us['TongDinhBien'])) * 100); ?>
                                    <div class="progress-bar bg-danger" role="progressbar" style="width: <?= $pct ?>%;" aria-valuenow="<?= $pct ?>" aria-valuemin="0" aria-valuemax="100"></div>
                                </div>
                                <div class="text-end">
                                    <a href="<?= url('employees') ?>?unit=<?= e($us['MaDV']) ?>" class="btn btn-outline-danger btn-xs py-0 px-2 fw-semibold">
                                        Đề xuất tuyển dụng &rarr;
                                    </a>
                                </div>
                            </div>
                        <?php endforeach; ?>
                    </div>
                <?php endif; ?>
            </div>
        </div>
    </div>
</div>

<!-- ========================================================================= -->
<!-- MODAL DRILL-DOWN: HIỂN THỊ DANH SÁCH NHÂN VIÊN CẤU THÀNH CON SỐ           -->
<!-- ========================================================================= -->
<div class="modal fade" id="drilldownModal" tabindex="-1" aria-labelledby="drilldownModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-xl modal-dialog-scrollable modal-dialog-centered">
        <div class="modal-content border-0 shadow-lg">
            <div class="modal-header text-white" style="background:#002F6C;">
                <div>
                    <h5 class="modal-title fw-bold" id="drilldownModalLabel">
                        <i class="bi bi-people-fill me-2"></i>Chi Tiết Danh Sách Nhân Sự Cấu Thành
                    </h5>
                    <div class="small opacity-75" id="drilldownModalSub">Dữ liệu phân tích chi tiết kỳ <?= e($selectedPeriod) ?></div>
                </div>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Đóng"></button>
            </div>
            <div class="modal-body p-0">
                <!-- Vùng Loading Spinner -->
                <div id="drilldownLoading" class="p-5 text-center">
                    <div class="spinner-border text-primary" role="status">
                        <span class="visually-hidden">Đang tải dữ liệu...</span>
                    </div>
                    <div class="text-muted mt-2 small">Đang truy vấn danh sách nhân sự từ cơ sở dữ liệu...</div>
                </div>

                <!-- Vùng Bảng dữ liệu nhân viên -->
                <div id="drilldownContent" style="display: none;">
                    <div class="p-3 bg-light border-bottom d-flex justify-content-between align-items-center flex-wrap gap-2">
                        <div class="text-muted small">
                            Tìm thấy <strong class="text-dark fs-6" id="drilldownCount">0</strong> nhân sự cấu thành chỉ số này
                        </div>
                        <input type="text" id="drilldownFilterInput" class="form-control form-control-sm w-auto" placeholder="Lọc nhanh tên, mã NV..." onkeyup="filterDrilldownTable()">
                    </div>

                    <div class="table-responsive" style="max-height: 480px;">
                        <table class="table table-hover align-middle mb-0" id="drilldownTable">
                            <thead class="table-light sticky-top border-bottom fs-8 text-uppercase text-muted">
                                <tr>
                                    <th style="width: 5%;">STT</th>
                                    <th style="width: 15%;">Mã Nhân Viên</th>
                                    <th style="width: 25%;">Họ Và Tên</th>
                                    <th style="width: 20%;">Chức Vụ</th>
                                    <th style="width: 20%;">Đơn Vị Công Tác</th>
                                    <th style="width: 15%;">Thông Tin Cấu Thành</th>
                                </tr>
                            </thead>
                            <tbody id="drilldownTbody">
                                <!-- Dữ liệu nạp qua Javascript AJAX -->
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
            <div class="modal-footer bg-light py-2">
                <button type="button" class="btn btn-outline-secondary btn-sm" data-bs-dismiss="modal">Đóng</button>
            </div>
        </div>
    </div>
</div>

<!-- ========================================================================= -->
<!-- MODAL CHỐT SNAPSHOT THÁNG (ADMIN ONLY)                                    -->
<!-- ========================================================================= -->
<?php if (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'BOD'])): ?>
<div class="modal fade" id="snapshotModal" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-0 shadow">
            <form method="POST" action="<?= url('dashboard/run_snapshot') ?>">
                <?= csrf_field() ?>
                <div class="modal-header text-white" style="background:#002F6C;">
                    <h5 class="modal-title fw-bold"><i class="bi bi-camera-fill me-2"></i>Chốt Snapshot Nhân Sự Tháng</h5>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body p-4">
                    <p class="text-muted small">
                        Hệ thống sẽ tổng hợp số liệu nhân sự cuối tháng, giờ làm thêm, ngày vắng mặt, quỹ lương và tỷ lệ nghỉ việc 12 tháng trượt vào bảng <code>fact_headcount_monthly</code>.
                    </p>
                    <div class="mb-3">
                        <label class="form-label fw-semibold">Chọn Kỳ Cần Chốt Snapshot:</label>
                        <input type="month" name="period" class="form-control" value="<?= e($selectedPeriod) ?>" required>
                    </div>
                </div>
                <div class="modal-footer bg-light">
                    <button type="button" class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Hủy</button>
                    <button type="submit" class="btn btn-primary btn-sm fw-semibold" style="background:#002F6C; border-color:#002F6C;">
                        Bắt Đầu Chốt Dữ Liệu
                    </button>
                </div>
            </form>
        </div>
    </div>
</div>
<?php endif; ?>

<!-- ========================================================================= -->
<!-- JAVASCRIPT: CHART.JS 4 INITIALIZATION & DRILL-DOWN LOGIC                  -->
<!-- ========================================================================= -->
<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js"></script>

<script>
document.addEventListener('DOMContentLoaded', function() {
    // 1. BIỂU ĐỒ 1: HEADCOUNT & NGHỈ VIỆC 12 THÁNG TRƯỢT
    const ctxTrend = document.getElementById('chartTrend12m')?.getContext('2d');
    if (ctxTrend) {
        new Chart(ctxTrend, {
            type: 'line',
            data: {
                labels: <?= json_encode($trend12m['labels'], JSON_UNESCAPED_UNICODE) ?>,
                datasets: [
                    {
                        label: 'Headcount Hiện Diện',
                        data: <?= json_encode($trend12m['headcount']) ?>,
                        borderColor: '#002F6C',
                        backgroundColor: 'rgba(0, 47, 108, 0.08)',
                        fill: true,
                        tension: 0.35,
                        borderWidth: 3,
                        pointBackgroundColor: '#002F6C',
                        pointRadius: 4,
                        yAxisID: 'y'
                    },
                    {
                        label: 'Số Người Thôi Việc',
                        data: <?= json_encode($trend12m['turnover']) ?>,
                        type: 'bar',
                        backgroundColor: 'rgba(220, 38, 38, 0.75)',
                        borderColor: '#dc2626',
                        borderRadius: 4,
                        barThickness: 16,
                        yAxisID: 'y1'
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: { position: 'top' },
                    tooltip: {
                        callbacks: {
                            label: function(c) {
                                return c.dataset.label + ': ' + c.parsed.y + (c.datasetIndex === 0 ? ' người' : ' người thôi việc');
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        type: 'linear',
                        display: true,
                        position: 'left',
                        title: { display: true, text: 'Headcount (Người)' },
                        grid: { color: 'rgba(0,0,0,0.05)' }
                    },
                    y1: {
                        type: 'linear',
                        display: true,
                        position: 'right',
                        title: { display: true, text: 'Thôi việc (Người)' },
                        grid: { drawOnChartArea: false },
                        min: 0,
                        ticks: { stepSize: 1 }
                    }
                }
            }
        });
    }

    // 2. BIỂU ĐỒ 2: CƠ CẤU THEO KHỐI (CỘT)
    const ctxBlock = document.getElementById('chartBlockDistribution')?.getContext('2d');
    if (ctxBlock) {
        new Chart(ctxBlock, {
            type: 'bar',
            data: {
                labels: <?= json_encode($blockDistribution['labels'], JSON_UNESCAPED_UNICODE) ?>,
                datasets: [{
                    label: 'Số Nhân Sự',
                    data: <?= json_encode($blockDistribution['data']) ?>,
                    backgroundColor: ['#002F6C', '#0047BA', '#00A3E0', '#22C55E', '#F59E0B'],
                    borderRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    y: { beginAtZero: true, grid: { color: 'rgba(0,0,0,0.05)' } }
                }
            }
        });
    }

    // 3. BIỂU ĐỒ 3: XẾP HẠNG GIỜ OT THEO NHÀ MÁY (CỘT NGANG)
    const ctxOt = document.getElementById('chartFactoryOtRanking')?.getContext('2d');
    if (ctxOt) {
        new Chart(ctxOt, {
            type: 'bar',
            data: {
                labels: <?= json_encode($factoryOtRanking['labels'], JSON_UNESCAPED_UNICODE) ?>,
                datasets: [{
                    label: 'Giờ OT Bình Quân (h)',
                    data: <?= json_encode($factoryOtRanking['data']) ?>,
                    backgroundColor: '#0ea5e9',
                    borderRadius: 4
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: {
                    x: {
                        beginAtZero: true,
                        title: { display: true, text: 'Giờ / Người / Tháng' },
                        grid: { color: 'rgba(0,0,0,0.05)' }
                    }
                }
            }
        });
    }

    // 4. BIỂU ĐỒ 4: THÁP TUỔI - GIỚI TÍNH (AGE-GENDER PYRAMID)
    const ctxAgeGender = document.getElementById('chartAgeGenderPyramid')?.getContext('2d');
    if (ctxAgeGender) {
        new Chart(ctxAgeGender, {
            type: 'bar',
            data: {
                labels: <?= json_encode($ageGenderPyramid['labels'], JSON_UNESCAPED_UNICODE) ?>,
                datasets: [
                    {
                        label: 'Nam',
                        data: <?= json_encode($ageGenderPyramid['male']) ?>,
                        backgroundColor: '#002F6C',
                        borderRadius: 4
                    },
                    {
                        label: 'Nữ',
                        data: <?= json_encode($ageGenderPyramid['female']) ?>,
                        backgroundColor: '#ec4899',
                        borderRadius: 4
                    }
                ]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'top' },
                    tooltip: {
                        callbacks: {
                            label: function(c) {
                                return c.dataset.label + ': ' + Math.abs(c.parsed.x) + ' người';
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        ticks: {
                            callback: function(v) { return Math.abs(v); }
                        },
                        title: { display: true, text: '← Nam | Nữ →' },
                        grid: { color: 'rgba(0,0,0,0.05)' }
                    }
                }
            }
        });
    }

    // 5. BIỂU ĐỒ 5: PHÂN BỔ THÂM NIÊN (BAR / DOUGHNUT)
    const ctxTenure = document.getElementById('chartTenureDistribution')?.getContext('2d');
    if (ctxTenure) {
        new Chart(ctxTenure, {
            type: 'doughnut',
            data: {
                labels: <?= json_encode($tenureDistribution['labels'], JSON_UNESCAPED_UNICODE) ?>,
                datasets: [{
                    data: <?= json_encode($tenureDistribution['data']) ?>,
                    backgroundColor: ['#93c5fd', '#3b82f6', '#1d4ed8', '#1e40af', '#172554'],
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'bottom', labels: { boxWidth: 12 } }
                }
            }
        });
    }
});

// =========================================================================
// HÀM DRILL-DOWN MODAL AJAX
// =========================================================================
function openDrilldownModal(metric, title) {
    const period = '<?= e($selectedPeriod) ?>';
    const unit   = '<?= e($selectedUnit) ?>';

    document.getElementById('drilldownModalLabel').innerText = title;
    document.getElementById('drilldownModalSub').innerText = 'Kỳ: ' + period + ' | Đơn vị: ' + unit;
    document.getElementById('drilldownLoading').style.display = 'block';
    document.getElementById('drilldownContent').style.display = 'none';

    const modal = new bootstrap.Modal(document.getElementById('drilldownModal'));
    modal.show();

    fetch('<?= url("dashboard/drilldown") ?>?metric=' + encodeURIComponent(metric) + '&period=' + encodeURIComponent(period) + '&unit=' + encodeURIComponent(unit))
        .then(response => response.json())
        .then(res => {
            document.getElementById('drilldownLoading').style.display = 'none';
            document.getElementById('drilldownContent').style.display = 'block';
            document.getElementById('drilldownCount').innerText = res.count;

            const tbody = document.getElementById('drilldownTbody');
            tbody.innerHTML = '';

            if (!res.data || res.data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="6" class="text-center p-4 text-muted">Không có nhân sự nào trong danh sách cấu thành này.</td></tr>';
                return;
            }

            res.data.forEach((emp, index) => {
                const tr = document.createElement('tr');
                tr.className = 'drilldown-row';
                tr.innerHTML = `
                    <td class="text-center text-muted small">${index + 1}</td>
                    <td class="fw-bold font-monospace text-primary">
                        <a href="<?= url("employees/show") ?>/${emp.MaNV}" class="text-decoration-none" target="_blank">
                            ${emp.MaNV} <i class="bi bi-box-arrow-up-right fs-8"></i>
                        </a>
                    </td>
                    <td>
                        <div class="fw-bold text-dark">${emp.HoTen || 'N/A'}</div>
                        <div class="text-muted fs-8">${emp.Email || ''}</div>
                    </td>
                    <td class="small">${emp.ChucVu || 'Nhân viên'}</td>
                    <td class="small">${emp.TenDV || 'Vinamilk'}</td>
                    <td>
                        <span class="badge bg-light text-dark border">${emp.ChiTietTrangThai || 'Hợp lệ'}</span>
                    </td>
                `;
                tbody.appendChild(tr);
            });
        })
        .catch(err => {
            document.getElementById('drilldownLoading').innerHTML = '<div class="text-danger p-4 text-center">Không thể tải dữ liệu: ' + err.message + '</div>';
        });
}

function filterDrilldownTable() {
    const q = document.getElementById('drilldownFilterInput').value.toLowerCase();
    const rows = document.querySelectorAll('#drilldownTbody tr.drilldown-row');
    rows.forEach(r => {
        r.style.display = r.innerText.toLowerCase().includes(q) ? '' : 'none';
    });
}
</script>

<style>
.kpi-card-hover {
    transition: transform 0.15s ease, box-shadow 0.15s ease;
}
.kpi-card-hover:hover {
    transform: translateY(-3px);
    box-shadow: 0 8px 20px rgba(0,0,0,0.08);
}
.click-hint {
    font-weight: 600;
    transition: opacity 0.2s;
    opacity: 0.7;
}
.kpi-card-hover:hover .click-hint {
    opacity: 1;
    text-decoration: underline;
}
.font-outfit {
    font-family: 'Outfit', 'Segoe UI', sans-serif;
}
.fs-8 {
    font-size: 0.75rem;
}
.btn-xs {
    font-size: 0.75rem;
    padding: 0.15rem 0.5rem;
}
</style>
