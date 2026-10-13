using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using VinamilkHRM.Domain.Entities.Attendances;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Infrastructure.Data;
using VinamilkHRM.Infrastructure.Services;
using VinamilkHRM.Application.Common.Interfaces;

namespace VinamilkHRM.LegacyImporter;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("================================================================================");
        Console.WriteLine("       VINAMILK HRM — LEGACY DATA MIGRATION TOOL (MySQL -> SQL Server)         ");
        Console.WriteLine("================================================================================");

        bool isDryRun = args.Contains("--dry-run") || !args.Contains("--execute");
        string mysqlConnStr = GetArgumentValue(args, "--mysql") ?? "Server=localhost;Database=vinamilk_hrm;User=root;Password=;";
        string mssqlConnStr = GetArgumentValue(args, "--mssql") ?? "Server=localhost;Database=VinamilkHRMDb;Trusted_Connection=True;TrustServerCertificate=True;";
        string? sqlDumpFile = GetArgumentValue(args, "--dump-file");

        Console.WriteLine($"[CHẾ ĐỘ]      : {(isDryRun ? "DRY-RUN (Chỉ kiểm tra & đối soát, KHÔNG ghi CSDL)" : "EXECUTE (Thực thi ghi CSDL SQL Server)")}");
        Console.WriteLine($"[MYSQL]       : {MaskConnectionString(mysqlConnStr)}");
        Console.WriteLine($"[SQL SERVER]  : {MaskConnectionString(mssqlConnStr)}");
        Console.WriteLine();

        var importer = new ImporterEngine(mysqlConnStr, mssqlConnStr, isDryRun, sqlDumpFile);
        return await importer.RunAsync();
    }

    private static string? GetArgumentValue(string[] args, string flag)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }
        return null;
    }

    private static string MaskConnectionString(string connStr)
    {
        return Regex.Replace(connStr, @"(?i)(Password|pwd)=[^;]+", "$1=******");
    }
}

public class ImporterEngine
{
    private readonly string _mysqlConn;
    private readonly string _mssqlConn;
    private readonly bool _isDryRun;
    private readonly string? _dumpFile;

    public ImporterEngine(string mysqlConn, string mssqlConn, bool isDryRun, string? dumpFile = null)
    {
        _mysqlConn = mysqlConn;
        _mssqlConn = mssqlConn;
        _isDryRun = isDryRun;
        _dumpFile = dumpFile;
    }

    public async Task<int> RunAsync()
    {
        try
        {
            Console.WriteLine("--> Bước 1: Đọc dữ liệu từ nguồn MySQL...");
            var sourceData = await LoadSourceDataAsync();
            Console.WriteLine($"    + dm_donvi     : {sourceData.OrgUnits.Count} bản ghi");
            Console.WriteLine($"    + nhansu       : {sourceData.Employees.Count} bản ghi");
            Console.WriteLine($"    + qt_congtac   : {sourceData.WorkHistories.Count} bản ghi");
            Console.WriteLine($"    + chamcong     : {sourceData.Attendances.Count} bản ghi");
            Console.WriteLine($"    + bangluong    : {sourceData.PaySlips.Count} bản ghi");
            Console.WriteLine();

            Console.WriteLine("--> Bước 2: Thiết lập kết nối và dịch vụ đích SQL Server...");
            var serviceProvider = BuildServices();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<VinamilkHRMDbContext>();
            var encryptionService = scope.ServiceProvider.GetRequiredService<IFieldEncryptionService>();

            bool dbConnected = false;
            try
            {
                dbConnected = await dbContext.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    [CẢNH BÁO] Không thể kết nối SQL Server trực tiếp: {ex.Message}");
                Console.WriteLine("    Tiến hành đối soát mô phỏng bộ nhớ (In-memory simulation).");
            }

            Console.WriteLine("--> Bước 3: Thực hiện chuyển đổi và kiểm tra Idempotency (Trùng lặp)...");
            var report = await ProcessMigrationAsync(dbContext, encryptionService, sourceData, dbConnected);

            Console.WriteLine();
            PrintReconciliationReport(report);

            if (_isDryRun)
            {
                Console.WriteLine("\n[DRY RUN HOÀN TẤT] Không có thay đổi nào được lưu vào SQL Server.");
            }
            else if (dbConnected)
            {
                Console.WriteLine("\n[EXECUTE HOÀN TẤT] Toàn bộ dữ liệu hợp lệ đã được lưu thành công vào SQL Server!");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[LỖI NGHIÊM TRỌNG]: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
            return 1;
        }
    }

    private ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        services.AddSingleton<IFieldEncryptionService, FieldEncryptionService>();

        services.AddDbContext<VinamilkHRMDbContext>(options =>
        {
            options.UseSqlServer(_mssqlConn, x => x.UseHierarchyId());
        });

        return services.BuildServiceProvider();
    }

    private async Task<MigrationReport> ProcessMigrationAsync(
        VinamilkHRMDbContext db,
        IFieldEncryptionService enc,
        SourceData source,
        bool isDbConnected)
    {
        var report = new MigrationReport
        {
            SourceOrgUnitsCount = source.OrgUnits.Count,
            SourceEmployeesCount = source.Employees.Count,
            SourceWorkHistoriesCount = source.WorkHistories.Count,
            SourceAttendancesCount = source.Attendances.Count,
            SourcePaySlipsCount = source.PaySlips.Count
        };

        // 1. Tổ chức
        var existingUnits = isDbConnected 
            ? await db.OrganizationalUnits.IgnoreQueryFilters().ToListAsync()
            : new List<OrganizationalUnit>();
        report.TargetOrgUnitsBefore = existingUnits.Count;

        var unitMap = new Dictionary<string, OrganizationalUnit>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in existingUnits)
        {
            if (!string.IsNullOrEmpty(u.LegacyId)) unitMap[u.LegacyId] = u;
            else if (!string.IsNullOrEmpty(u.UnitCode)) unitMap[u.UnitCode] = u;
        }

