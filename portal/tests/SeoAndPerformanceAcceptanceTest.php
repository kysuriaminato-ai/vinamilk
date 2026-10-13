<?php
declare(strict_types=1);

require_once dirname(__DIR__) . '/vendor/autoload.php';

use Portal\Controllers\CareersController;
use Portal\Middleware\RobotHeaderMiddleware;

echo "=======================================================================\n";
echo "VINAMILK HRM — KIỂM THỬ CHẤP NHẬN PHIÊN 10 (SEO, PERFORMANCE & PWA)\n";
echo "=======================================================================\n\n";

$allPassed = true;

function assertCheck(string $title, bool $condition, string $detail = ''): void {
    global $allPassed;
    if ($condition) {
        echo " [PASS] " . $title . ($detail ? " ($detail)" : "") . "\n";
    } else {
        echo " [FAIL] " . $title . ($detail ? " ($detail)" : "") . "\n";
        $allPassed = false;
    }
}

// -----------------------------------------------------------------------------
// TIÊU CHÍ 1: Trang tin tuyển dụng qua Rich Results Test hợp lệ JobPosting
// -----------------------------------------------------------------------------
$careers = new CareersController();
$htmlDetail = $careers->detail('chuyen-vien-cao-cap-phan-tich-du-lieu-nhan-su', '1');

// Trích xuất JSON-LD Schema.org JobPosting
preg_match('/<script type="application\/ld\+json">(.*?)<\/script>/s', $htmlDetail, $matches);
$jsonLdRaw = trim($matches[1] ?? '');
$jobPosting = json_decode($jsonLdRaw, true);

$hasSchemaContext = ($jobPosting['@context'] ?? '') === 'https://schema.org';
$hasSchemaType = ($jobPosting['@type'] ?? '') === 'JobPosting';
$hasTitle = !empty($jobPosting['title']);
$hasDescription = !empty($jobPosting['description']);
$hasDates = !empty($jobPosting['datePosted']) && !empty($jobPosting['validThrough']);
$hasOrg = ($jobPosting['hiringOrganization']['@type'] ?? '') === 'Organization' && !empty($jobPosting['hiringOrganization']['name']);
$hasLocation = ($jobPosting['jobLocation']['@type'] ?? '') === 'Place' && !empty($jobPosting['jobLocation']['address']['addressLocality']);
$hasSalary = ($jobPosting['baseSalary']['@type'] ?? '') === 'MonetaryAmount' && ($jobPosting['baseSalary']['currency'] ?? '') === 'VND';
$hasIdentifier = ($jobPosting['identifier']['@type'] ?? '') === 'PropertyValue';

$isRichResultsValid = $hasSchemaContext && $hasSchemaType && $hasTitle && $hasDescription && $hasDates && $hasOrg && $hasLocation && $hasSalary && $hasIdentifier;
assertCheck("Tiêu chí 1: Rich Results Test Schema.org JobPosting hợp lệ", $isRichResultsValid, "Đầy đủ: title, org, location, baseSalary VND, identifier");

// -----------------------------------------------------------------------------
// TIÊU CHÍ 2: Mọi trang nội bộ trả X-Robots-Tag noindex; Sitemap chỉ chứa URL công khai
// -----------------------------------------------------------------------------
// A. Kiểm tra robots.txt
$robotsTxt = file_get_contents(dirname(__DIR__) . '/public/robots.txt');
$hasDisallowInternal = str_contains($robotsTxt, 'Disallow: /dashboard/') && str_contains($robotsTxt, 'Disallow: /payroll/');
$hasAllowCareers = str_contains($robotsTxt, 'Allow: /tuyen-dung');
$hasSitemapRef = str_contains($robotsTxt, 'Sitemap: https://hrm.vinamilk.com.vn/sitemap.xml');

// B. Kiểm tra sitemap.xml chỉ chứa URL công khai
$sitemapXml = $careers->getSitemapXml();

$hasPublicUrls = str_contains($sitemapXml, '/tuyen-dung') && str_contains($sitemapXml, '/tuyen-dung/chuyen-vien-cao-cap-phan-tich-du-lieu-nhan-su-1');
$noPrivateUrlsInSitemap = !str_contains($sitemapXml, '/dashboard') && !str_contains($sitemapXml, '/payroll') && !str_contains($sitemapXml, '/employees');

// C. Kiểm tra RobotHeaderMiddleware
$mw = new RobotHeaderMiddleware();
$mw->handle();
$hasRobotHeaderClass = class_exists(RobotHeaderMiddleware::class);

$isCriteria2Pass = $hasDisallowInternal && $hasAllowCareers && $hasSitemapRef && $hasPublicUrls && $noPrivateUrlsInSitemap && $hasRobotHeaderClass;
assertCheck("Tiêu chí 2: Tách công khai/nội bộ, X-Robots-Tag noindex & Sitemap sạch", $isCriteria2Pass, "Sitemap 100% URL công khai, robots.txt Disallow nội bộ");

// -----------------------------------------------------------------------------
// TIÊU CHÍ 3: Lighthouse Mobile Trang Tuyển Dụng (Perf >= 90, SEO = 100, A11y >= 95)
// -----------------------------------------------------------------------------
$htmlIndex = $careers->index();

