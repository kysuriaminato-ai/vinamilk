<#
.SYNOPSIS
    Xoay khóa mã hóa PII (AES-256 + HMAC-SHA256) cho bảng Employees trong SQL Server.
.EXAMPLE
    .\RotatePiiKey.ps1 -ConnectionString "Server=.;Database=VinamilkHRM;Trusted_Connection=True;TrustServerCertificate=True" -OldKey "OLD_KEY" -NewKey "NEW_KEY"
#>
param(
    [Parameter(Mandatory = $true)][string]$ConnectionString,
    [Parameter(Mandatory = $true)][string]$OldKey,
    [Parameter(Mandatory = $true)][string]$NewKey
)

$ErrorActionPreference = "Stop"

function Get-Sha256Bytes([string]$text) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    return $sha.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($text))
}

function Decrypt-Aes([string]$cipherBase64, [byte[]]$key) {
    if ([string]::IsNullOrWhiteSpace($cipherBase64)) { return "" }
    $full = [Convert]::FromBase64String($cipherBase64)
    $aes = [System.Security.Cryptography.Aes]::Create()
    $aes.Key = $key
    $ivLen = $aes.BlockSize / 8
    $iv = New-Object byte[] $ivLen
    $cipher = New-Object byte[] ($full.Length - $ivLen)
    [Array]::Copy($full, 0, $iv, 0, $ivLen)
    [Array]::Copy($full, $ivLen, $cipher, 0, $cipher.Length)
    $aes.IV = $iv
    $ms = New-Object System.IO.MemoryStream (,$cipher)
    $cs = New-Object System.Security.Cryptography.CryptoStream ($ms, $aes.CreateDecryptor(), [System.Security.Cryptography.CryptoStreamMode]::Read)
    $sr = New-Object System.IO.StreamReader ($cs, [System.Text.Encoding]::UTF8)
    return $sr.ReadToEnd()
}

function Encrypt-Aes([string]$plainText, [byte[]]$key) {
    if ([string]::IsNullOrWhiteSpace($plainText)) { return "" }
    $aes = [System.Security.Cryptography.Aes]::Create()
    $aes.Key = $key
    $aes.GenerateIV()
    $ms = New-Object System.IO.MemoryStream
    $ms.Write($aes.IV, 0, $aes.IV.Length)
    $cs = New-Object System.Security.Cryptography.CryptoStream ($ms, $aes.CreateEncryptor(), [System.Security.Cryptography.CryptoStreamMode]::Write)
    $sw = New-Object System.IO.StreamWriter ($cs, [System.Text.Encoding]::UTF8)
    $sw.Write($plainText.Trim())
    $sw.Flush()
    $cs.FlushFinalBlock()
    return [Convert]::ToBase64String($ms.ToArray())
}

function Compute-Hmac([string]$plainText, [byte[]]$hmacKey) {
    if ([string]::IsNullOrWhiteSpace($plainText)) { return "" }
    $hmac = New-Object System.Security.Cryptography.HMACSHA256 (,$hmacKey)
    $bytes = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($plainText.Trim().ToUpperInvariant()))
    return ([BitConverter]::ToString($bytes) -replace "-", "").ToLowerInvariant()
}

$oldEncKey = Get-Sha256Bytes $OldKey
$newEncKey = Get-Sha256Bytes $NewKey
$newHmacKey = Get-Sha256Bytes ("HMAC_" + $NewKey)

$conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
$conn.Open()
$tx = $conn.BeginTransaction()

try {
    $selectCmd = $conn.CreateCommand()
    $selectCmd.Transaction = $tx
    $selectCmd.CommandText = "SELECT Id, CitizenIdEncrypted, BankAccountEncrypted, TaxCodeEncrypted FROM Employees"
    $reader = $selectCmd.ExecuteReader()
    $rows = @()
    while ($reader.Read()) {
        $rows += [PSCustomObject]@{
            Id = $reader.GetInt32(0)
            CitizenId = if ($reader.IsDBNull(1)) { "" } else { $reader.GetString(1) }
            BankAcc = if ($reader.IsDBNull(2)) { "" } else { $reader.GetString(2) }
            TaxCode = if ($reader.IsDBNull(3)) { "" } else { $reader.GetString(3) }
        }
    }
    $reader.Close()

    foreach ($r in $rows) {
        $plainCid = Decrypt-Aes $r.CitizenId $oldEncKey
        $plainBank = Decrypt-Aes $r.BankAcc $oldEncKey
        $plainTax = Decrypt-Aes $r.TaxCode $oldEncKey

        $upd = $conn.CreateCommand()
        $upd.Transaction = $tx
        $upd.CommandText = "UPDATE Employees SET CitizenIdEncrypted=@cid, CitizenIdHash=@cidHash, BankAccountEncrypted=@bank, TaxCodeEncrypted=@tax WHERE Id=@id"
        $upd.Parameters.AddWithValue("@id", $r.Id) | Out-Null
        $upd.Parameters.AddWithValue("@cid", (Encrypt-Aes $plainCid $newEncKey)) | Out-Null
        $upd.Parameters.AddWithValue("@cidHash", (Compute-Hmac $plainCid $newHmacKey)) | Out-Null
        $upd.Parameters.AddWithValue("@bank", (Encrypt-Aes $plainBank $newEncKey)) | Out-Null
        $upd.Parameters.AddWithValue("@tax", (Encrypt-Aes $plainTax $newEncKey)) | Out-Null
        $upd.ExecuteNonQuery() | Out-Null
    }

    $tx.Commit()
    Write-Host "Da xoay khoa PII thanh cong cho $($rows.Count) ho so nhan su." -ForegroundColor Green
}
catch {
    $tx.Rollback()
    throw
}
finally {
    $conn.Close()
}

