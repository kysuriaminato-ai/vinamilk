<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?= e($title ?? 'Hợp Đồng Lao Động - Vinamilk') ?></title>
    <link rel="preconnect" href="https://fonts.googleapis.com">
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
    <link href="https://fonts.googleapis.com/css2?family=Be+Vietnam+Pro:wght@400;500;600;700&family=Times+New+Roman&display=swap" rel="stylesheet">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css">
    <style>
        * {
            box-sizing: border-box;
        }
        body {
            font-family: 'Times New Roman', Times, serif;
            font-size: 14pt;
            line-height: 1.45;
            color: #000;
            background: #e2e8f0;
            margin: 0;
            padding: 20px;
        }
        .page-a4 {
            width: 210mm;
            min-height: 297mm;
            padding: 20mm 20mm 20mm 25mm;
            margin: 0 auto 20px auto;
            background: #fff;
            box-shadow: 0 4px 15px rgba(0,0,0,0.15);
            position: relative;
        }
        .no-print-bar {
            width: 210mm;
            margin: 0 auto 15px auto;
            display: flex;
            justify-content: space-between;
            align-items: center;
            font-family: 'Be Vietnam Pro', sans-serif;
            font-size: 13px;
        }
        .btn-print {
            background: #0033A0;
            color: #fff;
            border: none;
            padding: 8px 20px;
            border-radius: 4px;
            font-weight: 600;
            cursor: pointer;
        }
        .btn-close-win {
            background: #fff;
            color: #333;
            border: 1px solid #ccc;
            padding: 8px 16px;
            border-radius: 4px;
            cursor: pointer;
        }
        .text-center { text-align: center; }
        .text-right { text-align: right; }
        .fw-bold { font-weight: bold; }
        .text-uppercase { text-transform: uppercase; }
        .clause-title {
            font-weight: bold;
            margin-top: 14px;
            margin-bottom: 4px;
            text-decoration: underline;
        }
        p { margin: 4px 0; }
        ul { margin: 4px 0; padding-left: 25px; }
        li { margin-bottom: 3px; }
        .sign-grid {
            display: flex;
            justify-content: space-between;
            margin-top: 40px;
            page-break-inside: avoid;
        }
        .sign-col {
            width: 45%;
            text-align: center;
        }

        @media print {
            body {
                background: #fff;
                padding: 0;
            }
            .page-a4 {
                width: 100%;
                margin: 0;
                box-shadow: none;
                padding: 15mm 15mm 15mm 20mm;
            }
            .no-print-bar {
                display: none !important;
            }
        }
    </style>
</head>
<body>

<div class="no-print-bar">
    <div>
        <strong>VINAMILK HRIS</strong> &bull; Mẫu Hợp đồng Lao động theo Bộ luật Lao động 2019
    </div>
    <div>
        <button class="btn-close-win" onclick="window.close()">Đóng</button>
        <button class="btn-print" onclick="window.print()">
            <i class="bi bi-printer"></i> In Hợp Đồng A4
        </button>
    </div>
</div>