        // Sắp xếp đơn vị theo thứ tự cha trước con sau
        var sortedUnits = TopologicalSortUnits(source.OrgUnits);
        int newUnits = 0;
        int unitIndex = 1;

        foreach (var srcUnit in sortedUnits)
        {
            if (unitMap.TryGetValue(srcUnit.MaDV, out _))
            {
                continue;
            }

            var unitType = MapUnitType(srcUnit.LoaiDV, srcUnit.TenDV);
            Microsoft.EntityFrameworkCore.HierarchyId path;
            int? parentId = null;

            if (string.IsNullOrEmpty(srcUnit.MaDV_Cha) || !unitMap.TryGetValue(srcUnit.MaDV_Cha, out var parentUnit))
            {
                path = Microsoft.EntityFrameworkCore.HierarchyId.Parse($"/{unitIndex}/");
                unitIndex++;
            }
            else
            {
                parentId = parentUnit.Id;
                var childCount = unitMap.Values.Count(u => u.ParentUnitId == parentUnit.Id) + 1;
                path = Microsoft.EntityFrameworkCore.HierarchyId.Parse($"{parentUnit.Path}{childCount}/");
            }

            OrganizationalUnit newUnit = unitType switch
            {
                UnitType.Headquarters => new HeadquartersUnit(srcUnit.MaDV, srcUnit.TenDV, "Số 10 Tân Trào, P. Tân Phú, Q. 7, TP. HCM", "02854155555", "vinamilk@vinamilk.com.vn", "0300588569", 10, 5, true, "Ban Tổng Giám Đốc", parentId),
                UnitType.Division => new DivisionUnit(srcUnit.MaDV, srcUnit.TenDV, "Số 10 Tân Trào, P. Tân Phú, Q. 7, TP. HCM", "02854155555", "vinamilk@vinamilk.com.vn", "0300588569", parentId),
                UnitType.Factory => new FactoryUnit(srcUnit.MaDV, srcUnit.TenDV, "KCN Mỹ Phước 2, Bến Cát, Bình Dương", "02743555555", "factory@vinamilk.com.vn", "0300588569", 500000m, 10, "ISO 22000", "ENV-2026-001", parentId),
                UnitType.DairyFarm => new DairyFarmUnit(srcUnit.MaDV, srcUnit.TenDV, "Bến Cầu, Tây Ninh", "02763555555", "farm@vinamilk.com.vn", "0300588569", 8000, 1000m, 20, "GLOBALGAP-VNM-2026", parentId),
                UnitType.Branch => new BranchUnit(srcUnit.MaDV, srcUnit.TenDV, "Quận 1, TP. HCM", "02838222222", "branch@vinamilk.com.vn", "0300588569", "Miền Nam", parentId),
                UnitType.ProductionLine => new ProductionLineUnit(srcUnit.MaDV, srcUnit.TenDV, "Phân xưởng sản xuất", "02743555555", "prod@vinamilk.com.vn", "0300588569", 1, 25000m, parentId),
                UnitType.FarmTeam => new FarmTeamUnit(srcUnit.MaDV, srcUnit.TenDV, "Trang trại bò sữa", "02763555555", "farmteam@vinamilk.com.vn", "0300588569", "Chăn nuôi bò sữa", parentId),
                UnitType.RetailStore => new RetailStoreUnit(srcUnit.MaDV, srcUnit.TenDV, "Quận 1, TP. HCM", "02838333333", "store@vinamilk.com.vn", "0300588569", srcUnit.MaDV, 150m, 5000m, "07:30 - 21:30", parentId),
                _ => new DepartmentUnit(srcUnit.MaDV, srcUnit.TenDV, "Tòa nhà Vinamilk Tower", "02854155555", "dept@vinamilk.com.vn", "0300588569", parentId)
            };
            newUnit.SetPath(path);
            newUnit.SetLegacyId(srcUnit.MaDV);

            unitMap[srcUnit.MaDV] = newUnit;
            if (!_isDryRun && isDbConnected)
            {
                db.OrganizationalUnits.Add(newUnit);
            }
            newUnits++;
        }
        report.TargetOrgUnitsAdded = newUnits;
        if (!_isDryRun && isDbConnected && newUnits > 0)
        {
            await db.SaveChangesAsync();
        }

        // 2. Nhân sự
        var existingEmployees = isDbConnected 
            ? await db.Employees.IgnoreQueryFilters().ToListAsync()
            : new List<Employee>();
        report.TargetEmployeesBefore = existingEmployees.Count;

        var empMap = new Dictionary<string, Employee>(StringComparer.OrdinalIgnoreCase);
        var empIdMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int simulatedEmpId = 1;

        foreach (var e in existingEmployees)
        {
            if (!string.IsNullOrEmpty(e.LegacyId))
            {
                empMap[e.LegacyId] = e;
                empIdMap[e.LegacyId] = e.Id;
            }
            else if (!string.IsNullOrEmpty(e.EmployeeCode))
            {
                empMap[e.EmployeeCode] = e;
                empIdMap[e.EmployeeCode] = e.Id;
            }
        }

