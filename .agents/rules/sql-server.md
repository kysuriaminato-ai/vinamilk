---
trigger: glob
globs: "**/*.sql, **/Migrations/*.cs, **/Configurations/*.cs"
---
# Quy ước SQL Server & Schema Migrations — Vinamilk HRM

## 1. Cơ sở Dữ liệu & Collation
- Hệ quản trị CSDL đích: SQL Server 2022 (`VinamilkHRMDb`).
- Collation bắt buộc: `Vietnamese_CI_AS` để hỗ trợ tìm kiếm tiếng Việt có dấu/không dấu chính xác.
- Kiểu dữ liệu văn bản tiếng Việt sử dụng `NVARCHAR`, `NCHAR`.
- Kiểu dữ liệu tiền tệ: `DECIMAL(18,0)`.

## 2. Toàn vẹn Tham chiếu & Chỉ mục (Indexes & Foreign Keys)
- Mọi bảng quan hệ phải có Khóa ngoại (Foreign Key) rõ ràng với hành vi `ON DELETE RESTRICT` / `NO ACTION` (tránh CASCADE xóa dữ liệu dây chuyền ngoài ý muốn).
- Tạo Non-clustered Index cho tất cả các cột khóa ngoại, cột tìm kiếm thường xuyên (`EmployeeCode`, `NationalId`, `Email`), và các cột phân vùng (`OrganizationalUnitId`, `RegionId`, `Period`).
- Các bảng giao dịch có cập nhật đồng thời (chấm công, lương, hợp đồng) phải có cột `RowVersion` kiểu `ROWVERSION` / `byte[]` với cấu hình `.IsRowVersion()` trong EF Core để phòng chống xung đột ghi đè (Optimistic Concurrency Control).

## 3. Quản lý Thay đổi Schema & Dữ liệu
- Mọi thay đổi schema CSDL SQL Server chỉ được thực hiện thông qua **EF Core Migrations**.
- Script triển khai lên SSMS / Production bắt buộc được sinh bằng lệnh:
  ```bash
  dotnet ef migrations script --idempotent
  ```
- **Tuyệt đối không DROP bảng hay DROP cột chứa dữ liệu nghiệp vụ** trên môi trường Production.
- Sử dụng chiến lược migration mở rộng (Expand & Contract) khi cần tái cấu trúc trường hoặc bảng.
