<?php
declare(strict_types=1);

namespace Portal\Controllers;

use Portal\Core\View;
use Portal\Services\ApiClient\VinamilkApiClient;

class AnalyticsController
{
    private VinamilkApiClient $api;

    public function __construct()
    {
        $this->api = new VinamilkApiClient();
    }

    public function market(): string
    {
        $payStance = $this->api->getPayStance();
        return View::render('analytics/market.twig', ['payStance' => $payStance]);
    }
}