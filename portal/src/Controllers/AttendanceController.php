<?php
declare(strict_types=1);

namespace Portal\Controllers;

use Portal\Core\View;

class AttendanceController
{
    public function matrix(): string
    {
        return View::render('attendance/matrix.twig');
    }
}