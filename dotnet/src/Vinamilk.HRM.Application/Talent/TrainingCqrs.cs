using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Workflows;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Talent;

namespace Vinamilk.HRM.Application.Talent;

// ============================================================================
// 1. DTOs CHO ĐÀO TẠO (KHÓA HỌC, KẾ HOẠCH NĂM, ĐIỂM DANH, CHỨNG CHỈ & CHI PHÍ)
// ============================================================================

public record TrainingCourseDto(
    int Id,
    string CourseCode,
    string CourseName,
    string CourseCategory,
    int DurationHours,
    decimal StandardCostPerPerson,
    int CommitmentMonths,
    string? Description,
    bool IsActive,
    int ClassesCount);

public record TrainingEnrollmentDto(
    int Id,
    int TrainingClassId,
    string ClassCode,
    string ClassName,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string UnitName,
    DateOnly EnrolledDate,
    decimal AttendanceRatePercent,
    decimal FinalScore,
    string Result,
    string? CertificateNumber,
    int? FeedbackRating,
    string? FeedbackComment);

public record TrainingClassDto(
    int Id,
    string ClassCode,
    int TrainingCourseId,
    string CourseName,
    string CourseCategory,
    string ClassName,
    DateOnly StartDate,
    DateOnly EndDate,
    string? InstructorName,
    string? Location,
    int MaxCapacity,
    int EnrolledCount,
    decimal TotalActualCost,
    string Status,
    IReadOnlyList<TrainingEnrollmentDto> Enrollments);

public record TrainingOverviewDto(
    int Year,
    IReadOnlyList<TrainingCourseDto> Courses,
    IReadOnlyList<TrainingClassDto> Classes,
    IReadOnlyList<TrainingEnrollmentDto> AllEnrollments,
    IReadOnlyList<SimpleLookupDto> ActiveEmployees,
    int TotalCourses,
    int TotalClassesInYear,
    int TotalPassedCertificates,
    decimal TotalTrainingCostInYear);

// ============================================================================
// 2. AUTO-SEED DỮ LIỆU ĐÀO TẠO VINAMILK ACADEMY
// ============================================================================

public static class TrainingAcademySeeder
{
    public static async Task EnsureSeededAsync(IApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.TrainingCourses.AnyAsync(cancellationToken))
            return;

