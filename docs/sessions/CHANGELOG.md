# NHẬT KÝ THAY ĐỔI THEO CÁC PHIÊN LÀM VIỆC (SESSION CHANGELOG)
## Dự án: Vinamilk Enterprise HRM
**Repository**: `kysuriaminato-ai/vinamilk`

---

## PHIÊN 1: NÂNG CẤP NỀN TẢNG .NET, CSDL SQL SERVER MIGRATIONS & ĐỊNH DANH BẢO MẬT
*Thời gian thực hiện: 2026-10-13*  
*Người thực hiện: Senior .NET Architect*

### 1. Việc đã làm
1. **Nâng cấp Nền tảng**:
   - Target framework đồng bộ `net9.0` (tương thích máy trạm SDK 9.0.312), bật `<Nullable>enable</Nullable>` và `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` cho `VinamilkHRM.Domain` và `VinamilkHRM.Application`.
   - Thêm project mới `VinamilkHRM.WebApi` (ASP.NET Core Web API controller-based, route `/api/v1/...`). Giữ `VinamilkHRM.WebUI` làm khu quản trị Razor dùng chung Application layer.
   - Tích hợp OpenAPI (`Microsoft.AspNetCore.OpenApi`) và giao diện thử API Scalar (`Scalar.AspNetCore` với Purple theme), tự động kích hoạt ở môi trường Development tại `/scalar/v1`.

2. **Cơ sở Dữ liệu & EF Core Migrations**:
   - Cấu hình an toàn `ConnectionStrings:DefaultConnection` trong `appsettings.json` kết nối SQL Server LocalDB qua Windows Authentication (`Trusted_Connection=True`), không lưu mật khẩu thô.
   - Chuẩn hóa enum `KpiStatus`: Loại bỏ trùng lặp giá trị (`Draft=1, Assigned=2, InProgress=3, Submitted=4, Approved=5, Rejected=6, Cancelled=7`); thay thế toàn bộ mã nguồn sử dụng `PendingApproval` sang `Submitted`.
   - Bổ sung cột `Path` kiểu `hierarchyid` (`Microsoft.EntityFrameworkCore.SqlServer.HierarchyId`) vào `OrganizationalUnit` kèm non-clustered index, tối ưu hóa truy vấn phân cấp cây đơn vị.
   - Kích hoạt SQL Server System-Versioned Temporal Tables cho 5 bảng trọng yếu: `PaySlips`, `KpiAssignments`, `Attendances`, `LaborContracts`, `SalaryProposals` (tự động lưu vết lịch sử `*History`).
   - Thiết lập `RowVersion` (`rowversion NOT NULL`) cho tất cả các root entity kế thừa `BaseEntity` (Optimistic Concurrency Control).
   - Khởi tạo migration `InitialCreate` thành công.
   - Sinh script SQL bất biến `VinamilkHRM/database/schema.idempotent.sql` bằng lệnh `dotnet ef migrations script --idempotent`.
   - Chạy kiểm thử xác minh: `dotnet ef database update` tạo đủ 100% bảng trên DB trống; script `schema.idempotent.sql` chạy lặp lại 2 lần liên tiếp trên SQL Server không có bất kỳ lỗi nào.
   - Hoàn thành tài liệu so sánh schema trong `docs/DB-DIFF.md`.

3. **Định danh & Phân quyền**:
   - Tích hợp ASP.NET Core Identity: `ApplicationUser` (`EmployeeId` nullable, `MustChangePassword`, `LastLoginAt`, `FullName`, `CreatedAt`), `ApplicationRole`, `RefreshToken`.
   - Cấu hình Identity Options: Mật khẩu tối thiểu 10 ký tự, tự động khóa tài khoản sau 5 lần sai (15 phút), hỗ trợ 2FA TOTP bắt buộc cho các vai trò HR và Kế toán.
   - Định nghĩa 10 vai trò chuẩn Vinamilk (`Roles.cs`) và danh mục 35 quyền hạn `Module.Action` (`Permissions.cs`).
   - Xây dựng `PermissionPolicyProvider` kế thừa `IAuthorizationPolicyProvider` sinh policy động theo tên quyền và `PermissionAuthorizationHandler`.
   - Xây dựng `IDataScope` (`DataScopeService`) kiểm soát phạm vi truy cập phân cấp theo cây đơn vị qua `HierarchyId IsDescendantOf` và bảo vệ dữ liệu tự phục vụ của nhân viên (ESS).
   - Triển khai JWT Token Service (`IJwtTokenService`, `JwtTokenService`): Access token 15 phút, Refresh token xoay vòng (Rotation) lưu băm SHA-256 trong DB, hỗ trợ thu hồi tức thời (`RevokeToken`).
   - Hoàn thành tài liệu ma trận vai trò × quyền trong `docs/SECURITY.md`.

