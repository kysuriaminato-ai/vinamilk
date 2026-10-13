<?php
declare(strict_types=1);

namespace Portal\Middleware;

class RobotHeaderMiddleware
{
    public function handle(): mixed
    {
        // Gắn header bảo mật chống lập chỉ mục cho trang nội bộ và API
        if (!headers_sent()) {
            header('X-Robots-Tag: noindex, nofollow, noarchive, nosnippet', true);
        }
        return null;
    }
}