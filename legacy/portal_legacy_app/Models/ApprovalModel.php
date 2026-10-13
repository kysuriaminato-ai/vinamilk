<?php
/**
 * =====================================================================
 * VINAMILK HRM - MODEL QUẢN LÝ ĐƠN TỪ & PHÊ DUYỆT (don_tu)
 * =====================================================================
 * Phân quyền phê duyệt:
 *   - QUANLY_TRUCTIEP: Duyệt đơn của nhân viên cấp dưới
 *   - NHANVIEN (ESS): Gửi đơn và theo dõi trạng thái đơn của mình
 *   - Ghi audit_log tự động cho mọi thao tác duyệt/từ chối
 */

require_once __DIR__ . '/../core/Model.php';

class ApprovalModel extends Model
{
    protected string $table = 'don_tu';
    protected string $primaryKey = 'id';

    /**
     * LẤY DANH SÁCH ĐƠN CHỜ DUYỆT CỦA CẤP DƯỚI DÀNH CHO QUẢN LÝ TRỰC TIẾP
     */
    public function getPendingForManager(string $mgrMaNV, string $mgrUnit): array
    {
        // Quản lý được duyệt cho nhân sự cùng đơn vị hoặc cấp dưới trực tiếp
        $sql = "SELECT dt.*, ns.HoTen, ns.MaDV, ns.ChucVu, dv.TenDV
                FROM don_tu dt
                INNER JOIN nhansu ns ON dt.ma_nv = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                WHERE dt.trang_thai = 'CHO_DUYET'
                  AND (ns.MaDV = :unit OR ns.MaNV_QuanLy = :manv)
                ORDER BY dt.created_at ASC";
        
        return $this->fetchAll($sql, [
            ':unit' => $mgrUnit,
            ':manv' => $mgrMaNV
        ]);
    }

    /**
     * LẤY DANH SÁCH ĐƠN CÁ NHÂN (EMPLOYEE SELF-SERVICE - ESS)
     */
    public function getMyRequests(string $maNV): array
    {
        $sql = "SELECT dt.*, ns.HoTen, dv.TenDV
                FROM don_tu dt
                INNER JOIN nhansu ns ON dt.ma_nv = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                WHERE dt.ma_nv = :manv
                ORDER BY dt.created_at DESC";
        
        return $this->fetchAll($sql, [':manv' => $maNV]);
    }

    /**
     * DUYỆT ĐƠN (Có ghi Audit Log)
     */
    public function approve(int $id, string $approver, ?string $comments = null): bool
    {
        $old = $this->find($id);
        if (!$old) return false;

        $sql = "UPDATE don_tu 
                SET trang_thai = 'DA_DUYET', nguoi_duyet = :approver, ngay_duyet = NOW(), y_kien_duyet = :comments
                WHERE id = :id";
        
        $ok = $this->execute($sql, [
            ':id'       => $id,
            ':approver' => $approver,
            ':comments' => $comments
        ]) > 0;

        if ($ok) {
            $this->logAudit('APPROVE', 'don_tu', $id, $old, [
                'trang_thai'   => 'DA_DUYET',
                'nguoi_duyet'  => $approver,
                'y_kien_duyet' => $comments
            ]);
        }
        return $ok;
    }

    /**
     * TỪ CHỐI ĐƠN (Có ghi Audit Log)
     */
    public function reject(int $id, string $approver, ?string $comments = null): bool
    {
        $old = $this->find($id);
        if (!$old) return false;

        $sql = "UPDATE don_tu 
                SET trang_thai = 'TU_CHOI', nguoi_duyet = :approver, ngay_duyet = NOW(), y_kien_duyet = :comments
                WHERE id = :id";
        
        $ok = $this->execute($sql, [
            ':id'       => $id,
            ':approver' => $approver,
            ':comments' => $comments
        ]) > 0;

        if ($ok) {
            $this->logAudit('REJECT', 'don_tu', $id, $old, [
                'trang_thai'   => 'TU_CHOI',
                'nguoi_duyet'  => $approver,
                'y_kien_duyet' => $comments
            ]);
        }
        return $ok;
    }

    /**
     * TẠO ĐƠN NGHỈ PHÉP / ĐỀ XUẤT MỚI (ESS)
     */
    public function submitRequest(array $data): bool
    {
        $data['ma_don'] = 'REQ-' . date('Ymd') . '-' . strtoupper(substr(bin2hex(random_bytes(3)), 0, 5));
        $data['trang_thai'] = 'CHO_DUYET';

        $id = $this->insert($data);
        return $id !== false;
    }
}
