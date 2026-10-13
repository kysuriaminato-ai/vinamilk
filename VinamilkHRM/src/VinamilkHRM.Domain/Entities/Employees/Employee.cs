using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Assets;
using VinamilkHRM.Domain.Entities.Attendances;
using VinamilkHRM.Domain.Entities.Contracts;
using VinamilkHRM.Domain.Entities.Jobs;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Entities.LeaveRequests;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Employees;

/// <summary>
/// Lớp cơ sở trừu tượng đại diện cho một Cán bộ / Nhân viên trong tập đoàn Vinamilk.
/// Hỗ trợ thông tin nhân thân, người phụ thuộc, tài khoản ngân hàng, học vấn, chứng chỉ,
/// quá trình công tác, tài liệu đính kèm và mã hóa dữ liệu nhạy cảm.
/// </summary>
public abstract class Employee : BaseAuditableEntity
{
    public string EmployeeCode { get; protected set; } = string.Empty;
    public string FirstName { get; protected set; } = string.Empty;
    public string LastName { get; protected set; } = string.Empty;
    public string FullName => $"{LastName} {FirstName}".Trim();
    public string CitizenId { get; protected set; } = string.Empty; // Mã hóa ASP.NET Data Protection / Always Encrypted
    public DateOnly? CitizenIdIssueDate { get; protected set; }
    public string? CitizenIdIssuePlace { get; protected set; }
    public string? TaxCode { get; protected set; }                   // Mã số thuế cá nhân

    public DateTime DateOfBirth { get; protected set; }
    public Gender Gender { get; protected set; }
    public string Email { get; protected set; } = string.Empty;
    public string PhoneNumber { get; protected set; } = string.Empty;
    public string Address { get; protected set; } = string.Empty;
    public DateTime HireDate { get; protected set; }
    public EmployeeType EmployeeType { get; protected set; }
    public bool IsActive { get; protected set; } = true;

    // Tài khoản ngân hàng nhận lương (mã hóa cột BankAccountNumber)
    public string? BankAccountNumber { get; protected set; }
    public string? BankName { get; protected set; }
    public string? BankBranch { get; protected set; }

    // Người liên hệ khẩn cấp
    public string? EmergencyContactName { get; protected set; }
    public string? EmergencyContactPhone { get; protected set; }
    public string? EmergencyContactRelationship { get; protected set; }

    // Chức danh và ngạch bậc lương tiêu chuẩn
    public int? StandardJobTitleId { get; protected set; }
    public virtual JobTitle? StandardJobTitle { get; protected set; }

    public int? JobGradeId { get; protected set; }
    public virtual JobGrade? JobGrade { get; protected set; }

    public int SalaryStep { get; protected set; } = 1;

    public int OrganizationalUnitId { get; protected set; }
    public virtual OrganizationalUnit? OrganizationalUnit { get; protected set; }

    public int? ManagerId { get; protected set; }
    public virtual Employee? Manager { get; protected set; }

    public decimal BaseSalary { get; protected set; }
    public decimal Allowance { get; protected set; }
    public decimal InsuranceSalary { get; protected set; }            // Mức lương cơ sở trích nộp BHXH/BHYT/BHTN
    public int DependentCount { get; protected set; } = 0;             // Số người phụ thuộc giảm trừ gia cảnh
    public decimal AnnualLeaveBalance { get; protected set; } = 12.0m; // Quỹ phép năm chuẩn
    public decimal DiligenceScore { get; protected set; } = 100.0m;   // Điểm chuyên cần (0 - 100)
    public string? LegacyId { get; protected set; }

    // Chế độ thai sản và nuôi con nhỏ (Điều 137 BLLĐ 2019)
    public DateOnly? ExpectedDueDate { get; protected set; }            // Ngày dự sinh (nữ mang thai)
    public DateOnly? YoungestChildBirthDate { get; protected set; }     // Ngày sinh của con nhỏ nhất

    // Navigation properties
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

    private readonly List<Dependent> _dependents = new();
    public virtual IReadOnlyCollection<Dependent> Dependents => _dependents.AsReadOnly();

    private readonly List<EmployeeEducation> _educations = new();
    public virtual IReadOnlyCollection<EmployeeEducation> Educations => _educations.AsReadOnly();

    private readonly List<EmployeeCertificate> _certificates = new();
    public virtual IReadOnlyCollection<EmployeeCertificate> Certificates => _certificates.AsReadOnly();

    private readonly List<EmployeeWorkHistory> _workHistories = new();
    public virtual IReadOnlyCollection<EmployeeWorkHistory> WorkHistories => _workHistories.AsReadOnly();

    private readonly List<EmployeeAttachment> _attachments = new();
    public virtual IReadOnlyCollection<EmployeeAttachment> Attachments => _attachments.AsReadOnly();

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
        InsuranceSalary = baseSalary > 0 ? Math.Round(baseSalary * 0.85m, 0) : 0m;
        DependentCount = 0;
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

    public void UpdateBankInfo(string? bankAccountNumber, string? bankName, string? bankBranch)
    {
        BankAccountNumber = bankAccountNumber?.Trim();
        BankName = bankName?.Trim();
        BankBranch = bankBranch?.Trim();
    }

    public void UpdateEmergencyContact(string? name, string? phone, string? relationship)
    {
        EmergencyContactName = name?.Trim();
        EmergencyContactPhone = phone?.Trim();
        EmergencyContactRelationship = relationship?.Trim();
    }

