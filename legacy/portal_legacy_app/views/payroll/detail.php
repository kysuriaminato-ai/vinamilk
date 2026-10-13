<div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
    <div>
        <a href="<?= url('payroll') ?>" class="btn btn-outline-secondary btn-sm mb-2 no-print">
            <i class="bi bi-arrow-left me-1"></i> Quay lại Danh sách Bảng lương
        </a>
        <h3 class="fw-bold text-dark mb-1">
            <?= e($payroll['ten_bang_luong']) ?>
        </h3>
        <p class="text-muted small mb-0">
            Kỳ lương: <span class="badge bg-primary font-monospace"><?= e($payroll['ky_luong']) ?></span> &bull; 
            Đơn vị: <strong><?= e($payroll['TenDV'] ?: $payroll['ma_dv']) ?></strong> &bull; 
            Tổng số: <strong><?= count($payroll['details']) ?></strong> nhân sự
        </p>
    </div>

    <div class="d-flex gap-2 no-print align-items-center">
        <?php $stt = $payroll['trang_thai'] ?? 'NHAP'; ?>
        
        <?php if (in_array($stt, ['NHAP', 'DA_TINH', 'DANG_TINH']) && Auth::hasRole(['ADMIN', 'HR_TAPDOAN', 'KETOAN_LUONG'])): ?>
            <form action="<?= url('payroll/accountant_approve') ?>" method="POST" class="d-inline">
                <input type="hidden" name="csrf_token" value="<?= csrf_token() ?>">
                <input type="hidden" name="id" value="<?= $payroll['id'] ?>">
                <button type="submit" class="btn btn-outline-info btn-sm fw-semibold">
                    <i class="bi bi-check-circle me-1"></i> Kế Toán Duyệt
                </button>
            </form>
        <?php endif; ?>

        <?php if ($stt === 'KETOAN_DUYET' && Auth::hasRole(['ADMIN', 'BOD'])): ?>
            <form action="<?= url('payroll/director_approve') ?>" method="POST" class="d-inline">
                <input type="hidden" name="csrf_token" value="<?= csrf_token() ?>">
                <input type="hidden" name="id" value="<?= $payroll['id'] ?>">
                <button type="submit" class="btn btn-outline-success btn-sm fw-semibold">
                    <i class="bi bi-check2-all me-1"></i> Giám Đốc Duyệt
                </button>
            </form>
        <?php endif; ?>

        <?php if ($stt === 'GIAMDOC_DUYET' && Auth::hasRole(['ADMIN', 'BOD', 'KETOAN_LUONG'])): ?>
            <form action="<?= url('payroll/disburse') ?>" method="POST" class="d-inline" onsubmit="return confirm('CẢNH BÁO: Sau khi bấm ĐÃ CHI, bảng lương sẽ được KHÓA CỨNG và không thể chỉnh sửa hay tính lại. Bạn có chắc chắn?')">
                <input type="hidden" name="csrf_token" value="<?= csrf_token() ?>">
                <input type="hidden" name="id" value="<?= $payroll['id'] ?>">
                <button type="submit" class="btn btn-success btn-sm fw-semibold">
                    <i class="bi bi-cash-stack me-1"></i> Đã Chi Trả & Khóa Lương
                </button>
            </form>
        <?php endif; ?>

        <?php if (in_array($stt, ['DA_CHI', 'DA_CHI_TRA'])): ?>
            <span class="badge bg-dark py-2 px-3 border border-warning text-white">
                <i class="bi bi-lock-fill text-warning me-1"></i> ĐÃ CHI (ĐÃ KHÓA SỔ)
            </span>
        <?php endif; ?>

        <button onclick="window.print()" class="btn btn-outline-primary btn-sm fw-semibold">
            <i class="bi bi-printer-fill me-1"></i> In A4 Bảng Lương
        </button>
    </div>
</div>

<!-- Header bản in A4 -->
<div class="d-none d-print-block text-center mb-3">
    <h4 class="fw-bold text-uppercase mb-1">CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK)</h4>
    <h5 class="fw-bold mb-1"><?= e($payroll['ten_bang_luong']) ?></h5>
    <p class="small text-muted mb-0">Ngày tính: <?= format_date($payroll['created_at']) ?> &bull; Người lập: <?= e($payroll['nguoi_lap']) ?></p>
</div>

