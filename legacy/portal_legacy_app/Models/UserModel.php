<?php
/**
 * =====================================================================
 * VINAMILK HRM - MODEL TÀI KHOẢN NGƯỜI DÙNG & BẢO MẬT (users)
 * =====================================================================
 * Tuân thủ bảo mật cấp độ doanh nghiệp:
 *   1. Mật khẩu mã hóa Bcrypt
 *   2. Khóa tài khoản 15 phút sau 5 lần đăng nhập thất bại
 *   3. Kiểm tra cờ bắt buộc đổi mật khẩu lần đầu (MustChangePassword)
 *   4. Ghi vết Audit Log cho mọi hoạt động đăng nhập/đổi mật khẩu
 */

require_once __DIR__ . '/../core/Model.php';

class UserModel extends Model
{
    protected string $table = 'users';
    protected string $primaryKey = 'ID';

    public const MAX_LOGIN_ATTEMPTS = 5;
    public const LOCKOUT_MINUTES    = 15;

    /**
     * TÌM TÀI KHOẢN THEO USERNAME KÈM THÔNG TIN VAI TRÒ VÀ NHÂN SỰ
     */
    public function findByUsername(string $username): ?array
    {
        $sql = "SELECT u.*, r.RoleCode as R_Code, r.RoleName, ns.HoTen, ns.MaDV as NS_MaDV, ns.Email as NS_Email
                FROM users u
                LEFT JOIN roles r ON u.RoleID = r.RoleID
                LEFT JOIN nhansu ns ON u.MaNV = ns.MaNV
                WHERE u.Username = :username
                LIMIT 1";

        $row = $this->fetchOne($sql, [':username' => $username]);
        if (!$row) return null;

        if (empty($row['RoleCode'])) {
            $row['RoleCode'] = $row['R_Code'] ?? 'NHANVIEN';
        }
        if (empty($row['MaDV'])) {
            $row['MaDV'] = $row['NS_MaDV'] ?? 'VINAMILK';
        }

        return $row;
    }

