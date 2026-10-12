<?php
/**
 * =====================================================================
 * VINAMILK HRM - KẾT NỐI CƠ SỞ DỮ LIỆU MYSQL 8 (DATABASE PDO SINGLETON)
 * =====================================================================
 * Đảm bảo 100% Prepared Statements chống SQL Injection
 */

class Database
{
    private static ?Database $instance = null;
    private ?PDO $connection = null;

    private function __construct()
    {
        $host    = defined('DB_HOST') ? DB_HOST : '127.0.0.1';
        $port    = defined('DB_PORT') ? DB_PORT : 3306;
        $dbName  = defined('DB_NAME') ? DB_NAME : 'vinamilk_hrm';
        $user    = defined('DB_USER') ? DB_USER : 'root';
        $pass    = defined('DB_PASS') ? DB_PASS : '';
        $charset = defined('DB_CHARSET') ? DB_CHARSET : 'utf8mb4';

        $dsn = "mysql:host={$host};port={$port};dbname={$dbName};charset={$charset}";

        $options = [
            PDO::ATTR_ERRMODE            => PDO::ERRMODE_EXCEPTION, // Ném Exception khi có lỗi SQL
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,       // Trả về mảng kết hợp
            PDO::ATTR_EMULATE_PREPARES   => false,                  // Dùng Native Prepared Statements của MySQL 8
            PDO::MYSQL_ATTR_INIT_COMMAND => "SET NAMES {$charset} COLLATE utf8mb4_unicode_ci"
        ];

        try {
            $this->connection = new PDO($dsn, $user, $pass, $options);
        } catch (PDOException $e) {
            error_log('[Vinamilk HRM Database Error] ' . $e->getMessage());
            die('<div style="font-family:sans-serif;padding:30px;background:#fef2f2;border-left:5px solid #dc2626;color:#991b1b;">' .
                '<h3>Lỗi kết nối Cơ sở dữ liệu Vinamilk HRM</h3>' .
                '<p>' . htmlspecialchars($e->getMessage(), ENT_QUOTES, 'UTF-8') . '</p>' .
                '<small>Vui lòng kiểm tra dịch vụ MySQL trên cổng ' . htmlspecialchars((string)$port, ENT_QUOTES, 'UTF-8') . '</small></div>');
        }
    }

    /**
     * Singleton Instance
     */
    public static function getInstance(): Database
    {
        if (self::$instance === null) {
            self::$instance = new Database();
        }
        return self::$instance;
    }

    /**
     * Lấy đối tượng kết nối PDO
     */
    public function getConnection(): PDO
    {
        return $this->connection;
    }

    /**
     * Thực thi truy vấn SELECT với Prepared Statements
     */
    public function query(string $sql, array $params = []): PDOStatement
    {
        try {
            $stmt = $this->connection->prepare($sql);
            $stmt->execute($params);
            return $stmt;
        } catch (PDOException $e) {
            error_log('[Vinamilk HRM Query Error] ' . $e->getMessage() . ' | SQL: ' . $sql);
            throw $e;
        }
    }

    /**
     * Lấy 1 dòng kết quả duy nhất
     */
    public function queryOne(string $sql, array $params = []): array|false
    {
        return $this->query($sql, $params)->fetch();
    }

    /**
     * Lấy toàn bộ danh sách kết quả
     */
    public function queryAll(string $sql, array $params = []): array
    {
        return $this->query($sql, $params)->fetchAll();
    }

    /**
     * Thực thi lệnh INSERT / UPDATE / DELETE
     */
    public function execute(string $sql, array $params = []): int
    {
        return $this->query($sql, $params)->rowCount();
    }

    /**
     * Lấy ID vừa được tạo từ câu lệnh INSERT (Auto Increment)
     */
    public function lastInsertId(): string|false
    {
        return $this->connection->lastInsertId();
    }

    /**
     * Quản lý Transaction
     */
    public function beginTransaction(): bool
    {
        return $this->connection->beginTransaction();
    }

    public function commit(): bool
    {
        return $this->connection->commit();
    }

    public function rollBack(): bool
    {
        return $this->connection->rollBack();
    }
}
