using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Domain.Common;
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

using VinamilkHRM.Application.Common.Interfaces;

namespace VinamilkHRM.Infrastructure.Data;

/// <summary>
/// DbContext chính của Hệ thống Vinamilk Enterprise HRM.
/// Thiết lập ánh xạ Entity Framework Core và tự động cập nhật Audit Trails cho BaseAuditableEntity.
/// </summary>
public class VinamilkHRMDbContext : DbContext, IApplicationDbContext
{
    public VinamilkHRMDbContext(DbContextOptions<VinamilkHRMDbContext> options) : base(options)
    {
    }

    public DbSet<OrganizationalUnit> OrganizationalUnits => Set<OrganizationalUnit>();
    public DbSet<HeadquartersUnit> HeadquartersUnits => Set<HeadquartersUnit>();
    public DbSet<FactoryUnit> FactoryUnits => Set<FactoryUnit>();
    public DbSet<DairyFarmUnit> DairyFarmUnits => Set<DairyFarmUnit>();
    public DbSet<RetailStoreUnit> RetailStoreUnits => Set<RetailStoreUnit>();

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<OfficeEmployee> OfficeEmployees => Set<OfficeEmployee>();
    public DbSet<FactoryWorker> FactoryWorkers => Set<FactoryWorker>();
    public DbSet<FarmSpecialist> FarmSpecialists => Set<FarmSpecialist>();
    public DbSet<StoreAssociate> StoreAssociates => Set<StoreAssociate>();

    public DbSet<LaborContract> LaborContracts => Set<LaborContract>();
    public DbSet<ProbationContract> ProbationContracts => Set<ProbationContract>();
    public DbSet<DefiniteContract> DefiniteContracts => Set<DefiniteContract>();
    public DbSet<IndefiniteContract> IndefiniteContracts => Set<IndefiniteContract>();

    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<KpiAssignment> KpiAssignments => Set<KpiAssignment>();
    public DbSet<SalaryProposal> SalaryProposals => Set<SalaryProposal>();
    public DbSet<PaySlip> PaySlips => Set<PaySlip>();
    public DbSet<CompanyAsset> CompanyAssets => Set<CompanyAsset>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DisciplinaryWarning> DisciplinaryWarnings => Set<DisciplinaryWarning>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VinamilkHRMDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseAuditableEntity>();
        DateTime utcNow = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.SetAuditCreated("SYSTEM", utcNow);
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.SetAuditUpdated("SYSTEM", utcNow);
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
