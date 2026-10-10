<?php
/**
 * =====================================================================
 * VINAMILK HRM - Training Model (Quản lý Đào tạo & Phát triển)
 * =====================================================================
 * 
 * Model quản lý Danh mục khóa học (Tetra Pak, HACCP/ISO 22000, Green Farm),
 * đăng ký học viên, ghi nhận kết quả thi cử và lưu vào quá trình đào tạo `qt_daotao`.
 */

require_once APP_DIR . '/core/Model.php';

class TrainingModel extends Model {
    protected string $table = 'qt_daotao';
    protected string $primaryKey = 'ID';

    public function __construct() {
        parent::__construct();
        $this->ensureTablesExist();
    }

    private function ensureTablesExist(): void {
        $sqlCourse = "CREATE TABLE IF NOT EXISTS `dm_khoa_dao_tao` (
            `MaKhoa`        VARCHAR(30) PRIMARY KEY,
            `TenKhoaHoc`    NVARCHAR(300) NOT NULL,
            `LinhVuc`       NVARCHAR(100) NOT NULL, -- TetraPak, Safety/HACCP, Farm, Leadership, Digital
            `CoSoDaoTao`    NVARCHAR(200) NULL,
            `ThoiLuong`     NVARCHAR(50) NULL,
            `ChiPhi`        DECIMAL(15,0) DEFAULT 0,
            `MoTa`          TEXT NULL,
            `TrangThai`     TINYINT UNSIGNED DEFAULT 1,
            `CreatedAt`     DATETIME DEFAULT CURRENT_TIMESTAMP
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Danh mục các Khóa Đào tạo Vinamilk';";

        $this->db->exec($sqlCourse);
        $this->seedDefaultCourses();
    }

    private function seedDefaultCourses(): void {
        $check = $this->query("SELECT COUNT(*) FROM dm_khoa_dao_tao")->fetchColumn();
        if ($check == 0) {
            $sqlInsert = "INSERT INTO dm_khoa_dao_tao (MaKhoa, TenKhoaHoc, LinhVuc, CoSoDaoTao, ThoiLuong, ChiPhi, MoTa) VALUES
            ('KDT-TP01', N'Vận hành Dây chuyền Tiệt trùng Tetra Pak A3/Flex', N'TetraPak', N'Viện Kỹ thuật Tetra Pak Thụy Điển', N'40 Giờ', 15000000, N'Đào tạo kỹ thuật vận hành máy đóng gói tiệt trùng công nghệ cao.'),
            ('KDT-ISO01', N'An toàn Vệ sinh Thực phẩm & Hệ thống ISO 22000 / HACCP', N'Safety/HACCP', N'Trung tâm Quản lý Chất lượng Vinamilk', N'24 Giờ', 5000000, N'Khóa đào tạo tiêu chuẩn an toàn thực phẩm quốc tế cho công nhân nhà máy.'),
            ('KDT-GF01', N'Kỹ thuật Chăm sóc & Dinh dưỡng Đàn bò Green Farm', N'Farm', N'Trung tâm Nghiên cứu Giống bò sữa Vinamilk', N'36 Giờ', 8000000, N'Khóa đào tạo kỹ thuật chăn nuôi bò sữa công nghệ cao nhập khẩu.'),
            ('KDT-DX01', N'Chuyển đổi số & Ứng dụng AI trong Quản trị Chuỗi cung ứng', N'Digital', N'Tập đoàn Vinamilk IT Academy', N'30 Giờ', 10000000, N'Khóa học tối ưu hóa vận hành nhà máy và trang trại thông minh bằng AI.');";

            $this->db->exec($sqlInsert);
        }
    }

    /**
     * Lấy danh sách các Khóa Đào tạo
     */
    public function getCourses(): array {
        $sql = "SELECT kdt.*, COUNT(qtdt.ID) as TotalStudents
                FROM dm_khoa_dao_tao kdt
                LEFT JOIN qt_daotao qtdt ON kdt.TenKhoaHoc = qtdt.TenKhoaHoc
                GROUP BY kdt.MaKhoa
                ORDER BY kdt.CreatedAt DESC";

        return $this->query($sql)->fetchAll();
    }

    /**
     * Lấy danh sách Học viên và Quá trình Đào tạo
     */
    public function getTrainingRecords(?string $maNV = null): array {
        $where = [];
        $params = [];

        if ($maNV) {
            $where[] = "dt.MaNV = :maNV";
            $params['maNV'] = $maNV;
        }

        $whereSql = !empty($where) ? "WHERE " . implode(" AND ", $where) : "";

        $sql = "SELECT dt.*, ns.HoTen, dv.TenDV, cv.TenCV
                FROM qt_daotao dt
                JOIN nhansu ns ON dt.MaNV = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                {$whereSql}
                ORDER BY dt.CreatedAt DESC";

        return $this->query($sql, $params)->fetchAll();
    }

    /**
     * Ghi nhận Học viên tham gia & Cấp chứng chỉ Đào tạo
     */
    public function enrollAndComplete(array $data): bool {
        $sql = "INSERT INTO qt_daotao (MaNV, TenKhoaHoc, CoSoDaoTao, HinhThuc, TuNgay, DenNgay, BangCap_ChungChi, XepLoai, GhiChu)
                VALUES (:MaNV, :TenKhoaHoc, :CoSoDaoTao, :HinhThuc, :TuNgay, :DenNgay, :BangCap_ChungChi, :XepLoai, :GhiChu)";

        return $this->query($sql, [
            'MaNV' => $data['MaNV'],
            'TenKhoaHoc' => $data['TenKhoaHoc'],
            'CoSoDaoTao' => $data['CoSoDaoTao'] ?? 'Tập đoàn Vinamilk',
            'HinhThuc' => $data['HinhThuc'] ?? 'ChinhQuy',
            'TuNgay' => $data['TuNgay'] ?? date('Y-m-d'),
            'DenNgay' => $data['DenNgay'] ?? date('Y-m-d'),
            'BangCap_ChungChi' => $data['BangCap_ChungChi'] ?? 'Chứng chỉ Hoàn thành Khóa học Vinamilk',
            'XepLoai' => $data['XepLoai'] ?? 'Giỏi',
            'GhiChu' => $data['GhiChu'] ?? ''
        ])->rowCount() > 0;
    }

    /**
     * Tạo Khóa đào tạo mới
     */
    public function createCourse(array $data): bool {
        $sql = "INSERT INTO dm_khoa_dao_tao (MaKhoa, TenKhoaHoc, LinhVuc, CoSoDaoTao, ThoiLuong, ChiPhi, MoTa)
                VALUES (:MaKhoa, :TenKhoaHoc, :LinhVuc, :CoSoDaoTao, :ThoiLuong, :ChiPhi, :MoTa)";

        return $this->query($sql, $data)->rowCount() > 0;
    }
}
