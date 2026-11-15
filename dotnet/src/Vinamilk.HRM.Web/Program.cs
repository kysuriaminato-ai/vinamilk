using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Vinamilk.HRM.Application;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Infrastructure;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Vinamilk.HRM.Web.HealthChecks;
using Vinamilk.HRM.Web.Hubs;
using Vinamilk.HRM.Web.Security;
using Vinamilk.HRM.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình Serilog (Console + Rolling File + SQL Server)
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddDataProtection();
builder.Services.AddMemoryCache();
builder.Services.AddSignalR();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IRealtimeNotificationDispatcher, SignalRNotificationDispatcher>();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 1. Cấu hình Cookie Authentication chuẩn Doanh nghiệp (HttpOnly, SameSite, SlidingExpiration)
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Vinamilk.HRM.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Error/403";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });

// 2. Cấu hình RBAC Policy-Based Authorization + FallbackPolicy (Mặc định yêu cầu đăng nhập)
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// 3. Cấu hình Rate Limiting chống Brute-Force Login & Abuse Backup API
builder.Services.AddRateLimiter(limiterOptions =>
{
    limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    limiterOptions.AddFixedWindowLimiter("login-limiter", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });
    limiterOptions.AddFixedWindowLimiter("backup-limiter", options =>
    {
        options.PermitLimit = 3;
        options.Window = TimeSpan.FromMinutes(5);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 0;
    });
});

// 4. Hỗ trợ đa ngôn ngữ (Tiếng Việt mặc định + Tiếng Anh)
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Vinamilk Enterprise HRM API",
        Version = "v1",
        Description = "Hệ thống Quản trị Nguồn nhân lực Vinamilk (.NET 8 Clean Architecture)"
    });
});

// 5. Hiệu năng Core Web Vitals: Nén Brotli/Gzip + Response Caching + ProblemDetails + HealthChecks
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.SmallestSize);
builder.Services.AddResponseCaching();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("sqlserver", tags: new[] { "ready" })
    .AddCheck("self", () => HealthCheckResult.Healthy("Vinamilk HRM Web đang hoạt động."), tags: new[] { "live" });

var app = builder.Build();

if (args.Contains("--verify-security"))
{
    var handler = new PermissionAuthorizationHandler();
    var req = new PermissionRequirement("Payroll.Approve");
    var adminPrincipal = new System.Security.Claims.ClaimsPrincipal(
        new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "admin"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "SUPER_ADMIN")
        }, "Cookie"));
    var adminCtx = new AuthorizationHandlerContext(new[] { req }, adminPrincipal, null);
    await handler.HandleAsync(adminCtx);

    var empPrincipal = new System.Security.Claims.ClaimsPrincipal(
        new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "tung.pt"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "EMPLOYEE"),
            new System.Security.Claims.Claim("Permission", "Dashboard.View")
        }, "Cookie"));
    var empCtx = new AuthorizationHandlerContext(new[] { req }, empPrincipal, null);
    await handler.HandleAsync(empCtx);

    var totp = new Vinamilk.HRM.Infrastructure.Security.TotpAuthenticatorService();
    var secret = totp.GenerateBase32Secret();
    var code = totp.ComputeCurrentTotp(secret);
    var totpOk = totp.VerifyTotpCode(secret, code, allowDemoFallback: false);

    Console.WriteLine($"[SECURITY SELF-TEST] SuperAdmin Succeed={adminCtx.HasSucceeded}, Employee Denied={!empCtx.HasSucceeded}, TOTP RFC6238 Verified={totpOk}");
    return;
}

if (args.Contains("--verify-corehr"))
{
    var retMale = Vinamilk.HRM.Application.CoreHR.Lifecycle.VietnamRetirementCalculator.Calculate(new DateOnly(1975, 5, 15), "Nam", new DateOnly(2026, 11, 15));
    var retFemale = Vinamilk.HRM.Application.CoreHR.Lifecycle.VietnamRetirementCalculator.Calculate(new DateOnly(1971, 3, 10), "Nữ", new DateOnly(2026, 11, 15));
    using var scope = app.Services.CreateScope();
    var docService = scope.ServiceProvider.GetRequiredService<Vinamilk.HRM.Infrastructure.Services.CoreHrDocumentService>();
    var excelBytes = docService.GenerateEmployeeImportTemplateExcel();
    Console.WriteLine($"[COREHR SELF-TEST] MaleRetAge={retMale.RetirementAgeYears}y{retMale.RetirementAgeMonths}m, FemaleRetAge={retFemale.RetirementAgeYears}y{retFemale.RetirementAgeMonths}m, ExcelTemplateSize={excelBytes.Length} bytes");
    return;
}

