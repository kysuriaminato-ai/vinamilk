using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vinamilk.HRM.Application.Common.Interfaces;

namespace Vinamilk.HRM.Infrastructure.Security;

public sealed class PiiEncryptionService : IPiiEncryptionService
{
    private const string UserSecretsId = "vinamilk-hrm-enterprise-net8-secrets";
    private readonly byte[] _encryptionKey;
    private readonly byte[] _hmacKey;

    public PiiEncryptionService(
        IConfiguration configuration,
        IHostEnvironment? hostEnvironment = null,
        ILogger<PiiEncryptionService>? logger = null)
    {
        var secret = configuration["Security:PiiEncryptionKey"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            if (hostEnvironment?.IsDevelopment() == true)
            {
                secret = GetOrGenerateDevelopmentSecret(logger);
            }
            else
            {
                throw new InvalidOperationException(
                    "LỖI KHỞI ĐỘNG BẢO MẬT: Thiếu khóa mã hóa PII 'Security:PiiEncryptionKey'. " +
                    "Vui lòng thiết lập qua biến môi trường 'Security__PiiEncryptionKey' hoặc User Secrets.");
            }
        }

        _encryptionKey = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        _hmacKey = SHA256.HashData(Encoding.UTF8.GetBytes("HMAC_" + secret));
    }

    private static string GetOrGenerateDevelopmentSecret(ILogger<PiiEncryptionService>? logger)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var baseDir = string.IsNullOrWhiteSpace(appData)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".microsoft", "usersecrets")
            : Path.Combine(appData, "Microsoft", "UserSecrets");

        var secretsDir = Path.Combine(baseDir, UserSecretsId);
        var secretsFile = Path.Combine(secretsDir, "secrets.json");

        Dictionary<string, string>? dict = null;
        if (File.Exists(secretsFile))
        {
            try
            {
                dict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(secretsFile));
                if (dict is not null && dict.TryGetValue("Security:PiiEncryptionKey", out var existing) && !string.IsNullOrWhiteSpace(existing))
                {
                    logger?.LogWarning("CẢNH BÁO BẢO MẬT (Dev): Đang sử dụng khóa 'Security:PiiEncryptionKey' tự sinh từ User Secrets.");
                    return existing;
                }
            }
            catch
            {
                dict = null;
            }
        }

        var generated = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        dict ??= new Dictionary<string, string>();
        dict["Security:PiiEncryptionKey"] = generated;

        Directory.CreateDirectory(secretsDir);
        File.WriteAllText(secretsFile, JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));

        logger?.LogWarning(
            "CẢNH BÁO BẢO MẬT (Dev): Chưa cấu hình 'Security:PiiEncryptionKey'. Hệ thống đã tự sinh khóa ngẫu nhiên và lưu vào User Secrets ({SecretsPath}).",
            secretsFile);

        return generated;
    }

    public string Encrypt(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return string.Empty;

        return EncryptInternal(plainText.Trim(), _encryptionKey);
    }

    public string Decrypt(string? cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            return string.Empty;

        try
        {
            var fullCipher = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;

            var ivLength = aes.BlockSize / 8;
            if (fullCipher.Length <= ivLength)
                return cipherText;

            var iv = new byte[ivLength];
            var cipher = new byte[fullCipher.Length - ivLength];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipher);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);
            return sr.ReadToEnd();
        }
        catch
        {
            // Hỗ trợ đọc trực tiếp nếu dữ liệu cũ chưa mã hóa
            return cipherText;
        }
    }

    public string ComputeDeterministicHash(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        using var hmac = new HMACSHA256(_hmacKey);
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input.Trim().ToUpperInvariant()));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string EncryptInternal(string plainText, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        using var hmacIv = new HMACSHA256(key);
        var hash = hmacIv.ComputeHash(Encoding.UTF8.GetBytes(plainText));
        var iv = new byte[16];
        Buffer.BlockCopy(hash, 0, iv, 0, 16);
        aes.IV = iv;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        ms.Write(iv, 0, iv.Length);
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs, Encoding.UTF8))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }
}
