using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Domain.Entities.Assets;
using VinamilkHRM.Domain.Entities.Attendances;
using VinamilkHRM.Domain.Entities.Auditing;
using VinamilkHRM.Domain.Entities.Contracts;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Entities.LeaveRequests;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Entities.Shifts;

namespace VinamilkHRM.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<OrganizationalUnit> OrganizationalUnits { get; }
    DbSet<Employee> Employees { get; }
    DbSet<LaborContract> LaborContracts { get; }
    DbSet<Shift> Shifts { get; }
    DbSet<Attendance> Attendances { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<KpiAssignment> KpiAssignments { get; }
    DbSet<SalaryProposal> SalaryProposals { get; }
    DbSet<PaySlip> PaySlips { get; }
    DbSet<CompanyAsset> CompanyAssets { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<DisciplinaryWarning> DisciplinaryWarnings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
