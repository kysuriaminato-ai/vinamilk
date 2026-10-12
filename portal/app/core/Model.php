<?php
/**
 * =====================================================================
 * VINAMILK HRM - LỚP MODEL CƠ SỞ (BASE MODEL)
 * =====================================================================
 * Tích hợp bắt buộc:
 *   1. Hàm scopeByUnit() lọc danh sách theo cây đơn vị dm_donvi của người đăng nhập
 *   2. Thuật toán Recursive CTE MySQL 8 duyệt cây đơn vị cha - con đa tầng
 *   3. Ghi audit_log tự động cho mọi thao tác C/U/D/Approve
 *   4. 100% Prepared Statements qua PDO
 */

require_once __DIR__ . '/Database.php';
require_once __DIR__ . '/../helpers/Auth.php';

abstract class Model
{
    protected PDO $db;
    protected string $table = '';
    protected string $primaryKey = 'id';

    public function __construct()
    {
        $this->db = Database::getInstance()->getConnection();
    }

    /**
     * THỰC THI TRUY VẤN SCOPE THEO CÂY ĐƠN VỊ CỦA NGƯỜI ĐĂNG NHẬP (BẮT BUỘC THEO ĐẶC TẢ)
     * 
     * Phân quyền theo 7 vai trò:
     * - ADMIN, BOD, HR_TAPDOAN, KETOAN_LUONG: Xem toàn bộ tập đoàn (hoặc lọc theo cây của đơn vị được chọn trên UI)
     * - HR_DONVI: Chỉ xem cây MaDV của mình (gồm đơn vị mình quản lý và toàn bộ các đơn vị con/cháu)
     * - QUANLY_TRUCTIEP: Chỉ xem nhân sự thuộc đơn vị mình phụ trách hoặc nhân sự cấp dưới trực tiếp
     * - NHANVIEN: (ESS) Chỉ truy cập dữ liệu của chính mình (MaNV)
     * 
     * @param string $tableAlias Alias của bảng nhân sự (mặc định 'ns')
     * @param string $unitColumn Tên cột đơn vị trong bảng (mặc định 'MaDV')
     * @return array Mảng gồm ['where' => string, 'params' => array]
     */
    public function scopeByUnit(string $tableAlias = 'ns', ?string $unitColumn = 'MaDV'): array
    {
        $user = Auth::user();
        if (!$user) {
            // Chưa đăng nhập -> chặn hoàn toàn truy vấn
            return ['where' => '1 = 0', 'params' => []];
        }

        $role = strtoupper($user['RoleCode'] ?? '');
        $userUnit = $user['MaDV'] ?? '';
        $userMaNV = $user['MaNV'] ?? '';

        // 1. NHÓM QUYỀN TOÀN TẬP ĐOÀN (ADMIN, BOD, HR_TAPDOAN, KETOAN_LUONG)
        if (in_array($role, ['ADMIN', 'BOD', 'HR_TAPDOAN', 'KETOAN_LUONG', 'HR_MANAGER'])) {
            // Kiểm tra xem người dùng có đang áp dụng bộ lọc đơn vị cụ thể trên thanh Header hay không
            $filterUnit = $_GET['unit_scope'] ?? null;
            if (!empty($filterUnit) && $filterUnit !== 'ALL') {
                $childUnits = $this->getDescendantUnits($filterUnit);
                $placeholders = [];
                $params = [];
                foreach ($childUnits as $idx => $u) {
                    $pKey = ':scope_u_' . $idx;
                    $placeholders[] = $pKey;
                    $params[$pKey] = $u;
                }
                return [
                    'where' => "{$tableAlias}.{$unitColumn} IN (" . implode(', ', $placeholders) . ")",
                    'params' => $params
                ];
            }
            // Không chọn lọc -> xem toàn bộ
            return ['where' => '1 = 1', 'params' => []];
        }

        // 2. NHÓM HR ĐƠN VỊ (HR_DONVI): Chỉ xem cây đơn vị MaDV của mình và cấp dưới đệ quy
        if (in_array($role, ['HR_DONVI'])) {
            $allowedChildUnits = !empty($userUnit) ? $this->getDescendantUnits($userUnit) : [];
            if (empty($allowedChildUnits)) {
                $allowedChildUnits = [$userUnit];
            }

            // Nếu người dùng yêu cầu lọc một đơn vị cụ thể (qua tham số URL hoặc filter)
            $requestedUnit = $_GET['unit_scope'] ?? ($_GET['ma_dv'] ?? null);
            if (!empty($requestedUnit) && $requestedUnit !== 'ALL') {
                // Kiểm tra đơn vị được yêu cầu có nằm trong cây đơn vị được phép của HR này không
                if (!in_array($requestedUnit, $allowedChildUnits, true)) {
                    // Cố tình truy cập đơn vị ngoài phạm vi được phân quyền -> Trả về điều kiện rỗng 1 = 0
                    return [
                        'where' => '1 = 0',
                        'params' => []
                    ];
                }
                // Nếu nằm trong cây của mình -> lấy cây con của đơn vị được lọc
                $targetUnits = $this->getDescendantUnits($requestedUnit);
                $finalUnits = array_values(array_intersect($targetUnits, $allowedChildUnits));
            } else {
                $finalUnits = $allowedChildUnits;
            }

            if (empty($finalUnits)) {
                return ['where' => '1 = 0', 'params' => []];
            }

            $placeholders = [];
            $params = [];
            foreach ($finalUnits as $idx => $u) {
                $pKey = ':hr_scope_u_' . $idx;
                $placeholders[] = $pKey;
                $params[$pKey] = $u;
            }
            return [
                'where' => "{$tableAlias}.{$unitColumn} IN (" . implode(', ', $placeholders) . ")",
                'params' => $params
            ];
        }

        // 3. NHÓM QUẢN LÝ TRỰC TIẾP (QUANLY_TRUCTIEP / MANAGER): Thấy cấp dưới trực tiếp
        if (in_array($role, ['QUANLY_TRUCTIEP', 'MANAGER'])) {
            return [
                'where' => "({$tableAlias}.MaNV_QuanLy = :mgr_manv OR {$tableAlias}.MaNV = :mgr_manv)",
                'params' => [':mgr_manv' => $userMaNV]
            ];
        }

        // 4. NHÓM NHÂN VIÊN TỰ PHỤC VỤ (NHANVIEN / EMPLOYEE - ESS): Chỉ thấy MaNV của mình
        return [
            'where' => "{$tableAlias}.MaNV = :ess_manv",
            'params' => [':ess_manv' => $userMaNV]
        ];
    }

