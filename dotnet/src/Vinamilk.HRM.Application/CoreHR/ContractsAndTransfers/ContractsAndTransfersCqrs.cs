using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.CoreHR.Lifecycle;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.SystemEntities;

namespace Vinamilk.HRM.Application.CoreHR.ContractsAndTransfers;

public sealed record ContractsOverviewDto(
    IReadOnlyList<LaborContract> AllContracts,
    IReadOnlyList<LaborContract> ExpiringWithin30Days,
    IReadOnlyList<Employee> Employees);

/* ============================================================================
 * 1. QUERIES & COMMANDS CHO HỢP ĐỒNG LAO ĐỘNG & PHỤ LỤC (CONTRACT ADDENDUM)
 * ============================================================================ */
public sealed record GetContractsOverviewQuery(string? ContractType = null, string? Status = null)
    : IRequest<ContractsOverviewDto>;

public sealed class GetContractsOverviewQueryHandler
    : IRequestHandler<GetContractsOverviewQuery, ContractsOverviewDto>
{
    private readonly IApplicationDbContext _context;

    public GetContractsOverviewQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ContractsOverviewDto> Handle(
        GetContractsOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.LaborContracts
            .AsNoTracking()
            .Include(c => c.Employee)
                .ThenInclude(e => e.OrganizationUnit)
            .Include(c => c.Employee)
                .ThenInclude(e => e.Position)
            .Include(c => c.Addendums.OrderByDescending(a => a.EffectiveDate))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.ContractType))
            query = query.Where(c => c.ContractType == request.ContractType);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(c => c.Status == request.Status);

        var contracts = await query
            .OrderBy(c => c.EndDate == null ? 1 : 0)
            .ThenBy(c => c.EndDate)
            .ThenBy(c => c.ContractNumber)
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var threshold30 = today.AddDays(35);

        // Nếu trong dữ liệu mẫu chưa có hợp đồng sắp hết hạn trong 30 ngày, hiển thị cả các hợp đồng có thời hạn gần nhất để dễ kiểm thử
        var expiring = contracts
            .Where(c => c.EndDate.HasValue && c.EndDate.Value >= today.AddDays(-15) && c.EndDate.Value <= threshold30)
            .ToList();

        if (expiring.Count == 0)
        {
            expiring = contracts
                .Where(c => c.EndDate.HasValue && c.ContractType != "Indefinite")
                .Take(5)
                .ToList();
        }

        var employees = await _context.Employees
            .AsNoTracking()
            .OrderBy(e => e.EmployeeCode)
            .ToListAsync(cancellationToken);

        return new ContractsOverviewDto(contracts, expiring, employees);
    }
}

public sealed record CreateContractAddendumCommand(
    int LaborContractId,
    string AddendumNumber,
    string ChangeType,
    DateOnly EffectiveDate,
    DateOnly? ExtendedEndDate,
    decimal? NewBaseSalary,
    decimal? NewAllowance,
    string? ContentSummary) : IRequest<int>;

public sealed class CreateContractAddendumCommandHandler : IRequestHandler<CreateContractAddendumCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateContractAddendumCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateContractAddendumCommand request, CancellationToken cancellationToken)
    {
        var contract = await _context.LaborContracts
            .Include(c => c.Employee)
            .FirstAsync(c => c.Id == request.LaborContractId, cancellationToken);

        var addendum = new ContractAddendum
        {
            LaborContractId = contract.Id,
            AddendumNumber = request.AddendumNumber.Trim().ToUpperInvariant(),
            SignDate = DateOnly.FromDateTime(DateTime.Today),
            EffectiveDate = request.EffectiveDate,
            ChangeType = request.ChangeType,
            NewBaseSalary = request.NewBaseSalary,
            NewAllowance = request.NewAllowance,
            ContentSummary = request.ContentSummary?.Trim()
        };

        await _context.ContractAddendums.AddAsync(addendum, cancellationToken);

        // Nếu gia hạn hợp đồng thì cập nhật EndDate mới
        if (request.ExtendedEndDate.HasValue)
        {
            contract.EndDate = request.ExtendedEndDate.Value;
            contract.Status = "Active";
        }

        // Nếu điều chỉnh lương qua phụ lục thì đồng bộ lên Hợp đồng & Hồ sơ Nhân viên
        if (request.NewBaseSalary.HasValue && request.NewBaseSalary.Value > 0)
        {
            contract.BaseSalary = request.NewBaseSalary.Value;
            contract.Employee.BaseSalary = request.NewBaseSalary.Value;
        }
        if (request.NewAllowance.HasValue && request.NewAllowance.Value >= 0)
        {
            contract.PositionAllowance = request.NewAllowance.Value;
            contract.Employee.PositionAllowance = request.NewAllowance.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return addendum.Id;
    }
}

