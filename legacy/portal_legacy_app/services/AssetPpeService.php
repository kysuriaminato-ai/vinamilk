<?php
/**
 * =====================================================================
 * VINAMILK HRM - DỊCH VỤ QUẢN LÝ TÀI SẢN & TRANG BỊ PPE (AssetPpeService)
 * =====================================================================
 * Nghiệp vụ quy chuẩn:
 *   1. Danh mục định mức theo vị trí công việc:
 *      - Nhà máy sản xuất: Ủng bảo hộ, Áo blouse phòng sạch, Mũ trùm đầu, Găng tay nitrile
 *      - Văn phòng điều hành: Máy tính laptop, Thẻ từ thông minh RFID, Điện thoại & SIM
 *      - Trang trại bò sữa: Ủng chuồng trại, Quần áo bảo hộ, Găng thú y
 *   2. Quy trình Cấp phát & Thu hồi có ký nhận điện tử & biên bản bàn giao
 *   3. Quản lý tài sản chưa trả / báo mất / báo hỏng để chuyển khấu trừ lương
 */

require_once __DIR__ . '/../core/Database.php';

class AssetPpeService
{
    private PDO $db;

    public function __construct(?PDO $db = null)
    {
        $this->db = $db ?? Database::getInstance()->getConnection();
    }