<!-- Thẻ tổng hợp số liệu -->
<div class="row g-3 mb-4">
    <div class="col-md-3">
        <div class="card card-custom p-3 border-start border-4 border-primary">
            <div class="text-muted small">Tổng Thu Nhập Trước Giảm Trừ</div>
            <div class="fs-4 fw-bold text-dark mt-1"><?= format_money($payroll['tong_thu_nhap']) ?></div>
        </div>
    </div>
    <div class="col-md-3">
        <div class="card card-custom p-3 border-start border-4 border-danger">
            <div class="text-muted small">Tổng Trích BH (10.5% NV)</div>
            <div class="fs-4 fw-bold text-danger mt-1"><?= format_money($payroll['tong_bao_hiem']) ?></div>
        </div>
    </div>
    <div class="col-md-3">
        <div class="card card-custom p-3 border-start border-4 border-warning">
            <div class="text-muted small">Tổng Thuế TNCN Khấu Trừ</div>
            <div class="fs-4 fw-bold text-warning text-dark mt-1"><?= format_money($payroll['tong_thue_tncn']) ?></div>
        </div>
    </div>
    <div class="col-md-3">
        <div class="card card-custom p-3 border-start border-4 border-success">
            <div class="text-muted small">Tổng Thực Lĩnh Chi Trả</div>
            <div class="fs-4 fw-bold text-success mt-1"><?= format_money($payroll['tong_thuc_linh']) ?></div>
        </div>
    </div>
</div>

<!-- Bảng chi tiết lương từng nhân viên -->
<div class="card card-custom overflow-hidden">
    <div class="table-responsive">
        <table class="table table-bordered table-hover align-middle mb-0" style="font-size: 13px;">
            <thead class="table-light text-center small text-uppercase fw-bold align-middle">
                <tr>
                    <th rowspan="2" style="width: 40px;">STT</th>
                    <th rowspan="2">Mã NV</th>
                    <th rowspan="2">Họ và Tên</th>
                    <th rowspan="2">Đơn Vị</th>
                    <th colspan="4" class="text-center bg-primary bg-opacity-10 text-primary">Thu Nhập (VND)</th>
                    <th colspan="3" class="text-center bg-danger bg-opacity-10 text-danger">Các Khoản Trích Nộp Bảo Hiểm</th>
                    <th rowspan="2">Thuế TNCN</th>
                    <th rowspan="2" class="bg-success bg-opacity-10 text-success">Thực Lĩnh (VND)</th>
                    <th rowspan="2" class="no-print" style="width: 60px;">Phiếu</th>
                </tr>
                <tr>
                    <th>Lương P1</th>
                    <th>Lương P2</th>
                    <th>KPI P3</th>
                    <th>Tổng TN</th>
                    <th>BHXH (8%)</th>
                    <th>BHYT (1.5%)</th>
                    <th>BHTN (1%)</th>
                </tr>
            </thead>
            <tbody>
                <?php 
                    $stt = 1;
                    foreach ($payroll['details'] as $row): 
                ?>
                <tr>
                    <td class="text-center text-muted"><?= $stt++ ?></td>
                    <td class="font-monospace fw-bold text-primary"><?= e($row['ma_nv']) ?></td>
                    <td class="fw-semibold text-dark"><?= e($row['ho_ten']) ?></td>
                    <td><small class="text-muted"><?= e($row['ten_dv']) ?></small></td>
                    
                    <!-- Thu nhập -->
                    <td class="text-end"><?= format_money($row['luong_p1_hop_dong']) ?></td>
                    <td class="text-end"><?= format_money($row['luong_p2_hieu_suat']) ?></td>
                    <td class="text-end"><?= format_money($row['luong_p3_kpi']) ?></td>
                    <td class="text-end fw-bold text-dark"><?= format_money($row['tong_thu_nhap']) ?></td>

                    <!-- Bảo hiểm -->
                    <td class="text-end text-danger"><?= format_money($row['bhxh_nv']) ?></td>
                    <td class="text-end text-danger"><?= format_money($row['bhyt_nv']) ?></td>
                    <td class="text-end text-danger"><?= format_money($row['bhtn_nv']) ?></td>

                    <!-- Thuế -->
                    <td class="text-end text-warning text-dark fw-semibold"><?= format_money($row['thue_tncn']) ?></td>

                    <!-- Thực lĩnh -->
                    <td class="text-end fw-bold text-success fs-6"><?= format_money($row['thuc_linh']) ?></td>

                    <!-- Nút xem phiếu lương -->
                    <td class="text-center no-print">
                        <div class="d-flex justify-content-center gap-1">
                            <a href="<?= url('payroll/payslip/' . $row['ma_nv'] . '/' . $payroll['ky_luong']) ?>" class="btn btn-sm btn-outline-info py-0 px-2" title="Xem phiếu lương cá nhân (Web)">
                                <i class="bi bi-file-earmark-text"></i>
                            </a>
                            <a href="<?= url('payroll/pdf_payslip/' . $payroll['id'] . '/' . $row['ma_nv']) ?>" target="_blank" class="btn btn-sm btn-outline-primary py-0 px-2" title="Xuất / In Phiếu Lương PDF điện tử (ESS)">
                                <i class="bi bi-file-earmark-pdf-fill"></i>
                            </a>
                        </div>
                    </td>
                </tr>
                <?php endforeach; ?>
            </tbody>
        </table>
    </div>
</div>
