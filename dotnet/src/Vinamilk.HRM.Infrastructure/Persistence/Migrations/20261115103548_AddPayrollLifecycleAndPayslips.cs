using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vinamilk.HRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollLifecycleAndPayslips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasHighVarianceWarning",
                schema: "payroll",
                table: "PayrollRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OtHours150",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtHours200",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtHours300",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PitBracketsJson",
                schema: "payroll",
                table: "PayrollRecords",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PreviousPeriodNetSalary",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PreviousRevisionNetSalary",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnemploymentInsuranceBase",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VariancePercentVsPreviousPeriod",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(7,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CalculationVersion",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastUnlockReason",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LockedByUserId",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaidByUserId",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmittedByUserId",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnlockedAt",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnlockedByUserId",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowInstanceId",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PayrollCalculationRevisions",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollPeriodId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    RevisionNumber = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<string>(type: "varchar(30)", nullable: false),
                    AffectedEmployeeCount = table.Column<int>(type: "int", nullable: false),
                    TotalGrossIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPersonalIncomeTax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalNetSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetDeltaFromPreviousRevision = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HighVarianceWarningCount = table.Column<int>(type: "int", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CalculatedByUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollCalculationRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollCalculationRevisions_PayrollPeriods_PayrollPeriodId",
                        column: x => x.PayrollPeriodId,
                        principalSchema: "payroll",
                        principalTable: "PayrollPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayslipDispatchLogs",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollPeriodId = table.Column<int>(type: "int", nullable: false),
                    PayrollRecordId = table.Column<long>(type: "bigint", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EmployeeCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Queued"),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HangfireJobId = table.Column<string>(type: "varchar(100)", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayslipDispatchLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayslipDispatchLogs_PayrollPeriods_PayrollPeriodId",
                        column: x => x.PayrollPeriodId,
                        principalSchema: "payroll",
                        principalTable: "PayrollPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCalculationRevisions_Period_Rev",
                schema: "payroll",
                table: "PayrollCalculationRevisions",
                columns: new[] { "PayrollPeriodId", "RevisionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_PayslipDispatchLogs_Period_Batch",
                schema: "payroll",
                table: "PayslipDispatchLogs",
                columns: new[] { "PayrollPeriodId", "BatchId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollCalculationRevisions",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayslipDispatchLogs",
                schema: "payroll");

            migrationBuilder.DropColumn(
                name: "HasHighVarianceWarning",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "OtHours150",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "OtHours200",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "OtHours300",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "PitBracketsJson",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "PreviousPeriodNetSalary",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "PreviousRevisionNetSalary",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "UnemploymentInsuranceBase",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "VariancePercentVsPreviousPeriod",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "CalculationVersion",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "LastUnlockReason",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "LockedByUserId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "PaidByUserId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "UnlockedAt",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "UnlockedByUserId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "WorkflowInstanceId",
                schema: "payroll",
                table: "PayrollPeriods");
        }
    }
}
