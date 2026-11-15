using Vinamilk.HRM.Domain.Entities.Time;

namespace Vinamilk.HRM.Application.TimeAttendance;

// ============================================================================
// CL3: ENGINE TÍNH CÔNG THUẦN (KHÔNG PHỤ THUỘC DB / EF CORE)
// ============================================================================

public sealed record AttendanceSymbolConfig(
    string SymbolTypeCode,
    string SymbolCode,
    string SymbolName,
    string Category,
    decimal WorkdayMultiplier,
    bool IsPaidByCompany,
    bool IsPaidBySocialInsurance);

public sealed class AttendanceSymbolCatalog
{
    private readonly Dictionary<string, AttendanceSymbolConfig> _byType;

    public AttendanceSymbolCatalog(IEnumerable<AttendanceSymbolDefinition> definitions)
    {
        _byType = definitions
            .Where(d => d.IsActive)
            .GroupBy(d => d.SymbolTypeCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var d = g.First();
                    return new AttendanceSymbolConfig(
                        d.SymbolTypeCode,
                        d.SymbolCode,
                        d.SymbolName,
                        d.Category,
                        d.WorkdayMultiplier,
                        d.IsPaidByCompany,
                        d.IsPaidBySocialInsurance);
                },
                StringComparer.OrdinalIgnoreCase);
    }

    public AttendanceSymbolCatalog(IEnumerable<AttendanceSymbolConfig> configs)
    {
        _byType = configs.ToDictionary(c => c.SymbolTypeCode, StringComparer.OrdinalIgnoreCase);
    }

    public string GetCode(string symbolTypeCode, string fallbackCode)
        => _byType.TryGetValue(symbolTypeCode, out var cfg) && !string.IsNullOrWhiteSpace(cfg.SymbolCode)
            ? cfg.SymbolCode
            : fallbackCode;

    public decimal GetMultiplier(string symbolTypeCode, decimal fallback)
        => _byType.TryGetValue(symbolTypeCode, out var cfg) ? cfg.WorkdayMultiplier : fallback;

    public static AttendanceSymbolCatalog CreateDefault() => new AttendanceSymbolCatalog(new AttendanceSymbolConfig[]
    {
        new("Work", "X", "Đi làm đủ công", "Work", 1.0m, true, false),
        new("HalfWork", "½", "Đi làm nửa công", "Work", 0.5m, true, false),
        new("AnnualLeave", "P", "Nghỉ phép năm hưởng lương", "PaidLeave", 1.0m, true, false),
        new("Holiday", "L", "Nghỉ Lễ / Tết hưởng nguyên lương", "Holiday", 1.0m, true, false),
        new("Sick", "Ô", "Nghỉ ốm hưởng BHXH", "SocialInsurance", 1.0m, false, true),
        new("Maternity", "TS", "Nghỉ thai sản hưởng BHXH", "SocialInsurance", 1.0m, false, true),
        new("BusinessTrip", "CT", "Đi công tác", "Work", 1.0m, true, false),
        new("PersonalPaid", "R", "Nghỉ riêng có lương", "PersonalPaid", 1.0m, true, false),
        new("PersonalUnpaid", "Ro", "Nghỉ riêng không lương", "UnpaidLeave", 0m, false, false),
        new("UnexcusedAbsent", "KL", "Vắng không phép", "Absent", 0m, false, false),
        new("WeeklyOff", "OFF", "Nghỉ tuần", "Off", 0m, false, false),
        new("MissedPunch", "TC", "Thiếu chấm vào/ra", "Missing", 0m, false, false),
        new("PendingGeofence", "CGD", "Ngoài vùng GPS chờ duyệt", "Missing", 0m, false, false)
    });
}

public sealed record AttendancePunchInput(
    DateTime PunchTime,
    string PunchType, // CheckIn | CheckOut | BreakOut | BreakIn
    string SourceType,
    string? GeofenceStatus = null);

public sealed record AttendanceApprovedLeaveInput(
    string LeaveTypeCode,
    string SymbolCode,
    string Session, // FullDay | Morning | Afternoon
    decimal Days,
    bool IsPaidByCompany,
    bool IsPaidBySocialInsurance,
    bool IsBusinessTrip = false,
    string? Description = null);

public sealed record AttendanceApprovedOvertimeInput(
    TimeOnly StartTime,
    TimeOnly EndTime,
    decimal ApprovedHours,
    string? RateType = null);

public sealed record AttendanceApprovedExplanationInput(
    TimeOnly ProposedCheckIn,
    TimeOnly ProposedCheckOut,
    string? Reason = null);

