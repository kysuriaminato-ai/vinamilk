using FluentAssertions;
using Vinamilk.HRM.Application.Talent;
using Xunit;

namespace Vinamilk.HRM.UnitTests.Talent;

public class TalentManagementTests
{
    [Theory]
    [InlineData(95.0, "A_Excellent", 1.50)]
    [InlineData(82.5, "B_Good", 1.20)]
    [InlineData(65.0, "C_Average", 1.00)]
    [InlineData(52.0, "D_Poor", 0.50)]
    public void KpiGradingCalculator_ShouldClassifyAndComputeBonusMultiplierCorrectly(
        decimal score,
        string expectedGrade,
        decimal expectedMultiplier)
    {
        var (grade, multiplier, _) = KpiGradingCalculator.EvaluateGrade(score);

        grade.Should().Be(expectedGrade);
        multiplier.Should().Be(expectedMultiplier);
    }
}

