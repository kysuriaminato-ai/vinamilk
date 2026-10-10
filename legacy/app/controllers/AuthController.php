<?php
/**
 * =====================================================================
 * VINAMILK HRM - Authentication Controller
 * =====================================================================
 * 
 * Controller xử lý các luồng xác thực: Đăng nhập, Đăng xuất, 
 * Thông tin cá nhân (Profile) và Đổi mật khẩu tài khoản.
 */

require_once APP_DIR . '/core/Controller.php';

class AuthController extends Controller {

    /**
     * Trang đăng nhập (GET: Hiển thị form, POST: Xử lý đăng nhập)
     */
    public function login(): void {
        // Nếu đã đăng nhập rồi -> chuyển hướng thẳng tới Dashboard
        if (Auth::check()) {
            $this->redirect('dashboard');
        }

        // Xử lý khi Form submit POST
        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->validateCsrf();

            $username = trim($_POST['username'] ?? '');
            $password = trim($_POST['password'] ?? '');

            if (empty($username) || empty($password)) {
                Session::setFlash('login_error', 'Vui lòng nhập đầy đủ Tên đăng nhập và Mật khẩu.', 'danger');
                $this->redirect('auth/login');
            }

            /** @var User $userModel */
            $userModel = $this->model('User');
            $user = $userModel->getByUsername($username);

            /** @var AuditLog $auditLog */
            $auditLog = $this->model('AuditLog');

            if (!$user) {
                $auditLog->log('Login', 'Failed', null, $username, 'Tài khoản không tồn tại.');
                Session::setFlash('login_error', 'Tên đăng nhập hoặc mật khẩu không chính xác.', 'danger');
                $this->redirect('auth/login');
            }

            // Kiểm tra trạng thái kích hoạt tài khoản
            if ((int)$user['IsActive'] !== 1) {
                $auditLog->log('Login', 'Blocked', $user['ID'], $username, 'Tài khoản đã bị khóa.');
                Session::setFlash('login_error', 'Tài khoản này đang tạm thời bị khóa. Vui lòng liên hệ Admin.', 'warning');
                $this->redirect('auth/login');
            }

            // Kiểm tra mật khẩu hash
            if (!$userModel->verifyPassword($password, $user['Password'])) {
                $userModel->incrementFailedAttempts($user['ID']);
                $auditLog->log('Login', 'Failed', $user['ID'], $username, 'Mật khẩu sai.');
                Session::setFlash('login_error', 'Tên đăng nhập hoặc mật khẩu không chính xác.', 'danger');
                $this->redirect('auth/login');
            }

            // Đăng nhập thành công -> Lấy danh sách quyền hạn chi tiết
            $permissions = $userModel->getUserPermissions((int)$user['RoleID']);

            // Lưu thông tin đăng nhập vào Session (Ẩn hash password)
            unset($user['Password']);
            Auth::login($user, $permissions);

            // Cập nhật LastLogin trong DB
            $userModel->updateLastLogin($user['ID']);

            // Ghi nhật ký truy cập
            $auditLog->log('Login', 'Success', $user['ID'], $username, "Đăng nhập thành công với vai trò {$user['RoleName']}");

            Session::setFlash('success', "Cháo mừng <strong>{$user['HoTen']}</strong> ({$user['RoleName']}) đã đăng nhập thành công vào Vinamilk HRM!", 'success');
            $this->redirect('dashboard');
        }

        // Render trang Đăng nhập độc lập (Không dùng Master Layout)
        $this->view('auth/login', [
            'title' => 'Đăng nhập - Vinamilk HRM System'
        ], null);
    }

    /**
     * Đăng xuất tài khoản
     */
    public function logout(): void {
        if (Auth::check()) {
            /** @var AuditLog $auditLog */
            $auditLog = $this->model('AuditLog');
            $auditLog->log('Logout', 'Success', Auth::id(), Auth::user()['Username'], 'Đã đăng xuất hệ thống.');
        }

        Auth::logout();
        Session::setFlash('info', 'Bạn đã đăng xuất khỏi hệ thống Vinamilk HRM.', 'info');
        $this->redirect('auth/login');
    }

    /**
     * Trang xem thông tin cá nhân Profile
     */
    public function profile(): void {
        $this->requireAuth();
        $currentUser = Auth::user();

        /** @var User $userModel */
        $userModel = $this->model('User');
        $user = $userModel->getByUsername($currentUser['Username']);
        $permissions = $userModel->getUserPermissions((int)$user['RoleID']);

        $this->view('auth/profile', [
            'title' => 'Thông tin cá nhân & Phân quyền - Vinamilk HRM',
            'user' => $user,
            'permissions' => $permissions
        ]);
    }

    /**
     * Trang Đổi mật khẩu
     */
    public function changePassword(): void {
        $this->requireAuth();

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->validateCsrf();

            $currentPassword = trim($_POST['current_password'] ?? '');
            $newPassword = trim($_POST['new_password'] ?? '');
            $confirmPassword = trim($_POST['confirm_password'] ?? '');

            if (empty($currentPassword) || empty($newPassword) || empty($confirmPassword)) {
                Session::setFlash('password_error', 'Vui lòng điền đầy đủ tất cả các trường mật khẩu.', 'danger');
                $this->redirect('auth/changePassword');
            }

            if ($newPassword !== $confirmPassword) {
                Session::setFlash('password_error', 'Mật khẩu mới và mật khẩu xác nhận không trùng khớp.', 'danger');
                $this->redirect('auth/changePassword');
            }

            if (strlen($newPassword) < 6) {
                Session::setFlash('password_error', 'Mật khẩu mới phải có tối thiểu 6 ký tự.', 'danger');
                $this->redirect('auth/changePassword');
            }

            /** @var User $userModel */
            $userModel = $this->model('User');
            $userId = Auth::id();
            $userInfo = $userModel->find($userId);

            if (!$userModel->verifyPassword($currentPassword, $userInfo['Password'])) {
                Session::setFlash('password_error', 'Mật khẩu hiện tại không chính xác.', 'danger');
                $this->redirect('auth/changePassword');
            }

            // Cập nhật mật khẩu mới
            $userModel->changePassword($userId, $newPassword);

            /** @var AuditLog $auditLog */
            $auditLog = $this->model('AuditLog');
            $auditLog->log('ChangePassword', 'Success', $userId, Auth::user()['Username'], 'Thay đổi mật khẩu thành công.');

            Session::setFlash('success', 'Đổi mật khẩu thành công!', 'success');
            $this->redirect('auth/profile');
        }

        $this->view('auth/change_password', [
            'title' => 'Đổi mật khẩu tài khoản - Vinamilk HRM'
        ]);
    }
}
