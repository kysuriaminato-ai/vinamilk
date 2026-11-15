using System.Text;
using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.TimeAttendance;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Time;
using Vinamilk.HRM.Infrastructure.Persistence;
using Vinamilk.HRM.Infrastructure.Services;
using Xunit;

namespace Vinamilk.HRM.UnitTests.TimeAttendance;

public class AttendanceDataSourcesTests
{
    private sealed class MockCurrentUser(int userId = 10, string username = "hr.specialist") : ICurrentUserService
    {
        public int? UserId => userId;
        public string Username => username;
        public int? EmployeeId => null;
        public string? IpAddress => "10.10.1.99";
        public string? UserAgent => "UnitTest/CL2";
        public bool IsAuthenticated => true;
        public bool HasPermission(string permissionCode) => true;
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);

        db.AttendanceStatutoryParameters.Add(new AttendanceStatutoryParameter
        {
            EffectiveFrom = new DateOnly(2021, 1, 1),
            EffectiveTo = null,
            LegalReference = "BLLD 45/2019/QH14",
            NeedsHrVerification = true,
            NightWorkStartTime = new TimeOnly(22, 0),
            NightWorkEndTime = new TimeOnly(6, 0),
            MinInterShiftRestHours = 12m,
            MaxNormalDailyHours = 8m,
            MaxNormalWeeklyHours = 48m,
            MaxMonthlyOvertimeHours = 40m,
            MaxYearlyOvertimeHours = 200m,
            WeekdayOtMultiplier = 1.5m,
            WeeklyDayOffOtMultiplier = 2.0m,
            HolidayOtMultiplier = 3.0m,
            NightShiftBonusRate = 0.30m
        });

