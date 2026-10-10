<?php

namespace App\Models;

use App\Core\Database;
use Exception;
use PDOException;

class OnboardingModel
{
    private Database $db;

    public function __construct()
    {
        $this->db = Database::getInstance();
    }

    /**
     * Pipeline tạo tài khoản nhân viên mới 1-Click
     * Tự động tạo: Hồ sơ nhân viên, Hợp đồng thử việc, Mục tiêu KPI ban đầu
     * Đảm bảo tính toàn vẹn dữ liệu bằng Database Transaction
     */
    public function processOnboarding(array $candidateData): ?string
    {
        try {
            // Khởi tạo Transaction (Khóa database để thực thi chuỗi lệnh)
            $this->db->beginTransaction();

            // 1. Tạo ID nhân viên mới (Giả lập logic sinh mã tự động)
            $employeeId = 'VNM-' . str_pad((string)rand(1, 9999), 4, '0', STR_PAD_LEFT);

            // 2. INSERT vào bảng employees
            $sqlEmp = "INSERT INTO employees (id, name, gender, birth_date, department_id, position, status) 
                       VALUES (:id, :name, :gender, :birth_date, :department_id, :position, 'Thử việc')";
            
            $this->db->execute($sqlEmp, [
                'id'            => $employeeId,
                'name'          => $candidateData['name'],
                'gender'        => $candidateData['gender'] ?? 'Chưa rõ',
                'birth_date'    => $candidateData['birth_date'] ?? null,
                'department_id' => $candidateData['department_id'],
                'position'      => $candidateData['position']
            ]);

            // 3. INSERT vào bảng contracts (Tạo hợp đồng thử việc 2 tháng)
            // Lưu ý: Cần có bảng contracts trong CSDL thực tế
            $sqlContract = "INSERT INTO contracts (employee_id, contract_type, start_date, end_date, basic_salary, status) 
                            VALUES (:emp_id, 'Thử việc 2 tháng', CURRENT_DATE, DATE_ADD(CURRENT_DATE, INTERVAL 2 MONTH), :salary, 'Active')";
            
            $this->db->execute($sqlContract, [
                'emp_id' => $employeeId,
                'salary' => $candidateData['salary']
            ]);

            // 4. INSERT vào bảng kpi_metrics (Gán KPI 85% mặc định cho vòng thử việc)
            $sqlKpi = "INSERT INTO kpi_metrics (employee_id, score, evaluation_date, reviewer_id, comments) 
                       VALUES (:emp_id, 85.00, DATE_ADD(CURRENT_DATE, INTERVAL 2 MONTH), 'HR-SYSTEM', 'Mục tiêu đánh giá năng lực thử việc tự động')";
            
            $this->db->execute($sqlKpi, [
                'emp_id' => $employeeId
            ]);

            // Nếu cả 3 lệnh trên đều thành công, lưu vĩnh viễn vào Database
            $this->db->commit();

            return $employeeId;

        } catch (PDOException $e) {
            // Nếu có LỖI BẤT KỲ ở 3 lệnh trên, Rollback toàn bộ để tránh rác dữ liệu
            $this->db->rollBack();
            
            // Log lỗi vào file (thực tế) và ném ra Exception
            throw new Exception("Lỗi Onboarding Pipeline: " . $e->getMessage());
        }
    }
}
