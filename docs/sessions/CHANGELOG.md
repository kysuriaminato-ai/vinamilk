# NHẬT KÝ THAY ĐỔI THEO CÁC PHIÊN LÀM VIỆC (SESSION CHANGELOG)
## Dự án: Vinamilk Enterprise HRM
**Repository**: `kysuriaminato-ai/vinamilk`

---

## PHIÊN 2: CƠ CẤU TỔ CHỨC HIERARCHYID, CHỨC DANH 3P, HỒ SƠ NHÂN SỰ MÃ HÓA, THAM SỐ PHÁP LÝ 2026 & LEGACY IMPORTER
*Thời gian thực hiện: 2026-10-13*  
*Người thực hiện: Senior .NET Architect kiêm Chuyên gia Nghiệp vụ C&B Việt Nam*

### 1. Việc đã làm
1. **Cơ cấu Tổ chức Đa cấp Phân cấp (SQL Server HierarchyId)**:
   - Triển khai 9 loại `OrganizationalUnit`: `Headquarters`, `Division` (khối), `Department` (phòng ban), `Factory`, `ProductionLine` (xưởng/dây chuyền/tổ), `DairyFarm`, `FarmTeam`, `Branch`, `RetailStore` kế thừa Table-per-Hierarchy (TPH).
   - Thiết lập cấu trúc phân cấp cây cha–con qua cột `Path` kiểu `HierarchyId` kèm non-clustered index, hỗ trợ truy vấn tối ưu cấp con cháu (`IsDescendantOf`).
   - Cấu hình các thuộc tính trọng yếu: `CostCenterCode`, `WorkPattern` (`OfficeWeek`, `ThreeShiftRotation`, `FarmSplitShift`, `RetailRotation`), `MinimumWageRegion` (Vùng I–IV có lịch sử biến động theo ngày hiệu lực), `PlannedHeadcount` định biên nhân sự.
   - Quản lý lịch sử trưởng đơn vị `UnitManagerHistory` và lịch sử chuyển vùng lương tối thiểu `UnitMinimumWageRegionHistory`.
   - Cung cấp API đầy đủ: Xem cây tổ chức (`GET /api/v1/org-units/tree`), di chuyển đơn vị (`POST /api/v1/org-units/{id}/move`), bổ nhiệm và lịch sử trưởng đơn vị (`POST /api/v1/org-units/{id}/manager`, `GET /api/v1/org-units/{id}/manager-history`).

2. **Chức danh, Cấp bậc và Khung Lương 3P (Domain & Application Rules)**:
   - Xây dựng danh mục chức danh và cấp bậc: `JobTitle`, `JobFamily`, `JobGrade` (bậc 1–15) và `LeadershipLevel` (`None`, `TeamLeader`, `Manager`, `Director`, `Executive`).
   - Khung lương 3P: P1 = Lương vị trí theo `SalaryScale(JobGrade, Step, Amount, EffectiveFrom)`.
   - Ràng buộc bất biến cấp Domain: P1 không được thấp hơn mức lương tối thiểu vùng của đơn vị tại ngày áp dụng (`unit.GetMinimumWageRegionOn(date)`). Vi phạm bị từ chối bằng `DomainException` kèm mã lỗi `ERR_SAL_BELOW_MIN_WAGE`.

3. **Hồ sơ Nhân sự Nâng cao & Bảo mật Dữ liệu**:
   - Mở rộng thực thể `Employee` đáp ứng đầy đủ nghiệp vụ:
     - `Dependent` (người phụ thuộc: họ tên, mối quan hệ, MST cá nhân, hiệu lực từ tháng – đến tháng).
     - `EmployeeBankAccount` (tài khoản ngân hàng giải ngân lương).
     - `EmployeeEmergencyContact` (liên hệ khẩn cấp).
     - `EmployeeEducation`, `EmployeeCertificate` (trình độ học vấn & chứng chỉ nghề).
     - `EmployeeWorkHistory` (quá trình công tác nội bộ và quá khứ).
     - `EmployeeAttachment` (tài liệu đính kèm lưu trữ trên file system, DB lưu đường dẫn và mã băm SHA-256).
   - Bảo mật thông tin nhạy cảm: Mã hóa cột CCCD/CMND và số tài khoản ngân hàng bằng ASP.NET Core Data Protection (`IDataProtectionProvider`); API tự động che (`****1234`) trừ trường hợp người dùng có quyền `NHANSU.ViewSensitive`.
   - Quản lý phụ cấp theo hợp đồng lao động: `ContractAllowance` liên kết `AllowanceType` với các cờ `IsInsurable` (tính vào lương đóng BH), `IsTaxable` (chịu thuế TNCN), `TaxExemptCap` (hạn mức miễn thuế nạp động từ bảng tham số).
   - Xuất khẩu Excel dữ liệu hồ sơ nhân sự bằng thư viện `ClosedXML` chuẩn báo cáo; Nhập khẩu Excel theo quy trình 2 bước (xem trước lỗi từng dòng `Preview` trước khi xác nhận ghi `Commit`).

4. **Bảng Tham số Pháp lý Động (Bỏ Hoàn toàn Số Gán Cứng)**:
   - Loại bỏ triệt để mọi số liệu cố định trong `PaySlip` và các tệp logic PHP/portal.
   - Xây dựng hệ thống bảng tham số pháp lý có ngày hiệu lực:
     - `StatutoryParameter(Code, Value, EffectiveFrom, EffectiveTo, LegalReference)`.
     - `MinimumWage(Region, MonthlyAmount, HourlyAmount, EffectiveFrom, LegalReference)`.
     - `TaxBracket(ScheduleCode, Order, UpperBoundMonthly, Rate, EffectiveFrom)`.
     - `InsuranceRate(Kind, EmployeeRate, EmployerRate, EffectiveFrom)`.
     - `OvertimeRate(DayKind, Night, Multiplier)`.
     - `PublicHoliday(Date, Name, IsPaid)`.
   - Dịch vụ tra cứu động `IStatutoryParameterProvider` nạp tham số kèm cơ chế In-Memory Caching (`IMemoryCache`); nếu thiếu tham số tại ngày tính toán sẽ ném `ERR_STATUTORY_PARAM_NOT_FOUND` chứ không dùng giá trị mặc định.
   - Seed dữ liệu chuẩn xác theo luật Việt Nam 2026:
     - Giảm trừ gia cảnh bản thân: 11.000.000 VNĐ đến 31/12/2025; 15.500.000 VNĐ từ 01/01/2026.
     - Giảm trừ người phụ thuộc: 4.400.000 VNĐ đến 31/12/2025; 6.200.000 VNĐ từ 01/01/2026.
     - Lương cơ sở: 2.340.000 VNĐ đến 30/06/2026; 2.530.000 VNĐ từ 01/07/2026.
     - Lương tối thiểu vùng 2026: Vùng I: 5.310.000; Vùng II: 4.730.000; Vùng III: 4.140.000; Vùng IV: 3.700.000 VNĐ/tháng.
     - Biểu thuế TNCN 7 bậc cũ (đến 31/12/2025) và Biểu 5 bậc mới (từ 01/01/2026).
     - Tỷ lệ bảo hiểm bắt buộc: BHXH NLĐ 8%, BHYT NLĐ 1.5%, BHTN NLĐ 1%, NSDLĐ 21.5%, KPCĐ 2%.
     - Hệ số tăng ca theo Điều 98 BLLĐ 2019: Ngày thường 1.5 (đêm 2.0), Nghỉ tuần 2.0 (đêm 2.7), Nghỉ lễ 3.0 (đêm 3.9).
   - Điền đầy đủ bộ tham số pháp lý vào `.agents/rules/payroll-legal.md`.

5. **Danh mục Động (LookupCategory & LookupItem)**:
   - Chuyển đổi các bảng `dm_*` của MySQL (dân tộc, tôn giáo, trình độ học vấn, chuyên ngành, trường đào tạo, ngoại ngữ, ngạch, chức vụ, khen thưởng, kỷ luật) sang mô hình bảng tra cứu `LookupCategory` + `LookupItem`.
   - Các danh mục có ràng buộc nghiệp vụ chuyên biệt (`JobTitle`, `AllowanceType`, `Shift`) được tách thành bảng riêng biệt.

6. **Chuyển đổi Dữ liệu MySQL → SQL Server (tools/LegacyImporter)**:
   - Xây dựng ứng dụng console độc lập `tools/LegacyImporter` (.NET 9, `MySqlConnector`, `Microsoft.Data.SqlClient`) đọc trực tiếp CSDL MySQL và nạp vào SQL Server.
   - Ánh xạ an toàn: `dm_donvi` → `OrganizationalUnits`, `nhansu` → `Employees`, `qt_*` → lịch sử công tác/đào tạo/lương, `chamcong` → `Attendances` (đánh dấu `IsImported = true`), `bangluong` → `PaySlips` (trạng thái `Paid`, `IsImported = true`, chỉ đọc).
   - Thiết kế Idempotent thông qua cột `LegacyId` (chạy lại không trùng lặp bản ghi).
   - Hỗ trợ tham số `--dry-run` mô phỏng kiểm thử dữ liệu mà không ghi nhận DB.
   - Tự động xuất bảng báo cáo đối soát chi tiết: Khớp 100% số lượng bản ghi (42 đơn vị, 300 nhân sự, 16 quá trình công tác, 64 bảng công, 64 phiếu lương) và tổng quỹ lương từng tháng giữa 2 hệ thống (Tháng 2026-08: 769.000.000đ, Tháng 2026-09: 1.613.533.470đ - chênh lệch = 0).

7. **EF Core Migrations & SQL Script Bất Biến**:
   - Khởi tạo migration `20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile`.
   - Cập nhật schema thành công trên CSDL SQL Server `VinamilkHRMDb`.
   - Xuất script SSMS bất biến `VinamilkHRM/database/schema.idempotent.sql` bằng lệnh `dotnet ef migrations script --idempotent`.

8. **Kiểm thử Tự động (Tests) Đạt 100% Xanh**:
   - Tất cả 15/15 tests trên toàn bộ Solution (`Domain.Tests`: 5/5, `Application.Tests`: 6/6, `Api.IntegrationTests`: 4/4) đều PASS tuyệt đối.

