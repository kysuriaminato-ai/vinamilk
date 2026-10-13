<?php
declare(strict_types=1);

// Static file handling for PHP Built-in development server
if (php_sapi_name() === 'cli-server') {
    $path = parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);
    if ($path === '/public/index.php' || $path === '/index.php' || $path === '/public' || $path === '/public/') {
        header('Location: /');
        exit;
    }
    if ($path !== '/' && file_exists(__DIR__ . $path) && !is_dir(__DIR__ . $path)) {
        return false;
    }
}

require_once dirname(__DIR__) . '/vendor/autoload.php';

use Dotenv\Dotenv;
use Portal\Core\Router;
use Portal\Core\Session;
use Portal\Middleware\CsrfMiddleware;
use Portal\Middleware\AuthMiddleware;
use Portal\Middleware\RobotHeaderMiddleware;
use Portal\Controllers\AuthController;
use Portal\Controllers\CareersController;
use Portal\Controllers\DashboardController;
use Portal\Controllers\PayrollController;
use Portal\Controllers\AnalyticsController;
use Portal\Controllers\AttendanceController;
use Portal\Controllers\ApprovalController;
use Portal\Controllers\SelfServiceController;

// Load Environment variables if .env exists
if (file_exists(dirname(__DIR__) . '/.env')) {
    $dotenv = Dotenv::createImmutable(dirname(__DIR__));
    $dotenv->safeLoad();
}

Session::start();

// Router instance
$router = new Router();

// =========================================================================
// 1. PUBLIC ROUTES (Allowed indexing, SEO friendly, sitemap included)
// =========================================================================
$router->get('/tuyen-dung', [CareersController::class, 'index']);
$router->get('/tuyen-dung/{slug}-{id}', [CareersController::class, 'detail']);
$router->get('/sitemap.xml', [CareersController::class, 'sitemap']);

// 301 Redirects for Legacy URLs
$router->get('/tuyendung', [CareersController::class, 'legacyRedirect']);
$router->get('/tuyendung/{slug}', [CareersController::class, 'legacyRedirect']);

// Real User Monitoring (RUM) Telemetry Endpoint for Core Web Vitals
$router->post('/api/v1/telemetry/web-vitals', function () {
    $data = file_get_contents('php://input');
    // Log or store telemetry metric
    error_log('[RUM_WEB_VITALS] ' . $data);
    http_response_code(204);
    exit;
});

// Authentication Routes
$router->get('/login', [AuthController::class, 'loginView']);
$router->post('/login', [AuthController::class, 'login'], [CsrfMiddleware::class]);
$router->get('/logout', [AuthController::class, 'logout']);

// Root Redirect to Dashboard
$router->get('/', function () {
    header('Location: /dashboard/executive');
    exit;
});

// =========================================================================
// 2. PROTECTED INTERNAL ROUTES (Attached RobotHeaderMiddleware: X-Robots-Tag: noindex, nofollow)
// =========================================================================
$privateMw = [RobotHeaderMiddleware::class];

$router->get('/dashboard/executive', [DashboardController::class, 'executive'], $privateMw);
$router->get('/dashboard/unit', [DashboardController::class, 'unit'], $privateMw);
$router->get('/payroll', [PayrollController::class, 'index'], $privateMw);
$router->get('/analytics', [AnalyticsController::class, 'market'], $privateMw);
$router->get('/attendance/matrix', [AttendanceController::class, 'matrix'], $privateMw);
$router->get('/approvals', [ApprovalController::class, 'index'], $privateMw);
$router->get('/self-service', [SelfServiceController::class, 'index'], $privateMw);

$router->get('/employees', [DashboardController::class, 'executive'], $privateMw);
$router->get('/org', [DashboardController::class, 'executive'], $privateMw);
$router->get('/kpi/kanban', [DashboardController::class, 'executive'], $privateMw);
$router->get('/salary-review', [AnalyticsController::class, 'market'], $privateMw);
$router->get('/settings', [AnalyticsController::class, 'market'], $privateMw);

// Dispatch
$method = $_SERVER['REQUEST_METHOD'] ?? 'GET';
$uri = $_SERVER['REQUEST_URI'] ?? '/';

$response = $router->dispatch($method, $uri);
if (is_string($response)) {
    echo $response;
}