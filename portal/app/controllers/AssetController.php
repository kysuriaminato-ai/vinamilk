<?php
/**
 * =====================================================================
 * VINAMILK HRM - CONTROLLER QUẢN LÝ TÀI SẢN & PPE (AssetController)
 * =====================================================================
 * Quản lý danh mục theo vị trí, cấp phát, thu hồi có ký nhận điện tử & bồi thường
 */

require_once __DIR__ . '/../core/Controller.php';
require_once __DIR__ . '/../services/AssetPpeService.php';
require_once __DIR__ . '/../models/OrgUnitModel.php';

class AssetController extends Controller
{
    private AssetPpeService $assetService;
    private OrgUnitModel $orgUnitModel;

    public function __construct()
    {
        $this->assetService = new AssetPpeService();
        $this->orgUnitModel = new OrgUnitModel();
    }

    /**
     * TRANG TỔNG QUAN TÀI SẢN & TRANG BỊ PPE CẤP PHÁT
     */
    public function index(): void
    {
        $this->requireAuth();
        $this->requirePermission('nhansu.view');

        $db = Database::getInstance()->getConnection();

        $loaiTS = $_GET['loai_tai_san'] ?? '';
        $trangThai = $_GET['trang_thai'] ?? '';
        $maDV = $_GET['ma_dv'] ?? ($_GET['unit_scope'] ?? '');
        $keyword = trim($_GET['keyword'] ?? '');

        $sql = "SELECT ts.*, ns.HoTen, ns.MaDV, dv.TenDV, cv.TenCV
                FROM cap_phat_tai_san ts
                JOIN nhansu ns ON ts.ma_nv = ns.MaNV
                LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV
                LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV
                WHERE 1=1";
        $params = [];

        if (!empty($loaiTS) && $loaiTS !== 'ALL') {
            $sql .= " AND ts.loai_tai_san = :loaits";
            $params[':loaits'] = $loaiTS;
        }

        if (!empty($trangThai) && $trangThai !== 'ALL') {
            $sql .= " AND ts.trang_thai = :st";
            $params[':st'] = $trangThai;
        }

        if (!empty($maDV) && $maDV !== 'ALL') {
            $sql .= " AND ns.MaDV = :madv";
            $params[':madv'] = $maDV;
        }

        if (!empty($keyword)) {
            $sql .= " AND (ts.ten_tai_san LIKE :kw OR ts.ma_dinh_danh_serial LIKE :kw OR ns.HoTen LIKE :kw OR ts.ma_nv LIKE :kw)";
            $params[':kw'] = '%' . $keyword . '%';
        }

        $sql .= " ORDER BY ts.id DESC LIMIT 100";

        $stmt = $db->prepare($sql);
        $stmt->execute($params);
        $assets = $stmt->fetchAll(PDO::FETCH_ASSOC);

        // Thống kê nhanh
        $stats = $db->query("SELECT 
            COUNT(*) as tong_so,
            SUM(CASE WHEN loai_tai_san = 'BHLD_PPE' THEN 1 ELSE 0 END) as so_ppe,
            SUM(CASE WHEN loai_tai_san = 'MAY_TINH_LAPTOP' THEN 1 ELSE 0 END) as so_laptop,
            SUM(CASE WHEN da_ky_nhan_cap = 1 THEN 1 ELSE 0 END) as da_ky_nhan,
            SUM(CASE WHEN trang_thai IN ('BAO_MAT', 'BAO_HONG') THEN 1 ELSE 0 END) as bao_mat_hong,
            SUM(gia_tri_khau_tru) as tong_khau_tru
        FROM cap_phat_tai_san")->fetch(PDO::FETCH_ASSOC);

        $selectableUnits = $this->orgUnitModel->getSelectableUnitsForUser();
        $standards = $this->assetService->getStandardsByGroup();

        $this->view('assets/index', [
            'title'           => 'Quản Lý Tài Sản Cấp Phát & Trang Bị PPE - Vinamilk HRM',
            'assets'          => $assets,
            'stats'           => $stats,
            'standards'       => $standards,
            'selectableUnits' => $selectableUnits,
            'loaiTS'          => $loaiTS,
            'trangThai'       => $trangThai,
            'maDV'            => $maDV,
            'keyword'         => $keyword
        ]);
    }

    /**
     * DANH MỤC TIÊU CHUẨN ĐỊNH MỨC PPE THEO VỊ TRÍ
     */
    public function standards(): void
    {
        $this->requireAuth();

        $group = $_GET['group'] ?? null;
        $standards = $this->assetService->getStandardsByGroup($group);

        $this->view('assets/standards', [
            'title'     => 'Danh Mục Tiêu Chuẩn Định Mức PPE & Tài Sản Theo Vị Trí - Vinamilk',
            'standards' => $standards,
            'currentGroup' => $group
        ]);
    }

    /**
     * CẤP PHÁT TÀI SẢN MỚI (POST)
     */
    public function allocate(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI']);
        $this->requirePermission('nhansu.edit');
        $this->validateCsrf();

        try {
            $id = $this->assetService->allocateAsset($_POST);
            Session::setFlash('success', "Cấp phát tài sản thành công! Đã tạo biên bản bàn giao điện tử.", 'success');
        } catch (Throwable $e) {
            Session::setFlash('error', 'Lỗi cấp phát: ' . $e->getMessage(), 'danger');
        }

        $returnUrl = $_POST['return_url'] ?? 'assets';
        $this->redirect($returnUrl);
    }

    /**
     * KÝ NHẬN ĐIỆN TỬ BÀN GIAO TÀI SẢN (POST / AJAX)
     */
    public function sign(): void
    {
        $this->requireAuth();
        $this->validateCsrf();

        $assetId = (int)($_POST['asset_id'] ?? 0);
        $signer = Auth::username() ?? 'Employee';

        if ($this->assetService->signAllocation($assetId, $signer)) {
            Session::setFlash('success', 'Đã ký nhận biên bản bàn giao tài sản điện tử thành công!', 'success');
        } else {
            Session::setFlash('error', 'Lỗi ký nhận biên bản bàn giao.', 'danger');
        }

        $returnUrl = $_POST['return_url'] ?? 'assets';
        $this->redirect($returnUrl);
    }

    /**
     * THU HỒI / BÁO MẤT / BÁO HỎNG TÀI SẢN (POST)
     */
    public function recover(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI']);
        $this->requirePermission('nhansu.edit');
        $this->validateCsrf();

        $assetId   = (int)($_POST['asset_id'] ?? 0);
        $status    = $_POST['status'] ?? 'DA_THU_HOI';
        $penalty   = !empty($_POST['penalty_amount']) ? (float)$_POST['penalty_amount'] : null;
        $condition = $_POST['condition'] ?? '';
        $notes     = $_POST['notes'] ?? '';

        try {
            $this->assetService->returnOrRecoverAsset($assetId, $status, $penalty, $condition, $notes);
            $msg = ($status === 'DA_THU_HOI') 
                ? 'Đã thu hồi tài sản thành công!' 
                : 'Đã ghi nhận báo mất/hỏng và xác định khoản khấu trừ lương: ' . number_format($penalty ?? 0, 0, ',', '.') . 'đ.';
            Session::setFlash('success', $msg, 'success');
        } catch (Throwable $e) {
            Session::setFlash('error', 'Lỗi xử lý thu hồi: ' . $e->getMessage(), 'danger');
        }

        $returnUrl = $_POST['return_url'] ?? 'assets';
        $this->redirect($returnUrl);
    }

    /**
     * API GỢI Ý DANH MỤC THEO VỊ TRÍ NHÂN SỰ
     */
    public function api_suggest(): void
    {
        $this->requireAuth();
        $maNV = $_GET['ma_nv'] ?? '';

        $data = $this->assetService->suggestPpeByEmployee($maNV);
        $this->json([
            'success' => true,
            'data'    => $data
        ]);
    }
}
