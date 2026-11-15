using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vinamilk.HRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHolidayEffectiveFromAndPolicyGroupCompensatory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Holidays_HolidayDate",
                schema: "time",
                table: "Holidays");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveFrom",
                schema: "time",
                table: "Holidays",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PolicyGroupId",
                schema: "time",
                table: "Holidays",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_PolicyGroupId",
                schema: "time",
                table: "Holidays",
                column: "PolicyGroupId");

            migrationBuilder.CreateIndex(
                name: "UQ_Holidays_HolidayDate_PolicyGroup",
                schema: "time",
                table: "Holidays",
                columns: new[] { "HolidayDate", "PolicyGroupId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Holidays_PolicyGroups_PolicyGroupId",
                schema: "time",
                table: "Holidays",
                column: "PolicyGroupId",
                principalSchema: "time",
                principalTable: "PolicyGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(@"
UPDATE [time].[Holidays]
SET [LegalReference] = N'Nghị quyết 28/2026/QH16 về phát triển văn hóa Việt Nam, Điều 2 khoản 1, hiệu lực 01/7/2026 (chủ trương: NQ 80-NQ/TW)',
    [EffectiveFrom] = '2026-07-01',
    [NeedsHrVerification] = 1
WHERE [IsDeleted] = 0
  AND [HolidayDate] = '2026-11-24';

INSERT INTO [time].[Holidays]
    ([HolidayDate], [HolidayName], [Year], [IsPaidHoliday], [OvertimeMultiplier], [IsCompensatoryDay], [OriginalHolidayDate], [EffectiveFrom], [PolicyGroupId], [LegalReference], [NeedsHrVerification], [CreatedBy], [IsDeleted])
SELECT
    '2026-04-27',
    N'Nghỉ bù Giỗ Tổ Hùng Vương (10/3 ÂL)',
    2026,
    CAST(1 AS bit),
    3.00,
    CAST(1 AS bit),
    '2026-04-26',
    NULL,
    pg.[Id],
    N'Điều 112 BLLĐ 2019 khoản 1(d) & Điều 111 khoản 3 (nghỉ bù ngày lễ trùng ngày nghỉ hằng tuần)',
    CAST(1 AS bit),
    N'migration',
    CAST(0 AS bit)
FROM [time].[PolicyGroups] pg
WHERE pg.[IsDeleted] = 0
  AND pg.[IsActive] = 1
  AND EXISTS (
      SELECT 1 FROM [time].[Holidays] h0
      WHERE h0.[IsDeleted] = 0 AND h0.[HolidayDate] = '2026-04-26'
  )
  AND EXISTS (
      SELECT 1 FROM [time].[AttendancePolicies] ap
      WHERE ap.[IsDeleted] = 0
        AND ap.[PolicyGroupId] = pg.[Id]
        AND CHARINDEX('Sun', ISNULL(ap.[WeeklyWorkdaysMask], '')) = 0
  )
  AND NOT EXISTS (
      SELECT 1 FROM [time].[Holidays] hx
      WHERE hx.[IsDeleted] = 0
        AND hx.[HolidayDate] = '2026-04-27'
        AND (hx.[PolicyGroupId] = pg.[Id] OR hx.[PolicyGroupId] IS NULL)
  )
  AND NOT EXISTS (
      SELECT 1 FROM [time].[Holidays] hc
      WHERE hc.[IsDeleted] = 0
        AND hc.[IsCompensatoryDay] = 1
        AND hc.[OriginalHolidayDate] = '2026-04-26'
        AND hc.[PolicyGroupId] = pg.[Id]
  );
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM [time].[Holidays]
WHERE [IsCompensatoryDay] = 1
  AND [PolicyGroupId] IS NOT NULL;
");

            migrationBuilder.DropForeignKey(
                name: "FK_Holidays_PolicyGroups_PolicyGroupId",
                schema: "time",
                table: "Holidays");

            migrationBuilder.DropIndex(
                name: "IX_Holidays_PolicyGroupId",
                schema: "time",
                table: "Holidays");

            migrationBuilder.DropIndex(
                name: "UQ_Holidays_HolidayDate_PolicyGroup",
                schema: "time",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                schema: "time",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "PolicyGroupId",
                schema: "time",
                table: "Holidays");

            migrationBuilder.CreateIndex(
                name: "UQ_Holidays_HolidayDate",
                schema: "time",
                table: "Holidays",
                column: "HolidayDate",
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
