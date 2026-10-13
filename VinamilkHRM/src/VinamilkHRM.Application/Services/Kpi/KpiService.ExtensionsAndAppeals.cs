using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.DTOs.Kpis;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Services;

public partial class KpiService
{
    public async Task<KpiExtensionRequestDto> RequestExtensionAsync(RequestKpiExtensionInput input, CancellationToken ct = default)
    {
        var item = await _context.KpiItems
            .Include(i => i.ExtensionRequests)
            .FirstOrDefaultAsync(i => i.Id == input.ItemId, ct)
            ?? throw new DomainException($"Không tìm thấy chỉ tiêu Id = {input.ItemId}.", "ERR_KPI_ITEM_NOT_FOUND");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var req = item.RequestExtension(input.RequestedDueDate, input.Reason, input.ObjectiveReason, today);

        await _context.KpiExtensionRequests.AddAsync(req, ct);
        await _context.SaveChangesAsync(ct);

        return new KpiExtensionRequestDto
        {
            Id = req.Id,
            KpiItemId = req.KpiItemId,
            OriginalDueDate = req.OriginalDueDate,
            RequestedDueDate = req.RequestedDueDate,
            Reason = req.Reason,
            ObjectiveReason = req.ObjectiveReason,
            Status = req.Status
        };
    }

    public async Task ApproveExtensionAsync(ApproveKpiExtensionInput input, int approverId, CancellationToken ct = default)
    {
        var req = await _context.KpiExtensionRequests
            .Include(r => r.KpiItem)
            .FirstOrDefaultAsync(r => r.Id == input.RequestId, ct)
            ?? throw new DomainException($"Không tìm thấy yêu cầu gia hạn Id = {input.RequestId}.", "ERR_KPI_EXTENSION_NOT_FOUND");

        if (input.IsApproved)
        {
            req.Approve(approverId, input.Comment);
        }
        else
        {
            req.Reject(approverId, input.Comment ?? "Từ chối yêu cầu gia hạn");
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task SubmitAppealAsync(SubmitKpiAppealRequest request, int employeeId, CancellationToken ct = default)
    {
        var sheet = await _context.KpiSheets
            .Include(s => s.KpiPeriod)
            .FirstOrDefaultAsync(s => s.Id == request.SheetId, ct)
            ?? throw new DomainException($"Không tìm thấy phiếu KPI Id = {request.SheetId}.", "ERR_KPI_SHEET_NOT_FOUND");

        if (sheet.EmployeeId != employeeId)
            throw new DomainException("Chỉ người sở hữu phiếu KPI mới được quyền nộp khiếu nại.", "ERR_KPI_APPEAL_UNAUTHORIZED");

        if (sheet.Status != KpiStatus.Locked)
            throw new DomainException("Chỉ có thể khiếu nại sau khi kỳ đánh giá đã được khóa chính thức.", "ERR_KPI_APPEAL_NOT_LOCKED");

        var period = sheet.KpiPeriod;
        if (period?.ClosedAt != null && DateTime.UtcNow > period.ClosedAt.Value.AddDays(3))
        {
            throw new DomainException("Đã quá thời hạn khiếu nại (tối đa 3 ngày sau khi khóa kỳ).", "ERR_KPI_APPEAL_EXPIRED");
        }

        var appeal = new KpiAppeal(
            employeeId,
            sheet.Period,
            sheet.Id,
            request.Reason,
            sheet.TotalScore,
            request.ProposedScore);

        await _context.KpiAppeals.AddAsync(appeal, ct);
        await _context.SaveChangesAsync(ct);
    }
}
