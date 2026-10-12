---
trigger: glob
globs: VinamilkHRM/**/*.cs
---
# Quy ước Backend .NET — Vinamilk HRM

## 1. Kiến trúc Clean Architecture
- Tuân thủ nguyên tắc phụ thuộc một chiều: `Domain` ← `Application` ← `Infrastructure` ← `WebApi` / `WebUI`.
- **Domain**: Chứa thực thể (Entities), Value Objects, Enums, Domain Exceptions (`ERR_*`), Domain Events, Domain Services thuần C#. Tuyệt đối không tham chiếu thư viện EF Core hay ASP.NET Core.
- **Application**: Chứa Use Cases, Command/Query handlers (tự viết, không dùng MediatR >= 13), FluentValidation, DTOs, Mappers (thủ công hoặc Mapperly, không dùng AutoMapper >= 15).
- **Infrastructure**: Triển khai EF Core DbContext, Repositories, Migrations, dịch vụ bên ngoài, logging, xuất báo cáo.
- **WebUI / WebApi**: Controller / Endpoints tiếp nhận HTTP request, áp dụng `[Authorize]` + Policy, Antiforgery, chuyển tiếp qua Application layer, trả về Result pattern.

## 2. Quy ước Lập trình Bất đồng bộ (Async & Cancellation)
- Mọi thao tác truy xuất I/O (Database, HTTP API, File system) bắt buộc sử dụng phương thức bất đồng bộ `async` / `await`.
- Mọi phương thức async bắt buộc nhận tham số `CancellationToken cancellationToken = default` và truyền xuyên suốt qua các tầng cho EF Core / HttpClient.
- Không sử dụng `.Result` hay `.Wait()` gây deadlock thread pool.

## 3. Quy chuẩn Entity Framework Core
- Tuyệt đối **không bật Lazy Loading** (không dùng `UseLazyLoadingProxies` hoặc virtual navigation properties gây N+1 query).
- Sử dụng Eager Loading (`Include`, `ThenInclude`) có chọn lọc hoặc Explicit Projection (`Select`) sang DTO.
- Các truy vấn chỉ đọc bắt buộc dùng `.AsNoTracking()`.
- Tiền tệ VND khai báo kiểu `decimal(18,0)`, quy tắc làm tròn `MidpointRounding.AwayFromZero`.
- Ngày tháng: Ngày công dùng `DateOnly`, giờ giấc dùng `TimeOnly`, nhật ký kiểm toán dùng `DateTimeOffset` UTC.

## 4. Quy ước Đặt tên & Xử lý Lỗi
- Mã lỗi nghiệp vụ bắt buộc dùng tiền tố chuẩn `ERR_` kết hợp `DomainException` (Ví dụ: `ERR_PAYROLL_LOCKED`, `ERR_KPI_INVALID_WEIGHT`, `ERR_EMPLOYEE_NOT_FOUND`).
- Handler trả về Result Pattern (`Result<T>` / `Result.Success`, `Result.Failure`).
- Tên class, interface, method tuân thủ chuẩn PascalCase; tham số, biến cục bộ tuân thủ camelCase.
