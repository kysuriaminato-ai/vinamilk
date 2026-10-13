using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Application.DTOs.Attendances;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Attendances;
using VinamilkHRM.Domain.Entities.Shifts;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Services;

public class RosterService : IRosterService
{
    private readonly IApplicationDbContext _context;
    private readonly IRosterConstraintValidator _constraintValidator;

    public RosterService(
        IApplicationDbContext context,
        IRosterConstraintValidator constraintValidator)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _constraintValidator = constraintValidator ?? throw new ArgumentNullException(nameof(constraintValidator));
    }

    public async Task<int> GenerateMonthlyRosterAsync(GenerateRosterRequest request, CancellationToken cancellationToken = default)
    {
        string period = $"{request.Year:D4}-{request.Month:D2}";

        // 1. Kiểm tra khóa kỳ công
        var lockedPeriod = await _context.MonthlyTimesheetPeriods
            .FirstOrDefaultAsync(p => p.Period == period && p.OrganizationalUnitId == request.OrganizationalUnitId && p.Status == TimesheetPeriodStatus.Locked, cancellationToken);
        if (lockedPeriod != null)
        {
            throw new DomainException($"Kỳ công {period} đã bị khóa, không thể lập lại lịch.", "ERR_ATT_PERIOD_LOCKED");
        }

        // 2. Tải mẫu chu kỳ xoay ca
        var pattern = await _context.RotationPatterns
            .FirstOrDefaultAsync(r => r.Id == request.RotationPatternId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy mẫu chu kỳ Id={request.RotationPatternId}.", "ERR_ROT_NOT_FOUND");

        // 3. Tải danh sách kíp và thành viên của đơn vị
        var crews = await _context.Crews
            .Include(c => c.Members)
            .Where(c => c.OrganizationalUnitId == request.OrganizationalUnitId && c.IsActive)
            .ToListAsync(cancellationToken);

        // 4. Tải các định nghĩa ca làm việc
        var allShifts = await _context.ShiftDefinitions
            .Where(s => s.IsActive)
            .ToListAsync(cancellationToken);
        var shiftMap = allShifts.ToDictionary(s => s.Code, s => s, StringComparer.OrdinalIgnoreCase);

        // 5. Tải danh sách nhân viên của đơn vị
        var employees = await _context.Employees
            .Where(e => e.OrganizationalUnitId == request.OrganizationalUnitId && e.IsActive)
            .ToListAsync(cancellationToken);

        int totalDays = DateTime.DaysInMonth(request.Year, request.Month);
        var startDate = new DateOnly(request.Year, request.Month, 1);
        var endDate = new DateOnly(request.Year, request.Month, totalDays);

        // Tải các bản ghi RosterEntry hiện hữu trong tháng
        var existingEntries = await _context.RosterEntries
            .Where(r => r.WorkDate >= startDate && r.WorkDate <= endDate)
            .ToListAsync(cancellationToken);
        var entryMap = existingEntries.ToDictionary(r => (r.EmployeeId, r.WorkDate));

        // Tải thiết lập xoay kíp CrewRotation
        var crewRotations = await _context.CrewRotations
            .Where(cr => cr.RotationPatternId == pattern.Id)
            .ToListAsync(cancellationToken);
        var crewRotationMap = crewRotations.ToDictionary(cr => cr.CrewId);

        // Chuẩn bị danh sách phân bổ
        var entriesToSave = new List<RosterEntry>();

        // Trường hợp đơn vị đã có Crews và CrewRotations
        if (crews.Count > 0)
        {
            foreach (var crew in crews)
            {
                if (!crewRotationMap.TryGetValue(crew.Id, out var rotation))
                {
                    // Nếu kíp chưa có thiết lập, tạo ngày neo mặc định là ngày đầu tháng, offset theo kíp
                    rotation = new CrewRotation(crew.Id, pattern.Id, startDate, 0);
                }

                foreach (var member in crew.Members)
                {
                    var emp = employees.FirstOrDefault(e => e.Id == member.EmployeeId);
                    if (emp == null) continue;

                    for (int day = 1; day <= totalDays; day++)
                    {
                        var curDate = new DateOnly(request.Year, request.Month, day);
                        if (member.EffectiveFrom > curDate || (member.EffectiveTo.HasValue && member.EffectiveTo.Value < curDate))
                            continue;

                        string shiftCode = rotation.CalculateShiftCodeForDate(curDate, pattern);
                        bool isOff = shiftCode.Equals("OFF", StringComparison.OrdinalIgnoreCase);
                        ShiftDefinition? shift = null;
                        if (!isOff && shiftMap.TryGetValue(shiftCode, out var foundShift))
                        {
                            shift = foundShift;
                        }

                        // Ràng buộc thai sản & nuôi con nhỏ
                        _constraintValidator.ValidateMaternalProtection(emp, curDate, shift, 0m);

                        if (entryMap.TryGetValue((emp.Id, curDate), out var existing))
                        {
                            if (existing.Status != RosterStatus.Published)
                            {
                                existing.UpdateShift(shift?.Id, isOff, RosterSource.Pattern);
                            }
                        }
                        else
                        {
                            var newEntry = new RosterEntry(emp.Id, curDate, shift?.Id, isOff, RosterSource.Pattern, RosterStatus.Draft);
                            _context.RosterEntries.Add(newEntry);
                        }
                    }
                }
            }
        }
        else
        {
            // Nếu chưa chia kíp cụ thể trong DB, phân bổ xoay vòng đều cho các nhân sự của đơn vị
            int empIndex = 0;
            foreach (var emp in employees)
            {
                // Chia 4 nhóm kíp so le: 0, 2, 4, 6
                int offset = (empIndex % 4) * 2;
                empIndex++;

                for (int day = 1; day <= totalDays; day++)
                {
                    var curDate = new DateOnly(request.Year, request.Month, day);
                    int daysDiff = curDate.DayNumber - startDate.DayNumber;
                    int cycleDay = daysDiff + offset;
                    string shiftCode = pattern.GetShiftCodeForDay(cycleDay);

                    bool isOff = shiftCode.Equals("OFF", StringComparison.OrdinalIgnoreCase);
                    ShiftDefinition? shift = null;
                    if (!isOff && shiftMap.TryGetValue(shiftCode, out var foundShift))
                    {
                        shift = foundShift;
                    }

                    _constraintValidator.ValidateMaternalProtection(emp, curDate, shift, 0m);

                    if (entryMap.TryGetValue((emp.Id, curDate), out var existing))
                    {
                        if (existing.Status != RosterStatus.Published)
                        {
                            existing.UpdateShift(shift?.Id, isOff, RosterSource.Pattern);
                        }
                    }
                    else
                    {
                        var newEntry = new RosterEntry(emp.Id, curDate, shift?.Id, isOff, RosterSource.Pattern, RosterStatus.Draft);
                        _context.RosterEntries.Add(newEntry);
                    }
                }
            }
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RosterEntryDto> UpdateRosterEntryAsync(UpdateRosterEntryRequest request, CancellationToken cancellationToken = default)
    {
        string period = $"{request.WorkDate.Year:D4}-{request.WorkDate.Month:D2}";

        var entry = await _context.RosterEntries
            .Include(r => r.Employee)
            .Include(r => r.ShiftDefinition)
            .FirstOrDefaultAsync(r => r.EmployeeId == request.EmployeeId && r.WorkDate == request.WorkDate, cancellationToken);

        int unitId = entry?.Employee?.OrganizationalUnitId ?? 0;
        if (unitId > 0)
        {
            var lockedPeriod = await _context.MonthlyTimesheetPeriods
                .FirstOrDefaultAsync(p => p.Period == period && p.OrganizationalUnitId == unitId && p.Status == TimesheetPeriodStatus.Locked, cancellationToken);
            if (lockedPeriod != null)
            {
                throw new DomainException($"Kỳ công {period} đã bị khóa sổ, không thể chỉnh sửa phân ca.", "ERR_ATT_PERIOD_LOCKED");
            }
        }

        ShiftDefinition? newShift = null;
        if (request.ShiftDefinitionId.HasValue)
        {
            newShift = await _context.ShiftDefinitions
                .Include(s => s.Segments)
                .FirstOrDefaultAsync(s => s.Id == request.ShiftDefinitionId.Value, cancellationToken)
                ?? throw new DomainException($"Không tìm thấy ca làm việc Id={request.ShiftDefinitionId.Value}.", "ERR_SHIFT_NOT_FOUND");
        }

        // Ràng buộc bảo vệ thai sản
        if (entry?.Employee != null && newShift != null)
        {
            _constraintValidator.ValidateMaternalProtection(entry.Employee, request.WorkDate, newShift, 0m);
        }

        // Ràng buộc nghỉ giữa 2 ca < 12 giờ với ca ngày hôm trước
        if (newShift != null)
        {
            var prevDate = request.WorkDate.AddDays(-1);
            var prevEntry = await _context.RosterEntries
                .Include(r => r.ShiftDefinition)
                    .ThenInclude(s => s!.Segments)
                .FirstOrDefaultAsync(r => r.EmployeeId == request.EmployeeId && r.WorkDate == prevDate, cancellationToken);

            if (prevEntry?.ShiftDefinition != null)
            {
                _constraintValidator.ValidateRestBetweenShifts(prevDate, prevEntry.ShiftDefinition, request.WorkDate, newShift);
            }
        }

        if (entry == null)
        {
            entry = new RosterEntry(
                request.EmployeeId,
                request.WorkDate,
                request.ShiftDefinitionId,
                request.IsOff,
                RosterSource.Manual,
                RosterStatus.Draft,
                request.Note);
            _context.RosterEntries.Add(entry);
        }
        else
        {
            entry.UpdateShift(request.ShiftDefinitionId, request.IsOff, RosterSource.Manual, request.Note);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new RosterEntryDto(
            entry.Id,
            entry.EmployeeId,
            entry.Employee?.EmployeeCode ?? string.Empty,
            entry.Employee?.FullName ?? string.Empty,
            entry.WorkDate,
            entry.ShiftDefinitionId,
            entry.ShiftDefinition?.Code,
            entry.ShiftDefinition?.Name,
            entry.IsOff,
            entry.Source,
            entry.Status,
            entry.Note);
    }

    public async Task<int> PublishRosterAsync(PublishRosterRequest request, CancellationToken cancellationToken = default)
    {
        string period = $"{request.Year:D4}-{request.Month:D2}";

        var lockedPeriod = await _context.MonthlyTimesheetPeriods
            .FirstOrDefaultAsync(p => p.Period == period && p.OrganizationalUnitId == request.OrganizationalUnitId && p.Status == TimesheetPeriodStatus.Locked, cancellationToken);
        if (lockedPeriod != null)
        {
            throw new DomainException($"Kỳ công {period} đã bị khóa sổ, không thể công bố lịch.", "ERR_ATT_PERIOD_LOCKED");
        }

        int totalDays = DateTime.DaysInMonth(request.Year, request.Month);
        var startDate = new DateOnly(request.Year, request.Month, 1);
        var endDate = new DateOnly(request.Year, request.Month, totalDays);

        var entries = await _context.RosterEntries
            .Include(r => r.Employee)
            .Include(r => r.ShiftDefinition)
                .ThenInclude(s => s!.Segments)
            .Where(r => r.Employee!.OrganizationalUnitId == request.OrganizationalUnitId &&
                        r.WorkDate >= startDate && r.WorkDate <= endDate)
            .ToListAsync(cancellationToken);

        // Kiểm tra ràng buộc cho từng nhân viên trước khi công bố
        var groupedByEmp = entries.GroupBy(r => r.EmployeeId);
        foreach (var empGroup in groupedByEmp)
        {
            var ordered = empGroup.OrderBy(r => r.WorkDate).ToList();
            for (int i = 0; i < ordered.Count - 1; i++)
            {
                var cur = ordered[i];
                var next = ordered[i + 1];
                if (!cur.IsOff && cur.ShiftDefinition != null && !next.IsOff && next.ShiftDefinition != null)
                {
                    _constraintValidator.ValidateRestBetweenShifts(cur.WorkDate, cur.ShiftDefinition, next.WorkDate, next.ShiftDefinition);
                }
            }
        }

        foreach (var entry in entries)
        {
            entry.Publish();
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RosterGridResponse> GetRosterGridAsync(int organizationalUnitId, int year, int month, CancellationToken cancellationToken = default)
    {
        var unit = await _context.OrganizationalUnits
            .FirstOrDefaultAsync(u => u.Id == organizationalUnitId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy đơn vị Id={organizationalUnitId}.", "ERR_UNIT_NOT_FOUND");

        int totalDays = DateTime.DaysInMonth(year, month);
        var startDate = new DateOnly(year, month, 1);
        var endDate = new DateOnly(year, month, totalDays);

        var employees = await _context.Employees
            .Include(e => e.StandardJobTitle)
            .Where(e => e.OrganizationalUnitId == organizationalUnitId && e.IsActive)
            .OrderBy(e => e.EmployeeCode)
            .ToListAsync(cancellationToken);

        var entries = await _context.RosterEntries
            .Include(r => r.ShiftDefinition)
            .Where(r => r.Employee!.OrganizationalUnitId == organizationalUnitId &&
                        r.WorkDate >= startDate && r.WorkDate <= endDate)
            .ToListAsync(cancellationToken);

        var entryLookup = entries.ToLookup(r => r.EmployeeId);

        var gridItems = new List<RosterGridItemDto>();
        foreach (var emp in employees)
        {
            var empEntries = entryLookup[emp.Id].ToDictionary(r => r.WorkDate.ToString("yyyy-MM-dd"), r => r.IsOff ? "OFF" : (r.ShiftDefinition?.Code ?? "OFF"));
            gridItems.Add(new RosterGridItemDto(
                emp.Id,
                emp.EmployeeCode,
                emp.FullName,
                emp.StandardJobTitle?.JobTitleName ?? string.Empty,
                empEntries));
        }

        return new RosterGridResponse(
            unit.Id,
            unit.UnitName,
            year,
            month,
            totalDays,
            gridItems);
    }

    public async Task<ShiftSwapRequestDto> ProposeShiftSwapAsync(CreateShiftSwapRequest request, CancellationToken cancellationToken = default)
    {
        var swap = new ShiftSwapRequest(
            request.RequesterEmployeeId,
            request.RequesterDate,
            request.TargetEmployeeId,
            request.TargetDate,
            request.Reason);

        _context.ShiftSwapRequests.Add(swap);
        await _context.SaveChangesAsync(cancellationToken);

        var reqEmp = await _context.Employees.FindAsync(new object[] { request.RequesterEmployeeId }, cancellationToken);
        var targetEmp = await _context.Employees.FindAsync(new object[] { request.TargetEmployeeId }, cancellationToken);

        return new ShiftSwapRequestDto(
            swap.Id,
            swap.RequesterEmployeeId,
            reqEmp?.FullName ?? string.Empty,
            swap.RequesterDate,
            swap.TargetEmployeeId,
            targetEmp?.FullName ?? string.Empty,
            swap.TargetDate,
            swap.Reason,
            swap.Status,
            null,
            null,
            null);
    }

    public async Task<ShiftSwapRequestDto> AcceptShiftSwapAsync(int swapRequestId, int targetEmployeeId, CancellationToken cancellationToken = default)
    {
        var swap = await _context.ShiftSwapRequests
            .Include(s => s.RequesterEmployee)
            .Include(s => s.TargetEmployee)
            .FirstOrDefaultAsync(s => s.Id == swapRequestId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy đề xuất đổi ca Id={swapRequestId}.", "ERR_SWAP_NOT_FOUND");

        if (swap.TargetEmployeeId != targetEmployeeId)
            throw new DomainException("Chỉ người được yêu cầu mới có quyền chấp thuận đổi ca.", "ERR_SWAP_UNAUTHORIZED");

        swap.Accept();
        await _context.SaveChangesAsync(cancellationToken);

        return new ShiftSwapRequestDto(
            swap.Id,
            swap.RequesterEmployeeId,
            swap.RequesterEmployee?.FullName ?? string.Empty,
            swap.RequesterDate,
            swap.TargetEmployeeId,
            swap.TargetEmployee?.FullName ?? string.Empty,
            swap.TargetDate,
            swap.Reason,
            swap.Status,
            null,
            null,
            null);
    }

    public async Task<ShiftSwapRequestDto> ApproveShiftSwapAsync(int swapRequestId, int approverId, CancellationToken cancellationToken = default)
    {
        var swap = await _context.ShiftSwapRequests
            .Include(s => s.RequesterEmployee)
            .Include(s => s.TargetEmployee)
            .FirstOrDefaultAsync(s => s.Id == swapRequestId, cancellationToken)
            ?? throw new DomainException($"Không tìm thấy đề xuất đổi ca Id={swapRequestId}.", "ERR_SWAP_NOT_FOUND");

        swap.Approve(approverId);

        // Tải 2 bản ghi phân ca của 2 người tại 2 ngày tương ứng
        var reqEntry = await _context.RosterEntries
            .FirstOrDefaultAsync(r => r.EmployeeId == swap.RequesterEmployeeId && r.WorkDate == swap.RequesterDate, cancellationToken);

        var targetEntry = await _context.RosterEntries
            .FirstOrDefaultAsync(r => r.EmployeeId == swap.TargetEmployeeId && r.WorkDate == swap.TargetDate, cancellationToken);

        if (reqEntry != null && targetEntry != null)
        {
            var tempShift = reqEntry.ShiftDefinitionId;
            var tempOff = reqEntry.IsOff;

            reqEntry.ApplySwap(targetEntry.ShiftDefinitionId, targetEntry.IsOff, $"Đổi ca với {swap.TargetEmployee?.FullName}");
            targetEntry.ApplySwap(tempShift, tempOff, $"Đổi ca với {swap.RequesterEmployee?.FullName}");
        }

        await _context.SaveChangesAsync(cancellationToken);

        var approver = await _context.Employees.FindAsync(new object[] { approverId }, cancellationToken);

        return new ShiftSwapRequestDto(
            swap.Id,
            swap.RequesterEmployeeId,
            swap.RequesterEmployee?.FullName ?? string.Empty,
            swap.RequesterDate,
            swap.TargetEmployeeId,
            swap.TargetEmployee?.FullName ?? string.Empty,
            swap.TargetDate,
            swap.Reason,
            swap.Status,
            swap.ApproverId,
            approver?.FullName,
            swap.ApprovedAt);
    }
}
