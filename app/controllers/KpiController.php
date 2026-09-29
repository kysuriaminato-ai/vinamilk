<?php

namespace App\Controllers;

use App\Models\KpiModel;

class KpiController
{
    private KpiModel $model;

    public function __construct()
    {
        $this->model = new KpiModel();
    }

    /**
     * Lấy điểm KPI của 1 nhân viên
     * GET /api/kpi?employee_id=VNM-0001
     */
    public function index()
    {
        $employeeId = $_GET['employee_id'] ?? null;

        if (!$employeeId) {
            http_response_code(400);
            echo json_encode(['status' => 'error', 'message' => 'Thiếu employee_id']);
            return;
        }

        $kpis = $this->model->getByEmployeeId($employeeId);
        
        header('Content-Type: application/json');
        echo json_encode([
            'status' => 'success',
            'data' => $kpis
        ]);
    }

    /**
     * Chấm điểm KPI mới (POST)
     */
    public function store()
    {
        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            http_response_code(405);
            return;
        }

        $data = [
            'employee_id' => $_POST['employee_id'] ?? '',
            'score' => $_POST['score'] ?? 0,
            'evaluation_date' => $_POST['evaluation_date'] ?? date('Y-m-d'),
            'reviewer_id' => $_POST['reviewer_id'] ?? '',
            'comments' => $_POST['comments'] ?? ''
        ];

        try {
            $this->model->create($data);
            echo json_encode(['status' => 'success', 'message' => 'Lưu điểm KPI thành công!']);
        } catch (\PDOException $e) {
            http_response_code(500);
            echo json_encode(['status' => 'error', 'message' => 'Lỗi DB: ' . $e->getMessage()]);
        }
    }
}