        var defaultGroup = new PolicyGroup
        {
            PolicyCode = "DEFAULT_CO",
            PolicyName = "Mặc định công ty",
            IsCompanyDefault = true,
            IsActive = true
        };
        defaultGroup.AttendancePolicies.Add(new AttendancePolicy
        {
            GraceLateMinutes = 5,
            GraceEarlyMinutes = 5,
            RoundingIntervalMinutes = 5,
            RoundingMode = "Nearest",
            HalfDayLateThresholdMinutes = 60,
            ZeroDayLateThresholdMinutes = 240,
            MaxMissedPunchExplanationsPerMonth = 3,
            MissedPunchOverQuotaAction = "FixedFine",
            MissedPunchPenaltyAmount = 100000m,
            WeeklyWorkdaysMask = "Mon,Tue,Wed,Thu,Fri"
        });
        db.PolicyGroups.Add(defaultGroup);
        db.SaveChanges();
        return db;
    }

    private static Employee SeedEmployee(ApplicationDbContext db, string empCode = "VNM201", string fullName = "Lê Văn Công")
    {
        var unit = db.OrganizationUnits.FirstOrDefault() ?? new OrganizationUnit
        {
            UnitCode = "HQ",
            UnitName = "Trụ sở chính",
            UnitLevel = 1,
            TreePath = "/1/"
        };
        if (unit.Id == 0) db.OrganizationUnits.Add(unit);

        var pos = db.Positions.FirstOrDefault() ?? new Position
        {
            PositionCode = "CV",
            PositionName = "Chuyên viên"
        };
        if (pos.Id == 0) db.Positions.Add(pos);
        db.SaveChanges();

        var emp = new Employee
        {
            EmployeeCode = empCode,
            FullName = fullName,
            OrganizationUnitId = unit.Id,
            PositionId = pos.Id,
            WorkStatus = "Official"
        };
        db.Employees.Add(emp);
        db.SaveChanges();
        return emp;
    }

    [Fact]
    public async Task BiometricFileParser_ShouldSupportCsvTxtDat_AndRejectDuplicatePunchesAcrossSyncs()
    {
        // Kịch bản thực tế: máy vân tay xuất 4 dòng trong file hỗn hợp CSV + ZKTeco DAT (.dat tab-separated),
        // trong đó có 1 dòng trùng ngay trong file (cùng máy, cùng user, cùng giây),
        // và khi sync lặp lại toàn bộ file lần 2 thì 0 log mới được thêm (toàn bộ bị loại trùng theo ExternalLogKey).
        using var db = CreateDb();
        var emp = SeedEmployee(db, "VNM201", "Trần Vân Tay");
        db.EmployeeDeviceMappings.Add(new EmployeeDeviceMapping
        {
            EmployeeId = emp.Id,
            DeviceCode = "BIO-HQ-01",
            DeviceUserCode = "1001",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var payload = string.Join("\n",
            "UserID,PunchTime,Type",
            "1001,2026-06-15 08:00:00,0",
            "1001,2026-06-15 08:00:00,0", // trùng trong cùng file
            "1001\t2026-06-15 17:05:00\t1"); // ZKTeco DAT tab-separated
        var bytes = Encoding.UTF8.GetBytes(payload);

        var connector = new FileAttendanceDeviceConnector();
        var handler = new IngestBiometricFileCommandHandler(db, connector, new MockCurrentUser());

        var firstRun = await handler.Handle(new IngestBiometricFileCommand("BIO-HQ-01", bytes), CancellationToken.None);
        firstRun.TotalRead.Should().Be(3);
        firstRun.ImportedCount.Should().Be(2);
        firstRun.SkippedDuplicateCount.Should().Be(1);
        firstRun.UnmappedCount.Should().Be(0);

        var secondRun = await handler.Handle(new IngestBiometricFileCommand("BIO-HQ-01", bytes), CancellationToken.None);
        secondRun.TotalRead.Should().Be(3);
        secondRun.ImportedCount.Should().Be(0);
        secondRun.SkippedDuplicateCount.Should().Be(3);

        var logs = await db.AttendanceLogs.Where(l => l.EmployeeId == emp.Id).ToListAsync();
        logs.Should().HaveCount(2);
        logs.All(l => l.SourceType == "BiometricDevice" && !string.IsNullOrWhiteSpace(l.ExternalLogKey)).Should().BeTrue();
    }

    [Fact]
    public async Task ScheduledDeviceSyncJob_ShouldSyncFileDeviceUnderSystemJobAccount_WithoutDuplicates()
    {
        using var db = CreateDb();
        var emp = SeedEmployee(db, "VNM202", "Phạm Đồng Bộ");
        db.EmployeeDeviceMappings.Add(new EmployeeDeviceMapping
        {
            EmployeeId = emp.Id,
            DeviceCode = "DEV-SYNC-01",
            DeviceUserCode = "2002",
            IsActive = true
        });

        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "2002;2026-06-16 07:58:00;IN\n2002;2026-06-16 17:02:00;OUT");
            db.AttendanceDevices.Add(new AttendanceDevice
            {
                DeviceCode = "DEV-SYNC-01",
                DeviceName = "Máy chấm công thử nghiệm",
                ConnectorType = "FileExport",
                EndpointOrFolderPath = tempFile,
                SyncIntervalMinutes = 10,
                IsActive = true
            });
            await db.SaveChangesAsync();

            var job = new AttendanceDeviceSyncJob(
                db,
                new IAttendanceDeviceConnector[] { new FileAttendanceDeviceConnector() },
                NullLogger<AttendanceDeviceSyncJob>.Instance);

            var run1 = await job.SyncAllActiveDevicesAsync();
            run1.Should().ContainSingle(s => s.DeviceCode == "DEV-SYNC-01" && s.ImportedCount == 2);

            var run2 = await job.SyncAllActiveDevicesAsync();
            run2.Should().ContainSingle(s => s.DeviceCode == "DEV-SYNC-01" && s.ImportedCount == 0 && s.SkippedDuplicateCount == 2);

            var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.ActionType == "AttendanceDeviceSync");
            audit.Should().NotBeNull();
            audit!.Username.Should().Be(SystemJobAccount.Username);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task UnmappedBiometricPunch_ShouldBeQueuedWithoutDataLoss_AndReplayedWhenMapped()
    {
        // Nhân viên mới chưa kịp map mã máy "9908" chấm 2 lần lúc 08:00 và 17:00.
        // Hệ thống không được làm rớt log mà lưu vào UnmappedAttendanceLogs (PendingMapping).
        // Khi HR chạy MapEmployeeDeviceAndReplayCommand -> cả 2 log được đẩy vào AttendanceLogs và Unmapped đổi Resolved.
        using var db = CreateDb();
        var emp = SeedEmployee(db, "VNM203", "Ngô Chờ Ánh Xạ");

        var connector = new FileAttendanceDeviceConnector();
        var ingestHandler = new IngestBiometricFileCommandHandler(db, connector, new MockCurrentUser());
        var bytes = Encoding.UTF8.GetBytes("9908,2026-06-18 08:02:00,0\n9908,2026-06-18 17:04:00,1");

        var syncRes = await ingestHandler.Handle(new IngestBiometricFileCommand("DEV-PLANT-01", bytes), CancellationToken.None);
        syncRes.ImportedCount.Should().Be(0);
        syncRes.UnmappedCount.Should().Be(2);

        (await db.UnmappedAttendanceLogs.CountAsync(u => u.Status == "PendingMapping")).Should().Be(2);
        (await db.AttendanceLogs.CountAsync(l => l.EmployeeId == emp.Id)).Should().Be(0);

        var mapHandler = new MapEmployeeDeviceAndReplayCommandHandler(db, new MockCurrentUser());
        var replayed = await mapHandler.Handle(
            new MapEmployeeDeviceAndReplayCommand(emp.Id, "9908", "DEV-PLANT-01", "Nhân viên mới thử việc"),
            CancellationToken.None);

        replayed.Should().Be(2);
        (await db.UnmappedAttendanceLogs.CountAsync(u => u.Status == "PendingMapping")).Should().Be(0);
        (await db.UnmappedAttendanceLogs.CountAsync(u => u.Status == "Resolved" && u.ResolvedEmployeeId == emp.Id)).Should().Be(2);
        (await db.AttendanceLogs.CountAsync(l => l.EmployeeId == emp.Id)).Should().Be(2);

        var daily = await db.DailyAttendances.FirstOrDefaultAsync(d => d.EmployeeId == emp.Id && d.AttendanceDate == new DateOnly(2026, 6, 18));
        daily.Should().NotBeNull();
    }

    [Fact]
    public async Task GeofenceOutsideRadius_ShouldRejectOrMarkPendingApprovalAccordingToLocationPolicy()
    {
        // Tọa độ Trụ sở TP.HCM ~ (10.7296, 106.7218), NV chấm ở Hà Nội (21.0285, 105.8542) -> ngoài vùng > 1,100km.
        // Cấu hình Reject -> ném InvalidOperationException.
        // Cấu hình PendingApproval -> cho ghi nhận AttendanceLog với GeofenceStatus="OutsidePendingApproval" và Daily Symbol="CGD", WorkdayCount=0.
        using var db = CreateDb();
        var emp = SeedEmployee(db, "VNM204", "Hoàng Địa Lý");

        var plantLoc = new GeofenceLocation
        {
            LocationCode = "VNM-BD",
            LocationName = "Nhà máy Bình Dương",
            Latitude = 11.1246m,
            Longitude = 106.6184m,
            AllowedRadiusMeters = 400,
            OutsideGeofenceAction = "Reject",
            QrRotationSeconds = 30,
            IsActive = true
        };
        db.GeofenceLocations.Add(plantLoc);
        await db.SaveChangesAsync();

        var handler = new WebCheckInCommandHandler(db);
        var rejectAct = () => handler.Handle(new WebCheckInCommand(
            emp.Id,
            "CheckIn",
            "MobileGps",
            21.0285,
            105.8542,
            OverridePunchTime: new DateTime(2026, 6, 19, 8, 5, 0)), CancellationToken.None);

        await rejectAct.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*vượt bán kính*");

        // Chuyển cấu hình sang PendingApproval
        plantLoc.OutsideGeofenceAction = "PendingApproval";
        await db.SaveChangesAsync();

        var pendingMsg = await handler.Handle(new WebCheckInCommand(
            emp.Id,
            "CheckIn",
            "MobileGps",
            21.0285,
            105.8542,
            OverridePunchTime: new DateTime(2026, 6, 19, 8, 5, 0)), CancellationToken.None);

        pendingMsg.Should().Contain("Chờ duyệt");
        var log = await db.AttendanceLogs.FirstAsync(l => l.EmployeeId == emp.Id);
        log.GeofenceStatus.Should().Be("OutsidePendingApproval");

        var daily = await db.DailyAttendances.FirstAsync(d => d.EmployeeId == emp.Id && d.AttendanceDate == new DateOnly(2026, 6, 19));
        daily.AttendanceSymbol.Should().Be("CGD");
        daily.WorkdayCount.Should().Be(0m);
    }

    [Fact]
    public async Task DynamicQr30s_ShouldRejectExpiredToken_AndAntiBuddyPunchingShouldBlockSharedDevice()
    {
        using var db = CreateDb();
        var emp1 = SeedEmployee(db, "VNM205", "Nguyễn Thiết Bị 1");
        var emp2 = SeedEmployee(db, "VNM206", "Lê Thiết Bị 2");

        db.GeofenceLocations.Add(new GeofenceLocation
        {
            LocationCode = "HQ_SG",
            LocationName = "Trụ sở chính",
            Latitude = 10.7296m,
            Longitude = 106.7218m,
            AllowedRadiusMeters = 300,
            OutsideGeofenceAction = "Reject",
            QrRotationSeconds = 30,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var nowUtc = new DateTime(2026, 6, 20, 1, 0, 15, DateTimeKind.Utc);
        var expiredQr = VinamilkGeofenceCalculator.GenerateDynamicQrToken(nowUtc.AddSeconds(-35), 30);
        var validQr = VinamilkGeofenceCalculator.GenerateDynamicQrToken(nowUtc, 30);

        var handler = new WebCheckInCommandHandler(db);

        // 1. Mã QR xoay 30s của slot trước đó (-35s) phải bị từ chối
        var expiredAct = () => handler.Handle(new WebCheckInCommand(
            emp1.Id,
            "CheckIn",
            "WebQr",
            10.7296,
            106.7218,
            QrToken: expiredQr,
            OverridePunchTime: new DateTime(2026, 6, 20, 8, 0, 0),
            ClientDeviceId: "DEV-EMP1-SERIAL",
            OverrideUtcNow: nowUtc), CancellationToken.None);

        await expiredAct.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*không hợp lệ hoặc đã hết hạn*");

        // 2. Emp1 dùng mã QR hợp lệ + DeviceId "DEV-EMP1-SERIAL" -> thành công & gắn binding
        await handler.Handle(new WebCheckInCommand(
            emp1.Id,
            "CheckIn",
            "WebQr",
            10.7296,
            106.7218,
            QrToken: validQr,
            OverridePunchTime: new DateTime(2026, 6, 20, 8, 0, 0),
            ClientDeviceId: "DEV-EMP1-SERIAL",
            OverrideUtcNow: nowUtc), CancellationToken.None);

        // 3. Emp2 mượn chính thiết bị "DEV-EMP1-SERIAL" chấm công hộ -> bị từ chối
        var buddyAct = () => handler.Handle(new WebCheckInCommand(
            emp2.Id,
            "CheckIn",
            "WebQr",
            10.7296,
            106.7218,
            QrToken: validQr,
            OverridePunchTime: new DateTime(2026, 6, 20, 8, 1, 0),
            ClientDeviceId: "DEV-EMP1-SERIAL",
            OverrideUtcNow: nowUtc), CancellationToken.None);

        await buddyAct.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*chặn chấm công hộ*");
    }

    [Fact]
    public async Task ExcelTwoStepImport_PreviewShouldNotWriteDb_AndConfirmShouldWriteOnlyValidRowsWithAuditLog()
    {
        using var db = CreateDb();
        var emp = SeedEmployee(db, "VNM207", "Vũ Excel Hai Bước");
        db.WorkShifts.Add(new WorkShift
        {
            ShiftCode = "HC",
            ShiftName = "Ca Hành chính",
            ShiftType = "Day",
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(17, 0),
            BreakStartTime = new TimeOnly(12, 0),
            BreakEndTime = new TimeOnly(13, 0),
            StandardHours = 8m,
            WorkdayValue = 1m
        });
        await db.SaveChangesAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Import");
        ws.Cell(1, 1).Value = "MaNV";
        ws.Cell(1, 2).Value = "HoTen";
        ws.Cell(1, 3).Value = "NgayCong";
        ws.Cell(1, 4).Value = "MaCa";
        ws.Cell(1, 5).Value = "GioVao";
        ws.Cell(1, 6).Value = "GioRa";
        ws.Cell(1, 7).Value = "KyHieu";
        ws.Cell(1, 8).Value = "OT150";
        ws.Cell(1, 9).Value = "OT200";
        ws.Cell(1, 10).Value = "OT300";
        ws.Cell(1, 11).Value = "MaThietBi";

        // Dòng 2 hợp lệ (đúng layout 11 cột template chuẩn)
        ws.Cell(2, 1).Value = "VNM207";
        ws.Cell(2, 2).Value = "Vũ Excel Hai Bước";
        ws.Cell(2, 3).Value = "2026-06-22";
        ws.Cell(2, 4).Value = "HC";
        ws.Cell(2, 5).Value = "08:00";
        ws.Cell(2, 6).Value = "17:00";
        ws.Cell(2, 7).Value = "X";
        ws.Cell(2, 11).Value = "MCC-EXCEL";

        // Dòng 3 lỗi: mã NV không tồn tại
        ws.Cell(3, 1).Value = "UNKNOWN_NV";
        ws.Cell(3, 2).Value = "Người Lạ";
        ws.Cell(3, 3).Value = "2026-06-22";
        ws.Cell(3, 5).Value = "08:00";
        ws.Cell(3, 6).Value = "17:00";

        // Dòng 4 lỗi: sai định dạng ngày
        ws.Cell(4, 1).Value = "VNM207";
        ws.Cell(4, 2).Value = "Vũ Excel Hai Bước";
        ws.Cell(4, 3).Value = "not-a-date";
        ws.Cell(4, 5).Value = "08:00";

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        var service = new TimeAttendanceDocumentService(db);

        // Bước 1: Preview -> không được ghi bất kỳ bản ghi nào vào CSDL
        var preview = await service.PreviewAttendanceImportFromExcelAsync(ms);
        preview.TotalRows.Should().Be(3);
        preview.ValidCount.Should().Be(1);
        preview.ErrorCount.Should().Be(2);
        preview.Rows.Should().Contain(r => !r.IsValid && r.EmployeeCode == "UNKNOWN_NV");
        preview.Rows.Should().Contain(r => !r.IsValid && r.WorkDateText == "not-a-date");

        (await db.DailyAttendances.CountAsync()).Should().Be(0);
        (await db.AttendanceLogs.CountAsync()).Should().Be(0);
        (await db.AuditLogs.CountAsync()).Should().Be(0);

        // Bước 2: Confirm -> chỉ ghi dòng hợp lệ & sinh AuditLog
        var confirm = await service.ConfirmAttendanceImportAsync(preview.Rows, 15, "hr.importer", "10.0.0.15");
        confirm.SuccessCount.Should().Be(1);
        confirm.ErrorCount.Should().Be(2);

        (await db.DailyAttendances.CountAsync(d => d.EmployeeId == emp.Id)).Should().Be(1);
        (await db.AttendanceLogs.CountAsync(l => l.EmployeeId == emp.Id && l.SourceType == "ExcelImport")).Should().Be(2);
        (await db.AuditLogs.CountAsync(a => a.Username == "hr.importer" && a.ActionType == "ExcelImportConfirm")).Should().Be(1);
    }

    [Fact]
    public async Task ManualHrAttendance_ShouldRequireReason_BlockLockedMonth_WriteAuditAndHighlightBoard()
    {
        using var db = CreateDb();
        var emp = SeedEmployee(db, "VNM208", "Đặng Nhập Tay HR");
        var handler = new UpsertManualAttendanceLogCommandHandler(db, new MockCurrentUser(22, "hr.director"));

        // 1. Không nhập lý do -> từ chối
        var noReasonAct = () => handler.Handle(new UpsertManualAttendanceLogCommand(
            emp.Id,
            new DateOnly(2026, 6, 23),
            new TimeOnly(8, 0),
            new TimeOnly(17, 0),
            "X",
            "   "), CancellationToken.None);

        await noReasonAct.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*bắt buộc phải nhập lý do*");

        // 2. Kỳ công đã khóa -> từ chối
        db.MonthlyAttendanceSummaries.Add(new MonthlyAttendanceSummary
        {
            EmployeeId = emp.Id,
            Month = 5,
            Year = 2026,
            IsLocked = true,
            LockedBy = "admin",
            LockedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var lockedAct = () => handler.Handle(new UpsertManualAttendanceLogCommand(
            emp.Id,
            new DateOnly(2026, 5, 12),
            new TimeOnly(8, 0),
            new TimeOnly(17, 0),
            "X",
            "Quên chấm công khi công tác"), CancellationToken.None);

        await lockedAct.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*đã khóa*");

        // 3. Tháng chưa khóa + đầy đủ lý do -> ghi log ManualHr, Daily.IsManualEdited=true, AuditLog và tô màu nổi bật
        var dailyId = await handler.Handle(new UpsertManualAttendanceLogCommand(
            emp.Id,
            new DateOnly(2026, 6, 23),
            new TimeOnly(8, 0),
            new TimeOnly(17, 0),
            "X",
            "Khách hàng gặp sáng sớm quên quét thẻ"), CancellationToken.None);

        dailyId.Should().BeGreaterThan(0);

        var manualLogs = await db.AttendanceLogs.Where(l => l.EmployeeId == emp.Id && l.SourceType == "ManualHr").ToListAsync();
        manualLogs.Should().HaveCount(2);
        manualLogs.All(l => l.EnteredByUserId == 22 && l.EnteredByUsername == "hr.director" && l.IsManualOverride).Should().BeTrue();

        var audit = await db.AuditLogs.FirstOrDefaultAsync(a => a.ActionType == "ManualAttendanceOverride" && a.Username == "hr.director");
        audit.Should().NotBeNull();

        var boardHandler = new GetMonthlyAttendanceBoardQueryHandler(db);
        var board = await boardHandler.Handle(new GetMonthlyAttendanceBoardQuery(6, 2026, emp.OrganizationUnitId, null), CancellationToken.None);
        var day23 = board.MatrixRows.Single(r => r.EmployeeId == emp.Id).Days.Single(d => d.Day == 23);

        day23.IsManualEdited.Should().BeTrue();
        day23.CssClass.Should().Contain("vnm-manual-edited-cell");
    }
}
