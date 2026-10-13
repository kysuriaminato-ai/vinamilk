# Vinamilk HRM — quy ước bắt buộc cho agent
## Sản phẩm
HRM doanh nghiệp cho Vinamilk (~10.000 CBNV; Trụ sở, nhà máy, trang trại, chi nhánh, cửa hàng). Giao diện tiếng Việt; tên mã nguồn tiếng Anh; chú thích nghiệp vụ tiếng Việt.
## Kiến trúc
- Backend VinamilkHRM/: .NET 10 LTS, ASP.NET Core, EF Core 10, SQL Server 2022. Clean Architecture: Domain ← Application ← Infrastructure ← WebApi/WebUI. Domain không tham chiếu EF Core hay ASP.NET.
- Mọi quy tắc lương, KPI, chấm công, phép nằm ở Domain/Application. PHP không tính nghiệp vụ, không kết nối DB.
- Frontend portal/: PHP >= 8.3, gọi WebApi qua HTTP, giữ JWT ở session phía server.
- CSDL duy nhất VinamilkHRMDb; đổi schema chỉ bằng EF migrations; script SSMS sinh bằng dotnet ef migrations script --idempotent.
## Quy tắc .NET
- Command/Query + handler tự viết, FluentValidation, Result pattern; lỗi nghiệp vụ là DomainException với mã ERR_*.
- Không thêm thư viện giấy phép thương mại (MediatR >= 13, AutoMapper >= 15); map bằng tay hoặc Mapperly.
- Tiền VND kiểu decimal(18,0), làm tròn AwayFromZero. Ngày công dùng DateOnly; audit dùng DateTimeOffset UTC; hiển thị Asia/Ho_Chi_Minh.
- Không hard-code tham số pháp lý (lương tối thiểu vùng, lương cơ sở, giảm trừ gia cảnh, biểu thuế, tỷ lệ bảo hiểm, hệ số tăng ca). Đọc từ bảng tham số theo ngày hiệu lực.
- Kỳ công/kỳ lương đã khóa là bất biến; sai sót sửa bằng khoản điều chỉnh ở kỳ sau.
- Mọi endpoint có [Authorize] + policy; dữ liệu lọc theo phạm vi đơn vị của người dùng.
- Audit ghi UserId thật và giá trị trước/sau cho lương, KPI, chấm công, hợp đồng.
## Quy tắc PHP
- declare(strict_types=1), PSR-12, escape mọi đầu ra, CSRF cho mọi POST, không hiển thị lỗi hệ thống cho người dùng.
## Hoàn thành một phiên
- Build xanh, test xanh, migration chạy được trên DB trống.
- Mọi công thức (KPI, lương, tăng ca, thuế, bảo hiểm) có unit test xUnit với ví dụ số cụ thể.
- Cập nhật docs/sessions/CHANGELOG.md: việc đã làm, file chính, việc còn lại.
- Không xóa dữ liệu, không force-push, không sửa lịch sử git khi chưa được người dùng xác nhận. Yêu cầu mâu thuẫn với file này thì hỏi lại.