public sealed record AttendanceDailyCalculationInput(
    DateOnly WorkDate,
    WorkShift? AssignedShift,
    bool IsDayOffSchedule,
    bool IsWeeklyDayOff,
    Holiday? Holiday,
    IReadOnlyList<AttendancePunchInput> Punches,
    IReadOnlyList<AttendanceApprovedLeaveInput> ApprovedLeaves,
    IReadOnlyList<AttendanceApprovedOvertimeInput> ApprovedOvertimes,
    AttendanceApprovedExplanationInput? ApprovedExplanation,
    AttendancePolicy Policy,
    AttendanceStatutoryParameter Statutory,
    AttendanceSymbolCatalog Symbols);

public sealed record AttendanceDailyCalculationOutput(
    DateOnly WorkDate,
    int? WorkShiftId,
    string ShiftCode,
    DateTime? ActualCheckIn,
    DateTime? ActualCheckOut,
    decimal StandardHours,
    decimal WorkingHours,
    decimal WorkdayCount,
    decimal PaidLeaveDays,
    decimal SocialInsuranceLeaveDays,
    decimal UnpaidLeaveDays,
    decimal UnexcusedAbsentDays,
    int RawLateMinutes,
    int LateMinutes,
    int RawEarlyLeaveMinutes,
    int EarlyLeaveMinutes,
    decimal NightShiftHours,
    decimal OvertimeHours150Day,
    decimal OvertimeHours150Night,
    decimal OvertimeHours200Day,
    decimal OvertimeHours200Night,
    decimal OvertimeHours300Day,
    decimal OvertimeHours300Night,
    string StatusCode,
    bool IsMissedPunch,
    bool HasApprovedExplanation,
    string CalculationNotes)
{
    public decimal OvertimeHours150 => OvertimeHours150Day + OvertimeHours150Night;
    public decimal OvertimeHours200 => OvertimeHours200Day + OvertimeHours200Night;
    public decimal OvertimeHours300 => OvertimeHours300Day + OvertimeHours300Night;
}

