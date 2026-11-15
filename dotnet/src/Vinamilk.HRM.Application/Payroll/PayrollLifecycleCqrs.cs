using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Helpers;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Workflows;
using Vinamilk.HRM.Domain.Entities.Payroll;
using Vinamilk.HRM.Domain.Entities.SystemEntities;

namespace Vinamilk.HRM.Application.Payroll;

public sealed record SubmitPayrollPeriodCommand(int PayrollPeriodId, string? Comment = null) : IRequest<int>;

public sealed class SubmitPayrollPeriodCommandHandler : IRequestHandler<SubmitPayrollPeriodCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender _sender;

    public SubmitPayrollPeriodCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<int> Handle(SubmitPayrollPeriodCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUser.RequireUserId();
        var actorUsername = _currentUser.RequireUsername();

        var period = await _context.PayrollPeriods
            .Include(p => p.PayrollRecords)
            .FirstOrDefaultAsync(p => p.Id == request.PayrollPeriodId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy kỳ lương #{request.PayrollPeriodId}.");

        if (!string.Equals(period.Status, PayrollPeriodStatuses.Calculated, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Chỉ có thể trình duyệt khi kỳ lương đang ở trạng thái 'Calculated' (hiện tại: '{period.Status}').");
        }

        if (period.PayrollRecords.Count == 0)
            throw new InvalidOperationException($"Kỳ lương {period.PeriodCode} chưa có dữ liệu bảng lương nào.");

        var initiatorEmpId = _currentUser.EmployeeId;
        if (!initiatorEmpId.HasValue || initiatorEmpId.Value <= 0)
        {
            initiatorEmpId = await _context.Employees
                .AsNoTracking()
                .OrderBy(e => e.Id)
                .Select(e => (int?)e.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (!initiatorEmpId.HasValue)
            throw new InvalidOperationException("Không tìm thấy hồ sơ nhân sự để khởi tạo luồng phê duyệt kỳ lương.");

        var workflowInstanceId = await _sender.Send(new StartWorkflowInstanceCommand(
            EntityType: "PayrollPeriod",
            EntityId: period.Id,
            InitiatorEmployeeId: initiatorEmpId.Value,
            DocumentTitle: $"Bảng lương {period.PeriodTitle} ({period.PeriodCode})",
            TargetUrl: $"/Payroll?periodId={period.Id}"), cancellationToken);

        period.Status = PayrollPeriodStatuses.Submitted;
        period.SubmittedByUserId = actorUserId;
        period.SubmittedAt = DateTime.UtcNow;
        period.WorkflowInstanceId = workflowInstanceId;

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = actorUserId,
            Username = actorUsername,
            ModuleName = "Payroll",
            ActionType = "PayrollPeriod_Submit",
            EntityName = nameof(PayrollPeriod),
            EntityId = period.Id.ToString(),
            OldValuesJson = JsonSerializer.Serialize(new { Status = PayrollPeriodStatuses.Calculated }),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                Status = PayrollPeriodStatuses.Submitted,
                WorkflowInstanceId = workflowInstanceId,
                EmployeeCount = period.PayrollRecords.Count,
                TotalNet = period.PayrollRecords.Sum(r => r.NetSalary),
                request.Comment
            }),
            AffectedColumns = "Status,SubmittedByUserId,SubmittedAt,WorkflowInstanceId",
            IpAddress = _currentUser.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser.UserAgent ?? "PayrollWeb",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorUsername
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return workflowInstanceId;
    }
}

public sealed record ApprovePayrollPeriodCommand(
    int PayrollPeriodId,
    bool Approve = true,
    string? Comment = null) : IRequest<string>;

public sealed class ApprovePayrollPeriodCommandHandler : IRequestHandler<ApprovePayrollPeriodCommand, string>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISender _sender;

    public ApprovePayrollPeriodCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ISender sender)
    {
        _context = context;
        _currentUser = currentUser;
        _sender = sender;
    }

    public async Task<string> Handle(ApprovePayrollPeriodCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUser.RequireUserId();
        var actorUsername = _currentUser.RequireUsername();

        var period = await _context.PayrollPeriods
            .FirstOrDefaultAsync(p => p.Id == request.PayrollPeriodId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy kỳ lương #{request.PayrollPeriodId}.");

        if (!string.Equals(period.Status, PayrollPeriodStatuses.Submitted, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Chỉ có thể phê duyệt/từ chối khi kỳ lương ở trạng thái 'Submitted' (hiện tại: '{period.Status}').");
        }

        if (period.CalculatedByUserId == actorUserId || period.SubmittedByUserId == actorUserId)
        {
            throw new InvalidOperationException(
                "Vi phạm phân tách nhiệm vụ: người tính hoặc trình bảng lương không được tự phê duyệt kỳ lương.");
        }

        if (period.WorkflowInstanceId.HasValue)
        {
            var inst = await _context.WorkflowInstances
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == period.WorkflowInstanceId.Value, cancellationToken);

            if (inst is not null && inst.Status == "InProgress")
            {
                return await _sender.Send(new ProcessWorkflowActionCommand(
                    inst.Id,
                    request.Approve ? "Approved" : "Rejected",
                    request.Comment ?? (request.Approve ? "Phê duyệt bảng lương" : "Trả lại bảng lương để kiểm tra")),
                    cancellationToken);
            }
        }

        var nextStatus = request.Approve ? PayrollPeriodStatuses.Approved : PayrollPeriodStatuses.Calculated;
        period.Status = nextStatus;
        if (request.Approve)
        {
            period.ApprovedByUserId = actorUserId;
            period.ApprovedAt = DateTime.UtcNow;
        }

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = actorUserId,
            Username = actorUsername,
            ModuleName = "Payroll",
            ActionType = request.Approve ? "PayrollPeriod_Approve" : "PayrollPeriod_Reject",
            EntityName = nameof(PayrollPeriod),
            EntityId = period.Id.ToString(),
            OldValuesJson = JsonSerializer.Serialize(new { Status = PayrollPeriodStatuses.Submitted }),
            NewValuesJson = JsonSerializer.Serialize(new { Status = nextStatus, request.Comment }),
            AffectedColumns = "Status,ApprovedByUserId,ApprovedAt",
            IpAddress = _currentUser.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser.UserAgent ?? "PayrollWeb",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorUsername
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return request.Approve ? "Đã phê duyệt kỳ lương." : "Đã trả lại kỳ lương về trạng thái Calculated.";
    }
}

