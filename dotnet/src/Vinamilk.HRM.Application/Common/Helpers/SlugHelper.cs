using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Vinamilk.HRM.Application.Common.Helpers;

public static partial class SlugHelper
{
    public static string ToSlug(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "tin-tuyen-dung";

        var normalized = input.Trim().ToLowerInvariant()
            .Replace('đ', 'd')
            .Replace('Đ', 'd')
            .Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        var ascii = sb.ToString().Normalize(NormalizationForm.FormC);
        ascii = InvalidCharsRegex().Replace(ascii, "-");
        ascii = MultiHyphenRegex().Replace(ascii, "-").Trim('-');

        return string.IsNullOrEmpty(ascii) ? "tin-tuyen-dung" : ascii;
    }

    [GeneratedRegex("[^a-z0-9\\s-]")]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex("[\\s-]+")]
    private static partial Regex MultiHyphenRegex();
}

