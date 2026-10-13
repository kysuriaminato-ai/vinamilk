<?php
declare(strict_types=1);

namespace Portal\Controllers;

use Portal\Core\View;

class SelfServiceController
{
    public function index(): string
    {
        return View::render('self_service/index.twig');
    }
}