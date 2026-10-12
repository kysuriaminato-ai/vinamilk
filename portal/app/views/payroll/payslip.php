<div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2 no-print">
    <div>
        <h3 class="fw-bold text-dark mb-1">
            <i class="bi bi-wallet2 text-primary me-2"></i>Phiếu Lương Điện Tử (Payslip)
        </h3>
        <p class="text-muted small mb-0">Tra cứu chi tiết thu nhập, trích nộp bảo hiểm và thuế TNCN theo quy chuẩn Vinamilk.</p>
    </div>

    <div class="d-flex gap-2">
        <a href="<?= url('payroll') ?>" class="btn btn-outline-secondary btn-sm">
            <i class="bi bi-arrow-left me-1"></i> Quay lại
        </a>
        <button onclick="window.print()" class="btn btn-primary btn-sm fw-semibold shadow-sm" style="background: #002F6C; border-color: #002F6C;">
            <i class="bi bi-printer-fill me-1"></i> In Phiếu Lương A4
        </button>
    </div>
</div>

<?php if (!$payslip): ?>
    <div class="card card-custom p-5 text-center text-muted">
        <i class="bi bi-exclamation-circle fs-1 text-warning d-block mb-3"></i>
        <h5 class="fw-bold text-dark">Chưa Có Dữ Liệu Bảng Lương</h5>
        <p class="small mb-0">Hồ sơ nhân sự này chưa được tính lương trong kỳ đã chọn. Vui lòng liên hệ Kế toán tiền lương.</p>
    </div>
<?php else: ?>

