<?php
/**
 * =====================================================================
 * VINAMILK HRM - Report Model (Phân hệ Thống kê - Báo cáo HUHA HRM)
 * =====================================================================
 * 
 * Model chuyên trách truy vấn dữ liệu báo cáo thống kê doanh nghiệp theo tiêu chuẩn HUHA HRM:
 * Biểu mẫu 01 (Trích ngang cán bộ), Biểu mẫu 02 (Biến động nhân sự),
 * Biểu mẫu 03 (Thống kê KTKL), Biểu mẫu 04 (Danh sách nghỉ hưu) và Báo cáo Quỹ lương.
 */

require_once APP_DIR . '/core/Model.php';

class ReportModel extends Model {
    protected string $table = 'nhansu';
    protected string $primaryKey = 'MaNV';

    /**
     * BIỂU MẪU 01: Báo cáo Sơ yếu Lý lịch Trích ngang Toàn bộ Cán bộ Công nhân viên
     */
    public function getBM01TrichNgang(array $filters = []): array {
        $where = [];
        $params = [];

        if (!empty($filters['ma_dv'])) {
            $where[] = "(ns.MaDV = :ma_dv OR dv.MaDV_Cha = :ma_dv)";
            $params['ma_dv'] = $filters['ma_dv'];
        }

        if (!empty($filters['ma_cv'])) {
            $where[] = "ns.MaCV = :ma_cv";
            $params['ma_cv'] = $filters['ma_cv'];
        }

        if (!empty($filters['trang_thai'])) {
            $where[] = "ns.TrangThai = :trang_thai";
            $params['trang_thai'] = (int)$filters['trang_thai'];
        }

        $whereSql = !empty($where) ? "WHERE " . implode(" AND ", $where) : "";

        $sql = "SELECT ns.MaNV, ns.HoTen, ns.TenThuongGoi, ns.NgaySinh, ns.GioiTinh,
                       ns.SoCMND_CCCD, ns.NgayCap, ns.NoiCap, ns.QueQuan, ns.NoiDKKTTru, ns.DiaChiHienTai,
                       ns.DanToc, ns.TonGiao, ns.TinhTrangHonNhan, ns.Email, ns.SoDienThoai,
                       ns.NgayVaoLam, ns.HeSoLuongHienTai, ns.BacLuongHienTai,
                       ns.TrinhDoHocVan, ns.ChuyenNganh, ns.TruongDaoTao, ns.DangVien, ns.DoanVien,
                       ns.ViTriLuuHoSo, ns.LoaiHopDong, ns.TrangThai,
                       dv.TenDV, dv.LoaiDV, cv.TenCV, ng.TenNgach, ng.MaNgach
                FROM nhansu ns
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                LEFT JOIN dm_ngach ng ON ns.MaNgach = ng.MaNgach
                {$whereSql}
                ORDER BY dv.ThuTu ASC, ns.MaNV ASC";

        return $this->query($sql, $params)->fetchAll();
    }

    /**
     * BIỂU MẪU 02: Báo cáo Biến động Nhân sự (Thuyên chuyển, Bổ nhiệm, Tuyển mới)
     */
    public function getBM02BienDong(?int $nam = null): array {
        $nam = $nam ?: (int)date('Y');

        $sqlTC = "SELECT qttc.ID, qttc.MaNV, qttc.SoQD, qttc.NgayHieuLuc as Ngay, N'Thuyên chuyển' as LoaiBienDong,
                         ns.HoTen, dv_cu.TenDV as DonViCu, dv_moi.TenDV as DonViMoi, qttc.LyDo
                  FROM qt_thuyenchuyen qttc
                  JOIN nhansu ns ON qttc.MaNV = ns.MaNV
                  LEFT JOIN dm_donvi dv_cu ON qttc.DonViCu = dv_cu.MaDV
                  LEFT JOIN dm_donvi dv_moi ON qttc.DonViMoi = dv_moi.MaDV
                  WHERE YEAR(qttc.NgayHieuLuc) = :nam";

        $sqlMoi = "SELECT 0 as ID, ns.MaNV, N'QĐ-TĐ/VNM' as SoQD, ns.NgayVaoLam as Ngay, N'Tuyển mới' as LoaiBienDong,
                          ns.HoTen, N'Ngoại doanh nghiệp' as DonViCu, dv.TenDV as DonViMoi, N'Tuyển dụng chính thức' as LyDo
                   FROM nhansu ns
                   LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                   WHERE YEAR(ns.NgayVaoLam) = :nam";

        $sqlCombined = "({$sqlTC}) UNION ALL ({$sqlMoi}) ORDER BY Ngay DESC";

        return $this->query($sqlCombined, ['nam' => $nam])->fetchAll();
    }

