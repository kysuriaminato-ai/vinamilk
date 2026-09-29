<?php
/**
 * =====================================================================
 * VINAMILK HRM - Core App & Front-Controller Dispatcher
 * =====================================================================
 * 
 * Lớp Router định tuyến URL thông minh của bộ khung MVC.
 * Hỗ trợ Route Aliases tiếng Việt / tiếng Anh linh hoạt.
 */

class App {
    protected string $controller = 'DashboardController';
    protected string $method = 'index';
    protected array $params = [];

    // Bảng Ánh xạ Routes (Route Aliases)
    protected array $routeAliases = [
        'nhansu' => 'EmployeeController',
        'employees' => 'EmployeeController',
        'tuyenchuyen' => 'TransferController',
        'transfers' => 'TransferController',
        'ktkl' => 'RewardDisciplineController',
        'rewards' => 'RewardDisciplineController',
        'auth' => 'AuthController',
        'dashboard' => 'DashboardController',
        'roles' => 'RoleController',
        'chamcong' => 'AttendanceController',
        'attendance' => 'AttendanceController',
        'luong' => 'PayrollController',
        'payroll' => 'PayrollController',
        'tuyendung' => 'RecruitmentController',
        'recruitment' => 'RecruitmentController',
        'daotao' => 'TrainingController',
        'training' => 'TrainingController',
        'ai' => 'AiExpertController',
        'baocao' => 'ReportController',
        'reports' => 'ReportController'
    ];

    public function __construct() {
        $url = $this->parseUrl();

        // 1. Kiểm tra Controller / Alias
        if (!empty($url[0])) {
            $rawRoute = strtolower($url[0]);
            
            if (isset($this->routeAliases[$rawRoute])) {
                $this->controller = $this->routeAliases[$rawRoute];
                unset($url[0]);
            } else {
                $controllerName = ucfirst($url[0]) . 'Controller';
                $controllerFile = APP_DIR . '/controllers/' . $controllerName . '.php';

                if (file_exists($controllerFile)) {
                    $this->controller = $controllerName;
                    unset($url[0]);
                } else {
                    $this->show404("Controller '{$controllerName}' không tồn tại!");
                    return;
                }
            }
        }

        // Require file Controller
        $controllerPath = APP_DIR . '/controllers/' . $this->controller . '.php';
        if (!file_exists($controllerPath)) {
            $this->show404("Không tìm thấy Controller '{$this->controller}'");
            return;
        }

        require_once $controllerPath;
        $this->controller = new $this->controller();

        // 2. Kiểm tra Action (Method)
        if (isset($url[1])) {
            if (method_exists($this->controller, $url[1])) {
                $this->method = $url[1];
                unset($url[1]);
            } else {
                $this->show404("Phương thức '{$url[1]}' không tồn tại trong " . get_class($this->controller));
                return;
            }
        }

        // 3. Lấy mảng tham số Parameters
        $this->params = $url ? array_values($url) : [];

        // 4. Thực thi Controller Action với Parameters
        call_user_func_array([$this->controller, $this->method], $this->params);
    }

    /**
     * Phân tích Request URI
     */
    private function parseUrl(): array {
        if (isset($_GET['url'])) {
            return explode('/', filter_var(rtrim($_GET['url'], '/'), FILTER_SANITIZE_URL));
        }

        $requestUri = $_SERVER['REQUEST_URI'] ?? '/';
        $scriptName = $_SERVER['SCRIPT_NAME'] ?? '';
        $baseDir = dirname($scriptName);

        if (str_starts_with($requestUri, $baseDir)) {
            $requestUri = substr($requestUri, strlen($baseDir));
        }

        $path = parse_url($requestUri, PHP_URL_PATH);
        $path = trim($path, '/');

        if (empty($path)) {
            return [];
        }

        return explode('/', filter_var($path, FILTER_SANITIZE_URL));
    }

    /**
     * Hiển thị trang 404 Not Found
     */
    private function show404(string $message = ''): void {
        http_response_code(404);
        $errorFile = APP_DIR . '/views/errors/404.php';
        if (file_exists($errorFile)) {
            require_once APP_DIR . '/core/Controller.php';
            $title = '404 - Trang không tồn tại';
            require_once $errorFile;
        } else {
            echo "<h1 style='color:#cc0000;'>404 Not Found</h1><p>{$message}</p>";
        }
        exit;
    }
}
