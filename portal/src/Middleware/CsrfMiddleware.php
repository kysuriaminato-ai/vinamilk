<?php
declare(strict_types=1);

namespace Portal\Middleware;

use Portal\Core\Session;

class CsrfMiddleware
{
    public function handle(): mixed
    {
        $method = $_SERVER['REQUEST_METHOD'] ?? 'GET';
        if (in_array($method, ['POST', 'PUT', 'DELETE'], true)) {
            $token = $_POST['_csrf_token'] ?? $_SERVER['HTTP_X_CSRF_TOKEN'] ?? null;
            if (!Session::validateCsrfToken($token)) {
                http_response_code(403);
                header('Content-Type: text/plain; charset=utf-8');
                echo '403 Forbidden: Mã CSRF token không hợp lệ hoặc đã hết hạn.';
                exit;
            }
        }
        return null;
    }
}