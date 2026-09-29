<?php

namespace App\Models;

use App\Core\Database;
use PDO;

class TransferModel
{
    private PDO $db;

    public function __construct()
    {
        $this->db = Database::getInstance()->getConnection();
    }

    /**
     * Thêm mới yêu cầu thuyên chuyển
     */
    public function createTransfer(array $data): bool
    {
        $sql = "INSERT INTO transfers (employee_id, old_department_id, new_department_id, transfer_date, reason, status) 
                VALUES (:employee_id, :old_dept, :new_dept, :transfer_date, :reason, 'pending')";
        $stmt = $this->db->prepare($sql);
        
        return $stmt->execute([
            'employee_id'   => $data['employee_id'],
            'old_dept'      => $data['old_department_id'],
            'new_dept'      => $data['new_department_id'],
            'transfer_date' => $data['transfer_date'],
            'reason'        => $data['reason'] ?? ''
        ]);
    }

    /**
     * Duyệt hoặc Từ chối yêu cầu thuyên chuyển
     */
    public function updateStatus(int $transferId, string $status, string $approvedBy): bool
    {
        $sql = "UPDATE transfers 
                SET status = :status, approved_by = :approved_by, updated_at = NOW() 
                WHERE id = :id";
        $stmt = $this->db->prepare($sql);
        
        return $stmt->execute([
            'status'      => $status,
            'approved_by' => $approvedBy,
            'id'          => $transferId
        ]);
    }
}
