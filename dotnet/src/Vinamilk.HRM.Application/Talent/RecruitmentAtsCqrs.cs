using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Workflows;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Talent;

namespace Vinamilk.HRM.Application.Talent;

// ============================================================================
// 1. DTOs CHO TUYỂN DỤNG (ATS) & TRANG CAREERS CÔNG KHAI CHUẨN SEO
// ============================================================================

public record RecruitmentRequestItemDto(
    int Id,
    string RequestCode,
    int OrganizationUnitId,
    string UnitName,
    int PositionId,
    string PositionName,
    int Quantity,
    int HiredCount,
    string EmploymentType,
    string ReasonType,
    decimal BudgetMinSalary,
    decimal BudgetMaxSalary,
    DateOnly? ExpectedOnboardDate,
    string? JobRequirements,
    string Status,
    DateTime CreatedAt);

public record JobPostingItemDto(
    int Id,
    string PostingCode,
    int? RecruitmentRequestId,
    string Title,
    int OrganizationUnitId,
    string UnitName,
    int PositionId,
    string PositionName,
    int Vacancies,
    string? SalaryRangeText,
    string? WorkLocation,
    DateOnly PublishDate,
    DateOnly ClosingDate,
    string? JobDescription,
    string? JobRequirements,
    string? Benefits,
    string Status,
    int CandidateCount);

public record InterviewSummaryDto(
    int Id,
    int RoundNumber,
    string RoundTitle,
    DateTime ScheduledStartTime,
    DateTime ScheduledEndTime,
    string InterviewMode,
    string? LocationOrLink,
    string? InterviewerName,
    decimal TechnicalScore,
    decimal CultureFitScore,
    decimal TotalScore,
    string? EvaluationNotes,
    string Result);

public record JobOfferSummaryDto(
    int Id,
    string OfferCode,
    int CandidateId,
    string CandidateName,
    string CandidateEmail,
    string ProposedPositionName,
    string ProposedUnitName,
    decimal OfferedProbationSalary,
    decimal OfferedOfficialSalary,
    DateOnly ExpectedStartDate,
    DateOnly OfferExpiryDate,
    string Status,
    string? CandidateFeedback);

public record CandidateCardDto(
    int Id,
    string CandidateCode,
    int JobPostingId,
    string JobTitle,
    string UnitName,
    string PositionName,
    string FullName,
    DateOnly? DateOfBirth,
    string Gender,
    string PhoneNumber,
    string Email,
    string? Major,
    decimal YearsOfExperience,
    decimal ExpectedSalary,
    string SourceChannel,
    string? ResumeUrl,
    string Stage,
    decimal OverallScore,
    int? ConvertedEmployeeId,
    DateTime AppliedAt,
    IReadOnlyList<InterviewSummaryDto> Interviews,
    JobOfferSummaryDto? LatestOffer);

public record AtsOverviewDto(
    IReadOnlyList<RecruitmentRequestItemDto> Requests,
    IReadOnlyList<JobPostingItemDto> JobPostings,
    IReadOnlyList<CandidateCardDto> Candidates,
    IReadOnlyList<JobOfferSummaryDto> Offers,
    IReadOnlyList<SimpleLookupDto> Units,
    IReadOnlyList<SimpleLookupDto> Positions,
    IReadOnlyList<SimpleLookupDto> ActiveEmployees,
    int OpenJobsCount,
    int TotalCandidatesCount,
    int InterviewingCount,
    int HiredCount);

public record PublicCareersPortalDto(
    string? Keyword,
    int? UnitId,
    IReadOnlyList<JobPostingItemDto> OpenPostings,
    IReadOnlyList<SimpleLookupDto> Units);

// ============================================================================
// 2. AUTO-SEED DỮ LIỆU MẪU TUYỂN DỤNG ATS (VINAMILK CAREERS)
// ============================================================================

