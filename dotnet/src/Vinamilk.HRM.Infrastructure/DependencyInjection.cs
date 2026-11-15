using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Persistence.Interceptors;
using Vinamilk.HRM.Infrastructure.Persistence.Repositories;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Vinamilk.HRM.Infrastructure.Security;
using Vinamilk.HRM.Infrastructure.Services;

namespace Vinamilk.HRM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        QuestPDF.Settings.License = LicenseType.Community;

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
        services.AddScoped<Vinamilk.HRM.Application.Analytics.IAiHrmExpertService, LocalRagAiHrmExpertService>();
        services.AddSingleton<IEmailNotificationService, EmailNotificationService>();
        services.AddScoped<DatabaseBackupService>();
        services.AddScoped<ContractExpirationAlertJob>();

        return services;
    }
}

