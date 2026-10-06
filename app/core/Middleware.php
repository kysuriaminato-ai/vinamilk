<?php

namespace App\Core;

/**
 * Interface Middleware
 * Hợp đồng bắt buộc cho mọi lớp Middleware trong hệ thống
 */
interface Middleware
{
    /**
     * Xử lý request trước khi đi vào Controller.
     * 
     * @param array $params Tham số cấu hình cho Middleware (nếu có)
     * @return bool Trả về true nếu cho phép đi tiếp, false nếu chặn lại
     */
    public function handle(array $params = []): bool;
}
