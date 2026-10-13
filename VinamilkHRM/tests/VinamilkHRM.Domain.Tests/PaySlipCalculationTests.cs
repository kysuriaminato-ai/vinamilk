using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Enums;
using Xunit;

namespace VinamilkHRM.Domain.Tests;

public class PaySlipCalculationTests
{
    [Fact]
    public void PaySlip_CalculateFinancials_ShouldComputeInsuranceAndTaxAccurately()
    {
        // Lương cơ bản: 20,000,000 VND
        // Phụ cấp: 3,000,000 VND
        // Ngày công chuẩn: 22; Thực tế làm: 22
        // Tăng ca: 10 giờ, tiền tăng ca: 1,704,545 VND
        // Thưởng KPI: 2,000,000 VND
        // Thưởng khác: 500,000 VND
        // Lương đóng bảo hiểm: 20,000,000 VND
        // Số người phụ thuộc: 1
        int employeeId = 100;
        string period = "2026-10";
        decimal baseSalary = 20_000_000m;
        decimal allowance = 3_000_000m;
        decimal actualWorkDays = 22m;
        decimal overtimeHours = 10m;
        decimal overtimePay = 1_704_545m;
        decimal kpiBonus = 2_000_000m;
        decimal otherBonuses = 500_000m;
        decimal insuranceSalary = 20_000_000m;
        decimal standardWorkDays = 22m;
        int dependentCount = 1;

        var payslip = new PaySlip(
            employeeId: employeeId,
            payrollPeriod: period,
            baseSalary: baseSalary,
            allowance: allowance,
            actualWorkDays: actualWorkDays,
            overtimeHours: overtimeHours,
            overtimePay: overtimePay,
            kpiBonus: kpiBonus,
            otherBonuses: otherBonuses,
            insuranceSalary: insuranceSalary,
            standardWorkDays: standardWorkDays,
            dependentCount: dependentCount
        );

        // Tổng thu nhập Gross = 20tr + 3tr + 1.704.545 + 2tr + 500k = 27,204,545 VND
        Assert.Equal(20_000_000m + 3_000_000m + 1_704_545m + 2_000_000m + 500_000m, payslip.GrossSalary);

        // Bảo hiểm: BHXH 8% (1.6tr), BHYT 1.5% (300k), BHTN 1% (200k) tính trên lương đóng BH 20tr
        Assert.Equal(1_600_000m, payslip.SocialInsuranceDeduction);
        Assert.Equal(300_000m, payslip.HealthInsuranceDeduction);
        Assert.Equal(200_000m, payslip.UnemploymentInsuranceDeduction);

        // Giảm trừ gia cảnh: Bản thân 11tr, người phụ thuộc 4.4tr
        Assert.Equal(11_000_000m, payslip.PersonalDeduction);
        Assert.Equal(4_400_000m, payslip.DependentDeduction);

        // Thu nhập tính thuế và thuế TNCN lũy tiến > 0
        Assert.True(payslip.TaxableIncome > 0);
        Assert.True(payslip.PersonalIncomeTax > 0);

        // Net = Gross - Total Deductions
        Assert.Equal(payslip.GrossSalary - payslip.TotalDeductions, payslip.NetSalary);
        Assert.Equal(PaySlipStatus.Draft, payslip.Status);
    }

    [Fact]
    public void PaySlip_Approve_WhenDraft_ShouldTransitionToApproved()
    {
        var payslip = new PaySlip(101, "2026-10", 15_000_000m, 1_000_000m, 22m, 0m, 0m, 0m, 0m, 15_000_000m, 22m, 0);
        Assert.Equal(PaySlipStatus.Draft, payslip.Status);

        payslip.Approve();

        Assert.Equal(PaySlipStatus.Approved, payslip.Status);
    }
}