public static class AttendanceCalculationEngine
{
    public static AttendanceDailyCalculationOutput CalculateDaily(AttendanceDailyCalculationInput input)
    {
        var symbols = input.Symbols ?? AttendanceSymbolCatalog.CreateDefault();
        var shift = input.AssignedShift;
        var stdHours = shift?.StandardHours > 0m ? shift.StandardHours : input.Statutory.MaxNormalDailyHours;
        var shiftValue = shift?.WorkdayValue > 0m ? shift.WorkdayValue : 1.0m;
        var shiftCode = shift?.ShiftCode ?? "HC";

        // Lọc punch hợp lệ & sắp xếp tăng dần theo thời gian
        var orderedPunches = input.Punches
            .Where(p => !string.Equals(p.GeofenceStatus, "OutsideRejected", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.PunchTime)
            .ToList();

        var validPunches = orderedPunches
            .Where(p => !string.Equals(p.GeofenceStatus, "OutsidePendingApproval", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // 1. Nếu chỉ có log ngoài vùng GPS đang chờ duyệt
        if (orderedPunches.Count > 0 && validPunches.Count == 0 && input.ApprovedExplanation == null)
        {
            return new AttendanceDailyCalculationOutput(
                input.WorkDate, shift?.Id, shiftCode,
                orderedPunches.First().PunchTime,
                orderedPunches.Count > 1 ? orderedPunches.Last().PunchTime : null,
                stdHours, 0m, 0m, 0m, 0m, 0m, 0m,
                0, 0, 0, 0, 0m, 0m, 0m, 0m, 0m, 0m, 0m,
                symbols.GetCode("PendingGeofence", "CGD"),
                false, false,
                "Chấm công ngoài vùng GPS đang chờ duyệt.");
        }

        // 2. Xác định ngày công tác (CT) hoặc nghỉ cả ngày được duyệt
        var fullBusinessTrip = input.ApprovedLeaves.FirstOrDefault(l =>
            (l.IsBusinessTrip || string.Equals(l.LeaveTypeCode, "BUSINESS_TRIP", StringComparison.OrdinalIgnoreCase))
            && l.Days >= 1.0m);

        var fullDayLeave = input.ApprovedLeaves.FirstOrDefault(l =>
            !(l.IsBusinessTrip || string.Equals(l.LeaveTypeCode, "BUSINESS_TRIP", StringComparison.OrdinalIgnoreCase))
            && l.Days >= 1.0m);

        // Nếu nghỉ Lễ có hưởng lương (và không đi làm / không nghỉ thai sản BHXH)
        bool isHoliday = input.Holiday != null;
        bool isWeeklyOff = !isHoliday && (input.IsDayOffSchedule || input.IsWeeklyDayOff);

        // Phân tách OT được duyệt theo ban ngày / ban đêm
        var (otDay, otNight) = ComputeOvertimeDayNight(
            input.WorkDate,
            shift,
            validPunches,
            input.ApprovedOvertimes,
            input.Policy,
            input.Statutory,
            isNonWorkingDay: isHoliday || isWeeklyOff);

        decimal ot150D = 0m, ot150N = 0m, ot200D = 0m, ot200N = 0m, ot300D = 0m, ot300N = 0m;
        if (isHoliday)
        {
            ot300D = otDay;
            ot300N = otNight;
        }
        else if (isWeeklyOff)
        {
            ot200D = otDay;
            ot200N = otNight;
        }
        else
        {
            ot150D = otDay;
            ot150N = otNight;
        }

        // 2a. Công tác cả ngày (CT): không cần chấm công vẫn hưởng đủ công ca chuẩn
        if (fullBusinessTrip != null && !isHoliday && !isWeeklyOff)
        {
            var btCode = ResolveLeaveSymbol(fullBusinessTrip, symbols, "BusinessTrip", "CT");
            return new AttendanceDailyCalculationOutput(
                input.WorkDate, shift?.Id, shiftCode,
                validPunches.FirstOrDefault()?.PunchTime,
                validPunches.Count > 1 ? validPunches.Last().PunchTime : null,
                stdHours, stdHours, shiftValue,
                0m, 0m, 0m, 0m,
                0, 0, 0, 0, 0m,
                ot150D, ot150N, ot200D, ot200N, ot300D, ot300N,
                btCode, false, false,
                $"Đi công tác cả ngày ({fullBusinessTrip.Description ?? btCode}) - Hưởng đủ {shiftValue:0.##} công chuẩn.");
        }

        // 2b. Ngày Lễ
        if (isHoliday)
        {
            var holSymbol = symbols.GetCode("Holiday", "L");
            var paidHolidayWorkdays = input.Holiday!.IsPaidHoliday ? shiftValue : 0m;
            var actualIn = validPunches.FirstOrDefault()?.PunchTime;
            var actualOut = validPunches.Count > 1 ? validPunches.Last().PunchTime : (DateTime?)null;

            return new AttendanceDailyCalculationOutput(
                input.WorkDate, shift?.Id, shiftCode,
                actualIn, actualOut,
                stdHours, 0m, paidHolidayWorkdays,
                0m, 0m, 0m, 0m,
                0, 0, 0, 0, 0m,
                ot150D, ot150N, ot200D, ot200N, ot300D, ot300N,
                holSymbol, false, false,
                $"Nghỉ Lễ '{input.Holiday.HolidayName}' (OT 300%: ngày {ot300D:0.##}h, đêm {ot300N:0.##}h).");
        }

        // 2c. Nghỉ phép / Ốm / Thai sản / Việc riêng cả ngày
        if (fullDayLeave != null && !isWeeklyOff)
        {
            var leaveSym = ResolveLeaveSymbol(fullDayLeave, symbols);
            decimal paidLeave = fullDayLeave.IsPaidByCompany ? shiftValue : 0m;
            decimal siLeave = fullDayLeave.IsPaidBySocialInsurance ? shiftValue : 0m;
            decimal unpaidLeave = (!fullDayLeave.IsPaidByCompany && !fullDayLeave.IsPaidBySocialInsurance) ? shiftValue : 0m;

            return new AttendanceDailyCalculationOutput(
                input.WorkDate, shift?.Id, shiftCode,
                null, null,
                stdHours, 0m, 0m,
                paidLeave, siLeave, unpaidLeave, 0m,
                0, 0, 0, 0, 0m,
                0m, 0m, 0m, 0m, 0m, 0m,
                leaveSym, false, false,
                $"Nghỉ cả ngày theo đơn '{fullDayLeave.LeaveTypeCode}' ({leaveSym}).");
        }

        // 2d. Ngày nghỉ tuần (OFF)
        if (isWeeklyOff)
        {
            var offSym = symbols.GetCode("WeeklyOff", "OFF");
            var actualIn = validPunches.FirstOrDefault()?.PunchTime;
            var actualOut = validPunches.Count > 1 ? validPunches.Last().PunchTime : (DateTime?)null;

            return new AttendanceDailyCalculationOutput(
                input.WorkDate, shift?.Id, shiftCode,
                actualIn, actualOut,
                0m, 0m, 0m,
                0m, 0m, 0m, 0m,
                0, 0, 0, 0, 0m,
                ot150D, ot150N, ot200D, ot200N, ot300D, ot300N,
                offSym, false, false,
                $"Ngày nghỉ tuần ({offSym}) (OT 200%: ngày {ot200D:0.##}h, đêm {ot200N:0.##}h).");
        }

        // 3. Ngày làm việc bình thường (có thể kèm nghỉ nửa ngày / công tác nửa ngày)
        var halfLeave = input.ApprovedLeaves.FirstOrDefault(l => l.Days > 0m && l.Days < 1.0m);

        // Xử lý giải trình thiếu công được duyệt (thay thế hoặc bổ sung chấm vào/ra)
        bool hasApprovedExplanation = input.ApprovedExplanation != null;
        DateTime? effectiveIn = null;
        DateTime? effectiveOut = null;
        var workedSpans = new List<(DateTime Start, DateTime End)>();
        bool isMissedPunch = false;

        if (hasApprovedExplanation)
        {
            var exp = input.ApprovedExplanation!;
            effectiveIn = input.WorkDate.ToDateTime(exp.ProposedCheckIn);
            effectiveOut = exp.ProposedCheckOut <= exp.ProposedCheckIn
                ? input.WorkDate.AddDays(1).ToDateTime(exp.ProposedCheckOut)
                : input.WorkDate.ToDateTime(exp.ProposedCheckOut);
            workedSpans.Add((effectiveIn.Value, effectiveOut.Value));
        }
        else if (validPunches.Count == 0)
        {
            // Không chấm công vào ngày làm việc & không có đơn từ -> Vắng không phép (KL)
            if (halfLeave == null)
            {
                return new AttendanceDailyCalculationOutput(
                    input.WorkDate, shift?.Id, shiftCode,
                    null, null,
                    stdHours, 0m, 0m,
                    0m, 0m, 0m, shiftValue,
                    0, 0, 0, 0, 0m,
                    0m, 0m, 0m, 0m, 0m, 0m,
                    symbols.GetCode("UnexcusedAbsent", "KL"),
                    false, false,
                    "Không có dữ liệu chấm công và không có đơn xin nghỉ.");
            }
            // Nếu nghỉ nửa ngày còn nửa ngày không chấm -> tính phần nghỉ nửa ngày, nửa còn lại vắng không phép
            var halfLeaveSym = ResolveLeaveSymbol(halfLeave, symbols);
            decimal hlPaid = halfLeave.IsPaidByCompany ? 0.5m : 0m;
            decimal hlSi = halfLeave.IsPaidBySocialInsurance ? 0.5m : 0m;
            decimal hlUnpaid = (!halfLeave.IsPaidByCompany && !halfLeave.IsPaidBySocialInsurance) ? 0.5m : 0m;
            var absentSym = symbols.GetCode("UnexcusedAbsent", "KL");
            return new AttendanceDailyCalculationOutput(
                input.WorkDate, shift?.Id, shiftCode,
                null, null,
                stdHours, 0m, 0m,
                hlPaid, hlSi, hlUnpaid, 0.5m,
                0, 0, 0, 0, 0m,
                0m, 0m, 0m, 0m, 0m, 0m,
                $"{halfLeaveSym}+{absentSym}",
                false, false,
                $"Nghỉ nửa ngày ({halfLeaveSym}) + vắng nửa ngày không chấm ({absentSym}).");
        }
        else
        {
            // Phân tích các lần chấm theo FirstLast hoặc PairedSegments
            (effectiveIn, effectiveOut, workedSpans, isMissedPunch) = ResolvePunchesToIntervals(validPunches, input.Policy.PunchPairingMode);
        }

        // Nếu thiếu 1 đầu vào/ra (không có giải trình duyệt)
        if (isMissedPunch || !effectiveIn.HasValue || !effectiveOut.HasValue)
        {
            decimal missedCredit = input.Policy.MissedPunchCreditWorkdays;
            decimal hlPaid = halfLeave != null && halfLeave.IsPaidByCompany ? 0.5m : 0m;
            decimal hlSi = halfLeave != null && halfLeave.IsPaidBySocialInsurance ? 0.5m : 0m;
            decimal hlUnpaid = halfLeave != null && !halfLeave.IsPaidByCompany && !halfLeave.IsPaidBySocialInsurance ? 0.5m : 0m;
            return new AttendanceDailyCalculationOutput(
                input.WorkDate, shift?.Id, shiftCode,
                effectiveIn, effectiveOut,
                stdHours, 0m, missedCredit,
                hlPaid, hlSi, hlUnpaid, 0m,
                0, 0, 0, 0, 0m,
                0m, 0m, 0m, 0m, 0m, 0m,
                symbols.GetCode("MissedPunch", "TC"),
                true, false,
                "Thiếu lượt chấm vào hoặc chấm ra, chưa có giải trình được duyệt.");
        }

        // 4. Tính giờ làm thực tế, đi muộn, về sớm, làm đêm trên khung ca chuẩn (xử lý cả ca qua đêm & nghỉ nửa ngày)
        var effectiveShift = shift ?? new WorkShift
        {
            ShiftCode = "HC",
            ShiftType = "Fixed",
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(17, 0),
            BreakStartTime = new TimeOnly(12, 0),
            BreakEndTime = new TimeOnly(13, 0),
            StandardHours = stdHours,
            WorkdayValue = 1.0m,
            GraceLateMinutes = input.Policy.GraceLateMinutes
        };

        var (shiftWinStart, shiftWinEnd) = AttendancePolicyAndShiftEngine.GetPrimaryShiftWindow(input.WorkDate, effectiveShift);
        var (anchorStart, anchorEnd) = ResolveLateEarlyAnchors(input.WorkDate, effectiveShift, shiftWinStart, shiftWinEnd, halfLeave);

        var graceLate = input.Policy.GraceLateMinutes > 0 ? input.Policy.GraceLateMinutes : effectiveShift.GraceLateMinutes;
        var graceEarly = Math.Max(0, input.Policy.GraceEarlyMinutes);

        int rawLate = effectiveIn.Value > anchorStart ? (int)Math.Floor((effectiveIn.Value - anchorStart).TotalMinutes) : 0;
        int lateMinutes = rawLate <= graceLate
            ? 0
            : AttendancePolicyAndShiftEngine.RoundMinutes(rawLate, input.Policy.RoundingIntervalMinutes, input.Policy.RoundingMode);

        int rawEarly = effectiveOut.Value < anchorEnd ? (int)Math.Ceiling((anchorEnd - effectiveOut.Value).TotalMinutes) : 0;
        int earlyMinutes = rawEarly <= graceEarly
            ? 0
            : AttendancePolicyAndShiftEngine.RoundMinutes(rawEarly, input.Policy.RoundingIntervalMinutes, input.Policy.RoundingMode);

        // Khung tính giờ công chuẩn trong ca: cắt theo [shiftWinStart .. shiftWinEnd] và trừ giờ nghỉ giữa ca không hưởng lương
        var regularShiftSpans = BuildRegularWorkedSpans(input.WorkDate, effectiveShift, shiftWinStart, shiftWinEnd, workedSpans);
        decimal totalWorkedMinutes = regularShiftSpans.Sum(s => (decimal)(s.End - s.Start).TotalMinutes);
        decimal workingHours = Math.Round(totalWorkedMinutes / 60m, 2, MidpointRounding.AwayFromZero);

        decimal nightShiftHours = CalculateNightHoursFromSpans(
            regularShiftSpans,
            input.Statutory.NightWorkStartTime,
            input.Statutory.NightWorkEndTime);

        // Số công làm việc & ký hiệu ngày
        decimal actualWorkdays;
        string baseWorkSymbol;
        decimal halfPaidLeave = 0m, halfSiLeave = 0m, halfUnpaidLeave = 0m, halfAbsent = 0m;

        if (halfLeave != null)
        {
            if (halfLeave.IsBusinessTrip)
            {
                actualWorkdays = 1.0m; // Nửa ngày làm + nửa ngày công tác = 1.0 công thực tế
                baseWorkSymbol = $"{symbols.GetCode("HalfWork", "½")}+{ResolveLeaveSymbol(halfLeave, symbols, "BusinessTrip", "CT")}";
            }
            else
            {
                decimal halfEarned = (input.Policy.HalfDayLateThresholdMinutes > 0 && lateMinutes > input.Policy.HalfDayLateThresholdMinutes)
                    || (input.Policy.ZeroDayLateThresholdMinutes > 0 && lateMinutes > input.Policy.ZeroDayLateThresholdMinutes)
                    ? 0m
                    : 0.5m;
                actualWorkdays = halfEarned;
                if (halfLeave.IsPaidByCompany) halfPaidLeave = 0.5m;
                else if (halfLeave.IsPaidBySocialInsurance) halfSiLeave = 0.5m;
                else halfUnpaidLeave = 0.5m;

                if (halfEarned == 0m) halfAbsent = 0.5m;
                var workPartSym = halfEarned > 0m ? symbols.GetCode("HalfWork", "½") : symbols.GetCode("UnexcusedAbsent", "KL");
                var leavePartSym = ResolveLeaveSymbol(halfLeave, symbols);
                baseWorkSymbol = $"{workPartSym}+{leavePartSym}";
            }
        }
        else if (string.Equals(effectiveShift.ShiftType, "Hourly", StringComparison.OrdinalIgnoreCase))
        {
            var ratio = effectiveShift.StandardHours > 0m ? Math.Min(1m, workingHours / effectiveShift.StandardHours) : 1m;
            actualWorkdays = Math.Round(effectiveShift.WorkdayValue * ratio, 2, MidpointRounding.AwayFromZero);
            baseWorkSymbol = actualWorkdays >= 0.9m ? symbols.GetCode("Work", "X") : actualWorkdays > 0m ? symbols.GetCode("HalfWork", "½") : symbols.GetCode("UnexcusedAbsent", "KL");
        }
        else if (input.Policy.ZeroDayLateThresholdMinutes > 0 && lateMinutes > input.Policy.ZeroDayLateThresholdMinutes)
        {
            actualWorkdays = 0m;
            halfAbsent = effectiveShift.WorkdayValue;
            baseWorkSymbol = symbols.GetCode("UnexcusedAbsent", "KL");
        }
        else if (input.Policy.HalfDayLateThresholdMinutes > 0 && lateMinutes > input.Policy.HalfDayLateThresholdMinutes)
        {
            actualWorkdays = Math.Round(effectiveShift.WorkdayValue * 0.5m, 2, MidpointRounding.AwayFromZero);
            halfAbsent = Math.Max(0m, effectiveShift.WorkdayValue - actualWorkdays);
            baseWorkSymbol = symbols.GetCode("HalfWork", "½");
        }
        else
        {
            actualWorkdays = effectiveShift.WorkdayValue;
            baseWorkSymbol = symbols.GetCode("Work", "X");
        }

        var note = $"Ca {shiftCode} | Làm {workingHours:0.##}h | Đêm {nightShiftHours:0.##}h | Trễ {lateMinutes}p | Sớm {earlyMinutes}p"
            + (hasApprovedExplanation ? " | Có giải trình được duyệt" : string.Empty)
            + (ot150D + ot150N > 0m ? $" | OT150 ngày {ot150D:0.##}h, đêm {ot150N:0.##}h" : string.Empty);

        return new AttendanceDailyCalculationOutput(
            input.WorkDate,
            shift?.Id,
            shiftCode,
            effectiveIn,
            effectiveOut,
            stdHours,
            workingHours,
            actualWorkdays,
            halfPaidLeave,
            halfSiLeave,
            halfUnpaidLeave,
            halfAbsent,
            rawLate,
            lateMinutes,
            rawEarly,
            earlyMinutes,
            nightShiftHours,
            ot150D,
            ot150N,
            ot200D,
            ot200N,
            ot300D,
            ot300N,
            baseWorkSymbol,
            false,
            hasApprovedExplanation,
            note);
    }

    private static string ResolveLeaveSymbol(
        AttendanceApprovedLeaveInput leave,
        AttendanceSymbolCatalog symbols,
        string? explicitType = null,
        string? explicitFallback = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitType))
            return symbols.GetCode(explicitType, explicitFallback ?? leave.SymbolCode);

        var codeUpper = leave.LeaveTypeCode.Trim().ToUpperInvariant();
        if (leave.IsBusinessTrip || codeUpper == "BUSINESS_TRIP")
            return symbols.GetCode("BusinessTrip", "CT");
        if (codeUpper == "ANNUAL")
            return symbols.GetCode("AnnualLeave", "P");
        if (codeUpper == "SICK")
            return symbols.GetCode("Sick", "Ô");
        if (codeUpper == "MATERNITY")
            return symbols.GetCode("Maternity", "TS");
        if (codeUpper is "MARRIAGE" or "PERSONAL_PAID")
            return symbols.GetCode("PersonalPaid", "R");
        if (codeUpper is "UNPAID" or "PERSONAL_UNPAID" or "UNPAID_PERSONAL")
            return symbols.GetCode("PersonalUnpaid", "Ro");

        return !string.IsNullOrWhiteSpace(leave.SymbolCode)
            ? leave.SymbolCode
            : (leave.IsPaidByCompany ? symbols.GetCode("AnnualLeave", "P") : symbols.GetCode("PersonalUnpaid", "Ro"));
    }

    private static (DateTime? In, DateTime? Out, List<(DateTime Start, DateTime End)> Spans, bool IsMissedPunch)
        ResolvePunchesToIntervals(IReadOnlyList<AttendancePunchInput> punches, string pairingMode)
    {
        if (punches.Count == 1)
        {
            var single = punches[0];
            bool isOutTag = single.PunchType.Contains("Out", StringComparison.OrdinalIgnoreCase);
            return isOutTag
                ? (null, single.PunchTime, new List<(DateTime, DateTime)>(), true)
                : (single.PunchTime, null, new List<(DateTime, DateTime)>(), true);
        }

        if (string.Equals(pairingMode, "PairedSegments", StringComparison.OrdinalIgnoreCase))
        {
            var spans = new List<(DateTime Start, DateTime End)>();
            for (int i = 0; i + 1 < punches.Count; i += 2)
            {
                if (punches[i + 1].PunchTime > punches[i].PunchTime)
                    spans.Add((punches[i].PunchTime, punches[i + 1].PunchTime));
            }
            bool oddMissed = punches.Count % 2 != 0 || spans.Count == 0;
            return (punches.First().PunchTime, punches.Last().PunchTime, spans, oddMissed);
        }

        // Mặc định FirstLast: lấy lần đầu & lần cuối
        var first = punches.First().PunchTime;
        var last = punches.Last().PunchTime;
        if (last <= first)
            return (first, null, new List<(DateTime, DateTime)>(), true);

        return (first, last, new List<(DateTime, DateTime)> { (first, last) }, false);
    }

    private static (DateTime AnchorStart, DateTime AnchorEnd) ResolveLateEarlyAnchors(
        DateOnly workDate,
        WorkShift shift,
        DateTime shiftWinStart,
        DateTime shiftWinEnd,
        AttendanceApprovedLeaveInput? halfLeave)
    {
        DateTime start = string.Equals(shift.ShiftType, "Flexible", StringComparison.OrdinalIgnoreCase) && shift.CoreStartTime.HasValue
            ? workDate.ToDateTime(shift.CoreStartTime.Value)
            : shiftWinStart;

        DateTime end = string.Equals(shift.ShiftType, "Flexible", StringComparison.OrdinalIgnoreCase) && shift.CoreEndTime.HasValue
            ? workDate.ToDateTime(shift.CoreEndTime.Value)
            : shiftWinEnd;

        if (halfLeave == null)
            return (start, end);

        var midBreakStart = shift.BreakStartTime.HasValue
            ? workDate.ToDateTime(shift.BreakStartTime.Value)
            : start.AddMinutes((end - start).TotalMinutes / 2.0);
        var midBreakEnd = shift.BreakEndTime.HasValue
            ? workDate.ToDateTime(shift.BreakEndTime.Value)
            : start.AddMinutes((end - start).TotalMinutes / 2.0);

        if (string.Equals(halfLeave.Session, "Morning", StringComparison.OrdinalIgnoreCase))
            return (midBreakEnd, end);

        if (string.Equals(halfLeave.Session, "Afternoon", StringComparison.OrdinalIgnoreCase))
            return (start, midBreakStart);

        return (start, end);
    }

    private static List<(DateTime Start, DateTime End)> BuildRegularWorkedSpans(
        DateOnly workDate,
        WorkShift shift,
        DateTime shiftWinStart,
        DateTime shiftWinEnd,
        IReadOnlyList<(DateTime Start, DateTime End)> punchSpans)
    {
        var allowedWindows = new List<(DateTime Start, DateTime End)>();
        if (string.Equals(shift.ShiftType, "Split", StringComparison.OrdinalIgnoreCase)
            && shift.SplitStartTime2.HasValue
            && shift.SplitEndTime2.HasValue)
        {
            var seg1Start = workDate.ToDateTime(shift.StartTime);
            var seg1End = shift.EndTime <= shift.StartTime ? workDate.AddDays(1).ToDateTime(shift.EndTime) : workDate.ToDateTime(shift.EndTime);
            var seg2Start = workDate.ToDateTime(shift.SplitStartTime2.Value);
            if (seg2Start < seg1Start) seg2Start = seg2Start.AddDays(1);
            var seg2End = workDate.ToDateTime(shift.SplitEndTime2.Value);
            if (seg2End <= seg2Start) seg2End = seg2End.AddDays(1);

            allowedWindows.Add((seg1Start, seg1End));
            allowedWindows.Add((seg2Start, seg2End));
        }
        else
        {
            allowedWindows.Add((shiftWinStart, shiftWinEnd));
        }

        // Giao giữa các khoảng chấm công với khung ca chuẩn
        var clipped = new List<(DateTime Start, DateTime End)>();
        foreach (var p in punchSpans)
        {
            foreach (var w in allowedWindows)
            {
                var s = p.Start > w.Start ? p.Start : w.Start;
                var e = p.End < w.End ? p.End : w.End;
                if (e > s) clipped.Add((s, e));
            }
        }

        if (shift.IsBreakPaid || !shift.BreakStartTime.HasValue || !shift.BreakEndTime.HasValue)
            return clipped;

        var breakStart = workDate.ToDateTime(shift.BreakStartTime.Value);
        if (breakStart < shiftWinStart) breakStart = breakStart.AddDays(1);
        var breakEnd = workDate.ToDateTime(shift.BreakEndTime.Value);
        if (breakEnd <= breakStart) breakEnd = breakEnd.AddDays(1);

        var finalSpans = new List<(DateTime Start, DateTime End)>();
        foreach (var seg in clipped)
        {
            if (seg.End <= breakStart || seg.Start >= breakEnd)
            {
                finalSpans.Add(seg);
                continue;
            }
            if (seg.Start < breakStart) finalSpans.Add((seg.Start, breakStart));
            if (seg.End > breakEnd) finalSpans.Add((breakEnd, seg.End));
        }

        return finalSpans;
    }

    private static (decimal DayOtHours, decimal NightOtHours) ComputeOvertimeDayNight(
        DateOnly workDate,
        WorkShift? shift,
        IReadOnlyList<AttendancePunchInput> validPunches,
        IReadOnlyList<AttendanceApprovedOvertimeInput> approvedOvertimes,
        AttendancePolicy policy,
        AttendanceStatutoryParameter statutory,
        bool isNonWorkingDay)
    {
        if (approvedOvertimes.Count == 0 && policy.RequireApprovedOvertimeRequest)
            return (0m, 0m);

        decimal dayMinutes = 0m;
        decimal nightMinutes = 0m;

        DateTime? firstPunch = validPunches.FirstOrDefault()?.PunchTime;
        DateTime? lastPunch = validPunches.Count > 1 ? validPunches.Last().PunchTime : null;

        if (approvedOvertimes.Count > 0)
        {
            foreach (var ot in approvedOvertimes)
            {
                var otStart = workDate.ToDateTime(ot.StartTime);
                var otEnd = ot.EndTime <= ot.StartTime
                    ? workDate.AddDays(1).ToDateTime(ot.EndTime)
                    : workDate.ToDateTime(ot.EndTime);

                // Nếu có log chấm công thực tế thì kẹp trong khoảng [firstPunch .. lastPunch], nếu thiếu punch trên ngày OT duyệt vẫn tôn trọng tối đa ApprovedHours
                if (firstPunch.HasValue && lastPunch.HasValue && lastPunch > firstPunch)
                {
                    var effStart = otStart > firstPunch.Value ? otStart : firstPunch.Value;
                    var effEnd = otEnd < lastPunch.Value ? otEnd : lastPunch.Value;
                    if (effEnd <= effStart)
                    {
                        // Không chấm trong khung OT -> dùng otEnd/otStart theo ApprovedHours nếu không yêu cầu punch? Thực tế phải có mặt để tính OT: nếu punch nằm hoàn toàn ngoài -> 0; trừ khi 0 punch hoàn toàn (giả định test chỉ truyền ApprovedOvertime hoặc kèm punch)
                        continue;
                    }
                    otStart = effStart;
                    otEnd = effEnd;
                }

                // Kẹp thời lượng tối đa không vượt quá ApprovedHours (nếu > 0)
                if (ot.ApprovedHours > 0m)
                {
                    var maxEnd = otStart.AddMinutes((double)(ot.ApprovedHours * 60m));
                    if (otEnd > maxEnd) otEnd = maxEnd;
                }

                var nMins = CalculateNightMinutesSingleSpan(otStart, otEnd, statutory.NightWorkStartTime, statutory.NightWorkEndTime);
                var totalMins = Math.Max(0m, (decimal)(otEnd - otStart).TotalMinutes);
                nightMinutes += nMins;
                dayMinutes += Math.Max(0m, totalMins - nMins);
            }
        }
        else if (!policy.RequireApprovedOvertimeRequest && firstPunch.HasValue && lastPunch.HasValue && lastPunch > firstPunch)
        {
            // Trường hợp chính sách cho phép tự tính OT không cần đơn
            if (isNonWorkingDay)
            {
                var nMins = CalculateNightMinutesSingleSpan(firstPunch.Value, lastPunch.Value, statutory.NightWorkStartTime, statutory.NightWorkEndTime);
                var totalMins = (decimal)(lastPunch.Value - firstPunch.Value).TotalMinutes;
                nightMinutes = nMins;
                dayMinutes = Math.Max(0m, totalMins - nMins);
            }
            else if (shift != null)
            {
                var (_, shiftEnd) = AttendancePolicyAndShiftEngine.GetPrimaryShiftWindow(workDate, shift);
                if (lastPunch.Value > shiftEnd)
                {
                    var nMins = CalculateNightMinutesSingleSpan(shiftEnd, lastPunch.Value, statutory.NightWorkStartTime, statutory.NightWorkEndTime);
                    var totalMins = (decimal)(lastPunch.Value - shiftEnd).TotalMinutes;
                    nightMinutes = nMins;
                    dayMinutes = Math.Max(0m, totalMins - nMins);
                }
            }
        }

        return (
            Math.Round(dayMinutes / 60m, 2, MidpointRounding.AwayFromZero),
            Math.Round(nightMinutes / 60m, 2, MidpointRounding.AwayFromZero));
    }

    public static decimal CalculateNightHoursFromSpans(
        IReadOnlyList<(DateTime Start, DateTime End)> spans,
        TimeOnly nightStart,
        TimeOnly nightEnd)
    {
        decimal totalNightMins = 0m;
        foreach (var (s, e) in spans)
        {
            totalNightMins += CalculateNightMinutesSingleSpan(s, e, nightStart, nightEnd);
        }
        return Math.Round(totalNightMins / 60m, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateNightMinutesSingleSpan(DateTime spanStart, DateTime spanEnd, TimeOnly nightStart, TimeOnly nightEnd)
    {
        if (spanEnd <= spanStart) return 0m;

        decimal nightMinutes = 0m;
        var baseDate = DateOnly.FromDateTime(spanStart).AddDays(-1);
        var lastDate = DateOnly.FromDateTime(spanEnd).AddDays(1);

        for (var d = baseDate; d <= lastDate; d = d.AddDays(1))
        {
            var winStart = d.ToDateTime(nightStart);
            var winEnd = nightEnd <= nightStart ? d.AddDays(1).ToDateTime(nightEnd) : d.ToDateTime(nightEnd);
            var s = spanStart > winStart ? spanStart : winStart;
            var e = spanEnd < winEnd ? spanEnd : winEnd;
            if (e > s) nightMinutes += (decimal)(e - s).TotalMinutes;
        }

        return nightMinutes;
    }
}
