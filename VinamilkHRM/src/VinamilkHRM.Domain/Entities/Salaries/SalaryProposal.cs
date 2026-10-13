using System;
using System.Collections.Generic;
using System.Linq;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Salaries;

/// <summary>
/// Đại diện cho Tờ trình đề xuất điều chỉnh / nâng bậc lương nhân sự Vinamilk (SalaryProposal & SalaryAdjustment).
/// Hỗ trợ cả 2 luồng: Thường lệ (Merit, StepIncrease, Promotion, Market, MinimumWage) và Đặc cách (Special Fast-Track).
/// Tích hợp route phê duyệt đa cấp, cờ xung đột lợi ích (Conflict of Interest) và khả năng hồi tố truy lĩnh.
/// </summary>
public class SalaryProposal : BaseAuditableEntity
{
    public int EmployeeId { get; protected set; }
    public virtual Employee? Employee { get; protected set; }

    public int ProposedById { get; protected set; }
    public virtual Employee? ProposedBy { get; protected set; }

    public decimal CurrentSalary { get; protected set; }
    public decimal ProposedSalary { get; protected set; }
    public decimal CurrentAllowance { get; protected set; }
    public decimal ProposedAllowance { get; protected set; }
    public string Reason { get; protected set; } = string.Empty;
    public DateTime EffectiveDate { get; protected set; }
    public SalaryProposalStatus Status { get; protected set; } = SalaryProposalStatus.Draft;

    public int? DepartmentHeadApprovedById { get; protected set; }
    public string? DepartmentHeadNotes { get; protected set; }

    public int? HRDirectorApprovedById { get; protected set; }
    public string? HRDirectorNotes { get; protected set; }

    public DateTime? FinalDecisionDate { get; protected set; }

    // Mở rộng Total Rewards (Phiên 7)
    public int? SalaryReviewCycleId { get; protected set; }
    public virtual SalaryReviewCycle? SalaryReviewCycle { get; protected set; }

    public SalaryAdjustmentType AdjustmentType { get; protected set; } = SalaryAdjustmentType.Merit;
    public decimal IncreasePercentage { get; protected set; }
    public decimal CompaRatioBefore { get; protected set; }
    public decimal CompaRatioAfter { get; protected set; }
    public decimal? AnnualKpiScore { get; protected set; }
    public KpiGrade? AnnualKpiGrade { get; protected set; }
    public string? Evidence { get; protected set; }
    public SalaryFundSource FundSource { get; protected set; } = SalaryFundSource.CycleBudget;

    public int? OrganizationalUnitId { get; protected set; }
    public virtual OrganizationalUnit? OrganizationalUnit { get; protected set; }

    public bool IsConflictOfInterestFlagged { get; protected set; }
    public string? ConflictReason { get; protected set; }

    // Thuộc tính tiện ích
    public decimal CurrentP1 => CurrentSalary;
    public decimal ProposedP1 => ProposedSalary;
    public decimal AmountIncrease => ProposedSalary - CurrentSalary;

    private readonly List<SalaryAdjustmentApprovalStep> _approvalSteps = new();
    public virtual IReadOnlyCollection<SalaryAdjustmentApprovalStep> ApprovalSteps => _approvalSteps.AsReadOnly();

    protected SalaryProposal()
    {
    }

    public SalaryProposal(
        int employeeId,
        int proposedById,
        decimal currentSalary,
        decimal proposedSalary,
        decimal currentAllowance,
        decimal proposedAllowance,
        string reason,
        DateTime effectiveDate,
        SalaryAdjustmentType adjustmentType = SalaryAdjustmentType.Merit,
        decimal? compaRatioBefore = null,
        decimal? compaRatioAfter = null,
        decimal? annualKpiScore = null,
        KpiGrade? annualKpiGrade = null,
        string? evidence = null,
        SalaryFundSource fundSource = SalaryFundSource.CycleBudget,
        int? salaryReviewCycleId = null,
        int? organizationalUnitId = null)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân sự được đề xuất không hợp lệ.", "ERR_SAL_EMP_INVALID");

        if (proposedById <= 0)
            throw new DomainException("Mã người lập đề xuất không hợp lệ.", "ERR_SAL_PROPOSER_INVALID");

        if (proposedSalary <= 0)
            throw new DomainException("Mức lương mới đề xuất phải lớn hơn 0.", "ERR_SAL_PROPOSED_INVALID");

        if (proposedAllowance < 0)
            throw new DomainException("Phụ cấp đề xuất không thể âm.", "ERR_SAL_ALLOWANCE_NEGATIVE");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Lý do đề xuất tăng lương không được để trống.", "ERR_SAL_REASON_EMPTY");

        EmployeeId = employeeId;
        ProposedById = proposedById;
        CurrentSalary = currentSalary;
        ProposedSalary = proposedSalary;
        CurrentAllowance = currentAllowance;
        ProposedAllowance = proposedAllowance;
        Reason = reason.Trim();
        EffectiveDate = effectiveDate;
        Status = SalaryProposalStatus.Draft;

