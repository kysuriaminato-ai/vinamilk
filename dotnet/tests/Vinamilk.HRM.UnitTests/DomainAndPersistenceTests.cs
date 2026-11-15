using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Persistence.Interceptors;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Vinamilk.HRM.Infrastructure.Security;
using Xunit;

namespace Vinamilk.HRM.UnitTests;

public sealed class StubHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "Vinamilk.HRM.UnitTests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

public sealed class FakeCurrentUserService : ICurrentUserService
{
    public int? UserId => 1;
    public string Username => "huyen.ntt";
    public int? EmployeeId => 20;
    public string? IpAddress => "10.0.0.15";
    public string? UserAgent => "xUnit-Test-Runner";
    public bool IsAuthenticated => true;
    public bool HasPermission(string permissionCode) => true;
}

public class DomainAndPersistenceTests
{
    private static ApplicationDbContext CreateInMemoryContext(string dbName)
    {
        var currentUser = new FakeCurrentUserService();
        var interceptor = new AuditableEntityInterceptor(currentUser);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(interceptor)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public void PiiEncryptionService_Should_Encrypt_Decrypt_And_Hash_Deterministically()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:PiiEncryptionKey"] = "TestSecretKey_Vinamilk_2026"
            })
            .Build();

        var service = new PiiEncryptionService(config);
        var rawCccd = "079075012345";

        var encrypted = service.Encrypt(rawCccd);
        encrypted.Should().NotBeNullOrWhiteSpace();
        encrypted.Should().NotBe(rawCccd);

        var decrypted = service.Decrypt(encrypted);
        decrypted.Should().Be(rawCccd);

        var hash1 = service.ComputeDeterministicHash(rawCccd);
        var hash2 = service.ComputeDeterministicHash(" 079075012345 ");
        hash1.Should().HaveLength(64);
        hash1.Should().Be(hash2);
    }

    [Fact]
    public async Task AuditableEntityInterceptor_Should_Set_AuditFields_SoftDelete_And_Write_AuditLog()
    {
        using var context = CreateInMemoryContext(Guid.NewGuid().ToString());

        var unit = new OrganizationUnit
        {
            UnitCode = "TEST_DV",
            UnitName = "Phòng Kiểm thử Tự động"
        };

        await context.OrganizationUnits.AddAsync(unit);
        await context.SaveChangesAsync();

        unit.CreatedBy.Should().Be("huyen.ntt");
        unit.IsDeleted.Should().BeFalse();

        // Cập nhật
        unit.UnitName = "Phòng Kiểm thử Tự động (Đã cập nhật)";
        await context.SaveChangesAsync();

        unit.UpdatedBy.Should().Be("huyen.ntt");
        unit.UpdatedAt.Should().NotBeNull();

        // Xóa -> Chuyển thành Soft Delete
        context.OrganizationUnits.Remove(unit);
        await context.SaveChangesAsync();

        // Global Query Filter ẩn bản ghi đã xóa mềm
        var visibleCount = await context.OrganizationUnits.CountAsync();
        visibleCount.Should().Be(0);

        // IgnoreQueryFilters vẫn thấy bản ghi với IsDeleted = true
        var deletedEntity = await context.OrganizationUnits.IgnoreQueryFilters().FirstAsync();
        deletedEntity.IsDeleted.Should().BeTrue();

        // Kiểm tra AuditLogs đã ghi đủ 3 sự kiện (Create, Update, Delete)
        var logs = await context.AuditLogs.OrderBy(a => a.Id).ToListAsync();
        logs.Should().HaveCount(3);
        logs.Select(l => l.ActionType).Should().ContainInOrder("Create", "Update", "Delete");
    }

    [Fact]
    public async Task ApplicationDbContextInitializer_Should_Seed_All_Roles_Units_And_55_Employees()
    {
        using var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Security:PiiEncryptionKey"] = "SeedTest_Key_32Chars_Vinamilk!" })
            .Build();
        var encryption = new PiiEncryptionService(config);
        var passwordService = new PasswordSecurityService();
        var devEnv = new StubHostEnvironment { EnvironmentName = Environments.Development };
        var initializer = new ApplicationDbContextInitializer(
            NullLogger<ApplicationDbContextInitializer>.Instance,
            context,
            encryption,
            passwordService,
            devEnv,
            config);

        await initializer.SeedAsync();

        (await context.Roles.CountAsync()).Should().Be(6);
        (await context.OrganizationUnits.CountAsync()).Should().Be(32);
        (await context.Positions.CountAsync()).Should().Be(15);
        (await context.SalaryScales.CountAsync()).Should().Be(10);
        (await context.SalarySteps.CountAsync()).Should().Be(50);
        (await context.Employees.CountAsync()).Should().Be(55);
        (await context.LaborContracts.CountAsync()).Should().Be(55);
        (await context.PayrollRecords.CountAsync()).Should().Be(11);

        var allUsers = await context.Users.Include(u => u.UserRoles).ToListAsync();
        allUsers.Should().HaveCount(11);
        allUsers.Should().OnlyContain(u => u.MustChangePassword);
        allUsers.Should().OnlyContain(u => u.PasswordHash.StartsWith("PBKDF2$"));

        var adminUser = allUsers.Single(u => u.Username == "admin");
        adminUser.IsSystemAccount.Should().BeFalse();
        passwordService.VerifyPassword("Vinamilk@2026", adminUser.PasswordHash).Should().BeTrue();
        passwordService.VerifyPassword("Vinamilk@2026!", adminUser.PasswordHash).Should().BeFalse();

        var systemJobUser = allUsers.Single(u => u.Username == SystemJobAccount.Username);
        systemJobUser.IsActive.Should().BeTrue();
        systemJobUser.IsSystemAccount.Should().BeTrue();
        systemJobUser.UserRoles.Should().BeEmpty();
    }

    [Fact]
    public async Task Production_Seed_Should_Require_SuperAdmin_EnvVar_And_Not_Use_DemoPassword()
    {
        var prevEnv = Environment.GetEnvironmentVariable(ApplicationDbContextInitializer.InitialSuperAdminPasswordEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(ApplicationDbContextInitializer.InitialSuperAdminPasswordEnvVar, null);

            using var contextNoEnv = CreateInMemoryContext(Guid.NewGuid().ToString());
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Security:PiiEncryptionKey"] = "SeedTest_Key_32Chars_Vinamilk!" })
                .Build();
            var encryption = new PiiEncryptionService(config);
            var passwordService = new PasswordSecurityService();
            var prodEnv = new StubHostEnvironment { EnvironmentName = Environments.Production };

            var initializerMissing = new ApplicationDbContextInitializer(
                NullLogger<ApplicationDbContextInitializer>.Instance,
                contextNoEnv,
                encryption,
                passwordService,
                prodEnv,
                config);

            Func<Task> seedWithoutEnv = () => initializerMissing.SeedAsync();
            await seedWithoutEnv.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage($"*{ApplicationDbContextInitializer.InitialSuperAdminPasswordEnvVar}*");

            // Khi có biến môi trường -> Seed thành công SuperAdmin bằng mật khẩu env, không dùng mật khẩu mẫu cho user khác
            const string prodSecret = "Prod_Super@2026_Secure!";
            Environment.SetEnvironmentVariable(ApplicationDbContextInitializer.InitialSuperAdminPasswordEnvVar, prodSecret);

            using var contextWithEnv = CreateInMemoryContext(Guid.NewGuid().ToString());
            var initializerWithEnv = new ApplicationDbContextInitializer(
                NullLogger<ApplicationDbContextInitializer>.Instance,
                contextWithEnv,
                encryption,
                passwordService,
                prodEnv,
                config);

            await initializerWithEnv.SeedAsync();

            var admin = await contextWithEnv.Users.SingleAsync(u => u.Username == "admin");
            var hrUser = await contextWithEnv.Users.SingleAsync(u => u.Username == "huyen.ntt");

            admin.MustChangePassword.Should().BeTrue();
            hrUser.MustChangePassword.Should().BeTrue();
            passwordService.VerifyPassword(prodSecret, admin.PasswordHash).Should().BeTrue();
            passwordService.VerifyPassword("Vinamilk@2026", admin.PasswordHash).Should().BeFalse();
            passwordService.VerifyPassword("Vinamilk@2026", hrUser.PasswordHash).Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable(ApplicationDbContextInitializer.InitialSuperAdminPasswordEnvVar, prevEnv);
        }
    }
}

