<?php

namespace App\Controllers;

use App\Models\EmployeeModel;

class ApiEmployeeController
{
    private EmployeeModel $model;

    public function __construct()
    {
        $this->model = new EmployeeModel();
    }

    /**
     * GET /api-employee/index
     * Endpoint API lấy danh sách toàn bộ cán bộ
     */
    public function index(): void
    {
        // Cho phép CORS nếu cần thiết (optional)
        header("Access-Control-Allow-Origin: *");
        header("Content-Type: application/json; charset=UTF-8");
        header("Access-Control-Allow-Methods: GET");
        header("Access-Control-Max-Age: 3600");
        header("Access-Control-Allow-Headers: Content-Type, Access-Control-Allow-Headers, Authorization, X-Requested-With");

        try {
            // Lấy tham số từ Query String
            $page = isset($_GET['page']) ? (int) $_GET['page'] : 1;
            $limit = isset($_GET['limit']) ? (int) $_GET['limit'] : 15; // Mặc định 15 dòng/trang
            $offset = ($page - 1) * $limit;

            $filters = [
                'search' => $_GET['search'] ?? '',
                'department' => $_GET['department'] ?? '',
                'status' => $_GET['status'] ?? ''
            ];

            // Lấy dữ liệu và tổng số bản ghi
            $employees = $this->model->search($filters, $limit, $offset);
            $totalRecords = $this->model->countTotal($filters);
            $totalPages = ceil($totalRecords / $limit);
            
            http_response_code(200);
            echo json_encode([
                'status' => 'success',
                'data' => $employees,
                'meta' => [
                    'current_page' => $page,
                    'per_page' => $limit,
                    'total_pages' => $totalPages,
                    'total_records' => $totalRecords
                ]
            ]);
        } catch (\Exception $e) {
            http_response_code(500);
            echo json_encode([
                'status' => 'error',
                'message' => 'Lỗi máy chủ khi lấy dữ liệu: ' . $e->getMessage()
            ]);
        }
    }
}
