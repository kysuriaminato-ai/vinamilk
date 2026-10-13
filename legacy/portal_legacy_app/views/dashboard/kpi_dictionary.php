<?php
/**
 * =====================================================================
 * VINAMILK HRM - TỪ ĐIỂN CHỈ SỐ QUẢN TRỊ NHÂN LỰC (HR METRIC DICTIONARY)
 * =====================================================================
 * Đường dẫn: /kpi-dictionary
 * Tra cứu: Tên, Công thức, Mẫu số, Nguồn bảng, Tần suất, Ý nghĩa quản trị
 */
?>
<div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
    <div>
        <nav aria-label="breadcrumb">
            <ol class="breadcrumb mb-1">
                <li class="breadcrumb-item"><a href="<?= url('dashboard') ?>"><i class="bi bi-speedometer2 me-1"></i>Dashboard</a></li>
                <li class="breadcrumb-item active" aria-current="page">Từ Điển Chỉ Số KPI</li>
            </ol>
        </nav>
        <h3 class="fw-bold text-dark mb-1">
            <i class="bi bi-book-half text-primary me-2"></i>Từ Điển Chỉ Số Quản Trị Nhân Sự (HR Metric Dictionary)
        </h3>
        <p class="text-muted small mb-0">
            Hệ quy chuẩn định nghĩa, công thức tính toán, mẫu số, nguồn dữ liệu và tần suất đo lường chuẩn Vinamilk HR Analytics.
        </p>
    </div>

    <div class="d-flex gap-2">
        <a href="<?= url('dashboard') ?>" class="btn btn-outline-primary btn-sm fw-semibold">
            <i class="bi bi-arrow-left me-1"></i> Quay Lại Dashboard
        </a>
        <button onclick="window.print()" class="btn btn-primary btn-sm fw-semibold" style="background:#002F6C; border-color:#002F6C;">
            <i class="bi bi-printer me-1"></i> In / Xuất PDF Quy Chuẩn
        </button>
    </div>
</div>

<!-- KHUNG TÌM KIẾM NHANH CHỈ SỐ -->
<div class="card card-custom p-3 mb-4 shadow-sm">
    <div class="row g-2 align-items-center">
        <div class="col-md-6">
            <div class="input-group">
                <span class="input-group-text bg-white border-end-0 text-muted"><i class="bi bi-search"></i></span>
                <input type="text" id="kpiSearchInput" class="form-control border-start-0" placeholder="Tìm kiếm nhanh tên chỉ số, công thức hoặc nguồn bảng..." onkeyup="filterKpiTable()">
            </div>
        </div>
        <div class="col-md-3">
            <select id="kpiFrequencyFilter" class="form-select" onchange="filterKpiTable()">
                <option value="">-- Tất cả tần suất đo lường --</option>
                <option value="Hằng tháng">Hằng tháng</option>
                <option value="Hằng quý">Hằng quý</option>
                <option value="Hằng năm">Hằng năm</option>
            </select>
        </div>
        <div class="col-md-3 text-md-end text-muted small">
            Đang hiển thị <strong id="kpiDisplayCount">6</strong> chỉ số chuẩn cốt lõi
        </div>
    </div>
</div>

