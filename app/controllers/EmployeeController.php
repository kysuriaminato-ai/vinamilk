<?php
/**
 * =====================================================================
 * VINAMILK HRM - CONTROLLER QUẢN LÝ HỒ SƠ NHÂN SỰ (EmployeeController)
 * =====================================================================
 * Tuân thủ nghiêm ngặt:
 *   - Lọc 100% qua scopeByUnit() theo cây đơn vị
 *   - Danh sách đa chiều & tìm kiếm nâng cao AND/OR (tuổi, thâm niên, trình độ)
 *   - Xuất Excel tương thích Microsoft Excel UTF-8 BOM
 *   - Trang chi tiết 8 tab: Lý lịch, Hợp đồng, Quá trình công tác, Lương,
 *     Đào tạo & chứng chỉ, KT-KL, Tài sản, Tài liệu scan
 *   - Quản lý hợp đồng: Cảnh báo 60 và 30 ngày, CHẶN KÝ LẦN 3 HĐ xác định thời hạn
 *   - In hợp đồng mẫu chuẩn Vinamilk A4
 *   - Wizard tiếp nhận nhân viên mới 4 bước
 *   - Upload an toàn PDF/ảnh <= 5MB lưu ngoài public/ & download bảo mật
 */

require_once __DIR__ . '/../core/Controller.php';
require_once __DIR__ . '/../models/EmployeeModel.php';
require_once __DIR__ . '/../models/OrgUnitModel.php';
require_once __DIR__ . '/../models/ContractModel.php';
require_once __DIR__ . '/../models/DocumentModel.php';
require_once __DIR__ . '/../services/CertificateService.php';
require_once __DIR__ . '/../services/AssetPpeService.php';
require_once __DIR__ . '/../services/OffboardingService.php';

class EmployeeController extends Controller
{
    private EmployeeModel $employeeModel;
    private OrgUnitModel $orgUnitModel;
    private ContractModel $contractModel;
    private DocumentModel $documentModel;
    private CertificateService $certService;
    private AssetPpeService $assetService;
    private OffboardingService $offboardingService;

    public function __construct()
    {
        $this->employeeModel = new EmployeeModel();
        $this->orgUnitModel  = new OrgUnitModel();
        $this->contractModel = new ContractModel();
        $this->documentModel = new DocumentModel();
        $this->certService   = new CertificateService();
        $this->assetService  = new AssetPpeService();
        $this->offboardingService = new OffboardingService();
    }

