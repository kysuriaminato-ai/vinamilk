<?php
/**
 * =====================================================================
 * VINAMILK HRM - Recruitment Model (Tuyển dụng & Hồ sơ Ứng viên)
 * =====================================================================
 * 
 * Model quản lý Đề xuất tuyển dụng, Tin tuyển dụng, Hồ sơ ứng viên,
 * Kết quả phỏng vấn và tự động chuyển ứng viên trúng tuyển thành Nhân viên thử việc.
 */

require_once APP_DIR . '/core/Model.php';

class RecruitmentModel extends Model {
    protected string $table = 'tintuyendung';
    protected string $primaryKey = 'MaTT';

    public function __construct() {
        parent::__construct();
        $this->ensureTablesExist();
    }

    private function ensureTablesExist(): void {
        $sqlRequest = "CREATE TABLE IF NOT EXISTS `de_xuat_tuyen_dung` (
            `ID`            INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
            `MaDV`          VARCHAR(20) NOT NULL,
            `ViTriCanTuyen` NVARCHAR(200) NOT NULL,
            `SoLuong`       INT UNSIGNED DEFAULT 1,
            `LyDo`          NVARCHAR(500) NULL,
            `NguoiDeXuat`   VARCHAR(50) NULL,
            `TrangThai`     ENUM('ChoDuyet', 'DaDuyet', 'TuChoi') DEFAULT 'ChoDuyet',
            `NguoiDuyet`    VARCHAR(50) NULL,
            `NgayDuyet`     DATETIME NULL,
            `CreatedAt`     DATETIME DEFAULT CURRENT_TIMESTAMP
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Đề xuất nhu cầu tuyển dụng từ bộ phận';";

        $this->db->exec($sqlRequest);
    }

    /**
     * Lấy tất cả Tin tuyển dụng đính kèm số lượng Hồ sơ ứng viên
     */
    public function getAllJobs(?string $trangThai = null): array {
        $where = [];
        $params = [];

        if (!empty($trangThai)) {
            $where[] = "ttd.TrangThai = :trangThai";
            $params['trangThai'] = $trangThai;
        }

        $whereSql = !empty($where) ? "WHERE " . implode(" AND ", $where) : "";

        $sql = "SELECT ttd.*, dv.TenDV, COUNT(uv.MaUV) as TotalCandidates
                FROM tintuyendung ttd
                LEFT JOIN dm_donvi dv ON ttd.MaDV = dv.MaDV
                LEFT JOIN hoso_ungvien uv ON ttd.MaTT = uv.MaTT
                {$whereSql}
                GROUP BY ttd.MaTT
                ORDER BY ttd.CreatedAt DESC";

        return $this->query($sql, $params)->fetchAll();
    }

    /**
     * Lấy danh sách Hồ sơ ứng viên (có thể lọc theo Tin tuyển dụng hoặc Trạng thái Kanban)
     */
    public function getCandidates(?int $maTT = null, ?string $trangThai = null): array {
        $where = [];
        $params = [];

        if ($maTT) {
            $where[] = "uv.MaTT = :maTT";
            $params['maTT'] = $maTT;
        }

        if ($trangThai) {
            $where[] = "uv.TrangThai = :trangThai";
            $params['trangThai'] = $trangThai;
        }

        $whereSql = !empty($where) ? "WHERE " . implode(" AND ", $where) : "";

        $sql = "SELECT uv.*, ttd.TieuDe as TenTinTuyen, dv.TenDV
                FROM hoso_ungvien uv
                JOIN tintuyendung ttd ON uv.MaTT = ttd.MaTT
                LEFT JOIN dm_donvi dv ON ttd.MaDV = dv.MaDV
                {$whereSql}
                ORDER BY uv.CreatedAt DESC";

        return $this->query($sql, $params)->fetchAll();
    }

