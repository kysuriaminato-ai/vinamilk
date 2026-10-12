<div class="d-flex justify-content-between align-items-center mb-4 flex-wrap gap-2">
    <div>
        <h3 class="fw-bold text-dark mb-1">
            <i class="bi bi-file-earmark-arrow-up-fill text-primary me-2"></i>Import Dữ Liệu Máy Chấm Công
        </h3>
        <p class="text-muted small mb-0">
            Hỗ trợ nạp file CSV/TXT từ thiết bị quẹt thẻ sinh trắc học &bull; Tự động ghép ca &bull; Xử lý ca 3 xuyên đêm &bull; Phát hiện đi trễ, về sớm & quên quẹt thẻ.
        </p>
    </div>

    <div class="d-flex gap-2 no-print">
        <a href="<?= url('attendance') ?>" class="btn btn-outline-secondary btn-sm fw-semibold">
            <i class="bi bi-calendar-check me-1"></i> Bảng Chấm Công Ngày
        </a>
        <a href="<?= url('attendance/monthly') ?>" class="btn btn-outline-success btn-sm fw-semibold">
            <i class="bi bi-calendar3-range me-1"></i> Bảng Công Tháng
        </a>
    </div>
</div>

<?php if (!empty($importStats)): ?>
<!-- Kết quả thống kê sau khi Import -->
<div class="card card-custom border-0 shadow-sm mb-4" style="border-radius: 8px; border-left: 5px solid #10B981 !important;">
    <div class="card-body p-4">
        <h5 class="fw-bold text-success mb-3">
            <i class="bi bi-check-circle-fill me-2"></i>Kết Quả Xử Lý File Thành Công!
        </h5>
        <div class="row g-3">
            <div class="col-md-3">
                <div class="p-3 bg-light rounded text-center border">
                    <div class="text-muted small text-uppercase fw-semibold">Lượt quẹt thẻ thô</div>
                    <div class="fs-3 fw-bold text-primary mt-1"><?= number_format($importStats['imported_rows']) ?></div>
                    <small class="text-muted">Đã ghi nhận vào hệ thống</small>
                </div>
            </div>
            <div class="col-md-3">
                <div class="p-3 bg-light rounded text-center border">
                    <div class="text-muted small text-uppercase fw-semibold">Ngày công đã ghép ca</div>
                    <div class="fs-3 fw-bold text-success mt-1"><?= number_format($importStats['processed_days']) ?></div>
                    <small class="text-muted">Tính toán công thực tế</small>
                </div>
            </div>
            <div class="col-md-3">
                <div class="p-3 bg-light rounded text-center border">
                    <div class="text-muted small text-uppercase fw-semibold">Ca 3 làm việc đêm</div>
                    <div class="fs-3 fw-bold text-purple mt-1" style="color: #7C3AED;"><?= number_format($importStats['overnight_shifts']) ?></div>
                    <small class="text-muted">Ghép ca qua ngày (22h - 06h)</small>
                </div>
            </div>
            <div class="col-md-3">
                <div class="p-3 bg-light rounded text-center border">
                    <div class="text-muted small text-uppercase fw-semibold">Phát hiện quên quẹt</div>
                    <div class="fs-3 fw-bold text-danger mt-1"><?= number_format($importStats['missing_swipes']) ?></div>
                    <small class="text-muted">Thiếu giờ vào hoặc ra</small>
                </div>
            </div>
        </div>

        <div class="mt-3 text-end">
            <a href="<?= url('attendance') ?>" class="btn btn-primary btn-sm px-4 fw-semibold" style="background: #0033A0;">
                <i class="bi bi-eye-fill me-1"></i> Xem Dữ Liệu Chấm Công Đã Ghép
            </a>
        </div>
    </div>
</div>
<?php endif; ?>

