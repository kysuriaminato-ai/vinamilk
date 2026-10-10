<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <title><?= htmlspecialchars($title ?? 'In Quyết định Điều động') ?></title>
    <style>
        body {
            font-family: "Times New Roman", Times, serif;
            font-size: 13pt;
            line-height: 1.5;
            color: #000;
            background: #fff;
            padding: 20px;
        }

        .qd-container {
            width: 210mm;
            min-height: 297mm;
            margin: 0 auto;
            padding: 15mm;
            box-sizing: border-box;
        }

        .header-table {
            width: 100%;
            margin-bottom: 25px;
        }

        .title-main {
            text-align: center;
            font-size: 16pt;
            font-weight: bold;
            text-transform: uppercase;
            margin: 20px 0 5px 0;
        }

        .title-sub {
            text-align: center;
            font-size: 13pt;
            font-weight: bold;
            margin-bottom: 25px;
        }

        .content-body {
            text-align: justify;
            text-indent: 1cm;
        }

        .article-title {
            font-weight: bold;
            margin-top: 15px;
        }

        .footer-sig {
            width: 100%;
            margin-top: 40px;
            page-break-inside: avoid;
        }

        .btn-print {
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 10px 20px;
            background: #005696;
            color: #fff;
            border: none;
            border-radius: 5px;
            font-size: 14px;
            font-weight: bold;
            cursor: pointer;
        }

        @media print {
            .btn-print { display: none; }
            body { padding: 0; }
            .qd-container { padding: 0; width: 100%; }
        }
    </style>
</head>
<body>
    <button class="btn-print" onclick="window.print()">🖨️ In Quyết định (Ctrl+P)</button>

    <div class="qd-container">
        <!-- Header -->
        <table class="header-table">
            <tr>
                <td style="width: 45%; vertical-align: top; text-align: center;">
                    <strong>CÔNG TY CP SỮA VIỆT NAM</strong><br>
                    <strong>HỘI ĐỒNG QUẢN TRỊ / BGD</strong><br>
                    --------<br>
                    Số: <strong><?= htmlspecialchars($qd['SoQD']) ?></strong>
                </td>
                <td style="text-align: center; vertical-align: top;">
                    <strong>CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</strong><br>
                    <strong>Độc lập - Tự do - Hạnh phúc</strong><br>
                    -------------------------<br>
                    <i>TP. Hồ Chí Minh, ngày <?= date('d', strtotime($qd['NgayHieuLuc'])) ?> tháng <?= date('m', strtotime($qd['NgayHieuLuc'])) ?> năm <?= date('Y', strtotime($qd['NgayHieuLuc'])) ?></i>
                </td>
            </tr>
        </table>

        <!-- Tiêu đề -->
        <div class="title-main">QUYẾT ĐỊNH</div>
        <div class="title-sub">Về việc điều động và thuyên chuyển công tác cán bộ nhân viên</div>

        <!-- Căn cứ pháp lý -->
        <div class="content-body">
            <p><i>- Căn cứ Điều lệ tổ chức và hoạt động của Công ty Cổ phần Sữa Việt Nam (Vinamilk);</i></p>
            <p><i>- Căn cứ Quy chế Quản trị Nguồn nhân lực và Thẩm quyền điều động cán bộ Tập đoàn;</i></p>
            <p><i>- Xét yêu cầu hoạt động sản xuất kinh doanh và đề nghị của Giám đốc Khối Nhân sự Vinamilk;</i></p>
        </div>

        <div style="text-align: center; font-weight: bold; margin: 20px 0 10px 0; font-size: 14pt;">TỔNG GIÁM ĐỐC CÔNG TY CP SỮA VIỆT NAM QUYẾT ĐỊNH</div>

        <div class="content-body">
            <p class="article-title">Điều 1. Điều động và thuyên chuyển Ông/Bà:</p>
            <p style="text-indent: 1.5cm;">- Họ và tên: <strong><?= htmlspecialchars($qd['HoTen']) ?></strong> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Mã số nhân viên: <strong><?= htmlspecialchars($qd['MaNV']) ?></strong></p>
            <p style="text-indent: 1.5cm;">- Đơn vị công tác cũ: <strong><?= htmlspecialchars($qd['TenDonViCu'] ?? $qd['DonViCu']) ?></strong></p>
            <p style="text-indent: 1.5cm;">- Đến nhận công tác mới tại: <strong><?= htmlspecialchars($qd['TenDonViMoi'] ?? $qd['DonViMoi']) ?></strong></p>
            <p style="text-indent: 1.5cm;">- Chức vụ/Vị trí mới: <strong><?= htmlspecialchars($qd['ChucVuMoi']) ?></strong></p>

            <p class="article-title">Điều 2. Thời hạn và Chế độ đãi ngộ:</p>
            <p style="text-indent: 1.5cm;">- Quyết định này có hiệu lực kể từ ngày <strong><?= format_date($qd['NgayHieuLuc']) ?></strong>.</p>
            <p style="text-indent: 1.5cm;">- Lương, phụ cấp và các chế độ đãi ngộ khác được hưởng theo quy định ngạch bậc tại đơn vị mới kể từ ngày hiệu lực.</p>

            <p class="article-title">Điều 3. Trách nhiệm thi hành:</p>
            <p style="text-indent: 1.5cm;">Các Ông/Bà Giám đốc Khối Nhân sự, Giám đốc đơn vị chuyển đi, Giám đốc đơn vị tiếp nhận và Ông/Bà <strong><?= htmlspecialchars($qd['HoTen']) ?></strong> chịu trách nhiệm thi hành Quyết định này.</p>
        </div>

        <!-- Chữ ký -->
        <table class="footer-sig">
            <tr>
                <td style="width: 50%; vertical-align: top;">
                    <strong><i>Nơi nhận:</i></strong><br>
                    - Như Điều 3;<br>
                    - Lưu VT, Hồ sơ NV;<br>
                    - Phòng Kế toán Lương.
                </td>
                <td style="width: 50%; text-align: center; vertical-align: top;">
                    <strong>TM. BAN GIÁM ĐỐC TẬP ĐOÀN</strong><br>
                    <strong>TỔNG GIÁM ĐỐC</strong><br><br><br><br><br>
                    <strong>Mai Kiều Liên</strong>
                </td>
            </tr>
        </table>
    </div>
</body>
</html>
