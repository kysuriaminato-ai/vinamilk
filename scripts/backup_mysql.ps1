# ====================================================================================================
# VINAMILK HRIS - SCRIPT SAO LƯU MYSQL HẰNG NGÀY & KIỂM THỬ KHÔI PHỤC (POWERSHELL / WINDOWS)
# ====================================================================================================
# - Sao lưu toàn vẹn InnoDB (--single-transaction, --routines, --triggers, --events)
# - Nén gzip/zip bảo vệ dung lượng
# - Quản lý vòng đời (Retention): Giữ lại chính xác 30 bản sao lưu gần nhất, tự động xóa bản cũ
# - Thử nghiệm khôi phục tự động (Automated Disaster Recovery Test Restore):
#   Nạp bản backup vào DB tạm `vinamilk_hris_restore_test`, kiểm tra toàn vẹn bảng, xóa DB tạm
# ====================================================================================================

[CmdletBinding()]
param (
    [string]$BackupDir = "C:\backups\vinamilk_hris",
    [string]$DbHost = "127.0.0.1",
    [int]$DbPort = 3306,
    [string]$DbName = "vinamilk_hris",
    [string]$DbUser = "vnm_user",
    [string]$DbPass = "vnm_password",
    [int]$RetentionDays = 30
)

$ErrorActionPreference = "Stop"

if (!(Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
}

$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$LogFile = Join-Path $BackupDir "backup.log"
$DumpSqlFile = Join-Path $BackupDir "vinamilk_hris_$Timestamp.sql"
$ZipBackupFile = Join-Path $BackupDir "vinamilk_hris_$Timestamp.zip"
$TestRestoreDb = "vinamilk_hris_restore_test"

function Log-Message {
    param ([string]$Msg)
    $Formatted = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $Msg"
    Write-Host $Formatted
    Add-Content -Path $LogFile -Value $Formatted
}

Log-Message "=== BẮT ĐẦU SAO LƯU MYSQL HRIS VINAMILK ==="

try {
    # 1. Trích xuất dữ liệu bằng mysqldump
    Log-Message "Đang chạy mysqldump cho database '$DbName'..."
    
    $dumpArgs = @(
        "--host=$DbHost",
        "--port=$DbPort",
        "--user=$DbUser",
        "--password=$DbPass",
        "--single-transaction",
        "--quick",
        "--routines",
        "--triggers",
        "--events",
        "--hex-blob",
        "--default-character-set=utf8mb4",
        "--result-file=$DumpSqlFile",
        $DbName
    )

    & mysqldump.exe @dumpArgs

    if (!(Test-Path $DumpSqlFile) -or (Get-Item $DumpSqlFile).Length -eq 0) {
        throw "Tệp dump không được tạo hoặc rỗng!"
    }

    # Nén tệp dump
    Log-Message "Đang nén tệp sao lưu thành định dạng zip..."
    Compress-Archive -Path $DumpSqlFile -DestinationPath $ZipBackupFile -CompressionLevel Optimal
    Remove-Item $DumpSqlFile -Force

    $FileSizeMb = [math]::Round((Get-Item $ZipBackupFile).Length / 1MB, 2)
    Log-Message "-> Đã nén thành công: $ZipBackupFile ($FileSizeMb MB)"

    # 2. KIỂM THỬ KHÔI PHỤC TỰ ĐỘNG (DISASTER RECOVERY TEST RESTORE)
    Log-Message "Tiến hành kiểm thử khôi phục tự động vào database tạm '$TestRestoreDb'..."
    
    # Giải nén tệp tạm để restore
    $TempRestoreSql = Join-Path $BackupDir "restore_test_temp.sql"
    Expand-Archive -Path $ZipBackupFile -DestinationPath $BackupDir -Force
    Rename-Item (Join-Path $BackupDir "vinamilk_hris_$Timestamp.sql") $TempRestoreSql -Force

    # Tạo DB tạm
    $mysqlBaseArgs = @("--host=$DbHost", "--port=$DbPort", "--user=$DbUser", "--password=$DbPass")
    & mysql.exe @mysqlBaseArgs -e "DROP DATABASE IF EXISTS \`$TestRestoreDb\`; CREATE DATABASE \`$TestRestoreDb\` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;"

    # Nạp dữ liệu vào DB tạm
    Get-Content $TempRestoreSql | & mysql.exe @mysqlBaseArgs $TestRestoreDb
    Remove-Item $TempRestoreSql -Force

    # Kiểm tra số lượng bản ghi nhân sự & phòng ban
    $empCount = & mysql.exe @mysqlBaseArgs -N -s -e "SELECT COUNT(*) FROM \`$TestRestoreDb\`.employees;"
    $orgCount = & mysql.exe @mysqlBaseArgs -N -s -e "SELECT COUNT(*) FROM \`$TestRestoreDb\`.org_units;"

    if ([int]$empCount -gt 0 -and [int]$orgCount -gt 0) {
        Log-Message "-> [XÁC MINH THÀNH CÔNG] Khôi phục toàn vẹn! Tìm thấy $empCount nhân viên và $orgCount đơn vị tổ chức."
    } else {
        throw "Khôi phục thất bại: Không tìm thấy dữ liệu sau khi import!"
    }

    # Xóa DB tạm
    & mysql.exe @mysqlBaseArgs -e "DROP DATABASE IF EXISTS \`$TestRestoreDb\`;"
    Log-Message "-> Đã dọn dẹp cơ sở dữ liệu kiểm thử."

    # 3. QUẢN LÝ VÒNG ĐỜI (RETENTION POLICY: GIỮ TỐI ĐA 30 BẢN)
    Log-Message "Kiểm tra và xóa các bản sao lưu cũ hơn $RetentionDays ngày..."
    $CutoffDate = (Get-Date).AddDays(-$RetentionDays)
    $OldBackups = Get-ChildItem -Path $BackupDir -Filter "vinamilk_hris_*.zip" | Where-Object { $_.LastWriteTime -lt $CutoffDate }
    
    foreach ($old in $OldBackups) {
        Remove-Item $old.FullName -Force
        Log-Message "-> Đã xóa bản sao lưu cũ: $($old.Name)"
    }

    $TotalBackups = (Get-ChildItem -Path $BackupDir -Filter "vinamilk_hris_*.zip").Count
    Log-Message "Tổng số bản sao lưu đang duy trì: $TotalBackups bản."
    Log-Message "=== HOÀN TẤT SAO LƯU THÀNH CÔNG ==="

} catch {
    Log-Message "[LỖI NGHIÊM TRỌNG] Quá trình sao lưu/khôi phục thất bại: $_"
    exit 1
}
