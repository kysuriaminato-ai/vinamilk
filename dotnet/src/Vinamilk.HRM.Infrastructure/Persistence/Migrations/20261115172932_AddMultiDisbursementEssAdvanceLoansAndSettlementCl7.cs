using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vinamilk.HRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiDisbursementEssAdvanceLoansAndSettlementCl7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_PayrollPeriods_Year_Month_Type",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                schema: "payroll",
                table: "SalaryAdvances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserId",
                schema: "payroll",
                table: "SalaryAdvances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApproverComment",
                schema: "payroll",
                table: "SalaryAdvances",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisbursementPayrollPeriodId",
                schema: "payroll",
                table: "SalaryAdvances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxAllowedAmountSnapshot",
                schema: "payroll",
                table: "SalaryAdvances",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PolicyGroupIdUsed",
                schema: "payroll",
                table: "SalaryAdvances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TerminationSettlementId",
                schema: "payroll",
                table: "SalaryAdvances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowInstanceId",
                schema: "payroll",
                table: "SalaryAdvances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSeveranceStatutoryTaxExempt",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SeveranceAllowanceMonthsPerYear",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "decimal(6,2)",
                nullable: false,
                defaultValue: 0.50m);

            migrationBuilder.AddColumn<int>(
                name: "SeveranceMinTenureMonths",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "int",
                nullable: false,
                defaultValue: 12);

            migrationBuilder.AddColumn<int>(
                name: "SeveranceRoundingHalfYearMonths",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "int",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DisbursementDate",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisbursementSequence",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "AdjustmentCode",
                schema: "payroll",
                table: "PayrollAdjustments",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "RecalculateTaxInSourcePeriod",
                schema: "payroll",
                table: "PayrollAdjustments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SourcePeriodTaxDelta",
                schema: "payroll",
                table: "PayrollAdjustments",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TaxCalculationNote",
                schema: "payroll",
                table: "PayrollAdjustments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TerminationSettlementId",
                schema: "payroll",
                table: "PayrollAdjustments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TerminationSettlementId",
                schema: "payroll",
                table: "EmployeeLoanInstallments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeTerminationSettlements",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettlementCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LastWorkingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TerminationType = table.Column<string>(type: "varchar(40)", nullable: false, defaultValue: "Resignation"),
                    IsEligibleForSeverance = table.Column<bool>(type: "bit", nullable: false),
                    StandardWorkdaysUsed = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 26m),
                    WorkedDaysInFinalMonth = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    BaseSalaryUsed = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    PositionAllowanceUsed = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    FinalMonthSalaryAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    AverageSalaryLast6Months = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    TotalTenureMonths = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    UnemploymentInsuranceCoveredMonths = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    EligibleSeveranceYears = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    StatutorySeveranceRatePerYear = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    CompanyExtraSeveranceRatePerYear = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    StatutorySeveranceAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    CompanyExtraSeveranceAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    UnusedLeaveDays = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    UnusedLeavePayoutAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    OtherBonusOrRetroAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    TotalGrossSettlement = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    EmployeeInsuranceDeduction = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    PersonalIncomeTaxDeduction = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    SalaryAdvanceDeduction = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    LoanPayoffDeduction = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    OtherDeduction = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    NetSettlementAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    RemainingDebtToRecover = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    CalculationBreakdownJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Draft"),
                    TargetPayrollYear = table.Column<int>(type: "int", nullable: false),
                    TargetPayrollMonth = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeTerminationSettlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTerminationSettlements_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollPolicies",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PolicyGroupId = table.Column<int>(type: "int", nullable: false),
                    MaxAdvanceLimitPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 50.00m),
                    AdvanceMinTenureDays = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    AdvanceCutoffDayOfMonth = table.Column<int>(type: "int", nullable: false, defaultValue: 25),
                    SettleLoansOnTermination = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RetroTaxMode = table.Column<string>(type: "varchar(40)", nullable: false, defaultValue: "RecalculateSourcePeriod"),
                    ExtraSeveranceMonthsPerYear = table.Column<decimal>(type: "decimal(6,2)", nullable: false, defaultValue: 0m),
                    PayMidMonthAdvanceWithoutPit = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LegalReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NeedsHrVerification = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollPolicies_PolicyGroups_PolicyGroupId",
                        column: x => x.PolicyGroupId,
                        principalSchema: "time",
                        principalTable: "PolicyGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_PayrollPeriods_Year_Month_Type_Seq",
                schema: "payroll",
                table: "PayrollPeriods",
                columns: new[] { "Year", "Month", "PeriodType", "DisbursementSequence" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTerminationSettlements_Emp_Period",
                schema: "payroll",
                table: "EmployeeTerminationSettlements",
                columns: new[] { "EmployeeId", "TargetPayrollYear", "TargetPayrollMonth" });

            migrationBuilder.CreateIndex(
                name: "UQ_EmployeeTerminationSettlements_Code",
                schema: "payroll",
                table: "EmployeeTerminationSettlements",
                column: "SettlementCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_PayrollPolicies_PolicyGroupId",
                schema: "payroll",
                table: "PayrollPolicies",
                column: "PolicyGroupId",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeTerminationSettlements",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollPolicies",
                schema: "payroll");

            migrationBuilder.DropIndex(
                name: "UQ_PayrollPeriods_Year_Month_Type_Seq",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                schema: "payroll",
                table: "SalaryAdvances");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                schema: "payroll",
                table: "SalaryAdvances");

            migrationBuilder.DropColumn(
                name: "ApproverComment",
                schema: "payroll",
                table: "SalaryAdvances");

            migrationBuilder.DropColumn(
                name: "DisbursementPayrollPeriodId",
                schema: "payroll",
                table: "SalaryAdvances");

            migrationBuilder.DropColumn(
                name: "MaxAllowedAmountSnapshot",
                schema: "payroll",
                table: "SalaryAdvances");

            migrationBuilder.DropColumn(
                name: "PolicyGroupIdUsed",
                schema: "payroll",
                table: "SalaryAdvances");

            migrationBuilder.DropColumn(
                name: "TerminationSettlementId",
                schema: "payroll",
                table: "SalaryAdvances");

            migrationBuilder.DropColumn(
                name: "WorkflowInstanceId",
                schema: "payroll",
                table: "SalaryAdvances");

            migrationBuilder.DropColumn(
                name: "IsSeveranceStatutoryTaxExempt",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "SeveranceAllowanceMonthsPerYear",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "SeveranceMinTenureMonths",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "SeveranceRoundingHalfYearMonths",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "DisbursementDate",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "DisbursementSequence",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "AdjustmentCode",
                schema: "payroll",
                table: "PayrollAdjustments");

            migrationBuilder.DropColumn(
                name: "RecalculateTaxInSourcePeriod",
                schema: "payroll",
                table: "PayrollAdjustments");

            migrationBuilder.DropColumn(
                name: "SourcePeriodTaxDelta",
                schema: "payroll",
                table: "PayrollAdjustments");

            migrationBuilder.DropColumn(
                name: "TaxCalculationNote",
                schema: "payroll",
                table: "PayrollAdjustments");

            migrationBuilder.DropColumn(
                name: "TerminationSettlementId",
                schema: "payroll",
                table: "PayrollAdjustments");

            migrationBuilder.DropColumn(
                name: "TerminationSettlementId",
                schema: "payroll",
                table: "EmployeeLoanInstallments");

            migrationBuilder.CreateIndex(
                name: "UQ_PayrollPeriods_Year_Month_Type",
                schema: "payroll",
                table: "PayrollPeriods",
                columns: new[] { "Year", "Month", "PeriodType" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