public static class TalentAtsSeeder
{
    public static async Task EnsureSampleAtsDataAsync(
        IApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        if (await context.JobPostings.AnyAsync(cancellationToken))
            return;

        var units = await context.OrganizationUnits.AsNoTracking().ToListAsync(cancellationToken);
        var positions = await context.Positions.AsNoTracking().ToListAsync(cancellationToken);
        var itUnit = units.FirstOrDefault(u => u.UnitCode == "PB_IT") ?? units.First();
        var rdUnit = units.FirstOrDefault(u => u.UnitCode == "PB_RD") ?? units.First();
        var bdFactory = units.FirstOrDefault(u => u.UnitCode == "NM_BD") ?? units.First();

        var cvnPos = positions.FirstOrDefault(p => p.PositionCode == "CVN") ?? positions.First();
        var cvPos = positions.FirstOrDefault(p => p.PositionCode == "CV") ?? positions.First();
        var qlPos = positions.FirstOrDefault(p => p.PositionCode == "QL") ?? positions.First();

        var req1 = new RecruitmentRequest
        {
            RequestCode = "TD-2026-001",
            OrganizationUnitId = itUnit.Id,
            PositionId = cvnPos.Id,
            Quantity = 2,
            HiredCount = 0,
            EmploymentType = "FullTime",
            ReasonType = "NewHeadcount",
            BudgetMinSalary = 30000000m,
            BudgetMaxSalary = 48000000m,
            ExpectedOnboardDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            JobRequirements = "Thành thạo .NET 8, Clean Architecture, SQL Server, Azure/Docker, tối thiểu 4 năm kinh nghiệm.",
            Status = "Approved"
        };

        var req2 = new RecruitmentRequest
        {
            RequestCode = "TD-2026-002",
            OrganizationUnitId = rdUnit.Id,
            PositionId = cvPos.Id,
            Quantity = 3,
            HiredCount = 0,
            EmploymentType = "FullTime",
            ReasonType = "Expansion",
            BudgetMinSalary = 18000000m,
            BudgetMaxSalary = 28000000m,
            ExpectedOnboardDate = DateOnly.FromDateTime(DateTime.Today.AddDays(45)),
            JobRequirements = "Tốt nghiệp ĐH Công nghệ Thực phẩm / Hóa sinh, am hiểu tiêu chuẩn FSSC 22000 & HACCP.",
            Status = "Approved"
        };

        var req3 = new RecruitmentRequest
        {
            RequestCode = "TD-2026-003",
            OrganizationUnitId = bdFactory.Id,
            PositionId = qlPos.Id,
            Quantity = 1,
            HiredCount = 0,
            EmploymentType = "FullTime",
            ReasonType = "Replacement",
            BudgetMinSalary = 25000000m,
            BudgetMaxSalary = 38000000m,
            ExpectedOnboardDate = DateOnly.FromDateTime(DateTime.Today.AddDays(20)),
            JobRequirements = "Kinh nghiệm quản lý dây chuyền đóng gói vô trùng Tetra Pak tối thiểu 5 năm.",
            Status = "Pending"
        };

        await context.RecruitmentRequests.AddRangeAsync(new[] { req1, req2, req3 }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var post1 = new JobPosting
        {
            PostingCode = "VNM-JOB-2026-01",
            RecruitmentRequestId = req1.Id,
            Title = "Senior .NET Solution Architect (Hệ thống ERP / HRM)",
            OrganizationUnitId = itUnit.Id,
            PositionId = cvnPos.Id,
            Vacancies = 2,
            SalaryRangeText = "32.000.000 - 48.000.000 VNĐ + Thưởng KPI 4 tháng",
            WorkLocation = "Vinamilk Tower — Số 10 Tân Trào, Q.7, TP.HCM",
            PublishDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-10)),
            ClosingDate = DateOnly.FromDateTime(DateTime.Today.AddDays(35)),
            JobDescription = "Thiết kế kiến trúc, phát triển và tối ưu các hệ thống quản trị doanh nghiệp lõi trên nền tảng ASP.NET Core 8, SQL Server, SignalR và Cloud.",
            JobRequirements = "Tốt nghiệp ĐH CNTT; trên 4 năm kinh nghiệm C#/.NET Core, EF Core, CQRS/MediatR, Clean Architecture.",
            Benefits = "Bảo hiểm sức khỏe cao cấp Vinamilk Care, thưởng tháng 13 + KPI, du lịch hàng năm, môi trường Top 1 Nơi làm việc tốt nhất VN.",
            Status = "Open"
        };

