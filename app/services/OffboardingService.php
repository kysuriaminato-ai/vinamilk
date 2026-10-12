<?php
/**
 * =====================================================================
 * VINAMILK HRM - DỊCH VỤ QUY TRÌNH THÔI VIỆC (OffboardingService)
 * =====================================================================
 * Nghiệp vụ quy chuẩn:
 *   1. Checklist 6 bước tuần tự & chặt chẽ:
 *      - Bước 1: Bàn giao công việc
 *      - Bước 2: Thu hồi tài sản / PPE
 *      - Bước 3: Khóa tài khoản người dùng hệ thống
 *      - Bước 4: Chốt công & phép tồn
 *      - Bước 5: Quyết toán lương
 *      - Bước 6: Chốt sổ BHXH & trao quyết định thôi việc
 *   2. TÀI SẢN CHƯA HOÀN TRẢ TỰ ĐỘNG CHUYỂN SANG KHẤU TRỪ LƯƠNG QUYẾT TOÁN
 *   3. KHẢO SÁT LÝ DO NGHỈ VIỆC (Exit Interview Survey) CUNG CẤP DỮ LIỆU ĐÀO TẠO MÔ HÌNH AI
 */

require_once __DIR__ . '/../core/Database.php';
require_once __DIR__ . '/AssetPpeService.php';

class OffboardingService
{
    private PDO $db;
    private AssetPpeService $assetService;

    public function __construct(?PDO $db = null)
    {
        $this->db = $db ?? Database::getInstance()->getConnection();
        $this->assetService = new AssetPpeService($this->db);
    }

    /**
     * KHỞI TẠO HỒ SƠ THÔI VIỆC MỚI
     */
    public function createOffboarding(array $data): int
    {
        $maNV        = trim($data['ma_nv'] ?? '');
        $ngayDeXuat  = $data['ngay_de_xuat'] ?? date('Y-m-d');
        $ngayCuoi    = $data['ngay_lam_viec_cuoi'] ?? date('Y-m-d', strtotime('+30 days'));
        $lyDo        = trim($data['ly_do_nghi'] ?? 'Nghỉ việc theo nguyện vọng cá nhân');
        $nguoiLap    = $data['nguoi_lap_ho_so'] ?? (Auth::username() ?? 'HR Admin');

        if (empty($maNV)) {
            throw new InvalidArgumentException("Mã nhân viên không được để trống.");
        }

        // Kiểm tra xem nhân viên đã có hồ sơ đang xử lý chưa
        $stmtCheck = $this->db->prepare("SELECT id FROM offboarding_process WHERE ma_nv = :manv AND trang_thai IN ('KHOI_TAO', 'DANG_XU_LY') LIMIT 1");
        $stmtCheck->execute([':manv' => $maNV]);
        $existing = $stmtCheck->fetchColumn();

        if ($existing) {
            return (int)$existing;
        }

        $maHoSo = 'OFF-' . date('Y') . '-' . strtoupper(substr(uniqid(), -5));

        $sql = "INSERT INTO offboarding_process 
                (ma_nv, ma_ho_so, ngay_de_xuat, ngay_lam_viec_cuoi, ly_do_nghi, trang_thai, nguoi_lap_ho_so, created_at)
                VALUES 
                (:manv, :mahoso, :ngaydx, :ngaycuoi, :lydo, 'DANG_XU_LY', :nguoilap, NOW())";

        $stmt = $this->db->prepare($sql);
        $stmt->execute([
            ':manv'     => $maNV,
            ':mahoso'   => $maHoSo,
            ':ngaydx'   => $ngayDeXuat,
            ':ngaycuoi' => $ngayCuoi,
            ':lydo'     => $lyDo,
            ':nguoilap' => $nguoiLap
        ]);

        return (int)$this->db->lastInsertId();
    }

