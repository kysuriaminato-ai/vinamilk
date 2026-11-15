using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Analytics;
using Vinamilk.HRM.Application.Common.Interfaces;

namespace Vinamilk.HRM.Infrastructure.Services;

/// <summary>
/// Default local RAG + heuristic AI HRM Expert implementation.
/// Can be replaced in DI with OpenAI / Azure OpenAI / Gemini provider without changing Application/Web layers.
/// </summary>
public class LocalRagAiHrmExpertService : IAiHrmExpertService
{
    private readonly IApplicationDbContext _context;

    public LocalRagAiHrmExpertService(IApplicationDbContext context) => _context = context;

    public string ProviderName => "Vinamilk Local RAG + Heuristic Engine (Swappable IAiHrmExpertService)";

    private sealed record KnowledgeChunk(string SourceCode, string Title, string[] Keywords, string Content);

    private static readonly List<KnowledgeChunk> KnowledgeBase =
    [
        new("BLLD-2019-D113", "Nghỉ phép năm (Điều 113 Bộ luật Lao động 2019)",
            ["nghỉ phép", "phép năm", "12 ngày", "thâm niên", "annual leave", "ngay phep"],
            "Người lao động làm việc đủ 12 tháng được nghỉ hằng năm hưởng nguyên lương 12 ngày (công việc bình thường), 14-16 ngày (nặng nhọc/độc hại). Cứ đủ 05 năm làm việc tại Vinamilk được cộng thêm 01 ngày phép thâm niên. Đơn nghỉ phép cần gửi trên ESS trước ít nhất 02 ngày làm việc."),
        new("BLLD-2019-D98", "Làm thêm giờ / Tăng ca OT (Điều 98 BLLĐ 2019)",
            ["tăng ca", "ot", "làm thêm giờ", "overtime", "150%", "200%", "300%"],
            "Tiền lương làm thêm giờ được tính: ít nhất 150% vào ngày thường, 200% vào ngày nghỉ hằng tuần, và 300% vào ngày lễ/Tết (chưa kể lương ngày lễ). Nhân sự đăng ký OT qua ESS và phải được Quản lý trực tiếp phê duyệt trước khi thực hiện."),
        new("VNM-BHXH-2026", "Tỷ lệ trích đóng BHXH, BHYT, BHTN, Công đoàn & Thuế TNCN theo hiệu lực",
            ["bhxh", "bhyt", "bhtn", "bảo hiểm", "công đoàn", "tncn", "thuế", "giảm trừ"],
            "Người lao động đóng 10.5% (8% BHXH + 1.5% BHYT + 1% BHTN), Doanh nghiệp đóng 21.5% + 2% Kinh phí Công đoàn; trần BHTN tính theo 20×LTTV vùng (I–IV). Từ 01/01/2026 áp dụng Luật 109/2025/QH15 & NQ 110/2025/UBTVQH15: biểu thuế TNCN 5 bậc (5%–35%), giảm trừ bản thân 15,5 triệu VNĐ/tháng và người phụ thuộc 6,2 triệu VNĐ/người/tháng; lương cơ sở điều chỉnh từ 01/07/2026 theo bảng tham số hiệu lực."),
        new("BLLD-2019-D25", "Thời gian thử việc & Tỷ lệ hưởng lương thử việc (Điều 25-26)",
            ["thử việc", "probation", "85%", "60 ngày", "hợp đồng"],
            "Thời gian thử việc tối đa 60 ngày đối với công việc có chức danh nghề nghiệp cần trình độ cao đẳng trở lên, 180 ngày với người quản lý doanh nghiệp. Mức lương thử việc tại Vinamilk tối thiểu bằng 85% mức lương chính thức của vị trí."),
        new("VNM-KPI-2025", "Chính sách Đánh giá KPI & Thưởng hiệu suất",
            ["kpi", "thưởng", "đánh giá", "xếp loại", "nâng lương", "hiệu suất"],
            "Chu kỳ đánh giá KPI thực hiện theo Quý/Năm. Xếp loại A (>100%): thưởng 150% quỹ thưởng KPI và ưu tiên xét nâng bậc lương sớm. Xếp loại B (85-100%): 100% quỹ thưởng. Xếp loại D (<70%): cần lập Kế hoạch cải thiện hiệu suất (PIP) 60 ngày.")
    ];

