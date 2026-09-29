<?php

namespace App\Models;

use App\Core\Database;
use PDO;

class DepartmentModel
{
    private PDO $db;

    public function __construct()
    {
        $this->db = Database::getInstance()->getConnection();
    }

    /**
     * Lấy danh sách tất cả phòng ban
     */
    public function getAll(): array
    {
        $stmt = $this->db->query("SELECT * FROM departments ORDER BY name ASC");
        return $stmt->fetchAll();
    }

    /**
     * Lấy thông tin chi tiết một phòng ban theo ID
     */
    public function getById(string $id): ?array
    {
        $stmt = $this->db->prepare("SELECT * FROM departments WHERE id = :id");
        $stmt->execute(['id' => $id]);
        $result = $stmt->fetch();
        return $result ?: null;
    }

    /**
     * Thêm mới một phòng ban
     */
    public function create(array $data): bool
    {
        $sql = "INSERT INTO departments (id, name, parent_id, manager_id) VALUES (:id, :name, :parent_id, :manager_id)";
        $stmt = $this->db->prepare($sql);
        return $stmt->execute([
            'id' => $data['id'],
            'name' => $data['name'],
            'parent_id' => $data['parent_id'] ?? null,
            'manager_id' => $data['manager_id'] ?? null
        ]);
    }
}
