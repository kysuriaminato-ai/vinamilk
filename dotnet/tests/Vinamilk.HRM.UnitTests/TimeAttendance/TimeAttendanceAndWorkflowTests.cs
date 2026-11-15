using FluentAssertions;
using Vinamilk.HRM.Application.TimeAttendance;
using Vinamilk.HRM.Application.Workflows;
using Xunit;

namespace Vinamilk.HRM.UnitTests.TimeAttendance;

public class TimeAttendanceAndWorkflowTests
{
    [Fact]
    public void GeofenceCalculator_ShouldAcceptCoordinatesInsideVinamilkHeadOfficeRadius()
    {
        // Tọa độ ngay tại Trụ sở chính Vinamilk Tower (Số 10 Tân Trào, Q.7, TP.HCM)
        var (nearest, distanceMeters, isWithin) = VinamilkGeofenceCalculator.EvaluateGps(10.7297, 106.7219);

        nearest.Code.Should().Be("HQ_SG");
        distanceMeters.Should().BeLessThan(100);
        isWithin.Should().BeTrue();
    }

    [Fact]
    public void AnnualLeavePolicyCalculator_ShouldAddOneExtraDayPerFiveYearsOfService()
    {
        // Nhân viên vào làm từ 10/03/2014 -> tính đến năm 2026 là 12 năm thâm niên -> +2 ngày phép thâm niên
        var (seniorityYears, standardDays, extraDays) = VietnamAnnualLeavePolicyCalculator.CalculateEntitlement(
            new DateOnly(2014, 3, 10),
            2026);

        seniorityYears.Should().Be(12);
        standardDays.Should().Be(12m);
        extraDays.Should().Be(2m);
    }

    [Fact]
    public void AnnualLeavePolicyCalculator_ShouldExcludeSundaysAndPublicHolidays()
    {
        var holidays = new HashSet<DateOnly> { new(2026, 9, 2), new(2026, 9, 3) };
        var totalDays = VietnamAnnualLeavePolicyCalculator.CalculateWorkingLeaveDays(
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 5),
            "FullDay",
            "FullDay",
            holidays);

        // Từ 01/09 đến 05/09/2026 (5 ngày lịch), trừ 2 ngày lễ Quốc khánh (02/09, 03/09) -> còn 3.0 ngày nghỉ phép thực tế
        totalDays.Should().Be(3.0m);
    }

    [Theory]
    [InlineData("TotalDays >= 3", 3.0, true)]
    [InlineData("TotalDays >= 3", 1.5, false)]
    [InlineData("RequestedHours >= 4", 6.0, true)]
    [InlineData("Always", 0.5, true)]
    public void WorkflowConditionEvaluator_ShouldEvaluateNumericStepConditionsAccurately(
        string expression,
        decimal metricValue,
        bool expectedResult)
    {
        var actual = WorkflowSeederHelper.EvaluateStepCondition(expression, metricValue);
        actual.Should().Be(expectedResult);
    }
}

