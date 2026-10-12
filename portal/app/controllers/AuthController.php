<?php
/**
 * =====================================================================
 * VINAMILK HRM - CONTROLLER XÁC THỰC & BẢO MẬT (AuthController)
 * =====================================================================
 * Tuân thủ nghiêm ngặt các quy tắc bảo mật:
 *   1. Mật khẩu mã hóa Bcrypt
 *   2. Khóa tài khoản 15 phút sau 5 lần sai (dùng cột LoginAttempts)
 *   3. Bắt buộc đổi mật khẩu lần đầu (MustChangePassword)
 *   4. Session regenerate sau khi login thành công
 *   5. Timeout phiên làm việc 30 phút không hoạt động
 */

require_once __DIR__ . '/../core/Controller.php';
require_once __DIR__ . '/../models/UserModel.php';

class AuthController extends Controller
{
    private UserModel $userModel;

    public function __construct()
    {
        $this->userModel = new UserModel();
    }

    public function index(): void
    {
        $this->login();
    }

    /**
     * MÀN HÌNH ĐĂNG NHẬP
     */
    public function login(): void
    {
        // Nếu đã đăng nhập và không bị bắt đổi mật khẩu -> chuyển thẳng tới dashboard
        if (Auth::check()) {
            if (Auth::mustChangePassword()) {
                $this->redirect('auth/change_password');
            }
            $this->redirect('dashboard');
        }

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            // Xác thực CSRF Token bắt buộc
            $this->validateCsrf();

            $username = trim($_POST['username'] ?? '');
            $password = trim($_POST['password'] ?? '');

            $twoFactorCode = trim($_POST['two_factor_code'] ?? '');

            // 1. Gọi Web API .NET (VinamilkHRM.WebApi) qua ApiClient
            require_once __DIR__ . '/../services/ApiClient.php';
            $apiClient = new \App\Services\ApiClient();
            $apiResult = $apiClient->login($username, $password, $twoFactorCode ?: null);

            if ($apiResult['status'] === 'SUCCESS') {
                $profile = $apiResult['profile'] ?? [];
                $user = [
                    'ID' => $profile['userId'] ?? 1,
                    'Username' => $profile['userName'] ?? $username,
                    'HoTen' => $profile['fullName'] ?? $username,
                    'Email' => $profile['email'] ?? null,
                    'RoleCode' => !empty($profile['roles']) ? $profile['roles'][0] : 'Employee',
                    'RoleName' => !empty($profile['roles']) ? $profile['roles'][0] : 'Employee',
                    'MaNV' => !empty($profile['employeeId']) ? 'VNM-' . str_pad((string)$profile['employeeId'], 4, '0', STR_PAD_LEFT) : 'VNM-0001',
                    'MustChangePassword' => !empty($apiResult['data']['mustChangePassword']) ? 1 : 0
                ];

                Auth::login($user);

                if (!empty($user['MustChangePassword'])) {
                    Session::setFlash('warning', 'Xin chào ' . ($user['HoTen'] ?: $user['Username']) . '! Bạn bắt buộc phải đổi mật khẩu mới trong lần đăng nhập đầu tiên.', 'warning');
                    $this->redirect('auth/change_password');
                }

                Session::setFlash('success', 'Xin chào ' . ($user['HoTen'] ?: $user['Username']) . ', chào mừng bạn đến với Vinamilk HRM!', 'success');
                $this->redirect('dashboard');
            } elseif ($apiResult['status'] === 'REQUIRES_2FA') {
                Session::setFlash('login_error', 'Tài khoản yêu cầu mã xác thực 2 bước (TOTP 2FA). Vui lòng nhập mã xác thực.', 'warning');
                $this->view('auth/login', [
                    'title' => 'Xác thực 2 bước 2FA - Vinamilk HRM',
                    'requires2Fa' => true,
                    'username' => $username
                ], null);
                return;
            } elseif ($apiResult['status'] === 'LOCKED') {
                Session::setFlash('login_error', $apiResult['message'], 'danger');
                $this->redirect('auth/login');
            } else {
                // Fallback nếu API ngoại tuyến hoặc kiểm tra tài khoản legacy
                $authResult = $this->userModel->authenticate($username, $password);
                if ($authResult['status'] === 'SUCCESS') {
                    $user = $authResult['user'];
                    Auth::login($user);
                    if (!empty($user['MustChangePassword'])) {
                        Session::setFlash('warning', 'Bạn bắt buộc phải đổi mật khẩu mới.', 'warning');
                        $this->redirect('auth/change_password');
                    }
                    Session::setFlash('success', 'Chào mừng quay trở lại!', 'success');
                    $this->redirect('dashboard');
                } else {
                    $alertType = ($authResult['status'] === 'LOCKED') ? 'danger' : 'warning';
                    Session::setFlash('login_error', $apiResult['message'] ?: $authResult['message'], $alertType);
                    $this->redirect('auth/login');
                }
            }
        }

