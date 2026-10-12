using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VinamilkHRM.Application.Common.Security;
using VinamilkHRM.Infrastructure.Identity;

namespace VinamilkHRM.Infrastructure.Data;

/// <summary>
/// Trình khởi tạo dữ liệu mẫu (Seeder) cho 10 Vai trò doanh nghiệp và các tài khoản demo khởi tạo ban đầu.
/// Mật khẩu được sinh ngẫu nhiên an toàn và chỉ in ra console đúng 1 lần duy nhất trong quá trình seed.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedIdentityAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<VinamilkHRMDbContext>();
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
                Permissions.BaocaoExport, Permissions.HethongView
            }),
            [Roles.HRDirector] = ("Giám đốc Nhân sự Tập đoàn", new[]
            {
                Permissions.NhansuView, Permissions.NhansuAdd, Permissions.NhansuEdit, Permissions.NhansuDelete, Permissions.NhansuExport,
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
                Permissions.NhansuView, Permissions.NhansuAdd, Permissions.NhansuEdit, Permissions.NhansuExport,
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
                Permissions.LuongView, Permissions.LuongApprove, Permissions.LuongExport,
                Permissions.ChamcongView, Permissions.BaocaoView, Permissions.BaocaoExport
            }),
            [Roles.PayrollAccountant] = ("Kế toán tiền lương & Chế độ phúc lợi", new[]
            {
                Permissions.LuongView, Permissions.LuongAdd, Permissions.LuongEdit, Permissions.LuongExport,
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

            // Đồng bộ Claims quyền hạn cho vai trò
            var existingClaims = await roleManager.GetClaimsAsync(role);
            foreach (var permission in permissions)
            {
                if (!existingClaims.Any(c => c.Type == "permission" && c.Value == permission))
                {
                    await roleManager.AddClaimAsync(role, new Claim("permission", permission));
                }
            }
        }

        // 2. Tạo 10 tài khoản demo tương ứng 10 vai trò (Mật khẩu sinh ngẫu nhiên)
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

        Console.WriteLine("\n================================================================================");
        Console.WriteLine("              VINAMILK ENTERPRISE HRM - DEMO ACCOUNTS SEED CREDENTIALS         ");
        Console.WriteLine("================================================================================");
        Console.WriteLine($"{"Tên đăng nhập",-18} | {"Vai trò",-17} | {"Mật khẩu khởi tạo (MustChangePassword)",-35}");
        Console.WriteLine(new string('-', 80));

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
                    Console.WriteLine($"{username,-18} | {roleName,-17} | {temporaryPassword,-35}");
                }
                else
                {
                    logger.LogError("Không thể tạo tài khoản seed {Username}: {Errors}",
                        username, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
        }

        Console.WriteLine("================================================================================\n");
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

        // Shuffle characters
        return new string(passwordChars.OrderBy(_ => RandomNumberGenerator.GetInt32(100)).ToArray());
    }
}
