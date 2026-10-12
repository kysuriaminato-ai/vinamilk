<?php
/**
 * =====================================================================
 * VINAMILK HRM - CONTROLLER CHỨNG CHỈ & AN TOÀN (CertificateController)
 * =====================================================================
 * Quản lý Thẻ xanh ATTP, ATLĐ, Chứng chỉ vận hành thiết bị & Cảnh báo 60 ngày
 */

require_once __DIR__ . '/../core/Controller.php';
require_once __DIR__ . '/../services/CertificateService.php';
require_once __DIR__ . '/../models/OrgUnitModel.php';

class CertificateController extends Controller
{
    private CertificateService $certService;
    private OrgUnitModel $orgUnitModel;

    public function __construct()
    {
        $this->certService = new CertificateService();
        $this->orgUnitModel = new OrgUnitModel();
    }

    /**
     * TRANG TỔNG QUAN CHỨNG CHỈ & CẢNH BÁO 60 NGÀY
     */
    public function index(): void
    {
        $this->requireAuth();
        $this->requirePermission('nhansu.view');

        $warningDays = max(1, (int)($_GET['warning_days'] ?? 60));
        $maDV = $_GET['ma_dv'] ?? ($_GET['unit_scope'] ?? '');
        $loaiCC = $_GET['loai_chung_chi'] ?? '';

        $expiringCerts = $this->certService->getExpiringCertificates($warningDays, $maDV);

        // Lọc theo loại nếu có yêu cầu
        if (!empty($loaiCC) && $loaiCC !== 'ALL') {
            $expiringCerts = array_filter($expiringCerts, fn($c) => $c['loai_chung_chi'] === $loaiCC);
        }

        // Đếm số lượng theo nhóm
        $countAttpExpired = 0;
        $countAttpWarning = 0;
        $countAtldWarning = 0;
        $countEquipWarning = 0;

        foreach ($expiringCerts as $c) {
            $isAttp = ($c['loai_chung_chi'] === 'THE_XANH_ATTP' || !empty($c['la_the_xanh_attp']));
            if ($isAttp) {
                if ($c['so_ngay_con_lai'] < 0) {
                    $countAttpExpired++;
                } else {
                    $countAttpWarning++;
                }
            } elseif ($c['loai_chung_chi'] === 'ATLD_VE_SINH_LD') {
                $countAtldWarning++;
            } elseif ($c['loai_chung_chi'] === 'CHUNG_CHI_THIET_BI') {
                $countEquipWarning++;
            }
        }

        $selectableUnits = $this->orgUnitModel->getSelectableUnitsForUser();

        $this->view('certificates/index', [
            'title'             => 'Quản Lý Chứng Chỉ & An Toàn Thực Phẩm (ATTP / ATLĐ) - Vinamilk HRM',
            'expiringCerts'     => $expiringCerts,
            'warningDays'       => $warningDays,
            'maDV'              => $maDV,
            'loaiCC'            => $loaiCC,
            'countAttpExpired'  => $countAttpExpired,
            'countAttpWarning'  => $countAttpWarning,
            'countAtldWarning'  => $countAtldWarning,
            'countEquipWarning' => $countEquipWarning,
            'selectableUnits'   => $selectableUnits
        ]);
    }

    /**
     * THÊM HOẶC CẬP NHẬT CHỨNG CHỈ (POST)
     */
    public function save(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI']);
        $this->requirePermission('nhansu.edit');
        $this->validateCsrf();

        try {
            $certId = $this->certService->saveCertificate($_POST);
            Session::setFlash('success', 'Đã lưu chứng chỉ thành công!', 'success');
        } catch (Throwable $e) {
            Session::setFlash('error', 'Lỗi lưu chứng chỉ: ' . $e->getMessage(), 'danger');
        }

        $returnUrl = $_POST['return_url'] ?? 'certificates';
        $this->redirect($returnUrl);
    }

    /**
     * API KIỂM TRA HỢP LỆ XẾP CA DÂY CHUYỀN THEO THẺ ATTP
     */
    public function api_validate_shift(): void
    {
        $this->requireAuth();

        $maNV      = $_GET['ma_nv'] ?? '';
        $shiftCode = $_GET['shift_code'] ?? '';
        $workDate  = $_GET['work_date'] ?? date('Y-m-d');
        $maDV      = $_GET['ma_dv'] ?? '';

        try {
            $this->certService->validateShiftAssignment($maNV, $shiftCode, $workDate, $maDV);
            $this->json([
                'success' => true,
                'allowed' => true,
                'message' => 'Hợp lệ! Thẻ xanh ATTP còn hiệu lực.'
            ]);
        } catch (DomainException $e) {
            $this->json([
                'success' => false,
                'allowed' => false,
                'message' => $e->getMessage()
            ], 422);
        } catch (Throwable $e) {
            $this->json([
                'error'   => true,
                'message' => $e->getMessage()
            ], 500);
        }
    }
}