    public Task<AiPolicyAnswerDto> AskPolicyAsync(string question, CancellationToken cancellationToken = default)
    {
        var q = (question ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(q))
        {
            return Task.FromResult(new AiPolicyAnswerDto(
                question ?? string.Empty,
                "Vui lòng nhập câu hỏi về chính sách nhân sự, phép năm, tăng ca (OT), BHXH, thuế TNCN, thử việc hoặc KPI.",
                ProviderName,
                new List<AiRagCitationDto>()));
        }

        var scored = KnowledgeBase
            .Select(chunk => new
            {
                Chunk = chunk,
                Score = chunk.Keywords.Count(k => q.Contains(k)) * 10
                        + (q.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Count(w => w.Length > 2 && chunk.Content.ToLowerInvariant().Contains(w)))
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(2)
            .ToList();

        if (scored.Count == 0)
        {
            var fallback = KnowledgeBase.Take(2).Select(c => new AiRagCitationDto(c.SourceCode, c.Title, c.Content)).ToList();
            return Task.FromResult(new AiPolicyAnswerDto(
                question!,
                "Chưa tìm thấy điều khoản khớp chính xác với từ khóa trong kho tri thức nội bộ. Bạn có thể hỏi rõ hơn về: Nghỉ phép năm, Tăng ca (OT), BHXH & Thuế TNCN, Thử việc, hoặc KPI. Dưới đây là 2 tài liệu tham khảo gần nhất:",
                ProviderName,
                fallback));
        }

        var citations = scored.Select(x => new AiRagCitationDto(x.Chunk.SourceCode, x.Chunk.Title, x.Chunk.Content)).ToList();
        var answerText = string.Join("\n\n", scored.Select(x => $"• **{x.Chunk.Title}**: {x.Chunk.Content}"));

        return Task.FromResult(new AiPolicyAnswerDto(question!, answerText, ProviderName, citations));
    }

    public async Task<List<CandidateJobMatchDto>> MatchCandidatesAsync(int? jobPostingId = null, CancellationToken cancellationToken = default)
    {
        var jobQuery = _context.JobPostings
            .AsNoTracking()
            .Include(j => j.Position)
            .Where(j => j.Status == "Open");

        if (jobPostingId.HasValue && jobPostingId > 0)
            jobQuery = jobQuery.Where(j => j.Id == jobPostingId.Value);

        var openJobs = await jobQuery.Take(10).ToListAsync(cancellationToken);
        var candidates = await _context.Candidates
            .AsNoTracking()
            .OrderByDescending(c => c.YearsOfExperience)
            .Take(30)
            .ToListAsync(cancellationToken);

        var results = new List<CandidateJobMatchDto>();
        foreach (var cand in candidates)
        {
            var job = openJobs.FirstOrDefault(j => j.Id == cand.JobPostingId) ?? openJobs.FirstOrDefault();
            if (job is null) continue;

            var score = 55;
            var reasons = new List<string>();

            if (cand.JobPostingId == job.Id)
            {
                score += 15;
                reasons.Add("Ứng tuyển đúng vị trí");
            }
            if (cand.YearsOfExperience >= 3)
            {
                score += 15;
                reasons.Add($"{cand.YearsOfExperience:0.#} năm kinh nghiệm");
            }
            else if (cand.YearsOfExperience >= 1)
            {
                score += 8;
                reasons.Add($"{cand.YearsOfExperience:0.#} năm kinh nghiệm");
            }

            if (!string.IsNullOrWhiteSpace(cand.Major) &&
                (!string.IsNullOrWhiteSpace(job.JobRequirements) &&
                 job.JobRequirements.Contains(cand.Major, StringComparison.OrdinalIgnoreCase) ||
                 job.Title.Contains(cand.Major, StringComparison.OrdinalIgnoreCase)))
            {
                score += 12;
                reasons.Add($"Chuyên ngành {cand.Major} phù hợp");
            }
            else if (!string.IsNullOrWhiteSpace(cand.Major))
            {
                score += 5;
                reasons.Add($"Chuyên ngành: {cand.Major}");
            }

            score = Math.Min(98, score);
            results.Add(new CandidateJobMatchDto(
                cand.Id,
                cand.FullName,
                cand.Email,
                job.Id,
                job.Title,
                score,
                reasons.Count > 0 ? string.Join(" • ", reasons) : "Phù hợp tiêu chí cơ bản"));
        }

        return results.OrderByDescending(r => r.MatchScore).Take(10).ToList();
    }

    public async Task<List<AttritionRiskItemDto>> AnalyzeAttritionRisksAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var threshold30 = today.AddDays(30);

        var employees = await _context.Employees
            .AsNoTracking()
            .Include(e => e.OrganizationUnit)
            .Where(e => e.WorkStatus != "Terminated")
            .ToListAsync(cancellationToken);

        if (employees.Count == 0)
            return new List<AttritionRiskItemDto>();

        var avgSalary = employees.Average(e => e.BaseSalary);

        var expiringContractEmpIds = await _context.LaborContracts
            .AsNoTracking()
            .Where(c => c.Status == "Active" && c.EndDate.HasValue && c.EndDate.Value >= today && c.EndDate.Value <= threshold30)
            .Select(c => c.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var lateCounts = await _context.MonthlyAttendanceSummaries
            .AsNoTracking()
            .GroupBy(a => a.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, TotalLate = g.Sum(x => x.TotalLateCount) })
            .ToDictionaryAsync(x => x.EmployeeId, x => x.TotalLate, cancellationToken);

        var risks = new List<AttritionRiskItemDto>();
        foreach (var emp in employees)
        {
            var score = 10;
            var factors = new List<string>();

            if (emp.WorkStatus == "Probation")
            {
                score += 25;
                factors.Add("Đang trong giai đoạn thử việc");
            }

            if (expiringContractEmpIds.Contains(emp.Id))
            {
                score += 30;
                factors.Add("Hợp đồng lao động sắp hết hạn trong 30 ngày");
            }

            if (emp.BaseSalary < avgSalary * 0.75m)
            {
                score += 20;
                factors.Add("Mức lương thấp hơn 25% so với bình quân công ty");
            }

            if (lateCounts.TryGetValue(emp.Id, out var late) && late >= 3)
            {
                score += 20;
                factors.Add($"Đi trễ {late} lần trong kỳ chấm công");
            }

            if (factors.Count == 0)
                continue;

            var level = score >= 65 ? "Cao" : score >= 40 ? "Trung bình" : "Thấp";
            var action = score >= 65
                ? "HRBP & Quản lý trực tiếp trao đổi 1-1 ngay trong tuần và rà soát tái ký/điều chỉnh đãi ngộ."
                : "Theo dõi mức độ gắn kết và trao đổi định kỳ trong buổi họp nhóm.";

            risks.Add(new AttritionRiskItemDto(
                emp.Id,
                emp.EmployeeCode,
                emp.FullName,
                emp.OrganizationUnit.UnitName,
                Math.Min(99, score),
                level,
                factors,
                action));
        }

        return risks.OrderByDescending(r => r.RiskScore).Take(12).ToList();
    }
}