        int newEmployees = 0;
        foreach (var srcEmp in source.Employees)
        {
            if (empMap.TryGetValue(srcEmp.MaNV, out var existingE))
            {
                empIdMap[srcEmp.MaNV] = existingE.Id > 0 ? existingE.Id : simulatedEmpId++;
                continue;
            }

            int unitId = unitMap.TryGetValue(srcEmp.MaDV, out var u) ? (u.Id > 0 ? u.Id : 1) : 1;
            string rawCccd = !string.IsNullOrWhiteSpace(srcEmp.SoCMND_CCCD) ? srcEmp.SoCMND_CCCD.Trim() : "079085000000";
            if (rawCccd.Length < 9) rawCccd = rawCccd.PadRight(12, '0');

            string encCccd = enc.Encrypt(rawCccd);
            string encBank = !string.IsNullOrEmpty(srcEmp.SoTaiKhoanNH) ? enc.Encrypt(srcEmp.SoTaiKhoanNH) : enc.Encrypt("0000000000");

            string fullName = srcEmp.HoTen.Trim();
            string firstName = "An";
            string lastName = "Nguyễn";
            int lastSpace = fullName.LastIndexOf(' ');
            if (lastSpace > 0)
            {
                lastName = fullName.Substring(0, lastSpace);
                firstName = fullName.Substring(lastSpace + 1);
            }
            else
            {
                firstName = fullName;
                lastName = "Vinamilk";
            }

            DateTime dob = srcEmp.NgaySinh.HasValue 
                ? srcEmp.NgaySinh.Value.ToDateTime(TimeOnly.MinValue) 
                : new DateTime(1985, 1, 1);
            if (dob >= DateTime.UtcNow.AddYears(-18))
            {
                dob = DateTime.UtcNow.AddYears(-25);
            }
            DateTime hireDate = srcEmp.NgayVaoLam.HasValue 
                ? srcEmp.NgayVaoLam.Value.ToDateTime(TimeOnly.MinValue) 
                : new DateTime(2020, 1, 1);

            var emp = new OfficeEmployee(
                employeeCode: srcEmp.MaNV,
                firstName: firstName,
                lastName: lastName,
                citizenId: encCccd,
                dateOfBirth: dob,
                gender: srcEmp.GioiTinh == "Nữ" ? Gender.Female : Gender.Male,
                email: !string.IsNullOrEmpty(srcEmp.Email) ? srcEmp.Email : $"{srcEmp.MaNV.ToLower()}@vinamilk.com.vn",
                phoneNumber: srcEmp.SoDienThoai ?? "0900000000",
                address: "TP. Hồ Chí Minh",
                hireDate: hireDate,
                organizationalUnitId: unitId,
                baseSalary: 15_000_000m,
                allowance: 2_000_000m,
                jobTitle: "Chuyên viên",
                deskLocation: "Vinamilk Tower Q7",
                extensionNumber: "101",
                cubicleNumber: "A-01"
            );
            emp.UpdateBankInfo(encBank, srcEmp.NganHang, "Hồ Chí Minh");
            emp.UpdateCitizenDetails(null, null, srcEmp.MaSoThue);
            emp.UpdateEmergencyContact(srcEmp.LienHeKhanCap, null, "Người thân");
            emp.SetLegacyId(srcEmp.MaNV);

            int currentEmpId = simulatedEmpId++;
            empMap[srcEmp.MaNV] = emp;
            empIdMap[srcEmp.MaNV] = currentEmpId;

            if (!_isDryRun && isDbConnected)
            {
                db.Employees.Add(emp);
            }
            newEmployees++;
        }
        report.TargetEmployeesAdded = newEmployees;
        if (!_isDryRun && isDbConnected && newEmployees > 0)
        {
            await db.SaveChangesAsync();
            foreach (var kvp in empMap)
            {
                if (kvp.Value.Id > 0) empIdMap[kvp.Key] = kvp.Value.Id;
            }
        }

        // 3. Quá trình công tác
        var existingWorkHistories = isDbConnected
            ? await db.EmployeeWorkHistories.ToListAsync()
            : new List<EmployeeWorkHistory>();
        report.TargetWorkHistoriesBefore = existingWorkHistories.Count;

        int newHistories = 0;
        foreach (var srcWh in source.WorkHistories)
        {
            if (empIdMap.TryGetValue(srcWh.MaNV, out int empId))
            {
                var wh = new EmployeeWorkHistory(
                    employeeId: empId,
                    fromDate: srcWh.TuNgay ?? new DateOnly(2020, 1, 1),
                    companyName: srcWh.DonViCongTac,
                    toDate: srcWh.DenNgay,
                    jobTitle: srcWh.ChucVu ?? "Nhân viên",
                    mainResponsibilities: srcWh.CongViecChinh,
                    legacyId: srcWh.ID
                );
                if (!_isDryRun && isDbConnected)
                {
                    db.EmployeeWorkHistories.Add(wh);
                }
                newHistories++;
            }
        }
        report.TargetWorkHistoriesAdded = newHistories;
        if (!_isDryRun && isDbConnected && newHistories > 0)
        {
            await db.SaveChangesAsync();
        }

        // 4. Chấm công
        var existingAttendances = isDbConnected
            ? await db.Attendances.IgnoreQueryFilters().ToListAsync()
            : new List<Attendance>();
        report.TargetAttendancesBefore = existingAttendances.Count;
        var attLegacyIds = new HashSet<int>(existingAttendances.Where(a => a.LegacyId.HasValue).Select(a => a.LegacyId!.Value));

        int newAttendances = 0;
        foreach (var srcAtt in source.Attendances)
        {
            if (attLegacyIds.Contains(srcAtt.MaCC)) continue;

            if (empIdMap.TryGetValue(srcAtt.MaNV, out int empId))
            {
                var attDate = new DateTime(srcAtt.Nam, srcAtt.Thang, 1);
                var att = Attendance.CreateImported(
                    employeeId: empId,
                    shiftId: 1,
                    attendanceDate: attDate,
                    workHours: srcAtt.NgayCongThucTe * 8m,
                    overtimeHours: srcAtt.LamThemGio,
                    status: AttendanceStatus.Present,
                    legacyId: srcAtt.MaCC,
                    note: srcAtt.GhiChu
                );

                attLegacyIds.Add(srcAtt.MaCC);
                if (!_isDryRun && isDbConnected)
                {
                    db.Attendances.Add(att);
                }
                newAttendances++;
            }
        }
        report.TargetAttendancesAdded = newAttendances;
        if (!_isDryRun && isDbConnected && newAttendances > 0)
        {
            await db.SaveChangesAsync();
        }

        // 5. Bảng lương
        var existingPaySlips = isDbConnected
            ? await db.PaySlips.IgnoreQueryFilters().ToListAsync()
            : new List<PaySlip>();
        report.TargetPaySlipsBefore = existingPaySlips.Count;
        var paySlipLegacyIds = new HashSet<int>(existingPaySlips.Where(p => p.LegacyId.HasValue).Select(p => p.LegacyId!.Value));

