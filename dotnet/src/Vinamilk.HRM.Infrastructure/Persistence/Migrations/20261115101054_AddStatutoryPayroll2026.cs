using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vinamilk.HRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStatutoryPayroll2026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WageRegion",
                schema: "core",
                table: "OrganizationUnits",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "WageRegion",
                schema: "core",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PayrollStatutoryParameters",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    LegalReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StatutoryBaseSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinWageRegion1 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinWageRegion2 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinWageRegion3 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinWageRegion4 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SocialInsuranceCapMultiplier = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    UnemploymentInsuranceCapMultiplier = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    EmployeeSiRate = table.Column<decimal>(type: "decimal(7,4)", nullable: false),
                    EmployeeHiRate = table.Column<decimal>(type: "decimal(7,4)", nullable: false),
                    EmployeeUiRate = table.Column<decimal>(type: "decimal(7,4)", nullable: false),
                    CompanySiRate = table.Column<decimal>(type: "decimal(7,4)", nullable: false),
                    CompanyHiRate = table.Column<decimal>(type: "decimal(7,4)", nullable: false),
                    CompanyUiRate = table.Column<decimal>(type: "decimal(7,4)", nullable: false),
                    CompanyUnionFeeRate = table.Column<decimal>(type: "decimal(7,4)", nullable: false),
                    SelfDeduction = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DependentDeduction = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollStatutoryParameters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PitBrackets",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BracketLevel = table.Column<int>(type: "int", nullable: false),
                    LowerBound = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UpperBound = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TaxRate = table.Column<decimal>(type: "decimal(7,4)", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    LegalReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PitBrackets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollStatutoryParameters_EffectivePeriod",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                columns: new[] { "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "UQ_PitBrackets_EffectiveFrom_Level",
                schema: "payroll",
                table: "PitBrackets",
                columns: new[] { "EffectiveFrom", "BracketLevel" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollStatutoryParameters",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PitBrackets",
                schema: "payroll");

            migrationBuilder.DropColumn(
                name: "WageRegion",
                schema: "core",
                table: "OrganizationUnits");

            migrationBuilder.DropColumn(
                name: "WageRegion",
                schema: "core",
                table: "Employees");
        }
    }
}
