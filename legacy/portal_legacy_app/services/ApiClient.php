<?php

declare(strict_types=1);

namespace App\Services;

/**
 * =====================================================================
 * VINAMILK HRM - CẦU NỐI WEB API .NET (ApiClient)
 * =====================================================================
 * Giao tiếp HTTP RESTful với VinamilkHRM.WebApi (.NET Core):
 *   1. Quản lý access token (15m) & refresh token xoay vòng trong $_SESSION
 *   2. Tự động gọi POST /api/v1/auth/refresh khi nhận HTTP 401 Unauthorized
 *   3. Thu hồi token qua POST /api/v1/auth/revoke khi đăng xuất
 *   4. Đồng bộ quyền và profile người dùng từ GET /api/v1/me
 */
class ApiClient
{
    private string $baseUrl;

    public function __construct(?string $baseUrl = null)
    {
        if ($baseUrl !== null && $baseUrl !== '') {
            $this->baseUrl = rtrim($baseUrl, '/');
        } else {
            $this->baseUrl = rtrim($this->resolveBaseUrl(), '/');
        }
    }

    /**
     * Tự động nhận diện API_BASE_URL từ .env hoặc biến môi trường
     */
    private function resolveBaseUrl(): string
    {
        if (!empty($_ENV['API_BASE_URL'])) {
            return (string)$_ENV['API_BASE_URL'];
        }

        $envVar = getenv('API_BASE_URL');
        if ($envVar !== false && $envVar !== '') {
            return (string)$envVar;
        }

        $envPaths = [
            __DIR__ . '/../../.env',
            __DIR__ . '/../../../.env'
        ];

        foreach ($envPaths as $path) {
            if (file_exists($path)) {
                $lines = file($path, FILE_IGNORE_NEW_LINES | FILE_SKIP_EMPTY_LINES);
                if ($lines !== false) {
                    foreach ($lines as $line) {
                        $line = trim($line);
                        if (str_starts_with($line, '#')) continue;
                        if (str_starts_with($line, 'API_BASE_URL=')) {
                            $val = substr($line, strlen('API_BASE_URL='));
                            return trim($val, "\"' ");
                        }
                    }
                }
            }
        }

        return 'http://localhost:5000';
    }

    /**
     * Đăng nhập người dùng qua POST /api/v1/auth/login
     */
    public function login(string $username, string $password, ?string $twoFactorCode = null): array
    {
        $payload = [
            'username' => $username,
            'password' => $password,
        ];
        if ($twoFactorCode !== null && $twoFactorCode !== '') {
            $payload['twoFactorCode'] = $twoFactorCode;
        }

        $response = $this->requestRaw('POST', '/api/v1/auth/login', $payload, false);

        if ($response['statusCode'] === 200 && !empty($response['data']['succeeded'])) {
            if (session_status() === PHP_SESSION_NONE) {
                session_start();
            }

            $_SESSION['vnm_jwt'] = [
                'access_token' => $response['data']['accessToken'],
                'refresh_token' => $response['data']['refreshToken'],
                'expires_at' => $response['data']['expiresAt'] ?? null,
                'must_change_password' => $response['data']['mustChangePassword'] ?? false,
            ];

            // Tự động đồng bộ quyền hạn từ GET /api/v1/me
            $meResponse = $this->getMe();
            if ($meResponse !== null) {
                $_SESSION['vnm_user_profile'] = $meResponse;
                $_SESSION['vnm_user_permissions'] = $meResponse['permissions'] ?? [];
            }

            return [
                'status' => 'SUCCESS',
                'data' => $response['data'],
                'profile' => $meResponse,
                'message' => 'Đăng nhập thành công.'
            ];
        }

        if ($response['statusCode'] === 200 && !empty($response['data']['requiresTwoFactor'])) {
            return [
                'status' => 'REQUIRES_2FA',
                'message' => $response['data']['message'] ?? 'Yêu cầu mã xác thực 2 bước (TOTP 2FA).'
            ];
        }

        if ($response['statusCode'] === 423) {
            return [
                'status' => 'LOCKED',
                'message' => $response['data']['detail'] ?? 'Tài khoản đã bị tạm khóa do nhập sai mật khẩu quá 5 lần.'
            ];
        }

        return [
            'status' => 'FAILED',
            'statusCode' => $response['statusCode'],
            'message' => $response['data']['detail'] ?? ($response['data']['title'] ?? 'Tên đăng nhập hoặc mật khẩu không chính xác.')
        ];
    }