<!-- PHIẾU LƯƠNG CHUẨN A4 VINAMILK -->
<div class="card card-custom p-4 p-md-5 mx-auto shadow-sm" style="max-width: 850px; background: #ffffff;">
    <!-- Header Doanh Nghiệp -->
    <div class="d-flex justify-content-between align-items-start border-bottom pb-4 mb-4">
        <div>
            <div class="d-flex align-items-center gap-2 mb-2">
                <span style="background: #002F6C; color: #fff; font-weight: 800; padding: 4px 10px; border-radius: 4px; letter-spacing: 1px;">VINAMILK</span>
                <span class="fw-bold text-muted small">CÔNG TY CỔ PHẦN SỮA VIỆT NAM</span>
            </div>
            <h4 class="fw-bold text-primary mb-1" style="color: #002F6C !important;">PHIẾU THANH TOÁN LƯƠNG CHI TIẾT</h4>
            <div class="text-muted small">Kỳ tính lương: <strong class="text-dark fs-6"><?= e($payslip['ky_luong']) ?></strong> &bull; Trạng thái: <span class="badge bg-success">Đã phê duyệt</span></div>
        </div>

        <div class="text-end small text-muted">
            <div>Mã số thuế: <strong>0300588569</strong></div>
            <div>Số 10 Tân Trào, P. Tân Phú, Q.7, TP.HCM</div>
            <div class="mt-1"><span class="badge bg-light text-dark border">Bản in chính thức A4</span></div>
        </div>
    </div>

    <!-- Thông tin người lao động -->
    <div class="row g-2 mb-4 p-3 bg-light rounded-3 small">
        <div class="col-sm-6 col-md-3">
            <span class="text-muted">Mã nhân viên:</span>
            <div class="fw-bold text-dark font-monospace fs-6"><?= e($payslip['ma_nv']) ?></div>
        </div>
        <div class="col-sm-6 col-md-3">
            <span class="text-muted">Họ và tên:</span>
            <div class="fw-bold text-dark fs-6"><?= e($payslip['ho_ten']) ?></div>
        </div>
        <div class="col-sm-6 col-md-3">
            <span class="text-muted">Chức vụ:</span>
            <div class="fw-semibold text-dark"><?= e($payslip['chuc_vu']) ?></div>
        </div>
        <div class="col-sm-6 col-md-3">
            <span class="text-muted">Đơn vị công tác:</span>
            <div class="fw-semibold text-dark"><?= e($payslip['ten_dv']) ?></div>
        </div>

        <div class="col-sm-6 col-md-3 mt-2">
            <span class="text-muted">Số CMND/CCCD:</span>
            <div class="fw-semibold text-dark"><?= e($payslip['SoCMND_CCCD'] ?? '-') ?></div>
        </div>
        <div class="col-sm-6 col-md-3 mt-2">
            <span class="text-muted">Tài khoản Ngân hàng:</span>
            <div class="fw-semibold text-dark"><?= e($payslip['SoTaiKhoanNH'] ?? 'Chưa cập nhật') ?></div>
        </div>
        <div class="col-sm-6 col-md-3 mt-2">
            <span class="text-muted">Vùng lương áp dụng:</span>
            <div class="fw-semibold text-dark">Vùng <?= e($payslip['vung_luong']) ?> (NĐ 74/2024)</div>
        </div>
        <div class="col-sm-6 col-md-3 mt-2">
            <span class="text-muted">Người phụ thuộc (NPT):</span>
            <div class="fw-semibold text-dark"><?= (int)(($payslip['giam_tru_phu_thuoc'] ?? 0) / ($legalConfig['GIAM_TRU_PHU_THUOC'] ?? 4400000)) ?> người</div>
        </div>
    </div>

    <!-- Bảng 1: Các khoản thu nhập -->
    <h6 class="fw-bold text-dark mb-2"><i class="bi bi-plus-circle-fill text-success me-1"></i> I. CÁC KHOẢN THU NHẬP (A)</h6>
    <table class="table table-sm table-bordered mb-4" style="font-size: 13px;">
        <thead class="table-light text-muted small">
            <tr>
                <th>Khoản mục thu nhập</th>
                <th>Căn cứ tính toán</th>
                <th class="text-end" style="width: 160px;">Số tiền (VND)</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td><strong>Lương ngạch bậc vị trí (P1)</strong></td>
                <td class="text-muted small">Hợp đồng lao động & căn cứ đóng BH</td>
                <td class="text-end fw-semibold"><?= format_money($payslip['luong_p1_hop_dong']) ?></td>
            </tr>
            <tr>
                <td><strong>Lương sản lượng / chức danh (P2)</strong></td>
                <td class="text-muted small">Năng suất lao động theo ngày công chuẩn</td>
                <td class="text-end fw-semibold"><?= format_money($payslip['luong_p2_hieu_suat']) ?></td>
            </tr>
            <tr>
                <td><strong>Thưởng hiệu quả công việc (P3)</strong></td>
                <td class="text-muted small">Đánh giá kết quả KPI kỳ hoàn thành</td>
                <td class="text-end fw-semibold"><?= format_money($payslip['luong_p3_kpi']) ?></td>
            </tr>
            <tr>
                <td><strong>Phụ cấp ăn ca, độc hại, xăng xe</strong></td>
                <td class="text-muted small">Theo quy chế chế độ đãi ngộ Vinamilk</td>
                <td class="text-end fw-semibold"><?= format_money($payslip['phu_cap']) ?></td>
            </tr>
            <tr class="table-primary fw-bold text-primary">
                <td colspan="2">TỔNG THU NHẬP TRƯỚC GIẢM TRỪ (A):</td>
                <td class="text-end fs-6"><?= format_money($payslip['tong_thu_nhap']) ?></td>
            </tr>
        </tbody>
    </table>

    <!-- Bảng 2: Các khoản khấu trừ bảo hiểm & thuế -->
    <h6 class="fw-bold text-dark mb-2"><i class="bi bi-dash-circle-fill text-danger me-1"></i> II. CÁC KHOẢN KHẤU TRỪ BẢO HIỂM & THUẾ (B)</h6>
    <table class="table table-sm table-bordered mb-4" style="font-size: 13px;">
        <thead class="table-light text-muted small">
            <tr>
                <th>Khoản mục khấu trừ</th>
                <th>Tỷ lệ trích nộp pháp lý (cau_hinh_luong)</th>
                <th class="text-end" style="width: 160px;">Số tiền trích (VND)</th>
            </tr>
        </thead>
        <tbody>
            <tr>
                <td>Bảo hiểm Xã hội (BHXH)</td>
                <td class="text-muted small">8% trích lương đóng BH (<?= format_money($payslip['luong_dong_bh']) ?>)</td>
                <td class="text-end text-danger"><?= format_money($payslip['bhxh_nv']) ?></td>
            </tr>
            <tr>
                <td>Bảo hiểm Y tế (BHYT)</td>
                <td class="text-muted small">1.5% trích lương đóng BH</td>
                <td class="text-end text-danger"><?= format_money($payslip['bhyt_nv']) ?></td>
            </tr>
            <tr>
                <td>Bảo hiểm Thất nghiệp (BHTN)</td>
                <td class="text-muted small">1.0% trích lương đóng bảo hiểm (trần 20 lần lương tối thiểu vùng)</td>
                <td class="text-end text-danger"><?= format_money($payslip['bhtn_nv']) ?></td>
            </tr>
            <tr>
                <td>Thuế Thu nhập Cá nhân (TNCN)</td>
                <td class="text-muted small">
                    Thu nhập tính thuế: <?= format_money($payslip['thu_nhap_tinh_thue']) ?> 
                    (sau giảm trừ bản thân 11tr & NPT <?= format_money($payslip['giam_tru_phu_thuoc']) ?>)
                </td>
                <td class="text-end text-danger fw-semibold"><?= format_money($payslip['thue_tncn']) ?></td>
            </tr>
            <tr class="table-danger fw-bold text-danger">
                <td colspan="2">TỔNG CÁC KHOẢN KHẤU TRỪ (B):</td>
                <td class="text-end fs-6"><?= format_money($payslip['tong_bao_hiem_nv'] + $payslip['thue_tncn']) ?></td>
            </tr>
        </tbody>
    </table>

    <!-- Bảng 3: THỰC LĨNH -->
    <div class="p-3 rounded-3 mb-4 text-white d-flex justify-content-between align-items-center" style="background: linear-gradient(90deg, #002F6C 0%, #0047BA 100%);">
        <div>
            <div class="small text-uppercase fw-bold opacity-75">THỰC LĨNH CHI TRẢ (A - B)</div>
            <div class="small opacity-75">Chuyển khoản qua Ngân hàng vào ngày 05 hàng tháng</div>
        </div>
        <div class="fs-2 fw-bold font-monospace">
            <?= format_money($payslip['thuc_linh']) ?>
        </div>
    </div>

    <!-- Chữ ký điện tử / Ban Giám Đốc xác nhận -->
    <div class="row text-center mt-5 pt-3 small text-muted">
        <div class="col-4">
            <div class="fw-bold text-dark">NGƯỜI LẬP PHIẾU</div>
            <div class="mt-1 text-muted">Kế toán Tiền lương</div>
            <div class="mt-4 pt-4 fw-semibold text-primary">KETOAN_LUONG</div>
        </div>
        <div class="col-4">
            <div class="fw-bold text-dark">BAN NHÂN SỰ TẬP ĐOÀN</div>
            <div class="mt-1 text-muted">Giám đốc C&B</div>
            <div class="mt-4 pt-4 fw-semibold text-primary">HR_TAPDOAN</div>
        </div>
        <div class="col-4">
            <div class="fw-bold text-dark">NGƯỜI LAO ĐỘNG</div>
            <div class="mt-1 text-muted">Xác nhận trực tuyến</div>
            <div class="mt-4 pt-4 fw-semibold text-success"><i class="bi bi-shield-check"></i> ĐÃ XÁC NHẬN</div>
        </div>
    </div>
</div>

<?php endif; ?>