        var post2 = new JobPosting
        {
            PostingCode = "VNM-JOB-2026-02",
            RecruitmentRequestId = req2.Id,
            Title = "Chuyên viên Nghiên cứu & Phát triển Sản phẩm Sữa (R&D)",
            OrganizationUnitId = rdUnit.Id,
            PositionId = cvPos.Id,
            Vacancies = 3,
            SalaryRangeText = "20.000.000 - 28.000.000 VNĐ",
            WorkLocation = "Trung tâm R&D Vinamilk — Quận 7, TP.HCM",
            PublishDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-7)),
            ClosingDate = DateOnly.FromDateTime(DateTime.Today.AddDays(40)),
            JobDescription = "Nghiên cứu công thức sản phẩm sữa tươi Organic, sữa hạt và thực phẩm dinh dưỡng thế hệ mới.",
            JobRequirements = "Tốt nghiệp Công nghệ Thực phẩm, có kiến thức về cảm quan, vi sinh và thử nghiệm Pilot.",
            Benefits = "Chế độ phúc lợi toàn diện, đào tạo chuyên sâu cùng chuyên gia quốc tế, phụ cấp ăn trưa & đi lại.",
            Status = "Open"
        };

        await context.JobPostings.AddRangeAsync(new[] { post1, post2 }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var candidates = new List<Candidate>
        {
            new()
            {
                CandidateCode = "UV-2026-001",
                JobPostingId = post1.Id,
                FullName = "Nguyễn Minh Quân",
                DateOfBirth = new DateOnly(1994, 8, 12),
                Gender = "Nam",
                PhoneNumber = "0908112233",
                Email = "quan.nm.dev@gmail.com",
                Major = "Kỹ thuật Phần mềm",
                YearsOfExperience = 6.5m,
                ExpectedSalary = 42000000m,
                SourceChannel = "CareersPortal",
                Stage = "Offered",
                OverallScore = 92.5m,
                AppliedAt = DateTime.UtcNow.AddDays(-8)
            },
            new()
            {
                CandidateCode = "UV-2026-002",
                JobPostingId = post1.Id,
                FullName = "Lê Hoàng Anh",
                DateOfBirth = new DateOnly(1996, 3, 22),
                Gender = "Nam",
                PhoneNumber = "0912334455",
                Email = "hoanganh.le@outlook.com",
                Major = "Khoa học Máy tính",
                YearsOfExperience = 4.5m,
                ExpectedSalary = 35000000m,
                SourceChannel = "LinkedIn",
                Stage = "Interview1",
                OverallScore = 84.0m,
                AppliedAt = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                CandidateCode = "UV-2026-003",
                JobPostingId = post2.Id,
                FullName = "Trần Ngọc Bảo Châu",
                DateOfBirth = new DateOnly(1998, 11, 5),
                Gender = "Nữ",
                PhoneNumber = "0933445566",
                Email = "baochau.foodtech@gmail.com",
                Major = "Công nghệ Thực phẩm",
                YearsOfExperience = 3.0m,
                ExpectedSalary = 23000000m,
                SourceChannel = "CareersPortal",
                Stage = "Screening",
                OverallScore = 80.0m,
                AppliedAt = DateTime.UtcNow.AddDays(-3)
            },
            new()
            {
                CandidateCode = "UV-2026-004",
                JobPostingId = post2.Id,
                FullName = "Phạm Quốc Đạt",
                DateOfBirth = new DateOnly(1999, 1, 18),
                Gender = "Nam",
                PhoneNumber = "0977889900",
                Email = "quocdat.pham@gmail.com",
                Major = "Công nghệ Sinh học",
                YearsOfExperience = 2.0m,
                ExpectedSalary = 21000000m,
                SourceChannel = "CareersPortal",
                Stage = "Applied",
                OverallScore = 0m,
                AppliedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        await context.Candidates.AddRangeAsync(candidates, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await context.Interviews.AddAsync(new Interview
        {
            CandidateId = candidates[0].Id,
            RoundNumber = 1,
            RoundTitle = "Phỏng vấn Kiến trúc Hệ thống & Technical",
            ScheduledStartTime = DateTime.Today.AddDays(-4).AddHours(10),
            ScheduledEndTime = DateTime.Today.AddDays(-4).AddHours(11),
            InterviewMode = "InPerson",
            LocationOrLink = "Phòng họp 12A — Vinamilk Tower Q.7",
            TechnicalScore = 94m,
            CultureFitScore = 91m,
            TotalScore = 92.5m,
            EvaluationNotes = "Nắm vững Clean Architecture, CQRS, SQL Server Tuning, tư duy giải quyết vấn đề xuất sắc.",
            Result = "Passed"
        }, cancellationToken);

        await context.JobOffers.AddAsync(new JobOffer
        {
            OfferCode = "OFFER-2026-001",
            CandidateId = candidates[0].Id,
            ProposedPositionId = cvnPos.Id,
            ProposedUnitId = itUnit.Id,
            OfferedProbationSalary = 35700000m,
            OfferedOfficialSalary = 42000000m,
            ExpectedStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(14)),
            OfferExpiryDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            Status = "Accepted",
            CandidateFeedback = "Ứng viên đã xác nhận đồng ý thư mời nhận việc."
        }, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }
}

// ============================================================================
// 3. QUERIES: ATS OVERVIEW & PUBLIC CAREERS PORTAL
// ============================================================================

public record GetAtsOverviewQuery(int? JobPostingId = null) : IRequest<AtsOverviewDto>;

public class GetAtsOverviewQueryHandler : IRequestHandler<GetAtsOverviewQuery, AtsOverviewDto>
{
    private readonly IApplicationDbContext _context;

    public GetAtsOverviewQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AtsOverviewDto> Handle(GetAtsOverviewQuery request, CancellationToken cancellationToken)
    {
        await TalentAtsSeeder.EnsureSampleAtsDataAsync(_context, cancellationToken);

        var units = await _context.OrganizationUnits.AsNoTracking()
            .OrderBy(u => u.SortOrder)
            .Select(u => new SimpleLookupDto(u.Id, u.UnitCode, u.UnitName))
            .ToListAsync(cancellationToken);

        var positions = await _context.Positions.AsNoTracking()
            .OrderBy(p => p.PositionLevel)
            .Select(p => new SimpleLookupDto(p.Id, p.PositionCode, p.PositionName))
            .ToListAsync(cancellationToken);

        var activeEmployees = await _context.Employees.AsNoTracking()
            .Where(e => e.WorkStatus == "Official" || e.WorkStatus == "Probation")
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new SimpleLookupDto(e.Id, e.EmployeeCode, $"{e.EmployeeCode} - {e.FullName}"))
            .ToListAsync(cancellationToken);

        var empNameMap = activeEmployees.ToDictionary(e => e.Id, e => e.Name);

        var requests = await _context.RecruitmentRequests.AsNoTracking()
            .Include(r => r.OrganizationUnit)
            .Include(r => r.Position)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RecruitmentRequestItemDto(
                r.Id,
                r.RequestCode,
                r.OrganizationUnitId,
                r.OrganizationUnit.UnitName,
                r.PositionId,
                r.Position.PositionName,
                r.Quantity,
                r.HiredCount,
                r.EmploymentType,
                r.ReasonType,
                r.BudgetMinSalary,
                r.BudgetMaxSalary,
                r.ExpectedOnboardDate,
                r.JobRequirements,
                r.Status,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        var postings = await _context.JobPostings.AsNoTracking()
            .Include(j => j.OrganizationUnit)
            .Include(j => j.Position)
            .Include(j => j.Candidates)
            .OrderByDescending(j => j.PublishDate)
            .Select(j => new JobPostingItemDto(
                j.Id,
                j.PostingCode,
                j.RecruitmentRequestId,
                j.Title,
                j.OrganizationUnitId,
                j.OrganizationUnit.UnitName,
                j.PositionId,
                j.Position.PositionName,
                j.Vacancies,
                j.SalaryRangeText,
                j.WorkLocation,
                j.PublishDate,
                j.ClosingDate,
                j.JobDescription,
                j.JobRequirements,
                j.Benefits,
                j.Status,
                j.Candidates.Count))
            .ToListAsync(cancellationToken);

        var candQuery = _context.Candidates.AsNoTracking()
            .Include(c => c.JobPosting).ThenInclude(j => j.OrganizationUnit)
            .Include(c => c.JobPosting).ThenInclude(j => j.Position)
            .Include(c => c.Interviews)
            .Include(c => c.Offers).ThenInclude(o => o.ProposedPosition)
            .Include(c => c.Offers).ThenInclude(o => o.ProposedUnit)
            .AsQueryable();

        if (request.JobPostingId.HasValue && request.JobPostingId.Value > 0)
            candQuery = candQuery.Where(c => c.JobPostingId == request.JobPostingId.Value);

        var candidatesRaw = await candQuery.OrderByDescending(c => c.AppliedAt).ToListAsync(cancellationToken);

        var candidates = candidatesRaw.Select(c =>
        {
            var latestOffer = c.Offers.OrderByDescending(o => o.Id).FirstOrDefault();
            return new CandidateCardDto(
                c.Id,
                c.CandidateCode,
                c.JobPostingId,
                c.JobPosting.Title,
                c.JobPosting.OrganizationUnit.UnitName,
                c.JobPosting.Position.PositionName,
                c.FullName,
                c.DateOfBirth,
                c.Gender,
                c.PhoneNumber,
                c.Email,
                c.Major,
                c.YearsOfExperience,
                c.ExpectedSalary,
                c.SourceChannel,
                c.ResumeUrl,
                c.Stage,
                c.OverallScore,
                c.ConvertedEmployeeId,
                c.AppliedAt,
                c.Interviews.OrderBy(i => i.RoundNumber).Select(i => new InterviewSummaryDto(
                    i.Id,
                    i.RoundNumber,
                    i.RoundTitle,
                    i.ScheduledStartTime,
                    i.ScheduledEndTime,
                    i.InterviewMode,
                    i.LocationOrLink,
                    i.InterviewerEmployeeId.HasValue && empNameMap.TryGetValue(i.InterviewerEmployeeId.Value, out var ivn) ? ivn : "Hội đồng Tuyển dụng",
                    i.TechnicalScore,
                    i.CultureFitScore,
                    i.TotalScore,
                    i.EvaluationNotes,
                    i.Result)).ToList(),
                latestOffer != null ? new JobOfferSummaryDto(
                    latestOffer.Id,
                    latestOffer.OfferCode,
                    c.Id,
                    c.FullName,
                    c.Email,
                    latestOffer.ProposedPosition.PositionName,
                    latestOffer.ProposedUnit.UnitName,
                    latestOffer.OfferedProbationSalary,
                    latestOffer.OfferedOfficialSalary,
                    latestOffer.ExpectedStartDate,
                    latestOffer.OfferExpiryDate,
                    latestOffer.Status,
                    latestOffer.CandidateFeedback) : null);
        }).ToList();

        var offers = await _context.JobOffers.AsNoTracking()
            .Include(o => o.Candidate)
            .Include(o => o.ProposedPosition)
            .Include(o => o.ProposedUnit)
            .OrderByDescending(o => o.Id)
            .Select(o => new JobOfferSummaryDto(
                o.Id,
                o.OfferCode,
                o.CandidateId,
                o.Candidate.FullName,
                o.Candidate.Email,
                o.ProposedPosition.PositionName,
                o.ProposedUnit.UnitName,
                o.OfferedProbationSalary,
                o.OfferedOfficialSalary,
                o.ExpectedStartDate,
                o.OfferExpiryDate,
                o.Status,
                o.CandidateFeedback))
            .ToListAsync(cancellationToken);

        return new AtsOverviewDto(
            requests,
            postings,
            candidates,
            offers,
            units,
            positions,
            activeEmployees,
            postings.Count(p => p.Status == "Open"),
            candidates.Count,
            candidates.Count(c => c.Stage is "Interview1" or "Interview2"),
            candidates.Count(c => c.Stage == "Hired"));
    }
}

public record GetPublicCareersQuery(string? Keyword = null, int? UnitId = null) : IRequest<PublicCareersPortalDto>;

public class GetPublicCareersQueryHandler : IRequestHandler<GetPublicCareersQuery, PublicCareersPortalDto>
{
    private readonly IApplicationDbContext _context;

    public GetPublicCareersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PublicCareersPortalDto> Handle(GetPublicCareersQuery request, CancellationToken cancellationToken)
    {
        await TalentAtsSeeder.EnsureSampleAtsDataAsync(_context, cancellationToken);

        var query = _context.JobPostings.AsNoTracking()
            .Include(j => j.OrganizationUnit)
            .Include(j => j.Position)
            .Include(j => j.Candidates)
            .Where(j => j.Status == "Open");

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim().ToLower();
            query = query.Where(j => j.Title.ToLower().Contains(kw) || (j.JobDescription != null && j.JobDescription.ToLower().Contains(kw)));
        }

        if (request.UnitId.HasValue && request.UnitId.Value > 0)
            query = query.Where(j => j.OrganizationUnitId == request.UnitId.Value);

        var postings = await query.OrderByDescending(j => j.PublishDate)
            .Select(j => new JobPostingItemDto(
                j.Id,
                j.PostingCode,
                j.RecruitmentRequestId,
                j.Title,
                j.OrganizationUnitId,
                j.OrganizationUnit.UnitName,
                j.PositionId,
                j.Position.PositionName,
                j.Vacancies,
                j.SalaryRangeText,
                j.WorkLocation,
                j.PublishDate,
                j.ClosingDate,
                j.JobDescription,
                j.JobRequirements,
                j.Benefits,
                j.Status,
                j.Candidates.Count))
            .ToListAsync(cancellationToken);

        var units = await _context.OrganizationUnits.AsNoTracking()
            .OrderBy(u => u.SortOrder)
            .Select(u => new SimpleLookupDto(u.Id, u.UnitCode, u.UnitName))
            .ToListAsync(cancellationToken);

        return new PublicCareersPortalDto(request.Keyword, request.UnitId, postings, units);
    }
}

// ============================================================================
// 4. COMMANDS: YÊU CẦU TUYỂN DỤNG, TIN TUYỂN DỤNG, NỘP CV, KANBAN, PHỎNG VẤN, OFFER & HIRED
// ============================================================================

public record CreateRecruitmentRequestCommand(
    int OrganizationUnitId,
    int PositionId,
    int Quantity,
    string ReasonType,
    decimal BudgetMinSalary,
    decimal BudgetMaxSalary,
    DateOnly? ExpectedOnboardDate,
    string? JobRequirements) : IRequest<int>;

public class CreateRecruitmentRequestCommandHandler : IRequestHandler<CreateRecruitmentRequestCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateRecruitmentRequestCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateRecruitmentRequestCommand request, CancellationToken cancellationToken)
    {
        var count = await _context.RecruitmentRequests.CountAsync(cancellationToken) + 1;
        var entity = new RecruitmentRequest
        {
            RequestCode = $"TD-{DateTime.Today.Year}-{count:D3}",
            OrganizationUnitId = request.OrganizationUnitId,
            PositionId = request.PositionId,
            Quantity = Math.Max(1, request.Quantity),
            ReasonType = request.ReasonType,
            BudgetMinSalary = request.BudgetMinSalary,
            BudgetMaxSalary = request.BudgetMaxSalary,
            ExpectedOnboardDate = request.ExpectedOnboardDate,
            JobRequirements = request.JobRequirements?.Trim(),
            Status = "Pending"
        };

        await _context.RecruitmentRequests.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public record ApproveRecruitmentRequestCommand(int RequestId, bool IsApproved) : IRequest<string>;

public class ApproveRecruitmentRequestCommandHandler : IRequestHandler<ApproveRecruitmentRequestCommand, string>
{
    private readonly IApplicationDbContext _context;

    public ApproveRecruitmentRequestCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> Handle(ApproveRecruitmentRequestCommand request, CancellationToken cancellationToken)
    {
        var req = await _context.RecruitmentRequests.FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu tuyển dụng.");

        req.Status = request.IsApproved ? "Approved" : "Rejected";
        await _context.SaveChangesAsync(cancellationToken);

        return request.IsApproved
            ? $"Đã PHÊ DUYỆT yêu cầu tuyển dụng {req.RequestCode}. Có thể đăng tin tuyển dụng lên trang Careers."
            : $"Đã từ chối yêu cầu tuyển dụng {req.RequestCode}.";
    }
}

public record CreateJobPostingCommand(
    int? RecruitmentRequestId,
    string Title,
    int OrganizationUnitId,
    int PositionId,
    int Vacancies,
    string SalaryRangeText,
    string WorkLocation,
    DateOnly ClosingDate,
    string JobDescription,
    string JobRequirements,
    string Benefits) : IRequest<int>;

public class CreateJobPostingCommandHandler : IRequestHandler<CreateJobPostingCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateJobPostingCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateJobPostingCommand request, CancellationToken cancellationToken)
    {
        var count = await _context.JobPostings.CountAsync(cancellationToken) + 1;
        var posting = new JobPosting
        {
            PostingCode = $"VNM-JOB-{DateTime.Today.Year}-{count:D2}",
            RecruitmentRequestId = request.RecruitmentRequestId,
            Title = request.Title.Trim(),
            OrganizationUnitId = request.OrganizationUnitId,
            PositionId = request.PositionId,
            Vacancies = Math.Max(1, request.Vacancies),
            SalaryRangeText = request.SalaryRangeText.Trim(),
            WorkLocation = request.WorkLocation.Trim(),
            PublishDate = DateOnly.FromDateTime(DateTime.Today),
            ClosingDate = request.ClosingDate,
            JobDescription = request.JobDescription.Trim(),
            JobRequirements = request.JobRequirements.Trim(),
            Benefits = request.Benefits.Trim(),
            Status = "Open"
        };

        await _context.JobPostings.AddAsync(posting, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return posting.Id;
    }
}

