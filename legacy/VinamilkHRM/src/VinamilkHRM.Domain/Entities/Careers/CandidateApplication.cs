using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Careers;

/// <summary>
/// Đại diện cho Hồ sơ ứng tuyển của Ứng viên gửi qua Cổng Careers Vinamilk.
/// Khi ứng viên trúng tuyển (Hired), hệ thống kích hoạt Onboarding tự động sang Nhân viên thử việc (Probation Employee).
/// </summary>
public class CandidateApplication : BaseAuditableEntity
{
    public int JobPostingId { get; private set; }
    public virtual JobPosting? JobPosting { get; private set; }

    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public string CitizenId { get; private set; } = string.Empty;
    public DateTime DateOfBirth { get; private set; }
    public Gender Gender { get; private set; } = Gender.Male;
    public string Address { get; private set; } = string.Empty;

    public string ResumeFileName { get; private set; } = string.Empty;
    public string CoverLetter { get; private set; } = string.Empty;
    public string PortfolioUrl { get; private set; } = string.Empty;

    public JobApplicationStatus Status { get; private set; } = JobApplicationStatus.Submitted;
    public DateTime AppliedAt { get; private set; } = DateTime.UtcNow;

    public DateTime? InterviewDate { get; private set; }
    public decimal? InterviewScore { get; private set; }
    public string? EvaluationNotes { get; private set; }

    public int? HiredEmployeeId { get; private set; }
    public virtual Employee? HiredEmployee { get; private set; }
    public DateTime? HiredDate { get; private set; }

    protected CandidateApplication()
    {
    }

    public CandidateApplication(
        int jobPostingId,
        string fullName,
        string email,
        string phoneNumber,
        string citizenId,
        DateTime dateOfBirth,
        Gender gender,
        string address,
        string resumeFileName,
        string coverLetter = "",
        string portfolioUrl = "")
    {
        if (jobPostingId <= 0)
            throw new DomainException("Mã tin tuyển dụng không hợp lệ.", "ERR_JOB_ID_INVALID");

        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Họ và tên ứng viên không được để trống.", "ERR_CANDIDATE_NAME_EMPTY");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email ứng viên không được để trống.", "ERR_CANDIDATE_EMAIL_EMPTY");

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new DomainException("Số điện thoại ứng viên không được để trống.", "ERR_CANDIDATE_PHONE_EMPTY");

        JobPostingId = jobPostingId;
        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        PhoneNumber = phoneNumber.Trim();
        CitizenId = citizenId?.Trim() ?? string.Empty;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Address = address?.Trim() ?? string.Empty;
        ResumeFileName = resumeFileName?.Trim() ?? "CV_Applicant.pdf";
        CoverLetter = coverLetter?.Trim() ?? string.Empty;
        PortfolioUrl = portfolioUrl?.Trim() ?? string.Empty;
        Status = JobApplicationStatus.Submitted;
        AppliedAt = DateTime.UtcNow;
    }

    public void ScheduleInterview(DateTime interviewDate, string notes = "")
    {
        InterviewDate = interviewDate;
        EvaluationNotes = notes?.Trim();
        Status = JobApplicationStatus.Interviewed;
    }

    public void RecordInterviewResult(decimal score, string notes, bool passed)
    {
        InterviewScore = score;
        EvaluationNotes = notes?.Trim();
        Status = passed ? JobApplicationStatus.Offered : JobApplicationStatus.Rejected;
    }

    public void MarkAsHired(int employeeId)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên vừa tạo không hợp lệ.", "ERR_HIRED_EMP_ID_INVALID");

        HiredEmployeeId = employeeId;
        HiredDate = DateTime.UtcNow;
        Status = JobApplicationStatus.Hired;
    }

    public void Reject(string reason = "")
    {
        Status = JobApplicationStatus.Rejected;
        EvaluationNotes = reason?.Trim();
    }
}
