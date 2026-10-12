<?php
/**
 * =====================================================================
 * VINAMILK HRM - MODEL BÁO CÁO NHÂN SỰ & QUỸ LƯƠNG (ReportModel)
 * =====================================================================
 * Tuân thủ nghiêm ngặt:
 *   - Mọi báo cáo số liệu đều lọc qua scopeByUnit()
 *   - HR_DONVI chỉ tổng hợp trong cây đơn vị của mình
 *   - QUANLY_TRUCTIEP chỉ tổng hợp nhân viên cấp dưới trực tiếp
 *   - Che dữ liệu chi phí quỹ lương nếu không có quyền luong.sensitive
 */

require_once __DIR__ . '/../core/Model.php';

class ReportModel extends Model
{
    /**
     * BÁO CÁO QUY MÔ NHÂN SỰ THEO CƠ CẤU ĐƠN VỊ (HEADCOUNT REPORT)
     */
    public function getHeadcountReport(): array
    {
        $scope = $this->scopeByUnit('ns', 'MaDV');
        $sql = "SELECT 
                    dv.MaDV, dv.TenDV, dv.LoaiDV, dv.CapDV,
                    COUNT(ns.MaNV) as TongNhanSu,
                    SUM(CASE WHEN ns.TrangThai = 1 THEN 1 ELSE 0 END) as DangLamViec,
                    SUM(CASE WHEN ns.TrangThai = 0 THEN 1 ELSE 0 END) as DaNghiViec,
                    SUM(CASE WHEN ns.GioiTinh = 'Nam' THEN 1 ELSE 0 END) as SoNam,
                    SUM(CASE WHEN ns.GioiTinh = 'Nữ' THEN 1 ELSE 0 END) as SoNu
                FROM nhansu ns
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                WHERE {$scope['where']}
                GROUP BY dv.MaDV, dv.TenDV, dv.LoaiDV, dv.CapDV
                ORDER BY dv.CapDV ASC, dv.MaDV ASC";

        return $this->fetchAll($sql, $scope['params']);
    }

    /**
     * BÁO CÁO TỔNG QUỸ LƯƠNG VÀ CHI PHÍ (PAYROLL COST REPORT)
     */
    public function getPayrollCostReport(?string $kyLuong = null): array
    {
        $scope = $this->scopeByUnit('bl', 'ma_dv');
        $params = $scope['params'];
        $whereKy = "";

        if ($kyLuong) {
            $whereKy = "AND bl.ky_luong = :ky";
            $params[':ky'] = $kyLuong;
        }

        $sql = "SELECT 
                    bl.ky_luong,
                    bl.ma_dv,
                    dv.TenDV,
                    SUM(bl.tong_nhan_su) as TongSoNhanSu,
                    SUM(bl.tong_thu_nhap) as TongThuNhap,
                    SUM(bl.tong_bao_hiem) as TongBaoHiem,
                    SUM(bl.tong_thue_tncn) as TongThueTNCN,
                    SUM(bl.tong_thuc_linh) as TongThucLinh
                FROM bang_luong bl
                LEFT JOIN dm_donvi dv ON bl.ma_dv = dv.MaDV
                WHERE ({$scope['where']}) {$whereKy}
                GROUP BY bl.ky_luong, bl.ma_dv, dv.TenDV
                ORDER BY bl.ky_luong DESC, bl.ma_dv ASC";

        $rows = $this->fetchAll($sql, $params);

        // Che dữ liệu số tiền nếu không có quyền luong.sensitive
        if (!Auth::canViewSensitive('luong')) {
            foreach ($rows as &$r) {
                $r['TongThuNhap']  = 0;
                $r['TongBaoHiem']  = 0;
                $r['TongThueTNCN'] = 0;
                $r['TongThucLinh'] = 0;
                $r['TongThucLinhMasked'] = '****** ₫';
            }
            unset($r);
        }

        return $rows;
    }

    /**
     * BÁO CÁO TỶ LỆ ĐI LÀM & CHUYÊN CẦN (ATTENDANCE SUMMARY)
     */
    public function getAttendanceRateReport(?string $fromDate = null, ?string $toDate = null): array
    {
        $scope = $this->scopeByUnit('ns', 'MaDV');
        $params = $scope['params'];
        $whereDate = "";

        if ($fromDate && $toDate) {
            $whereDate = "AND cc.ngay BETWEEN :from AND :to";
            $params[':from'] = $fromDate;
            $params[':to']   = $toDate;
        }

        $sql = "SELECT 
                    dv.MaDV, dv.TenDV,
                    COUNT(cc.id) as TongLuotChamCong,
                    SUM(CASE WHEN cc.loai_cong = 'DI_LAM' THEN 1 ELSE 0 END) as SoLuotDiLam,
                    SUM(cc.cong_thuc_te) as TongCongThucTe,
                    SUM(cc.gio_lam_them) as TongGioOT,
                    SUM(cc.di_muon_phut) as TongPhutDiMuon
                FROM cham_cong_ngay cc
                JOIN nhansu ns ON cc.ma_nv = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                WHERE ({$scope['where']}) {$whereDate}
                GROUP BY dv.MaDV, dv.TenDV
                ORDER BY dv.MaDV ASC";

        return $this->fetchAll($sql, $params);
    }
}