<div class="row g-4">
    <!-- Form Upload File -->
    <div class="col-lg-7">
        <div class="card card-custom border-0 shadow-sm h-100" style="border-radius: 8px;">
            <div class="card-header bg-white py-3 border-bottom">
                <h5 class="fw-bold text-dark mb-0">
                    <i class="bi bi-cloud-arrow-up-fill text-primary me-2"></i>Tải Lên File Dữ Liệu Quẹt Thẻ
                </h5>
            </div>
            <div class="card-body p-4">
                <form action="<?= url('attendance/import') ?>" method="POST" enctype="multipart/form-data" id="uploadForm">
                    <input type="hidden" name="csrf_token" value="<?= csrf_token() ?>">

                    <div class="mb-4">
                        <label class="form-label fw-semibold text-dark">Chọn file dữ liệu máy chấm công (.csv, .txt):</label>
                        <div class="drop-zone p-4 text-center rounded border-2 border-dashed bg-light position-relative" style="border: 2px dashed #CBD5E1; cursor: pointer; transition: all 0.2s ease;">
                            <input type="file" name="csv_file" id="csvFile" class="position-absolute top-0 start-0 w-100 h-100 opacity-0" accept=".csv, .txt" required onchange="showSelectedFile(this)">
                            <div id="dropZoneContent">
                                <i class="bi bi-file-earmark-spreadsheet-fill fs-1 text-primary mb-2 d-block"></i>
                                <span class="fw-bold text-dark d-block">Kéo thả file vào đây hoặc nhấp để duyệt</span>
                                <small class="text-muted">Định dạng hỗ trợ: CSV, TXT (Dung lượng tối đa 15MB)</small>
                            </div>
                            <div id="fileInfo" class="d-none">
                                <i class="bi bi-file-check-fill fs-2 text-success mb-1 d-block"></i>
                                <span class="fw-bold text-success fs-6" id="fileNameDisplay">file.csv</span>
                                <div class="small text-muted" id="fileSizeDisplay">-- KB</div>
                                <span class="badge bg-primary-subtle text-primary border border-primary-subtle mt-2">Đã sẵn sàng tải lên</span>
                            </div>
                        </div>
                    </div>

                    <div class="alert alert-info py-2 px-3 small d-flex align-items-center mb-4">
                        <i class="bi bi-info-circle-fill fs-5 me-2 text-primary"></i>
                        <div>
                            Hệ thống sẽ tự động ghép với <strong>Kế hoạch phân ca tuần</strong> hiện tại. Sau khi import, dữ liệu được chuyển đổi ngay lập tức thành công ngày và công tháng.
                        </div>
                    </div>

                    <div class="d-flex justify-content-between align-items-center">
                        <button type="button" class="btn btn-outline-secondary btn-sm" onclick="downloadSampleCsv()">
                            <i class="bi bi-download me-1"></i> Tải File Mẫu (sample.csv)
                        </button>
                        <button type="submit" class="btn btn-primary px-4 fw-semibold" style="background: #0033A0;">
                            <i class="bi bi-upload me-1"></i> Bắt Đầu Nạp & Ghép Ca
                        </button>
                    </div>
                </form>
            </div>
        </div>
    </div>

    <!-- Hướng Dẫn Định Dạng & Thuật Toán Xử Lý -->
    <div class="col-lg-5">
        <div class="card card-custom border-0 shadow-sm h-100" style="border-radius: 8px;">
            <div class="card-header bg-white py-3 border-bottom">
                <h5 class="fw-bold text-dark mb-0">
                    <i class="bi bi-gear-wide-connected text-primary me-2"></i>Quy Chuẩn Xử Lý Tự Động
                </h5>
            </div>
            <div class="card-body p-4 small">
                <h6 class="fw-bold text-dark mb-2">1. Cấu trúc file CSV chuẩn:</h6>
                <div class="p-2 bg-dark text-white rounded font-monospace mb-3" style="font-size: 0.78rem;">
                    MaNV,ThoiGianQuet,MaMay<br>
                    VNM-0001,2026-10-12 05:55:00,MCC-01<br>
                    VNM-0001,2026-10-12 14:05:00,MCC-01<br>
                    VNM-0002,2026-10-12 21:50:00,MCC-02<br>
                    VNM-0002,2026-10-13 06:05:00,MCC-02
                </div>

                <h6 class="fw-bold text-dark mb-2">2. Xử lý ca 3 qua đêm (22:00 - 06:00):</h6>
                <ul class="text-muted ps-3 mb-3">
                    <li><strong>Quẹt vào:</strong> Khớp trong khoảng <code>20:30 &rarr; 23:59</code> của ngày làm việc D.</li>
                    <li><strong>Quẹt ra:</strong> Tự động tìm kiếm lần quẹt trong khoảng <code>04:30 &rarr; 09:00</code> của sáng ngày tiếp theo (D+1).</li>
                    <li>Tự động ghi nhận <strong>8.0 giờ làm việc ban đêm</strong> tính phụ cấp lương đêm theo luật Lao động.</li>
                </ul>

                <h6 class="fw-bold text-dark mb-2">3. Đi trễ, Về sớm & Quên quẹt:</h6>
                <ul class="text-muted ps-3 mb-0">
                    <li><strong>Ân hạn:</strong> Cho phép quẹt trễ tối đa <strong>5 phút</strong> không tính vi phạm.</li>
                    <li><strong>Quên quẹt:</strong> Nếu chỉ quẹt 1 đầu (chỉ vào hoặc chỉ ra), hệ thống đánh dấu <span class="badge bg-danger-subtle text-danger">Quên quẹt</span> và tạm tính 0.5 công chờ xác nhận giải trình.</li>
                    <li><strong>Làm thêm giờ (OT):</strong> Quẹt ra sau giờ kết thúc ca trên 30 phút tự động tính giờ OT phân loại (Thường 150%, Nghỉ 200%, Lễ 300%).</li>
                </ul>
            </div>
        </div>
    </div>
</div>

<script>
function showSelectedFile(input) {
    if (input.files && input.files[0]) {
        const file = input.files[0];
        document.getElementById('dropZoneContent').classList.add('d-none');
        const fileInfo = document.getElementById('fileInfo');
        fileInfo.classList.remove('d-none');
        document.getElementById('fileNameDisplay').textContent = file.name;
        document.getElementById('fileSizeDisplay').textContent = (file.size / 1024).toFixed(1) + ' KB';
    }
}

function downloadSampleCsv() {
    const today = new Date().toISOString().slice(0, 10);
    const tomorrow = new Date(Date.now() + 86400000).toISOString().slice(0, 10);

    const csvContent = "MaNV,ThoiGianQuet,MaMay\n" +
        "VNM-0001," + today + " 05:55:00,MCC-01\n" +
        "VNM-0001," + today + " 14:05:00,MCC-01\n" +
        "VNM-0002," + today + " 13:50:00,MCC-02\n" +
        "VNM-0002," + today + " 22:10:00,MCC-02\n" +
        "VNM-0003," + today + " 21:55:00,MCC-01\n" +
        "VNM-0003," + tomorrow + " 06:05:00,MCC-01\n" +
        "VNM-0004," + today + " 07:50:00,MCC-01\n" +
        "VNM-0004," + today + " 17:15:00,MCC-01\n";

    const blob = new Blob(["\uFEFF" + csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement("a");
    const url = URL.createObjectURL(blob);
    link.setAttribute("href", url);
    link.setAttribute("download", "mau_quet_the_vinamilk.csv");
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}
</script>
