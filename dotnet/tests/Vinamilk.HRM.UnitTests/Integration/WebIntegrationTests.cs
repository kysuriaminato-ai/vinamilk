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
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Talent;
using Vinamilk.HRM.Infrastructure.Persistence;
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
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Name, "test-user"),
            new(ClaimTypes.Role, role)
        };

        if (Request.Headers.TryGetValue("X-Test-Permission", out var perm))
            claims.Add(new Claim("Permission", perm.ToString()));

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
}

