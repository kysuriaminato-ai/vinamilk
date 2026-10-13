<?php
/**
 * =====================================================================
 * VINAMILK HRM - MODEL CƠ CẤU TỔ CHỨC CÂY ĐƠN VỊ (dm_donvi)
 * =====================================================================
 * Phân cấp 5 tầng:
 *   1. Trụ sở chính Tập đoàn (TP.HCM)
 *   2. Khối chức năng (Văn phòng, Sản xuất, Vùng nguyên liệu, Kinh doanh)
 *   3. Nhà máy / Trang trại / Chi nhánh
 *   4. Phòng / Ban / Xưởng sản xuất
 *   5. Tổ / Ca / Đội sản xuất
 */

require_once __DIR__ . '/../core/Model.php';

class OrgUnitModel extends Model
{
    protected string $table = 'dm_donvi';
    protected string $primaryKey = 'MaDV';

    /**
     * Lấy toàn bộ cây đơn vị kèm theo thông tin đơn vị cha
     */
    public function getTree(): array
    {
        $sql = "SELECT d.*, p.TenDV AS TenDV_Cha
                FROM dm_donvi d
                LEFT JOIN dm_donvi p ON d.MaDV_Cha = p.MaDV
                ORDER BY d.CapDV ASC, d.ThuTu ASC, d.TenDV ASC";
        return $this->fetchAll($sql);
    }

    /**
     * Lấy danh sách cây đơn vị có định dạng thụt đầu dòng (Indented Tree) cho Dropdown Select
     */
    public function getTreeForSelect(?string $rootUnit = null): array
    {
        $allUnits = $this->getTree();
        $branch = [];
        $this->buildBranch($allUnits, $rootUnit, 1, $branch);
        return $branch;
    }

    private function buildBranch(array &$elements, ?string $parentId, int $depth, array &$output): void
    {
        foreach ($elements as $key => $element) {
            $elemParent = $element['MaDV_Cha'];
            $match = ($parentId === null) 
                ? (empty($elemParent) || $element['CapDV'] == 1) 
                : ($elemParent === $parentId);

            if ($match) {
                $prefix = str_repeat('── ', max(0, $depth - 1));
                $element['IndentTitle'] = ($depth > 1 ? '└' . $prefix : '') . $element['TenDV'];
                $element['Depth'] = $depth;
                $output[] = $element;

                // Đệ quy tìm con
                $this->buildBranch($elements, $element['MaDV'], $depth + 1, $output);
            }
        }
    }

    /**
     * Lấy danh sách đơn vị người dùng hiện tại được phép chọn để lọc
     */
    public function getSelectableUnitsForUser(): array
    {
        $user = Auth::user();
        if (!$user) return [];

        $role = strtoupper($user['RoleCode'] ?? '');
        $userUnit = $user['MaDV'] ?? '';

        if (Auth::canViewAllUnits()) {
            return $this->getTreeForSelect(null);
        }

        if ($role === 'HR_DONVI' || $role === 'QUANLY_TRUCTIEP') {
            return $this->getTreeForSelect($userUnit);
        }

        // Nhân viên chỉ thuộc 1 đơn vị của mình
        $single = $this->find($userUnit);
        return $single ? [$single] : [];
    }

    /**
     * Thống kê số lượng nhân sự theo từng khối và đơn vị
     */
    public function getHeadcountByUnits(): array
    {
        $sql = "SELECT d.MaDV, d.TenDV, d.LoaiDV, d.CapDV, d.MaDV_Cha, COUNT(ns.MaNV) as TongSoNhanSu
                FROM dm_donvi d
                LEFT JOIN nhansu ns ON d.MaDV = ns.MaDV AND ns.TrangThai = 1
                GROUP BY d.MaDV, d.TenDV, d.LoaiDV, d.CapDV, d.MaDV_Cha
                ORDER BY d.CapDV ASC, TongSoNhanSu DESC";
        return $this->fetchAll($sql);
    }
}
