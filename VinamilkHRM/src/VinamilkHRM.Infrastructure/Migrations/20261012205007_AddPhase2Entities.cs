using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinamilkHRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase2Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_CitizenId",
                table: "Employees");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnemploymentInsuranceDeduction",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDeductions",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxableIncome",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "SocialInsuranceDeduction",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "SalesCommission",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "PersonalIncomeTax",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "PersonalDeduction",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "OvertimePay",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "OtherBonuses",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "NightShiftPay",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "NetSalary",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "LunchAllowance",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "KpiBonus",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "HealthInsuranceDeduction",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "HazardousAllowance",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "GrossSalary",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "DependentDeduction",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "BaseSalary",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Allowance",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<bool>(
                name: "IsImported",
                table: "PaySlips",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LegacyId",
                table: "PaySlips",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BranchTerritory",
                table: "OrganizationalUnits",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostCenterCode",
                table: "OrganizationalUnits",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DesignedCapacityPerHour",
                table: "OrganizationalUnits",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FarmSpecialty",
                table: "OrganizationalUnits",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegacyId",
                table: "OrganizationalUnits",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LineNumber",
                table: "OrganizationalUnits",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumWageRegion",
                table: "OrganizationalUnits",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "PlannedHeadcount",
                table: "OrganizationalUnits",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WorkPattern",
                table: "OrganizationalUnits",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<decimal>(
                name: "InsuranceSalary",
                table: "Employees",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<string>(
                name: "CitizenId",
                table: "Employees",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "BaseSalary",
                table: "Employees",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "Allowance",
                table: "Employees",
                type: "decimal(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "Employees",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankBranch",
                table: "Employees",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Employees",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CitizenIdIssueDate",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CitizenIdIssuePlace",
                table: "Employees",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactName",
                table: "Employees",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                table: "Employees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactRelationship",
                table: "Employees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "JobGradeId",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegacyId",
                table: "Employees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalaryStep",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StandardJobTitleId",
                table: "Employees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxCode",
                table: "Employees",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsImported",
                table: "Attendances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LegacyId",
                table: "Attendances",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AllowanceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsInsurable = table.Column<bool>(type: "bit", nullable: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false),
                    TaxExemptCap = table.Column<decimal>(type: "decimal(18,0)", nullable: false, defaultValue: 0m),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllowanceTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dependents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TaxCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CitizenId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    FromMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    ToMonth = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dependents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dependents_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ChecksumSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAttachments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeCertificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    CertificateName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CertificateCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IssuingAuthority = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ScoreOrGrade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeCertificates_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeEducations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    DegreeLevel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Major = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SchoolName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    GraduationYear = table.Column<int>(type: "int", nullable: true),
                    Classification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EducationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeEducations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeEducations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeWorkHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MainResponsibilities = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReferenceContact = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LegacyId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeWorkHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkHistories_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    EmployeeRate = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    EmployerRate = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    LegalReference = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobFamilies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FamilyCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FamilyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobFamilies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobGrades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GradeNumber = table.Column<int>(type: "int", nullable: false),
                    GradeCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GradeName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LegacyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobGrades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MinimumWages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Region = table.Column<int>(type: "int", nullable: false),
                    MonthlyAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    HourlyAmount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    LegalReference = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MinimumWages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OvertimeRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DayKind = table.Column<int>(type: "int", nullable: false),
                    Night = table.Column<bool>(type: "bit", nullable: false),
                    Multiplier = table.Column<decimal>(type: "decimal(4,2)", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OvertimeRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicHolidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicHolidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatutoryParameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    LegalReference = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatutoryParameters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxBrackets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    UpperBoundMonthly = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
                    Rate = table.Column<decimal>(type: "decimal(5,4)", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxBrackets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitManagerHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationalUnitId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AppointmentDecisionNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitManagerHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitManagerHistories_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitManagerHistories_OrganizationalUnits_OrganizationalUnitId",
                        column: x => x.OrganizationalUnitId,
                        principalTable: "OrganizationalUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnitMinimumWageRegionHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganizationalUnitId = table.Column<int>(type: "int", nullable: false),
                    MinimumWageRegion = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LegalReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitMinimumWageRegionHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitMinimumWageRegionHistories_OrganizationalUnits_OrganizationalUnitId",
                        column: x => x.OrganizationalUnitId,
                        principalTable: "OrganizationalUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractAllowances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LaborContractId = table.Column<int>(type: "int", nullable: false),
                    AllowanceTypeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractAllowances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractAllowances_AllowanceTypes_AllowanceTypeId",
                        column: x => x.AllowanceTypeId,
                        principalTable: "AllowanceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContractAllowances_LaborContracts_LaborContractId",
                        column: x => x.LaborContractId,
                        principalTable: "LaborContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobTitles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobTitleCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JobTitleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    JobFamilyId = table.Column<int>(type: "int", nullable: false),
                    LeadershipLevel = table.Column<int>(type: "int", nullable: false),
                    StandardJobGradeId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LegacyId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTitles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobTitles_JobFamilies_JobFamilyId",
                        column: x => x.JobFamilyId,
                        principalTable: "JobFamilies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobTitles_JobGrades_StandardJobGradeId",
                        column: x => x.StandardJobGradeId,
                        principalTable: "JobGrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SalaryScales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobGradeId = table.Column<int>(type: "int", nullable: false),
                    Step = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    LegalReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryScales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryScales_JobGrades_JobGradeId",
                        column: x => x.JobGradeId,
                        principalTable: "JobGrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LookupItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LookupItems_LookupCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "LookupCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaySlips_LegacyId",
                table: "PaySlips",
                column: "LegacyId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationalUnits_LegacyId",
                table: "OrganizationalUnits",
                column: "LegacyId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_JobGradeId",
                table: "Employees",
                column: "JobGradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_LegacyId",
                table: "Employees",
                column: "LegacyId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_StandardJobTitleId",
                table: "Employees",
                column: "StandardJobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_LegacyId",
                table: "Attendances",
                column: "LegacyId");

            migrationBuilder.CreateIndex(
                name: "IX_AllowanceTypes_Code",
                table: "AllowanceTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContractAllowances_AllowanceTypeId",
                table: "ContractAllowances",
                column: "AllowanceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAllowances_LaborContractId_AllowanceTypeId",
                table: "ContractAllowances",
                columns: new[] { "LaborContractId", "AllowanceTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Dependents_EmployeeId_FromMonth",
                table: "Dependents",
                columns: new[] { "EmployeeId", "FromMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttachments_EmployeeId",
                table: "EmployeeAttachments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCertificates_EmployeeId_ExpiryDate",
                table: "EmployeeCertificates",
                columns: new[] { "EmployeeId", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEducations_EmployeeId",
                table: "EmployeeEducations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkHistories_EmployeeId_FromDate",
                table: "EmployeeWorkHistories",
                columns: new[] { "EmployeeId", "FromDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkHistories_LegacyId",
                table: "EmployeeWorkHistories",
                column: "LegacyId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceRates_Kind_EffectiveFrom",
                table: "InsuranceRates",
                columns: new[] { "Kind", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_JobFamilies_FamilyCode",
                table: "JobFamilies",
                column: "FamilyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobGrades_GradeCode",
                table: "JobGrades",
                column: "GradeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobGrades_GradeNumber",
                table: "JobGrades",
                column: "GradeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobGrades_LegacyId",
                table: "JobGrades",
                column: "LegacyId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTitles_JobFamilyId",
                table: "JobTitles",
                column: "JobFamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTitles_JobTitleCode",
                table: "JobTitles",
                column: "JobTitleCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobTitles_LegacyId",
                table: "JobTitles",
                column: "LegacyId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTitles_StandardJobGradeId",
                table: "JobTitles",
                column: "StandardJobGradeId");

            migrationBuilder.CreateIndex(
                name: "IX_LookupCategories_Code",
                table: "LookupCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupItems_CategoryId_Code",
                table: "LookupItems",
                columns: new[] { "CategoryId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MinimumWages_Region_EffectiveFrom",
                table: "MinimumWages",
                columns: new[] { "Region", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRates_DayKind_Night_EffectiveFrom",
                table: "OvertimeRates",
                columns: new[] { "DayKind", "Night", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidays_Date",
                table: "PublicHolidays",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidays_Year",
                table: "PublicHolidays",
                column: "Year");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryScales_JobGradeId_Step_EffectiveFrom",
                table: "SalaryScales",
                columns: new[] { "JobGradeId", "Step", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_StatutoryParameters_Code_EffectiveFrom",
                table: "StatutoryParameters",
                columns: new[] { "Code", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxBrackets_ScheduleCode_Order_EffectiveFrom",
                table: "TaxBrackets",
                columns: new[] { "ScheduleCode", "Order", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitManagerHistories_EmployeeId",
                table: "UnitManagerHistories",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitManagerHistories_OrganizationalUnitId_FromDate",
                table: "UnitManagerHistories",
                columns: new[] { "OrganizationalUnitId", "FromDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitMinimumWageRegionHistories_OrganizationalUnitId_EffectiveFrom",
                table: "UnitMinimumWageRegionHistories",
                columns: new[] { "OrganizationalUnitId", "EffectiveFrom" });

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_JobGrades_JobGradeId",
                table: "Employees",
                column: "JobGradeId",
                principalTable: "JobGrades",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_JobTitles_StandardJobTitleId",
                table: "Employees",
                column: "StandardJobTitleId",
                principalTable: "JobTitles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_JobGrades_JobGradeId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_JobTitles_StandardJobTitleId",
                table: "Employees");

            migrationBuilder.DropTable(
                name: "ContractAllowances");

            migrationBuilder.DropTable(
                name: "Dependents");

            migrationBuilder.DropTable(
                name: "EmployeeAttachments");

            migrationBuilder.DropTable(
                name: "EmployeeCertificates");

            migrationBuilder.DropTable(
                name: "EmployeeEducations");

            migrationBuilder.DropTable(
                name: "EmployeeWorkHistories");

            migrationBuilder.DropTable(
                name: "InsuranceRates");

            migrationBuilder.DropTable(
                name: "JobTitles");

            migrationBuilder.DropTable(
                name: "LookupItems");

            migrationBuilder.DropTable(
                name: "MinimumWages");

            migrationBuilder.DropTable(
                name: "OvertimeRates");

            migrationBuilder.DropTable(
                name: "PublicHolidays");

            migrationBuilder.DropTable(
                name: "SalaryScales");

            migrationBuilder.DropTable(
                name: "StatutoryParameters");

            migrationBuilder.DropTable(
                name: "TaxBrackets");

            migrationBuilder.DropTable(
                name: "UnitManagerHistories");

            migrationBuilder.DropTable(
                name: "UnitMinimumWageRegionHistories");

            migrationBuilder.DropTable(
                name: "AllowanceTypes");

            migrationBuilder.DropTable(
                name: "JobFamilies");

            migrationBuilder.DropTable(
                name: "LookupCategories");

            migrationBuilder.DropTable(
                name: "JobGrades");

            migrationBuilder.DropIndex(
                name: "IX_PaySlips_LegacyId",
                table: "PaySlips");

            migrationBuilder.DropIndex(
                name: "IX_OrganizationalUnits_LegacyId",
                table: "OrganizationalUnits");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Email",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_JobGradeId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_LegacyId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_StandardJobTitleId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_LegacyId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "IsImported",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "LegacyId",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "BranchTerritory",
                table: "OrganizationalUnits");

            migrationBuilder.DropColumn(
                name: "CostCenterCode",
                table: "OrganizationalUnits");

            migrationBuilder.DropColumn(
                name: "DesignedCapacityPerHour",
                table: "OrganizationalUnits");

            migrationBuilder.DropColumn(
                name: "FarmSpecialty",
                table: "OrganizationalUnits");

            migrationBuilder.DropColumn(
                name: "LegacyId",
                table: "OrganizationalUnits");

            migrationBuilder.DropColumn(
                name: "LineNumber",
                table: "OrganizationalUnits");

            migrationBuilder.DropColumn(
                name: "MinimumWageRegion",
                table: "OrganizationalUnits");

            migrationBuilder.DropColumn(
                name: "PlannedHeadcount",
                table: "OrganizationalUnits");

            migrationBuilder.DropColumn(
                name: "WorkPattern",
                table: "OrganizationalUnits");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BankBranch",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CitizenIdIssueDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CitizenIdIssuePlace",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContactName",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContactRelationship",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "JobGradeId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "LegacyId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SalaryStep",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "StandardJobTitleId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TaxCode",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsImported",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "LegacyId",
                table: "Attendances");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnemploymentInsuranceDeduction",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDeductions",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxableIncome",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "SocialInsuranceDeduction",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "SalesCommission",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PersonalIncomeTax",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PersonalDeduction",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "OvertimePay",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "OtherBonuses",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "NightShiftPay",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "NetSalary",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "LunchAllowance",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "KpiBonus",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "HealthInsuranceDeduction",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "HazardousAllowance",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "GrossSalary",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DependentDeduction",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "BaseSalary",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Allowance",
                table: "PaySlips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "InsuranceSalary",
                table: "Employees",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<string>(
                name: "CitizenId",
                table: "Employees",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250);

            migrationBuilder.AlterColumn<decimal>(
                name: "BaseSalary",
                table: "Employees",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Allowance",
                table: "Employees",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,0)");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CitizenId",
                table: "Employees",
                column: "CitizenId",
                unique: true);
        }
    }
}
