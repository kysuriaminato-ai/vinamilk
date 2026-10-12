<?php
/**
 * =====================================================================
 * VINAMILK HRM - CONTROLLER PHÊ DUYỆT ĐƠN TỪ (ApprovalController)
 * =====================================================================
 */

require_once __DIR__ . '/../core/Controller.php';
require_once __DIR__ . '/../models/ApprovalModel.php';

class ApprovalController extends Controller
{
    private ApprovalModel $approvalModel;

    public function __construct()
    {
        $this->approvalModel = new ApprovalModel();
    }

    /**
     * HỘP THƯ DUYỆT ĐƠN & THEO DÕI ĐƠN TỪ
     */
    public function index(): void
    {
        $this->requireAuth();

        $user = Auth::user();
        $role = strtoupper(Auth::role());

        $isManager = in_array($role, ['ADMIN', 'HR_TAPDOAN', 'QUANLY_TRUCTIEP', 'MANAGER']);

        $pendingApprovals = [];
        if ($isManager) {
            $pendingApprovals = $this->approvalModel->getPendingForManager($user['MaNV'] ?? '', $user['MaDV'] ?? '');
        }

        $myRequests = $this->approvalModel->getMyRequests($user['MaNV'] ?? '');

        $this->view('approvals/index', [
            'title'            => 'Hộp Thư Phê Duyệt & Đơn Từ - Vinamilk HRM',
            'isManager'        => $isManager,
            'pendingApprovals' => $pendingApprovals,
            'myRequests'       => $myRequests
        ]);
    }

    /**
     * GỬI ĐƠN NGHỈ PHÉP / ĐỀ XUẤT MỚI (ESS)
     */
    public function create(): void
    {
        $this->requireAuth();

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->validateCsrf();

            $data = [
                'ma_nv'         => Auth::maNV() ?: 'VNM-0001',
                'loai_don'      => trim($_POST['loai_don'] ?? 'NGHI_PHEP'),
                'ngay_bat_dau'  => trim($_POST['ngay_bat_dau'] ?? date('Y-m-d')),
                'ngay_ket_thuc' => trim($_POST['ngay_ket_thuc'] ?? date('Y-m-d')),
                'so_ngay'       => (float)($_POST['so_ngay'] ?? 1.0),
                'ly_do'         => trim($_POST['ly_do'] ?? '')
            ];

            if (empty($data['ly_do'])) {
                Session::setFlash('error', 'Vui lòng nhập rõ lý do xin nghỉ hoặc đề xuất.', 'danger');
                $this->redirect('approvals');
            }

            if ($this->approvalModel->submitRequest($data)) {
                Session::setFlash('success', 'Đã gửi đơn thành công đến Quản lý trực tiếp!', 'success');
            } else {
                Session::setFlash('error', 'Không thể gửi đơn, vui lòng thử lại.', 'danger');
            }
            $this->redirect('approvals');
        }

        $this->redirect('approvals');
    }

    /**
     * PHÊ DUYỆT ĐƠN (POST)
     */
    public function approve(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'QUANLY_TRUCTIEP', 'MANAGER']);
        $this->validateCsrf();

        $id = (int)($_POST['id'] ?? 0);
        $comments = trim($_POST['comments'] ?? 'Đã phê duyệt chấp thuận');

        if ($this->approvalModel->approve($id, Auth::user()['Username'] ?? 'QUANLY', $comments)) {
            Session::setFlash('success', "Đã phê duyệt thành công đơn #{$id}!", 'success');
        } else {
            Session::setFlash('error', "Không thể phê duyệt đơn #{$id}.", 'danger');
        }
        $this->redirect('approvals');
    }

    /**
     * TỪ CHỐI ĐƠN (POST)
     */
    public function reject(): void
    {
        $this->requireRole(['ADMIN', 'HR_TAPDOAN', 'QUANLY_TRUCTIEP', 'MANAGER']);
        $this->validateCsrf();

        $id = (int)($_POST['id'] ?? 0);
        $comments = trim($_POST['comments'] ?? 'Không chấp thuận');

        if ($this->approvalModel->reject($id, Auth::user()['Username'] ?? 'QUANLY', $comments)) {
            Session::setFlash('warning', "Đã từ chối đơn #{$id}.", 'warning');
        } else {
            Session::setFlash('error', "Không thể cập nhật đơn #{$id}.", 'danger');
        }
        $this->redirect('approvals');
    }
}
