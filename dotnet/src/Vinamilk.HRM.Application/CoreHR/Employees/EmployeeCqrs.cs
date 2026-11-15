using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.CoreHR.Lifecycle;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Domain.Entities.Talent;

namespace Vinamilk.HRM.Application.CoreHR.Employees;

public sealed record EmployeeListResultDto(
    IReadOnlyList<Employee> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    IReadOnlyList<OrganizationUnit> Units,
    IReadOnlyList<Position> Positions,
    IReadOnlyList<SalaryStep> SalarySteps,
    IReadOnlyList<EducationLevel> EducationLevels);

/* ----------------------------------------------------------------------------
 * 1. QUERY: Tìm kiếm nâng cao & Lọc đa điều kiện Danh sách Nhân viên
 * ---------------------------------------------------------------------------- */
public sealed record GetEmployeesAdvancedQuery(
    string? Keyword = null,
    int? OrganizationUnitId = null,
    int? PositionId = null,
    string? WorkStatus = null,
    string? ContractType = null,
    string? Gender = null,
    decimal? MinSalary = null,
    decimal? MaxSalary = null,
    string SortBy = "EmployeeCode",
    int PageNumber = 1,
    int PageSize = 20) : IRequest<EmployeeListResultDto>;

public sealed class GetEmployeesAdvancedQueryHandler
    : IRequestHandler<GetEmployeesAdvancedQuery, EmployeeListResultDto>
{
    private readonly IApplicationDbContext _context;

    public GetEmployeesAdvancedQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeListResultDto> Handle(
        GetEmployeesAdvancedQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Include(e => e.Position)
            .Include(e => e.EducationLevel)
            .Include(e => e.SalaryStep)
                .ThenInclude(st => st!.SalaryScale)
            .Include(e => e.Dependents)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim();
            query = query.Where(e =>
                e.EmployeeCode.Contains(kw) ||
                e.FullName.Contains(kw) ||
                (e.WorkEmail != null && e.WorkEmail.Contains(kw)) ||
                (e.PhoneNumber != null && e.PhoneNumber.Contains(kw)) ||
                (e.Major != null && e.Major.Contains(kw)));
        }

        if (request.OrganizationUnitId.HasValue && request.OrganizationUnitId.Value > 0)
        {
            var selectedUnit = await _context.OrganizationUnits
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.OrganizationUnitId.Value, cancellationToken);

            if (selectedUnit is not null && !string.IsNullOrWhiteSpace(selectedUnit.TreePath))
            {
                var prefix = selectedUnit.TreePath;
                query = query.Where(e =>
                    e.OrganizationUnitId == selectedUnit.Id ||
                    (e.OrganizationUnit.TreePath != null && e.OrganizationUnit.TreePath.StartsWith(prefix)));
            }
            else
            {
                query = query.Where(e => e.OrganizationUnitId == request.OrganizationUnitId.Value);
            }
        }

        if (request.PositionId.HasValue && request.PositionId.Value > 0)
        {
            query = query.Where(e => e.PositionId == request.PositionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.WorkStatus))
        {
            query = query.Where(e => e.WorkStatus == request.WorkStatus);
        }

        if (!string.IsNullOrWhiteSpace(request.ContractType))
        {
            query = query.Where(e => e.CurrentContractType == request.ContractType);
        }

        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            query = query.Where(e => e.Gender == request.Gender);
        }

        if (request.MinSalary.HasValue)
        {
            query = query.Where(e => e.BaseSalary >= request.MinSalary.Value);
        }

        if (request.MaxSalary.HasValue)
        {
            query = query.Where(e => e.BaseSalary <= request.MaxSalary.Value);
        }

        query = request.SortBy switch
        {
            "FullName" => query.OrderBy(e => e.FullName),
            "SalaryDesc" => query.OrderByDescending(e => e.BaseSalary),
            "HireDateDesc" => query.OrderByDescending(e => e.HireDate),
            _ => query.OrderBy(e => e.EmployeeCode)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.PageNumber);
        var size = Math.Clamp(request.PageSize, 5, 200);

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        var units = await _context.OrganizationUnits
            .AsNoTracking()
            .OrderBy(u => u.UnitLevel)
            .ThenBy(u => u.SortOrder)
            .ToListAsync(cancellationToken);

        var positions = await _context.Positions
            .AsNoTracking()
            .OrderBy(p => p.PositionLevel)
            .ToListAsync(cancellationToken);

        var steps = await _context.SalarySteps
            .AsNoTracking()
            .Include(s => s.SalaryScale)
            .OrderBy(s => s.SalaryScale.ScaleCode)
            .ThenBy(s => s.StepOrder)
            .ToListAsync(cancellationToken);

        var eduLevels = await _context.EducationLevels
            .AsNoTracking()
            .OrderBy(l => l.RankOrder)
            .ToListAsync(cancellationToken);

        return new EmployeeListResultDto(items, totalCount, page, size, units, positions, steps, eduLevels);
    }
}

