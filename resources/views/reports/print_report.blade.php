<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="UTF-8">
    <title>{{ $title ?? 'In Báo cáo Vinamilk' }}</title>
    <style>
        @page {
            size: A4 landscape;
            margin: 12mm 15mm 15mm 15mm;
        }
        body {
            font-family: 'Times New Roman', Times, serif;
            font-size: 11pt;
            color: #000;
            background: #fff;
            margin: 0;
            padding: 10px;
        }
        .header-table {
            width: 100%;
            margin-bottom: 20px;
            border-collapse: collapse;
        }
        .header-table td {
            vertical-align: top;
            border: none;
            padding: 0;
        }
        .company-name {
            font-size: 11pt;
            font-weight: bold;
            text-transform: uppercase;
        }
        .national-title {
            text-align: center;
            font-size: 11pt;
            font-weight: bold;
        }
        .national-motto {
            text-align: center;
            font-size: 10pt;
            font-style: italic;
        }
        .report-title {
            text-align: center;
            font-size: 16pt;
            font-weight: bold;
            text-transform: uppercase;
            margin: 15px 0 5px 0;
        }
        .report-subtitle {
            text-align: center;
            font-size: 11pt;
            font-style: italic;
            margin-bottom: 20px;
        }
        .data-table {
            width: 100%;
            border-collapse: collapse;
            font-size: 10pt;
        }
        .data-table th, .data-table td {
            border: 1px solid #000;
            padding: 5px 8px;
            page-break-inside: avoid;
        }
        .data-table th {
            background-color: #f2f2f2;
            font-weight: bold;
            text-align: center;
        }
        .text-center { text-align: center; }
        .text-right { text-align: right; }
        .signature-table {
            width: 100%;
            margin-top: 30px;
            border-collapse: collapse;
            page-break-inside: avoid;
        }
        .signature-table td {
            border: none;
            text-align: center;
            vertical-align: top;
            width: 33.33%;
        }
        .sign-role { font-weight: bold; }
        .sign-note { font-style: italic; font-size: 9pt; }
        .sign-space { height: 70px; }
        @media print {
            .no-print { display: none !important; }
            body { padding: 0; }
        }
    </style>
