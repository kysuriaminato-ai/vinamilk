<?php
/**
 * =====================================================================
 * VINAMILK HRM - Reward & Discipline Model (Khen thưởng & Kỷ luật)
 * =====================================================================
 * 
 * Model quản lý danh mục và quá trình Khen thưởng (KT) / Kỷ luật (KL).
 * Tích hợp tính giá trị tiền thưởng (+) hoặc tiền phạt (-) vào kỳ lương.
 */

require_once APP_DIR . '/core/Model.php';

class RewardDisciplineModel extends Model {
    protected string $table = 'qt_ktkl';
    protected string $primaryKey = 'ID';

    /**
     * Lấy danh sách tất cả các Lượt Khen thưởng / Kỷ luật
     */
    public function getAllKTKL(?string $loai = null): array {
        $where = [];
        $params = [];

        if (!empty($loai)) {
            $where[] = "dm.Loai = :loai";
            $params['loai'] = $loai; // KhenThuong hoặc KyLuat
        }

        $whereSql = !empty($where) ? "WHERE " . implode(" AND ", $where) : "";

        $sql = "SELECT qt.*,
                       ns.HoTen, ns.MaNV, dv.TenDV, cv.TenCV,
                       dm.Loai as LoaiDanhMuc, dm.TenHinhThuc as TenHinhThucDM
                FROM qt_ktkl qt
                JOIN nhansu ns ON qt.MaNV = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                LEFT JOIN dm_ktkl dm ON qt.MaKTKL = dm.MaKTKL
                {$whereSql}
                ORDER BY qt.NgayQD DESC, qt.ID DESC";

        return $this->query($sql, $params)->fetchAll();
    }

    /**
     * Lấy chi tiết 1 Quyết định Khen thưởng / Kỷ luật theo ID để in ấn
     */
    public function getKTKLById(int $id): ?array {
        $sql = "SELECT qt.*,
                       ns.HoTen, ns.GioiTinh, ns.NgaySinh, ns.SoCMND_CCCD,
                       dv.TenDV, cv.TenCV,
                       dm.Loai as LoaiDanhMuc, dm.TenHinhThuc as TenHinhThucDM
                FROM qt_ktkl qt
                JOIN nhansu ns ON qt.MaNV = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                LEFT JOIN dm_ktkl dm ON qt.MaKTKL = dm.MaKTKL
                WHERE qt.ID = :id LIMIT 1";

        $res = $this->query($sql, ['id' => $id])->fetch();
        return $res ?: null;
    }

    /**
     * Lấy danh mục HÌnh thức Khen thưởng / Kỷ luật
     */
    public function getDanhMucKTKL(): array {
        $sql = "SELECT * FROM dm_ktkl WHERE TrangThai = 1 ORDER BY Loai ASC, MaKTKL ASC";
        return $this->query($sql)->fetchAll();
    }

    /**
     * Tự động tạo Số Quyết định KTKL chuẩn (VD: QĐ-KT/VNM/2026/001)
     */
    public function generateDecisionNumber(string $loai = 'KhenThuong'): string {
        $prefix = ($loai === 'KhenThuong') ? 'QĐ-KT' : 'QĐ-KL';
        $year = date('Y');

        $sql = "SELECT COUNT(*) as total FROM qt_ktkl WHERE SoQD LIKE :pref AND YEAR(CreatedAt) = :yr";
        $total = (int) $this->query($sql, [
            'pref' => $prefix . '%',
            'yr' => $year
        ])->fetch()['total'] + 1;

        return $prefix . '/VNM/' . $year . '/' . str_pad($total, 3, '0', STR_PAD_LEFT);
    }
}