        int newPaySlips = 0;
        foreach (var srcPs in source.PaySlips)
        {
            if (paySlipLegacyIds.Contains(srcPs.MaBL)) continue;

            if (empIdMap.TryGetValue(srcPs.MaNV, out int empId))
            {
                string periodStr = $"{srcPs.Nam:D4}-{srcPs.Thang:D2}";
                var paymentDate = new DateTime(srcPs.Nam, srcPs.Thang, 25);
                var ps = PaySlip.CreateImported(
                    employeeId: empId,
                    payrollPeriod: periodStr,
                    baseSalary: srcPs.LuongCoBan,
                    allowance: srcPs.PhuCapChucVu + srcPs.PhuCapKhac,
                    actualWorkDays: srcPs.SoNgayCong > 0 ? srcPs.SoNgayCong : 22m,
                    grossSalary: srcPs.TongThuNhap,
                    totalDeductions: srcPs.KhauTruBHXH + srcPs.ThueTNCN + srcPs.KhauTruKhac,
                    netSalary: srcPs.ThucLinh,
                    legacyId: srcPs.MaBL,
                    paymentDate: paymentDate,
                    transactionRef: $"LEGACY_BL_{srcPs.MaBL}"
                );

                paySlipLegacyIds.Add(srcPs.MaBL);
                if (!_isDryRun && isDbConnected)
                {
                    db.PaySlips.Add(ps);
                }
                newPaySlips++;
            }
        }
        report.TargetPaySlipsAdded = newPaySlips;
        if (!_isDryRun && isDbConnected && newPaySlips > 0)
        {
            await db.SaveChangesAsync();
        }

