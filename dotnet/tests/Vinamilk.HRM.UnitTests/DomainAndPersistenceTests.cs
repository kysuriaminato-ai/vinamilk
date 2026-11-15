using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Persistence.Interceptors;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Vinamilk.HRM.Infrastructure.Security;
using Xunit;

namespace Vinamilk.HRM.UnitTests;

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
        var initializer = new ApplicationDbContextInitializer(
            NullLogger<ApplicationDbContextInitializer>.Instance,
            context,
            encryption);

        await initializer.SeedAsync();

        (await context.Roles.CountAsync()).Should().Be(6);
        (await context.OrganizationUnits.CountAsync()).Should().Be(32);
        (await context.Positions.CountAsync()).Should().Be(15);
        (await context.SalaryScales.CountAsync()).Should().Be(10);
        (await context.SalarySteps.CountAsync()).Should().Be(50);
        (await context.Employees.CountAsync()).Should().Be(55);
        (await context.LaborContracts.CountAsync()).Should().Be(55);
        (await context.PayrollRecords.CountAsync()).Should().Be(11);
        (await context.Users.CountAsync()).Should().Be(11);

        var systemJobUser = await context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Username == Vinamilk.HRM.Application.Common.Interfaces.SystemJobAccount.Username);
        systemJobUser.Should().NotBeNull();
        systemJobUser!.IsActive.Should().BeTrue();
        systemJobUser.UserRoles.Should().BeEmpty();
    }
}

