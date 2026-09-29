<?php
/**
 * =====================================================================
 * VINAMILK HRM - Payroll Model (Phân hệ Tính lương 3P & Thuế TNCN)
 * =====================================================================
 * 
 * Model tính toán tự động Bảng lương tháng Vinamilk (3P: P1 + P2 + P3),
 * trích nộp bảo hiểm (10.5%), Thuế TNCN biểu lũy tiến từng phần,
 * tích hợp thưởng/phạt KTKL, chốt khóa bảng lương và xuất Payslip.
 */

require_once APP_DIR . '/core/Model.php';

class PayrollModel extends Model {
    protected string $table = 'bangluong';
    protected string $primaryKey = 'MaBL';

    const LUONG_CO_SO = 5000000; // Lương cơ sở / tối thiểu vùng Vinamilk (VNĐ)
    const MAX_LUONG_BHXH = 36000000; // Mức trần đóng BHXH (20 lần lương cơ sở)
    const GIAM_TRU_BAN_THAN = 11000000; // Giảm trừ gia cảnh bản thân
    const GIAM_TRU_PHU_THUOC = 4400000; // Giảm trừ người phụ thuộc / người / tháng

    /**
     * Tự động Tính toán toàn bộ Bảng lương Tháng/Năm cho tất cả Nhân viên
     */
    public function calculateMonthlyPayroll(int $thang, int $nam): array {
        // Lấy dữ liệu chấm công từ bảng chamcong
        $sqlCC = "SELECT cc.*, 
                         ns.HoTen, ns.MaDV, ns.MaCV, ns.MaNgach, ns.HeSoLuongHienTai, 
                         ns.SoNguoiPhuThuoc, ns.LuongBaoHiem, ns.TrangThai,
                         cv.PhuCapCV
                  FROM chamcong cc
                  JOIN nhansu ns ON cc.MaNV = ns.MaNV
                  LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                  WHERE cc.Thang = :thang AND cc.Nam = :nam AND ns.TrangThai IN (1, 2)";

        $ccRows = $this->query($sqlCC, ['thang' => $thang, 'nam' => $nam])->fetchAll();

        // Lấy tổng thưởng/phạt từ quá trình KTKL trong tháng
        $sqlKTKL = "SELECT MaNV, 
                           SUM(CASE WHEN GiaTri > 0 THEN GiaTri ELSE 0 END) as TongThuong,
                           SUM(CASE WHEN GiaTri < 0 THEN ABS(GiaTri) ELSE 0 END) as TongPhat
                    FROM qt_ktkl
                    WHERE MONTH(NgayQD) = :thang AND YEAR(NgayQD) = :nam
                    GROUP BY MaNV";
        $ktklRows = $this->query($sqlKTKL, ['thang' => $thang, 'nam' => $nam])->fetchAll();

        $ktklMap = [];
        foreach ($ktklRows as $k) {
            $ktklMap[$k['MaNV']] = $k;
        }

        $payrollData = [];

        foreach ($ccRows as $row) {
            $maNV = $row['MaNV'];
            $heSoLuong = (float)($row['HeSoLuongHienTai'] ?? 1.00);
            $ngayCongThucTe = (float)($row['NgayCongThucTe'] ?? 0.0);
            $soCongChuan = (float)($row['SoNgayCongChuan'] ?? 22.0);
            $gioTangCa = (float)($row['LamThemGio'] ?? 0.0);

            // 1. Tính Lương P1 (Lương theo hệ số & công thực tế)
            $luongP1 = round((self::LUONG_CO_SO * $heSoLuong / $soCongChuan) * $ngayCongThucTe);

            // 2. Phụ cấp Chức vụ & Phụ cấp Khác (Ăn trưa, ĐT, Xăng xe)
            $phuCapChucVu = (float)($row['PhuCapCV'] ?? 0);
            $phuCapKhac = 1530000; // Fixed 730k ăn trưa + 800k phụ cấp đi lại Vinamilk

            // 3. Lương P2 (Lương năng lực) & Lương P3 (Thưởng KPI/Hiệu quả)
            $luongP2 = round(self::LUONG_CO_SO * 0.4); // 40% lương cơ sở
            $thuongKPI = 2000000; // Mặc định thưởng KPI hoàn thành tốt công việc

            // 4. Tiền làm thêm giờ / tăng ca (x 1.5)
            $donGiaGio = (self::LUONG_CO_SO * $heSoLuong) / 176;
            $tienTangCa = round($donGiaGio * $gioTangCa * 1.5);

            // 5. Thưởng & Phạt từ Khen thưởng Kỷ luật
            $thuongKT = (float)($ktklMap[$maNV]['TongThuong'] ?? 0);
            $phatKL = (float)($ktklMap[$maNV]['TongPhat'] ?? 0);

            // 6. Tổng thu nhập trước khấu trừ
            $tongThuNhap = $luongP1 + $phuCapChucVu + $phuCapKhac + $luongP2 + $thuongKPI + $tienTangCa + $thuongKT;

            // 7. Khấu trừ Bảo hiểm (BHXH 8% + BHYT 1.5% + BHTN 1% = 10.5%)
            $luongBaoHiem = (float)($row['LuongBaoHiem'] ?? 0);
            if ($luongBaoHiem <= 0) {
                $luongBaoHiem = self::LUONG_CO_SO * $heSoLuong;
            }
            $luongBaoHiem = min($luongBaoHiem, self::MAX_LUONG_BHXH);
            $khauTruBHXH = round($luongBaoHiem * 0.105);

            // 8. Tính Thuế TNCN Biểu lũy tiến từng phần
            $soNguoiPhuThuoc = (int)($row['SoNguoiPhuThuoc'] ?? 0);
            $thuNhapChiuThue = max(0, $tongThuNhap - $khauTruBHXH - $phuCapKhac); // Phụ cấp ăn trưa không chịu thuế
            $thuNhapTinhThue = max(0, $thuNhapChiuThue - self::GIAM_TRU_BAN_THAN - ($soNguoiPhuThuoc * self::GIAM_TRU_PHU_THUOC));
            
            $thueTNCN = $this->calculateProgressiveTax($thuNhapTinhThue);

            // 9. Thực lĩnh
            $thucLinh = max(0, $tongThuNhap - $khauTruBHXH - $thueTNCN - $phatKL);

            // Lưu hoặc cập nhật vào CSDL
            $this->upsertPayrollRecord([
                'MaNV' => $maNV,
                'Thang' => $thang,
                'Nam' => $nam,
                'LuongCoBan' => $luongP1,
                'HeSoLuong' => $heSoLuong,
                'PhuCapChucVu' => $phuCapChucVu,
                'PhuCapKhac' => $phuCapKhac,
                'LuongNangLuc' => $luongP2,
                'ThuongKPI' => $thuongKPI,
                'ThuongKhac' => $tienTangCa + $thuongKT,
                'TongThuNhap' => $tongThuNhap,
                'KhauTruBHXH' => $khauTruBHXH,
                'ThueTNCN' => $thueTNCN,
                'KhauTruKhac' => $phatKL,
                'ThucLinh' => $thucLinh,
                'SoNgayCong' => $ngayCongThucTe,
                'TrangThaiDuyet' => 'DaTinh'
            ]);

            $payrollData[] = [
                'MaNV' => $maNV,
                'HoTen' => $row['HoTen'],
                'SoNgayCong' => $ngayCongThucTe,
                'LuongCoBan' => $luongP1,
                'PhuCap' => $phuCapChucVu + $phuCapKhac,
                'TongThuNhap' => $tongThuNhap,
                'KhauTruBHXH' => $khauTruBHXH,
                'ThueTNCN' => $thueTNCN,
                'ThucLinh' => $thucLinh,
                'TrangThaiDuyet' => 'DaTinh'
            ];
        }

        return $payrollData;
    }

