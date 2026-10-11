using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Interfaces.Repositories;

namespace VinamilkHRM.WebUI.Controllers;

/// <summary>
/// Controller Quản trị và Kết xuất Biểu mẫu Hành chính chuẩn A4 (@media print).
/// Bao gồm:
/// 1. Sơ yếu lý lịch 2C-BNV/2008 chuẩn Bộ Nội Vụ (Print_2C).
/// 2. BM01: Báo cáo Trích ngang nhân sự (A4 Ngang).
/// 3. BM02: Báo cáo Biến động nhân sự (A4 Ngang).
/// 4. BM03: Thống kê Khen thưởng & Kỷ luật (A4 Ngang).
/// 5. BM04: Danh sách Nhân sự tiệm cận nghỉ hưu theo Luật Lao động (A4 Ngang).
/// </summary>
public class AdministrativeReportsController : Controller
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IOrganizationalUnitRepository _unitRepository;

    public AdministrativeReportsController(
        IApplicationDbContext dbContext,
        IEmployeeRepository employeeRepository,
        IOrganizationalUnitRepository unitRepository)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _unitRepository = unitRepository ?? throw new ArgumentNullException(nameof(unitRepository));
    }

    /// <summary>
    /// Bảng điều khiển Trung tâm Quản trị In ấn Biểu mẫu Hành chính.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var employees = await _employeeRepository.GetAllAsync();
        var units = await _unitRepository.GetAllAsync();

        ViewBag.Employees = employees;
        ViewBag.Units = units;

        return View();
    }

    /// <summary>
    /// Sơ yếu lý lịch Mẫu 2C-BNV/2008 của Bộ Nội vụ & Vinamilk Corporate (Khổ A4 Đứng).
    /// </summary>
    public async Task<IActionResult> Print2C(int? id)
    {
        Employee? employee;
        if (id.HasValue && id.Value > 0)
        {
            employee = await _employeeRepository.GetWithDetailsAsync(id.Value);
        }
        else
        {
            var first = (await _employeeRepository.GetAllAsync()).FirstOrDefault();
            employee = first != null ? await _employeeRepository.GetWithDetailsAsync(first.Id) : null;
        }

        if (employee == null)
        {
            return NotFound("Không tìm thấy thông tin nhân viên để in Sơ yếu lý lịch 2C.");
        }

        ViewBag.Employee = employee;
        ViewBag.LaborContract = await _dbContext.LaborContracts
            .Where(c => c.EmployeeId == employee.Id)
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync();

        return View();
    }

    /// <summary>
    /// BIỂU MẪU 01: Sơ yếu Lý lịch Trích ngang Cán bộ Công nhân viên (Khổ A4 Ngang).
    /// </summary>
    public async Task<IActionResult> BM01_Summary(int? unitId)
    {
        var query = _dbContext.Employees
            .Include(e => e.OrganizationalUnit)
            .Include(e => e.LaborContracts)
            .AsNoTracking();

        if (unitId.HasValue && unitId.Value > 0)
        {
            query = query.Where(e => e.OrganizationalUnitId == unitId.Value);
        }

        var employees = await query.OrderBy(e => e.OrganizationalUnitId).ThenBy(e => e.EmployeeCode).ToListAsync();
        var units = await _unitRepository.GetAllAsync();

        ViewBag.SelectedUnitId = unitId;
        ViewBag.Units = units;
        ViewBag.CurrentUnitName = unitId.HasValue
            ? units.FirstOrDefault(u => u.Id == unitId.Value)?.UnitName ?? "Toàn bộ Tập đoàn Vinamilk"
            : "Toàn bộ Tập đoàn Vinamilk";

        return View(employees);
    }

    /// <summary>
    /// BIỂU MẪU 02: Báo cáo Biến động Nhân sự (Tuyển mới, Điều chuyển, Nâng bậc lương, Nghỉ việc) (Khổ A4 Ngang).
    /// </summary>
    public async Task<IActionResult> BM02_Fluctuations(int? year)
    {
        int reportYear = year ?? DateTime.UtcNow.Year;

        // Lấy danh sách nhân viên và các biến động nâng lương từ phụ lục hợp đồng
        var employees = await _dbContext.Employees
            .Include(e => e.OrganizationalUnit)
            .AsNoTracking()
            .ToListAsync();

        var appendices = await _dbContext.ElectronicContractAppendices
            .Include(a => a.Employee)
                .ThenInclude(e => e!.OrganizationalUnit)
            .AsNoTracking()
            .Where(a => a.EffectiveDate.Year == reportYear)
            .OrderByDescending(a => a.EffectiveDate)
            .ToListAsync();

        ViewBag.ReportYear = reportYear;
        ViewBag.Appendices = appendices;

        return View(employees);
    }

    /// <summary>
    /// BIỂU MẪU 03: Thống kê Khen thưởng & Kỷ luật Toàn tập đoàn (Khổ A4 Ngang).
    /// </summary>
    public async Task<IActionResult> BM03_DisciplineRewards(int? year)
    {
        int reportYear = year ?? DateTime.UtcNow.Year;

        var warnings = await _dbContext.DisciplinaryWarnings
            .Include(w => w.Employee)
                .ThenInclude(e => e!.OrganizationalUnit)
            .AsNoTracking()
            .Where(w => w.WarningDate.Year == reportYear)
            .OrderByDescending(w => w.WarningDate)
            .ToListAsync();

        var employees = await _dbContext.Employees
            .Include(e => e.OrganizationalUnit)
            .AsNoTracking()
            .ToListAsync();

        ViewBag.ReportYear = reportYear;
        ViewBag.Warnings = warnings;

        return View(employees);
    }

    /// <summary>
    /// BIỂU MẪU 04: Thống kê Danh sách Nhân sự Tiệm cận Tuổi Nghỉ hưu theo Bộ luật Lao động (Khổ A4 Ngang).
    /// Lộ trình tăng tuổi nghỉ hưu theo Nghị định 135/2020/NĐ-CP (Nam: 62 tuổi vào 2028; Nữ: 60 tuổi vào 2035).
    /// </summary>
    public async Task<IActionResult> BM04_Retirement()
    {
        // Xét nhân viên sắp đến tuổi hưu trong vòng 1-3 năm tới (sinh trước hoặc trong giai đoạn tiệm cận)
        var allEmployees = await _dbContext.Employees
            .Include(e => e.OrganizationalUnit)
            .AsNoTracking()
            .ToListAsync();

        DateTime now = DateTime.UtcNow;

        // Tiêu chuẩn nghỉ hưu năm 2026: Nam 61 tuổi 6 tháng, Nữ 56 tuổi 8 tháng
        var nearRetirement = allEmployees
            .Select(e =>
            {
                int age = now.Year - e.DateOfBirth.Year;
                int retirementAge = e.Gender == Domain.Enums.Gender.Male ? 62 : 60;
                int yearsLeft = retirementAge - age;
                DateTime estimatedDate = e.DateOfBirth.AddYears(retirementAge);
                return new RetirementCandidateDto(
                    Employee: e,
                    CurrentAge: age,
                    RetirementAge: retirementAge,
                    YearsLeft: yearsLeft,
                    EstimatedRetirementDate: estimatedDate,
                    NoticeDate: estimatedDate.AddMonths(-6), // Thông báo trước 6 tháng theo Luật
                    DecisionDate: estimatedDate.AddMonths(-3) // Ra quyết định trước 3 tháng theo Luật
                );
            })
            .Where(x => x.YearsLeft <= 3 && x.YearsLeft >= -1)
            .OrderBy(x => x.EstimatedRetirementDate)
            .ToList();

        ViewBag.RetirementList = nearRetirement;

        return View(allEmployees);
    }
}

public record RetirementCandidateDto(
    Employee Employee,
    int CurrentAge,
    int RetirementAge,
    int YearsLeft,
    DateTime EstimatedRetirementDate,
    DateTime NoticeDate,
    DateTime DecisionDate);
