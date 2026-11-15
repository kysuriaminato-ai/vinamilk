using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vinamilk.HRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveAndRequestPoliciesCl4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                schema: "time",
                table: "ShiftSwapRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                schema: "time",
                table: "ShiftSwapRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledByUsername",
                schema: "time",
                table: "ShiftSwapRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowInstanceId",
                schema: "time",
                table: "ShiftSwapRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnusedLeavePayout",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualWorkedOtHours",
                schema: "time",
                table: "OvertimeRequests",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                schema: "time",
                table: "OvertimeRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                schema: "time",
                table: "OvertimeRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledByUsername",
                schema: "time",
                table: "OvertimeRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompensationMode",
                schema: "time",
                table: "OvertimeRequests",
                type: "varchar(30)",
                nullable: false,
                defaultValue: "Paid");

            migrationBuilder.AddColumn<decimal>(
                name: "CompensatoryHoursCredited",
                schema: "time",
                table: "OvertimeRequests",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "LimitWarningMessage",
                schema: "time",
                table: "OvertimeRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OvertimeBatchCode",
                schema: "time",
                table: "OvertimeRequests",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReconciledOtHours",
                schema: "time",
                table: "OvertimeRequests",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "AttendanceSymbol",
                schema: "time",
                table: "LeaveTypes",
                type: "nvarchar(20)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(10)");

            migrationBuilder.AddColumn<bool>(
                name: "AllowHalfDay",
                schema: "time",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowHourly",
                schema: "time",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CountsAgainstAnnualLeave",
                schema: "time",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "time",
                table: "LeaveTypes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                schema: "time",
                table: "LeaveTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "time",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LegalReference",
                schema: "time",
                table: "LeaveTypes",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDaysPerRequest",
                schema: "time",
                table: "LeaveTypes",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsHrVerification",
                schema: "time",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresAttachment",
                schema: "time",
                table: "LeaveTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CancelReason",
                schema: "time",
                table: "LeaveRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                schema: "time",
                table: "LeaveRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledByUsername",
                schema: "time",
                table: "LeaveRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeaveDurationMode",
                schema: "time",
                table: "LeaveRequests",
                type: "varchar(20)",
                nullable: false,
                defaultValue: "Days");

            migrationBuilder.AddColumn<decimal>(
                name: "RequestedHours",
                schema: "time",
                table: "LeaveRequests",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AdvanceUsedDays",
                schema: "time",
                table: "LeaveBalances",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CompensatoryHoursBalance",
                schema: "time",
                table: "LeaveBalances",
                type: "decimal(6,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExpiredCarryForwardDays",
                schema: "time",
                table: "LeaveBalances",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsTerminationSettled",
                schema: "time",
                table: "LeaveBalances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TerminationPayoutAmount",
                schema: "time",
                table: "LeaveBalances",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TerminationPayoutDays",
                schema: "time",
                table: "LeaveBalances",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TerminationPayrollPeriodId",
                schema: "time",
                table: "LeaveBalances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TerminationSettledAt",
                schema: "time",
                table: "LeaveBalances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UsedCarriedForwardDays",
                schema: "time",
                table: "LeaveBalances",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDailyOvertimeHours",
                schema: "time",
                table: "AttendanceStatutoryParameters",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxSpecialIndustryYearlyOvertimeHours",
                schema: "time",
                table: "AttendanceStatutoryParameters",
                type: "decimal(6,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxTotalDailyWorkingAndOtHours",
                schema: "time",
                table: "AttendanceStatutoryParameters",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SeniorityBonusDaysPerInterval",
                schema: "time",
                table: "AttendanceStatutoryParameters",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SeniorityBonusYearsInterval",
                schema: "time",
                table: "AttendanceStatutoryParameters",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardAnnualLeaveDays",
                schema: "time",
                table: "AttendanceStatutoryParameters",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "AllowAdvanceAnnualLeave",
                schema: "time",
                table: "AttendancePolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowCompensatoryLeaveForOt",
                schema: "time",
                table: "AttendancePolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualLeaveCarryForwardCapDays",
                schema: "time",
                table: "AttendancePolicies",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 5m);

            migrationBuilder.AddColumn<bool>(
                name: "BlockOvertimeWhenExceedingLimit",
                schema: "time",
                table: "AttendancePolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CarryForwardExpiryDay",
                schema: "time",
                table: "AttendancePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CarryForwardExpiryMonth",
                schema: "time",
                table: "AttendancePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxAdvanceAnnualLeaveDays",
                schema: "time",
                table: "AttendancePolicies",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 3m);

            migrationBuilder.AddColumn<int>(
                name: "MidMonthHireCutoffDay",
                schema: "time",
                table: "AttendancePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OtReconciliationMode",
                schema: "time",
                table: "AttendancePolicies",
                type: "varchar(40)",
                nullable: false,
                defaultValue: "MinOfApprovedAndActual");

            migrationBuilder.AddColumn<int>(
                name: "OvertimeWarningThresholdPercent",
                schema: "time",
                table: "AttendancePolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "UseSpecialIndustry300OtLimit",
                schema: "time",
                table: "AttendancePolicies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EmployeeTimeRequests",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    RequestType = table.Column<string>(type: "varchar(30)", nullable: false),
                    SubType = table.Column<string>(type: "varchar(30)", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PlannedTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    ProposedEndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    RequestedMinutes = table.Column<int>(type: "int", nullable: true),
                    LocationOrDestination = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Pending"),
                    WorkflowInstanceId = table.Column<int>(type: "int", nullable: true),
                    ApprovedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApproverComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledByUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeTimeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTimeRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RequestCancellationRequests",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequesterEmployeeId = table.Column<int>(type: "int", nullable: false),
                    OriginalEntityType = table.Column<string>(type: "varchar(40)", nullable: false),
                    OriginalEntityId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Pending"),
                    WorkflowInstanceId = table.Column<int>(type: "int", nullable: true),
                    ProcessedByUserId = table.Column<int>(type: "int", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApproverComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RestoredBalanceSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestCancellationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestCancellationRequests_Employees_RequesterEmployeeId",
                        column: x => x.RequesterEmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TerminationLeaveSettlements",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    SettlementYear = table.Column<int>(type: "int", nullable: false),
                    TerminationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ProRatedEntitlementDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SeniorityExtraDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ValidCarryForwardDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    UsedDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    UnusedPayableDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    BaseSalaryUsed = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    StandardWorkdaysUsed = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PayoutAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    TargetPayrollMonth = table.Column<int>(type: "int", nullable: false),
                    TargetPayrollYear = table.Column<int>(type: "int", nullable: false),
                    PayrollPeriodId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "PushedToPayroll"),
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
                    table.PrimaryKey("PK_TerminationLeaveSettlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TerminationLeaveSettlements_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTimeRequests_Emp_Type_Start",
                schema: "time",
                table: "EmployeeTimeRequests",
                columns: new[] { "EmployeeId", "RequestType", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestCancellationRequests_Entity",
                schema: "time",
                table: "RequestCancellationRequests",
                columns: new[] { "OriginalEntityType", "OriginalEntityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestCancellationRequests_RequesterEmployeeId",
                schema: "time",
                table: "RequestCancellationRequests",
                column: "RequesterEmployeeId");

            migrationBuilder.CreateIndex(
                name: "UQ_TerminationLeaveSettlements_Emp_Year",
                schema: "time",
                table: "TerminationLeaveSettlements",
                columns: new[] { "EmployeeId", "SettlementYear" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeTimeRequests",
                schema: "time");

            migrationBuilder.DropTable(
                name: "RequestCancellationRequests",
                schema: "time");

            migrationBuilder.DropTable(
                name: "TerminationLeaveSettlements",
                schema: "time");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                schema: "time",
                table: "ShiftSwapRequests");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                schema: "time",
                table: "ShiftSwapRequests");

            migrationBuilder.DropColumn(
                name: "CancelledByUsername",
                schema: "time",
                table: "ShiftSwapRequests");

            migrationBuilder.DropColumn(
                name: "WorkflowInstanceId",
                schema: "time",
                table: "ShiftSwapRequests");

            migrationBuilder.DropColumn(
                name: "UnusedLeavePayout",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "ActualWorkedOtHours",
                schema: "time",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                schema: "time",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                schema: "time",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "CancelledByUsername",
                schema: "time",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "CompensationMode",
                schema: "time",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "CompensatoryHoursCredited",
                schema: "time",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "LimitWarningMessage",
                schema: "time",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "OvertimeBatchCode",
                schema: "time",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "ReconciledOtHours",
                schema: "time",
                table: "OvertimeRequests");

            migrationBuilder.DropColumn(
                name: "AllowHalfDay",
                schema: "time",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "AllowHourly",
                schema: "time",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "CountsAgainstAnnualLeave",
                schema: "time",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "time",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                schema: "time",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "time",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "LegalReference",
                schema: "time",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "MaxDaysPerRequest",
                schema: "time",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "NeedsHrVerification",
                schema: "time",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "RequiresAttachment",
                schema: "time",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "CancelReason",
                schema: "time",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                schema: "time",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "CancelledByUsername",
                schema: "time",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "LeaveDurationMode",
                schema: "time",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "RequestedHours",
                schema: "time",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "AdvanceUsedDays",
                schema: "time",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "CompensatoryHoursBalance",
                schema: "time",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "ExpiredCarryForwardDays",
                schema: "time",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "IsTerminationSettled",
                schema: "time",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "TerminationPayoutAmount",
                schema: "time",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "TerminationPayoutDays",
                schema: "time",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "TerminationPayrollPeriodId",
                schema: "time",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "TerminationSettledAt",
                schema: "time",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "UsedCarriedForwardDays",
                schema: "time",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "MaxDailyOvertimeHours",
                schema: "time",
                table: "AttendanceStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "MaxSpecialIndustryYearlyOvertimeHours",
                schema: "time",
                table: "AttendanceStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "MaxTotalDailyWorkingAndOtHours",
                schema: "time",
                table: "AttendanceStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "SeniorityBonusDaysPerInterval",
                schema: "time",
                table: "AttendanceStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "SeniorityBonusYearsInterval",
                schema: "time",
                table: "AttendanceStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "StandardAnnualLeaveDays",
                schema: "time",
                table: "AttendanceStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "AllowAdvanceAnnualLeave",
                schema: "time",
                table: "AttendancePolicies");

            migrationBuilder.DropColumn(
                name: "AllowCompensatoryLeaveForOt",
                schema: "time",
                table: "AttendancePolicies");

            migrationBuilder.DropColumn(
                name: "AnnualLeaveCarryForwardCapDays",
                schema: "time",
                table: "AttendancePolicies");

            migrationBuilder.DropColumn(
                name: "BlockOvertimeWhenExceedingLimit",
                schema: "time",
                table: "AttendancePolicies");

            migrationBuilder.DropColumn(
                name: "CarryForwardExpiryDay",
                schema: "time",
                table: "AttendancePolicies");

            migrationBuilder.DropColumn(
                name: "CarryForwardExpiryMonth",
                schema: "time",
                table: "AttendancePolicies");

            migrationBuilder.DropColumn(
                name: "MaxAdvanceAnnualLeaveDays",
                schema: "time",
                table: "AttendancePolicies");

            migrationBuilder.DropColumn(
                name: "MidMonthHireCutoffDay",
                schema: "time",
                table: "AttendancePolicies");

            migrationBuilder.DropColumn(
                name: "OtReconciliationMode",
                schema: "time",
                table: "AttendancePolicies");

            migrationBuilder.DropColumn(
                name: "OvertimeWarningThresholdPercent",
                schema: "time",
                table: "AttendancePolicies");

            migrationBuilder.DropColumn(
                name: "UseSpecialIndustry300OtLimit",
                schema: "time",
                table: "AttendancePolicies");

            migrationBuilder.AlterColumn<string>(
                name: "AttendanceSymbol",
                schema: "time",
                table: "LeaveTypes",
                type: "varchar(10)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)");
        }
    }
}
