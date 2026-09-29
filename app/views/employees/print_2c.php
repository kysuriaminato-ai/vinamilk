<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <title><?= htmlspecialchars($title ?? 'In Sơ yếu lý lịch 2C') ?></title>
    <style>
        body {
            font-family: "Times New Roman", Times, serif;
            font-size: 13pt;
            line-height: 1.4;
            color: #000;
            background: #fff;
            margin: 0;
            padding: 20px;
        }

        .print-container {
            width: 210mm;
            min-height: 297mm;
            margin: 0 auto;
            background: #fff;
            padding: 10mm;
            box-sizing: border-box;
        }

        .header-table {
            width: 100%;
            margin-bottom: 20px;
        }

        .header-title {
            text-align: center;
            font-size: 16pt;
            font-weight: bold;
            text-transform: uppercase;
            margin: 20px 0 10px 0;
        }

        .sub-title {
            text-align: center;
            font-size: 11pt;
            font-style: italic;
            margin-bottom: 20px;
        }

        .photo-box {
            width: 3cm;
            height: 4cm;
            border: 1px solid #000;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 10pt;
            text-align: center;
        }

        table.info-table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 15px;
        }

        table.info-table td {
            padding: 4px 6px;
            vertical-align: top;
        }

        table.bordered {
            width: 100%;
            border-collapse: collapse;
            margin: 15px 0;
        }

        table.bordered th, table.bordered td {
            border: 1px solid #000;
            padding: 6px;
            font-size: 11pt;
        }

        table.bordered th {
            background-color: #f0f0f0;
            text-align: center;
        }

        .footer-sig {
            width: 100%;
            margin-top: 30px;
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
            box-shadow: 0 4px 10px rgba(0,0,0,0.2);
        }

        @media print {
            .btn-print { display: none; }
            body { padding: 0; }
            .print-container { padding: 0; width: 100%; }
        }
    </style>
