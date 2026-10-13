<?php
declare(strict_types=1);

namespace Portal\Controllers;

use Portal\Core\View;

class CareersController
{
    private array $jobs = [
        [
            'id' => 1,
            'jobCode' => 'VNM-REC-2026-088',
            'title' => 'Chuyên Viên Cao Cấp Phân Tích Dữ Liệu Nhân Sự (People Analytics)',
            'slug' => 'chuyen-vien-cao-cap-phan-tich-du-lieu-nhan-su',
            'department' => 'Ban Quản Trị Nhân Sự Tập Đoàn',
            'workLocation' => 'Tòa nhà Vinamilk, Số 10 Tân Trào, P. Tân Phú, Quận 7',
            'city' => 'Hồ Chí Minh',
            'employmentType' => 'FULL_TIME',
            'salaryMin' => 25000000,
            'salaryMax' => 42000000,
            'salaryDisplay' => '25.000.000 ₫ - 42.000.000 ₫',
            'datePosted' => '2026-10-01T08:00:00+07:00',
            'validThrough' => '2026-11-30T23:59:59+07:00',
            'isActive' => true,
            'isExpired' => false,
            'description' => '<p>Chịu trách nhiệm xây dựng mô hình dự báo biến động nhân sự, phân tích vị thế thị trường và trực quan hóa Dashboard điều hành People Analytics cho Hội đồng Quản trị và Ban Tổng Giám Đốc.</p>',
            'requirements' => '<ul><li>Tối thiểu 3 năm kinh nghiệm trong lĩnh vực HR Data / People Analytics.</li><li>Thành thạo SQL, Python/R, Power BI hoặc Tableau.</li><li>Am hiểu thị trường lao động Việt Nam và các chỉ số C&B.</li></ul>',
            'benefits' => '<ul><li>Lương thưởng cạnh tranh P75 thị trường, tháng lương 13 + thưởng KPI hàng năm.</li><li>Bảo hiểm sức khỏe toàn diện Vinamilk Care cho bản thân và gia đình.</li><li>Chế độ dùng sản phẩm sữa Vinamilk hàng tháng.</li></ul>'
        ],
        [
            'id' => 2,
            'jobCode' => 'VNM-REC-2026-089',
            'title' => 'Kỹ Sư Tự Động Hóa Vận Hành Dây Chuyền Chiết Rót',
            'slug' => 'ky-su-tu-dong-hoa-van-hanh-day-chuyen-chiet-rot',
            'department' => 'Khối Sản Xuất — Nhà Máy Sữa Thống Nhất',
            'workLocation' => 'Nhà máy Sữa Thống Nhất, KCN Trường Thọ, TP. Thủ Đức',
            'city' => 'Hồ Chí Minh',
            'employmentType' => 'FULL_TIME',
            'salaryMin' => 18000000,
            'salaryMax' => 28000000,
            'salaryDisplay' => '18.000.000 ₫ - 28.000.000 ₫',
            'datePosted' => '2026-10-05T08:00:00+07:00',
            'validThrough' => '2026-11-20T23:59:59+07:00',
            'isActive' => true,
            'isExpired' => false,
            'description' => '<p>Vận hành, bảo trì và lập trình PLC cho hệ thống chiết rót tiệt trùng Tetra Pak chuẩn tự động hóa cao nhất của Vinamilk.</p>',
            'requirements' => '<ul><li>Tốt nghiệp Đại học chuyên ngành Cơ điện tử, Tự động hóa.</li><li>Kinh nghiệm vận hành dây chuyền FMCG từ 2 năm.</li></ul>',
            'benefits' => '<ul><li>Phụ cấp ca, phụ cấp sữa, cơm trưa tại nhà máy.</li><li>Cơ hội đào tạo chuyên sâu tại Thụy Điển và Đức.</li></ul>'
        ],
        [
            'id' => 3,
            'jobCode' => 'VNM-REC-2026-065',
            'title' => 'Quản Lý Chất Lượng Sữa Tươi Nguyên Liệu (QC Farm)',
            'slug' => 'quan-ly-chat-luong-sua-tuoi-nguyen-lieu-qc-farm',
            'department' => 'Khối Phát Triển Vùng Nguyên Liệu',
            'workLocation' => 'Trang trại Bò sữa Vinamilk Tây Ninh, Xã Long Khánh, Huyện Bến Cầu',
            'city' => 'Tây Ninh',
            'employmentType' => 'FULL_TIME',
            'salaryMin' => 16000000,
            'salaryMax' => 24000000,
            'salaryDisplay' => '16.000.000 ₫ - 24.000.000 ₫',
            'datePosted' => '2026-08-01T08:00:00+07:00',
            'validThrough' => '2026-09-15T23:59:59+07:00', // ĐÃ HẾT HẠN
            'isActive' => false,
            'isExpired' => true,
            'description' => '<p>Kiểm soát chỉ tiêu vi sinh, độ đạm, béo của sữa tươi nguyên liệu trước khi xuất sang nhà máy chế biến.</p>',
            'requirements' => '<ul><li>Tốt nghiệp Công nghệ Thực phẩm, Chăn nuôi Thú y.</li></ul>',
            'benefits' => '<ul><li>Nhà ở lưu trú trang trại chuẩn resort sinh thái, đầy đủ tiện nghi.</li></ul>'
        ]
    ];

