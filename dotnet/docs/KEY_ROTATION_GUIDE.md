# Hướng dẫn Xoay khóa Mã hóa PII (AES-256 Key Rotation)

## 1. Cấu hình Khóa an toàn (Không lưu trong `appsettings.json`)
- **Development**: Dùng .NET User Secrets:
  ```powershell
  cd c:\xampp\htdocs\vinamilk\dotnet\src\Vinamilk.HRM.Web
  dotnet user-secrets set "Security:PiiEncryptionKey" "<NEW_32_CHAR_SECRET_KEY>"
  ```
- **Production (Docker / IIS / Linux)**: Thiết lập biến môi trường `Security__PiiEncryptionKey`.
  - Nếu thiếu khóa ở môi trường `Production`, ứng dụng sẽ **từ chối khởi động** (`InvalidOperationException`).

## 2. Quy trình Xoay khóa (Key Rotation)
1. **Sao lưu toàn bộ Database** trước khi thực hiện (`POST /api/v1/system/backup` hoặc `BACKUP DATABASE`).
2. Chuyển hệ thống sang chế độ bảo trì (`/Error/Maintenance`).
3. Chạy script `dotnet/scripts/RotatePiiKey.ps1` (hoặc console tool) với `-OldKey` và `-NewKey` để giải mã và mã hóa lại các cột PII trong bảng `Employees` (`CitizenIdEncrypted`, `CitizenIdHash`, `BankAccountEncrypted`, `TaxCodeEncrypted`).
4. Cập nhật biến môi trường `Security__PiiEncryptionKey` thành khóa mới và khởi động lại ứng dụng.
5. Ghi nhận sự kiện xoay khóa vào `AuditLog`.

