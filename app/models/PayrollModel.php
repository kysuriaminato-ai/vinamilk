<?php
/**
 * =====================================================================
 * VINAMILK HRM - Payroll Model (Bảng lương 3P, quy định 2026)
 * =====================================================================
 *
 * Mỗi nhân viên được tính bằng computeEmployee(): hàm trả về cả các con số
 * (lưu vào bảng `bangluong`) lẫn danh sách các bước tính có công thức,
 * để trang "Chi tiết tính lương" hiển thị đúng phép tính đã dùng.
 *
 * Căn cứ tham số (cần đối chiếu khi pháp luật thay đổi):
 *  - Giảm trừ gia cảnh 15.500.000 ₫ (bản thân), 6.200.000 ₫/người phụ thuộc (áp dụng từ kỳ tính thuế 2026)
 *  - Biểu thuế lũy tiến 5 bậc 5% - 10% - 20% - 30% - 35% (Luật Thuế TNCN sửa đổi, từ 2026)
 *  - Lương cơ sở 2.530.000 ₫ -> trần đóng BHXH, BHYT = 20 lần = 50.600.000 ₫
 *  - Lương tối thiểu vùng I 5.310.000 ₫ (sàn đóng bảo hiểm)
 *  - Người lao động đóng BHXH 8%, BHYT 1,5%, BHTN 1%
 *  - Tăng ca ngày thường 150%; phần trả cao hơn lương giờ bình thường được miễn thuế
 */

require_once APP_DIR . '/core/Model.php';

class PayrollModel extends Model {
    protected string $table = 'bangluong';
    protected string $primaryKey = 'MaBL';

    // Mức lương cơ sở nội bộ Vinamilk dùng nhân với hệ số lương để ra P1
    const LUONG_CO_SO = 5000000;
    // Lương cơ sở Nhà nước -> trần BHXH, BHYT
    const LUONG_CO_SO_NN = 2530000;
    const MAX_LUONG_BHXH = 50600000;        // 20 x 2.530.000
    const LUONG_TOI_THIEU_VUNG = 5310000;   // Vùng I
    const GIAM_TRU_BAN_THAN = 15500000;
    const GIAM_TRU_PHU_THUOC = 6200000;
    const AN_TRUA = 730000;                 // Phụ cấp ăn trưa (không chịu thuế đến mức này)
    const DI_LAI = 800000;                  // Phụ cấp đi lại (chịu thuế)
    const TY_LE_KPI = 0.15;                 // P3 mục tiêu = 15% P1
    const TRU_KPI_MOI_LAN_TRE = 0.05;       // Mỗi lần đi trễ / về sớm trừ 5% thưởng KPI
    const HE_SO_TANG_CA = 1.5;
    const GIO_MOT_CONG = 8;

    /** Biểu thuế lũy tiến 5 bậc: [cận dưới, cận trên, thuế suất %] */
    const BIEU_THUE = [
        [0, 10000000, 5],
        [10000000, 30000000, 10],
        [30000000, 60000000, 20],
        [60000000, 100000000, 30],
        [100000000, PHP_INT_MAX, 35],
    ];

    /** Hệ số năng lực P2 theo trình độ học vấn cao nhất */
    const HE_SO_NANG_LUC = [
        'Tiến sĩ' => 0.80,
        'Thạc sĩ' => 0.60,
        'Đại học' => 0.40,
        'Cao đẳng' => 0.30,
        'Trung cấp' => 0.25,
    ];

    // ------------------------------------------------------------------
    // TÍNH LƯƠNG
    // ------------------------------------------------------------------

    /**
     * Tính lương tháng cho TẤT CẢ nhân viên đang làm việc / thử việc.
     * Người chưa có chấm công tháng đó được tạm tính đủ công chuẩn (ghi chú rõ trên bảng lương).
     */
    public function calculateMonthlyPayroll(int $thang, int $nam): array {
        $rows = $this->getPayrollInputs($thang, $nam);
        $ktklMap = $this->getKtklMap($thang, $nam);

        $result = [];
        foreach ($rows as $row) {
            $calc = $this->computeEmployee($row, $ktklMap[$row['MaNV']] ?? [], $thang, $nam);
            $this->upsertPayrollRecord($calc['record']);
            $result[] = $calc['record'];
        }
        return $result;
    }