    /**
     * LẤY HỒ SƠ OFFBOARDING THEO ID HOẶC THEO MÃ NHÂN VIÊN
     */
    public function getOffboarding(int|string $idOrMaNV): ?array
    {
        if (is_numeric($idOrMaNV)) {
            $sql = "SELECT op.*, ns.HoTen, ns.MaDV, dv.TenDV, cv.TenCV, ns.NgayVaoLam, ns.Email, ns.SoDienThoai
                    FROM offboarding_process op
                    JOIN nhansu ns ON op.ma_nv = ns.MaNV
                    LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                    LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                    WHERE op.id = :val LIMIT 1";
        } else {
            $sql = "SELECT op.*, ns.HoTen, ns.MaDV, dv.TenDV, cv.TenCV, ns.NgayVaoLam, ns.Email, ns.SoDienThoai
                    FROM offboarding_process op
                    JOIN nhansu ns ON op.ma_nv = ns.MaNV
                    LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                    LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                    WHERE op.ma_nv = :val 
                    ORDER BY op.id DESC LIMIT 1";
        }

        $stmt = $this->db->prepare($sql);
        $stmt->execute([':val' => $idOrMaNV]);
        $row = $stmt->fetch(PDO::FETCH_ASSOC);

        return $row ?: null;
    }