    /**
     * Tự động làm mới access token qua POST /api/v1/auth/refresh
     */
    public function refreshToken(): bool
    {
        if (session_status() === PHP_SESSION_NONE) {
            session_start();
        }

        $refreshToken = $_SESSION['vnm_jwt']['refresh_token'] ?? null;
        if (empty($refreshToken)) {
            return false;
        }

        $response = $this->requestRaw('POST', '/api/v1/auth/refresh', [
            'refreshToken' => $refreshToken
        ], false);

        if ($response['statusCode'] === 200 && !empty($response['data']['succeeded'])) {
            $_SESSION['vnm_jwt']['access_token'] = $response['data']['accessToken'];
            $_SESSION['vnm_jwt']['refresh_token'] = $response['data']['refreshToken'];
            $_SESSION['vnm_jwt']['expires_at'] = $response['data']['expiresAt'] ?? null;
            return true;
        }

        // Thu hồi phiên nếu refresh token hết hạn
        unset($_SESSION['vnm_jwt']);
        return false;
    }

    /**
     * Đăng xuất và thu hồi Refresh Token qua POST /api/v1/auth/revoke
     */
    public function revoke(): bool
    {
        if (session_status() === PHP_SESSION_NONE) {
            session_start();
        }

        $refreshToken = $_SESSION['vnm_jwt']['refresh_token'] ?? null;
        if (!empty($refreshToken)) {
            $this->request('POST', '/api/v1/auth/revoke', [
                'refreshToken' => $refreshToken
            ]);
        }

        unset($_SESSION['vnm_jwt'], $_SESSION['vnm_user_profile'], $_SESSION['vnm_user_permissions']);
        return true;
    }

    /**
     * Lấy thông tin cá nhân và claims quyền từ GET /api/v1/me
     */
    public function getMe(): ?array
    {
        $response = $this->request('GET', '/api/v1/me');
        if ($response['statusCode'] === 200 && is_array($response['data'])) {
            return $response['data'];
        }
        return null;
    }

    /**
     * Gửi yêu cầu HTTP kèm Authorization header và tự refresh khi gặp 401
     */
    public function request(string $method, string $endpoint, ?array $payload = null): array
    {
        $response = $this->requestRaw($method, $endpoint, $payload, true);

        // Tự động retry một lần khi nhận 401 Unauthorized
        if ($response['statusCode'] === 401) {
            if ($this->refreshToken()) {
                $response = $this->requestRaw($method, $endpoint, $payload, true);
            }
        }

        return $response;
    }

    /**
     * Thực thi cURL request tầng thấp
     */
    private function requestRaw(string $method, string $endpoint, ?array $payload = null, bool $withAuth = true): array
    {
        $url = $this->baseUrl . '/' . ltrim($endpoint, '/');
        $ch = curl_init($url);

        $headers = [
            'Accept: application/json',
            'Content-Type: application/json'
        ];

        if ($withAuth) {
            if (session_status() === PHP_SESSION_NONE) {
                session_start();
            }
            $accessToken = $_SESSION['vnm_jwt']['access_token'] ?? null;
            if (!empty($accessToken)) {
                $headers[] = 'Authorization: Bearer ' . $accessToken;
            }
        }

        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);
        curl_setopt($ch, CURLOPT_TIMEOUT, 15);

        $method = strtoupper($method);
        if ($method === 'POST') {
            curl_setopt($ch, CURLOPT_POST, true);
            if ($payload !== null) {
                curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($payload, JSON_THROW_ON_ERROR));
            }
        } elseif ($method === 'PUT' || $method === 'DELETE' || $method === 'PATCH') {
            curl_setopt($ch, CURLOPT_CUSTOMREQUEST, $method);
            if ($payload !== null) {
                curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($payload, JSON_THROW_ON_ERROR));
            }
        }

        $rawBody = curl_exec($ch);
        $httpCode = (int)curl_getinfo($ch, CURLINFO_HTTP_CODE);
        $curlError = curl_error($ch);
        curl_close($ch);

        if ($rawBody === false || $rawBody === '') {
            return [
                'statusCode' => $httpCode ?: 500,
                'data' => null,
                'error' => $curlError ?: 'Không nhận được phản hồi từ Web API .NET'
            ];
        }

        $decoded = json_decode((string)$rawBody, true);
        return [
            'statusCode' => $httpCode,
            'data' => $decoded,
            'error' => null
        ];
    }
}
