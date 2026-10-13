<?php

namespace App\Modules\Report\Http\Controllers;

use App\Http\Controllers\Controller;
use App\Modules\Organization\Models\OrgUnit;
use App\Modules\Organization\Models\Position;
use App\Modules\Report\Services\ReportService;
use Illuminate\Http\Request;
use Illuminate\Http\Response;
use Illuminate\View\View;

class ReportController extends Controller
{
    public function __construct(
        protected ReportService $reportService
    ) {}

    /**
     * Trung tâm Báo cáo Thống kê & Dashboard HR Analytics
     */
    public function index(Request $request): View
    {
        $orgPath = $request->get('org_path', '/1/');
        $year = (int)$request->get('year', date('Y'));

        $orgUnits = OrgUnit::orderBy('path')->get();
        $positions = Position::orderBy('level')->get();

        return view('reports.index', [
            'title'     => 'Trung tâm Báo cáo Thống kê & HR Analytics - Vinamilk',
            'orgUnits'  => $orgUnits,
            'positions' => $positions,
            'orgPath'   => $orgPath,
            'year'      => $year,
        ]);
    }

    /**
     * BIỂU MẪU 01: Sơ yếu lý lịch trích ngang
     */
    public function bm01(Request $request): View
    {
        $filters = [
            'org_path'          => $request->get('org_path', '/1/'),
            'position_id'       => $request->get('position_id'),
            'employment_status' => $request->get('employment_status'),
            'search'            => $request->get('search'),
        ];

        $list = $this->reportService->getBM01TrichNgang($filters);
        $orgUnits = OrgUnit::orderBy('path')->get();
        $positions = Position::orderBy('level')->get();

        return view('reports.bm01_trichngang', [
            'title'     => 'Biểu mẫu 01: Sơ yếu Lý lịch Trích ngang CBNV - Vinamilk',
            'list'      => $list,
            'filters'   => $filters,
            'orgUnits'  => $orgUnits,
            'positions' => $positions,
        ]);
    }

    /**
     * BIỂU MẪU 02: Báo cáo Biến động Nhân sự
     */
    public function bm02(Request $request): View
    {
        $year = (int)$request->get('year', date('Y'));
        $orgPath = $request->get('org_path', '/1/');

        $list = $this->reportService->getBM02BienDong($year, $orgPath);
        $orgUnits = OrgUnit::orderBy('path')->get();

        return view('reports.bm02_biendong', [
            'title'    => "Biểu mẫu 02: Báo cáo Biến động Nhân sự Năm {$year} - Vinamilk",
            'list'     => $list,
            'year'     => $year,
            'orgPath'  => $orgPath,
            'orgUnits' => $orgUnits,
        ]);
    }

    /**
     * BIỂU MẪU 03: Thống kê Khen thưởng & Kỷ luật
     */
    public function bm03(Request $request): View
    {
        $year = (int)$request->get('year', date('Y'));
        $orgPath = $request->get('org_path', '/1/');

        $list = $this->reportService->getBM03KTKL($year, $orgPath);
        $orgUnits = OrgUnit::orderBy('path')->get();

        return view('reports.bm03_ktkl', [
            'title'    => "Biểu mẫu 03: Thống kê Khen thưởng & Kỷ luật Năm {$year} - Vinamilk",
            'list'     => $list,
            'year'     => $year,
            'orgPath'  => $orgPath,
            'orgUnits' => $orgUnits,
        ]);
    }

    /**
     * BIỂU MẪU 04: Thống kê Nhân sự Tiệm cận Nghỉ hưu theo NĐ 135/2020
     */
    public function bm04(Request $request): View
    {
        $orgPath = $request->get('org_path', '/1/');
        $withinMonths = (int)$request->get('within_months', 12);

        $list = $this->reportService->getBM04NghiHuu($orgPath, $withinMonths);
        $orgUnits = OrgUnit::orderBy('path')->get();

        return view('reports.bm04_nghihuu', [
            'title'        => 'Biểu mẫu 04: Danh sách Nhân sự Tiệm cận Nghỉ hưu (NĐ 135/2020) - Vinamilk',
            'list'         => $list,
            'orgPath'      => $orgPath,
            'withinMonths' => $withinMonths,
            'orgUnits'     => $orgUnits,
        ]);
    }

    /**
     * Xuất Báo cáo ra File Excel / CSV (Unicode UTF-8 BOM)
     */
    public function exportExcel(Request $request, string $type = 'bm01'): Response
    {
        $filters = [
            'org_path'          => $request->get('org_path', '/1/'),
            'position_id'       => $request->get('position_id'),
            'employment_status' => $request->get('employment_status'),
            'search'            => $request->get('search'),
            'year'              => (int)$request->get('year', date('Y')),
        ];

        $csvContent = $this->reportService->generateExcelCsv($type, $filters);
        $filename = "Vinamilk_Report_" . strtoupper($type) . "_" . date('Ymd_His') . ".csv";

        return response($csvContent, 200, [
            'Content-Type'        => 'text/csv; charset=UTF-8',
            'Content-Disposition' => "attachment; filename=\"{$filename}\"",
        ]);
    }

    /**
     * Phôi in Báo cáo Chuẩn A4 (@media print)
     */
    public function printReport(Request $request, string $type = 'bm01'): View
    {
        $filters = [
            'org_path'          => $request->get('org_path', '/1/'),
            'position_id'       => $request->get('position_id'),
            'employment_status' => $request->get('employment_status'),
            'year'              => (int)$request->get('year', date('Y')),
        ];

        $list = match ($type) {
            'bm01'  => $this->reportService->getBM01TrichNgang($filters),
            'bm02'  => $this->reportService->getBM02BienDong((int)$filters['year'], $filters['org_path']),
            'bm03'  => $this->reportService->getBM03KTKL((int)$filters['year'], $filters['org_path']),
            'bm04'  => $this->reportService->getBM04NghiHuu($filters['org_path'], 12),
            default => $this->reportService->getBM01TrichNgang($filters),
        };

        return view('reports.print_report', [
            'type'    => $type,
            'title'   => "In Báo cáo: " . strtoupper($type),
            'list'    => $list,
            'filters' => $filters,
        ]);
    }
}