    /**
     * Dữ liệu đầu vào để tính lương: hồ sơ + chức vụ + chấm công của tháng (có thể chưa có).
     */
    public function getPayrollInputs(int $thang, int $nam, ?string $maNV = null): array {
        $cuoiThang = sprintf('%04d-%02d-%02d', $nam, $thang, (int)date('t', mktime(0, 0, 0, $thang, 1, $nam)));
        $sql = "SELECT ns.MaNV, ns.HoTen, ns.MaDV, ns.MaCV, ns.MaNgach, ns.HeSoLuongHienTai, ns.BacLuongHienTai,
                       ns.SoNguoiPhuThuoc, ns.LuongBaoHiem, ns.TrangThai, ns.TrinhDoHocVan, ns.NgayVaoLam, ns.DoanVien,
                       cv.TenCV, cv.PhuCapCV, dv.TenDV,
                       cc.MaCC, cc.SoNgayCongChuan, cc.NgayCongThucTe, cc.NghiPhep, cc.NghiKhongLuong, cc.NghiLe,
                       cc.LamThemGio, cc.DiTreVeSom, cc.TrangThaiDuyet AS TrangThaiCC
                FROM nhansu ns
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN chamcong cc ON cc.MaNV = ns.MaNV AND cc.Thang = :thang AND cc.Nam = :nam
                WHERE ns.TrangThai IN (1, 2) AND ns.NgayVaoLam <= :cuoiThang";
        $params = ['thang' => $thang, 'nam' => $nam, 'cuoiThang' => $cuoiThang];
        if ($maNV !== null) {
            $sql .= " AND ns.MaNV = :maNV";
            $params['maNV'] = $maNV;
        }
        $sql .= " ORDER BY ns.MaNV ASC";
        return $this->query($sql, $params)->fetchAll();
    }

    /** Tổng thưởng (+) / phạt (-) từ Khen thưởng - Kỷ luật trong tháng, theo nhân viên */
    public function getKtklMap(int $thang, int $nam, ?string $maNV = null): array {
        $sql = "SELECT MaNV, HinhThuc, LyDo, GiaTri, NgayQD
                FROM qt_ktkl
                WHERE MONTH(NgayQD) = :thang AND YEAR(NgayQD) = :nam AND GiaTri <> 0";
        $params = ['thang' => $thang, 'nam' => $nam];
        if ($maNV !== null) {
            $sql .= " AND MaNV = :maNV";
            $params['maNV'] = $maNV;
        }
        $map = [];
        foreach ($this->query($sql, $params)->fetchAll() as $r) {
            $map[$r['MaNV']][] = $r;
        }
        return $map;
    }

