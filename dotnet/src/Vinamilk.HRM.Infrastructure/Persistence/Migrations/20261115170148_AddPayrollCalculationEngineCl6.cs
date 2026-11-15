using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vinamilk.HRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollCalculationEngineCl6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_PayrollPeriods_Year_Month",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.AddColumn<decimal>(
                name: "CompanyOccupationalAccidentRate",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "decimal(7,4)",
                nullable: false,
                defaultValue: 0.005m);

            migrationBuilder.AddColumn<decimal>(
                name: "EmployeeUnionFeeCapMultiplier",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "decimal(7,4)",
                nullable: false,
                defaultValue: 0.10m);

            migrationBuilder.AddColumn<decimal>(
                name: "EmployeeUnionFeeRate",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "decimal(7,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsOtExcessTaxExempt",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OtMultiplier150",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "decimal(6,2)",
                nullable: false,
                defaultValue: 1.50m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtMultiplier200",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "decimal(6,2)",
                nullable: false,
                defaultValue: 2.00m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtMultiplier300",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "decimal(6,2)",
                nullable: false,
                defaultValue: 3.00m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtNightOvertimeExtraRate",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "decimal(7,4)",
                nullable: false,
                defaultValue: 0.20m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtNightShiftBonusRate",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "decimal(7,4)",
                nullable: false,
                defaultValue: 0.30m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtTaxableBaseRate",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "decimal(7,4)",
                nullable: false,
                defaultValue: 1.00m);

            migrationBuilder.AddColumn<decimal>(
                name: "PayrollVarianceWarningThresholdPercent",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 20.00m);

            migrationBuilder.AddColumn<decimal>(
                name: "PitNonResidentRate",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "decimal(7,4)",
                nullable: false,
                defaultValue: 0.20m);

            migrationBuilder.AddColumn<decimal>(
                name: "PitShortTermRate",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "decimal(7,4)",
                nullable: false,
                defaultValue: 0.10m);

            migrationBuilder.AddColumn<decimal>(
                name: "PitShortTermThreshold",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 2000000m);

            migrationBuilder.AddColumn<string>(
                name: "CalculationTraceJson",
                schema: "payroll",
                table: "PayrollRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CommissionBonus",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CompanyOccupationalAccidentInsurance",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "EmployeeUnionFee",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsNetToGross",
                schema: "payroll",
                table: "PayrollRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "LoanInstallmentDeduction",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtNightExtraPay",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtTaxExemptAmount",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RetroDeductionAmount",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RetroEarningAmount",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SalaryAdvanceDeduction",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetNetSalary",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(18,0)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxResidencyMode",
                schema: "payroll",
                table: "PayrollRecords",
                type: "varchar(30)",
                nullable: false,
                defaultValue: "ResidentStandard");

            migrationBuilder.AddColumn<decimal>(
                name: "ThirteenthMonthBonus",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCompanyCost",
                schema: "payroll",
                table: "PayrollRecords",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "UsedCommitment08",
                schema: "payroll",
                table: "PayrollRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AttendanceWarningMessage",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAttendanceLockWarning",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PeriodType",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "varchar(30)",
                nullable: false,
                defaultValue: "Monthly");

            migrationBuilder.AddColumn<string>(
                name: "Commitment08Number",
                schema: "payroll",
                table: "EmployeeSalaryProfiles",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasPitCommitment08",
                schema: "payroll",
                table: "EmployeeSalaryProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsNetContract",
                schema: "payroll",
                table: "EmployeeSalaryProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetNetSalary",
                schema: "payroll",
                table: "EmployeeSalaryProfiles",
                type: "decimal(18,0)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxResidencyMode",
                schema: "payroll",
                table: "EmployeeSalaryProfiles",
                type: "varchar(30)",
                nullable: false,
                defaultValue: "ResidentStandard");

            migrationBuilder.CreateTable(
                name: "EmployeeLoans",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LoanCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    LoanType = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Loan"),
                    PrincipalAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    MonthlyInstallmentAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    TotalInstallments = table.Column<int>(type: "int", nullable: false),
                    StartYear = table.Column<int>(type: "int", nullable: false),
                    StartMonth = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Active"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeLoans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeLoans_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollAdjustments",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TargetYear = table.Column<int>(type: "int", nullable: false),
                    TargetMonth = table.Column<int>(type: "int", nullable: false),
                    SourceLockedPeriodId = table.Column<int>(type: "int", nullable: true),
                    AdjustmentType = table.Column<string>(type: "varchar(30)", nullable: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false),
                    IsSocialInsuranceBase = table.Column<bool>(type: "bit", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Approved"),
                    AppliedPayrollPeriodId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollAdjustments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalaryAdvances",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AdvanceNumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    RequestDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TargetYear = table.Column<int>(type: "int", nullable: false),
                    TargetMonth = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Approved"),
                    DeductedPayrollPeriodId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryAdvances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryAdvances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeLoanInstallments",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeLoanId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "int", nullable: false),
                    TargetYear = table.Column<int>(type: "int", nullable: false),
                    TargetMonth = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Scheduled"),
                    DeductedPayrollPeriodId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeLoanInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeLoanInstallments_EmployeeLoans_EmployeeLoanId",
                        column: x => x.EmployeeLoanId,
                        principalSchema: "payroll",
                        principalTable: "EmployeeLoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeLoanInstallments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_PayrollPeriods_Year_Month_Type",
                schema: "payroll",
                table: "PayrollPeriods",
                columns: new[] { "Year", "Month", "PeriodType" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoanInstallments_Emp_Period",
                schema: "payroll",
                table: "EmployeeLoanInstallments",
                columns: new[] { "EmployeeId", "TargetYear", "TargetMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoanInstallments_EmployeeLoanId",
                schema: "payroll",
                table: "EmployeeLoanInstallments",
                column: "EmployeeLoanId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoans_EmployeeId",
                schema: "payroll",
                table: "EmployeeLoans",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "UQ_EmployeeLoans_LoanCode",
                schema: "payroll",
                table: "EmployeeLoans",
                column: "LoanCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustments_Emp_Period",
                schema: "payroll",
                table: "PayrollAdjustments",
                columns: new[] { "EmployeeId", "TargetYear", "TargetMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_SalaryAdvances_Emp_Period",
                schema: "payroll",
                table: "SalaryAdvances",
                columns: new[] { "EmployeeId", "TargetYear", "TargetMonth" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeLoanInstallments",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollAdjustments",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "SalaryAdvances",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "EmployeeLoans",
                schema: "payroll");

            migrationBuilder.DropIndex(
                name: "UQ_PayrollPeriods_Year_Month_Type",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "CompanyOccupationalAccidentRate",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "EmployeeUnionFeeCapMultiplier",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "EmployeeUnionFeeRate",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "IsOtExcessTaxExempt",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "OtMultiplier150",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "OtMultiplier200",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "OtMultiplier300",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "OtNightOvertimeExtraRate",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "OtNightShiftBonusRate",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "OtTaxableBaseRate",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "PayrollVarianceWarningThresholdPercent",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "PitNonResidentRate",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "PitShortTermRate",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "PitShortTermThreshold",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "CalculationTraceJson",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "CommissionBonus",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "CompanyOccupationalAccidentInsurance",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "EmployeeUnionFee",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "IsNetToGross",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "LoanInstallmentDeduction",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "OtNightExtraPay",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "OtTaxExemptAmount",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "RetroDeductionAmount",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "RetroEarningAmount",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "SalaryAdvanceDeduction",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "TargetNetSalary",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "TaxResidencyMode",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "ThirteenthMonthBonus",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "TotalCompanyCost",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "UsedCommitment08",
                schema: "payroll",
                table: "PayrollRecords");

            migrationBuilder.DropColumn(
                name: "AttendanceWarningMessage",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "HasAttendanceLockWarning",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "PeriodType",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "Commitment08Number",
                schema: "payroll",
                table: "EmployeeSalaryProfiles");

            migrationBuilder.DropColumn(
                name: "HasPitCommitment08",
                schema: "payroll",
                table: "EmployeeSalaryProfiles");

            migrationBuilder.DropColumn(
                name: "IsNetContract",
                schema: "payroll",
                table: "EmployeeSalaryProfiles");

            migrationBuilder.DropColumn(
                name: "TargetNetSalary",
                schema: "payroll",
                table: "EmployeeSalaryProfiles");

            migrationBuilder.DropColumn(
                name: "TaxResidencyMode",
                schema: "payroll",
                table: "EmployeeSalaryProfiles");

            migrationBuilder.CreateIndex(
                name: "UQ_PayrollPeriods_Year_Month",
                schema: "payroll",
                table: "PayrollPeriods",
                columns: new[] { "Year", "Month" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
