using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Contracts;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Interfaces.Contracts;
using VinamilkHRM.Domain.Interfaces.Notifications;
using VinamilkHRM.Domain.Services.Contracts;

namespace VinamilkHRM.Application.Contracts;

public class CreateContractDto
{
    public string ContractNumber { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public ContractType ContractType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal InsuranceSalary { get; set; }
    public DateTime SignDate { get; set; }
    public int DurationMonths { get; set; } = 12;
    public bool IsExempted { get; set; } = false;
    public string? ExemptionReason { get; set; }
}

public class ContractExpirationNoticeDto
{
    public int ContractId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
    public int DaysRemaining { get; set; }
    public string AlertLevel { get; set; } = string.Empty; // "60_DAYS", "30_DAYS", "7_DAYS"
}

public interface IContractManagementService
{
    Task<LaborContract> CreateContractAsync(CreateContractDto dto, CancellationToken ct = default);
    Task<List<ContractExpirationNoticeDto>> CheckAndNotifyExpiringContractsAsync(DateTime checkDate, CancellationToken ct = default);
    Task<byte[]> GenerateContractDocxAsync(int contractId, CancellationToken ct = default);
    Task<ESignResponse> SendContractForESignAsync(int contractId, CancellationToken ct = default);
}

public class ContractManagementService : IContractManagementService
{
    private readonly IApplicationDbContext _context;
    private readonly IContractDocumentGenerator _documentGenerator;
    private readonly IESignProvider _eSignProvider;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ContractManagementService> _logger;

    public ContractManagementService(
        IApplicationDbContext context,
        IContractDocumentGenerator documentGenerator,
        IESignProvider eSignProvider,
        INotificationService notificationService,
        ILogger<ContractManagementService> logger)
    {
        _context = context;
        _documentGenerator = documentGenerator;
        _eSignProvider = eSignProvider;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<LaborContract> CreateContractAsync(CreateContractDto dto, CancellationToken ct = default)
    {
        var employee = await _context.Employees
            .Include(e => e.StandardJobTitle)
            .Include(e => e.OrganizationalUnit)
            .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId, ct);

        if (employee == null)
            throw new DomainException($"Không tìm thấy nhân viên ID {dto.EmployeeId}.", "ERR_CONTRACT_EMP_NOT_FOUND");

        // Lấy danh sách hợp đồng trước đây của nhân viên
        var previousContracts = await _context.LaborContracts
            .Where(c => c.EmployeeId == dto.EmployeeId)
            .ToListAsync(ct);

        // Kiểm tra chặn ký hợp đồng xác định thời hạn lần thứ 3 (Tiêu chí 3)
        ContractRuleValidator.ValidateDefiniteContractLimit(
            employee,
            dto.ContractType,
            previousContracts,
            dto.IsExempted,
            dto.ExemptionReason);

        LaborContract newContract;
        if (dto.ContractType == ContractType.Definite)
        {
            DateTime endDate = dto.EndDate ?? dto.StartDate.AddMonths(dto.DurationMonths).AddDays(-1);
            newContract = new DefiniteContract(
                dto.ContractNumber,
                dto.EmployeeId,
                dto.StartDate,
                endDate,
                dto.BasicSalary,
                dto.InsuranceSalary,
                dto.SignDate,
                dto.DurationMonths);
        }
        else if (dto.ContractType == ContractType.Indefinite)
        {
            newContract = new IndefiniteContract(
                dto.ContractNumber,
                dto.EmployeeId,
                dto.StartDate,
                dto.BasicSalary,
                dto.InsuranceSalary,
                dto.SignDate,
                severancePackageTerms: "Theo chế độ trợ cấp thôi việc hiện hành của Tập đoàn Vinamilk");
        }
        else
        {
            DateTime endDate = dto.EndDate ?? dto.StartDate.AddMonths(2).AddDays(-1);
            newContract = new ProbationContract(
                dto.ContractNumber,
                dto.EmployeeId,
                dto.StartDate,
                endDate,
                dto.BasicSalary,
                dto.InsuranceSalary,
                dto.SignDate,
                mentorEmployeeId: null);
        }

        _context.LaborContracts.Add(newContract);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Đã khởi tạo hợp đồng {ContractNumber} ({Type}) cho nhân viên {EmpId}.", 
            newContract.ContractNumber, newContract.ContractType, employee.Id);

