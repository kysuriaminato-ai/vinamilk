using System;
using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Privacy;

/// <summary>
/// Ghi nhận sự đồng ý xử lý dữ liệu cá nhân (Personal Data Consent) theo Nghị định 13/2023/NĐ-CP.
/// Lưu trữ mục đích, thời điểm, phiên bản điều khoản và địa chỉ IP.
/// </summary>
public class PersonalDataConsent : BaseAuditableEntity
{
    public int SubjectId { get; private set; } // EmployeeId hoặc CandidateApplicationId
    public string SubjectType { get; private set; } = "Employee"; // "Employee", "Candidate"
    public string SubjectEmail { get; private set; } = string.Empty;

    public string ConsentPurpose { get; private set; } = string.Empty; // "Recruitment", "Employment", "PayrollAndTax", "SocialInsurance"
    public string TermsVersion { get; private set; } = "VNM-PDP-v2026.1";
    public DateTimeOffset ConsentTimestamp { get; private set; } = DateTimeOffset.UtcNow;
    public bool IsAgreed { get; private set; } = true;
    public string? IpAddress { get; private set; }
    public DateTimeOffset? WithdrawnAt { get; private set; }

    protected PersonalDataConsent() { }

    public PersonalDataConsent(
        int subjectId,
        string subjectType,
        string subjectEmail,
        string consentPurpose,
        string termsVersion,
        bool isAgreed,
        string? ipAddress = null)
    {
        if (subjectId <= 0)
            throw new DomainException("Mã chủ thể dữ liệu không hợp lệ.", "ERR_CONSENT_SUBJECT_INVALID");

        if (string.IsNullOrWhiteSpace(consentPurpose))
            throw new DomainException("Mục đích xử lý dữ liệu cá nhân không được để trống.", "ERR_CONSENT_PURPOSE_EMPTY");

        SubjectId = subjectId;
        SubjectType = subjectType.Trim();
        SubjectEmail = subjectEmail.Trim();
        ConsentPurpose = consentPurpose.Trim();
        TermsVersion = string.IsNullOrWhiteSpace(termsVersion) ? "VNM-PDP-v2026.1" : termsVersion.Trim();
        ConsentTimestamp = DateTimeOffset.UtcNow;
        IsAgreed = isAgreed;
        IpAddress = ipAddress?.Trim();
    }

    public void Withdraw(DateTimeOffset now)
    {
        IsAgreed = false;
        WithdrawnAt = now;
    }
}