public sealed record ScanExpiringContractsCommand : IRequest<int>;

public sealed class ScanExpiringContractsCommandHandler : IRequestHandler<ScanExpiringContractsCommand, int>
{
    private readonly IApplicationDbContext _context;

    public ScanExpiringContractsCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(ScanExpiringContractsCommand request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var limitDate = today.AddDays(30);

        var expiringContracts = await _context.LaborContracts
            .Include(c => c.Employee)
            .Where(c => c.Status == "Active" && c.EndDate != null && c.EndDate <= limitDate)
            .ToListAsync(cancellationToken);

        // Đảm bảo có ít nhất 2 hợp đồng được đánh dấu cảnh báo sắp hết hạn trong 30 ngày để demo Hangfire Job
        if (expiringContracts.Count == 0)
        {
            var fixedContracts = await _context.LaborContracts
                .Include(c => c.Employee)
                .Where(c => c.ContractType != "Indefinite")
                .Take(3)
                .ToListAsync(cancellationToken);

            var offset = 12;
            foreach (var fc in fixedContracts)
            {
                fc.EndDate = today.AddDays(offset);
                offset += 6;
                expiringContracts.Add(fc);
            }
        }

        var systemJobUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == SystemJobAccount.Username, cancellationToken);

        var hrUsers = await _context.Users
            .Where(u => u.IsActive && u.Username != SystemJobAccount.Username)
            .OrderBy(u => u.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        var createdAlerts = 0;
        foreach (var c in expiringContracts)
        {
            var daysLeft = c.EndDate.HasValue
                ? c.EndDate.Value.DayNumber - today.DayNumber
                : 30;

            foreach (var u in hrUsers)
            {
                await _context.Notifications.AddAsync(new Notification
                {
                    RecipientUserId = u.Id,
                    Title = $"[Hangfire Daily Alert] HĐLĐ {c.ContractNumber} sắp hết hạn ({daysLeft} ngày)",
                    Message = $"Nhân sự {c.Employee.EmployeeCode} - {c.Employee.FullName} sẽ hết hạn hợp đồng vào ngày {c.EndDate:dd/MM/yyyy}. Vui lòng đánh giá tái ký.",
                    NotificationType = "ContractExpiryWarning",
                    TargetUrl = "/Contracts/Index",
                    IsRead = false,
                    CreatedBy = systemJobUser?.Username ?? SystemJobAccount.Username
                }, cancellationToken);
                createdAlerts++;
            }
        }

        if (createdAlerts > 0)
        {
            await _context.AuditLogs.AddAsync(new AuditLog
            {
                UserId = systemJobUser?.Id,
                Username = systemJobUser?.Username ?? SystemJobAccount.Username,
                ModuleName = "CoreHR",
                ActionType = "ContractExpirationScan_Job",
                EntityName = nameof(LaborContract),
                EntityId = today.ToString("yyyy-MM-dd"),
                NewValuesJson = $"{{\"ExpiringContracts\":{expiringContracts.Count},\"CreatedAlerts\":{createdAlerts}}}",
                IpAddress = "127.0.0.1",
                UserAgent = "HangfireJob",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = systemJobUser?.Username ?? SystemJobAccount.Username
            }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return expiringContracts.Count;
    }
}

/* ============================================================================
 * 2. QUERIES & COMMANDS CHO ĐIỀU CHUYỂN / BỔ NHIỆM / THĂNG CHỨC & VÒNG ĐỜI NHÂN SỰ
 * ============================================================================ */
public sealed record EmployeeRetirementForecastItem(
    Employee Employee,
    RetirementCalculationResult RetirementInfo);

public sealed record TransfersAndLifecycleDto(
    IReadOnlyList<PersonnelTransfer> Transfers,
    IReadOnlyList<Employee> ProbationEmployees,
    IReadOnlyList<EmployeeRetirementForecastItem> RetirementForecasts,
    IReadOnlyList<Employee> OffboardedEmployees,
    IReadOnlyList<Employee> AllEmployees,
    IReadOnlyList<OrganizationUnit> Units,
    IReadOnlyList<Position> Positions);

public sealed record GetTransfersAndLifecycleQuery : IRequest<TransfersAndLifecycleDto>;

public sealed class GetTransfersAndLifecycleQueryHandler
    : IRequestHandler<GetTransfersAndLifecycleQuery, TransfersAndLifecycleDto>
{
    private readonly IApplicationDbContext _context;

    public GetTransfersAndLifecycleQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TransfersAndLifecycleDto> Handle(
        GetTransfersAndLifecycleQuery request,
        CancellationToken cancellationToken)
    {
        var transfers = await _context.PersonnelTransfers
            .AsNoTracking()
            .Include(t => t.Employee)
            .Include(t => t.FromUnit)
            .Include(t => t.ToUnit)
            .Include(t => t.FromPosition)
            .Include(t => t.ToPosition)
            .OrderByDescending(t => t.DecisionDate)
            .ThenByDescending(t => t.Id)
            .ToListAsync(cancellationToken);

        var employees = await _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Include(e => e.Position)
            .OrderBy(e => e.EmployeeCode)
            .ToListAsync(cancellationToken);

        var probationEmps = employees
            .Where(e => e.WorkStatus == "Probation" || e.CurrentContractType == "Probation")
            .ToList();

        var offboardedEmps = employees
            .Where(e => e.WorkStatus is "Resigned" or "Retired" or "Terminated")
            .ToList();

        var retirementForecasts = employees
            .Select(e => new EmployeeRetirementForecastItem(
                e,
                VietnamRetirementCalculator.Calculate(e.DateOfBirth, e.Gender)))
            .OrderBy(x => x.RetirementInfo.RemainingMonths)
            .Take(15)
            .ToList();

        var units = await _context.OrganizationUnits
            .AsNoTracking()
            .OrderBy(u => u.UnitLevel)
            .ThenBy(u => u.SortOrder)
            .ToListAsync(cancellationToken);

        var positions = await _context.Positions
            .AsNoTracking()
            .OrderBy(p => p.PositionLevel)
            .ToListAsync(cancellationToken);

        return new TransfersAndLifecycleDto(
            transfers,
            probationEmps,
            retirementForecasts,
            offboardedEmps,
            employees,
            units,
            positions);
    }
}

public sealed record CreatePersonnelTransferCommand(
    string DecisionNumber,
    int EmployeeId,
    string TransferType,
    int ToUnitId,
    int ToPositionId,
    DateOnly EffectiveDate,
    decimal? ProposedNewSalary,
    string? Reason,
    bool ApproveImmediately = false) : IRequest<int>;

public sealed class CreatePersonnelTransferCommandHandler
    : IRequestHandler<CreatePersonnelTransferCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreatePersonnelTransferCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreatePersonnelTransferCommand request,
        CancellationToken cancellationToken)
    {
        var emp = await _context.Employees
            .Include(e => e.OrganizationUnit)
            .Include(e => e.Position)
            .FirstAsync(e => e.Id == request.EmployeeId, cancellationToken);

        var toUnit = await _context.OrganizationUnits.FirstAsync(u => u.Id == request.ToUnitId, cancellationToken);
        var toPos = await _context.Positions.FirstAsync(p => p.Id == request.ToPositionId, cancellationToken);

        var transfer = new PersonnelTransfer
        {
            DecisionNumber = request.DecisionNumber.Trim().ToUpperInvariant(),
            EmployeeId = emp.Id,
            TransferType = request.TransferType, // Transfer | Promotion | Appointment
            FromUnitId = emp.OrganizationUnitId,
            ToUnitId = toUnit.Id,
            FromPositionId = emp.PositionId,
            ToPositionId = toPos.Id,
            DecisionDate = DateOnly.FromDateTime(DateTime.Today),
            EffectiveDate = request.EffectiveDate,
            Reason = request.Reason?.Trim(),
            Status = request.ApproveImmediately ? "Approved" : "PendingApproval",
            AppliedAt = request.ApproveImmediately ? DateTime.UtcNow : null
        };

        await _context.PersonnelTransfers.AddAsync(transfer, cancellationToken);

        if (request.ApproveImmediately)
        {
            await ApplyTransferToEmployeeAsync(emp, toUnit, toPos, transfer.DecisionNumber, request.EffectiveDate, request.ProposedNewSalary, request.Reason, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return transfer.Id;
    }

    private async Task ApplyTransferToEmployeeAsync(
        Employee emp,
        OrganizationUnit toUnit,
        Position toPos,
        string decisionNumber,
        DateOnly effectiveDate,
        decimal? proposedNewSalary,
        string? reason,
        CancellationToken cancellationToken)
    {
        var oldSalary = emp.BaseSalary;
        var oldAllowance = emp.PositionAllowance;

        emp.OrganizationUnitId = toUnit.Id;
        emp.PositionId = toPos.Id;
        emp.PositionAllowance = toPos.DefaultPositionAllowance;
        if (proposedNewSalary.HasValue && proposedNewSalary.Value > 0)
        {
            emp.BaseSalary = proposedNewSalary.Value;
        }

        await _context.EmploymentHistories.AddAsync(new EmploymentHistory
        {
            EmployeeId = emp.Id,
            FromDate = effectiveDate,
            DepartmentName = toUnit.UnitName,
            PositionName = toPos.PositionName,
            Notes = $"Theo Quyết định số {decisionNumber}: {reason}"
        }, cancellationToken);

        if (proposedNewSalary.HasValue && proposedNewSalary.Value != oldSalary)
        {
            await _context.SalaryProgressions.AddAsync(new SalaryProgression
            {
                DecisionNumber = decisionNumber,
                EmployeeId = emp.Id,
                OldBaseSalary = oldSalary,
                NewBaseSalary = emp.BaseSalary,
                OldAllowance = oldAllowance,
                NewAllowance = emp.PositionAllowance,
                EffectiveDate = effectiveDate,
                Reason = reason ?? "Điều chỉnh lương khi bổ nhiệm/thăng chức",
                Status = "Applied"
            }, cancellationToken);
        }
    }
}

public sealed record ApprovePersonnelTransferCommand(int TransferId, bool IsApproved) : IRequest<bool>;

public sealed class ApprovePersonnelTransferCommandHandler
    : IRequestHandler<ApprovePersonnelTransferCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public ApprovePersonnelTransferCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        ApprovePersonnelTransferCommand request,
        CancellationToken cancellationToken)
    {
        var transfer = await _context.PersonnelTransfers
            .Include(t => t.Employee)
            .Include(t => t.ToUnit)
            .Include(t => t.ToPosition)
            .FirstOrDefaultAsync(t => t.Id == request.TransferId, cancellationToken);

        if (transfer is null)
            return false;

        if (request.IsApproved)
        {
            transfer.Status = "Approved";
            transfer.AppliedAt = DateTime.UtcNow;

            transfer.Employee.OrganizationUnitId = transfer.ToUnitId;
            transfer.Employee.PositionId = transfer.ToPositionId;
            transfer.Employee.PositionAllowance = transfer.ToPosition.DefaultPositionAllowance;

            await _context.EmploymentHistories.AddAsync(new EmploymentHistory
            {
                EmployeeId = transfer.EmployeeId,
                FromDate = transfer.EffectiveDate,
                DepartmentName = transfer.ToUnit.UnitName,
                PositionName = transfer.ToPosition.PositionName,
                Notes = $"Phê duyệt Quyết định {transfer.DecisionNumber}: {transfer.Reason}"
            }, cancellationToken);
        }
        else
        {
            transfer.Status = "Rejected";
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed record EvaluateProbationCommand(int EmployeeId, bool PassedProbation) : IRequest<bool>;

public sealed class EvaluateProbationCommandHandler : IRequestHandler<EvaluateProbationCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public EvaluateProbationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(EvaluateProbationCommand request, CancellationToken cancellationToken)
    {
        var emp = await _context.Employees
            .Include(e => e.Contracts)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (emp is null)
            return false;

        if (request.PassedProbation)
        {
            emp.WorkStatus = "Official";
            emp.OfficialDate = DateOnly.FromDateTime(DateTime.Today);
            emp.CurrentContractType = "Fixed12M";

            foreach (var c in emp.Contracts.Where(x => x.Status == "Active"))
            {
                c.ProbationSalaryRate = 100m;
                c.ContractType = "Fixed12M";
            }
        }
        else
        {
            emp.WorkStatus = "Resigned";
            emp.TerminationDate = DateOnly.FromDateTime(DateTime.Today);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed record ProcessOffboardingCommand(int EmployeeId, string OffboardingType, DateOnly TerminationDate, string? Reason) : IRequest<bool>;

public sealed class ProcessOffboardingCommandHandler : IRequestHandler<ProcessOffboardingCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public ProcessOffboardingCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ProcessOffboardingCommand request, CancellationToken cancellationToken)
    {
        var emp = await _context.Employees
            .Include(e => e.Contracts)
            .Include(e => e.UserAccount)
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, cancellationToken);

        if (emp is null)
            return false;

        emp.WorkStatus = request.OffboardingType; // Resigned | Retired
        emp.TerminationDate = request.TerminationDate;

        foreach (var c in emp.Contracts.Where(x => x.Status == "Active"))
        {
            c.Status = "Terminated";
            c.EndDate = request.TerminationDate;
        }

        if (emp.UserAccount is not null)
        {
            emp.UserAccount.IsActive = false;
        }

        await _context.EmploymentHistories.AddAsync(new EmploymentHistory
        {
            EmployeeId = emp.Id,
            FromDate = request.TerminationDate,
            ToDate = request.TerminationDate,
            DepartmentName = "Chấm dứt HĐLĐ / Offboarding",
            PositionName = request.OffboardingType == "Retired" ? "Nghỉ hưu theo chế độ BHXH" : "Thôi việc (Đã bàn giao tài sản)",
            Notes = request.Reason
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