    /**
     * DANH SÁCH NHÂN SỰ (Lọc đa chiều & Tìm kiếm nâng cao AND/OR)
     */
    public function index(): void
    {
        $this->requireAuth();
        $this->requirePermission('nhansu.view');

        $page = max(1, (int)($_GET['page'] ?? 1));
        $limit = max(1, min(100, (int)($_GET['limit'] ?? 20)));
        $offset = ($page - 1) * $limit;

        $targetUnit = $_GET['ma_dv'] ?? ($_GET['unit_scope'] ?? '');

        // Kiểm tra phân quyền phạm vi đơn vị nếu HR_DONVI yêu cầu đơn vị ngoài cây
        if (Auth::role() === 'HR_DONVI' && !empty($targetUnit) && $targetUnit !== 'ALL') {
            $allowed = $this->orgUnitModel->getDescendantUnits(Auth::maDV() ?: '');
            if (!in_array($targetUnit, $allowed, true)) {
                if ($this->isJsonRequest()) {
                    $this->json([
                        'error' => '403 Forbidden',
                        'message' => "Bạn không có quyền truy cập nhân sự thuộc đơn vị [{$targetUnit}]."
                    ], 403);
                }
            }
        }

        $filters = [
            'keyword'        => trim($_GET['keyword'] ?? ''),
            'ma_dv'          => $targetUnit,
            'ma_cv'          => trim($_GET['ma_cv'] ?? ''),
            'trang_thai'     => $_GET['trang_thai'] ?? '',
            'loai_hd'        => trim($_GET['loai_hd'] ?? ''),
            // Tìm kiếm nâng cao
            'gioi_tinh'      => trim($_GET['gioi_tinh'] ?? ''),
            'age_min'        => !empty($_GET['age_min']) ? (int)$_GET['age_min'] : null,
            'age_max'        => !empty($_GET['age_max']) ? (int)$_GET['age_max'] : null,
            'trinh_do'       => trim($_GET['trinh_do'] ?? ''),
            'seniority'      => trim($_GET['seniority'] ?? ''),
            'advanced_logic' => strtoupper($_GET['advanced_logic'] ?? 'AND')
        ];

        // Mọi truy vấn nhân sự đều BẮT BUỘC qua EmployeeModel::getEmployees() (có scopeByUnit)
        $result = $this->employeeModel->getEmployees($filters, $limit, $offset);

        // Hỗ trợ phản hồi JSON nếu client gọi API
        if ($this->isJsonRequest()) {
            $this->json([
                'success'    => true,
                'data'       => $result['data'],
                'pagination' => [
                    'total'        => $result['total'],
                    'current_page' => $result['current_page'],
                    'total_pages'  => $result['total_pages'],
                    'limit'        => $result['limit']
                ]
            ]);
        }

        $selectableUnits = $this->orgUnitModel->getSelectableUnitsForUser();
        
        // Danh mục chức vụ
        $db = Database::getInstance();
        $positions = $db->query("SELECT MaCV, TenCV FROM dm_chucvu WHERE TrangThai = 1 ORDER BY CapBac ASC, TenCV ASC")->fetchAll(PDO::FETCH_ASSOC);

        $this->view('employees/index', [
            'title'           => 'Danh sách Nhân sự - Vinamilk HRM',
            'employees'       => $result['data'],
            'pagination'      => $result,
            'filters'         => $filters,
            'selectableUnits' => $selectableUnits,
            'positions'       => $positions
        ]);
    }

    /**
     * API DANH SÁCH NHÂN SỰ
     */
    public function api_list(): void
    {
        $this->requireAuth();
        $this->requirePermission('nhansu.view');

        $targetUnit = $_GET['ma_dv'] ?? ($_GET['unit_scope'] ?? '');

        // Kiểm tra phạm vi đơn vị cho HR_DONVI
        if (Auth::role() === 'HR_DONVI' && !empty($targetUnit) && $targetUnit !== 'ALL') {
            $allowed = $this->orgUnitModel->getDescendantUnits(Auth::maDV() ?: '');
            if (!in_array($targetUnit, $allowed, true)) {
                $this->json([
                    'error' => '403 Forbidden',
                    'message' => "Bạn không có quyền truy cập nhân sự thuộc đơn vị [{$targetUnit}]."
                ], 403);
            }
        }

        $page = max(1, (int)($_GET['page'] ?? 1));
        $limit = max(1, min(100, (int)($_GET['limit'] ?? 20)));
        $offset = ($page - 1) * $limit;

        $filters = [
            'keyword'        => trim($_GET['keyword'] ?? ''),
            'ma_dv'          => $targetUnit,
            'ma_cv'          => trim($_GET['ma_cv'] ?? ''),
            'trang_thai'     => $_GET['trang_thai'] ?? '',
            'loai_hd'        => trim($_GET['loai_hd'] ?? ''),
            'gioi_tinh'      => trim($_GET['gioi_tinh'] ?? ''),
            'age_min'        => !empty($_GET['age_min']) ? (int)$_GET['age_min'] : null,
            'age_max'        => !empty($_GET['age_max']) ? (int)$_GET['age_max'] : null,
            'trinh_do'       => trim($_GET['trinh_do'] ?? ''),
            'seniority'      => trim($_GET['seniority'] ?? ''),
            'advanced_logic' => strtoupper($_GET['advanced_logic'] ?? 'AND')
        ];

        $result = $this->employeeModel->getEmployees($filters, $limit, $offset);

        $this->json([
            'success'    => true,
            'data'       => $result['data'],
            'pagination' => [
                'total'        => $result['total'],
                'current_page' => $result['current_page'],
                'total_pages'  => $result['total_pages'],
                'limit'        => $result['limit']
            ]
        ]);
    }

