<div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
    <div>
        <a href="<?= url('payroll') ?>" class="btn btn-outline-secondary btn-sm mb-2">
            <i class="bi bi-arrow-left me-1"></i> Quay lại Phân hệ Lương
        </a>
        <h3 class="fw-bold text-dark mb-1">
            <i class="bi bi-shield-check text-primary me-2"></i>Bảng Tham Số Pháp Lý & Biểu Thuế
        </h3>
        <p class="text-muted small mb-0">
            Toàn bộ công thức tính lương của Vinamilk HRM đọc trực tiếp từ bảng <code>cau_hinh_luong</code> và <code>bieu_thue_tncn</code> theo ngày hiệu lực. Tuyệt đối không hard-code bất kỳ hằng số nào trong mã nguồn.
        </p>
    </div>
</div>

<!-- 1. BẢNG THAM SỐ TIỀN LƯƠNG & BẢO HIỂM PHÁP LÝ (cau_hinh_luong) -->
<div class="card card-custom p-4 mb-4">
    <div class="d-flex justify-content-between align-items-center mb-3">
        <h5 class="fw-bold text-dark mb-0">
            <i class="bi bi-sliders text-primary me-2"></i>1. Bảng Tham Số Pháp Lý Tiền Lương & Bảo Hiểm (<code>cau_hinh_luong</code>)
        </h5>
        <span class="badge bg-primary">Dynamic Parameters</span>
    </div>

    <div class="table-responsive">
        <table class="table table-bordered table-hover align-middle mb-0" style="font-size: 13px;">
            <thead class="table-light text-muted small text-uppercase fw-bold">
                <tr>
                    <th>Mã Tham Số</th>
                    <th>Tên Tham Số / Diễn Giải</th>
                    <th class="text-end">Giá Trị CSDL</th>
                    <th class="text-center">Đơn Vị</th>
                    <th class="text-center">Ngày Hiệu Lực</th>
                    <th>Văn Bản Pháp Lý Căn Cứ</th>
                </tr>
            </thead>
            <tbody>
                <?php foreach ($legalRows as $r): ?>
                <tr>
                    <td class="font-monospace fw-bold text-primary"><?= e($r['ma_tham_so']) ?></td>
                    <td class="fw-semibold text-dark"><?= e($r['ten_tham_so']) ?></td>
                    <td class="text-end fw-bold text-success fs-6">
                        <?= ($r['don_vi_tinh'] === 'VND') ? format_money($r['gia_tri']) : (($r['don_vi_tinh'] === '%') ? ($r['gia_tri'] * 100) . '%' : number_format($r['gia_tri'])) ?>
                    </td>
                    <td class="text-center"><span class="badge bg-light text-dark border"><?= e($r['don_vi_tinh']) ?></span></td>
                    <td class="text-center"><?= format_date($r['ngay_hieu_luc']) ?></td>
                    <td><small class="text-muted"><?= e($r['van_ban_phap_ly'] ?: 'Quy chế công ty') ?></small></td>
                </tr>
                <?php endforeach; ?>
            </tbody>
        </table>
    </div>
</div>

<!-- 2. BIỂU THUẾ THU NHẬP CÁ NHÂN LŨY TIẾN TỪNG PHẦN (bieu_thue_tncn) -->
<div class="card card-custom p-4">
    <div class="d-flex justify-content-between align-items-center mb-3">
        <h5 class="fw-bold text-dark mb-0">
            <i class="bi bi-graph-up text-primary me-2"></i>2. Biểu Thuế Thu Nhập Cá Nhân (TNCN) Lũy Tiến Từng Phần (<code>bieu_thue_tncn</code>)
        </h5>
        <span class="badge bg-success">Luật Thuế TNCN Hiện Hành</span>
    </div>

    <div class="table-responsive">
        <table class="table table-bordered table-hover align-middle mb-0" style="font-size: 13px;">
            <thead class="table-light text-muted small text-uppercase fw-bold text-center">
                <tr>
                    <th>Bậc Thuế</th>
                    <th>Thu Nhập Tính Thuế / Tháng (Từ)</th>
                    <th>Thu Nhập Tính Thuế / Tháng (Đến)</th>
                    <th>Thuế Suất (%)</th>
                    <th>Cách Tính Nhanh (Số trừ)</th>
                </tr>
            </thead>
            <tbody>
                <?php foreach ($taxRows as $t): ?>
                <tr>
                    <td class="text-center fw-bold font-monospace fs-6 text-primary">Bậc <?= e($t['bac_thue']) ?></td>
                    <td class="text-end"><?= format_money($t['thu_nhap_tu']) ?></td>
                    <td class="text-end"><?= $t['thu_nhap_den'] ? format_money($t['thu_nhap_den']) : '<span class="text-muted">Trên ' . format_money($t['thu_nhap_tu']) . '</span>' ?></td>
                    <td class="text-center fw-bold text-danger fs-6"><?= (float)$t['thue_suat'] ?>%</td>
                    <td class="text-end text-muted"><?= format_money($t['so_tru_nhanh']) ?></td>
                </tr>
                <?php endforeach; ?>
            </tbody>
        </table>
    </div>
</div>
