using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vinamilk.HRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixEmployerSiRateAndNationalDay2026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "CompanySiRate",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "decimal(7,4)",
                nullable: false,
                defaultValue: 0.17m,
                oldClrType: typeof(decimal),
                oldType: "decimal(7,4)");

            // Sửa trùng 0,5% TNLĐ-BNN trong CompanySiRate (0.175 -> 0.17, kết hợp 0.005 TNLĐ-BNN = 17.5% BHXH DN; tổng 21.5% + KPCĐ 2% tách riêng)
            migrationBuilder.Sql(@"
UPDATE [payroll].[PayrollStatutoryParameters]
SET [CompanySiRate] = 0.1700,
    [CompanyOccupationalAccidentRate] = 0.0050,
    [NeedsHrVerification] = 1,
    [UpdatedAt] = SYSUTCDATETIME(),
    [UpdatedBy] = N'migration.fix.si'
WHERE [IsDeleted] = 0 AND [CompanySiRate] = 0.1750;

IF NOT EXISTS (SELECT 1 FROM [time].[Holidays] WHERE [HolidayDate] = '2026-09-02' AND [IsDeleted] = 0)
BEGIN
    INSERT INTO [time].[Holidays]
        ([HolidayDate], [HolidayName], [Year], [IsPaidHoliday], [OvertimeMultiplier], [IsCompensatoryDay], [LegalReference], [NeedsHrVerification], [CreatedAt], [CreatedBy], [IsDeleted])
    VALUES
        ('2026-09-02', N'Quốc khánh nước CHXHCN Việt Nam (02/09)', 2026, 1, 3.00, 0, N'Điều 112.1.đ BLLĐ 2019', 0, SYSUTCDATETIME(), N'migration.fix.holiday', 0);
END

IF NOT EXISTS (SELECT 1 FROM [time].[Holidays] WHERE [HolidayDate] IN ('2026-09-01', '2026-09-03') AND [IsDeleted] = 0)
BEGIN
    INSERT INTO [time].[Holidays]
        ([HolidayDate], [HolidayName], [Year], [IsPaidHoliday], [OvertimeMultiplier], [IsCompensatoryDay], [LegalReference], [NeedsHrVerification], [CreatedAt], [CreatedBy], [IsDeleted])
    VALUES
        ('2026-09-03', N'Nghỉ liền kề Quốc khánh 2/9 (Dự kiến sau - HR xác nhận 01/09 hoặc 03/09)', 2026, 1, 3.00, 0, N'Điều 112.1.đ BLLĐ 2019 (chờ QĐ Thủ tướng: HR chọn 01/09 trước hoặc 03/09 sau)', 1, SYSUTCDATETIME(), N'migration.fix.holiday', 0);
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE [payroll].[PayrollStatutoryParameters]
SET [CompanySiRate] = 0.1750,
    [UpdatedAt] = SYSUTCDATETIME(),
    [UpdatedBy] = N'migration.rollback'
WHERE [IsDeleted] = 0 AND [UpdatedBy] = N'migration.fix.si';");

            migrationBuilder.AlterColumn<decimal>(
                name: "CompanySiRate",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "decimal(7,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(7,4)",
                oldDefaultValue: 0.17m);
        }
    }
}
