<?php
/**
 * =====================================================================
 * VINAMILK HRM - BỘ ĐIỀU HƯỚNG TẬP TRUNG (FRONT CONTROLLER & ROUTER)
 * =====================================================================
 */

class App
{
    protected string $controller = 'DashboardController';
    protected string $action = 'index';
    protected array $params = [];

    // Bảng định danh tên đường dẫn thân thiện (Route Aliases tiếng Việt & tiếng Anh)
    protected array $routeAliases = [
        'auth'        => 'AuthController',
        'login'       => 'AuthController',
        'logout'      => 'AuthController',
        'dashboard'   => 'DashboardController',
        'nhansu'      => 'EmployeeController',
        'employees'   => 'EmployeeController',
        'luong'       => 'PayrollController',
        'payroll'     => 'PayrollController',
        'approvals'   => 'ApprovalController',
        'pheduyet'    => 'ApprovalController',
        'don-tu'      => 'ApprovalController',
        'audit'       => 'AuditLogController',
        'kiemtoan'    => 'AuditLogController',
        'donvi'       => 'OrgUnitController',
        'cautruc'     => 'OrgUnitController',
        'chamcong'    => 'AttendanceController',
        'attendance'  => 'AttendanceController',
        'kitchen-sink'=> 'KitchenSinkController',
        'kitchen_sink'=> 'KitchenSinkController',
        'ai'          => 'AiExpertController',
        'ai-expert'   => 'AiExpertController',
        'ai_expert'   => 'AiExpertController',
        'certificates'=> 'CertificateController',
        'chung-chi'   => 'CertificateController',
        'chungchi'    => 'CertificateController',
        'assets'      => 'AssetController',
        'tai-san'     => 'AssetController',
        'taisan'      => 'AssetController',
        'offboarding' => 'OffboardingController',
        'thoi-viec'   => 'OffboardingController',
        'thoiviec'    => 'OffboardingController'
    ];

    public function __construct()
    {
        $url = $this->parseUrl();

        // 1. Phân giải Controller
        if (!empty($url[0])) {
            $rawRoute = strtolower($url[0]);
            if ($rawRoute === 'kpi-dictionary' || $rawRoute === 'kpi_dictionary') {
                $this->controller = 'DashboardController';
                $this->action = 'kpi_dictionary';
                unset($url[0]);
            } elseif (isset($this->routeAliases[$rawRoute])) {
                $this->controller = $this->routeAliases[$rawRoute];
                unset($url[0]);
            } else {
                $candidate = str_replace(' ', '', ucwords(str_replace(['-', '_'], ' ', $url[0]))) . 'Controller';
                if (file_exists(APP_DIR . '/controllers/' . $candidate . '.php')) {
                    $this->controller = $candidate;
                    unset($url[0]);
                }
            }
        }

        // Kiểm tra file controller
        $controllerFile = APP_DIR . '/controllers/' . $this->controller . '.php';
        if (!file_exists($controllerFile)) {
            $this->handleNotFound("Không tìm thấy Controller [{$this->controller}]");
            return;
        }

        require_once $controllerFile;
        if (!class_exists($this->controller)) {
            $this->handleNotFound("Class [{$this->controller}] chưa được khai báo");
            return;
        }

        $controllerInstance = new $this->controller();

        // 2. Phân giải Action (Phương thức xử lý)
        if (!empty($url[1])) {
            $actionCandidate = str_replace('-', '_', $url[1]);
            if (method_exists($controllerInstance, $actionCandidate)) {
                $this->action = $actionCandidate;
                unset($url[1]);
            } elseif (method_exists($controllerInstance, $url[1])) {
                $this->action = $url[1];
                unset($url[1]);
            }
        }

        // 3. Phân giải Params
        $this->params = $url ? array_values($url) : [];

        // 4. Thực thi Action với các Params
        try {
            call_user_func_array([$controllerInstance, $this->action], $this->params);
        } catch (Throwable $e) {
            $this->handleException($e);
        }
    }

    /**
     * Phân tách URL từ Request
     */
    protected function parseUrl(): array
    {
        if (isset($_GET['url']) && !empty($_GET['url'])) {
            return explode('/', filter_var(rtrim($_GET['url'], '/'), FILTER_SANITIZE_URL));
        }

        // Phân tích từ REQUEST_URI nếu web server dùng mod_rewrite
        $uri = parse_url($_SERVER['REQUEST_URI'] ?? '', PHP_URL_PATH);
        if (php_sapi_name() !== 'cli-server') {
            $scriptDir = str_replace('\\', '/', dirname($_SERVER['SCRIPT_NAME'] ?? ''));
            if ($scriptDir !== '/' && !empty($scriptDir) && str_starts_with($uri, $scriptDir)) {
                $uri = substr($uri, strlen($scriptDir));
            }
        }

        $uri = trim($uri, '/');
        if (str_starts_with($uri, 'public/')) {
            $uri = substr($uri, 7);
        }

        if (!empty($uri)) {
            return explode('/', filter_var($uri, FILTER_SANITIZE_URL));
        }

        return [];
    }

    protected function handleNotFound(string $msg): void
    {
        http_response_code(404);
        echo '<div style="font-family:sans-serif;margin:40px auto;max-width:600px;padding:30px;background:#fff;border-radius:8px;box-shadow:0 4px 12px rgba(0,0,0,0.1);border-top:4px solid #002F6C;">' .
             '<h2 style="color:#002F6C;margin-top:0;">404 - Trang không tồn tại</h2>' .
             '<p style="color:#666;">Yêu cầu của bạn không tìm thấy trang hoặc hành động phù hợp trên hệ thống Vinamilk HRM.</p>' .
             '<small style="color:#999;">' . htmlspecialchars($msg, ENT_QUOTES, 'UTF-8') . '</small><br><br>' .
             '<a href="' . (defined('BASE_URL') ? BASE_URL : '/') . '" style="display:inline-block;padding:8px 16px;background:#002F6C;color:#fff;text-decoration:none;border-radius:4px;font-weight:600;">Về Trang chủ</a>' .
             '</div>';
        exit;
    }

    protected function handleException(Throwable $e): void
    {
        http_response_code(500);
        error_log('[Vinamilk App Exception] ' . $e->getMessage() . "\n" . $e->getTraceAsString());
        echo '<div style="font-family:sans-serif;margin:40px auto;max-width:700px;padding:30px;background:#fff;border-radius:8px;box-shadow:0 4px 12px rgba(0,0,0,0.1);border-top:4px solid #dc2626;">' .
             '<h2 style="color:#dc2626;margin-top:0;">500 - Lỗi xử lý hệ thống Vinamilk HRM</h2>' .
             '<p style="color:#444;">Đã xảy ra lỗi trong quá trình thực thi yêu cầu của bạn.</p>' .
             '<div style="background:#fef2f2;padding:12px;border-radius:4px;color:#991b1b;font-family:monospace;font-size:13px;">' .
             htmlspecialchars($e->getMessage(), ENT_QUOTES, 'UTF-8') .
             '</div><br>' .
             '<small style="color:#888;">File: ' . htmlspecialchars($e->getFile(), ENT_QUOTES, 'UTF-8') . ' | Line: ' . $e->getLine() . '</small><br><br>' .
             '<a href="' . (defined('BASE_URL') ? BASE_URL : '/') . '" style="display:inline-block;padding:8px 16px;background:#002F6C;color:#fff;text-decoration:none;border-radius:4px;font-weight:600;">Về Bảng điều khiển</a>' .
             '</div>';
        exit;
    }
}
