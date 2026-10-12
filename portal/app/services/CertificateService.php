<?php
/**
 * =====================================================================
 * VINAMILK HRM - DỊCH VỤ QUẢN LÝ CHỨNG CHỈ & AN TOÀN (CertificateService)
 * =====================================================================
 * Nghiệp vụ quy chuẩn:
 *   1. Quản lý các chứng chỉ: Thẻ xanh ATTP (khám định kỳ), ATLĐ (nhóm 1-6), Chứng chỉ thiết bị (Tetra Pak, xe nâng, lò hơi...)
 *   2. Cảnh báo tự động trước 60 ngày hết hạn cho nhân sự và ban điều hành
 *   3. CHẶN XẾP CA DÂY CHUYỀN SẢN XUẤT nếu Thẻ xanh ATTP đã hết hạn (Tuân thủ ISO 22000, HACCP & VSATTP Vinamilk)
 */

require_once __DIR__ . '/../core/Database.php';

class CertificateService
{
    private PDO $db;

    public function __construct(?PDO $db = null)
    {
        $this->db = $db ?? Database::getInstance()->getConnection();
    }

    /**
     * TỰ ĐỘNG CẬP NHẬT TRẠNG THÁI CHỨNG CHỈ THEO NGÀY HẾT HẠN
     */
    public function updateCertificateStatuses(): int
    {
        // 1. Đã hết hạn
        $sqlExpired = "UPDATE chung_chi_nhan_vien 
                       SET trang_thai = 'DA_HET_HAN' 
                       WHERE ngay_het_han < CURDATE() AND trang_thai != 'DA_HET_HAN'";
        $count1 = $this->db->exec($sqlExpired);

        // 2. Sắp hết hạn trong 60 ngày
        $sqlWarning = "UPDATE chung_chi_nhan_vien 
                       SET trang_thai = 'SAP_HET_HAN' 
                       WHERE ngay_het_han >= CURDATE() 
                         AND ngay_het_han <= DATE_ADD(CURDATE(), INTERVAL 60 DAY) 
                         AND trang_thai NOT IN ('SAP_HET_HAN', 'DA_HET_HAN')";
        $count2 = $this->db->exec($sqlWarning);

        // 3. Còn hiệu lực (> 60 ngày)
        $sqlValid = "UPDATE chung_chi_nhan_vien 
                     SET trang_thai = 'CON_HIEU_LUC' 
                     WHERE ngay_het_han > DATE_ADD(CURDATE(), INTERVAL 60 DAY) 
                       AND trang_thai != 'CON_HIEU_LUC'";
        $count3 = $this->db->exec($sqlValid);

        return ($count1 + $count2 + $count3);
    }

