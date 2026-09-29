<?php

namespace App\Controllers;

use App\Models\AttendanceModel;
use Exception;

class AttendanceController
{
    private AttendanceModel $model;

    public function __construct()
    {
        $this->model = new AttendanceModel();
    }

    /**
     * Nhân viên gửi yêu cầu nghỉ phép (POST)
     */
    public function requestLeave(): void
    {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            http_response_code(405);
            return;
        }

        $data = [
            'employee_id' => $_POST['employee_id'] ?? null,
            'start_date'  => $_POST['start_date'] ?? null,
            'end_date'    => $_POST['end_date'] ?? null,
            'reason'      => $_POST['reason'] ?? ''
        ];

        if (!$data['employee_id'] || !$data['start_date'] || !$data['end_date']) {
            $this->jsonResponse('error', 'Vui lòng điền đủ thông tin bắt buộc', 400);
            return;
        }

        try {
            $this->model->createLeaveRequest($data);
            $this->jsonResponse('success', 'Đã gửi yêu cầu nghỉ phép thành công. Đang chờ duyệt.');
        } catch (Exception $e) {
            $this->jsonResponse('error', 'Lỗi Database: ' . $e->getMessage(), 500);
        }
    }

    /**
     * Quản lý phê duyệt yêu cầu (POST)
     */
    public function approveLeave(): void
    {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            http_response_code(405);
            return;
        }

        // Trong thực tế, role và manager_id sẽ lấy từ Session (Auth)
        $role = $_POST['role'] ?? 'Employee';
        $managerId = $_POST['manager_id'] ?? '';
        $requestId = (int)($_POST['request_id'] ?? 0);
        $action = $_POST['action'] ?? ''; // 'approve' hoặc 'reject'

        if (!in_array($role, ['Admin', 'HR_Manager', 'Manager'])) {
            $this->jsonResponse('error', 'Bạn không có quyền phê duyệt', 403);
            return;
        }

        if ($requestId === 0 || !in_array($action, ['approve', 'reject'])) {
            $this->jsonResponse('error', 'Dữ liệu không hợp lệ', 400);
            return;
        }

        $status = ($action === 'approve') ? 'approved' : 'rejected';

        try {
            $this->model->updateStatus($requestId, $status, $managerId);
            $this->jsonResponse('success', "Yêu cầu đã được $status thành công.");
        } catch (Exception $e) {
            $this->jsonResponse('error', 'Lỗi Database: ' . $e->getMessage(), 500);
        }
    }

    /**
     * Helper function trả về JSON
     */
    private function jsonResponse(string $status, string $message, int $httpCode = 200): void
    {
        http_response_code($httpCode);
        header('Content-Type: application/json');
        echo json_encode(['status' => $status, 'message' => $message]);
    }
}
