namespace Vinamilk.HRM.Application.CoreHR.Lifecycle;

public sealed record RetirementCalculationResult(
    DateOnly DateOfBirth,
    string Gender,
    int CurrentAgeYears,
    int CurrentAgeMonths,
    int RetirementAgeYears,
    int RetirementAgeMonths,
    DateOnly ExpectedRetirementDate,
    int RemainingMonths,
    string LegalBasisNote);

/// <summary>
/// Bộ tính tuổi nghỉ hưu và thời điểm nghỉ hưu theo Điều 169 Bộ luật Lao động 2019
/// và Nghị định 135/2020/NĐ-CP (Lộ trình tăng tuổi nghỉ hưu: Nam +3 tháng/năm tới 62 tuổi vào 2028; Nữ +4 tháng/năm tới 60 tuổi vào 2035).
/// </summary>
public static class VietnamRetirementCalculator
{
    public static RetirementCalculationResult Calculate(DateOnly dateOfBirth, string gender, DateOnly? referenceDate = null)
    {
        var today = referenceDate ?? DateOnly.FromDateTime(DateTime.Today);
        var isFemale = string.Equals(gender, "Nữ", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(gender, "Female", StringComparison.OrdinalIgnoreCase);

        int retYears;
        int retMonths;

        if (!isFemale)
        {
            // Lao động Nam: Sinh từ tháng 10/1963 trở đi nghỉ hưu ở tuổi 62 (từ năm 2028 trở đi).
            // Trước đó tăng 3 tháng mỗi năm kể từ năm 2021 (60 tuổi 3 tháng).
            var baseYear60 = dateOfBirth.Year + 60;
            if (baseYear60 <= 2020)
            {
                retYears = 60;
                retMonths = 0;
            }
            else
            {
                var steps = Math.Min(8, baseYear60 - 2020);
                var totalMonths = 60 * 12 + steps * 3;
                if (totalMonths >= 62 * 12)
                {
                    retYears = 62;
                    retMonths = 0;
                }
                else
                {
                    retYears = totalMonths / 12;
                    retMonths = totalMonths % 12;
                }
            }
        }
        else
        {
            // Lao động Nữ: Tăng 4 tháng mỗi năm kể từ năm 2021 (55 tuổi 4 tháng) cho tới đủ 60 tuổi vào năm 2035.
            var baseYear55 = dateOfBirth.Year + 55;
            if (baseYear55 <= 2020)
            {
                retYears = 55;
                retMonths = 0;
            }
            else
            {
                var steps = Math.Min(15, baseYear55 - 2020);
                var totalMonths = 55 * 12 + steps * 4;
                if (totalMonths >= 60 * 12)
                {
                    retYears = 60;
                    retMonths = 0;
                }
                else
                {
                    retYears = totalMonths / 12;
                    retMonths = totalMonths % 12;
                }
            }
        }

        // Thời điểm hưởng lương hưu là ngày đầu tiên của tháng liền kề sau tháng đủ tuổi nghỉ hưu
        var exactBirthdayPlusAge = dateOfBirth.AddYears(retYears).AddMonths(retMonths);
        var nextMonth = exactBirthdayPlusAge.AddMonths(1);
        var expectedRetirementDate = new DateOnly(nextMonth.Year, nextMonth.Month, 1);

        var totalMonthsToday = (today.Year - dateOfBirth.Year) * 12 + (today.Month - dateOfBirth.Month);
        if (today.Day < dateOfBirth.Day)
            totalMonthsToday = Math.Max(0, totalMonthsToday - 1);

        var currentAgeYears = totalMonthsToday / 12;
        var currentAgeMonths = totalMonthsToday % 12;

        var remainingMonths = Math.Max(
            0,
            (expectedRetirementDate.Year - today.Year) * 12 + (expectedRetirementDate.Month - today.Month));

        var legalNote = isFemale
            ? $"Lao động Nữ (Điều 169 BLLĐ 2019 & NĐ 135/2020): Đủ {retYears} tuổi {(retMonths > 0 ? $"{retMonths} tháng" : "")}"
            : $"Lao động Nam (Điều 169 BLLĐ 2019 & NĐ 135/2020): Đủ {retYears} tuổi {(retMonths > 0 ? $"{retMonths} tháng" : "")}";

        return new RetirementCalculationResult(
            dateOfBirth,
            isFemale ? "Nữ" : "Nam",
            currentAgeYears,
            currentAgeMonths,
            retYears,
            retMonths,
            expectedRetirementDate,
            remainingMonths,
            legalNote);
    }
}

