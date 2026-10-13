<?php
declare(strict_types=1);

namespace Portal\Controllers;

use Portal\Core\View;

class ApprovalController
{
    public function index(): string
    {
        return View::render('approvals/index.twig');
    }
}