using Microsoft.AspNetCore.DataProtection;
using VinamilkHRM.Application.Common.Helpers;
using VinamilkHRM.Application.Common.Interfaces;

namespace VinamilkHRM.Infrastructure.Services;

public class FieldEncryptionService : IFieldEncryptionService
{
    private readonly IDataProtector _protector;
    private const string Purpose = "VinamilkHRM.SensitiveFields.v1";

    public FieldEncryptionService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    public string Encrypt(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return string.Empty;

        return _protector.Protect(plainText.Trim());
    }

    public string? Decrypt(string? cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            return null;

        try
        {
            return _protector.Unprotect(cipherText.Trim());
        }
        catch
        {
            // Nếu dữ liệu chưa mã hóa (dữ liệu thô từ legacy), trả về nguyên trạng
            return cipherText;
        }
    }

    public string Mask(string? plainOrCipherText)
    {
        if (string.IsNullOrWhiteSpace(plainOrCipherText))
            return string.Empty;

        string? decrypted = Decrypt(plainOrCipherText);
        return SensitiveDataMasker.Mask(decrypted ?? plainOrCipherText);
    }
}