    /**
     * LẤY DANH MỤC TIÊU CHUẨN PPE THEO NHÓM VỊ TRÍ
     */
    public function getStandardsByGroup(?string $group = null): array
    {
        $sql = "SELECT * FROM dm_tieuchuan_ppe";
        $params = [];
        if (!empty($group)) {
            $sql .= " WHERE nhom_vi_tri = :grp";
            $params[':grp'] = $group;
        }
        $sql .= " ORDER BY nhom_vi_tri ASC, bat_buoc DESC, ten_trang_bi ASC";

        $stmt = $this->db->prepare($sql);
        $stmt->execute($params);
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    /**
     * TỰ ĐỘNG GỢI Ý DANH MỤC TRANG BỊ THEO HỒ SƠ NHÂN SỰ
     */
    public function suggestPpeByEmployee(string $maNV): array
    {
        $stmt = $this->db->prepare("SELECT MaNV, HoTen, MaDV, ChucVu, VungLuong FROM nhansu WHERE MaNV = :manv LIMIT 1");
        $stmt->execute([':manv' => $maNV]);
        $emp = $stmt->fetch(PDO::FETCH_ASSOC);

        if (!$emp) {
            return [];
        }

        $maDV = $emp['MaDV'] ?? '';
        $chucVu = mb_strtolower($emp['ChucVu'] ?? '', 'UTF-8');

        // Xác định nhóm vị trí
        if (str_starts_with($maDV, 'TT_') || str_contains($chucVu, 'trang trại') || str_contains($chucVu, 'chăn nuôi') || str_contains($chucVu, 'thú y')) {
            $group = 'TRANG_TRAI_BO_SUA';
        } elseif (str_starts_with($maDV, 'NM_') || str_contains($chucVu, 'công nhân') || str_contains($chucVu, 'vận hành') || str_contains($chucVu, 'kỹ thuật') || str_contains($chucVu, 'chiết rót') || str_contains($chucVu, 'tiệt trùng') || str_contains($chucVu, 'sản xuất') || str_contains($chucVu, 'qa') || str_contains($chucVu, 'qc')) {
            $group = 'NHA_MAY_SAN_XUAT';
        } elseif (str_contains($chucVu, 'kho') || str_contains($chucVu, 'logistics') || str_contains($chucVu, 'lái xe')) {
            $group = 'KHO_LOGISTICS';
        } else {
            $group = 'VAN_PHONG_DIEU_HANH';
        }

        $standards = $this->getStandardsByGroup($group);

        return [
            'employee'   => $emp,
            'group'      => $group,
            'standards'  => $standards
        ];
    }

    /**
     * CẤP PHÁT TÀI SẢN / PPE CHO NHÂN VIÊN CÓ KÝ NHẬN
     */
    public function allocateAsset(array $data): int
    {
        $maNV       = trim($data['ma_nv'] ?? '');
        $loaiTS     = trim($data['loai_tai_san'] ?? 'BHLD_PPE');
        $tenTS      = trim($data['ten_tai_san'] ?? '');
        $serial     = trim($data['ma_dinh_danh_serial'] ?? '');
        $soLuong    = max(1, (int)($data['so_luong'] ?? 1));
        $dvt        = trim($data['don_vi_tinh'] ?? 'Chiếc');
        $giaTri     = (float)($data['gia_tri_tai_san'] ?? 0);
        $ngayCap    = $data['ngay_cap_phat'] ?? date('Y-m-d');
        $tinhTrang  = $data['tinh_trang_khi_cap'] ?? 'Mới 100%';
        $nguoiGiao  = $data['nguoi_giao'] ?? (Auth::username() ?? 'Cán bộ QLTS');
        $isSigned   = !empty($data['da_ky_nhan_cap']) ? 1 : 0;
        $ghiChu     = $data['ghi_chu'] ?? '';

        if (empty($maNV) || empty($tenTS)) {
            throw new InvalidArgumentException("Mã nhân viên và Tên tài sản không được để trống.");
        }

        $maBienBan = 'BB-CP-' . date('Y') . '-' . strtoupper(substr(uniqid(), -6));
        $hashSignature = $isSigned ? hash('sha256', $maNV . '|' . $tenTS . '|' . $ngayCap . '|' . time()) : null;
        $ngayKy = $isSigned ? date('Y-m-d H:i:s') : null;

        $sql = "INSERT INTO cap_phat_tai_san 
                (ma_nv, loai_tai_san, ten_tai_san, ma_dinh_danh_serial, so_luong, don_vi_tinh, gia_tri_tai_san, gia_tri_khau_tru, ngay_cap_phat, tinh_trang_khi_cap, nguoi_giao, nguoi_nhan_ky, da_ky_nhan_cap, ngay_ky_nhan_cap, ma_bien_ban, chu_ky_hash, trang_thai, ghi_chu, created_at)
                VALUES 
                (:manv, :loai, :ten, :serial, :soluong, :dvt, :giatri, 0, :ngaycap, :tinhtrang, :nguoigiao, :nguoinhan, :daky, :ngayky, :mabb, :hash, 'DANG_SU_DUNG', :ghichu, NOW())";

        $stmt = $this->db->prepare($sql);
        $stmt->execute([
            ':manv'       => $maNV,
            ':loai'       => $loaiTS,
            ':ten'        => $tenTS,
            ':serial'     => $serial,
            ':soluong'    => $soLuong,
            ':dvt'        => $dvt,
            ':giatri'     => $giaTri,
            ':ngaycap'    => $ngayCap,
            ':tinhtrang'  => $tinhTrang,
            ':nguoigiao'  => $nguoiGiao,
            ':nguoinhan'  => $maNV,
            ':daky'       => $isSigned,
            ':ngayky'     => $ngayKy,
            ':mabb'       => $maBienBan,
            ':hash'       => $hashSignature,
            ':ghichu'     => $ghiChu
        ]);

        return (int)$this->db->lastInsertId();
    }

    /**
     * KÝ NHẬN ĐIỆN TỬ BÀN GIAO TÀI SẢN
     */
    public function signAllocation(int $assetId, ?string $signer = null): bool
    {
        $hash = hash('sha256', 'SIGN_ALLOCATION|' . $assetId . '|' . time());
        $sql = "UPDATE cap_phat_tai_san 
                SET da_ky_nhan_cap = 1, ngay_ky_nhan_cap = NOW(), chu_ky_hash = :hash 
                WHERE id = :id";
        $stmt = $this->db->prepare($sql);
        return $stmt->execute([':hash' => $hash, ':id' => $assetId]);
    }

    /**
     * THU HỒI TÀI SẢN HOẶC BÁO MẤT / BÁO HỎNG CÓ KÝ NHẬN
     */
    public function returnOrRecoverAsset(int $assetId, string $status, ?float $penaltyAmount = null, ?string $condition = null, ?string $notes = null): bool
    {
        // status: DA_THU_HOI, BAO_MAT, BAO_HONG
        $validStatuses = ['DA_THU_HOI', 'BAO_MAT', 'BAO_HONG'];
        if (!in_array($status, $validStatuses, true)) {
            $status = 'DA_THU_HOI';
        }

        $stmtGet = $this->db->prepare("SELECT * FROM cap_phat_tai_san WHERE id = :id LIMIT 1");
        $stmtGet->execute([':id' => $assetId]);
        $asset = $stmtGet->fetch(PDO::FETCH_ASSOC);

        if (!$asset) {
            throw new InvalidArgumentException("Không tìm thấy tài sản ID #{$assetId}");
        }

        // Nếu báo mất hoặc báo hỏng và chưa nhập số tiền phạt -> Lấy giá trị tài sản
        if ($status !== 'DA_THU_HOI' && ($penaltyAmount === null || $penaltyAmount <= 0)) {
            $penaltyAmount = (float)$asset['gia_tri_tai_san'];
            if ($penaltyAmount <= 0) {
                // Giá trị mặc định theo loại
                $penaltyAmount = match ($asset['loai_tai_san']) {
                    'MAY_TINH_LAPTOP' => 18500000.0,
                    'THE_TU_RA_VAO'   => 150000.0,
                    'DIEN_THOAI_SIM'  => 3500000.0,
                    'BHLD_PPE'        => 450000.0,
                    default           => 500000.0
                };
            }
        } elseif ($status === 'DA_THU_HOI') {
            $penaltyAmount = 0.0;
        }

        $sql = "UPDATE cap_phat_tai_san 
                SET trang_thai = :status,
                    ngay_thu_hoi_thuc_te = CURDATE(),
                    tinh_trang_khi_thu_hoi = :tinhtrang,
                    gia_tri_khau_tru = :penalty,
                    da_ky_nhan_thu_hoi = 1,
                    ngay_ky_nhan_thu_hoi = NOW(),
                    ghi_chu = CONCAT(COALESCE(ghi_chu, ''), ' | Thu hồi: ', :notes)
                WHERE id = :id";

        $stmt = $this->db->prepare($sql);
        return $stmt->execute([
            ':status'    => $status,
            ':tinhtrang' => $condition ?? ($status === 'DA_THU_HOI' ? 'Đã thu hồi đầy đủ' : 'Thất thoát / Hư hỏng'),
            ':penalty'   => $penaltyAmount,
            ':notes'     => $notes ?? '',
            ':id'        => $assetId
        ]);
    }

    /**
     * LẤY DANH SÁCH TÀI SẢN CHƯA HOÀN TRẢ HOẶC BỊ PHẠT CỦA NHÂN VIÊN
     */
    public function getUnreturnedAssets(string $maNV): array
    {
        $sql = "SELECT * FROM cap_phat_tai_san 
                WHERE ma_nv = :manv 
                  AND (trang_thai IN ('BAO_MAT', 'BAO_HONG') OR (trang_thai = 'DANG_SU_DUNG' AND ngay_thu_hoi_thuc_te IS NULL))
                ORDER BY ngay_cap_phat DESC";

        $stmt = $this->db->prepare($sql);
        $stmt->execute([':manv' => $maNV]);
        return $stmt->fetchAll(PDO::FETCH_ASSOC);
    }

    /**
     * TÍNH TỔNG TIỀN KHẤU TRỪ TÀI SẢN CỦA NHÂN VIÊN
     */
    public function calculateTotalAssetPenalty(string $maNV): float
    {
        $sql = "SELECT SUM(
                    CASE 
                        WHEN gia_tri_khau_tru > 0 THEN gia_tri_khau_tru
                        WHEN trang_thai IN ('BAO_MAT', 'BAO_HONG') AND gia_tri_tai_san > 0 THEN gia_tri_tai_san
                        WHEN trang_thai IN ('BAO_MAT', 'BAO_HONG') THEN 500000
                        ELSE 0 
                    END
                ) as total_penalty
                FROM cap_phat_tai_san
                WHERE ma_nv = :manv";

        $stmt = $this->db->prepare($sql);
        $stmt->execute([':manv' => $maNV]);
        $val = $stmt->fetchColumn();
        return (float)($val ?: 0.0);
    }
}
