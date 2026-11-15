using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Payroll;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Persistence.Interceptors;
using Vinamilk.HRM.Infrastructure.Persistence.Repositories;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Vinamilk.HRM.Infrastructure.Security;
using Vinamilk.HRM.Infrastructure.Services;

namespace Vinamilk.HRM.Infrastructure;

public static class DependencyInjection
{
    public static LicenseType ResolveQuestPdfLicense(IConfiguration configuration)
    {
        var configured = configuration["QuestPDF:LicenseType"];
        if (string.IsNullOrWhiteSpace(configured))
            return LicenseType.Community;

        if (Enum.TryParse<LicenseType>(configured.Trim(), ignoreCase: true, out var licenseType))
            return licenseType;

        throw new InvalidOperationException(
            $"Cấu hình 'QuestPDF:LicenseType' = '{configured}' không hợp lệ. Chỉ hỗ trợ: Community, Professional, Enterprise.");
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        QuestPDF.Settings.License = ResolveQuestPdfLicense(configuration);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Chưa cấu hình chuỗi kết nối 'DefaultConnection' trong appsettings.json.");

        services.AddSingleton<IPiiEncryptionService, PiiEncryptionService>();
        services.AddSingleton<PasswordSecurityService>();
        services.AddSingleton<TotpAuthenticatorService>();
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddScoped<SlowQueryLoggingInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
            var slowQueryInterceptor = sp.GetRequiredService<SlowQueryLoggingInterceptor>();
            options.AddInterceptors(interceptor, slowQueryInterceptor);
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                sqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            });
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ApplicationDbContextInitializer>();
        services.AddScoped<CoreHrDocumentService>();
        services.AddScoped<TimeAttendanceDocumentService>();
        services.AddScoped<TalentDocumentService>();
        services.AddScoped<AnalyticsReportExcelService>();
        services.AddScoped<IPayrollDocumentService, PayrollDocumentService>();
        services.AddScoped<IBankTransferTemplateFormatter, CommonBankTransferTemplateFormatter>();
        services.AddScoped<IBankTransferTemplateFormatter, VietcombankTransferTemplateFormatter>();
        services.AddScoped<IPayslipEmailJob, PayslipEmailHangfireJob>();
        services.AddScoped<Vinamilk.HRM.Application.Analytics.IAiHrmExpertService, LocalRagAiHrmExpertService>();
        services.AddSingleton<IEmailNotificationService, EmailNotificationService>();
        services.AddScoped<DatabaseBackupService>();
        services.AddScoped<ContractExpirationAlertJob>();

        var useHangfireSql = configuration.GetValue<bool>("Hangfire:UseSqlServerStorage");
        if (useHangfireSql)
        {
            services.AddHangfire(cfg => cfg
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
            services.AddScoped<IPayslipBatchDispatcher, HangfirePayslipBatchDispatcher>();
        }
        else
        {
            services.AddScoped<IPayslipBatchDispatcher, InProcessPayslipBatchDispatcher>();
        }

        return services;
    }
}

