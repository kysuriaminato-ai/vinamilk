using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vinamilk.HRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVietnamCultureDayAndDefaultNationalDay0109 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- 1. Bổ sung Ngày Văn hóa Việt Nam 24/11 từ năm 2026 (đủ 12 ngày lễ luật định)
IF NOT EXISTS (SELECT 1 FROM [time].[Holidays] WHERE [HolidayDate] = '2026-11-24' AND [IsDeleted] = 0)
BEGIN
    INSERT INTO [time].[Holidays]
        ([HolidayDate], [HolidayName], [Year], [IsPaidHoliday], [OvertimeMultiplier], [IsCompensatoryDay], [LegalReference], [NeedsHrVerification], [CreatedAt], [CreatedBy], [IsDeleted])
    VALUES
        ('2026-11-24', N'Ngày Văn hóa Việt Nam (24/11)', 2026, 1, 3.00, 0, N'Nghị quyết 80-NQ/TW ngày 07/01/2026; BLLĐ 2019 sửa đổi bổ sung Điều 112 (áp dụng từ 2026)', 1, SYSUTCDATETIME(), N'migration.holiday.2411', 0);
END

-- 2. Đổi mặc định ngày liền kề Quốc khánh 2026 từ 03/09 sang 01/09 theo Thông báo 9441/TB-BNV (nếu HR chưa xác nhận 03/09)
IF EXISTS (SELECT 1 FROM [time].[Holidays] WHERE [HolidayDate] = '2026-09-03' AND [IsDeleted] = 0 AND [NeedsHrVerification] = 1)
   AND NOT EXISTS (SELECT 1 FROM [time].[Holidays] WHERE [HolidayDate] = '2026-09-01' AND [IsDeleted] = 0)
BEGIN
    UPDATE [time].[Holidays]
    SET [IsDeleted] = 1,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedBy] = N'migration.holiday.9441'
    WHERE [HolidayDate] = '2026-09-03' AND [IsDeleted] = 0 AND [NeedsHrVerification] = 1;

    INSERT INTO [time].[Holidays]
        ([HolidayDate], [HolidayName], [Year], [IsPaidHoliday], [OvertimeMultiplier], [IsCompensatoryDay], [LegalReference], [NeedsHrVerification], [CreatedAt], [CreatedBy], [IsDeleted])
    VALUES
        ('2026-09-01', N'Nghỉ liền kề Quốc khánh 2/9 (01/09 theo TB 9441/TB-BNV)', 2026, 1, 3.00, 0, N'Điều 112.1.đ BLLĐ 45/2019/QH14; Thông báo 9441/TB-BNV (mặc định 01/09, HR chọn 01/09 hoặc 03/09)', 1, SYSUTCDATETIME(), N'migration.holiday.9441', 0);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM [time].[Holidays] WHERE [HolidayDate] = '2026-11-24' AND [CreatedBy] = N'migration.holiday.2411';

IF EXISTS (SELECT 1 FROM [time].[Holidays] WHERE [HolidayDate] = '2026-09-01' AND [CreatedBy] = N'migration.holiday.9441')
BEGIN
    DELETE FROM [time].[Holidays] WHERE [HolidayDate] = '2026-09-01' AND [CreatedBy] = N'migration.holiday.9441';
    UPDATE [time].[Holidays]
    SET [IsDeleted] = 0,
        [UpdatedAt] = SYSUTCDATETIME(),
        [UpdatedBy] = N'migration.rollback'
    WHERE [HolidayDate] = '2026-09-03' AND [UpdatedBy] = N'migration.holiday.9441';
END");
        }
    }
}