    /**
     * LẤY DANH SÁCH OFFBOARDING TOÀN BỘ CÔNG TY
     */
    public function getAllOffboarding(array $filters = []): array
    {
        $sql = "SELECT op.*, ns.HoTen, ns.MaDV, dv.TenDV, cv.TenCV,
                       (op.buoc_1_ban_giao_cong_viec + op.buoc_2_thu_hoi_tai_san + op.buoc_3_khoa_tai_khoan + 
                        op.buoc_4_chot_cong + op.buoc_5_quyet_toan_luong + op.buoc_6_chot_bhxh) AS so_buoc_hoan_thanh
                FROM offboarding_process op
                JOIN nhansu ns ON op.ma_nv = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                WHERE 1=1";
        $params = [];

        if (!empty($filters['trang_thai'])) {
            $sql .= " AND op.trang_thai = :st";
            $params[':st'] = $filters['trang_thai'];
        }

        if (!empty($filters['ma_dv']) && $filters['ma_dv'] !== 'ALL') {
            $sql .= " AND ns.MaDV = :madv";
            $params[':madv'] = $filters['ma_dv'];
        }

        if (!empty($filters['keyword'])) {
            $sql .= " AND (ns.HoTen LIKE :kw OR op.ma_nv LIKE :kw OR op.ma_ho_so LIKE :kw)";
            $params[':kw'] = '%' . $filters['keyword'] . '%';
        }

        $sql .= " ORDER BY op.id DESC";

        $stmt = $this->db->prepare($sql);
        $stmt->execute($params);
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    /**
     * BƯỚC 1: BÀN GIAO CÔNG VIỆC
     */
    public function completeStep1Handover(int $offboardingId, string $confirmer, ?string $content = null): bool
    {
        $sql = "UPDATE offboarding_process 
                SET buoc_1_ban_giao_cong_viec = 1,
                    nguoi_xac_nhan_bg = :confirmer,
                    ngay_xac_nhan_bg = NOW(),
                    noi_dung_ban_giao = :content
                WHERE id = :id";

        $stmt = $this->db->prepare($sql);
        $res = $stmt->execute([
            ':confirmer' => $confirmer,
            ':content'   => $content ?? 'Đã bàn giao đầy đủ tài liệu công việc, mật khẩu chuyên môn và dự án phụ trách.',
            ':id'        => $offboardingId
        ]);

        $this->checkAndFinalize($offboardingId);
        return $res;
    }

    /**
     * BƯỚC 2: THU HỒI TÀI SẢN & TỰ ĐỘNG CHUYỂN TÀI SẢN CHƯA TRẢ SANG KHẤU TRỪ LƯƠNG
     */
    public function completeStep2AssetRecovery(int $offboardingId, string $confirmer, ?string $note = null): array
    {
        $off = $this->getOffboarding($offboardingId);
        if (!$off) {
            throw new InvalidArgumentException("Không tìm thấy hồ sơ thôi việc #{$offboardingId}");
        }

        $maNV = $off['ma_nv'];

        // 1. Quét tài sản đã trả vs chưa trả trong cap_phat_tai_san
        $sqlAssets = "SELECT id, ten_tai_san, loai_tai_san, trang_thai, gia_tri_tai_san, gia_tri_khau_tru, ngay_thu_hoi_thuc_te 
                      FROM cap_phat_tai_san 
                      WHERE ma_nv = :manv";
        $stmtA = $this->db->prepare($sqlAssets);
        $stmtA->execute([':manv' => $maNV]);
        $assets = $stmtA->fetchAll(PDO::FETCH_ASSOC);

        $returnedCount = 0;
        $unreturnedCount = 0;
        $totalPenalty = 0.0;
        $unreturnedDetails = [];

        foreach ($assets as $a) {
            if ($a['trang_thai'] === 'DA_THU_HOI' && !empty($a['ngay_thu_hoi_thuc_te'])) {
                $returnedCount++;
            } else {
                // Tài sản chưa trả hoặc đã báo mất/hỏng
                $unreturnedCount++;
                $penalty = (float)$a['gia_tri_khau_tru'];
                if ($penalty <= 0) {
                    $penalty = (float)$a['gia_tri_tai_san'];
                    if ($penalty <= 0) {
                        $penalty = match ($a['loai_tai_san']) {
                            'MAY_TINH_LAPTOP' => 18500000.0,
                            'THE_TU_RA_VAO'   => 150000.0,
                            'DIEN_THOAI_SIM'  => 3500000.0,
                            'BHLD_PPE'        => 450000.0,
                            default           => 500000.0
                        };
                    }
                }
                $totalPenalty += $penalty;
                $unreturnedDetails[] = $a['ten_tai_san'] . " (" . number_format($penalty, 0, ',', '.') . "đ)";
            }
        }

        $autoNote = "Đã thu hồi $returnedCount tài sản. Nợ $unreturnedCount tài sản chưa hoàn trả. " .
                    "Tổng tiền khấu trừ chuyển sang lương: " . number_format($totalPenalty, 0, ',', '.') . " VNĐ.";
        if (!empty($unreturnedDetails)) {
            $autoNote .= " Danh mục nợ: " . implode(', ', $unreturnedDetails);
        }
        if (!empty($note)) {
            $autoNote .= " | " . $note;
        }

        $sql = "UPDATE offboarding_process 
                SET buoc_2_thu_hoi_tai_san = 1,
                    nguoi_xac_nhan_ts = :confirmer,
                    ngay_xac_nhan_ts = NOW(),
                    so_tai_san_da_tra = :returned,
                    so_tai_san_chua_tra = :unreturned,
                    tong_tien_khau_tru_ts = :penalty,
                    ghi_chu_tai_san = :note
                WHERE id = :id";

        $stmt = $this->db->prepare($sql);
        $stmt->execute([
            ':confirmer'  => $confirmer,
            ':returned'   => $returnedCount,
            ':unreturned' => $unreturnedCount,
            ':penalty'    => $totalPenalty,
            ':note'       => $autoNote,
            ':id'         => $offboardingId
        ]);

        $this->checkAndFinalize($offboardingId);

        return [
            'returned_count'   => $returnedCount,
            'unreturned_count' => $unreturnedCount,
            'total_penalty'    => $totalPenalty,
            'note'             => $autoNote
        ];
    }

    /**
     * BƯỚC 3: KHÓA TÀI KHOẢN NGƯỜI DÙNG TỰ ĐỘNG
     */
    public function completeStep3AccountLockout(int $offboardingId, string $confirmer, ?string $note = null): bool
    {
        $off = $this->getOffboarding($offboardingId);
        if (!$off) {
            throw new InvalidArgumentException("Không tìm thấy hồ sơ thôi việc #{$offboardingId}");
        }

        $maNV = $off['ma_nv'];

        // Khóa tài khoản trong bảng users
        $stmtUser = $this->db->prepare("SELECT Username FROM users WHERE MaNV = :manv LIMIT 1");
        $stmtUser->execute([':manv' => $maNV]);
        $username = $stmtUser->fetchColumn() ?: ('user_' . $maNV);

        $stmtLock = $this->db->prepare("UPDATE users 
                                        SET IsActive = 0, LockoutUntil = '2099-12-31 23:59:59' 
                                        WHERE MaNV = :manv");
        $stmtLock->execute([':manv' => $maNV]);

        $lockNote = "Đã vô hiệu hóa tài khoản [$username], thu hồi quyền truy cập hệ thống và email.";
        if (!empty($note)) {
            $lockNote .= " | " . $note;
        }

        $sql = "UPDATE offboarding_process 
                SET buoc_3_khoa_tai_khoan = 1,
                    nguoi_xac_nhan_tk = :confirmer,
                    ngay_xac_nhan_tk = NOW(),
                    username_khoa = :username,
                    ghi_chu_khoa_tk = :note
                WHERE id = :id";

        $stmt = $this->db->prepare($sql);
        $res = $stmt->execute([
            ':confirmer' => $confirmer,
            ':username'  => $username,
            ':note'      => $lockNote,
            ':id'        => $offboardingId
        ]);

        $this->checkAndFinalize($offboardingId);
        return $res;
    }

    /**
     * BƯỚC 4: CHỐT CÔNG & PHÉP TỒN
     */
    public function completeStep4TimesheetClose(int $offboardingId, string $confirmer, float $workDays, float $remainingLeave, ?string $note = null): bool
    {
        $closeNote = "Đã chốt công thực tế: $workDays ngày. Số ngày phép năm còn tồn: $remainingLeave ngày.";
        if (!empty($note)) {
            $closeNote .= " | " . $note;
        }

        $sql = "UPDATE offboarding_process 
                SET buoc_4_chot_cong = 1,
                    nguoi_xac_nhan_cong = :confirmer,
                    ngay_xac_nhan_cong = NOW(),
                    so_cong_chot = :days,
                    so_phep_con_lai = :leave,
                    ghi_chu_chot_cong = :note
                WHERE id = :id";

        $stmt = $this->db->prepare($sql);
        $res = $stmt->execute([
            ':confirmer' => $confirmer,
            ':days'      => $workDays,
            ':leave'     => $remainingLeave,
            ':note'      => $closeNote,
            ':id'        => $offboardingId
        ]);

        $this->checkAndFinalize($offboardingId);
        return $res;
    }

    /**
     * BƯỚC 5: QUYẾT TOÁN LƯƠNG & TỰ ĐỘNG KHẤU TRỪ TIỀN TÀI SẢN
     */
    public function completeStep5FinalPayroll(int $offboardingId, string $confirmer, float $settlementAmount, ?string $note = null): bool
    {
        $off = $this->getOffboarding($offboardingId);
        if (!$off) {
            throw new InvalidArgumentException("Không tìm thấy hồ sơ thôi việc #{$offboardingId}");
        }

        $assetDeduction = (float)($off['tong_tien_khau_tru_ts'] ?? 0);
        $netSettlement = max(0, $settlementAmount - $assetDeduction);

        $payNote = "Quyết toán lương tổng cộng: " . number_format($settlementAmount, 0, ',', '.') . " VNĐ. " .
                   "Đã khấu trừ tài sản chưa hoàn trả: " . number_format($assetDeduction, 0, ',', '.') . " VNĐ. " .
                   "Thực lĩnh chuyển khoản: " . number_format($netSettlement, 0, ',', '.') . " VNĐ.";
        if (!empty($note)) {
            $payNote .= " | " . $note;
        }

        $sql = "UPDATE offboarding_process 
                SET buoc_5_quyet_toan_luong = 1,
                    nguoi_xac_nhan_luong = :confirmer,
                    ngay_xac_nhan_luong = NOW(),
                    thuc_linh_quyet_toan = :net,
                    khau_tru_tai_san_da_tru = :deduct,
                    ghi_chu_luong = :note
                WHERE id = :id";

        $stmt = $this->db->prepare($sql);
        $res = $stmt->execute([
            ':confirmer' => $confirmer,
            ':net'       => $netSettlement,
            ':deduct'    => $assetDeduction,
            ':note'      => $payNote,
            ':id'        => $offboardingId
        ]);

        $this->checkAndFinalize($offboardingId);
        return $res;
    }

    /**
     * BƯỚC 6: CHỐT SỔ BHXH & HOÀN TẤT NGHỈ VIỆC
     */
    public function completeStep6SocialInsurance(int $offboardingId, string $confirmer, string $soBhxh, ?string $returnDate = null, ?string $note = null): bool
    {
        $bhxhNote = "Đã hoàn tất thủ tục chốt sổ BHXH số [$soBhxh] tại cơ quan BHXH và bàn giao cho người lao động.";
        if (!empty($note)) {
            $bhxhNote .= " | " . $note;
        }

        $sql = "UPDATE offboarding_process 
                SET buoc_6_chot_bhxh = 1,
                    nguoi_xac_nhan_bhxh = :confirmer,
                    ngay_xac_nhan_bhxh = NOW(),
                    so_so_bhxh = :sobhxh,
                    ngay_tra_so_bhxh = :returndate,
                    ghi_chu_bhxh = :note
                WHERE id = :id";

        $stmt = $this->db->prepare($sql);
        $res = $stmt->execute([
            ':confirmer'  => $confirmer,
            ':sobhxh'     => $soBhxh,
            ':returndate' => $returnDate ?? date('Y-m-d'),
            ':note'       => $bhxhNote,
            ':id'         => $offboardingId
        ]);

        $this->checkAndFinalize($offboardingId);
        return $res;
    }

    /**
     * KIỂM TRA ĐỦ 6 BƯỚC -> TỰ ĐỘNG CHUYỂN TRẠNG THÁI HOÀN TẤT & ĐỔI TRẠNG THÁI NHÂN SỰ SANG NGHỈ VIỆC
     */
    public function checkAndFinalize(int $offboardingId): bool
    {
        $off = $this->getOffboarding($offboardingId);
        if (!$off) {
            return false;
        }

        $allDone = ($off['buoc_1_ban_giao_cong_viec'] == 1 &&
                    $off['buoc_2_thu_hoi_tai_san'] == 1 &&
                    $off['buoc_3_khoa_tai_khoan'] == 1 &&
                    $off['buoc_4_chot_cong'] == 1 &&
                    $off['buoc_5_quyet_toan_luong'] == 1 &&
                    $off['buoc_6_chot_bhxh'] == 1);

        if ($allDone && $off['trang_thai'] !== 'HOAN_TAT') {
            // Cập nhật hoàn tất hồ sơ offboarding
            $this->db->prepare("UPDATE offboarding_process 
                                SET trang_thai = 'HOAN_TAT', ngay_hoan_tat = NOW() 
                                WHERE id = :id")
                     ->execute([':id' => $offboardingId]);

            // Cập nhật bảng nhansu -> TrangThai = 3 (Nghỉ việc)
            $this->db->prepare("UPDATE nhansu 
                                SET TrangThai = 3, GhiChu = CONCAT(COALESCE(GhiChu, ''), ' | Nghỉ việc theo hồ sơ ', :mahoso) 
                                WHERE MaNV = :manv")
                     ->execute([':mahoso' => $off['ma_ho_so'], ':manv' => $off['ma_nv']]);

            return true;
        }

        return false;
    }

    /**
     * LƯU KHẢO SÁT LÝ DO NGHỈ VIỆC (EXIT INTERVIEW SURVEY - NUÔI DỮ LIỆU CHO AI)
     */
    public function saveExitSurvey(array $data): int
    {
        $maNV       = trim($data['ma_nv'] ?? '');
        $offId      = !empty($data['offboarding_id']) ? (int)$data['offboarding_id'] : null;
        $ngayKS     = $data['ngay_khao_sat'] ?? date('Y-m-d');
        $luong      = max(1, min(5, (int)($data['hai_long_luong_thuong'] ?? 3)));
        $quanLy     = max(1, min(5, (int)($data['hai_long_quan_ly'] ?? 3)));
        $thangTien  = max(1, min(5, (int)($data['hai_long_co_hoi_thang_tien'] ?? 3)));
        $apLucOT    = max(1, min(5, (int)($data['ap_luc_cong_viec_ot'] ?? 3)));
        $moiTruong  = max(1, min(5, (int)($data['hai_long_moi_truong_dong_nghiep'] ?? 3)));
        $phucLoi    = max(1, min(5, (int)($data['hai_long_chinh_sach_phuc_loi'] ?? 3)));
        $lyDoChinh  = $data['ly_do_chinh'] ?? 'CO_HOI_VIEC_LAM_TOT_HON';
        $ctyMoi     = $data['cong_ty_moi_loai_hinh'] ?? '';
        $tangLuong  = !empty($data['muc_luong_moi_tang_phan_tram']) ? (float)$data['muc_luong_moi_tang_phan_tram'] : null;
        $gopY       = $data['dieu_gi_vnm_nen_cai_thien'] ?? '';
        $quayLai    = !empty($data['san_sang_quay_lai_vnm']) ? 1 : 0;

        if (empty($maNV)) {
            throw new InvalidArgumentException("Mã nhân viên khảo sát không được để trống.");
        }

        // Xóa khảo sát cũ nếu có
        $this->db->prepare("DELETE FROM exit_interview_survey WHERE ma_nv = :manv")->execute([':manv' => $maNV]);

        $sql = "INSERT INTO exit_interview_survey 
                (offboarding_id, ma_nv, ngay_khao_sat, hai_long_luong_thuong, hai_long_quan_ly, hai_long_co_hoi_thang_tien, 
                 ap_luc_cong_viec_ot, hai_long_moi_truong_dong_nghiep, hai_long_chinh_sach_phuc_loi, ly_do_chinh, 
                 cong_ty_moi_loai_hinh, muc_luong_moi_tang_phan_tram, dieu_gi_vnm_nen_cai_thien, san_sang_quay_lai_vnm, created_at)
                VALUES 
                (:offid, :manv, :ngayks, :luong, :quanly, :thangtien, :apluc, :moitruong, :phucloi, :lydo, :ctymoi, :tangluong, :gopy, :quaylai, NOW())";

        $stmt = $this->db->prepare($sql);
        $stmt->execute([
            ':offid'     => $offId,
            ':manv'      => $maNV,
            ':ngayks'    => $ngayKS,
            ':luong'     => $luong,
            ':quanly'    => $quanLy,
            ':thangtien' => $thangTien,
            ':apluc'     => $apLucOT,
            ':moitruong' => $moiTruong,
            ':phucloi'   => $phucLoi,
            ':lydo'      => $lyDoChinh,
            ':ctymoi'    => $ctyMoi,
            ':tangluong' => $tangLuong,
            ':gopy'      => $gopY,
            ':quaylai'   => $quayLai
        ]);

        return (int)$this->db->lastInsertId();
    }

    /**
     * LẤY BÁO CÁO PHÂN TÍCH KHẢO SÁT THÔI VIỆC (DỮ LIỆU ĐẦU VÀO CHO AI ANALYTICS)
     */
    public function getExitSurveyAnalytics(): array
    {
        // 1. Phân bố lý do chính nghỉ việc
        $sqlReasons = "SELECT ly_do_chinh, COUNT(*) as so_luong,
                              ROUND(COUNT(*) * 100.0 / (SELECT COUNT(*) FROM exit_interview_survey), 1) as ty_le_phan_tram
                       FROM exit_interview_survey
                       GROUP BY ly_do_chinh
                       ORDER BY so_luong DESC";
        $reasons = $this->db->query($sqlReasons)->fetchAll(PDO::FETCH_ASSOC);

        // 2. Điểm trung bình các tiêu chí hài lòng
        $sqlScores = "SELECT 
                        ROUND(AVG(hai_long_luong_thuong), 2) as avg_luong,
                        ROUND(AVG(hai_long_quan_ly), 2) as avg_quan_ly,
                        ROUND(AVG(hai_long_co_hoi_thang_tien), 2) as avg_thang_tien,
                        ROUND(AVG(ap_luc_cong_viec_ot), 2) as avg_ap_luc_ot,
                        ROUND(AVG(hai_long_moi_truong_dong_nghiep), 2) as avg_moi_truong,
                        ROUND(AVG(hai_long_chinh_sach_phuc_loi), 2) as avg_phuc_loi,
                        COUNT(*) as tong_khao_sat,
                        SUM(CASE WHEN san_sang_quay_lai_vnm = 1 THEN 1 ELSE 0 END) as so_nguoi_san_sang_quay_lai
                      FROM exit_interview_survey";
        $scores = $this->db->query($sqlScores)->fetch(PDO::FETCH_ASSOC);

        return [
            'reasons' => $reasons,
            'scores'  => $scores
        ];
    }
}
