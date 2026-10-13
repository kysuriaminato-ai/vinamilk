<?php
/**
 * =====================================================================
 * VINAMILK HRM - AI CHUYÊN GIA DỰ BÁO RỦI RO & ĐỊNH BIÊN (index.php)
 * =====================================================================
 * ĐÁP ỨNG ĐẦY ĐỦ YÊU CẦU:
 * - Ghi rõ nhãn: "Mô hình huấn luyện trên dữ liệu giả lập"
 * - Hiển thị Top nhân sự rủi ro nghỉ việc, 3 lý do chính mỗi người, khuyến nghị hành động
 * - Dự báo định biên: Công suất ÷ Năng suất/người - Dự báo thôi việc - Nghỉ hưu BLLĐ 2019
 */

$userRole = Auth::role();
$userUnit = Auth::maDV() ?: 'Toàn tập đoàn';
?>

<!-- BANNER BẮT BUỘC: MÔ HÌNH HUẤN LUYỆN TRÊN DỮ LIỆU GIẢ LẬP -->
<div class="alert alert-warning border-warning d-flex align-items-center justify-content-between p-3 mb-4 shadow-sm" role="alert">
    <div class="d-flex align-items-center gap-3">
        <div class="fs-2 text-warning"><i class="bi bi-robot"></i></div>
        <div>
            <h6 class="alert-heading fw-bold mb-1">
                <i class="bi bi-info-circle-fill me-1"></i>Hệ Chuyên Gia AI Dự Báo Rủi Ro Nghỉ Việc & Định Biên Nguồn Lực
            </h6>
            <div class="small text-dark">
                <strong>LƯU Ý QUAN TRỌNG:</strong> <span class="badge bg-dark text-white px-2 py-1">Mô hình huấn luyện trên dữ liệu giả lập</span> 
                (Synthetic Dataset: 1.000 nhân viên &times; 24 tháng theo quy luật thâm niên, tăng lương, giờ OT và KPI). 
                Dữ liệu phục vụ hỗ trợ ra quyết định giữ chân nhân tài và quy hoạch định biên nhân sự.
            </div>
        </div>
    </div>
    <div class="d-none d-md-block text-end">
        <span class="badge bg-primary px-3 py-2">Algorithm: Logistic Regression Standardized</span>
    </div>
</div>

<!-- THANH TIÊU ĐỀ & ĐIỀU HƯỚNG TỔNG QUAN -->
<div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
    <div>
        <h3 class="fw-bold text-dark mb-1">
            <i class="bi bi-cpu-fill text-primary me-2"></i>AI Retention & Workforce Capacity Planning
        </h3>
        <p class="text-muted small mb-0">
            Hệ thống chấm điểm rủi ro nghỉ việc nhân sự hằng tháng & Công cụ dự báo định biên theo Bộ luật Lao động 2019 &bull; Phạm vi: 
            <span class="badge bg-primary"><?= e($userRole) ?></span> 
            <span class="badge bg-secondary"><?= e($userUnit) ?></span>
        </p>
    </div>

    <div class="d-flex gap-2 align-items-center flex-wrap">
        <a href="<?= url('dashboard') ?>" class="btn btn-outline-secondary btn-sm fw-semibold">
            <i class="bi bi-arrow-left me-1"></i> Dashboard Điều Hành
        </a>
        <a href="<?= url('kpi-dictionary') ?>" class="btn btn-outline-primary btn-sm fw-semibold">
            <i class="bi bi-book-half me-1"></i> Từ Điển Chỉ Số
        </a>
        <?php if (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'BOD'])): ?>
            <button type="button" class="btn btn-sm btn-primary fw-semibold" style="background:#002F6C; border-color:#002F6C;" data-bs-toggle="modal" data-bs-target="#rescoreModal">
                <i class="bi bi-arrow-repeat me-1"></i> Chạy Lại Chấm Điểm AI
            </button>
        <?php endif; ?>
    </div>
</div>

