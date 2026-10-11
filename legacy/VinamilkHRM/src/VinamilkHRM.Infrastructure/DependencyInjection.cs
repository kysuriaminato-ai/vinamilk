using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VinamilkHRM.Domain.Interfaces.Printing;
using VinamilkHRM.Domain.Interfaces.Reporting;
using VinamilkHRM.Domain.Interfaces.Repositories;
using VinamilkHRM.Infrastructure.Data;
using VinamilkHRM.Infrastructure.Repositories;
using VinamilkHRM.Infrastructure.Services;

namespace VinamilkHRM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=VinamilkHRMDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

            services.AddDbContext<VinamilkHRMDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(VinamilkHRMDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
            }));

        services.AddScoped<VinamilkHRM.Application.Common.Interfaces.IApplicationDbContext>(provider => 
            provider.GetRequiredService<VinamilkHRMDbContext>());

        // Đăng ký Unit of Work và Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IOrganizationalUnitRepository, OrganizationalUnitRepository>();
        services.AddScoped<ILaborContractRepository, LaborContractRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IPaySlipRepository, PaySlipRepository>();
        services.AddScoped<IKpiAssignmentRepository, KpiAssignmentRepository>();

        // Đăng ký Services Polymorphic
        services.AddScoped<VinamilkHRM.Infrastructure.Services.Printing.OfficePayslipPrinter>();
        services.AddScoped<VinamilkHRM.Infrastructure.Services.Printing.FactoryPayslipPrinter>();
        services.AddScoped<VinamilkHRM.Infrastructure.Services.Printing.RetailStorePayslipPrinter>();
        services.AddScoped<IPayslipPrinterFactory, VinamilkHRM.Infrastructure.Services.Printing.PayslipPrinterFactory>();
        services.AddScoped<IPayslipPrinter, PayslipPrinter>();
        services.AddScoped<IReportExporter, ReportExporter>();

        // Đăng ký Quản lý Phân vùng Đa điểm (Regional Scoping & Multi-Unit)
        services.AddScoped<VinamilkHRM.Domain.Interfaces.IRegionScopeService, VinamilkHRM.Infrastructure.Services.RegionScopeService>();

        return services;
    }
}
