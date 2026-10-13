using System;
using VinamilkHRM.Domain.Entities.Assets;
using VinamilkHRM.Domain.Entities.OnboardingOffboarding;
using VinamilkHRM.Domain.Enums;
using Xunit;

namespace VinamilkHRM.Domain.Tests.OnboardingOffboarding;

public class OnboardingOffboardingTests
{
    [Fact]
    public void FinalSettlement_Calculation_ShouldAccuratelyComputeNetSettlementAmount()
    {
        // ARRANGE: Nhân viên nghỉ việc ngày 15/10/2026
        int employeeId = 105;
        var lastWorkingDate = new DateOnly(2026, 10, 15);
        var officialTerminationDate = new DateOnly(2026, 10, 15);
        decimal workingDaysWorked = 12m;          // 12 ngày làm việc trong tháng
        decimal dailyRate = 1000000m;             // Lương 1 ngày: 1.000.000 VNĐ
        decimal unusedLeaveDays = 4.5m;           // 4.5 ngày phép năm chưa nghỉ
        decimal severance = 10000000m;            // Trợ cấp thôi việc: 10.000.000 VNĐ
        decimal otherAllowances = 1000000m;       // Phụ cấp khác: 1.000.000 VNĐ
        decimal deductions = 3000000m;            // Khấu trừ công nợ: 3.000.000 VNĐ

        // ACT: Khởi tạo bản ghi quyết toán cuối cùng
        var settlement = new FinalSettlement(
            employeeId,
            lastWorkingDate,
            officialTerminationDate,
            workingDaysWorked,
            dailyRate,
            unusedLeaveDays,
            severanceAllowance: severance,
            otherAllowances: otherAllowances,
            deductions: deductions);

        // ASSERT: Kiểm tra chi tiết từng thành phần quyết toán
        Assert.Equal(12000000m, settlement.EarnedSalary);       // 12 * 1.000.000
        Assert.Equal(4500000m, settlement.UnusedLeavePay);      // 4.5 * 1.000.000
        Assert.Equal(10000000m, settlement.SeveranceAllowance); // 10.000.000
        Assert.Equal(3000000m, settlement.Deductions);          // 3.000.000
        
        // Thực nhận: 12.000.000 + 4.500.000 + 10.000.000 + 1.000.000 - 3.000.000 = 24.500.000 VNĐ
        decimal expectedNet = 12000000m + 4500000m + 10000000m + 1000000m - 3000000m;
        Assert.Equal(expectedNet, settlement.NetSettlementAmount);
        Assert.Equal(FinalSettlementStatus.Calculated, settlement.Status);

        // Phê duyệt và chi trả
        var now = DateTimeOffset.UtcNow;
        settlement.Approve(approverId: 1, now);
        Assert.Equal(FinalSettlementStatus.Approved, settlement.Status);

        settlement.MarkPaid(now.AddHours(2));
        Assert.Equal(FinalSettlementStatus.Paid, settlement.Status);
    }

    [Fact]
    public void EmployeeChecklistTask_Complete_ShouldUpdateStatusAndNotes()
    {
        var task = new EmployeeChecklistTask(
            employeeId: 201,
            checklistType: ChecklistType.Onboarding,
            title: "Cấp phát laptop ThinkPad và tài khoản AD",
            description: "Cài đặt phần mềm Vinamilk ERP và VPN",
            assignedRole: "IT",
            dueDate: DateTime.Today.AddDays(2));

        Assert.False(task.IsCompleted);
        Assert.Null(task.CompletedAt);

        var now = DateTimeOffset.UtcNow;
        task.Complete(completedByUserId: 10, notes: "Đã bàn giao máy tính S/N: VNM-NB-2026-99", now: now);

        Assert.True(task.IsCompleted);
        Assert.Equal(10, task.CompletedByUserId);
        Assert.Equal(now, task.CompletedAt);
        Assert.Contains("VNM-NB-2026-99", task.Notes);
    }

    [Fact]
    public void CompanyAsset_ReturnAsset_ShouldMarkAvailableAndClearAssignment()
    {
        // ARRANGE: Tài sản cấp phát cho nhân viên
        var asset = new CompanyAsset(
            assetCode: "AST-NB-001",
            assetName: "Dell Latitude 7420",
            category: "Thiết bị CNTT",
            serialNumber: "SN-DELL-7420-VN",
            originalValue: 28000000m);

        asset.AssignToEmployee(employeeId: 301);
        Assert.Equal(AssetStatus.InUse, asset.Status);
        Assert.Equal(301, asset.AssignedToEmployeeId);

        // ACT: Thu hồi tài sản trong quy trình Offboarding
        asset.ReturnAsset("Thiết bị hoạt động tốt, phụ kiện sạc đầy đủ.");

        // ASSERT: Trạng thái chuyển về Available và gỡ người sở hữu
        Assert.Equal(AssetStatus.Available, asset.Status);
        Assert.Null(asset.AssignedToEmployeeId);
        Assert.Contains("Thu hồi", asset.Notes);
    }

    [Fact]
    public void SocialInsuranceDeclaration_WorkflowLifecycle_ShouldTransitionProperly()
    {
        var decl = new SocialInsuranceDeclaration(
            declarationNumber: "D02-OUT-101-202610",
            employeeId: 101,
            type: SocialInsuranceDeclarationType.DecreaseExit,
            effectiveMonth: "2026-10",
            insuranceSalary: 25000000m);

        Assert.Equal("Draft", decl.Status);

        decl.Submit(DateTime.Today);
        Assert.Equal("Submitted", decl.Status);
        Assert.NotNull(decl.SubmittedDate);

        decl.MarkAccepted("BHXH TP.HCM đã tiếp nhận và chốt sổ số 079090001234.");
        Assert.Equal("Accepted", decl.Status);
        Assert.Contains("chốt sổ", decl.ApprovalNotes);
    }
}