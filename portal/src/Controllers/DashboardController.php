<?php
declare(strict_types=1);

namespace Portal\Controllers;

use Portal\Core\View;
use Portal\Services\ApiClient\VinamilkApiClient;

class DashboardController
{
    private VinamilkApiClient $api;

    public function __construct()
    {
        $this->api = new VinamilkApiClient();
    }

    public function executive(): string
    {
        $data = $this->api->getExecutiveDashboard();
        return View::render('dashboard/executive.twig', ['data' => $data]);
    }

    public function unit(): string
    {
        $unitId = $_GET['unitId'] ?? 'UNIT-CB-01';
        $data = $this->api->getUnitDashboard($unitId);
        return View::render('dashboard/unit.twig', ['data' => $data]);
    }
}