</head>
<body>
    <div class="no-print" style="margin-bottom: 20px; text-align: right;">
        <button onclick="window.print()" style="padding: 10px 20px; background: #003399; color: #fff; border: none; border-radius: 4px; cursor: pointer; font-weight: bold; font-size: 14px;">
            🖨️ In Báo cáo (Ctrl + P)
        </button>
        <button onclick="window.close()" style="padding: 10px 15px; background: #6c757d; color: #fff; border: none; border-radius: 4px; cursor: pointer; font-size: 14px; margin-left: 10px;">
            Đóng
        </button>
    </div>

    <!-- Header Quốc hiệu & Tiêu ngữ Doanh nghiệp -->
    <table class="header-table">
        <tr>
            <td style="width: 50%;">
                <div class="company-name">TẬP ĐOÀN SỮA VIỆT NAM (VINAMILK)</div>
                <div>Số: ...... /BC-HR/VNM</div>
            </td>
            <td style="width: 50%;">
                <div class="national-title">CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM</div>
                <div class="national-motto">Độc lập - Tự do - Hạnh phúc</div>
                <div style="text-align: center;">-----------------------</div>
            </td>
        </tr>
    </table>

    @if($type === 'bm01')
        <div class="report-title">BIỂU MẪU 01: SƠ YẾU LÝ LỊCH TRÍCH NGANG CÁN BỘ CÔNG NHÂN VIÊN</div>
        <div class="report-subtitle">Thời điểm trích xuất: Ngày {{ date('d/m/Y') }}</div>
        <table class="data-table">
            <thead>
                <tr>
                    <th style="width: 30px;">STT</th>
                    <th>Mã NV</th>
                    <th>Họ và Tên</th>
                    <th>Giới tính</th>
                    <th>Ngày sinh</th>
                    <th>Số CCCD</th>
                    <th>Đơn vị / Phòng ban</th>
                    <th>Chức danh</th>
                    <th>Khung lương</th>
                    <th>Lương P1 (đ)</th>
                    <th>Lương P2 (đ)</th>
                    <th>Lương đóng BH (đ)</th>
                    <th>Trạng thái</th>
                </tr>
            </thead>
            <tbody>
                @forelse($list as $idx => $r)
                <tr>
                    <td class="text-center">{{ $idx + 1 }}</td>
                    <td class="text-center"><strong>{{ $r->employee_code }}</strong></td>
                    <td>{{ $r->full_name }}</td>
                    <td class="text-center">{{ $r->gender === 'MALE' ? 'Nam' : 'Nữ' }}</td>
                    <td class="text-center">{{ date('d/m/Y', strtotime($r->date_of_birth)) }}</td>
                    <td class="text-center">{{ $r->national_id }}</td>
                    <td>{{ $r->unit_name }}</td>
                    <td>{{ $r->position_name }}</td>
                    <td class="text-center">{{ $r->band_code ?? 'BAND-STD' }}</td>
                    <td class="text-right">{{ number_format($r->p1_position_salary, 0, ',', '.') }}</td>
                    <td class="text-right">{{ number_format($r->p2_competency_salary, 0, ',', '.') }}</td>
                    <td class="text-right">{{ number_format($r->insurance_salary, 0, ',', '.') }}</td>
                    <td class="text-center">{{ $r->employment_status }}</td>
                </tr>
                @empty
                <tr><td colspan="13" class="text-center">Không có dữ liệu phù hợp</td></tr>
                @endforelse
            </tbody>
        </table>
    @elseif($type === 'bm02')
        <div class="report-title">BIỂU MẪU 02: BÁO CÁO BIẾN ĐỘNG NHÂN SỰ</div>
        <div class="report-subtitle">Năm {{ $filters['year'] ?? date('Y') }} - Trích xuất ngày {{ date('d/m/Y') }}</div>
        <table class="data-table">
            <thead>
                <tr>
                    <th style="width: 30px;">STT</th>
                    <th>Mã NV</th>
                    <th>Họ và Tên</th>
                    <th>Loại biến động</th>
                    <th>Diễn giải</th>
                    <th>Ngày hiệu lực</th>
                    <th>Đơn vị cũ</th>
                    <th>Đơn vị mới</th>
                    <th>Chức vụ mới</th>
                    <th>Số Quyết định</th>
                </tr>
            </thead>
            <tbody>
                @forelse($list as $idx => $r)
                <tr>
                    <td class="text-center">{{ $idx + 1 }}</td>
                    <td class="text-center"><strong>{{ $r->employee_code }}</strong></td>
                    <td>{{ $r->full_name }}</td>
                    <td class="text-center"><strong>{{ $r->loai_bien_dong }}</strong></td>
                    <td>{{ $r->dien_giai }}</td>
                    <td class="text-center">{{ date('d/m/Y', strtotime($r->ngay_hieu_luc)) }}</td>
                    <td>{{ $r->don_vi_cu }}</td>
                    <td>{{ $r->don_vi_moi }}</td>
                    <td>{{ $r->chuc_vu_moi }}</td>
                    <td class="text-center">{{ $r->so_quyet_dinh }}</td>
                </tr>
                @empty
                <tr><td colspan="10" class="text-center">Không có biến động nhân sự trong kỳ</td></tr>
                @endforelse
            </tbody>
        </table>
    @elseif($type === 'bm03')
        <div class="report-title">BIỂU MẪU 03: THỐNG KÊ KHEN THƯỞNG VÀ KỶ LUẬT</div>
        <div class="report-subtitle">Năm {{ $filters['year'] ?? date('Y') }} - Trích xuất ngày {{ date('d/m/Y') }}</div>
        <table class="data-table">
            <thead>
                <tr>
                    <th style="width: 30px;">STT</th>
                    <th>Số Quyết định</th>
                    <th>Ngày QĐ</th>
                    <th>Mã NV</th>
                    <th>Họ và Tên</th>
                    <th>Đơn vị</th>
                    <th>Chức danh</th>
                    <th>Hình thức</th>
                    <th>Lý do</th>
                    <th>Tiền thưởng (VND)</th>
                </tr>
            </thead>
            <tbody>
                @forelse($list as $idx => $r)
                <tr>
                    <td class="text-center">{{ $idx + 1 }}</td>
                    <td class="text-center"><strong>{{ $r->so_quyet_dinh }}</strong></td>
                    <td class="text-center">{{ date('d/m/Y', strtotime($r->ngay_quyet_dinh)) }}</td>
                    <td class="text-center"><strong>{{ $r->employee_code }}</strong></td>
                    <td>{{ $r->full_name }}</td>
                    <td>{{ $r->unit_name }}</td>
                    <td>{{ $r->position_name }}</td>
                    <td>{{ $r->hinh_thuc }}</td>
                    <td>{{ $r->ly_do }}</td>
                    <td class="text-right">{{ number_format($r->so_tien_thuong, 0, ',', '.') }}</td>
                </tr>
                @empty
                <tr><td colspan="10" class="text-center">Không có bản ghi khen thưởng kỷ luật</td></tr>
                @endforelse
            </tbody>
        </table>
    @elseif($type === 'bm04')
        <div class="report-title">BIỂU MẪU 04: DANH SÁCH NHÂN SỰ TIỆM CẬN NGHỈ HƯU (NĐ 135/2020)</div>
        <div class="report-subtitle">Thời điểm rà soát 12 tháng tới - Ngày {{ date('d/m/Y') }}</div>
        <table class="data-table">
            <thead>
                <tr>
                    <th style="width: 30px;">STT</th>
                    <th>Mã NV</th>
                    <th>Họ và Tên</th>
                    <th>Giới tính</th>
                    <th>Ngày sinh</th>
                    <th>Tuổi hiện tại</th>
                    <th>Đơn vị</th>
                    <th>Chức danh</th>
                    <th>Ngày nghỉ hưu (NĐ 135/2020)</th>
                    <th>Số tháng còn lại</th>
                    <th>Số ngày còn lại</th>
                </tr>
            </thead>
            <tbody>
                @forelse($list as $idx => $r)
                <tr>
                    <td class="text-center">{{ $idx + 1 }}</td>
                    <td class="text-center"><strong>{{ $r->employee_code }}</strong></td>
                    <td>{{ $r->full_name }}</td>
                    <td class="text-center">{{ $r->gender === 'MALE' ? 'Nam' : 'Nữ' }}</td>
                    <td class="text-center">{{ date('d/m/Y', strtotime($r->date_of_birth)) }}</td>
                    <td class="text-center">{{ $r->current_age }}</td>
                    <td>{{ $r->unit_name }}</td>
                    <td>{{ $r->position_name }}</td>
                    <td class="text-center" style="color: #b30000; font-weight: bold;">{{ date('d/m/Y', strtotime($r->retirement_date)) }}</td>
                    <td class="text-center"><strong>{{ $r->months_left }} tháng</strong></td>
                    <td class="text-center">{{ $r->days_left }} ngày</td>
                </tr>
                @empty
                <tr><td colspan="11" class="text-center">Không có nhân sự tiệm cận nghỉ hưu trong 12 tháng tới</td></tr>
                @endforelse
            </tbody>
        </table>
    @endif

    <!-- Chữ ký 3 bên -->
    <table class="signature-table">
        <tr>
            <td>
                <div class="sign-role">NGƯỜI LẬP BIỂU</div>
                <div class="sign-note">(Ký, ghi rõ họ tên)</div>
                <div class="sign-space"></div>
                <div><strong>{{ auth()->user()->name ?? 'Chuyên viên Nhân sự' }}</strong></div>
            </td>
            <td>
                <div class="sign-role">TRƯỞNG PHÒNG NHÂN SỰ</div>
                <div class="sign-note">(Ký, ghi rõ họ tên)</div>
                <div class="sign-space"></div>
                <div><strong>Ban Nhân sự Vinamilk</strong></div>
            </td>
            <td>
                <div class="sign-role">GIÁM ĐỐC ĐƠN VỊ / TỔNG GIÁM ĐỐC</div>
                <div class="sign-note">(Ký, đóng dấu)</div>
                <div class="sign-space"></div>
                <div><strong>Lãnh đạo phê duyệt</strong></div>
            </td>
        </tr>
    </table>
</body>
</html>
