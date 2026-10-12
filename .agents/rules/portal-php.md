---
trigger: glob
globs: "portal/**/*.php, portal/**/*.css, portal/**/*.js"
---
# Quy ước Giao diện Portal PHP — Vinamilk HRM

## 1. Vai trò của Portal PHP
- Thư mục `portal/` đóng vai trò là Web Portal giao diện người dùng, kết nối và tiêu thụ Web API từ lõi .NET Backend.
- PHP không tính toán quy tắc nghiệp vụ cốt lõi (tiền lương, thuế, bảo hiểm, chấm công, duyệt phép); mọi tính toán nghiệp vụ phải do .NET thực thi.
- Không kết nối trực tiếp đến CSDL SQL Server ghi dữ liệu nghiệp vụ.

## 2. Tiêu chuẩn Mã nguồn & Bảo mật
- Tất cả các file PHP bắt buộc khai báo `declare(strict_types=1);` ở đầu file.
- Tuân thủ tiêu chuẩn định dạng mã nguồn PSR-12.
- **Bảo mật đầu ra**: Escape 100% dữ liệu xuất ra HTML bằng `htmlspecialchars($value, ENT_QUOTES, 'UTF-8')`.
- **Bảo mật Form & API**:
  - Bắt buộc kiểm tra CSRF Token cho toàn bộ các request phương thức `POST`, `PUT`, `DELETE`.
  - Không sử dụng CORS wildcard `Access-Control-Allow-Origin: *`.
  - Tuyệt đối không in thông báo lỗi hệ thống, stack trace hoặc lỗi CSDL trực tiếp ra màn hình người dùng. Mọi exception phải được log qua `error_log()` phía máy chủ.
  - Quản lý phiên đăng nhập an toàn, lưu trữ JWT trong session phía server (HttpOnly, Secure cookie).

## 3. Quy ước Giao diện & Trải nghiệm (UI/UX)
- Giao diện đồng bộ nhận diện thương hiệu Vinamilk: Màu chủ đạo `#0033A0`, font chữ hiện đại, bố cục rõ ràng, responsive cho máy tính bảng và máy trạm nhà máy.
- Hệ thống hỗ trợ tương tác phản hồi nhanh (toast notification, modal xác nhận hành động nguy hiểm).
*(Chi tiết quy chuẩn thiết kế UI, Design System Tokens và Components sẽ được hoàn thiện và bổ sung đầy đủ ở Phiên 9).*