    /**
     * XUẤT FILE EXCEL DANH SÁCH NHÂN SỰ (CSV UTF-8 BOM)
     */
    public function export_excel(): void
    {
        $this->requireAuth();
        $this->requirePermission('nhansu.export');

        $filters = [
            'keyword'        => trim($_GET['keyword'] ?? ''),
            'ma_dv'          => $_GET['ma_dv'] ?? '',
            'ma_cv'          => trim($_GET['ma_cv'] ?? ''),
            'trang_thai'     => $_GET['trang_thai'] ?? '',
            'loai_hd'        => trim($_GET['loai_hd'] ?? ''),
            'gioi_tinh'      => trim($_GET['gioi_tinh'] ?? ''),
            'age_min'        => !empty($_GET['age_min']) ? (int)$_GET['age_min'] : null,
            'age_max'        => !empty($_GET['age_max']) ? (int)$_GET['age_max'] : null,
            'trinh_do'       => trim($_GET['trinh_do'] ?? ''),
            'seniority'      => trim($_GET['seniority'] ?? ''),
            'advanced_logic' => strtoupper($_GET['advanced_logic'] ?? 'AND')
        ];

        $employees = $this->employeeModel->exportEmployees($filters);

        // Xuất file CSV định dạng chuẩn UTF-8 BOM
        $filename = 'Danh_sach_nhan_su_Vinamilk_' . date('Ymd_His') . '.csv';
        header('Content-Type: text/csv; charset=UTF-8');
        header('Content-Disposition: attachment; filename="' . $filename . '"');
        header('Pragma: no-cache');
        header('Expires: 0');

        $output = fopen('php://output', 'w');
        // Ghi UTF-8 BOM để Excel không lỗi font tiếng Việt
        fprintf($output, chr(0xEF).chr(0xBB).chr(0xBF));

        // Tiêu đề cột
        fputcsv($output, [
            'Mã NV',
            'Họ và tên',
            'Ngày sinh',
            'Tuổi',
            'Giới tính',
            'CCCD / CMND',
            'Mã số thuế',
            'Đơn vị công tác',
            'Chức danh',
            'Loại hợp đồng',
            'Ngày vào làm',
            'Thâm niên (năm)',
            'Trạng thái',
            'Lương đóng BH',
            'Email',
            'Số điện thoại'
        ]);

        foreach ($employees as $emp) {
            $statusText = match ((int)$emp['TrangThai']) {
                1 => 'Đang làm việc',
                2 => 'Thử việc',
                3 => 'Nghỉ việc',
                4 => 'Nghỉ hưu',
                default => 'Khác'
            };

            fputcsv($output, [
                $emp['MaNV'],
                $emp['HoTen'],
                $emp['NgaySinh'],
                $emp['Tuoi'] ?? '',
                $emp['GioiTinh'],
                $emp['SoCMND_CCCD'],
                $emp['MaSoThue'] ?? '',
                $emp['TenDV'] ?? $emp['MaDV'],
                $emp['TenCV'] ?? $emp['ChucVu'],
                $emp['LoaiHopDong'] ?? '',
                $emp['NgayVaoLam'],
                $emp['ThamNienNam'] ?? '',
                $statusText,
                is_numeric($emp['LuongBaoHiem']) ? number_format((float)$emp['LuongBaoHiem'], 0, ',', '.') : $emp['LuongBaoHiem'],
                $emp['Email'],
                $emp['SoDienThoai']
            ]);
        }

        fclose($output);
        exit;
    }