        $this->view('auth/login', [
            'title' => 'Đăng nhập - Vinamilk HRM Enterprise'
        ], null); // Login view độc lập
    }

    /**
     * MÀN HÌNH ĐỔI MẬT KHẨU (BẮT BUỘC LẦN ĐẦU HOẶC NGƯỜI DÙNG TỰ ĐỔI)
     */
    public function change_password(): void
    {
        // Yêu cầu đăng nhập, cho phép truy cập ngay cả khi cờ MustChangePassword đang bật
        $this->requireAuth(true);

        $user = Auth::user();

        if ($_SERVER['REQUEST_METHOD'] === 'POST') {
            $this->validateCsrf();

            $currentPassword = (string)($_POST['current_password'] ?? '');
            $newPassword     = (string)($_POST['new_password'] ?? '');
            $confirmPassword = (string)($_POST['confirm_password'] ?? '');

            // 1. Kiểm tra không để trống
            if (empty($currentPassword) || empty($newPassword) || empty($confirmPassword)) {
                Session::setFlash('error', 'Vui lòng điền đầy đủ tất cả các trường mật khẩu.', 'danger');
                $this->redirect('auth/change_password');
            }

            // 2. Xác thực mật khẩu hiện tại
            $dbUser = $this->userModel->findByUsername($user['Username']);
            if (!$dbUser || !password_verify($currentPassword, $dbUser['Password'])) {
                Session::setFlash('error', 'Mật khẩu hiện tại không chính xác.', 'danger');
                $this->redirect('auth/change_password');
            }

            // 3. Kiểm tra mật khẩu mới không được trùng mật khẩu cũ
            if (password_verify($newPassword, $dbUser['Password'])) {
                Session::setFlash('error', 'Mật khẩu mới không được trùng với mật khẩu hiện tại.', 'danger');
                $this->redirect('auth/change_password');
            }

            // 4. Kiểm tra mật khẩu xác nhận
            if ($newPassword !== $confirmPassword) {
                Session::setFlash('error', 'Mật khẩu xác nhận không khớp với mật khẩu mới.', 'danger');
                $this->redirect('auth/change_password');
            }

            // 5. Kiểm tra độ phức tạp của mật khẩu mới (Tối thiểu 8 ký tự, gồm hoa, thường, số, ký tự đặc biệt)
            if (strlen($newPassword) < 8 ||
                !preg_match('/[A-Z]/', $newPassword) ||
                !preg_match('/[a-z]/', $newPassword) ||
                !preg_match('/[0-9]/', $newPassword) ||
                !preg_match('/[@$!%*?&#^()_\-+=\[\]{}|]/', $newPassword)) {
                Session::setFlash('error', 'Mật khẩu mới phải có tối thiểu 8 ký tự, bao gồm ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt.', 'danger');
                $this->redirect('auth/change_password');
            }

            // 6. Cập nhật mật khẩu mới bằng Bcrypt
            if ($this->userModel->changePassword((int)$user['ID'], $newPassword)) {
                // Cập nhật lại session người dùng (tắt cờ MustChangePassword)
                $user['MustChangePassword'] = 0;
                Session::set(defined('AUTH_SESSION_KEY') ? AUTH_SESSION_KEY : 'vnm_logged_user', $user);

                Session::setFlash('success', 'Đổi mật khẩu thành công! Tài khoản của bạn đã được bảo vệ an toàn.', 'success');
                $this->redirect('dashboard');
            } else {
                Session::setFlash('error', 'Có lỗi xảy ra trong quá trình đổi mật khẩu. Vui lòng thử lại.', 'danger');
                $this->redirect('auth/change_password');
            }
        }

        $this->view('auth/change_password', [
            'title' => 'Đổi Mật Khẩu - Vinamilk HRM Enterprise',
            'user'  => $user,
            'isFirstTime' => Auth::mustChangePassword()
        ], 'master');
    }

    /**
     * ĐĂNG XUẤT HỆ THỐNG
     */
    public function logout(): void
    {
        if (Auth::check()) {
            $user = Auth::user();
            $this->userModel->logAudit('LOGOUT', 'users', Auth::id() ?? 0, null, [
                'Username' => $user['Username'] ?? '',
                'MaNV' => $user['MaNV'] ?? ''
            ]);

            // Thu hồi token trên Web API .NET
            require_once __DIR__ . '/../services/ApiClient.php';
            $apiClient = new \App\Services\ApiClient();
            $apiClient->revoke();

            Auth::logout();
            Session::setFlash('info', 'Bạn đã đăng xuất an toàn khỏi hệ thống Vinamilk HRM.', 'info');
        }
        $this->redirect('auth/login');
    }
}
