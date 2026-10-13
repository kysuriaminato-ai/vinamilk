<?php
declare(strict_types=1);

namespace Portal\Controllers;

use Portal\Core\View;
use Portal\Core\Session;
use Portal\Services\ApiClient\VinamilkApiClient;

class AuthController
{
    private VinamilkApiClient $api;

    public function __construct()
    {
        $this->api = new VinamilkApiClient();
    }

    public function loginView(): string
    {
        Session::start();
        if (Session::get('user')) {
            header('Location: /dashboard/executive');
            exit;
        }
        return View::render('auth/login.twig');
    }

    public function login(): void
    {
        Session::start();
        $username = trim($_POST['username'] ?? '');
        $password = trim($_POST['password'] ?? '');

        $result = $this->api->login($username, $password);
        if (!empty($result['success'])) {
            Session::set('user', [
                'userId' => $result['userId'],
                'fullName' => $result['fullName'],
                'role' => $result['role']
            ]);
            Session::set('jwt_token', $result['token']);
            header('Location: /dashboard/executive');
            exit;
        }

        echo View::render('auth/login.twig', ['error' => $result['message'] ?? 'Đăng nhập không thành công']);
    }

    public function logout(): void
    {
        Session::destroy();
        header('Location: /login');
        exit;
    }
}