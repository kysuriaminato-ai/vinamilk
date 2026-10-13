<?php
declare(strict_types=1);

namespace Portal\Core;

class Router
{
    private array $routes = [];

    public function get(string $path, array|callable $handler, array $middlewares = []): void
    {
        $this->addRoute('GET', $path, $handler, $middlewares);
    }

    public function post(string $path, array|callable $handler, array $middlewares = []): void
    {
        $this->addRoute('POST', $path, $handler, $middlewares);
    }

    private function addRoute(string $method, string $path, array|callable $handler, array $middlewares): void
    {
        $pattern = preg_replace('/\{([a-zA-Z0-9_]+)\}/', '(?P<$1>[^/]+)', $path);
        $pattern = '#^' . $pattern . '$#';

        $this->routes[] = [
            'method' => $method,
            'path' => $path,
            'pattern' => $pattern,
            'handler' => $handler,
            'middlewares' => $middlewares
        ];
    }

    public function dispatch(string $method, string $uri): mixed
    {
        $path = parse_url($uri, PHP_URL_PATH) ?: '/';

        foreach ($this->routes as $route) {
            if ($route['method'] === $method && preg_match($route['pattern'], $path, $matches)) {
                // Execute Middlewares
                foreach ($route['middlewares'] as $middlewareClass) {
                    $mw = new $middlewareClass();
                    $result = $mw->handle();
                    if ($result !== null) {
                        return $result;
                    }
                }

                $params = array_filter($matches, 'is_string', ARRAY_FILTER_USE_KEY);
                $handler = $route['handler'];

                if (is_callable($handler)) {
                    return call_user_func_array($handler, $params);
                }

                if (is_array($handler) && count($handler) === 2) {
                    [$class, $methodName] = $handler;
                    $controller = new $class();
                    return call_user_func_array([$controller, $methodName], $params);
                }
            }
        }

        http_response_code(404);
        return View::render('errors/404.twig');
    }
}