<?php
/**
 * =====================================================================
 * VINAMILK HRM - Base Model Class
 * =====================================================================
 * 
 * Lớp Model cơ sở cho kiến trúc MVC Vinamilk HRM.
 * Tích hợp kết nối CSDL PDO Singleton, cung cấp các phương thức
 * truy vấn dữ liệu CRUD cơ bản (insert, update, delete, find, query...).
 */

require_once __DIR__ . '/Database.php';

abstract class Model {
    protected PDO $db;
    protected string $table = '';
    protected string $primaryKey = 'ID';

    public function __construct() {
        $this->db = Database::getInstance()->getConnection();
    }

    /**
     * Thực thi câu lệnh SQL tùy chỉnh
     */
    public function query(string $sql, array $params = []): PDOStatement {
        return Database::getInstance()->query($sql, $params);
    }

    /**
     * Lấy tất cả bản ghi
     */
    public function all(string $orderBy = '', string $direction = 'ASC'): array {
        $sql = "SELECT * FROM {$this->table}";
        if (!empty($orderBy)) {
            $sql .= " ORDER BY {$orderBy} {$direction}";
        }
        return $this->query($sql)->fetchAll();
    }

    /**
     * Tìm bản ghi theo Khóa chính (Primary Key)
     */
    public function find(mixed $id): ?array {
        $sql = "SELECT * FROM {$this->table} WHERE {$this->primaryKey} = :id LIMIT 1";
        $result = $this->query($sql, ['id' => $id])->fetch();
        return $result ?: null;
    }

    /**
     * Lấy bản ghi theo điều kiện WHERE (Mảng key => value)
     */
    public function where(array $conditions, string $orderBy = '', string $direction = 'ASC'): array {
        $whereClause = [];
        $params = [];

        foreach ($conditions as $column => $value) {
            $whereClause[] = "{$column} = :{$column}";
            $params[$column] = $value;
        }

        $sql = "SELECT * FROM {$this->table} WHERE " . implode(' AND ', $whereClause);
        if (!empty($orderBy)) {
            $sql .= " ORDER BY {$orderBy} {$direction}";
        }

        return $this->query($sql, $params)->fetchAll();
    }

    /**
     * Thêm bản ghi mới
     */
    public function insert(array $data): string|false {
        $columns = implode(', ', array_keys($data));
        $placeholders = ':' . implode(', :', array_keys($data));

        $sql = "INSERT INTO {$this->table} ({$columns}) VALUES ({$placeholders})";
        $stmt = $this->query($sql, $data);

        if ($stmt->rowCount() > 0) {
            return Database::getInstance()->lastInsertId();
        }
        return false;
    }

    /**
     * Cập nhật bản ghi theo Khóa chính
     */
    public function update(mixed $id, array $data): bool {
        $fields = [];
        foreach (array_keys($data) as $column) {
            $fields[] = "{$column} = :{$column}";
        }

        $sql = "UPDATE {$this->table} SET " . implode(', ', $fields) . " WHERE {$this->primaryKey} = :_primary_id";
        $data['_primary_id'] = $id;

        $stmt = $this->query($sql, $data);
        return $stmt->rowCount() > 0;
    }

    /**
     * Xóa bản ghi theo Khóa chính
     */
    public function delete(mixed $id): bool {
        $sql = "DELETE FROM {$this->table} WHERE {$this->primaryKey} = :id";
        $stmt = $this->query($sql, ['id' => $id]);
        return $stmt->rowCount() > 0;
    }

    /**
     * Đếm tổng số bản ghi
     */
    public function count(array $conditions = []): int {
        if (empty($conditions)) {
            $sql = "SELECT COUNT(*) as total FROM {$this->table}";
            return (int) $this->query($sql)->fetch()['total'];
        }

        $whereClause = [];
        $params = [];
        foreach ($conditions as $column => $value) {
            $whereClause[] = "{$column} = :{$column}";
            $params[$column] = $value;
        }

        $sql = "SELECT COUNT(*) as total FROM {$this->table} WHERE " . implode(' AND ', $whereClause);
        return (int) $this->query($sql, $params)->fetch()['total'];
    }

    /**
     * Bắt đầu một Database Transaction
     */
    public function beginTransaction(): bool {
        return Database::getInstance()->beginTransaction();
    }

    /**
     * Commit Transaction
     */
    public function commit(): bool {
        return Database::getInstance()->commit();
    }

    /**
     * Rollback Transaction
     */
    public function rollBack(): bool {
        return Database::getInstance()->rollBack();
    }
}
