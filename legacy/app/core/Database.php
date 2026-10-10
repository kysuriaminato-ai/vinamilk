<?php

namespace App\Core;

use PDO;
use PDOException;
use PDOStatement;

class Database
{
    private static ?Database $instance = null;
    private ?PDO $connection = null;

    private function __construct()
    {
        $host = $_ENV['DB_HOST'] ?? '127.0.0.1';
        $port = $_ENV['DB_PORT'] ?? '3306';
        $db   = $_ENV['DB_DATABASE'] ?? 'vinamilk_hris';
        $user = $_ENV['DB_USERNAME'] ?? 'root';
        $pass = $_ENV['DB_PASSWORD'] ?? '';
        $charset = 'utf8mb4';

        $dsn = "mysql:host=$host;port=$port;dbname=$db;charset=$charset";
        
        $options = [
            PDO::ATTR_ERRMODE            => PDO::ERRMODE_EXCEPTION, // Throw exceptions on error
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,       // Fetch associative arrays by default
            PDO::ATTR_EMULATE_PREPARES   => false,                  // Use native prepared statements
        ];

        try {
            $this->connection = new PDO($dsn, $user, $pass, $options);
        } catch (PDOException $e) {
            // Log the error in a real app, but for now we stop execution
            die("Database Connection Error: " . $e->getMessage());
        }
    }

    /**
     * Get the Singleton instance of the Database.
     */
    public static function getInstance(): Database
    {
        if (self::$instance === null) {
            self::$instance = new Database();
        }
        return self::$instance;
    }

    /**
     * Get the active PDO connection.
     */
    public function getConnection(): PDO
    {
        return $this->connection;
    }
    // =====================================================================
    // CÁC PHƯƠNG THỨC TIỆN ÍCH (HELPER METHODS)
    // =====================================================================

    /**
     * Thực thi câu truy vấn SELECT với Prepared Statement.
     * 
     * @param string $sql Câu SQL có placeholder
     * @param array<string, mixed> $params Mảng tham số [':key' => value]
     * @return PDOStatement Đối tượng PDOStatement
     */
    public function query(string $sql, array $params = []): PDOStatement
    {
        try {
            $stmt = $this->getConnection()->prepare($sql);
            $stmt->execute($params);
            return $stmt;
        } catch (PDOException $e) {
            error_log('[Vinamilk HRM] Query Error: ' . $e->getMessage() . ' | SQL: ' . $sql);
            throw $e;
        }
    }

    /**
     * Thực thi câu truy vấn SELECT và trả về 1 bản ghi duy nhất.
     */
    public function queryOne(string $sql, array $params = []): array|false
    {
        try {
            $stmt = $this->getConnection()->prepare($sql);
            $stmt->execute($params);
            return $stmt->fetch();
        } catch (PDOException $e) {
            error_log('[Vinamilk HRM] QueryOne Error: ' . $e->getMessage());
            throw $e;
        }
    }

    /**
     * Thực thi câu lệnh INSERT, UPDATE, DELETE.
     */
    public function execute(string $sql, array $params = []): int
    {
        try {
            $stmt = $this->getConnection()->prepare($sql);
            $stmt->execute($params);
            return $stmt->rowCount();
        } catch (PDOException $e) {
            error_log('[Vinamilk HRM] Execute Error: ' . $e->getMessage() . ' | SQL: ' . $sql);
            throw $e;
        }
    }

    /**
     * Lấy ID của bản ghi vừa INSERT.
     */
    public function lastInsertId(): string
    {
        return $this->getConnection()->lastInsertId();
    }

    // =====================================================================
    // TRANSACTION SUPPORT
    // =====================================================================

    public function beginTransaction(): bool
    {
        return $this->getConnection()->beginTransaction();
    }

    public function commit(): bool
    {
        return $this->getConnection()->commit();
    }

    public function rollBack(): bool
    {
        return $this->getConnection()->rollBack();
    }

    public function transaction(callable $callback): mixed
    {
        $this->beginTransaction();
        try {
            $result = $callback($this->getConnection());
            $this->commit();
            return $result;
        } catch (\Throwable $e) {
            $this->rollBack();
            error_log('[Vinamilk HRM] Transaction Error: ' . $e->getMessage());
            throw $e;
        }
    }

    /**
     * Destructor - Đóng kết nối khi đối tượng bị hủy.
     */
    public function __destruct()
    {
        $this->connection = null;
    }

    public static function resetInstance(): void
    {
        if (self::$instance !== null) {
            self::$instance->connection = null;
            self::$instance = null;
        }
    }
}

// Hỗ trợ mã nguồn cũ: Cho phép gọi Database::getInstance() không cần namespace
class_alias('App\Core\Database', 'Database');
