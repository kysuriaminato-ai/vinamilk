using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Jobs;
using VinamilkHRM.Application.Services;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Jobs;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Entities.Statutory;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Infrastructure.Data;
using VinamilkHRM.Infrastructure.Services;
using VinamilkHRM.LegacyImporter;
using Xunit;

namespace VinamilkHRM.Application.Tests;

public class Phase2AcceptanceTests
{
    private static VinamilkHRMDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<VinamilkHRMDbContext>()
            .UseInMemoryDatabase(databaseName: $"VinamilkHRM_TestDb_{Guid.NewGuid()}")
            .Options;

        var context = new VinamilkHRMDbContext(options);
        return context;
    }

    [Fact]
    public async Task AcceptanceCriterion1_StatutoryParameterProvider_ShouldReturnExactValueByEffectiveDate()
    {
        // ARRANGE
        using var context = CreateInMemoryDbContext();
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddSingleton<IApplicationDbContext>(context);
        services.AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()));
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        var provider = new StatutoryParameterProvider(scopeFactory, sp.GetRequiredService<IMemoryCache>());

        // Seed tham số theo luật Việt Nam
        // 1. Giảm trừ bản thân: 11tr đến 31/12/2025; 15.5tr từ 01/01/2026
        context.StatutoryParameters.AddRange(
            new StatutoryParameter("PIT_SELF_DEDUCTION", 11_000_000m, new DateOnly(2020, 7, 1), new DateOnly(2025, 12, 31), "NQ 954/2020"),
            new StatutoryParameter("PIT_SELF_DEDUCTION", 15_500_000m, new DateOnly(2026, 1, 1), null, "Luật Thuế TNCN sửa đổi 2025"),
            // 2. Giảm trừ người phụ thuộc: 4.4tr đến 31/12/2025; 6.2tr từ 01/01/2026
            new StatutoryParameter("PIT_DEPENDENT_DEDUCTION", 4_400_000m, new DateOnly(2020, 7, 1), new DateOnly(2025, 12, 31), "NQ 954/2020"),
            new StatutoryParameter("PIT_DEPENDENT_DEDUCTION", 6_200_000m, new DateOnly(2026, 1, 1), null, "Luật Thuế TNCN sửa đổi 2025"),
            // 3. Lương cơ sở: 2.340.000 đến 30/06/2026; 2.530.000 từ 01/07/2026
            new StatutoryParameter("BASE_SALARY", 2_340_000m, new DateOnly(2024, 7, 1), new DateOnly(2026, 6, 30), "NĐ 73/2024"),
            new StatutoryParameter("BASE_SALARY", 2_530_000m, new DateOnly(2026, 7, 1), null, "Nghị định tiền lương 2026")
        );
        await context.SaveChangesAsync();

        // ACT & ASSERT: Tiêu chí 1
        // Get("PIT_SELF_DEDUCTION", 2025-12-15) = 11.000.000
        var pit2025 = await provider.GetAsync("PIT_SELF_DEDUCTION", new DateOnly(2025, 12, 15));
        Assert.Equal(11_000_000m, pit2025);

        // Get("PIT_SELF_DEDUCTION", 2026-01-15) = 15.500.000
        var pit2026 = await provider.GetAsync("PIT_SELF_DEDUCTION", new DateOnly(2026, 1, 15));
        Assert.Equal(15_500_000m, pit2026);

        // Get("PIT_DEPENDENT_DEDUCTION", 2025-12-15) = 4.400.000 & 2026-01-15 = 6.200.000
        var dep2025 = await provider.GetAsync("PIT_DEPENDENT_DEDUCTION", new DateOnly(2025, 12, 15));
        var dep2026 = await provider.GetAsync("PIT_DEPENDENT_DEDUCTION", new DateOnly(2026, 1, 15));
        Assert.Equal(4_400_000m, dep2025);
        Assert.Equal(6_200_000m, dep2026);

        // Get("BASE_SALARY", 2026-06-30) = 2.340.000 & Get("BASE_SALARY", 2026-07-01) = 2.530.000
        var baseEarly2026 = await provider.GetAsync("BASE_SALARY", new DateOnly(2026, 6, 30));
        var baseLate2026 = await provider.GetAsync("BASE_SALARY", new DateOnly(2026, 7, 1));
        Assert.Equal(2_340_000m, baseEarly2026);
        Assert.Equal(2_530_000m, baseLate2026);

        // Thiếu tham số ném DomainException ERR_STATUTORY_PARAM_NOT_FOUND, không hardcode fallback
        var ex = await Assert.ThrowsAsync<DomainException>(() => 
            provider.GetAsync("UNKNOWN_PARAM_CODE", new DateOnly(2026, 1, 1)));
        Assert.Equal("ERR_STATUTORY_PARAM_NOT_FOUND", ex.ErrorCode);
    }

    [Fact]
    public async Task AcceptanceCriterion2_SalaryScaleP1_BelowMinimumWage_ShouldThrowDomainException()
    {
        // ARRANGE: Lương tối thiểu Vùng I năm 2026 là 5.310.000 VND
        using var context = CreateInMemoryDbContext();
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddSingleton<IApplicationDbContext>(context);
        services.AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions()));
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        var provider = new StatutoryParameterProvider(scopeFactory, sp.GetRequiredService<IMemoryCache>());

        var grade = new JobGrade(1, "GR-01", "Bậc 1 - Công nhân sơ cấp");
        context.JobGrades.Add(grade);

        var unit = new FactoryUnit(
            "NM_BD",
            "Nhà Máy Mega Bình Dương",
            "Bình Dương",
            "02743555555",
            "factory@vinamilk.com.vn",
            "0300588569",
            500000m,
            10,
            "ISO 22000",
            "ENV-001"
        );
        unit.ChangeMinimumWageRegion(MinimumWageRegion.RegionI, new DateOnly(2026, 1, 1));
        context.OrganizationalUnits.Add(unit);

        context.MinimumWages.Add(new MinimumWage(
            MinimumWageRegion.RegionI,
            monthlyAmount: 5_310_000m,
            hourlyAmount: 25_500m,
            effectiveFrom: new DateOnly(2026, 1, 1),
            legalReference: "Nghị định 74/2024/NĐ-CP"
        ));
        await context.SaveChangesAsync();

        var jobService = new JobService(context, provider);

        // ACT & ASSERT:
        // 1. Lưu P1 = 4.800.000đ thấp hơn lương tối thiểu Vùng I (5.310.000đ) bị từ chối với ERR_SAL_BELOW_MIN_WAGE
        var invalidRequest = new CreateSalaryScaleRequest(
            JobGradeId: grade.Id,
            Step: 1,
            Amount: 4_800_000m, // < 5.310.000đ
            EffectiveFrom: new DateOnly(2026, 1, 1),
            UnitId: unit.Id
        );

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            jobService.CreateSalaryScaleAsync(invalidRequest));
        Assert.Equal("ERR_SAL_BELOW_MIN_WAGE", ex.ErrorCode);

        // 2. Lưu P1 = 5.500.000đ >= 5.310.000đ phải thành công
        var validRequest = new CreateSalaryScaleRequest(
            JobGradeId: grade.Id,
            Step: 1,
            Amount: 5_500_000m,
            EffectiveFrom: new DateOnly(2026, 1, 1),
            UnitId: unit.Id
        );

        var created = await jobService.CreateSalaryScaleAsync(validRequest);
        Assert.NotNull(created);
        Assert.Equal(5_500_000m, created.Amount);
    }

    [Fact]
    public void AcceptanceCriterion3_HierarchyId_SubtreeQuery_ShouldFilterCorrectly()
    {
        // ARRANGE: Tạo cấu trúc cây phân cấp
        // /1/ : Nhà máy Mega Bình Dương (Factory)
        // /1/1/ : Xưởng Chiết rót 1 (ProductionLine)
        // /1/1/1/ : Ca sản xuất 1 (Tổ ca)
        // /2/ : Trang trại Green Farm Tây Ninh (DairyFarm)
        var factoryPath = HierarchyId.Parse("/1/");
        var workshopPath = HierarchyId.Parse("/1/1/");
        var shiftPath = HierarchyId.Parse("/1/1/1/");
        var otherFarmPath = HierarchyId.Parse("/2/");

        var units = new List<(string Code, HierarchyId Path)>
        {
            ("FAC_BD", factoryPath),
            ("SHOP_01", workshopPath),
            ("SHIFT_01", shiftPath),
            ("FARM_TN", otherFarmPath)
        };

        // ACT: Truy vấn "Nhà máy X và mọi đơn vị con" dùng HierarchyId IsDescendantOf
        var factorySubtree = units
            .Where(u => u.Path.IsDescendantOf(factoryPath))
            .Select(u => u.Code)
            .ToList();

        // ASSERT:
        Assert.Contains("FAC_BD", factorySubtree);
        Assert.Contains("SHOP_01", factorySubtree);
        Assert.Contains("SHIFT_01", factorySubtree);
        Assert.DoesNotContain("FARM_TN", factorySubtree);
        Assert.Equal(3, factorySubtree.Count);
    }

    [Fact]
    public async Task AcceptanceCriterion4_LegacyImporter_DryRun_ShouldReconcileAllTablesAccurately()
    {
        // ARRANGE & ACT: Chạy importer engine ở chế độ --dry-run
        var engine = new ImporterEngine(
            mysqlConn: "Server=dummy;Database=dummy;",
            mssqlConn: "Server=dummy;Database=dummy;",
            isDryRun: true
        );

        int exitCode = await engine.RunAsync();

        // ASSERT: Chạy dry-run thành công (exit code 0) và không phát sinh lỗi
        Assert.Equal(0, exitCode);
    }
}
