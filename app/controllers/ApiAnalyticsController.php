<?php

namespace App\Controllers;

use App\Models\AnalyticsModel;

class ApiAnalyticsController
{
    private AnalyticsModel $model;

    public function __construct()
    {
        $this->model = new AnalyticsModel();
    }

    /**
     * GET /api-analytics/overview
     * Trả về toàn bộ dữ liệu tổng quan cho Dashboard
     */
    public function overview(): void
    {
        header("Access-Control-Allow-Origin: *");
        header("Content-Type: application/json; charset=UTF-8");

        try {
            // Lấy dữ liệu từ Model
            $headcount = $this->model->getHeadcountMetrics();
            $distribution = $this->model->getDepartmentDistribution();
            $turnoverRate = $this->model->getTurnoverRate();

            // Đóng gói JSON trả về cho Frontend
            http_response_code(200);
            echo json_encode([
                'status' => 'success',
                'data' => [
                    'headcount' => $headcount,
                    'department_distribution' => $distribution,
                    'turnover_rate_percent' => $turnoverRate,
                    'last_updated' => date('Y-m-d H:i:s')
                ]
            ]);

        } catch (\Exception $e) {
            http_response_code(500);
            echo json_encode([
                'status' => 'error',
                'message' => 'Lỗi hệ thống khi trích xuất báo cáo: ' . $e->getMessage()
            ]);
        }
    }
}
