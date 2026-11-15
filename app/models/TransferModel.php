<?php
/**
 * =====================================================================
 * VINAMILK HRM - Transfer Model (Thuyên chuyển công tác & Bổ nhiệm)
 * =====================================================================
 * 
 * Model quản lý điều động nhân sự giữa các Khối/Nhà máy/Trang trại,
 * tạo quyết định bổ nhiệm và lưu vết lịch sử thuyên chuyển.
 */

require_once APP_DIR . '/core/Model.php';

class TransferModel extends Model {
    protected string $table = 'qt_thuyenchuyen';
    protected string $primaryKey = 'ID';

    /**
     * Lấy danh sách tất cả các Lượt Thuyên chuyển công tác
     */
    public function getAllTransfers(): array {
        $sql = "SELECT qttc.*,
                       ns.HoTen, ns.MaNV,
                       dv_cu.TenDV as TenDonViCu,
                       dv_moi.TenDV as TenDonViMoi
                FROM qt_thuyenchuyen qttc
                JOIN nhansu ns ON qttc.MaNV = ns.MaNV
                LEFT JOIN dm_donvi dv_cu ON qttc.DonViCu = dv_cu.MaDV
                LEFT JOIN dm_donvi dv_moi ON qttc.DonViMoi = dv_moi.MaDV
                ORDER BY qttc.NgayHieuLuc DESC, qttc.ID DESC";

        return $this->query($sql)->fetchAll();
    }

    /**
     * Lấy chi tiết Quyết định Thuyên chuyển theo ID để in ấn
     */
    public function getTransferById(int $id): ?array {
        $sql = "SELECT qttc.*,
                       ns.HoTen, ns.GioiTinh, ns.NgaySinh, ns.SoCMND_CCCD, ns.Email, ns.SoDienThoai,
                       dv_cu.TenDV as TenDonViCu, dv_cu.DiaChi as DiaChiDVCu,
                       dv_moi.TenDV as TenDonViMoi, dv_moi.DiaChi as DiaChiDVMoi
                FROM qt_thuyenchuyen qttc
                JOIN nhansu ns ON qttc.MaNV = ns.MaNV
                LEFT JOIN dm_donvi dv_cu ON qttc.DonViCu = dv_cu.MaDV
                LEFT JOIN dm_donvi dv_moi ON qttc.DonViMoi = dv_moi.MaDV
                WHERE qttc.ID = :id LIMIT 1";

        $res = $this->query($sql, ['id' => $id])->fetch();
        return $res ?: null;
    }

    /**
     * Tự động tạo Số Quyết định điều động chuẩn (VD: QĐ-TDC/VNM/2026/001)
     */
    public function generateDecisionNumber(): string {
        $year = date('Y');
        $sql = "SELECT COUNT(*) as total FROM qt_thuyenchuyen WHERE YEAR(CreatedAt) = :yr";
        $total = (int) $this->query($sql, ['yr' => $year])->fetch()['total'] + 1;

        return 'QĐ-TDC/VNM/' . $year . '/' . str_pad($total, 3, '0', STR_PAD_LEFT);
    }

    /**
     * Thực hiện Quyết định Thuyên chuyển công tác (Dùng Transaction)
     */
    public function createTransferDecision(array $data): bool {
        try {
            $this->beginTransaction();

            // 1. Thêm bản ghi vào bảng qt_thuyenchuyen
            $transferId = $this->insert([
                'MaNV' => $data['MaNV'],
                'SoQD' => $data['SoQD'],
                'DonViCu' => $data['DonViCu'],
                'DonViMoi' => $data['DonViMoi'],
                'ChucVuCu' => $data['ChucVuCu'],
                'ChucVuMoi' => $data['ChucVuMoi'],
                'NgayHieuLuc' => $data['NgayHieuLuc'],
                'LyDo' => $data['LyDo'],
                'GhiChu' => $data['GhiChu'] ?? ''
            ]);

            if (!$transferId) {
                $this->rollBack();
                return false;
            }

            // 2. Cập nhật Đơn vị mới và Chức vụ mới trong bảng nhansu
            $sqlUpdate = "UPDATE nhansu 
                          SET MaDV = :donViMoi, 
                              MaCV = (SELECT MaCV FROM dm_chucvu WHERE TenCV = :chucVuMoi OR MaCV = :chucVuMoi LIMIT 1)
                          WHERE MaNV = :maNV";

            $this->query($sqlUpdate, [
                'donViMoi' => $data['DonViMoi'],
                'chucVuMoi' => $data['ChucVuMoi'],
                'maNV' => $data['MaNV']
            ]);

            $this->commit();
            return true;
        } catch (Exception $e) {
            $this->rollBack();
            error_log("Error creating transfer decision: " . $e->getMessage());
            return false;
        }
    }
}