    public void UpdateCitizenDetails(DateOnly? issueDate, string? issuePlace, string? taxCode)
    {
        CitizenIdIssueDate = issueDate;
        CitizenIdIssuePlace = issuePlace?.Trim();
        TaxCode = taxCode?.Trim();
    }

    public void AssignStandardJob(int? jobTitleId, int? jobGradeId, int salaryStep = 1)
    {
        StandardJobTitleId = jobTitleId;
        JobGradeId = jobGradeId;
        SalaryStep = Math.Max(1, salaryStep);
    }

    public void SetLegacyId(string? legacyId)
    {
        LegacyId = legacyId?.Trim();
    }

    public void UpdateDependents(int count)
    {
        if (count < 0)
            throw new DomainException("Số người phụ thuộc không thể âm.", "ERR_EMP_DEPENDENTS_NEGATIVE");

        DependentCount = count;
    }

    public void AddDependent(Dependent dependent)
    {
        ArgumentNullException.ThrowIfNull(dependent);
        _dependents.Add(dependent);
        DependentCount = _dependents.Count(d => d.IsActive);
    }

    public void AddEducation(EmployeeEducation education)
    {
        ArgumentNullException.ThrowIfNull(education);
        _educations.Add(education);
    }

    public void AddCertificate(EmployeeCertificate certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        _certificates.Add(certificate);
    }

    public void AddWorkHistory(EmployeeWorkHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);
        _workHistories.Add(history);
    }

    public void AddAttachment(EmployeeAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        _attachments.Add(attachment);
    }

    public void SetInsuranceSalary(decimal insuranceSalary)
    {
        if (insuranceSalary < 0)
            throw new DomainException("Mức lương đóng bảo hiểm không thể âm.", "ERR_EMP_INSURANCE_NEGATIVE");

        InsuranceSalary = insuranceSalary;
    }

    public void SetMaternalStatus(DateOnly? expectedDueDate, DateOnly? youngestChildBirthDate)
    {
        ExpectedDueDate = expectedDueDate;
        YoungestChildBirthDate = youngestChildBirthDate;
    }

    /// <summary>
    /// Kiểm tra người lao động có thuộc diện miễn làm việc ban đêm và làm thêm giờ theo Điều 137 BLLĐ 2019
    /// (Nữ mang thai từ tháng thứ 7 hoặc đang nuôi con dưới 12 tháng).
    /// </summary>
    public bool IsExemptFromNightAndOt(DateOnly onDate)
    {
        if (Gender != Gender.Female)
            return false;

        // 1. Nữ mang thai từ tháng thứ 7 (khoảng 3 tháng trước ngày dự sinh)
        if (ExpectedDueDate.HasValue)
        {
            var seventhMonthStart = ExpectedDueDate.Value.AddMonths(-3);
            if (onDate >= seventhMonthStart && onDate <= ExpectedDueDate.Value)
                return true;
        }

        // 2. Nữ nuôi con dưới 12 tháng tuổi
        if (YoungestChildBirthDate.HasValue)
        {
            var childOneYearOld = YoungestChildBirthDate.Value.AddYears(1);
            if (onDate >= YoungestChildBirthDate.Value && onDate <= childOneYearOld)
                return true;
        }

        return false;
    }

    public void PromoteSalary(decimal newBaseSalary, decimal newAllowance, string reason, decimal? newInsuranceSalary = null)
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
        InsuranceSalary = newInsuranceSalary ?? Math.Round(newBaseSalary * 0.85m, 0);
    }

    public void UpdateContactInfo(string email, string phoneNumber, string address)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email liên hệ không được để trống.", "ERR_EMP_EMAIL_EMPTY");

        Email = email.Trim().ToLowerInvariant();
        PhoneNumber = phoneNumber?.Trim() ?? string.Empty;
        Address = address?.Trim() ?? string.Empty;
    }

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

    public void DeductAnnualLeave(decimal days)
    {
        if (days <= 0)
            throw new DomainException("Số ngày phép cần trừ phải lớn hơn 0.", "ERR_LEAVE_DEDUCT_DAYS_INVALID");

        if (AnnualLeaveBalance < days)
            throw new DomainException($"Quỹ phép năm không đủ để trừ (Còn lại: {AnnualLeaveBalance} ngày, yêu cầu trừ: {days} ngày).", "ERR_LEAVE_BALANCE_INSUFFICIENT");

        AnnualLeaveBalance -= days;
    }

    public void AddAnnualLeave(decimal days)
    {
        if (days <= 0)
            throw new DomainException("Số ngày phép cộng phải lớn hơn 0.", "ERR_LEAVE_ADD_DAYS_INVALID");

        AnnualLeaveBalance += days;
    }

    public void DeductDiligenceScore(decimal points)
    {
        if (points <= 0) return;
        DiligenceScore = Math.Max(0m, DiligenceScore - points);
    }

    public void ResetDiligenceScore()
    {
        DiligenceScore = 100.0m;
    }

    public virtual decimal CalculateOvertimePay(decimal hours, bool isHoliday)
    {
        if (hours <= 0)
            return 0m;

        const decimal standardMonthlyHours = 176m;
        decimal hourlyRate = BaseSalary / standardMonthlyHours;
        decimal multiplier = isHoliday ? 3.0m : 1.5m;

        return Math.Round(hours * hourlyRate * multiplier, 0, MidpointRounding.AwayFromZero);
    }
}
