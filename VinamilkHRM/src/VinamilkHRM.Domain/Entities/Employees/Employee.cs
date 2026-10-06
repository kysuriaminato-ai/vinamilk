using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Assets;
using VinamilkHRM.Domain.Entities.Attendances;
using VinamilkHRM.Domain.Entities.Contracts;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Entities.LeaveRequests;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Employees;

/// <summary>
/// Lớp cơ sở trừu tượng đại diện cho một Cán bộ / Nhân viên trong tập đoàn Vinamilk.
/// Áp dụng các nguyên lý OOP cốt lõi: Tính đóng gói (Encapsulation), Tính kế thừa (Inheritance),
/// và Tính đa hình (Polymorphism) qua phương thức tính lương ngoài giờ CalculateOvertimePay.
/// </summary>
public abstract class Employee : BaseAuditableEntity
{
    public string EmployeeCode { get; protected set; } = string.Empty;
    public string FirstName { get; protected set; } = string.Empty;
    public string LastName { get; protected set; } = string.Empty;
    public string FullName => $"{LastName} {FirstName}".Trim();
    public string CitizenId { get; protected set; } = string.Empty;
    public DateTime DateOfBirth { get; protected set; }
    public Gender Gender { get; protected set; }
    public string Email { get; protected set; } = string.Empty;
    public string PhoneNumber { get; protected set; } = string.Empty;
    public string Address { get; protected set; } = string.Empty;
    public DateTime HireDate { get; protected set; }
    public EmployeeType EmployeeType { get; protected set; }
    public bool IsActive { get; protected set; } = true;

    public int OrganizationalUnitId { get; protected set; }
    public virtual OrganizationalUnit? OrganizationalUnit { get; protected set; }

    public int? ManagerId { get; protected set; }
    public virtual Employee? Manager { get; protected set; }

    public decimal BaseSalary { get; protected set; }
    public decimal Allowance { get; protected set; }
    public decimal AnnualLeaveBalance { get; protected set; } = 12.0m; // Quỹ phép năm chuẩn theo Luật Lao động
    public decimal DiligenceScore { get; protected set; } = 100.0m;   // Điểm chuyên cần (Sàn 0 - Trần 100)

    // Quan hệ điều hướng (Navigation properties)
    private readonly List<Employee> _subordinates = new();
    public virtual IReadOnlyCollection<Employee> Subordinates => _subordinates.AsReadOnly();

    private readonly List<LaborContract> _laborContracts = new();
    public virtual IReadOnlyCollection<LaborContract> LaborContracts => _laborContracts.AsReadOnly();

    private readonly List<Attendance> _attendances = new();
    public virtual IReadOnlyCollection<Attendance> Attendances => _attendances.AsReadOnly();

    private readonly List<LeaveRequest> _leaveRequests = new();
    public virtual IReadOnlyCollection<LeaveRequest> LeaveRequests => _leaveRequests.AsReadOnly();

    private readonly List<KpiAssignment> _kpiAssignments = new();
    public virtual IReadOnlyCollection<KpiAssignment> KpiAssignments => _kpiAssignments.AsReadOnly();

    private readonly List<SalaryProposal> _salaryProposals = new();
    public virtual IReadOnlyCollection<SalaryProposal> SalaryProposals => _salaryProposals.AsReadOnly();

    private readonly List<PaySlip> _paySlips = new();
    public virtual IReadOnlyCollection<PaySlip> PaySlips => _paySlips.AsReadOnly();

    private readonly List<CompanyAsset> _assignedAssets = new();
    public virtual IReadOnlyCollection<CompanyAsset> AssignedAssets => _assignedAssets.AsReadOnly();

    protected Employee()
    {
    }

    protected Employee(
        string employeeCode,
        string firstName,
        string lastName,
        string citizenId,
        DateTime dateOfBirth,
        Gender gender,
        string email,
        string phoneNumber,
        string address,
        DateTime hireDate,
        EmployeeType employeeType,
        int organizationalUnitId,
        decimal baseSalary,
        decimal allowance,
        int? managerId = null)
    {
        ValidateCreationParameters(employeeCode, firstName, lastName, citizenId, dateOfBirth, baseSalary, allowance);

        EmployeeCode = employeeCode.Trim().ToUpperInvariant();
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        CitizenId = citizenId.Trim();
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Email = email?.Trim().ToLowerInvariant() ?? string.Empty;
        PhoneNumber = phoneNumber?.Trim() ?? string.Empty;
        Address = address?.Trim() ?? string.Empty;
        HireDate = hireDate;
        EmployeeType = employeeType;
        OrganizationalUnitId = organizationalUnitId;
        BaseSalary = baseSalary;
        Allowance = allowance;
        ManagerId = managerId;
        IsActive = true;
    }

    private static void ValidateCreationParameters(
        string code,
        string firstName,
        string lastName,
        string citizenId,
        DateTime dob,
        decimal baseSalary,
        decimal allowance)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Mã nhân viên Vinamilk không được để trống.", "ERR_EMP_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Họ và tên nhân viên không được để trống.", "ERR_EMP_NAME_EMPTY");

        if (string.IsNullOrWhiteSpace(citizenId) || citizenId.Trim().Length < 9)
            throw new DomainException("Số Căn cước công dân không hợp lệ (tối thiểu 9-12 chữ số).", "ERR_EMP_CITIZEN_ID_INVALID");

        if (dob >= DateTime.UtcNow.AddYears(-18))
            throw new DomainException("Nhân sự phải đủ 18 tuổi trở lên theo Luật Lao Động Việt Nam.", "ERR_EMP_UNDERAGE");