    /**
     * Tính lương một nhân viên. Trả về ['record' => dòng bangluong, 'steps' => các bước tính, 'inputs' => đầu vào].
     */
    public function computeEmployee(array $row, array $ktklList, int $thang, int $nam): array {
        $f = fn ($n) => number_format((float)$n, 0, ',', '.');
        $d = fn ($n) => rtrim(rtrim(number_format((float)$n, 2, ',', '.'), '0'), ',');
        $steps = [];

        // --- Đầu vào chấm công ---
        $coChamCong = !empty($row['MaCC']);
        $congChuan = (float)($row['SoNgayCongChuan'] ?? 0) > 0 ? (float)$row['SoNgayCongChuan'] : 22.0;
        $congThucTe = $coChamCong ? (float)$row['NgayCongThucTe'] : $congChuan;
        $nghiPhep = $coChamCong ? (float)$row['NghiPhep'] : 0.0;
        $nghiLe = $coChamCong ? (float)$row['NghiLe'] : 0.0;
        $nghiKhongLuong = $coChamCong ? (float)$row['NghiKhongLuong'] : 0.0;
        $gioTangCa = $coChamCong ? (float)$row['LamThemGio'] : 0.0;
        $soLanTre = $coChamCong ? (int)$row['DiTreVeSom'] : 0;
        $congHuongLuong = min($congChuan, $congThucTe + $nghiPhep + $nghiLe);

        $heSo = (float)($row['HeSoLuongHienTai'] ?? 1.0);
        $soNPT = (int)($row['SoNguoiPhuThuoc'] ?? 0);

        $steps[] = ['group' => 'A. Ngày công', 'label' => 'Công hưởng lương',
            'formula' => "{$d($congThucTe)} công thực tế + {$d($nghiPhep)} phép + {$d($nghiLe)} lễ (tối đa {$d($congChuan)} công chuẩn)",
            'value' => $d($congHuongLuong) . ' công',
            'note' => $coChamCong ? "Nghỉ không lương {$d($nghiKhongLuong)} ngày: không tính công, không trừ thêm tiền."
                                  : 'Chưa có dữ liệu chấm công tháng này: tạm tính đủ công chuẩn.'];

        // --- P1: Lương vị trí theo hệ số ---
        $p1ThangDu = self::LUONG_CO_SO * $heSo;
        $p1 = round($p1ThangDu / $congChuan * $congHuongLuong);
        $steps[] = ['group' => 'B. Thu nhập', 'label' => 'P1 – Lương vị trí (theo hệ số và ngày công)',
            'formula' => "{$f(self::LUONG_CO_SO)} × hệ số {$d($heSo)} ÷ {$d($congChuan)} × {$d($congHuongLuong)}",
            'value' => $p1];

        // --- P2: Lương năng lực ---
        $trinhDo = trim((string)($row['TrinhDoHocVan'] ?? ''));
        $hsTrinhDo = 0.20;
        foreach (self::HE_SO_NANG_LUC as $key => $hs) {
            if ($trinhDo !== '' && mb_stripos($trinhDo, $key) === 0) { $hsTrinhDo = $hs; break; }
        }
        $namThamNien = 0;
        if (!empty($row['NgayVaoLam'])) {
            $namThamNien = (int)(new DateTime($row['NgayVaoLam']))->diff(new DateTime(sprintf('%04d-%02d-01', $nam, $thang)))->y;
        }
        $hsThamNien = min(0.30, 0.02 * $namThamNien);
        $p2 = round(self::LUONG_CO_SO * ($hsTrinhDo + $hsThamNien) / $congChuan * $congHuongLuong);
        $steps[] = ['group' => 'B. Thu nhập', 'label' => 'P2 – Lương năng lực',
            'formula' => "{$f(self::LUONG_CO_SO)} × (trình độ " . ($trinhDo ?: 'khác') . " {$d($hsTrinhDo)} + thâm niên {$namThamNien} năm × 0,02 = {$d($hsThamNien)}) ÷ {$d($congChuan)} × {$d($congHuongLuong)}",
            'value' => $p2, 'note' => 'Hệ số thâm niên tối đa 0,30.'];

        // --- P3: Thưởng KPI / chuyên cần ---
        $tyLeCongP3 = min(1, $congHuongLuong / $congChuan);
        $heSoChuyenCan = max(0, 1 - self::TRU_KPI_MOI_LAN_TRE * $soLanTre);
        $p3 = round($p1ThangDu * self::TY_LE_KPI * $tyLeCongP3 * $heSoChuyenCan);
        $steps[] = ['group' => 'B. Thu nhập', 'label' => 'P3 – Thưởng hiệu suất (KPI)',
            'formula' => "P1 đủ tháng {$f($p1ThangDu)} × 15% × tỷ lệ công {$d($tyLeCongP3 * 100)}% × chuyên cần (1 − {$soLanTre} lần trễ × 5%) = × {$d($heSoChuyenCan * 100)}%",
            'value' => $p3];

        // --- Phụ cấp ---
        $phuCapCV = (float)($row['PhuCapCV'] ?? 0);
        $phuCapKhac = self::AN_TRUA + self::DI_LAI;
        $steps[] = ['group' => 'B. Thu nhập', 'label' => 'Phụ cấp chức vụ' . (!empty($row['TenCV']) ? ' (' . $row['TenCV'] . ')' : ''),
            'formula' => 'Theo danh mục chức vụ', 'value' => $phuCapCV];
        $steps[] = ['group' => 'B. Thu nhập', 'label' => 'Phụ cấp ăn trưa + đi lại',
            'formula' => "{$f(self::AN_TRUA)} + {$f(self::DI_LAI)}", 'value' => $phuCapKhac];

        // --- Tăng ca ---
        $luongGio = $p1ThangDu / ($congChuan * self::GIO_MOT_CONG);
        $tienTangCa = round($luongGio * $gioTangCa * self::HE_SO_TANG_CA);
        $tangCaMienThue = round($luongGio * $gioTangCa * (self::HE_SO_TANG_CA - 1));
        $steps[] = ['group' => 'B. Thu nhập', 'label' => 'Tiền tăng ca (150%)',
            'formula' => "lương giờ {$f($luongGio)} × {$d($gioTangCa)} giờ × 1,5",
            'value' => $tienTangCa, 'note' => "Lương giờ = {$f($p1ThangDu)} ÷ ({$d($congChuan)} × 8)."];

        // --- Khen thưởng / kỷ luật ---
        $thuongKT = 0; $phatKL = 0;
        foreach ($ktklList as $k) {
            $v = (float)$k['GiaTri'];
            if ($v > 0) { $thuongKT += $v; } else { $phatKL += abs($v); }
        }
        if ($thuongKT > 0) {
            $steps[] = ['group' => 'B. Thu nhập', 'label' => 'Khen thưởng trong tháng',
                'formula' => implode('; ', array_map(fn ($k) => $k['HinhThuc'] . ' ' . $f($k['GiaTri']), array_filter($ktklList, fn ($k) => $k['GiaTri'] > 0))),
                'value' => $thuongKT];
        }

        $tongThuNhap = $p1 + $p2 + $p3 + $phuCapCV + $phuCapKhac + $tienTangCa + $thuongKT;
        $steps[] = ['group' => 'B. Thu nhập', 'label' => 'TỔNG THU NHẬP (gross)',
            'formula' => "{$f($p1)} + {$f($p2)} + {$f($p3)} + {$f($phuCapCV)} + {$f($phuCapKhac)} + {$f($tienTangCa)} + {$f($thuongKT)}",
            'value' => $tongThuNhap, 'strong' => true];

        // --- Bảo hiểm ---
        $luongBHGoc = (float)($row['LuongBaoHiem'] ?? 0);
        if ($luongBHGoc <= 0) { $luongBHGoc = $p1ThangDu + $phuCapCV; }
        $khongDongBH = $nghiKhongLuong >= 14;
        $luongBH = $khongDongBH ? 0 : min(self::MAX_LUONG_BHXH, max(self::LUONG_TOI_THIEU_VUNG, $luongBHGoc));
        $bhxh = round($luongBH * 0.08);
        $bhyt = round($luongBH * 0.015);
        $bhtn = round($luongBH * 0.01);
        $tongBH = $bhxh + $bhyt + $bhtn;
        $steps[] = ['group' => 'C. Khấu trừ', 'label' => 'Lương đóng bảo hiểm',
            'formula' => "{$f($luongBHGoc)}, sàn {$f(self::LUONG_TOI_THIEU_VUNG)}, trần {$f(self::MAX_LUONG_BHXH)} (20 × {$f(self::LUONG_CO_SO_NN)})",
            'value' => $luongBH, 'note' => $khongDongBH ? 'Nghỉ không lương từ 14 ngày: tháng này không đóng bảo hiểm.' : null];
        $steps[] = ['group' => 'C. Khấu trừ', 'label' => 'BHXH 8% + BHYT 1,5% + BHTN 1%',
            'formula' => "{$f($bhxh)} + {$f($bhyt)} + {$f($bhtn)}", 'value' => $tongBH];

        // --- Thuế TNCN ---
        $anTruaMienThue = min(self::AN_TRUA, $phuCapKhac);
        $tnChiuThue = max(0, $tongThuNhap - $anTruaMienThue - $tangCaMienThue);
        $giamTru = self::GIAM_TRU_BAN_THAN + $soNPT * self::GIAM_TRU_PHU_THUOC;
        $tnTinhThue = max(0, $tnChiuThue - $tongBH - $giamTru);
        $steps[] = ['group' => 'C. Khấu trừ', 'label' => 'Thu nhập chịu thuế',
            'formula' => "{$f($tongThuNhap)} − ăn trưa {$f($anTruaMienThue)} − phần tăng ca miễn thuế {$f($tangCaMienThue)}",
            'value' => $tnChiuThue];
        $steps[] = ['group' => 'C. Khấu trừ', 'label' => 'Thu nhập tính thuế',
            'formula' => "{$f($tnChiuThue)} − bảo hiểm {$f($tongBH)} − giảm trừ ({$f(self::GIAM_TRU_BAN_THAN)} + {$soNPT} × {$f(self::GIAM_TRU_PHU_THUOC)})",
            'value' => $tnTinhThue];
        [$thue, $bacThue] = $this->calculateProgressiveTax($tnTinhThue);
        $steps[] = ['group' => 'C. Khấu trừ', 'label' => 'Thuế TNCN (lũy tiến 5 bậc)',
            'formula' => $bacThue ? implode(' + ', array_map(fn ($b) => "{$f($b['amount'])} × {$b['rate']}%", $bacThue)) : 'Thu nhập tính thuế = 0',
            'value' => $thue];

        // --- Đoàn phí, kỷ luật ---
        $doanPhi = (!empty($row['DoanVien']) && $luongBH > 0) ? min(round($luongBH * 0.01), round(self::LUONG_CO_SO_NN * 0.10)) : 0;
        if ($doanPhi > 0) {
            $steps[] = ['group' => 'C. Khấu trừ', 'label' => 'Đoàn phí công đoàn 1%',
                'formula' => "1% × {$f($luongBH)}, tối đa {$f(self::LUONG_CO_SO_NN * 0.10)}", 'value' => $doanPhi];
        }
        if ($phatKL > 0) {
            $steps[] = ['group' => 'C. Khấu trừ', 'label' => 'Khấu trừ theo quyết định kỷ luật / bồi thường',
                'formula' => implode('; ', array_map(fn ($k) => $k['HinhThuc'] . ' ' . $f(abs($k['GiaTri'])), array_filter($ktklList, fn ($k) => $k['GiaTri'] < 0))),
                'value' => $phatKL];
        }
        $khauTruKhac = $doanPhi + $phatKL;

        $thucLinh = max(0, $tongThuNhap - $tongBH - $thue - $khauTruKhac);
        $steps[] = ['group' => 'D. Thực lĩnh', 'label' => 'THỰC LĨNH',
            'formula' => "{$f($tongThuNhap)} − {$f($tongBH)} − {$f($thue)} − {$f($khauTruKhac)}",
            'value' => $thucLinh, 'strong' => true];

        $congTyDong = round($luongBH * 0.215) + round(($p1ThangDu + $phuCapCV) * 0.02);

        $ghiChu = $coChamCong ? null : 'Chưa có chấm công - tạm tính đủ công chuẩn';

        return [
            'record' => [
                'MaNV' => $row['MaNV'], 'Thang' => $thang, 'Nam' => $nam,
                'LuongCoBan' => $p1, 'HeSoLuong' => $heSo, 'PhuCapChucVu' => $phuCapCV, 'PhuCapKhac' => $phuCapKhac,
                'LuongNangLuc' => $p2, 'ThuongKPI' => $p3, 'ThuongKhac' => $tienTangCa + $thuongKT,
                'TongThuNhap' => $tongThuNhap, 'KhauTruBHXH' => $tongBH, 'ThueTNCN' => $thue,
                'KhauTruKhac' => $khauTruKhac, 'ThucLinh' => $thucLinh, 'SoNgayCong' => $congHuongLuong,
                'TrangThaiDuyet' => 'DaTinh', 'GhiChu' => $ghiChu,
            ],
            'steps' => $steps,
            'extra' => [
                'coChamCong' => $coChamCong, 'congChuan' => $congChuan, 'congThucTe' => $congThucTe, 'nghiPhep' => $nghiPhep,
                'nghiLe' => $nghiLe, 'nghiKhongLuong' => $nghiKhongLuong, 'gioTangCa' => $gioTangCa, 'soLanTre' => $soLanTre,
                'luongBH' => $luongBH, 'bhxh' => $bhxh, 'bhyt' => $bhyt, 'bhtn' => $bhtn, 'doanPhi' => $doanPhi, 'phatKL' => $phatKL,
                'tienTangCa' => $tienTangCa, 'thuongKT' => $thuongKT, 'giamTru' => $giamTru, 'tnTinhThue' => $tnTinhThue,
                'congTyDong' => $congTyDong, 'luongGio' => $luongGio, 'ktkl' => $ktklList,
            ],
        ];
    }

