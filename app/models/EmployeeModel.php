<?php
/**
 * =====================================================================
 * VINAMILK HRM - Employee Model (Hồ sơ Nhân sự & Vòng đời)
 * =====================================================================
 * 
 * Model quản lý thông tin Sơ yếu lý lịch số hóa, tìm kiếm phân trang,
 * lọc đa tiêu chí, thời gian thử việc, nghỉ hưu & thôi việc.
 */

require_once APP_DIR . '/core/Model.php';

class EmployeeModel extends Model {
    protected string $table = 'nhansu';
    protected string $primaryKey = 'MaNV';

    /**
     * Lấy danh sách Nhân sự có phân trang và bộ lọc nâng cao
     */
    public function getFilteredEmployees(array $filters = [], int $page = 1, int $perPage = 15): array {
        $where = [];
        $params = [];

        // Lọc theo từ khóa (Mã NV, Họ tên, CMND, Email, SĐT)
        if (!empty($filters['keyword'])) {
            $where[] = "(ns.MaNV LIKE :kw OR ns.HoTen LIKE :kw OR ns.SoCMND_CCCD LIKE :kw OR ns.Email LIKE :kw OR ns.SoDienThoai LIKE :kw)";
            $params['kw'] = '%' . trim($filters['keyword']) . '%';
        }

        // Lọc theo Đơn vị / Phòng ban / Nhà máy / Trang trại
        if (!empty($filters['ma_dv'])) {
            $where[] = "(ns.MaDV = :ma_dv OR dv.MaDV_Cha = :ma_dv)";
            $params['ma_dv'] = $filters['ma_dv'];
        }

        // Lọc theo Chức vụ
        if (!empty($filters['ma_cv'])) {
            $where[] = "ns.MaCV = :ma_cv";
            $params['ma_cv'] = $filters['ma_cv'];
        }

        // Lọc theo Ngạch lương
        if (!empty($filters['ma_ngach'])) {
            $where[] = "ns.MaNgach = :ma_ngach";
            $params['ma_ngach'] = $filters['ma_ngach'];
        }

        // Lọc theo Trạng thái (1=Đang làm, 2=Thử việc, 3=Nghỉ việc, 4=Nghỉ hưu)
        if (!empty($filters['trang_thai'])) {
            $where[] = "ns.TrangThai = :trang_thai";
            $params['trang_thai'] = (int)$filters['trang_thai'];
        }

        // Lọc theo Giới tính
        if (!empty($filters['gioi_tinh'])) {
            $where[] = "ns.GioiTinh = :gioi_tinh";
            $params['gioi_tinh'] = $filters['gioi_tinh'];
        }

        $whereSql = !empty($where) ? "WHERE " . implode(" AND ", $where) : "";

        // Đếm tổng số bản ghi
        $countSql = "SELECT COUNT(*) as total 
                     FROM nhansu ns 
                     LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV 
                     {$whereSql}";
        
        $totalRows = (int) $this->query($countSql, $params)->fetch()['total'];

        // Tính offset phân trang
        $offset = ($page - 1) * $perPage;
        $totalPages = ceil($totalRows / $perPage) ?: 1;

        // Truy vấn lấy dữ liệu chi tiết
        $sql = "SELECT ns.*, 
                       dv.TenDV, dv.TenVietTat as TenVTDV, dv.LoaiDV,
                       cv.TenCV, 
                       ng.TenNgach, ng.Nhom as NhomNgach
                FROM nhansu ns
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                LEFT JOIN dm_ngach ng ON ns.MaNgach = ng.MaNgach
                {$whereSql}
                ORDER BY ns.CreatedAt DESC, ns.MaNV DESC
                LIMIT {$perPage} OFFSET {$offset}";

        $data = $this->query($sql, $params)->fetchAll();

        return [
            'data' => $data,
            'total' => $totalRows,
            'page' => $page,
            'perPage' => $perPage,
            'totalPages' => $totalPages
        ];
    }

    /**
     * Lấy thông tin chi tiết đầy đủ 360 độ của 1 Nhân viên theo MaNV
     */
    public function getEmployeeByMaNV(string $maNV): ?array {
        $sql = "SELECT ns.*, 
                       dv.TenDV, dv.LoaiDV, dv.DiaChi as DiaChiDV, dv_cha.TenDV as TenDVCha,
                       cv.TenCV, cv.HeSoCV, cv.PhuCapCV,
                       ng.TenNgach, ng.Nhom as NhomNgach,
                       u.Username, u.RoleID, u.IsActive as UserIsActive, r.RoleName
                FROM nhansu ns
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_donvi dv_cha ON dv.MaDV_Cha = dv_cha.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                LEFT JOIN dm_ngach ng ON ns.MaNgach = ng.MaNgach
                LEFT JOIN users u ON ns.MaNV = u.MaNV
                LEFT JOIN roles r ON u.RoleID = r.RoleID
                WHERE ns.MaNV = :maNV LIMIT 1";

        $emp = $this->query($sql, ['maNV' => $maNV])->fetch();
        if (!$emp) return null;

        // Lấy danh sách quá trình công tác
        $sqlQTCT = "SELECT * FROM qt_congtac WHERE MaNV = :maNV ORDER BY TuNgay DESC";
        $emp['QuatTrinhCongTac'] = $this->query($sqlQTCT, ['maNV' => $maNV])->fetchAll();

        // Lấy danh sách quá trình thuyên chuyển
        $sqlQTTC = "SELECT qttc.*, dv_cu.TenDV as TenDonViCu, dv_moi.TenDV as TenDonViMoi
                    FROM qt_thuyenchuyen qttc
                    LEFT JOIN dm_donvi dv_cu ON qttc.DonViCu = dv_cu.MaDV
                    LEFT JOIN dm_donvi dv_moi ON qttc.DonViMoi = dv_moi.MaDV
                    WHERE qttc.MaNV = :maNV ORDER BY qttc.NgayHieuLuc DESC";
        $emp['QuatTrinhThuyenChuyen'] = $this->query($sqlQTTC, ['maNV' => $maNV])->fetchAll();

        // Lấy danh sách quá trình khen thưởng kỷ luật
        $sqlKTKL = "SELECT * FROM qt_ktkl WHERE MaNV = :maNV ORDER BY NgayQD DESC";
        $emp['KhenThuongKyLuat'] = $this->query($sqlKTKL, ['maNV' => $maNV])->fetchAll();

        return $emp;
    }

