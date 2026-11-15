<?php

namespace App\Controllers;

class ApiSecurityController
{
    /**
     * Cung cấp CSRF Token cho Frontend JS
     * Endpoint: GET /api-security/csrf-token
     */
    public function csrfToken(): void
    {
        if (session_status() === PHP_SESSION_NONE) {
            session_start();
        }

        // Tạo token ngẫu nhiên nếu chưa có trong session
        if (empty($_SESSION['csrf_token'])) {
            $_SESSION['csrf_token'] = bin2hex(random_bytes(32));
        }

        header("Access-Control-Allow-Origin: *");
        header("Content-Type: application/json; charset=UTF-8");

        echo json_encode([
            'status' => 'success',
            'csrf_token' => $_SESSION['csrf_token']
        ]);
    }
}