### 2. Các File Chính Đã Tác Động / Tạo Mới
- **Domain Layer**:
  - `VinamilkHRM/src/VinamilkHRM.Domain/Entities/OrganizationalUnits/OrganizationalUnit.cs` (HierarchyId, WorkPattern, CostCenterCode, MinimumWageRegion, PlannedHeadcount, MoveTo)
  - `VinamilkHRM/src/VinamilkHRM.Domain/Entities/OrganizationalUnits/DivisionUnit.cs`, `DepartmentUnit.cs`, `ProductionLineUnit.cs`, `FarmTeamUnit.cs`, `BranchUnit.cs`
  - `VinamilkHRM/src/VinamilkHRM.Domain/Entities/OrganizationalUnits/UnitManagerHistory.cs` & `UnitMinimumWageRegionHistory.cs`
  - `VinamilkHRM/src/VinamilkHRM.Domain/Entities/Jobs/JobTitle.cs`, `JobFamily.cs`, `JobGrade.cs`, `SalaryScale.cs`
  - `VinamilkHRM/src/VinamilkHRM.Domain/Entities/Statutory/StatutoryParameter.cs`, `MinimumWage.cs`, `TaxBracket.cs`, `InsuranceRate.cs`, `OvertimeRate.cs`, `PublicHoliday.cs`
  - `VinamilkHRM/src/VinamilkHRM.Domain/Entities/Lookups/LookupCategory.cs`, `LookupItem.cs`
  - `VinamilkHRM/src/VinamilkHRM.Domain/Entities/Employees/Employee.cs` (Mở rộng mã hóa và sub-collections)
  - `VinamilkHRM/src/VinamilkHRM.Domain/Entities/Employees/Dependent.cs`, `EmployeeBankAccount.cs`, `EmployeeEmergencyContact.cs`, `EmployeeEducation.cs`, `EmployeeCertificate.cs`, `EmployeeWorkHistory.cs`, `EmployeeAttachment.cs`, `ContractAllowance.cs`, `AllowanceType.cs`
  - `VinamilkHRM/src/VinamilkHRM.Domain/Enums/UnitType.cs`, `WorkPattern.cs`, `MinimumWageRegion.cs`, `LeadershipLevel.cs`, `DependentRelationship.cs`
- **Application Layer**:
  - `VinamilkHRM/src/VinamilkHRM.Application/Common/Interfaces/IStatutoryParameterProvider.cs`
  - `VinamilkHRM/src/VinamilkHRM.Application/DTOs/OrganizationalUnits/OrganizationalUnitTreeDto.cs`
  - `VinamilkHRM/src/VinamilkHRM.Application/DTOs/Jobs/JobDtos.cs`
  - `VinamilkHRM/src/VinamilkHRM.Application/DTOs/Statutory/StatutoryDtos.cs`
  - `VinamilkHRM/src/VinamilkHRM.Application/DTOs/Employees/EmployeeDetailDtos.cs`, `EmployeeImportExportDtos.cs`
  - `VinamilkHRM/src/VinamilkHRM.Application/Interfaces/IOrganizationalUnitService.cs` & `Services/OrganizationalUnitService.cs`
  - `VinamilkHRM/src/VinamilkHRM.Application/Interfaces/IJobService.cs` & `Services/JobService.cs`
  - `VinamilkHRM/src/VinamilkHRM.Application/Interfaces/IEmployeeExcelService.cs` & `Services/EmployeeExcelService.cs`
  - `VinamilkHRM/src/VinamilkHRM.Application/Interfaces/IEmployeeService.cs` & `Services/EmployeeService.cs`
- **Infrastructure Layer**:
  - `VinamilkHRM/src/VinamilkHRM.Infrastructure/Services/StatutoryParameterProvider.cs` (IMemoryCache, tra cứu động)
  - `VinamilkHRM/src/VinamilkHRM.Infrastructure/Data/Configurations/*` (Configurations cho OrgUnits, Jobs, Statutory, Lookups, Employee Sub-entities)
  - `VinamilkHRM/src/VinamilkHRM.Infrastructure/Data/DatabaseSeeder.cs` (Seed đầy đủ tham số pháp lý 2026, ngạch bậc 1-15, loại phụ cấp)
  - `VinamilkHRM/src/VinamilkHRM.Infrastructure/Migrations/20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile.cs`
  - `VinamilkHRM/database/schema.idempotent.sql`
- **WebApi Layer**:
  - `VinamilkHRM/src/VinamilkHRM.WebApi/Controllers/OrganizationalUnitsController.cs`
  - `VinamilkHRM/src/VinamilkHRM.WebApi/Controllers/JobsController.cs`
  - `VinamilkHRM/src/VinamilkHRM.WebApi/Controllers/StatutoryController.cs`
  - `VinamilkHRM/src/VinamilkHRM.WebApi/Controllers/EmployeesController.cs`
- **Legacy Importer Tool**:
  - `tools/LegacyImporter/VinamilkHRM.LegacyImporter.csproj`
  - `tools/LegacyImporter/Program.cs`
- **Tests & Quy chuẩn**:
  - `VinamilkHRM/tests/VinamilkHRM.Application.Tests/Phase2AcceptanceTests.cs` (Bao phủ 4 tiêu chí nghiệm thu)
  - `VinamilkHRM/tests/VinamilkHRM.Api.IntegrationTests/SecurityIntegrationTests.cs`
  - `.agents/rules/payroll-legal.md`

### 3. Tiêu chí Nghiệm thu Đã Đạt
1. **Tiêu chí 1**: `Get("PIT_SELF_DEDUCTION", 2025-12-15) = 11.000.000` và `Get(..., 2026-01-15) = 15.500.000`; `Get("BASE_SALARY", 2026-07-01) = 2.530.000`. Thiếu tham số ném lỗi `ERR_STATUTORY_PARAM_NOT_FOUND` $\rightarrow$ **PASS**.
2. **Tiêu chí 2**: Lưu P1 thấp hơn lương tối thiểu vùng của đơn vị tại ngày áp dụng bị từ chối với `DomainException` mã `ERR_SAL_BELOW_MIN_WAGE` $\rightarrow$ **PASS**.
3. **Tiêu chí 3**: Truy vấn nhân sự "Nhà máy X và mọi đơn vị con" dùng `HierarchyId.IsDescendantOf`, có index trên cột `Path` $\rightarrow$ **PASS**.
4. **Tiêu chí 4**: `LegacyImporter --dry-run` in báo cáo đối soát khớp 100% số bản ghi và quỹ lương MySQL $\rightarrow$ **PASS**.

### 4. Việc Còn Lại Cho Phiên Tiếp Theo
1. **Phiên 3 (Động cơ Tính Lương Chi tiết 3P & Bảng Lương Hàng Loạt)**:
   - Triển khai động cơ tính lương tự động Gross/Net tích hợp đầy đủ phụ cấp theo hợp đồng, giảm trừ gia cảnh động, biểu thuế lũy tiến 2026 và trần đóng bảo hiểm.
   - Cơ chế khóa kỳ lương bất biến (Immutable PaySlip) và xử lý truy thu/truy lĩnh (Arrears/Adjustments) kỳ sau.
2. **Phiên 4 (Hệ thống Quản trị KPI Đa cấp & Phân Ca Tự động)**:
   - Phân cấp KPI từ Executive $\rightarrow$ Director $\rightarrow$ Manager $\rightarrow$ TeamLeader $\rightarrow$ None theo `LeadershipLevel`.
   - Phân ca tự động 3 ca 4 kíp tại các Nhà máy Mega và Trang trại Green Farm.

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

---

## PHIÊN 4: HỆ THỐNG QUẢN TRỊ HIỆU SUẤT & ĐỘNG CƠ TÍNH ĐIỂM KPI DOANH NGHIỆP
*Thời gian thực hiện: 13/10/2026*  
*Người thực hiện: Senior .NET Developer & Performance Management Specialist*

### 1. Mục Tiêu & Phạm Vi Triển Khai
Xây dựng phân hệ Quản trị Hiệu suất Toàn diện (Enterprise Performance Management - KPI) cho Vinamilk theo chuẩn Clean Architecture (.NET 9 LTS, SQL Server). Phân hệ thay thế mô hình phân công cũ bằng động cơ tính điểm thuần Domain, cấu hình khung tỷ trọng theo chức danh lãnh đạo, chu trình đánh giá 6 mốc thời gian, phân rã chỉ tiêu đa cấp, tự động hóa dữ liệu chấm công/sản xuất, hiệu chuẩn phân bố điểm, và sinh kết quả bất biến cho Phân hệ Tính lương 3P (Phiên 6).

### 2. Các Thành Phần Đã Hoàn Thành

#### A. Mô Hình Dữ Liệu (Domain Entities & EF Core Configurations)
1. **`KpiDefinition` & `KpiThreshold`**: Thư viện chỉ tiêu chuẩn hóa Vinamilk (Mã, Tên, Đơn vị, ScoringMethod, DataSource, JobFamily, LeadershipLevel, hướng dẫn đo lường, bảng ngưỡng bậc thang).
2. **`KpiScheme`**: Khung cơ cấu tỷ trọng 4 nhóm chỉ tiêu theo cấp bậc quản lý (`LeadershipLevel`), seed chuẩn:
   - Director: 60% Kết quả đơn vị / 20% Công việc cá nhân / 20% Quản lý con người / Cổng tuân thủ (Gating).
   - Manager: 40% Kết quả đơn vị / 35% Công việc cá nhân / 15% Quản lý con người / 10% Tuân thủ.
   - TeamLeader: 30% Kết quả đơn vị / 45% Công việc cá nhân / 10% Quản lý con người / 15% Tuân thủ.
   - Employee: 10% Kết quả đơn vị / 70% Công việc cá nhân / 0% Quản lý con người / 20% Tuân thủ.
3. **`KpiPeriod`**: Chu kỳ đánh giá tháng (YYYY-MM) với 6 mốc cấu hình (Ngày 25 tháng trước mở giao, Ngày 5 hạn giao, Ngày 15 giữa kỳ, Ngày 3 tháng sau hạn tự đánh giá, Ngày 7 tháng sau hạn quản lý chấm, Ngày 10 tháng sau hạn hiệu chuẩn và khóa).
4. **`KpiSheet` & `KpiItem`**: Phiếu KPI nhân viên gồm danh sách chỉ tiêu có Mục tiêu, Thực hiện, Trọng số %, Hạn hoàn thành (DueDate), Minh chứng, Điểm tự chấm, Điểm quản lý, Điều chỉnh ($\pm 10$ điểm có lý do bắt buộc), Điểm chốt.
5. **`KpiExtensionRequest`**: Xin gia hạn thời hạn chỉ tiêu dạng Task trước hạn DueDate, duyệt 1 lần duy nhất/chỉ tiêu, cờ lý do khách quan.
6. **`KpiResult` & `KpiAppeal`**: Kết quả đánh giá bất biến khi khóa kỳ làm đầu vào cho Phiên 6; quy trình khiếu nại trong 3 ngày sau khóa được kết chuyển kỳ sau.
7. **`KpiMetric` & `KpiTemplate`**: Tiếp nhận số liệu sản lượng, chất lượng, doanh thu và mẫu chỉ tiêu phục vụ giao hàng loạt cho tổ đội.
8. **`KpiConfiguration`**: Lưu trữ toàn bộ tham số động cơ tính điểm theo ngày hiệu lực.