/* ----------------------------------------------------------------------------
 * 2. QUERY: Hồ sơ Nhân viên 360° (Đủ 11 Tab Nghiệp vụ)
 * ---------------------------------------------------------------------------- */
public sealed record Employee360ProfileDto(
    Employee Employee,
    string DecryptedCitizenId,
    string DecryptedBankAccount,
    string DecryptedTaxCode,
    RetirementCalculationResult RetirementInfo,
    IReadOnlyList<TrainingEnrollment> TrainingEnrollments,
    IReadOnlyList<AuditLog> ChangeHistoryLogs,
    IReadOnlyList<OrganizationUnit> Units,
    IReadOnlyList<Position> Positions,
    IReadOnlyList<SalaryStep> SalarySteps,
    IReadOnlyList<EducationLevel> EducationLevels);

public sealed record GetEmployee360ProfileQuery(int EmployeeId) : IRequest<Employee360ProfileDto?>;

public sealed class GetEmployee360ProfileQueryHandler
    : IRequestHandler<GetEmployee360ProfileQuery, Employee360ProfileDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IPiiEncryptionService _encryptionService;

    public GetEmployee360ProfileQueryHandler(
        IApplicationDbContext context,
        IPiiEncryptionService encryptionService)
    {
        _context = context;
        _encryptionService = encryptionService;
    }

    public async Task<Employee360ProfileDto?> Handle(
        GetEmployee360ProfileQuery request,
        CancellationToken cancellationToken)
    {
        var emp = await _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Include(e => e.Position)
            .Include(e => e.DirectManager)
            .Include(e => e.EducationLevel)
            .Include(e => e.SalaryStep)
                .ThenInclude(st => st!.SalaryScale)
            .Include(e => e.Dependents)
            .Include(e => e.Certificates)
            .Include(e => e.Documents)
            .Include(e => e.Contracts)
                .ThenInclude(c => c.Addendums)
            .Include(e => e.EmploymentHistories.OrderByDescending(h => h.FromDate))
            .Include(e => e.Transfers)
                .ThenInclude(t => t.FromUnit)
            .Include(e => e.Transfers)
                .ThenInclude(t => t.ToUnit)
            .Include(e => e.SalaryProgressions.OrderByDescending(sp => sp.EffectiveDate))
            .Include(e => e.RewardDisciplines.OrderByDescending(r => r.DecisionDate))
                .ThenInclude(r => r.Category)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (emp is null)
            return null;

        var trainings = await _context.TrainingEnrollments
            .AsNoTracking()
            .Include(te => te.TrainingClass)
                .ThenInclude(tc => tc.TrainingCourse)
            .Where(te => te.EmployeeId == emp.Id)
            .OrderByDescending(te => te.EnrolledDate)
            .ToListAsync(cancellationToken);

        var idStr = emp.Id.ToString();
        var codeStr = emp.EmployeeCode;
        var auditLogs = await _context.AuditLogs
            .AsNoTracking()
            .Where(a =>
                (a.EntityName == "Employee" && (a.EntityId == idStr || a.EntityId == codeStr)) ||
                (a.NewValuesJson != null && a.NewValuesJson.Contains(codeStr)))
            .OrderByDescending(a => a.CreatedAt)
            .Take(25)
            .ToListAsync(cancellationToken);

        var units = await _context.OrganizationUnits.AsNoTracking().OrderBy(u => u.UnitLevel).ThenBy(u => u.SortOrder).ToListAsync(cancellationToken);
        var positions = await _context.Positions.AsNoTracking().OrderBy(p => p.PositionLevel).ToListAsync(cancellationToken);
        var steps = await _context.SalarySteps.AsNoTracking().Include(s => s.SalaryScale).OrderBy(s => s.SalaryScale.ScaleCode).ThenBy(s => s.StepOrder).ToListAsync(cancellationToken);
        var eduLevels = await _context.EducationLevels.AsNoTracking().OrderBy(l => l.RankOrder).ToListAsync(cancellationToken);

        var retirement = VietnamRetirementCalculator.Calculate(emp.DateOfBirth, emp.Gender);

        return new Employee360ProfileDto(
            emp,
            _encryptionService.Decrypt(emp.CitizenIdEncrypted),
            _encryptionService.Decrypt(emp.BankAccountEncrypted),
            _encryptionService.Decrypt(emp.TaxCodeEncrypted),
            retirement,
            trainings,
            auditLogs,
            units,
            positions,
            steps,
            eduLevels);
    }
}

