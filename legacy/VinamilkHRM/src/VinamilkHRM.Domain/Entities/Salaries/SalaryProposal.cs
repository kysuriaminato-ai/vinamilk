using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Salaries;

/// <summary>
/// Đại diện cho Tờ trình đề xuất điều chỉnh / nâng bậc lương nhân sự Vinamilk.
/// </summary>
public class SalaryProposal : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public int ProposedById { get; private set; }
    public virtual Employee? ProposedBy { get; private set; }

    public decimal CurrentSalary { get; private set; }
    public decimal ProposedSalary { get; private set; }
    public decimal CurrentAllowance { get; private set; }
    public decimal ProposedAllowance { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public DateTime EffectiveDate { get; private set; }
    public SalaryProposalStatus Status { get; private set; } = SalaryProposalStatus.Draft;

    public int? DepartmentHeadApprovedById { get; private set; }
    public string? DepartmentHeadNotes { get; private set; }

    public int? HRDirectorApprovedById { get; private set; }
    public string? HRDirectorNotes { get; private set; }

    public DateTime? FinalDecisionDate { get; private set; }

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
        DateTime effectiveDate)
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
        Status = SalaryProposalStatus.PendingDepartmentHead;
    }

    public void ApproveByDepartmentHead(int managerId, string notes)
    {
        if (Status != SalaryProposalStatus.PendingDepartmentHead)
            throw new DomainException("Đề xuất không ở trạng thái chờ duyệt của Trưởng bộ phận.", "ERR_SAL_INVALID_STAGE");

        DepartmentHeadApprovedById = managerId;
        DepartmentHeadNotes = notes?.Trim() ?? "Đồng ý chuyển tiếp lên Giám đốc Nhân sự.";
        Status = SalaryProposalStatus.PendingHRDirector;
    }

    public void ApproveByHRDirector(int hrDirectorId, string notes)
    {
        if (Status != SalaryProposalStatus.PendingHRDirector)
            throw new DomainException("Đề xuất không ở trạng thái chờ Giám đốc Nhân sự duyệt.", "ERR_SAL_INVALID_STAGE");

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

        HRDirectorNotes = rejectionReason.Trim();
        FinalDecisionDate = DateTime.UtcNow;
        Status = SalaryProposalStatus.Rejected;
    }

    /// <summary>
    /// Giám đốc ĐẶC CÁCH tăng lương (CEO Fast-Track Executive Override).
    /// Bỏ qua toàn bộ quy trình thẩm định đa cấp, phê duyệt trực tiếp ngay lập tức.
    /// </summary>
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
    }
}
