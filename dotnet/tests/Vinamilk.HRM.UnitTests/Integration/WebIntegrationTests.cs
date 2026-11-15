using System.IO.Compression;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.Talent;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Xunit;

namespace Vinamilk.HRM.UnitTests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"VnmIntegrationDb_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:AutoMigrateAndSeedOnStartup"] = "false",
                ["Hangfire:UseSqlServerStorage"] = "false",
                ["Security:PiiEncryptionKey"] = "IntegrationTest_SecretKey_Only_32Chars!"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "TestScheme";
                options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
            }).AddScheme<AuthenticationSchemeOptions, TestHeaderAuthHandler>("TestScheme", _ => { });
        });
    }

    public async Task SeedPublicJobAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!await db.OrganizationUnits.AnyAsync())
        {
            db.OrganizationUnits.Add(new OrganizationUnit { Id = 1, UnitCode = "PB_CNTT", UnitName = "Phòng CNTT", UnitType = "Department", IsActive = true });
            db.Positions.Add(new Position { Id = 1, PositionCode = "DEV", PositionName = "Kỹ sư Phần mềm", IsActive = true });
            db.Employees.Add(new Employee
            {
                Id = 1,
                EmployeeCode = "VNM-0001",
                FullName = "Mai Kiều Liên",
                Gender = "Nữ",
                DateOfBirth = new DateOnly(1953, 9, 1),
                OrganizationUnitId = 1,
                PositionId = 1,
                HireDate = new DateOnly(1976, 8, 1),
                WorkStatus = "Official",
                BaseSalary = 120000000m
            });
            db.JobPostings.Add(new JobPosting
            {
                Id = 101,
                PostingCode = "VNM-JOB-101",
                Title = "Kỹ sư Phần mềm .NET 8",
                OrganizationUnitId = 1,
                PositionId = 1,
                Vacancies = 2,
                WorkLocation = "TP. Hồ Chí Minh",
                SalaryRangeText = "30 - 45 triệu",
                PublishDate = DateOnly.FromDateTime(DateTime.Today),
                ClosingDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                Status = "Open"
            });
            await db.SaveChangesAsync();
        }
    }

    public async Task<(int CalculatedPeriodId, int ApprovedPeriodId, long Emp1RecordId, long Emp2RecordId)> SeedPayrollIntegrationDataAsync()
    {
        await SeedPublicJobAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var enc = scope.ServiceProvider.GetRequiredService<IPiiEncryptionService>();

        if (!await db.PayrollStatutoryParameters.AnyAsync())
        {
            await db.PayrollStatutoryParameters.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultStatutoryParameters());
            await db.PitBrackets.AddRangeAsync(ApplicationDbContextInitializer.BuildDefaultPitBrackets());
        }

        if (!await db.Employees.AnyAsync(e => e.Id == 2))
        {
            db.Employees.Add(new Employee
            {
                Id = 2,
                EmployeeCode = "VNM-0002",
                FullName = "Nguyễn Văn B",
                OrganizationUnitId = 1,
                PositionId = 1,
                WorkStatus = "Official",
                BaseSalary = 25_000_000m,
                CitizenIdEncrypted = enc.Encrypt("079095002222"),
                BankAccountEncrypted = enc.Encrypt("190022220002"),
                BankName = "Vietcombank"
            });
        }

        var existingCalc = await db.PayrollPeriods.FirstOrDefaultAsync(p => p.PeriodCode == "PAY-2026-09");
        var existingApp = await db.PayrollPeriods.FirstOrDefaultAsync(p => p.PeriodCode == "PAY-2026-10");
        if (existingCalc is not null && existingApp is not null)
        {
            var r1 = await db.PayrollRecords.FirstAsync(r => r.PayrollPeriodId == existingApp.Id && r.EmployeeId == 1);
            var r2 = await db.PayrollRecords.FirstAsync(r => r.PayrollPeriodId == existingApp.Id && r.EmployeeId == 2);
            return (existingCalc.Id, existingApp.Id, r1.Id, r2.Id);
        }

        var pCalc = new PayrollPeriod
        {
            PeriodCode = "PAY-2026-09",
            Month = 9,
            Year = 2026,
            PeriodTitle = "Bảng lương T09/2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 9, 30),
            StandardWorkdays = 26m,
            Status = PayrollPeriodStatuses.Calculated
        };
        var pApp = new PayrollPeriod
        {
            PeriodCode = "PAY-2026-10",
            Month = 10,
            Year = 2026,
            PeriodTitle = "Bảng lương T10/2026",
            StartDate = new DateOnly(2026, 10, 1),
            EndDate = new DateOnly(2026, 10, 31),
            StandardWorkdays = 26m,
            Status = PayrollPeriodStatuses.Approved
        };
        db.PayrollPeriods.AddRange(pCalc, pApp);
        await db.SaveChangesAsync();

        var rec1 = new PayrollRecord
        {
            PayrollPeriodId = pApp.Id,
            EmployeeId = 1,
            OrganizationUnitId = 1,
            PositionId = 1,
            StandardWorkdays = 26m,
            PayableWorkdays = 26m,
            ContractBaseSalary = 20_000_000m,
            GrossIncome = 20_000_000m,
            NetSalary = 18_000_000m,
            PersonalIncomeTax = 500_000m,
            PaymentStatus = PayrollPeriodStatuses.Approved
        };
        var rec2 = new PayrollRecord
        {
            PayrollPeriodId = pApp.Id,
            EmployeeId = 2,
            OrganizationUnitId = 1,
            PositionId = 1,
            StandardWorkdays = 26m,
            PayableWorkdays = 26m,
            ContractBaseSalary = 25_000_000m,
            GrossIncome = 25_000_000m,
            NetSalary = 22_000_000m,
            PersonalIncomeTax = 1_000_000m,
            PaymentStatus = PayrollPeriodStatuses.Approved
        };
        db.PayrollRecords.AddRange(rec1, rec2);
        await db.SaveChangesAsync();

        return (pCalc.Id, pApp.Id, rec1.Id, rec2.Id);
    }
}

