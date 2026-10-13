using System;
using System.Collections.Generic;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Salaries;

/// <summary>
/// Chu kỳ xét tăng lương định kỳ tập đoàn: Năm, Ngày hiệu lực, Trạng thái.
/// Trạng thái: Draft -> Open -> UnitReview -> DivisionReview -> HRReview -> ExecutiveApproval -> Approved -> Applied.
/// </summary>
public class SalaryReviewCycle : BaseAuditableEntity
{
    public int Year { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateOnly EffectiveDate { get; private set; }
    public SalaryReviewCycleStatus Status { get; private set; } = SalaryReviewCycleStatus.Draft;

    // Tiêu chí cấu hình xét tăng
    public int MinTenureMonths { get; private set; } = 6;
    public bool ExcludeDisciplined { get; private set; } = true;
    public int ExcludeRecentPromotionMonths { get; private set; } = 6;

    private readonly List<SalaryBudget> _budgets = new();
    public virtual IReadOnlyCollection<SalaryBudget> Budgets => _budgets.AsReadOnly();

    private readonly List<SalaryProposal> _adjustments = new();
    public virtual IReadOnlyCollection<SalaryProposal> Adjustments => _adjustments.AsReadOnly();

    protected SalaryReviewCycle() { }

    public SalaryReviewCycle(
        int year,
        string name,
        DateOnly effectiveDate,
        int minTenureMonths = 6,
        bool excludeDisciplined = true,
        int excludeRecentPromotionMonths = 6)
    {
        if (year < 2020 || year > 2050)
            throw new DomainException("Năm xét tăng lương không hợp lệ.", "ERR_SAL_CYCLE_YEAR_INVALID");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tên chu kỳ xét tăng lương không được để trống.", "ERR_SAL_CYCLE_NAME_EMPTY");

        Year = year;
        Name = name.Trim();
        EffectiveDate = effectiveDate;
        MinTenureMonths = Math.Max(0, minTenureMonths);
        ExcludeDisciplined = excludeDisciplined;
        ExcludeRecentPromotionMonths = Math.Max(0, excludeRecentPromotionMonths);
        Status = SalaryReviewCycleStatus.Draft;
    }

    public void Open()
    {
        if (Status != SalaryReviewCycleStatus.Draft)
            throw new DomainException("Chỉ có thể mở chu kỳ từ trạng thái Dự thảo (Draft).", "ERR_SAL_CYCLE_INVALID_STAGE");

        Status = SalaryReviewCycleStatus.Open;
    }

    public void MoveToUnitReview()
    {
        if (Status != SalaryReviewCycleStatus.Open)
            throw new DomainException("Chu kỳ phải ở trạng thái Mở (Open) để chuyển sang đơn vị rà soát.", "ERR_SAL_CYCLE_INVALID_STAGE");

        Status = SalaryReviewCycleStatus.UnitReview;
    }

    public void MoveToDivisionReview()
    {
        if (Status != SalaryReviewCycleStatus.UnitReview)
            throw new DomainException("Chu kỳ phải hoàn thành rà soát đơn vị để chuyển Giám đốc khối.", "ERR_SAL_CYCLE_INVALID_STAGE");

        Status = SalaryReviewCycleStatus.DivisionReview;
    }

    public void MoveToHRReview()
    {
        if (Status != SalaryReviewCycleStatus.DivisionReview)
            throw new DomainException("Chu kỳ phải hoàn thành thẩm định khối để chuyển Ban Nhân sự.", "ERR_SAL_CYCLE_INVALID_STAGE");

        Status = SalaryReviewCycleStatus.HRReview;
    }

    public void MoveToExecutiveApproval()
    {
        if (Status != SalaryReviewCycleStatus.HRReview)
            throw new DomainException("Chu kỳ phải hoàn thành thẩm định Nhân sự để chuyển Tổng Giám Đốc.", "ERR_SAL_CYCLE_INVALID_STAGE");

        Status = SalaryReviewCycleStatus.ExecutiveApproval;
    }

    public void Approve(int approvedBy)
    {
        if (Status != SalaryReviewCycleStatus.ExecutiveApproval && Status != SalaryReviewCycleStatus.HRReview)
            throw new DomainException("Chu kỳ chưa sẵn sàng để phê duyệt chính thức.", "ERR_SAL_CYCLE_INVALID_STAGE");

        Status = SalaryReviewCycleStatus.Approved;
    }

    public void Apply(int appliedBy)
    {
        if (Status != SalaryReviewCycleStatus.Approved)
            throw new DomainException("Chỉ có thể áp dụng chu kỳ sau khi đã được phê duyệt chính thức.", "ERR_SAL_CYCLE_INVALID_STAGE");

        Status = SalaryReviewCycleStatus.Applied;
    }
}