<?php
/**
 * =====================================================================
 * VINAMILK HRM - MODEL NHẬT KÝ KIỂM TOÁN (audit_log)
 * =====================================================================
 */

require_once __DIR__ . '/../core/Model.php';

class AuditLogModel extends Model
{
    protected string $table = 'audit_log';
    protected string $primaryKey = 'id';

    /**
     * LẤY DANH SÁCH NHẬT KÝ KIỂM TOÁN GẦN ĐÂY
     */
    public function getLogs(int $limit = 100, ?string $action = null, ?string $table = null): array
    {
        $where = ["1 = 1"];
        $params = [];

        if (!empty($action) && $action !== 'ALL') {
            $where[] = "hanh_dong = :action";
            $params[':action'] = $action;
        }

        if (!empty($table) && $table !== 'ALL') {
            $where[] = "bang_tac_dong = :tbl";
            $params[':tbl'] = $table;
        }

        $whereSql = implode(' AND ', $where);
        $sql = "SELECT al.*, ns.HoTen, ns.MaDV
                FROM audit_log al
                LEFT JOIN nhansu ns ON al.ma_nv = ns.MaNV
                WHERE {$whereSql}
                ORDER BY al.thoi_gian DESC
                LIMIT {$limit}";

        return $this->fetchAll($sql, $params);
    }
}