    /**
     * TRUY VẤN TOÀN BỘ CÂY ĐƠN VỊ CON CHÁU DÙNG MYSQL 8 RECURSIVE CTE
     * 
     * Đảm bảo tính toán chính xác cây tổ chức:
     * Trụ sở -> Khối -> Nhà máy/Trang trại/Chi nhánh -> Xưởng/Phòng -> Tổ/Ca
     * 
     * @param string $rootUnit Mã đơn vị gốc
     * @return array Danh sách toàn bộ MaDV thuộc cây (gồm gốc + con + cháu)
     */
    public function getDescendantUnits(string $rootUnit): array
    {
        $sql = "WITH RECURSIVE UnitTree AS (
                    SELECT MaDV, MaDV_Cha FROM dm_donvi WHERE MaDV = :root_unit
                    UNION ALL
                    SELECT d.MaDV, d.MaDV_Cha 
                    FROM dm_donvi d 
                    INNER JOIN UnitTree ut ON d.MaDV_Cha = ut.MaDV
                )
                SELECT MaDV FROM UnitTree";
        
        try {
            $stmt = $this->db->prepare($sql);
            $stmt->execute([':root_unit' => $rootUnit]);
            $results = $stmt->fetchAll(PDO::FETCH_COLUMN);
            return !empty($results) ? $results : [$rootUnit];
        } catch (PDOException $e) {
            error_log('[Vinamilk CTE Error] ' . $e->getMessage());
            return [$rootUnit];
        }
    }

    /**
     * GHI VẾT NHẬT KÝ KIỂM TOÁN (AUDIT LOG) - BẮT BUỘC THEO ĐẶC TẢ
     * 
     * @param string $action Hành động (CREATE, UPDATE, DELETE, APPROVE, REJECT, CALCULATE_PAYROLL)
     * @param string $table Tên bảng bị tác động
     * @param string|int $recordId Khóa chính hoặc mã bản ghi
     * @param array|null $oldData Dữ liệu trước khi sửa/xóa
     * @param array|null $newData Dữ liệu sau khi tạo/sửa
     */
    public function logAudit(string $action, string $table, string|int $recordId, ?array $oldData = null, ?array $newData = null): bool
    {
        $user = Auth::user();
        $username = $user['Username'] ?? 'SYSTEM';
        $maNV = $user['MaNV'] ?? null;
        $ip = $_SERVER['REMOTE_ADDR'] ?? '127.0.0.1';
        $userAgent = $_SERVER['HTTP_USER_AGENT'] ?? 'CLI';

        $sql = "INSERT INTO audit_log (ma_nv, username, hanh_dong, bang_tac_dong, ban_ghi_id, du_lieu_cu, du_lieu_moi, ip_address, user_agent, thoi_gian)
                VALUES (:ma_nv, :username, :hanh_dong, :bang_tac_dong, :ban_ghi_id, :du_lieu_cu, :du_lieu_moi, :ip_address, :user_agent, NOW())";
        
        try {
            $stmt = $this->db->prepare($sql);
            return $stmt->execute([
                ':ma_nv'         => $maNV,
                ':username'      => $username,
                ':hanh_dong'     => $action,
                ':bang_tac_dong' => $table,
                ':ban_ghi_id'    => (string)$recordId,
                ':du_lieu_cu'    => $oldData ? json_encode($oldData, JSON_UNESCAPED_UNICODE) : null,
                ':du_lieu_moi'   => $newData ? json_encode($newData, JSON_UNESCAPED_UNICODE) : null,
                ':ip_address'    => $ip,
                ':user_agent'    => mb_substr($userAgent, 0, 500)
            ]);
        } catch (PDOException $e) {
            error_log('[AuditLog Error] ' . $e->getMessage());
            return false;
        }
    }

    /**
     * Truy vấn thông thường với Prepared Statements
     */
    public function query(string $sql, array $params = []): PDOStatement
    {
        return Database::getInstance()->query($sql, $params);
    }

    /**
     * Lấy 1 dòng kết quả
     */
    public function fetchOne(string $sql, array $params = []): array|false
    {
        return Database::getInstance()->queryOne($sql, $params);
    }

    /**
     * Lấy tất cả dòng kết quả
     */
    public function fetchAll(string $sql, array $params = []): array
    {
        return Database::getInstance()->queryAll($sql, $params);
    }

    /**
     * Lấy bản ghi theo Khóa chính
     */
    public function find(mixed $id): ?array
    {
        $sql = "SELECT * FROM {$this->table} WHERE {$this->primaryKey} = :id LIMIT 1";
        $res = $this->fetchOne($sql, [':id' => $id]);
        return $res ?: null;
    }

    /**
     * Lấy tất cả bản ghi có sắp xếp
     */
    public function all(string $orderBy = '', string $direction = 'ASC'): array
    {
        $sql = "SELECT * FROM {$this->table}";
        if (!empty($orderBy)) {
            $direction = strtoupper($direction) === 'DESC' ? 'DESC' : 'ASC';
            $sql .= " ORDER BY {$orderBy} {$direction}";
        }
        return $this->fetchAll($sql);
    }

    /**
     * Thêm mới bản ghi
     */
    public function insert(array $data): string|false
    {
        $columns = implode('`, `', array_keys($data));
        $placeholders = ':' . implode(', :', array_keys($data));

        $sql = "INSERT INTO `{$this->table}` (`{$columns}`) VALUES ({$placeholders})";
        
        $params = [];
        foreach ($data as $k => $v) {
            $params[':' . $k] = $v;
        }

        $stmt = $this->query($sql, $params);
        if ($stmt->rowCount() > 0) {
            $insertId = Database::getInstance()->lastInsertId();
            $this->logAudit('CREATE', $this->table, $insertId, null, $data);
            return $insertId;
        }
        return false;
    }

    /**
     * Cập nhật bản ghi theo Khóa chính
     */
    public function update(mixed $id, array $data): bool
    {
        $old = $this->find($id);

        $fields = [];
        $params = [':_id' => $id];
        foreach ($data as $col => $val) {
            $fields[] = "`{$col}` = :{$col}";
            $params[':' . $col] = $val;
        }

        $sql = "UPDATE `{$this->table}` SET " . implode(', ', $fields) . " WHERE `{$this->primaryKey}` = :_id";
        $stmt = $this->query($sql, $params);
        $success = $stmt->rowCount() >= 0;

        if ($success) {
            $this->logAudit('UPDATE', $this->table, $id, $old, $data);
        }
        return $success;
    }

    /**
     * Xóa bản ghi theo Khóa chính
     */
    public function delete(mixed $id): bool
    {
        $old = $this->find($id);
        $sql = "DELETE FROM `{$this->table}` WHERE `{$this->primaryKey}` = :id";
        $stmt = $this->query($sql, [':id' => $id]);
        $success = $stmt->rowCount() > 0;

        if ($success) {
            $this->logAudit('DELETE', $this->table, $id, $old, null);
        }
        return $success;
    }

    /**
     * Đếm số bản ghi
     */
    public function count(string $where = '1 = 1', array $params = []): int
    {
        $sql = "SELECT COUNT(*) as total FROM `{$this->table}` WHERE {$where}";
        $row = $this->fetchOne($sql, $params);
        return (int)($row['total'] ?? 0);
    }

    /**
     * Thực thi câu lệnh SQL INSERT/UPDATE/DELETE và trả về số dòng bị tác động
     */
    public function execute(string $sql, array $params = []): int
    {
        $stmt = $this->query($sql, $params);
        return $stmt->rowCount();
    }

    public function beginTransaction(): bool
    {
        return $this->db->inTransaction() ? true : $this->db->beginTransaction();
    }

    public function commit(): bool
    {
        return $this->db->inTransaction() ? $this->db->commit() : true;
    }

    public function rollBack(): bool
    {
        return $this->db->inTransaction() ? $this->db->rollBack() : false;
    }

    public function lastInsertId(?string $name = null): string|false
    {
        return $this->db->lastInsertId($name);
    }
}