<!-- BỘ LỌC KỲ VÀ ĐƠN VỊ (SCOPE BY UNIT) -->
<div class="card card-custom p-3 mb-4 shadow-sm border-0">
    <form method="GET" action="<?= url('ai-expert') ?>" class="row g-2 align-items-center">
        <div class="col-md-3">
            <label class="form-label small fw-bold text-muted mb-1"><i class="bi bi-calendar-month me-1"></i>Kỳ Đánh Giá:</label>
            <input type="month" name="period" class="form-control form-control-sm fw-semibold" value="<?= e($selectedPeriod) ?>" onchange="this.form.submit()">
        </div>

        <div class="col-md-6">
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

        <div class="col-md-3 text-md-end pt-md-3">
            <button type="submit" class="btn btn-primary btn-sm px-3 fw-semibold" style="background:#002F6C; border-color:#002F6C;">
                <i class="bi bi-funnel-fill me-1"></i> Lọc dữ liệu AI
            </button>
        </div>
    </form>
</div>

<!-- 4 THẺ THỐNG KÊ RỦI RO NGHỈ VIỆC -->
<div class="row g-3 mb-4">
    <div class="col-sm-6 col-xl-3">
        <div class="card card-custom p-3 h-100 border-start border-4 border-primary shadow-sm">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="text-muted small fw-semibold text-uppercase">Nhân Sự Đánh Giá</div>
                    <div class="fs-3 fw-bold text-dark mt-1 font-outfit"><?= number_format($summary['TongNhanSu']) ?></div>
                    <div class="small text-muted mt-1">Lực lượng lao động hiện diện</div>
                </div>
                <div class="bg-primary bg-opacity-10 p-3 rounded-circle text-primary fs-3">
                    <i class="bi bi-people-fill"></i>
                </div>
            </div>
        </div>
    </div>

    <div class="col-sm-6 col-xl-3">
        <div class="card card-custom p-3 h-100 border-start border-4 border-danger shadow-sm">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="text-muted small fw-semibold text-uppercase">Rủi Ro Cao (High Risk)</div>
                    <div class="fs-3 fw-bold text-danger mt-1 font-outfit"><?= number_format($summary['SoRuiRoCao']) ?></div>
                    <div class="small text-danger mt-1"><i class="bi bi-exclamation-triangle-fill me-1"></i>Xác suất nghỉ việc &ge; 5.0%</div>
                </div>
                <div class="bg-danger bg-opacity-10 p-3 rounded-circle text-danger fs-3">
                    <i class="bi bi-person-x-fill"></i>
                </div>
            </div>
        </div>
    </div>

    <div class="col-sm-6 col-xl-3">
        <div class="card card-custom p-3 h-100 border-start border-4 border-warning shadow-sm">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="text-muted small fw-semibold text-uppercase">Rủi Ro Trung Bình</div>
                    <div class="fs-3 fw-bold text-warning mt-1 font-outfit"><?= number_format($summary['SoRuiRoTB']) ?></div>
                    <div class="small text-muted mt-1">Xác suất nghỉ việc 3.0% - 5.0%</div>
                </div>
                <div class="bg-warning bg-opacity-10 p-3 rounded-circle text-warning fs-3">
                    <i class="bi bi-shield-exclamation"></i>
                </div>
            </div>
        </div>
    </div>

    <div class="col-sm-6 col-xl-3">
        <div class="card card-custom p-3 h-100 border-start border-4 border-success shadow-sm">
            <div class="d-flex justify-content-between align-items-center">
                <div>
                    <div class="text-muted small fw-semibold text-uppercase">Rủi Ro Thấp / Ổn Định</div>
                    <div class="fs-3 fw-bold text-success mt-1 font-outfit"><?= number_format($summary['SoRuiRoThap']) ?></div>
                    <div class="small text-success mt-1"><i class="bi bi-check-circle-fill me-1"></i>Tâm lý ổn định, gắn bó</div>
                </div>
                <div class="bg-success bg-opacity-10 p-3 rounded-circle text-success fs-3">
                    <i class="bi bi-shield-check"></i>
                </div>
            </div>
        </div>
    </div>