        return newContract;
    }

    public async Task<List<ContractExpirationNoticeDto>> CheckAndNotifyExpiringContractsAsync(DateTime checkDate, CancellationToken ct = default)
    {
        var activeContracts = await _context.LaborContracts
            .Include(c => c.Employee)
            .Where(c => c.Status == ContractStatus.Active && c.EndDate.HasValue)
            .ToListAsync(ct);

        var notices = new List<ContractExpirationNoticeDto>();

        foreach (var contract in activeContracts)
        {
            int daysRemaining = (contract.EndDate!.Value.Date - checkDate.Date).Days;

            // Nhắc hết hạn 60/30/7 ngày
            if (daysRemaining == 60 || daysRemaining == 30 || daysRemaining == 7)
            {
                string alertLevel = $"{daysRemaining}_DAYS";
                var notice = new ContractExpirationNoticeDto
                {
                    ContractId = contract.Id,
                    ContractNumber = contract.ContractNumber,
                    EmployeeId = contract.EmployeeId,
                    EmployeeName = contract.Employee?.FullName ?? "",
                    EndDate = contract.EndDate.Value,
                    DaysRemaining = daysRemaining,
                    AlertLevel = alertLevel
                };
                notices.Add(notice);

                // Gửi thông báo tự động qua Notification Engine (Transactional Outbox)
                var parameters = new Dictionary<string, string>
                {
                    { "ContractNumber", contract.ContractNumber },
                    { "EmployeeName", contract.Employee?.FullName ?? "" },
                    { "DaysRemaining", daysRemaining.ToString() },
                    { "EndDate", contract.EndDate.Value.ToString("dd/MM/yyyy") }
                };

                await _notificationService.QueueNotificationAsync(
                    contract.EmployeeId,
                    "NOTIF_CONTRACT_EXPIRING",
                    parameters,
                    targetUrl: $"/contracts/{contract.Id}",
                    ct: ct);
            }
        }

        return notices;
    }

    public async Task<byte[]> GenerateContractDocxAsync(int contractId, CancellationToken ct = default)
    {
        var contract = await _context.LaborContracts
            .Include(c => c.Employee)
                .ThenInclude(e => e!.StandardJobTitle)
            .Include(c => c.Employee)
                .ThenInclude(e => e!.OrganizationalUnit)
            .FirstOrDefaultAsync(c => c.Id == contractId, ct);

        if (contract == null || contract.Employee == null)
            throw new DomainException($"Không tìm thấy hợp đồng ID {contractId}.", "ERR_CONTRACT_NOT_FOUND");

        return _documentGenerator.GenerateDocxContract(contract, contract.Employee);
    }

    public async Task<ESignResponse> SendContractForESignAsync(int contractId, CancellationToken ct = default)
    {
        var contract = await _context.LaborContracts
            .Include(c => c.Employee)
            .FirstOrDefaultAsync(c => c.Id == contractId, ct);

        if (contract == null || contract.Employee == null)
            throw new DomainException($"Không tìm thấy hợp đồng ID {contractId}.", "ERR_CONTRACT_NOT_FOUND");

        byte[] docBytes = await GenerateContractDocxAsync(contractId, ct);

        var eSignRequest = new ESignRequest
        {
            DocumentId = contract.ContractNumber,
            DocumentTitle = $"Hợp đồng Lao động Vinamilk - {contract.ContractNumber} - {contract.Employee.FullName}",
            DocumentBytes = docBytes,
            Signers = new List<SignerInfo>
            {
                new SignerInfo
                {
                    SignerName = contract.Employee.FullName,
                    Email = contract.Employee.Email,
                    PhoneNumber = contract.Employee.PhoneNumber,
                    CitizenId = contract.Employee.CitizenId,
                    SignOrder = 1,
                    Role = "Employee"
                },
                new SignerInfo
                {
                    SignerName = "Mai Kiều Liên",
                    Email = "ceo@vinamilk.com.vn",
                    PhoneNumber = "02854155555",
                    SignOrder = 2,
                    Role = "EmployerRepresentative"
                }
            }
        };

        return await _eSignProvider.SendDocumentForSigningAsync(eSignRequest, ct);
    }
}