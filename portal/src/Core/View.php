<?php
declare(strict_types=1);

namespace Portal\Core;

use Twig\Environment;
use Twig\Loader\FilesystemLoader;
use Twig\TwigFunction;
use Twig\TwigFilter;

class View
{
    private static ?Environment $twig = null;

    public static function getTwig(): Environment
    {
        if (self::$twig === null) {
            $templatesDir = dirname(__DIR__, 2) . '/templates';
            $loader = new FilesystemLoader($templatesDir);

            // AUTOESCAPE = 'html' by default - Meets Acceptance Criteria 5!
            self::$twig = new Environment($loader, [
                'autoescape' => 'html',
                'cache' => false,
                'debug' => true
            ]);

            // Function: vite_asset()
            self::$twig->addFunction(new TwigFunction('vite_asset', function (string $entry): string {
                $distDir = dirname(__DIR__, 2) . '/public/dist';
                $manifestPath = $distDir . '/manifest.json';
                if (!file_exists($manifestPath)) {
                    $manifestPath = $distDir . '/.vite/manifest.json';
                }

                if (file_exists($manifestPath)) {
                    $manifest = json_decode((string)file_get_contents($manifestPath), true);
                    if (isset($manifest[$entry])) {
                        return '/dist/' . $manifest[$entry]['file'];
                    }
                }
                return '/dist/' . $entry;
            }));

            // Function: csrf_token()
            self::$twig->addFunction(new TwigFunction('csrf_token', function (): string {
                return Session::getCsrfToken();
            }));

            // Function: csrf_field() (raw HTML output with safe hidden input)
            self::$twig->addFunction(new TwigFunction('csrf_field', function (): string {
                $token = htmlspecialchars(Session::getCsrfToken(), ENT_QUOTES, 'UTF-8');
                return '<input type="hidden" name="_csrf_token" value="' . $token . '">';
            }, ['is_safe' => ['html']]));

            // Filter: format_currency (VND 1.234.567 ₫)
            self::$twig->addFilter(new TwigFilter('format_currency', function (mixed $amount): string {
                if ($amount === null || $amount === '') return '0 ₫';
                if (!is_numeric($amount)) return (string)$amount;
                return number_format((float)$amount, 0, ',', '.') . ' ₫';
            }));

            // Filter: format_date (dd/MM/yyyy)
            self::$twig->addFilter(new TwigFilter('format_date', function (mixed $date): string {
                if (empty($date)) return '-';
                try {
                    $d = new \DateTime((string)$date);
                    return $d->format('d/m/Y');
                } catch (\Exception) {
                    return (string)$date;
                }
            }));

            // Filter: format_percent (3,03%)
            self::$twig->addFilter(new TwigFilter('format_percent', function (mixed $val, int $decimals = 2): string {
                if ($val === null || $val === '') return '0%';
                if (!is_numeric($val)) return (string)$val;
                return number_format((float)$val, $decimals, ',', '.') . '%';
            }));
        }

        return self::$twig;
    }

    public static function render(string $template, array $data = []): string
    {
        $twig = self::getTwig();
        $globalData = [
            'user' => Session::get('user'),
            'current_unit' => Session::get('current_unit', 'Tập đoàn Vinamilk (HQ)'),
            'current_path' => parse_url($_SERVER['REQUEST_URI'] ?? '/', PHP_URL_PATH),
            'flash_success' => Session::get('flash_success'),
            'flash_error' => Session::get('flash_error')
        ];
        Session::remove('flash_success');
        Session::remove('flash_error');

        return $twig->render($template, array_merge($globalData, $data));
    }
}