</div>

<!-- ========================================================================= -->
<!-- BẢNG TOP NHÂN VIÊN RỦI RO NGHỈ VIỆC, 3 LÝ DO CHÍNH & KHUYẾN NGHỊ HÀNH ĐỘNG  -->
<!-- ========================================================================= -->
<div class="card card-custom shadow-sm mb-4">
    <div class="card-header bg-white py-3 border-bottom d-flex justify-content-between align-items-center flex-wrap gap-2">
        <div>
            <h5 class="fw-bold text-dark mb-0">
                <i class="bi bi-shield-lock-fill text-danger me-2"></i>Top Nhân Sự Có Nguy Cơ Thôi Việc Cao Nhất
            </h5>
            <div class="text-muted fs-8">
                Mô hình phân tích tự động dựa trên 8 đặc trưng hành vi và đãi ngộ (Huấn luyện trên dữ liệu giả lập)
            </div>
        </div>
        <span class="badge bg-danger bg-opacity-10 text-danger border border-danger px-3 py-2 fw-semibold">
            <i class="bi bi-bell-fill me-1"></i> Ưu tiên can thiệp giữ chân nhân sự
        </span>
    </div>
    <div class="card-body p-0">
        <div class="table-responsive">
            <table class="table table-hover align-middle mb-0">
                <thead class="table-light fs-8 text-uppercase text-muted border-bottom">
                    <tr>
                        <th style="width: 5%;" class="text-center">Hạng</th>
                        <th style="width: 15%;">Nhân Viên</th>
                        <th style="width: 15%;">Đơn Vị & Vị Trí</th>
                        <th style="width: 12%;">Xác Suất Rủi Ro</th>
                        <th style="width: 28%;">3 Lý Do Chính Đóng Góp Rủi Ro</th>
                        <th style="width: 25%;">Khuyến Nghị Hành Động Của AI</th>
                    </tr>
                </thead>
                <tbody>
                    <?php if (empty($topRisky)): ?>
                        <tr>
                            <td colspan="6" class="text-center p-5 text-muted">
                                <i class="bi bi-check2-circle text-success fs-1 d-block mb-2"></i>
                                Không có nhân sự nào trong phạm vi rủi ro thôi việc kỳ này.
                            </td>
                        </tr>
                    <?php else: ?>
                        <?php foreach ($topRisky as $idx => $r): ?>
                            <?php 
                                $riskPct = round((float)$r['xac_suat_nghi_viec'] * 100, 1);
                                $badgeClass = match($r['muc_do_rui_ro']) {
                                    'CAO'         => 'bg-danger text-white',
                                    'TRUNG_BINH'  => 'bg-warning text-dark',
                                    default       => 'bg-success text-white'
                                };
                            ?>
                            <tr>
                                <td class="text-center fw-bold text-muted"><?= $idx + 1 ?></td>
                                <td>
                                    <div class="fw-bold text-dark"><?= e($r['HoTen']) ?></div>
                                    <div class="small font-monospace text-primary">
                                        <a href="<?= url('employees/show/' . $r['ma_nv']) ?>" class="text-decoration-none" target="_blank">
                                            <?= e($r['ma_nv']) ?> <i class="bi bi-box-arrow-up-right fs-8"></i>
                                        </a>
                                    </div>
                                    <div class="text-muted fs-8"><?= e($r['Email'] ?? '') ?></div>
                                </td>
                                <td>
                                    <div class="fw-semibold text-dark small"><?= e($r['TenDV']) ?></div>
                                    <div class="text-muted fs-8"><?= e($r['ChucVu'] ?: 'Nhân viên') ?></div>
                                </td>
                                <td>
                                    <div class="d-flex align-items-center gap-2 mb-1">
                                        <span class="badge <?= $badgeClass ?>"><?= e($r['muc_do_rui_ro']) ?></span>
                                        <span class="fw-bold fs-6 <?= $riskPct >= 5.0 ? 'text-danger' : 'text-dark' ?>"><?= $riskPct ?>%</span>
                                    </div>
                                    <div class="progress" style="height: 6px;">
                                        <div class="progress-bar <?= $riskPct >= 5.0 ? 'bg-danger' : 'bg-warning' ?>" 
                                             role="progressbar" 
                                             style="width: <?= min(100, $riskPct * 15) ?>%;"></div>
                                    </div>
                                </td>
                                <td>
                                    <ul class="list-unstyled mb-0 small ps-1" style="line-height: 1.5;">
                                        <li class="mb-1 text-danger">
                                            <i class="bi bi-1-circle-fill me-1"></i><?= e($r['ly_do_1']) ?>
                                        </li>
                                        <li class="mb-1 text-warning text-dark">
                                            <i class="bi bi-2-circle-fill me-1"></i><?= e($r['ly_do_2']) ?>
                                        </li>
                                        <li class="text-muted">
                                            <i class="bi bi-3-circle-fill me-1"></i><?= e($r['ly_do_3']) ?>
                                        </li>
                                    </ul>
                                </td>
                                <td>
                                    <div class="p-2 bg-light rounded small border border-primary border-opacity-25" style="line-height: 1.6;">
                                        <strong class="text-primary"><i class="bi bi-lightbulb-fill me-1"></i>Hành động:</strong> 
                                        <?= e($r['khuyen_nghi_hanh_dong']) ?>
                                    </div>
                                    <div class="mt-1 text-end">
                                        <a href="<?= url('employees/show/' . $r['ma_nv']) ?>" class="btn btn-outline-primary btn-xs py-0 px-2 fw-semibold">
                                            Lập kế hoạch giữ chân &rarr;
                                        </a>
                                    </div>
                                </td>
                            </tr>
                        <?php endforeach; ?>
                    <?php endif; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>