        AdjustmentType = adjustmentType;
        IncreasePercentage = currentSalary > 0
            ? Math.Round(((proposedSalary - currentSalary) / currentSalary) * 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;
        CompaRatioBefore = compaRatioBefore ?? 0m;
        CompaRatioAfter = compaRatioAfter ?? 0m;
        AnnualKpiScore = annualKpiScore;
        AnnualKpiGrade = annualKpiGrade;
        Evidence = evidence?.Trim();
        FundSource = fundSource;
        SalaryReviewCycleId = salaryReviewCycleId;
        OrganizationalUnitId = organizationalUnitId;
    }

    public void AddApprovalStep(int stepNumber, ApprovalRoleLevel roleLevel, string roleName, int? approverId = null)
    {
        var step = new SalaryAdjustmentApprovalStep(stepNumber, roleLevel, roleName, approverId);
        _approvalSteps.Add(step);
    }

    /// <summary>
    /// Cấu hình quy trình duyệt chuẩn theo loại tăng lương và cấp độ.
    /// Nếu đặc cách vượt trần 30% hoặc vượt Max của khung -> thêm bước Hội đồng quản trị.
    /// </summary>
    public void ConfigureRoute(bool requiresBoardOfDirectors, int? unitManagerId = null, int? divisionDirectorId = null, int? hrDirectorId = null, int? ceoId = null)
    {
        _approvalSteps.Clear();
        int step = 1;

        if (AdjustmentType == SalaryAdjustmentType.Merit)
        {
            AddApprovalStep(step++, ApprovalRoleLevel.UnitManager, "Trưởng đơn vị rà soát", unitManagerId);
            AddApprovalStep(step++, ApprovalRoleLevel.DivisionDirector, "Giám đốc khối thẩm định", divisionDirectorId);
            AddApprovalStep(step++, ApprovalRoleLevel.HRDirector, "Giám đốc Nhân sự & C&B", hrDirectorId);
            AddApprovalStep(step++, ApprovalRoleLevel.CEO, "Tổng Giám Đốc phê duyệt", ceoId);
        }
        else if (AdjustmentType == SalaryAdjustmentType.Special)
        {
            AddApprovalStep(step++, ApprovalRoleLevel.HRDirector, "Ban Nhân sự thẩm định quỹ đặc cách", hrDirectorId);
            AddApprovalStep(step++, ApprovalRoleLevel.CEO, "Tổng Giám Đốc phê duyệt đặc cách", ceoId);

            if (requiresBoardOfDirectors)
            {
                AddApprovalStep(step++, ApprovalRoleLevel.BoardOfDirectors, "Hội đồng quản trị phê duyệt vượt trần đặc cách");
            }
        }
        else
        {
            AddApprovalStep(step++, ApprovalRoleLevel.HRDirector, "Ban Nhân sự thẩm định", hrDirectorId);
            AddApprovalStep(step++, ApprovalRoleLevel.CEO, "Tổng Giám Đốc phê duyệt", ceoId);
        }
    }

    /// <summary>
    /// Xử lý duyệt từng bước kèm kiểm tra nghiêm ngặt Xung đột lợi ích (Conflict of Interest).
    /// </summary>
    public void ProcessStepApproval(
        int reviewerId,
        string? notes,
        IReadOnlyList<string>? reviewerRelativeCitizenIds = null,
        string? reviewerCitizenId = null)
    {
        // 1. Kiểm tra không cho người duyệt là chính người được tăng
        if (reviewerId == EmployeeId)
        {
            FlagConflict("Người duyệt là chính người được tăng lương.");
            throw new DomainException("Xung đột lợi ích: Người phê duyệt không thể là chính người được tăng lương.", "ERR_SAL_APPROVAL_CONFLICT_SELF");
        }

        // 2. Người khởi tạo đề xuất không được tự phê duyệt
        if (reviewerId == ProposedById)
        {
            FlagConflict("Người khởi tạo đề xuất không được tự phê duyệt.");
            throw new DomainException("Người khởi tạo đề xuất không được tự phê duyệt.", "ERR_SAL_PROPOSER_CANNOT_APPROVE");
        }

        // 3. Kiểm tra thân nhân đã khai báo
        if (Employee != null)
        {
            string empCitizenId = Employee.CitizenId ?? string.Empty;
            if (!string.IsNullOrEmpty(reviewerCitizenId) && !string.IsNullOrEmpty(empCitizenId) && reviewerCitizenId == empCitizenId)
            {
                FlagConflict("Người duyệt có trùng số định danh với nhân viên.");
                throw new DomainException("Xung đột lợi ích: Người phê duyệt là chính nhân sự hoặc trùng số định danh.", "ERR_SAL_APPROVAL_CONFLICT_SELF");
            }

            if (reviewerRelativeCitizenIds != null && !string.IsNullOrEmpty(empCitizenId) && reviewerRelativeCitizenIds.Contains(empCitizenId))
            {
                FlagConflict("Người duyệt có quan hệ thân nhân đã khai báo với nhân viên.");
                throw new DomainException("Xung đột lợi ích: Người phê duyệt có quan hệ thân nhân đã khai báo với người được tăng lương.", "ERR_SAL_APPROVAL_CONFLICT_RELATIVE");
            }
        }

        // 4. Tìm bước đang chờ duyệt
        var currentStep = _approvalSteps.OrderBy(s => s.StepNumber).FirstOrDefault(s => s.Status == ApprovalStepStatus.Pending);
        if (currentStep != null)
        {
            currentStep.Approve(reviewerId, notes);

            // Kiểm tra xem đã duyệt hết tất cả các bước chưa
            bool hasMorePending = _approvalSteps.Any(s => s.Status == ApprovalStepStatus.Pending);
            if (!hasMorePending)
            {
                FinalDecisionDate = DateTime.UtcNow;
                Status = SalaryProposalStatus.Approved;
                HRDirectorApprovedById = reviewerId;
                HRDirectorNotes = notes ?? "Đã hoàn thành toàn bộ các bước phê duyệt.";
            }
            else
            {
                // Cập nhật trạng thái trung gian
                if (currentStep.RoleLevel == ApprovalRoleLevel.UnitManager)
                {
                    Status = SalaryProposalStatus.PendingDepartmentHead;
                    DepartmentHeadApprovedById = reviewerId;
                    DepartmentHeadNotes = notes;
                }
                else
                {
                    Status = SalaryProposalStatus.PendingHRDirector;
                }
            }
        }
        else
        {
            // Trường hợp không cấu hình step riêng
            ApproveByHRDirector(reviewerId, notes ?? "Phê duyệt tờ trình tăng bậc lương.");
        }
    }

