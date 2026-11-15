using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vinamilk.HRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalaryStructureAndFormulasCl5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FormulaExpression",
                schema: "payroll",
                table: "SalaryComponents",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CalculationType",
                schema: "payroll",
                table: "SalaryComponents",
                type: "varchar(30)",
                nullable: false,
                defaultValue: "Formula");

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultAmount",
                schema: "payroll",
                table: "SalaryComponents",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "payroll",
                table: "SalaryComponents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalReference",
                schema: "payroll",
                table: "SalaryComponents",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsHrVerification",
                schema: "payroll",
                table: "SalaryComponents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowOnPayslip",
                schema: "payroll",
                table: "SalaryComponents",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxExemptLimitParameterCode",
                schema: "payroll",
                table: "SalaryComponents",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinProbationSalaryRatePercent",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 85.00m);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsHrVerification",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "SalaryTemplates",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    TemplateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StaffGroup = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Office"),
                    DefaultPayFrequency = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Monthly"),
                    DefaultProbationSalaryRatePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 85.00m),
                    RoundingRule = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "RoundToDong"),
                    IsDefaultForCompany = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LegalReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    NeedsHrVerification = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_SalaryTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSalaryProfiles",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    BaseSalary = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    InsuranceSalary = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    SalaryScaleId = table.Column<int>(type: "int", nullable: true),
                    SalaryStepId = table.Column<int>(type: "int", nullable: true),
                    SalaryTemplateId = table.Column<int>(type: "int", nullable: true),
                    PayMode = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Monthly"),
                    UnitRate = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    IsProbation = table.Column<bool>(type: "bit", nullable: false),
                    ProbationRatePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 100.00m),
                    PositionAllowance = table.Column<decimal>(type: "decimal(18,0)", nullable: false, defaultValue: 0m),
                    OtherAllowance = table.Column<decimal>(type: "decimal(18,0)", nullable: false, defaultValue: 0m),
                    IsUnionMember = table.Column<bool>(type: "bit", nullable: false),
                    DecisionNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_EmployeeSalaryProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSalaryProfiles_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeSalaryProfiles_SalaryScales_SalaryScaleId",
                        column: x => x.SalaryScaleId,
                        principalSchema: "core",
                        principalTable: "SalaryScales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeSalaryProfiles_SalarySteps_SalaryStepId",
                        column: x => x.SalaryStepId,
                        principalSchema: "core",
                        principalTable: "SalarySteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeSalaryProfiles_SalaryTemplates_SalaryTemplateId",
                        column: x => x.SalaryTemplateId,
                        principalSchema: "payroll",
                        principalTable: "SalaryTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalaryTemplateComponents",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalaryTemplateId = table.Column<int>(type: "int", nullable: false),
                    SalaryComponentId = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    OverrideFormulaExpression = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DefaultFixedAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryTemplateComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryTemplateComponents_SalaryComponents_SalaryComponentId",
                        column: x => x.SalaryComponentId,
                        principalSchema: "payroll",
                        principalTable: "SalaryComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryTemplateComponents_SalaryTemplates_SalaryTemplateId",
                        column: x => x.SalaryTemplateId,
                        principalSchema: "payroll",
                        principalTable: "SalaryTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSalaryProfileAllowances",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeSalaryProfileId = table.Column<int>(type: "int", nullable: false),
                    SalaryComponentId = table.Column<int>(type: "int", nullable: false),
                    ComponentCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "system.job"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSalaryProfileAllowances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSalaryProfileAllowances_EmployeeSalaryProfiles_EmployeeSalaryProfileId",
                        column: x => x.EmployeeSalaryProfileId,
                        principalSchema: "payroll",
                        principalTable: "EmployeeSalaryProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeSalaryProfileAllowances_SalaryComponents_SalaryComponentId",
                        column: x => x.SalaryComponentId,
                        principalSchema: "payroll",
                        principalTable: "SalaryComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaryProfileAllowances_SalaryComponentId",
                schema: "payroll",
                table: "EmployeeSalaryProfileAllowances",
                column: "SalaryComponentId");

            migrationBuilder.CreateIndex(
                name: "UQ_EmpSalaryProfileAllowances_Profile_Comp",
                schema: "payroll",
                table: "EmployeeSalaryProfileAllowances",
                columns: new[] { "EmployeeSalaryProfileId", "SalaryComponentId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaryProfiles_Emp_Effective",
                schema: "payroll",
                table: "EmployeeSalaryProfiles",
                columns: new[] { "EmployeeId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaryProfiles_SalaryScaleId",
                schema: "payroll",
                table: "EmployeeSalaryProfiles",
                column: "SalaryScaleId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaryProfiles_SalaryStepId",
                schema: "payroll",
                table: "EmployeeSalaryProfiles",
                column: "SalaryStepId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSalaryProfiles_SalaryTemplateId",
                schema: "payroll",
                table: "EmployeeSalaryProfiles",
                column: "SalaryTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryTemplateComponents_SalaryComponentId",
                schema: "payroll",
                table: "SalaryTemplateComponents",
                column: "SalaryComponentId");

            migrationBuilder.CreateIndex(
                name: "UQ_SalaryTemplateComponents_Template_Comp",
                schema: "payroll",
                table: "SalaryTemplateComponents",
                columns: new[] { "SalaryTemplateId", "SalaryComponentId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_SalaryTemplates_TemplateCode",
                schema: "payroll",
                table: "SalaryTemplates",
                column: "TemplateCode",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeSalaryProfileAllowances",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "SalaryTemplateComponents",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "EmployeeSalaryProfiles",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "SalaryTemplates",
                schema: "payroll");

            migrationBuilder.DropColumn(
                name: "CalculationType",
                schema: "payroll",
                table: "SalaryComponents");

            migrationBuilder.DropColumn(
                name: "DefaultAmount",
                schema: "payroll",
                table: "SalaryComponents");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "payroll",
                table: "SalaryComponents");

            migrationBuilder.DropColumn(
                name: "LegalReference",
                schema: "payroll",
                table: "SalaryComponents");

            migrationBuilder.DropColumn(
                name: "NeedsHrVerification",
                schema: "payroll",
                table: "SalaryComponents");

            migrationBuilder.DropColumn(
                name: "ShowOnPayslip",
                schema: "payroll",
                table: "SalaryComponents");

            migrationBuilder.DropColumn(
                name: "TaxExemptLimitParameterCode",
                schema: "payroll",
                table: "SalaryComponents");

            migrationBuilder.DropColumn(
                name: "MinProbationSalaryRatePercent",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.DropColumn(
                name: "NeedsHrVerification",
                schema: "payroll",
                table: "PayrollStatutoryParameters");

            migrationBuilder.AlterColumn<string>(
                name: "FormulaExpression",
                schema: "payroll",
                table: "SalaryComponents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