public class TestHeaderAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestHeaderAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Role", out var roleValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        var role = roleValues.ToString();
        var userId = Request.Headers.TryGetValue("X-Test-UserId", out var uidVal) ? uidVal.ToString() : "1";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, $"test-user-{userId}"),
            new(ClaimTypes.Role, role)
        };

        if (Request.Headers.TryGetValue("X-Test-EmployeeId", out var empIdVal))
            claims.Add(new Claim("EmployeeId", empIdVal.ToString()));

        if (Request.Headers.TryGetValue("X-Test-MustChangePassword", out var mustChangeVal))
            claims.Add(new Claim("MustChangePassword", mustChangeVal.ToString()));

        if (Request.Headers.TryGetValue("X-Test-Permission", out var perm))
        {
            foreach (var p in perm.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                claims.Add(new Claim("Permission", p));
            }
        }

        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "TestScheme")));
    }
}

public class WebIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WebIntegrationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task PublicEndpoints_ShouldCompressWithBrotliOrGzip_AndReturnValidJobPostingJsonLd()
    {
        await _factory.SeedPublicJobAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("Accept-Encoding", "br, gzip");

        var aboutResp = await client.GetAsync("/gioi-thieu");
        aboutResp.StatusCode.Should().Be(HttpStatusCode.OK);
        aboutResp.Content.Headers.ContentEncoding.Should().Contain(enc => enc == "br" || enc == "gzip");

        var detailResp = await client.GetAsync("/tuyen-dung/ky-su-phan-mem-net-8-101");
        detailResp.StatusCode.Should().Be(HttpStatusCode.OK);
        string html;
        await using (var rawStream = await detailResp.Content.ReadAsStreamAsync())
        {
            var enc = detailResp.Content.Headers.ContentEncoding.FirstOrDefault();
            if (enc == "br")
            {
                await using var br = new BrotliStream(rawStream, CompressionMode.Decompress);
                using var sr = new StreamReader(br);
                html = await sr.ReadToEndAsync();
            }
            else if (enc == "gzip")
            {
                await using var gz = new GZipStream(rawStream, CompressionMode.Decompress);
                using var sr = new StreamReader(gz);
                html = await sr.ReadToEndAsync();
            }
            else
            {
                using var sr = new StreamReader(rawStream);
                html = await sr.ReadToEndAsync();
            }
        }

        var match = Regex.Match(html, "<script type=\"application/ld\\+json\">(.*?)</script>", RegexOptions.Singleline);
        match.Success.Should().BeTrue("Trang chi tiết tuyển dụng phải có JSON-LD schema");

        using var doc = JsonDocument.Parse(match.Groups[1].Value);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        var jobSchema = doc.RootElement.EnumerateArray().First(e => e.GetProperty("@type").GetString() == "JobPosting");
        jobSchema.GetProperty("title").GetString().Should().Be("Kỹ sư Phần mềm .NET 8");
        jobSchema.GetProperty("hiringOrganization").GetProperty("name").GetString().Should().Be("Vinamilk");
    }

    [Fact]
    public async Task UnauthenticatedRequests_ToAdminAndBackupApi_ShouldBeBlocked()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // API backup chưa đăng nhập -> 401 Unauthorized
        var backupResp = await client.PostAsync("/api/v1/system/backup", null);
        backupResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Trang quản trị chưa đăng nhập -> 302 Redirect về /Auth/Login
        var homeResp = await client.GetAsync("/");
        homeResp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        homeResp.Headers.Location?.ToString().Should().Contain("/Auth/Login");

        var empResp = await client.GetAsync("/Employees");
        empResp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        empResp.Headers.Location?.ToString().Should().Contain("/Auth/Login");

        // /health/live được AllowAnonymous -> 200 OK
        var liveResp = await client.GetAsync("/health/live");
        liveResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthenticatedAdminRequests_ShouldReturnNoIndex_HealthReady200_AndBackupSafeJson()
    {
        await _factory.SeedPublicJobAsync();
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Test-Role", "SUPER_ADMIN");

        // /health/ready trả 200 OK khi đã đăng nhập
        var readyResp = await client.GetAsync("/health/ready");
        readyResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Trang quản trị có meta noindex, nofollow
        var adminResp = await client.GetAsync("/");
        adminResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var adminHtml = await adminResp.Content.ReadAsStringAsync();
        adminHtml.Should().Contain("noindex, nofollow");

        // SuperAdmin gọi POST /api/v1/system/backup -> 200 OK và không lộ đường dẫn file
        var backupResp = await client.PostAsync("/api/v1/system/backup", null);
        backupResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var backupJson = await backupResp.Content.ReadAsStringAsync();
        backupJson.Should().NotContain("FilePath");
        backupJson.Should().NotContain(".json");
    }

    [Fact]
    public async Task AuthenticatedEmployeeWithoutPermission_ShouldReceive403OnBackup()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add("X-Test-Role", "EMPLOYEE");

        var backupResp = await client.PostAsync("/api/v1/system/backup", null);
        backupResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PayrollEndpoints_AndEssPayslipDownload_ShouldEnforceRbacOwnershipAndPeriodState()
    {
        var (calcPeriodId, approvedPeriodId, emp1RecordId, emp2RecordId) = await _factory.SeedPayrollIntegrationDataAsync();

        // 1. Anonymous requests -> 302 redirect to /Auth/Login
        var anonClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        (await anonClient.GetAsync("/Payroll")).StatusCode.Should().Be(HttpStatusCode.Redirect);
        (await anonClient.GetAsync($"/Payroll/ExportBankTransfer?payrollPeriodId={approvedPeriodId}")).StatusCode.Should().Be(HttpStatusCode.Redirect);
        (await anonClient.GetAsync($"/ESS/Home/DownloadPayslip?payrollRecordId={emp1RecordId}")).StatusCode.Should().Be(HttpStatusCode.Redirect);

        // 2. EMPLOYEE role không có quyền Payroll -> Redirect /Error/403 trên Payroll & Unlock
        var empOnlyClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        empOnlyClient.DefaultRequestHeaders.Add("X-Test-Role", "EMPLOYEE");
        empOnlyClient.DefaultRequestHeaders.Add("X-Test-UserId", "101");
        empOnlyClient.DefaultRequestHeaders.Add("X-Test-EmployeeId", "1");

        var empPayrollResp = await empOnlyClient.GetAsync("/Payroll");
        empPayrollResp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        empPayrollResp.Headers.Location?.ToString().Should().Contain("/Error/403");

        var empUnlockResp = await empOnlyClient.PostAsync("/Payroll/Unlock", null);
        empUnlockResp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        empUnlockResp.Headers.Location?.ToString().Should().Contain("/Error/403");

        // Employee 1 tải phiếu lương CỦA MÌNH -> 200 OK PDF; tải của Employee 2 -> Redirect /Error/403
        var ownPdfResp = await empOnlyClient.GetAsync($"/ESS/Home/DownloadPayslip?payrollRecordId={emp1RecordId}");
        ownPdfResp.StatusCode.Should().Be(HttpStatusCode.OK);
        ownPdfResp.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");

        var otherPdfResp = await empOnlyClient.GetAsync($"/ESS/Home/DownloadPayslip?payrollRecordId={emp2RecordId}");
        otherPdfResp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        otherPdfResp.Headers.Location?.ToString().Should().Contain("/Error/403");

        // 3. HR_MANAGER (kể cả gán claim Payroll.Unlock) KHÔNG nằm trong Role SUPER_ADMIN/HR_DIRECTOR -> Unlock bị từ chối (/Error/403)
        var hrManagerClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        hrManagerClient.DefaultRequestHeaders.Add("X-Test-Role", "HR_MANAGER");
        hrManagerClient.DefaultRequestHeaders.Add("X-Test-Permission", "Payroll.View,Payroll.Unlock,Payroll.Export");

        var hrMgrUnlockResp = await hrManagerClient.PostAsync("/Payroll/Unlock", null);
        hrMgrUnlockResp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        hrMgrUnlockResp.Headers.Location?.ToString().Should().Contain("/Error/403");

        // 4. Xuất file chuyển khoản ở trạng thái Calculated -> 400 BadRequest; ở trạng thái Approved -> 200 OK .xlsx
        var calcExportResp = await hrManagerClient.GetAsync($"/Payroll/ExportBankTransfer?payrollPeriodId={calcPeriodId}&format=excel&templateCode=COMMON");
        calcExportResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var approvedExportResp = await hrManagerClient.GetAsync($"/Payroll/ExportBankTransfer?payrollPeriodId={approvedPeriodId}&format=excel&templateCode=COMMON");
        approvedExportResp.StatusCode.Should().Be(HttpStatusCode.OK);
        approvedExportResp.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        var deptExcelResp = await hrManagerClient.GetAsync($"/Payroll/ExportDepartmentSummaryExcel?payrollPeriodId={approvedPeriodId}");
        deptExcelResp.StatusCode.Should().Be(HttpStatusCode.OK);
        deptExcelResp.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task MustChangePasswordMiddleware_Should_BlockAllOtherPages_AndAllowChangePassword()
    {
        var forcedClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        forcedClient.DefaultRequestHeaders.Add("X-Test-Role", "SUPER_ADMIN");
        forcedClient.DefaultRequestHeaders.Add("X-Test-UserId", "9999");
        forcedClient.DefaultRequestHeaders.Add("X-Test-MustChangePassword", "true");

        var homeResp = await forcedClient.GetAsync("/");
        homeResp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        homeResp.Headers.Location?.ToString().Should().Be("/Auth/ChangePassword?firstLogin=true");

        var payrollResp = await forcedClient.GetAsync("/Payroll");
        payrollResp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        payrollResp.Headers.Location?.ToString().Should().Be("/Auth/ChangePassword?firstLogin=true");

        var empResp = await forcedClient.GetAsync("/Employee");
        empResp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        empResp.Headers.Location?.ToString().Should().Be("/Auth/ChangePassword?firstLogin=true");

        var apiResp = await forcedClient.GetAsync("/api/v1/restricted-check");
        apiResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var changePassResp = await forcedClient.GetAsync("/Auth/ChangePassword?firstLogin=true");
        changePassResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthController_Should_RejectSampleVariant_UpgradeLegacyHash_AndBlockSystemJobLogin()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<Vinamilk.HRM.Infrastructure.Security.PasswordSecurityService>();
        var totpService = scope.ServiceProvider.GetRequiredService<Vinamilk.HRM.Infrastructure.Security.TotpAuthenticatorService>();
        var dpProvider = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>();

        var legacySha256Bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("VinamilkHRM_Salt_2026::Vinamilk@2026"));
        var legacyHash = Convert.ToBase64String(legacySha256Bytes);

        var role = await db.Roles.FirstOrDefaultAsync(r => r.RoleCode == "EMPLOYEE");
        if (role is null)
        {
            role = new Vinamilk.HRM.Domain.Entities.Identity.Role { Id = 50, RoleCode = "EMPLOYEE", RoleName = "Nhân viên", DefaultDataScope = "SelfOnly" };
            db.Roles.Add(role);
        }

        var legacyUser = new Vinamilk.HRM.Domain.Entities.Identity.User
        {
            Id = 501,
            Username = "legacy.user",
            Email = "legacy.user@vinamilk.com.vn",
            PasswordHash = legacyHash,
            IsActive = true,
            MustChangePassword = false,
            IsSystemAccount = false
        };
        legacyUser.UserRoles.Add(new Vinamilk.HRM.Domain.Entities.Identity.UserRole { RoleId = role.Id });

        const string systemPass = "SystemJob_Secret@2026!";
        var sysUser = new Vinamilk.HRM.Domain.Entities.Identity.User
        {
            Id = 502,
            Username = SystemJobAccount.Username,
            Email = SystemJobAccount.Email,
            PasswordHash = passwordService.HashPassword(systemPass),
            IsActive = true,
            MustChangePassword = true,
            IsSystemAccount = true
        };

        db.Users.AddRange(legacyUser, sysUser);
        await db.SaveChangesAsync();

        var httpServices = new ServiceCollection();
        httpServices.AddSingleton<IAuthenticationService, StubCookieAuthService>();
        var sp = httpServices.BuildServiceProvider();

        var controller = new Vinamilk.HRM.Web.Controllers.AuthController(db, passwordService, totpService, dpProvider);
        var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { RequestServices = sp };
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(
            httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor());
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext(actionContext);
        controller.Url = new StubUrlHelper(actionContext);
        controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            httpContext,
            new StubTempDataProvider());

        // 1. "Vinamilk@2026!" PHẢI bị từ chối với hash legacy của "Vinamilk@2026"
        var variantResult = await controller.LoginSubmit("legacy.user", "Vinamilk@2026!", "826904");
        variantResult.Should().BeOfType<Microsoft.AspNetCore.Mvc.ViewResult>();
        (await db.Users.AsNoTracking().SingleAsync(u => u.Id == 501)).PasswordHash.Should().Be(legacyHash);

        // 2. Đăng nhập thành công bằng hash legacy -> tự động nâng cấp PBKDF2 + bật MustChangePassword + redirect ChangePassword
        var legacyLoginResult = await controller.LoginSubmit("legacy.user", "Vinamilk@2026", "826904");
        var redirect = legacyLoginResult.Should().BeOfType<Microsoft.AspNetCore.Mvc.RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("ChangePassword");

        var upgradedUser = await db.Users.AsNoTracking().SingleAsync(u => u.Id == 501);
        upgradedUser.PasswordHash.Should().StartWith("PBKDF2$");
        upgradedUser.MustChangePassword.Should().BeTrue();
        passwordService.VerifyPassword("Vinamilk@2026", upgradedUser.PasswordHash).Should().BeTrue();

        // 3. system.job PHẢI bị từ chối trong mọi trường hợp (kể cả đúng mật khẩu) và ghi AuditLog
        var sysLoginResult = await controller.LoginSubmit(SystemJobAccount.Username, systemPass, "826904");
        sysLoginResult.Should().BeOfType<Microsoft.AspNetCore.Mvc.ViewResult>();

        var blockedAudit = await db.AuditLogs.FirstOrDefaultAsync(
            a => a.UserId == 502 && a.ActionType == "SystemAccountLoginBlocked");
        blockedAudit.Should().NotBeNull();
        blockedAudit!.Username.Should().Be(SystemJobAccount.Username);

        // 4. Người dùng đã đăng nhập với MustChangePassword = true đổi mật khẩu thành công -> tắt MustChangePassword & lưu PBKDF2 mới
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "501"),
            new Claim(ClaimTypes.Name, "legacy.user"),
            new Claim("SessionId", "1")
        }, "Cookie"));

        var changeResp = await controller.ChangePasswordSubmit(null, "Vinamilk@2026", "NewStrong@2026#Pass", "NewStrong@2026#Pass", CancellationToken.None);
        var essRedirect = changeResp.Should().BeOfType<Microsoft.AspNetCore.Mvc.RedirectResult>().Subject;
        essRedirect.Url.Should().Be("/ESS");

        var updatedUser = await db.Users.AsNoTracking().SingleAsync(u => u.Id == 501);
        updatedUser.MustChangePassword.Should().BeFalse();
        updatedUser.PasswordHash.Should().StartWith("PBKDF2$");
        passwordService.VerifyPassword("NewStrong@2026#Pass", updatedUser.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task AllSixRoles_Should_EnforceStrictRbacHttpMatrix_AndEssSelfOnlyOwnership()
    {
        var (_, _, emp1RecordId, emp2RecordId) = await _factory.SeedPayrollIntegrationDataAsync();

        HttpClient CreateRoleClient(string roleCode, string userId, string employeeId, bool allowAutoRedirect = true)
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = allowAutoRedirect,
                HandleCookies = false
            });
            client.DefaultRequestHeaders.Add("X-Test-Role", roleCode);
            client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
            client.DefaultRequestHeaders.Add("X-Test-EmployeeId", employeeId);

            var def = Vinamilk.HRM.Application.Common.Security.RbacPolicyMatrix.GetRoleDefinition(roleCode)!;
            if (def.PermissionCodes.Count > 0)
            {
                client.DefaultRequestHeaders.Add("X-Test-Permission", string.Join(",", def.PermissionCodes));
            }
            return client;
        }

        // 1. EMPLOYEE (tung.pt / EmployeeId=1): 403 thực trên /Payroll, /Employees, /Security, /Payroll/RecordTrace; ESS chỉ xem được của mình
        var employeeClient = CreateRoleClient("EMPLOYEE", "10", "1", allowAutoRedirect: true);

        (await employeeClient.GetAsync("/Payroll")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await employeeClient.GetAsync("/Employees")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await employeeClient.GetAsync("/Security")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await employeeClient.GetAsync($"/Payroll/RecordTrace?recordId={emp1RecordId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var essOwnResp = await employeeClient.GetAsync("/ESS");
        essOwnResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var essOwnHtml = await essOwnResp.Content.ReadAsStringAsync();
        essOwnHtml.Should().Contain("PAY-2026-10");

        var essOtherResp = await employeeClient.GetAsync("/ESS?employeeId=2");
        essOtherResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var essOtherPdf = await employeeClient.GetAsync($"/ESS/Home/DownloadPayslip?payrollRecordId={emp2RecordId}");
        essOtherPdf.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var essOwnPdf = await employeeClient.GetAsync($"/ESS/Home/DownloadPayslip?payrollRecordId={emp1RecordId}");
        essOwnPdf.StatusCode.Should().Be(HttpStatusCode.OK);
        essOwnPdf.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");

        // 2. LINE_MANAGER: xem được /, /Employees, /Attendance, /Leaves, /Kpi; bị chặn 403 ở /Payroll, /Contracts, /Reports, /Security
        var lineMgrClient = CreateRoleClient("LINE_MANAGER", "20", "1", allowAutoRedirect: true);
        (await lineMgrClient.GetAsync("/")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await lineMgrClient.GetAsync("/Employees")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await lineMgrClient.GetAsync("/Attendance")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await lineMgrClient.GetAsync("/Leaves")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await lineMgrClient.GetAsync("/Kpi")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await lineMgrClient.GetAsync("/Payroll")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await lineMgrClient.GetAsync($"/Payroll/RecordTrace?recordId={emp1RecordId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await lineMgrClient.GetAsync("/Contracts")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await lineMgrClient.GetAsync("/Reports")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await lineMgrClient.GetAsync("/Security")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3. ACCOUNTANT: xem /Payroll, /Reports; bị chặn 403 khi Calculate / Approve / Lock / Security
        var accountantClient = CreateRoleClient("ACCOUNTANT", "30", "1", allowAutoRedirect: true);
        (await accountantClient.GetAsync("/Payroll")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await accountantClient.GetAsync("/Reports")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await accountantClient.PostAsync("/Payroll/Calculate", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await accountantClient.PostAsync("/Payroll/Approve", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await accountantClient.PostAsync("/Payroll/Lock", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await accountantClient.GetAsync("/Security")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 4. HR_STAFF: xem /Payroll, /Employees, /Contracts; bị chặn 403 khi Approve / Lock / Unlock / Security
        var hrStaffClient = CreateRoleClient("HR_STAFF", "40", "1", allowAutoRedirect: true);
        (await hrStaffClient.GetAsync("/Payroll")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await hrStaffClient.GetAsync("/Employees")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await hrStaffClient.GetAsync("/Contracts")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await hrStaffClient.PostAsync("/Payroll/Approve", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await hrStaffClient.PostAsync("/Payroll/Lock", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await hrStaffClient.PostAsync("/Payroll/Unlock", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await hrStaffClient.GetAsync("/Security")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 5. HR_MANAGER: xem /Payroll, /Reports, /AiExpert; bị chặn 403 khi Unlock / Security / Backup
        var hrMgrClient = CreateRoleClient("HR_MANAGER", "50", "1", allowAutoRedirect: true);
        (await hrMgrClient.GetAsync("/Payroll")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await hrMgrClient.GetAsync("/Reports")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await hrMgrClient.GetAsync("/AiExpert")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await hrMgrClient.PostAsync("/Payroll/Unlock", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await hrMgrClient.GetAsync("/Security")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Dùng factory tách riêng cho endpoint có EnableRateLimiting("backup-limiter") để tránh đụng quota với test trước
        await using var backupFactory = new CustomWebApplicationFactory();
        var isolatedHrMgr = backupFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true, HandleCookies = false });
        isolatedHrMgr.DefaultRequestHeaders.Add("X-Test-Role", "HR_MANAGER");
        isolatedHrMgr.DefaultRequestHeaders.Add("X-Test-UserId", "50");
        isolatedHrMgr.DefaultRequestHeaders.Add("X-Test-Permission", string.Join(",", Vinamilk.HRM.Application.Common.Security.RbacPolicyMatrix.GetRoleDefinition("HR_MANAGER")!.PermissionCodes));
        (await isolatedHrMgr.PostAsync("/api/v1/system/backup", null)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 6. SUPER_ADMIN: truy cập /Security, /Payroll và /api/v1/system/backup đều 200 OK
        var superAdminClient = CreateRoleClient("SUPER_ADMIN", "1", "1", allowAutoRedirect: true);
        (await superAdminClient.GetAsync("/Security")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await superAdminClient.GetAsync("/Payroll")).StatusCode.Should().Be(HttpStatusCode.OK);

        var isolatedSuperAdmin = backupFactory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true, HandleCookies = false });
        isolatedSuperAdmin.DefaultRequestHeaders.Add("X-Test-Role", "SUPER_ADMIN");
        isolatedSuperAdmin.DefaultRequestHeaders.Add("X-Test-UserId", "1");
        isolatedSuperAdmin.DefaultRequestHeaders.Add("X-Test-Permission", string.Join(",", Vinamilk.HRM.Application.Common.Security.RbacPolicyMatrix.GetRoleDefinition("SUPER_ADMIN")!.PermissionCodes));
        (await isolatedSuperAdmin.PostAsync("/api/v1/system/backup", null)).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

file sealed class StubUrlHelper : Microsoft.AspNetCore.Mvc.IUrlHelper
{
    public StubUrlHelper(Microsoft.AspNetCore.Mvc.ActionContext actionContext) => ActionContext = actionContext;
    public Microsoft.AspNetCore.Mvc.ActionContext ActionContext { get; }
    public string? Action(Microsoft.AspNetCore.Mvc.Routing.UrlActionContext actionContext) => $"/{actionContext.Controller}/{actionContext.Action}";
    public string? Content(string? contentPath) => contentPath;
    public bool IsLocalUrl(string? url) => !string.IsNullOrEmpty(url) && url.StartsWith('/');
    public string? Link(string? routeName, object? values) => "/";
    public string? RouteUrl(Microsoft.AspNetCore.Mvc.Routing.UrlRouteContext routeContext) => "/";
}

file sealed class StubCookieAuthService : IAuthenticationService
{
    public Task<AuthenticateResult> AuthenticateAsync(Microsoft.AspNetCore.Http.HttpContext context, string? scheme)
        => Task.FromResult(AuthenticateResult.NoResult());
    public Task ChallengeAsync(Microsoft.AspNetCore.Http.HttpContext context, string? scheme, AuthenticationProperties? properties)
        => Task.CompletedTask;
    public Task ForbidAsync(Microsoft.AspNetCore.Http.HttpContext context, string? scheme, AuthenticationProperties? properties)
        => Task.CompletedTask;
    public Task SignInAsync(Microsoft.AspNetCore.Http.HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        => Task.CompletedTask;
    public Task SignOutAsync(Microsoft.AspNetCore.Http.HttpContext context, string? scheme, AuthenticationProperties? properties)
        => Task.CompletedTask;
}

file sealed class StubTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
{
    private readonly Dictionary<string, object?> _data = new();
    public IDictionary<string, object?> LoadTempData(Microsoft.AspNetCore.Http.HttpContext context) => _data;
    public void SaveTempData(Microsoft.AspNetCore.Http.HttpContext context, IDictionary<string, object?> values)
    {
        _data.Clear();
        foreach (var kv in values) _data[kv.Key] = kv.Value;
    }
}

