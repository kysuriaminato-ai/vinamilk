namespace VinamilkHRM.Application.Common.Helpers;

public static class SensitiveDataMasker
{
    /// <summary>
    /// Che dữ liệu nhạy cảm dạng ****1234.
    /// Giữ 4 ký tự cuối, thay phần trước bằng 4 dấu sao.
    /// </summary>
    public static string Mask(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string trimmed = value.Trim();
        if (trimmed.Length <= 4)
            return "****";

        string last4 = trimmed[^4..];
        return $"****{last4}";
    }
}