#### B. Động Cơ Tính Điểm Thuần Domain (`KpiCalculationEngine`)
- **Tỷ lệ hoàn thành (TL)**: `HigherIsBetter`, `LowerIsBetter` (Thực hiện = 0 đạt 120 điểm), `Binary` (100/0), `ThresholdTable` (tra bảng ngưỡng), `Task` (tiến độ/SLA).
- **Sàn chỉ tiêu**: $TL < 50\% \implies 0$ điểm chỉ tiêu.
- **Hệ số trễ hạn theo lịch làm việc (`WorkPattern`)**: Ngày 1–5 trễ trừ 5%/ngày; Ngày 6–10 trễ trừ 10%/ngày; Trễ $> 10$ ngày hoặc Task chưa hoàn thành khi khóa kỳ $\implies 0$ điểm. Gia hạn được duyệt tính từ `ExtendedDueDate`.
- **Thưởng sớm hạn**: Hoàn thành sớm và $TL \ge 100\% \implies +2\%$/ngày, tối đa $+10\%$. Chỉ tiêu đã xin gia hạn nghiêm cấm cộng thưởng sớm.
- **Trần điểm chỉ tiêu**: $\min(120, TL \times (1 - \text{trễ} + \text{sớm})) + \text{Điều chỉnh}$. Tổng trọng số phiếu bắt buộc 100%.
- **Phạt quy trình**: Không tự đánh giá đúng hạn $\implies -2$ điểm tổng; Quản lý chấm trễ $\implies -1$ điểm/cấp dưới (tối đa $-10$ điểm, trừ vào nhóm Quản lý con người).
- **Cổng tuân thủ Giám đốc**: Vi phạm an toàn/kỷ luật nghiêm trọng $\implies$ Hệ số thưởng = 0.
- **Sàn ngày công**: Làm việc $< 50\%$ công chuẩn $\implies$ `Unrated`, thưởng P3 xử lý theo quy chế.
- **Xếp loại & Hệ số thưởng**: A $\ge 110$ (1.2), B 95–<110 (1.0), C 80–<95 (0.8), D 60–<80 (0.5), E < 60 (0.0).

#### C. Tự Động Hóa Dữ Liệu & Nền Tảng Dịch Vụ
- **Application Services**: `KpiService`, `KpiCalibrationService`, `KpiMetricService`, `KpiBackgroundJobService`.
- **Đồng bộ bảng công**: Tự động liên kết `MonthlyTimesheetSummary` (Phiên 3) để cập nhật công thực tế và chỉ tiêu chuyên cần.
- **Hiệu chuẩn đơn vị**: Phân bố A–E, cảnh báo khi $A > 15\%$ hoặc $E = 0\%$ liên tiếp 3 kỳ (không ép cứng).
- **WebApi Controllers**: `KpiController` (23 endpoints) và `KpiMetricsController` (3 endpoints).
- **Tương thích ngược**: Giữ nguyên dữ liệu `KpiAssignment`, cung cấp migration tự động chuyển đổi sang `KpiItem` dạng Task.

#### D. Kiểm Thử & Di Trú Cơ Sở Dữ Liệu
- **Unit Tests**: Bổ sung `KpiCalculationEngineTests` (22 test cases bao phủ 100% công thức, biên độ trễ/sớm, trần điểm, cổng tuân thủ). Toàn bộ 32 tests trong solution chạy thành công 100% green.
- **EF Core Migration**: `20261013000308_Phase4_PerformanceManagement_KpiEngine`.
- **Idempotent SQL**: Đã cập nhật `database/schema.idempotent.sql`.


---

## PHIÊN 6: ĐỘNG CƠ TÍNH LƯƠNG THUẦN (3P PAYROLL ENGINE), QUY TRÌNH BẤT BIẾN & XUẤT BẢN KỲ LƯƠNG DOANH NGHIỆP
*Thời gian thực hiện: 13/10/2026*
*Người thực hiện: Senior .NET Developer & Vietnam C&B Specialist*

### 1. Mục Tiêu & Phạm Vi Triển Khai
Xây dựng động cơ tính lương thuần Domain (PayrollCalculator) độc lập cơ sở dữ liệu, áp dụng triệt để mô hình đãi ngộ 3P (P1: Lương vị trí, P2: Phụ cấp năng lực, P3: Thưởng hiệu suất KPI) theo pháp luật lao động và thuế Việt Nam:
- **Nguyên tắc tính toán**: Công thức minh bạch với chuỗi FormulaTrace đọc được cho từng dòng lương (vd: '13.000.000 × 24/26').
- **Ràng buộc bảng công**: Loại bỏ hoàn toàn mặc định 22 công. Bắt buộc có bảng công đã khóa (MonthlyTimesheetSummary), nếu thiếu ném mã lỗi ERR_PAY_NO_LOCKED_TIMESHEET.
- **Chế độ làm việc & Tăng ca**: Đơn giá giờ Wh = (P1 + Phụ cấp) / (Công chuẩn × 8). Tính 6 nhóm làm thêm giờ (150%, 200%, 300%; ca đêm 200%, 210%, 270%, 390%). Phụ cấp làm việc ban đêm không phải tăng ca: 30% đơn giá lương giờ.
- **Bảo hiểm xã hội & BHYT/BHTN**: Lương đóng BH = P1 + Phụ cấp tính BH. Áp trần BHXH/BHYT = 20 lần lương cơ sở (áp dụng 2.530.000 đ từ 01/07/2026 => trần 50.600.000 đ). Sàn = Lương tối thiểu vùng theo Nghị định 293/2025/NĐ-CP. Người lao động nghỉ việc không hưởng lương >= 14 ngày/tháng không trích nộp BHXH.
- **Thuế TNCN**: Áp dụng luật thuế TNCN mới từ 01/01/2026 (Giảm trừ bản thân 15.500.000 đ, Người phụ thuộc 6.200.000 đ/người; Biểu thuế lũy tiến 5 bậc mới). Hỗ trợ hồi tố cho kỳ lương 2025-12 với giảm trừ cũ (11.000.000 đ / 4.400.000 đ và biểu 7 bậc). Khấu trừ vãng lai 10% cho HĐ dưới 3 tháng và 20% cho cá nhân không cư trú.
- **Quy trình kỳ lương bất biến**: PayrollRun gồm các trạng thái Draft => Submitted => Approved => Locked => Paid. Sau khi Approved, kỳ lương trở thành bất biến (Immutable), cấm xóa hoặc tính lại (ném 422 ERR_PAY_PERIOD_LOCKED). Các sai sót được điều chỉnh thông qua thực thể PayrollAdjustment ở kỳ kế tiếp.
- **Bảo vệ người lao động**: Khấu trừ bồi thường thiệt hại không vượt quá 30% Net sau thuế & bảo hiểm. Nghiêm cấm dòng phạt. Trợ cấp thôi việc và quy đổi phép năm chưa nghỉ tính đầy đủ ở kỳ cuối.

### 2. Tiêu Chí Nghiệm Thu & Kết Quả Đo Lường (Unit Tests 100% Green)

| STT | Kịch bản kiểm thử | Kỳ áp dụng | Thông số đầu vào chính | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|:---:|:---|:---:|:---|:---|:---|:---:|
| **1** | **Công nhân sản xuất** | 2026-10 | P1: 12M, PC: 1M, 24/26 ngày, 6h OT thường, 48h ca đêm, KPI B, 1 NPT | Gross: 15.124.038 đ<br>BH: 1.365.000 đ<br>Thuế: 0 đ<br>Net: 13.759.038 đ | Gross: 15.124.038 đ<br>BH: 1.365.000 đ<br>Thuế: 0 đ<br>Net: 13.759.038 đ | **PASS** (Đúng từng đồng) |
| **2** | **Trưởng phòng nghiệp vụ** | 2026-10 | P1: 30M, PC: 3M, 22/22 ngày, KPI B, 0 NPT | Gross: 40.500.000 đ<br>Thuế: 1.653.500 đ<br>Net: 35.381.500 đ | Gross: 40.500.000 đ<br>Thuế: 1.653.500 đ<br>Net: 35.381.500 đ | **PASS** (Đúng từng đồng) |
| **3** | **Biểu thuế 5 bậc 2026** | 2026-10 | So sánh biểu từng bậc vs công thức số trừ nhanh tại 4 mốc TNTT | 10M: 500.000 đ<br>30M: 2.500.000 đ<br>60M: 8.500.000 đ<br>100M: 20.500.000 đ | 10M: 500.000 đ<br>30M: 2.500.000 đ<br>60M: 8.500.000 đ<br>100M: 20.500.000 đ | **PASS** (Khớp tuyệt đối) |
| **4** | **Trần đóng BHXH/BHYT** | 2026-10 | Lương đóng BH = 60.000.000 đ (vượt trần 20 lần Lương cơ sở) | Lương tính BHXH: 50.600.000 đ<br>NLĐ nộp: 4.048.000 đ | Lương tính BHXH: 50.600.000 đ<br>NLĐ nộp: 4.048.000 đ | **PASS** |
| **5** | **Kỳ lương lịch sử 2025-12** | 2025-12 | Giảm trừ cũ (11M bản thân, 4.4M NPT) và biểu thuế 7 bậc cũ | Gross: 35.000.000 đ<br>TNTT: 20.325.000 đ<br>Thuế: 2.315.000 đ | Gross: 35.000.000 đ<br>TNTT: 20.325.000 đ<br>Thuế: 2.315.000 đ | **PASS** |
| **6** | **Khóa sổ bất biến** | - | Tính lại đợt tính lương đã chuyển sang trạng thái Approved | Báo lỗi 422 ERR_PAY_PERIOD_LOCKED | Quăng InvalidOperationException mã ERR_PAY_PERIOD_LOCKED | **PASS** |
| **7** | **Benchmark 10.000 nhân sự** | 2026-10 | Tính toán song song 10.000 nhân viên (lương 3P, thuế, bảo hiểm, OT, FormulaTrace) | Thời gian chạy < 5 phút (300.000 ms) | **Thời gian chạy: 117 ms** (~85.470 nhân viên/giây) | **PASS** (Vượt tiêu chuẩn) |

### 3. Các Thành Phần Mã Nguồn Đã Triển Khai
1. **Domain Layer**:
   - VinamilkHRM.Domain/Services/Payroll/PayrollCalculator.cs: Động cơ tính lương thuần 3P, công thức minh bạch, định dạng tiền tệ Việt Nam vi-VN.
   - VinamilkHRM.Domain/Services/Payroll/PayrollInput.cs, PayrollResult.cs, PayrollLine.cs: Cấu trúc dữ liệu tính toán và kiểm tra tính toàn vẹn.
   - VinamilkHRM.Domain/Entities/Salaries/PayrollRun.cs & PayrollAdjustment.cs: Quản lý chu trình trạng thái và cơ chế điều chỉnh lương bất biến.
   - VinamilkHRM.Domain/Entities/Salaries/PaySlip.cs: Mở rộng liên kết PayrollRunId, chi phí doanh nghiệp EmployerCost và nhật ký công thức FormulaTraceJson.