</head>
<body>
    <button class="btn-print" onclick="window.print()">🖨️ In phôi Sơ yếu lý lịch (Ctrl+P)</button>

    <div class="print-container">
        <!-- Header Quốc hiệu -->
        <table class="header-table">
            <tr>
                <td style="width: 35%; vertical-align: top;">
                    <strong>CÔNG TY CP SỮA VIỆT NAM</strong><br>
                    <strong>CƠ CẤU TỔ CHỨC VNM</strong><br>
                    Mã NV: <strong><?= htmlspecialchars($emp['MaNV']) ?></strong>
                </td>
                <td style="text-align: center; vertical-align: top;">
                    <strong>CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</strong><br>
                    <strong>Độc lập - Tự do - Hạnh phúc</strong><br>
                    -------------------------
                </td>
            </tr>
        </table>

        <!-- Tiêu đề -->
        <div class="header-title">SƠ YẾU LÝ LỊCH CÁN BỘ, NHÂN VIÊN</div>
        <div class="sub-title">(Theo mẫu chuẩn quản trị nguồn nhân lực Vinamilk HRM)</div>

        <table style="width: 100%; margin-bottom: 20px;">
            <tr>
                <td style="width: 3.5cm; vertical-align: top;">
                    <div class="photo-box">
                        <?php if (!empty($emp['AnhChanDung'])): ?>
                            <img src="<?= asset($emp['AnhChanDung']) ?>" style="width: 100%; height: 100%; object-fit: cover;">
                        <?php else: ?>
                            Ảnh 3x4 / 4x6<br>Vinamilk
                        <?php endif; ?>
                    </div>
                </td>
                <td style="vertical-align: top; padding-left: 15px;">
                    <table class="info-table">
                        <tr>
                            <td style="width: 140px;">1) Họ và tên khai sinh:</td>
                            <td><strong style="text-transform: uppercase; font-size: 14pt;"><?= htmlspecialchars($emp['HoTen']) ?></strong></td>
                        </tr>
                        <tr>
                            <td>2) Tên thường gọi:</td>
                            <td><?= htmlspecialchars($emp['TenThuongGoi'] ?? '-') ?></td>
                        </tr>
                        <tr>
                            <td>3) Ngày sinh & Giới tính:</td>
                            <td><?= format_date($emp['NgaySinh']) ?> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Giới tính: <strong><?= $emp['GioiTinh'] ?></strong></td>
                        </tr>
                        <tr>
                            <td>4) Số CMND/CCCD:</td>
                            <td><?= htmlspecialchars($emp['SoCMND_CCCD']) ?> (Ngày cấp: <?= format_date($emp['NgayCap']) ?> - Nơi cấp: <?= htmlspecialchars($emp['NoiCap'] ?? '') ?>)</td>
                        </tr>
                        <tr>
                            <td>5) Quê quán:</td>
                            <td><?= htmlspecialchars($emp['QueQuan'] ?? '-') ?></td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>

        <table class="info-table">
            <tr>
                <td style="width: 170px;">6) Hộ khẩu thường trú:</td>
                <td><?= htmlspecialchars($emp['NoiDKKTTru'] ?? '-') ?></td>
            </tr>
            <tr>
                <td>7) Nơi ở hiện nay:</td>
                <td><?= htmlspecialchars($emp['DiaChiHienTai'] ?? '-') ?></td>
            </tr>
            <tr>
                <td>8) Dân tộc / Tôn giáo:</td>
                <td><?= htmlspecialchars($emp['DanToc'] ?? 'Kinh') ?> &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp; Tôn giáo: <?= htmlspecialchars($emp['TonGiao'] ?? 'Không') ?></td>
            </tr>
            <tr>
                <td>9) Đơn vị công tác hiện tại:</td>
                <td><strong><?= htmlspecialchars($emp['TenDV'] ?? 'Vinamilk') ?></strong></td>
            </tr>
            <tr>
                <td>10) Chức vụ / Vị trí:</td>
                <td><strong><?= htmlspecialchars($emp['TenCV'] ?? '-') ?></strong></td>
            </tr>
            <tr>
                <td>11) Ngạch lương chức danh:</td>
                <td><?= htmlspecialchars($emp['TenNgach'] ?? '-') ?> (Mã ngạch: <?= $emp['MaNgach'] ?> | Hệ số lương: <strong><?= $emp['HeSoLuongHienTai'] ?></strong>)</td>
            </tr>
            <tr>
                <td>12) Trình độ chuyên môn:</td>
                <td><?= htmlspecialchars($emp['TrinhDoHocVan'] ?? '-') ?> - Chuyên ngành: <?= htmlspecialchars($emp['ChuyenNganh'] ?? '-') ?> (Trường: <?= htmlspecialchars($emp['TruongDaoTao'] ?? '-') ?>)</td>
            </tr>
            <tr>
                <td>13) Vị trí lưu hồ sơ giấy:</td>
                <td><strong><?= htmlspecialchars($emp['ViTriLuuHoSo'] ?? 'Kho VP / Tủ 01') ?></strong></td>
            </tr>
        </table>

        <!-- Bảng Quá trình công tác -->
        <div style="font-weight: bold; margin-top: 15px; margin-bottom: 5px;">14) TÓM TẮT QUÁ TRÌNH CÔNG TÁC TẠI VINAMILK:</div>
        <table class="bordered">
            <thead>
                <tr>
                    <th style="width: 25%;">Từ tháng/năm đến tháng/năm</th>
                    <th style="width: 45%;">Chức danh, chức vụ, đơn vị công tác</th>
                    <th style="width: 30%;">Ghi chú</th>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($emp['QuatTrinhCongTac'])): ?>
                    <tr>
                        <td style="text-align: center;"><?= format_date($emp['NgayVaoLam']) ?> - Nay</td>
                        <td><?= htmlspecialchars($emp['TenCV'] ?? '') ?> tại <?= htmlspecialchars($emp['TenDV'] ?? '') ?></td>
                        <td>Hồ sơ tuyển dụng chính thức</td>
                    </tr>
                <?php else: ?>
                    <?php foreach ($emp['QuatTrinhCongTac'] as $qt): ?>
                        <tr>
                            <td style="text-align: center;"><?= format_date($qt['TuNgay']) ?> - <?= !empty($qt['DenNgay']) ? format_date($qt['DenNgay']) : 'Nay' ?></td>
                            <td><?= htmlspecialchars($qt['ChucVu']) ?> - <?= htmlspecialchars($qt['DonViCongTac']) ?></td>
                            <td><?= htmlspecialchars($qt['CongViecChinh'] ?? '') ?></td>
                        </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
        </table>

        <!-- Chữ ký xác nhận -->
        <table class="footer-sig">
            <tr>
                <td style="width: 50%; text-align: center; vertical-align: top;">
                    <strong>NGƯỜI KHAI KÝ TÊN</strong><br>
                    <i>(Ký và ghi rõ họ tên)</i><br><br><br><br><br>
                    <strong><?= htmlspecialchars($emp['HoTen']) ?></strong>
                </td>
                <td style="width: 50%; text-align: center; vertical-align: top;">
                    <i>TP. Hồ Chí Minh, ngày <?= date('d') ?> tháng <?= date('m') ?> năm <?= date('Y') ?></i><br>
                    <strong>TM. HỘI ĐỒNG QUẢN TRỊ / BAN GIÁM ĐỐC</strong><br>
                    <strong>GIÁM ĐỐC NHÂN SỰ VINAMILK</strong><br><br><br><br>
                    <strong>Nguyễn Thị Thanh Huyền</strong>
                </td>
            </tr>
        </table>
    </div>
</body>
</html>
