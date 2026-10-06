using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.LeaveRequests;

/// <summary>
/// Đại diện cho Đơn xin nghỉ phép của cán bộ nhân viên Vinamilk.
/// Hỗ trợ nghỉ phép năm trừ quỹ phép, nghỉ ốm đau chế độ BHXH 75% kèm xuất biểu mẫu C70a-HD.
/// </summary>
public class LeaveRequest : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public LeaveType LeaveType { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public decimal TotalDays { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public LeaveStatus Status { get; private set; } = LeaveStatus.Pending;

    public int? ApprovedById { get; private set; }
    public virtual Employee? ApprovedBy { get; private set; }

    public string? ApprovalNotes { get; private set; }
    public DateTime? DecisionDate { get; private set; }

    // Quản lý chế độ Bảo hiểm Xã hội (BHXH) khi nghỉ ốm đau / thai sản
    public bool IsSocialInsuranceCovered { get; private set; }
    public decimal SocialInsuranceBenefitRate { get; private set; } // vd: 0.75m (75%)
    public string? SocialInsuranceFormCode { get; private set; }   // vd: "C70a-HD"
    public string? SocialInsuranceFormContent { get; private set; }

    protected LeaveRequest()
    {
    }

    public LeaveRequest(
        int employeeId,
        LeaveType leaveType,
        DateTime startDate,
        DateTime endDate,
        decimal totalDays,
        string reason)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên làm đơn nghỉ phép không hợp lệ.", "ERR_LEAVE_EMP_INVALID");

        if (endDate < startDate)
            throw new DomainException("Ngày kết thúc nghỉ phép không thể trước ngày bắt đầu.", "ERR_LEAVE_DATES_INVALID");

        if (totalDays <= 0)
            throw new DomainException("Số ngày nghỉ phép phải lớn hơn 0.", "ERR_LEAVE_DAYS_INVALID");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Lý do xin nghỉ phép không được để trống.", "ERR_LEAVE_REASON_EMPTY");

        EmployeeId = employeeId;
        LeaveType = leaveType;
        StartDate = startDate.Date;
        EndDate = endDate.Date;
        TotalDays = totalDays;
        Reason = reason.Trim();
        Status = LeaveStatus.Pending;

        // Nếu là nghỉ ốm đau hoặc thai sản: tự động thiết lập chế độ BHXH
        if (leaveType == LeaveType.SickLeave)
        {
            IsSocialInsuranceCovered = true;
            SocialInsuranceBenefitRate = 0.75m; // 75% mức tiền lương đóng BHXH
            SocialInsuranceFormCode = "Mẫu C70a-HD";
        }
        else if (leaveType == LeaveType.MaternityLeave)
        {
            IsSocialInsuranceCovered = true;
            SocialInsuranceBenefitRate = 1.00m; // 100% chế độ thai sản
            SocialInsuranceFormCode = "Mẫu C70a-HD";
        }
        else
        {
            IsSocialInsuranceCovered = false;
            SocialInsuranceBenefitRate = 0m;
            SocialInsuranceFormCode = null;
        }
    }

    /// <summary>
    /// Cấp quản lý phê duyệt đơn nghỉ phép.
    /// </summary>
    public void Approve(int approverId, string? notes = null)
    {
        if (Status != LeaveStatus.Pending)
            throw new DomainException($"Đơn nghỉ phép này đã được xử lý trước đó với trạng thái: {Status}.", "ERR_LEAVE_ALREADY_PROCESSED");

        if (approverId <= 0)
            throw new DomainException("Mã người phê duyệt đơn không hợp lệ.", "ERR_LEAVE_APPROVER_INVALID");

        ApprovedById = approverId;
        ApprovalNotes = notes?.Trim() ?? "Đã chấp thuận đơn nghỉ phép.";
        DecisionDate = DateTime.UtcNow;
        Status = LeaveStatus.Approved;
    }

    /// <summary>
    /// Xuất biểu mẫu đề nghị thanh toán chế độ ốm đau BHXH (Mẫu C70a-HD).
    /// </summary>
    public void GenerateSocialInsuranceForm(string employeeName, string citizenId, string unitName, string hospitalCertificateNumber)
    {
        if (!IsSocialInsuranceCovered)
            throw new DomainException("Đơn xin nghỉ phép này không thuộc đối tượng giải quyết chế độ BHXH.", "ERR_LEAVE_NOT_BHXH");

        SocialInsuranceFormCode = "Mẫu C70a-HD";
        SocialInsuranceFormContent = 
$"""
DANH SÁCH ĐỀ NGHỊ GIẢI QUYẾT HƯỞNG CHẾ ĐỘ ỐM ĐAU, THAI SẢN (MẪU C70a-HD)
Cơ quan BHXH: Bảo hiểm Xã hội TP. Hồ Chí Minh
Đơn vị sử dụng lao động: {unitName}
Mã số đơn vị: 0300588569

1. Họ và tên người lao động: {employeeName}
2. Số CCCD / Số sổ BHXH: {citizenId}
3. Chế độ giải quyết: {LeaveType} (Mức trợ cấp: {SocialInsuranceBenefitRate * 100:0}%)
4. Thời gian nghỉ thực tế: Từ ngày {StartDate:dd/MM/yyyy} đến ngày {EndDate:dd/MM/yyyy} (Tổng số ngày: {TotalDays} ngày)
5. Chứng từ y tế / Giấy ra viện số: {hospitalCertificateNumber}
6. Tình trạng phê duyệt nội bộ: {Status} (Ngày duyệt: {DecisionDate:dd/MM/yyyy})
Căn cứ: Luật Bảo hiểm Xã hội số 58/2014/QH13 và Hướng dẫn thực hiện chế độ ốm đau Vinamilk.
""";
    }

    /// <summary>
    /// Cấp quản lý từ chối đơn nghỉ phép.
    /// </summary>
    public void Reject(int approverId, string rejectionReason)
    {
        if (Status != LeaveStatus.Pending)
            throw new DomainException($"Đơn nghỉ phép đã ở trạng thái {Status}, không thể từ chối.", "ERR_LEAVE_ALREADY_PROCESSED");

        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new DomainException("Phải nêu rõ lý do từ chối đơn nghỉ phép.", "ERR_LEAVE_REJECT_REASON_EMPTY");

        ApprovedById = approverId;
        ApprovalNotes = rejectionReason.Trim();
        DecisionDate = DateTime.UtcNow;
        Status = LeaveStatus.Rejected;
    }

    /// <summary>
    /// Nhân viên hủy đơn nghỉ phép khi chưa tới ngày nghỉ.
    /// </summary>
    public void Cancel(string cancelReason)
    {
        if (Status == LeaveStatus.Cancelled)
            throw new DomainException("Đơn nghỉ phép này đã được hủy trước đó.", "ERR_LEAVE_ALREADY_CANCELLED");

        Status = LeaveStatus.Cancelled;
        ApprovalNotes = $"[Người làm đơn đã hủy]: {cancelReason?.Trim()}";
    }
}