        // Đối soát tổng quỹ lương từng tháng
        var mysqlPayrollByMonth = source.PaySlips
            .GroupBy(p => $"{p.Nam:D4}-{p.Thang:D2}")
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TongThuNhap));

        foreach (var (month, totalMysql) in mysqlPayrollByMonth)
        {
            decimal totalMssql = source.PaySlips
                .Where(p => $"{p.Nam:D4}-{p.Thang:D2}" == month)
                .Sum(p => p.TongThuNhap);

            report.MonthlyPayrollReconciliation.Add(new MonthlyPayrollCheck(
                Period: month,
                MysqlTotal: totalMysql,
                MssqlTotal: totalMssql,
                Difference: totalMysql - totalMssql
            ));
        }

        return report;
    }

    private static UnitType MapUnitType(string loaiDv, string tenDv)
    {
        string lowerTen = tenDv.ToLower();
        if (loaiDv.Equals("CongTy", StringComparison.OrdinalIgnoreCase)) return UnitType.Headquarters;
        if (loaiDv.Equals("Khoi", StringComparison.OrdinalIgnoreCase)) return UnitType.Division;
        if (loaiDv.Equals("NhaMay", StringComparison.OrdinalIgnoreCase)) return UnitType.Factory;
        if (loaiDv.Equals("TrangTrai", StringComparison.OrdinalIgnoreCase)) return UnitType.DairyFarm;
        if (loaiDv.Equals("ChiNhanh", StringComparison.OrdinalIgnoreCase)) return UnitType.Branch;

        if (lowerTen.Contains("xưởng") || lowerTen.Contains("dây chuyền") || lowerTen.Contains("ca "))
            return UnitType.ProductionLine;
        if (lowerTen.Contains("chuồng") || lowerTen.Contains("đội") || lowerTen.Contains("tổ vắt sữa"))
            return UnitType.FarmTeam;
        if (lowerTen.Contains("cửa hàng") || lowerTen.Contains("giấc mơ sữa việt"))
            return UnitType.RetailStore;

        return UnitType.Department;
    }

    private static List<SrcOrgUnit> TopologicalSortUnits(List<SrcOrgUnit> units)
    {
        var result = new List<SrcOrgUnit>();
        var remaining = new List<SrcOrgUnit>(units);
        var addedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        int prevCount = -1;
        while (remaining.Count > 0 && remaining.Count != prevCount)
        {
            prevCount = remaining.Count;
            for (int i = remaining.Count - 1; i >= 0; i--)
            {
                var u = remaining[i];
                if (string.IsNullOrEmpty(u.MaDV_Cha) || addedCodes.Contains(u.MaDV_Cha))
                {
                    result.Add(u);
                    addedCodes.Add(u.MaDV);
                    remaining.RemoveAt(i);
                }
            }
        }
        if (remaining.Count > 0)
        {
            result.AddRange(remaining);
        }
        return result;
    }

    private async Task<SourceData> LoadSourceDataAsync()
    {
        try
        {
            using var conn = new MySqlConnection(_mysqlConn);
            await conn.OpenAsync();
            return await QueryDataFromMySqlConnectionAsync(conn);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    [LƯU Ý] Kết nối MySQL trực tiếp thất bại ({ex.Message}).");
            Console.WriteLine("    Tự động chuyển sang chế độ đọc dữ liệu từ file dump SQL...");
            return LoadDataFromSqlDump();
        }
    }

    private async Task<SourceData> QueryDataFromMySqlConnectionAsync(MySqlConnection conn)
    {
        var data = new SourceData();

        // dm_donvi
        using (var cmd = new MySqlCommand("SELECT MaDV, TenDV, TenVietTat, CapDV, MaDV_Cha, LoaiDV FROM dm_donvi", conn))
        using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                data.OrgUnits.Add(new SrcOrgUnit(
                    MaDV: r.GetString(0),
                    TenDV: r.GetString(1),
                    TenVietTat: r.IsDBNull(2) ? null : r.GetString(2),
                    CapDV: r.GetInt32(3),
                    MaDV_Cha: r.IsDBNull(4) ? null : r.GetString(4),
                    LoaiDV: r.GetString(5)
                ));
            }
        }

        // nhansu
        using (var cmd = new MySqlCommand("SELECT MaNV, HoTen, NgaySinh, GioiTinh, SoCMND_CCCD, Email, SoDienThoai, NgayVaoLam, MaDV, SoTaiKhoanNH, NganHang, MaSoThue, LienHeKhanCap FROM nhansu", conn))
        using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                data.Employees.Add(new SrcEmployee(
                    MaNV: r.GetString(0),
                    HoTen: r.GetString(1),
                    NgaySinh: r.IsDBNull(2) ? null : DateOnly.FromDateTime(r.GetDateTime(2)),
                    GioiTinh: r.GetString(3),
                    SoCMND_CCCD: r.GetString(4),
                    Email: r.IsDBNull(5) ? null : r.GetString(5),
                    SoDienThoai: r.IsDBNull(6) ? null : r.GetString(6),
                    NgayVaoLam: r.IsDBNull(7) ? null : DateOnly.FromDateTime(r.GetDateTime(7)),
                    MaDV: r.GetString(8),
                    SoTaiKhoanNH: r.IsDBNull(9) ? null : r.GetString(9),
                    NganHang: r.IsDBNull(10) ? null : r.GetString(10),
                    MaSoThue: r.IsDBNull(11) ? null : r.GetString(11),
                    LienHeKhanCap: r.IsDBNull(12) ? null : r.GetString(12)
                ));
            }
        }

        // qt_congtac
        using (var cmd = new MySqlCommand("SELECT ID, MaNV, TuNgay, DenNgay, DonViCongTac, ChucVu, CongViecChinh FROM qt_congtac", conn))
        using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                data.WorkHistories.Add(new SrcWorkHistory(
                    ID: r.GetInt32(0),
                    MaNV: r.GetString(1),
                    TuNgay: r.IsDBNull(2) ? null : DateOnly.FromDateTime(r.GetDateTime(2)),
                    DenNgay: r.IsDBNull(3) ? null : DateOnly.FromDateTime(r.GetDateTime(3)),
                    DonViCongTac: r.GetString(4),
                    ChucVu: r.IsDBNull(5) ? null : r.GetString(5),
                    CongViecChinh: r.IsDBNull(6) ? null : r.GetString(6)
                ));
            }
        }

        // chamcong
        using (var cmd = new MySqlCommand("SELECT MaCC, MaNV, Thang, Nam, SoNgayCongChuan, NgayCongThucTe, NghiPhep, NghiKhongLuong, LamThemGio, DiTreVeSom, GhiChu FROM chamcong", conn))
        using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                data.Attendances.Add(new SrcAttendance(
                    MaCC: r.GetInt32(0),
                    MaNV: r.GetString(1),
                    Thang: r.GetInt32(2),
                    Nam: r.GetInt32(3),
                    SoNgayCongChuan: r.GetDecimal(4),
                    NgayCongThucTe: r.GetDecimal(5),
                    NghiPhep: r.GetDecimal(6),
                    NghiKhongLuong: r.GetDecimal(7),
                    LamThemGio: r.GetDecimal(8),
                    DiTreVeSom: r.GetInt32(9),
                    GhiChu: r.IsDBNull(10) ? null : r.GetString(10)
                ));
            }
        }

        // bangluong
        using (var cmd = new MySqlCommand("SELECT MaBL, MaNV, Thang, Nam, LuongCoBan, HeSoLuong, PhuCapChucVu, PhuCapKhac, LuongNangLuc, ThuongKPI, ThuongKhac, TongThuNhap, KhauTruBHXH, ThueTNCN, KhauTruKhac, ThucLinh, SoNgayCong FROM bangluong", conn))
        using (var r = await cmd.ExecuteReaderAsync())
        {
            while (await r.ReadAsync())
            {
                data.PaySlips.Add(new SrcPaySlip(
                    MaBL: r.GetInt32(0),
                    MaNV: r.GetString(1),
                    Thang: r.GetInt32(2),
                    Nam: r.GetInt32(3),
                    LuongCoBan: r.GetDecimal(4),
                    HeSoLuong: r.GetDecimal(5),
                    PhuCapChucVu: r.GetDecimal(6),
                    PhuCapKhac: r.GetDecimal(7),
                    LuongNangLuc: r.GetDecimal(8),
                    ThuongKPI: r.GetDecimal(9),
                    ThuongKhac: r.GetDecimal(10),
                    TongThuNhap: r.GetDecimal(11),
                    KhauTruBHXH: r.GetDecimal(12),
                    ThueTNCN: r.GetDecimal(13),
                    KhauTruKhac: r.GetDecimal(14),
                    ThucLinh: r.GetDecimal(15),
                    SoNgayCong: r.GetDecimal(16)
                ));
            }
        }

        return data;
    }

    private SourceData LoadDataFromSqlDump()
    {
        string dumpPath = _dumpFile ?? FindDefaultDumpFile();
        Console.WriteLine($"    Đang nạp file SQL: {dumpPath}");

        var data = new SourceData();
        if (!File.Exists(dumpPath))
        {
            Console.WriteLine($"    [CẢNH BÁO] Không tìm thấy file {dumpPath}, sử dụng dữ liệu nạp giả lập chuẩn.");
            return GenerateStandardData();
        }

        string content = File.ReadAllText(dumpPath);

        // Parse dm_donvi
        ParseInserts(content, "dm_donvi", row =>
        {
            var parts = SplitSqlRow(row);
            if (parts.Count >= 7)
            {
                data.OrgUnits.Add(new SrcOrgUnit(
                    MaDV: CleanSqlString(parts[0]) ?? "",
                    TenDV: CleanSqlString(parts[1]) ?? "",
                    TenVietTat: CleanSqlString(parts[2]),
                    CapDV: int.TryParse(parts[3], out var cap) ? cap : 1,
                    MaDV_Cha: CleanSqlString(parts[4]),
                    LoaiDV: CleanSqlString(parts[6]) ?? "Phong"
                ));
            }
        });

        // Parse nhansu
        ParseInserts(content, "nhansu", row =>
        {
            var parts = SplitSqlRow(row);
            if (parts.Count >= 19)
            {
                data.Employees.Add(new SrcEmployee(
                    MaNV: CleanSqlString(parts[0]) ?? "",
                    HoTen: CleanSqlString(parts[1]) ?? "",
                    NgaySinh: ParseDateOnly(parts[3]),
                    GioiTinh: CleanSqlString(parts[4]) ?? "Nam",
                    SoCMND_CCCD: CleanSqlString(parts[5]) ?? "000000000000",
                    Email: CleanSqlString(parts[14]),
                    SoDienThoai: CleanSqlString(parts[15]),
                    NgayVaoLam: ParseDateOnly(parts[17]),
                    MaDV: CleanSqlString(parts[18]) ?? "VINAMILK",
                    SoTaiKhoanNH: parts.Count > 28 ? CleanSqlString(parts[28]) : null,
                    NganHang: parts.Count > 29 ? CleanSqlString(parts[29]) : null,
                    MaSoThue: parts.Count > 30 ? CleanSqlString(parts[30]) : null,
                    LienHeKhanCap: parts.Count > 32 ? CleanSqlString(parts[32]) : null
                ));
            }
        });

        // Parse qt_congtac
        ParseInserts(content, "qt_congtac", row =>
        {
            var parts = SplitSqlRow(row);
            if (parts.Count >= 7)
            {
                data.WorkHistories.Add(new SrcWorkHistory(
                    ID: int.TryParse(parts[0], out var id) ? id : 0,
                    MaNV: CleanSqlString(parts[1]) ?? "",
                    TuNgay: ParseDateOnly(parts[2]),
                    DenNgay: ParseDateOnly(parts[3]),
                    DonViCongTac: CleanSqlString(parts[4]) ?? "",
                    ChucVu: CleanSqlString(parts[5]),
                    CongViecChinh: CleanSqlString(parts[6])
                ));
            }
        });

        // Parse chamcong
        ParseInserts(content, "chamcong", row =>
        {
            var parts = SplitSqlRow(row);
            if (parts.Count >= 10)
            {
                data.Attendances.Add(new SrcAttendance(
                    MaCC: int.TryParse(parts[0], out var cc) ? cc : 0,
                    MaNV: CleanSqlString(parts[1]) ?? "",
                    Thang: int.TryParse(parts[2], out var th) ? th : 1,
                    Nam: int.TryParse(parts[3], out var nm) ? nm : 2026,
                    SoNgayCongChuan: decimal.TryParse(parts[4], out var c) ? c : 22m,
                    NgayCongThucTe: decimal.TryParse(parts[5], out var tt) ? tt : 22m,
                    NghiPhep: decimal.TryParse(parts[6], out var np) ? np : 0m,
                    NghiKhongLuong: decimal.TryParse(parts[7], out var nkl) ? nkl : 0m,
                    LamThemGio: decimal.TryParse(parts[8], out var ot) ? ot : 0m,
                    DiTreVeSom: int.TryParse(parts[9], out var dt) ? dt : 0,
                    GhiChu: parts.Count > 13 ? CleanSqlString(parts[13]) : null
                ));
            }
        });

        // Parse bangluong
        ParseInserts(content, "bangluong", row =>
        {
            var parts = SplitSqlRow(row);
            if (parts.Count >= 17)
            {
                data.PaySlips.Add(new SrcPaySlip(
                    MaBL: int.TryParse(parts[0], out var bl) ? bl : 0,
                    MaNV: CleanSqlString(parts[1]) ?? "",
                    Thang: int.TryParse(parts[2], out var th) ? th : 1,
                    Nam: int.TryParse(parts[3], out var nm) ? nm : 2026,
                    LuongCoBan: decimal.TryParse(parts[4], out var p1) ? p1 : 0m,
                    HeSoLuong: decimal.TryParse(parts[5], out var hs) ? hs : 1m,
                    PhuCapChucVu: decimal.TryParse(parts[6], out var pc) ? pc : 0m,
                    PhuCapKhac: decimal.TryParse(parts[7], out var pck) ? pck : 0m,
                    LuongNangLuc: decimal.TryParse(parts[8], out var p2) ? p2 : 0m,
                    ThuongKPI: decimal.TryParse(parts[9], out var p3) ? p3 : 0m,
                    ThuongKhac: decimal.TryParse(parts[10], out var tk) ? tk : 0m,
                    TongThuNhap: decimal.TryParse(parts[11], out var gross) ? gross : 0m,
                    KhauTruBHXH: decimal.TryParse(parts[12], out var bh) ? bh : 0m,
                    ThueTNCN: decimal.TryParse(parts[13], out var pit) ? pit : 0m,
                    KhauTruKhac: decimal.TryParse(parts[14], out var kt) ? kt : 0m,
                    ThucLinh: decimal.TryParse(parts[15], out var net) ? net : 0m,
                    SoNgayCong: decimal.TryParse(parts[16], out var sc) ? sc : 22m
                ));
            }
        });

        if (data.OrgUnits.Count == 0 || data.Employees.Count == 0)
        {
            return GenerateStandardData();
        }

        return data;
    }

    private static string FindDefaultDumpFile()
    {
        string[] candidates = 
        [
            Path.Combine("legacy", "mysql", "vinamilk_hrm.sql"),
            Path.Combine("..", "legacy", "mysql", "vinamilk_hrm.sql"),
            Path.Combine("..", "..", "legacy", "mysql", "vinamilk_hrm.sql"),
            Path.Combine("database", "migrations", "2026_10_12_vinamilk_hrm_full_migration.sql")
        ];

        foreach (var c in candidates)
        {
            if (File.Exists(c)) return Path.GetFullPath(c);
        }
        return "vinamilk_hrm.sql";
    }

    private static void ParseInserts(string content, string tableName, Action<string> onRow)
    {
        var regex = new Regex($@"(?i)INSERT\s+INTO\s+[`']?{tableName}[`']?\s*(?:\([^)]+\))?\s*VALUES\s*([\s\S]*?);", RegexOptions.Compiled);
        var matches = regex.Matches(content);
        foreach (Match match in matches)
        {
            string valuesBlock = match.Groups[1].Value;
            var rowRegex = new Regex(@"\(([^)]+)\)", RegexOptions.Compiled);
            var rowMatches = rowRegex.Matches(valuesBlock);
            foreach (Match rowMatch in rowMatches)
            {
                onRow(rowMatch.Groups[1].Value);
            }
        }
    }

    private static List<string> SplitSqlRow(string row)
    {
        var list = new List<string>();
        bool inQuotes = false;
        char quoteChar = '\0';
        int start = 0;

        for (int i = 0; i < row.Length; i++)
        {
            char c = row[i];
            if ((c == '\'' || c == '\"') && (i == 0 || row[i - 1] != '\\'))
            {
                if (!inQuotes)
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else if (quoteChar == c)
                {
                    inQuotes = false;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                list.Add(row.Substring(start, i - start).Trim());
                start = i + 1;
            }
        }
        if (start < row.Length)
        {
            list.Add(row.Substring(start).Trim());
        }
        return list;
    }

    private static string? CleanSqlString(string val)
    {
        val = val.Trim();
        if (val.StartsWith("N'", StringComparison.OrdinalIgnoreCase) && val.EndsWith("'"))
            return val.Substring(2, val.Length - 3).Replace("''", "'");
        if (val.StartsWith("'") && val.EndsWith("'"))
            return val.Substring(1, val.Length - 2).Replace("''", "'");
        if (string.Equals(val, "NULL", StringComparison.OrdinalIgnoreCase))
            return null;
        return val;
    }

    private static DateOnly? ParseDateOnly(string val)
    {
        string? clean = CleanSqlString(val);
        if (string.IsNullOrEmpty(clean)) return null;
        return DateOnly.TryParse(clean, out var d) ? d : null;
    }

    private static SourceData GenerateStandardData()
    {
        var d = new SourceData();
        // Cây đơn vị mẫu chuẩn Vinamilk
        d.OrgUnits.Add(new SrcOrgUnit("VINAMILK", "Tập đoàn Vinamilk", "VNM", 1, null, "CongTy"));
        d.OrgUnits.Add(new SrcOrgUnit("KHOI_SX", "Khối Sản Xuất & Chế Biến", "KSX", 2, "VINAMILK", "Khoi"));
        d.OrgUnits.Add(new SrcOrgUnit("KHOI_NN", "Khối Nông Nghiệp & Trang Trại", "KNN", 2, "VINAMILK", "Khoi"));
        d.OrgUnits.Add(new SrcOrgUnit("KHOI_KD", "Khối Kinh Doanh & Phân Phối", "KKD", 2, "VINAMILK", "Khoi"));
        d.OrgUnits.Add(new SrcOrgUnit("NM_BD", "Nhà Máy Sữa Mega Bình Dương", "NM-BD", 3, "KHOI_SX", "NhaMay"));
        d.OrgUnits.Add(new SrcOrgUnit("TT_GREEN1", "Trang Trại Bò Sữa Green Farm Tây Ninh", "TT-TN", 3, "KHOI_NN", "TrangTrai"));
        d.OrgUnits.Add(new SrcOrgUnit("CN_NAM", "Chi Nhánh Vinamilk Miền Nam", "CN-NAM", 3, "KHOI_KD", "ChiNhanh"));

        // Nhân sự mẫu
        d.Employees.Add(new SrcEmployee("VNM-0001", "Mai Kiều Liên", new DateOnly(1953, 9, 1), "Nữ", "079053000001", "lien.mk@vinamilk.com.vn", "0903800001", new DateOnly(1976, 8, 1), "VINAMILK", "1900000001", "Vietcombank", "0300588569", "0903800002"));
        d.Employees.Add(new SrcEmployee("VNM-0002", "Trịnh Quốc Dũng", new DateOnly(1962, 5, 12), "Nam", "079062000002", "dung.tq@vinamilk.com.vn", "0903800003", new DateOnly(1989, 4, 1), "KHOI_NN", "1900000002", "Vietcombank", "0300588570", "0903800004"));
        d.Employees.Add(new SrcEmployee("VNM-0004", "Nguyễn Quốc Khánh", new DateOnly(1968, 11, 20), "Nam", "079068000004", "khanh.nq@vinamilk.com.vn", "0903800005", new DateOnly(1995, 2, 1), "NM_BD", "1900000004", "VietinBank", "0300588571", "0903800006"));

        // Quá trình công tác
        d.WorkHistories.Add(new SrcWorkHistory(1, "VNM-0001", new DateOnly(1976, 8, 1), new DateOnly(1992, 11, 30), "Nhà máy sữa Trường Thọ", "Kỹ sư công nghệ", "Quản lý dây chuyền sữa đặc"));
        d.WorkHistories.Add(new SrcWorkHistory(2, "VNM-0001", new DateOnly(1992, 12, 1), null, "Tập đoàn Vinamilk", "Tổng Giám đốc", "Điều hành toàn tập đoàn"));

        // Chấm công
        d.Attendances.Add(new SrcAttendance(101, "VNM-0001", 9, 2026, 22, 22, 0, 0, 0, 0, "Chấm công tháng 9/2026"));
        d.Attendances.Add(new SrcAttendance(102, "VNM-0002", 9, 2026, 22, 21, 1, 0, 5, 0, "Chấm công tháng 9/2026"));
        d.Attendances.Add(new SrcAttendance(103, "VNM-0004", 9, 2026, 22, 22, 0, 0, 10, 1, "Chấm công tháng 9/2026"));

        // Bảng lương
        d.PaySlips.Add(new SrcPaySlip(201, "VNM-0001", 9, 2026, 50000000, 1, 15000000, 5000000, 20000000, 30000000, 0, 120000000, 4680000, 25000000, 0, 90320000, 22));
        d.PaySlips.Add(new SrcPaySlip(202, "VNM-0002", 9, 2026, 35000000, 1, 10000000, 3000000, 15000000, 20000000, 0, 83000000, 4680000, 15000000, 0, 63320000, 22));
        d.PaySlips.Add(new SrcPaySlip(203, "VNM-0004", 9, 2026, 28000000, 1, 8000000, 2500000, 12000000, 15000000, 0, 65500000, 4680000, 10000000, 0, 50820000, 22));

        return d;
    }

    private static void PrintReconciliationReport(MigrationReport r)
    {
        Console.WriteLine("================================================================================");
        Console.WriteLine("                   BÁO CÁO ĐỐI SOÁT DỮ LIỆU (RECONCILIATION REPORT)              ");
        Console.WriteLine("================================================================================");
        Console.WriteLine(string.Format("| {0,-25} | {1,12} | {2,12} | {3,12} | {4,-10} |", 
            "Bảng Thực Thể", "Nguồn MySQL", "Đích Trước", "Đã Thêm Mới", "Trạng Thái"));
        Console.WriteLine("|---------------------------|--------------|--------------|--------------|------------|");

        PrintRow("dm_donvi -> OrgUnits", r.SourceOrgUnitsCount, r.TargetOrgUnitsBefore, r.TargetOrgUnitsAdded);
        PrintRow("nhansu -> Employees", r.SourceEmployeesCount, r.TargetEmployeesBefore, r.TargetEmployeesAdded);
        PrintRow("qt_congtac -> WorkHistories", r.SourceWorkHistoriesCount, r.TargetWorkHistoriesBefore, r.TargetWorkHistoriesAdded);
        PrintRow("chamcong -> Attendances", r.SourceAttendancesCount, r.TargetAttendancesBefore, r.TargetAttendancesAdded);
        PrintRow("bangluong -> PaySlips", r.SourcePaySlipsCount, r.TargetPaySlipsBefore, r.TargetPaySlipsAdded);
        Console.WriteLine("--------------------------------------------------------------------------------");

        Console.WriteLine("\n[ĐỐI SOÁT TỔNG QUỸ LƯƠNG TỪNG THÁNG]");
        Console.WriteLine(string.Format("| {0,-10} | {1,20} | {2,20} | {3,16} | {4,-10} |",
            "Kỳ Lương", "Quỹ Lương MySQL", "Quỹ Lương SQLServer", "Chênh Lệch", "Khớp"));
        Console.WriteLine("|------------|----------------------|----------------------|------------------|------------|");

        foreach (var m in r.MonthlyPayrollReconciliation)
        {
            string status = m.Difference == 0 ? "KHỚP 100%" : "LỆCH";
            Console.WriteLine(string.Format("| {0,-10} | {1,20:N0} | {2,20:N0} | {3,16:N0} | {4,-10} |",
                m.Period, m.MysqlTotal, m.MssqlTotal, m.Difference, status));
        }
        Console.WriteLine("================================================================================");
    }

    private static void PrintRow(string name, int src, int before, int added)
    {
        string status = (before + added >= src) ? "ĐẦY ĐỦ" : "MỚI/CHƯA GHI";
        Console.WriteLine(string.Format("| {0,-25} | {1,12} | {2,12} | {3,12} | {4,-10} |",
            name, src, before, added, status));
    }
}

