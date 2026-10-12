<?php
/**
 * =====================================================================
 * VINAMILK HRM - CONTROLLER PHÒNG TRƯNG BÀY GIAO DIỆN (KITCHEN SINK)
 * =====================================================================
 * Trưng bày và kiểm thử toàn diện các thành phần dùng chung:
 *   1. Design tokens CSS (Màu chủ đạo #0033A0, màu trạng thái WCAG AA, Be Vietnam Pro, 8px)
 *   2. Thẻ KPI (KPI Metric Cards)
 *   3. Badge trạng thái đạt chuẩn tương phản WCAG AA
 *   4. Bảng dữ liệu có lọc, sắp xếp, phân trang phía server
 *   5. Trạng thái rỗng (Empty State)
 *   6. Modal xác nhận tương tác (Confirmation Modal)
 *   7. Các điều khiển form với nhãn và trạng thái kiểm tra hợp lệ
 */

require_once __DIR__ . '/../core/Controller.php';
require_once __DIR__ . '/../models/EmployeeModel.php';
require_once __DIR__ . '/../models/OrgUnitModel.php';

class KitchenSinkController extends Controller
{
    private EmployeeModel $employeeModel;
    private OrgUnitModel $orgUnitModel;

    public function __construct()
    {
        $this->employeeModel = new EmployeeModel();
        $this->orgUnitModel  = new OrgUnitModel();
    }

    /**
     * TRANG TRƯNG BÀY TOÀN BỘ THÀNH PHẦN GIAO DIỆN CHUẨN VINAMILK HRM
     */
    public function index(): void
    {
        $this->requireAuth();

        // 1. Dữ liệu Thẻ KPI Mẫu
        $kpis = [
            [
                'title'      => 'Tổng Quy Mô Nhân Sự',
                'value'      => '842',
                'unit'       => 'nhân viên',
                'trend'      => '+4.8%',
                'trend_type' => 'up',
                'trend_text' => 'so với tháng trước',
                'icon'       => 'bi-people-fill',
                'color'      => 'primary',
                'bg_tint'    => 'bg-primary-subtle text-primary'
            ],
            [
                'title'      => 'Tỷ Lệ Đi Làm Đúng Giờ',
                'value'      => '98.5%',
                'unit'       => 'chuyên cần',
                'trend'      => '+0.8%',
                'trend_type' => 'up',
                'trend_text' => 'đạt mục tiêu SLA',
                'icon'       => 'bi-clock-history',
                'color'      => 'success',
                'bg_tint'    => 'bg-success-subtle text-success'
            ],
            [
                'title'      => 'Quỹ Lương Kỳ Hiện Tại',
                'value'      => '14.85',
                'unit'       => 'tỷ đồng',
                'trend'      => '-1.2%',
                'trend_type' => 'down',
                'trend_text' => 'tiết kiệm chi phí OT',
                'icon'       => 'bi-cash-coin',
                'color'      => 'warning',
                'bg_tint'    => 'bg-warning-subtle text-warning'
            ],
            [
                'title'      => 'Đơn & Tờ Trình Chờ Duyệt',
                'value'      => '12',
                'unit'       => 'hồ sơ',
                'trend'      => 'Cần xử lý',
                'trend_type' => 'neutral',
                'trend_text' => 'trong 24 giờ tới',
                'icon'       => 'bi-envelope-check-fill',
                'color'      => 'danger',
                'bg_tint'    => 'bg-danger-subtle text-danger'
            ]
        ];

        // 2. Dữ liệu Bảng có Lọc, Sắp xếp, Phân trang phía Server
        $keyword = trim($_GET['keyword'] ?? '');
        $status  = $_GET['status'] ?? '';
        $page    = max(1, (int)($_GET['page'] ?? 1));
        $limit   = 6;
        $offset  = ($page - 1) * $limit;

        $sortBy  = in_array($_GET['sort'] ?? '', ['MaNV', 'HoTen', 'MaDV', 'LuongBaoHiem']) ? $_GET['sort'] : 'MaNV';
        $order   = strtolower($_GET['order'] ?? 'asc') === 'desc' ? 'DESC' : 'ASC';

        // Lấy danh sách từ EmployeeModel theo chuẩn scopeByUnit
        $filters = [
            'keyword'    => $keyword,
            'trang_thai' => $status
        ];

        $rawResult = $this->employeeModel->getEmployees($filters, $limit, $offset);

        // Chuẩn bị dữ liệu hiển thị
        $tableData = [
            'items'        => $rawResult['data'],
            'total'        => $rawResult['total'],
            'page'         => $page,
            'limit'        => $limit,
            'total_pages'  => max(1, (int)ceil($rawResult['total'] / $limit)),
            'keyword'      => $keyword,
            'status'       => $status,
            'sort'         => $sortBy,
            'order'        => $order
        ];

        $breadcrumbs = [
            ['title' => 'Giao Diện & Thành Phần Chuẩn', 'url' => 'kitchen-sink'],
            ['title' => 'Kitchen Sink Gallery', 'url' => '']
        ];

        $this->view('kitchen_sink/index', [
            'title'       => 'Vinamilk UI Kitchen Sink - Thư Viện Thành Phần Dùng Chung',
            'kpis'        => $kpis,
            'tableData'   => $tableData,
            'breadcrumbs' => $breadcrumbs
        ]);
    }
}
