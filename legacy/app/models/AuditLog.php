<?php
/**
 * =====================================================================
 * VINAMILK HRM - Access & Audit Log Model
 * =====================================================================
 * 
 * Model ghi nhận lịch sử truy cập (Login/Logout) và thao tác hệ thống.
 */

require_once APP_DIR . '/core/Model.php';

class AuditLog extends Model {
    protected string $table = 'lich_su_truy_cap';
    protected string $primaryKey = 'ID';

    public function __construct() {
        parent::__construct();
        $this->ensureTableExists();
    }

    /**
     * Tự động tạo bảng lịch sử truy cập nếu chưa tồn tại trong CSDL
     */
    private function ensureTableExists(): void {
        $sql = "CREATE TABLE IF NOT EXISTS `lich_su_truy_cap` (
            `ID`            INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
            `UserID`        INT UNSIGNED NULL,
            `Username`      VARCHAR(50) NOT NULL,
            `HanhDong`      VARCHAR(100) NOT NULL,
            `TrangThai`     VARCHAR(20) NOT NULL DEFAULT 'Success',
            `DiaChiIP`      VARCHAR(45) NULL,
            `UserAgent`     VARCHAR(255) NULL,
            `MoTa`          TEXT NULL,
            `CreatedAt`     DATETIME DEFAULT CURRENT_TIMESTAMP,
            KEY `idx_audit_user` (`UserID`),
            KEY `idx_audit_created` (`CreatedAt`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Nhật ký lịch sử truy cập và thao tác hệ thống';";

        $this->db->exec($sql);
    }

    /**
     * Ghi Log thao tác
     */
    public function log(string $action, string $status = 'Success', ?int $userId = null, string $username = '', string $description = ''): void {
        $ip = $_SERVER['REMOTE_ADDR'] ?? '127.0.0.1';
        $agent = substr($_SERVER['HTTP_USER_AGENT'] ?? 'Unknown', 0, 250);

        $this->insert([
            'UserID' => $userId,
            'Username' => $username,
            'HanhDong' => $action,
            'TrangThai' => $status,
            'DiaChiIP' => $ip,
            'UserAgent' => $agent,
            'MoTa' => $description
        ]);
    }

    /**
     * Lấy 50 nhật ký truy cập mới nhất
     */
    public function getRecentLogs(int $limit = 50): array {
        $sql = "SELECT * FROM lich_su_truy_cap ORDER BY CreatedAt DESC LIMIT :limit";
        $stmt = $this->db->prepare($sql);
        $stmt->bindValue(':limit', $limit, PDO::PARAM_INT);
        $stmt->execute();
        return $stmt->fetchAll();
    }
}