        var courses = new List<TrainingCourse>
        {
            new() { CourseCode = "TRN-FSSC22K", CourseName = "Tiêu chuẩn An toàn Thực phẩm FSSC 22000 & HACCP", CourseCategory = "Safety", DurationHours = 24, StandardCostPerPerson = 4500000m, CommitmentMonths = 12, Description = "Đào tạo bắt buộc cho khối Nhà máy & Quản lý chất lượng (QLCL)." },
            new() { CourseCode = "TRN-DOTNET8", CourseName = "Kiến trúc Enterprise .NET 8, CQRS & Cloud Native", CourseCategory = "Technical", DurationHours = 32, StandardCostPerPerson = 6800000m, CommitmentMonths = 18, Description = "Chương trình nâng cao năng lực đội ngũ CNTT & Chuyển đổi số." },
            new() { CourseCode = "TRN-LEAD26", CourseName = "Năng lực Lãnh đạo & Quản trị Hiệu suất OKR/KPI", CourseCategory = "Leadership", DurationHours = 16, StandardCostPerPerson = 5500000m, CommitmentMonths = 12, Description = "Dành cho cấp Trưởng phòng / Quản đốc / Giám đốc." },
            new() { CourseCode = "TRN-LEAN6S", CourseName = "Quản trị Sản xuất Tinh gọn Lean Six Sigma Green Belt", CourseCategory = "Technical", DurationHours = 40, StandardCostPerPerson = 9200000m, CommitmentMonths = 24, Description = "Tối ưu OEE dây chuyền đóng gói tại các Mega Factory." }
        };
        await context.TrainingCourses.AddRangeAsync(courses, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var cls1 = new TrainingClass
        {
            ClassCode = "CLS-2026-FSSC01",
            TrainingCourseId = courses[0].Id,
            ClassName = "Lớp FSSC 22000 Khóa K1/2026 — Mega Factory Bình Dương",
            StartDate = new DateOnly(2026, 3, 10),
            EndDate = new DateOnly(2026, 3, 12),
            InstructorName = "TS. Nguyễn Hoàng Nam (Chuyên gia SGS)",
            Location = "Hội trường A — Nhà máy Sữa Việt Nam",
            MaxCapacity = 30,
            TotalActualCost = 45000000m,
            Status = "Completed"
        };

        var cls2 = new TrainingClass
        {
            ClassCode = "CLS-2026-LEAD02",
            TrainingCourseId = courses[2].Id,
            ClassName = "Lớp Quản trị Mục tiêu OKR/KPI Dành cho Cấp Quản lý",
            StartDate = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 5),
            EndDate = new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 7),
            InstructorName = "PGS.TS Trần Minh Tuấn",
            Location = "Vinamilk Academy — Tầng 15 Vinamilk Tower",
            MaxCapacity = 25,
            TotalActualCost = 38500000m,
            Status = "InProgress"
        };

        await context.TrainingClasses.AddRangeAsync(new[] { cls1, cls2 }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var emps = await context.Employees.AsNoTracking().OrderBy(e => e.EmployeeCode).Take(4).ToListAsync(cancellationToken);
        if (emps.Count >= 3)
        {
            await context.TrainingEnrollments.AddRangeAsync(new List<TrainingEnrollment>
            {
                new() { TrainingClassId = cls1.Id, EmployeeId = emps[0].Id, EnrolledDate = new DateOnly(2026, 3, 1), AttendanceRatePercent = 100m, FinalScore = 9.2m, Result = "Passed", CertificateNumber = "VNM-CERT-FSSC-2026-001", FeedbackRating = 5, FeedbackComment = "Nội dung thực tế, áp dụng ngay vào kiểm soát dây chuyền." },
                new() { TrainingClassId = cls1.Id, EmployeeId = emps[1].Id, EnrolledDate = new DateOnly(2026, 3, 1), AttendanceRatePercent = 95m, FinalScore = 8.6m, Result = "Passed", CertificateNumber = "VNM-CERT-FSSC-2026-002", FeedbackRating = 5, FeedbackComment = "Giảng viên truyền đạt dễ hiểu." },
                new() { TrainingClassId = cls2.Id, EmployeeId = emps[2].Id, EnrolledDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)), AttendanceRatePercent = 100m, FinalScore = 0m, Result = "Enrolled" }
            }, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}

// ============================================================================
// 3. QUERIES & COMMANDS
// ============================================================================

public record GetTrainingOverviewQuery(int? Year = null) : IRequest<TrainingOverviewDto>;

public class GetTrainingOverviewQueryHandler : IRequestHandler<GetTrainingOverviewQuery, TrainingOverviewDto>
{
    private readonly IApplicationDbContext _context;

    public GetTrainingOverviewQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TrainingOverviewDto> Handle(GetTrainingOverviewQuery request, CancellationToken cancellationToken)
    {
        await TrainingAcademySeeder.EnsureSeededAsync(_context, cancellationToken);
        int year = request.Year ?? DateTime.Today.Year;

        var activeEmployees = await _context.Employees.AsNoTracking()
            .Where(e => e.WorkStatus == "Official" || e.WorkStatus == "Probation")
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new SimpleLookupDto(e.Id, e.EmployeeCode, $"{e.EmployeeCode} - {e.FullName}"))
            .ToListAsync(cancellationToken);

        var courses = await _context.TrainingCourses.AsNoTracking()
            .Include(c => c.Classes)
            .OrderBy(c => c.CourseCode)
            .Select(c => new TrainingCourseDto(
                c.Id,
                c.CourseCode,
                c.CourseName,
                c.CourseCategory,
                c.DurationHours,
                c.StandardCostPerPerson,
                c.CommitmentMonths,
                c.Description,
                c.IsActive,
                c.Classes.Count))
            .ToListAsync(cancellationToken);