if (args.Contains("--verify-attendance"))
{
    var gpsEval = Vinamilk.HRM.Application.TimeAttendance.VinamilkGeofenceCalculator.EvaluateGps(10.7298, 106.7220);
    var qrToken = Vinamilk.HRM.Application.TimeAttendance.VinamilkGeofenceCalculator.GenerateDynamicQrToken(DateTime.UtcNow);
    var leaveEnt = Vinamilk.HRM.Application.TimeAttendance.VietnamAnnualLeavePolicyCalculator.CalculateEntitlement(new DateOnly(2014, 3, 1), 2026);
    var condMatch = Vinamilk.HRM.Application.Workflows.WorkflowSeederHelper.EvaluateStepCondition("TotalDays >= 3", 4.0m);
    using var scope = app.Services.CreateScope();
    var attDocService = scope.ServiceProvider.GetRequiredService<Vinamilk.HRM.Infrastructure.Services.TimeAttendanceDocumentService>();
    var templateBytes = attDocService.GenerateAttendanceImportTemplateExcel();
    Console.WriteLine($"[ATTENDANCE & WORKFLOW SELF-TEST] GeofenceValid={gpsEval.IsWithinGeofence} ({gpsEval.DistanceMeters}m), QR={qrToken}, Seniority12YExtraLeave=+{leaveEnt.SeniorityExtraDays}d, WorkflowCondMatch={condMatch}, AttTemplateSize={templateBytes.Length} bytes");
    return;
}

if (args.Contains("--verify-talent"))
{
    var (gradeA, multA, _) = Vinamilk.HRM.Application.Talent.KpiGradingCalculator.EvaluateGrade(92.5m);
    var (gradeB, multB, _) = Vinamilk.HRM.Application.Talent.KpiGradingCalculator.EvaluateGrade(81.0m);
    using var scope = app.Services.CreateScope();
    var talentDocService = scope.ServiceProvider.GetRequiredService<Vinamilk.HRM.Infrastructure.Services.TalentDocumentService>();
    var bm03Bytes = talentDocService.ExportBm03RewardDisciplineExcel(new Vinamilk.HRM.Application.Talent.RewardsAndBm03OverviewDto(
        2026,
        Array.Empty<Vinamilk.HRM.Application.Talent.RewardDisciplineCategoryDto>(),
        Array.Empty<Vinamilk.HRM.Application.Talent.RewardDisciplineItemDto>(),
        new[] { new Vinamilk.HRM.Application.Talent.Bm03UnitSummaryDto("PB_IT", "Phòng CNTT", 2, 10000000m, 0, 0m) },
        Array.Empty<Vinamilk.HRM.Application.Workflows.SimpleLookupDto>(),
        2, 10000000m, 0, 0m));
    Console.WriteLine($"[TALENT SELF-TEST] Kpi92.5={gradeA} (x{multA}), Kpi81.0={gradeB} (x{multB}), Bm03ExcelSize={bm03Bytes.Length} bytes");
    return;
}

if (args.Contains("--verify-analytics"))
{
    using var scope = app.Services.CreateScope();
    var excelSvc = scope.ServiceProvider.GetRequiredService<Vinamilk.HRM.Infrastructure.Services.AnalyticsReportExcelService>();
    var aiSvc = scope.ServiceProvider.GetRequiredService<Vinamilk.HRM.Application.Analytics.IAiHrmExpertService>();
    var bm01Bytes = excelSvc.ExportBm01(new List<Vinamilk.HRM.Application.Analytics.Bm01EmployeeRowDto>
    {
        new("VNM-0001", "Mai Kiều Liên", "Nữ", new DateOnly(1953, 9, 1), "Ban Tổng Giám đốc", "Tổng Giám đốc", "Đại học", new DateOnly(1976, 8, 1), "Official", "Indefinite", 120000000m)
    });
    var ragAns = await aiSvc.AskPolicyAsync("Nghỉ phép năm được mấy ngày và tỷ lệ tăng ca OT?");
    Console.WriteLine($"[ANALYTICS SELF-TEST] Bm01ExcelSize={bm01Bytes.Length} bytes, AiProvider={aiSvc.ProviderName}, RagCitations={ragAns.Citations.Count}");
    return;
}

// Tự động Migrate & Seed khi khởi động nếu được bật trong cấu hình
if (app.Configuration.GetValue<bool>("Database:AutoMigrateAndSeedOnStartup") || args.Contains("--seed-only"))
{
    using var scope = app.Services.CreateScope();
    var initializer = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitializer>();
    await initializer.InitializeAsync();
    await initializer.SeedAsync();

    if (args.Contains("--seed-only"))
    {
        Console.WriteLine("Vinamilk HRM SQL Server Migration & Seed completed successfully.");
        return;
    }
}

var supportedCultures = new[] { new CultureInfo("vi-VN"), new CultureInfo("en-US") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("vi-VN"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.UseResponseCompression();
app.UseResponseCaching();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vinamilk HRM API v1"));
}
else
{
    app.UseExceptionHandler(exceptionHandlerApp =>
    {
        exceptionHandlerApp.Run(async context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await Results.Problem(
                    title: "Lỗi hệ thống nội bộ",
                    detail: "Đã xảy ra sự cố khi xử lý yêu cầu API.",
                    statusCode: StatusCodes.Status500InternalServerError).ExecuteAsync(context);
            }
            else
            {
                context.Response.Redirect("/Error/500");
            }
        });
    });
    app.UseHsts();
}

app.UseWhen(
    ctx => !ctx.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseStatusCodePagesWithReExecute("/Error/{0}"));

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "public,max-age=2592000";
    }
});
app.UseRouting();

app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<SecurityHeadersAndSessionMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") }).AllowAnonymous();

app.MapHub<NotificationHub>("/hubs/notifications");

if (app.Configuration.GetValue<bool>("Hangfire:UseSqlServerStorage"))
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireSuperAdminAuthFilter() }
    });
}

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }

internal sealed class HangfireSuperAdminAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("SUPER_ADMIN");
    }
}
