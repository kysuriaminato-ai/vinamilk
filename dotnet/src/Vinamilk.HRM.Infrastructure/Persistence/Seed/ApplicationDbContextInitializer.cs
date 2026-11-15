using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Payroll;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Identity;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.SystemEntities;
using Vinamilk.HRM.Domain.Entities.Talent;
using Vinamilk.HRM.Domain.Entities.Time;
using Vinamilk.HRM.Infrastructure.Persistence.SqlObjects;
using Vinamilk.HRM.Infrastructure.Security;

namespace Vinamilk.HRM.Infrastructure.Persistence.Seed;

public sealed class ApplicationDbContextInitializer
{
    public const string InitialSuperAdminPasswordEnvVar = "VINAMILK_INITIAL_SUPERADMIN_PASSWORD";

    private readonly ILogger<ApplicationDbContextInitializer> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IPiiEncryptionService _encryptionService;
    private readonly PasswordSecurityService _passwordService;
    private readonly IHostEnvironment? _hostEnvironment;
    private readonly IConfiguration? _configuration;

    public ApplicationDbContextInitializer(
        ILogger<ApplicationDbContextInitializer> logger,
        ApplicationDbContext context,
        IPiiEncryptionService encryptionService,
        PasswordSecurityService? passwordService = null,
        IHostEnvironment? hostEnvironment = null,
        IConfiguration? configuration = null)
    {
        _logger = logger;
        _context = context;
        _encryptionService = encryptionService;
        _passwordService = passwordService ?? new PasswordSecurityService();
        _hostEnvironment = hostEnvironment;
        _configuration = configuration;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_context.Database.IsSqlServer())
            {
                await _context.Database.MigrateAsync(cancellationToken);
                await EnsureReportingSqlObjectsAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi trong quá trình Migrate cơ sở dữ liệu Vinamilk HRM.");
            throw;
        }
    }

    private async Task EnsureReportingSqlObjectsAsync(CancellationToken cancellationToken)
    {
        await _context.Database.ExecuteSqlRawAsync(ReportingSqlObjects.CreateDepartmentHeadcountViewSql, cancellationToken);
        await _context.Database.ExecuteSqlRawAsync(ReportingSqlObjects.CreateMonthlyPayrollFundViewSql, cancellationToken);
        await _context.Database.ExecuteSqlRawAsync(ReportingSqlObjects.CreateSpMonthlyHeadcountAndTurnoverSql, cancellationToken);
        await _context.Database.ExecuteSqlRawAsync(ReportingSqlObjects.CreateSpPayrollFundAnalysisSql, cancellationToken);
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TrySeedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi trong quá trình Seed dữ liệu mẫu Vinamilk HRM.");
            throw;
        }
    }

    private async Task TrySeedAsync(CancellationToken cancellationToken)
    {
        // 1. Seed Roles & Permissions (6 Roles chuẩn theo yêu cầu)
        if (!await _context.Roles.AnyAsync(cancellationToken))
        {
            var roles = new List<Role>
            {
                new() { RoleCode = "SUPER_ADMIN", RoleName = "SuperAdmin", Description = "Quản trị viên hệ thống tối cao", DefaultDataScope = "All", IsSystemRole = true },
                new() { RoleCode = "HR_MANAGER", RoleName = "HR Manager", Description = "Giám đốc / Trưởng phòng Nhân sự", DefaultDataScope = "All", IsSystemRole = true },
                new() { RoleCode = "HR_STAFF", RoleName = "HR Staff", Description = "Chuyên viên Nhân sự (Hồ sơ, Tuyển dụng, C&B)", DefaultDataScope = "All", IsSystemRole = true },
                new() { RoleCode = "LINE_MANAGER", RoleName = "Line Manager", Description = "Trưởng đơn vị / Quản lý trực tiếp", DefaultDataScope = "UnitTree", IsSystemRole = true },
                new() { RoleCode = "EMPLOYEE", RoleName = "Employee", Description = "Nhân viên (Cổng thông tin tự phục vụ ESS)", DefaultDataScope = "SelfOnly", IsSystemRole = true },
                new() { RoleCode = "ACCOUNTANT", RoleName = "Accountant", Description = "Kế toán Tiền lương, BHXH & Thuế TNCN", DefaultDataScope = "All", IsSystemRole = true }
            };
            await _context.Roles.AddRangeAsync(roles, cancellationToken);

            var modules = new (string Code, string Name, string[] Actions)[]
            {
                ("Dashboard", "Bảng điều khiển", new[] { "View" }),
                ("Employees", "Quản lý Hồ sơ Nhân sự", new[] { "View", "Create", "Edit", "Delete", "Export" }),
                ("Departments", "Cơ cấu Tổ chức & Đơn vị", new[] { "View", "Create", "Edit", "Delete" }),
                ("Contracts", "Hợp đồng Lao động", new[] { "View", "Create", "Edit", "Approve", "Export" }),
                ("Attendance", "Chấm công & Ca làm việc", new[] { "View", "Create", "Edit", "Approve", "Lock", "Export" }),
                ("Leave", "Nghỉ phép & Quỹ phép", new[] { "View", "Create", "Approve" }),
                ("Payroll", "Tiền lương 3P, BHXH & Thuế", new[] { "View", "Calculate", "Approve", "Lock", "Unlock", "Export" }),
                ("Recruitment", "Tuyển dụng (ATS)", new[] { "View", "Create", "Edit", "Approve" }),
                ("Onboarding", "Onboarding & Tài sản", new[] { "View", "Create", "Edit" }),
                ("Training", "Đào tạo & Phát triển", new[] { "View", "Create", "Edit" }),
                ("Kpi", "Đánh giá KPI / OKR", new[] { "View", "Create", "Evaluate", "Approve" }),
                ("Transfers", "Điều chuyển & Bổ nhiệm", new[] { "View", "Create", "Approve" }),
                ("Rewards", "Khen thưởng & Kỷ luật", new[] { "View", "Create", "Approve" }),
                ("Reports", "Báo cáo Thống kê BM01-BM04", new[] { "View", "Export" }),
                ("Analytics", "Phân tích Nhân sự & AI Expert", new[] { "View" }),
                ("System", "Quản trị Hệ thống & Phân quyền", new[] { "View", "Manage" })
            };

            var permissions = new List<Permission>();
            var sortOrder = 1;
            foreach (var mod in modules)
            {
                foreach (var act in mod.Actions)
                {
                    permissions.Add(new Permission
                    {
                        PermissionCode = $"{mod.Code}.{act}",
                        ModuleName = mod.Code,
                        ActionName = act,
                        DisplayName = $"{mod.Name} - {act}",
                        SortOrder = sortOrder++
                    });
                }
            }
            await _context.Permissions.AddRangeAsync(permissions, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Gán RolePermissions
            var rolePermissions = new List<RolePermission>();
            var superAdminRole = roles.First(r => r.RoleCode == "SUPER_ADMIN");
            var hrManagerRole = roles.First(r => r.RoleCode == "HR_MANAGER");
            var hrStaffRole = roles.First(r => r.RoleCode == "HR_STAFF");
            var lineManagerRole = roles.First(r => r.RoleCode == "LINE_MANAGER");
            var accountantRole = roles.First(r => r.RoleCode == "ACCOUNTANT");
            var employeeRole = roles.First(r => r.RoleCode == "EMPLOYEE");

            foreach (var perm in permissions)
            {
                rolePermissions.Add(new RolePermission { RoleId = superAdminRole.Id, PermissionId = perm.Id });

                if (perm.ModuleName != "System" && perm.PermissionCode != "Payroll.Unlock")
                    rolePermissions.Add(new RolePermission { RoleId = hrManagerRole.Id, PermissionId = perm.Id });

                if (perm.ModuleName is "Dashboard" or "Employees" or "Departments" or "Contracts" or "Attendance" or "Leave" or "Recruitment" or "Onboarding" or "Training"
                    && perm.ActionName is "View" or "Create" or "Edit" or "Export")
                    rolePermissions.Add(new RolePermission { RoleId = hrStaffRole.Id, PermissionId = perm.Id });

                if (perm.ActionName == "View" || (perm.ModuleName is "Attendance" or "Leave" or "Kpi" && perm.ActionName is "Approve" or "Evaluate"))
                    rolePermissions.Add(new RolePermission { RoleId = lineManagerRole.Id, PermissionId = perm.Id });

                if ((perm.ModuleName is "Dashboard" or "Payroll" or "Reports" && perm.PermissionCode != "Payroll.Unlock")
                    || (perm.ModuleName is "Employees" or "Attendance" && perm.ActionName == "View"))
                    rolePermissions.Add(new RolePermission { RoleId = accountantRole.Id, PermissionId = perm.Id });

                if (perm.ModuleName is "Dashboard" or "Leave" || (perm.ModuleName is "Employees" or "Attendance" or "Payroll" or "Training" or "Kpi" && perm.ActionName == "View"))
                    rolePermissions.Add(new RolePermission { RoleId = employeeRole.Id, PermissionId = perm.Id });
            }
            await _context.RolePermissions.AddRangeAsync(rolePermissions, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // 2. Seed Danh mục Cơ cấu Tổ chức (32 Đơn vị từ dm_donvi)
        if (!await _context.OrganizationUnits.AnyAsync(cancellationToken))
        {
            var rootUnit = new OrganizationUnit
            {
                UnitCode = "VINAMILK",
                UnitName = "Công ty Cổ phần Sữa Việt Nam (Vinamilk)",
                UnitType = "HeadOffice",
                UnitLevel = 1,
                TreePath = "/VINAMILK/",
                Address = "Số 10 Tân Trào, P. Tân Phú, Q.7, TP.HCM",
                Phone = "028-54155555",
                PlannedHeadcount = 10156,
                SortOrder = 1
            };
            await _context.OrganizationUnits.AddAsync(rootUnit, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var divisions = new List<OrganizationUnit>
            {
                new() { UnitCode = "KHOI_SX", UnitName = "Khối Sản xuất", UnitType = "Division", UnitLevel = 2, ParentUnitId = rootUnit.Id, TreePath = "/VINAMILK/KHOI_SX/", PlannedHeadcount = 5420, SortOrder = 10 },
                new() { UnitCode = "KHOI_TT", UnitName = "Khối Phát triển Vùng nguyên liệu", UnitType = "Division", UnitLevel = 2, ParentUnitId = rootUnit.Id, TreePath = "/VINAMILK/KHOI_TT/", PlannedHeadcount = 1850, SortOrder = 20 },
                new() { UnitCode = "KHOI_KD", UnitName = "Khối Kinh doanh & Marketing", UnitType = "Division", UnitLevel = 2, ParentUnitId = rootUnit.Id, TreePath = "/VINAMILK/KHOI_KD/", PlannedHeadcount = 1980, SortOrder = 30 },
                new() { UnitCode = "KHOI_VP", UnitName = "Khối Văn phòng", UnitType = "Division", UnitLevel = 2, ParentUnitId = rootUnit.Id, TreePath = "/VINAMILK/KHOI_VP/", PlannedHeadcount = 906, SortOrder = 40 }
            };
            await _context.OrganizationUnits.AddRangeAsync(divisions, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var divMap = divisions.ToDictionary(d => d.UnitCode, d => d.Id);
            var level3Specs = new (string Code, string Name, string ParentCode, string Type, string Addr, string Phone, int Headcount, int Order)[]
            {
                ("NM_TS", "Nhà máy Sữa Tiên Sơn", "KHOI_SX", "Factory", "KCN Tiên Sơn, Bắc Ninh", "0222-3741234", 820, 11),
                ("NM_NA", "Nhà máy Sữa Nghệ An", "KHOI_SX", "Factory", "KCN Nam Cấm, Nghi Lộc, Nghệ An", "0238-3851234", 650, 12),
                ("NM_BD", "Nhà máy Sữa Bình Dương", "KHOI_SX", "Factory", "KCN Mỹ Phước, Bến Cát, Bình Dương", "0274-3651234", 780, 13),
                ("NM_DN", "Nhà máy Sữa Đà Nẵng", "KHOI_SX", "Factory", "KCN Hòa Khánh, Đà Nẵng", "0236-3731234", 520, 14),
                ("NM_SG", "Nhà máy Sữa Sài Gòn", "KHOI_SX", "Factory", "Q.Bình Tân, TP.HCM", "028-54161234", 900, 15),
                ("NM_TL", "Nhà máy Sữa Thống Nhất", "KHOI_SX", "Factory", "Q.Tân Bình, TP.HCM", "028-38461234", 680, 16),
                ("NM_CT", "Nhà máy Sữa Cần Thơ", "KHOI_SX", "Factory", "KCN Trà Nóc, Cần Thơ", "0292-3841234", 540, 17),
                ("NM_DL", "Nhà máy Sữa Đà Lạt", "KHOI_SX", "Factory", "KCN Phú Hội, Đức Trọng, Lâm Đồng", "0263-3551234", 530, 18),
                ("TT_GREEN1", "Trang trại Green Farm Tây Ninh", "KHOI_TT", "Farm", "Trảng Bàng, Tây Ninh", "0276-3851234", 320, 21),
                ("TT_GREEN2", "Trang trại Green Farm Hà Tĩnh", "KHOI_TT", "Farm", "Hương Sơn, Hà Tĩnh", "0239-3851234", 280, 22),
                ("TT_ORGANIC1", "Trang trại Organic Đà Lạt", "KHOI_TT", "Farm", "TP. Đà Lạt, Lâm Đồng", "0263-3551235", 350, 23),
                ("TT_ORGANIC2", "Trang trại Organic Lâm Đồng", "KHOI_TT", "Farm", "Đức Trọng, Lâm Đồng", "0263-3551236", 300, 24),
                ("TT_RESEDA", "Trang trại Reseda Thanh Hóa", "KHOI_TT", "Farm", "Thạch Thành, Thanh Hóa", "0237-3851234", 280, 25),
                ("TT_TQ", "Trang trại Tuyên Quang", "KHOI_TT", "Farm", "Sơn Dương, Tuyên Quang", "0207-3851234", 320, 26),
                ("CN_BAC", "Chi nhánh miền Bắc", "KHOI_KD", "Branch", "Cầu Giấy, Hà Nội", "024-37741234", 580, 31),
                ("CN_TRUNG", "Chi nhánh miền Trung", "KHOI_KD", "Branch", "TP. Đà Nẵng", "0236-3731235", 420, 32),
                ("CN_NAM", "Chi nhánh miền Nam", "KHOI_KD", "Branch", "Q.7, TP.HCM", "028-54151234", 620, 33),
                ("PB_XUATKHAU", "Phòng Xuất khẩu", "KHOI_KD", "Department", "Q.7, TP.HCM", "028-54151235", 180, 34),
                ("PB_TMDT", "Phòng Thương mại điện tử", "KHOI_KD", "Department", "Q.7, TP.HCM", "028-54151236", 180, 35),
                ("PB_NHANSU", "Phòng Nhân sự", "KHOI_VP", "Department", "Q.7, TP.HCM", "028-54151237", 85, 41),
                ("PB_TAICHINH", "Phòng Tài chính - Kế toán", "KHOI_VP", "Department", "Q.7, TP.HCM", "028-54151238", 120, 42),
                ("PB_MARKETING", "Phòng Marketing", "KHOI_VP", "Department", "Q.7, TP.HCM", "028-54151239", 95, 43),
                ("PB_RD", "Trung tâm Nghiên cứu & Phát triển", "KHOI_VP", "Department", "Q.7, TP.HCM", "028-54151240", 180, 44),
                ("PB_IT", "Phòng Công nghệ thông tin", "KHOI_VP", "Department", "Q.7, TP.HCM", "028-54151241", 110, 45),
                ("PB_PHAPLUAT", "Phòng Pháp luật", "KHOI_VP", "Department", "Q.7, TP.HCM", "028-54151242", 45, 46),
                ("PB_HANHCHINH", "Phòng Hành chính", "KHOI_VP", "Department", "Q.7, TP.HCM", "028-54151243", 75, 47),
                ("PB_QLCL", "Phòng Quản lý Chất lượng", "KHOI_VP", "Department", "Q.7, TP.HCM", "028-54151244", 96, 48)
            };

            var level3Units = level3Specs.Select(s => new OrganizationUnit
            {
                UnitCode = s.Code,
                UnitName = s.Name,
                UnitType = s.Type,
                UnitLevel = 3,
                ParentUnitId = divMap[s.ParentCode],
                TreePath = $"/VINAMILK/{s.ParentCode}/{s.Code}/",
                Address = s.Addr,
                Phone = s.Phone,
                PlannedHeadcount = s.Headcount,
                WageRegion = s.Code switch
                {
                    "NM_DN" or "CN_TRUNG" or "NM_CT" or "NM_TS" => 2,
                    "NM_DL" or "TT_ORGANIC1" or "TT_ORGANIC2" or "TT_GREEN1" or "NM_NA" => 3,
                    "TT_GREEN2" or "TT_RESEDA" or "TT_TQ" => 4,
                    _ => 1
                },
                SortOrder = s.Order
            }).ToList();

            await _context.OrganizationUnits.AddRangeAsync(level3Units, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // 3. Seed Chức vụ, Ngạch Bậc Lương, Trình độ Học vấn, Danh mục KTKL
        if (!await _context.Positions.AnyAsync(cancellationToken))
        {
            var positions = new List<Position>
            {
                new() { PositionCode = "TGD", PositionName = "Tổng Giám đốc", PositionGroup = "Executive", PositionLevel = 1, DefaultPositionAllowance = 25000000m },
                new() { PositionCode = "PTGD", PositionName = "Phó Tổng Giám đốc", PositionGroup = "Executive", PositionLevel = 2, DefaultPositionAllowance = 20000000m },
                new() { PositionCode = "GD", PositionName = "Giám đốc Khối/Chức năng", PositionGroup = "Director", PositionLevel = 3, DefaultPositionAllowance = 15000000m },
                new() { PositionCode = "PGD", PositionName = "Phó Giám đốc", PositionGroup = "Director", PositionLevel = 4, DefaultPositionAllowance = 12000000m },
                new() { PositionCode = "GDNM", PositionName = "Giám đốc Nhà máy", PositionGroup = "Director", PositionLevel = 4, DefaultPositionAllowance = 12000000m },
                new() { PositionCode = "GDTT", PositionName = "Giám đốc Trang trại", PositionGroup = "Director", PositionLevel = 4, DefaultPositionAllowance = 10000000m },
                new() { PositionCode = "TP", PositionName = "Trưởng phòng", PositionGroup = "Manager", PositionLevel = 5, DefaultPositionAllowance = 8000000m },
                new() { PositionCode = "PTP", PositionName = "Phó Trưởng phòng", PositionGroup = "Manager", PositionLevel = 6, DefaultPositionAllowance = 6000000m },
                new() { PositionCode = "QL", PositionName = "Quản lý/Quản đốc", PositionGroup = "Manager", PositionLevel = 7, DefaultPositionAllowance = 5000000m },
                new() { PositionCode = "TCA", PositionName = "Trưởng ca", PositionGroup = "Manager", PositionLevel = 8, DefaultPositionAllowance = 4000000m },
                new() { PositionCode = "TT", PositionName = "Tổ trưởng", PositionGroup = "Specialist", PositionLevel = 9, DefaultPositionAllowance = 3000000m },
                new() { PositionCode = "CVN", PositionName = "Chuyên viên cao cấp", PositionGroup = "Specialist", PositionLevel = 10, DefaultPositionAllowance = 4000000m },
                new() { PositionCode = "CV", PositionName = "Chuyên viên", PositionGroup = "Specialist", PositionLevel = 11, DefaultPositionAllowance = 3000000m },
                new() { PositionCode = "NV", PositionName = "Nhân viên", PositionGroup = "Staff", PositionLevel = 12, DefaultPositionAllowance = 2000000m },
                new() { PositionCode = "CN", PositionName = "Công nhân", PositionGroup = "Worker", PositionLevel = 13, DefaultPositionAllowance = 1500000m }
            };
            await _context.Positions.AddRangeAsync(positions, cancellationToken);

            var scaleSpecs = new (string Code, string Name, decimal StartCoeff, decimal BaseVnd)[]
            {
                ("NG_A1_1", "Chuyên viên cao cấp", 6.20m, 30000000m),
                ("NG_A1_2", "Kỹ sư cao cấp", 6.20m, 28000000m),
                ("NG_A2", "Chuyên viên chính", 4.40m, 22000000m),
                ("NG_A3", "Chuyên viên", 2.34m, 14000000m),
                ("NG_B", "Cán sự", 1.86m, 11000000m),
                ("NG_C1", "Kỹ thuật viên", 1.65m, 10000000m),
                ("NG_C2", "Nhân viên", 1.50m, 9000000m),
                ("NG_C3", "Công nhân kỹ thuật", 1.35m, 8000000m),
                ("NG_KS", "Kỹ sư công nghệ thực phẩm", 2.34m, 15000000m),
                ("NG_BS", "Bác sĩ thú y", 4.40m, 19000000m)
            };

            foreach (var sc in scaleSpecs)
            {
                var scale = new SalaryScale
                {
                    ScaleCode = sc.Code,
                    ScaleName = sc.Name,
                    ReviewCycleMonths = 24,
                    Description = $"Ngạch lương {sc.Name}"
                };
                for (int stepIdx = 1; stepIdx <= 5; stepIdx++)
                {
                    scale.Steps.Add(new SalaryStep
                    {
                        StepOrder = stepIdx,
                        StepName = $"Bậc {stepIdx}",
                        SalaryCoefficient = Math.Round(sc.StartCoeff + (stepIdx - 1) * 0.35m, 2),
                        StandardSalary = sc.BaseVnd + (stepIdx - 1) * 2500000m
                    });
                }
                await _context.SalaryScales.AddAsync(scale, cancellationToken);
            }

            var eduLevels = new List<EducationLevel>
            {
                new() { LevelCode = "TD01", LevelName = "Tiến sĩ", RankOrder = 1 },
                new() { LevelCode = "TD02", LevelName = "Thạc sĩ", RankOrder = 2 },
                new() { LevelCode = "TD03", LevelName = "Đại học", RankOrder = 3 },
                new() { LevelCode = "TD04", LevelName = "Cao đẳng", RankOrder = 4 },
                new() { LevelCode = "TD05", LevelName = "Trung cấp", RankOrder = 5 },
                new() { LevelCode = "TD06", LevelName = "THPT", RankOrder = 6 }
            };
            await _context.EducationLevels.AddRangeAsync(eduLevels, cancellationToken);

            var ktklCategories = new List<RewardDisciplineCategory>
            {
                new() { CategoryCode = "KT_BK", CategoryName = "Bằng khen Công ty", CategoryType = "Reward", DefaultAmount = 5000000m, Description = "Bằng khen của Tổng Giám đốc Vinamilk" },
                new() { CategoryCode = "KT_CSTD", CategoryName = "Chiến sĩ thi đua cơ sở", CategoryType = "Reward", DefaultAmount = 3000000m, Description = "Danh hiệu CSTĐ cấp cơ sở hàng năm" },
                new() { CategoryCode = "KT_LDTT", CategoryName = "Lao động tiên tiến", CategoryType = "Reward", DefaultAmount = 2000000m, Description = "Danh hiệu LĐTT hàng năm" },
                new() { CategoryCode = "KT_ST", CategoryName = "Sáng kiến - Cải tiến", CategoryType = "Reward", DefaultAmount = 10000000m, Description = "Thưởng sáng kiến cải tiến sản xuất" },
                new() { CategoryCode = "KT_TH", CategoryName = "Thưởng thành tích đột xuất", CategoryType = "Reward", DefaultAmount = 5000000m, Description = "Thưởng nóng cho thành tích xuất sắc" },
                new() { CategoryCode = "KL_KT", CategoryName = "Khiển trách", CategoryType = "Discipline", DefaultAmount = 500000m, Description = "Hình thức kỷ luật khiển trách" },
                new() { CategoryCode = "KL_CC", CategoryName = "Cảnh cáo", CategoryType = "Discipline", DefaultAmount = 2000000m, Description = "Hình thức kỷ luật cảnh cáo" },
                new() { CategoryCode = "KL_ST", CategoryName = "Sa thải", CategoryType = "Discipline", DefaultAmount = 0m, Description = "Chấm dứt hợp đồng lao động" }
            };
            await _context.RewardDisciplineCategories.AddRangeAsync(ktklCategories, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // 4. Seed Ca làm việc, Ngày lễ, Loại phép, Tham số Lương/BHXH/Thuế TNCN 7 Bậc
        if (!await _context.WorkShifts.AnyAsync(cancellationToken))
        {
            var shifts = new List<WorkShift>
            {
                new() { ShiftCode = "HC", ShiftName = "Ca Hành chính (08:00 - 17:00)", ShiftType = "Fixed", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0), BreakStartTime = new TimeOnly(12, 0), BreakEndTime = new TimeOnly(13, 0), IsBreakPaid = false, StandardHours = 8m, WorkdayValue = 1m },
                new() { ShiftCode = "CA1", ShiftName = "Ca 1 Sản xuất (06:00 - 14:00)", ShiftType = "Fixed", StartTime = new TimeOnly(6, 0), EndTime = new TimeOnly(14, 0), StandardHours = 8m, WorkdayValue = 1m },
                new() { ShiftCode = "CA2", ShiftName = "Ca 2 Sản xuất (14:00 - 22:00)", ShiftType = "Fixed", StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(22, 0), StandardHours = 8m, WorkdayValue = 1m },
                new() { ShiftCode = "CA3", ShiftName = "Ca 3 Đêm (22:00 - 06:00)", ShiftType = "Overnight", StartTime = new TimeOnly(22, 0), EndTime = new TimeOnly(6, 0), IsOvernight = true, StandardHours = 8m, WorkdayValue = 1m, NightShiftAllowanceRate = 0.30m },
                new() { ShiftCode = "LINH_HOAT", ShiftName = "Ca Linh hoạt (Core 09:30 - 15:30)", ShiftType = "Flexible", StartTime = new TimeOnly(7, 30), EndTime = new TimeOnly(18, 30), CoreStartTime = new TimeOnly(9, 30), CoreEndTime = new TimeOnly(15, 30), BreakStartTime = new TimeOnly(12, 0), BreakEndTime = new TimeOnly(13, 0), IsBreakPaid = false, StandardHours = 8m, WorkdayValue = 1m },
                new() { ShiftCode = "CA_GAY", ShiftName = "Ca Gãy (08:00-12:00 & 14:00-18:00)", ShiftType = "Split", StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(12, 0), SplitStartTime2 = new TimeOnly(14, 0), SplitEndTime2 = new TimeOnly(18, 0), IsBreakPaid = false, StandardHours = 8m, WorkdayValue = 1m },
                new() { ShiftCode = "THEO_GIO", ShiftName = "Ca Theo giờ (Part-time / Bán thời gian)", ShiftType = "Hourly", StartTime = new TimeOnly(7, 0), EndTime = new TimeOnly(22, 0), StandardHours = 4m, WorkdayValue = 0.5m }
            };
            await _context.WorkShifts.AddRangeAsync(shifts, cancellationToken);

            var leaveTypes = BuildDefaultLeaveTypes();
            await _context.LeaveTypes.AddRangeAsync(leaveTypes, cancellationToken);

            var payrollParams = new List<PayrollParameter>
            {
                new() { ParamCode = "STATUTORY_BASE_SALARY", ParamName = "Mức lương cơ sở Nhà nước", ParamValue = 2340000m, ValueUnit = "VND", EffectiveFrom = new DateOnly(2024, 7, 1), LegalReference = "Nghị định 73/2024/NĐ-CP" },
                new() { ParamCode = "SI_MAX_SALARY_CAP", ParamName = "Mức trần tiền lương đóng BHXH/BHYT (20 lần lương cơ sở)", ParamValue = 46800000m, ValueUnit = "VND", EffectiveFrom = new DateOnly(2024, 7, 1), LegalReference = "Luật BHXH & NĐ 73/2024/NĐ-CP" },
                new() { ParamCode = "TAX_SELF_DEDUCTION", ParamName = "Mức giảm trừ gia cảnh bản thân", ParamValue = 11000000m, ValueUnit = "VND", EffectiveFrom = new DateOnly(2020, 7, 1), LegalReference = "Nghị quyết 954/2020/UBTVQH14" },
                new() { ParamCode = "TAX_DEPENDENT_DEDUCTION", ParamName = "Mức giảm trừ mỗi người phụ thuộc", ParamValue = 4400000m, ValueUnit = "VND", EffectiveFrom = new DateOnly(2020, 7, 1), LegalReference = "Nghị quyết 954/2020/UBTVQH14" },
                new() { ParamCode = "SI_EMP_RATE", ParamName = "Tỷ lệ BHXH nhân viên đóng (8%)", ParamValue = 8.0m, ValueUnit = "Percent", EffectiveFrom = new DateOnly(2024, 1, 1) },
                new() { ParamCode = "HI_EMP_RATE", ParamName = "Tỷ lệ BHYT nhân viên đóng (1.5%)", ParamValue = 1.5m, ValueUnit = "Percent", EffectiveFrom = new DateOnly(2024, 1, 1) },
                new() { ParamCode = "UI_EMP_RATE", ParamName = "Tỷ lệ BHTN nhân viên đóng (1%)", ParamValue = 1.0m, ValueUnit = "Percent", EffectiveFrom = new DateOnly(2024, 1, 1) }
            };
            await _context.PayrollParameters.AddRangeAsync(payrollParams, cancellationToken);

            var taxBrackets = new List<TaxBracket>
            {
                new() { BracketLevel = 1, FromIncome = 0m, ToIncome = 5000000m, TaxRatePercent = 5m, QuickSubtractionAmount = 0m, EffectiveFrom = new DateOnly(2020, 1, 1) },
                new() { BracketLevel = 2, FromIncome = 5000000m, ToIncome = 10000000m, TaxRatePercent = 10m, QuickSubtractionAmount = 250000m, EffectiveFrom = new DateOnly(2020, 1, 1) },
                new() { BracketLevel = 3, FromIncome = 10000000m, ToIncome = 18000000m, TaxRatePercent = 15m, QuickSubtractionAmount = 750000m, EffectiveFrom = new DateOnly(2020, 1, 1) },
                new() { BracketLevel = 4, FromIncome = 18000000m, ToIncome = 32000000m, TaxRatePercent = 20m, QuickSubtractionAmount = 1650000m, EffectiveFrom = new DateOnly(2020, 1, 1) },
                new() { BracketLevel = 5, FromIncome = 32000000m, ToIncome = 52000000m, TaxRatePercent = 25m, QuickSubtractionAmount = 3250000m, EffectiveFrom = new DateOnly(2020, 1, 1) },
                new() { BracketLevel = 6, FromIncome = 52000000m, ToIncome = 80000000m, TaxRatePercent = 30m, QuickSubtractionAmount = 5850000m, EffectiveFrom = new DateOnly(2020, 1, 1) },
                new() { BracketLevel = 7, FromIncome = 80000000m, ToIncome = null, TaxRatePercent = 35m, QuickSubtractionAmount = 9850000m, EffectiveFrom = new DateOnly(2020, 1, 1) }
            };
            await _context.TaxBrackets.AddRangeAsync(taxBrackets, cancellationToken);

            var salaryComponents = new List<SalaryComponent>
            {
                new() { ComponentCode = "BASIC_PAY", ComponentName = "Lương cơ bản theo ngày công (P1)", ComponentType = "Earning", IsTaxable = true, IsSocialInsuranceBase = true, SortOrder = 1 },
                new() { ComponentCode = "POS_ALLOWANCE", ComponentName = "Phụ cấp chức vụ", ComponentType = "Allowance", IsTaxable = true, IsSocialInsuranceBase = true, SortOrder = 2 },
                new() { ComponentCode = "OTHER_ALLOWANCE", ComponentName = "Phụ cấp ăn ca, xăng xe, điện thoại (P2)", ComponentType = "Allowance", IsTaxable = true, TaxExemptLimit = 730000m, SortOrder = 3 },
                new() { ComponentCode = "KPI_BONUS", ComponentName = "Thưởng hiệu quả KPI (P3)", ComponentType = "Bonus", IsTaxable = true, SortOrder = 4 },
                new() { ComponentCode = "SI_DEDUCTION", ComponentName = "Khấu trừ BHXH, BHYT, BHTN (10.5%)", ComponentType = "Deduction", IsTaxable = false, SortOrder = 5 },
                new() { ComponentCode = "PIT_TAX", ComponentName = "Thuế Thu nhập cá nhân (TNCN)", ComponentType = "Deduction", IsTaxable = false, SortOrder = 6 }
            };
            await _context.SalaryComponents.AddRangeAsync(salaryComponents, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // 4b. Seed Tham số Lương tối thiểu 4 vùng, Lương cơ sở & Biểu thuế TNCN (Giai đoạn trước 2026 & Luật 2026)
        if (!await _context.PayrollStatutoryParameters.AnyAsync(cancellationToken))
        {
            await _context.PayrollStatutoryParameters.AddRangeAsync(BuildDefaultStatutoryParameters(), cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (!await _context.PitBrackets.AnyAsync(cancellationToken))
        {
            await _context.PitBrackets.AddRangeAsync(BuildDefaultPitBrackets(), cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // 5. Seed 55 Nhân viên từ database/vinamilk_hrm.sql + Hợp đồng + Người phụ thuộc + Công + Lương + Tài khoản
        if (!await _context.Employees.AnyAsync(cancellationToken))
        {
            var unitMap = await _context.OrganizationUnits.ToDictionaryAsync(u => u.UnitCode, u => u.Id, cancellationToken);
            var posMap = await _context.Positions.ToDictionaryAsync(p => p.PositionCode, p => p, cancellationToken);
            var eduMap = await _context.EducationLevels.ToDictionaryAsync(e => e.LevelName, e => e.Id, cancellationToken);
            var steps = await _context.SalarySteps.Include(s => s.SalaryScale).ToListAsync(cancellationToken);
            var annualLeaveType = await _context.LeaveTypes.FirstAsync(l => l.LeaveTypeCode == "ANNUAL", cancellationToken);

            var createdEmployees = new List<Employee>();

            foreach (var row in LegacyMysqlSeedData.Employees)
            {
                var pos = posMap[row.PositionCode];
                var step = steps.FirstOrDefault(s => s.SalaryScale.ScaleCode == row.ScaleCode && s.StepOrder == Math.Min(row.Step, 5))
                           ?? steps.First(s => s.SalaryScale.ScaleCode == row.ScaleCode);

                var hireDate = DateOnly.Parse(row.HireDate);
                var workStatus = row.Status switch
                {
                    1 => "Official",
                    2 => "Probation",
                    3 => "Resigned",
                    4 => "Retired",
                    _ => "Official"
                };

                var emp = new Employee
                {
                    EmployeeCode = row.Code,
                    FullName = row.FullName,
                    DateOfBirth = DateOnly.Parse(row.BirthDate),
                    Gender = row.Gender,
                    CitizenIdEncrypted = _encryptionService.Encrypt(row.CitizenId),
                    CitizenIdHash = _encryptionService.ComputeDeterministicHash(row.CitizenId),
                    CitizenIdIssueDate = DateOnly.Parse(row.IssueDate),
                    CitizenIdIssuePlace = row.IssuePlace,
                    PlaceOfOrigin = row.Origin,
                    PermanentAddress = row.Address,
                    CurrentAddress = row.Address,
                    MaritalStatus = row.MaritalStatus,
                    WorkEmail = row.Email,
                    PhoneNumber = row.Phone,
                    HireDate = hireDate,
                    OfficialDate = workStatus == "Probation" ? null : hireDate.AddMonths(2),
                    TerminationDate = workStatus is "Resigned" or "Retired" ? new DateOnly(2025, 12, 31) : null,
                    OrganizationUnitId = unitMap[row.UnitCode],
                    PositionId = pos.Id,
                    EducationLevelId = eduMap.TryGetValue(row.EduLevelName, out var eduId) ? eduId : null,
                    Major = row.Major,
                    SalaryStepId = step.Id,
                    BaseSalary = row.BaseSalary,
                    PositionAllowance = pos.DefaultPositionAllowance,
                    OtherAllowance = 3500000m,
                    BankAccountEncrypted = _encryptionService.Encrypt(row.BankAccount),
                    BankName = row.BankName,
                    TaxCodeEncrypted = _encryptionService.Encrypt(row.TaxCode),
                    WorkStatus = workStatus,
                    CurrentContractType = row.ContractType ?? "Indefinite",
                    IsUnionMember = workStatus == "Official"
                };

                // Tạo Hợp đồng lao động tương ứng
                emp.Contracts.Add(new LaborContract
                {
                    ContractNumber = $"HD-{row.Code}/2025/VNM",
                    ContractType = row.ContractType ?? "Indefinite",
                    SignDate = hireDate,
                    StartDate = hireDate,
                    EndDate = string.IsNullOrEmpty(row.ContractEndDate) ? null : DateOnly.Parse(row.ContractEndDate),
                    SalaryStepId = step.Id,
                    BaseSalary = row.BaseSalary,
                    PositionAllowance = pos.DefaultPositionAllowance,
                    OtherAllowance = 3500000m,
                    ProbationSalaryRate = workStatus == "Probation" ? 85m : 100m,
                    Status = workStatus is "Resigned" or "Retired" ? "Terminated" : "Active"
                });

                // Tạo Người phụ thuộc giảm trừ gia cảnh
                for (int d = 1; d <= row.Dependents; d++)
                {
                    emp.Dependents.Add(new EmployeeDependent
                    {
                        FullName = $"Người phụ thuộc {d} của {row.FullName}",
                        Relationship = d == 1 ? "Child" : "Mother",
                        DateOfBirth = new DateOnly(2015 + d, 5, 10),
                        IsTaxDeductible = true,
                        DeductionFromDate = new DateOnly(2022, 1, 1)
                    });
                }

                // Tạo Quỹ phép năm 2026
                var seniorityYears = Math.Max(0, 2026 - hireDate.Year);
                var extraLeave = seniorityYears / 5;
                emp.LeaveBalances.Add(new LeaveBalance
                {
                    LeaveTypeId = annualLeaveType.Id,
                    Year = 2026,
                    StandardEntitlement = 12m,
                    SeniorityExtraDays = extraLeave,
                    CarriedForwardDays = 2m,
                    UsedDays = 1m,
                    RemainingDays = 13m + extraLeave
                });

                // Tạo Consent Bảo vệ dữ liệu cá nhân (Nghị định 13/2023/NĐ-CP)
                createdEmployees.Add(emp);
            }

            await _context.Employees.AddRangeAsync(createdEmployees, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var empMap = createdEmployees.ToDictionary(e => e.EmployeeCode, e => e);

            // Seed Quá trình công tác, Bổ nhiệm/Điều chuyển, Khen thưởng/Kỷ luật
            var histories = new List<EmploymentHistory>
            {
                new() { EmployeeId = empMap["VNM-0001"].Id, FromDate = new DateOnly(2002, 6, 1), ToDate = new DateOnly(2008, 12, 31), DepartmentName = "Nhà máy Sữa Tiên Sơn", PositionName = "Kỹ sư Sản xuất", Notes = "Vận hành dây chuyền sản xuất sữa tươi" },
                new() { EmployeeId = empMap["VNM-0001"].Id, FromDate = new DateOnly(2009, 1, 1), ToDate = new DateOnly(2015, 6, 30), DepartmentName = "Nhà máy Sữa Tiên Sơn", PositionName = "Quản đốc Phân xưởng", Notes = "Quản lý phân xưởng chế biến" },
                new() { EmployeeId = empMap["VNM-0001"].Id, FromDate = new DateOnly(2015, 7, 1), ToDate = null, DepartmentName = "Nhà máy Sữa Tiên Sơn", PositionName = "Giám đốc Nhà máy", Notes = "Điều hành toàn bộ hoạt động nhà máy" },
                new() { EmployeeId = empMap["VNM-0020"].Id, FromDate = new DateOnly(2005, 1, 10), ToDate = new DateOnly(2016, 6, 30), DepartmentName = "Phòng Nhân sự", PositionName = "Trưởng phòng Nhân sự", Notes = "Quản lý phòng Nhân sự" },
                new() { EmployeeId = empMap["VNM-0020"].Id, FromDate = new DateOnly(2016, 7, 1), ToDate = null, DepartmentName = "Phòng Nhân sự", PositionName = "Giám đốc Nhân sự (CHRO)", Notes = "Hoạch định chiến lược nhân sự toàn công ty" }
            };
            await _context.EmploymentHistories.AddRangeAsync(histories, cancellationToken);

            var ktklMap = await _context.RewardDisciplineCategories.ToDictionaryAsync(c => c.CategoryCode, c => c.Id, cancellationToken);
            var rewards = new List<RewardDiscipline>
            {
                new() { DecisionNumber = "QĐ-KT-2025/001", EmployeeId = empMap["VNM-0001"].Id, CategoryId = ktklMap["KT_BK"], Type = "Reward", DecisionDate = new DateOnly(2025, 1, 15), EffectiveDate = new DateOnly(2025, 1, 15), Amount = 5000000m, Reason = "Hoàn thành xuất sắc kế hoạch sản xuất năm 2024" },
                new() { DecisionNumber = "QĐ-KT-2024/009", EmployeeId = empMap["VNM-0009"].Id, CategoryId = ktklMap["KT_ST"], Type = "Reward", DecisionDate = new DateOnly(2024, 6, 15), EffectiveDate = new DateOnly(2024, 6, 15), Amount = 10000000m, Reason = "Áp dụng công nghệ IoT giám sát sức khỏe bò sữa" },
                new() { DecisionNumber = "QĐ-KT-2025/024", EmployeeId = empMap["VNM-0024"].Id, CategoryId = ktklMap["KT_BK"], Type = "Reward", DecisionDate = new DateOnly(2025, 1, 15), EffectiveDate = new DateOnly(2025, 1, 15), Amount = 5000000m, Reason = "Quản lý tài chính hiệu quả, tiết kiệm 5% ngân sách" },
                new() { DecisionNumber = "QĐ-KL-2025/016", EmployeeId = empMap["VNM-0016"].Id, CategoryId = ktklMap["KL_KT"], Type = "Discipline", DecisionDate = new DateOnly(2025, 5, 10), EffectiveDate = new DateOnly(2025, 5, 10), Amount = 500000m, Reason = "Đi trễ 5 lần trong tháng 4/2025" },
                new() { DecisionNumber = "QĐ-KT-2026/030", EmployeeId = empMap["VNM-0030"].Id, CategoryId = ktklMap["KT_TH"], Type = "Reward", DecisionDate = new DateOnly(2026, 4, 1), EffectiveDate = new DateOnly(2026, 4, 1), Amount = 5000000m, Reason = "Triển khai thành công hệ thống HRM mới" }
            };
            await _context.RewardDisciplines.AddRangeAsync(rewards, cancellationToken);

            // Seed Chấm công tháng 08/2026 & Kỳ lương tháng 08/2026
            var payrollPeriod = new PayrollPeriod
            {
                PeriodCode = "PAY-2026-08",
                Month = 8,
                Year = 2026,
                PeriodTitle = "Kỳ lương Tháng 08/2026",
                StartDate = new DateOnly(2026, 8, 1),
                EndDate = new DateOnly(2026, 8, 31),
                StandardWorkdays = 22m,
                Status = "Locked",
                IsLocked = true,
                CalculatedAt = DateTime.UtcNow,
                ApprovedAt = DateTime.UtcNow
            };
            await _context.PayrollPeriods.AddAsync(payrollPeriod, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var samplePayrollSpecs = new (string Code, decimal Workdays, decimal LeaveDays, decimal OtHours, decimal BasePay, decimal PosAllow, decimal OtherAllow, decimal KpiBonus, decimal Gross, decimal EmpSi, decimal Pit, decimal Net)[]
            {
                ("VNM-0001", 22.0m, 0m, 8m, 35000000m, 12000000m, 26000000m, 12000000m, 85000000m, 3780000m, 8900000m, 72320000m),
                ("VNM-0002", 21.0m, 1m, 4m, 22000000m, 5000000m, 17000000m, 8000000m, 52000000m, 2457000m, 4200000m, 45343000m),
                ("VNM-0003", 22.0m, 0m, 12m, 14000000m, 3000000m, 10000000m, 5000000m, 32000000m, 1556100m, 1800000m, 28643900m),
                ("VNM-0004", 21.5m, 0m, 16m, 9500000m, 1500000m, 6000000m, 2500000m, 19500000m, 1050000m, 600000m, 17850000m),
                ("VNM-0006", 22.0m, 0m, 10m, 18000000m, 4000000m, 13500000m, 7000000m, 42500000m, 2003400m, 3200000m, 37296600m),
                ("VNM-0009", 22.0m, 0m, 6m, 32000000m, 10000000m, 22000000m, 10000000m, 74000000m, 3561600m, 7500000m, 62938400m),
                ("VNM-0014", 20.0m, 2m, 4m, 30000000m, 15000000m, 23000000m, 15000000m, 83000000m, 3339000m, 8600000m, 71061000m),
                ("VNM-0020", 21.0m, 1m, 8m, 45000000m, 15000000m, 30000000m, 15000000m, 105000000m, 3780000m, 13500000m, 87720000m),
                ("VNM-0024", 22.0m, 0m, 6m, 48000000m, 15000000m, 34000000m, 18000000m, 115000000m, 3780000m, 15800000m, 95420000m),
                ("VNM-0028", 21.0m, 1m, 10m, 40000000m, 15000000m, 28000000m, 12000000m, 95000000m, 3780000m, 11500000m, 79720000m),
                ("VNM-0030", 22.0m, 0m, 20m, 28000000m, 8000000m, 20000000m, 10000000m, 66000000m, 3116400m, 6200000m, 56683600m)
            };

            var seedStatutoryContext = await VietnamPayrollEngine.ResolveStatutoryContextAsync(
                _context, payrollPeriod.EndDate, cancellationToken);
            var orgRegionMap = await _context.OrganizationUnits
                .AsNoTracking()
                .ToDictionaryAsync(u => u.Id, u => u.WageRegion, cancellationToken);

            foreach (var sp in samplePayrollSpecs)
            {
                var emp = empMap[sp.Code];
                var attSummary = new MonthlyAttendanceSummary
                {
                    EmployeeId = emp.Id,
                    Month = 8,
                    Year = 2026,
                    StandardWorkdays = 22m,
                    ActualWorkdays = sp.Workdays,
                    PaidLeaveDays = sp.LeaveDays,
                    TotalOtHours150 = sp.OtHours,
                    TotalPayableWorkdays = sp.Workdays + sp.LeaveDays,
                    IsLocked = true,
                    LockedAt = DateTime.UtcNow,
                    LockedBy = "huyen.ntt"
                };
                await _context.MonthlyAttendanceSummaries.AddAsync(attSummary, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                var depCount = emp.Dependents.Count(d => d.IsTaxDeductible);
                var wageRegion = emp.WageRegion ?? (orgRegionMap.TryGetValue(emp.OrganizationUnitId, out var r) ? r : 1);
                var breakdown = VietnamPayrollEngine.Calculate(new PayrollCalculationInput(
                    sp.BasePay,
                    sp.PosAllow,
                    sp.OtherAllow,
                    22m,
                    sp.Workdays + sp.LeaveDays,
                    sp.OtHours,
                    0m,
                    0m,
                    sp.KpiBonus,
                    0m,
                    depCount,
                    emp.IsUnionMember,
                    wageRegion), seedStatutoryContext);

                var record = new PayrollRecord
                {
                    PayrollPeriodId = payrollPeriod.Id,
                    EmployeeId = emp.Id,
                    OrganizationUnitId = emp.OrganizationUnitId,
                    PositionId = emp.PositionId,
                    MonthlyAttendanceSummaryId = attSummary.Id,
                    StandardWorkdays = 22m,
                    PayableWorkdays = sp.Workdays + sp.LeaveDays,
                    OtHours150 = sp.OtHours,
                    TotalOtHours = sp.OtHours,
                    ContractBaseSalary = sp.BasePay,
                    ActualBaseSalary = sp.BasePay,
                    PositionAllowance = sp.PosAllow,
                    OtherAllowance = sp.OtherAllow,
                    OvertimePay = breakdown.OvertimePay,
                    KpiBonus = sp.KpiBonus,
                    GrossIncome = breakdown.GrossIncome,
                    InsuranceSalaryBase = breakdown.SocialInsuranceBase,
                    UnemploymentInsuranceBase = breakdown.UnemploymentInsuranceBase,
                    EmployeeSocialInsurance = breakdown.EmployeeSocialInsurance,
                    EmployeeHealthInsurance = breakdown.EmployeeHealthInsurance,
                    EmployeeUnemploymentInsurance = breakdown.EmployeeUnemploymentInsurance,
                    TotalEmployeeInsurance = breakdown.TotalEmployeeInsurance,
                    CompanySocialInsurance = breakdown.CompanySocialInsurance,
                    CompanyHealthInsurance = breakdown.CompanyHealthInsurance,
                    CompanyUnemploymentInsurance = breakdown.CompanyUnemploymentInsurance,
                    CompanyOccupationalAccidentInsurance = breakdown.CompanyOccupationalAccidentInsurance,
                    CompanyUnionFee = breakdown.CompanyUnionFee,
                    TotalCompanyCost = breakdown.TotalCompanyCost,
                    OtTaxExemptAmount = breakdown.OtTaxExemptAmount,
                    DependentCount = depCount,
                    SelfDeductionAmount = breakdown.SelfDeductionAmount,
                    DependentDeductionAmount = breakdown.DependentDeductionAmount,
                    TaxableIncome = breakdown.TaxableIncome,
                    AssessableIncome = breakdown.AssessableIncome,
                    PersonalIncomeTax = breakdown.PersonalIncomeTax,
                    DisciplineDeduction = 0m,
                    NetSalary = breakdown.NetSalary,
                    PaymentStatus = "Paid",
                    PaidAt = DateTime.UtcNow
                };
                await _context.PayrollRecords.AddAsync(record, cancellationToken);
            }

            // Seed Tài khoản đăng nhập (PBKDF2-HMACSHA256, bắt buộc đổi mật khẩu ở lần đầu)
            var rolesByCode = await _context.Roles.ToDictionaryAsync(r => r.RoleCode, r => r.Id, cancellationToken);
            var isDevelopment = _hostEnvironment?.IsDevelopment() == true;
            string? initialSuperAdminPassword = null;
            string? devSamplePassword = null;

            if (isDevelopment)
            {
                devSamplePassword = _configuration?["Security:DemoSeedPassword"];
                if (string.IsNullOrWhiteSpace(devSamplePassword))
                    devSamplePassword = "Vinamilk@2026";
            }
            else
            {
                initialSuperAdminPassword =
                    Environment.GetEnvironmentVariable(InitialSuperAdminPasswordEnvVar)
                    ?? Environment.GetEnvironmentVariable("Security__InitialSuperAdminPassword");

                if (string.IsNullOrWhiteSpace(initialSuperAdminPassword))
                {
                    throw new InvalidOperationException(
                        $"LỖI KHỞI ĐỘNG BẢO MẬT (Production): Thiếu biến môi trường '{InitialSuperAdminPasswordEnvVar}' cho mật khẩu SuperAdmin ban đầu.");
                }
            }

            var userSpecs = new (string Username, string Email, string? EmpCode, string RoleCode)[]
            {
                ("admin", "admin@vinamilk.com.vn", null, "SUPER_ADMIN"),
                ("huyen.ntt", "huyen.ntt@vinamilk.com.vn", "VNM-0020", "HR_MANAGER"),
                ("dung.la", "dung.la@vinamilk.com.vn", "VNM-0021", "HR_STAFF"),
                ("long.th", "long.th@vinamilk.com.vn", "VNM-0022", "HR_STAFF"),
                ("anh.lp", "anh.lp@vinamilk.com.vn", "VNM-0043", "HR_STAFF"),
                ("tri.dm", "tri.dm@vinamilk.com.vn", "VNM-0024", "ACCOUNTANT"),
                ("hung.nv", "hung.nv@vinamilk.com.vn", "VNM-0001", "LINE_MANAGER"),
                ("son.bt", "son.bt@vinamilk.com.vn", "VNM-0009", "LINE_MANAGER"),
                ("phuc.nm", "phuc.nm@vinamilk.com.vn", "VNM-0030", "LINE_MANAGER"),
                ("tung.pt", "tung.pt@vinamilk.com.vn", "VNM-0016", "EMPLOYEE")
            };

            foreach (var u in userSpecs)
            {
                var rawPassword = isDevelopment
                    ? devSamplePassword!
                    : (u.RoleCode == "SUPER_ADMIN"
                        ? initialSuperAdminPassword!
                        : Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));

                var user = new User
                {
                    Username = u.Username,
                    Email = u.Email,
                    PasswordHash = _passwordService.HashPassword(rawPassword),
                    EmployeeId = u.EmpCode != null ? empMap[u.EmpCode].Id : null,
                    IsActive = true,
                    MustChangePassword = true,
                    IsSystemAccount = false
                };
                user.UserRoles.Add(new UserRole { RoleId = rolesByCode[u.RoleCode] });
                await _context.Users.AddAsync(user, cancellationToken);
            }

            // Seed tài khoản hệ thống riêng cho background job (Hangfire) với 0 Role & IsSystemAccount = true
            var nonInteractiveSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            await _context.Users.AddAsync(new User
            {
                Username = SystemJobAccount.Username,
                Email = SystemJobAccount.Email,
                PasswordHash = _passwordService.HashPassword(nonInteractiveSecret),
                EmployeeId = null,
                IsActive = true,
                MustChangePassword = true,
                IsSystemAccount = true
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Đã Seed thành công 55 nhân viên Vinamilk HRM cùng toàn bộ danh mục và nghiệp vụ.");
        }

        // Idempotent seed cho CL1: Tham số pháp lý chấm công, Nhóm chính sách mặc định và Mẫu xoay ca 3 ca 4 kíp
        if (!await _context.AttendanceStatutoryParameters.AnyAsync(cancellationToken))
        {
            await _context.AttendanceStatutoryParameters.AddRangeAsync(BuildDefaultAttendanceStatutoryParameters(), cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (!await _context.PolicyGroups.AnyAsync(cancellationToken))
        {
            var defaultGroup = BuildDefaultCompanyPolicyGroup();
            await _context.PolicyGroups.AddAsync(defaultGroup, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (!await _context.ShiftRotationPatterns.AnyAsync(cancellationToken))
        {
            await _context.ShiftRotationPatterns.AddRangeAsync(BuildDefaultShiftRotationPatterns(), cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Idempotent seed cho CL2: Điểm Geofence & Thiết bị máy chấm công
        if (!await _context.GeofenceLocations.AnyAsync(cancellationToken))
        {
            await _context.GeofenceLocations.AddRangeAsync(BuildDefaultGeofenceLocations(), cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (!await _context.AttendanceDevices.AnyAsync(cancellationToken))
        {
            await _context.AttendanceDevices.AddRangeAsync(BuildDefaultAttendanceDevices(), cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Idempotent seed cho CL3: Danh mục ký hiệu công chuẩn hóa cấu hình được
        if (!await _context.AttendanceSymbolDefinitions.AnyAsync(cancellationToken))
        {
            await _context.AttendanceSymbolDefinitions.AddRangeAsync(BuildDefaultAttendanceSymbols(), cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Idempotent seed cho CL4: Bổ sung các loại nghỉ theo BLLĐ 2019 (chính sách nghỉ việc riêng, nghỉ bù OT, công tác)
        var existingLeaveCodes = await _context.LeaveTypes.Select(l => l.LeaveTypeCode).ToListAsync(cancellationToken);
        var defaultLeaveTypes = BuildDefaultLeaveTypes();
        var missingLeaveTypes = defaultLeaveTypes.Where(l => !existingLeaveCodes.Contains(l.LeaveTypeCode)).ToList();
        if (missingLeaveTypes.Count > 0)
        {
            await _context.LeaveTypes.AddRangeAsync(missingLeaveTypes, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Idempotent seed cho CL5: Tham số miễn thuế, Thành phần lương công thức NCalc, 3 Mẫu cấu trúc lương & Hồ sơ lương nhân viên
        await EnsureSalaryStructureSeededAsync(cancellationToken);
    }

    private async Task EnsureSalaryStructureSeededAsync(CancellationToken cancellationToken)
    {
        // 1. Tham số miễn thuế & mức tối thiểu thử việc (hỗ trợ EffectiveFrom/EffectiveTo & LegalReference)
        var existingParamCodes = await _context.PayrollParameters
            .Select(p => p.ParamCode)
            .Distinct()
            .ToListAsync(cancellationToken);
        var cl5Params = BuildDefaultTaxExemptPayrollParameters()
            .Where(p => !existingParamCodes.Contains(p.ParamCode, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (cl5Params.Count > 0)
        {
            await _context.PayrollParameters.AddRangeAsync(cl5Params, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // 2. Thành phần lương NCalc đa tầng + tham chiếu miễn thuế
        var defaultComponents = BuildDefaultSalaryComponentsCl5();
        var existingComponents = await _context.SalaryComponents.ToListAsync(cancellationToken);
        var compByCode = existingComponents.ToDictionary(c => c.ComponentCode, StringComparer.OrdinalIgnoreCase);
        bool compChanged = false;

        foreach (var def in defaultComponents)
        {
            if (!compByCode.TryGetValue(def.ComponentCode, out var existing))
            {
                await _context.SalaryComponents.AddAsync(def, cancellationToken);
                compByCode[def.ComponentCode] = def;
                compChanged = true;
            }
            else if (string.IsNullOrWhiteSpace(existing.LegalReference) && !string.IsNullOrWhiteSpace(def.LegalReference))
            {
                existing.CalculationType = def.CalculationType;
                existing.FormulaExpression ??= def.FormulaExpression;
                existing.TaxExemptLimitParameterCode ??= def.TaxExemptLimitParameterCode;
                existing.LegalReference = def.LegalReference;
                existing.NeedsHrVerification = def.NeedsHrVerification;
                existing.Description ??= def.Description;
                compChanged = true;
            }
        }
        if (compChanged)
        {
            await _context.SaveChangesAsync(cancellationToken);
            compByCode = (await _context.SalaryComponents.ToListAsync(cancellationToken))
                .ToDictionary(c => c.ComponentCode, StringComparer.OrdinalIgnoreCase);
        }

        // 3. 3 Mẫu cấu trúc lương theo nhóm đối tượng (Văn phòng / Nhà máy / Kinh doanh)
        var existingTemplateCodes = await _context.SalaryTemplates
            .Select(t => t.TemplateCode)
            .ToListAsync(cancellationToken);
        var cl5Templates = BuildDefaultSalaryTemplatesWithComponents(compByCode)
            .Where(t => !existingTemplateCodes.Contains(t.TemplateCode, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (cl5Templates.Count > 0)
        {
            await _context.SalaryTemplates.AddRangeAsync(cl5Templates, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // 4. Khởi tạo hồ sơ lương theo ngày hiệu lực cho các nhân viên hiện có nếu chưa có hồ sơ
        if (!await _context.EmployeeSalaryProfiles.AnyAsync(cancellationToken))
        {
            var templatesByCode = await _context.SalaryTemplates
                .AsNoTracking()
                .ToDictionaryAsync(t => t.TemplateCode, StringComparer.OrdinalIgnoreCase, cancellationToken);

            var employees = await _context.Employees
                .Include(e => e.OrganizationUnit)
                .Include(e => e.Position)
                .Include(e => e.SalaryStep)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (employees.Count > 0 && templatesByCode.Count > 0)
            {
                var defaultTpl = templatesByCode.Values.FirstOrDefault(t => t.IsDefaultForCompany) ?? templatesByCode.Values.First();
                templatesByCode.TryGetValue("FACTORY_SHIFT", out var factoryTpl);
                templatesByCode.TryGetValue("SALES_COMMISSION", out var salesTpl);

                var profiles = new List<EmployeeSalaryProfile>(employees.Count);
                foreach (var emp in employees)
                {
                    var unitCode = emp.OrganizationUnit?.UnitCode ?? string.Empty;
                    var unitName = emp.OrganizationUnit?.UnitName ?? string.Empty;
                    var posName = emp.Position?.PositionName ?? string.Empty;

                    SalaryTemplate chosenTpl = defaultTpl;
                    if (factoryTpl != null && (unitCode.StartsWith("NM", StringComparison.OrdinalIgnoreCase)
                        || unitCode.StartsWith("TT", StringComparison.OrdinalIgnoreCase)
                        || unitName.Contains("Nhà máy", StringComparison.OrdinalIgnoreCase)
                        || unitName.Contains("Trang trại", StringComparison.OrdinalIgnoreCase)
                        || posName.Contains("Công nhân", StringComparison.OrdinalIgnoreCase)))
                    {
                        chosenTpl = factoryTpl;
                    }
                    else if (salesTpl != null && (unitName.Contains("Kinh doanh", StringComparison.OrdinalIgnoreCase)
                        || unitCode.StartsWith("KD", StringComparison.OrdinalIgnoreCase)
                        || posName.Contains("Kinh doanh", StringComparison.OrdinalIgnoreCase)
                        || posName.Contains("Bán hàng", StringComparison.OrdinalIgnoreCase)))
                    {
                        chosenTpl = salesTpl;
                    }

                    bool isProbation = string.Equals(emp.WorkStatus, "Probation", StringComparison.OrdinalIgnoreCase);
                    profiles.Add(new EmployeeSalaryProfile
                    {
                        EmployeeId = emp.Id,
                        EffectiveFrom = emp.HireDate,
                        EffectiveTo = null,
                        BaseSalary = emp.BaseSalary,
                        InsuranceSalary = emp.BaseSalary + emp.PositionAllowance,
                        SalaryScaleId = emp.SalaryStep?.SalaryScaleId,
                        SalaryStepId = emp.SalaryStepId,
                        SalaryTemplateId = chosenTpl.Id,
                        PayMode = "Monthly",
                        UnitRate = 0m,
                        IsProbation = isProbation,
                        ProbationRatePercent = isProbation ? 85.00m : 100.00m,
                        PositionAllowance = emp.PositionAllowance,
                        OtherAllowance = emp.OtherAllowance,
                        IsUnionMember = emp.IsUnionMember,
                        DecisionNumber = $"HĐLĐ-{emp.EmployeeCode}",
                        Reason = "Khởi tạo hồ sơ lương theo hiệu lực từ dữ liệu hợp đồng gốc",
                        IsActive = true
                    });
                }

                await _context.EmployeeSalaryProfiles.AddRangeAsync(profiles, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public static List<AttendanceSymbolDefinition> BuildDefaultAttendanceSymbols() =>
    [
        new() { SymbolTypeCode = "Work", SymbolCode = "X", SymbolName = "Đi làm đủ công", Category = "Work", WorkdayMultiplier = 1.0m, IsPaidByCompany = true, IsPaidBySocialInsurance = false, ColorHex = "#198754", CssClass = "bg-success-subtle text-success fw-semibold", DisplayOrder = 1 },
        new() { SymbolTypeCode = "HalfWork", SymbolCode = "½", SymbolName = "Đi làm nửa công", Category = "Work", WorkdayMultiplier = 0.5m, IsPaidByCompany = true, IsPaidBySocialInsurance = false, ColorHex = "#f59e0b", CssClass = "bg-warning-subtle text-warning-emphasis fw-semibold", DisplayOrder = 2 },
        new() { SymbolTypeCode = "AnnualLeave", SymbolCode = "P", SymbolName = "Nghỉ phép năm hưởng lương", Category = "PaidLeave", WorkdayMultiplier = 1.0m, IsPaidByCompany = true, IsPaidBySocialInsurance = false, ColorHex = "#0d6efd", CssClass = "bg-primary-subtle text-primary fw-bold", DisplayOrder = 3 },
        new() { SymbolTypeCode = "Holiday", SymbolCode = "L", SymbolName = "Nghỉ Lễ / Tết hưởng nguyên lương", Category = "Holiday", WorkdayMultiplier = 1.0m, IsPaidByCompany = true, IsPaidBySocialInsurance = false, ColorHex = "#dc3545", CssClass = "bg-danger text-white fw-bold", DisplayOrder = 4 },
        new() { SymbolTypeCode = "Sick", SymbolCode = "Ô", SymbolName = "Nghỉ ốm hưởng BHXH", Category = "SocialInsurance", WorkdayMultiplier = 1.0m, IsPaidByCompany = false, IsPaidBySocialInsurance = true, ColorHex = "#ffc107", CssClass = "bg-warning text-dark fw-semibold", DisplayOrder = 5 },
        new() { SymbolTypeCode = "Maternity", SymbolCode = "TS", SymbolName = "Nghỉ thai sản hưởng BHXH", Category = "SocialInsurance", WorkdayMultiplier = 1.0m, IsPaidByCompany = false, IsPaidBySocialInsurance = true, ColorHex = "#fd7e14", CssClass = "bg-warning text-dark fw-semibold", DisplayOrder = 6 },
        new() { SymbolTypeCode = "BusinessTrip", SymbolCode = "CT", SymbolName = "Đi công tác (hưởng nguyên công)", Category = "Work", WorkdayMultiplier = 1.0m, IsPaidByCompany = true, IsPaidBySocialInsurance = false, ColorHex = "#0dcaf0", CssClass = "bg-info-subtle text-info-emphasis fw-bold", DisplayOrder = 7 },
        new() { SymbolTypeCode = "PersonalPaid", SymbolCode = "R", SymbolName = "Nghỉ việc riêng có lương (hiếu/hỷ)", Category = "PersonalPaid", WorkdayMultiplier = 1.0m, IsPaidByCompany = true, IsPaidBySocialInsurance = false, ColorHex = "#20c997", CssClass = "bg-info-subtle text-dark fw-semibold", DisplayOrder = 8 },
        new() { SymbolTypeCode = "PersonalUnpaid", SymbolCode = "Ro", SymbolName = "Nghỉ việc riêng không lương", Category = "UnpaidLeave", WorkdayMultiplier = 0m, IsPaidByCompany = false, IsPaidBySocialInsurance = false, ColorHex = "#6c757d", CssClass = "bg-secondary-subtle text-secondary fw-semibold", DisplayOrder = 9 },
        new() { SymbolTypeCode = "UnexcusedAbsent", SymbolCode = "KL", SymbolName = "Nghỉ không phép / Vắng không lý do", Category = "Absent", WorkdayMultiplier = 0m, IsPaidByCompany = false, IsPaidBySocialInsurance = false, ColorHex = "#212529", CssClass = "bg-dark-subtle text-dark fw-bold", DisplayOrder = 10 },
        new() { SymbolTypeCode = "WeeklyOff", SymbolCode = "OFF", SymbolName = "Nghỉ tuần theo lịch ca", Category = "Off", WorkdayMultiplier = 0m, IsPaidByCompany = false, IsPaidBySocialInsurance = false, ColorHex = "#adb5bd", CssClass = "bg-light text-muted", DisplayOrder = 11 },
        new() { SymbolTypeCode = "MissedPunch", SymbolCode = "TC", SymbolName = "Thiếu chấm vào/ra chưa giải trình", Category = "Missing", WorkdayMultiplier = 0m, IsPaidByCompany = false, IsPaidBySocialInsurance = false, ColorHex = "#dc3545", CssClass = "bg-danger-subtle text-danger fw-bold border border-danger", DisplayOrder = 12 },
        new() { SymbolTypeCode = "PendingGeofence", SymbolCode = "CGD", SymbolName = "Chấm ngoài vùng GPS chờ duyệt", Category = "Missing", WorkdayMultiplier = 0m, IsPaidByCompany = false, IsPaidBySocialInsurance = false, ColorHex = "#ffc107", CssClass = "bg-warning text-dark fw-bold", DisplayOrder = 13 }
    ];

    public static List<GeofenceLocation> BuildDefaultGeofenceLocations() =>
    [
        new() { LocationCode = "HQ_SG", LocationName = "Trụ sở chính Vinamilk Tower (Q.7, TP.HCM)", Address = "Số 10 Tân Trào, Phường Tân Phú, Quận 7, TP.HCM", Latitude = 10.7296m, Longitude = 106.7218m, AllowedRadiusMeters = 300d, OutsideGeofenceAction = "PendingApproval", QrRotationSeconds = 30, IsActive = true },
        new() { LocationCode = "NM_BD", LocationName = "Nhà máy Sữa Việt Nam - Mega Factory (Bình Dương)", Address = "KCN Mỹ Phước 2, Bến Cát, Bình Dương", Latitude = 11.1154m, Longitude = 106.6122m, AllowedRadiusMeters = 500d, OutsideGeofenceAction = "Reject", QrRotationSeconds = 30, IsActive = true },
        new() { LocationCode = "NM_TS", LocationName = "Nhà máy Sữa Tiên Sơn (Bắc Ninh)", Address = "KCN Tiên Sơn, Tiên Du, Bắc Ninh", Latitude = 21.1189m, Longitude = 105.9864m, AllowedRadiusMeters = 500d, OutsideGeofenceAction = "Reject", QrRotationSeconds = 30, IsActive = true },
        new() { LocationCode = "CN_HN", LocationName = "Chi nhánh Vinamilk Hà Nội (Cầu Giấy)", Address = "Tháp Thành Công, Dịch Vọng Hậu, Cầu Giấy, Hà Nội", Latitude = 21.0285m, Longitude = 105.7892m, AllowedRadiusMeters = 300d, OutsideGeofenceAction = "PendingApproval", QrRotationSeconds = 30, IsActive = true },
        new() { LocationCode = "TT_TN", LocationName = "Trang trại Bò sữa Tây Ninh (Resort Bò sữa)", Address = "Xã Long Khánh, Huyện Bến Cầu, Tây Ninh", Latitude = 11.1245m, Longitude = 106.1520m, AllowedRadiusMeters = 800d, OutsideGeofenceAction = "PendingApproval", QrRotationSeconds = 30, IsActive = true }
    ];

    public static List<AttendanceDevice> BuildDefaultAttendanceDevices() =>
    [
        new() { DeviceCode = "ZKTECO-F22-GATE1", DeviceName = "Máy vân tay ZKTeco Cổng 1 Mega Factory (File Export CSV/TXT/DAT)", ConnectorType = "FileExport", LocationName = "Nhà máy Sữa Bình Dương", SyncIntervalMinutes = 15, IsActive = true },
        new() { DeviceCode = "HIKVISION-FACE-HQ", DeviceName = "Máy nhận diện khuôn mặt Hikvision Trụ sở chính", ConnectorType = "FileExport", LocationName = "Trụ sở chính Vinamilk Tower", SyncIntervalMinutes = 15, IsActive = true }
    ];

    public static List<LeaveType> BuildDefaultLeaveTypes() =>
    [
        new()
        {
            LeaveTypeCode = "ANNUAL",
            LeaveTypeName = "Nghỉ phép năm hưởng nguyên lương",
            IsPaidByCompany = true,
            IsPaidBySocialInsurance = false,
            CountsAgainstAnnualLeave = true,
            AllowHalfDay = true,
            AllowHourly = true,
            RequiresAttachment = false,
            MaxDaysPerRequest = 15m,
            MaxDaysPerYear = 12m,
            AllowCarryForward = true,
            AttendanceSymbol = "P",
            DisplayOrder = 1,
            IsActive = true,
            LegalReference = "Điều 113 & 114 Bộ luật Lao động 45/2019/QH14 (12 ngày cơ bản + 1 ngày / 5 năm thâm niên)",
            NeedsHrVerification = true,
            Description = "Phép năm tiêu chuẩn; phần nghỉ chưa dùng có thể chuyển năm theo chính sách công ty hoặc thanh toán khi nghỉ việc."
        },
        new()
        {
            LeaveTypeCode = "MARRIAGE",
            LeaveTypeName = "Nghỉ việc riêng: Bản thân kết hôn (Hưởng nguyên lương)",
            IsPaidByCompany = true,
            IsPaidBySocialInsurance = false,
            CountsAgainstAnnualLeave = false,
            AllowHalfDay = false,
            AllowHourly = false,
            RequiresAttachment = true,
            MaxDaysPerRequest = 3m,
            MaxDaysPerYear = 3m,
            AllowCarryForward = false,
            AttendanceSymbol = "R",
            DisplayOrder = 2,
            IsActive = true,
            LegalReference = "Điều 115 khoản 1 điểm a Bộ luật Lao động 45/2019/QH14",
            NeedsHrVerification = true,
            Description = "Người lao động kết hôn được nghỉ 03 ngày hưởng nguyên lương."
        },
        new()
        {
            LeaveTypeCode = "CHILD_MARRIAGE",
            LeaveTypeName = "Nghỉ việc riêng: Con đẻ/con nuôi kết hôn (Hưởng nguyên lương)",
            IsPaidByCompany = true,
            IsPaidBySocialInsurance = false,
            CountsAgainstAnnualLeave = false,
            AllowHalfDay = false,
            AllowHourly = false,
            RequiresAttachment = true,
            MaxDaysPerRequest = 1m,
            MaxDaysPerYear = 2m,
            AllowCarryForward = false,
            AttendanceSymbol = "R",
            DisplayOrder = 3,
            IsActive = true,
            LegalReference = "Điều 115 khoản 1 điểm b Bộ luật Lao động 45/2019/QH14",
            NeedsHrVerification = true,
            Description = "Con đẻ, con nuôi kết hôn được nghỉ 01 ngày hưởng nguyên lương."
        },
        new()
        {
            LeaveTypeCode = "BEREAVEMENT",
            LeaveTypeName = "Nghỉ việc riêng: Tang chế người thân (Hưởng nguyên lương)",
            IsPaidByCompany = true,
            IsPaidBySocialInsurance = false,
            CountsAgainstAnnualLeave = false,
            AllowHalfDay = false,
            AllowHourly = false,
            RequiresAttachment = true,
            MaxDaysPerRequest = 3m,
            MaxDaysPerYear = 6m,
            AllowCarryForward = false,
            AttendanceSymbol = "R",
            DisplayOrder = 4,
            IsActive = true,
            LegalReference = "Điều 115 khoản 1 điểm c Bộ luật Lao động 45/2019/QH14",
            NeedsHrVerification = true,
            Description = "Cha mẹ đẻ/nuôi, cha mẹ vợ/chồng, vợ/chồng, con đẻ/con nuôi chết được nghỉ 03 ngày hưởng nguyên lương."
        },
        new()
        {
            LeaveTypeCode = "SICK",
            LeaveTypeName = "Nghỉ ốm hưởng chế độ BHXH",
            IsPaidByCompany = false,
            IsPaidBySocialInsurance = true,
            CountsAgainstAnnualLeave = false,
            AllowHalfDay = false,
            AllowHourly = false,
            RequiresAttachment = true,
            MaxDaysPerRequest = 30m,
            MaxDaysPerYear = 30m,
            AllowCarryForward = false,
            AttendanceSymbol = "Ô",
            DisplayOrder = 5,
            IsActive = true,
            LegalReference = "Luật BHXH (Điều 25-26) — Cần giấy xác nhận nghỉ việc hưởng BHXH",
            NeedsHrVerification = true,
            Description = "Cơ quan BHXH chi trả theo hồ sơ y tế hợp lệ."
        },
        new()
        {
            LeaveTypeCode = "MATERNITY",
            LeaveTypeName = "Nghỉ thai sản hưởng chế độ BHXH",
            IsPaidByCompany = false,
            IsPaidBySocialInsurance = true,
            CountsAgainstAnnualLeave = false,
            AllowHalfDay = false,
            AllowHourly = false,
            RequiresAttachment = true,
            MaxDaysPerRequest = 180m,
            MaxDaysPerYear = 180m,
            AllowCarryForward = false,
            AttendanceSymbol = "TS",
            DisplayOrder = 6,
            IsActive = true,
            LegalReference = "Điều 139 Bộ luật Lao động 45/2019/QH14 & Luật BHXH",
            NeedsHrVerification = true,
            Description = "Nghỉ thai sản 6 tháng (hưởng BHXH); cần đính kèm giấy chứng sinh / xác nhận y tế."
        },
        new()
        {
            LeaveTypeCode = "UNPAID",
            LeaveTypeName = "Nghỉ việc riêng không hưởng lương",
            IsPaidByCompany = false,
            IsPaidBySocialInsurance = false,
            CountsAgainstAnnualLeave = false,
            AllowHalfDay = true,
            AllowHourly = true,
            RequiresAttachment = false,
            MaxDaysPerRequest = 30m,
            MaxDaysPerYear = 30m,
            AllowCarryForward = false,
            AttendanceSymbol = "Ro",
            DisplayOrder = 7,
            IsActive = true,
            LegalReference = "Điều 115 khoản 2-3 Bộ luật Lao động 45/2019/QH14",
            NeedsHrVerification = true,
            Description = "Nghỉ không lương theo thỏa thuận hoặc chế độ không hưởng lương."
        },
        new()
        {
            LeaveTypeCode = "BUSINESS_TRIP",
            LeaveTypeName = "Đi công tác (Hưởng nguyên lương)",
            IsPaidByCompany = true,
            IsPaidBySocialInsurance = false,
            CountsAgainstAnnualLeave = false,
            AllowHalfDay = true,
            AllowHourly = false,
            RequiresAttachment = false,
            MaxDaysPerRequest = 30m,
            MaxDaysPerYear = 90m,
            AllowCarryForward = false,
            AttendanceSymbol = "CT",
            DisplayOrder = 8,
            IsActive = true,
            LegalReference = "Quy chế công tác nội bộ doanh nghiệp",
            NeedsHrVerification = true,
            Description = "Được ghi nhận đủ công đi công tác mà không cần chấm công tại văn phòng."
        },
        new()
        {
            LeaveTypeCode = "COMP_LEAVE",
            LeaveTypeName = "Nghỉ bù từ làm thêm giờ (Hưởng lương / trừ quỹ nghỉ bù)",
            IsPaidByCompany = true,
            IsPaidBySocialInsurance = false,
            CountsAgainstAnnualLeave = false,
            AllowHalfDay = true,
            AllowHourly = true,
            RequiresAttachment = false,
            MaxDaysPerRequest = 5m,
            MaxDaysPerYear = 30m,
            AllowCarryForward = false,
            AttendanceSymbol = "P",
            DisplayOrder = 9,
            IsActive = true,
            LegalReference = "Thỏa thuận nghỉ bù OT theo quy chế lao động & BLLĐ 2019",
            NeedsHrVerification = true,
            Description = "Sử dụng quỹ giờ nghỉ bù đã tích lũy từ các đơn OT chọn hình thức nghỉ bù."
        }
    ];

    public static List<AttendanceStatutoryParameter> BuildDefaultAttendanceStatutoryParameters() =>
    [
        new()
        {
            EffectiveFrom = new DateOnly(2021, 1, 1),
            EffectiveTo = null,
            LegalReference = "Bộ luật Lao động 45/2019/QH14 (Điều 105, 106, 107, 109, 112, 113, 114); NĐ 145/2020/NĐ-CP",
            NeedsHrVerification = true,
            NightWorkStartTime = new TimeOnly(22, 0),
            NightWorkEndTime = new TimeOnly(6, 0),
            MinInterShiftRestHours = 12m,
            MaxNormalDailyHours = 8m,
            MaxNormalWeeklyHours = 48m,
            MaxDailyOvertimeHours = 4m,
            MaxTotalDailyWorkingAndOtHours = 12m,
            MaxMonthlyOvertimeHours = 40m,
            MaxYearlyOvertimeHours = 200m,
            MaxSpecialIndustryYearlyOvertimeHours = 300m,
            StandardAnnualLeaveDays = 12m,
            SeniorityBonusYearsInterval = 5,
            SeniorityBonusDaysPerInterval = 1m,
            WeekdayOtMultiplier = 1.5m,
            WeeklyDayOffOtMultiplier = 2.0m,
            HolidayOtMultiplier = 3.0m,
            NightShiftBonusRate = 0.30m,
            NightOtExtraBonusRate = 0.20m,
            Notes = "Tham số pháp lý chấm công/OT/phép năm chuẩn BLLĐ 2019 — Cần HR đối chiếu khi ban hành văn bản mới"
        }
    ];

    public static PolicyGroup BuildDefaultCompanyPolicyGroup()
    {
        var group = new PolicyGroup
        {
            PolicyCode = "DEFAULT_COMPANY",
            PolicyName = "Chính sách chấm công mặc định Toàn Công ty",
            IsCompanyDefault = true,
            IsActive = true,
            Description = "Áp dụng mặc định khi Nhân viên, Chức danh và Đơn vị không có PolicyGroup riêng"
        };
        group.AttendancePolicies.Add(new AttendancePolicy
        {
            GraceLateMinutes = 5,
            GraceEarlyMinutes = 5,
            RoundingIntervalMinutes = 5,
            RoundingMode = "Nearest",
            HalfDayLateThresholdMinutes = 60,
            ZeroDayLateThresholdMinutes = 240,
            MaxMissedPunchExplanationsPerMonth = 3,
            MissedPunchOverQuotaAction = "FixedFine",
            MissedPunchPenaltyAmount = 100000m,
            RequireApprovedOvertimeRequest = true,
            WeeklyWorkdaysMask = "Mon,Tue,Wed,Thu,Fri",
            EmployeeReviewWindowDays = 3,
            AnnualLeaveCarryForwardCapDays = 5m,
            CarryForwardExpiryMonth = 3,
            CarryForwardExpiryDay = 31,
            AllowAdvanceAnnualLeave = false,
            MaxAdvanceAnnualLeaveDays = 3m,
            MidMonthHireCutoffDay = 15,
            OtReconciliationMode = "MinOfApprovedAndActual",
            OvertimeWarningThresholdPercent = 85,
            BlockOvertimeWhenExceedingLimit = true,
            AllowCompensatoryLeaveForOt = true,
            UseSpecialIndustry300OtLimit = false
        });
        return group;
    }

    public static List<ShiftRotationPattern> BuildDefaultShiftRotationPatterns() =>
    [
        new()
        {
            PatternCode = "3CA_4KIP",
            PatternName = "Xoay ca sản xuất 3 ca 4 kíp (8 ngày/chu kỳ)",
            CycleDays = 8,
            CrewCount = 4,
            ShiftSequenceCsv = "CA1,CA1,CA2,CA2,CA3,CA3,OFF,OFF",
            CrewOffsetDays = 2,
            IsActive = true,
            Description = "Mỗi kíp làm 2 ngày Ca 1, 2 ngày Ca 2, 2 ngày Ca 3 đêm, nghỉ 2 ngày xoay vòng"
        }
    ];

    public static List<PayrollStatutoryParameter> BuildDefaultStatutoryParameters() =>
    [
        new()
        {
            EffectiveFrom = new DateOnly(2024, 7, 1),
            EffectiveTo = new DateOnly(2025, 12, 31),
            LegalReference = "NĐ 73/2024/NĐ-CP (LCS 2.34tr), NĐ 74/2024/NĐ-CP (LTTV), NQ 954/2020/UBTVQH14 (GTGC 11tr/4.4tr)",
            Notes = "Giai đoạn trước 01/01/2026",
            StatutoryBaseSalary = 2_340_000m,
            MinWageRegion1 = 4_960_000m,
            MinWageRegion2 = 4_410_000m,
            MinWageRegion3 = 3_860_000m,
            MinWageRegion4 = 3_450_000m,
            SocialInsuranceCapMultiplier = 20m,
            UnemploymentInsuranceCapMultiplier = 20m,
            EmployeeSiRate = 0.08m,
            EmployeeHiRate = 0.015m,
            EmployeeUiRate = 0.01m,
            CompanySiRate = 0.175m,
            CompanyHiRate = 0.03m,
            CompanyUiRate = 0.01m,
            CompanyUnionFeeRate = 0.02m,
            CompanyOccupationalAccidentRate = 0.005m,
            EmployeeUnionFeeRate = 0m,
            EmployeeUnionFeeCapMultiplier = 0.10m,
            PitNonResidentRate = 0.20m,
            PitShortTermRate = 0.10m,
            PitShortTermThreshold = 2_000_000m,
            OtMultiplier150 = 1.50m,
            OtMultiplier200 = 2.00m,
            OtMultiplier300 = 3.00m,
            OtNightShiftBonusRate = 0.30m,
            OtNightOvertimeExtraRate = 0.20m,
            OtTaxableBaseRate = 1.00m,
            IsOtExcessTaxExempt = true,
            NeedsHrVerification = true,
            SelfDeduction = 11_000_000m,
            DependentDeduction = 4_400_000m
        },
        new()
        {
            EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveTo = new DateOnly(2026, 6, 30),
            LegalReference = "Luật 109/2025/QH15, NQ 110/2025/UBTVQH15 (GTGC 15.5tr/6.2tr), NĐ 293/2025 (LTTV 4 vùng)",
            Notes = "Giai đoạn 01/01/2026 – 30/06/2026 (LCS giữ 2.340.000đ)",
            StatutoryBaseSalary = 2_340_000m,
            MinWageRegion1 = 5_310_000m,
            MinWageRegion2 = 4_730_000m,
            MinWageRegion3 = 4_140_000m,
            MinWageRegion4 = 3_700_000m,
            SocialInsuranceCapMultiplier = 20m,
            UnemploymentInsuranceCapMultiplier = 20m,
            EmployeeSiRate = 0.08m,
            EmployeeHiRate = 0.015m,
            EmployeeUiRate = 0.01m,
            CompanySiRate = 0.175m,
            CompanyHiRate = 0.03m,
            CompanyUiRate = 0.01m,
            CompanyUnionFeeRate = 0.02m,
            CompanyOccupationalAccidentRate = 0.005m,
            EmployeeUnionFeeRate = 0m,
            EmployeeUnionFeeCapMultiplier = 0.10m,
            PitNonResidentRate = 0.20m,
            PitShortTermRate = 0.10m,
            PitShortTermThreshold = 2_000_000m,
            OtMultiplier150 = 1.50m,
            OtMultiplier200 = 2.00m,
            OtMultiplier300 = 3.00m,
            OtNightShiftBonusRate = 0.30m,
            OtNightOvertimeExtraRate = 0.20m,
            OtTaxableBaseRate = 1.00m,
            IsOtExcessTaxExempt = true,
            NeedsHrVerification = true,
            SelfDeduction = 15_500_000m,
            DependentDeduction = 6_200_000m
        },
        new()
        {
            EffectiveFrom = new DateOnly(2026, 7, 1),
            EffectiveTo = null,
            LegalReference = "Điều chỉnh LCS 2.530.000đ từ 01/07/2026; Luật 109/2025/QH15; NQ 110/2025/UBTVQH15; NĐ 293/2025",
            Notes = "Giai đoạn từ 01/07/2026 trở đi (trần BHXH/BHYT 50.600.000đ)",
            StatutoryBaseSalary = 2_530_000m,
            MinWageRegion1 = 5_310_000m,
            MinWageRegion2 = 4_730_000m,
            MinWageRegion3 = 4_140_000m,
            MinWageRegion4 = 3_700_000m,
            SocialInsuranceCapMultiplier = 20m,
            UnemploymentInsuranceCapMultiplier = 20m,
            EmployeeSiRate = 0.08m,
            EmployeeHiRate = 0.015m,
            EmployeeUiRate = 0.01m,
            CompanySiRate = 0.175m,
            CompanyHiRate = 0.03m,
            CompanyUiRate = 0.01m,
            CompanyUnionFeeRate = 0.02m,
            CompanyOccupationalAccidentRate = 0.005m,
            EmployeeUnionFeeRate = 0m,
            EmployeeUnionFeeCapMultiplier = 0.10m,
            PitNonResidentRate = 0.20m,
            PitShortTermRate = 0.10m,
            PitShortTermThreshold = 2_000_000m,
            OtMultiplier150 = 1.50m,
            OtMultiplier200 = 2.00m,
            OtMultiplier300 = 3.00m,
            OtNightShiftBonusRate = 0.30m,
            OtNightOvertimeExtraRate = 0.20m,
            OtTaxableBaseRate = 1.00m,
            IsOtExcessTaxExempt = true,
            NeedsHrVerification = true,
            SelfDeduction = 15_500_000m,
            DependentDeduction = 6_200_000m
        }
    ];

    public static List<PitBracket> BuildDefaultPitBrackets() =>
    [
        // Biểu thuế lũy tiến 7 bậc (áp dụng trước 01/01/2026)
        new() { BracketLevel = 1, LowerBound = 0m, UpperBound = 5_000_000m, TaxRate = 0.05m, EffectiveFrom = new DateOnly(2020, 1, 1), EffectiveTo = new DateOnly(2025, 12, 31), LegalReference = "Luật Thuế TNCN (Biểu 7 bậc)" },
        new() { BracketLevel = 2, LowerBound = 5_000_000m, UpperBound = 10_000_000m, TaxRate = 0.10m, EffectiveFrom = new DateOnly(2020, 1, 1), EffectiveTo = new DateOnly(2025, 12, 31), LegalReference = "Luật Thuế TNCN (Biểu 7 bậc)" },
        new() { BracketLevel = 3, LowerBound = 10_000_000m, UpperBound = 18_000_000m, TaxRate = 0.15m, EffectiveFrom = new DateOnly(2020, 1, 1), EffectiveTo = new DateOnly(2025, 12, 31), LegalReference = "Luật Thuế TNCN (Biểu 7 bậc)" },
        new() { BracketLevel = 4, LowerBound = 18_000_000m, UpperBound = 32_000_000m, TaxRate = 0.20m, EffectiveFrom = new DateOnly(2020, 1, 1), EffectiveTo = new DateOnly(2025, 12, 31), LegalReference = "Luật Thuế TNCN (Biểu 7 bậc)" },
        new() { BracketLevel = 5, LowerBound = 32_000_000m, UpperBound = 52_000_000m, TaxRate = 0.25m, EffectiveFrom = new DateOnly(2020, 1, 1), EffectiveTo = new DateOnly(2025, 12, 31), LegalReference = "Luật Thuế TNCN (Biểu 7 bậc)" },
        new() { BracketLevel = 6, LowerBound = 52_000_000m, UpperBound = 80_000_000m, TaxRate = 0.30m, EffectiveFrom = new DateOnly(2020, 1, 1), EffectiveTo = new DateOnly(2025, 12, 31), LegalReference = "Luật Thuế TNCN (Biểu 7 bậc)" },
        new() { BracketLevel = 7, LowerBound = 80_000_000m, UpperBound = null, TaxRate = 0.35m, EffectiveFrom = new DateOnly(2020, 1, 1), EffectiveTo = new DateOnly(2025, 12, 31), LegalReference = "Luật Thuế TNCN (Biểu 7 bậc)" },

        // Biểu thuế lũy tiến 5 bậc theo Luật 109/2025/QH15 (áp dụng từ 01/01/2026)
        new() { BracketLevel = 1, LowerBound = 0m, UpperBound = 10_000_000m, TaxRate = 0.05m, EffectiveFrom = new DateOnly(2026, 1, 1), EffectiveTo = null, LegalReference = "Luật 109/2025/QH15 (Biểu 5 bậc)" },
        new() { BracketLevel = 2, LowerBound = 10_000_000m, UpperBound = 30_000_000m, TaxRate = 0.10m, EffectiveFrom = new DateOnly(2026, 1, 1), EffectiveTo = null, LegalReference = "Luật 109/2025/QH15 (Biểu 5 bậc)" },
        new() { BracketLevel = 3, LowerBound = 30_000_000m, UpperBound = 60_000_000m, TaxRate = 0.20m, EffectiveFrom = new DateOnly(2026, 1, 1), EffectiveTo = null, LegalReference = "Luật 109/2025/QH15 (Biểu 5 bậc)" },
        new() { BracketLevel = 4, LowerBound = 60_000_000m, UpperBound = 100_000_000m, TaxRate = 0.30m, EffectiveFrom = new DateOnly(2026, 1, 1), EffectiveTo = null, LegalReference = "Luật 109/2025/QH15 (Biểu 5 bậc)" },
        new() { BracketLevel = 5, LowerBound = 100_000_000m, UpperBound = null, TaxRate = 0.35m, EffectiveFrom = new DateOnly(2026, 1, 1), EffectiveTo = null, LegalReference = "Luật 109/2025/QH15 (Biểu 5 bậc)" }
    ];

    public static List<PayrollParameter> BuildDefaultTaxExemptPayrollParameters() =>
    [
        new()
        {
            ParamCode = "PIT_EXEMPT_MEAL",
            ParamName = "Mức miễn thuế TNCN phụ cấp ăn ca/trưa hàng tháng",
            ParamValue = 730_000m,
            ValueUnit = "VND",
            EffectiveFrom = new DateOnly(2015, 1, 1),
            EffectiveTo = null,
            LegalReference = "Thông tư 111/2013/TT-BTC Điều 2 khoản 2 điểm g.5 & TT 26/2016/TT-BLĐTBXH (cần HR đối chiếu khi văn bản mới có hiệu lực)"
        },
        new()
        {
            ParamCode = "PIT_EXEMPT_PHONE",
            ParamName = "Mức khoán chi điện thoại phục vụ công việc miễn thuế TNCN",
            ParamValue = 500_000m,
            ValueUnit = "VND",
            EffectiveFrom = new DateOnly(2024, 1, 1),
            EffectiveTo = null,
            LegalReference = "Quy chế tài chính nội bộ & TT 111/2013/TT-BTC Điều 2 khoản 2 điểm đ.4 (cần HR đối chiếu)"
        },
        new()
        {
            ParamCode = "MIN_PROBATION_SALARY_RATE",
            ParamName = "Tỷ lệ lương thử việc tối thiểu so với lương công việc (%)",
            ParamValue = 85.00m,
            ValueUnit = "Percent",
            EffectiveFrom = new DateOnly(2021, 1, 1),
            EffectiveTo = null,
            LegalReference = "Điều 26 Bộ luật Lao động 45/2019/QH14 (tối thiểu 85%)"
        },
        new()
        {
            ParamCode = "KPI_BONUS_RATE_OF_BASE",
            ParamName = "Tỷ lệ quỹ thưởng KPI chuẩn theo lương cơ bản (hệ số nhân KPI = 1.0)",
            ParamValue = 0.15m,
            ValueUnit = "Ratio",
            EffectiveFrom = new DateOnly(2024, 1, 1),
            EffectiveTo = null,
            LegalReference = "Quy chế thưởng hiệu quả KPI nội bộ doanh nghiệp (cần HR đối chiếu)"
        }
    ];

    public static List<SalaryComponent> BuildDefaultSalaryComponentsCl5() =>
    [
        new()
        {
            ComponentCode = "INFO_HOURLY_RATE",
            ComponentName = "Đơn giá giờ công chuẩn (biến trung gian)",
            ComponentType = "Info",
            CalculationType = "Formula",
            FormulaExpression = "LUONG_CO_BAN_HIEN_TAI / (CONG_CHUAN * GIO_LAM_VIEC_NGAY)",
            IsTaxable = false,
            IsSocialInsuranceBase = false,
            ShowOnPayslip = false,
            SortOrder = 1,
            LegalReference = "Điều 98 Bộ luật Lao động 2019",
            NeedsHrVerification = true,
            Description = "Thành phần trung gian tính đơn giá 1 giờ công để dùng lại trong công thức OT và phụ cấp ca đêm."
        },
        new()
        {
            ComponentCode = "BASIC_PAY",
            ComponentName = "Lương cơ bản theo ngày công (P1)",
            ComponentType = "Earning",
            CalculationType = "Formula",
            FormulaExpression = "Round(LUONG_CO_BAN_HIEN_TAI * CONG_THUC_TE / CONG_CHUAN, 0)",
            IsTaxable = true,
            IsSocialInsuranceBase = true,
            ShowOnPayslip = true,
            SortOrder = 2,
            LegalReference = "Điều 90 Bộ luật Lao động 2019",
            NeedsHrVerification = true,
            Description = "Lương theo công thực tế/chuẩn; tự động xét tỷ lệ thử việc."
        },
        new()
        {
            ComponentCode = "POS_ALLOWANCE",
            ComponentName = "Phụ cấp chức vụ",
            ComponentType = "Allowance",
            CalculationType = "Fixed",
            IsTaxable = true,
            IsSocialInsuranceBase = true,
            ShowOnPayslip = true,
            SortOrder = 3,
            LegalReference = "Thông tư 59/2015/TT-BLĐTBXH & TT 10/2020/TT-BLĐTBXH",
            NeedsHrVerification = true
        },
        new()
        {
            ComponentCode = "MEAL_ALLOWANCE",
            ComponentName = "Phụ cấp tiền ăn ca / ăn trưa",
            ComponentType = "Allowance",
            CalculationType = "Fixed",
            DefaultAmount = 730_000m,
            IsTaxable = true,
            TaxExemptLimit = 730_000m,
            TaxExemptLimitParameterCode = "PIT_EXEMPT_MEAL",
            IsSocialInsuranceBase = false,
            ShowOnPayslip = true,
            SortOrder = 4,
            LegalReference = "TT 111/2013/TT-BTC Điều 2.2.g.5 (miễn thuế tối đa 730.000đ/tháng - cần HR đối chiếu)",
            NeedsHrVerification = true
        },
        new()
        {
            ComponentCode = "PHONE_ALLOWANCE",
            ComponentName = "Khoán phụ cấp điện thoại công việc",
            ComponentType = "Allowance",
            CalculationType = "Fixed",
            DefaultAmount = 500_000m,
            IsTaxable = true,
            TaxExemptLimit = 500_000m,
            TaxExemptLimitParameterCode = "PIT_EXEMPT_PHONE",
            IsSocialInsuranceBase = false,
            ShowOnPayslip = true,
            SortOrder = 5,
            LegalReference = "Khoán chi công việc theo quy chế công ty (cần HR đối chiếu)",
            NeedsHrVerification = true
        },
        new()
        {
            ComponentCode = "HAZARD_ALLOWANCE",
            ComponentName = "Phụ cấp nặng nhọc, độc hại nhà máy (5%)",
            ComponentType = "Allowance",
            CalculationType = "Formula",
            FormulaExpression = "Round(LUONG_CO_BAN_HIEN_TAI * 0.05 * CONG_THUC_TE / CONG_CHUAN, 0)",
            IsTaxable = true,
            IsSocialInsuranceBase = false,
            ShowOnPayslip = true,
            SortOrder = 6,
            LegalReference = "Điều 90 BLLĐ 2019 & Quy chế nhà máy (cần HR đối chiếu)",
            NeedsHrVerification = true
        },
        new()
        {
            ComponentCode = "NIGHT_ALLOWANCE",
            ComponentName = "Phụ cấp làm việc ban đêm (+30% đơn giá giờ)",
            ComponentType = "Allowance",
            CalculationType = "Formula",
            FormulaExpression = "Round(INFO_HOURLY_RATE * GIO_DEM * 0.30, 0)",
            IsTaxable = false,
            IsSocialInsuranceBase = false,
            ShowOnPayslip = true,
            SortOrder = 7,
            LegalReference = "Điều 98 Khoản 2 BLLĐ 2019 (làm đêm thêm ít nhất 30%; phần trả thêm miễn thuế TNCN)",
            NeedsHrVerification = true
        },
        new()
        {
            ComponentCode = "OT_PAY",
            ComponentName = "Tiền làm thêm giờ (OT 150%/200%/300%)",
            ComponentType = "Earning",
            CalculationType = "Formula",
            FormulaExpression = "Round(INFO_HOURLY_RATE * (OT150 * 1.5 + OT200 * 2.0 + OT300 * 3.0), 0)",
            IsTaxable = true,
            IsSocialInsuranceBase = false,
            ShowOnPayslip = true,
            SortOrder = 8,
            LegalReference = "Điều 98 Khoản 1 BLLĐ 2019",
            NeedsHrVerification = true
        },
        new()
        {
            ComponentCode = "OTHER_ALLOWANCE",
            ComponentName = "Phụ cấp xăng xe, điện thoại, hỗ trợ khác (P2)",
            ComponentType = "Allowance",
            CalculationType = "Fixed",
            IsTaxable = true,
            TaxExemptLimit = 730_000m,
            TaxExemptLimitParameterCode = "PIT_EXEMPT_MEAL",
            IsSocialInsuranceBase = false,
            ShowOnPayslip = true,
            SortOrder = 9,
            LegalReference = "Quy chế phụ cấp nội bộ (cần HR đối chiếu mức miễn thuế)",
            NeedsHrVerification = true
        },
        new()
        {
            ComponentCode = "KPI_BONUS",
            ComponentName = "Thưởng hiệu quả công việc KPI (P3)",
            ComponentType = "Bonus",
            CalculationType = "Fixed",
            IsTaxable = true,
            IsSocialInsuranceBase = false,
            ShowOnPayslip = true,
            SortOrder = 10,
            LegalReference = "Quy chế thưởng KPI doanh nghiệp",
            NeedsHrVerification = true
        },
        new()
        {
            ComponentCode = "SALES_COMMISSION",
            ComponentName = "Hoa hồng doanh số khối Kinh doanh",
            ComponentType = "Bonus",
            CalculationType = "Formula",
            FormulaExpression = "Round(HOA_HONG_DOANH_SO * DIEM_KPI, 0)",
            IsTaxable = true,
            IsSocialInsuranceBase = false,
            ShowOnPayslip = true,
            SortOrder = 11,
            LegalReference = "Chính sách hoa hồng khối Kinh doanh",
            NeedsHrVerification = true
        },
        new()
        {
            ComponentCode = "SI_DEDUCTION",
            ComponentName = "Khấu trừ BHXH, BHYT, BHTN (NLĐ 10.5%)",
            ComponentType = "Deduction",
            CalculationType = "System",
            IsTaxable = false,
            IsSocialInsuranceBase = false,
            ShowOnPayslip = true,
            SortOrder = 12,
            LegalReference = "Luật BHXH 41/2024/QH15 & Luật BHYT",
            NeedsHrVerification = true
        },
        new()
        {
            ComponentCode = "PIT_TAX",
            ComponentName = "Thuế Thu nhập cá nhân (TNCN)",
            ComponentType = "Deduction",
            CalculationType = "System",
            IsTaxable = false,
            IsSocialInsuranceBase = false,
            ShowOnPayslip = true,
            SortOrder = 13,
            LegalReference = "Luật Thuế TNCN & Luật 109/2025/QH15",
            NeedsHrVerification = true
        }
    ];

    public static List<SalaryTemplate> BuildDefaultSalaryTemplatesWithComponents(
        IReadOnlyDictionary<string, SalaryComponent> compByCode)
    {
        static List<SalaryTemplateComponent> MapCodes(
            IReadOnlyDictionary<string, SalaryComponent> lookup,
            params (string Code, int Order)[] items)
        {
            var list = new List<SalaryTemplateComponent>();
            foreach (var (code, order) in items)
            {
                if (lookup.TryGetValue(code, out var comp))
                {
                    list.Add(new SalaryTemplateComponent
                    {
                        SalaryComponentId = comp.Id,
                        SortOrder = order,
                        IsRequired = true
                    });
                }
            }
            return list;
        }

        return
        [
            new()
            {
                TemplateCode = "OFFICE_STD",
                TemplateName = "Khối Văn phòng chuẩn (3P)",
                StaffGroup = "Office",
                DefaultPayFrequency = "Monthly",
                DefaultProbationSalaryRatePercent = 85.00m,
                RoundingRule = "RoundToDong",
                IsDefaultForCompany = true,
                IsActive = true,
                LegalReference = "Quy chế lương 3P Khối Văn phòng & BLLĐ 2019",
                NeedsHrVerification = true,
                Description = "Áp dụng cho nhân sự khối Văn phòng, Tài chính, Nhân sự, CNTT.",
                Components = MapCodes(compByCode,
                    ("INFO_HOURLY_RATE", 1),
                    ("BASIC_PAY", 2),
                    ("POS_ALLOWANCE", 3),
                    ("OTHER_ALLOWANCE", 4),
                    ("OT_PAY", 5),
                    ("SI_DEDUCTION", 6),
                    ("PIT_TAX", 7))
            },
            new()
            {
                TemplateCode = "FACTORY_SHIFT",
                TemplateName = "Khối Nhà máy & Trang trại (Ca đêm, Độc hại)",
                StaffGroup = "Factory",
                DefaultPayFrequency = "Monthly",
                DefaultProbationSalaryRatePercent = 85.00m,
                RoundingRule = "RoundToDong",
                IsDefaultForCompany = false,
                IsActive = true,
                LegalReference = "Điều 98 BLLĐ 2019 (Ca đêm +30%, OT) & Quy chế Nhà máy",
                NeedsHrVerification = true,
                Description = "Bao gồm phụ cấp độc hại 5% và phụ cấp làm việc ban đêm (+30% đơn giá giờ).",
                Components = MapCodes(compByCode,
                    ("INFO_HOURLY_RATE", 1),
                    ("BASIC_PAY", 2),
                    ("POS_ALLOWANCE", 3),
                    ("OTHER_ALLOWANCE", 4),
                    ("HAZARD_ALLOWANCE", 5),
                    ("NIGHT_ALLOWANCE", 6),
                    ("OT_PAY", 7),
                    ("SI_DEDUCTION", 8),
                    ("PIT_TAX", 9))
            },
            new()
            {
                TemplateCode = "SALES_COMMISSION",
                TemplateName = "Khối Kinh doanh & Hoa hồng doanh số",
                StaffGroup = "Sales",
                DefaultPayFrequency = "Monthly",
                DefaultProbationSalaryRatePercent = 85.00m,
                RoundingRule = "RoundToDong",
                IsDefaultForCompany = false,
                IsActive = true,
                LegalReference = "Chính sách hoa hồng khối Kinh doanh",
                NeedsHrVerification = true,
                Description = "Lương cơ bản + phụ cấp khoán công tác + hoa hồng doanh số theo hệ số KPI.",
                Components = MapCodes(compByCode,
                    ("INFO_HOURLY_RATE", 1),
                    ("BASIC_PAY", 2),
                    ("POS_ALLOWANCE", 3),
                    ("OTHER_ALLOWANCE", 4),
                    ("SALES_COMMISSION", 5),
                    ("OT_PAY", 6),
                    ("SI_DEDUCTION", 7),
                    ("PIT_TAX", 8))
            }
        ];
    }
}