        var classesRaw = await _context.TrainingClasses.AsNoTracking()
            .Include(c => c.TrainingCourse)
            .Include(c => c.Enrollments).ThenInclude(e => e.Employee).ThenInclude(emp => emp.OrganizationUnit)
            .OrderByDescending(c => c.StartDate)
            .ToListAsync(cancellationToken);

        var classes = classesRaw.Select(c => new TrainingClassDto(
            c.Id,
            c.ClassCode,
            c.TrainingCourseId,
            c.TrainingCourse.CourseName,
            c.TrainingCourse.CourseCategory,
            c.ClassName,
            c.StartDate,
            c.EndDate,
            c.InstructorName,
            c.Location,
            c.MaxCapacity,
            c.Enrollments.Count,
            c.TotalActualCost,
            c.Status,
            c.Enrollments.Select(e => new TrainingEnrollmentDto(
                e.Id,
                c.Id,
                c.ClassCode,
                c.ClassName,
                e.EmployeeId,
                e.Employee.EmployeeCode,
                e.Employee.FullName,
                e.Employee.OrganizationUnit.UnitName,
                e.EnrolledDate,
                e.AttendanceRatePercent,
                e.FinalScore,
                e.Result,
                e.CertificateNumber,
                e.FeedbackRating,
                e.FeedbackComment)).ToList()
        )).ToList();

        var allEnrollments = classes.SelectMany(c => c.Enrollments).ToList();

        return new TrainingOverviewDto(
            year,
            courses,
            classes,
            allEnrollments,
            activeEmployees,
            courses.Count,
            classes.Count(c => c.StartDate.Year == year),
            allEnrollments.Count(e => e.Result == "Passed" && !string.IsNullOrEmpty(e.CertificateNumber)),
            classes.Where(c => c.StartDate.Year == year).Sum(c => c.TotalActualCost));
    }
}

public record CreateTrainingCourseCommand(
    string CourseCode,
    string CourseName,
    string CourseCategory,
    int DurationHours,
    decimal StandardCostPerPerson,
    int CommitmentMonths,
    string? Description) : IRequest<int>;

public class CreateTrainingCourseCommandHandler : IRequestHandler<CreateTrainingCourseCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateTrainingCourseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateTrainingCourseCommand request, CancellationToken cancellationToken)
    {
        var course = new TrainingCourse
        {
            CourseCode = request.CourseCode.Trim().ToUpperInvariant(),
            CourseName = request.CourseName.Trim(),
            CourseCategory = request.CourseCategory,
            DurationHours = Math.Max(1, request.DurationHours),
            StandardCostPerPerson = Math.Max(0m, request.StandardCostPerPerson),
            CommitmentMonths = Math.Max(0, request.CommitmentMonths),
            Description = request.Description?.Trim(),
            IsActive = true
        };
        await _context.TrainingCourses.AddAsync(course, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return course.Id;
    }
}

public record CreateTrainingClassCommand(
    int TrainingCourseId,
    string ClassName,
    DateOnly StartDate,
    DateOnly EndDate,
    string InstructorName,
    string Location,
    int MaxCapacity,
    decimal TotalActualCost) : IRequest<int>;

public class CreateTrainingClassCommandHandler : IRequestHandler<CreateTrainingClassCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateTrainingClassCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateTrainingClassCommand request, CancellationToken cancellationToken)
    {
        var count = await _context.TrainingClasses.CountAsync(cancellationToken) + 1;
        var cls = new TrainingClass
        {
            ClassCode = $"CLS-{request.StartDate.Year}-{count:D3}",
            TrainingCourseId = request.TrainingCourseId,
            ClassName = request.ClassName.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            InstructorName = request.InstructorName.Trim(),
            Location = request.Location.Trim(),
            MaxCapacity = Math.Max(1, request.MaxCapacity),
            TotalActualCost = Math.Max(0m, request.TotalActualCost),
            Status = "Planned"
        };
        await _context.TrainingClasses.AddAsync(cls, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return cls.Id;
    }
}