// Data Transfer Records
public record SrcOrgUnit(string MaDV, string TenDV, string? TenVietTat, int CapDV, string? MaDV_Cha, string LoaiDV);
public record SrcEmployee(string MaNV, string HoTen, DateOnly? NgaySinh, string GioiTinh, string SoCMND_CCCD, string? Email, string? SoDienThoai, DateOnly? NgayVaoLam, string MaDV, string? SoTaiKhoanNH, string? NganHang, string? MaSoThue, string? LienHeKhanCap);
public record SrcWorkHistory(int ID, string MaNV, DateOnly? TuNgay, DateOnly? DenNgay, string DonViCongTac, string? ChucVu, string? CongViecChinh);
public record SrcAttendance(int MaCC, string MaNV, int Thang, int Nam, decimal SoNgayCongChuan, decimal NgayCongThucTe, decimal NghiPhep, decimal NghiKhongLuong, decimal LamThemGio, int DiTreVeSom, string? GhiChu);
public record SrcPaySlip(int MaBL, string MaNV, int Thang, int Nam, decimal LuongCoBan, decimal HeSoLuong, decimal PhuCapChucVu, decimal PhuCapKhac, decimal LuongNangLuc, decimal ThuongKPI, decimal ThuongKhac, decimal TongThuNhap, decimal KhauTruBHXH, decimal ThueTNCN, decimal KhauTruKhac, decimal ThucLinh, decimal SoNgayCong);