    /**
     * LẤY DANH SÁCH CHỨNG CHỈ CẢNH BÁO 60 NGÀY & ĐÃ HẾT HẠN
     */
    public function getExpiringCertificates(int $warningDays = 60, ?string $maDV = null): array
    {
        $this->updateCertificateStatuses();

        $params = [':days' => $warningDays];
        $unitCondition = '';

        if (!empty($maDV) && $maDV !== 'ALL') {
            $unitCondition = " AND ns.MaDV = :madv";
            $params[':madv'] = $maDV;
        }

        $sql = "SELECT cc.*, 
                       ns.HoTen, ns.MaDV, dv.TenDV, cv.TenCV,
                       DATEDIFF(cc.ngay_het_han, CURDATE()) AS so_ngay_con_lai
                FROM chung_chi_nhan_vien cc
                JOIN nhansu ns ON cc.ma_nv = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                WHERE cc.ngay_het_han <= DATE_ADD(CURDATE(), INTERVAL :days DAY)
                $unitCondition
                ORDER BY cc.ngay_het_han ASC, cc.id DESC";

        $stmt = $this->db->prepare($sql);
        $stmt->execute($params);
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    /**
     * KIỂM TRA THẺ XANH ATTP CÒN HIỆU LỰC TẠI NGÀY XẾP CA HAY KHÔNG
     */
    public function hasValidFoodSafetyCard(string $maNV, string $date): bool
    {
        $sql = "SELECT id, ngay_het_han, trang_thai 
                FROM chung_chi_nhan_vien
                WHERE ma_nv = :manv 
                  AND (loai_chung_chi = 'THE_XANH_ATTP' OR la_the_xanh_attp = 1)
                  AND ngay_het_han >= :ngay
                  AND trang_thai != 'DA_HET_HAN'
                ORDER BY ngay_het_han DESC 
                LIMIT 1";

        $stmt = $this->db->prepare($sql);
        $stmt->execute([':manv' => $maNV, ':ngay' => $date]);
        $row = $stmt->fetch(PDO::FETCH_ASSOC);

        return !empty($row);
    }

    /**
     * KIỂM TRA XEM CA LÀM VIỆC CÓ PHẢI LÀ CA SẢN XUẤT / DÂY CHUYỀN KHÔNG
     */
    public function isProductionShift(string $shiftCode, ?string $unitCode = null): bool
    {
        // Danh sách ca sản xuất nhà máy chuẩn Vinamilk
        $productionShifts = [
            'CA_1', 'CA_2', 'CA_3', 
            'CA_1_SANG', 'CA_2_CHIEU', 'CA_3_DEM', 
            'CA_GAY_TT'
        ];

        if (in_array(strtoupper($shiftCode), $productionShifts, true)) {
            return true;
        }

        // Nếu ca không phải OFF, không phải CA_HANH_CHINH và thuộc đơn vị Nhà máy sản xuất (NM_*) hoặc Trang trại (TT_*)
        if (!empty($unitCode) && (str_starts_with($unitCode, 'NM_') || str_starts_with($unitCode, 'TT_'))) {
            if (!in_array(strtoupper($shiftCode), ['OFF', 'CA_HANH_CHINH'], true)) {
                return true;
            }
        }

        return false;
    }

    /**
     * QUY TẮC CHẶN XẾP CA DÂY CHUYỀN NẾU THẺ ATTP HẾT HẠN
     * 
     * @param string $maNV Mã nhân viên
     * @param string $shiftCode Mã ca được xếp
     * @param string $workDate Ngày làm việc (YYYY-MM-DD)
     * @param string|null $unitCode Mã đơn vị phân ca
     * @throws DomainException Nếu vi phạm quy tắc an toàn thực phẩm
     * @return bool True nếu hợp lệ
     */
    public function validateShiftAssignment(string $maNV, string $shiftCode, string $workDate, ?string $unitCode = null): bool
    {
        // 1. Nếu không phải ca sản xuất/dây chuyền -> Cho phép xếp bình thường
        if (!$this->isProductionShift($shiftCode, $unitCode)) {
            return true;
        }

        // 2. Nếu là ca dây chuyền/sản xuất: BẮT BUỘC có Thẻ xanh ATTP còn hiệu lực
        $hasValidCard = $this->hasValidFoodSafetyCard($maNV, $workDate);

        if (!$hasValidCard) {
            // Lấy thông tin nhân viên để thông báo lỗi rõ ràng
            $stmt = $this->db->prepare("SELECT HoTen FROM nhansu WHERE MaNV = :manv LIMIT 1");
            $stmt->execute([':manv' => $maNV]);
            $empName = $stmt->fetchColumn() ?: $maNV;

            // Tìm ngày hết hạn gần nhất (nếu có)
            $stmtCard = $this->db->prepare("SELECT ngay_het_han FROM chung_chi_nhan_vien 
                                            WHERE ma_nv = :manv AND (loai_chung_chi = 'THE_XANH_ATTP' OR la_the_xanh_attp = 1)
                                            ORDER BY ngay_het_han DESC LIMIT 1");
            $stmtCard->execute([':manv' => $maNV]);
            $lastExpiry = $stmtCard->fetchColumn();

            $expiryNotice = $lastExpiry 
                ? "đã hết hạn vào ngày " . date('d/m/Y', strtotime($lastExpiry)) 
                : "chưa được cấp hoặc chưa có trong hệ thống";

            throw new DomainException(
                "CHẶN XẾP CA DÂY CHUYỀN: Nhân viên [{$empName} - {$maNV}] có Thẻ xanh An toàn Thực phẩm (ATTP) {$expiryNotice}. " .
                "Theo quy chuẩn ISO 22000, HACCP và Nội quy An toàn Thực phẩm Vinamilk, NGHIÊM CẤM bố trí nhân sự vào ca sản xuất [{$shiftCode}] tại ngày {$workDate} khi thẻ ATTP không còn hiệu lực!"
            );
        }

        return true;
    }

    /**
     * THÊM HOẶC GIA HẠN CHỨNG CHỈ NHÂN VIÊN
     */
    public function saveCertificate(array $data): int
    {
        $maNV       = trim($data['ma_nv'] ?? '');
        $loaiCC     = trim($data['loai_chung_chi'] ?? 'KHAC');
        $tenCC      = trim($data['ten_chung_chi'] ?? '');
        $soHieu     = trim($data['so_hieu_chung_chi'] ?? '');
        $noiCap     = trim($data['noi_cap'] ?? 'Vinamilk');
        $coSoYTe    = trim($data['co_so_y_te'] ?? $noiCap);
        $ngayCap    = $data['ngay_cap'] ?? date('Y-m-d');
        $ngayKham   = $data['ngay_kham_dinh_ky'] ?? $ngayCap;
        $ngayHetHan = $data['ngay_het_han'] ?? date('Y-m-d', strtotime('+1 year', strtotime($ngayCap)));
        $canhBao    = (int)($data['canh_bao_truoc_ngay'] ?? 60);
        $ketQuaKham = $data['ket_qua_kham'] ?? 'Đạt tiêu chuẩn ATTP';
        $ghiChu     = $data['ghi_chu'] ?? '';

        if (empty($maNV) || empty($tenCC)) {
            throw new InvalidArgumentException("Mã nhân viên và Tên chứng chỉ không được để trống.");
        }

        $laTheXanh = ($loaiCC === 'THE_XANH_ATTP' || str_contains(strtoupper($tenCC), 'THẺ XANH') || str_contains(strtoupper($tenCC), 'ATTP')) ? 1 : 0;

        // Tính trạng thái
        $today = date('Y-m-d');
        $warnDate = date('Y-m-d', strtotime("+{$canhBao} days"));
        if ($ngayHetHan < $today) {
            $trangThai = 'DA_HET_HAN';
        } elseif ($ngayHetHan <= $warnDate) {
            $trangThai = 'SAP_HET_HAN';
        } else {
            $trangThai = 'CON_HIEU_LUC';
        }

        if (!empty($data['id'])) {
            $sql = "UPDATE chung_chi_nhan_vien 
                    SET loai_chung_chi = :loai, la_the_xanh_attp = :lathexanh, ten_chung_chi = :ten,
                        so_hieu_chung_chi = :sohieu, noi_cap = :noicap, co_so_y_te = :cosoyte,
                        ngay_cap = :ngaycap, ngay_kham_dinh_ky = :ngaykham, ngay_het_han = :ngayhethan,
                        trang_thai = :trangthai, canh_bao_truoc_ngay = :canhbao, ket_qua_kham = :ketqua,
                        ghi_chu = :ghichu
                    WHERE id = :id";
            $stmt = $this->db->prepare($sql);
            $stmt->execute([
                ':id'         => (int)$data['id'],
                ':loai'       => $loaiCC,
                ':lathexanh'  => $laTheXanh,
                ':ten'        => $tenCC,
                ':sohieu'     => $soHieu,
                ':noicap'     => $noiCap,
                ':cosoyte'    => $coSoYTe,
                ':ngaycap'    => $ngayCap,
                ':ngaykham'   => $ngayKham,
                ':ngayhethan' => $ngayHetHan,
                ':trangthai'  => $trangThai,
                ':canhbao'    => $canhBao,
                ':ketqua'     => $ketQuaKham,
                ':ghichu'     => $ghiChu
            ]);
            return (int)$data['id'];
        } else {
            $sql = "INSERT INTO chung_chi_nhan_vien 
                    (ma_nv, loai_chung_chi, la_the_xanh_attp, ten_chung_chi, so_hieu_chung_chi, noi_cap, co_so_y_te, ngay_cap, ngay_kham_dinh_ky, ngay_het_han, trang_thai, canh_bao_truoc_ngay, ket_qua_kham, ghi_chu, created_at)
                    VALUES 
                    (:manv, :loai, :lathexanh, :ten, :sohieu, :noicap, :cosoyte, :ngaycap, :ngaykham, :ngayhethan, :trangthai, :canhbao, :ketqua, :ghichu, NOW())";
            $stmt = $this->db->prepare($sql);
            $stmt->execute([
                ':manv'       => $maNV,
                ':loai'       => $loaiCC,
                ':lathexanh'  => $laTheXanh,
                ':ten'        => $tenCC,
                ':sohieu'     => $soHieu,
                ':noicap'     => $noiCap,
                ':cosoyte'    => $coSoYTe,
                ':ngaycap'    => $ngayCap,
                ':ngaykham'   => $ngayKham,
                ':ngayhethan' => $ngayHetHan,
                ':trangthai'  => $trangThai,
                ':canhbao'    => $canhBao,
                ':ketqua'     => $ketQuaKham,
                ':ghichu'     => $ghiChu
            ]);
            return (int)$this->db->lastInsertId();
        }
    }
}