4. **Hạ tầng Xuyên suốt**:
   - `ICurrentUser` (`CurrentUserService`) trích xuất thông tin người dùng từ JWT claims.
   - `AuditableEntityInterceptor` tự động ghi `CreatedBy`/`UpdatedBy` bằng `UserId` thật và tự động ghi `AuditLog` JSON trước/sau cho các entity nhạy cảm.
   - Global Exception Handler RFC 7807 (`GlobalExceptionHandler`): `DomainException` $\rightarrow$ 422 kèm mã lỗi `ERR_*`, lỗi phân quyền $\rightarrow$ 403, lỗi hệ thống $\rightarrow$ 500 (không lộ stack trace).
   - Serilog ghi log ra Console và file xoay vòng hàng ngày `logs/vinamilk-api-.log`.
   - Health checks `/health` giám sát kết nối cơ sở dữ liệu.
   - Rate limiting trên endpoint nhạy cảm `/api/v1/auth/*` (15 req/phút).
   - Hangfire Dashboard `/hangfire` với bộ lọc chỉ cho phép `SystemAdmin`.

5. **Bộ Kiểm thử Tự động (Tests)**:
   - Thêm 3 test projects: `VinamilkHRM.Domain.Tests`, `VinamilkHRM.Application.Tests`, `VinamilkHRM.Api.IntegrationTests`.
   - Đạt 100% test xanh (11/11 tests pass):
     1. Gọi API không token $\rightarrow$ 401 Unauthorized.
     2. Employee gọi lương người khác $\rightarrow$ 403 Forbidden.
     3. UnitManager nhà máy A không thấy nhân sự nhà máy B $\rightarrow$ 403 Forbidden.
     4. AuditLog ghi đúng UserId thật của người thực hiện.
     5. Unit tests công thức tính tiền lương, BHXH (8%), BHYT (1.5%), BHTN (1%), thuế TNCN lũy tiến, tăng ca và SLA KPI bonus/penalty.

6. **Cầu nối PHP (`portal/`)**:
   - Tạo dịch vụ `App\Services\ApiClient` đọc `API_BASE_URL` từ `.env`, quản lý JWT access/refresh token trong `$_SESSION`, tự động refresh khi 401, thu hồi token khi đăng xuất.
   - Cập nhật `AuthController.php` gọi `POST /api/v1/auth/login` và `/auth/revoke`.
   - Cập nhật `MenuHelper.php` và `Auth.php` hiển thị menu động theo claims quyền từ `GET /api/v1/me`.

7. **Khởi tạo Dữ liệu Mẫu (Seeder)**:
   - Tạo `DatabaseSeeder.cs` tự động khởi tạo 10 vai trò và 10 tài khoản demo ngẫu nhiên (`admin`, `executive`, `hr_director`, `hr_manager`, `hr_specialist`, `chief_accountant`, `payroll_accountant`, `unit_manager`, `team_leader`, `employee`) với `MustChangePassword = true`, mật khẩu sinh an toàn in ra console một lần.

### 2. Các File Chính Đã Tác Động / Tạo Mới
- **Solution & Projects**:
  - `VinamilkHRM/VinamilkHRM.sln`
  - `VinamilkHRM/src/VinamilkHRM.WebApi/` (Toàn bộ project Web API mới)
  - `VinamilkHRM/tests/VinamilkHRM.Domain.Tests/`
  - `VinamilkHRM/tests/VinamilkHRM.Application.Tests/`
  - `VinamilkHRM/tests/VinamilkHRM.Api.IntegrationTests/`