<!-- ========================================================================= -->
<!-- CÔNG CỤ DỰ BÁO ĐỊNH BIÊN & LỘ TRÌNH TUỔI NGHỈ HƯU BỘ LUẬT LAO ĐỘNG 2019  -->
<!-- ========================================================================= -->
<div class="card card-custom shadow-sm mb-4">
    <div class="card-header bg-white py-3 border-bottom d-flex justify-content-between align-items-center flex-wrap gap-2">
        <div>
            <h5 class="fw-bold text-dark mb-0">
                <i class="bi bi-calculator-fill text-primary me-2"></i>Dự Báo Định Biên Nhân Lực & Lộ Trình Nghỉ Hưu BLLĐ 2019
            </h5>
            <div class="text-muted fs-8">
                Công thức: Headcount kỳ vọng = (Kế hoạch công suất &divide; Năng suất/người) - Dự báo thôi việc (AI) - Dự báo nghỉ hưu (Luật lao động)
            </div>
        </div>
        <span class="badge bg-info bg-opacity-10 text-primary border border-primary px-3 py-2 fw-semibold">
            Điều 169 BLLĐ 2019 &bull; Năm 2026: Nam 61t 6th | Nữ 57t
        </span>
    </div>

    <div class="card-body p-4">
        <!-- FORM ĐIỀU CHỈNH THAM SỐ CÔNG SUẤT -->
        <form method="GET" action="<?= url('ai-expert') ?>" class="row g-3 p-3 bg-light rounded border mb-4">
            <input type="hidden" name="period" value="<?= e($selectedPeriod) ?>">
            <input type="hidden" name="unit" value="<?= e($selectedUnit) ?>">

            <div class="col-md-4">
                <label class="form-label small fw-bold text-dark mb-1">Kế Hoạch Công Suất / Sản Lượng Tháng:</label>
                <div class="input-group input-group-sm">
                    <input type="number" name="capacity" class="form-control fw-semibold" value="<?= e($forecast['ke_hoach_cong_suat']) ?>" step="100000" required>
                    <span class="input-group-text">lít sữa/tháng</span>
                </div>
            </div>

            <div class="col-md-4">
                <label class="form-label small fw-bold text-dark mb-1">Năng Suất Định Mức Bình Quân / Người:</label>
                <div class="input-group input-group-sm">
                    <input type="number" name="productivity" class="form-control fw-semibold" value="<?= e($forecast['nang_suat_nguoi']) ?>" step="1000" required>
                    <span class="input-group-text">lít/người/tháng</span>
                </div>
            </div>

            <div class="col-md-2">
                <label class="form-label small fw-bold text-dark mb-1">Năm Dự Báo:</label>
                <select name="year" class="form-select form-select-sm fw-semibold">
                    <option value="2026" <?= ($forecast['nam_ap_dung'] == 2026) ? 'selected' : '' ?>>2026 (Nam 61.5t, Nữ 57t)</option>
                    <option value="2027" <?= ($forecast['nam_ap_dung'] == 2027) ? 'selected' : '' ?>>2027 (Nam 61.75t, Nữ 57.3t)</option>
                    <option value="2028" <?= ($forecast['nam_ap_dung'] == 2028) ? 'selected' : '' ?>>2028 (Nam 62t, Nữ 57.7t)</option>
                </select>
            </div>

            <div class="col-md-2 d-flex align-items-end">
                <button type="submit" class="btn btn-primary btn-sm w-100 fw-semibold" style="background:#002F6C; border-color:#002F6C;">
                    <i class="bi bi-arrow-repeat me-1"></i> Tính toán lại
                </button>
            </div>
        </form>

        <!-- KẾT QUẢ DỰ BÁO ĐỊNH BIÊN 4 THẺ SỐ LIỆU -->
        <div class="row g-3 mb-4">
            <div class="col-sm-6 col-lg-3">
                <div class="p-3 bg-white border rounded text-center">
                    <div class="text-muted small fw-semibold text-uppercase">1. Định Biên Gộp Cần Thiết</div>
                    <div class="fs-2 fw-bold text-dark mt-1 font-outfit"><?= number_format($forecast['dinh_bien_gop']) ?></div>
                    <div class="small text-muted">Công suất &divide; Năng suất định mức</div>
                </div>
            </div>

            <div class="col-sm-6 col-lg-3">
                <div class="p-3 bg-white border rounded text-center">
                    <div class="text-muted small fw-semibold text-uppercase">2. Dự Báo Thôi Việc (AI)</div>
                    <div class="fs-2 fw-bold text-danger mt-1 font-outfit">-<?= number_format($forecast['du_bao_thoi_viec']) ?></div>
                    <div class="small text-danger">Nhân sự rủi ro cao kỳ này</div>
                </div>
            </div>

            <div class="col-sm-6 col-lg-3">
                <div class="p-3 bg-white border rounded text-center">
                    <div class="text-muted small fw-semibold text-uppercase">3. Dự Báo Nghỉ Hưu (BLLĐ 2019)</div>
                    <div class="fs-2 fw-bold text-warning mt-1 font-outfit">-<?= number_format($forecast['du_bao_nghi_huu']) ?></div>
                    <div class="small text-muted">Nam &ge; 61.5t, Nữ &ge; 57t trong năm</div>
                </div>
            </div>

            <div class="col-sm-6 col-lg-3">
                <div class="p-3 bg-white border rounded text-center border-primary border-2">
                    <div class="text-primary small fw-semibold text-uppercase">4. Nhu Cầu Tuyển Dụng Bổ Sung</div>
                    <div class="fs-2 fw-bold text-primary mt-1 font-outfit"><?= number_format($forecast['nhu_cau_tuyen_dung']) ?></div>
                    <div class="small text-muted">Khuyết định biên vận hành ròng</div>
                </div>
            </div>
        </div>

        <!-- DANH SÁCH NHÂN SỰ CHẠM MỐC TUỔI NGHỈ HƯU BLLĐ 2019 -->
        <h6 class="fw-bold text-dark mb-2">
            <i class="bi bi-person-badge-fill text-warning me-1"></i>Danh Sách Nhân Sự Chạm Mốc / Tiệm Cận Tuổi Nghỉ Hưu Trong Năm <?= e($forecast['nam_ap_dung']) ?>:
        </h6>
        <div class="table-responsive">
            <table class="table table-bordered table-sm align-middle mb-0">
                <thead class="table-light fs-8 text-uppercase text-muted">
                    <tr>
                        <th style="width: 15%;">Mã NV</th>
                        <th style="width: 25%;">Họ Và Tên</th>
                        <th style="width: 10%;">Giới Tính</th>
                        <th style="width: 15%;">Ngày Sinh</th>
                        <th style="width: 10%;">Tuổi Hiện Tại</th>
                        <th style="width: 25%;">Tình Trạng Nghỉ Hưu (Lộ Trình BLLĐ 2019)</th>
                    </tr>
                </thead>
                <tbody>
                    <?php if (empty($forecast['danh_sach_nghi_huu'])): ?>
                        <tr>
                            <td colspan="6" class="text-center p-3 text-muted small">
                                <i class="bi bi-check-circle text-success me-1"></i>
                                Không có cán bộ nhân viên nào chạm hoặc tiệm cận tuổi nghỉ hưu trong năm <?= e($forecast['nam_ap_dung']) ?>.
                            </td>
                        </tr>
                    <?php else: ?>
                        <?php foreach ($forecast['danh_sach_nghi_huu'] as $ret): ?>
                            <tr>
                                <td class="font-monospace fw-bold text-primary"><?= e($ret['MaNV']) ?></td>
                                <td class="fw-semibold text-dark"><?= e($ret['HoTen']) ?></td>
                                <td>
                                    <span class="badge <?= $ret['GioiTinh'] === 'Nam' ? 'bg-primary' : 'bg-danger' ?>">
                                        <?= e($ret['GioiTinh']) ?>
                                    </span>
                                </td>
                                <td><?= date('d/m/Y', strtotime($ret['NgaySinh'])) ?></td>
                                <td class="fw-bold"><?= (int)$ret['Tuoi'] ?> tuổi</td>
                                <td>
                                    <span class="badge bg-warning text-dark border">
                                        <?= e($ret['TinhTrangHuu']) ?>
                                    </span>
                                </td>
                            </tr>
                        <?php endforeach; ?>
                    <?php endif; ?>
                </tbody>
            </table>
        </div>
    </div>