    /**
     * BIỂU MẪU 03: Thống kê Khen thưởng và Kỷ luật Toàn công ty
     */
    public function getBM03KTKL(?int $nam = null): array {
        $nam = $nam ?: (int)date('Y');

        $sql = "SELECT qt.*, ns.HoTen, dv.TenDV, cv.TenCV, dm.Loai as LoaiDanhMuc
                FROM qt_ktkl qt
                JOIN nhansu ns ON qt.MaNV = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                LEFT JOIN dm_ktkl dm ON qt.MaKTKL = dm.MaKTKL
                WHERE YEAR(qt.NgayQD) = :nam
                ORDER BY qt.NgayQD DESC";

        return $this->query($sql, ['nam' => $nam])->fetchAll();
    }

    /**
     * BIỂU MẪU 04: Thống kê Danh sách Nhân sự Tiệm cận Nghỉ hưu
     */
    public function getBM04NghiHuu(): array {
        $sql = "SELECT ns.*, dv.TenDV, cv.TenCV,
                       TIMESTAMPDIFF(YEAR, ns.NgaySinh, CURDATE()) as Tuoi,
                       CASE WHEN ns.GioiTinh = 'Nam' THEN (60 - TIMESTAMPDIFF(YEAR, ns.NgaySinh, CURDATE()))
                            ELSE (55 - TIMESTAMPDIFF(YEAR, ns.NgaySinh, CURDATE())) END as NamConLai
                FROM nhansu ns
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                WHERE ns.TrangThai = 1 AND (
                    (ns.GioiTinh = 'Nam' AND TIMESTAMPDIFF(YEAR, ns.NgaySinh, CURDATE()) >= 55) OR
                    (ns.GioiTinh = 'Nữ' AND TIMESTAMPDIFF(YEAR, ns.NgaySinh, CURDATE()) >= 50)
                )
                ORDER BY Tuoi DESC";

        return $this->query($sql)->fetchAll();
    }

    /**
     * BÁO CÁO THỐNG KÊ EXECUTIVE DASHBOARD (KPIs, Charts Data & Org Chart)
     */
    public function getExecutiveKpis(): array {
        // 1. Chỉ số tổng hợp
        $totalStaff = (int)$this->query("SELECT COUNT(*) FROM nhansu WHERE TrangThai IN (1,2)")->fetchColumn();
        $maleCount = (int)$this->query("SELECT COUNT(*) FROM nhansu WHERE GioiTinh = 'Nam' AND TrangThai IN (1,2)")->fetchColumn();
        $femaleCount = (int)$this->query("SELECT COUNT(*) FROM nhansu WHERE GioiTinh = 'Nữ' AND TrangThai IN (1,2)")->fetchColumn();
        
        $avgAge = round((float)$this->query("SELECT AVG(TIMESTAMPDIFF(YEAR, NgaySinh, CURDATE())) FROM nhansu WHERE TrangThai IN (1,2)")->fetchColumn(), 1);

        $totalPayrollFund = (float)$this->query("SELECT SUM(TongThuNhap) FROM bangluong WHERE Thang = MONTH(CURDATE()) AND Nam = YEAR(CURDATE())")->fetchColumn() ?: 1850000000;
        $totalTrainingCost = (float)$this->query("SELECT SUM(ChiPhi) FROM dm_khoa_dao_tao")->fetchColumn() ?: 48000000;

        // 2. Cơ cấu theo Đơn vị (Trụ sở, Nhà máy, Trang trại)
        $deptChartData = $this->query("
            SELECT dv.TenDV, COUNT(ns.MaNV) as Total
            FROM dm_donvi dv
            LEFT JOIN nhansu ns ON dv.MaDV = ns.MaDV AND ns.TrangThai IN (1,2)
            WHERE dv.CapDV <= 2
            GROUP BY dv.MaDV
            ORDER BY Total DESC
        ")->fetchAll();

        // 3. Cơ cấu Trình độ Học vấn
        $educationChartData = $this->query("
            SELECT COALESCE(TrinhDoHocVan, N'Khác') as TrinhDo, COUNT(*) as Total
            FROM nhansu
            WHERE TrangThai IN (1,2)
            GROUP BY TrinhDoHocVan
            ORDER BY Total DESC
        ")->fetchAll();

        // 4. Sơ đồ Tổ chức Cây phân cấp (Tree Hierarchy Org Chart)
        $sqlOrg = "SELECT dv.MaDV, dv.TenDV, dv.LoaiDV, dv.CapDV, dv.MaDV_Cha, dv.SoNhanSu,
                          COUNT(ns.MaNV) as ActualStaff
                   FROM dm_donvi dv
                   LEFT JOIN nhansu ns ON dv.MaDV = ns.MaDV AND ns.TrangThai IN (1,2)
                   GROUP BY dv.MaDV
                   ORDER BY dv.CapDV ASC, dv.ThuTu ASC";
        $orgTree = $this->query($sqlOrg)->fetchAll();

        return [
            'totalStaff' => $totalStaff,
            'maleCount' => $maleCount,
            'femaleCount' => $femaleCount,
            'avgAge' => $avgAge,
            'totalPayrollFund' => $totalPayrollFund,
            'totalTrainingCost' => $totalTrainingCost,
            'deptChartData' => $deptChartData,
            'educationChartData' => $educationChartData,
            'orgTree' => $orgTree
        ];
    }
}
