using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;

namespace VinamilkHRM.Domain.Entities.Privacy;

/// <summary>
/// Nhật ký truy cập dữ liệu cá nhân nhạy cảm (CCCD, Số tài khoản ngân hàng, Lương, Mã số thuế).
/// Phục vụ thanh tra tuân thủ Nghị định 13/2023/NĐ-CP (Tiêu chí 4).
/// </summary>
public class PersonalDataAccessLog : BaseAuditableEntity
{
    public int ViewerUserId { get; private set; }
    public int TargetEmployeeId { get; private set; }
    public virtual Employee? TargetEmployee { get; private set; }

    public string AccessedField { get; private set; } = string.Empty; // "CitizenId", "BankAccountNumber", "BaseSalary", "TaxCode", "PaySlip"
    public DateTimeOffset AccessTimestamp { get; private set; } = DateTimeOffset.UtcNow;
    public string AccessPurpose { get; private set; } = string.Empty; // "HR Profile Verification", "Payroll Audit", "Employee Detail View"
    public string? IpAddress { get; private set; }
    public bool IsAuthorized { get; private set; } = true;

    protected PersonalDataAccessLog() { }

    public PersonalDataAccessLog(
        int viewerUserId,
        int targetEmployeeId,
        string accessedField,
        string accessPurpose,
        string? ipAddress = null,
        bool isAuthorized = true)
    {
        if (viewerUserId <= 0)
            throw new DomainException("Mã người truy cập không hợp lệ.", "ERR_LOG_VIEWER_INVALID");

        if (targetEmployeeId <= 0)
            throw new DomainException("Mã nhân viên bị truy cập không hợp lệ.", "ERR_LOG_TARGET_INVALID");

        if (string.IsNullOrWhiteSpace(accessedField))
            throw new DomainException("Trường dữ liệu truy cập không được để trống.", "ERR_LOG_FIELD_EMPTY");

        ViewerUserId = viewerUserId;
        TargetEmployeeId = targetEmployeeId;
        AccessedField = accessedField.Trim();
        AccessPurpose = accessPurpose?.Trim() ?? "Chi tiết hồ sơ nhân sự";
        AccessTimestamp = DateTimeOffset.UtcNow;
        IpAddress = ipAddress?.Trim();
        IsAuthorized = isAuthorized;
    }
}