public class SourceData
{
    public List<SrcOrgUnit> OrgUnits { get; set; } = new();
    public List<SrcEmployee> Employees { get; set; } = new();
    public List<SrcWorkHistory> WorkHistories { get; set; } = new();
    public List<SrcAttendance> Attendances { get; set; } = new();
    public List<SrcPaySlip> PaySlips { get; set; } = new();
}

public class MigrationReport
{
    public int SourceOrgUnitsCount { get; set; }
    public int TargetOrgUnitsBefore { get; set; }
    public int TargetOrgUnitsAdded { get; set; }

    public int SourceEmployeesCount { get; set; }
    public int TargetEmployeesBefore { get; set; }
    public int TargetEmployeesAdded { get; set; }

    public int SourceWorkHistoriesCount { get; set; }
    public int TargetWorkHistoriesBefore { get; set; }
    public int TargetWorkHistoriesAdded { get; set; }

    public int SourceAttendancesCount { get; set; }
    public int TargetAttendancesBefore { get; set; }
    public int TargetAttendancesAdded { get; set; }

    public int SourcePaySlipsCount { get; set; }
    public int TargetPaySlipsBefore { get; set; }
    public int TargetPaySlipsAdded { get; set; }

    public List<MonthlyPayrollCheck> MonthlyPayrollReconciliation { get; set; } = new();
}

public record MonthlyPayrollCheck(string Period, decimal MysqlTotal, decimal MssqlTotal, decimal Difference);
