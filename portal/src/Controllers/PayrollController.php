<?php
declare(strict_types=1);

namespace Portal\Controllers;

use Portal\Core\View;
use Portal\Services\ApiClient\VinamilkApiClient;

class PayrollController
{
    private VinamilkApiClient $api;

    public function __construct()
    {
        $this->api = new VinamilkApiClient();
    }

    public function index(): string
    {
        $page = max(1, (int)($_GET['page'] ?? 1));
        $pageSize = 25;
        $payroll = $this->api->getPayrollRows($page, $pageSize);
        return View::render('payroll/index.twig', ['payroll' => $payroll]);
    }
}