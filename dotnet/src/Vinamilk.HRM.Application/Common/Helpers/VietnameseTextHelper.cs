using System.Globalization;
using System.Text;

namespace Vinamilk.HRM.Application.Common.Helpers;

public static class VietnameseTextHelper
{
    public static string RemoveDiacriticsAndUpper(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = text.Trim().Replace('Đ', 'D').Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

    public static string ExtractPayslipPassword(string? plainCitizenId, string employeeCode, DateOnly dateOfBirth)
    {
        if (!string.IsNullOrWhiteSpace(plainCitizenId))
        {
            var digits = new string(plainCitizenId.Where(char.IsDigit).ToArray());
            if (digits.Length >= 6)
                return digits[^6..];
            if (digits.Length > 0)
                return digits.PadLeft(6, '0');
        }

        if (dateOfBirth != default)
            return dateOfBirth.ToString("ddMMyy");

        var codeDigits = new string(employeeCode.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        return string.IsNullOrWhiteSpace(codeDigits) ? "123456" : codeDigits;
    }
}

