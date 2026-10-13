using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Employees;

/// <summary>
/// Chứng chỉ chuyên môn, ngoại ngữ, an toàn vệ sinh thực phẩm (HACCP, ISO 22000, FSSC...).
/// </summary>
public class EmployeeCertificate : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public string CertificateName { get; private set; } = string.Empty;
    public string? CertificateCode { get; private set; }
    public string IssuingAuthority { get; private set; } = string.Empty;
    public DateOnly IssueDate { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public string? ScoreOrGrade { get; private set; }

    protected EmployeeCertificate()
    {
    }

    public EmployeeCertificate(
        int employeeId,
        string certificateName,
        string issuingAuthority,
        DateOnly issueDate,
        DateOnly? expiryDate = null,
        string? certificateCode = null,
        string? scoreOrGrade = null)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên chứng chỉ không hợp lệ.", "ERR_CERT_EMP_INVALID");

        if (string.IsNullOrWhiteSpace(certificateName))
            throw new DomainException("Tên chứng chỉ không được để trống.", "ERR_CERT_NAME_EMPTY");

        if (string.IsNullOrWhiteSpace(issuingAuthority))
            throw new DomainException("Đơn vị cấp chứng chỉ không được để trống.", "ERR_CERT_ISSUER_EMPTY");

        if (expiryDate.HasValue && expiryDate.Value < issueDate)
            throw new DomainException("Ngày hết hạn chứng chỉ không thể trước ngày cấp.", "ERR_CERT_DATES_INVALID");

        EmployeeId = employeeId;
        CertificateName = certificateName.Trim();
        IssuingAuthority = issuingAuthority.Trim();
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
        CertificateCode = certificateCode?.Trim();
        ScoreOrGrade = scoreOrGrade?.Trim();
    }
}
