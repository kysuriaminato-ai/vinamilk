<?php
/**
 * =====================================================================
 * VINAMILK HRM - CONTROLLER QUY TRÌNH THÔI VIỆC (OffboardingController)
 * =====================================================================
 * Quản lý checklist 6 bước, khấu trừ tài sản vào lương & khảo sát lý do nghỉ cho AI
 */

require_once __DIR__ . '/../core/Controller.php';
require_once __DIR__ . '/../services/OffboardingService.php';
require_once __DIR__ . '/../services/AssetPpeService.php';
require_once __DIR__ . '/../models/OrgUnitModel.php';

class OffboardingController extends Controller
{
    private OffboardingService $offboardingService;
    private AssetPpeService $assetService;
    private OrgUnitModel $orgUnitModel;

    public function __construct()
    {
        $this->offboardingService = new OffboardingService();
        $this->assetService = new AssetPpeService();
        $this->orgUnitModel = new OrgUnitModel();
    }

    /**
     * DANH SÁCH HỒ SƠ THÔI VIỆC OFFBOARDING TOÀN CÔNG TY
     */
    public function index(): void
    {
        $this->requireAuth();
        $this->requirePermission('nhansu.view');

        $filters = [
            'trang_thai' => $_GET['trang_thai'] ?? '',
            'ma_dv'      => $_GET['ma_dv'] ?? ($_GET['unit_scope'] ?? ''),
            'keyword'    => trim($_GET['keyword'] ?? '')
        ];

        $list = $this->offboardingService->getAllOffboarding($filters);
        $selectableUnits = $this->orgUnitModel->getSelectableUnitsForUser();
        $surveyAnalytics = $this->offboardingService->getExitSurveyAnalytics();

        $this->view('offboarding/index', [
            'title'           => 'Quy Trình Thôi Việc (Offboarding Checklist) - Vinamilk HRM',
            'list'            => $list,
            'filters'         => $filters,
            'selectableUnits' => $selectableUnits,
            'analytics'       => $surveyAnalytics
        ]);
    }

    /**
     * CHI TIẾT TIẾN ĐỘ 6 BƯỚC HỒ SƠ THÔI VIỆC
     */
    public function detail(string $id = ''): void
    {
        $this->requireAuth();
        $this->requirePermission('nhansu.view');

        $offboarding = $this->offboardingService->getOffboarding($id);
        if (!$offboarding) {
            Session::setFlash('error', "Không tìm thấy hồ sơ thôi việc [{$id}].", 'danger');
            $this->redirect('offboarding');
        }

        $maNV = $offboarding['ma_nv'];
        $assets = $this->assetService->getUnreturnedAssets($maNV);
        $allAssets = Database::getInstance()->getConnection()
            ->query("SELECT * FROM cap_phat_tai_san WHERE ma_nv = '{$maNV}' ORDER BY id DESC")
            ->fetchAll(PDO::FETCH_ASSOC);

        $survey = Database::getInstance()->getConnection()
            ->query("SELECT * FROM exit_interview_survey WHERE ma_nv = '{$maNV}' ORDER BY id DESC LIMIT 1")
            ->fetch(PDO::FETCH_ASSOC);

        $this->view('offboarding/detail', [
            'title'       => 'Chi Tiết Quy Trình Thôi Việc: ' . $offboarding['HoTen'] . ' (' . $offboarding['ma_ho_so'] . ')',
            'offboarding' => $offboarding,
            'assets'      => $assets,
            'allAssets'   => $allAssets,
            'survey'      => $survey
        ]);
    }

    /**
     * TẠO HỒ SƠ THÔI VIỆC MỚI (POST)
     */
    public function create(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI']);
        $this->requirePermission('nhansu.edit');
        $this->validateCsrf();

        try {
            $newId = $this->offboardingService->createOffboarding($_POST);
            Session::setFlash('success', 'Đã khởi tạo quy trình thôi việc thành công!', 'success');
            $this->redirect('offboarding/detail/' . $newId);
        } catch (Throwable $e) {
            Session::setFlash('error', 'Lỗi khởi tạo thôi việc: ' . $e->getMessage(), 'danger');
            $this->redirect('offboarding');
        }
    }

