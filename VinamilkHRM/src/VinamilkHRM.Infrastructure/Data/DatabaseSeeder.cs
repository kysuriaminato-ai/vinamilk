using VinamilkHRM.Domain.Entities.Kpis;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VinamilkHRM.Application.Common.Security;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Jobs;
using VinamilkHRM.Domain.Entities.Lookups;
using VinamilkHRM.Domain.Entities.Shifts;
using VinamilkHRM.Domain.Entities.Statutory;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Infrastructure.Identity;

namespace VinamilkHRM.Infrastructure.Data;

/// <summary>
/// Trình khởi tạo dữ liệu mẫu (Seeder) cho 10 Vai trò doanh nghiệp, tài khoản demo và toàn bộ tham số pháp lý,
/// thang bảng lương 3P, chức danh ngạch bậc và danh mục dùng chung Phiên 2.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAllAsync(IServiceProvider serviceProvider)
    {
        await SeedIdentityAsync(serviceProvider);
        await SeedPhase2DataAsync(serviceProvider);
        await SeedKpiDataAsync(serviceProvider);
    }

    public static async Task SeedIdentityAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseSeeder));

        // 1. Tạo 10 vai trò chuẩn nếu chưa có
        var roleDefinitions = new Dictionary<string, (string Description, string[] Permissions)>
        {
            [Roles.SystemAdmin] = ("Quản trị viên toàn quyền hệ thống Vinamilk HRM", Permissions.All.ToArray()),
            [Roles.Executive] = ("Ban Tổng Giám đốc (CEO, Phó TGĐ)", new[]
            {
                Permissions.NhansuView, Permissions.ChamcongView, Permissions.LuongView, Permissions.LuongApprove,
                Permissions.KpiView, Permissions.KpiApprove, Permissions.DonviView, Permissions.TuyendungView,
                Permissions.TuyendungApprove, Permissions.KtklView, Permissions.KtklApprove, Permissions.BaocaoView,
                Permissions.BaocaoExport, Permissions.HethongView, Permissions.NhansuViewSensitive
            }),
            [Roles.HRDirector] = ("Giám đốc Nhân sự Tập đoàn", new[]
            {
                Permissions.NhansuView, Permissions.NhansuAdd, Permissions.NhansuEdit, Permissions.NhansuDelete, Permissions.NhansuExport, Permissions.NhansuViewSensitive,
                Permissions.ChamcongView, Permissions.ChamcongAdd, Permissions.ChamcongEdit, Permissions.ChamcongApprove, Permissions.ChamcongExport,
                Permissions.LuongView, Permissions.LuongApprove, Permissions.LuongExport,
                Permissions.KpiView, Permissions.KpiEvaluate, Permissions.KpiApprove,
                Permissions.DonviView, Permissions.DonviEdit,
                Permissions.TuyendungView, Permissions.TuyendungAdd, Permissions.TuyendungEdit, Permissions.TuyendungDelete, Permissions.TuyendungApprove,
                Permissions.DaotaoView, Permissions.DaotaoAdd, Permissions.DaotaoEdit,
                Permissions.KtklView, Permissions.KtklAdd, Permissions.KtklApprove,
                Permissions.BaocaoView, Permissions.BaocaoExport
            }),
            [Roles.HRManager] = ("Trưởng phòng Nhân sự Nhà máy / Trang trại", new[]
            {
                Permissions.NhansuView, Permissions.NhansuAdd, Permissions.NhansuEdit, Permissions.NhansuExport, Permissions.NhansuViewSensitive,
                Permissions.ChamcongView, Permissions.ChamcongAdd, Permissions.ChamcongEdit, Permissions.ChamcongApprove, Permissions.ChamcongExport,
                Permissions.KpiView, Permissions.KpiEvaluate, Permissions.DonviView,
                Permissions.TuyendungView, Permissions.TuyendungAdd, Permissions.TuyendungEdit,
                Permissions.DaotaoView, Permissions.DaotaoAdd, Permissions.DaotaoEdit,
                Permissions.KtklView, Permissions.KtklAdd, Permissions.BaocaoView
            }),
            [Roles.HRSpecialist] = ("Chuyên viên C&B / Tuyển dụng / Đào tạo", new[]
            {
                Permissions.NhansuView, Permissions.NhansuAdd, Permissions.NhansuEdit, Permissions.NhansuExport,
                Permissions.ChamcongView, Permissions.ChamcongAdd, Permissions.ChamcongEdit,
                Permissions.TuyendungView, Permissions.TuyendungAdd, Permissions.TuyendungEdit,
                Permissions.DaotaoView, Permissions.DaotaoAdd, Permissions.DaotaoEdit,
                Permissions.BaocaoView
            }),
            [Roles.ChiefAccountant] = ("Kế toán trưởng Tập đoàn", new[]
            {
                Permissions.LuongView, Permissions.LuongApprove, Permissions.LuongExport, Permissions.NhansuViewSensitive,
                Permissions.ChamcongView, Permissions.BaocaoView, Permissions.BaocaoExport
            }),
            [Roles.PayrollAccountant] = ("Kế toán tiền lương & Chế độ phúc lợi", new[]
            {
                Permissions.LuongView, Permissions.LuongAdd, Permissions.LuongEdit, Permissions.LuongExport, Permissions.NhansuViewSensitive,
                Permissions.ChamcongView, Permissions.ChamcongExport, Permissions.BaocaoView
            }),
            [Roles.UnitManager] = ("Giám đốc Đơn vị (Nhà máy, Trang trại, Chi nhánh)", new[]
            {
                Permissions.NhansuView, Permissions.ChamcongView, Permissions.ChamcongApprove,
                Permissions.KpiView, Permissions.KpiEvaluate, Permissions.KpiApprove,
                Permissions.KtklView, Permissions.KtklAdd, Permissions.BaocaoView
            }),
            [Roles.TeamLeader] = ("Tổ trưởng sản xuất / Trưởng ca vận hành", new[]
            {
                Permissions.NhansuView, Permissions.ChamcongView, Permissions.ChamcongEdit,
                Permissions.KpiView, Permissions.KpiEvaluate
            }),
            [Roles.Employee] = ("Cán bộ nhân viên / Công nhân (Tự phục vụ ESS)", new[]
            {
                Permissions.NhansuView, Permissions.ChamcongView, Permissions.LuongView, Permissions.KpiView
            })
        };

        foreach (var (roleName, (description, permissions)) in roleDefinitions)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                role = new ApplicationRole(roleName, description);
                await roleManager.CreateAsync(role);
            }

            var existingClaims = await roleManager.GetClaimsAsync(role);
            foreach (var permission in permissions)
            {
                if (!existingClaims.Any(c => c.Type == "permission" && c.Value == permission))
                {
                    await roleManager.AddClaimAsync(role, new Claim("permission", permission));
                }
            }
        }

        // 2. Tạo 10 tài khoản demo
        var userSeeds = new (string Username, string FullName, string Email, string Role, int? EmployeeId)[]
        {
            ("admin", "Quản trị viên Hệ thống", "admin@vinamilk.com.vn", Roles.SystemAdmin, null),
            ("executive", "Mai Kiều Liên (TGĐ)", "executive@vinamilk.com.vn", Roles.Executive, 1),
            ("hr_director", "Trần Minh Tâm (GĐ Nhân sự)", "hr_director@vinamilk.com.vn", Roles.HRDirector, 2),
            ("hr_manager", "Lê Hoàng Yến (Trưởng phòng HR)", "hr_manager@vinamilk.com.vn", Roles.HRManager, 3),
            ("hr_specialist", "Vũ Phương Linh (Chuyên viên C&B)", "hr_specialist@vinamilk.com.vn", Roles.HRSpecialist, 4),
            ("chief_accountant", "Nguyễn Văn Tuấn (Kế toán trưởng)", "chief_accountant@vinamilk.com.vn", Roles.ChiefAccountant, 5),
            ("payroll_accountant", "Hoàng Kim Oanh (Kế toán lương)", "payroll_accountant@vinamilk.com.vn", Roles.PayrollAccountant, 6),
            ("unit_manager", "Đặng Quốc Hưng (GĐ Nhà máy)", "unit_manager@vinamilk.com.vn", Roles.UnitManager, 7),
            ("team_leader", "Bùi Thanh Phong (Tổ trưởng ca)", "team_leader@vinamilk.com.vn", Roles.TeamLeader, 8),
            ("employee", "Nguyễn Văn An (Công nhân SX)", "employee@vinamilk.com.vn", Roles.Employee, 9)
        };

        foreach (var (username, fullName, email, roleName, employeeId) in userSeeds)
        {
            var existingUser = await userManager.FindByNameAsync(username);
            if (existingUser == null)
            {
                string temporaryPassword = GenerateSecurePassword();

                var newUser = new ApplicationUser
                {
                    UserName = username,
                    FullName = fullName,
                    Email = email,
                    EmailConfirmed = true,
                    EmployeeId = employeeId,
                    MustChangePassword = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var createResult = await userManager.CreateAsync(newUser, temporaryPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(newUser, roleName);
                }
            }
        }
    }

    public static async Task SeedPhase2DataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VinamilkHRMDbContext>();

        // 1. Tham số pháp lý (StatutoryParameters)
        if (!await db.StatutoryParameters.AnyAsync())
        {
            var statParams = new List<StatutoryParameter>
            {
                // Giảm trừ gia cảnh bản thân
                new("PIT_SELF_DEDUCTION", 11_000_000m, new DateOnly(2020, 7, 1), new DateOnly(2025, 12, 31), "Nghị quyết 954/2020/UBTVQH14", "Giảm trừ gia cảnh bản thân giai đoạn 2020-2025"),
                new("PIT_SELF_DEDUCTION", 15_500_000m, new DateOnly(2026, 1, 1), null, "Luật Thuế TNCN sửa đổi 2025/2026", "Giảm trừ gia cảnh bản thân từ năm 2026"),

                // Giảm trừ người phụ thuộc
                new("PIT_DEPENDENT_DEDUCTION", 4_400_000m, new DateOnly(2020, 7, 1), new DateOnly(2025, 12, 31), "Nghị quyết 954/2020/UBTVQH14", "Giảm trừ gia cảnh người phụ thuộc giai đoạn 2020-2025"),
                new("PIT_DEPENDENT_DEDUCTION", 6_200_000m, new DateOnly(2026, 1, 1), null, "Luật Thuế TNCN sửa đổi 2025/2026", "Giảm trừ gia cảnh người phụ thuộc từ năm 2026"),

                // Mức lương cơ sở
                new("BASE_SALARY", 2_340_000m, new DateOnly(2024, 7, 1), new DateOnly(2026, 6, 30), "Nghị định 73/2024/NĐ-CP", "Mức lương cơ sở từ 01/07/2024 đến 30/06/2026"),
                new("BASE_SALARY", 2_530_000m, new DateOnly(2026, 7, 1), null, "Nghị định điều chỉnh lương cơ sở 2026", "Mức lương cơ sở áp dụng từ 01/07/2026"),

                // Trần đóng bảo hiểm (20 lần lương cơ sở đối với BHXH/BHYT)
                new("MAX_INSURANCE_BASE_MULTIPLIER", 20m, new DateOnly(2020, 1, 1), null, "Luật BHXH 2014 & Luật BHYT", "Mức trần đóng BHXH/BHYT bằng 20 lần mức lương cơ sở"),

                // Hạn mức miễn thuế phụ cấp ăn ca
                new("LUNCH_ALLOWANCE_TAX_EXEMPT_CAP", 730_000m, new DateOnly(2020, 1, 1), null, "Thông tư 25/2018/TT-BTC", "Tiền ăn giữa ca được miễn thuế tối đa 730.000 đ/tháng")
            };
            db.StatutoryParameters.AddRange(statParams);
        }

        // 2. Lương tối thiểu vùng (MinimumWages)
        if (!await db.MinimumWages.AnyAsync())
        {
            var minWages = new List<MinimumWage>
            {
                // Giai đoạn 2024 - 2025 (Nghị định 74/2024/NĐ-CP)
                new(MinimumWageRegion.RegionI, 4_960_000m, 23_800m, new DateOnly(2024, 7, 1), new DateOnly(2025, 12, 31), "Nghị định 74/2024/NĐ-CP"),
                new(MinimumWageRegion.RegionII, 4_410_000m, 21_200m, new DateOnly(2024, 7, 1), new DateOnly(2025, 12, 31), "Nghị định 74/2024/NĐ-CP"),
                new(MinimumWageRegion.RegionIII, 3_860_000m, 18_600m, new DateOnly(2024, 7, 1), new DateOnly(2025, 12, 31), "Nghị định 74/2024/NĐ-CP"),
                new(MinimumWageRegion.RegionIV, 3_450_000m, 16_600m, new DateOnly(2024, 7, 1), new DateOnly(2025, 12, 31), "Nghị định 74/2024/NĐ-CP"),

                // Giai đoạn 2026 trở đi (Luật & Nghị định mới năm 2026)
                new(MinimumWageRegion.RegionI, 5_310_000m, 25_500m, new DateOnly(2026, 1, 1), null, "Nghị định 293/2025/NĐ-CP"),
                new(MinimumWageRegion.RegionII, 4_730_000m, 22_700m, new DateOnly(2026, 1, 1), null, "Nghị định 293/2025/NĐ-CP"),
                new(MinimumWageRegion.RegionIII, 4_140_000m, 19_900m, new DateOnly(2026, 1, 1), null, "Nghị định 293/2025/NĐ-CP"),
                new(MinimumWageRegion.RegionIV, 3_700_000m, 17_800m, new DateOnly(2026, 1, 1), null, "Nghị định 293/2025/NĐ-CP")
            };
            db.MinimumWages.AddRange(minWages);
        }

        // 3. Biểu thuế TNCN lũy tiến từng phần (TaxBrackets)
        if (!await db.TaxBrackets.AnyAsync())
        {
            var taxBrackets = new List<TaxBracket>
            {
                // Biểu 7 bậc cũ áp dụng đến 31/12/2025
                new("PIT_REGULAR", 1, 5_000_000m, 0.05m, new DateOnly(2009, 1, 1), new DateOnly(2025, 12, 31), "Đến 5 triệu đồng"),
                new("PIT_REGULAR", 2, 10_000_000m, 0.10m, new DateOnly(2009, 1, 1), new DateOnly(2025, 12, 31), "Trên 5 đến 10 triệu đồng"),
                new("PIT_REGULAR", 3, 18_000_000m, 0.15m, new DateOnly(2009, 1, 1), new DateOnly(2025, 12, 31), "Trên 10 đến 18 triệu đồng"),
                new("PIT_REGULAR", 4, 32_000_000m, 0.20m, new DateOnly(2009, 1, 1), new DateOnly(2025, 12, 31), "Trên 18 đến 32 triệu đồng"),
                new("PIT_REGULAR", 5, 52_000_000m, 0.25m, new DateOnly(2009, 1, 1), new DateOnly(2025, 12, 31), "Trên 32 đến 52 triệu đồng"),
                new("PIT_REGULAR", 6, 80_000_000m, 0.30m, new DateOnly(2009, 1, 1), new DateOnly(2025, 12, 31), "Trên 52 đến 80 triệu đồng"),
                new("PIT_REGULAR", 7, null, 0.35m, new DateOnly(2009, 1, 1), new DateOnly(2025, 12, 31), "Trên 80 triệu đồng"),

                // Biểu 5 bậc mới rút gọn áp dụng từ 01/01/2026
                new("PIT_REGULAR", 1, 10_000_000m, 0.05m, new DateOnly(2026, 1, 1), null, "Bậc 1: Đến 10 triệu đồng (5%)"),
                new("PIT_REGULAR", 2, 30_000_000m, 0.10m, new DateOnly(2026, 1, 1), null, "Bậc 2: Trên 10 đến 30 triệu đồng (10%)"),
                new("PIT_REGULAR", 3, 60_000_000m, 0.20m, new DateOnly(2026, 1, 1), null, "Bậc 3: Trên 30 đến 60 triệu đồng (20%)"),
                new("PIT_REGULAR", 4, 100_000_000m, 0.30m, new DateOnly(2026, 1, 1), null, "Bậc 4: Trên 60 đến 100 triệu đồng (30%)"),
                new("PIT_REGULAR", 5, null, 0.35m, new DateOnly(2026, 1, 1), null, "Bậc 5: Trên 100 triệu đồng (35%)")
            };
            db.TaxBrackets.AddRange(taxBrackets);
        }

        // 4. Tỷ lệ bảo hiểm bắt buộc (InsuranceRates)
        if (!await db.InsuranceRates.AnyAsync())
        {
            var rates = new List<InsuranceRate>
            {
                new(InsuranceRateKind.BHXH_HuuTri, 0.08m, 0.14m, new DateOnly(2020, 1, 1), null, "Luật BHXH - Quỹ Hưu trí & tử tuất"),
                new(InsuranceRateKind.BHXH_OmDau, 0.00m, 0.03m, new DateOnly(2020, 1, 1), null, "Luật BHXH - Quỹ Ốm đau & thai sản"),
                new(InsuranceRateKind.TNLD_BNN, 0.00m, 0.005m, new DateOnly(2020, 1, 1), null, "Nghị định 58/2020/NĐ-CP - Quỹ TNLĐ-BNN"),
                new(InsuranceRateKind.BHYT, 0.015m, 0.03m, new DateOnly(2020, 1, 1), null, "Luật BHYT"),
                new(InsuranceRateKind.BHTN, 0.01m, 0.01m, new DateOnly(2020, 1, 1), null, "Luật Việc làm - BHTN"),
                new(InsuranceRateKind.KPCD, 0.00m, 0.02m, new DateOnly(2020, 1, 1), null, "Luật Công đoàn - Kinh phí công đoàn 2%")
            };
            db.InsuranceRates.AddRange(rates);
        }

        // 5. Hệ số lương làm thêm giờ (OvertimeRates)
        if (!await db.OvertimeRates.AnyAsync())
        {
            var otRates = new List<OvertimeRate>
            {
                new(OvertimeDayKind.Weekday, false, 1.50m, new DateOnly(2021, 1, 1), null, "BLLĐ 2019 Điều 98 - Ngày thường ban ngày"),
                new(OvertimeDayKind.Weekday, true, 2.00m, new DateOnly(2021, 1, 1), null, "BLLĐ 2019 Điều 98 - Ngày thường ban đêm"),
                new(OvertimeDayKind.WeeklyRest, false, 2.00m, new DateOnly(2021, 1, 1), null, "BLLĐ 2019 Điều 98 - Ngày nghỉ tuần ban ngày"),
                new(OvertimeDayKind.WeeklyRest, true, 2.70m, new DateOnly(2021, 1, 1), null, "BLLĐ 2019 Điều 98 - Ngày nghỉ tuần ban đêm"),
                new(OvertimeDayKind.PublicHoliday, false, 3.00m, new DateOnly(2021, 1, 1), null, "BLLĐ 2019 Điều 98 - Ngày lễ tết ban ngày"),
                new(OvertimeDayKind.PublicHoliday, true, 3.90m, new DateOnly(2021, 1, 1), null, "BLLĐ 2019 Điều 98 - Ngày lễ tết ban đêm")
            };
            db.OvertimeRates.AddRange(otRates);
        }

        // 6. Ngày nghỉ lễ tết (PublicHolidays)
        if (!await db.PublicHolidays.AnyAsync())
        {
            var holidays = new List<PublicHoliday>
            {
                new(new DateOnly(2025, 1, 1), "Tết Dương lịch 2025", true),
                new(new DateOnly(2025, 4, 30), "Ngày Giải phóng miền Nam 2025", true),
                new(new DateOnly(2025, 5, 1), "Ngày Quốc tế Lao động 2025", true),
                new(new DateOnly(2025, 9, 2), "Quốc khánh 2025", true),
                new(new DateOnly(2026, 1, 1), "Tết Dương lịch 2026", true),
                new(new DateOnly(2026, 4, 30), "Ngày Giải phóng miền Nam 2026", true),
                new(new DateOnly(2026, 5, 1), "Ngày Quốc tế Lao động 2026", true),
                new(new DateOnly(2026, 9, 2), "Quốc khánh 2026", true),
                new(new DateOnly(2026, 9, 3), "Nghỉ liền kề Quốc khánh 2026", true)
            };
            db.PublicHolidays.AddRange(holidays);
        }

        // 7. Loại phụ cấp (AllowanceTypes)
        if (!await db.AllowanceTypes.AnyAsync())
        {
            var allowances = new List<AllowanceType>
            {
                new("TRACH_NHIEM", "Phụ cấp trách nhiệm quản lý", isInsurable: true, isTaxable: true, taxExemptCap: 0m, "Phụ cấp chức vụ, trưởng ca, quản đốc"),
                new("DOC_HAI", "Phụ cấp nặng nhọc, độc hại, nguy hiểm", isInsurable: true, isTaxable: true, taxExemptCap: 0m, "Phụ cấp dây chuyền tiệt trùng, phòng lạnh"),
                new("CA_DEM", "Phụ cấp làm việc ban đêm (+30%)", isInsurable: false, isTaxable: true, taxExemptCap: 0m, "Phụ cấp làm ca 3 ban đêm"),
                new("AN_CA", "Phụ cấp ăn giữa ca", isInsurable: false, isTaxable: true, taxExemptCap: 730_000m, "Miễn thuế tối đa 730.000 đ/tháng"),
                new("XANG_XE", "Phụ cấp xăng xe, đi lại", isInsurable: false, isTaxable: true, taxExemptCap: 1_000_000m, "Khoán chi công tác xăng xe"),
                new("DIEN_THOAI", "Phụ cấp cước điện thoại liên lạc", isInsurable: false, isTaxable: true, taxExemptCap: 500_000m, "Khoán chi cước điện thoại"),
                new("NHA_O", "Hỗ trợ tiền thuê nhà ở", isInsurable: false, isTaxable: true, taxExemptCap: 0m, "Hỗ trợ lưu trú chuyên gia và luân chuyển")
            };
            db.AllowanceTypes.AddRange(allowances);
        }

        // 8. Danh mục dùng chung (LookupCategories & LookupItems)
        if (!await db.LookupCategories.AnyAsync())
        {
            var catEthnicity = new LookupCategory("ETHNICITY", "Dân tộc", "Danh mục các dân tộc Việt Nam", true);
            var catReligion = new LookupCategory("RELIGION", "Tôn giáo", "Danh mục tôn giáo tín ngưỡng", true);
            var catEduLevel = new LookupCategory("EDUCATION_LEVEL", "Trình độ học vấn", "Bậc học chuyên môn cao nhất", true);
            var catMajor = new LookupCategory("MAJOR", "Chuyên ngành đào tạo", "Các ngành nghề đào tạo chuyên môn", true);
            var catUniversity = new LookupCategory("UNIVERSITY", "Trường đại học/cao đẳng", "Danh mục cơ sở đào tạo", true);
            var catLanguage = new LookupCategory("FOREIGN_LANGUAGE", "Ngoại ngữ", "Các ngoại ngữ thông dụng", true);
            var catRank = new LookupCategory("CIVIL_SERVICE_RANK", "Ngạch chuyên môn", "Hệ thống ngạch công chức/chuyên môn", true);
            var catPosition = new LookupCategory("POSITION_TITLE", "Chức danh bổ nhiệm", "Chức danh quản lý doanh nghiệp", true);
            var catRewardDiscipline = new LookupCategory("REWARD_DISCIPLINE_TYPE", "Hình thức Khen thưởng - Kỷ luật", "Danh mục hình thức thi đua và kỷ luật", true);

            db.LookupCategories.AddRange(catEthnicity, catReligion, catEduLevel, catMajor, catUniversity, catLanguage, catRank, catPosition, catRewardDiscipline);
            await db.SaveChangesAsync();

            var items = new List<LookupItem>
            {
                // Dân tộc
                new(catEthnicity.Id, "KINH", "Kinh", 1),
                new(catEthnicity.Id, "TAY", "Tày", 2),
                new(catEthnicity.Id, "THAI", "Thái", 3),
                new(catEthnicity.Id, "MUONG", "Mường", 4),
                new(catEthnicity.Id, "HMONG", "H'Mông", 5),
                new(catEthnicity.Id, "KHMER", "Khmer", 6),
                new(catEthnicity.Id, "HOA", "Hoa", 7),

                // Tôn giáo
                new(catReligion.Id, "KHONG", "Không", 1),
                new(catReligion.Id, "PHAT", "Phật giáo", 2),
                new(catReligion.Id, "CONG_GIAO", "Công giáo", 3),
                new(catReligion.Id, "TIN_LANH", "Tin lành", 4),

                // Trình độ
                new(catEduLevel.Id, "TIEN_SI", "Tiến sĩ", 1),
                new(catEduLevel.Id, "THAC_SI", "Thạc sĩ", 2),
                new(catEduLevel.Id, "DAI_HOC", "Đại học", 3),
                new(catEduLevel.Id, "CAO_DANG", "Cao đẳng", 4),
                new(catEduLevel.Id, "TRUNG_CAP", "Trung cấp", 5),
                new(catEduLevel.Id, "THPT", "Trung học phổ thông", 6),

                // Khen thưởng - Kỷ luật
                new(catRewardDiscipline.Id, "BK_THU_TUONG", "Bằng khen của Thủ tướng Chính phủ", 1),
                new(catRewardDiscipline.Id, "CSTĐ_CO_SO", "Chiến sĩ thi đua cấp cơ sở", 2),
                new(catRewardDiscipline.Id, "GIAY_KHEN_TGĐ", "Giấy khen của Tổng Giám đốc Vinamilk", 3),
                new(catRewardDiscipline.Id, "KHIEN_TRACH", "Khiển trách bằng văn bản", 10),
                new(catRewardDiscipline.Id, "CANH_CAO", "Cảnh cáo toàn công ty", 11),
                new(catRewardDiscipline.Id, "SA_THAI", "Sa thải chấm dứt HĐLĐ", 12)
            };
            db.LookupItems.AddRange(items);
        }

        // 9. Khối chức danh (JobFamilies) & Ngạch bậc (JobGrades) & Chức danh (JobTitles) & Thang lương P1 (SalaryScales)
        if (!await db.JobFamilies.AnyAsync())
        {
            var famProd = new JobFamily("MFG", "Khối Sản xuất & Chế biến sữa", "Các nhà máy chế biến sữa");
            var famFarm = new JobFamily("FARM", "Khối Nông nghiệp & Trang trại bò sữa", "Hệ thống trang trại sinh thái Vinamilk Green Farm");
            var famTech = new JobFamily("TECH", "Khối Kỹ thuật, Cơ điện & Tự động hóa", "Bảo trì thiết bị Tetra Pak, GEA");
            var famSales = new JobFamily("SALES", "Khối Kinh doanh & Chuỗi Cửa hàng", "Chuỗi Giấc Mơ Sữa Việt & Kênh MT/GT");
            var famFinance = new JobFamily("FIN", "Khối Tài chính & Kế toán", "Quản trị tài chính và kế toán doanh nghiệp");
            var famHR = new JobFamily("HR", "Khối Quản trị Nguồn nhân lực", "Nhân sự, C&B, tuyển dụng, đào tạo");

            db.JobFamilies.AddRange(famProd, famFarm, famTech, famSales, famFinance, famHR);
            await db.SaveChangesAsync();

            // 15 Bậc ngạch lương (JobGrades)
            var grades = new List<JobGrade>();
            for (int g = 1; g <= 15; g++)
            {
                grades.Add(new JobGrade(g, $"GR_{g:D2}", $"Ngạch bậc {g}", $"Bậc ngạch chức danh tiêu chuẩn cấp {g}"));
            }
            db.JobGrades.AddRange(grades);
            await db.SaveChangesAsync();

            // Chức danh chuẩn (JobTitles)
            var titles = new List<JobTitle>
            {
                new("CEO", "Tổng Giám Đốc", famProd.Id, LeadershipLevel.Executive, grades[14].Id, "Lãnh đạo cao nhất điều hành tập đoàn"),
                new("DIR_FACTORY", "Giám Đốc Nhà Máy Chế Biến Sữa", famProd.Id, LeadershipLevel.Director, grades[12].Id, "Quản lý toàn diện nhà máy Mega"),
                new("MGR_PROD", "Quản Đốc Phân Xưởng Sản Xuất", famProd.Id, LeadershipLevel.Manager, grades[9].Id, "Điều hành các dây chuyền chiết rót"),
                new("TL_PROD", "Tổ Trưởng Ca Sản Xuất", famProd.Id, LeadershipLevel.TeamLeader, grades[6].Id, "Trực tiếp điều phối ca 3 ca 4 kíp"),
                new("OP_PACKAGING", "Công Nhân Vận Hành Máy Chiết Rót", famProd.Id, LeadershipLevel.None, grades[2].Id, "Vận hành máy đóng gói tiệt trùng Tetra Pak"),
                new("DIR_FARM", "Giám Đốc Trang Trại Bò Sữa", famFarm.Id, LeadershipLevel.Director, grades[12].Id, "Điều hành trang trại sinh thái Tây Ninh"),
                new("VET_SPEC", "Bác Sĩ Thú Y Trang Trại", famFarm.Id, LeadershipLevel.None, grades[6].Id, "Chăm sóc sức khỏe đàn bò sữa thuần chủng"),
                new("FARM_MILKING", "Nhân Viên Vắt Sữa Tự Động", famFarm.Id, LeadershipLevel.None, grades[2].Id, "Vận hành giàn vắt sữa xoay tự động"),
                new("STORE_MGR", "Cửa Hàng Trưởng Giấc Mơ Sữa Việt", famSales.Id, LeadershipLevel.TeamLeader, grades[5].Id, "Quản lý cửa hàng bán lẻ"),
                new("HR_DIRECTOR", "Giám Đốc Nhân Sự Tập Đoàn", famHR.Id, LeadershipLevel.Director, grades[13].Id, "Hoạch định chiến lược nguồn nhân lực"),
                new("CB_SPEC", "Chuyên Viên Cao Cấp C&B", famHR.Id, LeadershipLevel.None, grades[7].Id, "Thiết kế lương 3P và chính sách phúc lợi")
            };
            db.JobTitles.AddRange(titles);

            // Thang lương 3P: P1 theo (JobGrade, Step) áp dụng từ 01/01/2026
            // Ràng buộc P1 >= Lương tối thiểu vùng I (5,310,000 VND)
            var scales = new List<SalaryScale>();
            decimal[] baseByGrade = {
                5_500_000m,  // Bậc 1: Công nhân thử việc/phổ thông (> 5.31tr)
                6_200_000m,  // Bậc 2
                7_100_000m,  // Bậc 3: Công nhân chính thức
                8_200_000m,  // Bậc 4
                9_500_000m,  // Bậc 5: Kỹ thuật viên / Cửa hàng trưởng
                11_200_000m, // Bậc 6: Trưởng ca / Giám sát
                13_500_000m, // Bậc 7: Chuyên viên / Bác sĩ thú y
                16_500_000m, // Bậc 8: Chuyên viên cao cấp
                21_000_000m, // Bậc 9: Trưởng nhóm / Trợ lý quản đốc
                27_000_000m, // Bậc 10: Quản đốc xưởng / Trưởng phòng
                35_000_000m, // Bậc 11: Phó Giám đốc khối
                46_000_000m, // Bậc 12: Giám đốc nhà máy quy mô vừa
                60_000_000m, // Bậc 13: Giám đốc Mega Factory / Giám đốc chức năng
                85_000_000m, // Bậc 14: Phó Tổng Giám đốc
                130_000_000m // Bậc 15: Tổng Giám Đốc (CEO)
            };

            for (int g = 0; g < grades.Count; g++)
            {
                decimal gradeBase = baseByGrade[g];
                for (int step = 1; step <= 5; step++)
                {
                    // Mỗi step tăng 7%
                    decimal stepAmount = Math.Round(gradeBase * (1m + (step - 1) * 0.07m), 0, MidpointRounding.AwayFromZero);
                    scales.Add(new SalaryScale(grades[g].Id, step, stepAmount, new DateOnly(2026, 1, 1), null, "Thang bảng lương Vinamilk 2026"));
                }
            }
            db.SalaryScales.AddRange(scales);
        }

        // 8. Định nghĩa ca làm việc 4 khối Vinamilk (Phase 3 ShiftDefinitions)
        if (!await db.ShiftDefinitions.AnyAsync())
        {
            // 8.1. Khối Hành chính Văn phòng (08h00 - 17h30, nghỉ trưa 90 phút)
            var shiftOffice = new ShiftDefinition("OFFICE", "Ca Hành chính Văn phòng", crossesMidnight: false, totalPlannedWorkHours: 8.0m);
            shiftOffice.AddSegment(1, new TimeOnly(8, 0), new TimeOnly(17, 30), breakMinutes: 90, isPaidBreak: false);

            // 8.2. Khối Nhà máy Mega Factory (3 ca liên tục)
            var shiftCa1 = new ShiftDefinition("CA_1", "Ca 1 - Sáng (06h00 - 14h00)", crossesMidnight: false, totalPlannedWorkHours: 8.0m);
            shiftCa1.AddSegment(1, new TimeOnly(6, 0), new TimeOnly(14, 0), breakMinutes: 0, isPaidBreak: true);

            var shiftCa2 = new ShiftDefinition("CA_2", "Ca 2 - Chiều (14h00 - 22h00)", crossesMidnight: false, totalPlannedWorkHours: 8.0m);
            shiftCa2.AddSegment(1, new TimeOnly(14, 0), new TimeOnly(22, 0), breakMinutes: 0, isPaidBreak: true);

            var shiftCa3 = new ShiftDefinition("CA_3", "Ca 3 - Đêm (22h00 - 06h00)", crossesMidnight: true, totalPlannedWorkHours: 8.0m);
            shiftCa3.AddSegment(1, new TimeOnly(22, 0), new TimeOnly(6, 0), breakMinutes: 0, isPaidBreak: true);

            // 8.3. Khối Trang trại Bò sữa sinh thái (Ca gãy 2 đoạn)
            var shiftFarm = new ShiftDefinition("CA_GAY_FARM", "Ca gãy Trang trại (Vắt sữa sớm & chiều)", crossesMidnight: false, totalPlannedWorkHours: 8.0m);
            shiftFarm.AddSegment(1, new TimeOnly(4, 30), new TimeOnly(8, 30), breakMinutes: 0, isPaidBreak: true);
            shiftFarm.AddSegment(2, new TimeOnly(13, 30), new TimeOnly(17, 30), breakMinutes: 0, isPaidBreak: true);

            // 8.4. Khối Cửa hàng Giấc Mơ Sữa Việt (Ca xoay bán lẻ)
            var shiftRetailS = new ShiftDefinition("RETAIL_S", "Ca Sáng Cửa hàng (06h30 - 14h30)", crossesMidnight: false, totalPlannedWorkHours: 8.0m);
            shiftRetailS.AddSegment(1, new TimeOnly(6, 30), new TimeOnly(14, 30), breakMinutes: 0, isPaidBreak: true);

            var shiftRetailC = new ShiftDefinition("RETAIL_C", "Ca Chiều Cửa hàng (14h00 - 22h00)", crossesMidnight: false, totalPlannedWorkHours: 8.0m);
            shiftRetailC.AddSegment(1, new TimeOnly(14, 0), new TimeOnly(22, 0), breakMinutes: 0, isPaidBreak: true);

            db.ShiftDefinitions.AddRange(shiftOffice, shiftCa1, shiftCa2, shiftCa3, shiftFarm, shiftRetailS, shiftRetailC);
        }

        // 9. Mẫu chu kỳ xoay ca (RotationPatterns)
        if (!await db.RotationPatterns.AnyAsync())
        {
            var patterns = new List<RotationPattern>
            {
                new(
                    "ROT_4CREW_3SHIFT_8D",
                    "3 ca 4 kíp chu kỳ 8 ngày (S,S,C,C,D,D,OFF,OFF)",
                    8,
                    "CA_1,CA_1,CA_2,CA_2,CA_3,CA_3,OFF,OFF",
                    "Áp dụng cho các nhà máy sản xuất Mega Factory vận hành liên tục 24/7/365"),

                new(
                    "ROT_FARM_7D",
                    "Ca gãy trang trại 7 ngày (6 ngày làm 1 ngày nghỉ)",
                    7,
                    "CA_GAY_FARM,CA_GAY_FARM,CA_GAY_FARM,CA_GAY_FARM,CA_GAY_FARM,CA_GAY_FARM,OFF",
                    "Áp dụng cho nhân viên chuyên trách đàn bò và vắt sữa tại Trang trại Green Farm"),

                new(
                    "ROT_RETAIL_7D",
                    "Ca xoay bán lẻ 7 ngày cửa hàng Giấc Mơ Sữa Việt",
                    7,
                    "RETAIL_S,RETAIL_S,RETAIL_C,RETAIL_C,RETAIL_S,RETAIL_C,OFF",
                    "Áp dụng cho nhân viên bán hàng chuỗi cửa hàng Giấc Mơ Sữa Việt")
            };

            db.RotationPatterns.AddRange(patterns);
        }

        await db.SaveChangesAsync();
    }

    private static string GenerateSecurePassword()
    {
        const string upperChars = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lowerChars = "abcdefghijkmnopqrstuvwxyz";
        const string digitChars = "23456789";
        const string specialChars = "@#$%&*";

        var bytes = RandomNumberGenerator.GetBytes(12);
        var passwordChars = new char[12];

        passwordChars[0] = upperChars[bytes[0] % upperChars.Length];
        passwordChars[1] = lowerChars[bytes[1] % lowerChars.Length];
        passwordChars[2] = digitChars[bytes[2] % digitChars.Length];
        passwordChars[3] = specialChars[bytes[3] % specialChars.Length];

        string allChars = upperChars + lowerChars + digitChars + specialChars;
        for (int i = 4; i < 12; i++)
        {
            passwordChars[i] = allChars[bytes[i] % allChars.Length];
        }

        return new string(passwordChars.OrderBy(_ => RandomNumberGenerator.GetInt32(100)).ToArray());
    }
    public static async Task SeedKpiDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VinamilkHRMDbContext>();

        // 1. Khung cơ cấu tỷ trọng theo LeadershipLevel (KpiScheme)
        if (!await db.KpiSchemes.AnyAsync())
        {
            var effectiveDate = new DateOnly(2026, 1, 1);
            var schemes = new List<KpiScheme>
            {
                // Director: 60 / 20 / 20 / Cổng (ComplianceGating = true, weight = 0)
                new(LeadershipLevel.Director, 60m, 20m, 20m, 0m, isComplianceGating: true, effectiveDate, null, "Khung tỷ trọng KPI Giám đốc đơn vị/chức năng (Compliance đóng vai trò cổng)"),
                // Executive
                new(LeadershipLevel.Executive, 60m, 20m, 20m, 0m, isComplianceGating: true, effectiveDate, null, "Khung tỷ trọng KPI Ban Điều hành (Compliance đóng vai trò cổng)"),
                // Manager: 40 / 35 / 15 / 10
                new(LeadershipLevel.Manager, 40m, 35m, 15m, 10m, isComplianceGating: false, effectiveDate, null, "Khung tỷ trọng KPI Cấp Quản lý / Trưởng phòng / Quản đốc"),
                // TeamLeader: 30 / 45 / 10 / 15
                new(LeadershipLevel.TeamLeader, 30m, 45m, 10m, 15m, isComplianceGating: false, effectiveDate, null, "Khung tỷ trọng KPI Cấp Trưởng nhóm / Tổ trưởng ca"),
                // Employee: 10 / 70 / 0 / 20
                new(LeadershipLevel.None, 10m, 70m, 0m, 20m, isComplianceGating: false, effectiveDate, null, "Khung tỷ trọng KPI Nhân viên chuyên môn / Công nhân sản xuất")
            };
            db.KpiSchemes.AddRange(schemes);
        }

        // 2. Bảng tham số cấu hình quy chế KPI có ngày hiệu lực (KpiConfiguration)
        if (!await db.KpiConfigurations.AnyAsync())
        {
            var effectiveDate = new DateOnly(2026, 1, 1);
            var configs = new List<KpiConfiguration>
            {
                new("MIN_COMPLETION_RATE_THRESHOLD", 50m, effectiveDate, null, "50", "Ngưỡng tỷ lệ hoàn thành tối thiểu, dưới 50% tính 0 điểm"),
                new("MAX_KPI_ITEM_SCORE", 120m, effectiveDate, null, "120", "Trần điểm tối đa cho mỗi chỉ tiêu KPI"),
                new("MAX_EARLY_BONUS_PERCENT", 10m, effectiveDate, null, "10", "Tỷ lệ thưởng tối đa khi hoàn thành sớm hạn (10%)"),
                new("EARLY_BONUS_RATE_PER_DAY", 2m, effectiveDate, null, "2", "Tỷ lệ thưởng mỗi ngày làm việc hoàn thành sớm (2%/ngày)"),
                new("LATE_PENALTY_DAYS_1_TO_5", 5m, effectiveDate, null, "5", "Mức phạt trễ hạn mỗi ngày làm việc cho ngày 1 đến 5 (5%/ngày)"),
                new("LATE_PENALTY_DAYS_6_TO_10", 10m, effectiveDate, null, "10", "Mức phạt trễ hạn mỗi ngày làm việc cho ngày 6 đến 10 (10%/ngày)"),
                new("MAX_LATE_DAYS_BEFORE_ZERO", 10m, effectiveDate, null, "10", "Số ngày làm việc trễ tối đa trước khi tính 0 điểm (> 10 ngày)"),
                new("SELF_REVIEW_LATE_PENALTY", 2m, effectiveDate, null, "2", "Điểm phạt trừ vào tổng điểm khi nhân viên tự đánh giá trễ hạn"),
                new("MANAGER_REVIEW_LATE_PENALTY_PER_SUBORDINATE", 1m, effectiveDate, null, "1", "Điểm phạt trừ vào điểm PeopleManagement của lãnh đạo mỗi cấp dưới bị chấm trễ"),
                new("MAX_MANAGER_REVIEW_LATE_PENALTY", 10m, effectiveDate, null, "10", "Mức phạt tối đa đối với lãnh đạo chấm trễ hạn"),
                new("MIN_ATTENDANCE_STANDARD_PERCENT", 50m, effectiveDate, null, "50", "Tỷ lệ ngày công chuẩn tối thiểu để được xếp loại (< 50% không xếp loại)"),
                new("GRADE_A_MIN_SCORE", 110m, effectiveDate, null, "110", "Ngưỡng điểm tối thiểu xếp loại A"),
                new("GRADE_B_MIN_SCORE", 95m, effectiveDate, null, "95", "Ngưỡng điểm tối thiểu xếp loại B"),
                new("GRADE_C_MIN_SCORE", 80m, effectiveDate, null, "80", "Ngưỡng điểm tối thiểu xếp loại C"),
                new("GRADE_D_MIN_SCORE", 60m, effectiveDate, null, "60", "Ngưỡng điểm tối thiểu xếp loại D"),
                new("BONUS_COEFFICIENT_A", 1.2m, effectiveDate, null, "1.2", "Hệ số thưởng loại A"),
                new("BONUS_COEFFICIENT_B", 1.0m, effectiveDate, null, "1.0", "Hệ số thưởng loại B"),
                new("BONUS_COEFFICIENT_C", 0.8m, effectiveDate, null, "0.8", "Hệ số thưởng loại C"),
                new("BONUS_COEFFICIENT_D", 0.5m, effectiveDate, null, "0.5", "Hệ số thưởng loại D"),
                new("BONUS_COEFFICIENT_E", 0.0m, effectiveDate, null, "0.0", "Hệ số thưởng loại E"),
                new("CALIBRATION_WARNING_MAX_A_PERCENT", 15m, effectiveDate, null, "15", "Cảnh báo hiệu chuẩn khi tỷ lệ loại A vượt quá 15%"),
                new("CALIBRATION_WARNING_ZERO_E_CONSECUTIVE_PERIODS", 3m, effectiveDate, null, "3", "Cảnh báo hiệu chuẩn khi tỷ lệ loại E bằng 0% liên tiếp 3 kỳ")
            };
            db.KpiConfigurations.AddRange(configs);
        }

        // 3. Thư viện chỉ tiêu KPI chuẩn hóa (KpiDefinition)
        if (!await db.KpiDefinitions.AnyAsync())
        {
            var defs = new List<KpiDefinition>
            {
                new("KPI_PROD_OUTPUT", "Sản lượng sản xuất sữa thành phẩm", "Lít", KpiGroup.UnitResult, KpiScoringMethod.HigherIsBetter, KpiDataSource.Production, null, null, "Tổng sản lượng sữa chế biến đạt chuẩn đóng gói xuất xưởng."),
                new("KPI_PROD_QUALITY", "Tỷ lệ sản phẩm đạt tiêu chuẩn KCS", "%", KpiGroup.UnitResult, KpiScoringMethod.HigherIsBetter, KpiDataSource.Quality, null, null, "Tỷ lệ lô hàng đạt chuẩn kiểm nghiệm ngay lần kiểm tra đầu tiên."),
                new("KPI_LOSS_RATE", "Tỷ lệ hao hụt nguyên phụ liệu", "%", KpiGroup.IndividualWork, KpiScoringMethod.LowerIsBetter, KpiDataSource.Quality, null, null, "Tỷ lệ thất thoát bao bì và nguyên liệu sữa trong quá trình vận hành."),
                new("KPI_ATTENDANCE", "Tỷ lệ ngày công làm việc chuyên cần", "%", KpiGroup.Compliance, KpiScoringMethod.HigherIsBetter, KpiDataSource.Attendance, null, null, "Tỷ lệ ngày công thực tế so với công chuẩn của đơn vị."),
                new("KPI_SALES_REVENUE", "Doanh số bán lẻ chuỗi Giấc Mơ Sữa Việt", "VND", KpiGroup.UnitResult, KpiScoringMethod.HigherIsBetter, KpiDataSource.Sales, null, null, "Tổng doanh thu bán hàng thực tế đạt được trong tháng."),
                new("KPI_SUBORDINATE_GRADE_B", "Tỷ lệ cấp dưới đạt loại B trở lên", "%", KpiGroup.PeopleManagement, KpiScoringMethod.HigherIsBetter, KpiDataSource.Manual, null, null, "Đo lường năng lực phát triển đội ngũ của quản lý trực tiếp."),
                new("KPI_SAFETY_COMPLIANCE", "Tuân thủ an toàn lao động và 5S", "Điểm", KpiGroup.Compliance, KpiScoringMethod.Binary, KpiDataSource.Manual, null, null, "Không để xảy ra sự cố mất an toàn lao động và vi phạm quy chuẩn 5S."),
                new("KPI_TASK_PROJECT", "Hoàn thành nhiệm vụ/dự án trọng tâm đúng hạn", "Task", KpiGroup.IndividualWork, KpiScoringMethod.Task, KpiDataSource.Manual, null, null, "Nhiệm vụ đột xuất hoặc dự án trọng điểm được giao theo tháng.")
            };
            db.KpiDefinitions.AddRange(defs);
        }

        // 4. Kỳ đánh giá mẫu tháng 2026-10 (KpiPeriod)
        if (!await db.KpiPeriods.AnyAsync(p => p.Period == "2026-10"))
        {
            var period202610 = KpiPeriod.CreateDefault(2026, 10);
            period202610.MoveToInProgress();
            db.KpiPeriods.Add(period202610);
        }

        await db.SaveChangesAsync();
    }
}


