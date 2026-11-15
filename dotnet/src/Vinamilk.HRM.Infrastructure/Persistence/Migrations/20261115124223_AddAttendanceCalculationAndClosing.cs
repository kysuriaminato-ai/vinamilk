using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vinamilk.HRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceCalculationAndClosing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClosingStatus",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "varchar(30)",
                nullable: false,
                defaultValue: "Open");

            migrationBuilder.AddColumn<DateTime>(
                name: "ManagerConfirmedAt",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerConfirmedBy",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewDeadlineAt",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewWindowDays",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalEarlyLeaveCount",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalEarlyLeaveMinutes",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalMissedPunchCount",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalOtHours150Day",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalOtHours150Night",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalOtHours200Day",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalOtHours200Night",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalOtHours300Day",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalOtHours300Night",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPaidPersonalLeaveDays",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalUnexcusedAbsentDays",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalUnpaidPersonalLeaveDays",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnitSubmittedAt",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitSubmittedBy",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AttendanceSymbol",
                schema: "time",
                table: "DailyAttendances",
                type: "nvarchar(20)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(10)");

            migrationBuilder.AddColumn<bool>(
                name: "IsMissedPunch",
                schema: "time",
                table: "DailyAttendances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OvertimeHours150Day",
                schema: "time",
                table: "DailyAttendances",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OvertimeHours150Night",
                schema: "time",
                table: "DailyAttendances",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OvertimeHours200Day",
                schema: "time",
                table: "DailyAttendances",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OvertimeHours200Night",
                schema: "time",
                table: "DailyAttendances",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OvertimeHours300Day",
                schema: "time",
                table: "DailyAttendances",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OvertimeHours300Night",
                schema: "time",
                table: "DailyAttendances",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidLeaveDays",
                schema: "time",
                table: "DailyAttendances",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardHours",
                schema: "time",
                table: "DailyAttendances",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnpaidLeaveDays",
                schema: "time",
                table: "DailyAttendances",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeReviewWindowDays",
                schema: "time",
                table: "AttendancePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MissedPunchCreditWorkdays",
                schema: "time",
                table: "AttendancePolicies",
                type: "decimal(4,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PunchPairingMode",
                schema: "time",
                table: "AttendancePolicies",
                type: "varchar(30)",
                nullable: false,
                defaultValue: "FirstLast");

            migrationBuilder.CreateTable(
                name: "AttendanceRecalculationBatches",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchCode = table.Column<string>(type: "varchar(60)", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    OrganizationUnitId = table.Column<int>(type: "int", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Queued"),
                    TotalEmployees = table.Column<int>(type: "int", nullable: false),
                    ProcessedEmployees = table.Column<int>(type: "int", nullable: false),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    RequestedByUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecalculationBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceSymbolDefinitions",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SymbolTypeCode = table.Column<string>(type: "varchar(40)", nullable: false),
                    SymbolCode = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    SymbolName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "varchar(30)", nullable: false),
                    WorkdayMultiplier = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsPaidByCompany = table.Column<bool>(type: "bit", nullable: false),
                    IsPaidBySocialInsurance = table.Column<bool>(type: "bit", nullable: false),
                    ColorHex = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "#198754"),
                    CssClass = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AttendanceSymbolDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PostLockAttendanceAdjustments",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TargetMonth = table.Column<int>(type: "int", nullable: false),
                    TargetYear = table.Column<int>(type: "int", nullable: false),
                    AdjustmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AppliedPayrollMonth = table.Column<int>(type: "int", nullable: false),
                    AppliedPayrollYear = table.Column<int>(type: "int", nullable: false),
                    AdjustmentType = table.Column<string>(type: "varchar(30)", nullable: false),
                    WorkdayDelta = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    OtHours150Delta = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    OtHours200Delta = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    OtHours300Delta = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    NightHoursDelta = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Pending"),
                    ApprovedByUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_PostLockAttendanceAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostLockAttendanceAdjustments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_AttendanceRecalculationBatches_Code",
                schema: "time",
                table: "AttendanceRecalculationBatches",
                column: "BatchCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_AttendanceSymbolDefinitions_TypeCode",
                schema: "time",
                table: "AttendanceSymbolDefinitions",
                column: "SymbolTypeCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PostLockAdjustments_TargetPeriod_Emp",
                schema: "time",
                table: "PostLockAttendanceAdjustments",
                columns: new[] { "TargetYear", "TargetMonth", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_PostLockAttendanceAdjustments_EmployeeId",
                schema: "time",
                table: "PostLockAttendanceAdjustments",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceRecalculationBatches",
                schema: "time");

            migrationBuilder.DropTable(
                name: "AttendanceSymbolDefinitions",
                schema: "time");

            migrationBuilder.DropTable(
                name: "PostLockAttendanceAdjustments",
                schema: "time");

            migrationBuilder.DropColumn(
                name: "ClosingStatus",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "ManagerConfirmedAt",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "ManagerConfirmedBy",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "ReviewDeadlineAt",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "ReviewWindowDays",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "TotalEarlyLeaveCount",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "TotalEarlyLeaveMinutes",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "TotalMissedPunchCount",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "TotalOtHours150Day",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "TotalOtHours150Night",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "TotalOtHours200Day",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "TotalOtHours200Night",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "TotalOtHours300Day",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "TotalOtHours300Night",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "TotalPaidPersonalLeaveDays",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "TotalUnexcusedAbsentDays",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "TotalUnpaidPersonalLeaveDays",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "UnitSubmittedAt",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "UnitSubmittedBy",
                schema: "time",
                table: "MonthlyAttendanceSummaries");

            migrationBuilder.DropColumn(
                name: "IsMissedPunch",
                schema: "time",
                table: "DailyAttendances");

            migrationBuilder.DropColumn(
                name: "OvertimeHours150Day",
                schema: "time",
                table: "DailyAttendances");

            migrationBuilder.DropColumn(
                name: "OvertimeHours150Night",
                schema: "time",
                table: "DailyAttendances");

            migrationBuilder.DropColumn(
                name: "OvertimeHours200Day",
                schema: "time",
                table: "DailyAttendances");

            migrationBuilder.DropColumn(
                name: "OvertimeHours200Night",
                schema: "time",
                table: "DailyAttendances");

            migrationBuilder.DropColumn(
                name: "OvertimeHours300Day",
                schema: "time",
                table: "DailyAttendances");

            migrationBuilder.DropColumn(
                name: "OvertimeHours300Night",
                schema: "time",
                table: "DailyAttendances");

            migrationBuilder.DropColumn(
                name: "PaidLeaveDays",
                schema: "time",
                table: "DailyAttendances");

            migrationBuilder.DropColumn(
                name: "StandardHours",
                schema: "time",
                table: "DailyAttendances");

            migrationBuilder.DropColumn(
                name: "UnpaidLeaveDays",
                schema: "time",
                table: "DailyAttendances");

            migrationBuilder.DropColumn(
                name: "EmployeeReviewWindowDays",
                schema: "time",
                table: "AttendancePolicies");

            migrationBuilder.DropColumn(
                name: "MissedPunchCreditWorkdays",
                schema: "time",
                table: "AttendancePolicies");

            migrationBuilder.DropColumn(
                name: "PunchPairingMode",
                schema: "time",
                table: "AttendancePolicies");

            migrationBuilder.AlterColumn<string>(
                name: "AttendanceSymbol",
                schema: "time",
                table: "DailyAttendances",
                type: "varchar(10)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)");
        }
    }
}