    /**
     * CẬP NHẬT CÁC BƯỚC CHECKLIST (POST)
     */
    public function update_step(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'HR_DONVI', 'KETOAN_LUONG', 'QUANLY_TRUCTIEP']);
        $this->validateCsrf();

        $offId = (int)($_POST['offboarding_id'] ?? 0);
        $step = (int)($_POST['step'] ?? 0);
        $confirmer = Auth::username() ?? 'HR Officer';

        try {
            switch ($step) {
                case 1:
                    $content = $_POST['content'] ?? '';
                    $this->offboardingService->completeStep1Handover($offId, $confirmer, $content);
                    Session::setFlash('success', 'Bước 1: Đã xác nhận bàn giao công việc thành công!', 'success');
                    break;

                case 2:
                    $note = $_POST['note'] ?? '';
                    $res = $this->offboardingService->completeStep2AssetRecovery($offId, $confirmer, $note);
                    $penaltyMsg = ($res['total_penalty'] > 0)
                        ? " Đã tự động chuyển {$res['unreturned_count']} tài sản nợ (" . number_format($res['total_penalty'], 0, ',', '.') . "đ) sang khấu trừ lương quyết toán!"
                        : " Đã thu hồi toàn bộ tài sản.";
                    Session::setFlash('success', 'Bước 2: Thu hồi tài sản hoàn tất.' . $penaltyMsg, 'success');
                    break;

                case 3:
                    $note = $_POST['note'] ?? '';
                    $this->offboardingService->completeStep3AccountLockout($offId, $confirmer, $note);
                    Session::setFlash('success', 'Bước 3: Đã khóa tài khoản đăng nhập người dùng thành công!', 'success');
                    break;

                case 4:
                    $days = (float)($_POST['work_days'] ?? 0);
                    $leave = (float)($_POST['remaining_leave'] ?? 0);
                    $note = $_POST['note'] ?? '';
                    $this->offboardingService->completeStep4TimesheetClose($offId, $confirmer, $days, $leave, $note);
                    Session::setFlash('success', 'Bước 4: Đã chốt công thực tế và phép tồn thành công!', 'success');
                    break;

                case 5:
                    $amount = (float)($_POST['settlement_amount'] ?? 0);
                    $note = $_POST['note'] ?? '';
                    $this->offboardingService->completeStep5FinalPayroll($offId, $confirmer, $amount, $note);
                    Session::setFlash('success', 'Bước 5: Quyết toán lương đã trừ tiền tài sản chưa trả thành công!', 'success');
                    break;

                case 6:
                    $soBhxh = trim($_POST['so_bhxh'] ?? '');
                    $returnDate = $_POST['return_date'] ?? date('Y-m-d');
                    $note = $_POST['note'] ?? '';
                    $this->offboardingService->completeStep6SocialInsurance($offId, $confirmer, $soBhxh, $returnDate, $note);
                    Session::setFlash('success', 'Bước 6: Đã chốt sổ BHXH và hoàn tất toàn bộ quy trình thôi việc!', 'success');
                    break;

                default:
                    throw new InvalidArgumentException("Bước xử lý không hợp lệ: $step");
            }
        } catch (Throwable $e) {
            Session::setFlash('error', 'Lỗi xử lý bước ' . $step . ': ' . $e->getMessage(), 'danger');
        }

        $this->redirect('offboarding/detail/' . $offId);
    }

    /**
     * LƯU KHẢO SÁT LÝ DO THÔI VIỆC (EXIT INTERVIEW SURVEY - POST)
     */
    public function save_survey(): void
    {
        $this->requireAuth();
        $this->validateCsrf();

        try {
            $this->offboardingService->saveExitSurvey($_POST);
            Session::setFlash('success', 'Đã lưu kết quả khảo sát thôi việc thành công! Dữ liệu đã được nạp vào mô hình AI Analytics.', 'success');
        } catch (Throwable $e) {
            Session::setFlash('error', 'Lỗi lưu khảo sát: ' . $e->getMessage(), 'danger');
        }

        $offId = $_POST['offboarding_id'] ?? '';
        if ($offId) {
            $this->redirect('offboarding/detail/' . $offId);
        } else {
            $this->redirect('offboarding');
        }
    }
}
