<?php
/**
 * =====================================================================
 * VINAMILK HRM - Attendance Model (Chấm công & Nghỉ phép)
 * =====================================================================
 * 
 * Model quản lý bảng chấm công ma trận tháng 30/31 ngày, ca làm việc,
 * import dữ liệu máy chấm công và phê duyệt đơn xin nghỉ phép.
 */

require_once APP_DIR . '/core/Model.php';

class AttendanceModel extends Model {
    protected string $table = 'chamcong';
    protected string $primaryKey = 'MaCC';

    public function __construct() {
        parent::__construct();
        $this->ensureTablesExist();
    }

    /**
     * Tự động tạo các bảng chấm công chi tiết và đơn xin nghỉ nếu chưa có
     */
    private function ensureTablesExist(): void {
        // 1. Bảng chấm công chi tiết từng ngày trong tháng (1..31)
        $sqlDetail = "CREATE TABLE IF NOT EXISTS `bang_cham_cong_chitiet` (
            `ID`            INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
            `MaNV`          VARCHAR(10) NOT NULL,
            `Thang`         TINYINT UNSIGNED NOT NULL,
            `Nam`           SMALLINT UNSIGNED NOT NULL,
            `Ngay`          TINYINT UNSIGNED NOT NULL,
            `KyHieu`        VARCHAR(10) NOT NULL DEFAULT 'X', -- X: Đủ công, P: Phép năm, O: Không lương, TC: Tăng ca, TS: Thai sản, H: Học tập
            `MaCa`          VARCHAR(20) DEFAULT 'HC', -- HC: Hành chính, C1: Ca 1, C2: Ca 2, C3: Ca 3
            `GioVao`        TIME NULL,
            `GioRa`         TIME NULL,
            `CreatedAt`     DATETIME DEFAULT CURRENT_TIMESTAMP,
            `UpdatedAt`     DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
            UNIQUE KEY `uk_cc_nv_day` (`MaNV`, `Thang`, `Nam`, `Ngay`),
            KEY `idx_cc_lookup` (`Thang`, `Nam`, `MaNV`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Chi tiết chấm công từng ngày trong tháng';";

        // 2. Bảng Đơn xin nghỉ phép trực tuyến
        $sqlLeave = "CREATE TABLE IF NOT EXISTS `don_xin_nghi` (
            `ID`            INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
            `MaNV`          VARCHAR(10) NOT NULL,
            `TuNgay`        DATE NOT NULL,
            `DenNgay`       DATE NOT NULL,
            `SoNgay`        DECIMAL(4,1) NOT NULL DEFAULT 1.0,
            `LoaiNghi`      VARCHAR(50) NOT NULL DEFAULT 'Phép năm', -- Phép năm, Không lương, Thai sản, Học tập
            `LyDo`          NVARCHAR(500) NULL,
            `TrangThai`     ENUM('ChoDuyet', 'DaDuyet', 'TuChoi') DEFAULT 'ChoDuyet',
            `NguoiDuyet`    VARCHAR(10) NULL,
            `NgayDuyet`     DATETIME NULL,
            `CreatedAt`     DATETIME DEFAULT CURRENT_TIMESTAMP,
            KEY `idx_don_nv` (`MaNV`),
            KEY `idx_don_trangthai` (`TrangThai`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Đơn xin nghỉ phép trực tuyến';";

        $this->db->exec($sqlDetail);
        $this->db->exec($sqlLeave);
    }

    /**
     * Lấy Bảng chấm công Lưới (Grid Calendar 30/31 ngày) của tất cả Nhân sự trong Tháng/Năm
     */
    public function getMonthlyAttendanceGrid(int $thang, int $nam, ?string $maDV = null): array {
        $daysInMonth = cal_days_in_month(CAL_GREGORIAN, $thang, $nam);

        // Lấy danh sách nhân viên đang hoạt động (TrangThai = 1 hoặc 2)
        $whereDV = !empty($maDV) ? "AND (ns.MaDV = :maDV OR dv.MaDV_Cha = :maDV)" : "";
        $paramsDV = !empty($maDV) ? ['maDV' => $maDV] : [];

        $sqlEmps = "SELECT ns.MaNV, ns.HoTen, ns.MaDV, dv.TenDV, cv.TenCV
                    FROM nhansu ns
                    LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                    LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                    WHERE ns.TrangThai IN (1, 2) {$whereDV}
                    ORDER BY ns.MaNV ASC";

        $employees = $this->query($sqlEmps, $paramsDV)->fetchAll();

        // Lấy dữ liệu công chi tiết từng ngày đã lưu
        $sqlGrid = "SELECT MaNV, Ngay, KyHieu, MaCa FROM bang_cham_cong_chitiet WHERE Thang = :thang AND Nam = :nam";
        $gridRows = $this->query($sqlGrid, ['thang' => $thang, 'nam' => $nam])->fetchAll();

        $gridMap = [];
        foreach ($gridRows as $row) {
            $gridMap[$row['MaNV']][$row['Ngay']] = $row['KyHieu'];
        }

        // Lấy mảng tổng hợp từ bảng chamcong
        $sqlSummary = "SELECT * FROM chamcong WHERE Thang = :thang AND Nam = :nam";
        $sumRows = $this->query($sqlSummary, ['thang' => $thang, 'nam' => $nam])->fetchAll();

        $sumMap = [];
        foreach ($sumRows as $s) {
            $sumMap[$s['MaNV']] = $s;
        }

        // Tạo mảng kết quả hoàn chỉnh cho từng nhân viên
        $result = [];
        foreach ($employees as $emp) {
            $maNV = $emp['MaNV'];
            $days = [];
            $totalCong = 0.0;
            $totalPhep = 0.0;
            $totalKhongLuong = 0.0;
            $totalTangCa = 0.0;

            for ($day = 1; $day <= $daysInMonth; $day++) {
                // Kiểm tra ngày trong tuần (Chủ nhật = 0, Thứ 7 = 6)
                $dayOfWeek = date('w', strtotime("{$nam}-{$thang}-{$day}"));

                if (isset($gridMap[$maNV][$day])) {
                    $symbol = $gridMap[$maNV][$day];
                } else {
                    // Mặc định: Chủ nhật nghỉ (O), các ngày còn lại đủ công (X)
                    $symbol = ($dayOfWeek == 0) ? 'O' : 'X';
                }

                $days[$day] = $symbol;

                // Tích lũy công
                if ($symbol === 'X' || $symbol === 'H') $totalCong += 1.0;
                else if ($symbol === 'P' || $symbol === 'TS') { $totalCong += 1.0; $totalPhep += 1.0; }
                else if ($symbol === 'TC') { $totalCong += 1.0; $totalTangCa += 2.0; }
                else if ($symbol === 'O') { $totalKhongLuong += 1.0; }
            }

            // Đồng bộ bảng tổng hợp chamcong nếu chưa có
            if (!isset($sumMap[$maNV])) {
                $this->insertSummary($maNV, $thang, $nam, $totalCong, $totalPhep, $totalKhongLuong, $totalTangCa);
            }

            $result[] = [
                'MaNV' => $maNV,
                'HoTen' => $emp['HoTen'],
                'TenDV' => $emp['TenDV'],
                'TenCV' => $emp['TenCV'],
                'Days' => $days,
                'TotalCong' => $totalCong,
                'TotalPhep' => $totalPhep,
                'TotalKhongLuong' => $totalKhongLuong,
                'TotalTangCa' => $totalTangCa,
                'TrangThaiDuyet' => $sumMap[$maNV]['TrangThaiDuyet'] ?? 'ChoDuyet'
            ];
        }

        return [
            'daysInMonth' => $daysInMonth,
            'thang' => $thang,
            'nam' => $nam,
            'grid' => $result
        ];
    }

    /**
     * Chèn/Cập nhật ký hiệu chấm công của 1 ngày qua Ajax
     */
    public function updateDailySymbol(string $maNV, int $thang, int $nam, int $ngay, string $kyHieu, string $maCa = 'HC'): bool {
        $sql = "INSERT INTO bang_cham_cong_chitiet (MaNV, Thang, Nam, Ngay, KyHieu, MaCa)
                VALUES (:maNV, :thang, :nam, :ngay, :kyHieu, :maCa)
                ON DUPLICATE KEY UPDATE KyHieu = VALUES(KyHieu), MaCa = VALUES(MaCa)";

        $stmt = $this->query($sql, [
            'maNV' => $maNV,
            'thang' => $thang,
            'nam' => $nam,
            'ngay' => $ngay,
            'kyHieu' => $kyHieu,
            'maCa' => $maCa
        ]);

        // Cập nhật lại số liệu tổng hợp trong bảng chamcong
        $this->recalculateSummary($maNV, $thang, $nam);

        return true;
    }

    /**
     * Tính toán lại số liệu tổng hợp tháng từ các ô ngày chi tiết
     */
    private function recalculateSummary(string $maNV, int $thang, int $nam): void {
        $sql = "SELECT KyHieu, COUNT(*) as cnt FROM bang_cham_cong_chitiet 
                WHERE MaNV = :maNV AND Thang = :thang AND Nam = :nam 
                GROUP BY KyHieu";
        $rows = $this->query($sql, ['maNV' => $maNV, 'thang' => $thang, 'nam' => $nam])->fetchAll();

        $totalCong = 0.0;
        $totalPhep = 0.0;
        $totalKhongLuong = 0.0;
        $totalTangCa = 0.0;

        foreach ($rows as $r) {
            $sym = $r['KyHieu'];
            $cnt = (float)$r['cnt'];

            if ($sym === 'X' || $sym === 'H') $totalCong += $cnt;
            else if ($sym === 'P' || $sym === 'TS') { $totalCong += $cnt; $totalPhep += $cnt; }
            else if ($sym === 'TC') { $totalCong += $cnt; $totalTangCa += ($cnt * 2.0); }
            else if ($sym === 'O') { $totalKhongLuong += $cnt; }
        }

        $this->insertSummary($maNV, $thang, $nam, $totalCong, $totalPhep, $totalKhongLuong, $totalTangCa);
    }

    /**
     * Upsert bảng chamcong tổng hợp
     */
    private function insertSummary(string $maNV, int $thang, int $nam, float $cong, float $phep, float $khongLuong, float $tangCa): void {
        $sql = "INSERT INTO chamcong (MaNV, Thang, Nam, SoNgayCongChuan, NgayCongThucTe, NghiPhep, NghiKhongLuong, LamThemGio)
                VALUES (:maNV, :thang, :nam, 22.0, :cong, :phep, :khongLuong, :tangCa)
                ON DUPLICATE KEY UPDATE NgayCongThucTe = VALUES(NgayCongThucTe), 
                                        NghiPhep = VALUES(NghiPhep), 
                                        NghiKhongLuong = VALUES(NghiKhongLuong), 
                                        LamThemGio = VALUES(LamThemGio)";

        $this->query($sql, [
            'maNV' => $maNV,
            'thang' => $thang,
            'nam' => $nam,
            'cong' => $cong,
            'phep' => $phep,
            'khongLuong' => $khongLuong,
            'tangCa' => $tangCa
        ]);
    }

    /**
     * Import dữ liệu chấm công từ file CSV / Excel mảng dữ liệu
     */
    public function importAttendanceData(array $rows, int $thang, int $nam): int {
        $count = 0;
        try {
            $this->beginTransaction();
            foreach ($rows as $row) {
                $maNV = trim($row['MaNV'] ?? '');
                $ngay = (int)($row['Ngay'] ?? 0);
                $kyHieu = trim($row['KyHieu'] ?? 'X');

                if (!empty($maNV) && $ngay >= 1 && $ngay <= 31) {
                    $this->updateDailySymbol($maNV, $thang, $nam, $ngay, $kyHieu);
                    $count++;
                }
            }
            $this->commit();
        } catch (Exception $e) {
            $this->rollBack();
            error_log("Error importing attendance: " . $e->getMessage());
        }
        return $count;
    }

    /**
     * Lấy danh sách Đơn xin nghỉ phép
     */
    public function getLeaveRequests(?string $maNV = null): array {
        $where = [];
        $params = [];

        if (!empty($maNV)) {
            $where[] = "don.MaNV = :maNV";
            $params['maNV'] = $maNV;
        }

        $whereSql = !empty($where) ? "WHERE " . implode(" AND ", $where) : "";

        $sql = "SELECT don.*, ns.HoTen, dv.TenDV, cv.TenCV
                FROM don_xin_nghi don
                JOIN nhansu ns ON don.MaNV = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                {$whereSql}
                ORDER BY don.CreatedAt DESC";

        return $this->query($sql, $params)->fetchAll();
    }

    /**
     * Tạo Đơn xin nghỉ phép mới
     */
    public function createLeaveRequest(array $data): bool {
        $sql = "INSERT INTO don_xin_nghi (MaNV, TuNgay, DenNgay, SoNgay, LoaiNghi, LyDo, TrangThai)
                VALUES (:maNV, :tuNgay, :denNgay, :soNgay, :loaiNghi, :lyDo, 'ChoDuyet')";

        $stmt = $this->query($sql, [
            'maNV' => $data['MaNV'],
            'tuNgay' => $data['TuNgay'],
            'denNgay' => $data['DenNgay'],
            'soNgay' => $data['SoNgay'],
            'loaiNghi' => $data['LoaiNghi'],
            'lyDo' => $data['LyDo'] ?? ''
        ]);

        return $stmt->rowCount() > 0;
    }

    /**
     * Duyệt đơn xin nghỉ phép -> Tự động điền ký hiệu 'P' hoặc 'O' vào bảng chấm công chi tiết!
     */
    public function approveLeaveRequest(int $leaveId, string $nguoiDuyet, string $trangThai = 'DaDuyet'): bool {
        $sqlFind = "SELECT * FROM don_xin_nghi WHERE ID = :id LIMIT 1";
        $leave = $this->query($sqlFind, ['id' => $leaveId])->fetch();

        if (!$leave) return false;

        $sqlUp = "UPDATE don_xin_nghi SET TrangThai = :st, NguoiDuyet = :nd, NgayDuyet = NOW() WHERE ID = :id";
        $this->query($sqlUp, ['st' => $trangThai, 'nd' => $nguoiDuyet, 'id' => $leaveId]);

        // Nếu Đã duyệt -> điền ký hiệu vào các ngày nghỉ tương ứng
        if ($trangThai === 'DaDuyet') {
            $tuNgay = strtotime($leave['TuNgay']);
            $denNgay = strtotime($leave['DenNgay']);

            $symbol = ($leave['LoaiNghi'] === 'Không lương') ? 'O' : (($leave['LoaiNghi'] === 'Thai sản') ? 'TS' : 'P');

            for ($current = $tuNgay; $current <= $denNgay; $current += 86400) {
                $thang = (int)date('m', $current);
                $nam = (int)date('Y', $current);
                $ngay = (int)date('d', $current);

                $this->updateDailySymbol($leave['MaNV'], $thang, $nam, $ngay, $symbol);
            }
        }

        return true;
    }
}