    /** Thuế lũy tiến 5 bậc. Trả về [tổng thuế, chi tiết từng bậc]. */
    public function calculateProgressiveTax(float $thuNhapTinhThue): array {
        if ($thuNhapTinhThue <= 0) return [0, []];
        $tong = 0; $chiTiet = [];
        foreach (self::BIEU_THUE as [$lo, $hi, $rate]) {
            if ($thuNhapTinhThue <= $lo) break;
            $phan = min($thuNhapTinhThue, $hi) - $lo;
            $thue = round($phan * $rate / 100);
            $tong += $thue;
            $chiTiet[] = ['amount' => $phan, 'rate' => $rate, 'tax' => $thue];
        }
        return [$tong, $chiTiet];
    }

    private function upsertPayrollRecord(array $data): void {
        $sql = "INSERT INTO bangluong (MaNV, Thang, Nam, LuongCoBan, HeSoLuong, PhuCapChucVu, PhuCapKhac, LuongNangLuc, ThuongKPI, ThuongKhac, TongThuNhap, KhauTruBHXH, ThueTNCN, KhauTruKhac, ThucLinh, SoNgayCong, TrangThaiDuyet, GhiChu)
                VALUES (:MaNV, :Thang, :Nam, :LuongCoBan, :HeSoLuong, :PhuCapChucVu, :PhuCapKhac, :LuongNangLuc, :ThuongKPI, :ThuongKhac, :TongThuNhap, :KhauTruBHXH, :ThueTNCN, :KhauTruKhac, :ThucLinh, :SoNgayCong, :TrangThaiDuyet, :GhiChu)
                ON DUPLICATE KEY UPDATE
                    LuongCoBan = VALUES(LuongCoBan), HeSoLuong = VALUES(HeSoLuong), PhuCapChucVu = VALUES(PhuCapChucVu),
                    PhuCapKhac = VALUES(PhuCapKhac), LuongNangLuc = VALUES(LuongNangLuc), ThuongKPI = VALUES(ThuongKPI),
                    ThuongKhac = VALUES(ThuongKhac), TongThuNhap = VALUES(TongThuNhap), KhauTruBHXH = VALUES(KhauTruBHXH),
                    ThueTNCN = VALUES(ThueTNCN), KhauTruKhac = VALUES(KhauTruKhac), ThucLinh = VALUES(ThucLinh),
                    SoNgayCong = VALUES(SoNgayCong), TrangThaiDuyet = VALUES(TrangThaiDuyet), GhiChu = VALUES(GhiChu)";
        $this->query($sql, $data);
    }

