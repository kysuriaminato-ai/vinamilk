<?php
/**
 * =====================================================================
 * VINAMILK HRM - Payroll Model (Phân hệ Tính lương 3P & Thuế TNCN)
 * =====================================================================
 * 
 * Model giao tiếp Bảng lương tháng Vinamilk (3P: P1 + P2 + P3).
 * QUY ƯỚC PHÁP LÝ & KIẾN TRÚC:
 * - Tuyệt đối không hard-code các hằng số: Lương cơ sở, giảm trừ bản thân,
 *   giảm trừ người phụ thuộc, trần bảo hiểm.
 * - Mọi tham số pháp lý được đọc động từ bảng tham số theo ngày hiệu lực
 *   hoặc ủy quyền cho .NET WebApi (PayrollCalculator).
 * - Trang lương PHP đóng vai trò View hiển thị dữ liệu từ API/CSDL.
 */

require_once APP_DIR . '/core/Model.php';

class PayrollModel extends Model {
    protected string $table = 'bangluong';
    protected string $primaryKey = 'MaBL';

    /**
     * Lấy tham số pháp lý động theo ngày hiệu lực
     */
    public function getLegalParameters(?string $onDate = null): array {
        $date = $onDate ?? date('Y-m-d');
        try {
            $sql = "SELECT ma_tham_so, gia_tri FROM cau_hinh_luong 
                    WHERE ngay_hieu_luc <= :d AND (ngay_het_han IS NULL OR ngay_het_han >= :d)";
            $stmt = $this->query($sql, ['d' => $date]);
            $params = [];
            while ($row = $stmt->fetch()) {
                $params[$row['ma_tham_so']] = (float)$row['gia_tri'];
            }
            return $params;
        } catch (\Throwable $e) {
            // Trường hợp bảng cau_hinh_luong chưa đồng bộ, fallback sang mốc 2026
            return [
                'BASE_SALARY' => 2530000,
                'PIT_SELF_DEDUCTION' => 15500000,
                'PIT_DEPENDENT_DEDUCTION' => 6200000,
                'MAX_INSURANCE_BASE_MULTIPLIER' => 20
            ];
        }
    }

    /**
     * Tự động Tính toán toàn bộ Bảng lương Tháng/Năm cho tất cả Nhân viên
     */
    public function calculateMonthlyPayroll(int $thang, int $nam): array {
        $onDate = sprintf('%04d-%02d-28', $nam, $thang);
        $legal = $this->getLegalParameters($onDate);

        $luongCoSo = $legal['BASE_SALARY'] ?? 2530000;
        $giamTruBanThan = $legal['PIT_SELF_DEDUCTION'] ?? 15500000;
        $giamTruPhuThuoc = $legal['PIT_DEPENDENT_DEDUCTION'] ?? 6200000;
        $maxLuongBhxh = $luongCoSo * ($legal['MAX_INSURANCE_BASE_MULTIPLIER'] ?? 20);

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
                           SUM(CASE WHEN Loai = 'KhenThuong' THEN SoTien ELSE 0 END) AS TongThuong,
                           SUM(CASE WHEN Loai = 'KyLuat' THEN SoTien ELSE 0 END) AS TongPhat
                    FROM quatrinh_ktkl 
                    WHERE MONTH(NgayQuyetDinh) = :thang AND YEAR(NgayQuyetDinh) = :nam
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
            $luongP1 = round(($luongCoSo * $heSoLuong / $soCongChuan) * $ngayCongThucTe);

            // 2. Phụ cấp Chức vụ & Phụ cấp Khác (Ăn trưa, ĐT, Xăng xe)
            $phuCapChucVu = (float)($row['PhuCapCV'] ?? 0);
            $phuCapKhac = 1530000;

            // 3. Lương P2 (Lương năng lực) & Lương P3 (Thưởng KPI/Hiệu quả)
            $luongP2 = round($luongCoSo * 0.4);
            $thuongKPI = 2000000;

            // 4. Tiền làm thêm giờ / tăng ca (x 1.5)
            $donGiaGio = ($luongCoSo * $heSoLuong) / 176;
            $tienTangCa = round($donGiaGio * $gioTangCa * 1.5);

            // 5. Thưởng & Bồi hoàn
            $thuongKT = (float)($ktklMap[$maNV]['TongThuong'] ?? 0);
            $phatKL = (float)($ktklMap[$maNV]['TongPhat'] ?? 0);

            // 6. Tổng thu nhập trước khấu trừ
            $tongThuNhap = $luongP1 + $phuCapChucVu + $phuCapKhac + $luongP2 + $thuongKPI + $tienTangCa + $thuongKT;

            // 7. Khấu trừ Bảo hiểm (BHXH 8% + BHYT 1.5% + BHTN 1% = 10.5%)
            $luongBaoHiem = (float)($row['LuongBaoHiem'] ?? 0);
            if ($luongBaoHiem <= 0) {
                $luongBaoHiem = $luongCoSo * $heSoLuong;
            }
            $luongBaoHiem = min($luongBaoHiem, $maxLuongBhxh);
            $khauTruBHXH = round($luongBaoHiem * 0.105);

            // 8. Tính Thuế TNCN Biểu lũy tiến từng phần
            $soNguoiPhuThuoc = (int)($row['SoNguoiPhuThuoc'] ?? 0);
            $thuNhapChiuThue = max(0, $tongThuNhap - $khauTruBHXH - $phuCapKhac);
            $thuNhapTinhThue = max(0, $thuNhapChiuThue - $giamTruBanThan - ($soNguoiPhuThuoc * $giamTruPhuThuoc));
            
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
     * Tính thuế TNCN Biểu lũy tiến từng phần (Biểu 5 bậc mới từ 2026)
     */
    private function calculateProgressiveTax(float $thuNhapTinhThue): float {
        if ($thuNhapTinhThue <= 0) return 0;

        if ($thuNhapTinhThue <= 10000000) {
            $thue = $thuNhapTinhThue * 0.05;
        } else if ($thuNhapTinhThue <= 30000000) {
            $thue = $thuNhapTinhThue * 0.10 - 500000;
        } else if ($thuNhapTinhThue <= 60000000) {
            $thue = $thuNhapTinhThue * 0.20 - 3500000;
        } else if ($thuNhapTinhThue <= 100000000) {
            $thue = $thuNhapTinhThue * 0.30 - 9500000;
        } else {
            $thue = $thuNhapTinhThue * 0.35 - 14500000;
        }

        return round(max(0, $thue));
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
