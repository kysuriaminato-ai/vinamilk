using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Careers;

/// <summary>
/// Đại diện cho Tin tuyển dụng công khai trên Cổng Careers của Tập đoàn Vinamilk.
/// Hỗ trợ chuẩn SEO, Schema.org JobPosting cho Google Jobs, và liên kết phân vùng khối làm việc.
/// </summary>
public class JobPosting : BaseAuditableEntity
{
    public string JobCode { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public UnitType UnitType { get; private set; }
    public string Department { get; private set; } = string.Empty;
    public string WorkLocation { get; private set; } = string.Empty;
    public string City { get; private set; } = "Hồ Chí Minh";
    public int RegionId { get; private set; } = 3; // 1: Bắc, 2: Trung, 3: Nam

    public string EmploymentType { get; private set; } = "FULL_TIME"; // Schema.org: FULL_TIME, PART_TIME, CONTRACT, INTERN
    public string ExperienceLevel { get; private set; } = "1-3 năm kinh nghiệm";
    public string EducationRequirement { get; private set; } = "Đại học / Cao đẳng";

    public decimal? SalaryMin { get; private set; }
    public decimal? SalaryMax { get; private set; }
    public string SalaryDisplay { get; private set; } = "Thỏa thuận theo năng lực";

    public DateTime DatePosted { get; private set; }
    public DateTime ValidThrough { get; private set; }

    public string Description { get; private set; } = string.Empty;
    public string Requirements { get; private set; } = string.Empty;
    public string Benefits { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;
    public bool IsFeatured { get; private set; } = false;
    public int ViewCount { get; private set; } = 0;

    private readonly List<CandidateApplication> _applications = new();
    public virtual IReadOnlyCollection<CandidateApplication> Applications => _applications.AsReadOnly();

    protected JobPosting()
    {
    }

    public JobPosting(
        string jobCode,
        string title,
        string slug,
        UnitType unitType,
        string department,
        string workLocation,
        string city,
        int regionId,
        string employmentType,
        string experienceLevel,
        string educationRequirement,
        decimal? salaryMin,
        decimal? salaryMax,
        string salaryDisplay,
        DateTime datePosted,
        DateTime validThrough,
        string description,
        string requirements,
        string benefits,
        bool isFeatured = false)
    {
        if (string.IsNullOrWhiteSpace(jobCode))
            throw new DomainException("Mã tin tuyển dụng không được để trống.", "ERR_JOB_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Tiêu đề tin tuyển dụng không được để trống.", "ERR_JOB_TITLE_EMPTY");

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Slug URL tuyển dụng không được để trống.", "ERR_JOB_SLUG_EMPTY");

        JobCode = jobCode.Trim().ToUpperInvariant();
        Title = title.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        UnitType = unitType;
        Department = department?.Trim() ?? "Nhân sự & Vận hành";
        WorkLocation = workLocation?.Trim() ?? "Vinamilk Tower, Tân Trào, Quận 7, TP.HCM";
        City = string.IsNullOrWhiteSpace(city) ? "Hồ Chí Minh" : city.Trim();
        RegionId = regionId > 0 ? regionId : 3;
        EmploymentType = string.IsNullOrWhiteSpace(employmentType) ? "FULL_TIME" : employmentType.Trim().ToUpperInvariant();
        ExperienceLevel = experienceLevel?.Trim() ?? "Theo yêu cầu công việc";
        EducationRequirement = educationRequirement?.Trim() ?? "Đại học";
        SalaryMin = salaryMin;
        SalaryMax = salaryMax;
        SalaryDisplay = string.IsNullOrWhiteSpace(salaryDisplay) ? "Thỏa thuận" : salaryDisplay.Trim();
        DatePosted = datePosted;
        ValidThrough = validThrough > datePosted ? validThrough : datePosted.AddDays(45);
        Description = description?.Trim() ?? string.Empty;
        Requirements = requirements?.Trim() ?? string.Empty;
        Benefits = benefits?.Trim() ?? string.Empty;
        IsActive = true;
        IsFeatured = isFeatured;
        ViewCount = 0;
    }

    public void IncrementViews()
    {
        ViewCount++;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
