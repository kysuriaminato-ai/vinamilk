<?php
declare(strict_types=1);

namespace Portal\Middleware;

use Portal\Core\Session;

class AuthMiddleware
{
    public function handle(): mixed
    {
        Session::start();
        $user = Session::get('user');
        if (!$user) {
            header('Location: /login');
            exit;
        }
        return null;
    }
}