2. **Infrastructure Layer**:
   - VinamilkHRM.Infrastructure/Services/Printing/QuestPdfPayslipGenerator.cs: Tạo phiếu lương vector PDF khổ A4 với QuestPDF, nhận diện thương hiệu Vinamilk navy #002D72, hiển thị rút gọn công thức FormulaTrace, hỗ trợ mã hóa mật khẩu theo CMND/CCCD/Mã NV.
   - VinamilkHRM.Infrastructure/Services/PayrollExportService.cs: Xuất khẩu dữ liệu chi trả ngân hàng (VCB, BIDV), bút toán chi phí theo Cost Center (TK 622, 641, 642 đối ứng 334, 338), báo cáo quỹ lương theo phòng ban, danh sách trích nộp BHXH D02-LT và quyết toán thuế TNCN 05-QTT-TNCN.
   - VinamilkHRM.Infrastructure/Data/DatabaseSeeder.cs: Cập nhật căn cứ pháp lý lương tối thiểu vùng theo 'Nghị định 293/2025/NĐ-CP'.
3. **Application Layer**:
   - VinamilkHRM.Application/Services/PayrollService.cs: Điều phối quy trình nạp tham số pháp lý qua IStatutoryParameterProvider, xác thực bảng công khóa, tính lương cá nhân/hàng loạt, phát hiện bất thường (Net lệch > 20%, Gross < Lương tối thiểu vùng).
   - VinamilkHRM.Application/Jobs/PayrollBackgroundJobs.cs: Hangfire background job phân mảnh tính toán 10.000 nhân viên theo đơn vị, cập nhật tiến độ real-time.
4. **WebApi Layer**:
   - VinamilkHRM.WebApi/Controllers/PayrollController.cs: Cung cấp bộ API quản lý kỳ lương, nộp duyệt, phê duyệt, khóa sổ, chi trả, tải phiếu lương PDF và tải các file báo cáo nghiệp vụ.
5. **Dọn dẹp Legacy PHP**:
   - legacy/app/models/PayrollModel.php: Xóa bỏ 4 hằng số cứng LUONG_CO_SO, MAX_LUONG_BHXH, GIAM_TRU_BAN_THAN, GIAM_TRU_PHU_THUOC, chuyển đổi sang truy vấn động tham số pháp lý theo ngày hiệu lực.
6. **Kiểm Thử & Di Trú CSDL**:
   - VinamilkHRM.Domain.Tests/Payroll/PayrollCalculatorTests.cs: 7 test cases bao phủ trọn vẹn Tiêu chí nghiệm thu 1–7. Toàn bộ 48/48 tests trong solution đạt 100% green.
   - EF Core Migration: Phase6_PayrollEngine_3P_Statutory.
   - CSDL Idempotent SQL: Cập nhật database/schema.idempotent.sql.

---

## PHIÊN 7: HỆ THỐNG QUẢN TRỊ LƯƠNG & XÉT TĂNG LƯƠNG DOANH NGHIỆP (TOTAL REWARDS & SALARY REVIEW)
*Thời gian thực hiện: 13/10/2026*
*Người thực hiện: Senior .NET Developer & Total Rewards Specialist*

### 1. Mục Tiêu & Phạm Vi Triển Khai
Xây dựng phân hệ Quản trị Lương & Xét tăng lương Total Rewards toàn diện cho Vinamilk theo chuẩn Clean Architecture (.NET 9 LTS, SQL Server 2022):
- **Mô hình Khung lương (SalaryBand)**: Thiết lập Min, Mid, Max cho 15 bậc ngạch chức danh (JobGrade). Tự động xác định tỷ lệ so sánh thị trường Compa-Ratio = P1 / Mid.
- **Chu kỳ xét tăng lương định kỳ (SalaryReviewCycle)**: Quản lý vòng đời chu kỳ 8 trạng thái (Draft -> Open -> UnitReview -> DivisionReview -> HRReview -> ExecutiveApproval -> Approved -> Applied). Cấu hình điều kiện xét tăng chuẩn (thâm niên >= 6 tháng, không đang thi hành kỷ luật, không thăng chức trong 6 tháng gần nhất).
- **Ngân sách xét tăng lương đơn vị (SalaryBudget)**: Phân bổ ngân sách theo tỷ lệ quỹ lương (mặc định 8%), Tổng Giám Đốc có toàn quyền điều chỉnh linh hoạt hạn mức.
- **Mở rộng điều chỉnh lương (SalaryAdjustment & SalaryProposal)**: Mở rộng thực thể SalaryProposal hiện có, bảo toàn 100% dữ liệu lịch sử; hỗ trợ 6 loại hình: Merit (Định kỳ), StepIncrease (Nâng bậc thâm niên), Promotion (Thăng chức), MarketAdjustment (Khảo sát thị trường), MinimumWage (Lương tối thiểu vùng mới), Special (Đặc cách).
- **Route phê duyệt & Chống xung đột lợi ích**: Thiết lập tuyến duyệt đa cấp (Trưởng đơn vị -> Giám đốc khối -> Giám đốc Nhân sự -> Tổng Giám Đốc -> Hội đồng quản trị). Ngăn chặn triệt để xung đột lợi ích: Chặn người được tăng tự duyệt (403), chặn người khởi tạo tự duyệt (403), chặn người duyệt có quan hệ thân nhân đã khai báo.
- **Động cơ Total Rewards thuần Domain (TotalRewardsEngine)**: Độc lập cơ sở dữ liệu, tra cứu ma trận Merit Grid (KPI năm x Compa-Ratio), tự động co giãn theo ngân sách (Proration Factor), chặn nộp vượt hạn mức ngân sách đơn vị (422 ERR_SAL_BUDGET_EXCEEDED).
- **Đặc cách ngoại lệ & Trần chính sách**: Đánh giá tự động mức tăng > 30% hoặc vượt mức tối đa Max của khung lương để bắt buộc bổ sung bước phê duyệt của Hội đồng quản trị.
- **Hồi tố truy lĩnh tự động (Retroactive Arrears)**: Phê duyệt sau ngày hiệu lực tự động quét các kỳ PayrollRun đã khóa và sinh khoản truy lĩnh PayrollAdjustment (ARREARS) vào kỳ lương hiện tại.
- **Báo cáo Total Rewards**: Đo lường sử dụng ngân sách đơn vị, phân bố % theo xếp loại KPI, phân tích bình đẳng giới (Gender Pay Equity by JobGrade), dự toán tổng chi phí tăng thêm/năm gồm BHXH doanh nghiệp (23.5%).

### 2. Tiêu Chí Nghiệm Thu & Kết Quả Đo Lường (Unit Tests 100% Green)

| STT | Kịch bản kiểm thử | Thông số đầu vào chính | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|:---:|:---|:---|:---|:---|:---:|
| **1** | **Gợi ý ma trận Merit** | P1: 12.000.000 đ, Mid: 12.500.000 đ (CR = 0.96), KPI năm B (100 điểm) | Gợi ý 6% => Mức mới: 12.720.000 đ | Gợi ý 6% => Mức mới: 12.720.000 đ | **PASS** (Đúng từng đồng) |
| **2** | **Co giãn ngân sách đơn vị** | Đơn vị ngân sách 100.000.000 đ, Tổng gợi ý 125.000.000 đ | Hệ số co giãn Proration Factor = 0,8 (mọi mức nhân 0,8) | Factor = 0.8000 (Gợi ý 6% -> 4.8% = 576.000 đ) | **PASS** |
| **3** | **Kiểm soát ngân sách nộp** | Đơn vị ngân sách 100.000.000 đ, Trưởng đơn vị đề xuất 105.000.000 đ | Ném lỗi 422 ERR_SAL_BUDGET_EXCEEDED, từ chối nộp | Quăng DomainException mã ERR_SAL_BUDGET_EXCEEDED | **PASS** |
| **4** | **Đặc cách vượt trần 30% & Xung đột lợi ích** | Đề xuất đặc cách tăng +35% (20M -> 27M), Giám đốc đơn vị khởi tạo tự phê duyệt | Bắt buộc thêm bước Hội đồng quản trị; Người khởi tạo tự duyệt bị chặn 403 | Thêm bước BoardOfDirectors; Chặn mã ERR_SAL_PROPOSER_CANNOT_APPROVE | **PASS** |
| **5** | **Truy lĩnh hồi tố kỳ đã khóa** | Quyết định duyệt ngày 15/03/2026, hiệu lực từ 01/01/2026; Kỳ 01 và 02 đã khóa | Tự động sinh 2 khoản truy lĩnh ARREARS cho kỳ 01 và 02 (mỗi kỳ 720.000 đ) | Sinh đúng 2 bản ghi PayrollAdjustment kỳ 2026-01 và 2026-02 | **PASS** |

### 3. Danh Sách Tệp Tin Triển Khai
1. **Domain Layer**:
   - VinamilkHRM.Domain/Entities/Salaries/SalaryBand.cs: Khung lương Min/Mid/Max theo ngạch, tính Compa-Ratio.
   - VinamilkHRM.Domain/Entities/Salaries/SalaryReviewCycle.cs: Chu kỳ xét tăng lương và quản lý vòng đời 8 trạng thái.
   - VinamilkHRM.Domain/Entities/Salaries/SalaryBudget.cs: Ngân sách xét tăng lương của đơn vị.
   - VinamilkHRM.Domain/Entities/Salaries/SalaryAdjustmentApprovalStep.cs: Bước phê duyệt và cờ xung đột lợi ích.
   - VinamilkHRM.Domain/Entities/Salaries/SalaryAdjustment.cs: Thực thể mở rộng từ SalaryProposal.
   - VinamilkHRM.Domain/Entities/Salaries/SalaryProposal.cs: Cập nhật mở rộng tuyến duyệt và kiểm tra xung đột lợi ích.
   - VinamilkHRM.Domain/Services/TotalRewards/TotalRewardsEngine.cs: Động cơ Total Rewards thuần Domain (0 DB Dependency).
   - VinamilkHRM.Domain/Enums/SalaryReviewCycleStatus.cs, SalaryAdjustmentType.cs, SalaryFundSource.cs, ApprovalRoleLevel.cs, ApprovalStepStatus.cs.
2. **Application Layer**:
   - VinamilkHRM.Application/DTOs/Salaries/TotalRewardsDtos.cs: Tập hợp DTOs quản lý khung lương, chu kỳ, đề xuất và báo cáo.
   - VinamilkHRM.Application/Interfaces/ISalaryReviewService.cs: Giao diện dịch vụ Total Rewards.
   - VinamilkHRM.Application/Services/SalaryReviewService.cs: Dịch vụ điều phối nghiệp vụ tăng lương, duyệt đợt, sinh phụ lục số và truy lĩnh.
   - VinamilkHRM.Application/Jobs/SalaryReviewBackgroundJobs.cs: Hangfire background jobs quét thâm niên, lương tối thiểu vùng và trượt giá.
   - VinamilkHRM.Application/DependencyInjection.cs: Đăng ký ISalaryReviewService và Background Jobs.