    // ------------------------------------------------------------------
    // TRUY VẤN
    // ------------------------------------------------------------------

    /** Bảng lương tháng của tất cả nhân viên (lọc đơn vị, từ khóa) */
    public function getMonthlyPayroll(int $thang, int $nam, ?string $maDV = null, string $keyword = ''): array {
        $sql = "SELECT bl.*, ns.HoTen, ns.MaDV, dv.TenDV, cv.TenCV
                FROM bangluong bl
                JOIN nhansu ns ON bl.MaNV = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                WHERE bl.Thang = :thang AND bl.Nam = :nam";
        $params = ['thang' => $thang, 'nam' => $nam];
        if (!empty($maDV)) {
            $sql .= " AND (ns.MaDV = :maDV1 OR dv.MaDV_Cha = :maDV2)";
            $params['maDV1'] = $maDV;
            $params['maDV2'] = $maDV;
        }
        if ($keyword !== '') {
            $sql .= " AND (ns.MaNV LIKE :kw1 OR ns.HoTen LIKE :kw2)";
            $params['kw1'] = '%' . $keyword . '%';
            $params['kw2'] = '%' . $keyword . '%';
        }
        $sql .= " ORDER BY dv.TenDV ASC, bl.MaNV ASC";
        return $this->query($sql, $params)->fetchAll();
    }

