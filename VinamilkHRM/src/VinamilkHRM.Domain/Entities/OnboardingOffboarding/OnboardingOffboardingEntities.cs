using System;
using System.Collections.Generic;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;

namespace VinamilkHRM.Domain.Entities.OnboardingOffboarding;

public enum ChecklistType
{
    Onboarding = 1,
    Offboarding = 2
}

public enum SocialInsuranceDeclarationType
{
    IncreaseNew = 1,    // Báo tăng mới lao động (Onboarding)
    DecreaseExit = 2    // Báo giảm lao động (Offboarding/Thôi việc)
}

public enum FinalSettlementStatus
{
    Draft = 0,
    Calculated = 1,
    Approved = 2,
    Paid = 3
}

/// <summary>
/// Mẫu danh mục công việc tiếp nhận hoặc thôi việc (Checklist Template).
/// </summary>
public class ChecklistTemplate : BaseAuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public ChecklistType ChecklistType { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    private readonly List<ChecklistTemplateItem> _items = new();
    public virtual IReadOnlyCollection<ChecklistTemplateItem> Items => _items.AsReadOnly();

    protected ChecklistTemplate() { }

    public ChecklistTemplate(string code, string name, ChecklistType checklistType, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Mã mẫu checklist không được để trống.", "ERR_CHECKLIST_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tên mẫu checklist không được để trống.", "ERR_CHECKLIST_NAME_EMPTY");

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        ChecklistType = checklistType;
        Description = description?.Trim();
        IsActive = true;
    }

    public void AddItem(ChecklistTemplateItem item)
    {
        if (item == null) throw new DomainException("Mục checklist không thể là null.", "ERR_CHECKLIST_ITEM_NULL");
        _items.Add(item);
    }
}

/// <summary>
/// Chi tiết một hạng mục công việc trong mẫu Checklist (phân vai trò và hạn thực hiện).
/// </summary>
public class ChecklistTemplateItem : BaseAuditableEntity
{
    public int ChecklistTemplateId { get; private set; }
    public virtual ChecklistTemplate? ChecklistTemplate { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string AssignedRole { get; private set; } = "HR"; // "HR", "IT", "DirectManager", "Finance", "Employee"
    public int DueDaysOffset { get; private set; } = 0;       // Số ngày chênh lệch so với ngày bắt đầu / ngày nghỉ
    public bool IsRequired { get; private set; } = true;
    public int Sequence { get; private set; } = 1;

    protected ChecklistTemplateItem() { }

    public ChecklistTemplateItem(
        string title,
        string description,
        string assignedRole,
        int dueDaysOffset,
        bool isRequired = true,
        int sequence = 1)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Tiêu đề công việc không được để trống.", "ERR_CHECKLIST_TITLE_EMPTY");

        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        AssignedRole = assignedRole.Trim();
        DueDaysOffset = dueDaysOffset;
        IsRequired = isRequired;
        Sequence = sequence;
    }
}

/// <summary>
/// Nhiệm vụ checklist cụ thể giao cho nhân viên hoặc phòng ban trong quy trình Onboarding/Offboarding.
/// </summary>
public class EmployeeChecklistTask : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public ChecklistType ChecklistType { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string AssignedRole { get; private set; } = "HR";
    public int? AssignedToUserId { get; private set; }
    public DateTime DueDate { get; private set; }

    public bool IsCompleted { get; private set; } = false;
    public DateTimeOffset? CompletedAt { get; private set; }
    public int? CompletedByUserId { get; private set; }
    public string? Notes { get; private set; }

    protected EmployeeChecklistTask() { }

    public EmployeeChecklistTask(
        int employeeId,
        ChecklistType checklistType,
        string title,
        string description,
        string assignedRole,
        DateTime dueDate,
        int? assignedToUserId = null)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên không hợp lệ.", "ERR_TASK_EMP_INVALID");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Tiêu đề nhiệm vụ không được để trống.", "ERR_TASK_TITLE_EMPTY");

        EmployeeId = employeeId;
        ChecklistType = checklistType;
        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        AssignedRole = assignedRole.Trim();
        DueDate = dueDate.Date;
        AssignedToUserId = assignedToUserId;
        IsCompleted = false;
    }

    public void Complete(int completedByUserId, string? notes = null, DateTimeOffset now = default)
    {
        if (IsCompleted)
            throw new DomainException("Nhiệm vụ này đã được hoàn thành trước đó.", "ERR_TASK_ALREADY_COMPLETED");

        IsCompleted = true;
        CompletedByUserId = completedByUserId;
        CompletedAt = now == default ? DateTimeOffset.UtcNow : now;
        Notes = notes?.Trim();
    }
}

/// <summary>
/// Hồ sơ Đăng ký Tăng / Giảm Bảo hiểm Xã hội (Mẫu D02-LT).
/// </summary>
public class SocialInsuranceDeclaration : BaseAuditableEntity
{
    public string DeclarationNumber { get; private set; } = string.Empty;
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public SocialInsuranceDeclarationType Type { get; private set; }
    public string EffectiveMonth { get; private set; } = string.Empty; // YYYY-MM
    public decimal InsuranceSalary { get; private set; }
    public string Status { get; private set; } = "Draft"; // "Draft", "Submitted", "Accepted"
    public DateTime? SubmittedDate { get; private set; }
    public string? ApprovalNotes { get; private set; }