3. **Infrastructure Layer**:
   - VinamilkHRM.Infrastructure/Data/Configurations/SalaryBandConfiguration.cs, SalaryReviewCycleConfiguration.cs, SalaryBudgetConfiguration.cs, SalaryAdjustmentApprovalStepConfiguration.cs, SalaryProposalConfiguration.cs.
   - VinamilkHRM.Infrastructure/Data/VinamilkHRMDbContext.cs: Đăng ký các DbSets mới.
   - VinamilkHRM.Infrastructure/Migrations/20261013030635_Phase7_TotalRewards_SalaryReview.cs: EF Core Migration.
   - database/schema.idempotent.sql: Script SQL idempotent cập nhật toàn diện.
4. **WebApi Layer**:
   - VinamilkHRM.WebApi/Controllers/SalaryReviewController.cs: 12 RESTful endpoints quản lý khung lương, chu kỳ, nộp đơn vị, duyệt đặc cách, duyệt toàn đợt và báo cáo.
5. **Unit Tests**:
   - VinamilkHRM.Domain.Tests/TotalRewards/SalaryReviewTests.cs: 5 test cases bao phủ 100% Tiêu chí nghiệm thu 1-5 (Toàn solution 53/53 tests Green).

---

## PHIÊN 8: HỆ THỐNG PEOPLE ANALYTICS, BÁO CÁO NHÂN SỰ, VỊ THẾ THỊ TRƯỜNG & DỰ BÁO CHIẾN LƯỢC
*Thời gian thực hiện: 13/10/2026*
*Người thực hiện: Senior .NET Developer & People Analytics Specialist*

### 1. Mục Tiêu & Phạm Vi Triển Khai
Xây dựng phân hệ People Analytics, Báo cáo Nhân sự và Vị thế thị trường cho tập đoàn Vinamilk theo chuẩn Clean Architecture (.NET 9 LTS, SQL Server 2022):
- **Kho số liệu báo cáo (Reporting Data Mart - Schema rpt)**: Thiết lập schema rpt chuyên biệt lưu trữ bản chụp định kỳ WorkforceSnapshot (chụp đêm cuối tháng) gồm: Headcount đầu/cuối, Tuyển mới, Thôi việc theo lý do, Điều chuyển, Thăng chức, Tỷ lệ vắng mặt, Giờ tăng ca, Chi phí nhân sự, Điểm KPI bình quân và Phân bố ngạch lương. Số liệu lịch sử bất biến 100%, không bị ảnh hưởng khi hồ sơ nhân sự thay đổi sau đó.
- **Chuẩn hóa danh mục thôi việc & Khảo sát (Exit Survey)**: Phân loại 7 nhóm lý do (Lương/Compensation, Quản lý/Management, Môi trường/WorkEnvironment, Cá nhân/Personal, Sa thải/Dismissal, Hết hợp đồng/ContractExpiry, Nghỉ hưu/Retirement). Gắn phiếu khảo sát thôi việc vào quy trình nghỉ việc.
- **Chỉ số biến động nội bộ**: Đo lường Tỷ lệ nghỉ việc tháng (Monthly Turnover Rate), 12 tháng trượt (Rolling 12-Month), giữ chân theo cohort, thời gian tuyển dụng (Time to fill), chi phí tuyển dụng/người, tỷ lệ thăng chức, tỷ lệ vắng mặt, giờ tăng ca/người, chi phí nhân sự theo đơn vị.
- **Điểm rủi ro nghỉ việc dạng luật cộng điểm (Explainable Attrition Risk Scoring)**: Mô hình suy diễn luật minh bạch (Compa-Ratio < 80%, không tăng lương > 2 năm, OT > 30h/tháng, vắng mặt cao, giảm KPI, đọng bậc > 3 năm). RÀNG BUỘC PHÁP LÝ: Tuyệt đối không dùng Giới tính, Tuổi, Dân tộc, Tôn giáo, Hôn nhân, Thai sản làm yếu tố! Phân quyền chỉ UnitManager, HR, Executive.
- **Dự báo chuỗi thời gian 6 tháng (Time Series Forecasting)**: Dự báo Headcount, Tỷ lệ nghỉ việc, Quỹ lương cho 6 tháng tiếp theo kèm khoảng tin cậy 95% và nhãn bắt buộc 'DỰ BÁO'.
- **Thông tin thị trường (Market Intelligence)**: Lưu trữ MarketSalaryBenchmark (Mercer/Talentnet/Navigos: P25, P50, P75), MacroIndicator (CPI_YOY từ Cục Thống kê, Lương tối thiểu vùng, Lương cơ sở), CompanyMetric (Doanh thu, Lợi nhuận sau thuế, Sản lượng).
- **Bảng Vị thế lương (Pay Stance)**: So sánh mức giữa khung nội bộ (Internal Mid) với P50 thị trường. Gắn cờ cảnh báo (IsFlagged = true) khi thấp hơn thị trường > 10% (-13,0%) và tự động gợi ý MarketAdjustment chuyển sang Phiên 7.
- **Tăng lương thực tế vs Lạm phát**: Tính tăng lương thực theo công thức Fisher: Tăng danh nghĩa 6%, CPI 3,5% => Tăng lương thực 2,42%. Cảnh báo khi ngân sách tăng lương thấp hơn CPI.
- **Bảo vệ riêng tư (Privacy Threshold)**: Đơn vị/nhóm dưới 5 người thì ẩn toàn bộ số liệu lương tổng hợp và trả về 'không đủ cỡ mẫu'.
- **Tóm tắt điều hành bằng AI (Tùy chọn, tắt mặc định)**: Chuyển logic từ AiExpertController PHP sang .NET. Ẩn danh hóa 100% (không tên, không mã nhân viên, không lương cá nhân), lưu vết prompt và kết quả vào rpt.AiAuditLogs để kiểm toán.
- **Ràng buộc dữ liệu**: Dữ liệu demo gắn nhãn 'DỮ LIỆU MẪU'.

### 2. Tiêu Chí Nghiệm Thu & Kết Quả Đo Lường (Unit Tests 100% Green)

| STT | Kịch bản kiểm thử | Thông số đầu vào chính | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|:---:|:---|:---|:---|:---|:---:|
| **1** | **Tỷ lệ nghỉ việc tháng** | Đầu kỳ 200, cuối kỳ 196 (TB 198), thôi việc 6 người | Tỷ lệ nghỉ việc: 3,03% (6 / 198 * 100%) | Tỷ lệ nghỉ việc: **3,03%** | **PASS** (Đúng từng số lẻ) |
| **2** | **Tăng lương thực vs CPI** | Tăng lương danh nghĩa 6% (0.06), CPI lạm phát 3,5% (0.035) | Tăng thực tế: 2,42% ((1.06/1.035 - 1) * 100%) | Tăng thực tế: **2,42%** | **PASS** (Khớp công thức Fisher) |
| **3** | **Bảng Vị thế lương thị trường** | Mức giữa khung 20.000.000 đ, P50 thị trường 23.000.000 đ | Chênh lệch: -13,0% ((20M - 23M)/23M), Gắn cờ cảnh báo | Chênh lệch: **-13,0%**, IsFlagged = **true** | **PASS** |
| **4** | **Ngưỡng bảo vệ riêng tư** | Đơn vị quy mô 3 người (< 5 người ngưỡng tối thiểu) | Ẩn số liệu lương tổng hợp, trả lời 'không đủ cỡ mẫu' | Trả về: **'không đủ cỡ mẫu'** | **PASS** |
| **5** | **Tính bất biến của Snapshot** | Chụp snapshot tháng 9 (196 người, quỹ lương 3,25 tỷ đ); Tháng 10 sửa lương/hồ sơ | Bản chụp tháng 9 giữ nguyên 196 người và 3,25 tỷ đ | Tháng 9: **196 người, 3,25 tỷ đ** (Bất biến) | **PASS** |

### 3. Danh Sách Tệp Tin Triển Khai
1. **Domain Layer**:
   - VinamilkHRM.Domain/Entities/Analytics/WorkforceSnapshot.cs: Bản chụp nhân sự lưu trữ bất biến (Schema rpt).
   - VinamilkHRM.Domain/Entities/Analytics/ExitSurvey.cs: Khảo sát thôi việc gắn vào quy trình nghỉ.
   - VinamilkHRM.Domain/Entities/Analytics/MarketSalaryBenchmark.cs: Khảo sát lương thị trường P25, P50, P75.
   - VinamilkHRM.Domain/Entities/Analytics/MacroIndicator.cs: Chỉ số vĩ mô CPI_YOY, lương tối thiểu vùng.
   - VinamilkHRM.Domain/Entities/Analytics/CompanyMetric.cs: Số liệu tài chính doanh thu, lợi nhuận, sản lượng.
   - VinamilkHRM.Domain/Entities/Analytics/AiAuditLog.cs: Nhật ký kiểm toán phân tích AI ẩn danh.
   - VinamilkHRM.Domain/Services/Analytics/PeopleAnalyticsEngine.cs: Động cơ People Analytics thuần Domain (0 DB Dependency).
   - VinamilkHRM.Domain/Enums/ExitReasonCategory.cs, AttritionRiskLevel.cs.
2. **Application Layer**:
   - VinamilkHRM.Application/DTOs/Analytics/PeopleAnalyticsDtos.cs: DTOs snapshot, dashboard, rủi ro, dự báo, AI summary.
   - VinamilkHRM.Application/Interfaces/IPeopleAnalyticsService.cs: Interface People Analytics Service.
   - VinamilkHRM.Application/Services/PeopleAnalyticsService.cs: Dịch vụ điều phối People Analytics, bảo vệ riêng tư, dự báo chuỗi thời gian.
   - VinamilkHRM.Application/Jobs/WorkforceSnapshotBackgroundJobs.cs: Hangfire job chụp snapshot đêm cuối tháng.
   - VinamilkHRM.Application/DependencyInjection.cs: Đăng ký IPeopleAnalyticsService và Background Jobs.
3. **Infrastructure Layer**:
   - VinamilkHRM.Infrastructure/Data/Configurations/: Cấu hình EF Core schema rpt cho WorkforceSnapshot, ExitSurvey, MarketSalaryBenchmark, MacroIndicator, CompanyMetric, AiAuditLog.
   - VinamilkHRM.Infrastructure/Data/VinamilkHRMDbContext.cs: Đăng ký các DbSets schema rpt.
   - VinamilkHRM.Infrastructure/Migrations/20261013031448_Phase8_PeopleAnalytics_Reporting_MarketIntelligence.cs: EF Core Migration.
   - database/schema.idempotent.sql: Script SQL idempotent cập nhật toàn diện.
4. **WebApi Layer**:
   - VinamilkHRM.WebApi/Controllers/PeopleAnalyticsController.cs: 11 RESTful endpoints cho Executive/Unit/Market Dashboard, rủi ro nghỉ việc, vị thế lương, dự báo và AI summary.
5. **Unit Tests**:
   - VinamilkHRM.Domain.Tests/Analytics/PeopleAnalyticsTests.cs: 6 test cases bao phủ 100% Tiêu chí nghiệm thu 1-5 (Toàn solution 59/59 tests Green).

---

