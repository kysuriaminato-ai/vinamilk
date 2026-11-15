using FluentAssertions;
using Vinamilk.HRM.Application.CoreHR.Lifecycle;
using Xunit;

namespace Vinamilk.HRM.UnitTests.CoreHR;

public class CoreHrTests
{
    [Fact]
    public void VietnamRetirementCalculator_ShouldCalculate62Years_ForMaleBornIn1975()
    {
        // Arrange: Lao động Nam sinh năm 1975 sẽ đủ 60 tuổi vào năm 2035 (> 2028) -> nghỉ hưu ở tuổi 62
        var dob = new DateOnly(1975, 5, 15);
        var refDate = new DateOnly(2026, 11, 15);

        // Act
        var result = VietnamRetirementCalculator.Calculate(dob, "Nam", refDate);

        // Assert
        result.RetirementAgeYears.Should().Be(62);
        result.RetirementAgeMonths.Should().Be(0);
        result.ExpectedRetirementDate.Should().Be(new DateOnly(2037, 6, 1));
    }

    [Fact]
    public void VietnamRetirementCalculator_ShouldCalculateRoadmapAge_ForFemaleBornIn1971()
    {
        // Arrange: Lao động Nữ sinh năm 1971 -> năm +55 là 2026 (bước thứ 6 kể từ 2021: +24 tháng = 57 tuổi 0 tháng)
        var dob = new DateOnly(1971, 3, 10);
        var refDate = new DateOnly(2026, 11, 15);

        // Act
        var result = VietnamRetirementCalculator.Calculate(dob, "Nữ", refDate);

        // Assert
        result.RetirementAgeYears.Should().Be(57);
        result.RetirementAgeMonths.Should().Be(0);
        result.ExpectedRetirementDate.Should().Be(new DateOnly(2028, 4, 1));
    }
}