    /**
     * Tính thuế TNCN Biểu lũy tiến từng phần
     */
    private function calculateProgressiveTax(float $thuNhapTinhThue): float {
        if ($thuNhapTinhThue <= 0) return 0;

        $thue = 0;
        if ($thuNhapTinhThue <= 5000000) {
            $thue = $thuNhapTinhThue * 0.05;
        } else if ($thuNhapTinhThue <= 10000000) {
            $thue = (5000000 * 0.05) + (($thuNhapTinhThue - 5000000) * 0.10);
        } else if ($thuNhapTinhThue <= 18000000) {
            $thue = (5000000 * 0.05) + (5000000 * 0.10) + (($thuNhapTinhThue - 10000000) * 0.15);
        } else if ($thuNhapTinhThue <= 32000000) {
            $thue = (5000000 * 0.05) + (5000000 * 0.10) + (8000000 * 0.15) + (($thuNhapTinhThue - 18000000) * 0.20);
        } else if ($thuNhapTinhThue <= 52000000) {
            $thue = (5000000 * 0.05) + (5000000 * 0.10) + (8000000 * 0.15) + (14000000 * 0.20) + (($thuNhapTinhThue - 32000000) * 0.25);
        } else if ($thuNhapTinhThue <= 80000000) {
            $thue = (5000000 * 0.05) + (5000000 * 0.10) + (8000000 * 0.15) + (14000000 * 0.20) + (20000000 * 0.25) + (($thuNhapTinhThue - 52000000) * 0.30);
        } else {
            $thue = (5000000 * 0.05) + (5000000 * 0.10) + (8000000 * 0.15) + (14000000 * 0.20) + (20000000 * 0.25) + (28000000 * 0.30) + (($thuNhapTinhThue - 80000000) * 0.35);
        }

        return round($thue);
    }