- **Domain & Application**:
  - `VinamilkHRM/src/VinamilkHRM.Domain/Common/BaseEntity.cs` (Bổ sung `RowVersion`)
  - `VinamilkHRM/src/VinamilkHRM.Domain/Entities/OrganizationalUnits/OrganizationalUnit.cs` (Bổ sung `Path` kiểu `HierarchyId`)
  - `VinamilkHRM/src/VinamilkHRM.Domain/Enums/KpiStatus.cs` (Chuẩn hóa 7 trạng thái, thay `PendingApproval` $\rightarrow$ `Submitted`)
  - `VinamilkHRM/src/VinamilkHRM.Application/Common/Security/Roles.cs`
  - `VinamilkHRM/src/VinamilkHRM.Application/Common/Security/Permissions.cs`
  - `VinamilkHRM/src/VinamilkHRM.Application/Common/Interfaces/ICurrentUser.cs`
  - `VinamilkHRM/src/VinamilkHRM.Application/Common/Interfaces/IDataScope.cs`
  - `VinamilkHRM/src/VinamilkHRM.Application/Common/Interfaces/IJwtTokenService.cs`
- **Infrastructure**:
  - `VinamilkHRM/src/VinamilkHRM.Infrastructure/Identity/ApplicationUser.cs`
  - `VinamilkHRM/src/VinamilkHRM.Infrastructure/Identity/ApplicationRole.cs`
  - `VinamilkHRM/src/VinamilkHRM.Infrastructure/Identity/RefreshToken.cs`
  - `VinamilkHRM/src/VinamilkHRM.Infrastructure/Security/PermissionRequirement.cs`
  - `VinamilkHRM/src/VinamilkHRM.Infrastructure/Security/PermissionAuthorizationHandler.cs`
  - `VinamilkHRM/src/VinamilkHRM.Infrastructure/Security/PermissionPolicyProvider.cs`
  - `VinamilkHRM/src/VinamilkHRM.Infrastructure/Interceptors/AuditableEntityInterceptor.cs`
  - `VinamilkHRM/src/VinamilkHRM.Infrastructure/Services/CurrentUserService.cs`
  - `VinamilkHRM/src/VinamilkHRM.Infrastructure/Services/DataScopeService.cs`
  - `VinamilkHRM/src/VinamilkHRM.Infrastructure/Services/JwtTokenService.cs`
  - `VinamilkHRM/src/VinamilkHRM.Infrastructure/Data/VinamilkHRMDbContext.cs`
  - `VinamilkHRM/src/VinamilkHRM.Infrastructure/Data/DatabaseSeeder.cs`
  - `VinamilkHRM/src/VinamilkHRM.Infrastructure/Data/Configurations/*` (Temporal tables & HierarchyId)
  - `VinamilkHRM/src/VinamilkHRM.Infrastructure/Migrations/*` (`InitialCreate`)
  - `VinamilkHRM/database/schema.idempotent.sql`
- **Tài liệu**:
  - `docs/DB-DIFF.md`
  - `docs/SECURITY.md`
  - `docs/sessions/CHANGELOG.md`
- **Portal PHP**:
  - `portal/.env.example`
  - `portal/app/services/ApiClient.php`
  - `portal/app/controllers/AuthController.php`
  - `portal/app/helpers/MenuHelper.php`
  - `portal/app/helpers/Auth.php`

### 3. Việc Còn Lại Cho Phiên Tiếp Theo
1. **Phiên 2 (Động cơ Tính Lương 3P & Tham số Pháp lý 2026)**:
   - Đọc tham số pháp lý từ CSDL theo ngày hiệu lực (Lương tối thiểu vùng, trần đóng BHXH/BHYT/BHTN, biểu thuế TNCN 2026).
   - Triển khai tính toán tự động lương Gross/Net, tăng ca, làm đêm, phụ cấp độc hại cho nhân sự văn phòng, công nhân nhà máy và chuyên viên trang trại.
   - Unit tests mở rộng cho quy trình tính lương hàng loạt và điều chỉnh lương kỳ sau.
2. **Phiên 3 (Quy trình Quản trị KPI Đa cấp & Chấm công Đa ca kíp)**:
   - Triển khai giao diện và API phân bổ KPI theo OKR từ cấp Tập đoàn xuống Tổ đội sản xuất.
   - Bảng phân ca tự động (3 ca 4 kíp) và tích hợp dữ liệu máy quẹt thẻ.

---

## PHIÊN 0: KIỂM TOÁN HỆ THỐNG, VÁ BẢO MẬT KHẨN CẤP & TÁI CẤU TRÚC KIẾN TRÚC
*Thời gian thực hiện: Tháng 10/2026*  
*Người thực hiện: Principal Software Architect & Lead Security Engineer*
*(Chi tiết xem lại mục lưu trữ)*