    /** Số nhân viên cần có bảng lương trong tháng */
    public function countPayableEmployees(int $thang, int $nam): int {
        $cuoiThang = sprintf('%04d-%02d-%02d', $nam, $thang, (int)date('t', mktime(0, 0, 0, $thang, 1, $nam)));
        return (int)$this->query("SELECT COUNT(*) AS c FROM nhansu WHERE TrangThai IN (1, 2) AND NgayVaoLam <= :d", ['d' => $cuoiThang])->fetch()['c'];
    }

    /** Trạng thái bảng lương tháng: null nếu chưa tính */
    public function getPeriodStatus(int $thang, int $nam): ?string {
        $r = $this->query("SELECT MAX(TrangThaiDuyet = 'KTDuyet' OR TrangThaiDuyet = 'DaChi') AS locked, COUNT(*) AS c FROM bangluong WHERE Thang = :thang AND Nam = :nam",
            ['thang' => $thang, 'nam' => $nam])->fetch();
        if (!$r || (int)$r['c'] === 0) return null;
        return (int)$r['locked'] === 1 ? 'locked' : 'open';
    }

    public function lockPayroll(int $thang, int $nam, string $nguoiDuyet): bool {
        $sql = "UPDATE bangluong
                SET TrangThaiDuyet = 'KTDuyet', NguoiDuyet = :nd, NgayDuyet = NOW()
                WHERE Thang = :thang AND Nam = :nam";
        return $this->query($sql, ['nd' => mb_substr($nguoiDuyet, 0, 10), 'thang' => $thang, 'nam' => $nam])->rowCount() > 0;
    }

