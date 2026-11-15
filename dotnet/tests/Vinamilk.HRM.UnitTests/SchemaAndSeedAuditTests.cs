using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Domain.Entities.Talent;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Persistence.Interceptors;
using Vinamilk.HRM.Infrastructure.Persistence.Seed;
using Vinamilk.HRM.Infrastructure.Security;
using Xunit;

namespace Vinamilk.HRM.UnitTests;

public class SchemaAndSeedAuditTests
{
    private static ApplicationDbContext CreateSqlServerModelOnlyContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=.;Database=VinamilkHRM_ModelAudit;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ApplicationDbContext CreateSeedInMemoryContext()
    {
        var interceptor = new AuditableEntityInterceptor(new FakeCurrentUserService());
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"SchemaAudit_{Guid.NewGuid():N}")
            .AddInterceptors(interceptor)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static int? ResolveConfiguredMaxLength(IProperty prop)
    {
        if (prop.GetMaxLength().HasValue)
            return prop.GetMaxLength();

        var colType = prop.GetColumnType();
        if (!string.IsNullOrWhiteSpace(colType))
        {
            var m = Regex.Match(colType, @"\((\d+)\)");
            if (m.Success && int.TryParse(m.Groups[1].Value, out var len))
                return len;
        }
        return null;
    }

    [Fact]
    public void KeyWidenedColumns_ShouldHaveSafeSqlServerMaxLengths()
    {
        using var sqlModelDb = CreateSqlServerModelOnlyContext();

        ResolveConfiguredMaxLength(sqlModelDb.Model.FindEntityType(typeof(EmployeeKpiEvaluation))!.FindProperty(nameof(EmployeeKpiEvaluation.RatingGrade))!)
            .Should().BeGreaterThanOrEqualTo(30);

        ResolveConfiguredMaxLength(sqlModelDb.Model.FindEntityType(typeof(EvaluationCycle))!.FindProperty(nameof(EvaluationCycle.CycleType))!)
            .Should().BeGreaterThanOrEqualTo(30);

        ResolveConfiguredMaxLength(sqlModelDb.Model.FindEntityType(typeof(Candidate))!.FindProperty(nameof(Candidate.Gender))!)
            .Should().BeGreaterThanOrEqualTo(20);

        ResolveConfiguredMaxLength(sqlModelDb.Model.FindEntityType(typeof(Domain.Entities.Core.Employee))!.FindProperty(nameof(Domain.Entities.Core.Employee.Gender))!)
            .Should().BeGreaterThanOrEqualTo(20);

        ResolveConfiguredMaxLength(sqlModelDb.Model.FindEntityType(typeof(AuditLog))!.FindProperty(nameof(AuditLog.ActionType))!)
            .Should().BeGreaterThanOrEqualTo(60);

        ResolveConfiguredMaxLength(sqlModelDb.Model.FindEntityType(typeof(AuditLog))!.FindProperty(nameof(AuditLog.EntityId))!)
            .Should().BeGreaterThanOrEqualTo(80);
    }

    [Fact]
    public async Task AllSeededEntities_ShouldNotExceedSqlServerColumnMaxLengths()
    {
        using var sqlModelDb = CreateSqlServerModelOnlyContext();
        using var db = CreateSeedInMemoryContext();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:PiiEncryptionKey"] = "SchemaAudit_Key_32Chars_Vinamilk!",
                ["Security:DevSamplePassword"] = "Vinamilk@2026"
            })
            .Build();

        var initializer = new ApplicationDbContextInitializer(
            NullLogger<ApplicationDbContextInitializer>.Instance,
            db,
            new PiiEncryptionService(config),
            new PasswordSecurityService(),
            new StubHostEnvironment(),
            config);

        await initializer.SeedAsync();

        var violations = new List<string>();
        var setMethod = typeof(DbContext).GetMethods()
            .First(m => m.Name == nameof(DbContext.Set) && m.IsGenericMethod && m.GetParameters().Length == 0);

        foreach (var entityType in sqlModelDb.Model.GetEntityTypes())
        {
            if (entityType.IsOwned() || entityType.ClrType.IsAbstract)
                continue;

            var boundedStringProps = entityType.GetProperties()
                .Where(p => p.ClrType == typeof(string) && p.PropertyInfo != null)
                .Select(p => new { Prop = p, MaxLen = ResolveConfiguredMaxLength(p) })
                .Where(x => x.MaxLen.HasValue)
                .ToList();

            if (boundedStringProps.Count == 0)
                continue;

            var genericSet = setMethod.MakeGenericMethod(entityType.ClrType);
            var queryable = (IQueryable)genericSet.Invoke(db, null)!;

            foreach (var row in queryable)
            {
                foreach (var bp in boundedStringProps)
                {
                    var val = bp.Prop.PropertyInfo!.GetValue(row) as string;
                    if (val != null && val.Length > bp.MaxLen!.Value)
                    {
                        violations.Add($"{entityType.ClrType.Name}.{bp.Prop.Name}: length {val.Length} > MaxLength {bp.MaxLen} (value='{val[..Math.Min(40, val.Length)]}')");
                    }
                }
            }
        }

        // Also validate extended rating labels and audit action codes that previously truncated at varchar(10)/varchar(30)
        string[] kpiGrades = { "A+", "A", "B+", "B", "C", "D", "Xuất sắc (A)", "Hoàn thành tốt (B)", "Vượt kỳ vọng" };
        int ratingMax = ResolveConfiguredMaxLength(sqlModelDb.Model.FindEntityType(typeof(EmployeeKpiEvaluation))!.FindProperty(nameof(EmployeeKpiEvaluation.RatingGrade))!)!.Value;
        kpiGrades.Should().OnlyContain(g => g.Length <= ratingMax);

        violations.Should().BeEmpty("tất cả dữ liệu seed không được vượt quá MaxLength của cột SQL Server");
    }
}