    /**
     * XEM CHI TIẾT HỒ SƠ NHÂN SỰ DẠNG 8 TAB
     */
    public function show(string $maNV = ''): void
    {
        $this->requireAuth();
        $this->requirePermission('nhansu.view');

        if (empty($maNV)) {
            $maNV = Auth::maNV() ?: '';
        }

        // Lấy thông tin cơ bản & kiểm tra quyền truy cập scopeByUnit
        $employee = $this->employeeModel->getEmployeeById($maNV);
        if (!$employee) {
            Session::setFlash('error', "Không tìm thấy hồ sơ nhân sự [{$maNV}] hoặc bạn không có quyền truy cập.", 'danger');
            $this->redirect('employees');
        }

        // Dữ liệu cho từng Tab:
        // Tab 2: Hợp đồng lao động
        $contracts = $this->contractModel->getContractsByEmployee($maNV);
        $fixedTermCount = $this->contractModel->countFixedTermContracts($maNV);
        $canSignFixedTerm = ($fixedTermCount < 2);

        // Tab 3: Quá trình công tác
        $careerHistory = $this->employeeModel->getCareerHistory($maNV);

        // Tab 4: Lương & điều chỉnh lương
        $salaryHistory = $this->employeeModel->getSalaryHistory($maNV);

        // Tab 5: Đào tạo & Chứng chỉ
        $certificates = $this->employeeModel->getCertificates($maNV);

        // Tab 6: Khen thưởng & Kỷ luật
        $rewardsDisciplines = $this->employeeModel->getRewardsAndDisciplines($maNV);

        // Tab 7: Tài sản cấp phát
        $assets = $this->employeeModel->getAssets($maNV);

        // Tab 8: Tài liệu scan
        $documents = $this->documentModel->getDocumentsByEmployee($maNV);

        // Tab 9: Quy trình thôi việc (Offboarding) & Khảo sát lý do nghỉ việc (Exit Survey)
        $offboarding = $this->employeeModel->getOffboarding($maNV);
        $exitSurvey  = $this->employeeModel->getExitSurvey($maNV);
        $ppeSuggestions = $this->assetService->suggestPpeByEmployee($maNV);

        $activeTab = $_GET['tab'] ?? 'ly-lich';

        $this->view('employees/show', [
            'title'              => 'Hồ sơ nhân sự: ' . $employee['HoTen'] . ' (' . $employee['MaNV'] . ')',
            'employee'           => $employee,
            'contracts'          => $contracts,
            'fixedTermCount'     => $fixedTermCount,
            'canSignFixedTerm'   => $canSignFixedTerm,
            'careerHistory'      => $careerHistory,
            'salaryHistory'      => $salaryHistory,
            'certificates'       => $certificates,
            'rewardsDisciplines' => $rewardsDisciplines,
            'assets'             => $assets,
            'documents'          => $documents,
            'offboarding'        => $offboarding,
            'exitSurvey'         => $exitSurvey,
            'ppeSuggestions'     => $ppeSuggestions,
            'activeTab'          => $activeTab
        ]);
    }

    /**
     * WIZARD TIẾP NHẬN NHÂN VIÊN MỚI 4 BƯỚC
     */
    public function wizard(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI']);
        $this->requirePermission('nhansu.create');

        $db = Database::getInstance();

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->validateCsrf();

            try {
                $newMaNV = $this->employeeModel->createFullEmployeeWithWizard($_POST);
                Session::setFlash('success', "Tiếp nhận nhân sự mới thành công! Mã nhân sự: [{$newMaNV}].", 'success');
                $this->redirect('employees/show/' . $newMaNV);
            } catch (DomainException | InvalidArgumentException $e) {
                Session::setFlash('error', $e->getMessage(), 'danger');
                // Trả lại form với dữ liệu đã điền
            } catch (Throwable $e) {
                Session::setFlash('error', 'Lỗi tiếp nhận nhân sự: ' . $e->getMessage(), 'danger');
            }
        }

        $selectableUnits = $this->orgUnitModel->getSelectableUnitsForUser();
        $positions = $db->query("SELECT MaCV, TenCV, HeSoCV, PhuCapCV FROM dm_chucvu WHERE TrangThai = 1 ORDER BY CapBac ASC, TenCV ASC")->fetchAll(PDO::FETCH_ASSOC);
        $managers = $db->query("SELECT MaNV, HoTen, TenCV, TenDV FROM nhansu ns LEFT JOIN dm_donvi dv ON ns.MaDV = dv.MaDV LEFT JOIN dm_chucvu cv ON ns.MaCV = cv.MaCV WHERE ns.TrangThai = 1 ORDER BY ns.HoTen ASC")->fetchAll(PDO::FETCH_ASSOC);