public record EnrollEmployeeToClassCommand(int TrainingClassId, int EmployeeId) : IRequest<int>;

public class EnrollEmployeeToClassCommandHandler : IRequestHandler<EnrollEmployeeToClassCommand, int>
{
    private readonly IApplicationDbContext _context;

    public EnrollEmployeeToClassCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(EnrollEmployeeToClassCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.TrainingEnrollments
            .AnyAsync(e => e.TrainingClassId == request.TrainingClassId && e.EmployeeId == request.EmployeeId, cancellationToken);

        if (exists)
            throw new InvalidOperationException("Nhân viên này đã được ghi danh vào lớp học này.");

        var enrollment = new TrainingEnrollment
        {
            TrainingClassId = request.TrainingClassId,
            EmployeeId = request.EmployeeId,
            EnrolledDate = DateOnly.FromDateTime(DateTime.Today),
            AttendanceRatePercent = 100m,
            FinalScore = 0m,
            Result = "Enrolled"
        };

        await _context.TrainingEnrollments.AddAsync(enrollment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return enrollment.Id;
    }
}

public record EvaluateTrainingEnrollmentCommand(
    int EnrollmentId,
    decimal AttendanceRatePercent,
    decimal FinalScore,
    int? FeedbackRating,
    string? FeedbackComment) : IRequest<string>;

public class EvaluateTrainingEnrollmentCommandHandler : IRequestHandler<EvaluateTrainingEnrollmentCommand, string>
{
    private readonly IApplicationDbContext _context;

    public EvaluateTrainingEnrollmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> Handle(EvaluateTrainingEnrollmentCommand request, CancellationToken cancellationToken)
    {
        var enr = await _context.TrainingEnrollments
            .Include(e => e.TrainingClass).ThenInclude(c => c.TrainingCourse)
            .FirstOrDefaultAsync(e => e.Id == request.EnrollmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ học viên.");

        enr.AttendanceRatePercent = Math.Clamp(request.AttendanceRatePercent, 0m, 100m);
        enr.FinalScore = Math.Clamp(request.FinalScore, 0m, 10m);
        enr.FeedbackRating = request.FeedbackRating;
        enr.FeedbackComment = request.FeedbackComment?.Trim();

        bool isPassed = enr.AttendanceRatePercent >= 80m && enr.FinalScore >= 5.0m;
        enr.Result = isPassed ? "Passed" : "Failed";

        if (isPassed && string.IsNullOrEmpty(enr.CertificateNumber))
        {
            enr.CertificateNumber = $"VNM-CERT-{enr.TrainingClass.TrainingCourse.CourseCode}-{enr.Id:D4}";

            // Tự động đồng bộ chứng chỉ vào Hồ sơ Nhân viên 360°
            await _context.EmployeeCertificates.AddAsync(new EmployeeCertificate
            {
                EmployeeId = enr.EmployeeId,
                CertificateType = "Professional",
                CertificateName = enr.TrainingClass.TrainingCourse.CourseName,
                IssuedBy = "Vinamilk Academy",
                IssueDate = DateOnly.FromDateTime(DateTime.Today),
                ScoreOrGrade = $"{enr.FinalScore:0.0}/10 (Chuyên cần {enr.AttendanceRatePercent:0}%)"
            }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return isPassed
            ? $"Học viên ĐẠT ({enr.FinalScore:0.0}/10). Đã cấp chứng chỉ {enr.CertificateNumber} vào Hồ sơ 360°."
            : $"Đã lưu kết quả KHÔNG ĐẠT ({enr.FinalScore:0.0}/10, chuyên cần {enr.AttendanceRatePercent:0}%).";
    }
}