    /**
     * XÁC THỰC NGƯỜI DÙNG VỚI CƠ CHẾ KHÓA 15 PHÚT SAU 5 LẦN SAI
     * 
     * @return array [
     *   'status' => 'SUCCESS' | 'LOCKED' | 'FAILED' | 'INACTIVE',
     *   'user' => ?array,
     *   'message' => string,
     *   'remaining_lockout' => ?int (phút còn lại)
     * ]
     */
    public function authenticate(string $username, string $password): array
    {
        $user = $this->findByUsername($username);

        // 1. Kiểm tra tài khoản tồn tại
        if (!$user) {
            return [
                'status'  => 'FAILED',
                'user'    => null,
                'message' => 'Tên đăng nhập hoặc mật khẩu không chính xác.'
            ];
        }

        // 2. Kiểm tra trạng thái hoạt động của tài khoản
        if ((int)$user['IsActive'] !== 1) {
            return [
                'status'  => 'INACTIVE',
                'user'    => null,
                'message' => 'Tài khoản của bạn đã bị khóa hoặc ngừng hoạt động. Vui lòng liên hệ quản trị viên.'
            ];
        }

        $now = new DateTime();

        // 3. Kiểm tra xem tài khoản có đang bị khóa 15 phút không
        if (!empty($user['LockoutUntil'])) {
            $lockoutTime = new DateTime($user['LockoutUntil']);
            if ($lockoutTime > $now) {
                $diff = $now->diff($lockoutTime);
                $remainingMinutes = ($diff->i) + ($diff->s > 0 ? 1 : 0);
                return [
                    'status'            => 'LOCKED',
                    'user'              => null,
                    'remaining_lockout' => $remainingMinutes,
                    'message'           => "Tài khoản đang bị tạm khóa do nhập sai mật khẩu quá 5 lần. Vui lòng thử lại sau {$remainingMinutes} phút."
                ];
            } else {
                // Đã hết 15 phút khóa -> tự động mở khóa và reset số lần sai
                $this->resetLoginAttempts((int)$user['ID']);
                $user['LoginAttempts'] = 0;
                $user['LockoutUntil']  = null;
            }
        }

        // 4. Kiểm tra mật khẩu Bcrypt
        if (password_verify($password, $user['Password'])) {
            // Đăng nhập đúng: reset số lần sai, cập nhật LastLogin
            $this->updateLoginSuccess((int)$user['ID']);
            $this->logAudit('LOGIN', 'users', $user['ID'], null, ['Username' => $username]);

            return [
                'status'  => 'SUCCESS',
                'user'    => $user,
                'message' => 'Đăng nhập thành công.'
            ];
        }

        // 5. Mật khẩu không đúng -> Tăng số lần thử sai
        $attempts = (int)$user['LoginAttempts'] + 1;
        $isLocked = false;
        $lockoutUntil = null;

        if ($attempts >= self::MAX_LOGIN_ATTEMPTS) {
            // Khóa tài khoản 15 phút
            $lockoutUntil = (new DateTime())->modify('+' . self::LOCKOUT_MINUTES . ' minutes')->format('Y-m-d H:i:s');
            $this->recordLoginFailure((int)$user['ID'], $attempts, $lockoutUntil);
            $this->logAudit('ACCOUNT_LOCKED', 'users', $user['ID'], null, [
                'Username' => $username,
                'Attempts' => $attempts,
                'LockoutUntil' => $lockoutUntil
            ]);

            return [
                'status'            => 'LOCKED',
                'user'              => null,
                'remaining_lockout' => self::LOCKOUT_MINUTES,
                'message'           => "Bạn đã nhập sai mật khẩu 5 lần liên tiếp. Tài khoản đã bị tạm khóa 15 phút để bảo mật."
            ];
        } else {
            $this->recordLoginFailure((int)$user['ID'], $attempts, null);
            $remaining = self::MAX_LOGIN_ATTEMPTS - $attempts;

            return [
                'status'   => 'FAILED',
                'user'     => null,
                'attempts' => $attempts,
                'message'  => "Mật khẩu không chính xác. Bạn còn {$remaining} lần thử trước khi tài khoản bị khóa 15 phút."
            ];
        }
    }

    /**
     * GHI NHẬN ĐĂNG NHẬP THẤT BẠI
     */
    public function recordLoginFailure(int $userId, int $attempts, ?string $lockoutUntil): void
    {
        $sql = "UPDATE users SET LoginAttempts = :attempts, LockoutUntil = :lockout WHERE ID = :id";
        $this->execute($sql, [
            ':attempts' => $attempts,
            ':lockout'  => $lockoutUntil,
            ':id'       => $userId
        ]);
    }

    /**
     * ĐẶT LẠI SỐ LẦN SAI & GỠ KHÓA
     */
    public function resetLoginAttempts(int $userId): void
    {
        $sql = "UPDATE users SET LoginAttempts = 0, LockoutUntil = NULL WHERE ID = :id";
        $this->execute($sql, [':id' => $userId]);
    }

    /**
     * CẬP NHẬT KHI ĐĂNG NHẬP THÀNH CÔNG
     */
    public function updateLoginSuccess(int $userId): void
    {
        $sql = "UPDATE users SET LastLogin = NOW(), LoginAttempts = 0, LockoutUntil = NULL WHERE ID = :id";
        $this->execute($sql, [':id' => $userId]);
    }

    /**
     * ĐỔI MẬT KHẨU NGƯỜI DÙNG (BCRYPT)
     */
    public function changePassword(int $userId, string $newPassword): bool
    {
        $hash = password_hash($newPassword, PASSWORD_BCRYPT, ['cost' => 10]);
        $sql = "UPDATE users 
                SET Password = :pass, MustChangePassword = 0, LoginAttempts = 0, LockoutUntil = NULL, UpdatedAt = NOW() 
                WHERE ID = :id";

        $ok = $this->execute($sql, [
            ':pass' => $hash,
            ':id'   => $userId
        ]);

        if ($ok) {
            $this->logAudit('CHANGE_PASSWORD', 'users', $userId, null, ['Action' => 'Password changed successfully']);
        }

        return $ok;
    }
}