public record ApplyCandidateCommand(
    int JobPostingId,
    string FullName,
    DateOnly? DateOfBirth,
    string Gender,
    string PhoneNumber,
    string Email,
    string? Major,
    decimal YearsOfExperience,
    decimal ExpectedSalary,
    string SourceChannel = "CareersPortal",
    string? ResumeUrl = null) : IRequest<string>;

public class ApplyCandidateCommandValidator : AbstractValidator<ApplyCandidateCommand>
{
    public ApplyCandidateCommandValidator()
    {
        RuleFor(x => x.JobPostingId).GreaterThan(0);
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Vui lòng nhập họ và tên ứng viên.");
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Vui lòng nhập số điện thoại liên hệ.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Email ứng viên không hợp lệ.");
    }
}

public class ApplyCandidateCommandHandler : IRequestHandler<ApplyCandidateCommand, string>
{
    private readonly IApplicationDbContext _context;

    public ApplyCandidateCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> Handle(ApplyCandidateCommand request, CancellationToken cancellationToken)
    {
        var count = await _context.Candidates.CountAsync(cancellationToken) + 1;
        var code = $"UV-{DateTime.Today.Year}-{count:D3}";

        var candidate = new Candidate
        {
            CandidateCode = code,
            JobPostingId = request.JobPostingId,
            FullName = request.FullName.Trim(),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            PhoneNumber = request.PhoneNumber.Trim(),
            Email = request.Email.Trim(),
            Major = request.Major?.Trim(),
            YearsOfExperience = request.YearsOfExperience,
            ExpectedSalary = request.ExpectedSalary,
            SourceChannel = request.SourceChannel,
            ResumeUrl = request.ResumeUrl,
            Stage = "Applied",
            AppliedAt = DateTime.UtcNow
        };

        await _context.Candidates.AddAsync(candidate, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return code;
    }
}

public record MoveCandidateStageCommand(int CandidateId, string NewStage) : IRequest<bool>;

public class MoveCandidateStageCommandHandler : IRequestHandler<MoveCandidateStageCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public MoveCandidateStageCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(MoveCandidateStageCommand request, CancellationToken cancellationToken)
    {
        var cand = await _context.Candidates.FirstOrDefaultAsync(c => c.Id == request.CandidateId, cancellationToken);
        if (cand == null) return false;

