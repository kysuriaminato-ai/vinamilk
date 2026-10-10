<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <title><?= htmlspecialchars($title ?? 'Phiếu Lương Cá Nhân') ?></title>
    <style>
        body {
            font-family: Arial, sans-serif;
            font-size: 13px;
            color: #1e293b;
            background: #f1f5f9;
            padding: 30px 10px;
        }

        .payslip-card {
            width: 210mm;
            margin: 0 auto;
            background: #ffffff;
            border-radius: 12px;
            padding: 30px;
            box-shadow: 0 4px 15px rgba(0,0,0,0.08);
            box-sizing: border-box;
            border: 1px solid #cbd5e1;
        }

        .payslip-header {
            display: flex;
            align-items: center;
            justify-content: space-between;
            border-bottom: 2px solid #005696;
            padding-bottom: 15px;
            margin-bottom: 20px;
        }

        .logo-text {
            font-size: 20px;
            font-weight: 800;
            color: #005696;
            letter-spacing: 0.5px;
        }

        .payslip-title {
            text-align: center;
            font-size: 18px;
            font-weight: bold;
            color: #0f172a;
            text-transform: uppercase;
            margin-bottom: 20px;
        }

        .info-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 15px;
            background: #f8fafc;
            padding: 15px;
            border-radius: 8px;
            margin-bottom: 20px;
            border: 1px solid #e2e8f0;
        }

        table.payslip-table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 20px;
        }

        table.payslip-table th, table.payslip-table td {
            padding: 10px 12px;
            border-bottom: 1px solid #e2e8f0;
        }

        table.payslip-table th {
            background-color: #f1f5f9;
            text-align: left;
            color: #475569;
            font-size: 12px;
            text-transform: uppercase;
        }

        .section-header {
            background: #e2e8f0;
            font-weight: bold;
            color: #0f172a;
        }

        .net-pay-box {
            background: #ecfdf5;
            border: 2px dashed #10b981;
            padding: 15px 20px;
            border-radius: 8px;
            display: flex;
            align-items: center;
            justify-content: space-between;
            margin-top: 20px;
        }

        .btn-print {
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 10px 20px;
            background: #005696;
            color: #fff;
            border: none;
            border-radius: 6px;
            font-weight: bold;
            cursor: pointer;
        }

        @media print {
            .btn-print { display: none; }
            body { background: #fff; padding: 0; }
            .payslip-card { box-shadow: none; border: none; width: 100%; }
        }
    </style>
</head>
<body>
    <button class="btn-print" onclick="window.print()">🖨️ In Phiếu Lương (Ctrl+P)</button>

    <div class="payslip-card">
        <!-- Payslip Header -->
        <div class="payslip-header">
            <div>
                <div class="logo-text">🥛 VINAMILK HRM</div>
                <div style="font-size: 11px; color: #64748b;">Công ty Cổ phần Sữa Việt Nam</div>
            </div>
            <div style="text-align: right;">
                <div style="font-weight: bold; color: #005696;">PHIẾU LƯƠNG ĐIỆN TỬ CÁ NHÂN</div>
                <div style="font-size: 12px; color: #64748b;">Kỳ lương: Tháng <strong><?= $ps['Thang'] ?>/<?= $ps['Nam'] ?></strong></div>
            </div>
        </div>

        <!-- Employee Summary Info -->
        <div class="info-grid">
            <div>
                <div>Họ và tên: <strong style="font-size: 14px; color: #0f172a;"><?= htmlspecialchars($ps['HoTen']) ?></strong></div>
                <div>Mã số nhân viên: <code><?= htmlspecialchars($ps['MaNV']) ?></code></div>
                <div>Đơn vị: <strong><?= htmlspecialchars($ps['TenDV'] ?? 'Vinamilk') ?></strong></div>
            </div>
            <div>
                <div>Chức vụ: <strong><?= htmlspecialchars($ps['TenCV'] ?? '-') ?></strong></div>
                <div>Ngạch lương: <strong><?= htmlspecialchars($ps['TenNgach'] ?? '-') ?></strong> (Hệ số: <?= $ps['HeSoLuong'] ?>)</div>
                <div>Số người phụ thuộc: <strong><?= $ps['SoNguoiPhuThuoc'] ?> người</strong></div>
            </div>
        </div>

        <!-- Payslip Calculations Table -->
        <table class="payslip-table">
            <thead>
                <tr>
                    <th>Hạng mục thu nhập / Khấu trừ 3P</th>
                    <th style="text-align: right;">Số tiền (VNĐ)</th>
                </tr>
            </thead>
            <tbody>
                <!-- Khoản I: Thu nhập -->
                <tr class="section-header">
                    <td colspan="2">I. THỦ NHẬP LƯƠNG THÁNG (GROSS INCOME)</td>
                </tr>
                <tr>
                    <td style="padding-left: 20px;">1. Lương cơ bản P1 (<?= $ps['SoNgayCong'] ?> công thực tế)</td>
                    <td style="text-align: right; font-weight: bold;"><?= format_money($ps['LuongCoBan']) ?></td>
                </tr>
                <tr>
                    <td style="padding-left: 20px;">2. Phụ cấp Chức vụ</td>
                    <td style="text-align: right;"><?= format_money($ps['PhuCapChucVu']) ?></td>
                </tr>
                <tr>
                    <td style="padding-left: 20px;">3. Phụ cấp Khác (Ăn trưa, ĐT, Xăng xe)</td>
                    <td style="text-align: right;"><?= format_money($ps['PhuCapKhac']) ?></td>
                </tr>
                <tr>
                    <td style="padding-left: 20px;">4. Lương năng lực P2</td>
                    <td style="text-align: right;"><?= format_money($ps['LuongNangLuc']) ?></td>
                </tr>
                <tr>
                    <td style="padding-left: 20px;">5. Thưởng hiệu quả / KPI P3</td>
                    <td style="text-align: right;"><?= format_money($ps['ThuongKPI']) ?></td>
                </tr>
                <tr>
                    <td style="padding-left: 20px;">6. Tiền tăng ca & Thưởng khác</td>
                    <td style="text-align: right;"><?= format_money($ps['ThuongKhac']) ?></td>
                </tr>
                <tr style="font-weight: bold; background: #f8fafc;">
                    <td>TỔNG THU NHẬP (GROSS)</td>
                    <td style="text-align: right; color: #005696; font-size: 14px;"><?= format_money($ps['TongThuNhap']) ?></td>
                </tr>

                <!-- Khoản II: Khấu trừ -->
                <tr class="section-header">
                    <td colspan="2">II. CÁC KHOẢN TRÍCH NỘP & KHẤU TRỪ</td>
                </tr>
                <tr>
                    <td style="padding-left: 20px;">1. Bảo hiểm trích NLD (BHXH 8%, BHYT 1.5%, BHTN 1% = 10.5%)</td>
                    <td style="text-align: right; color: #ef4444;"><?= format_money($ps['KhauTruBHXH']) ?></td>
                </tr>
                <tr>
                    <td style="padding-left: 20px;">2. Thuế Thu nhập cá nhân (Biểu lũy tiến từng phần)</td>
                    <td style="text-align: right; color: #f59e0b;"><?= format_money($ps['ThueTNCN']) ?></td>
                </tr>
                <?php if ($ps['KhauTruKhac'] > 0): ?>
                    <tr>
                        <td style="padding-left: 20px;">3. Khấu trừ Kỷ luật phạt / Khác</td>
                        <td style="text-align: right; color: #ef4444;"><?= format_money($ps['KhauTruKhac']) ?></td>
                    </tr>
                <?php endif; ?>
            </tbody>
        </table>

        <!-- Net Real Income Box -->
        <div class="net-pay-box">
            <div>
                <div style="font-size: 12px; color: #047857; text-transform: uppercase; font-weight: bold;">LƯƠNG THỰC LĨNH CHUYỂN KHOẢN (NET PAY)</div>
                <div style="font-size: 11px; color: #64748b;">TK Ngân hàng: <?= htmlspecialchars($ps['SoTaiKhoanNH'] ?? 'Chưa cập nhật') ?> (<?= htmlspecialchars($ps['NganHang'] ?? 'Vietcombank') ?>)</div>
            </div>
            <div style="font-size: 22px; font-weight: 800; color: #047857;">
                <?= format_money($ps['ThucLinh']) ?>
            </div>
        </div>

        <div style="margin-top: 30px; display: flex; justify-content: space-between; text-align: center; font-size: 11px; color: #64748b;">
            <div>
                <strong>NGƯỜI LẬP BẢNG LƯƠNG</strong><br><br><br><br>
                <span>Phòng Nhân sự VNM</span>
            </div>
            <div>
                <strong>KẾ TOÁN TRƯỞNG DUYỆT</strong><br><br><br><br>
                <span>Hà Thị Phương Linh</span>
            </div>
        </div>
    </div>
</body>
</html>