public sealed record UnlockPayrollPeriodCommand(int PayrollPeriodId, string Reason) : IRequest<bool>;

public sealed class UnlockPayrollPeriodCommandHandler : IRequestHandler<UnlockPayrollPeriodCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UnlockPayrollPeriodCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(UnlockPayrollPeriodCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUser.RequireUserId();
        var actorUsername = _currentUser.RequireUsername();

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Bắt buộc phải nhập lý do khi mở khóa kỳ lương.");

        var period = await _context.PayrollPeriods
            .FirstOrDefaultAsync(p => p.Id == request.PayrollPeriodId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy kỳ lương #{request.PayrollPeriodId}.");

        if (string.Equals(period.Status, PayrollPeriodStatuses.Paid, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Kỳ lương đã xác nhận chi trả (Paid), không được phép mở khóa.");

        if (!period.IsLocked && !string.Equals(period.Status, PayrollPeriodStatuses.Locked, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Kỳ lương {period.PeriodCode} hiện không ở trạng thái 'Locked'.");

        var oldStatus = period.Status;
        var reason = request.Reason.Trim();

        period.IsLocked = false;
        period.Status = PayrollPeriodStatuses.Calculated;
        period.UnlockedByUserId = actorUserId;
        period.UnlockedAt = DateTime.UtcNow;
        period.LastUnlockReason = reason;

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = actorUserId,
            Username = actorUsername,
            ModuleName = "Payroll",
            ActionType = "PayrollPeriod_Unlock",
            EntityName = nameof(PayrollPeriod),
            EntityId = period.Id.ToString(),
            OldValuesJson = JsonSerializer.Serialize(new { Status = oldStatus, IsLocked = true }),
            NewValuesJson = JsonSerializer.Serialize(new { Status = period.Status, IsLocked = false, Reason = reason }),
            AffectedColumns = "Status,IsLocked,UnlockedByUserId,UnlockedAt,LastUnlockReason",
            IpAddress = _currentUser.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser.UserAgent ?? "PayrollWeb",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorUsername
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed record MarkPayrollPeriodPaidCommand(int PayrollPeriodId, string? PaymentNote = null) : IRequest<bool>;

public sealed class MarkPayrollPeriodPaidCommandHandler : IRequestHandler<MarkPayrollPeriodPaidCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public MarkPayrollPeriodPaidCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<bool> Handle(MarkPayrollPeriodPaidCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUser.RequireUserId();
        var actorUsername = _currentUser.RequireUsername();

        var period = await _context.PayrollPeriods
            .Include(p => p.PayrollRecords)
            .FirstOrDefaultAsync(p => p.Id == request.PayrollPeriodId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy kỳ lương #{request.PayrollPeriodId}.");

        if (!string.Equals(period.Status, PayrollPeriodStatuses.Locked, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Chỉ có thể xác nhận chi trả (Paid) sau khi kỳ lương đã được khóa 'Locked' (hiện tại: '{period.Status}').");
        }

        var now = DateTime.UtcNow;

        period.Status = PayrollPeriodStatuses.Paid;
        period.IsLocked = true;
        period.PaidByUserId = actorUserId;
        period.PaidAt = now;

        foreach (var rec in period.PayrollRecords)
        {
            rec.PaymentStatus = PayrollPeriodStatuses.Paid;
            rec.PaidAt = now;
        }

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = actorUserId,
            Username = actorUsername,
            ModuleName = "Payroll",
            ActionType = "PayrollPeriod_MarkPaid",
            EntityName = nameof(PayrollPeriod),
            EntityId = period.Id.ToString(),
            OldValuesJson = JsonSerializer.Serialize(new { Status = PayrollPeriodStatuses.Locked }),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                Status = PayrollPeriodStatuses.Paid,
                PaidCount = period.PayrollRecords.Count,
                TotalNet = period.PayrollRecords.Sum(r => r.NetSalary),
                request.PaymentNote
            }),
            AffectedColumns = "Status,PaidByUserId,PaidAt,PayrollRecords.PaymentStatus",
            IpAddress = _currentUser.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser.UserAgent ?? "PayrollWeb",
            CreatedAt = now,
            CreatedBy = actorUsername
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public sealed record SendPayslipsBatchCommand(int PayrollPeriodId) : IRequest<PayslipDispatchBatchReportDto>;

public sealed class SendPayslipsBatchCommandHandler : IRequestHandler<SendPayslipsBatchCommand, PayslipDispatchBatchReportDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IPayslipBatchDispatcher _dispatcher;

    public SendPayslipsBatchCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IPayslipBatchDispatcher dispatcher)
    {
        _context = context;
        _currentUser = currentUser;
        _dispatcher = dispatcher;
    }

    public async Task<PayslipDispatchBatchReportDto> Handle(SendPayslipsBatchCommand request, CancellationToken cancellationToken)
    {
        var actorUserId = _currentUser.RequireUserId();
        var actorUsername = _currentUser.RequireUsername();

        var period = await _context.PayrollPeriods
            .FirstOrDefaultAsync(p => p.Id == request.PayrollPeriodId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy kỳ lương #{request.PayrollPeriodId}.");

        if (period.Status is not (PayrollPeriodStatuses.Approved or PayrollPeriodStatuses.Locked or PayrollPeriodStatuses.Paid))
        {
            throw new InvalidOperationException(
                $"Chỉ được gửi phiếu lương hàng loạt sau khi kỳ lương đã 'Approved' / 'Locked' / 'Paid' (hiện tại: '{period.Status}').");
        }

        var records = await _context.PayrollRecords
            .AsNoTracking()
            .Include(r => r.Employee)
            .Where(r => r.PayrollPeriodId == period.Id)
            .OrderBy(r => r.Employee.EmployeeCode)
            .ToListAsync(cancellationToken);

        if (records.Count == 0)
            throw new InvalidOperationException($"Kỳ lương {period.PeriodCode} chưa có phiếu lương để gửi.");

        var batchId = Guid.NewGuid();
        var logs = records.Select(r => new PayslipDispatchLog
        {
            BatchId = batchId,
            PayrollPeriodId = period.Id,
            PayrollRecordId = r.Id,
            EmployeeId = r.EmployeeId,
            EmployeeCode = r.Employee.EmployeeCode,
            EmployeeName = r.Employee.FullName,
            RecipientEmail = r.Employee.WorkEmail ?? r.Employee.PersonalEmail ?? string.Empty,
            Status = "Queued",
            AttemptCount = 0
        }).ToList();

        await _context.PayslipDispatchLogs.AddRangeAsync(logs, cancellationToken);
        period.PayslipSentAt = DateTime.UtcNow;

        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = actorUserId,
            Username = actorUsername,
            ModuleName = "Payroll",
            ActionType = "PayrollPeriod_DispatchPayslips",
            EntityName = nameof(PayrollPeriod),
            EntityId = period.Id.ToString(),
            OldValuesJson = null,
            NewValuesJson = JsonSerializer.Serialize(new { BatchId = batchId, RecipientCount = logs.Count }),
            AffectedColumns = "PayslipSentAt",
            IpAddress = _currentUser.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUser.UserAgent ?? "PayrollWeb",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = actorUsername
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await _dispatcher.EnqueueBatchAsync(period.Id, logs.Select(l => l.Id).ToList(), cancellationToken);

        var updatedLogs = await _context.PayslipDispatchLogs
            .AsNoTracking()
            .Where(l => l.BatchId == batchId)
            .OrderBy(l => l.EmployeeCode)
            .ToListAsync(cancellationToken);

        return new PayslipDispatchBatchReportDto(
            batchId,
            updatedLogs.Count,
            updatedLogs.Count(x => x.Status == "Queued"),
            updatedLogs.Count(x => x.Status == "Sent"),
            updatedLogs.Count(x => x.Status == "Failed"),
            updatedLogs.Select(x => new PayslipDispatchItemDto(
                x.Id,
                x.EmployeeCode,
                x.EmployeeName,
                x.RecipientEmail,
                x.Status,
                x.AttemptCount,
                x.ErrorMessage,
                x.SentAt)).ToList());
    }
}

public sealed record GetPayrollRecordPdfQuery(long PayrollRecordId, bool EnforceSelfOnly = false)
    : IRequest<PayslipPdfResult>;

public sealed class GetPayrollRecordPdfQueryHandler : IRequestHandler<GetPayrollRecordPdfQuery, PayslipPdfResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IPayrollDocumentService _documentService;

    public GetPayrollRecordPdfQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IPayrollDocumentService documentService)
    {
        _context = context;
        _currentUser = currentUser;
        _documentService = documentService;
    }

    public async Task<PayslipPdfResult> Handle(GetPayrollRecordPdfQuery request, CancellationToken cancellationToken)
    {
        var record = await _context.PayrollRecords
            .AsNoTracking()
            .Include(r => r.PayrollPeriod)
            .Include(r => r.OrganizationUnit)
            .Include(r => r.Position)
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == request.PayrollRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy phiếu lương #{request.PayrollRecordId}.");

        if (request.EnforceSelfOnly)
        {
            if (!_currentUser.EmployeeId.HasValue || _currentUser.EmployeeId.Value != record.EmployeeId)
                throw new UnauthorizedAccessException("Bạn chỉ được phép xem và tải phiếu lương của chính mình.");
        }
        else
        {
            var scope = await Common.Security.DataScopeResolver.ResolveAsync(_context, _currentUser, cancellationToken);
            scope.EnsureCanAccessEmployee(record.EmployeeId, record.OrganizationUnitId);
        }

        var periodEndDate = record.PayrollPeriod.EndDate != default
            ? record.PayrollPeriod.EndDate
            : new DateOnly(record.PayrollPeriod.Year, record.PayrollPeriod.Month,
                DateTime.DaysInMonth(record.PayrollPeriod.Year, record.PayrollPeriod.Month));

        var statutory = await VietnamPayrollEngine.ResolveStatutoryContextAsync(_context, periodEndDate, cancellationToken);
        return _documentService.GeneratePayslipPdf(record, record.Employee, record.PayrollPeriod, statutory.PitBrackets);
    }
}

public sealed record ExportBankTransferFileQuery(
    int PayrollPeriodId,
    string Format = "excel",
    string TemplateCode = "COMMON") : IRequest<BankTransferExportResult>;

public sealed class ExportBankTransferFileQueryHandler : IRequestHandler<ExportBankTransferFileQuery, BankTransferExportResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IPiiEncryptionService _encryptionService;
    private readonly IEnumerable<IBankTransferTemplateFormatter> _formatters;

    public ExportBankTransferFileQueryHandler(
        IApplicationDbContext context,
        IPiiEncryptionService encryptionService,
        IEnumerable<IBankTransferTemplateFormatter> formatters)
    {
        _context = context;
        _encryptionService = encryptionService;
        _formatters = formatters;
    }

    public async Task<BankTransferExportResult> Handle(ExportBankTransferFileQuery request, CancellationToken cancellationToken)
    {
        var period = await _context.PayrollPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PayrollPeriodId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy kỳ lương #{request.PayrollPeriodId}.");

        if (period.Status is not (PayrollPeriodStatuses.Approved or PayrollPeriodStatuses.Locked or PayrollPeriodStatuses.Paid))
        {
            throw new InvalidOperationException(
                $"Chỉ được phép xuất file chuyển khoản ngân hàng khi kỳ lương đã 'Approved' hoặc 'Locked' (hiện tại: '{period.Status}').");
        }

        var formatter = _formatters.FirstOrDefault(f =>
                            string.Equals(f.TemplateCode, request.TemplateCode, StringComparison.OrdinalIgnoreCase))
                        ?? _formatters.FirstOrDefault(f => f.TemplateCode == "COMMON")
                        ?? throw new InvalidOperationException("Không tìm thấy mẫu định dạng chuyển khoản ngân hàng phù hợp.");

        var records = await _context.PayrollRecords
            .AsNoTracking()
            .Include(r => r.Employee)
            .Where(r => r.PayrollPeriodId == period.Id)
            .OrderBy(r => r.Employee.EmployeeCode)
            .ToListAsync(cancellationToken);

        var rows = records.Select((r, idx) =>
        {
            var accountNo = _encryptionService.Decrypt(r.Employee.BankAccountEncrypted);
            return new BankTransferRowDto(
                idx + 1,
                r.Employee.EmployeeCode,
                string.IsNullOrWhiteSpace(accountNo) ? string.Empty : accountNo.Trim(),
                VietnameseTextHelper.RemoveDiacriticsAndUpper(r.Employee.FullName),
                r.Employee.BankName ?? string.Empty,
                r.Employee.BankBranch ?? string.Empty,
                r.NetSalary,
                VietnameseTextHelper.RemoveDiacriticsAndUpper($"VINAMILK THANH TOAN LUONG T{period.Month:D2}/{period.Year} {r.Employee.EmployeeCode}"));
        }).ToList();

        var isCsv = string.Equals(request.Format, "csv", StringComparison.OrdinalIgnoreCase);
        var bytes = isCsv
            ? formatter.ExportCsv(period, rows)
            : formatter.ExportExcel(period, rows);

        var contentType = isCsv
            ? "text/csv; charset=utf-8"
            : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        var ext = isCsv ? "csv" : "xlsx";
        var fileName = $"ChuyenKhoan_{period.PeriodCode}_{formatter.TemplateCode}.{ext}";

        return new BankTransferExportResult(bytes, contentType, fileName, formatter.TemplateCode, rows.Count, rows.Sum(r => r.Amount));
    }
}

public sealed record GetPayrollDepartmentSummaryReportQuery(int PayrollPeriodId)
    : IRequest<PayrollDepartmentSummaryReportDto>;

public sealed class GetPayrollDepartmentSummaryReportQueryHandler
    : IRequestHandler<GetPayrollDepartmentSummaryReportQuery, PayrollDepartmentSummaryReportDto>
{
    private readonly IApplicationDbContext _context;

    public GetPayrollDepartmentSummaryReportQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PayrollDepartmentSummaryReportDto> Handle(
        GetPayrollDepartmentSummaryReportQuery request,
        CancellationToken cancellationToken)
    {
        var period = await _context.PayrollPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PayrollPeriodId, cancellationToken)
            ?? throw new KeyNotFoundException($"Không tìm thấy kỳ lương #{request.PayrollPeriodId}.");

        var records = await _context.PayrollRecords
            .AsNoTracking()
            .Include(r => r.OrganizationUnit)
            .Where(r => r.PayrollPeriodId == period.Id)
            .ToListAsync(cancellationToken);

        var rows = records
            .GroupBy(r => new { r.OrganizationUnit.UnitCode, r.OrganizationUnit.UnitName })
            .Select(g => new PayrollDepartmentSummaryRowDto(
                g.Key.UnitCode,
                g.Key.UnitName,
                g.Count(),
                g.Sum(x => x.ActualBaseSalary + x.PositionAllowance + x.OtherAllowance),
                g.Sum(x => x.OvertimePay + x.KpiBonus),
                g.Sum(x => x.GrossIncome),
                g.Sum(x => x.TotalEmployeeInsurance),
                g.Sum(x => x.CompanySocialInsurance + x.CompanyHealthInsurance + x.CompanyUnemploymentInsurance),
                g.Sum(x => x.CompanyUnionFee),
                g.Sum(x => x.PersonalIncomeTax),
                g.Sum(x => x.NetSalary)))
            .OrderByDescending(r => r.TotalGross)
            .ToList();

        return new PayrollDepartmentSummaryReportDto(
            period.Id,
            period.PeriodCode,
            period.PeriodTitle,
            period.EndDate,
            period.Status,
            records.Count,
            rows,
            rows.Sum(r => r.TotalGross),
            rows.Sum(r => r.TotalEmployeeInsurance),
            rows.Sum(r => r.TotalCompanyInsurance),
            rows.Sum(r => r.TotalCompanyUnionFee),
            rows.Sum(r => r.TotalPit),
            rows.Sum(r => r.TotalNet));
    }
}