    /**
     * Thêm mới Hồ sơ ứng viên
     */
    public function createCandidate(array $data): string|false {
        $sql = "INSERT INTO hoso_ungvien (MaTT, HoTen, NgaySinh, GioiTinh, Email, SoDienThoai, DiaChi, TrinhDo, ChuyenNganh, KinhNghiem, FileCV, NguonHoSo, NgayNop, TrangThai)
                VALUES (:MaTT, :HoTen, :NgaySinh, :GioiTinh, :Email, :SoDienThoai, :DiaChi, :TrinhDo, :ChuyenNganh, :KinhNghiem, :FileCV, :NguonHoSo, NOW(), 'MoiNop')";

        $stmt = $this->query($sql, $data);
        if ($stmt->rowCount() > 0) {
            return Database::getInstance()->lastInsertId();
        }
        return false;
    }

    /**
     * Chuyển ứng viên Trúng tuyển thành Nhân viên thử việc chính thức trong CSDL
     */
    public function convertCandidateToEmployee(int $maUV, array $empDetails): string|false {
        try {
            $this->beginTransaction();

            // 1. Cập nhật trạng thái ứng viên -> TrungTuyen
            $sqlUV = "UPDATE hoso_ungvien SET TrangThai = 'TrungTuyen' WHERE MaUV = :maUV";
            $this->query($sqlUV, ['maUV' => $maUV]);

            // 2. Tự động sinh MaNV mới
            /** @var EmployeeModel $empModel */
            require_once APP_DIR . '/models/EmployeeModel.php';
            $empModel = new EmployeeModel();
            $nextMaNV = $empModel->generateNextMaNV();

            // 3. Thêm mới bản ghi vào bảng nhansu (Trạng thái 2 = Thử việc)
            $empData = [
                'MaNV' => $nextMaNV,
                'HoTen' => $empDetails['HoTen'],
                'NgaySinh' => $empDetails['NgaySinh'] ?? '1995-01-01',
                'GioiTinh' => $empDetails['GioiTinh'] ?? 'Nam',
                'SoCMND_CCCD' => $empDetails['SoCMND_CCCD'] ?? ('0790' . rand(10000000, 99999999)),
                'Email' => $empDetails['Email'] ?? '',
                'SoDienThoai' => $empDetails['SoDienThoai'] ?? '',
                'NgayVaoLam' => date('Y-m-d'),
                'MaDV' => $empDetails['MaDV'] ?? 'VINAMILK',
                'MaCV' => $empDetails['MaCV'] ?? 'NV',
                'MaNgach' => 'NG_C2',
                'HeSoLuongHienTai' => 1.50,
                'BacLuongHienTai' => 1,
                'LoaiHopDong' => 'Thử việc',
                'TrangThai' => 2 // 2 = Thử việc
            ];

            $empModel->insert($empData);

            $this->commit();
            return $nextMaNV;
        } catch (Exception $e) {
            $this->rollBack();
            error_log("Error converting candidate to employee: " . $e->getMessage());
            return false;
        }
    }

    /**
     * Lấy danh sách Phiếu đề xuất tuyển dụng
     */
    public function getRecruitmentRequests(): array {
        $sql = "SELECT dx.*, dv.TenDV
                FROM de_xuat_tuyen_dung dx
                LEFT JOIN dm_donvi dv ON dx.MaDV = dv.MaDV
                ORDER BY dx.CreatedAt DESC";

        return $this->query($sql)->fetchAll();
    }

    /**
     * Tạo Phiếu đề xuất tuyển dụng mới
     */
    public function createRecruitmentRequest(array $data): bool {
        $sql = "INSERT INTO de_xuat_tuyen_dung (MaDV, ViTriCanTuyen, SoLuong, LyDo, NguoiDeXuat, TrangThai)
                VALUES (:MaDV, :ViTriCanTuyen, :SoLuong, :LyDo, :NguoiDeXuat, 'ChoDuyet')";

        return $this->query($sql, $data)->rowCount() > 0;
    }

    /**
     * Duyệt Phiếu đề xuất tuyển dụng
     */
    public function approveRecruitmentRequest(int $id, string $nguoiDuyet, string $trangThai = 'DaDuyet'): bool {
        $sql = "UPDATE de_xuat_tuyen_dung SET TrangThai = :st, NguoiDuyet = :nd, NgayDuyet = NOW() WHERE ID = :id";
        return $this->query($sql, ['st' => $trangThai, 'nd' => $nguoiDuyet, 'id' => $id])->rowCount() > 0;
    }
}
