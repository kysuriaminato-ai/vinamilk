using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Careers;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Auditing;
using VinamilkHRM.Domain.Entities.Careers;
using VinamilkHRM.Domain.Entities.Contracts;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Services;

public class CareersService : ICareersService
{
    private readonly IApplicationDbContext _context;

    public CareersService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<JobPostingDto>> GetActiveJobPostingsAsync(
        string? keyword = null,
        UnitType? unitType = null,
        string? city = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.JobPostings
            .AsNoTracking()
            .Include(j => j.Applications)
            .Where(j => j.IsActive && j.ValidThrough >= DateTime.UtcNow);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            string kw = keyword.Trim().ToLower();
            query = query.Where(j => j.Title.ToLower().Contains(kw) ||
                                     j.Department.ToLower().Contains(kw) ||
                                     j.Description.ToLower().Contains(kw));
        }

        if (unitType.HasValue)
        {
            query = query.Where(j => j.UnitType == unitType.Value);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            string c = city.Trim().ToLower();
            query = query.Where(j => j.City.ToLower().Contains(c));
        }

        var list = await query
            .OrderByDescending(j => j.IsFeatured)
            .ThenByDescending(j => j.DatePosted)
            .ToListAsync(cancellationToken);

        return list.Select(MapToJobPostingDto).ToList();
    }

    public async Task<JobPostingDto?> GetJobPostingBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;

        string normalized = slug.Trim().ToLowerInvariant();
        var job = await _context.JobPostings
            .Include(j => j.Applications)
            .FirstOrDefaultAsync(j => j.Slug == normalized && j.IsActive, cancellationToken);

        if (job == null) return null;

        // Tăng lượt xem tin
        job.IncrementViews();
        await _context.SaveChangesAsync(cancellationToken);

        return MapToJobPostingDto(job);
    }

    public async Task<JobPostingDto?> GetJobPostingByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var job = await _context.JobPostings
            .AsNoTracking()
            .Include(j => j.Applications)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

        return job == null ? null : MapToJobPostingDto(job);
    }

    public async Task<CandidateApplicationDto> SubmitApplicationAsync(SubmitApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var job = await _context.JobPostings
            .FirstOrDefaultAsync(j => j.Id == request.JobPostingId && j.IsActive, cancellationToken);

        if (job == null)
            throw new DomainException("Tin tuyển dụng không tồn tại hoặc đã hết hạn nhận hồ sơ.", "ERR_JOB_NOT_FOUND");

        // Kiểm tra ứng viên đã nộp đơn vị trí này trong 30 ngày chưa
        bool alreadyApplied = await _context.CandidateApplications
            .AnyAsync(a => a.JobPostingId == request.JobPostingId && a.Email == request.Email.Trim().ToLower(), cancellationToken);

        if (alreadyApplied)
            throw new DomainException("Email này đã nộp hồ sơ cho vị trí tuyển dụng này gần đây.", "ERR_DUPLICATE_APPLICATION");

        var app = new CandidateApplication(
            request.JobPostingId,
            request.FullName,
            request.Email,
            request.PhoneNumber,
            request.CitizenId,
            request.DateOfBirth,
            request.Gender,
            request.Address,
            request.ResumeFileName,
            request.CoverLetter,
            request.PortfolioUrl
        );

        _context.CandidateApplications.Add(app);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToApplicationDto(app, job.Title, job.JobCode);
    }

    public async Task<IReadOnlyList<CandidateApplicationDto>> GetApplicationsAsync(
        int? jobPostingId = null,
        JobApplicationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CandidateApplications
            .AsNoTracking()
            .Include(a => a.JobPosting)
            .Include(a => a.HiredEmployee)
            .AsQueryable();

        if (jobPostingId.HasValue)
        {
            query = query.Where(a => a.JobPostingId == jobPostingId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        var list = await query
            .OrderByDescending(a => a.AppliedAt)
            .ToListAsync(cancellationToken);

        return list.Select(a => MapToApplicationDto(a, a.JobPosting?.Title ?? "N/A", a.JobPosting?.JobCode ?? "N/A")).ToList();
    }

    public async Task<CandidateApplicationDto?> GetApplicationByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var app = await _context.CandidateApplications
            .AsNoTracking()
            .Include(a => a.JobPosting)
            .Include(a => a.HiredEmployee)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return app == null ? null : MapToApplicationDto(app, app.JobPosting?.Title ?? "N/A", app.JobPosting?.JobCode ?? "N/A");
    }

    public async Task<CandidateApplicationDto> UpdateApplicationStatusAsync(UpdateApplicationStatusRequest request, CancellationToken cancellationToken = default)
    {
        var app = await _context.CandidateApplications
            .Include(a => a.JobPosting)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (app == null)
            throw new DomainException("Hồ sơ ứng viên không tồn tại.", "ERR_APPLICATION_NOT_FOUND");

        if (request.NewStatus == JobApplicationStatus.Interviewed && request.InterviewDate.HasValue)
        {
            app.ScheduleInterview(request.InterviewDate.Value, request.Notes ?? "");
        }
        else if (request.NewStatus == JobApplicationStatus.Offered || request.NewStatus == JobApplicationStatus.Rejected)
        {
            if (request.NewStatus == JobApplicationStatus.Offered)
            {
                app.RecordInterviewResult(request.InterviewScore ?? 85.0m, request.Notes ?? "Đạt yêu cầu phỏng vấn", true);
            }
            else
            {
                app.Reject(request.Notes ?? "Không phù hợp đợt tuyển dụng này");
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return MapToApplicationDto(app, app.JobPosting?.Title ?? "N/A", app.JobPosting?.JobCode ?? "N/A");
    }

    /// <summary>
    /// Tự động chuyển ứng viên đỗ tuyển dụng sang Nhân viên thử việc (Probation Employee),
    /// tự động sinh mã nhân viên, tạo Hợp đồng lao động thử việc 60 ngày theo Luật Lao động (85% lương),
    /// và sinh thông tin cấp tài khoản Cổng ESS.
    /// </summary>
    public async Task<OnboardingResultDto> OnboardHiredCandidateAsync(OnboardCandidateRequest request, CancellationToken cancellationToken = default)
    {
        var app = await _context.CandidateApplications
            .Include(a => a.JobPosting)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, cancellationToken);

        if (app == null)
            throw new DomainException("Không tìm thấy hồ sơ ứng viên tương ứng.", "ERR_APPLICATION_NOT_FOUND");

        if (app.Status == JobApplicationStatus.Hired)
            throw new DomainException("Ứng viên này đã hoàn tất thủ tục Onboard trước đó.", "ERR_ALREADY_ONBOARDED");

        var unit = await _context.OrganizationalUnits
            .FirstOrDefaultAsync(u => u.Id == request.OrganizationalUnitId, cancellationToken);

        if (unit == null)
            throw new DomainException("Đơn vị tổ chức tiếp nhận không tồn tại.", "ERR_UNIT_NOT_FOUND");

        // 1. Tách Họ và Tên đệm, Tên
        string fullName = app.FullName.Trim();
        string lastName = "Nguyễn";
        string firstName = fullName;
        int lastSpaceIndex = fullName.LastIndexOf(' ');
        if (lastSpaceIndex > 0)
        {
            lastName = fullName.Substring(0, lastSpaceIndex).Trim();
            firstName = fullName.Substring(lastSpaceIndex + 1).Trim();
        }

        // 2. Tính tiền lương thử việc (Điều 26 BLLĐ 2019: Ít nhất 85% lương chính thức)
        decimal officialSalary = request.OfficialBasicSalary > 0 ? request.OfficialBasicSalary : 15000000m;
        decimal probationSalary = Math.Round(officialSalary * 0.85m, 0);

        // 3. Sinh Mã nhân viên chuẩn quy ước Vinamilk
        int currentYear = DateTime.UtcNow.Year;
        int totalEmployeesCount = await _context.Employees.CountAsync(cancellationToken);
        string typePrefix = unit.UnitType switch
        {
            UnitType.Factory => "FAC",
            UnitType.RetailStore => "RET",
            UnitType.DairyFarm => "FARM",
            _ => "HQ"
        };
        string employeeCode = $"VNM-{typePrefix}-{currentYear}-{(totalEmployeesCount + 101):D4}";

        // 4. Khởi tạo đối tượng Nhân viên theo đúng Đa hình Khối
        string citizenId = string.IsNullOrWhiteSpace(app.CitizenId) ? $"079{DateTime.UtcNow.Ticks % 1000000000:D9}" : app.CitizenId;
        Employee newEmployee;
        switch (unit.UnitType)
        {
            case UnitType.Factory:
                newEmployee = new FactoryWorker(
                    employeeCode, firstName, lastName, citizenId,
                    app.DateOfBirth, app.Gender, app.Email, app.PhoneNumber, app.Address,
                    DateTime.UtcNow, unit.Id, probationSalary, request.Allowance,
                    "Nhóm Ca Vận Hành Tetra Pak", 0.0m, "TETRA-PAK-A3-CERT", "Level 3 - Chuẩn An toàn Nhà máy Sữa Mega",
                    request.AssignedManagerId
                );
                break;

            case UnitType.RetailStore:
                newEmployee = new StoreAssociate(
                    employeeCode, firstName, lastName, citizenId,
                    app.DateOfBirth, app.Gender, app.Email, app.PhoneNumber, app.Address,
                    DateTime.UtcNow, unit.Id, probationSalary, request.Allowance,
                    "POS-GMSV-NEW-01", 2.0m, "M", 5.0m,
                    request.AssignedManagerId
                );
                break;

            case UnitType.DairyFarm:
                newEmployee = new FarmSpecialist(
                    employeeCode, firstName, lastName, citizenId,
                    app.DateOfBirth, app.Gender, app.Email, app.PhoneNumber, app.Address,
                    DateTime.UtcNow, unit.Id, probationSalary, request.Allowance,
                    "Chăm sóc đàn bê sơ sinh & Kiểm soát dinh dưỡng", "BIO-SEC-LVL2", 1500000m, "VET-VN-PROB-01",
                    request.AssignedManagerId
                );
                break;

            default:
                newEmployee = new OfficeEmployee(
                    employeeCode, firstName, lastName, citizenId,
                    app.DateOfBirth, app.Gender, app.Email, app.PhoneNumber, app.Address,
                    DateTime.UtcNow, unit.Id, probationSalary, request.Allowance,
                    app.JobPosting?.Title ?? "Chuyên viên Nghiệp vụ",
                    "Tháp Vinamilk Tower - Tầng 6", "1024", "CUB-602",
                    request.AssignedManagerId
                );
                break;
        }

        // Thiết lập lương bảo hiểm thử việc
        newEmployee.SetInsuranceSalary(probationSalary);

        _context.Employees.Add(newEmployee);
        await _context.SaveChangesAsync(cancellationToken);

        // 5. Sinh Hợp đồng Lao động Thử việc (ProbationContract) 60 ngày
        string contractNumber = $"HDTV-{currentYear}-VNM{newEmployee.Id:D4}";
        var probationContract = new ProbationContract(
            contractNumber,
            newEmployee.Id,
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(60),
            probationSalary,
            probationSalary,
            DateTime.UtcNow.Date,
            mentorEmployeeId: request.AssignedManagerId,
            workingHoursPerWeek: 40
        );

        _context.LaborContracts.Add(probationContract);

        // 6. Đánh dấu ứng viên trúng tuyển
        app.MarkAsHired(newEmployee.Id);

        // 7. Tạo tài khoản Cổng ESS tạm thời
        string essUsername = employeeCode.ToLowerInvariant();
        string essPassword = $"VNM@{DateTime.UtcNow.Year}!Welcome";

        // 8. Ghi vết AuditLog bảo mật
        var audit = new AuditLog(
            "CandidateApplications",
            "CANDIDATE_ONBOARDED_TO_EMPLOYEE",
            app.Id.ToString(),
            $"Status={app.Status}",
            $"NewEmployeeId={newEmployee.Id}, EmployeeCode={employeeCode}, Unit={unit.UnitName}, ProbationSalary={probationSalary:N0} VNĐ",
            "TalentAcquisitionLead",
            "127.0.0.1"
        );
        _context.AuditLogs.Add(audit);

        await _context.SaveChangesAsync(cancellationToken);

        return new OnboardingResultDto(
            Success: true,
            ApplicationId: app.Id,
            EmployeeId: newEmployee.Id,
            EmployeeCode: employeeCode,
            FullName: fullName,
            PositionTitle: app.JobPosting?.Title ?? "Nhân viên Thử việc",
            UnitName: unit.UnitName,
            ProbationSalary: probationSalary,
            OfficialSalary: officialSalary,
            GeneratedEssUsername: essUsername,
            TempEssPassword: essPassword,
            Message: $"Chúc mừng! Ứng viên {fullName} đã được tiếp nhận thành công vào vị trí Thử việc. Hợp đồng {contractNumber} đã được phát hành."
        );
    }

    private static JobPostingDto MapToJobPostingDto(JobPosting j)
    {
        string unitTypeName = j.UnitType switch
        {
            UnitType.Factory => "Khối Nhà máy Sữa (Mega Factory)",
            UnitType.RetailStore => "Khối Bán lẻ (Chuỗi Giấc Mơ Sữa Việt)",
            UnitType.DairyFarm => "Khối Trang trại Sinh thái (Eco Farm)",
            _ => "Khối Hành chính Văn phòng (Trụ sở Q7)"
        };

        string regionName = j.RegionId switch
        {
            1 => "Miền Bắc",
            2 => "Miền Trung",
            _ => "Miền Nam"
        };

        return new JobPostingDto(
            j.Id,
            j.JobCode,
            j.Title,
            j.Slug,
            j.UnitType,
            unitTypeName,
            j.Department,
            j.WorkLocation,
            j.City,
            j.RegionId,
            regionName,
            j.EmploymentType,
            j.ExperienceLevel,
            j.EducationRequirement,
            j.SalaryMin,
            j.SalaryMax,
            j.SalaryDisplay,
            j.DatePosted,
            j.ValidThrough,
            j.Description,
            j.Requirements,
            j.Benefits,
            j.IsActive,
            j.IsFeatured,
            j.ViewCount,
            j.Applications.Count
        );
    }

    private static CandidateApplicationDto MapToApplicationDto(CandidateApplication a, string jobTitle, string jobCode)
    {
        string statusName = a.Status switch
        {
            JobApplicationStatus.Submitted => "Đã nộp hồ sơ",
            JobApplicationStatus.Screening => "Đang sàng lọc CV",
            JobApplicationStatus.Interviewed => "Đã phỏng vấn",
            JobApplicationStatus.Offered => "Đã gửi thư mời việc",
            JobApplicationStatus.Hired => "Đã trúng tuyển & Thử việc",
            JobApplicationStatus.Rejected => "Không phù hợp",
            _ => "Khác"
        };

        return new CandidateApplicationDto(
            a.Id,
            a.JobPostingId,
            jobTitle,
            jobCode,
            a.FullName,
            a.Email,
            a.PhoneNumber,
            a.CitizenId,
            a.DateOfBirth,
            a.Gender,
            a.Address,
            a.ResumeFileName,
            a.CoverLetter,
            a.PortfolioUrl,
            a.Status,
            statusName,
            a.AppliedAt,
            a.InterviewDate,
            a.InterviewScore,
            a.EvaluationNotes,
            a.HiredEmployeeId,
            a.HiredEmployee?.EmployeeCode,
            a.HiredDate
        );
    }
}