<div class="page-a4">
    <!-- Tiêu ngữ & Quốc hiệu -->
    <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 20px;">
        <div style="width: 45%; text-align: center; font-size: 11pt;">
            <div class="fw-bold text-uppercase">CÔNG TY CỔ PHẦN SỮA VIỆT NAM</div>
            <div>Số: <?= e($contract['so_hd']) ?></div>
            <div style="margin-top: 5px; font-style: italic;">TP.HCM, ngày <?= date('d', strtotime($contract['ngay_ky'])) ?> tháng <?= date('m', strtotime($contract['ngay_ky'])) ?> năm <?= date('Y', strtotime($contract['ngay_ky'])) ?></div>
        </div>

        <div style="width: 55%; text-align: center;">
            <div class="fw-bold text-uppercase" style="font-size: 12pt;">CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</div>
            <div class="fw-bold" style="font-size: 13pt;">Độc lập - Tự do - Hạnh phúc</div>
            <div style="margin-top: 2px;">-------------------</div>
        </div>
    </div>

    <!-- Tiêu đề Hợp đồng -->
    <div class="text-center" style="margin: 25px 0 20px 0;">
        <h2 class="fw-bold text-uppercase" style="font-size: 18pt; margin: 0 0 5px 0;">HỢP ĐỒNG LAO ĐỘNG</h2>
        <div style="font-style: italic; font-size: 12pt;">
            (Ban hành kèm theo Quy chế Quản lý Nhân sự Công ty Cổ phần Sữa Việt Nam - Vinamilk)
        </div>
    </div>

    <p style="font-style: italic;">
        Hôm nay, ngày <?= date('d/m/Y', strtotime($contract['ngay_ky'])) ?>, tại Trụ sở Công ty Cổ phần Sữa Việt Nam, chúng tôi gồm các bên:
    </p>

    <!-- Bên A: Người sử dụng lao động -->
    <div class="clause-title">BÊN A: NGƯỜI SỬ DỤNG LAO ĐỘNG</div>
    <p><strong>CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK)</strong></p>
    <p>Đại diện bởi: Ông/Bà <strong><?= e($contract['nguoi_dai_dien_ky'] ?: 'Bùi Thị Hương') ?></strong></p>
    <p>Chức vụ: <strong><?= e($contract['chuc_vu_nguoi_ky'] ?: 'Giám đốc Điều hành Nhân sự') ?></strong> (Theo Giấy ủy quyền của Tổng Giám đốc)</p>
    <p>Địa chỉ trụ sở: Số 10 Tân Trào, Phường Tân Phú, Quận 7, TP. Hồ Chí Minh</p>
    <p>Điện thoại: (028) 54 155 555 &bull; Mã số thuế: 0300588569</p>

    <!-- Bên B: Người lao động -->
    <div class="clause-title" style="margin-top: 15px;">BÊN B: NGƯỜI LAO ĐỘNG</div>
    <p>Họ và tên: <strong><?= e($contract['HoTen']) ?></strong> &nbsp;&nbsp;&nbsp;&nbsp; Giới tính: <?= e($contract['GioiTinh']) ?></p>
    <p>Ngày sinh: <?= format_date($contract['NgaySinh']) ?> &nbsp;&nbsp;&nbsp;&nbsp; Quốc tịch: Việt Nam</p>
    <p>Số CCCD / Định danh: <strong><?= e($contract['SoCMND_CCCD']) ?></strong> &nbsp;&nbsp;&nbsp;&nbsp; Cấp ngày: <?= format_date($contract['NgayCap'] ?? '') ?> tại: <?= e($contract['NoiCap'] ?: 'Cục Cảnh sát QLHC về TTXH') ?></p>
    <p>Nơi ở hiện nay: <?= e($contract['DiaChiHienTai']) ?></p>
    <p>Số điện thoại: <?= e($contract['SoDienThoai']) ?> &bull; Email: <?= e($contract['Email']) ?></p>
    <p>Số tài khoản nhận lương: <?= e($contract['SoTaiKhoanNH'] ?: 'Chưa cung cấp') ?> - Ngân hàng: <?= e($contract['NganHang'] ?: 'Vietcombank') ?></p>

    <p style="margin-top: 12px;">Cùng thỏa thuận ký kết hợp đồng lao động và cam kết thực hiện đúng những điều khoản sau đây:</p>

    <!-- Điều 1 -->
    <div class="clause-title">ĐIỀU 1: THỜI HẠN VÀ CÔNG VIỆC HỢP ĐỒNG</div>
    <p>- Loại hợp đồng lao động: <strong><?= e($contract['loai_hd']) ?></strong></p>
    <p>- Thời hạn hiệu lực: Từ ngày <strong><?= format_date($contract['tu_ngay']) ?></strong> 
       <?= !empty($contract['den_ngay']) ? 'đến ngày <strong>' . format_date($contract['den_ngay']) . '</strong>' : '<strong>(Hợp đồng không xác định thời hạn)</strong>' ?>.</p>
    <p>- Địa điểm làm việc: <?= e($contract['dia_diem_lam_viec'] ?: ($contract['TenDV'] ?? 'Các cơ sở của Công ty Vinamilk')) ?>.</p>
    <p>- Chức danh chuyên môn: <strong><?= e($contract['chuc_danh'] ?: ($contract['TenCV'] ?? 'Nhân viên')) ?></strong>.</p>

    <!-- Điều 2 -->
    <div class="clause-title">ĐIỀU 2: CHẾ ĐỘ LÀM VIỆC</div>
    <p>- Thời giờ làm việc: 08 giờ/ngày, 48 giờ/tuần (theo ca sản xuất hoặc giờ hành chính tùy phân công của đơn vị).</p>
    <p>- Được cấp phát trang thiết bị làm việc, bảo hộ lao động (BHLD) theo tiêu chuẩn chất lượng và an toàn thực phẩm của Vinamilk.</p>

    <!-- Điều 3 -->
    <div class="clause-title">ĐIỀU 3: TIỀN LƯƠNG VÀ CÁC KHOẢN PHỤ CẤP</div>
    <p>- Lương căn bản: <strong><?= number_format((float)$contract['luong_can_ban'], 0, ',', '.') ?> VNĐ/tháng</strong>.</p>
    <p>- Lương tham gia đóng BHXH, BHYT, BHTN: <strong><?= number_format((float)($contract['luong_dong_bh'] ?: $contract['luong_can_ban']), 0, ',', '.') ?> VNĐ/tháng</strong>.</p>
    <p>- Phụ cấp trách nhiệm: <?= number_format((float)($contract['phu_cap_trach_nhiem'] ?? 0), 0, ',', '.') ?> VNĐ &bull; Phụ cấp ăn ca: <?= number_format((float)($contract['phu_cap_an_ca'] ?? 730000), 0, ',', '.') ?> VNĐ.</p>
    <p>- Tỷ lệ hưởng lương trong giai đoạn: <strong><?= (float)($contract['ty_le_huong_luong'] ?? 100) ?>%</strong>.</p>
    <p>- Hình thức trả lương: Chuyển khoản trực tiếp vào tài khoản ngân hàng của Người lao động vào ngày 10 hàng tháng.</p>

    <!-- Điều 4 -->
    <div class="clause-title">ĐIỀU 4: BẢO HIỂM VÀ CÁC QUYỀN LỢI KHÁC</div>
    <p>- Tham gia đầy đủ BHXH, BHYT, BHTN và Bảo hiểm Sức khỏe nâng cao Vinamilk Care theo quy chế Tập đoàn.</p>
    <p>- Nghỉ phép năm có hưởng lương: 12 ngày/năm (tăng thêm theo thâm niên làm việc).</p>

    <!-- Ký tên -->
    <div class="sign-grid">
        <div class="sign-col">
            <div class="fw-bold text-uppercase">NGƯỜI LAO ĐỘNG</div>
            <div style="font-style: italic; font-size: 11pt;">(Ký và ghi rõ họ tên)</div>
            <div style="height: 80px;"></div>
            <div class="fw-bold"><?= e($contract['HoTen']) ?></div>
        </div>

        <div class="sign-col">
            <div class="fw-bold text-uppercase">ĐẠI DIỆN NGƯỜI SỬ DỤNG LAO ĐỘNG</div>
            <div style="font-style: italic; font-size: 11pt;">(Ký tên, đóng dấu công ty)</div>
            <div style="height: 80px;"></div>
            <div class="fw-bold"><?= e($contract['nguoi_dai_dien_ky'] ?: 'Bùi Thị Hương') ?></div>
        </div>
    </div>
</div>

</body>
</html>
