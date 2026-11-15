<?php

namespace App\Controllers;

use App\Core\Database;

class ApiEssController
{
    private Database $db;

    public function __construct()
    {
        $this->db = Database::getInstance();
    }

    /**
     * Dữ liệu tổng hợp cho Dashboard (Chấm công, Phép, Phiếu lương)
     * GET /api-ess/dashboard-data
     */
    public function dashboardData(): void
    {
        // Middleware đã chặn nếu chưa đăng nhập, nên có thể yên tâm gọi $_SESSION
        $employeeId = $_SESSION['user_id'] ?? 'VNM-0001'; // Fallback để demo tạm

        header("Content-Type: application/json; charset=UTF-8");

        try {
            // 1. Lấy dữ liệu chấm công 5 ngày gần nhất
            // Giả định có bảng attendance_records(employee_id, check_in_time, status)
            $sqlAttendance = "SELECT check_in_time, check_out_time, status 
                              FROM attendance_records 
                              WHERE employee_id = :emp_id 
                              ORDER BY check_in_time DESC LIMIT 5";
            
            // Trong thực tế sẽ chạy dòng này: $attendance = $this->db->fetchAll($sqlAttendance, ['emp_id' => $employeeId]);
            // Vì chưa có table thật, tôi giả lập data cho Frontend:
            $attendance = [
                ['check_in_time' => date('Y-m-d 08:00:00'), 'status' => 'Đúng giờ'],
                ['check_in_time' => date('Y-m-d 08:05:00', strtotime('-1 days')), 'status' => 'Đi muộn'],
                ['check_in_time' => date('Y-m-d 07:55:00', strtotime('-2 days')), 'status' => 'Đúng giờ']
            ];

            // 2. Lấy danh sách phép đã duyệt (upcoming leaves)
            $sqlLeaves = "SELECT start_date, end_date, reason 
                          FROM leave_requests 
                          WHERE employee_id = :emp_id AND status = 'approved' AND start_date >= CURRENT_DATE
                          ORDER BY start_date ASC LIMIT 3";
            $leaves = $this->db->fetchAll($sqlLeaves, ['emp_id' => $employeeId]);

            // 3. Thông tin phiếu lương gần nhất
            // Giả lập Dữ liệu phiếu lương tháng trước
            $latestPayslip = [
                'month' => date('m/Y', strtotime('-1 month')),
                'net_salary' => '15,500,000 VND',
                'url' => '#/payslips/latest' // Link điều hướng Frontend
            ];

            http_response_code(200);
            echo json_encode([
                'status' => 'success',
                'data' => [
                    'attendance' => $attendance,
                    'leaves' => $leaves,
                    'payslip' => $latestPayslip
                ]
            ]);

        } catch (\Exception $e) {
            http_response_code(500);
            echo json_encode(['status' => 'error', 'message' => 'Lỗi DB: ' . $e->getMessage()]);
        }
    }
}
