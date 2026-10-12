# Báo Cáo So Sánh Khác Biệt CSDL: Schema Cũ vs EF Core DbContext Chuẩn
**Dự án**: Vinamilk Enterprise HRM  
**Thời điểm thực hiện**: 2026-10-13  
**Tài liệu tham chiếu**:
- Schema SQL gốc: `VinamilkHRM/database/VinamilkHRMDb_Full_Schema_And_Seed.sql`
- DbContext chuẩn & Idempotent Migration: `VinamilkHRM/database/schema.idempotent.sql`

---

## 1. Nguyên Tắc Cốt Lõi
Theo quy ước kiến trúc tại [AGENTS.md](file:///c:/Users/MAi%20THU/OneDrive/HTTT%20QLNL/vinamilk/AGENTS.md):
- **`VinamilkHRMDbContext` là nguồn chân lý duy nhất (Single Source of Truth - SSOT)** cho toàn bộ mô hình dữ liệu.
- Mọi thay đổi schema đều được quản lý tập trung bằng **EF Core Migrations**; script triển khai SSMS / Production là `schema.idempotent.sql` (chạy lặp lại an toàn không lỗi).
- Script SQL thủ công cũ chỉ có giá trị lịch sử tham khảo.

---

## 2. Bảng Tổng Hợp Khác Biệt Chính

| Phân hệ / Tiêu chí | Schema SQL cũ (`VinamilkHRMDb_Full_Schema_And_Seed.sql`) | EF Core DbContext chuẩn (`schema.idempotent.sql`) | Lý do & Ý nghĩa kiến trúc |
| :--- | :--- | :--- | :--- |
| **Định danh & Tài khoản** | Bảng tự thiết kế `Users`, `Roles`, `RolePermissions`, `UserRoles` (mật khẩu varchar, thiếu cơ chế lockout) | Bộ bảng chuẩn ASP.NET Core Identity: `AspNetUsers`, `AspNetRoles`, `AspNetUserClaims`, `AspNetUserRoles`, `AspNetRoleClaims`, `AspNetUserLogins`, `AspNetUserTokens` | Tuân thủ bảo mật doanh nghiệp: Khóa sau 5 lần sai, mật khẩu tối thiểu 10 ký tự, hỗ trợ 2FA TOTP bắt buộc cho HR và Kế toán. |
| **Cơ chế Token API** | Không có bảng lưu Refresh Token (hoặc session phụ) | Bổ sung bảng `RefreshTokens` lưu trữ băm SHA-256 bí mật, có `RevokedAt`, `ReplacedByTokenHash`, `ExpiresAt` | Hỗ trợ JWT Authentication 15 phút an toàn kèm cơ chế Refresh Token Rotation (xoay vòng và thu hồi ngay lập tức). |
| **Tương tranh Lạc quan** | Một số bảng thiếu token kiểm soát xung đột | Mọi bảng kế thừa `BaseEntity` đều có cột `RowVersion rowversion NOT NULL` (`.IsRowVersion()`) | Ngăn chặn hiện tượng ghi đè dữ liệu (Lost Updates) khi nhiều người dùng cùng thao tác đồng thời. |
| **Bảng Lịch sử Thời gian** | Bảng thông thường, không bật System-Versioning | Kích hoạt **SQL Server Temporal Tables** cho 5 bảng: `PaySlips`, `KpiAssignments`, `Attendances`, `LaborContracts`, `SalaryProposals` | Lưu vết biến động dữ liệu nhạy cảm theo thời gian thực (hỗ trợ kiểm toán tài chính, thanh tra lao động và truy vết lịch sử hợp đồng/lương). |
| **Cây Tổ chức Doanh nghiệp** | Chỉ dùng `ParentUnitId int` (truy vấn cây phân cấp bằng CTE đệ quy chậm) | Bổ sung cột `Path hierarchyid NULL` với non-clustered index | Tối ưu hóa truy vấn phân cấp đơn vị đa cấp (Tập đoàn -> Khối -> Nhà máy/Trang trại -> Phân xưởng/Tổ đội) qua `hierarchyid::IsDescendantOf`. |
| **Trạng thái Enum KPI** | Trùng lặp giá trị enum `PendingApproval = 3`, `InProgress = 3` | Sửa enum `KpiStatus`: `Draft=1, Assigned=2, InProgress=3, Submitted=4, Approved=5, Rejected=6, Cancelled=7` | Loại bỏ lỗi logic nghiệp vụ khi lưu DB; thống nhất thuật ngữ quy trình từ `PendingApproval` sang `Submitted`. |
| **Nhật ký Kiểm toán (Audit Logs)** | Bảng `AuditLogs` trường tự do, thiếu chuẩn hóa JSON | Bổ sung `OldValues nvarchar(max)`, `NewValues nvarchar(max)` lưu JSON diff trước/sau, bắt buộc `PerformedBy` = UserId thật | Đảm bảo tính bất khả chối bỏ (Non-repudiation) qua `AuditableEntityInterceptor`. |
| **Toàn vẹn Khóa ngoại** | Một số quan hệ dùng CASCADE delete nguy hiểm | Toàn bộ Foreign Keys dùng `ON DELETE NO ACTION` / `RESTRICT` | Bảo vệ dữ liệu nhân sự, bảng lương và chấm công không bị xóa lan truyền. |

---

## 3. Chi Tiết Các Bảng Thay Đổi Cấu Trúc

### 3.1. Bảng Cây Đơn Vị (`OrganizationalUnits`)
- **Thêm cột**: `[Path] hierarchyid NULL`
- **Thêm chỉ mục**: `CREATE INDEX [IX_OrganizationalUnits_Path] ON [OrganizationalUnits] ([Path])`
- **Mục đích**: `IDataScope` sử dụng hàm `u.Path.IsDescendantOf(parentPath)` để lấy toàn bộ đơn vị trực thuộc chỉ trong một câu truy vấn `SELECT` với thời gian thực thi tối ưu O(log N).

### 3.2. Năm Bảng Temporal Tables (System-Versioned)
Các bảng sau được định nghĩa tự động lưu bảng lịch sử (`*History`):
1. **`PaySlips`** $\rightarrow$ `PaySlipsHistory`
2. **`KpiAssignments`** $\rightarrow$ `KpiAssignmentsHistory`
3. **`Attendances`** $\rightarrow$ `AttendancesHistory`
4. **`LaborContracts`** $\rightarrow$ `LaborContractsHistory`
5. **`SalaryProposals`** $\rightarrow$ `SalaryProposalsHistory`

Mỗi bảng đều có cấu trúc:
```sql
[PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
[PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [dbo].[TableNameHistory]))
```

### 3.3. Bảng Phân Quyền & Định Danh (`AspNetUsers`, `RefreshTokens`)
- Bổ sung trường doanh nghiệp vào `AspNetUsers`:
  - `EmployeeId int NULL` (liên kết với hồ sơ nhân sự thật trong `Employees`)
  - `MustChangePassword bit NOT NULL` (bắt buộc đổi mật khẩu khi đăng nhập lần đầu)
  - `LastLoginAt datetimeoffset NULL` (thời điểm đăng nhập gần nhất)
  - `FullName nvarchar(150) NOT NULL` (họ tên hiển thị)
  - `CreatedAt datetimeoffset NOT NULL` (thời điểm khởi tạo tài khoản)
- Thêm bảng `RefreshTokens`:
  - `TokenHash nvarchar(128) NOT NULL` (chỉ lưu mã băm SHA-256)
  - `ExpiresAt datetimeoffset NOT NULL`
  - `CreatedAt datetimeoffset NOT NULL`
  - `CreatedByIp nvarchar(50) NULL`
  - `RevokedAt datetimeoffset NULL`
  - `RevokedByIp nvarchar(50) NULL`
  - `ReplacedByTokenHash nvarchar(128) NULL`

---

## 4. Hướng Dẫn Triển Khai và Kiểm Thử
1. **Môi trường Phát triển (Local / Dev)**:
   ```powershell
   dotnet ef database update --project VinamilkHRM/src/VinamilkHRM.Infrastructure --startup-project VinamilkHRM/src/VinamilkHRM.WebApi
   ```
2. **Môi trường Production / Staging**:
   Mở file [schema.idempotent.sql](file:///c:/Users/MAi%20THU/OneDrive/HTTT%20QLNL/vinamilk/VinamilkHRM/database/schema.idempotent.sql) trong SQL Server Management Studio (SSMS) và thực thi. Script đã có đầy đủ logic kiểm tra `IF NOT EXISTS` trên từng bảng và từng migration step, bảo đảm chạy nhiều lần không gây lỗi.