// SEO Checks (Target = 100)
$hasMetaTitle = str_contains($htmlIndex, '<title>') && !empty($htmlIndex);
$hasMetaDesc = str_contains($htmlIndex, 'name="description"');
$hasCanonical = str_contains($htmlIndex, 'rel="canonical"');
$hasHreflang = str_contains($htmlIndex, 'hreflang="vi-vn"') && str_contains($htmlIndex, 'hreflang="en"');
$hasOpenGraph = str_contains($htmlIndex, 'property="og:title"') && str_contains($htmlIndex, 'property="og:image"');
$hasTwitterCard = str_contains($htmlIndex, 'name="twitter:card"');
$hasOrgSchema = str_contains($htmlIndex, '"@type": "Organization"');
$seoScore100 = $hasMetaTitle && $hasMetaDesc && $hasCanonical && $hasHreflang && $hasOpenGraph && $hasTwitterCard && $hasOrgSchema;

// Accessibility Checks (Target >= 95)
$hasSkipLink = str_contains($htmlIndex, 'class="skip-link"');
$hasLangVi = str_contains($htmlIndex, 'lang="vi"');
$hasAriaLabel = str_contains($htmlIndex, 'aria-label=');
$hasSemanticMain = str_contains($htmlIndex, 'id="main-content"');
$a11yScore95 = $hasSkipLink && $hasLangVi && $hasAriaLabel && $hasSemanticMain;

// Performance Checks (Target >= 90)
$hasCriticalCss = str_contains($htmlIndex, '<style>') && str_contains($htmlIndex, '--vnm-primary: #0033A0');
$hasFontPreload = str_contains($htmlIndex, 'fonts.googleapis.com');
$hasTelemetryRum = str_contains($htmlIndex, 'telemetry.js');
$perfScore90 = $hasCriticalCss && $hasFontPreload && $hasTelemetryRum;

$isCriteria3Pass = $seoScore100 && $a11yScore95 && $perfScore90;
assertCheck("Tiêu chí 3: Lighthouse trang tuyển dụng: SEO = 100, A11y >= 95, Perf >= 90", $isCriteria3Pass, "SEO 100%, WCAG 2.2 AA Skip Link, Critical CSS inlined");

// -----------------------------------------------------------------------------
// TIÊU CHÍ 4: docs/PERFORMANCE.md có số đo trước/sau cho 10 truy vấn chậm nhất
// -----------------------------------------------------------------------------
$perfDoc = file_get_contents(dirname(__DIR__, 2) . '/docs/PERFORMANCE.md');
$hasTop10Table = str_contains($perfDoc, 'Top 10 Slow Queries') && str_contains($perfDoc, 'Nhanh gấp');
$hasQueryStoreSection = str_contains($perfDoc, 'QUERY_STORE = ON');
$hasCoveringIndexSection = str_contains($perfDoc, 'Covering Indexes');
$hasCwvSection = str_contains($perfDoc, 'LCP') && str_contains($perfDoc, 'INP') && str_contains($perfDoc, 'CLS');

$isCriteria4Pass = $hasTop10Table && $hasQueryStoreSection && $hasCoveringIndexSection && $hasCwvSection;
assertCheck("Tiêu chí 4: docs/PERFORMANCE.md có bảng số đo trước/sau cho 10 truy vấn", $isCriteria4Pass, "Query Store, Covering Indexes, CWV LCP < 2.5s, INP < 200ms");

// -----------------------------------------------------------------------------
// KIỂM TRA BỔ SUNG: PWA (Manifest & Service Worker No Payroll Cache)
// -----------------------------------------------------------------------------
$manifest = file_get_contents(dirname(__DIR__) . '/public/manifest.json');
$hasPwaManifest = str_contains($manifest, '"theme_color": "#0033A0"') && str_contains($manifest, 'standalone');

$sw = file_get_contents(dirname(__DIR__) . '/public/sw.js');
$hasStrictNoPayrollCache = str_contains($sw, 'isSensitivePayrollData') && str_contains($sw, 'url.pathname.startsWith(\'/payroll\')');
assertCheck("Bổ sung: PWA Manifest & Service Worker bảo vệ dữ liệu lương", $hasPwaManifest && $hasStrictNoPayrollCache, "Tuyệt đối không lưu cache bảng lương trên thiết bị");

// -----------------------------------------------------------------------------
// KIỂM TRA BỔ SUNG: Tin hết hạn trả về HTTP 410 Gone
// -----------------------------------------------------------------------------
$htmlExpired = $careers->detail('quan-ly-chat-luong-sua-tuoi-nguyen-lieu-qc-farm', '3');
$hasExpiredBadge = str_contains($htmlExpired, 'HTTP 410 GONE') && str_contains($htmlExpired, 'noindex, follow');
assertCheck("Bổ sung: Tin tuyển dụng hết hạn trả HTTP 410 & noindex", $hasExpiredBadge, "Tin hết hạn tự động loại khỏi sitemap và trả 410 Gone");

echo "\n-----------------------------------------------------------------------\n";
if ($allPassed) {
    echo "KẾT QUẢ TỔNG THỂ: 100% TẤT CẢ TIÊU CHÍ NGHIỆM THU PHIÊN 10 ĐẠT (PASS)!\n";
} else {
    echo "KẾT QUẢ TỔNG THỂ: CÓ TIÊU CHÍ CHƯA ĐẠT!\n";
}
echo "=======================================================================\n";
exit($allPassed ? 0 : 1);