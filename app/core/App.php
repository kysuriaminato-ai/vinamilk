<?php

namespace App\Core;

class App
{
    protected $controller = 'DashboardController';
    protected string $action = 'index';
    protected array $params = [];

    // Cấu hình Middleware cho từng Controller
    protected array $routeMiddlewares = [
        // Bảo vệ toàn bộ PayrollController
        'PayrollController' => [
            ['middleware' => \App\Middlewares\AuthMiddleware::class],
            ['middleware' => \App\Middlewares\RoleMiddleware::class, 'params' => ['allowed_roles' => ['Admin', 'HR_Manager']]]
        ],
        // Bảo vệ Bảng điều khiển Nhân viên (Employee Self-Service)
        'EmployeeDashboardController' => [
            ['middleware' => \App\Middlewares\AuthMiddleware::class],
            ['middleware' => \App\Middlewares\RoleMiddleware::class, 'params' => ['allowed_roles' => ['Employee']]]
        ],
        // Bảo vệ API Self-Service
        'ApiEssController' => [
            ['middleware' => \App\Middlewares\AuthMiddleware::class],
            ['middleware' => \App\Middlewares\RoleMiddleware::class, 'params' => ['allowed_roles' => ['Employee']]]
        ],
        // Bảo vệ luồng Tiếp nhận nhân sự (Onboarding)
        'ApiOnboardingController' => [
            ['middleware' => \App\Middlewares\AuthMiddleware::class],
            ['middleware' => \App\Middlewares\RoleMiddleware::class, 'params' => ['allowed_roles' => ['Admin', 'HR_Manager']]]
        ],
        // Bảo vệ Báo cáo Analytics Toàn cảnh
        'ApiAnalyticsController' => [
            ['middleware' => \App\Middlewares\AuthMiddleware::class],
            ['middleware' => \App\Middlewares\RoleMiddleware::class, 'params' => ['allowed_roles' => ['Admin', 'HR_Manager']]]
        ]
    ];

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

    public function __construct()
    {
        $url = $this->parseUrl();

        // 1. Resolve Controller
        $isNamespaced = true;
        if (!empty($url[0])) {
            $rawRoute = strtolower($url[0]);
            
            // Check Route Aliases first
            if (isset($this->routeAliases[$rawRoute])) {
                $controllerName = $this->routeAliases[$rawRoute];
                $potentialController = '\\App\\Controllers\\' . $controllerName;
                if (class_exists($potentialController)) {
                    $this->controller = $controllerName;
                    $isNamespaced = true;
                } else {
                    $this->controller = $controllerName;
                    $isNamespaced = false;
                }
                unset($url[0]);
            } else {
                $controllerName = str_replace(' ', '', ucwords(str_replace('-', ' ', $url[0])));
                $potentialController = '\\App\\Controllers\\' . $controllerName . 'Controller';
                $legacyController = $controllerName . 'Controller';
                
                if (class_exists($potentialController)) {
                    $this->controller = $controllerName . 'Controller';
                    $isNamespaced = true;
                    unset($url[0]);
                } elseif (class_exists($legacyController)) {
                    $this->controller = $controllerName . 'Controller';
                    $isNamespaced = false;
                    unset($url[0]);
                }
            }
        }

        if ($isNamespaced) {
            $namespacedClass = '\\App\\Controllers\\' . $this->controller;
            if (class_exists($namespacedClass)) {
                $controllerClass = $namespacedClass;
            } else {
                $controllerClass = $this->controller;
            }
        } else {
            $controllerClass = $this->controller;
        }

        // 2. Execute Middlewares for this Controller
        if (!$this->runMiddlewares($this->controller)) {
            // Dừng ngay lập tức nếu Middleware chặn lại (đã gửi HTTP Error)
            return; 
        }

        // 3. Resolve Action & Parameters
        $this->controller = new $controllerClass();

        if (isset($url[1])) {
            $actionName = lcfirst(str_replace(' ', '', ucwords(str_replace('-', ' ', $url[1]))));
            if (method_exists($this->controller, $actionName)) {
                $this->action = $actionName;
                unset($url[1]);
            } else {
                $this->sendNotFound("Action {$actionName} không tồn tại.");
                return;
            }
        }

        $this->params = $url ? array_values($url) : [];

        // 4. Dispatch
        call_user_func_array([$this->controller, $this->action], $this->params);
    }

    private function parseUrl(): array
    {
        if (isset($_GET['url'])) {
            return explode('/', filter_var(rtrim($_GET['url'], '/'), FILTER_SANITIZE_URL));
        }
        return [];
    }

    private function runMiddlewares(string $controllerName): bool
    {
        if (isset($this->routeMiddlewares[$controllerName])) {
            foreach ($this->routeMiddlewares[$controllerName] as $routeConfig) {
                $middlewareClass = $routeConfig['middleware'];
                $params = $routeConfig['params'] ?? [];

                /** @var Middleware $middleware */
                $middleware = new $middlewareClass();
                
                // Chạy handle, nếu trả về false thì break và dừng request
                if (!$middleware->handle($params)) {
                    return false;
                }
            }
        }
        return true;
    }

    private function sendNotFound(string $msg): void
    {
        http_response_code(404);
        header('Content-Type: application/json');
        echo json_encode(['status' => 'error', 'message' => "404 Not Found: " . $msg]);
    }
}
