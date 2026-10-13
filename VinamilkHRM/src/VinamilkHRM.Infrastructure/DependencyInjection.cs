using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Domain.Entities.Statutory;
using VinamilkHRM.Domain.Interfaces.Printing;
using VinamilkHRM.Domain.Interfaces.Reporting;
using VinamilkHRM.Domain.Interfaces.Repositories;
using VinamilkHRM.Infrastructure.Data;
using VinamilkHRM.Infrastructure.Identity;
using VinamilkHRM.Infrastructure.Interceptors;
using VinamilkHRM.Infrastructure.Repositories;
using VinamilkHRM.Infrastructure.Services;

namespace VinamilkHRM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=(localdb)\\mssqllocaldb;Database=VinamilkHRMDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        // Bộ đệm & Bảo vệ dữ liệu nhạy cảm
        services.AddMemoryCache();
        services.AddDataProtection();

        // Đăng ký SaveChangesInterceptor kiểm toán
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<VinamilkHRMDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.UseHierarchyId();
                sqlOptions.MigrationsAssembly(typeof(VinamilkHRMDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
            })
            .AddInterceptors(interceptor);
        });

        services.AddScoped<IApplicationDbContext>(provider => 
            provider.GetRequiredService<VinamilkHRMDbContext>());

        // Cấu hình ASP.NET Core Identity
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // Mật khẩu tối thiểu 10 ký tự, có chữ hoa, chữ thường, số
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 10;

            // Khóa tài khoản sau 5 lần đăng nhập thất bại
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = false;
        })
        .AddEntityFrameworkStores<VinamilkHRMDbContext>()
        .AddDefaultTokenProviders();

        // Đăng ký dịch vụ định danh & phạm vi dữ liệu
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddScoped<IDataScope, DataScopeService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, Security.PermissionPolicyProvider>();
        services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Security.PermissionAuthorizationHandler>();

        // Đăng ký dịch vụ mã hóa trường nhạy cảm & nạp tham số pháp lý
        services.AddScoped<IFieldEncryptionService, FieldEncryptionService>();
        services.AddScoped<IStatutoryParameterProvider, StatutoryParameterProvider>();

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
        services.AddScoped<VinamilkHRM.Infrastructure.Services.Printing.QuestPdfPayslipGenerator>();
        services.AddScoped<VinamilkHRM.Application.Interfaces.IPayrollExportService, PayrollExportService>();
        services.AddScoped<IReportExporter, ReportExporter>();

        // Đăng ký Quản lý Phân vùng Đa điểm (Regional Scoping & Multi-Unit)
        services.AddScoped<VinamilkHRM.Domain.Interfaces.IRegionScopeService, VinamilkHRM.Infrastructure.Services.RegionScopeService>();

        // Session 11 P1-2 Smtp Email Sender
        services.AddTransient<VinamilkHRM.Domain.Interfaces.Notifications.IEmailSender, VinamilkHRM.Infrastructure.Services.Notifications.SmtpEmailSender>();

        // Session 11 P1-3 E-Signature Provider
        services.AddScoped<VinamilkHRM.Domain.Interfaces.Contracts.IESignProvider, VinamilkHRM.Infrastructure.Services.Contracts.MockESignProvider>();

        // Cấu hình Hangfire lưu trên SQL Server
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));

        services.AddHangfireServer();

        return services;
    }
}