    /**
     * Tự động tạo Mã NV mới theo chuẩn Vinamilk (VD: VNM-0056)
     */
    public function generateNextMaNV(): string {
        $sql = "SELECT MaNV FROM nhansu WHERE MaNV LIKE 'VNM-%' ORDER BY CAST(SUBSTRING(MaNV, 5) AS UNSIGNED) DESC LIMIT 1";
        $lastMa = $this->query($sql)->fetchColumn();

        if ($lastMa) {
            $num = (int) substr($lastMa, 4) + 1;
        } else {
            $num = 1;
        }

        return 'VNM-' . str_pad($num, 4, '0', STR_PAD_LEFT);
    }

    /**
     * Danh sách nhân viên đang trong giai đoạn Thử việc (Trạng thái = 2)
     */
    public function getProbationEmployees(): array {
        $sql = "SELECT ns.*, dv.TenDV, cv.TenCV
                FROM nhansu ns
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                WHERE ns.TrangThai = 2
                ORDER BY ns.NgayVaoLam ASC";
        return $this->query($sql)->fetchAll();
    }

    /**
     * Duyệt kết quả thử việc -> Đổi trạng thái thành Chính thức (1) và xếp ngạch bậc
     */
    public function approveProbation(string $maNV, string $maNgach, float $heSo, int $bac): bool {
        $sql = "UPDATE nhansu 
                SET TrangThai = 1, 
                    MaNgach = :maNgach, 
                    HeSoLuongHienTai = :heSo, 
                    BacLuongHienTai = :bac,
                    LoaiHopDong = N'Hợp đồng không xác định thời hạn'
                WHERE MaNV = :maNV";

        return $this->query($sql, [
            'maNV' => $maNV,
            'maNgach' => $maNgach,
            'heSo' => $heSo,
            'bac' => $bac
        ])->rowCount() > 0;
    }

    /**
     * Danh sách Nhân sự sắp đến tuổi nghỉ hưu theo quy định Luật Lao động
     * Nam >= 60 tuổi, Nữ >= 55 tuổi
     */
    public function getRetirementCandidates(): array {
        $sql = "SELECT ns.*, 
                       dv.TenDV, cv.TenCV,
                       TIMESTAMPDIFF(YEAR, ns.NgaySinh, CURDATE()) as Tuoi
                FROM nhansu ns
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                WHERE ns.TrangThai = 1 AND (
                    (ns.GioiTinh = 'Nam' AND TIMESTAMPDIFF(YEAR, ns.NgaySinh, CURDATE()) >= 58) OR
                    (ns.GioiTinh = 'Nữ' AND TIMESTAMPDIFF(YEAR, ns.NgaySinh, CURDATE()) >= 53)
                )
                ORDER BY Tuoi DESC";
        return $this->query($sql)->fetchAll();
    }

    /**
     * Xử lý cho Nghỉ việc / Nghỉ hưu
     */
    public function terminateEmployment(string $maNV, int $trangThaiMoi, string $ghiChu): bool {
        $sql = "UPDATE nhansu SET TrangThai = :trangThai, GhiChu = :ghiChu WHERE MaNV = :maNV";
        return $this->query($sql, [
            'maNV' => $maNV,
            'trangThai' => $trangThaiMoi, // 3=Nghỉ việc, 4=Nghỉ hưu
            'ghiChu' => $ghiChu
        ])->rowCount() > 0;
    }

    /**
     * Lấy mảng dữ liệu danh mục Dropdown (Đơn vị, Chức vụ, Ngạch lương)
     */
    public function getDropdownOptions(): array {
        $donvi = $this->query("SELECT MaDV, TenDV, LoaiDV FROM dm_donvi WHERE TrangThai = 1 ORDER BY ThuTu ASC, MaDV ASC")->fetchAll();
        $chucvu = $this->query("SELECT MaCV, TenCV FROM dm_chucvu WHERE TrangThai = 1 ORDER BY CapBac ASC")->fetchAll();
        $ngach = $this->query("SELECT MaNgach, TenNgach, HeSoKhoiDiem FROM dm_ngach WHERE TrangThai = 1 ORDER BY MaNgach ASC")->fetchAll();

        return [
            'donvi' => $donvi,
            'chucvu' => $chucvu,
            'ngach' => $ngach
        ];
    }
}