</div>

<!-- MODAL CHẠY LẠI CHẤM ĐIỂM AI (ADMIN ONLY) -->
<?php if (Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'BOD'])): ?>
<div class="modal fade" id="rescoreModal" tabindex="-1" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content border-0 shadow">
            <form method="POST" action="<?= url('ai-expert/rescore') ?>">
                <?= csrf_field() ?>
                <div class="modal-header text-white" style="background:#002F6C;">
                    <h5 class="modal-title fw-bold"><i class="bi bi-cpu me-2"></i>Chấm Điểm Rủi Ro Nghỉ Việc AI</h5>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body p-4">
                    <p class="text-muted small">
                        Hệ thống sẽ nạp hệ số từ file <code>storage/models/turnover_model.json</code>, thu thập 8 đặc trưng thực tế của toàn bộ nhân sự và cập nhật kết quả vào bảng <code>ai_risk_score</code>.
                    </p>
                    <div class="mb-3">
                        <label class="form-label fw-semibold">Chọn Kỳ Đánh Giá:</label>
                        <input type="month" name="period" class="form-control" value="<?= e($selectedPeriod) ?>" required>
                    </div>
                </div>
                <div class="modal-footer bg-light">
                    <button type="button" class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Hủy</button>
                    <button type="submit" class="btn btn-primary btn-sm fw-semibold" style="background:#002F6C; border-color:#002F6C;">
                        Bắt Đầu Chấm Điểm
                    </button>
                </div>
            </form>
        </div>
    </div>
</div>
<?php endif; ?>