    public function index(): string
    {
        if (!headers_sent()) {
            @header_remove('X-Robots-Tag');
        }

        $activeJobs = array_filter($this->jobs, fn($j) => $j['isActive'] && !$j['isExpired']);
        return View::render('careers/index.twig', [
            'jobs' => $activeJobs,
            'canonicalUrl' => 'https://hrm.vinamilk.com.vn/tuyen-dung',
            'pageTitle' => 'Cơ Hội Nghề Nghiệp Tại Tập Đoàn Vinamilk — Tuyển Dụng 2026',
            'metaDescription' => 'Khám phá các cơ hội nghề nghiệp hấp dẫn tại Vinamilk - Thương hiệu sữa số 1 Việt Nam. Chế độ đãi ngộ hàng đầu, môi trường làm việc chuẩn quốc tế.'
        ]);
    }

    public function detail(string $slug, string $id): string
    {
        $jobId = (int)$id;
        $job = null;
        foreach ($this->jobs as $j) {
            if ($j['id'] === $jobId) {
                $job = $j;
                break;
            }
        }

        if (!$job) {
            http_response_code(404);
            return View::render('errors/404.twig');
        }

        // Xử lý tin hết hạn: trả về HTTP 410 Gone và gắn noindex, follow!
        if ($job['isExpired'] || !$job['isActive']) {
            http_response_code(410);
            if (!headers_sent()) {
                header('X-Robots-Tag: noindex, follow', true);
            }
            $activeJobs = array_filter($this->jobs, fn($j) => $j['isActive'] && !$j['isExpired']);
            return View::render('careers/expired.twig', [
                'job' => $job,
                'similarJobs' => $activeJobs,
                'canonicalUrl' => 'https://hrm.vinamilk.com.vn/tuyen-dung/' . $job['slug'] . '-' . $job['id']
            ]);
        }

        if (!headers_sent()) {
            @header_remove('X-Robots-Tag');
        }

        $schemaJobPosting = [
            '@context' => 'https://schema.org',
            '@type' => 'JobPosting',
            'title' => $job['title'],
            'description' => $job['description'] . '<h3>Yêu cầu công việc</h3>' . $job['requirements'] . '<h3>Quyền lợi được hưởng</h3>' . $job['benefits'],
            'datePosted' => $job['datePosted'],
            'validThrough' => $job['validThrough'],
            'employmentType' => $job['employmentType'],
            'hiringOrganization' => [
                '@type' => 'Organization',
                'name' => 'Công Ty Cổ Phần Sữa Việt Nam (Vinamilk)',
                'sameAs' => 'https://www.vinamilk.com.vn',
                'logo' => 'https://hrm.vinamilk.com.vn/dist/assets/vinamilk-logo.png'
            ],
            'jobLocation' => [
                '@type' => 'Place',
                'address' => [
                    '@type' => 'PostalAddress',
                    'streetAddress' => $job['workLocation'],
                    'addressLocality' => $job['city'],
                    'addressRegion' => $job['city'],
                    'postalCode' => '700000',
                    'addressCountry' => 'VN'
                ]
            ],
            'baseSalary' => [
                '@type' => 'MonetaryAmount',
                'currency' => 'VND',
                'value' => [
                    '@type' => 'QuantitativeValue',
                    'minValue' => $job['salaryMin'],
                    'maxValue' => $job['salaryMax'],
                    'unitText' => 'MONTH'
                ]
            ],
            'identifier' => [
                '@type' => 'PropertyValue',
                'name' => 'Vinamilk',
                'value' => $job['jobCode']
            ]
        ];

        $canonicalUrl = 'https://hrm.vinamilk.com.vn/tuyen-dung/' . $job['slug'] . '-' . $job['id'];

        return View::render('careers/detail.twig', [
            'job' => $job,
            'jsonLd' => json_encode($schemaJobPosting, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES | JSON_PRETTY_PRINT),
            'canonicalUrl' => $canonicalUrl,
            'pageTitle' => $job['title'] . ' | Tuyển Dụng Vinamilk',
            'metaDescription' => 'Vinamilk tuyển dụng ' . $job['title'] . ' tại ' . $job['city'] . '. Mức lương: ' . $job['salaryDisplay'] . '. Ứng tuyển ngay tại Cổng Tuyển Dụng Vinamilk.'
        ]);
    }

    public function getSitemapXml(): string
    {
        $publicUrls = [
            [
                'loc' => 'https://hrm.vinamilk.com.vn/tuyen-dung',
                'lastmod' => date('Y-m-d'),
                'changefreq' => 'daily',
                'priority' => '1.0'
            ]
        ];

        foreach ($this->jobs as $j) {
            if ($j['isActive'] && !$j['isExpired']) {
                $publicUrls[] = [
                    'loc' => 'https://hrm.vinamilk.com.vn/tuyen-dung/' . $j['slug'] . '-' . $j['id'],
                    'lastmod' => substr($j['datePosted'], 0, 10),
                    'changefreq' => 'weekly',
                    'priority' => '0.8'
                ];
            }
        }

        return View::render('careers/sitemap.twig', ['urls' => $publicUrls]);
    }

    public function sitemap(): void
    {
        if (!headers_sent()) {
            header('Content-Type: application/xml; charset=utf-8');
            @header_remove('X-Robots-Tag');
        }
        echo $this->getSitemapXml();
    }

    public function legacyRedirect(string $legacyPath = ''): void
    {
        if (!headers_sent()) {
            header('HTTP/1.1 301 Moved Permanently');
            header('Location: /tuyen-dung');
        }
        exit;
    }
}