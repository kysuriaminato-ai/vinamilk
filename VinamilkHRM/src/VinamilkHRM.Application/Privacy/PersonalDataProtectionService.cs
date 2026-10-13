using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Privacy;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Privacy;

public class PersonalDataExportDto
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string CitizenId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public DateTime HireDate { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;

    public List<string> Contracts { get; set; } = new();
    public List<string> Consents { get; set; } = new();
    public List<string> DataAccessAuditLogs { get; set; } = new();
    public DateTimeOffset ExportedAt { get; set; } = DateTimeOffset.UtcNow;
}

public interface IPersonalDataProtectionService
{
    Task RecordConsentAsync(
        int subjectId,
        string subjectType,
        string subjectEmail,
        string purpose,
        string termsVersion,
        bool isAgreed,
        string? ipAddress = null,
        CancellationToken ct = default);

    Task LogSensitiveDataAccessAsync(
        int viewerUserId,
        int targetEmployeeId,
        string accessedField,
        string accessPurpose,
        string? ipAddress = null,
        CancellationToken ct = default);

    Task<int> AnonymizeExpiredCandidatesAsync(
        int retentionMonths = 12,
        CancellationToken ct = default);

    Task<PersonalDataExportDto> ExportEmployeePersonalDataAsync(
        int employeeId,
        int requestedByUserId,
        CancellationToken ct = default);
}

public class PersonalDataProtectionService : IPersonalDataProtectionService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<PersonalDataProtectionService> _logger;

    public PersonalDataProtectionService(
        IApplicationDbContext context,
        ILogger<PersonalDataProtectionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RecordConsentAsync(
        int subjectId,
        string subjectType,
        string subjectEmail,
        string purpose,
        string termsVersion,
        bool isAgreed,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var consent = new PersonalDataConsent(
            subjectId,
            subjectType,
            subjectEmail,
            purpose,
            termsVersion,
            isAgreed,
            ipAddress);

        _context.PersonalDataConsents.Add(consent);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Đã ghi nhận sự đồng ý dữ liệu cá nhân cho {Type} ID {Id} (Mục đích: {Purpose}).", 
            subjectType, subjectId, purpose);
    }

    public async Task LogSensitiveDataAccessAsync(
        int viewerUserId,
        int targetEmployeeId,
        string accessedField,
        string accessPurpose,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var log = new PersonalDataAccessLog(
            viewerUserId,
            targetEmployeeId,
            accessedField,
            accessPurpose,
            ipAddress,
            isAuthorized: true);

        _context.PersonalDataAccessLogs.Add(log);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[AUDIT NĐ 13] Người dùng {ViewerId} đã truy cập trường '{Field}' của nhân viên {TargetId} (Mục đích: {Purpose}).",
            viewerUserId, accessedField, targetEmployeeId, accessPurpose);
    }

    public async Task<int> AnonymizeExpiredCandidatesAsync(
        int retentionMonths = 12,
        CancellationToken ct = default)
    {
        DateTime cutoffDate = DateTime.UtcNow.AddMonths(-retentionMonths);

        var expiredCandidates = await _context.CandidateApplications
            .Where(c => c.Status == JobApplicationStatus.Rejected 
                     && c.AppliedAt <= cutoffDate
                     && !c.IsAnonymized)
            .ToListAsync(ct);

        int count = 0;
        foreach (var candidate in expiredCandidates)
        {
            candidate.Anonymize();
            count++;
        }

        if (count > 0)
        {
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Đã hoàn tất ẩn danh {Count} hồ sơ ứng viên không trúng tuyển quá hạn {Months} tháng theo Nghị định 13/2023.", 
                count, retentionMonths);
        }

        return count;
    }

    public async Task<PersonalDataExportDto> ExportEmployeePersonalDataAsync(
        int employeeId,
        int requestedByUserId,
        CancellationToken ct = default)
    {
        var employee = await _context.Employees
            .Include(e => e.StandardJobTitle)
            .Include(e => e.OrganizationalUnit)
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct);

        if (employee == null)
            throw new DomainException($"Không tìm thấy nhân viên ID {employeeId}.", "ERR_PDP_EMP_NOT_FOUND");

        // Ghi nhật ký hành động xuất dữ liệu cá nhân
        await LogSensitiveDataAccessAsync(
            requestedByUserId,
            employeeId,
            "FULL_PERSONAL_DATA_EXPORT",
            "Xuất toàn bộ gói dữ liệu cá nhân theo yêu cầu chủ thể (Điều 14 NĐ 13/2023)",
            null,
            ct);

        var contracts = await _context.LaborContracts
            .Where(c => c.EmployeeId == employeeId)
            .Select(c => $"{c.ContractNumber} ({c.ContractType}): Từ {c.StartDate:dd/MM/yyyy} - Lương: {c.BasicSalary:N0} VNĐ")
            .ToListAsync(ct);

        var consents = await _context.PersonalDataConsents
            .Where(c => c.SubjectId == employeeId && c.SubjectType == "Employee")
            .Select(c => $"Mục đích: {c.ConsentPurpose} | Điều khoản: {c.TermsVersion} | Thời điểm: {c.ConsentTimestamp:dd/MM/yyyy HH:mm} | Đồng ý: {c.IsAgreed}")
            .ToListAsync(ct);

        var auditLogs = await _context.PersonalDataAccessLogs
            .Where(l => l.TargetEmployeeId == employeeId)
            .OrderByDescending(l => l.AccessTimestamp)
            .Take(50)
            .Select(l => $"Người xem: ID {l.ViewerUserId} | Trường: {l.AccessedField} | Mục đích: {l.AccessPurpose} | Thời gian: {l.AccessTimestamp:dd/MM/yyyy HH:mm:ss}")
            .ToListAsync(ct);

        return new PersonalDataExportDto
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            FullName = employee.FullName,
            CitizenId = employee.CitizenId,
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            Address = employee.Address,
            BankAccountNumber = employee.BankAccountNumber ?? "Chưa cung cấp",
            BankName = employee.BankName ?? "Chưa cung cấp",
            DateOfBirth = employee.DateOfBirth,
            HireDate = employee.HireDate,
            JobTitle = employee.StandardJobTitle?.JobTitleName ?? "",
            Department = employee.OrganizationalUnit?.UnitName ?? "",
            Contracts = contracts,
            Consents = consents,
            DataAccessAuditLogs = auditLogs,
            ExportedAt = DateTimeOffset.UtcNow
        };
    }
}