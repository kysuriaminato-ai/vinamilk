<?php

namespace App\Models;

use App\Core\Database;
use PDO;

class AttendanceModel
{
    private PDO $db;

    public function __construct()
    {
        $this->db = Database::getInstance()->getConnection();
    }

    /**
     * Tạo mới một yêu cầu nghỉ phép (Leave Request)
     */
    public function createLeaveRequest(array $data): bool
    {
        $sql = "INSERT INTO leave_requests (employee_id, start_date, end_date, reason, status) 
                VALUES (:employee_id, :start_date, :end_date, :reason, 'pending')";
        $stmt = $this->db->prepare($sql);
        
        return $stmt->execute([
            'employee_id' => $data['employee_id'],
            'start_date'  => $data['start_date'],
            'end_date'    => $data['end_date'],
            'reason'      => $data['reason'] ?? ''
        ]);
    }

    /**
     * Lấy danh sách yêu cầu nghỉ phép cần duyệt (dành cho Quản lý)
     */
    public function getPendingRequests(): array
    {
        $sql = "SELECT * FROM leave_requests WHERE status = 'pending' ORDER BY created_at DESC";
        $stmt = $this->db->query($sql);
        
        return $stmt->fetchAll();
    }

    /**
     * Cập nhật trạng thái phê duyệt (Approve/Reject)
     */
    public function updateStatus(int $requestId, string $status, string $managerId): bool
    {
        $sql = "UPDATE leave_requests 
                SET status = :status, approved_by = :manager_id, updated_at = NOW() 
                WHERE id = :id";
        $stmt = $this->db->prepare($sql);
        
        return $stmt->execute([
            'status'     => $status,
            'manager_id' => $managerId,
            'id'         => $requestId
        ]);
    }
}
