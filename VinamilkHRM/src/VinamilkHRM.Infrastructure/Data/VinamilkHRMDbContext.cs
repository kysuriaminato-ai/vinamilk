using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
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
using VinamilkHRM.Infrastructure.Identity;

namespace VinamilkHRM.Infrastructure.Data;

/// <summary>
/// DbContext chính của Hệ thống Vinamilk Enterprise HRM kế thừa ASP.NET Core Identity.
/// Quản lý xác thực, phân quyền và toàn bộ thực thể nghiệp vụ nhân sự.
/// </summary>
public class VinamilkHRMDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>, IApplicationDbContext
{
    private readonly VinamilkHRM.Domain.Interfaces.IRegionScopeService? _regionScopeService;

    public VinamilkHRMDbContext(
        DbContextOptions<VinamilkHRMDbContext> options,
        VinamilkHRM.Domain.Interfaces.IRegionScopeService? regionScopeService = null) : base(options)
    {
        _regionScopeService = regionScopeService;
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
    public DbSet<ElectronicContractAppendix> ElectronicContractAppendices => Set<ElectronicContractAppendix>();

    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<KpiAssignment> KpiAssignments => Set<KpiAssignment>();
    public DbSet<SalaryProposal> SalaryProposals => Set<SalaryProposal>();
    public DbSet<PaySlip> PaySlips => Set<PaySlip>();
    public DbSet<CompanyAsset> CompanyAssets => Set<CompanyAsset>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DisciplinaryWarning> DisciplinaryWarnings => Set<DisciplinaryWarning>();
    public DbSet<VinamilkHRM.Domain.Entities.Careers.JobPosting> JobPostings => Set<VinamilkHRM.Domain.Entities.Careers.JobPosting>();
    public DbSet<VinamilkHRM.Domain.Entities.Careers.CandidateApplication> CandidateApplications => Set<VinamilkHRM.Domain.Entities.Careers.CandidateApplication>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VinamilkHRMDbContext).Assembly);

        // Cấu hình RowVersion concurrency token cho tất cả root entities kế thừa BaseEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) && entityType.BaseType == null)
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<byte[]>(nameof(BaseEntity.RowVersion))
                    .IsRowVersion();
            }
        }

        // Global Query Filter: Regional Scoping & Multi-Unit (Phân vùng theo Miền & Khối Đơn vị)
        if (_regionScopeService != null)
        {
            modelBuilder.Entity<OrganizationalUnit>().HasQueryFilter(u =>
                !_regionScopeService.IsFilterActive ||
                ((_regionScopeService.CurrentRegionId == null || u.RegionId == _regionScopeService.CurrentRegionId) &&
                 (_regionScopeService.CurrentUnitType == null || u.UnitType == _regionScopeService.CurrentUnitType) &&
                 (_regionScopeService.CurrentUnitId == null || u.Id == _regionScopeService.CurrentUnitId))
            );

            modelBuilder.Entity<Employee>().HasQueryFilter(e =>
                !_regionScopeService.IsFilterActive ||
                ((_regionScopeService.CurrentRegionId == null || (e.OrganizationalUnit != null && e.OrganizationalUnit.RegionId == _regionScopeService.CurrentRegionId)) &&
                 (_regionScopeService.CurrentUnitType == null || (e.OrganizationalUnit != null && e.OrganizationalUnit.UnitType == _regionScopeService.CurrentUnitType)) &&
                 (_regionScopeService.CurrentUnitId == null || e.OrganizationalUnitId == _regionScopeService.CurrentUnitId))
            );
        }
    }
}