        cand.Stage = request.NewStage;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public record ScheduleAndEvaluateInterviewCommand(
    int CandidateId,
    int RoundNumber,
    string RoundTitle,
    DateTime ScheduledStartTime,
    string InterviewMode,
    string LocationOrLink,
    int? InterviewerEmployeeId,
    decimal TechnicalScore,
    decimal CultureFitScore,
    string? EvaluationNotes,
    string Result) : IRequest<int>;

public class ScheduleAndEvaluateInterviewCommandHandler : IRequestHandler<ScheduleAndEvaluateInterviewCommand, int>
{
    private readonly IApplicationDbContext _context;

    public ScheduleAndEvaluateInterviewCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(ScheduleAndEvaluateInterviewCommand request, CancellationToken cancellationToken)
    {
        var cand = await _context.Candidates.FirstOrDefaultAsync(c => c.Id == request.CandidateId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy ứng viên.");

        var totalScore = Math.Round((request.TechnicalScore + request.CultureFitScore) / 2m, 1);

        var interview = new Interview
        {
            CandidateId = cand.Id,
            RoundNumber = request.RoundNumber,
            RoundTitle = request.RoundTitle.Trim(),
            ScheduledStartTime = request.ScheduledStartTime,
            ScheduledEndTime = request.ScheduledStartTime.AddHours(1),
            InterviewMode = request.InterviewMode,
            LocationOrLink = request.LocationOrLink.Trim(),
            InterviewerEmployeeId = request.InterviewerEmployeeId,
            TechnicalScore = request.TechnicalScore,
            CultureFitScore = request.CultureFitScore,
            TotalScore = totalScore,
            EvaluationNotes = request.EvaluationNotes?.Trim(),
            Result = request.Result
        };

        await _context.Interviews.AddAsync(interview, cancellationToken);

        if (totalScore > 0)
            cand.OverallScore = totalScore;

        if (cand.Stage is "Applied" or "Screening")
            cand.Stage = request.RoundNumber >= 2 ? "Interview2" : "Interview1";

        await _context.SaveChangesAsync(cancellationToken);
        return interview.Id;
    }
}

public record CreateJobOfferCommand(
    int CandidateId,
    decimal OfferedProbationSalary,
    decimal OfferedOfficialSalary,
    DateOnly ExpectedStartDate,
    DateOnly OfferExpiryDate) : IRequest<int>;

public class CreateJobOfferCommandHandler : IRequestHandler<CreateJobOfferCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateJobOfferCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateJobOfferCommand request, CancellationToken cancellationToken)
    {
        var cand = await _context.Candidates
            .Include(c => c.JobPosting)
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy ứng viên.");

