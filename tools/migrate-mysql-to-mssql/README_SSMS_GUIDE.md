# HƯỚNG DẪN CHUYỂN ĐỔI DỮ LIỆU MYSQL → SQL SERVER & VẬN HÀNH TRÊN SSMS

Tài liệu này hướng dẫn quy trình khởi tạo cơ sở dữ liệu `VinamilkHRM` trên **Microsoft SQL Server**, chuyển đổi toàn bộ dữ liệu mẫu từ `database/*.sql` (MySQL) sang cấu trúc chuẩn hóa 3NF (`.NET 8` + `EF Core 8`), và kiểm thử các View / Stored Procedure báo cáo trên **SQL Server Management Studio (SSMS)**.

---

## 1. Cấu hình Chuỗi kết nối (`appsettings.json`)

Chuỗi kết nối mặc định đã được cấu hình tại `dotnet/src/Vinamilk.HRM.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=VinamilkHRM;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

> **Lưu ý:** Nếu máy bạn sử dụng SQL Server Express hoặc LocalDB, bạn có thể đổi `Server=.` thành `Server=.\SQLEXPRESS` hoặc `Server=(localdb)\MSSQLLocalDB` trong `appsettings.Development.json` hoặc qua `dotnet user-secrets`.

---

## 2. Cách 1: Tự động tạo DB, Views, Stored Procedures & Seed 55 Nhân viên bằng EF Core CLI

Tại thư mục gốc `c:\xampp\htdocs\vinamilk`, mở PowerShell / Terminal và chạy:

```powershell
# Bước 1: Áp dụng Migration tạo 6 Schema (identity, core, time, payroll, talent, sys), 49 bảng chuẩn 3NF, Indexes, Views & Stored Procedures
dotnet ef database update --project dotnet/src/Vinamilk.HRM.Infrastructure --startup-project dotnet/src/Vinamilk.HRM.Web

# Bước 2: Chạy ứng dụng Web để ApplicationDbContextInitializer tự động Seed toàn bộ dữ liệu từ MySQL sang SQL Server
dotnet run --project dotnet/src/Vinamilk.HRM.Web
```

Khi ứng dụng khởi động, `ApplicationDbContextInitializer` sẽ tự động kiểm tra và Seed:
- **6 Vai trò chuẩn**: `SuperAdmin`, `HR Manager`, `HR Staff`, `Line Manager`, `Employee`, `Accountant` cùng **62 Permissions** chi tiết.
- **32 Đơn vị/Phòng ban/Nhà máy/Trang trại** (`VINAMILK`, `KHOI_SX`, `NM_TS`, `TT_GREEN1`, `PB_NHANSU`...) phân cấp 3 tầng kèm `TreePath`.
- **15 Chức vụ**, **10 Ngạch lương x 5 Bậc = 50 Bậc lương**, **6 Trình độ học vấn**, **8 Danh mục Khen thưởng/Kỷ luật**.
- **Toàn bộ 55 Nhân viên (`VNM-0001` → `VNM-0055`)** chuyển từ `database/vinamilk_hrm.sql`, tự động mã hóa AES-256 cho Số CCCD, Số TK ngân hàng, Mã số thuế TNCN và tạo mã băm `CitizenIdHash` (`HMAC-SHA256`).
- **55 Hợp đồng lao động**, Người phụ thuộc giảm trừ gia cảnh, Quỹ phép năm, Quá trình công tác, Khen thưởng/Kỷ luật, Bảng tổng hợp công và **Bảng lương 3P tháng 08/2026**.

---

## 3. Cách 2: Chạy Script trên SQL Server Management Studio (SSMS)

1. Mở **SQL Server Management Studio (SSMS)** và kết nối tới `Server = .` (Windows Authentication).
2. Nếu muốn xuất toàn bộ kịch bản DDL + Views + Stored Procedures từ EF Core Migration thành 1 file `.sql` độc lập để chạy trên SSMS, thực thi lệnh:
   ```powershell
   dotnet ef migrations script --idempotent --project dotnet/src/Vinamilk.HRM.Infrastructure --startup-project dotnet/src/Vinamilk.HRM.Web --output tools/migrate-mysql-to-mssql/00_Schema_Idempotent.sql
   ```
3. Mở file `tools/migrate-mysql-to-mssql/01_Migrate_MySQL_To_SQLServer_VinamilkHRM.sql` trong SSMS, chọn Database `VinamilkHRM` và nhấn **F5 (Execute)**.

---

## 4. Các Truy vấn Kiểm tra Nghiệm thu trên SSMS

Sau khi cập nhật Database, bạn có thể chạy các câu lệnh sau trên SSMS để kiểm tra:

```sql
USE [VinamilkHRM];
GO

-- 1. Kiểm tra danh sách 55 nhân viên và mã hóa CCCD (Nghị định 13/2023/NĐ-CP)
SELECT TOP 10
    EmployeeCode, FullName, Gender, WorkEmail,
    CitizenIdEncrypted, CitizenIdHash,
    BaseSalary, PositionAllowance, WorkStatus
FROM [core].[Employees]
ORDER BY EmployeeCode;

-- 2. Kiểm tra View Cơ cấu nhân sự theo Phòng ban / Nhà máy (BM01)
SELECT * FROM [core].[vw_DepartmentHeadcountStructure] ORDER BY ActualHeadcount DESC;

-- 3. Kiểm tra Stored Procedure Biến động Headcount & Turnover theo 12 tháng
EXEC [core].[sp_GetMonthlyHeadcountAndTurnover] @Year = 2026;

-- 4. Kiểm tra Stored Procedure Phân tích Tổng quỹ lương & Chi phí nhân sự
EXEC [payroll].[sp_GetPayrollFundAnalysis] @Year = 2026, @Month = 8;
```