        if (baseSalary < 0)
            throw new DomainException("Mức lương cơ bản không thể là số âm.", "ERR_EMP_SALARY_NEGATIVE");

        if (allowance < 0)
            throw new DomainException("Mức phụ cấp không thể là số âm.", "ERR_EMP_ALLOWANCE_NEGATIVE");
    }

    /// <summary>
    /// Nghiệp vụ điều chỉnh hoặc nâng bậc lương cho nhân viên kèm theo lưu vết lý do.
    /// </summary>
    public void PromoteSalary(decimal newBaseSalary, decimal newAllowance, string reason)
    {
        if (!IsActive)
            throw new DomainException("Không thể điều chỉnh lương cho nhân sự đã nghỉ việc.", "ERR_EMP_INACTIVE");

        if (newBaseSalary <= 0)
            throw new DomainException("Mức lương cơ bản mới phải lớn hơn 0.", "ERR_EMP_SALARY_INVALID");

        if (newAllowance < 0)
            throw new DomainException("Mức phụ cấp mới không thể âm.", "ERR_EMP_ALLOWANCE_INVALID");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Phải nêu rõ lý do hoặc quyết định nâng lương.", "ERR_EMP_PROMOTE_REASON_EMPTY");

        BaseSalary = newBaseSalary;
        Allowance = newAllowance;
    }

    /// <summary>
    /// Cập nhật thông tin liên lạc cá nhân.
    /// </summary>
    public void UpdateContactInfo(string email, string phoneNumber, string address)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email liên hệ không được để trống.", "ERR_EMP_EMAIL_EMPTY");

        Email = email.Trim().ToLowerInvariant();
        PhoneNumber = phoneNumber?.Trim() ?? string.Empty;
        Address = address?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Điều chuyển nhân sự sang đơn vị tổ chức mới trong nội bộ tập đoàn Vinamilk.
    /// </summary>
    public void TransferUnit(int newOrganizationalUnitId, string reason)
    {
        if (newOrganizationalUnitId <= 0)
            throw new DomainException("Mã đơn vị tiếp nhận không hợp lệ.", "ERR_EMP_UNIT_INVALID");

        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Phải ghi rõ lý do/quyết định điều chuyển công tác.", "ERR_EMP_TRANSFER_REASON_EMPTY");

        OrganizationalUnitId = newOrganizationalUnitId;
    }

    public void AssignManager(int managerId)
    {
        if (managerId == Id && Id > 0)
            throw new DomainException("Nhân viên không thể tự làm quản lý trực tiếp của chính mình.", "ERR_EMP_SELF_MANAGER");

        ManagerId = managerId;
    }

    public void RemoveManager()
    {
        ManagerId = null;
    }

    public void Deactivate(string terminationReason)
    {
        if (string.IsNullOrWhiteSpace(terminationReason))
            throw new DomainException("Phải ghi rõ lý do chấm dứt công tác hoặc nghỉ việc.", "ERR_EMP_DEACTIVATE_REASON_EMPTY");

        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Tự động trừ Quỹ phép năm khi nhân viên được duyệt nghỉ phép năm.
    /// </summary>
    public void DeductAnnualLeave(decimal days)
    {
        if (days <= 0)
            throw new DomainException("Số ngày phép cần trừ phải lớn hơn 0.", "ERR_LEAVE_DEDUCT_DAYS_INVALID");

        if (AnnualLeaveBalance < days)
            throw new DomainException($"Quỹ phép năm không đủ để trừ (Còn lại: {AnnualLeaveBalance} ngày, yêu cầu trừ: {days} ngày).", "ERR_LEAVE_BALANCE_INSUFFICIENT");

        AnnualLeaveBalance -= days;
    }

    /// <summary>
    /// Cộng thêm ngày phép năm (vd: thâm niên hoặc hoàn trả đơn hủy).
    /// </summary>
    public void AddAnnualLeave(decimal days)
    {
        if (days <= 0)
            throw new DomainException("Số ngày phép cộng phải lớn hơn 0.", "ERR_LEAVE_ADD_DAYS_INVALID");

        AnnualLeaveBalance += days;
    }

    /// <summary>
    /// Trừ điểm chuyên cần khi nhân viên vi phạm chấm công / nghỉ không phép.
    /// </summary>
    public void DeductDiligenceScore(decimal points)
    {
        if (points <= 0) return;
        DiligenceScore = Math.Max(0m, DiligenceScore - points);
    }

    /// <summary>
    /// Đặt lại điểm chuyên cần đầu chu kỳ đánh giá (mặc định 100 điểm).
    /// </summary>
    public void ResetDiligenceScore()
    {
        DiligenceScore = 100.0m;
    }

    /// <summary>
    /// Tính mức lương làm thêm giờ (Overtime Pay) theo chế độ tiêu chuẩn Vinamilk.
    /// Áp dụng tính ĐA HÌNH (Polymorphism): Các lớp dẫn xuất sẽ override để áp dụng hệ số đặc thù.
    /// Định mức: 22 ngày công/tháng, 8 giờ/ngày = 176 giờ chuẩn.
    /// </summary>
    public virtual decimal CalculateOvertimePay(decimal hours, bool isHoliday)
    {
        if (hours <= 0)
            return 0m;

        const decimal standardMonthlyHours = 176m;
        decimal hourlyRate = BaseSalary / standardMonthlyHours;
        decimal multiplier = isHoliday ? 3.0m : 1.5m; // Ngày lễ 300%, ngày thường 150%

        return Math.Round(hours * hourlyRate * multiplier, 0, MidpointRounding.AwayFromZero);
    }
}