/* ----------------------------------------------------------------------------
 * 3. COMMAND & VALIDATOR: Thêm mới / Cập nhật Hồ sơ Nhân viên
 * ---------------------------------------------------------------------------- */
public sealed record SaveEmployeeCommand(
    int? Id,
    string EmployeeCode,
    string FullName,
    string Gender,
    DateOnly DateOfBirth,
    string CitizenId,
    string? PhoneNumber,
    string? WorkEmail,
    string? PersonalEmail,
    string? PermanentAddress,
    string? CurrentAddress,
    int OrganizationUnitId,
    int PositionId,
    int? EducationLevelId,
    string? Major,
    int? SalaryStepId,
    decimal BaseSalary,
    decimal PositionAllowance,
    decimal OtherAllowance,
    string? BankAccount,
    string? BankName,
    string? TaxCode,
    string? SocialInsuranceNumber,
    DateOnly HireDate,
    string WorkStatus,
    string CurrentContractType) : IRequest<int>;

public sealed class SaveEmployeeCommandValidator : AbstractValidator<SaveEmployeeCommand>
{
    public SaveEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeCode)
            .NotEmpty().WithMessage("Mã nhân viên không được để trống.")
            .MaximumLength(20).WithMessage("Mã nhân viên tối đa 20 ký tự.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ và tên nhân viên không được để trống.")
            .MaximumLength(150);

        RuleFor(x => x.BaseSalary)
            .GreaterThan(0).WithMessage("Mức lương cơ bản phải lớn hơn 0 VNĐ.");
    }
}