    /** Một dòng bảng lương kèm hồ sơ (dùng cho phiếu lương và trang chi tiết) */
    public function getPayslip(string $maNV, int $thang, int $nam): ?array {
        $sql = "SELECT bl.*, ns.HoTen, ns.SoCMND_CCCD, ns.SoTaiKhoanNH, ns.NganHang, ns.SoNguoiPhuThuoc, ns.NgayVaoLam,
                       ns.TrinhDoHocVan, ns.HeSoLuongHienTai, ns.BacLuongHienTai, ns.LuongBaoHiem,
                       dv.TenDV, cv.TenCV, ng.TenNgach
                FROM bangluong bl
                JOIN nhansu ns ON bl.MaNV = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                LEFT JOIN dm_ngach ng ON ns.MaNgach = ng.MaNgach
                WHERE bl.MaNV = :maNV AND bl.Thang = :thang AND bl.Nam = :nam LIMIT 1";
        $res = $this->query($sql, ['maNV' => $maNV, 'thang' => $thang, 'nam' => $nam])->fetch();
        return $res ?: null;
    }

    /** Hồ sơ nhân viên phục vụ trang chi tiết / lịch sử */
    public function getEmployeeInfo(string $maNV): ?array {
        $sql = "SELECT ns.MaNV, ns.HoTen, ns.NgayVaoLam, ns.TrinhDoHocVan, ns.HeSoLuongHienTai, ns.BacLuongHienTai,
                       ns.LuongBaoHiem, ns.SoNguoiPhuThuoc, ns.SoTaiKhoanNH, ns.NganHang, ns.TrangThai, ns.LoaiHopDong,
                       dv.TenDV, cv.TenCV, cv.PhuCapCV, ng.TenNgach
                FROM nhansu ns
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                LEFT JOIN dm_ngach ng ON ns.MaNgach = ng.MaNgach
                WHERE ns.MaNV = :maNV LIMIT 1";
        $res = $this->query($sql, ['maNV' => $maNV])->fetch();
        return $res ?: null;
    }

