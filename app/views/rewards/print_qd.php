<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <title><?= htmlspecialchars($title ?? 'In Quyết định Khen thưởng Kỷ luật') ?></title>
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
                    <strong>HỘI ĐỒNG THI ĐƯA Khen thưởng</strong><br>
                    --------<br>
                    Số: <strong><?= htmlspecialchars($qd['SoQD']) ?></strong>
                </td>
                <td style="text-align: center; vertical-align: top;">
                    <strong>CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</strong><br>
                    <strong>Độc lập - Tự do - Hạnh phúc</strong><br>
                    -------------------------<br>
                    <i>TP. Hồ Chí Minh, ngày <?= date('d', strtotime($qd['NgayQD'])) ?> tháng <?= date('m', strtotime($qd['NgayQD'])) ?> năm <?= date('Y', strtotime($qd['NgayQD'])) ?></i>
                </td>
            </tr>
        </table>

        <!-- Tiêu đề -->
        <div class="title-main">QUYẾT ĐỊNH</div>
        <div class="title-sub">
            Về việc <?= ($qd['LoaiDanhMuc'] === 'KyLuat') ? 'xử lý kỷ luật lao động' : 'khen thưởng thành tích xuất sắc' ?>
        </div>

        <!-- Căn cứ pháp lý -->
        <div class="content-body">
            <p><i>- Căn cứ Điều lệ tổ chức và hoạt động của Công ty Cổ phần Sữa Việt Nam (Vinamilk);</i></p>
            <p><i>- Căn cứ Quy chế Khen thưởng & Kỷ luật lao động ban hành theo Quyết định của HĐQT Vinamilk;</i></p>
            <p><i>- Xét đề nghị của Hội đồng Thi đua Khen thưởng & Giám đốc Khối Nhân sự Vinamilk;</i></p>
        </div>

        <div style="text-align: center; font-weight: bold; margin: 20px 0 10px 0; font-size: 14pt;">TỔNG GIÁM ĐỐC CÔNG TY CP SỮA VIỆT NAM QUYẾT ĐỊNH</div>

        <div class="content-body">
            <p class="article-title">Điều 1.</p>
            <p style="text-indent: 1.5cm;">
                <?= ($qd['LoaiDanhMuc'] === 'KyLuat') ? 'Áp dụng hình thức kỷ luật đối với Ông/Bà:' : 'Khen thưởng trao tặng danh hiệu thi đua cho Ông/Bà:' ?>
            </p>
            <p style="text-indent: 1.5cm;">- Họ và tên: <strong><?= htmlspecialchars($qd['HoTen']) ?></strong> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Mã số nhân viên: <strong><?= htmlspecialchars($qd['MaNV']) ?></strong></p>
            <p style="text-indent: 1.5cm;">- Đơn vị công tác: <strong><?= htmlspecialchars($qd['TenDV'] ?? '-') ?></strong> — Chức vụ: <strong><?= htmlspecialchars($qd['TenCV'] ?? '-') ?></strong></p>
            <p style="text-indent: 1.5cm;">- Hình thức: <strong><?= htmlspecialchars($qd['HinhThuc']) ?></strong></p>

            <p class="article-title">Điều 2. Lý do & Mức tài chính:</p>
            <p style="text-indent: 1.5cm;">- Lý do: <?= htmlspecialchars($qd['LyDo'] ?? '-') ?></p>
            <?php if ($qd['GiaTri'] != 0): ?>
                <p style="text-indent: 1.5cm;">- Giá trị tài chính đính kèm: <strong><?= format_money(abs($qd['GiaTri'])) ?></strong> (<?= ($qd['GiaTri'] > 0) ? 'Thưởng cộng trực tiếp vào kỳ lương' : 'Khấu trừ vào kỳ lương' ?>).</p>
            <?php endif; ?>

            <p class="article-title">Điều 3. Trách nhiệm thi hành:</p>
            <p style="text-indent: 1.5cm;">Các Ông/Bà Giám đốc Khối Nhân sự, Giám đốc Tài chính Kế toán, Trưởng đơn vị trực thuộc và Ông/Bà <strong><?= htmlspecialchars($qd['HoTen']) ?></strong> chịu trách nhiệm thi hành Quyết định này.</p>
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
                    <strong>TM. HỘI ĐỒNG THI ĐƯA KHEN THƯỞNG</strong><br>
                    <strong>CHỦ TỊCH HỘI ĐỒNG</strong><br><br><br><br><br>
                    <strong>Nguyễn Thị Thanh Huyền</strong>
                </td>
            </tr>
        </table>
    </div>
</body>
</html>