<!-- BẢNG TỪ ĐIỂN CHỈ SỐ -->
<div class="card card-custom shadow-sm overflow-hidden mb-4">
    <div class="table-responsive">
        <table class="table table-hover align-middle mb-0" id="kpiDictionaryTable">
            <thead class="table-light border-bottom text-uppercase fs-8 text-muted">
                <tr>
                    <th style="width: 5%;">STT</th>
                    <th style="width: 18%;">Tên Chỉ Số (Metric Name)</th>
                    <th style="width: 25%;">Công Thức Tính Toán (Formula)</th>
                    <th style="width: 15%;">Mẫu Số (Denominator)</th>
                    <th style="width: 15%;">Nguồn Bảng Dữ Liệu</th>
                    <th style="width: 10%;">Tần Suất</th>
                    <th style="width: 12%;">Ý Nghĩa & Ngưỡng Chuẩn</th>
                </tr>
            </thead>
            <tbody>
                <!-- 1. Headcount -->
                <tr class="kpi-row">
                    <td class="text-center fw-bold text-muted">01</td>
                    <td>
                        <div class="fw-bold text-primary fs-6">Headcount</div>
                        <div class="small text-muted">Quy mô nhân sự hiện diện</div>
                        <span class="badge bg-primary bg-opacity-10 text-primary mt-1">Lực lượng lao động</span>
                    </td>
                    <td>
                        <div class="p-2 bg-light rounded font-monospace small border">
                            Headcount = &sum; [Nhân viên có TrangThai &isin; (1, 2) tại thời điểm chốt kỳ]
                        </div>
                        <div class="small text-muted mt-1">Đếm số lượng nhân sự chính thức và thử việc đang có hiệu lực làm việc.</div>
                    </td>
                    <td>
                        <span class="badge bg-secondary bg-opacity-10 text-secondary">Không có (Số tuyệt đối)</span>
                    </td>
                    <td>
                        <span class="badge bg-light text-dark border">nhansu</span><br>
                        <span class="badge bg-light text-dark border mt-1">fact_headcount_monthly</span>
                    </td>
                    <td>
                        <span class="badge bg-info text-dark">Hằng tháng</span>
                    </td>
                    <td>
                        <div class="small">
                            <strong>Mục tiêu:</strong> Phản ánh chính xác tổng quy mô lao động để hoạch định ngân sách và tổ chức sản xuất.
                        </div>
                    </td>
                </tr>

                <!-- 2. Tỷ lệ nghỉ việc 12 tháng trượt -->
                <tr class="kpi-row">
                    <td class="text-center fw-bold text-muted">02</td>
                    <td>
                        <div class="fw-bold text-danger fs-6">Tỷ Lệ Nghỉ Việc 12 Tháng Trượt</div>
                        <div class="small text-muted">12-Month Rolling Turnover Rate</div>
                        <span class="badge bg-danger bg-opacity-10 text-danger mt-1">Biến động nhân sự</span>
                    </td>
                    <td>
                        <div class="p-2 bg-light rounded font-monospace small border">
                            Turnover Rate (12M) = (&sum; Thôi việc trong 12 tháng trượt / Headcount bình quân 12M) &times; 100%
                        </div>
                        <div class="small text-muted mt-1">Tính trượt liên tục 12 kỳ tháng gần nhất để triệt tiêu yếu tố thời vụ.</div>
                    </td>
                    <td>
                        <div class="small fw-semibold text-dark">Headcount bình quân 12 tháng:</div>
                        <div class="font-monospace fs-8 text-muted">&sum; Headcount_i / 12</div>
                    </td>
                    <td>
                        <span class="badge bg-light text-dark border">su_kien_nhan_su</span><br>
                        <span class="badge bg-light text-dark border mt-1">fact_headcount_monthly</span>
                    </td>
                    <td>
                        <span class="badge bg-info text-dark">Hằng tháng</span>
                    </td>
                    <td>
                        <div class="small">
                            <span class="badge bg-success mb-1">&le; 8%: Tốt</span><br>
                            <span class="badge bg-danger">&gt; 12%: Cảnh báo đỏ</span>
                            <div class="text-muted fs-8 mt-1">Ngăn ngừa thất thoát chất xám và thiếu hụt vận hành.</div>
                        </div>
                    </td>
                </tr>

                <!-- 3. Tỷ lệ vắng mặt -->
                <tr class="kpi-row">
                    <td class="text-center fw-bold text-muted">03</td>
                    <td>
                        <div class="fw-bold text-warning fs-6">Tỷ Lệ Vắng Mặt</div>
                        <div class="small text-muted">Absenteeism Rate</div>
                        <span class="badge bg-warning bg-opacity-10 text-dark mt-1">Kỷ luật & Chuyên cần</span>
                    </td>
                    <td>
                        <div class="p-2 bg-light rounded font-monospace small border">
                            Absenteeism Rate = (Tổng ngày nghỉ phép, ốm, không lương / Tổng ngày công chuẩn) &times; 100%
                        </div>
                        <div class="small text-muted mt-1">Bao gồm nghỉ phép năm, ốm BHXH, thai sản, việc riêng và không lương.</div>
                    </td>
                    <td>
                        <div class="small fw-semibold text-dark">Tổng ngày công chuẩn kỳ:</div>
                        <div class="font-monospace fs-8 text-muted">Headcount &times; 26 ngày</div>
                    </td>
                    <td>
                        <span class="badge bg-light text-dark border">cham_cong_ngay</span><br>
                        <span class="badge bg-light text-dark border mt-1">don_nghi_phep</span>
                    </td>
                    <td>
                        <span class="badge bg-info text-dark">Hằng tháng</span>
                    </td>
                    <td>
                        <div class="small">
                            <span class="badge bg-success mb-1">&le; 3.0%: Đạt chuẩn</span><br>
                            <span class="badge bg-warning text-dark">&gt; 5.0%: Thiếu hụt nhân lực ca 3</span>
                        </div>
                    </td>
                </tr>

                <!-- 4. Giờ OT bình quân / người -->
                <tr class="kpi-row">
                    <td class="text-center fw-bold text-muted">04</td>
                    <td>
                        <div class="fw-bold text-info fs-6">Giờ OT Bình Quân / Người</div>
                        <div class="small text-muted">Average Overtime Hours per Employee</div>
                        <span class="badge bg-info bg-opacity-10 text-info mt-1">Năng suất & Sức khỏe</span>
                    </td>
                    <td>
                        <div class="p-2 bg-light rounded font-monospace small border">
                            Average OT = &sum; Giờ làm thêm (Thường + Nghỉ + Lễ + Đêm) / Headcount làm việc
                        </div>
                        <div class="small text-muted mt-1">Đo lường cường độ làm việc ngoài giờ tại các nhà máy chế biến sữa.</div>
                    </td>
                    <td>
                        <div class="small fw-semibold text-dark">Headcount đi làm thực tế trong kỳ</div>
                    </td>
                    <td>
                        <span class="badge bg-light text-dark border">cham_cong_ngay</span><br>
                        <span class="badge bg-light text-dark border mt-1">bang_cong_thang</span>
                    </td>
                    <td>
                        <span class="badge bg-info text-dark">Hằng tháng</span>
                    </td>
                    <td>
                        <div class="small">
                            <span class="badge bg-primary mb-1">&le; 40 giờ/tháng (Luật lao động)</span><br>
                            <span class="badge bg-danger">&gt; 40h: Vi phạm luật lao động</span>
                        </div>
                    </td>
                </tr>

                <!-- 5. Quỹ lương tháng -->
                <tr class="kpi-row">
                    <td class="text-center fw-bold text-muted">05</td>
                    <td>
                        <div class="fw-bold text-success fs-6">Quỹ Lương Tháng</div>
                        <div class="small text-muted">Monthly Payroll Fund</div>
                        <span class="badge bg-success bg-opacity-10 text-success mt-1">Chi phí nhân sự</span>
                    </td>
                    <td>
                        <div class="p-2 bg-light rounded font-monospace small border">
                            Total Payroll = &sum; [Lương P1 + Lương P2 + Thưởng P3 + Lương OT + Phụ cấp]
                        </div>
                        <div class="small text-muted mt-1">Tổng chi phí lương Gross phải chi trả cho người lao động trong kỳ.</div>
                    </td>
                    <td>
                        <span class="badge bg-secondary bg-opacity-10 text-secondary">Không có (Tổng tiền tệ VNĐ)</span>
                    </td>
                    <td>
                        <span class="badge bg-light text-dark border">bang_luong</span><br>
                        <span class="badge bg-light text-dark border mt-1">bang_luong_chi_tiet</span>
                    </td>
                    <td>
                        <span class="badge bg-info text-dark">Hằng tháng</span>
                    </td>
                    <td>
                        <div class="small">
                            <strong>Kiểm soát ngân sách:</strong> Bám sát kế hoạch năm, tỷ lệ chi phí lương trên doanh thu &le; 6.5%.
                        </div>
                    </td>
                </tr>

                <!-- 6. Tỷ lệ lấp đầy định biên -->
                <tr class="kpi-row">
                    <td class="text-center fw-bold text-muted">06</td>
                    <td>
                        <div class="fw-bold text-dark fs-6">Tỷ Lệ Lấp Đầy Định Biên</div>
                        <div class="small text-muted">Staffing Fill Rate / Capacity Ratio</div>
                        <span class="badge bg-dark bg-opacity-10 text-dark mt-1">Quy hoạch nguồn lực</span>
                    </td>
                    <td>
                        <div class="p-2 bg-light rounded font-monospace small border">
                            Fill Rate = (Headcount thực tế cuối kỳ / Định biên nhân sự được duyệt) &times; 100%
                        </div>
                        <div class="small text-muted mt-1">Đánh giá mức độ đáp ứng chỉ tiêu tuyển dụng và năng lực vận hành.</div>
                    </td>
                    <td>
                        <div class="small fw-semibold text-dark">Tổng số lượng định biên trong bảng `dinh_bien`</div>
                    </td>
                    <td>
                        <span class="badge bg-light text-dark border">dinh_bien</span><br>
                        <span class="badge bg-light text-dark border mt-1">dm_donvi</span>
                    </td>
                    <td>
                        <span class="badge bg-info text-dark">Hằng tháng</span>
                    </td>
                    <td>
                        <div class="small">
                            <span class="badge bg-success mb-1">&ge; 90%: An toàn vận hành</span><br>
                            <span class="badge bg-danger">&lt; 90%: Thiếu định biên &gt; 10% (Cảnh báo tuyển dụng)</span>
                        </div>
                    </td>
                </tr>
            </tbody>
        </table>
    </div>