    /** Các kỳ lương đã có của một nhân viên, mới nhất trước */
    public function getPayrollHistory(string $maNV): array {
        $sql = "SELECT * FROM bangluong WHERE MaNV = :maNV ORDER BY Nam DESC, Thang DESC";
        return $this->query($sql, ['maNV' => $maNV])->fetchAll();
    }

    /** Diễn biến hệ số / bậc lương (quyết định nâng lương) */
    public function getSalaryGradeHistory(string $maNV): array {
        $sql = "SELECT ID, TuNgay, HeSoLuong, BacLuong, VuotKhung, NgayHuong, SoQD, GhiChu
                FROM qt_luong WHERE MaNV = :maNV ORDER BY TuNgay DESC";
        return $this->query($sql, ['maNV' => $maNV])->fetchAll();
    }

    /** Gộp các kỳ lương theo quý: [năm => [quý => tổng hợp]], mới nhất trước */
    public function summarizeByQuarter(array $periods): array {
        $out = [];
        foreach ($periods as $p) {
            $y = (int)$p['Nam']; $q = intdiv((int)$p['Thang'] - 1, 3) + 1;
            $r = &$out[$y][$q];
            $r ??= ['nam' => $y, 'quy' => $q, 'soKy' => 0, 'thang' => [], 'cong' => 0, 'p123' => 0, 'phuCap' => 0, 'thuongKhac' => 0,
                    'gross' => 0, 'bh' => 0, 'thue' => 0, 'khac' => 0, 'net' => 0];
            $r['soKy']++;
            $r['thang'][] = (int)$p['Thang'];
            $r['cong'] += (float)$p['SoNgayCong'];
            $r['p123'] += (float)$p['LuongCoBan'] + (float)$p['LuongNangLuc'] + (float)$p['ThuongKPI'];
            $r['phuCap'] += (float)$p['PhuCapChucVu'] + (float)$p['PhuCapKhac'];
            $r['thuongKhac'] += (float)$p['ThuongKhac'];
            $r['gross'] += (float)$p['TongThuNhap'];
            $r['bh'] += (float)$p['KhauTruBHXH'];
            $r['thue'] += (float)$p['ThueTNCN'];
            $r['khac'] += (float)$p['KhauTruKhac'];
            $r['net'] += (float)$p['ThucLinh'];
            unset($r);
        }
        krsort($out);
        foreach ($out as &$qs) { krsort($qs); foreach ($qs as &$r) { sort($r['thang']); } unset($r); }
        unset($qs);
        return $out;
    }

    /** Tìm nhân viên theo mã / họ tên / đơn vị (cho ô tìm kiếm) */
    public function searchEmployees(string $keyword, int $limit = 15): array {
        $kw = '%' . $keyword . '%';
        $sql = "SELECT ns.MaNV, ns.HoTen, ns.TrangThai, dv.TenDV, cv.TenCV
                FROM nhansu ns
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                WHERE ns.MaNV LIKE :k1 OR ns.HoTen LIKE :k2 OR dv.TenDV LIKE :k3 OR cv.TenCV LIKE :k4
                ORDER BY (ns.MaNV = :exact) DESC, ns.HoTen ASC
                LIMIT " . max(1, min(50, $limit));
        return $this->query($sql, ['k1' => $kw, 'k2' => $kw, 'k3' => $kw, 'k4' => $kw, 'exact' => $keyword])->fetchAll();
    }
}