        $this->view('employees/wizard', [
            'title'           => 'Wizard Tiếp nhận Nhân viên Mới - Vinamilk HRM',
            'selectableUnits' => $selectableUnits,
            'positions'       => $positions,
            'managers'        => $managers,
            'old'             => $_POST
        ]);
    }

    /**
     * ALIAS CHO WIZARD TIẾP NHẬN
     */
    public function create(): void
    {
        $this->wizard();
    }

    /**
     * TẠO HỢP ĐỒNG LAO ĐỘNG MỚI (Có kiểm tra chặn lần 3 xác định thời hạn)
     */
    public function create_contract(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI']);
        $this->requirePermission('nhansu.edit');
        $this->validateCsrf();

        $maNV = trim($_POST['ma_nv'] ?? '');
        if (empty($maNV)) {
            $this->redirect('employees');
        }

        try {
            $contractId = $this->contractModel->createContract($_POST);
            Session::setFlash('success', 'Đã lập hợp đồng lao động mới thành công!', 'success');
        } catch (DomainException $e) {
            // Chặn ký HĐ xác định thời hạn lần 3
            Session::setFlash('error', $e->getMessage(), 'danger');
        } catch (Throwable $e) {
            Session::setFlash('error', 'Lỗi khi lập hợp đồng: ' . $e->getMessage(), 'danger');
        }

        $this->redirect('employees/show/' . $maNV . '?tab=hop-dong');
    }

    /**
     * IN HỢP ĐỒNG LAO ĐỘNG CHUẨN BIỂU MẪU VINAMILK (A4 Print Ready)
     */
    public function print_contract(string $contractId = ''): void
    {
        $this->requireAuth();
        $this->requirePermission('nhansu.view');

        $id = (int)$contractId;
        if ($id <= 0) {
            $this->redirect('employees');
        }

        $contract = $this->contractModel->getContractById($id);
        if (!$contract) {
            Session::setFlash('error', 'Hợp đồng lao động không tồn tại.', 'danger');
            $this->redirect('employees');
        }

        // Kiểm tra quyền scopeByUnit với nhân viên được in
        $empCheck = $this->employeeModel->getEmployeeById($contract['ma_nv']);
        if (!$empCheck) {
            Session::setFlash('error', 'Bạn không có quyền truy cập hợp đồng này.', 'danger');
            $this->redirect('employees');
        }

        $this->view('employees/print_contract', [
            'title'    => 'In Hợp đồng Lao động - ' . $contract['so_hd'],
            'contract' => $contract
        ], 'blank'); // Layout blank chuyên dụng in A4
    }

    /**
     * UPLOAD TÀI LIỆU SCAN (PDF / ẢNH <= 5MB, LƯU NGOÀI PUBLIC/)
     */
    public function upload_document(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI']);
        $this->requirePermission('nhansu.edit');
        $this->validateCsrf();

        $maNV = trim($_POST['ma_nv'] ?? '');
        if (empty($maNV) || empty($_FILES['document'])) {
            Session::setFlash('error', 'Dữ liệu tải lên không hợp lệ.', 'danger');
            $this->redirect('employees');
        }

        // Kiểm tra phạm vi đơn vị
        $emp = $this->employeeModel->getEmployeeById($maNV);
        if (!$emp) {
            Session::setFlash('error', 'Không có quyền thao tác trên nhân sự này.', 'danger');
            $this->redirect('employees');
        }

        try {
            $docId = $this->documentModel->uploadDocument(
                $maNV,
                $_FILES['document'],
                trim($_POST['ten_tai_lieu'] ?? 'Tài liệu hồ sơ'),
                trim($_POST['loai_tai_lieu'] ?? 'KHAC'),
                trim($_POST['ghi_chu'] ?? '')
            );
            Session::setFlash('success', 'Đã tải lên và lưu trữ an toàn tài liệu scan!', 'success');
        } catch (Throwable $e) {
            Session::setFlash('error', 'Lỗi tải lên tài liệu: ' . $e->getMessage(), 'danger');
        }

        $this->redirect('employees/show/' . $maNV . '?tab=tai-lieu');
    }

    /**
     * TẢI / XEM TÀI LIỆU SCAN BẢO MẬT (Kiểm tra quyền truy cập)
     */
    public function download_document(): void
    {
        $this->requireAuth();

        $docId = (int)($_GET['id'] ?? 0);
        if ($docId <= 0) {
            http_response_code(400);
            die("Yêu cầu không hợp lệ.");
        }

        $doc = $this->documentModel->getDocumentById($docId);
        if (!$doc) {
            http_response_code(404);
            die("Không tìm thấy tài liệu.");
        }

        // Kiểm tra quyền: Phải là chính mình HOẶC có quyền xem nhân sự thuộc scope
        $isSelf = (Auth::maNV() === $doc['ma_nv']);
        $canAccess = $isSelf || (Auth::can('nhansu.view') && $this->employeeModel->getEmployeeById($doc['ma_nv']) !== null);

        if (!$canAccess) {
            http_response_code(403);
            die("Bạn không có quyền truy cập hoặc tải tài liệu này.");
        }

        $filePath = ROOT_DIR . DIRECTORY_SEPARATOR . str_replace('/', DIRECTORY_SEPARATOR, $doc['file_path']);
        if (!file_exists($filePath)) {
            http_response_code(404);
            die("File tài liệu vật lý không tồn tại trên máy chủ.");
        }

        // Truyền file an toàn ra browser
        $disposition = isset($_GET['preview']) ? 'inline' : 'attachment';
        header('Content-Type: ' . $doc['mime_type']);
        header('Content-Disposition: ' . $disposition . '; filename="' . rawurlencode($doc['file_name_original']) . '"');
        header('Content-Length: ' . filesize($filePath));
        header('Cache-Control: private, max-age=0, must-revalidate');
        header('Pragma: public');
        header('X-Content-Type-Options: nosniff');

        readfile($filePath);
        exit;
    }

    /**
     * THÊM KHEN THƯỞNG / KỶ LUẬT (Tab 6)
     */
    public function add_ktkl(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI']);
        $this->requirePermission('nhansu.edit');
        $this->validateCsrf();

        $maNV = trim($_POST['ma_nv'] ?? '');
        if (empty($maNV)) {
            $this->redirect('employees');
        }

        try {
            $this->employeeModel->addRewardDiscipline($_POST);
            Session::setFlash('success', 'Đã lưu quyết định Khen thưởng / Kỷ luật thành công!', 'success');
        } catch (Throwable $e) {
            Session::setFlash('error', 'Lỗi: ' . $e->getMessage(), 'danger');
        }

        $this->redirect('employees/show/' . $maNV . '?tab=kt-kl');
    }

    /**
     * THÊM CHỨNG CHỈ CHO NHÂN VIÊN (Tab 5)
     */
    public function add_certificate(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI']);
        $this->requirePermission('nhansu.edit');
        $this->validateCsrf();

        $maNV = trim($_POST['ma_nv'] ?? '');
        if (empty($maNV)) {
            $this->redirect('employees');
        }

        try {
            $this->certService->saveCertificate($_POST);
            Session::setFlash('success', 'Đã lưu chứng chỉ / Thẻ xanh ATTP thành công!', 'success');
        } catch (Throwable $e) {
            Session::setFlash('error', 'Lỗi lưu chứng chỉ: ' . $e->getMessage(), 'danger');
        }

        $this->redirect('employees/show/' . $maNV . '?tab=dao-tao');
    }

    /**
     * CẤP PHÁT TÀI SẢN MỚI (Tab 7)
     */
    public function add_asset(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI']);
        $this->requirePermission('nhansu.edit');
        $this->validateCsrf();

        $maNV = trim($_POST['ma_nv'] ?? '');
        if (empty($maNV)) {
            $this->redirect('employees');
        }

        try {
            $this->assetService->allocateAsset($_POST);
            Session::setFlash('success', 'Đã cấp phát tài sản / PPE thành công! Đã tạo biên bản bàn giao điện tử.', 'success');
        } catch (Throwable $e) {
            Session::setFlash('error', 'Lỗi cấp phát tài sản: ' . $e->getMessage(), 'danger');
        }

        $this->redirect('employees/show/' . $maNV . '?tab=tai-san');
    }

    /**
     * KÝ NHẬN BÀN GIAO TÀI SẢN (Tab 7)
     */
    public function sign_asset(): void
    {
        $this->requireAuth();
        $this->validateCsrf();

        $maNV = trim($_POST['ma_nv'] ?? '');
        $assetId = (int)($_POST['asset_id'] ?? 0);

        try {
            $this->assetService->signAllocation($assetId, Auth::username() ?? $maNV);
            Session::setFlash('success', 'Đã ký nhận biên bản bàn giao tài sản thành công!', 'success');
        } catch (Throwable $e) {
            Session::setFlash('error', 'Lỗi ký nhận tài sản: ' . $e->getMessage(), 'danger');
        }

        $this->redirect('employees/show/' . $maNV . '?tab=tai-san');
    }

    /**
     * THU HỒI HOẶC BÁO MẤT/HỎNG TÀI SẢN (Tab 7)
     */
    public function return_asset(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI']);
        $this->requirePermission('nhansu.edit');
        $this->validateCsrf();

        $maNV = trim($_POST['ma_nv'] ?? '');
        $assetId = (int)($_POST['asset_id'] ?? 0);
        $status = $_POST['status'] ?? 'DA_THU_HOI';
        $penalty = !empty($_POST['penalty_amount']) ? (float)$_POST['penalty_amount'] : null;
        $cond = $_POST['condition'] ?? '';
        $note = $_POST['notes'] ?? '';

        try {
            $this->assetService->returnOrRecoverAsset($assetId, $status, $penalty, $cond, $note);
            $msg = ($status === 'DA_THU_HOI') 
                ? 'Đã xác nhận thu hồi tài sản thành công!' 
                : 'Đã báo mất/hỏng và ghi nhận số tiền khấu trừ lương: ' . number_format($penalty ?? 0, 0, ',', '.') . 'đ.';
            Session::setFlash('success', $msg, 'success');
        } catch (Throwable $e) {
            Session::setFlash('error', 'Lỗi xử lý tài sản: ' . $e->getMessage(), 'danger');
        }

        $this->redirect('employees/show/' . $maNV . '?tab=tai-san');
    }

    /**
     * KHỞI TẠO QUY TRÌNH THÔI VIỆC (Tab 9)
     */
    public function init_offboarding(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI']);
        $this->requirePermission('nhansu.edit');
        $this->validateCsrf();

        $maNV = trim($_POST['ma_nv'] ?? '');
        if (empty($maNV)) {
            $this->redirect('employees');
        }

        try {
            $offId = $this->offboardingService->createOffboarding($_POST);
            Session::setFlash('success', 'Đã khởi tạo quy trình thôi việc cho nhân sự!', 'success');
            $this->redirect('employees/show/' . $maNV . '?tab=offboarding');
        } catch (Throwable $e) {
            Session::setFlash('error', 'Lỗi khởi tạo thôi việc: ' . $e->getMessage(), 'danger');
            $this->redirect('employees/show/' . $maNV);
        }
    }

    /**
     * AJAX TÌM KIẾM NHANH NHÂN VIÊN
     */
    public function quick_search(): void
    {
        $this->requireAuth();
        header('Content-Type: application/json; charset=utf-8');
        $q = trim($_GET['q'] ?? '');
        if (strlen($q) < 1) {
            echo json_encode([]);
            exit;
        }

        $results = $this->employeeModel->quickSearch($q, 8);
        echo json_encode($results, JSON_UNESCAPED_UNICODE);
        exit;
    }
}