</div>

<!-- NGUYÊN TẮC QUẢN TRỊ & TIÊU CHUẨN VINAMILK -->
<div class="row g-4 mb-4">
    <div class="col-md-6">
        <div class="card card-custom p-4 h-100 border-start border-4 border-primary">
            <h5 class="fw-bold text-primary mb-3">
                <i class="bi bi-shield-check me-2"></i>Quy Định Bảo Mật & Scope By Unit
            </h5>
            <ul class="text-muted small ps-3 mb-0" style="line-height: 1.8;">
                <li>Mọi số liệu trong từ điển đều tuân thủ cơ chế phân quyền <strong>scopeByUnit()</strong>.</li>
                <li>Quản đốc, Trưởng nhà máy chỉ thấy dữ liệu cấu thành trong cây phân cấp của nhà máy mình.</li>
                <li>Dữ liệu nhạy cảm liên quan đến chi phí quỹ lương chỉ hiển thị với vai trò có thẩm quyền <code>*.sensitive</code>.</li>
                <li>Snapshot tự động khóa tại thời điểm 23:59:59 ngày cuối tháng bởi Cronjob.</li>
            </ul>
        </div>
    </div>

    <div class="col-md-6">
        <div class="card card-custom p-4 h-100 border-start border-4 border-success">
            <h5 class="fw-bold text-success mb-3">
                <i class="bi bi-cpu me-2"></i>Cơ Chế Snapshot & Đồng Bộ Dữ Liệu
            </h5>
            <ul class="text-muted small ps-3 mb-0" style="line-height: 1.8;">
                <li>Job Cron chạy tự động vào <strong>00:05 ngày 1 hằng tháng</strong> chốt dữ liệu tháng trước.</li>
                <li>Dữ liệu được lưu trữ cố định vào <code>fact_headcount_monthly</code> để đảm bảo tính bất biến của lịch sử.</li>
                <li>Biểu đồ xu hướng đọc trực tiếp từ bảng Fact để đạt tốc độ phản hồi dưới 50ms.</li>
                <li>Hỗ trợ xem chi tiết nhân viên (Drill-down) tại thời điểm thực tế.</li>
            </ul>
        </div>
    </div>
</div>

<script>
function filterKpiTable() {
    const searchText = document.getElementById('kpiSearchInput').value.toLowerCase();
    const freqFilter = document.getElementById('kpiFrequencyFilter').value.toLowerCase();
    const rows = document.querySelectorAll('#kpiDictionaryTable tbody tr');
    let visibleCount = 0;

    rows.forEach(row => {
        const text = row.innerText.toLowerCase();
        const matchesSearch = text.includes(searchText);
        const matchesFreq = freqFilter === '' || text.includes(freqFilter);

        if (matchesSearch && matchesFreq) {
            row.style.display = '';
            visibleCount++;
        } else {
            row.style.display = 'none';
        }
    });

    document.getElementById('kpiDisplayCount').innerText = visibleCount;
}
</script>