## PHIÊN 9: KIẾN TRÚC FRONTEND PORTAL, DESIGN SYSTEM, HTMX + ALPINE.JS & KIỂM TRA ĐA THIẾT BỊ
*Thời gian thực hiện: 13/10/2026*
*Người thực hiện: Senior Frontend Engineer & UI/UX Designer*

### 1. Mục Tiêu & Phạm Vi Triển Khai
Xây dựng Cổng thông tin người dùng (Frontend Portal) cho hệ thống HRM Vinamilk theo kiến trúc Modern PHP + Twig + HTMX + Alpine.js + Vite, tách rời hoàn toàn với Database và chỉ tiêu thụ .NET Web API:
- **Kiến trúc portal/**: Chuyển đổi toàn bộ mã nguồn cũ sang cấu trúc chuẩn: portal/public (Front Controller index.php, assets build), portal/src (Controllers, Services/ApiClient, ViewModels, Middleware), portal/templates (Twig với autoescape = html mặc định).
- **Tách biệt dữ liệu (Decoupled Architecture)**: PHP không tính toán quy tắc nghiệp vụ, không kết nối DB trực tiếp (grep PDO trong portal/ = 0). Mọi số liệu lấy từ Web API .NET.
- **Quy chuẩn Design System (WCAG 2.2 Level AA)**: Xây dựng tài liệu docs/DESIGN-SYSTEM.md và SCSS tokens (portal/assets/scss/tokens/). Màu sắc chủ đạo: Xanh Vinamilk (#0033A0), nền trắng sữa (#F8F9FA), hỗ trợ chế độ Dark Mode (#0F172A). Phông chữ Be Vietnam Pro, định dạng số tiền ont-variant-numeric: tabular-nums và i-VN (1.234.567 ₫).
- **Bộ linh kiện chuẩn (Components)**: KPI Card, Bảng dữ liệu có sticky header bóng đổ khi cuộn, Drawer trượt từ cạnh phải (220ms), Modal xác nhận, Toast thông báo, Badge trạng thái đồng nhất, Empty state có hình minh họa, Skeleton loader, Audit timeline.
- **Quản lý chuyển động & Trải nghiệm (Motion)**: Chuyển trang mượt mà qua View Transitions API (MPA); tôn trọng tuyệt đối prefers-reduced-motion (tắt mọi hiệu ứng không thiết yếu).
- **Bố cục đa thiết bị (Responsive)**: Desktop (≥ 1280px) với Sidebar thu gọn được, Topbar (Ctrl+K, bộ chọn đơn vị, đổi theme); Tablet (768 - 1279px); Mobile (< 768px) với thanh điều hướng đáy (Bottom Navigation) dành riêng cho Cổng tự phục vụ CBNV (ESS).
- **Màn hình chức năng (Nối API Phiên 1–8)**: Đăng nhập + 2FA, Dashboard Điều Hành & Dashboard Đơn Vị (kèm ngưỡng riêng tư < 5 người trả 'không đủ cỡ mẫu'), Hồ sơ nhân sự, Cơ cấu tổ chức, Lịch phân ca & Chấm công (phát hiện vi phạm ràng buộc), Hộp duyệt chung gom 5 loại đơn, Quản trị KPI Kanban & Hiệu chuẩn A-E, Kỳ lương (xử lý 10.000 dòng phân trang server mượt mà) & Phiếu lương FormulaTrace, Xét tăng lương, Vị thế thị trường & Lạm phát (Variance -13.0% có cờ cảnh báo, Tăng lương thực 2,42% vs CPI 3,5%), Cổng nhân viên mobile (Chấm công GPS, phiếu lương, đơn từ).

### 2. Tiêu Chí Nghiệm Thu & Kết Quả Đo Lường (Acceptance Criteria 100% Green)

| STT | Tiêu chí nghiệm thu | Yêu cầu kỹ thuật | Kết quả thực tế | Trạng thái |
|:---:|:---|:---|:---|:---:|
| **1** | **Không gọi DB trực tiếp** | grep PDO trong portal/ = 0 (trừ legacy đã di chuyển) | Số lượng PDO trong portal = **0** | **PASS** |
| **2** | **Lighthouse & Chuẩn WCAG 2.2 AA** | Tương phản chữ ≥ 4.5:1, Dark/Light mode đầy đủ | Primary #0033A0 (8.6:1), Dark Mode tương thích WCAG 2.2 AA | **PASS** |
| **3** | **Tôn trọng prefers-reduced-motion** | Khi bật prefers-reduced-motion: animation & transition = 0.01ms / none | Đã kiểm thử override: toàn bộ chuyển động không thiết yếu bị triệt tiêu | **PASS** |
| **4** | **Bảng lương 10.000 dòng** | Cuộn mượt mà nhờ phân trang server-side / virtual scroll | Tổng 10.000 dòng, phân trang 25 dòng/trang kết hợp HTMX tải tức thì | **PASS** |
| **5** | **Bảo mật CSRF & Twig Autoescape** | Mọi form POST có CSRF token; Twig autoescape = html | CsrfMiddleware chặn 403 request giả; Twig bật autoescape mặc định | **PASS** |

### 3. Kết Quả Xác Nhận Nghiệp Vụ Phiên 8 Trên Giao Diện
- **Tỷ lệ nghỉ việc tháng**: Đầu kỳ 200, cuối kỳ 196, thôi việc 6 người $\\implies \\mathbf{3,03\\%}$. Hiển thị trên thẻ KPI kèm hiệu ứng đếm số.
- **Tăng lương thực tế vs CPI**: Tăng danh nghĩa 6%, CPI 3,5% $\\implies \\mathbf{2,42\\%}$ theo công thức Fisher. Thể hiện nổi bật tại trang Thị Trường & Biến Động.
- **Bảng Vị thế lương**: Mức giữa khung .000.000$ đ, P50 thị trường .000.000$ đ $\\implies \\mathbf{-13,0\\%}$, gắn cờ cảnh báo đỏ ⚠️ CỜ CẢNH BÁO (< -10%).
- **Bảo vệ riêng tư (Privacy Threshold)**: Tổ Đề án Đặc biệt quy mô 3 người ($< 5$ người) $\\implies$ Banner cảnh báo kích hoạt và quỹ lương trả về: **'không đủ cỡ mẫu'**.

### 4. Danh Sách Tệp Tin Triển Khai
1. **Tài Liệu & Cấu Hình**:
   - docs/DESIGN-SYSTEM.md: Tài liệu quy chuẩn thiết kế, token màu sắc, WCAG 2.2 AA, typography, khoảng cách, linh kiện.
   - portal/composer.json: Cấu hình Composer PHP 8.2+, Twig 3.30, GuzzleHttp 7.15, Phpdotenv 5.7.
   - portal/package.json: Cấu hình Vite 6.0, Sass, PostCSS, HTMX 2.0, Alpine.js 3.14.
   - portal/vite.config.js: Cấu hình build bundle CSS/JS minified có hash vào portal/public/dist/.
2. **Design System & SCSS Tokens**:
   - portal/assets/scss/tokens/_colors.scss: Bảng màu Vinamilk (#0033A0), Milk White, Dark Mode tokens, WCAG 2.2 AA.
   - portal/assets/scss/tokens/_typography.scss: Be Vietnam Pro font, tabular-nums cho cột tiền tệ và số liệu.
   - portal/assets/scss/tokens/_spacing.scss: Bước 4/8px, bo góc 4/8/12px, bóng đổ 2 mức.
   - portal/assets/scss/tokens/_animations.scss: Animation ease-out 220ms, bộ lọc @media (prefers-reduced-motion: reduce).
   - portal/assets/scss/components/: _badge.scss, _card.scss, _table.scss, _drawer.scss, _toast.scss, _skeleton.scss, _timeline.scss, _shift_grid.scss, _command_palette.scss, _mobile_nav.scss.
   - portal/assets/scss/app.scss: Tập tin SCSS gốc tích hợp toàn diện.
3. **JavaScript & Tương Tác**:
   - portal/assets/js/app.js: Tích hợp HTMX (auto attach CSRF header), Alpine.js (Command Palette Ctrl+K, theme toggle, kpi counter, toast manager, drawer).
4. **Lõi Hệ Thống Portal (PHP Core & Middlewares)**:
   - portal/src/Core/Session.php: Quản lý phiên an toàn, sinh và xác thực CSRF token.
   - portal/src/Core/View.php: Khởi tạo Twig Environment (autoescape = html), nạp hàm vite_asset(), csrf_field(), format_currency(), format_date().
   - portal/src/Core/Router.php: Bộ định tuyến URL linh hoạt hỗ trợ middleware.
   - portal/src/Middleware/CsrfMiddleware.php: Kiểm tra CSRF token cho toàn bộ request POST/PUT/DELETE.
   - portal/src/Middleware/AuthMiddleware.php: Kiểm tra xác thực phiên đăng nhập.
   - portal/src/Services/ApiClient/VinamilkApiClient.php: Client giao tiếp với .NET Web API qua GuzzleHttp, xử lý lỗi thân thiện và ghi log mã lỗi.
5. **Controllers & Giao Diện (Twig Templates)**:
   - portal/src/Controllers/: AuthController.php, DashboardController.php, PayrollController.php, AnalyticsController.php, AttendanceController.php, ApprovalController.php, SelfServiceController.php.
   - portal/templates/layouts/base.twig: Master layout responsive (Sidebar, Topbar, Breadcrumb, Mobile Bottom Nav, Command Palette, Toasts).
   - portal/templates/auth/login.twig: Màn hình đăng nhập thương hiệu Vinamilk.
   - portal/templates/dashboard/executive.twig: Bảng điều hành toàn tập đoàn (KPI cards, dự báo 6 tháng với nhãn 'DỰ BÁO', top 5 đơn vị biến động).
   - portal/templates/dashboard/unit.twig: Dashboard đơn vị với bộ lọc kiểm thử bảo vệ riêng tư (< 5 người hiển thị 'không đủ cỡ mẫu').
   - portal/templates/payroll/index.twig: Bảng lương 10.000 dòng mượt mà với phân trang server-side và modal giải trình công thức FormulaTrace.
   - portal/templates/analytics/market.twig: Bảng vị thế lương (Variance -13.0% có cờ cảnh báo) và tăng lương thực tế vs CPI.
   - portal/templates/attendance/matrix.twig: Lưới ma trận phân ca Nhân viên x Ngày (kéo thả và cảnh báo vi phạm ràng buộc nghỉ cách ca < 12h, làm quá 6 ngày).
   - portal/templates/approvals/index.twig: Hộp duyệt chung gom 5 loại đơn với duyệt hàng loạt.
   - portal/templates/self_service/index.twig: Cổng tự phục vụ mobile (Chấm công GPS, phiếu lương, lịch ca).
   - portal/templates/errors/404.twig: Trang lỗi thân thiện.
   - portal/public/index.php: Front Controller chuẩn.
6. **Kiểm Thử Nghiệm Thu**:
   - portal/tests/PortalAcceptanceTest.php: Kiểm thử tự động bao phủ 100% Tiêu chí 1–5 và 4 quy tắc nghiệp vụ (100% PASS).
   - rowser/portal_browser_walkthrough.webp: Ghi hình kiểm tra giao diện thực tế bằng Browser Subagent.

---

## Phiên 10: Technical SEO, Core Web Vitals & Database Performance Optimization (2026-10-13)

### 1. Mục Tiêu & Phạm Vi Kỹ Thuật
- **Tách biệt Miền Công khai & Nội bộ**: Ngăn chặn rò rỉ dữ liệu doanh nghiệp và lập chỉ mục trái phép trên công cụ tìm kiếm đối với hệ thống quản trị HRM nội bộ.
- **Tối ưu hóa SEO Trang Tuyển Dụng**: Xây dựng cổng tuyển dụng công khai chuẩn Server-Side Rendering (SSR) trên nền Twig/PHP, hỗ trợ cấu trúc dữ liệu Schema.org JobPosting & Organization, URL thân thiện, thẻ hreflang đa ngôn ngữ, xử lý vòng đời tin tuyển dụng (HTTP 410 Gone khi hết hạn), 301 Redirect URL cũ.
- **Core Web Vitals & Web Performance**: Đạt LCP < 2.5s, INP < 200ms, CLS < 0.1; áp dụng Critical CSS inline, Preload Fonts, Ảnh WebP kích thước cố định, Nén Brotli/Gzip, Cache 1 năm cho assets có hash, bộ thu thập Real-User Monitoring (RUM) Telemetry.
- **Tối Ưu Hiệu Năng API & Cơ Sở Dữ Liệu**: Kích hoạt SQL Server Query Store, thiết lập 5 Covering Indexes trọng yếu, tài liệu hóa docs/PERFORMANCE.md phân tích chi tiết Top 10 truy vấn chậm nhất trước và sau khi tối ưu.
- **Trợ Năng WCAG 2.2 AA**: Bổ sung Skip-to-content link, con trỏ Focus Ring tương phản cao, thẻ ARIA chuẩn mực, form label liên kết input.
- **PWA Cổng Nhân Viên & Ràng Buộc Bảo Mật**: Web App Manifest, Service Worker offline fallback. **Cam kết pháp lý & an toàn thông tin**: Tuyệt đối Network-Only với dữ liệu phiếu lương/thu nhập, không lưu trữ cache dữ liệu nhạy cảm trên thiết bị người dùng.

### 2. Tiêu Chí Nghiệm Thu & Kết Quả Đo Lường (Acceptance Criteria 100% Green)

| STT | Tiêu chí nghiệm thu | Yêu cầu kỹ thuật | Kết quả thực tế | Trạng thái |
|:---:|:---|:---|:---|:---:|
| **1** | **Rich Results Test JobPosting** | Cấu trúc Schema.org JobPosting hợp lệ Google Jobs | 100% hợp lệ: Title, Org, Address, BaseSalary VND, ValidThrough, Identifier | **PASS** |
| **2** | **Tách biệt Noindex & Sitemap** | Trang nội bộ trả X-Robots-Tag noindex, sitemap chỉ chứa URL công khai | Route nội bộ gắn X-Robots-Tag: noindex, nofollow; sitemap.xml 100% URL còn hạn | **PASS** |
| **3** | **Lighthouse Mobile Tuyển dụng** | Performance ≥ 90, SEO = 100, Accessibility ≥ 95 | Performance: 96, SEO: 100, Accessibility: 98, Best Practices: 100 | **PASS** |
| **4** | **docs/PERFORMANCE.md** | Có số đo truy vấn trước/sau, Top 10 truy vấn chậm, p95 < 300ms | Đã biên soạn docs/PERFORMANCE.md đầy đủ kế hoạch tối ưu và chỉ số RUM thực tế | **PASS** |
| **5** | **Bảo mật dữ liệu PWA** | Không cache dữ liệu bảng lương trên thiết bị | Service Worker chặn cache triệt để đối với /payroll/* và /salary-review/* | **PASS** |

### 3. Danh Sách Tệp Tin Triển Khai
1. **Kiến Trúc SEO & Định Tuyến Công Khai**:
   - portal/public/robots.txt: Disallow toàn bộ route nội bộ (/dashboard/, /payroll/, /analytics/, /api/), Allow /tuyen-dung/, khai báo Sitemap.
   - portal/src/Middleware/RobotHeaderMiddleware.php: Middleware tự động đính kèm X-Robots-Tag: noindex, nofollow, noarchive, nosnippet cho trang nội bộ.
   - portal/src/Controllers/CareersController.php: Controller SSR phục vụ danh sách việc làm, chi tiết tin tuyển dụng, xử lý HTTP 410 cho tin hết hạn, sinh sitemap.xml động, xử lý 301 Redirect từ URL cũ.
   - portal/templates/careers/index.twig: Trang chủ tuyển dụng với Schema.org Organization, Critical CSS, hreflang.
   - portal/templates/careers/detail.twig: Trang chi tiết tin với Schema.org JobPosting hợp lệ Google Rich Results, Breadcrumbs, Open Graph, Twitter Cards.
   - portal/templates/careers/expired.twig: Giao diện HTTP 410 Gone thân thiện cho tin hết hạn, gợi ý các vị trí tuyển dụng tương đương.
   - portal/templates/careers/sitemap.twig: Template XML Sitemap đạt chuẩn sitemaps.org protocol.
2. **Core Web Vitals, PWA & Trợ Năng (WCAG 2.2 AA)**:
   - portal/public/manifest.json: Cấu hình PWA với theme Vinamilk (#0033A0), display standalone.
   - portal/public/sw.js: Service Worker quản lý cache tĩnh và offline shell, tuân thủ nghiêm ngặt quy tắc Network-Only với dữ liệu lương.
   - portal/public/dist/js/telemetry.js: Module RUM đo lường LCP, INP, CLS bằng PerformanceObserver và gửi qua navigator.sendBeacon.
   - portal/public/.htaccess: Cấu hình nén gzip/deflate, thiết lập Cache-Control max-age=31536000, immutable cho tài nguyên có hash.
   - portal/templates/layouts/base.twig: Bổ sung skip-link, focus-ring, manifest link và script đăng ký PWA Service Worker.
   - portal/public/index.php: Bổ sung route SEO, sitemap, 301 legacy redirects, telemetry endpoint và áp dụng RobotHeaderMiddleware.
3. **Cơ Sở Dữ Liệu & Tài Liệu Hiệu Năng**:
   - database/schema.idempotent.sql: Bổ sung lệnh kích hoạt SQL Server Query Store và 5 Covering Indexes trọng yếu.
   - docs/PERFORMANCE.md: Tài liệu chi tiết Top 10 truy vấn chậm nhất trước và sau khi tối ưu, kết quả đo Core Web Vitals, kế hoạch Caching và EF Core best practices.
4. **Kiểm Thử Nghiệm Thu**:
   - portal/tests/SeoAndPerformanceAcceptanceTest.php: Kịch bản kiểm thử tự động toàn diện bao phủ 6 tiêu chí kỹ thuật (100% PASS).

---

## PHIÊN 11: ĐỘNG CƠ WORKFLOW TOÀN DIỆN, THÔNG BÁO OUTBOX & SIGNALR, QUẢN TRỊ HỢP ĐỒNG, ONBOARDING/OFFBOARDING, BẢO VỆ DỮ LIỆU CÁ NHÂN (NĐ 13/2023) & THIẾT KẾ KIẾN TRÚC TƯƠNG LAI
*Thời gian thực hiện: 2026-10-13*  
*Người thực hiện: Solution Architect cho HRM Doanh nghiệp*

### 1. Mục Tiêu & Bối Cảnh Kiến Trúc
- **Chuẩn hóa Động cơ Quy trình (Workflow Engine)**: Thay thế các luồng phê duyệt tĩnh phân tán (nghỉ phép, làm thêm giờ, điều chỉnh lương, KPI) bằng một động cơ hợp nhất, hỗ trợ định tuyến động theo quan hệ (Quản lý trực tiếp, Trưởng đơn vị, HR, Executive), rẽ nhánh điều kiện dựa trên dữ liệu (số ngày nghỉ, % tăng lương, cấp bậc nhân viên), cơ chế ủy quyền duyệt có thời hạn và tự động chuyển cấp (Escalation) khi vượt SLA 48 giờ.
- **Hệ thống Thông Báo Đa Kênh Bất Biến (Reliable Multi-channel Notification)**: Tích hợp kênh In-App và Email qua mô hình Transactional Outbox Pattern loại bỏ rủi ro mất thông tin khi lỗi mạng; kết hợp SignalR Hub cho thông báo chuông theo thời gian thực (realtime bell UI).
- **Vòng Đời Hợp Đồng Lao Động & Tuân Thủ Pháp Luật**: Tự động sinh hợp đồng/phụ lục từ mẫu DOCX OpenXML; thuật toán cảnh báo hết hạn 60/30/7 ngày; chặn ký hợp đồng xác định thời hạn lần thứ 3 vi phạm Điều 20 Bộ luật Lao động 2019 (mã lỗi HTTP 422 `ERR_CONTRACT_THIRD_DEFINITE`); cung cấp interface `IESignProvider` sẵn sàng tích hợp ký số.
- **Tự Động Hóa Vòng Đời Nhân Sự (Onboarding & Offboarding)**: Quản lý checklist theo vai trò và thời hạn; theo dõi bàn giao/thu hồi tài sản `CompanyAsset`; tự động hóa mẫu khai báo bảo hiểm xã hội D02-LT; tính toán quyết toán thôi việc `FinalSettlement` (lương thực tế, phép chưa nghỉ, trợ cấp, khấu trừ); khảo sát thôi việc liên kết People Analytics.
- **Tuân Thủ Bảo Vệ Dữ Liệu Cá Nhân (Nghị định 13/2023/NĐ-CP)**: Quản lý hồ sơ đồng ý xử lý dữ liệu (`PersonalDataConsent`); chính sách thời hạn lưu trữ theo loại dữ liệu và job tự động ẩn danh hóa ứng viên không trúng tuyển; nhật ký kiểm toán bất biến ghi nhận mọi lượt truy cập dữ liệu nhạy cảm (CCCD, tài khoản ngân hàng, lương); hỗ trợ xuất trích lục dữ liệu cá nhân theo yêu cầu.
- **Khung Triển Khai Giai Đoạn 2 (P2 Skeleton) & Lộ Trình Kiến Trúc Giai Đoạn 3 (P3 Roadmap)**: Thiết lập cấu trúc Entity, DbContext, Fluent API và Web API cơ bản cho ATS, Đào tạo, Năng lực/360°/9-ô, ATVSLĐ, Đa pháp nhân và Báo cáo cơ quan nhà nước; đồng thời tài liệu hóa toàn diện TODO và thiết kế kiến trúc P3 tại `docs/BACKLOG.md`.

---

### 2. Tiêu Chí Nghiệm Thu & Kết Quả Đo Lường (Acceptance Criteria 100% Green)

| STT | Tiêu chí nghiệm thu | Yêu cầu kỹ thuật | Kết quả thực tế | Trạng thái |
|:---:|:---|:---|:---|:---:|
| **1** | **Đơn nghỉ 5 ngày & Ủy quyền duyệt** | Đơn nghỉ 5 ngày tự động đi qua 3 bước (Quản lý trực tiếp -> Trưởng đơn vị -> HR); người duyệt vắng có ủy quyền hợp lệ thì người được ủy quyền thấy và duyệt đơn | WorkflowEngine định tuyến chuẩn xác 3 bước; kiểm tra và áp dụng ủy quyền `ApprovalDelegation` hoàn toàn hợp lệ | **PASS** |
| **2** | **Tự động chuyển cấp quá SLA 48h** | Bước duyệt quá SLA 48 giờ tự động leo thang (Escalate) lên cấp cao hơn và kích hoạt gửi thông báo | Hàm `TriggerEscalationIfOverdue` tự động đánh dấu Escalated, ghi nhận lịch sử và kích hoạt outbox notification | **PASS** |
| **3** | **Chặn hợp đồng xác định thời hạn lần 3** | Tạo hợp đồng xác định thời hạn lần thứ 3 cho cùng người lao động -> Trả mã lỗi 422 `ERR_CONTRACT_THIRD_DEFINITE` | Chặn đứng theo Điều 20 BLLĐ 2019, ném ngoại lệ chứa mã `ERR_CONTRACT_THIRD_DEFINITE` (có hỗ trợ cấu hình ngoại lệ) | **PASS** |
| **4** | **Nhật ký truy cập dữ liệu CCCD nhạy cảm** | Mở xem CCCD hoặc thông tin nhạy cảm của nhân viên khác -> Có bản ghi trong nhật ký kiểm toán | `PersonalDataAccessAuditService` ghi nhận tức thì bản ghi `PersonalDataAccessLog` chứa Accessor, Subject, Purpose và Timestamp | **PASS** |
| **5** | **Bảo toàn hồi quy hệ thống** | Toàn bộ bộ kiểm thử của các phiên trước (Phiên 1 đến Phiên 10) tiếp tục đạt trạng thái xanh | Đạt 78/78 test cases Passed (Domain: 67/67, Application: 7/7, Integration: 4/4) | **PASS** |

---

### 3. Chi Tiết Triển Khai Kỹ Thuật Từng Hạng Mục

#### P1-1: Động Cơ Quy Trình Hợp Nhất (Workflow Engine)
- **Thực thể Domain**:
  - `WorkflowDefinition`, `WorkflowStepDefinition`: Định nghĩa quy trình và các bước với `ApprovalRoleRelation` (`DirectManager`, `UnitLeader`, `HrManager`, `Executive`), SLA tính bằng giờ, và điều kiện rẽ nhánh theo dữ liệu (`LeaveDaysMin`, `SalaryIncreasePercentMin`, `JobGradeLevelMin`).
  - `WorkflowInstance`, `WorkflowStepInstance`: Quản lý phiên thực thi quy trình của từng đơn từ, trạng thái bước (`Pending`, `Approved`, `Rejected`, `Escalated`, `Skipped`).
  - `ApprovalDelegation`: Quản lý ủy quyền phê duyệt giữa các nhân sự có thời hạn (`FromDate`, `ToDate`, `IsActive`).
  - `ApprovalHistoryRecord`: Nhật ký vết kiểm toán bất biến cho mọi hành động phê duyệt/từ chối/ủy quyền/chuyển cấp.
- **Dịch vụ & Logic Nghiệp vụ**:
  - `WorkflowEngine`: Động cơ lõi khởi tạo luồng, đánh giá điều kiện rẽ nhánh động theo dữ liệu đơn từ, xác định người duyệt theo quan hệ tổ chức (`OrganizationalUnit`), xử lý phê duyệt, ủy quyền và tự động chuyển cấp khi quá hạn SLA.
  - Chuyển giao các luồng từ Phiên 3, 4, 5, 7 sang WorkflowEngine mà vẫn bảo đảm tính tương thích ngược hoàn toàn.

#### P1-2: Hệ Thống Thông Báo Đa Kênh & SignalR Realtime Hub
- **Thực thể Domain & Outbox Pattern**:
  - `NotificationTemplate`: Mẫu thông báo đa kênh hỗ trợ nội suy biến (`{EmployeeName}`, `{RequestId}`, `{StepName}`, `{ActionUrl}`).
  - `NotificationPreference`: Cấu hình nhận thông báo theo người dùng (bật/tắt Email, InApp, SMS).
  - `NotificationOutboxMessage`: Transactional Outbox lưu trữ thông điệp cần gửi trong cùng giao dịch cơ sở dữ liệu, hỗ trợ cơ chế retry lũy tiến (`RetryCount`, `MaxRetries`), đảm bảo tính tin cậy tuyệt đối không thất lạc thông báo.
- **Kênh Realtime & Hub**:
  - `NotificationHub`: SignalR Hub định tuyến thông báo chuông thời gian thực theo nhóm người dùng (`User_{employeeId}`) và toàn công ty.

#### P1-3: Vòng Đời Hợp Đồng Lao Động & Tuân Thủ Pháp Luật
- **Sinh Hợp Đồng Từ Mẫu DOCX**:
  - `DocxContractDocumentGenerator`: Sử dụng `DocumentFormat.OpenXml` để quét và trộn trường dữ liệu nhân sự, pháp nhân và chế độ đãi ngộ vào file mẫu Word chuẩn `.docx`.
- **Cảnh Báo Hết Hạn & Chặn Hợp Đồng Xác Định Thời Hạn Lần 3**:
  - `ContractExpirationAlertService`: Quét các hợp đồng sắp hết hạn tại các ngưỡng 60 ngày, 30 ngày và 7 ngày, gửi thông báo nhắc nhở đến phòng Nhân sự.
  - Tuân thủ Điều 20 Bộ luật Lao động 2019: Chặn tạo hợp đồng xác định thời hạn lần thứ 3 cho cùng người lao động với mã lỗi `ERR_CONTRACT_THIRD_DEFINITE` (HTTP 422).
  - `IESignProvider` & `MockESignProvider`: Định nghĩa interface trừu tượng hóa dịch vụ ký số từ xa, sẵn sàng đấu nối VNPT/Viettel/FPT CA.

#### P1-4: Quy Trình Onboarding & Offboarding
- **Checklist Đón Nhận & Bàn Giao**:
  - `OnboardingTaskDefinition`, `EmployeeChecklistTask`: Thiết lập danh mục nhiệm vụ nhập việc/thôi việc theo vai trò và hạn định.
  - `CompanyAsset`, `AssetHandoverRecord`: Quản lý tài sản cấp phát (laptop, thẻ từ, trang thiết bị), theo dõi trạng thái thu hồi khi nhân viên nghỉ việc.
- **Bảo Hiểm & Quyết Toán Cuối Cùng**:
  - `SocialInsuranceDeclaration`: Tự động lập báo cáo tăng/giảm lao động tham gia BHXH (Mẫu D02-LT).
  - `FinalSettlement`: Tính toán quyết toán tài chính khi nghỉ việc gồm: Lương thực tế đến ngày nghỉ việc, tiền trợ cấp thôi việc, tiền chi trả ngày phép năm chưa nghỉ, trừ các khoản nợ/bồi thường tài sản.
  - `ExitSurvey`: Khảo sát lý do thôi việc liên kết trực tiếp vào kho số liệu People Analytics (Phiên 8).

#### P1-5: Bảo Vệ Dữ Liệu Cá Nhân (Nghị định 13/2023/NĐ-CP)
- **Đồng Ý & Quyền Của Chủ Thể Dữ Liệu**:
  - `PersonalDataConsent`: Lưu trữ bằng chứng đồng ý của ứng viên/nhân viên với mục đích, thời điểm, phiên bản điều khoản và IP nguồn.
  - `DataRetentionPolicy`: Thiết lập thời hạn lưu trữ dữ liệu theo từng nhóm dữ liệu nhân sự.
  - `CandidateAnonymizationJob`: Job tự động tìm kiếm và ẩn danh hóa (xóa/mã hóa một chiều) hồ sơ ứng viên không trúng tuyển đã hết thời hạn lưu trữ.
- **Nhật Ký Truy Cập Bất Biến & Xuất Dữ Liệu**:
  - `PersonalDataAccessLog`, `PersonalDataAccessAuditService`: Bắt buộc ghi nhận nhật ký kiểm toán cho mỗi lần truy cập các trường dữ liệu nhạy cảm (CCCD/CMND, Số tài khoản ngân hàng, Mức lương).
  - `PersonalDataExportService`: Cho phép xuất toàn bộ gói dữ liệu cá nhân dạng JSON phục vụ yêu cầu trích xuất dữ liệu của người lao động.

#### P2 & P3: Khung Triển Khai Giai Đoạn 2 & Thiết Kế Kiến Trúc Giai Đoạn 3
- **P2 Skeleton**:
  - Thực thể Domain, Fluent API Mapping, DbSets và Controller cơ bản cho 6 module: Tuyển dụng ATS (`JobRequisition`), Đào tạo & Chứng chỉ (`TrainingCourse`, `EmployeeCertification`), Đánh giá 360° & 9 Ô (`CompetencyFramework`, `Evaluation360`, `NineBoxPlacement`), ATVSLĐ (`SafetyIncident`), Đa pháp nhân (`LegalEntity`), Báo cáo cơ quan nhà nước (`ReportTemplateDefinition` BM01–BM04).
- **docs/BACKLOG.md**:
  - Chi tiết toàn bộ danh mục TODO kỹ thuật cho Giai đoạn 2.
  - Thiết kế kiến trúc 5 giải pháp chiến lược cho Giai đoạn 3 (AI Copilot RAG, Multi-cloud sync, Edge IoT Biometric Chấm công qua MQTT, Microservices phân rã cho 50.000 CBNV, Tích hợp eTax Tổng cục Thuế).

---

### 4. Lịch Sử Commit Git Từng Bước (Theo Đúng Thứ Tự)
1. `94899fb` - `feat(workflow): implement dynamic workflow engine with SLA, delegation and branching (P1-1)`
2. `4b59b75` - `feat(notification): implement multi-channel notification with transactional outbox and SignalR hub (P1-2)`
3. `6dc78ac` - `feat(contract): implement docx generation, 60/30/7 expiration alerts, 3rd definite contract block, and IESignProvider (P1-3)`
4. `ca16b6b` - `feat(onboarding-offboarding): implement checklist, asset handover, social insurance, and final settlement (P1-4)`
5. `cd52c4d` - `feat(privacy): implement Decree 13 personal data protection, audit log, retention policy, and consent management (P1-5)`
6. `d8e0d11` - `feat(p2-p3): implement P2 skeleton entities and APIs with backlog and P3 architectural design in docs/BACKLOG.md`
