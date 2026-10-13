using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Helpers;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Employees;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Interfaces.Repositories;

namespace VinamilkHRM.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IOrganizationalUnitRepository _unitRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationDbContext _context;
    private readonly IFieldEncryptionService _encryptionService;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IOrganizationalUnitRepository unitRepository,
        IUnitOfWork unitOfWork,
        IApplicationDbContext context,
        IFieldEncryptionService encryptionService)
    {
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _unitRepository = unitRepository ?? throw new ArgumentNullException(nameof(unitRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetWithDetailsAsync(id, cancellationToken);
        return employee == null ? null : MapToDto(employee);
    }

    public async Task<EmployeeDto?> GetEmployeeByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByCodeAsync(code, cancellationToken);
        return employee == null ? null : MapToDto(employee);
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetAllEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _employeeRepository.GetAllAsync(cancellationToken);
        return employees.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetEmployeesByUnitAsync(int unitId, CancellationToken cancellationToken = default)
    {
        var employees = await _employeeRepository.GetByUnitIdAsync(unitId, cancellationToken);
        return employees.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<VinamilkHRM.Application.DTOs.OrganizationalUnits.OrganizationalUnitDto>> GetAllUnitsAsync(CancellationToken cancellationToken = default)
    {
        var units = await _unitRepository.GetAllAsync(cancellationToken);
        return units.Select(u => new VinamilkHRM.Application.DTOs.OrganizationalUnits.OrganizationalUnitDto(
            u.Id,
            u.UnitCode,
            u.UnitName,
            u.UnitType,
            u.Address,
            u.PhoneNumber,
            u.Email,
            u.TaxCode,
            u.ParentUnitId,
            u.ParentUnit?.UnitName,
            u.ManagerEmployeeId,
            u.ManagerEmployee?.FullName,
            u.Employees.Count
        )).ToList();
    }

    public async Task<decimal> CalculateOvertimePayAsync(int employeeId, decimal hours, bool isHoliday, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(employeeId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy nhân viên với Id={employeeId}.", "ERR_EMP_NOT_FOUND");

        return employee.CalculateOvertimePay(hours, isHoliday);
    }

    public async Task<EmployeeDetailDto?> GetEmployeeDetailByIdAsync(int id, bool canViewSensitive = false, CancellationToken cancellationToken = default)
    {
        var emp = await _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationalUnit)
            .Include(e => e.Manager)
            .Include(e => e.StandardJobTitle)
            .Include(e => e.Dependents)
            .Include(e => e.Educations)
            .Include(e => e.Certificates)
            .Include(e => e.WorkHistories)
            .Include(e => e.Attachments)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (emp == null) return null;

        return MapToDetailDto(emp, canViewSensitive);
    }

    public async Task<IReadOnlyList<EmployeeDetailDto>> GetAllEmployeeDetailsAsync(bool canViewSensitive = false, CancellationToken cancellationToken = default)
    {
        var employees = await _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationalUnit)
            .Include(e => e.Manager)
            .Include(e => e.StandardJobTitle)
            .Include(e => e.Dependents)
            .Include(e => e.Educations)
            .Include(e => e.Certificates)
            .Include(e => e.WorkHistories)
            .Include(e => e.Attachments)
            .OrderBy(e => e.EmployeeCode)
            .ToListAsync(cancellationToken);

        return employees.Select(e => MapToDetailDto(e, canViewSensitive)).ToList();
    }

    public async Task<DependentDto> AddDependentAsync(int employeeId, CreateDependentRequest request, CancellationToken cancellationToken = default)
    {
        var emp = await _context.Employees
            .Include(e => e.Dependents)
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);

        if (emp == null)
            throw new DomainException($"Không tìm thấy nhân viên với ID {employeeId}.", "ERR_EMP_NOT_FOUND");

        var dependent = new Dependent(
            employeeId: emp.Id,
            fullName: request.FullName,
            relationship: request.Relationship,
            fromMonth: request.FromMonth,
            taxCode: request.TaxCode,
            citizenId: request.CitizenId,
            dateOfBirth: request.DateOfBirth,
            toMonth: request.ToMonth,
            note: request.Note
        );

        emp.AddDependent(dependent);
        await _context.SaveChangesAsync(cancellationToken);

        return new DependentDto(
            dependent.Id,
            dependent.EmployeeId,
            dependent.FullName,
            dependent.Relationship,
            dependent.DateOfBirth,
            dependent.TaxCode,
            dependent.CitizenId,
            dependent.FromMonth,
            dependent.ToMonth,
            dependent.IsActive,
            dependent.Note
        );
    }

    public async Task<EmployeeWorkHistoryDto> AddWorkHistoryAsync(int employeeId, CreateWorkHistoryRequest request, CancellationToken cancellationToken = default)
    {
        var emp = await _context.Employees
            .Include(e => e.WorkHistories)
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);

        if (emp == null)
            throw new DomainException($"Không tìm thấy nhân viên với ID {employeeId}.", "ERR_EMP_NOT_FOUND");

        var wh = new EmployeeWorkHistory(
            employeeId: emp.Id,
            fromDate: request.FromDate,
            companyName: request.CompanyName,
            toDate: request.ToDate,
            jobTitle: request.JobTitle,
            mainResponsibilities: request.MainResponsibilities,
            referenceContact: request.ReferenceContact,
            note: request.Note
        );

        emp.AddWorkHistory(wh);
        await _context.SaveChangesAsync(cancellationToken);

        return new EmployeeWorkHistoryDto(
            wh.Id,
            wh.EmployeeId,
            wh.FromDate,
            wh.ToDate,
            wh.CompanyName,
            wh.JobTitle,
            wh.MainResponsibilities,
            wh.ReferenceContact,
            wh.Note
        );
    }

    public async Task<EmployeeEducationDto> AddEducationAsync(int employeeId, CreateEducationRequest request, CancellationToken cancellationToken = default)
    {
        var emp = await _context.Employees
            .Include(e => e.Educations)
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);

        if (emp == null)
            throw new DomainException($"Không tìm thấy nhân viên với ID {employeeId}.", "ERR_EMP_NOT_FOUND");

        var edu = new EmployeeEducation(
            employeeId: emp.Id,
            degreeLevel: request.DegreeLevel,
            major: request.Major,
            schoolName: request.SchoolName,
            graduationYear: request.GraduationYear,
            classification: request.Classification,
            educationType: request.EducationType
        );

        emp.AddEducation(edu);
        await _context.SaveChangesAsync(cancellationToken);

        return new EmployeeEducationDto(
            edu.Id,
            edu.EmployeeId,
            edu.DegreeLevel,
            edu.Major,
            edu.SchoolName,
            edu.GraduationYear,
            edu.Classification,
            edu.EducationType
        );
    }

    public async Task<EmployeeCertificateDto> AddCertificateAsync(int employeeId, CreateCertificateRequest request, CancellationToken cancellationToken = default)
    {
        var emp = await _context.Employees
            .Include(e => e.Certificates)
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);

        if (emp == null)
            throw new DomainException($"Không tìm thấy nhân viên với ID {employeeId}.", "ERR_EMP_NOT_FOUND");

        var cert = new EmployeeCertificate(
            employeeId: emp.Id,
            certificateName: request.CertificateName,
            issuingAuthority: request.IssuingAuthority,
            issueDate: request.IssueDate,
            expiryDate: request.ExpiryDate,
            certificateCode: request.CertificateCode,
            scoreOrGrade: request.ScoreOrGrade
        );

        emp.AddCertificate(cert);
        await _context.SaveChangesAsync(cancellationToken);

        return new EmployeeCertificateDto(
            cert.Id,
            cert.EmployeeId,
            cert.CertificateName,
            cert.IssuingAuthority,
            cert.IssueDate,
            cert.ExpiryDate,
            cert.CertificateCode,
            cert.ScoreOrGrade
        );
    }

    public async Task<EmployeeAttachmentDto> AddAttachmentAsync(int employeeId, string documentType, string fileName, string filePath, long fileSizeBytes, string checksumSha256, CancellationToken cancellationToken = default)
    {
        var emp = await _context.Employees
            .Include(e => e.Attachments)
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);

        if (emp == null)
            throw new DomainException($"Không tìm thấy nhân viên với ID {employeeId}.", "ERR_EMP_NOT_FOUND");

        var att = new EmployeeAttachment(
            employeeId: emp.Id,
            fileName: fileName,
            documentType: documentType,
            filePath: filePath,
            fileSizeBytes: fileSizeBytes,
            checksumSha256: checksumSha256
        );

        emp.AddAttachment(att);
        await _context.SaveChangesAsync(cancellationToken);

        return new EmployeeAttachmentDto(
            att.Id,
            att.EmployeeId,
            att.FileName,
            att.DocumentType,
            att.FilePath,
            att.FileSizeBytes,
            att.ChecksumSha256,
            att.UploadedAt
        );
    }

    private EmployeeDetailDto MapToDetailDto(Employee e, bool canViewSensitive)
    {
        string rawCccd = !string.IsNullOrEmpty(e.CitizenId) ? (_encryptionService.Decrypt(e.CitizenId) ?? string.Empty) : string.Empty;
        string? rawBank = !string.IsNullOrEmpty(e.BankAccountNumber) ? _encryptionService.Decrypt(e.BankAccountNumber) : null;

        string maskedCccd = SensitiveDataMasker.Mask(rawCccd);
        string? maskedBank = SensitiveDataMasker.Mask(rawBank);

        return new EmployeeDetailDto(
            Id: e.Id,
            EmployeeCode: e.EmployeeCode,
            FirstName: e.FirstName,
            LastName: e.LastName,
            FullName: e.FullName,
            MaskedCitizenId: maskedCccd,
            RawCitizenId: canViewSensitive ? rawCccd : null,
            DateOfBirth: e.DateOfBirth,
            Gender: e.Gender,
            Email: e.Email,
            PhoneNumber: e.PhoneNumber,
            Address: e.Address,
            HireDate: e.HireDate,
            EmployeeType: e.EmployeeType,
            OrganizationalUnitId: e.OrganizationalUnitId,
            UnitName: e.OrganizationalUnit?.UnitName,
            CostCenterCode: e.OrganizationalUnit?.CostCenterCode,
            MinimumWageRegion: e.OrganizationalUnit?.MinimumWageRegion ?? Domain.Enums.MinimumWageRegion.RegionI,
            BaseSalary: e.BaseSalary,
            Allowance: e.Allowance,
            IsActive: e.IsActive,
            MaskedBankAccountNumber: maskedBank,
            RawBankAccountNumber: canViewSensitive ? rawBank : null,
            BankName: e.BankName,
            BankBranch: e.BankBranch,
            TaxCode: e.TaxCode,
            EmergencyContactName: e.EmergencyContactName,
            EmergencyContactPhone: e.EmergencyContactPhone,
            EmergencyContactRelationship: e.EmergencyContactRelationship,
            ManagerId: e.ManagerId,
            ManagerName: e.Manager?.FullName,
            StandardJobTitleId: e.StandardJobTitleId,
            StandardJobTitleName: e.StandardJobTitle?.JobTitleName,
            JobGradeId: e.JobGradeId,
            SalaryStep: e.SalaryStep,
            Dependents: e.Dependents.Select(d => new DependentDto(
                d.Id, d.EmployeeId, d.FullName, d.Relationship, d.DateOfBirth, d.TaxCode, d.CitizenId, d.FromMonth, d.ToMonth, d.IsActive, d.Note
            )).ToList(),
            Educations: e.Educations.Select(ed => new EmployeeEducationDto(
                ed.Id, ed.EmployeeId, ed.DegreeLevel, ed.Major, ed.SchoolName, ed.GraduationYear, ed.Classification, ed.EducationType
            )).ToList(),
            Certificates: e.Certificates.Select(c => new EmployeeCertificateDto(
                c.Id, c.EmployeeId, c.CertificateName, c.IssuingAuthority, c.IssueDate, c.ExpiryDate, c.CertificateCode, c.ScoreOrGrade
            )).ToList(),
            WorkHistories: e.WorkHistories.Select(w => new EmployeeWorkHistoryDto(
                w.Id, w.EmployeeId, w.FromDate, w.ToDate, w.CompanyName, w.JobTitle, w.MainResponsibilities, w.ReferenceContact, w.Note
            )).ToList(),
            Attachments: e.Attachments.Select(a => new EmployeeAttachmentDto(
                a.Id, a.EmployeeId, a.FileName, a.DocumentType, a.FilePath, a.FileSizeBytes, a.ChecksumSha256, a.UploadedAt
            )).ToList()
        );
    }

    private static EmployeeDto MapToDto(Employee e)
    {
        string roleInfo = e switch
        {
            OfficeEmployee off => $"Chức danh: {off.JobTitle} | Bàn: {off.CubicleNumber}",
            FactoryWorker fac => $"Tổ máy: {fac.ProductionShiftGroup} | Chuẩn AT: {fac.SafetyLevel}",
            FarmSpecialist farm => $"Chuyên môn: {farm.SpecializationArea} | Mã AT sinh học: {farm.BioSecurityClearanceCode}",
            StoreAssociate store => $"POS: {store.CashRegisterId} | Điểm DV: {store.CustomerServiceRating}/5★",
            _ => "Nhân viên tiêu chuẩn"
        };

        return new EmployeeDto(
            e.Id,
            e.EmployeeCode,
            e.FirstName,
            e.LastName,
            e.FullName,
            e.CitizenId,
            e.DateOfBirth,
            e.Gender,
            e.Email,
            e.PhoneNumber,
            e.Address,
            e.HireDate,
            e.EmployeeType,
            e.OrganizationalUnitId,
            e.OrganizationalUnit?.UnitName,
            e.BaseSalary,
            e.Allowance,
            e.IsActive,
            e.ManagerId,
            e.Manager?.FullName,
            roleInfo);
    }
}
