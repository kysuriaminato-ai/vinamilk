using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vinamilk.HRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollReportingAccountingBhxhPitCl8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayrollAccountingAccountConfigs",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountKey = table.Column<string>(type: "varchar(60)", nullable: false),
                    AccountCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    AccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalSide = table.Column<string>(type: "varchar(10)", nullable: false, defaultValue: "Credit"),
                    CostCenterPattern = table.Column<string>(type: "varchar(50)", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    LegalReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    NeedsHrVerification = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollAccountingAccountConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PitWithholdingCertificates",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CertificateCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    TaxYear = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    GrossIncome = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    TotalInsuranceDeducted = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    TotalFamilyDeductions = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    TaxableIncome = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    AssessableIncome = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    TotalPitWithheld = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    AnnualFinalTaxLiability = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    PitOverpaidOrUnderpaid = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    IssuedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IssuedByUserId = table.Column<int>(type: "int", nullable: false),
                    IssuedByUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Issued"),
                    LegalReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_PitWithholdingCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PitWithholdingCertificates_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SocialInsuranceReportTemplates",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateCode = table.Column<string>(type: "varchar(40)", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SoftwareVendor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    ColumnMappingsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LegalReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    NeedsHrVerification = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_SocialInsuranceReportTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAccountingConfigs_Key_Eff",
                schema: "payroll",
                table: "PayrollAccountingAccountConfigs",
                columns: new[] { "AccountKey", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_PitCertificates_Year_Emp",
                schema: "payroll",
                table: "PitWithholdingCertificates",
                columns: new[] { "TaxYear", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_PitWithholdingCertificates_EmployeeId",
                schema: "payroll",
                table: "PitWithholdingCertificates",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "UQ_PitCertificates_Code",
                schema: "payroll",
                table: "PitWithholdingCertificates",
                column: "CertificateCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_SocialInsuranceTemplates_Code",
                schema: "payroll",
                table: "SocialInsuranceReportTemplates",
                column: "TemplateCode",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollAccountingAccountConfigs",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PitWithholdingCertificates",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "SocialInsuranceReportTemplates",
                schema: "payroll");
        }
    }
}