    protected SocialInsuranceDeclaration() { }

    public SocialInsuranceDeclaration(
        string declarationNumber,
        int employeeId,
        SocialInsuranceDeclarationType type,
        string effectiveMonth,
        decimal insuranceSalary)
    {
        if (string.IsNullOrWhiteSpace(declarationNumber))
            throw new DomainException("Số hồ sơ BHXH không được để trống.", "ERR_SI_DECLARATION_NUM_EMPTY");

        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên không hợp lệ.", "ERR_SI_EMP_INVALID");

        if (insuranceSalary <= 0)
            throw new DomainException("Mức lương đóng BHXH phải lớn hơn 0.", "ERR_SI_SALARY_INVALID");

        DeclarationNumber = declarationNumber.Trim().ToUpperInvariant();
        EmployeeId = employeeId;
        Type = type;
        EffectiveMonth = effectiveMonth.Trim();
        InsuranceSalary = insuranceSalary;
        Status = "Draft";
    }

    public void Submit(DateTime submitDate)
    {
        Status = "Submitted";
        SubmittedDate = submitDate;
    }

    public void MarkAccepted(string notes)
    {
        Status = "Accepted";
        ApprovalNotes = notes?.Trim();
    }
}

/// <summary>
/// Quyết toán cuối khi thôi việc (Final Settlement / Liquidation).
/// Tính lương đến ngày nghỉ, phép chưa nghỉ, khấu trừ và trợ cấp thôi việc.
/// </summary>
public class FinalSettlement : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public DateOnly LastWorkingDate { get; private set; }
    public DateOnly OfficialTerminationDate { get; private set; }

    // Các thành phần thanh toán
    public decimal WorkingDaysWorkedInMonth { get; private set; }
    public decimal DailySalaryRate { get; private set; }
    public decimal EarnedSalary { get; private set; }

    public decimal RemainingAnnualLeaveDays { get; private set; }
    public decimal UnusedLeavePay { get; private set; }

    public decimal SeveranceAllowance { get; private set; } // Trợ cấp thôi việc Điều 46 BLLĐ 2019
    public decimal OtherAllowances { get; private set; }
    public decimal Deductions { get; private set; }          // Khấu trừ nợ tạm ứng, bàn giao thiếu tài sản
    public decimal NetSettlementAmount { get; private set; }

    public FinalSettlementStatus Status { get; private set; } = FinalSettlementStatus.Draft;
    public int? ApprovedById { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }

    protected FinalSettlement() { }

    public FinalSettlement(
        int employeeId,
        DateOnly lastWorkingDate,
        DateOnly officialTerminationDate,
        decimal workingDaysWorkedInMonth,
        decimal dailySalaryRate,
        decimal remainingAnnualLeaveDays,
        decimal severanceAllowance = 0m,
        decimal otherAllowances = 0m,
        decimal deductions = 0m)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên không hợp lệ.", "ERR_SETTLEMENT_EMP_INVALID");

        if (dailySalaryRate <= 0)
            throw new DomainException("Mức lương ngày tính toán phải lớn hơn 0.", "ERR_SETTLEMENT_RATE_INVALID");

        EmployeeId = employeeId;
        LastWorkingDate = lastWorkingDate;
        OfficialTerminationDate = officialTerminationDate;
        WorkingDaysWorkedInMonth = workingDaysWorkedInMonth;
        DailySalaryRate = dailySalaryRate;
        RemainingAnnualLeaveDays = remainingAnnualLeaveDays;

        EarnedSalary = Math.Round(workingDaysWorkedInMonth * dailySalaryRate, 0, MidpointRounding.AwayFromZero);
        UnusedLeavePay = Math.Round(remainingAnnualLeaveDays * dailySalaryRate, 0, MidpointRounding.AwayFromZero);
        SeveranceAllowance = Math.Round(severanceAllowance, 0, MidpointRounding.AwayFromZero);
        OtherAllowances = Math.Round(otherAllowances, 0, MidpointRounding.AwayFromZero);
        Deductions = Math.Round(deductions, 0, MidpointRounding.AwayFromZero);

        NetSettlementAmount = EarnedSalary + UnusedLeavePay + SeveranceAllowance + OtherAllowances - Deductions;
        Status = FinalSettlementStatus.Calculated;
    }

    public void Approve(int approverId, DateTimeOffset now)
    {
        ApprovedById = approverId;
        ApprovedAt = now;
        Status = FinalSettlementStatus.Approved;
    }

    public void MarkPaid(DateTimeOffset now)
    {
        if (Status != FinalSettlementStatus.Approved)
            throw new DomainException("Quyết toán phải được phê duyệt trước khi chi trả.", "ERR_SETTLEMENT_NOT_APPROVED");

        PaidAt = now;
        Status = FinalSettlementStatus.Paid;
    }
}