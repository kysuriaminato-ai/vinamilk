<?php

namespace App\Controllers;

use App\Models\DepartmentModel;

class DepartmentController
{
    private DepartmentModel $model;

    public function __construct()
    {
        $this->model = new DepartmentModel();
    }

    /**
     * Hiển thị danh sách phòng ban (API JSON)
     */
    public function index()
    {
        $departments = $this->model->getAll();
        
        // Trả về dữ liệu dạng JSON cho Frontend
        header('Content-Type: application/json');
        echo json_encode([
            'status' => 'success',
            'data' => $departments
        ]);
    }

    /**
     * API Thêm mới phòng ban (POST request)
     */
    public function store()
    {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            http_response_code(405);
            echo json_encode(['status' => 'error', 'message' => 'Method Not Allowed']);
            return;
        }

        $data = [
            'id' => $_POST['id'] ?? '',
            'name' => $_POST['name'] ?? '',
            'parent_id' => !empty($_POST['parent_id']) ? $_POST['parent_id'] : null,
            'manager_id' => !empty($_POST['manager_id']) ? $_POST['manager_id'] : null,
        ];

        if (empty($data['id']) || empty($data['name'])) {
            http_response_code(400);
            echo json_encode(['status' => 'error', 'message' => 'Mã và Tên phòng ban không được để trống']);
            return;
        }

        try {
            $this->model->create($data);
            echo json_encode(['status' => 'success', 'message' => 'Thêm phòng ban thành công']);
        } catch (\PDOException $e) {
            http_response_code(500);
            echo json_encode(['status' => 'error', 'message' => 'Lỗi Database: ' . $e->getMessage()]);
        }
    }
}
