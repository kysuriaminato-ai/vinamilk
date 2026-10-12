<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title><?= e($title ?? 'Trích lục hồ sơ nhân sự') ?></title>
    <link href="https://fonts.googleapis.com/css2?family=Times+New+Roman&display=swap" rel="stylesheet">
    <style>
        body { font-family: 'Times New Roman', Times, serif; font-size: 13pt; line-height: 1.4; color: #000; margin: 0; padding: 20px; background: #f1f5f9; }
        .page { width: 210mm; min-height: 297mm; padding: 20mm; margin: 0 auto; background: #fff; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }
        .header { display: flex; justify-content: space-between; border-bottom: 2px solid #000; padding-bottom: 10px; margin-bottom: 20px; }
        .title { text-align: center; font-size: 17pt; font-weight: bold; text-transform: uppercase; margin-bottom: 20px; }
        .section-title { font-weight: bold; font-size: 14pt; margin-top: 15px; margin-bottom: 8px; border-bottom: 1px dotted #888; text-transform: uppercase; }
        .grid-row { display: flex; margin-bottom: 6px; }
        .grid-label { width: 220px; font-weight: bold; color: #333; }
        .grid-value { flex: 1; }
        .no-print { text-align: center; margin-bottom: 15px; }
        @media print {
            body { background: #fff; padding: 0; }
            .page { width: 100%; box-shadow: none; padding: 15mm; }
            .no-print { display: none; }
        }
    </style>
</head>
<body>
<div class="no-print">
    <button onclick="window.print()" style="padding: 8px 20px; background: #0033A0; color:#fff; border:none; border-radius:4px; font-weight:bold; cursor:pointer;">In Trích Lục A4</button>
    <button onclick="window.close()" style="padding: 8px 16px; margin-left: 10px; cursor:pointer;">Đóng</button>
</div>

<div class="page">
    <div class="header">
        <div>
            <strong>CÔNG TY CỔ PHẦN SỮA VIỆT NAM</strong><br>
            <small>HỆ THỐNG QUẢN LÝ NHÂN SỰ VINAMILK</small>
        </div>
        <div style="text-align: right;">
            <strong>MÃ NHÂN VIÊN: <?= e($employee['MaNV']) ?></strong><br>
            <small>Ngày trích lục: <?= date('d/m/Y') ?></small>
        </div>
    </div>

    <div class="title">BẢN TRÍCH LỤC SƠ YẾU LÝ LỊCH NHÂN VIÊN</div>

    <div class="section-title">I. Thông Tin Cá Nhân</div>
    <div class="grid-row"><div class="grid-label">Họ và tên:</div><div class="grid-value"><strong><?= e($employee['HoTen']) ?></strong></div></div>
    <div class="grid-row"><div class="grid-label">Ngày sinh:</div><div class="grid-value"><?= format_date($employee['NgaySinh']) ?> (Giới tính: <?= e($employee['GioiTinh']) ?>)</div></div>
    <div class="grid-row"><div class="grid-label">Số CCCD / Định danh:</div><div class="grid-value"><?= e($employee['SoCMND_CCCD']) ?> &bull; Nơi cấp: <?= e($employee['NoiCap'] ?: 'Cục CS QLHC về TTXH') ?></div></div>
    <div class="grid-row"><div class="grid-label">Mã số thuế:</div><div class="grid-value"><?= e($employee['MaSoThue'] ?: 'Chưa cập nhật') ?></div></div>
    <div class="grid-row"><div class="grid-label">Quê quán:</div><div class="grid-value"><?= e($employee['QueQuan'] ?: 'Chưa cập nhật') ?></div></div>
    <div class="grid-row"><div class="grid-label">Địa chỉ hiện tại:</div><div class="grid-value"><?= e($employee['DiaChiHienTai'] ?: 'Chưa cập nhật') ?></div></div>
    <div class="grid-row"><div class="grid-label">Điện thoại / Email:</div><div class="grid-value"><?= e($employee['SoDienThoai']) ?> &bull; <?= e($employee['Email']) ?></div></div>

    <div class="section-title">II. Vị Trí Công Tác & Cơ Cấu</div>
    <div class="grid-row"><div class="grid-label">Đơn vị công tác:</div><div class="grid-value"><strong><?= e($employee['TenDV'] ?: $employee['MaDV']) ?></strong></div></div>
    <div class="grid-row"><div class="grid-label">Chức danh / Chức vụ:</div><div class="grid-value"><?= e($employee['TenCV'] ?: ($employee['ChucVu'] ?? 'Nhân viên')) ?></div></div>
    <div class="grid-row"><div class="grid-label">Ngày bắt đầu vào làm:</div><div class="grid-value"><?= format_date($employee['NgayVaoLam']) ?> (Thâm niên: <?= ($employee['ThamNienNam'] ?? 0) ?> năm)</div></div>
    <div class="grid-row"><div class="grid-label">Quản lý trực tiếp:</div><div class="grid-value"><?= e($employee['TenQuanLy'] ?: 'Không có') ?></div></div>
    <div class="grid-row"><div class="grid-label">Hợp đồng hiện tại:</div><div class="grid-value"><?= e($employee['LoaiHopDong'] ?: 'Chưa cập nhật') ?> (Hạn: <?= !empty($employee['NgayHetHanHD']) ? format_date($employee['NgayHetHanHD']) : 'Vô thời hạn' ?>)</div></div>

    <div class="section-title">III. Trình Độ Đào Tạo & Chuyên Môn</div>
    <div class="grid-row"><div class="grid-label">Trình độ học vấn:</div><div class="grid-value"><?= e($employee['TrinhDoHocVan'] ?: 'Đại học') ?></div></div>
    <div class="grid-row"><div class="grid-label">Chuyên ngành đào tạo:</div><div class="grid-value"><?= e($employee['ChuyenNganh'] ?: 'Chưa cập nhật') ?></div></div>
    <div class="grid-row"><div class="grid-label">Trường đào tạo:</div><div class="grid-value"><?= e($employee['TruongDaoTao'] ?: 'Chưa cập nhật') ?></div></div>

    <div class="section-title">IV. Chế Độ Lương & Phúc Lợi</div>
    <div class="grid-row"><div class="grid-label">Lương đóng BHXH:</div><div class="grid-value"><strong><?= is_numeric($employee['LuongBaoHiem']) ? format_money($employee['LuongBaoHiem']) : e($employee['LuongBaoHiem']) ?></strong></div></div>
    <div class="grid-row"><div class="grid-label">Hệ số lương / Bậc lương:</div><div class="grid-value">Hệ số <?= number_format((float)($employee['HeSoLuongHienTai'] ?? 1.0), 2) ?> &bull; Bậc <?= (int)($employee['BacLuongHienTai'] ?? 1) ?></div></div>
    <div class="grid-row"><div class="grid-label">Số người phụ thuộc:</div><div class="grid-value"><?= (int)($employee['SoNguoiPhuThuoc'] ?? 0) ?> người</div></div>

    <div style="display: flex; justify-content: space-between; margin-top: 50px;">
        <div style="text-align: center; width: 45%;">
            <strong>NGƯỜI TRÍCH LỤC</strong><br>
            <em>(Ký, ghi rõ họ tên)</em>
        </div>
        <div style="text-align: center; width: 45%;">
            <strong>XÁC NHẬN CỦA PHÒNG NHÂN SỰ</strong><br>
            <em>(Ký tên, đóng dấu)</em>
        </div>
    </div>
</div>
</body>
</html>
