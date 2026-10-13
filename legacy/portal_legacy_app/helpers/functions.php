<?php
/**
 * =====================================================================
 * VINAMILK HRM - HÀM TIỆN ÍCH DÙNG CHUNG (HELPER FUNCTIONS)
 * =====================================================================
 */

require_once __DIR__ . '/Auth.php';

/**
 * Escape chuỗi đầu ra chống tấn công XSS (BẮT BUỘC THEO ĐẶC TẢ)
 */
if (!function_exists('e')) {
    function e(mixed $value): string {
        if ($value === null) {
            return '';
        }
        return htmlspecialchars((string)$value, ENT_QUOTES, 'UTF-8');
    }
}

/**
 * Trả về thẻ input hidden chứa CSRF Token cho form POST
 */
if (!function_exists('csrf_field')) {
    function csrf_field(): string {
        $token = Session::getCsrfToken();
        $key = defined('CSRF_TOKEN_KEY') ? CSRF_TOKEN_KEY : 'vnm_csrf_token';
        return '<input type="hidden" name="' . e($key) . '" value="' . e($token) . '">';
    }
}

/**
 * Lấy mã token CSRF hiện tại trong phiên làm việc
 */
if (!function_exists('csrf_token')) {
    function csrf_token(): string {
        return Session::getCsrfToken();
    }
}

/**
 * Tạo URL đường dẫn tuyệt đối trong ứng dụng
 */
if (!function_exists('url')) {
    function url(string $path = ''): string {
        $path = ltrim($path, '/');
        $base = defined('BASE_URL') ? BASE_URL : '';
        return $base . ($path ? '/' . $path : '');
    }
}

/**
 * Tạo URL tài nguyên static assets (CSS, JS, hình ảnh)
 */
if (!function_exists('asset')) {
    function asset(string $path = ''): string {
        $path = ltrim($path, '/');
        $base = defined('ASSETS_URL') ? ASSETS_URL : url('assets');
        return $base . '/' . $path;
    }
}

/**
 * Chuyển hướng trình duyệt
 */
if (!function_exists('redirect')) {
    function redirect(string $path): void {
        $target = (str_starts_with($path, 'http://') || str_starts_with($path, 'https://')) ? $path : url($path);
        header('Location: ' . $target);
        exit;
    }
}

/**
 * Định dạng tiền tệ VNĐ chuẩn (VD: 15.000.000 đ)
 */
if (!function_exists('format_money')) {
    function format_money(float|int|string|null $amount): string {
        if ($amount === null || $amount === '') {
            return '0 ₫';
        }
        return number_format((float)$amount, 0, ',', '.') . ' ₫';
    }
}

/**
 * Định dạng ngày theo định dạng tiêu chuẩn Việt Nam (dd/mm/YYYY)
 */
if (!function_exists('format_date')) {
    function format_date(?string $dateStr, string $format = 'd/m/Y'): string {
        if (empty($dateStr) || $dateStr === '0000-00-00') {
            return '-';
        }
        $ts = strtotime($dateStr);
        return $ts ? date($format, $ts) : '-';
    }
}

/**
 * Định dạng ngày giờ Việt Nam (dd/mm/YYYY HH:ii)
 */
if (!function_exists('format_datetime')) {
    function format_datetime(?string $dateStr): string {
        return format_date($dateStr, 'd/m/Y H:i');
    }
}

/**
 * Lấy thông tin user hiện tại đang đăng nhập
 */
if (!function_exists('current_user')) {
    function current_user(): ?array {
        return Auth::user();
    }
}

/**
 * Lấy mã vai trò của user hiện tại (ADMIN, BOD, HR_TAPDOAN, HR_DONVI, KETOAN_LUONG, QUANLY_TRUCTIEP, NHANVIEN)
 */
if (!function_exists('user_role')) {
    function user_role(): string {
        $u = current_user();
        return $u['RoleCode'] ?? '';
    }
}

/**
 * Kiểm tra xem user có thuộc một trong các vai trò được phép hay không
 */
if (!function_exists('has_role')) {
    function has_role(array|string $roles): bool {
        return Auth::hasRole($roles);
    }
}

/**
 * Quản lý Flash messages thông báo trên giao diện
 */
if (!function_exists('flash')) {
    function flash(string $name, ?string $message = null, string $type = 'info'): ?array {
        if ($message !== null) {
            Session::setFlash($name, $message, $type);
            return null;
        }
        return Session::getFlash($name);
    }
}

/**
 * In dữ liệu debug (Dump & Die)
 */
if (!function_exists('dd')) {
    function dd(...$vars): void {
        echo '<pre style="background: #002F6C; color: #fff; padding: 15px; border-radius: 6px; font-family: monospace;">';
        foreach ($vars as $v) {
            var_dump($v);
        }
        echo '</pre>';
        exit;
    }
}

/**
 * Che một phần số CCCD/CMND theo Nghị định 13/2023 (Chỉ hiện 4 số cuối)
 */
if (!function_exists('mask_id_number')) {
    function mask_id_number(?string $idNumber): string {
        if (empty($idNumber)) {
            return '-';
        }
        $len = strlen($idNumber);
        if ($len <= 4) {
            return $idNumber;
        }
        return '***...' . substr($idNumber, -4);
    }
}

/**
 * Che số tài khoản ngân hàng (Chỉ hiện 4 số cuối)
 */
if (!function_exists('mask_bank_account')) {
    function mask_bank_account(?string $accountNumber): string {
        if (empty($accountNumber)) {
            return '-';
        }
        $len = strlen($accountNumber);
        if ($len <= 4) {
            return $accountNumber;
        }
        return '****' . substr($accountNumber, -4);
    }
}

/**
 * Tự động che dữ liệu nhạy cảm của hồ sơ nhân viên nếu vai trò không có quyền *.sensitive
 */
if (!function_exists('mask_sensitive_employee')) {
    function mask_sensitive_employee(array $emp): array {
        $viewerMaNV = Auth::maNV();
        $isSelf = (!empty($viewerMaNV) && !empty($emp['MaNV']) && $viewerMaNV === $emp['MaNV']);

        // 1. Dữ liệu CCCD & Tài khoản ngân hàng (yêu cầu nhansu.sensitive)
        if (!$isSelf && !Auth::canViewSensitive('nhansu')) {
            if (isset($emp['SoCMND_CCCD'])) {
                $emp['SoCMND_CCCD'] = mask_id_number($emp['SoCMND_CCCD']);
            }
            if (isset($emp['SoTaiKhoanNH'])) {
                $emp['SoTaiKhoanNH'] = mask_bank_account($emp['SoTaiKhoanNH']);
            }
        }

        // 2. Dữ liệu tiền lương & hệ số lương (yêu cầu luong.sensitive)
        if (!$isSelf && !Auth::canViewSensitive('luong')) {
            if (isset($emp['LuongBaoHiem'])) {
                $emp['LuongBaoHiem'] = 0;
                $emp['LuongBaoHiemMasked'] = '****** ₫';
            }
            if (isset($emp['LuongCoBan'])) {
                $emp['LuongCoBan'] = 0;
            }
            if (isset($emp['HeSoLuongHienTai'])) {
                $emp['HeSoLuongHienTai'] = null;
            }
            if (isset($emp['ThucLinh'])) {
                $emp['ThucLinh'] = 0;
                $emp['ThucLinhMasked'] = '****** ₫';
            }
        }

        return $emp;
    }
}


