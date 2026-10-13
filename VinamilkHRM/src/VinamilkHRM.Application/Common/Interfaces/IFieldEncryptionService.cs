namespace VinamilkHRM.Application.Common.Interfaces;

/// <summary>
/// Dịch vụ mã hóa và giải mã các trường dữ liệu nhạy cảm (CCCD, Số tài khoản ngân hàng)
/// sử dụng ASP.NET Core Data Protection.
/// </summary>
public interface IFieldEncryptionService
{
    /// <summary>
    /// Mã hóa chuỗi văn bản gốc thành chuỗi an toàn.
    /// </summary>
    string Encrypt(string? plainText);

    /// <summary>
    /// Giải mã chuỗi đã mã hóa về dạng văn bản thô.
    /// </summary>
    string? Decrypt(string? cipherText);

    /// <summary>
    /// Che dữ liệu nhạy cảm dạng ****1234 (lấy 4 ký tự cuối).
    /// </summary>
    string Mask(string? plainOrCipherText);
}