        var count = await _context.JobOffers.CountAsync(cancellationToken) + 1;
        var offer = new JobOffer
        {
            OfferCode = $"OFFER-{DateTime.Today.Year}-{count:D3}",
            CandidateId = cand.Id,
            ProposedPositionId = cand.JobPosting.PositionId,
            ProposedUnitId = cand.JobPosting.OrganizationUnitId,
            OfferedProbationSalary = request.OfferedProbationSalary,
            OfferedOfficialSalary = request.OfferedOfficialSalary,
            ExpectedStartDate = request.ExpectedStartDate,
            OfferExpiryDate = request.OfferExpiryDate,
            Status = "Sent"
        };

        cand.Stage = "Offered";
        await _context.JobOffers.AddAsync(offer, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return offer.Id;
    }
}

public record ConvertCandidateToEmployeeCommand(int CandidateId) : IRequest<string>;

public class ConvertCandidateToEmployeeCommandHandler : IRequestHandler<ConvertCandidateToEmployeeCommand, string>
{
    private readonly IApplicationDbContext _context;
    private readonly IPiiEncryptionService _encryption;

    public ConvertCandidateToEmployeeCommandHandler(
        IApplicationDbContext context,
        IPiiEncryptionService encryption)
    {
        _context = context;
        _encryption = encryption;
    }

