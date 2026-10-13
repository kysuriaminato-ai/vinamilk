using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Salaries;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Auditing;
using VinamilkHRM.Domain.Entities.Contracts;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Interfaces.Repositories;

namespace VinamilkHRM.Application.Services;

/// <summary>
/// Dịch vụ quản lý Quy trình Tăng lương Thường lệ & Giám đốc Đặc cách (SalaryAdjustmentService).
/// Thực thi trọn vẹn:
/// 1. Tăng lương phòng ban: Đi qua Workflow thẩm định ngân sách C&B và phê duyệt đa cấp.
/// 2. Giám đốc ĐẶC CÁCH tăng lương: CEO có quyền kích hoạt Fast-track Override, bỏ qua quy trình thẩm định,
///    tự động cập nhật mức lương mới vào hồ sơ nhân viên, sinh phụ lục hợp đồng điện tử và ghi vết AuditLog bảo mật cao.
/// </summary>
public class SalaryAdjustmentService : ISalaryAdjustmentService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILaborContractRepository _contractRepository;

    public SalaryAdjustmentService(
        IApplicationDbContext dbContext,
        IEmployeeRepository employeeRepository,
        ILaborContractRepository contractRepository)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        _contractRepository = contractRepository ?? throw new ArgumentNullException(nameof(contractRepository));
    }

    public async Task<IReadOnlyList<SalaryProposalDto>> GetAllProposalsAsync(SalaryProposalStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SalaryProposals
            .Include(p => p.Employee)
                .ThenInclude(e => e!.OrganizationalUnit)
            .Include(p => p.ProposedBy)
            .AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var list = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return list.Select(p => MapToProposalDto(p, null)).ToList();
    }

    public async Task<SalaryProposalDto?> GetProposalByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var proposal = await _dbContext.SalaryProposals
            .Include(p => p.Employee)
                .ThenInclude(e => e!.OrganizationalUnit)
            .Include(p => p.ProposedBy)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return proposal == null ? null : MapToProposalDto(proposal);
    }

    public async Task<SalaryProposalDto> SubmitDepartmentProposalAsync(CreateSalaryProposalRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy nhân viên Id={request.EmployeeId}.", "ERR_EMP_NOT_FOUND");

        var proposal = new SalaryProposal(
            employeeId: request.EmployeeId,
            proposedById: request.ProposedById,
            currentSalary: employee.BaseSalary,
            proposedSalary: request.ProposedSalary,
            currentAllowance: employee.Allowance,
            proposedAllowance: request.ProposedAllowance,
            reason: request.Reason,
            effectiveDate: request.EffectiveDate);

        await _dbContext.SalaryProposals.AddAsync(proposal, cancellationToken);

        var audit = new AuditLog(
            tableName: "SalaryProposals",
            action: "SUBMIT_DEPARTMENT_SALARY_PROPOSAL",
            entityId: employee.Id.ToString(),
            oldValues: JsonSerializer.Serialize(new { CurrentSalary = employee.BaseSalary, CurrentAllowance = employee.Allowance }),
            newValues: JsonSerializer.Serialize(new { ProposedSalary = request.ProposedSalary, ProposedAllowance = request.ProposedAllowance, request.Reason }),
            performedBy: $"EMPLOYEE_ID_{request.ProposedById}");

        await _dbContext.AuditLogs.AddAsync(audit, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToProposalDto(proposal, employee);
    }

    public async Task ApproveByDepartmentHeadAsync(int proposalId, int managerId, string notes, CancellationToken cancellationToken = default)
    {
        var proposal = await _dbContext.SalaryProposals.FirstOrDefaultAsync(p => p.Id == proposalId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy tờ trình đề xuất Id={proposalId}.", "ERR_SAL_NOT_FOUND");

        proposal.ApproveByDepartmentHead(managerId, notes);

        var audit = new AuditLog(
            tableName: "SalaryProposals",
            action: "DEPARTMENT_HEAD_APPROVE_SALARY",
            entityId: proposal.Id.ToString(),
            oldValues: JsonSerializer.Serialize(new { Status = "PendingDepartmentHead" }),
            newValues: JsonSerializer.Serialize(new { Status = "PendingHRDirector", ManagerId = managerId, Notes = notes }),
            performedBy: $"MANAGER_ID_{managerId}");

        await _dbContext.AuditLogs.AddAsync(audit, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveByHRDirectorAsync(int proposalId, int hrDirectorId, string notes, CancellationToken cancellationToken = default)
    {
        var proposal = await _dbContext.SalaryProposals
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.Id == proposalId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy tờ trình đề xuất Id={proposalId}.", "ERR_SAL_NOT_FOUND");

        var employee = proposal.Employee ?? await _employeeRepository.GetByIdAsync(proposal.EmployeeId, cancellationToken)
            ?? throw new DomainException("Không tìm thấy hồ sơ nhân sự để áp dụng mức lương mới.", "ERR_EMP_NOT_FOUND");

        // 1. Phê duyệt đề xuất
        proposal.ApproveByHRDirector(hrDirectorId, notes);

        // 2. Cập nhật hồ sơ lương nhân viên (Encapsulation)
        employee.PromoteSalary(proposal.ProposedSalary, proposal.ProposedAllowance, $"Quyết định nâng lương thường lệ: {proposal.Reason}");

        // 3. Tự động sinh Phụ lục Hợp đồng Lao động Điện tử
        var contract = await _contractRepository.GetActiveContractByEmployeeIdAsync(employee.Id, cancellationToken);
        string appendixNumber = $"PLHD-{DateTime.UtcNow:yyyyMMdd}-{employee.EmployeeCode}-{Random.Shared.Next(100, 999)}";
        string signatureHash = ComputeSha256($"{appendixNumber}_{employee.EmployeeCode}_{proposal.ProposedSalary}_{DateTime.UtcNow:O}_VNM_HR_STANDARD");

        var appendix = new ElectronicContractAppendix(
            appendixNumber: appendixNumber,
            laborContractId: contract?.Id ?? 1,
            employeeId: employee.Id,
            effectiveDate: proposal.EffectiveDate,
            signDate: DateTime.UtcNow,
            previousSalary: proposal.CurrentSalary,
            newSalary: proposal.ProposedSalary,
            previousAllowance: proposal.CurrentAllowance,
            newAllowance: proposal.ProposedAllowance,
            adjustmentReason: proposal.Reason,
            isCeoOverride: false,
            ceoOverrideCode: null,
            digitalSignatureHash: signatureHash,
            signerName: "Nguyễn Thị Thanh Huyền",
            signerTitle: "Giám Đốc Khối Quản Trị Nguồn Nhân Lực (C&B)");

        await _dbContext.ElectronicContractAppendices.AddAsync(appendix, cancellationToken);

        // 4. Ghi vết AuditLog chuẩn C&B
        var audit = new AuditLog(
            tableName: "Employees_Salaries",
            action: "SALARY_PROMOTION_STANDARD_APPROVED",
            entityId: employee.Id.ToString(),
            oldValues: JsonSerializer.Serialize(new { BaseSalary = proposal.CurrentSalary, Allowance = proposal.CurrentAllowance }),
            newValues: JsonSerializer.Serialize(new { BaseSalary = proposal.ProposedSalary, Allowance = proposal.ProposedAllowance, Appendix = appendixNumber }),
            performedBy: $"HR_DIRECTOR_ID_{hrDirectorId}");

        await _dbContext.AuditLogs.AddAsync(audit, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectProposalAsync(int proposalId, int reviewerId, string rejectionReason, CancellationToken cancellationToken = default)
    {
        var proposal = await _dbContext.SalaryProposals.FirstOrDefaultAsync(p => p.Id == proposalId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy tờ trình đề xuất Id={proposalId}.", "ERR_SAL_NOT_FOUND");

        proposal.Reject(reviewerId, rejectionReason);

        var audit = new AuditLog(
            tableName: "SalaryProposals",
            action: "REJECT_SALARY_PROPOSAL",
            entityId: proposal.Id.ToString(),
            oldValues: null,
            newValues: JsonSerializer.Serialize(new { ReviewerId = reviewerId, Reason = rejectionReason }),
            performedBy: $"REVIEWER_ID_{reviewerId}");

        await _dbContext.AuditLogs.AddAsync(audit, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Giám đốc ĐẶC CÁCH tăng lương (CEO Fast-Track Executive Override).
    /// Bỏ qua toàn bộ quy trình thẩm định C&B, tự động cập nhật mức lương mới vào hồ sơ nhân viên,
    /// sinh phụ lục hợp đồng điện tử có chữ ký số CEO, và ghi vết AuditLog bảo mật cao.
    /// </summary>
    public async Task<ElectronicContractAppendixDto> FastTrackExecutiveOverrideAsync(FastTrackCeoOverrideRequest request, CancellationToken cancellationToken = default)
    {
        var proposal = await _dbContext.SalaryProposals
            .Include(p => p.Employee)
            .FirstOrDefaultAsync(p => p.Id == request.ProposalId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy tờ trình đề xuất Id={request.ProposalId}.", "ERR_SAL_NOT_FOUND");

        var employee = proposal.Employee ?? await _employeeRepository.GetByIdAsync(proposal.EmployeeId, cancellationToken)
            ?? throw new DomainException("Không tìm thấy hồ sơ nhân sự để đặc cách tăng lương.", "ERR_EMP_NOT_FOUND");

        string overrideCode = string.IsNullOrWhiteSpace(request.OverrideCode)
            ? $"CEO-FASTTRACK-{DateTime.UtcNow:yyyyMMdd-HHmm}"
            : request.OverrideCode.Trim();

        // 1. Kích hoạt Fast-track Override trên đề xuất
        proposal.FastTrackCeoOverride(request.CeoEmployeeId, request.Justification, overrideCode);

        // 2. Cập nhật hồ sơ lương nhân viên ngay lập tức (Bỏ qua thẩm định C&B)
        employee.PromoteSalary(proposal.ProposedSalary, proposal.ProposedAllowance, $"CEO ĐẶC CÁCH: {request.Justification}");

        // 3. Tự động sinh Phụ lục Hợp đồng Lao động Điện tử đặc cách
        var contract = await _contractRepository.GetActiveContractByEmployeeIdAsync(employee.Id, cancellationToken);
        string appendixNumber = $"PLHD-EXECUTIVE-{DateTime.UtcNow:yyyyMMdd}-{employee.EmployeeCode}";
        string rawSignature = $"CEO_OVERRIDE_{overrideCode}_{employee.EmployeeCode}_{proposal.ProposedSalary}_{DateTime.UtcNow:O}_VINAMILK_EXECUTIVE_SECRET";
        string digitalSignatureHash = ComputeSha256(rawSignature);

        var appendix = new ElectronicContractAppendix(
            appendixNumber: appendixNumber,
            laborContractId: contract?.Id ?? 1,
            employeeId: employee.Id,
            effectiveDate: proposal.EffectiveDate,
            signDate: DateTime.UtcNow,
            previousSalary: proposal.CurrentSalary,
            newSalary: proposal.ProposedSalary,
            previousAllowance: proposal.CurrentAllowance,
            newAllowance: proposal.ProposedAllowance,
            adjustmentReason: $"[CEO ĐẶC CÁCH] {request.Justification}",
            isCeoOverride: true,
            ceoOverrideCode: overrideCode,
            digitalSignatureHash: digitalSignatureHash,
            signerName: "Mai Kiều Liên",
            signerTitle: "Tổng Giám Đốc (CEO) - Vinamilk");

        await _dbContext.ElectronicContractAppendices.AddAsync(appendix, cancellationToken);

        // 4. Ghi vết AuditLog BẢO MẬT CAO (High-Security Audit Trail)
        var audit = new AuditLog(
            tableName: "Employees_Salaries",
            action: "CEO_FAST_TRACK_SALARY_OVERRIDE",
            entityId: employee.Id.ToString(),
            oldValues: JsonSerializer.Serialize(new { BaseSalary = proposal.CurrentSalary, Allowance = proposal.CurrentAllowance }),
            newValues: JsonSerializer.Serialize(new
            {
                BaseSalary = proposal.ProposedSalary,
                Allowance = proposal.ProposedAllowance,
                OverrideCode = overrideCode,
                Justification = request.Justification,
                AppendixNumber = appendixNumber,
                DigitalSignatureHash = digitalSignatureHash,
                SecurityLevel = "CONFIDENTIAL_EXECUTIVE_OVERRIDE"
            }),
            performedBy: $"CEO_EXECUTIVE_OVERRIDE (EmpId: {request.CeoEmployeeId})",
            ipAddress: request.IpAddress ?? "10.0.0.1 (Vinamilk Executive HQ Network)");

        await _dbContext.AuditLogs.AddAsync(audit, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToAppendixDto(appendix, employee, contract?.ContractNumber ?? "HDLD-VNM-STANDARD");
    }

    public async Task<ElectronicContractAppendixDto> CreateDirectCeoOverrideAsync(DirectCeoOverrideRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy nhân viên Id={request.EmployeeId}.", "ERR_EMP_NOT_FOUND");

        DateTime effectiveDate = request.EffectiveDate ?? DateTime.UtcNow;
        string overrideCode = string.IsNullOrWhiteSpace(request.OverrideCode)
            ? $"CEO-DIRECT-{DateTime.UtcNow:yyyyMMdd-HHmm}"
            : request.OverrideCode.Trim();

        // 1. Tạo và phê duyệt đề xuất tức thời
        var proposal = new SalaryProposal(
            employeeId: employee.Id,
            proposedById: request.CeoEmployeeId,
            currentSalary: employee.BaseSalary,
            proposedSalary: request.NewSalary,
            currentAllowance: employee.Allowance,
            proposedAllowance: request.NewAllowance,
            reason: request.Justification,
            effectiveDate: effectiveDate);

        proposal.FastTrackCeoOverride(request.CeoEmployeeId, request.Justification, overrideCode);
        await _dbContext.SalaryProposals.AddAsync(proposal, cancellationToken);

        // 2. Cập nhật hồ sơ nhân viên
        decimal oldSalary = employee.BaseSalary;
        decimal oldAllowance = employee.Allowance;
        employee.PromoteSalary(request.NewSalary, request.NewAllowance, $"CEO ĐẶC CÁCH TRỰC TIẾP: {request.Justification}");

        // 3. Sinh Phụ lục hợp đồng số
        var contract = await _contractRepository.GetActiveContractByEmployeeIdAsync(employee.Id, cancellationToken);
        string appendixNumber = $"PLHD-DIRECT-CEO-{DateTime.UtcNow:yyyyMMdd}-{employee.EmployeeCode}";
        string rawSignature = $"DIRECT_CEO_KEY_{overrideCode}_{employee.EmployeeCode}_{request.NewSalary}_{DateTime.UtcNow:O}";
        string digitalSignatureHash = ComputeSha256(rawSignature);

        var appendix = new ElectronicContractAppendix(
            appendixNumber: appendixNumber,
            laborContractId: contract?.Id ?? 1,
            employeeId: employee.Id,
            effectiveDate: effectiveDate,
            signDate: DateTime.UtcNow,
            previousSalary: oldSalary,
            newSalary: request.NewSalary,
            previousAllowance: oldAllowance,
            newAllowance: request.NewAllowance,
            adjustmentReason: $"[CEO ĐẶC CÁCH TRỰC TIẾP] {request.Justification}",
            isCeoOverride: true,
            ceoOverrideCode: overrideCode,
            digitalSignatureHash: digitalSignatureHash,
            signerName: "Mai Kiều Liên",
            signerTitle: "Tổng Giám Đốc (CEO) - Vinamilk");

        await _dbContext.ElectronicContractAppendices.AddAsync(appendix, cancellationToken);

        // 4. Ghi vết AuditLog bảo mật cao
        var audit = new AuditLog(
            tableName: "Employees_Salaries",
            action: "CEO_DIRECT_SALARY_OVERRIDE",
            entityId: employee.Id.ToString(),
            oldValues: JsonSerializer.Serialize(new { BaseSalary = oldSalary, Allowance = oldAllowance }),
            newValues: JsonSerializer.Serialize(new
            {
                BaseSalary = request.NewSalary,
                Allowance = request.NewAllowance,
                OverrideCode = overrideCode,
                Justification = request.Justification,
                AppendixNumber = appendixNumber,
                DigitalSignatureHash = digitalSignatureHash,
                SecurityLevel = "CONFIDENTIAL_EXECUTIVE_OVERRIDE"
            }),
            performedBy: $"CEO_DIRECT_ACTION (EmpId: {request.CeoEmployeeId})",
            ipAddress: request.IpAddress ?? "10.0.0.1 (Vinamilk Executive HQ Network)");

        await _dbContext.AuditLogs.AddAsync(audit, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToAppendixDto(appendix, employee, contract?.ContractNumber ?? "HDLD-VNM-STANDARD");
    }

    public async Task<IReadOnlyList<ElectronicContractAppendixDto>> GetAppendicesByEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.ElectronicContractAppendices
            .Include(a => a.Employee)
            .Include(a => a.LaborContract)
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.SignDate)
            .ToListAsync(cancellationToken);

        return list.Select(a => MapToAppendixDto(a, a.Employee, a.LaborContract?.ContractNumber)).ToList();
    }

    public async Task<IReadOnlyList<ElectronicContractAppendixDto>> GetAllAppendicesAsync(CancellationToken cancellationToken = default)
    {
        var list = await _dbContext.ElectronicContractAppendices
            .Include(a => a.Employee)
            .Include(a => a.LaborContract)
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return list.Select(a => MapToAppendixDto(a, a.Employee, a.LaborContract?.ContractNumber)).ToList();
    }

    private static string ComputeSha256(string rawData)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToHexString(bytes);
    }

    private static SalaryProposalDto MapToProposalDto(SalaryProposal p, Employee? emp = null)
    {
        var employee = emp ?? p.Employee;
        decimal increase = p.ProposedSalary - p.CurrentSalary;
        decimal pct = p.CurrentSalary > 0 ? Math.Round((increase / p.CurrentSalary) * 100m, 1) : 0m;

        return new SalaryProposalDto(
            p.Id,
            p.EmployeeId,
            employee?.EmployeeCode ?? "N/A",
            employee?.FullName ?? "N/A",
            employee?.OrganizationalUnit?.UnitName ?? "Vinamilk",
            p.ProposedById,
            p.ProposedBy?.FullName ?? "Quản lý Đơn vị",
            p.CurrentSalary,
            p.ProposedSalary,
            p.CurrentAllowance,
            p.ProposedAllowance,
            increase,
            pct,
            p.Reason,
            p.EffectiveDate,
            p.Status,
            p.DepartmentHeadApprovedById,
            p.DepartmentHeadApprovedById.HasValue ? "Trưởng Bộ Phận" : null,
            p.DepartmentHeadNotes,
            p.HRDirectorApprovedById,
            p.HRDirectorApprovedById.HasValue ? "Giám Đốc Nhân Sự / CEO" : null,
            p.HRDirectorNotes,
            p.FinalDecisionDate,
            p.CreatedAt);
    }

    private static ElectronicContractAppendixDto MapToAppendixDto(ElectronicContractAppendix a, Employee? emp = null, string? contractNum = null)
    {
        var employee = emp ?? a.Employee;
        decimal increase = a.NewSalary - a.PreviousSalary;

        return new ElectronicContractAppendixDto(
            a.Id,
            a.AppendixNumber,
            a.LaborContractId,
            contractNum ?? a.LaborContract?.ContractNumber ?? "HDLD-VNM-STANDARD",
            a.EmployeeId,
            employee?.EmployeeCode ?? "N/A",
            employee?.FullName ?? "N/A",
            a.EffectiveDate,
            a.SignDate,
            a.PreviousSalary,
            a.NewSalary,
            increase,
            a.PreviousAllowance,
            a.NewAllowance,
            a.AdjustmentReason,
            a.IsCeoOverride,
            a.CeoOverrideCode,
            a.DigitalSignatureHash,
            a.SignerName,
            a.SignerTitle,
            a.IsSigned,
            a.CreatedAt);
    }
}
