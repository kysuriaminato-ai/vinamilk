using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vinamilk.HRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftsAndAttendancePolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_ShiftSchedules_Emp_Date",
                schema: "time",
                table: "ShiftSchedules");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "CoreEndTime",
                schema: "time",
                table: "WorkShifts",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "CoreStartTime",
                schema: "time",
                table: "WorkShifts",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBreakPaid",
                schema: "time",
                table: "WorkShifts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxEarlyCheckInMinutes",
                schema: "time",
                table: "WorkShifts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxLateCheckOutMinutes",
                schema: "time",
                table: "WorkShifts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ShiftType",
                schema: "time",
                table: "WorkShifts",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "Fixed");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "SplitEndTime2",
                schema: "time",
                table: "WorkShifts",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "SplitStartTime2",
                schema: "time",
                table: "WorkShifts",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConflictWarning",
                schema: "time",
                table: "ShiftSchedules",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MissedPunchOverQuotaCount",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MissedPunchPenaltyAmount",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompensatoryDay",
                schema: "time",
                table: "Holidays",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LegalReference",
                schema: "time",
                table: "Holidays",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsHrVerification",
                schema: "time",
                table: "Holidays",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "OriginalHolidayDate",
                schema: "time",
                table: "Holidays",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttendanceStatutoryParameters",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    LegalReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    NeedsHrVerification = table.Column<bool>(type: "bit", nullable: false),
                    NightWorkStartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    NightWorkEndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    MinInterShiftRestHours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MaxNormalDailyHours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MaxNormalWeeklyHours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MaxMonthlyOvertimeHours = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    MaxYearlyOvertimeHours = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    WeekdayOtMultiplier = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    WeeklyDayOffOtMultiplier = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    HolidayOtMultiplier = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    NightShiftBonusRate = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    NightOtExtraBonusRate = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceStatutoryParameters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PolicyGroups",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyCode = table.Column<string>(type: "varchar(40)", nullable: false),
                    PolicyName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsCompanyDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShiftRotationPatterns",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatternCode = table.Column<string>(type: "varchar(40)", nullable: false),
                    PatternName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CycleDays = table.Column<int>(type: "int", nullable: false),
                    CrewCount = table.Column<int>(type: "int", nullable: false),
                    ShiftSequenceCsv = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CrewOffsetDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftRotationPatterns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShiftSwapRequests",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequesterEmployeeId = table.Column<int>(type: "int", nullable: false),
                    TargetEmployeeId = table.Column<int>(type: "int", nullable: false),
                    RequesterWorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TargetWorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequesterOriginalShiftId = table.Column<int>(type: "int", nullable: true),
                    TargetOriginalShiftId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Pending"),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApproverComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftSwapRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_Employees_RequesterEmployeeId",
                        column: x => x.RequesterEmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftSwapRequests_Employees_TargetEmployeeId",
                        column: x => x.TargetEmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendancePolicies",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyGroupId = table.Column<int>(type: "int", nullable: false),
                    GraceLateMinutes = table.Column<int>(type: "int", nullable: false),
                    GraceEarlyMinutes = table.Column<int>(type: "int", nullable: false),
                    RoundingIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    RoundingMode = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "Nearest"),
                    HalfDayLateThresholdMinutes = table.Column<int>(type: "int", nullable: false),
                    ZeroDayLateThresholdMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxMissedPunchExplanationsPerMonth = table.Column<int>(type: "int", nullable: false),
                    MissedPunchOverQuotaAction = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "FixedFine"),
                    MissedPunchPenaltyAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    RequireApprovedOvertimeRequest = table.Column<bool>(type: "bit", nullable: false),
                    WeeklyWorkdaysMask = table.Column<string>(type: "varchar(80)", nullable: false, defaultValue: "Mon,Tue,Wed,Thu,Fri"),
                    MaxWeeklyWorkingHoursOverride = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendancePolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendancePolicies_PolicyGroups_PolicyGroupId",
                        column: x => x.PolicyGroupId,
                        principalSchema: "time",
                        principalTable: "PolicyGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PolicyGroupAssignments",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyGroupId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    PositionId = table.Column<int>(type: "int", nullable: true),
                    OrganizationUnitId = table.Column<int>(type: "int", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyGroupAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolicyGroupAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PolicyGroupAssignments_OrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalSchema: "core",
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PolicyGroupAssignments_PolicyGroups_PolicyGroupId",
                        column: x => x.PolicyGroupId,
                        principalSchema: "time",
                        principalTable: "PolicyGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PolicyGroupAssignments_Positions_PositionId",
                        column: x => x.PositionId,
                        principalSchema: "core",
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_ShiftSchedules_Emp_Date_Shift",
                schema: "time",
                table: "ShiftSchedules",
                columns: new[] { "EmployeeId", "WorkDate", "WorkShiftId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_AttendancePolicies_PolicyGroupId",
                schema: "time",
                table: "AttendancePolicies",
                column: "PolicyGroupId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceStatutoryParameters_Effective",
                schema: "time",
                table: "AttendanceStatutoryParameters",
                columns: new[] { "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyGroupAssignments_EmployeeId",
                schema: "time",
                table: "PolicyGroupAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyGroupAssignments_OrganizationUnitId",
                schema: "time",
                table: "PolicyGroupAssignments",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyGroupAssignments_PositionId",
                schema: "time",
                table: "PolicyGroupAssignments",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyGroupAssignments_Scope",
                schema: "time",
                table: "PolicyGroupAssignments",
                columns: new[] { "PolicyGroupId", "EmployeeId", "PositionId", "OrganizationUnitId" });

            migrationBuilder.CreateIndex(
                name: "UQ_PolicyGroups_Code",
                schema: "time",
                table: "PolicyGroups",
                column: "PolicyCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_ShiftRotationPatterns_Code",
                schema: "time",
                table: "ShiftRotationPatterns",
                column: "PatternCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_Emp_Status",
                schema: "time",
                table: "ShiftSwapRequests",
                columns: new[] { "RequesterEmployeeId", "TargetEmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSwapRequests_TargetEmployeeId",
                schema: "time",
                table: "ShiftSwapRequests",
                column: "TargetEmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendancePolicies",
                schema: "time");

            migrationBuilder.DropTable(
                name: "AttendanceStatutoryParameters",
                schema: "time");

            migrationBuilder.DropTable(
                name: "PolicyGroupAssignments",
                schema: "time");

            migrationBuilder.DropTable(
                name: "ShiftRotationPatterns",
                schema: "time");

            migrationBuilder.DropTable(
                name: "ShiftSwapRequests",
                schema: "time");

            migrationBuilder.DropTable(
                name: "PolicyGroups",
                schema: "time");

            migrationBuilder.DropIndex(
                name: "UQ_ShiftSchedules_Emp_Date_Shift",
                schema: "time",
                table: "ShiftSchedules");

            migrationBuilder.DropColumn(
                name: "CoreEndTime",
                schema: "time",
                table: "WorkShifts");

            migrationBuilder.DropColumn(
                name: "CoreStartTime",
                schema: "time",
                table: "WorkShifts");

            migrationBuilder.DropColumn(
                name: "IsBreakPaid",
                schema: "time",
                table: "WorkShifts");

            migrationBuilder.DropColumn(
                name: "MaxEarlyCheckInMinutes",
                schema: "time",
                table: "WorkShifts");

            migrationBuilder.DropColumn(
                name: "MaxLateCheckOutMinutes",
                schema: "time",
                table: "WorkShifts");

            migrationBuilder.DropColumn(
                name: "ShiftType",
                schema: "time",
                table: "WorkShifts");

            migrationBuilder.DropColumn(
                name: "SplitEndTime2",
                schema: "time",
                table: "WorkShifts");

            migrationBuilder.DropColumn(
                name: "SplitStartTime2",
                schema: "time",
                table: "WorkShifts");

            migrationBuilder.DropColumn(
                name: "ConflictWarning",
                schema: "time",
                table: "ShiftSchedules");

            migrationBuilder.DropColumn(
                name: "MissedPunchOverQuotaCount",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "MissedPunchPenaltyAmount",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "IsCompensatoryDay",
                schema: "time",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "LegalReference",
                schema: "time",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "NeedsHrVerification",
                schema: "time",
                table: "Holidays");

            migrationBuilder.DropColumn(
                name: "OriginalHolidayDate",
                schema: "time",
                table: "Holidays");

            migrationBuilder.CreateIndex(
                name: "UQ_ShiftSchedules_Emp_Date",
                schema: "time",
                table: "ShiftSchedules",
                columns: new[] { "EmployeeId", "WorkDate" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
