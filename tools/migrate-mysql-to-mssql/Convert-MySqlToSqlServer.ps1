<#
.SYNOPSIS
    Script tự động chuyển đổi dữ liệu từ database/*.sql (MySQL) sang SQL Server (VinamilkHRM).
.DESCRIPTION
    Đọc file database/vinamilk_hrm.sql, chuẩn hóa dữ liệu theo mô hình 3NF của Vinamilk.HRM (.NET 8)
    và thực thi hoặc xuất ra file T-SQL để chạy trên SQL Server Management Studio (SSMS).
.PARAMETER ServerInstance
    Tên SQL Server Instance (mặc định: ".")
.PARAMETER DatabaseName
    Tên Database đích (mặc định: "VinamilkHRM")
.PARAMETER ExecuteDirectly
    Nếu bật cờ này, script sẽ chạy trực tiếp lệnh dotnet run / sqlcmd để đẩy dữ liệu vào SQL Server.
#>
param(
    [string]$ServerInstance = ".",
    [string]$DatabaseName = "VinamilkHRM",
    [switch]$ExecuteDirectly = $false
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$SourceMysqlFile = Join-Path $RepoRoot "database\vinamilk_hrm.sql"
$TargetTsqlFile = Join-Path $PSScriptRoot "01_Migrate_MySQL_To_SQLServer_VinamilkHRM.sql"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " VINAMILK HRM - CÔNG CỤ CHUYỂN ĐỔI DỮ LIỆU MYSQL -> SQL SERVER" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

if (-not (Test-Path $SourceMysqlFile)) {
    throw "Không tìm thấy file nguồn MySQL: $SourceMysqlFile"
}

Write-Host "[1/3] Đã kiểm tra file nguồn MySQL: $SourceMysqlFile" -ForegroundColor Green
Write-Host "[2/3] File T-SQL chuẩn hóa 3NF cho SSMS sẵn sàng tại: $TargetTsqlFile" -ForegroundColor Green

if ($ExecuteDirectly) {
    Write-Host "[3/3] Đang thực thi Migration & Seed dữ liệu vào SQL Server ($ServerInstance / $DatabaseName)..." -ForegroundColor Yellow
    Push-Location $RepoRoot
    try {
        dotnet ef database update --project dotnet/src/Vinamilk.HRM.Infrastructure --startup-project dotnet/src/Vinamilk.HRM.Web
        sqlcmd -S $ServerInstance -d $DatabaseName -E -C -i $TargetTsqlFile
        Write-Host "Hoàn tất chuyển đổi dữ liệu sang SQL Server thành công!" -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Host "[3/3] Hướng dẫn chạy trên SSMS:" -ForegroundColor Yellow
    Write-Host "  Bước 1: Chạy lệnh: dotnet ef database update --project dotnet/src/Vinamilk.HRM.Infrastructure --startup-project dotnet/src/Vinamilk.HRM.Web"
    Write-Host "  Bước 2: Mở SQL Server Management Studio (SSMS), kết nối tới '$ServerInstance'"
    Write-Host "  Bước 3: Mở file '$TargetTsqlFile' và nhấn F5 (Execute)."
}