    /**
     * Upsert bản ghi vào bảng bangluong
     */
    private function upsertPayrollRecord(array $data): void {
        $sql = "INSERT INTO bangluong (MaNV, Thang, Nam, LuongCoBan, HeSoLuong, PhuCapChucVu, PhuCapKhac, LuongNangLuc, ThuongKPI, ThuongKhac, TongThuNhap, KhauTruBHXH, ThueTNCN, KhauTruKhac, ThucLinh, SoNgayCong, TrangThaiDuyet)
                VALUES (:MaNV, :Thang, :Nam, :LuongCoBan, :HeSoLuong, :PhuCapChucVu, :PhuCapKhac, :LuongNangLuc, :ThuongKPI, :ThuongKhac, :TongThuNhap, :KhauTruBHXH, :ThueTNCN, :KhauTruKhac, :ThucLinh, :SoNgayCong, :TrangThaiDuyet)
                ON DUPLICATE KEY UPDATE 
                    LuongCoBan = VALUES(LuongCoBan),
                    HeSoLuong = VALUES(HeSoLuong),
                    PhuCapChucVu = VALUES(PhuCapChucVu),
                    PhuCapKhac = VALUES(PhuCapKhac),
                    LuongNangLuc = VALUES(LuongNangLuc),
                    ThuongKPI = VALUES(ThuongKPI),
                    ThuongKhac = VALUES(ThuongKhac),
                    TongThuNhap = VALUES(TongThuNhap),
                    KhauTruBHXH = VALUES(KhauTruBHXH),
                    ThueTNCN = VALUES(ThueTNCN),
                    KhauTruKhac = VALUES(KhauTruKhac),
                    ThucLinh = VALUES(ThucLinh),
                    SoNgayCong = VALUES(SoNgayCong),
                    TrangThaiDuyet = VALUES(TrangThaiDuyet)";

        $this->query($sql, $data);
    }

    /**
     * Lấy Bảng lương tháng tổng hợp
     */
    public function getMonthlyPayroll(int $thang, int $nam, ?string $maDV = null): array {
        $whereDV = !empty($maDV) ? "AND (ns.MaDV = :maDV OR dv.MaDV_Cha = :maDV)" : "";
        $paramsDV = ['thang' => $thang, 'nam' => $nam];
        if (!empty($maDV)) $paramsDV['maDV'] = $maDV;

        $sql = "SELECT bl.*, ns.HoTen, dv.TenDV, cv.TenCV
                FROM bangluong bl
                JOIN nhansu ns ON bl.MaNV = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                WHERE bl.Thang = :thang AND bl.Nam = :nam {$whereDV}
                ORDER BY bl.MaNV ASC";

        return $this->query($sql, $paramsDV)->fetchAll();
    }

    /**
     * Khóa Bảng lương (Lock Payroll) chống sửa đổi
     */
    public function lockPayroll(int $thang, int $nam, string $nguoiDuyet): bool {
        $sql = "UPDATE bangluong 
                SET TrangThaiDuyet = 'KTDuyet', NguoiDuyet = :nd, NgayDuyet = NOW() 
                WHERE Thang = :thang AND Nam = :nam";

        return $this->query($sql, ['nd' => $nguoiDuyet, 'thang' => $thang, 'nam' => $nam])->rowCount() > 0;
    }

    /**
     * Lấy Phiếu lương (Payslip) chi tiết của 1 Nhân viên
     */
    public function getPayslip(string $maNV, int $thang, int $nam): ?array {
        $sql = "SELECT bl.*, ns.HoTen, ns.SoCMND_CCCD, ns.SoTaiKhoanNH, ns.NganHang, ns.SoNguoiPhuThuoc,
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
}
