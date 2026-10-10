<?php
/**
 * =====================================================================
 * VINAMILK HRM - Executive Dashboard Controller
 * =====================================================================
 * 
 * Controller trang chủ điều hành Vinamilk HRM với đầy đủ chỉ số KPI thời gian thực,
 * biểu đồ cơ cấu đơn vị, trình độ học vấn, lịch sử live log và Sơ đồ cây Tổ chức tương tác.
 */

require_once APP_DIR . '/core/Controller.php';

class DashboardController extends Controller {

    public function index(): void {
        $this->requireAuth();

        $db = Database::getInstance()->getConnection();

        // 1. Thống kê tổng số Nhân sự & Trạng thái
        $totalStaff = (int) $db->query("SELECT COUNT(*) FROM nhansu WHERE TrangThai IN (1, 2)")->fetchColumn();
        $activeStaff = (int) $db->query("SELECT COUNT(*) FROM nhansu WHERE TrangThai = 1")->fetchColumn();
        $probationStaff = (int) $db->query("SELECT COUNT(*) FROM nhansu WHERE TrangThai = 2")->fetchColumn();
        $resignedStaff = (int) $db->query("SELECT COUNT(*) FROM nhansu WHERE TrangThai = 3")->fetchColumn();

        // 2. Thống kê theo Giới tính & Độ tuổi trung bình
        $maleCount = (int) $db->query("SELECT COUNT(*) FROM nhansu WHERE GioiTinh = 'Nam' AND TrangThai IN (1, 2)")->fetchColumn();
        $femaleCount = (int) $db->query("SELECT COUNT(*) FROM nhansu WHERE GioiTinh = 'Nữ' AND TrangThai IN (1, 2)")->fetchColumn();
        $avgAge = round((float) $db->query("SELECT AVG(TIMESTAMPDIFF(YEAR, NgaySinh, CURDATE())) FROM nhansu WHERE TrangThai IN (1, 2)")->fetchColumn(), 1);

        // 3. Thống kê theo Khối / Đơn vị
        $deptStats = $db->query("
            SELECT dv.MaDV, dv.TenDV, dv.LoaiDV, COUNT(ns.MaNV) as TotalCount
            FROM dm_donvi dv
            LEFT JOIN nhansu ns ON dv.MaDV = ns.MaDV AND ns.TrangThai IN (1, 2)
            WHERE dv.CapDV <= 2
            GROUP BY dv.MaDV, dv.TenDV, dv.LoaiDV
            ORDER BY TotalCount DESC
        ")->fetchAll();

        // 4. Cơ cấu Trình độ học vấn
        $eduStats = $db->query("
            SELECT COALESCE(TrinhDoHocVan, N'Khác') as TrinhDo, COUNT(*) as TotalCount
            FROM nhansu
            WHERE TrangThai IN (1, 2)
            GROUP BY TrinhDoHocVan
            ORDER BY TotalCount DESC
        ")->fetchAll();

        // 5. Thống kê theo Vai trò Đăng nhập
        $roleStats = $db->query("
            SELECT r.RoleName, r.RoleCode, COUNT(u.ID) as TotalUsers
            FROM roles r
            LEFT JOIN users u ON r.RoleID = u.RoleID
            GROUP BY r.RoleID
            ORDER BY r.RoleID ASC
        ")->fetchAll();

        // 6. Sơ đồ Tổ chức Cây phân cấp (Interactive Org Chart)
        $orgTree = $db->query("
            SELECT dv.MaDV, dv.TenDV, dv.TenVietTat, dv.CapDV, dv.MaDV_Cha, dv.LoaiDV, dv.SoNhanSu,
                   COUNT(ns.MaNV) as ActualCount
            FROM dm_donvi dv
            LEFT JOIN nhansu ns ON dv.MaDV = ns.MaDV AND ns.TrangThai IN (1, 2)
            GROUP BY dv.MaDV
            ORDER BY dv.CapDV ASC, dv.ThuTu ASC
        ")->fetchAll();

        // 7. Nhật ký lịch sử truy cập gần nhất (cho Admin/Manager)
        /** @var AuditLog $auditModel */
        $auditModel = $this->model('AuditLog');
        $recentLogs = $auditModel->getRecentLogs(10);

        // Render Dashboard View
        $this->view('dashboard/index', [
            'title' => 'Dashboard Điều hành Nhân sự - Vinamilk Executive HRM',
            'stats' => [
                'totalStaff' => $totalStaff,
                'activeStaff' => $activeStaff,
                'probationStaff' => $probationStaff,
                'resignedStaff' => $resignedStaff,
                'maleCount' => $maleCount,
                'femaleCount' => $femaleCount,
                'avgAge' => $avgAge
            ],
            'deptStats' => $deptStats,
            'eduStats' => $eduStats,
            'roleStats' => $roleStats,
            'orgTree' => $orgTree,
            'recentLogs' => $recentLogs
        ]);
    }
}
