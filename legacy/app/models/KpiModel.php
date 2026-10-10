<?php

namespace App\Models;

use App\Core\Database;
use PDO;

class KpiModel
{
    private PDO $db;

    public function __construct()
    {
        $this->db = Database::getInstance()->getConnection();
    }

    /**
     * Lấy lịch sử đánh giá KPI của một nhân viên
     */
    public function getByEmployeeId(string $employeeId): array
    {
        $stmt = $this->db->prepare("SELECT * FROM kpi_metrics WHERE employee_id = :emp_id ORDER BY evaluation_date DESC");
        $stmt->execute(['emp_id' => $employeeId]);
        return $stmt->fetchAll();
    }

    /**
     * Thêm bản ghi đánh giá KPI mới
     */
    public function create(array $data): bool
    {
        $sql = "INSERT INTO kpi_metrics (employee_id, score, evaluation_date, reviewer_id, comments) 
                VALUES (:employee_id, :score, :evaluation_date, :reviewer_id, :comments)";
        $stmt = $this->db->prepare($sql);
        return $stmt->execute([
            'employee_id' => $data['employee_id'],
            'score' => $data['score'],
            'evaluation_date' => $data['evaluation_date'],
            'reviewer_id' => $data['reviewer_id'],
            'comments' => $data['comments'] ?? null
        ]);
    }
}
