<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?= htmlspecialchars($report_title ?? 'Báo Cáo HUHA HRM - Vinamilk') ?></title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
    <style>
        body {
            font-family: 'Times New Roman', Times, serif;
            font-size: 13pt;
            color: #000;
            background: #f8f9fa;
        }

        .print-container {
            max-width: <?= $is_landscape ? '297mm' : '210mm' ?>;
            margin: 20px auto;
            background: #fff;
            padding: 20mm 15mm;
            box-shadow: 0 0 10px rgba(0,0,0,0.1);
        }

        .header-section table {
            width: 100%;
            border-collapse: collapse;
        }

        .report-title {
            text-transform: uppercase;
            font-weight: bold;
            font-size: 16pt;
            margin-top: 20px;
            margin-bottom: 5px;
            color: #002244;
        }

        .table-print {
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
            font-size: 11pt;
        }

        .table-print th, .table-print td {
            border: 1px solid #000 !important;
            padding: 6px 8px;
            vertical-align: middle;
        }

        .table-print th {
            background-color: #e9ecef !important;
            font-weight: bold;
            text-align: center;
        }

        .signature-section {
            margin-top: 30px;
            page-break-inside: avoid;
        }

        @media print {
            @page {
                size: <?= $is_landscape ? 'A4 landscape' : 'A4 portrait' ?>;
                margin: 10mm;
            }
            body {
                background: #fff !important;
                padding: 0 !important;
            }
            .print-container {
                max-width: 100% !important;
                margin: 0 !important;
                padding: 0 !important;
                box-shadow: none !important;
            }
            .no-print {
                display: none !important;
            }
        }
    </style>
</head>
<body>

    <!-- Bar điều khiển in -->
    <div class="no-print bg-dark text-white py-3 px-4 shadow-sm mb-3">
        <div class="d-flex justify-content-between align-items-center max-w-100">
            <div>
                <h6 class="mb-0 fw-bold"><i class="bi bi-printer me-2"></i>Xem Trước Trang In A4 (<?= $is_landscape ? 'Khổ Ngang' : 'Khổ Đứng' ?>)</h6>
                <small class="text-white-50">Nhấp "Thực hiện In" hoặc ấn Ctrl + P để xuất ra file PDF / Máy in</small>
            </div>
            <div class="d-flex gap-2">
                <button onclick="window.print();" class="btn btn-primary btn-sm rounded-pill px-4 fw-bold">
                    <i class="bi bi-printer-fill me-1"></i>Thực Hiện In / Export PDF
                </button>
                <button onclick="window.close();" class="btn btn-outline-light btn-sm rounded-pill px-3">
                    Đóng
                </button>
            </div>
        </div>
    </div>

    <!-- Khung Báo Cáo Chuẩn In A4 -->
    <div class="print-container">
        <!-- Header Quốc Hiệu & Tên Công Ty -->
        <div class="header-section mb-4">
            <table>
                <tr>
                    <td style="width: 45%; vertical-align: top; text-align: center;">
                        <strong style="text-transform: uppercase; font-size: 11pt;">CÔNG TY CỔ PHẦN SỮA VIỆT NAM</strong><br>
                        <span style="font-size: 10pt; font-style: italic;">Tập đoàn Vinamilk - Khối Nhân Sự</span><br>
                        ---------------------
                    </td>
                    <td style="width: 55%; vertical-align: top; text-align: center;">
                        <strong style="text-transform: uppercase; font-size: 11pt;">CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</strong><br>
                        <strong style="font-size: 10pt;">Độc lập - Tự do - Hạnh phúc</strong><br>
                        ---------------------------------
                    </td>
                </tr>
            </table>
        </div>

        <!-- Tiêu đề báo cáo -->
        <div class="text-center mb-4">
            <h2 class="report-title"><?= htmlspecialchars($report_title ?? 'BÁO CÁO NHÂN SỰ') ?></h2>
            <p class="mb-0" style="font-size: 11pt; font-style: italic;">
                (Ngày xuất báo cáo: <?= date('d/m/Y H:i') ?> - Theo tiêu chuẩn quản trị HUHA HRM)
            </p>
        </div>

        <!-- Bảng Dữ Liệu In -->
        <table class="table-print">
            <thead>
                <tr>
                    <th style="width: 35px;">STT</th>
                    <?php foreach ($headers as $h): ?>
                        <th><?= htmlspecialchars($h) ?></th>
                    <?php endforeach; ?>
                </tr>
            </thead>
            <tbody>
                <?php if (empty($rows)): ?>
                    <tr>
                        <td colspan="<?= count($headers) + 1 ?>" class="text-center py-3">
                            <em>Không có dữ liệu báo cáo phù hợp điều kiện lọc.</em>
                        </td>
                    </tr>
                <?php else: ?>
                    <?php $stt = 1; foreach ($rows as $row): ?>
                        <tr>
                            <td class="text-center"><?= $stt++ ?></td>
                            <?php foreach ($row as $val): ?>
                                <td><?= htmlspecialchars((string)$val) ?></td>
                            <?php endforeach; ?>
                        </tr>
                    <?php endforeach; ?>
                <?php endif; ?>
            </tbody>
        </table>

        <!-- Summary note if exists -->
        <?php if (!empty($summary)): ?>
            <div class="mt-3" style="font-size: 11pt;">
                <strong>Tổng kết / Ghi chú:</strong> <?= htmlspecialchars($summary) ?>
            </div>
        <?php endif; ?>

        <!-- Chữ Ký Các Cấp -->
        <div class="signature-section">
            <table style="width: 100%; border: none;">
                <tr>
                    <td style="width: 33%; text-align: center; vertical-align: top; border: none;">
                        <strong>NGƯỜI LẬP BÁO CÁO</strong><br>
                        <span style="font-size: 10pt; font-style: italic;">(Ký, ghi rõ họ tên)</span>
                        <br><br><br><br>
                        <strong><?= htmlspecialchars($_SESSION['user']['ho_ten'] ?? 'Chuyên viên HR') ?></strong>
                    </td>
                    <td style="width: 33%; text-align: center; vertical-align: top; border: none;">
                        <strong>TRƯỞNG PHÒNG TỔ CHỨC CÁN BỘ</strong><br>
                        <span style="font-size: 10pt; font-style: italic;">(Ký, ghi rõ họ tên)</span>
                        <br><br><br><br>
                        <strong>Phạm Hoàng Nam</strong>
                    </td>
                    <td style="width: 34%; text-align: center; vertical-align: top; border: none;">
                        <span style="font-size: 10pt; font-style: italic;">TP. Hồ Chí Minh, ngày <?= date('d') ?> tháng <?= date('m') ?> năm <?= date('Y') ?></span><br>
                        <strong>TỔNG GIÁM ĐỐC / BAN GIÁM ĐỐC</strong><br>
                        <span style="font-size: 10pt; font-style: italic;">(Ký tên và đóng dấu)</span>
                        <br><br><br><br>
                        <strong>Mai Kiều Liên</strong>
                    </td>
                </tr>
            </table>
        </div>
    </div>

</body>
</html>
