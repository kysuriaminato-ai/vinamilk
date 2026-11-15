# Hướng dẫn Triển khai Vinamilk HRM trên IIS (Windows Server)

## 1. Yêu cầu môi trường
- Windows Server 2019/2022 + IIS 10+.
- Cài đặt **.NET 8.0 Hosting Bundle** (ASP.NET Core Runtime + IIS Module v2).
- SQL Server 2019/2022.

## 2. Publish ứng dụng
```powershell
cd c:\xampp\htdocs\vinamilk\dotnet
dotnet publish src\Vinamilk.HRM.Web\Vinamilk.HRM.Web.csproj -c Release -o C:\inetpub\wwwroot\VinamilkHRM
```

## 3. Cấu hình IIS Application Pool & Site
1. Tạo Application Pool `VinamilkHrmPool`:
   - **.NET CLR version**: `No Managed Code`
   - **Managed pipeline mode**: `Integrated`
2. Tạo Website trỏ tới `C:\inetpub\wwwroot\VinamilkHRM`, gắn SSL Certificate (HTTPS 443).
3. Cấp quyền `Modify` cho `IIS AppPool\VinamilkHrmPool` vào thư mục `logs/` và `backups/`.

## 4. Cấu hình Biến môi trường Bảo mật (Không lưu mật khẩu trong git)
Thiết lập trong `IIS Manager -> Configuration Editor -> system.webServer/aspNetCore/environmentVariables` hoặc `appsettings.Production.json`:
- `ASPNETCORE_ENVIRONMENT`: `Production`
- `ConnectionStrings__DefaultConnection`: `Server=...;Database=VinamilkHRM;...`
- `Security__PiiEncryptionKey`: Khóa AES-256 bí mật (32+ ký tự).

## 5. Kiểm tra sau triển khai
- Truy cập `/health` -> trả JSON `Healthy`.
- Truy cập `/gioi-thieu`, `/tuyen-dung`, `/sitemap.xml`, `/robots.txt`.