public sealed class SaveEmployeeCommandHandler : IRequestHandler<SaveEmployeeCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IPiiEncryptionService _encryptionService;

    public SaveEmployeeCommandHandler(
        IApplicationDbContext context,
        IPiiEncryptionService encryptionService)
    {
        _context = context;
        _encryptionService = encryptionService;
    }

    public async Task<int> Handle(SaveEmployeeCommand request, CancellationToken cancellationToken)
    {
        Employee emp;
        var isNew = !request.Id.HasValue || request.Id.Value <= 0;

        if (!isNew)
        {
            emp = await _context.Employees.FirstAsync(e => e.Id == request.Id!.Value, cancellationToken);
        }
        else
        {
            emp = new Employee();
            await _context.Employees.AddAsync(emp, cancellationToken);
        }

        emp.EmployeeCode = request.EmployeeCode.Trim().ToUpperInvariant();
        emp.FullName = request.FullName.Trim();
        emp.Gender = request.Gender;
        emp.DateOfBirth = request.DateOfBirth;

        if (!string.IsNullOrWhiteSpace(request.CitizenId))
        {
            emp.CitizenIdEncrypted = _encryptionService.Encrypt(request.CitizenId);
            emp.CitizenIdHash = _encryptionService.ComputeDeterministicHash(request.CitizenId);
        }

        emp.PhoneNumber = request.PhoneNumber?.Trim();
        emp.WorkEmail = request.WorkEmail?.Trim();
        emp.PersonalEmail = request.PersonalEmail?.Trim();
        emp.PermanentAddress = request.PermanentAddress?.Trim();
        emp.CurrentAddress = request.CurrentAddress?.Trim();
        emp.OrganizationUnitId = request.OrganizationUnitId;
        emp.PositionId = request.PositionId;
        emp.EducationLevelId = request.EducationLevelId;
        emp.Major = request.Major?.Trim();
        emp.SalaryStepId = request.SalaryStepId;
        emp.BaseSalary = request.BaseSalary;
        emp.PositionAllowance = request.PositionAllowance;
        emp.OtherAllowance = request.OtherAllowance;

        if (!string.IsNullOrWhiteSpace(request.BankAccount))
            emp.BankAccountEncrypted = _encryptionService.Encrypt(request.BankAccount);

        emp.BankName = request.BankName?.Trim();

        if (!string.IsNullOrWhiteSpace(request.TaxCode))
            emp.TaxCodeEncrypted = _encryptionService.Encrypt(request.TaxCode);

        emp.SocialInsuranceNumber = request.SocialInsuranceNumber?.Trim();
        emp.HireDate = request.HireDate;
        emp.WorkStatus = request.WorkStatus;
        emp.CurrentContractType = request.CurrentContractType;

        await _context.SaveChangesAsync(cancellationToken);

        if (isNew)
        {
            // Tự động khởi tạo Hợp đồng lao động đầu tiên & Lịch sử công tác
            await _context.LaborContracts.AddAsync(new LaborContract
            {
                EmployeeId = emp.Id,
                ContractNumber = $"HD-{emp.EmployeeCode}/{DateTime.Now.Year}/VNM",
                ContractType = emp.CurrentContractType,
                SignDate = emp.HireDate,
                StartDate = emp.HireDate,
                EndDate = emp.CurrentContractType == "Indefinite" ? null : emp.HireDate.AddMonths(12),
                SalaryStepId = emp.SalaryStepId,
                BaseSalary = emp.BaseSalary,
                PositionAllowance = emp.PositionAllowance,
                OtherAllowance = emp.OtherAllowance,
                ProbationSalaryRate = emp.WorkStatus == "Probation" ? 85m : 100m,
                Status = "Active"
            }, cancellationToken);

            await _context.EmploymentHistories.AddAsync(new EmploymentHistory
            {
                EmployeeId = emp.Id,
                FromDate = emp.HireDate,
                DepartmentName = "Tiếp nhận nhân sự mới",
                PositionName = "Gia nhập Vinamilk",
                Notes = "Tạo mới hồ sơ nhân sự trên hệ thống .NET 8"
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        return emp.Id;
    }
}

/* ----------------------------------------------------------------------------
 * 4. COMMANDS: Thêm Người phụ thuộc, Chứng chỉ, Tài liệu đính kèm & Crop Ảnh đại diện
 * ---------------------------------------------------------------------------- */
public sealed record AddEmployeeDependentCommand(
    int EmployeeId,
    string FullName,
    string Relationship,
    DateOnly? DateOfBirth,
    bool IsTaxDeductible,
    bool IsEmergencyContact,
    string? EmergencyPhone) : IRequest<int>;

public sealed class AddEmployeeDependentCommandHandler : IRequestHandler<AddEmployeeDependentCommand, int>
{
    private readonly IApplicationDbContext _context;

    public AddEmployeeDependentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(AddEmployeeDependentCommand request, CancellationToken cancellationToken)
    {
        var dep = new EmployeeDependent
        {
            EmployeeId = request.EmployeeId,
            FullName = request.FullName.Trim(),
            Relationship = request.Relationship,
            DateOfBirth = request.DateOfBirth,
            IsTaxDeductible = request.IsTaxDeductible,
            DeductionFromDate = DateOnly.FromDateTime(DateTime.Today),
            IsEmergencyContact = request.IsEmergencyContact,
            EmergencyPhone = request.EmergencyPhone?.Trim()
        };
        await _context.EmployeeDependents.AddAsync(dep, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return dep.Id;
    }
}

public sealed record AddEmployeeCertificateCommand(
    int EmployeeId,
    string CertificateName,
    string CertificateType,
    string? IssuedBy,
    DateOnly? IssueDate,
    string? ScoreOrGrade) : IRequest<int>;

public sealed class AddEmployeeCertificateCommandHandler : IRequestHandler<AddEmployeeCertificateCommand, int>
{
    private readonly IApplicationDbContext _context;

    public AddEmployeeCertificateCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(AddEmployeeCertificateCommand request, CancellationToken cancellationToken)
    {
        var cert = new EmployeeCertificate
        {
            EmployeeId = request.EmployeeId,
            CertificateName = request.CertificateName.Trim(),
            CertificateType = request.CertificateType,
            IssuedBy = request.IssuedBy?.Trim(),
            IssueDate = request.IssueDate ?? DateOnly.FromDateTime(DateTime.Today),
            ScoreOrGrade = request.ScoreOrGrade?.Trim()
        };
        await _context.EmployeeCertificates.AddAsync(cert, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return cert.Id;
    }
}

public sealed record AddEmployeeDocumentCommand(
    int EmployeeId,
    string DocumentCategory,
    string DocumentTitle,
    string FilePath,
    long FileSizeBytes) : IRequest<int>;

public sealed class AddEmployeeDocumentCommandHandler : IRequestHandler<AddEmployeeDocumentCommand, int>
{
    private readonly IApplicationDbContext _context;

    public AddEmployeeDocumentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(AddEmployeeDocumentCommand request, CancellationToken cancellationToken)
    {
        var doc = new EmployeeDocument
        {
            EmployeeId = request.EmployeeId,
            DocumentCategory = request.DocumentCategory,
            DocumentTitle = request.DocumentTitle.Trim(),
            FilePath = request.FilePath,
            ContentType = "application/pdf",
            FileSizeBytes = request.FileSizeBytes
        };
        await _context.EmployeeDocuments.AddAsync(doc, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return doc.Id;
    }
}

public sealed record UpdateEmployeeAvatarCommand(int EmployeeId, string CroppedDataUrl) : IRequest<bool>;

public sealed class UpdateEmployeeAvatarCommandHandler : IRequestHandler<UpdateEmployeeAvatarCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateEmployeeAvatarCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateEmployeeAvatarCommand request, CancellationToken cancellationToken)
    {
        var emp = await _context.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);
        if (emp is null)
            return false;

        emp.AvatarUrl = request.CroppedDataUrl;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

