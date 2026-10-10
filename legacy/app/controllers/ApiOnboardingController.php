<?php

namespace App\Controllers;

use App\Models\OnboardingModel;
use App\Core\Validator;
use Exception;

class ApiOnboardingController
{
    private OnboardingModel $model;

    public function __construct()
    {
        $this->model = new OnboardingModel();
    }

    /**
     * POST /api-onboarding/process
     * Nhận dữ liệu ứng viên trúng tuyển từ Frontend
     */
    public function process(): void
    {
        header("Content-Type: application/json; charset=UTF-8");

        if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
            http_response_code(405);
            echo json_encode(['status' => 'error', 'message' => 'Chỉ chấp nhận phương thức POST']);
            return;
        }

        // 1. Kiểm tra tính hợp lệ của dữ liệu đầu vào (Form Validation)
        $validator = new Validator($_POST);
        
        // Cấu hình quy tắc kiểm tra
        $rules = [
            'name'          => 'required|min:3',
            'department_id' => 'required',
            'position'      => 'required',
            'salary'        => 'required'
        ];

        if (!$validator->validate($rules)) {
            http_response_code(400); // 400 Bad Request
            echo json_encode([
                'status' => 'error',
                'message' => 'Dữ liệu không hợp lệ',
                'errors' => $validator->getErrors()
            ]);
            return;
        }

        // 2. Thực thi Pipeline Onboarding
        try {
            $newEmployeeId = $this->model->processOnboarding($_POST);

            http_response_code(200);
            echo json_encode([
                'status' => 'success',
                'message' => 'Tiếp nhận nhân sự thành công!',
                'employee_id' => $newEmployeeId,
                'next_step' => 'Vui lòng thông báo nhân sự mới đăng nhập bằng ID: ' . $newEmployeeId
            ]);
        } catch (Exception $e) {
            http_response_code(500); // 500 Internal Server Error
            echo json_encode([
                'status' => 'error',
                'message' => 'Quá trình tiếp nhận bị lỗi do Database. Hệ thống đã tự động Rollback. Chi tiết: ' . $e->getMessage()
            ]);
        }
    }
}
