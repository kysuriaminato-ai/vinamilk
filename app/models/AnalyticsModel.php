<?php

namespace App\Models;

use App\Core\Database;

class AnalyticsModel
{
    private Database $db;

    public function __construct()
    {
        $this->db = Database::getInstance();
    }

    /**
     * Lấy thống kê tổng quan về Headcount (Nhân sự)
     */
    public function getHeadcountMetrics(): array
    {
        // 1. Tổng nhân sự đang hoạt động
        $sqlTotal = "SELECT COUNT(id) as total FROM employees WHERE status != 'Đã nghỉ việc'";
        $total = $this->db->fetch($sqlTotal)['total'] ?? 0;

        // 2. Nhân sự mới trong tháng này (dựa vào created_at)
        $sqlNew = "SELECT COUNT(id) as total_new FROM employees 
                   WHERE MONTH(created_at) = MONTH(CURRENT_DATE) 
                   AND YEAR(created_at) = YEAR(CURRENT_DATE)";
        $newHires = $this->db->fetch($sqlNew)['total_new'] ?? 0;

        // 3. Số nhân sự đang thử việc
        $sqlProbation = "SELECT COUNT(id) as probation FROM employees WHERE status = 'Thử việc'";
        $probation = $this->db->fetch($sqlProbation)['probation'] ?? 0;

        return [
            'total_active' => (int)$total,
            'new_hires_this_month' => (int)$newHires,
            'on_probation' => (int)$probation
        ];
    }

    /**
     * Lấy phân bổ nhân sự theo từng Phòng ban/Nhà máy
     * Rất hữu ích để vẽ biểu đồ Pie/Donut Chart
     */
    public function getDepartmentDistribution(): array
    {
        $sql = "SELECT d.name as department_name, COUNT(e.id) as employee_count
                FROM employees e
                LEFT JOIN departments d ON e.department_id = d.id
                WHERE e.status != 'Đã nghỉ việc'
                GROUP BY d.id, d.name
                ORDER BY employee_count DESC";
                
        return $this->db->fetchAll($sql);
    }

    /**
     * Tính toán tỉ lệ nghỉ việc (Turnover Rate) của tháng trước
     */
    public function getTurnoverRate(): float
    {
        // Giả lập logic tính Turnover Rate (Thực tế cần bảng termination_history)
        // Công thức: (Số người nghỉ trong tháng / Tổng headcount trung bình) * 100
        $sqlLeft = "SELECT COUNT(id) as left_count FROM employees 
                    WHERE status = 'Đã nghỉ việc' 
                    AND MONTH(updated_at) = MONTH(CURRENT_DATE - INTERVAL 1 MONTH)";
        $leftCount = $this->db->fetch($sqlLeft)['left_count'] ?? 0;

        $sqlTotal = "SELECT COUNT(id) as total FROM employees";
        $totalCount = $this->db->fetch($sqlTotal)['total'] ?? 1; // Tránh chia cho 0

        if ($totalCount == 0) return 0.0;

        $rate = ($leftCount / $totalCount) * 100;
        return round($rate, 2);
    }
}