    public async Task<string> Handle(ConvertCandidateToEmployeeCommand request, CancellationToken cancellationToken)
    {
        var cand = await _context.Candidates
            .Include(c => c.JobPosting).ThenInclude(j => j.RecruitmentRequest)
            .Include(c => c.Offers)
            .FirstOrDefaultAsync(c => c.Id == request.CandidateId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy ứng viên.");

        if (cand.ConvertedEmployeeId.HasValue)
            throw new InvalidOperationException("Ứng viên này đã được chuyển đổi thành nhân viên chính thức trước đó.");

        var latestOffer = cand.Offers.OrderByDescending(o => o.Id).FirstOrDefault();
        var officialSalary = latestOffer?.OfferedOfficialSalary ?? (cand.ExpectedSalary > 0 ? cand.ExpectedSalary : 20000000m);
        var hireDate = latestOffer?.ExpectedStartDate ?? DateOnly.FromDateTime(DateTime.Today);

        var empCount = await _context.Employees.CountAsync(cancellationToken) + 1;
        var empCode = $"VNM-{empCount:D4}";
        var syntheticCccd = $"07909{empCount:D7}";

        var employee = new Employee
        {
            EmployeeCode = empCode,
            FullName = cand.FullName,
            DateOfBirth = cand.DateOfBirth ?? new DateOnly(1996, 1, 1),
            Gender = cand.Gender,
            CitizenIdEncrypted = _encryption.Encrypt(syntheticCccd),
            CitizenIdHash = _encryption.ComputeDeterministicHash(syntheticCccd),
            WorkEmail = cand.Email,
            PhoneNumber = cand.PhoneNumber,
            Major = cand.Major,
            HireDate = hireDate,
            OrganizationUnitId = cand.JobPosting.OrganizationUnitId,
            PositionId = cand.JobPosting.PositionId,
            BaseSalary = officialSalary,
            OtherAllowance = 2500000m,
            WorkStatus = "Probation",
            CurrentContractType = "Probation"
        };

        employee.Contracts.Add(new LaborContract
        {
            ContractNumber = $"HD-TV-{empCode}/{DateTime.Today.Year}",
            ContractType = "Probation",
            SignDate = hireDate,
            StartDate = hireDate,
            EndDate = hireDate.AddMonths(2),
            BaseSalary = officialSalary,
            ProbationSalaryRate = 85m,
            Status = "Active"
        });

        await _context.Employees.AddAsync(employee, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _context.EmploymentHistories.AddAsync(new EmploymentHistory
        {
            EmployeeId = employee.Id,
            FromDate = hireDate,
            DepartmentName = cand.JobPosting.OrganizationUnit?.UnitName ?? "Vinamilk",
            PositionName = cand.JobPosting.Position?.PositionName ?? "Chuyên viên",
            Notes = $"Tiếp nhận từ Tuyển dụng (Mã UV: {cand.CandidateCode})"
        }, cancellationToken);

        // Tự động tạo Onboarding Checklist từ mẫu
        var templates = await _context.OnboardingTemplates
            .AsNoTracking()
            .Where(t => t.ProcessType == "Onboarding")
            .OrderBy(t => t.SortOrder)
            .ToListAsync(cancellationToken);

        foreach (var tpl in templates)
        {
            await _context.EmployeeOnboardingTasks.AddAsync(new EmployeeOnboardingTask
            {
                EmployeeId = employee.Id,
                OnboardingTemplateId = tpl.Id,
                ProcessType = "Onboarding",
                TaskName = tpl.TaskName,
                DueDate = hireDate.AddDays(tpl.DueDaysOffset),
                IsCompleted = false,
                Notes = $"Bộ phận phụ trách: {tpl.AssignedDepartmentRole}"
            }, cancellationToken);
        }

        cand.Stage = "Hired";
        cand.ConvertedEmployeeId = employee.Id;
        if (cand.JobPosting.RecruitmentRequest != null)
            cand.JobPosting.RecruitmentRequest.HiredCount += 1;

        await _context.SaveChangesAsync(cancellationToken);

        return $"Đã chuyển đổi ứng viên {cand.FullName} thành nhân viên {empCode} (HĐ Thử việc + Onboarding Checklist)!";
    }
}
