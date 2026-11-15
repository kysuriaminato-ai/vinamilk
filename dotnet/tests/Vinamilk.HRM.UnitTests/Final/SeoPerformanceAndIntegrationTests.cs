using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Vinamilk.HRM.Application.Common.Helpers;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Talent;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Services;
using Vinamilk.HRM.Web.Controllers;
using Vinamilk.HRM.Web.Controllers.Api;
using Vinamilk.HRM.Web.HealthChecks;
using Xunit;

namespace Vinamilk.HRM.UnitTests.Final;

public class SeoPerformanceAndIntegrationTests
{
    [Theory]
    [InlineData("Kỹ sư Phần mềm .NET 8 (Đà Nẵng)", "ky-su-phan-mem-net-8-da-nang")]
    [InlineData("Chuyên viên Tuyển dụng & Đào tạo", "chuyen-vien-tuyen-dung-dao-tao")]
    [InlineData("   ", "tin-tuyen-dung")]
    public void SlugHelper_ToSlug_ShouldNormalizeVietnameseDiacritics(string input, string expected)
    {
        SlugHelper.ToSlug(input).Should().Be(expected);
    }

    [Fact]
    public async Task CareersApi_And_DatabaseBackup_And_HealthCheck_ShouldWorkWithInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"VnmTest_{Guid.NewGuid()}")
            .Options;

        await using var db = new ApplicationDbContext(options);

        var unit = new OrganizationUnit { Id = 1, UnitCode = "PB_IT", UnitName = "Phòng CNTT", IsActive = true };
        var pos = new Position { Id = 1, PositionCode = "DEV", PositionName = "Kỹ sư Phần mềm", IsActive = true };
        db.OrganizationUnits.Add(unit);
        db.Positions.Add(pos);
        db.JobPostings.Add(new JobPosting
        {
            Id = 10,
            PostingCode = "JOB-010",
            Title = "Chuyên viên Phát triển .NET 8",
            OrganizationUnitId = 1,
            PositionId = 1,
            Vacancies = 2,
            PublishDate = DateOnly.FromDateTime(DateTime.Today),
            ClosingDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            Status = "Open"
        });
        await db.SaveChangesAsync();

        var backupSvc = new DatabaseBackupService(db, new ConfigurationBuilder().Build(), NullLogger<DatabaseBackupService>.Instance);
        var tempDir = Path.Combine(Path.GetTempPath(), $"vnm_bak_{Guid.NewGuid():N}");
        var backupResult = await backupSvc.RunScheduledBackupAsync(tempDir);
        backupResult.Success.Should().BeTrue();
        File.Exists(backupResult.FilePath).Should().BeTrue();
        Directory.Delete(tempDir, recursive: true);

        var apiController = new CareersApiController(db);
        var okResult = await apiController.GetOpenJobs();
        okResult.Should().NotBeNull();

        var healthCheck = new DatabaseHealthCheck(db);
        var health = await healthCheck.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());
        health.Status.Should().Be(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy);
    }
}

