using Vinamilk.HRM.Application.Analytics;
using Vinamilk.HRM.Infrastructure.Services;
using Xunit;

namespace Vinamilk.HRM.UnitTests.Analytics;

public class AnalyticsAndAiTests
{
    [Fact]
    public void ExportBm01AndDynamicReport_ShouldReturnNonEmptyExcelBytes()
    {
        var svc = new AnalyticsReportExcelService();
        var bm01 = svc.ExportBm01(new List<Bm01EmployeeRowDto>
        {
            new("VNM-0001", "Nguyễn Văn A", "Nam", new DateOnly(1990, 5, 10), "Phòng CNTT", "Kỹ sư phần mềm", "Đại học", new DateOnly(2020, 1, 15), "Official", "Indefinite", 25000000m)
        });
        Assert.True(bm01.Length > 1000);

        var dyn = svc.ExportDynamicReport(new DynamicReportResultDto(
            [new("EmployeeCode", "Mã NV"), new("FullName", "Họ và tên")],
            [new Dictionary<string, string> { ["EmployeeCode"] = "VNM-0001", ["FullName"] = "Nguyễn Văn A" }],
            [new("Phòng CNTT", 1, 25000000m)],
            "UnitName"));
        Assert.True(dyn.Length > 1000);
    }

    [Fact]
    public void DynamicReportCatalog_ShouldContainStandardColumns()
    {
        Assert.Contains(DynamicReportCatalog.AllColumns, c => c.Key == "EmployeeCode");
        Assert.Contains(DynamicReportCatalog.AllColumns, c => c.Key == "BaseSalary");
    }
}

