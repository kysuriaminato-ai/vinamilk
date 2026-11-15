using System.Security.Cryptography;
using System.Text;

namespace Vinamilk.HRM.Infrastructure.Security;

public sealed record PasswordValidationResult(bool IsValid, IReadOnlyList<string> Errors);

/// <summary>
/// Dịch vụ băm mật khẩu chuẩn PBKDF2-HMACSHA256 (100.000 vòng lặp) tương thích ngược với dữ liệu Seeded,
/// kết hợp kiểm tra Chính sách Mật khẩu Doanh nghiệp Vinamilk HRM.
/// </summary>
public sealed class PasswordSecurityService
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;
    private const string Prefix = "PBKDF2$";

    public string HashPassword(string rawPassword)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(rawPassword),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return $"{Prefix}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool IsPbkdf2Hash(string? storedHash) =>
        !string.IsNullOrWhiteSpace(storedHash) && storedHash.StartsWith(Prefix, StringComparison.Ordinal);

    public bool VerifyPassword(string rawPassword, string storedHash) =>
        VerifyPassword(rawPassword, storedHash, out _);

    public bool VerifyPassword(string rawPassword, string storedHash, out bool isLegacyHash)
    {
        isLegacyHash = false;

        if (string.IsNullOrWhiteSpace(rawPassword) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        // 1. Kiểm tra định dạng PBKDF2$100000$<salt>$<hash>
        if (storedHash.StartsWith(Prefix, StringComparison.Ordinal))
        {
            var parts = storedHash.Split('$');
            if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations))
                return false;

            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(rawPassword),
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }

        // 2. Tương thích với dữ liệu legacy (SHA256 + Salt cũ) để tự động nâng cấp sang PBKDF2 khi đăng nhập
        var legacyHash = ComputeLegacySeededHash(rawPassword);
        if (CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(legacyHash),
                Encoding.UTF8.GetBytes(storedHash)))
        {
            isLegacyHash = true;
            return true;
        }

        return false;
    }

    public PasswordValidationResult ValidatePasswordPolicy(string newPassword, string? username = null)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
            errors.Add("Mật khẩu phải có độ dài tối thiểu 8 ký tự.");

        if (!string.IsNullOrEmpty(newPassword))
        {
            if (!newPassword.Any(char.IsUpper))
                errors.Add("Mật khẩu phải chứa ít nhất 01 chữ cái viết HOA (A–Z).");

            if (!newPassword.Any(char.IsLower))
                errors.Add("Mật khẩu phải chứa ít nhất 01 chữ cái viết thường (a–z).");

            if (!newPassword.Any(char.IsDigit))
                errors.Add("Mật khẩu phải chứa ít nhất 01 chữ số (0–9).");

            if (!newPassword.Any(ch => !char.IsLetterOrDigit(ch)))
                errors.Add("Mật khẩu phải chứa ít nhất 01 ký tự đặc biệt (!@#$%^&*...).");

            if (!string.IsNullOrWhiteSpace(username) &&
                newPassword.Contains(username, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Mật khẩu không được chứa tên đăng nhập của tài khoản.");
            }
        }

        return new PasswordValidationResult(errors.Count == 0, errors);
    }

    private static string ComputeLegacySeededHash(string rawPassword)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes("VinamilkHRM_Salt_2026::" + rawPassword));
        return Convert.ToBase64String(bytes);
    }
}