    public void FlagConflict(string reason)
    {
        IsConflictOfInterestFlagged = true;
        ConflictReason = reason.Trim();
    }

    public void ApproveByDepartmentHead(int managerId, string notes)
    {
        if (Status != SalaryProposalStatus.Draft && Status != SalaryProposalStatus.PendingDepartmentHead)
            throw new DomainException("Đề xuất không ở trạng thái chờ duyệt của Trưởng bộ phận.", "ERR_SAL_INVALID_STAGE");

        DepartmentHeadApprovedById = managerId;
        DepartmentHeadNotes = notes?.Trim() ?? "Đồng ý chuyển tiếp lên Giám đốc Nhân sự.";
        Status = SalaryProposalStatus.PendingHRDirector;
    }

    public void ApproveByHRDirector(int hrDirectorId, string notes)
    {
        HRDirectorApprovedById = hrDirectorId;
        HRDirectorNotes = notes?.Trim() ?? "Phê duyệt tờ trình tăng bậc lương.";
        FinalDecisionDate = DateTime.UtcNow;
        Status = SalaryProposalStatus.Approved;
    }

    public void Reject(int reviewerId, string rejectionReason)
    {
        if (Status == SalaryProposalStatus.Approved || Status == SalaryProposalStatus.Rejected)
            throw new DomainException("Đề xuất này đã được hoàn tất trước đó.", "ERR_SAL_ALREADY_FINALIZED");

        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new DomainException("Phải ghi rõ lý do từ chối tờ trình đề xuất lương.", "ERR_SAL_REJECT_REASON_EMPTY");

        var currentStep = _approvalSteps.OrderBy(s => s.StepNumber).FirstOrDefault(s => s.Status == ApprovalStepStatus.Pending);
        currentStep?.Reject(reviewerId, rejectionReason);

        HRDirectorNotes = rejectionReason.Trim();
        FinalDecisionDate = DateTime.UtcNow;
        Status = SalaryProposalStatus.Rejected;
    }

    public void FastTrackCeoOverride(int ceoId, string justification, string? overrideCode = null)
    {
        if (Status == SalaryProposalStatus.Approved)
            throw new DomainException("Đề xuất tăng lương này đã được phê duyệt trước đó.", "ERR_SAL_ALREADY_APPROVED");

        if (string.IsNullOrWhiteSpace(justification))
            throw new DomainException("Quyết định đặc cách của Tổng Giám đốc phải nêu rõ căn cứ và lý do.", "ERR_SAL_CEO_JUSTIFICATION_EMPTY");

        string code = string.IsNullOrWhiteSpace(overrideCode) ? $"CEO-DECISION-{DateTime.UtcNow:yyyyMMdd-HHmm}" : overrideCode.Trim();
        HRDirectorApprovedById = ceoId;
        HRDirectorNotes = $"[CEO FAST-TRACK OVERRIDE - Mã QĐ: {code}] {justification.Trim()}";
        FinalDecisionDate = DateTime.UtcNow;
        Status = SalaryProposalStatus.Approved;

        foreach (var s in _approvalSteps.Where(s => s.Status == ApprovalStepStatus.Pending))
        {
            s.Approve(ceoId, $"CEO Override: {justification}");
        }
    }
}